"""
Главный модуль инициализации приложения «MuzzStream API».

Создает экземпляр FastAPI, настраивает глобальные обработчики ошибок и регистрирует маршруты для работы с аудиосервисом.
"""

import asyncio
from contextlib import asynccontextmanager
from sqlalchemy import text
from sqlalchemy.ext.asyncio import AsyncSession
from fastapi import FastAPI, Depends, Request, status
from fastapi.responses import JSONResponse
from app.database import engine, async_session_local
from app.routers import tracks, auth, playlists, references, moderation

CHECK_AUTHOR_STATUS_INTERVAL_SEC = 60

async def auto_restore_author_statuses():
    """
    Фоновая задача, сбрасывающая временные баны обратно на исторический статус группы по истечении времени.
    """
    while True:
        await asyncio.sleep(CHECK_AUTHOR_STATUS_INTERVAL_SEC)
        print("[BACKGROUND] Проверка временных санкций исполнителей...")
        
        async with async_session_local() as db:
            try:
                stmt = """
                    UPDATE authors 
                    SET current_status = next_status,
                        status_until = NULL,
                        next_status = NULL
                    WHERE status_until IS NOT NULL
                    AND NOW() >= status_until
                    AND next_status IS NOT NULL;
                """
                result = await db.execute(text(stmt))
                await db.commit()
                
                if result.rowcount > 0:
                    print(f"[BACKGROUND] Успешно восстановлен статус для {result.rowcount} исполнителей.")
            except Exception as e:
                await db.rollback()
                print(f"[BACKGROUND] Ошибка фонового обновления статусов: {str(e)}")


@asynccontextmanager
async def lifespan(app: FastAPI):
    status_task = asyncio.create_task(auto_restore_author_statuses())
    yield
    status_task.cancel()

app = FastAPI(
    title="MuzzStream API",
    description="Бэкенд-API для управления треками, авторами, пользовательскими плейлистами и аудиостримингом.",
    version="KKMT",
    lifespan=lifespan
)

app.include_router(references.router, prefix="/api/v1", tags=["References"])
app.include_router(auth.router, prefix="/api/v1/auth", tags=["Auth"])
app.include_router(tracks.router, prefix="/api/v1/tracks", tags=["Tracks"])
app.include_router(playlists.router, prefix="/api/v1/playlists", tags=["Playlists"])
app.include_router(moderation.router, prefix="/api/v1/moderation", tags=["Moderation"])

@app.middleware("http")
async def check_db(request: Request, call_next):
    """
    Глобальный фильтр доступности.
    Перед обработкой любого HTTP-запроса выполняет поверку активности БД. Если СУБД не отвечает за 2 секунды, прерывает запрос и возвращает HTTP 503 Service Unavailable.
    """

    if request.url.path in ["/", "/docs", "/openapi.json", "/redoc"]:
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

@app.get("/api/v1")
async def root() -> dict:
    """Корневой эндпоинт для проверки статуса работы сервиса"""
    return {"status": "working", "message": "Welcome to the MuzzStream Backend"}