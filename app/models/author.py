"""
Модуль моделей для авторов (исполнителей)
"""

from sqlalchemy import TEXT, INT
from sqlalchemy.orm import Mapped, mapped_column
from app.models.base import Base


class Author(Base):
    """Модель таблицы 'authors'"""
    __tablename__ = "authors"

    author_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    author_name: Mapped[str] = mapped_column(TEXT, nullable=False)
