from pydantic import BaseModel, EmailStr, Field
from typing import Optional

class AdminCreateUserSchema(BaseModel):
    nickname: str = Field(..., min_length=3, max_length=16)
    username: Optional[str] = None
    email: EmailStr
    password: str = Field(..., min_length=8)
    is_verified: bool = True

class ChangeUserRoleSchema(BaseModel):
    user_id: int
    new_role_id: int
    
class AdminChangePasswordSchema(BaseModel):
    user_id: int
    new_password: str = Field(..., min_length=8)

class AdminUpdateAuthorSchema(BaseModel):
    author_name: Optional[str] = None
    country: Optional[str] = None
