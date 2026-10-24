"""
Модуль маршрутизации для работы с музыкальным каталогом и аудиостримингом.
Реализует эндпоинты получения списков треков через модели СУБД и порционную отдачу аудио (стриминг)
"""

import os
import io
from typing import List, Optional
from fastapi import APIRouter, HTTPException, Depends, status, Header
from fastapi.responses import FileResponse, StreamingResponse
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, text
from sqlalchemy.orm import joinedload, selectinload

from app.database import get_db
from app.models.catalog import Track, Composition, Genre, Album, genres_of_composition
from app.models.views import TrackDetailsView
from app.schemas.catalog import TrackResponseSchema, AlbumFullResponseSchema, LogListeningRequestSchema

router = APIRouter()

@router.get("/all", response_model=List[TrackResponseSchema])
async def get_all_tracks(
    genre_name: Optional[str] = None, 
    db: AsyncSession = Depends(get_db)
):
    """
    Получение списка всех доступных треков системы с полной информацией.
    """
    try:
        stmt = select(Track).options(
            joinedload(Track.album),
            joinedload(Track.composition).options(
                selectinload(Composition.authors),
                selectinload(Composition.genres)
            )
        )
        
        if genre_name:
            stmt = (
                stmt.join(Composition, Track.composition_id == Composition.composition_id)
                .join(genres_of_composition, Composition.composition_id == genres_of_composition.c.composition_id)
                .join(Genre, genres_of_composition.c.genre_id == Genre.genre_id)
                .where(Genre.genre_name == genre_name)
            )
            
        result = await db.execute(stmt)
        tracks_data = result.scalars().unique().all()
        
        return tracks_data
        
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Ошибка ORM при получении каталога треков: {str(e)}"
        )

@router.get("/albums/{album_id}", response_model=AlbumFullResponseSchema)
async def get_album_with_tracks(album_id: int, db: AsyncSession = Depends(get_db)):
    """
    Получение полной информации об альбоме, его авторах, жанрах и всех входящих в него треках.
    """
    try:
        stmt_album = (
            select(Album)
            .where(Album.album_id == album_id)
            .options(
                selectinload(Album.authors),
                selectinload(Album.genres)
            )
        )
        album_result = await db.execute(stmt_album)
        album = album_result.scalars().first()
        
        if not album:
            raise HTTPException(status_code=404, detail="Альбом не найден в базе данных")
            
        stmt_tracks = (
            select(Track)
            .where(Track.album_id == album_id)
            .options(
                joinedload(Track.composition).options(
                    selectinload(Composition.authors),
                    selectinload(Composition.genres)
                )
            )
        )
        
        tracks_result = await db.execute(stmt_tracks)
        tracks_data = tracks_result.scalars().unique().all()
        
        return {
            "album_id": album.album_id,
            "album_name": album.album_name,
            "release_year": album.release_year,
            "tracks_count": len(tracks_data),
            "authors": album.authors,
            "genres": album.genres,
            "tracks": tracks_data
        }
        
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Ошибка ORM при получении данных альбома: {str(e)}"
        )

def get_audio_chunk_generator(file_path: str, start: int, end: int, chunk_size: int = 1024 * 512):
    """
    Асинхронный генератор для чтения файла
    """
    with open(file_path, "rb") as audio:
        audio.seek(start)
        remaining_bytes = (end - start) + 1
        
        while remaining_bytes > 0:
            bytes_to_read = min(chunk_size, remaining_bytes)
            data = audio.read(bytes_to_read)
            if not data:
                break
            remaining_bytes -= len(data)
            yield data

@router.get("/{track_id}/stream")
async def stream_track(track_id: int, db: AsyncSession = Depends(get_db)):
    track = await db.get(Track, track_id)
    if not track or not os.path.exists(track.absolute_filepath):
        raise HTTPException(status_code=404, detail="Файл не найден")
        
    return FileResponse(
        path=track.absolute_filepath, 
        media_type="audio/mpeg",
        filename=f"track_{track_id}.mp3"
    )

@router.post("/listen", status_code=status.HTTP_201_CREATED)
async def log_track_listening(
    payload: LogListeningRequestSchema, 
    db: AsyncSession = Depends(get_db)
):
    """
    Фиксация факта прослушивания трека в истории пользователя.
    Вызывает хранимую процедуру СУБД 'log_listening'.
    """
    try:
        track = await db.get(Track, payload.track_id)
        if not track:
            raise HTTPException(status_code=404, detail="Трек не найден")

        await db.execute(
            text("CALL log_listening(:user_id, :track_id, :duration)"),
            {
                "user_id": payload.user_id,
                "track_id": payload.track_id,
                "duration": payload.listened_duration
            }
        )
        await db.commit()
        return {"status": "success", "message": "История зафиксирована в СУБД через процедуру"}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))
