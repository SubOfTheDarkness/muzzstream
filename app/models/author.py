"""
Модуль моделей для авторов (исполнителей)
"""

from sqlalchemy import TEXT, INT, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.models.base import Base

class Author(Base):
    """Модель таблицы 'authors'"""
    __tablename__ = "authors"

    author_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    author_name: Mapped[str] = mapped_column(TEXT, nullable=False)
    country: Mapped[str | None] = mapped_column(TEXT, nullable=True)
    
    is_verified: Mapped[bool] = mapped_column(nullable=False, default=False)
    created_by_user_id: Mapped[int | None] = mapped_column(
        INT, 
        ForeignKey("users.user_id", ondelete="SET NULL"), 
        nullable=True
    )

class GroupRole(Base):
    """Модель таблицы 'group_roles' (Справочник должностей в группе)"""
    __tablename__ = "group_roles"

    group_role_id: Mapped[int] = mapped_column(INT, primary_key=True, autoincrement=True)
    role_name: Mapped[str] = mapped_column(TEXT, unique=True, nullable=False)


class AuthorMember(Base):
    """
    Модель таблицы 'author_members'.
    Связывает юзера с исполнителем(группой), определяя его членство в группе и уровень прав (group_role_id).
    """
    __tablename__ = "author_members"

    author_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("authors.author_id", ondelete="CASCADE"), 
        primary_key=True
    )
    user_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("users.user_id", ondelete="CASCADE"), 
        primary_key=True
    )
    group_role_id: Mapped[int] = mapped_column(
        INT, 
        ForeignKey("group_roles.group_role_id", ondelete="RESTRICT"), 
        nullable=False, 
        default=3
    )

    role: Mapped["GroupRole"] = relationship("GroupRole")
