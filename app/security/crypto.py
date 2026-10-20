"""
Модуль криптографии
Реализует хеширование паролей через bcrypt и верификацию
"""

import bcrypt


def hash_password(password: str) -> str:
    """Превращает пароль в хеш"""
    salt = bcrypt.gensalt()
    hashed = bcrypt.hashpw(password.encode("utf-8"), salt)
    return hashed.decode("utf-8")


def verify_password(plain_password: str, hashed_password: str) -> bool:
    """Проверяет, соответствует ли введенный пароль сохраненному в базе хешу"""
    return bcrypt.checkpw(plain_password.encode("utf-8"), hashed_password.encode("utf-8"))
