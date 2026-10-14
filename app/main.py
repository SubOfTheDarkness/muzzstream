"""
Главный модуль инициализации
Создает экземпляр FastAPI, настраивает глобальные обработчики ошибок и регистрирует маршрутизацию (роутеры)
"""

from fastapi import FastAPI, Depends
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import text
from app.database import get_db

app = FastAPI(
    title="MuzzStream API",
    description="Бэкенд-API для управления треками, авторами, пользовательскими плейлистами и аудиостримингом.",
    version="1.0.0"
)


@app.get("/")
async def root() -> dict:
    """Корневой эндпоинт для проверки статуса работы сервиса"""
    return {"status": "working", "message": "Welcome to the MuzzStream Backend"}


@app.get("/api/v1/db-check")
async def check_db_connection(db: AsyncSession = Depends(get_db)) -> dict:
    """
    Тестовый эндпоинт для проверки соединения с БД
    Выполняет простейший легковесный SQL-запрос через пул подключений
    """
    try:
        result = await db.execute(text("SELECT 1"))
        return {"status": "connected", "postgres_response": result.scalar()}
    except Exception as e:
        return {"status": "error", "details": str(e)}
