"""
Модуль моделей для Views

Позволяет взаимодействовать с результатами запросов средствами SQLAlchemy без использования сырого SQL
"""

from sqlalchemy import TEXT, INT, SMALLINT
from sqlalchemy.orm import Mapped, mapped_column
from app.models.base import Base


class TrackDetailsView(Base):
    """
    Отображение представления 'view_track_details'.
    
    Содержит полную агрегированную информацию о физическом треке, его версии, родительской композиции, альбоме и авторах.
    """
    __tablename__ = "view_track_details"

    track_id: Mapped[int] = mapped_column(INT, primary_key=True)
    duration: Mapped[int] = mapped_column(INT)
    version_type: Mapped[str] = mapped_column(TEXT)
    filepath: Mapped[str] = mapped_column(TEXT)
    
    composition_id: Mapped[int] = mapped_column(INT)
    composition_name: Mapped[str] = mapped_column(TEXT)
    composition_release_year: Mapped[int] = mapped_column(SMALLINT)
    
    album_id: Mapped[int | None] = mapped_column(INT, nullable=True)
    album_name: Mapped[str | None] = mapped_column(TEXT, nullable=True)
    album_release_year: Mapped[int | None] = mapped_column(SMALLINT, nullable=True)
    
    authors_list: Mapped[str | None] = mapped_column(TEXT, nullable=True)

