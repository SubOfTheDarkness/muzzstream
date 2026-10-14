"""
Модуль моделей музыкального каталога (Альбомы, Треки и их связи)
"""

from sqlalchemy import TEXT, INT, SMALLINT, ForeignKey, Table
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base
from app.models.author import Author
from app.models.genre import Genre
from typing import List

# Авторы альбома (N:M)
authors_of_album = Table(
    "authors_of_album",
    Base.metadata,
    mapped_column("album_id", INT, ForeignKey("albums.album_id", ondelete="CASCADE"), primary_key=True),
    mapped_column("author_id", INT, ForeignKey("authors.author_id", ondelete="CASCADE"), primary_key=True)
)

# Жанры альбома (N:M)
genres_of_album = Table(
    "genres_of_album",
    Base.metadata,
    mapped_column("album_id", INT, ForeignKey("albums.album_id", ondelete="CASCADE"), primary_key=True),
    mapped_column("genre_id", INT, ForeignKey("genres.genre_id", ondelete="RESTRICT"), primary_key=True)
)

# Авторы трека (N:M)
authors_of_track = Table(
    "authors_of_track",
    Base.metadata,
    mapped_column("track_id", INT, ForeignKey("tracks.track_id", ondelete="CASCADE"), primary_key=True),
    mapped_column("author_id", INT, ForeignKey("authors.author_id", ondelete="CASCADE"), primary_key=True)
)

# Жанры трека (N:M)
genres_of_track = Table(
    "genres_of_track",
    Base.metadata,
    mapped_column("track_id", INT, ForeignKey("tracks.track_id", ondelete="CASCADE"), primary_key=True),
    mapped_column("genre_id", INT, ForeignKey("genres.genre_id", ondelete="RESTRICT"), primary_key=True)
)


class Album(Base):
    """Модель таблицы 'albums'"""
    __tablename__ = "albums"

    album_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    album_name: Mapped[str] = mapped_column(TEXT, nullable=False)
    release_year: Mapped[int] = mapped_column(INT, nullable=False)

    authors: Mapped[List[Author]] = relationship("Author", secondary=authors_of_album)
    genres: Mapped[List[Genre]] = relationship("Genre", secondary=genres_of_album)
    
    tracks: Mapped[List["Track"]] = relationship("Track", back_populates="album", cascade="all, delete-orphan")


class Track(Base):
    """Модель таблицы 'tracks'"""
    __tablename__ = "tracks"

    track_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    track_name: Mapped[str] = mapped_column(TEXT, nullable=False)
    release_year: Mapped[int] = mapped_column(SMALLINT, nullable=False)
    duration_seconds: Mapped[int] = mapped_column(INT, nullable=False)
    
    album_id: Mapped[int | None] = mapped_column(INT, ForeignKey("albums.album_id", ondelete="SET NULL"), nullable=True)

    album: Mapped[Album | None] = relationship("Album", back_populates="tracks")

    authors: Mapped[List[Author]] = relationship("Author", secondary=authors_of_track)
    genres: Mapped[List[Genre]] = relationship("Genre", secondary=genres_of_track)
