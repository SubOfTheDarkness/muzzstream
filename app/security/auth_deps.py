"""
Модуль аутентификации на основе сессий
Предоставляет функции-зависимости для защиты маршрутов и извлечения текущего авторизованного пользователя из PostgreSQL
"""

from datetime import datetime, timezone
from fastapi import Depends, HTTPException, status, Security
from fastapi.security import APIKeyHeader
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select
from app.database import get_db
from app.models.session import Session as UserSession
from app.models.user import User
from app.models.author import Author, AuthorMember

api_key_header = APIKeyHeader(name="Authorization", auto_error=False)


async def get_current_user(
    token: str = Depends(api_key_header),
    db: AsyncSession = Depends(get_db)
) -> User:
    """
    Зависимость для проверки валидности сессии.
    Ищет токен в таблице 'sessions'. Если сессия найдена, возвращает
    объект текущего пользователя (User), иначе выкидывает HTTP 401 Unauthorized.
    """
    if not token:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="В заголовках запроса отсутствует аутентификационный токен сессии."
        )

    stmt = select(UserSession).where(UserSession.token == token)
    result = await db.execute(stmt)
    db_session = result.scalars().first()

    if not db_session:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Указанная сессия не найдена или её срок действия истёк."
        )

    user_stmt = select(User).where(User.user_id == db_session.user_id)
    user_result = await db.execute(user_stmt)
    user = user_result.scalars().first()

    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Пользователь, привязанный к данной сессии, отсутствует в системе."
        )

    return user

async def verify_artist_upload_rights(
    author_id: int,
    current_user: User = Depends(get_current_user),
    db: AsyncSession = Depends(get_db)
) -> Author:
    """Проверяет права пользователя на добавление треков и управление каталогом."""
    author = await db.get(Author, author_id)
    if not author:
        raise HTTPException(status_code=404, detail="Исполнитель не найден.")

    if author.status_until and datetime.now(timezone.utc) < author.status_until.replace(tzinfo=timezone.utc):
        if author.current_status == 5:  # Banned
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN, 
                detail="Группа забанена. Любые действия с каталогом заблокированы."
            )
        if author.current_status == 4:  # Limited
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN, 
                detail="Исполнителю временно запрещено выкладывать новые треки."
            )

    stmt = select(AuthorMember).where(
        AuthorMember.author_id == author_id,
        AuthorMember.user_id == current_user.user_id
    )
    result = await db.execute(stmt)
    member_record = result.scalars().first()

    if not member_record or member_record.group_role_id not in [1, 2]:
        raise HTTPException(status_code=403, detail="Недостаточно прав. Требуется роль Owner или Manager.")

    return author
