"""
Модуль маршрутизации для управления пользовательскими плейлистами
"""

from fastapi import APIRouter, HTTPException, Depends, status
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import text
from app.database import get_db
from app.models.playlist import Playlist
from app.models.catalog import Track
from app.models.user import User
from app.security.auth_deps import get_current_user
from app.schemas.playlist import PlaylistCreateRequestSchema, AddTrackToPlaylistRequestSchema

router = APIRouter()


@router.post("/create", status_code=status.HTTP_201_CREATED)
async def create_playlist(
    payload: PlaylistCreateRequestSchema,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    """Создание нового пустого плейлиста для текущего авторизованного пользователя"""
    try:
        new_playlist = Playlist(
            user_id=current_user.user_id,
            playlist_name=payload.playlist_name,
            is_public=payload.is_public
        )
        db.add(new_playlist)
        await db.commit()
        return {"status": "success", "message": f"Плейлист успешно создан для пользователя {current_user.nickname}"}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/add-track", status_code=status.HTTP_200_OK)
async def add_track(
    payload: AddTrackToPlaylistRequestSchema, 
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    """
    Добавление трека в плейлист
    Вызывает процедуру 'add_track_to_playlist'
    """
    try:
        track = await db.get(Track, payload.track_id)
        playlist = await db.get(Playlist, payload.playlist_id)
        
        if not track or not playlist:
            raise HTTPException(status_code=404, detail="Трек или плейлист не найдены в базе данных")

        if playlist.user_id != current_user.user_id:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN, 
                detail="Вы не являетесь владельцем этого плейлиста"
            )

        await db.execute(
            text("CALL add_track_to_playlist(:track_id, :playlist_id)"),
            {"track_id": payload.track_id, "playlist_id": payload.playlist_id}
        )
        await db.commit()
        return {"status": "success", "message": "Трек добавлен в плейлист"}
    except HTTPException:
        raise
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка процедуры: {str(e)}")
