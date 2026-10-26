"""
Модуль схем для работы с плейлистами пользователей
"""

from pydantic import BaseModel, Field
from typing import List, Optional
from app.schemas.base import BaseResponseSchema
from app.schemas.catalog import TrackResponseSchema


class PlaylistCreateRequestSchema(BaseModel):
    """Схема запроса на создание нового плейлиста"""
    playlist_name: str = Field(..., min_length=1, max_length=100)
    is_public: bool = False


class AddTrackToPlaylistRequestSchema(BaseModel):
    """Схема запроса для добавления трека в плейлист"""
    track_id: int
    playlist_id: int

class PlaylistResponseSchema(BaseResponseSchema):
    """Полная схема плейлиста со списком входящих в него треков"""
    playlist_id: int
    user_id: int
    playlist_name: str
    is_public: bool
    tracks_count: int
    total_duration: int
    tracks: List[TrackResponseSchema]
