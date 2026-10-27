import secrets
import os
import shutil
from typing import Optional, List
from datetime import datetime
from fastapi import APIRouter, Depends, HTTPException, status, UploadFile, File, Form
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, delete, update, text

from app.config import settings
from app.database import get_db
from app.models.user import User
from app.models.playlist import Playlist
from app.models.author import Author, AuthorMember
from app.models.genre import Genre
from app.models.catalog import Track, Composition, Album
from app.models.session import Session as UserSession
from app.security.crypto import hash_password
from app.security.auth_deps import require_admin, require_moderator
from app.schemas.moderation import (
    AdminCreateUserSchema, 
    ChangeUserRoleSchema, 
    AdminChangePasswordSchema,
    AdminUpdateAuthorSchema
)
from app.schemas.author import AuthorCreateRequestSchema, AuthorResponseSchema

router = APIRouter()


@router.delete("/users/{user_id}/delete", status_code=status.HTTP_200_OK)
async def admin_delete_user(
    user_id: int, 
    db: AsyncSession = Depends(get_db),
    admin: User = Depends(require_admin)
):
    """Админ может сносить юзера полностью"""
    user = await db.get(User, user_id)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")
        
    if user.user_id == admin.user_id:
        raise HTTPException(status_code=400, detail="Вы не можете удалить самого себя")

    try:
        await db.delete(user)
        await db.commit()
        return {"status": "success", "message": f"Пользователь ID {user_id} полностью удален."}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))


@router.patch("/users/password/force-change", status_code=status.HTTP_200_OK)
async def admin_change_user_password(
    payload: AdminChangePasswordSchema,
    db: AsyncSession = Depends(get_db),
    admin: User = Depends(require_admin)
):
    """Админ может менять пароль юзеру с выкидыванием из сессий"""
    user = await db.get(User, payload.user_id)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")

    try:
        user.password = hash_password(payload.new_password)
        await db.execute(delete(UserSession).where(UserSession.user_id == payload.user_id))
        await db.commit()
        return {"status": "success", "message": "Пароль изменен, активные сессии пользователя аннулированы."}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))


@router.patch("/users/role/change", status_code=status.HTTP_200_OK)
async def admin_change_user_role(
    payload: ChangeUserRoleSchema,
    db: AsyncSession = Depends(get_db),
    admin: User = Depends(require_admin)
):
    """Админ может менять роли у юзеров как угодно."""
    if payload.new_role_id not in [1, 2, 3]:
        raise HTTPException(status_code=400, detail="Неверный ID роли")

    user = await db.get(User, payload.user_id)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")

    if user.user_id == admin.user_id:
        raise HTTPException(status_code=400, detail="Вы не можете изменить роль у самого себя")

    try:
        user.role_id = payload.new_role_id
        await db.execute(delete(UserSession).where(UserSession.user_id == payload.user_id))
        await db.commit()
        return {"status": "success", "message": "Роль успешно изменена. Сессии сброшены."}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/users/create-verified", status_code=status.HTTP_201_CREATED)
async def moderator_create_user(
    payload: AdminCreateUserSchema,
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Модер может создать юзера сразу верифицированного или нет"""
    nick_check = await db.execute(select(User).where(User.nickname == payload.nickname))
    if nick_check.scalars().first():
        raise HTTPException(status_code=400, detail="Никнейм уже занят")
        
    email_check = await db.execute(select(User).where(User.email == payload.email))
    if email_check.scalars().first():
        raise HTTPException(status_code=400, detail="Email уже зарегистрирован")

    try:
        new_user = User(
            nickname=payload.nickname,
            username=payload.username,
            email=payload.email,
            password=hash_password(payload.password),
            is_verified=payload.is_verified,
            role_id=3
        )
        db.add(new_user)
        await db.commit()
        return {"status": "success", "message": "Пользователь успешно создан модератором."}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))

@router.patch("/playlists/{playlist_id}/visibility", status_code=status.HTTP_200_OK)
async def moderate_playlist_visibility(
    playlist_id: int,
    is_public: bool,
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Управление плейлистами: админ как угодно, модер максимум скрыть публичный"""
    playlist = await db.get(Playlist, playlist_id)
    if not playlist:
        raise HTTPException(status_code=404, detail="Плейлист не найден")

    if moderator.role_id == 2:
        if is_public is True:
            raise HTTPException(
                status_code=403, 
                detail="Модератор не может делать приватные плейлисты публичными. Допустимо только скрытие."
            )
        if playlist.is_public is False:
            return {"status": "info", "message": "Плейлист и так является приватным."}

    try:
        playlist.is_public = is_public
        await db.commit()
        return {"status": "success", "message": f"Видимость плейлиста изменена на {'публичный' if is_public else 'приватный'}"}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/authors/create-synthetic", response_model=AuthorResponseSchema, status_code=status.HTTP_201_CREATED)
async def moderator_create_synthetic_author(
    payload: AuthorCreateRequestSchema,
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Модер может создавать группы со статусом Synthetic"""
    name_check = await db.execute(select(Author).where(Author.author_name == payload.author_name))
    if name_check.scalars().first():
        raise HTTPException(status_code=400, detail="Исполнитель с таким именем уже существует")

    try:
        new_author = Author(
            author_name=payload.author_name,
            country=payload.country,
            current_status=3,
            created_by_user_id=moderator.user_id
        )
        db.add(new_author)
        await db.flush()
        
        await db.commit()
        return new_author
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))


@router.patch("/authors/{author_id}/status-sanction", status_code=status.HTTP_200_OK)
async def apply_author_sanction(
    author_id: int,
    target_status: int,
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Модер может ограничивать, Админ может банить"""
    author = await db.get(Author, author_id)
    if not author:
        raise HTTPException(status_code=404, detail="Исполнитель не найден")

    if target_status == 5 and moderator.role_id != 1:
        raise HTTPException(status_code=403, detail="Выставлять статус Banned может только Администратор.")
        
    if target_status not in [1, 2, 3, 4, 5]:
        raise HTTPException(status_code=400, detail="Недопустимый статус для санкции")

    try:
        author.current_status = target_status
        await db.commit()
        return {"status": "success", "message": f"Исполнителю назначен статус {target_status}"}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))


@router.patch("/authors/{author_id}/edit-catalog", status_code=status.HTTP_200_OK)
async def moderate_author_info(
    author_id: int,
    payload: AdminUpdateAuthorSchema,
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Админ рулит всеми, Модер только группами со статусом Synthetic"""
    author = await db.get(Author, author_id)
    if not author:
        raise HTTPException(status_code=404, detail="Исполнитель не найден")

    if moderator.role_id == 2 and author.current_status != 3:
        raise HTTPException(status_code=403, detail="Модератор может редактировать информацию только Synthetic-групп.")

    try:
        if payload.author_name is not None:
            author.author_name = payload.author_name
        if payload.country is not None:
            author.country = payload.country
        await db.commit()
        return {"status": "success", "message": "Информация о группе успешно обновлена."}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/tracks/upload", status_code=status.HTTP_201_CREATED)
async def moderate_upload_track(
    author_id: int = Form(...),
    composition_name: str = Form(...),
    release_year: int = Form(...),
    duration: int = Form(...),
    genre_id: int = Form(...),
    version_type: str = Form("Studio"),
    album_id: Optional[int] = Form(None),
    audio_file: UploadFile = File(...),
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Загрузка треков администрацией или модерацией.
    Админ льет куда угодно. Модер — только в группы со статусом 'Synthetic'."""
    author = await db.get(Author, author_id)
    if not author:
        raise HTTPException(status_code=404, detail="Указанный исполнитель не найден")
    if moderator.role_id == 2 and author.current_status != 3:
        raise HTTPException(
        status_code=status.HTTP_403_FORBIDDEN,
        detail="Модератор имеет право загружать треки строго только для исполнителей со статусом Synthetic."
        )
    if not audio_file.filename.endswith(".mp3"):
        raise HTTPException(
        status_code=status.HTTP_400_BAD_REQUEST,
        detail="Недопустимый формат файла. Сервер принимает только файлы с расширением .mp3"
        )
    try:
        new_comp = Composition(composition_name=composition_name, release_year=release_year)
        db.add(new_comp)
        await db.flush()
        await db.execute(
        text("INSERT INTO authors_of_composition (composition_id, author_id) VALUES (:cid, :aid)"),
            {"cid": new_comp.composition_id, "aid": author_id})
        await db.execute(
        text("INSERT INTO genres_of_composition (composition_id, genre_id) VALUES (:cid, :gid)"),
            {"cid": new_comp.composition_id, "gid": genre_id})
        file_hash = secrets.token_hex(4)
        relative_db_path = f"tracks/{new_comp.composition_id}_{file_hash}.mp3"
        absolute_server_path = os.path.join(settings.base_dir, "media", relative_db_path)
        os.makedirs(os.path.dirname(absolute_server_path), exist_ok=True)
        with open(absolute_server_path, "wb") as buffer:
            shutil.copyfileobj(audio_file.file, buffer)
        new_track = Track(
            composition_id=new_comp.composition_id,
            album_id=album_id,
            filepath=relative_db_path,
            duration=duration,
            version_type=version_type
        )
        db.add(new_track)
        await db.commit()
        return {"status": "success", "message": f"Трек для группы '{author.author_name}' успешно загружен администрацией."}
    except Exception as e:
        await db.rollback()
        if 'absolute_server_path' in locals() and os.path.exists(absolute_server_path):
            os.remove(absolute_server_path)
        raise HTTPException(status_code=500, detail=f"Ошибка файловой транзакции: {str(e)}")

@router.delete("/tracks/{track_id}/delete", status_code=status.HTTP_200_OK)
async def moderate_delete_track(
    track_id: int,
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Админ сносит всё. Модер сносит Synthetic, а также треки не-Synthetic групп"""
    track = await db.get(Track, track_id)
    if not track:
        raise HTTPException(status_code=404, detail="Трек не найден")
    try:
        file_path = track.absolute_filepath
        if os.path.exists(file_path):
            os.remove(file_path)
        await db.delete(track)
        await db.commit()
        return {"status": "success", "message": f"Трек ID {track_id} успешно удален модерацией."}
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/users/all", status_code=status.HTTP_200_OK)
async def admin_get_all_users(
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Получение списка всех пользователей"""
    result = await db.execute(select(User).order_by(User.user_id.desc()))
    users = result.scalars().all()
    return [
        {
            "user_id": u.user_id,
            "nickname": u.nickname,
            "username": u.username or "",
            "email": u.email,
            "role_id": u.role_id,
            "is_verified": u.is_verified
        } for u in users
    ]

@router.get("/playlists/all-details", status_code=status.HTTP_200_OK)
async def admin_get_playlists_with_authors(
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Вывод всех плейлистов системы с авторами"""
    stmt = text("""
        SELECT p.playlist_id, p.playlist_name, p.is_public, u.nickname as creator_nick, p.user_id
        FROM playlists p
        JOIN users u ON p.user_id = u.user_id
        ORDER BY p.playlist_id DESC;
    """)
    res = await db.execute(stmt)
    return [
        {
            "playlist_id": r.playlist_id,
            "playlist_name": r.playlist_name,
            "is_public": r.is_public,
            "creator_nick": r.creator_nick,
            "user_id": r.user_id
        } for r in res.fetchall()
    ]

@router.delete("/albums/{album_id}/delete", status_code=status.HTTP_200_OK)
async def admin_delete_album(album_id: int, db: AsyncSession = Depends(get_db), admin: User = Depends(require_admin)):
    """Админ может каскадно удалить любой альбом"""
    album = await db.get(Album, album_id)
    if not album: raise HTTPException(status_code=404, detail="Альбом не найден")
    await db.delete(album)
    await db.commit()
    return {"status": "success", "message": "Альбом успешно удален."}

@router.delete("/authors/{author_id}/delete-force", status_code=status.HTTP_200_OK)
async def admin_force_delete_author(author_id: int, db: AsyncSession = Depends(get_db), admin: User = Depends(require_admin)):
    """Админ может каскадно удалить любого исполнителя"""
    author = await db.get(Author, author_id)
    if not author: raise HTTPException(status_code=404, detail="Исполнитель не найден")
    await db.delete(author)
    await db.commit()
    return {"status": "success", "message": "Исполнитель полностью удален."}

@router.delete("/genres/{genre_id}/delete", status_code=status.HTTP_200_OK)
async def admin_delete_genre(genre_id: int, db: AsyncSession = Depends(get_db), admin: User = Depends(require_admin)):
    """Админ может удалить жанр"""
    genre = await db.get(Genre, genre_id)
    if not genre: raise HTTPException(status_code=404, detail="Жанр не найден")
    await db.delete(genre)
    await db.commit()
    return {"status": "success", "message": "Жанр успешно удален."}

@router.get("/albums/all-raw", status_code=status.HTTP_200_OK)
async def admin_get_all_albums(db: AsyncSession = Depends(get_db), moderator: User = Depends(require_moderator)):
    """Получение всех альбомов"""
    res = await db.execute(select(Album).order_by(Album.album_id.desc()))
    return [{"album_id": a.album_id, "album_name": a.album_name, "release_year": a.release_year} for a in res.scalars().all()]

@router.get("/authors/all-raw", status_code=status.HTTP_200_OK)
async def admin_get_all_authors(db: AsyncSession = Depends(get_db), moderator: User = Depends(require_moderator)):
    """Получение всех авторов"""
    res = await db.execute(select(Author).order_by(Author.author_id.desc()))
    return [{"author_id": a.author_id, "author_name": a.author_name, "country": a.country or "Не указана", "current_status": a.current_status} for a in res.scalars().all()]

@router.post("/genres/add", status_code=status.HTTP_201_CREATED)
async def admin_add_genre(
    genre_name: str = Form(...),
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Модер может добавить новый жанр в глобальный справочник"""
    check = await db.execute(select(Genre).where(Genre.genre_name == genre_name.strip()))
    if check.scalars().first():
        raise HTTPException(status_code=400, detail="Такой жанр уже существует")
        
    new_genre = Genre(genre_name=genre_name.strip())
    db.add(new_genre)
    await db.commit()
    return {"status": "success", "message": "Жанр добавлен"}


@router.post("/albums/add", status_code=status.HTTP_201_CREATED)
async def admin_add_album(
    album_name: str = Form(...),
    release_year: int = Form(...),
    author_ids: List[int] = Form(...),
    genre_ids: List[int] = Form(...),
    db: AsyncSession = Depends(get_db),
    moderator: User = Depends(require_moderator)
):
    """Модер может создать новый альбом"""
    try:
        new_album = Album(album_name=album_name.strip(), release_year=release_year)
        db.add(new_album)
        await db.flush()

        for author_id in author_ids:
            await db.execute(
                text("INSERT INTO authors_of_album (album_id, author_id) VALUES (:al_id, :au_id)"),
                {"al_id": new_album.album_id, "au_id": author_id}
            )

        for genre_id in genre_ids:
            await db.execute(
                text("INSERT INTO genres_of_album (album_id, genre_id) VALUES (:al_id, :g_id)"),
                {"al_id": new_album.album_id, "g_id": genre_id}
            )

        await db.commit()
        return {"status": "success", "message": "Альбом успешно создан."}
        
    except Exception as e:
        await db.rollback()
        raise HTTPException(status_code=500, detail=f"Ошибка транзакции: {str(e)}")