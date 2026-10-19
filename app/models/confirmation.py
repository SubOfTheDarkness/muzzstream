"""
Модуль модели для кодов подтверждения электронной почты
"""

from datetime import datetime
from sqlalchemy import TEXT, VARCHAR, TIMESTAMP, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column
from app.models.base import Base


class EmailConfirmation(Base):
    """
    Модель таблицы 'email_confirmations'
    Хранит временные шестизначные коды верификации аккаунтов
    """
    __tablename__ = "email_confirmations"

    email: Mapped[str] = mapped_column(
        TEXT, 
        ForeignKey("users.email", ondelete="CASCADE"), 
        primary_key=True
    )
    code: Mapped[str] = mapped_column(VARCHAR(6), nullable=False)
    expires_at: Mapped[datetime] = mapped_column(TIMESTAMP(timezone=True), nullable=False)
