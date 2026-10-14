"""
Модуль моделей для пользовательских плейлистов
"""

from datetime import datetime
from sqlalchemy import TEXT, INT, BOOLEAN, TIMESTAMPTZ, ForeignKey, Table, func
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from app.models.catalog import Track
from typing import List

# Треки в плейлистах (N:M)
tracks_of_playlist = Table(
    "tracks_of_playlist",
    Base.metadata,
    mapped_column("track_id", INT, ForeignKey("tracks.track_id", ondelete="CASCADE"), primary_key=True),
    mapped_column("playlist_id", INT, ForeignKey("playlists.playlist_id", ondelete="CASCADE"), primary_key=True)
)


class Playlist(Base):
    """Модель таблицы 'playlists'"""
    __tablename__ = "playlists"

    playlist_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    user_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("users.user_id", ondelete="CASCADE"), 
        nullable=False
    )
    playlist_name: Mapped[str] = mapped_column(TEXT, nullable=False)

    is_public: Mapped[bool] = mapped_column(BOOLEAN, nullable=False, default=False)
    
    created_at: Mapped[datetime] = mapped_column(
        TIMESTAMPTZ, 
        nullable=False, 
        server_default=func.now()
    )
    updated_at: Mapped[datetime] = mapped_column(
        TIMESTAMPTZ, 
        nullable=False, 
        server_default=func.now(), 
        onupdate=func.now()
    )

    tracks: Mapped[List[Track]] = relationship("Track", secondary=tracks_of_playlist)
