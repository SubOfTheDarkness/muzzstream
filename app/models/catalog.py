"""
Модуль моделей музыкального каталога.
Разделяется на композиции (юридическая суть песни, авторы, жанры)
и треки (сами физические файлы).
"""

import os
from app.config import settings
from typing import List, Optional
from sqlalchemy import TEXT, INT, SMALLINT, ForeignKey, Table, Column  # Добавили Column в импорты
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from app.models.author import Author
from app.models.genre import Genre

# Авторы альбома (N:M)
authors_of_album = Table(
    "authors_of_album",
    Base.metadata,
    Column("album_id", INT, ForeignKey("albums.album_id", ondelete="CASCADE"), primary_key=True),
    Column("author_id", INT, ForeignKey("authors.author_id", ondelete="CASCADE"), primary_key=True)
)

# Жанры альбома (N:M)
genres_of_album = Table(
    "genres_of_album",
    Base.metadata,
    Column("album_id", INT, ForeignKey("albums.album_id", ondelete="CASCADE"), primary_key=True),
    Column("genre_id", INT, ForeignKey("genres.genre_id", ondelete="RESTRICT"), primary_key=True)
)

# Авторы композиции (N:M)
authors_of_composition = Table(
    "authors_of_composition",
    Base.metadata,
    Column("composition_id", INT, ForeignKey("compositions.composition_id", ondelete="CASCADE"), primary_key=True),
    Column("author_id", INT, ForeignKey("authors.author_id", ondelete="CASCADE"), primary_key=True)
)

# Жанры композиции (N:M)
genres_of_composition = Table(
    "genres_of_composition",
    Base.metadata,
    Column("composition_id", INT, ForeignKey("compositions.composition_id", ondelete="CASCADE"), primary_key=True),
    Column("genre_id", INT, ForeignKey("genres.genre_id", ondelete="RESTRICT"), primary_key=True)
)


class Album(Base):
    """Модель таблицы 'albums'"""
    __tablename__ = "albums"

    album_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    album_name: Mapped[str] = mapped_column(TEXT, nullable=False)
    release_year: Mapped[int] = mapped_column(SMALLINT, nullable=False)

    authors: Mapped[List[Author]] = relationship("Author", secondary=authors_of_album)
    genres: Mapped[List[Genre]] = relationship("Genre", secondary=genres_of_album)
    
    tracks: Mapped[List["Track"]] = relationship("Track", back_populates="album", cascade="all, delete-orphan")

class Composition(Base):
    """
    Модель таблицы 'compositions'.
    
    Описывает саму песню.
    Хранит имя, год создания, авторов и жанры. Может иметь несколько версий аудиозаписей.
    """
    __tablename__ = "compositions"

    composition_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    composition_name: Mapped[str] = mapped_column(TEXT, nullable=False)
    release_year: Mapped[int] = mapped_column(SMALLINT, nullable=False)

    authors: Mapped[List[Author]] = relationship("Author", secondary=authors_of_composition)
    genres: Mapped[List[Genre]] = relationship("Genre", secondary=genres_of_composition)

    tracks: Mapped[List["Track"]] = relationship(
        "Track", 
        back_populates="composition", 
        cascade="all, delete-orphan"
    )


class Track(Base):
    """
    Модель таблицы 'tracks'.
    
    Описывает конкретный физический аудиофайл на сервере.
    Хранит длительность, тип версии и указывает на родительскую композицию.
    """
    __tablename__ = "tracks"

    track_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    composition_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("compositions.composition_id", ondelete="CASCADE"), 
        nullable=False
    )
    
    album_id: Mapped[Optional[int]] = mapped_column(
        INT, 
        ForeignKey("albums.album_id", ondelete="SET NULL"), 
        nullable=True
    )
    
    filepath: Mapped[str] = mapped_column(TEXT, unique=True, nullable=False)
    duration: Mapped[int] = mapped_column(INT, nullable=False)
    version_type: Mapped[str] = mapped_column(TEXT, nullable=False, default="Studio")

    composition: Mapped["Composition"] = relationship("Composition", back_populates="tracks")
    album: Mapped[Optional["Album"]] = relationship("Album", back_populates="tracks")

    @property
    def absolute_filepath(self) -> str:
        """
        Динамически вычисляет абсолютный путь к аудиофайлу на сервере.
        """
        return os.path.join(settings.base_dir, "media", self.filepath)
