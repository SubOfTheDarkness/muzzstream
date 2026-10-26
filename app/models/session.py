"""
Модуль модели для сессий пользователей
"""

from sqlalchemy import TEXT, VARCHAR, INT, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from typing import Optional

class Session(Base):
    """
    Модель таблицы 'sessions'.
    """
    __tablename__ = "sessions"

    token: Mapped[str] = mapped_column(TEXT, primary_key=True)
    user_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("users.user_id", ondelete="CASCADE"), 
        nullable=False
    )
    device_name: Mapped[str] = mapped_column(TEXT, nullable=False)
    ip_address: Mapped[str] = mapped_column(VARCHAR(45), nullable=False)

    user: Mapped["User"] = relationship("User")
