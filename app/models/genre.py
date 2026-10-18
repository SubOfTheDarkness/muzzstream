"""
Модуль моделей для жанров
"""

from sqlalchemy import TEXT, INT
from sqlalchemy.orm import Mapped, mapped_column
from app.models.base import Base
from typing import Optional

class Genre(Base):
    """Модель таблицы 'genres'"""
    __tablename__ = "genres"

    genre_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    genre_name: Mapped[str] = mapped_column(TEXT, unique=True, nullable=False)
