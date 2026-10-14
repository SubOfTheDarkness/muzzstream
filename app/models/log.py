"""
Модуль моделей для аудита и логов безопасности
"""

from datetime import datetime
from sqlalchemy import TEXT, INT, TIMESTAMPTZ, ForeignKey, func
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base


class StatusCode(Base):
    """
    Модель таблицы 'status_codes'.
    """
    __tablename__ = "status_codes"

    status_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    status_text: Mapped[str] = mapped_column(TEXT, unique=True, nullable=False)


class AuditLog(Base):
    """
    Модель таблицы 'audit_log'.
    
    Журнал безопасности, фиксирующий тип операции, время выполнения,
    её статус и инициатора (с сохранением записи при удалении пользователя).
    """
    __tablename__ = "audit_log"

    log_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    
    user_id: Mapped[int | None] = mapped_column(
        INT, 
        ForeignKey("users.user_id", ondelete="SET NULL"), 
        nullable=True
    )
    
    operation: Mapped[str] = mapped_column(TEXT, nullable=False)
    
    operation_status: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("status_codes.status_id"), 
        nullable=False
    )
    
    operation_time: Mapped[datetime] = mapped_column(
        TIMESTAMPTZ, 
        nullable=False, 
        server_default=func.now()
    )

    user: Mapped["User" | None] = relationship("User")
    status: Mapped["StatusCode"] = relationship("StatusCode")
