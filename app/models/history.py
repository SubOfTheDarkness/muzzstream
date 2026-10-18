"""
Модуль моделей для истории прослушиваний
"""

from datetime import datetime
from sqlalchemy import INT, TIMESTAMP, ForeignKey, func
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from app.models.catalog import Track
from typing import Optional

class ListeningHistory(Base):
    """Модель таблицы 'listening_history'"""
    __tablename__ = "listening_history"

    history_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    user_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("users.user_id", ondelete="CASCADE"), 
        nullable=False
    )
    track_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("tracks.track_id", ondelete="CASCADE"), 
        nullable=False
    )
    listened_duration: Mapped[int] = mapped_column(INT, nullable=False)
    
    listening_at: Mapped[datetime] = mapped_column(
        TIMESTAMP(timezone=True), 
        nullable=False, 
        server_default=func.now()
    )

    track: Mapped[Track] = relationship("Track")
