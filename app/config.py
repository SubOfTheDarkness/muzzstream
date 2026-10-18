"""
Конфигурация бэкенда
Загружает переменные окружения из файла .env и настраивает подключение к БД
"""

import os
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Менеджер настроек"""
    DB_USER: str
    DB_PASS: str
    DB_HOST: str
    DB_PORT: int
    DB_NAME: str

    @property
    def database_url(self) -> str:
        """Формирует строку подключения к БД для psycopg"""
        return f"postgresql+psycopg://{self.DB_USER}:{self.DB_PASS}@{self.DB_HOST}:{self.DB_PORT}/{self.DB_NAME}"
    
    @property
    def base_dir(self) -> str:
        """Возвращает абсолютный путь к корню всего проекта."""
        return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))

    model_config = SettingsConfigDict(
        env_file=os.path.join(os.path.dirname(os.path.dirname(__file__)), ".env"),
        env_file_encoding="utf-8"
    )


settings = Settings()
