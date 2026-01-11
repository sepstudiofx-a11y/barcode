from sqlalchemy import Column, String, Boolean, Integer, DateTime
from mindzen_erp.core.orm import BaseModel
import hashlib
import os

class User(BaseModel):
    __tablename__ = 'res_users'
    
    name = Column(String, nullable=False)
    username = Column(String, unique=True, nullable=False)
    password_hash = Column(String, nullable=False)
    email = Column(String)
    is_admin = Column(Boolean, default=False)
    is_active = Column(Boolean, default=True)
    
    # Simple image avatar URL or path
    image_url = Column(String)

    def set_password(self, password: str):
        """Hash and set password"""
        salt = os.urandom(32).hex()
        hash = hashlib.sha256((password + salt).encode('utf-8')).hexdigest()
        self.password_hash = f"{salt}:{hash}"

    def check_password(self, password: str) -> bool:
        """Verify password"""
        try:
            salt, hash = self.password_hash.split(':')
            verify_hash = hashlib.sha256((password + salt).encode('utf-8')).hexdigest()
            return hash == verify_hash
        except ValueError:
            return False
