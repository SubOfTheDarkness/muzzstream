"""
Модуль Pydantic-схем музыкального каталога (Жанры, Альбомы, Композиции, Треки).
"""

from pydantic import BaseModel, Field
from typing import List, Optional
from app.schemas.base import BaseResponseSchema
from app.schemas.author import AuthorResponseSchema


class LogListeningRequestSchema(BaseModel):
    """Схема запроса добавления трека в историю прослушиваний"""
    user_id: int
    track_id: int
    listened_duration: int

class TrackUploadFieldsSchema(BaseModel):
    """Схема запроса на загрузку трека"""
    composition_name: str
    release_year: int
    duration: int
    version_type: str = "Studio"
    genre_id: int
    album_id: Optional[int] = None

class TrackUpdateRequestSchema(BaseModel):
    """Схема запроса для редактирования данных трека и композиции"""
    track_name: Optional[str] = None
    release_year: Optional[int] = None
    duration: Optional[int] = None
    version_type: Optional[str] = None
    album_id: Optional[int] = None


class GenreSchema(BaseResponseSchema):
    """Схема музыкального жанра"""
    genre_id: int
    genre_name: str


class AlbumShortSchema(BaseResponseSchema):
    """Краткая схема альбома для вложения в треки"""
    album_id: int
    album_name: str
    release_year: int


class CompositionShortSchema(BaseResponseSchema):
    """Схема композиции с авторами и жанрами"""
    composition_id: int
    composition_name: str
    release_year: int
    authors: List[AuthorResponseSchema]
    genres: List[GenreSchema]


class TrackResponseSchema(BaseResponseSchema):
    """Основная схема трека для общего списка"""
    track_id: int
    duration: int
    version_type: str
    composition: CompositionShortSchema
    album: Optional[AlbumShortSchema] = None


class TrackShortResponseSchema(BaseResponseSchema):
    """
    Укороченная схема трека для вывода внутри альбома.
    """
    track_id: int
    duration: int
    version_type: str
    composition: CompositionShortSchema


class AlbumFullResponseSchema(BaseResponseSchema):
    """
    Полная схема альбома со всей метаинформацией и списком треков.
    """
    album_id: int
    album_name: str
    release_year: int
    tracks_count: int
    authors: List[AuthorResponseSchema]
    genres: List[GenreSchema]
    tracks: List[TrackShortResponseSchema]
