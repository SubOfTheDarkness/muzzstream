"""
Модуль базового класса для моделей SQLAlchemy
Предоставляет единую точку инициализации метаданных для всех таблиц
"""

from sqlalchemy.orm import DeclarativeBase


class Base(DeclarativeBase):
    """Базовый класс для всех моделей"""
