"""
Модуль моделей для пользователей
"""

from datetime import datetime
from sqlalchemy import TEXT, BOOLEAN, TIMESTAMP, INT, VARCHAR, ForeignKey, func
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from app.models.role import Role
from typing import Optional

class User(Base):
    """Модель таблицы 'users'"""
    __tablename__ = "users"

    user_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    nickname: Mapped[str] = mapped_column(VARCHAR(16), unique=True, nullable=False)
    username: Mapped[Optional[str]] = mapped_column(TEXT, nullable=True)
    email: Mapped[str] = mapped_column(TEXT, unique=True, nullable=False)
    password: Mapped[str] = mapped_column(TEXT, nullable=False)
    is_verified: Mapped[bool] = mapped_column(BOOLEAN, nullable=False, default=False)
    
    role_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("roles.role_id", ondelete="RESTRICT"), 
        nullable=False, 
        default=3
    )
    
    created_at: Mapped[datetime] = mapped_column(
        TIMESTAMP(timezone=True), 
        nullable=False, 
        server_default=func.now()
    )

    role: Mapped["Role"] = relationship("Role", back_populates="users")
