"""
Управление подключением и сессиями базы данных
Инициализирует асинхронный SQLAlchemy и предоставляет функцию-зависимость для получения изолированных контекстов сессий при обработке запросов
"""

from typing import AsyncGenerator
from sqlalchemy.ext.asyncio import create_async_engine, async_sessionmaker, AsyncSession
from app.config import settings

engine = create_async_engine(
    settings.database_url, 
    echo=True,
    connect_args={
        "connect_timeout": 2
    }
)

async_session_local = async_sessionmaker(
    bind=engine,
    class_=AsyncSession,
    expire_on_commit=False
)


async def get_db() -> AsyncGenerator[AsyncSession, None]:
    """
    Функция-зависимость, создающая активную асинхронную сессию базы данных.
    Гарантирует автоматическое закрытие соединения и возврат его в пул после завершения жизненного цикла HTTP-запроса.
    """
    async with async_session_local() as session:
        yield session
