"""
Модуль Pydantic-схем музыкального каталога (Жанры, Альбомы, Композиции, Треки).
"""

from pydantic import BaseModel, Field
from typing import List, Optional
from app.schemes.base import BaseResponseSchema
from app.schemes.author import AuthorResponseSchema


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
