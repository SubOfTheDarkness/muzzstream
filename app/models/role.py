"""
Модуль моделей для ролей
"""
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from typing import List, Optional
from sqlalchemy import TEXT, INT

class Role(Base):
    """Модель таблицы 'roles'"""
    __tablename__ = "roles"

    role_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    role_name: Mapped[str] = mapped_column(TEXT, unique=True, nullable=False)

    users: Mapped[List["User"]] = relationship(
        "User", 
        back_populates="role", 
        cascade="all, delete-orphan"
    )