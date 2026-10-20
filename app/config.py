"""
Конфигурация бэкенда
Загружает переменные окружения из файла .env и настраивает подключение к БД
"""

import os
from pydantic_settings import BaseSettings, SettingsConfigDict
from pydantic import EmailStr
from fastapi_mail import ConnectionConfig

class Settings(BaseSettings):
    """Менеджер настроек"""
    DB_USER: str
    DB_PASS: str
    DB_HOST: str
    DB_PORT: int
    DB_NAME: str

    SMTP_SERVER: str
    SMTP_PORT: int
    SMTP_EMAIL: EmailStr
    SMTP_TOKEN: str

    @property
    def database_url(self) -> str:
        """Формирует строку подключения к БД для psycopg"""
        return f"postgresql+psycopg://{self.DB_USER}:{self.DB_PASS}@{self.DB_HOST}:{self.DB_PORT}/{self.DB_NAME}"
    
    @property
    def base_dir(self) -> str:
        """Возвращает абсолютный путь к корню всего проекта."""
        return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))

    @property
    def mail_config(self) -> ConnectionConfig:
        """Генерирует объект конфигурации для fastapi-mail"""
        return ConnectionConfig(
            MAIL_USERNAME=self.SMTP_EMAIL,
            MAIL_PASSWORD=self.SMTP_TOKEN,
            MAIL_FROM=self.SMTP_EMAIL,
            MAIL_PORT=self.SMTP_PORT,
            MAIL_SERVER=self.SMTP_SERVER,
            MAIL_FROM_NAME="MuzzStream Service",
            MAIL_STARTTLS=False,
            MAIL_SSL_TLS=True,
            USE_CREDENTIALS=True,
            VALIDATE_CERTS=True
        )

    model_config = SettingsConfigDict(
        env_file=os.path.join(os.path.dirname(os.path.dirname(__file__)), ".env"),
        env_file_encoding="utf-8"
    )


settings = Settings()
