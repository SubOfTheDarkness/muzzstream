"""
Модуль маршрутизации для управления карточками исполнителей
"""

import os
import secrets
import shutil
from typing import Optional
from fastapi import APIRouter, HTTPException, Depends, status, UploadFile, File, Form
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, text

from app.config import settings
from app.database import get_db
from app.models.author import Author, AuthorMember
from app.models.catalog import Composition, Track
from app.models.user import User
from app.security.auth_deps import get_current_user, verify_artist_upload_rights
from app.schemas.author import AuthorCreateRequestSchema, AuthorResponseSchema, ChangeMemberRoleRequestSchema
from app.schemas.catalog import TrackUpdateRequestSchema

router = APIRouter()


@router.post("/apply", response_model=AuthorResponseSchema, status_code=status.HTTP_201_CREATED)
async def create_author_application(
    payload: AuthorCreateRequestSchema,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
) -> Author:
    """
    Подача заявки пользователем на создание новой карточки исполнителя / группы.
    Создает запись в таблице 'authors' со статусом 'Unverified' (ID=1) по умолчанию.
    Пользователь, отправивший запрос, автоматически фиксируется в СУБД как создатель и Владелец (Owner)
    """
    try:
        stmt_check = select(Author).where(Author.author_name == payload.author_name)
        check_result = await db.execute(stmt_check)
        if check_result.scalars().first():
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST, 
                detail="Исполнитель с таким именем уже зарегистрирован в системе"
            )

        new_author = Author(
            author_name=payload.author_name,
            country=payload.country,
            current_status=1,
            created_by_user_id=current_user.user_id
        )
        db.add(new_author)
        await db.flush()

        new_member = AuthorMember(
            author_id=new_author.author_id,
            user_id=current_user.user_id,
            group_role_id=1
        )
        db.add(new_member)
        await db.commit()

        return new_author

    except HTTPException:
        raise
    except Exception as e:
        await db.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, 
            detail=f"Ошибка транзакции при подаче заявки: {str(e)}"
        )


@router.get("/{author_id}/check-rights", status_code=status.HTTP_200_OK)
async def check_upload_rights(
    author_id: int,
    author: Author = Depends(verify_artist_upload_rights)
) -> dict:
    """Диагностический маршрут для проверки прав на загрузку треков. Вызывает зависимость проверки статусов и блокировок"""
    return {
        "status": "allowed",
        "message": f"Успешно. Вы имеете полное право выкладывать треки от лица '{author.author_name}'."
    }


@router.post("/{author_id}/members/role", status_code=status.HTTP_200_OK)
async def change_member_role(
    author_id: int,
    payload: ChangeMemberRoleRequestSchema,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
) -> dict:
    """
    Изменение должностей и прав участников внутри коллектива.
    Доступно строго только пользователю с ролью 'Owner'
    """
    owner_stmt = select(AuthorMember).where(
        AuthorMember.author_id == author_id,
        AuthorMember.user_id == current_user.user_id,
        AuthorMember.group_role_id == 1
    )
    owner_res = await db.execute(owner_stmt)
    if not owner_res.scalars().first():
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN, 
            detail="Доступ запрещен. Только Владелец группы имеет право управлять должностями участников."
        )

    if payload.target_user_id == current_user.user_id:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST, 
            detail="Операция отклонена. Вы не можете изменить или понизить свою собственную роль."
        )

    member_stmt = select(AuthorMember).where(
        AuthorMember.author_id == author_id,
        AuthorMember.user_id == payload.target_user_id
    )
    result = await db.execute(member_stmt)
    member = result.scalars().first()
    
    if not member:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, 
            detail="Указанный пользователь не найден в составе участников данного группы."
        )

    member.group_role_id = payload.new_group_role_id
    await db.commit()
    
    return {
        "status": "success", 
        "message": "Должность и уровень доступа участника успешно обновлены в базе данных."
    }


@router.post("/{author_id}/tracks/upload", status_code=status.HTTP_201_CREATED)
async def upload_track(
    author_id: int,
    composition_name: str = Form(...),
    release_year: int = Form(...),
    duration: int = Form(...),
    genre_id: int = Form(...),
    version_type: str = Form("Studio"),
    album_id: Optional[int] = Form(None),
    audio_file: UploadFile = File(...),
    db: AsyncSession = Depends(get_db),
    author: Author = Depends(verify_artist_upload_rights)
) -> dict:
    """
    Выкладывание треков: Загрузка аудиофайла на диск и создание записей каталога.
    Принимает бинарный поток, валидирует формат, сохраняет файл в media/ и сохраняет в СУБД.
    """
    if not audio_file.filename.endswith(".mp3"):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST, 
            detail="Недопустимый формат файла. Сервер принимает аудиопотоки строго с расширением .mp3"
        )

    try:
        new_comp = Composition(
            composition_name=composition_name, 
            release_year=release_year
        )
        db.add(new_comp)
        await db.flush()

        await db.execute(
            text("INSERT INTO authors_of_composition (composition_id, author_id) VALUES (:cid, :aid)"),
            {"cid": new_comp.composition_id, "aid": author_id}
        )
        await db.execute(
            text("INSERT INTO genres_of_composition (composition_id, genre_id) VALUES (:cid, :gid)"),
            {"cid": new_comp.composition_id, "gid": genre_id}
        )

        file_hash = secrets.token_hex(4)
        relative_db_path = f"tracks/{new_comp.composition_id}_{file_hash}.mp3"
        absolute_server_path = os.path.join(settings.base_dir, "media", relative_db_path)

        os.makedirs(os.path.dirname(absolute_server_path), exist_ok=True)

        with open(absolute_server_path, "wb") as buffer:
            shutil.copyfileobj(audio_file.file, buffer)

        new_track = Track(
            composition_id=new_comp.composition_id,
            album_id=album_id,
            filepath=relative_db_path,
            duration=duration,
            version_type=version_type
        )
        db.add(new_track)
        await db.commit()

        return {
            "status": "success", 
            "message": f"Трек успешно сохранен в базе данных."
        }

    except Exception as e:
        await db.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, 
            detail=f"Ошибка выполнения файловых трансляций СУБД: {str(e)}"
        )

@router.patch("/{author_id}/tracks/{track_id}/edit", status_code=status.HTTP_200_OK)
async def edit_track_metadata(
    author_id: int,
    track_id: int,
    payload: TrackUpdateRequestSchema,
    db: AsyncSession = Depends(get_db),
    author: Author = Depends(verify_artist_upload_rights)
) -> dict:
    """
    Редактирование трека - изменение данных трека и композиции.
    Доступно участникам группы с правами Owner или Manager.
    """
    try:
        track = await db.get(Track, track_id)
        if not track:
            raise HTTPException(status_code=404, detail="Указанный трек не найден в базе данных")

        link_stmt = text(
            "SELECT 1 FROM authors_of_composition WHERE composition_id = :cid AND author_id = :aid"
        )
        link_res = await db.execute(link_stmt, {"cid": track.composition_id, "aid": author_id})
        if not link_res.scalars().first():
            raise HTTPException(status_code=400, detail="Данный трек не принадлежит указанному исполнителю")

        if payload.duration is not None:
            track.duration = payload.duration
        if payload.version_type is not None:
            track.version_type = payload.version_type
        if payload.album_id is not None:
            track.album_id = payload.album_id if payload.album_id != 0 else None

        if payload.track_name is not None or payload.release_year is not None:
            composition = await db.get(Composition, track.composition_id)
            if composition:
                if payload.track_name is not None:
                    composition.composition_name = payload.track_name
                if payload.release_year is not None:
                    composition.release_year = payload.release_year

        await db.commit()

        return {
            "status": "success", 
            "message": f"Метаданные трека ID {track_id} успешно обновлены в СУБД для группы '{author.author_name}'."
        }

    except HTTPException:
        raise
    except Exception as e:
        await db.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Ошибка транзакции обновления данных СУБД: {str(e)}"
        )


@router.delete("/{author_id}/delete", status_code=status.HTTP_200_OK)
async def delete_group(
    author_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    """
    Удаление группы
    Доступно только Owner-у
    """
    owner_stmt = select(AuthorMember).where(
        AuthorMember.author_id == author_id,
        AuthorMember.user_id == current_user.user_id,
        AuthorMember.group_role_id == 1
    )
    res = await db.execute(owner_stmt)
    if not res.scalars().first():
        raise HTTPException(status_code=403, detail="Удалить карточку исполнителя может только её Владелец.")

    try:
        author = await db.get(Author, author_id)
        if author:
            await db.delete(author)
            await db.commit()
            return {"status": "success", "message": "Карточка исполнителя полностью удалена из СУБД."}
        raise HTTPException(status_code=404, detail="Исполнитель не найден.")
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))
