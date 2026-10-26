from fastapi import APIRouter, HTTPException, Depends, status
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, text
from sqlalchemy.orm import selectinload, joinedload
from typing import List

from app.database import get_db
from app.models.playlist import Playlist
from app.models.catalog import Track, Composition
from app.models.user import User
from app.security.auth_deps import get_current_user
from app.schemas.playlist import PlaylistCreateRequestSchema, AddTrackToPlaylistRequestSchema, PlaylistResponseSchema

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
    """Добавление трека в плейлист"""
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

        check_stmt = text(
            "SELECT 1 FROM tracks_of_playlist WHERE track_id = :tid AND playlist_id = :pid"
        )
        check_res = await db.execute(check_stmt, {"tid": payload.track_id, "pid": payload.playlist_id})
        if check_res.scalars().first():
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Этот трек уже добавлен в плейлист."
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


@router.get("/my", response_model=List[PlaylistResponseSchema])
async def get_my_playlists(
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    """Получение всех плейлистов текущего пользователя"""
    try:
        stmt = (
            select(Playlist)
            .where(Playlist.user_id == current_user.user_id)
            .options(
                selectinload(Playlist.tracks).options(
                    joinedload(Track.composition).options(
                        selectinload(Composition.authors),
                        selectinload(Composition.genres)
                    )
                )
            )
        )
        result = await db.execute(stmt)
        playlists_data = result.scalars().unique().all()
        return playlists_data
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ошибка получения плейлистов: {str(e)}")


@router.get("/{playlist_id}", response_model=PlaylistResponseSchema)
async def get_playlist_by_id(
    playlist_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    """Получение конкретного плейлиста по ID"""
    try:
        stmt = (
            select(Playlist)
            .where(Playlist.playlist_id == playlist_id)
            .options(
                selectinload(Playlist.tracks).options(
                    joinedload(Track.composition).options(
                        selectinload(Composition.authors),
                        selectinload(Composition.genres)
                    )
                )
            )
        )
        result = await db.execute(stmt)
        playlist = result.scalars().first()

        if not playlist:
            raise HTTPException(status_code=404, detail="Плейлист не найден")

        if not playlist.is_public and playlist.user_id != current_user.user_id:
            raise HTTPException(status_code=403, detail="Доступ к приватному плейлисту запрещен")

        return playlist
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ошибка загрузки плейлиста: {str(e)}")


@router.delete("/{playlist_id}/delete", status_code=status.HTTP_200_OK)
async def delete_playlist(
    playlist_id: int,
    db: AsyncSession = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    """Удаление плейлиста"""
    try:
        playlist = await db.get(Playlist, playlist_id)
        if not playlist:
            raise HTTPException(status_code=404, detail="Плейлист не найден")

        if playlist.user_id != current_user.user_id:
            raise HTTPException(status_code=403, detail="Вы можете удалять только свои плейлисты")

        await db.delete(playlist)
        await db.commit()
        return {"status": "success", "message": "Плейлист успешно удален"}
    except HTTPException:
        raise
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка удаления: {str(e)}")
