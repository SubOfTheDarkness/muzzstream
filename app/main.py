"""
Главный модуль инициализации приложения «MuzzStream API».

Создает экземпляр FastAPI, настраивает глобальные обработчики ошибок и регистрирует маршрутизацию (роутеры) для работы с аудиосервисом.
"""

import asyncio
from fastapi import FastAPI, Depends, Request, status
from sqlalchemy.ext.asyncio import AsyncSession
from fastapi.responses import JSONResponse
from sqlalchemy import text
from app.database import engine
from app.routers import tracks

app = FastAPI(
    title="MuzzStream API",
    description="Бэкенд-API для управления треками, авторами, пользовательскими плейлистами и аудиостримингом.",
    version="1.0.0"
)

app.include_router(tracks.router, prefix="/api/v1/tracks", tags=["Tracks"])

@app.middleware("http")
async def check_db(request: Request, call_next):
    """
    Глобальный фильтр доступности.
    Перед обработкой любого HTTP-запроса выполняет поверку активности БД. Если СУБД не отвечает за 2 секунды, прерывает запрос и возвращает HTTP 503 Service Unavailable.
    """

    if request.url.path in ["/", "/docs", "/openapi.json"]:
        return await call_next(request)
        
    try:
        async with engine.connect() as conn:
            await asyncio.wait_for(
                conn.execute(text("SELECT 1")),
                timeout=2.0
            )
    except (asyncio.TimeoutError, Exception):
        return JSONResponse(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            content={
                "error": "Database Connection Error",
                "message": "Сервис временно недоступен по техническим причинам: отсутствует связь с базой данных.",
                "code": 503
            }
        )
        
    return await call_next(request)

@app.get("/")
async def root() -> dict:
    """Корневой эндпоинт для проверки статуса работы сервиса"""
    return {"status": "working", "message": "Welcome to the MuzzStream Backend"}