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
from app.security.auth_deps import get_current_user
from app.schemas.auth import (
    UserRegisterRequestSchema, 
    UserLoginRequestSchema, 
    TokenResponseSchema,
    EmailConfirmRequestSchema,
    PasswordResetRequestSchema,
    PasswordChangeRequestSchema
)

router = APIRouter()

async def send_password_reset_email(email: str, new_password: str):
    html_content = f"""
    <html>
        <body style="font-family: Arial, sans-serif; text-align: center; padding: 20px;">
            <div style="max-width: 500px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px;">
                <h2 style="color: #FF3333;">Сброс пароля MuzzStream</h2>
                <p>Ваш временный пароль для входа в систему:</p>
                <div style="font-size: 24px; font-weight: bold; color: #333; margin: 20px 0; padding: 10px; background: #eee; border-radius: 5px;">
                    {new_password}
                </div>
                <p style="font-size: 12px; color: #777;">После входа в личный кабинет настоятельно рекомендуем сразу изменить его в целях безопасности.</p>
            </div>
        </body>
    </html>
    """
    try:
        message = MessageSchema(
            subject="Восстановление доступа MuzzStream",
            recipients=[email],
            body=html_content,
            subtype=MessageType.html
        )
        fm = FastMail(settings.mail_config)
        await fm.send_message(message)
    except Exception as ex:
        print(f"[SMTP_RESET_ERR] {str(ex)}")

async def send_verification_email(email: str, code: str, is_new_register: bool = True):
    """
    Универсальная изолированная функция для асинхронной отправки писем.
    Защищает основной поток эндпоинтов от блокировок во время ожидания ответа SMTP.
    """
    title = "Добро пожаловать в MuzzStream!" if is_new_register else "Запрос подтверждения MuzzStream"
    subtitle = (
        "Для подтверждения вашего адреса электронной почты введите следующий код в приложении:"
        if is_new_register else 
        "Вы пытаетесь войти в аккаунт с неподтвержденной почтой. Ваш новый код активации:"
    )
    
    html_content = f"""
    <html>
        <body style="font-family: Arial, sans-serif; text-align: center; background-color: #f4f4f4; padding: 20px;">
            <div style="max-width: 500px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.1);">
                <h2 style="color: #1DB954;">{title}</h2>
                <p>{subtitle}</p>
                <div style="font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #333; margin: 20px 0; padding: 10px; background: #eee; border-radius: 5px;">
                    {code}
                </div>
                <p style="font-size: 12px; color: #777;">Код действителен в течение 10 минут. Если вы не запрашивали этот код, просто проигнорируйте письмо.</p>
            </div>
        </body>
    </html>
    """
    
    try:
        message = MessageSchema(
            subject="Подтверждение аккаунта MuzzStream",
            recipients=[email],
            body=html_content,
            subtype=MessageType.html
        )
        fm = FastMail(settings.mail_config)
        await fm.send_message(message)
    except Exception as mail_ex:
        print(f"[SMTP_DELAY_WARN] Проблема с отправкой почты на {email}: {str(mail_ex)}")


@router.post("/register", status_code=status.HTTP_201_CREATED)
async def register_user(payload: UserRegisterRequestSchema, db: AsyncSession = Depends(get_db)) -> dict:
    """Регистрация нового пользователя и отправка кода на почту"""
    if len(payload.nickname.strip()) < 4:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST, 
            detail="Никнейм слишком короткий. Минимальная длина — 4 символа."
        )
        
    if len(payload.password) < 8:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST, 
            detail="Пароль слишком короткий. Минимальная длина — 8 символов."
        )
    try:
        stmt_user = select(User).where(User.email == payload.email)
        result_user = await db.execute(stmt_user)
        user = result_user.scalars().first()

        if user:
            if user.is_verified:
                raise HTTPException(
                    status_code=status.HTTP_400_BAD_REQUEST, 
                    detail="Пользователь с такой почтой уже зарегистрирован"
                )
            
        else:
            stmt_nick = select(User).where(User.nickname == payload.nickname)
            result_nick = await db.execute(stmt_nick)
            if result_nick.scalars().first():
                raise HTTPException(
                    status_code=status.HTTP_400_BAD_REQUEST, 
                    detail="Этот уникальный никнейм уже занят"
                )

            hashed_pass = hash_password(payload.password)
            user = User(
                nickname=payload.nickname,
                username=payload.username,
                email=payload.email,
                password=hashed_pass,
                role_id=3
            )
            db.add(user)
            await db.flush()

        verification_code = "".join(secrets.choice("0123456789") for _ in range(6))
        expiration_time = datetime.now(timezone.utc) + timedelta(minutes=10)

        await db.execute(delete(EmailConfirmation).where(EmailConfirmation.email == payload.email))

        await db.commit()

        db_code = EmailConfirmation(
            email=payload.email,
            code=verification_code,
            expires_at=expiration_time
        )
        db.add(db_code)
        
        await send_verification_email(payload.email, verification_code, is_new_register=True)
        await db.commit()

        return {
            "status": "success", 
            "message": "Пользователь создан. Код подтверждения успешно отправлен на указанную почту."
        }

    except HTTPException:
        raise
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

    if "@" in payload.identity:
        stmt = select(User).where(User.email == payload.identity.strip())
    else:
        stmt = select(User).where(User.nickname == payload.identity.strip())
        
    result = await db.execute(stmt)
    user = result.scalars().first()

    if not user or not verify_password(payload.password, user.password):
        await db.execute(
            text("INSERT INTO audit_log (user_id, operation, operation_status) VALUES (NULL, :op, 2)"),
            {"op": f"FAILED_LOGIN_ATTEMPT_FOR_{payload.identity}"}
        )
        await db.commit()
        raise HTTPException(status_code=401, detail="Неверный никнейм/почта или пароль")

    if not user.is_verified:
        verification_code = "".join(secrets.choice("0123456789") for _ in range(6))
        expiration_time = datetime.now(timezone.utc) + timedelta(minutes=10)

        await db.execute(delete(EmailConfirmation).where(EmailConfirmation.email == user.email))
        await db.commit()
        db_code = EmailConfirmation(email=user.email, code=verification_code, expires_at=expiration_time)
        db.add(db_code)
        
        await send_verification_email(user.email, verification_code, is_new_register=False)
        await db.commit()
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="UNVERIFIED_EMAIL")

    try:
        session_token = secrets.token_hex(32)
        new_session = Session(token=session_token, user_id=user.user_id, device_name=payload.device_name, ip_address=client_ip)
        db.add(new_session)
        await db.commit()

        return {
            "token": session_token, 
            "user_id": user.user_id,
            "nickname": user.nickname, 
            "username": user.username,
            "email": user.email,
            "role_id": user.role_id
        }
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка СУБД сессии: {str(e)}")



@router.post("/reset-password", status_code=status.HTTP_200_OK)
async def reset_password(payload: PasswordResetRequestSchema, db: AsyncSession = Depends(get_db)):
    """Генерация временного пароля и отправка"""
    if "@" in payload.identity:
        stmt = select(User).where(User.email == payload.identity.strip())
    else:
        stmt = select(User).where(User.nickname == payload.identity.strip())
        
    result = await db.execute(stmt)
    user = result.scalars().first()

    if not user:
        raise HTTPException(status_code=404, detail="Пользователь с таким никнеймом/email не найден в системе.")

    chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ23456789"
    temp_password = "".join(secrets.choice(chars) for _ in range(8)) + "!"

    try:
        user.password = hash_password(temp_password)
        await db.commit()

        await send_password_reset_email(user.email, temp_password)

        return {"status": "success", "message": f"Новый пароль успешно отправлен на почту: {user.email}"}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка при сбросе пароля: {str(e)}")

@router.patch("/password/change", status_code=status.HTTP_200_OK)
async def change_password(
    payload: PasswordChangeRequestSchema,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    """Смена пароля пользователем"""
    if len(payload.new_password) < 8:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Новый пароль слишком короткий. Минимальная длина — 8 символов."
        )

    if not verify_password(payload.old_password, current_user.password):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="INVALID_OLD_PASSWORD"
        )

    try:
        current_user.password = hash_password(payload.new_password)
        await db.commit()
        
        return {"status": "success", "message": "Пароль успешно изменен."}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка транзакции: {str(e)}")
