"""
Модуль моделей для истории прослушиваний
"""

from datetime import datetime
from sqlalchemy import INT, TIMESTAMPTZ, ForeignKey, func
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from app.models.catalog import Track


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
    listened_seconds: Mapped[int] = mapped_column(INT, nullable=False)
    
    listening_at: Mapped[datetime] = mapped_column(
        TIMESTAMPTZ, 
        nullable=False, 
        server_default=func.now()
    )

    track: Mapped[Track] = relationship("Track")
