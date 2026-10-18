"""
Точка инициализации пакета моделей
Импортирует все модели для их автоматической регистрации в метаданных SQLAlchemy
"""

from app.models.base import Base
from app.models.user import User
from app.models.role import Role
from app.models.session import Session
from app.models.log import AuditLog, StatusCode
from app.models.author import Author, GroupRole, AuthorMember
from app.models.genre import Genre
from app.models.catalog import Album, Composition, Track
from app.models.playlist import Playlist
from app.models.history import ListeningHistory

from app.models.views import TrackDetailsView

__all__ = [
    "Base", "User", "Role", "Session", "AuditLog", "StatusCode", "Author", "GroupRole", "AuthorMember", "Genre", "Album", "Composition", "Track", "Playlist", "ListeningHistory",
    "TrackDetailsView"
]
