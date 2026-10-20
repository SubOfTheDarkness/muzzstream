"""
Модуль маршрутизации для аутентификации пользователей, верификации и сессий
"""

import secrets
from datetime import datetime, timedelta, timezone
from fastapi import APIRouter, HTTPException, Depends, Request, status
from fastapi_mail import FastMail, MessageSchema, MessageType
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, text, delete

from app.config import settings
from app.database import get_db
from app.models.user import User
from app.models.confirmation import EmailConfirmation
from app.models.session import Session
from app.security.crypto import hash_password, verify_password
from app.schemas.auth import (
    UserRegisterRequestSchema, 
    UserLoginRequestSchema, 
    TokenResponseSchema,
    EmailConfirmRequestSchema
)

router = APIRouter()


@router.post("/register", status_code=status.HTTP_201_CREATED)
async def register_user(payload: UserRegisterRequestSchema, db: AsyncSession = Depends(get_db)) -> dict:
    """Регистрация нового пользователя и отправка кода на почту"""
    try:
        if (await db.execute(select(User).where(User.email == payload.email))).scalar_one_or_none():
            raise HTTPException(status_code=400, detail="Пользователь с такой почтой уже зарегистрирован")
        if (await db.execute(select(User).where(User.nickname == payload.nickname))).scalar_one_or_none():
            raise HTTPException(status_code=400, detail="Этот уникальный никнейм уже занят")

        hashed_pass = hash_password(payload.password)
        new_user = User(
            nickname=payload.nickname,
            username=payload.username,
            email=payload.email,
            password=hashed_pass,
            role_id=4
        )
        db.add(new_user)
        await db.flush()

        verification_code = "".join(secrets.choice("0123456789") for _ in range(6))
        expiration_time = datetime.now(timezone.utc) + timedelta(minutes=10)

        await db.execute(delete(EmailConfirmation).where(EmailConfirmation.email == payload.email))

        db_code = EmailConfirmation(
            email=payload.email,
            code=verification_code,
            expires_at=expiration_time
        )
        db.add(db_code)
        
        html_content = f"""
        <html>
            <body style="font-family: Arial, sans-serif; text-align: center; background-color: #f4f4f4; padding: 20px;">
                <div style="max-width: 500px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.1);">
                    <h2 style="color: #1DB954;">Добро пожаловать в MuzzStream!</h2>
                    <p>Для подтверждения вашего адреса электронной почты введите следующий код в приложении:</p>
                    <div style="font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #333; margin: 20px 0; padding: 10px; background: #eee; border-radius: 5px;">
                        {verification_code}
                    </div>
                    <p style="font-size: 12px; color: #777;">Код действителен в течение 10 минут. Если вы не регистрировались в нашем сервисе, просто проигнорируйте это письмо.</p>
                </div>
            </body>
        </html>
        """
        
        message = MessageSchema(
            subject="Подтверждение аккаунта MuzzStream",
            recipients=[payload.email],
            body=html_content,
            subtype=MessageType.html
        )
        
        fm = FastMail(settings.mail_config)
        await fm.send_message(message)
        
        await db.commit()

        return {
            "status": "success", 
            "message": "Пользователь создан. Код подтверждения успешно отправлен на указанную почту."
        }

    except Exception as e:
        await db.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Ошибка при регистрации или отправке почты: {str(e)}"
        )


@router.post("/confirm", status_code=status.HTTP_200_OK)
async def confirm_email(payload: EmailConfirmRequestSchema, db: AsyncSession = Depends(get_db)) -> dict:
    """Проверка кода верификации с контролем таймаута времени жизни"""
    stmt = select(EmailConfirmation).where(EmailConfirmation.email == payload.email)
    result = await db.execute(stmt)
    db_confirmation = result.scalars().first()

    if not db_confirmation:
        raise HTTPException(status_code=400, detail="Код подтверждения не запрашивался или не существует")

    current_time = datetime.now(timezone.utc)
    if current_time > db_confirmation.expires_at.replace(tzinfo=timezone.utc):
        await db.delete(db_confirmation)
        await db.commit()
        raise HTTPException(status_code=400, detail="Срок действия кода подтверждения истек")

    if db_confirmation.code != payload.code:
        raise HTTPException(status_code=400, detail="Введен неверный код подтверждения")

    try:
        user_stmt = select(User).where(User.email == payload.email)
        user_result = await db.execute(user_stmt)
        user = user_result.scalars().first()
        
        if user:
            user.is_verified = True
            
        await db.delete(db_confirmation)
        await db.commit()

        return {"status": "success", "message": "Электронная почта успешно подтверждена."}

    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка транзакции верификации: {str(e)}")


@router.post("/login", response_model=TokenResponseSchema)
async def login_user(payload: UserLoginRequestSchema, request: Request, db: AsyncSession = Depends(get_db)) -> dict:
    """Авторизация пользователя"""
    client_ip = request.client.host if request.client else "127.0.0.1"

    user_stmt = select(User).where(User.email == payload.email)
    user_result = await db.execute(user_stmt)
    user = user_result.scalars().first()

    if not user or not verify_password(payload.password, user.password):
        await db.execute(
            text("INSERT INTO audit_log (user_id, operation, operation_status) VALUES (NULL, :op, 2)"),
            {"op": f"FAILED_LOGIN_ATTEMPT_FOR_EMAIL_{payload.email}"}
        )
        await db.commit()
        raise HTTPException(status_code=401, detail="Неверная электронная почта или пароль")

    if not user.is_verified:
        await db.execute(
            text("INSERT INTO audit_log (user_id, operation, operation_status) VALUES (:uid, 'LOGIN_BLOCKED_UNCONFIRMED_EMAIL', 2)"),
            {"uid": user.user_id}
        )
        await db.commit()
        raise HTTPException(status_code=403, detail="Вход запрещен - не подтвержден адрес электронной почты.")

    try:
        session_token = secrets.token_hex(32)
        new_session = Session(
            token=session_token,
            user_id=user.user_id,
            device_name=payload.device_name,
            ip_address=client_ip
        )
        db.add(new_session)

        await db.execute(
            text("INSERT INTO audit_log (user_id, operation, operation_status) VALUES (:uid, 'USER_LOGIN', 1)"),
            {"uid": user.user_id}
        )
        await db.commit()

        return {"token": session_token, "nickname": user.nickname, "role_id": user.role_id}

    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка СУБД при обработке сессии: {str(e)}")
