"""
Модульсхем для авторов/исполнителей/групп и управления их участниками.
"""

from pydantic import BaseModel, EmailStr
from typing import Optional
from app.schemes.base import BaseResponseSchema


# --- ВХОДЯЩИЕ ДАННЫЕ ---

class AuthorCreateRequestSchema(BaseModel):
    """Схема для отправки заявки на создание карточки автора / группы"""
    author_name: str
    country: Optional[str] = None


# --- ИСХОДЯЩИЕ ДАННЫЕ ---

class AuthorResponseSchema(BaseResponseSchema):
    """Основная схема автора/исполнителя для вывода в каталоге"""
    author_id: int
    author_name: str
    country: Optional[str] = None


class GroupRoleSchema(BaseResponseSchema):
    """Схема должности участника внутри коллектива"""
    group_role_id: int
    role_name: str


class MemberUserShortSchema(BaseResponseSchema):
    """Краткая информация о пользователе-участнике группы"""
    user_id: int
    nickname: str
    email: EmailStr


class AuthorMemberResponseSchema(BaseResponseSchema):
    """Схема связи участника с группой и его уровнем прав"""
    user: MemberUserShortSchema
    role: GroupRoleSchema
