"""
Модуль базового класса для схем
Предоставляет единые настройки конфигурации для всех объектов данных
"""

from pydantic import BaseModel, ConfigDict


class BaseResponseSchema(BaseModel):
    """
    Базовая схема для всех выходных данных (ответов API).
    Включает автоматический маппинг атрибутов SQLAlchemy.
    """
    model_config = ConfigDict(from_attributes=True)
