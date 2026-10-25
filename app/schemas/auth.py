"""
Модуль схем для аутентификации
"""

from pydantic import BaseModel, EmailStr, Field
from typing import Optional
from app.schemas.base import BaseResponseSchema


class UserRegisterRequestSchema(BaseModel):
    """Схема для валидации запроса на регистрацию нового пользователя"""
    nickname: str = Field(..., min_length=3, max_length=16, description="Уникальное имя аккаунта")
    username: Optional[str] = Field(None, min_length=4, description="Отображаемое имя пользователя")
    email: EmailStr = Field(..., description="Электронная почта пользователя")
    password: str = Field(..., min_length=8, description="Пароль (минимум 8 символов)")


class UserLoginRequestSchema(BaseModel):
    """Схема для валидации входных данных при авторизации пользователя"""
    identity: str = Field(..., description="Электронная почта")
    password: str = Field(..., description="Пароль")
    device_name: str = Field(..., description="Название устройства")    # Это указывает не юзер, а клиентское приложение,а потом сервер должен будет зауникалить и ввести в бд

class PasswordResetRequestSchema(BaseModel):
    """Схема для валидации входных данных при сбросе пароля пользователя"""
    identity: str

class PasswordChangeRequestSchema(BaseModel):
    """Схема для валидации входных данных при смене пароля пользователя"""
    old_password: str
    new_password: str

class EmailConfirmRequestSchema(BaseModel):
    """Схема запроса для подтверждения почты шестизначным кодом"""
    email: EmailStr = Field(..., description="Электронная почта, которую подтверждаем")
    code: str = Field(..., min_length=6, max_length=6, description="Шестизначный код подтверждения")


class TokenResponseSchema(BaseResponseSchema):
    """Схема ответа сервера при успешном входе. Возвращает токен для авторизации запросов."""
    token: str
    user_id: int
    nickname: str
    username: str
    email: str
    role_id: int

