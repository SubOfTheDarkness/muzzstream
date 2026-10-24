from fastapi import APIRouter, Depends
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select
from app.database import get_db
from app.models.role import Role
from app.models.genre import Genre
from app.models.author import GroupRole, AuthorStatus

router = APIRouter()

@router.get("/roles/all")
async def get_all_roles(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Role))
    return result.scalars().all()

@router.get("/genres/all")
async def get_all_genres(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Genre))
    return result.scalars().all()

@router.get("/group_roles/all")
async def get_all_group_roles(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(GroupRole))
    return result.scalars().all()

@router.get("/author_statuses/all")
async def get_all_author_statuses(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(AuthorStatus))
    return result.scalars().all()
