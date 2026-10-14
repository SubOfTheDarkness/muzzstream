"""
Модуль маршрутизации для работы с музыкальным каталогом и аудиостримингом.
"""

from fastapi import APIRouter, HTTPException, Depends
from fastapi.responses import FileResponse
from sqlalchemy.ext.asyncio import AsyncSession
import os
from app.database import get_db
from app.models.catalog import Track

router = APIRouter()


@router.get("/{track_id}/stream")
async def stream_track(track_id: int, db: AsyncSession = Depends(get_db)) -> FileResponse:
    """
    Эндпоинт для потоковой отдачи аудиофайла по его идентификатору.
    FastAPI автоматически обрабатывает байтовые Range-запросы от аудио-движка клиента, обеспечивая буферизацию и перемотку.
    """

    track = await db.get(Track, track_id)
    if not track:
        raise HTTPException(status_code=404, detail="Запись о треке не найдена в базе данных")

    file_path = track.filepath

    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="Аудиофайл отсутствует на сервере")

    return FileResponse(
        path=file_path,
        media_type="audio/mpeg",
        filename=f"{track.track_name}.mp3"
    )
