"""
Auth Controller
"""
import logging
from typing import Optional
from ..user import User

logger = logging.getLogger(__name__)

class AuthController:
    def __init__(self, engine=None):
        self.engine = engine
        
    def login(self, username, password) -> Optional[User]:
        """Authenticate user"""
        users = User.find_by(username=username)
        if not users:
            return None
        
        user = users[0]
        if user.check_password(password):
            logger.info(f"User logged in: {username}")
            return user
        
        logger.warning(f"Failed login attempt for: {username}")
        return None
    
    def ensure_superadmin(self):
        """Create superadmin if doesn't exist"""
        if not User.find_by(username="admin"):
            admin = User.create({
                "name": "Super Admin",
                "username": "admin",
                "email": "admin@mindzen.com",
                "is_admin": True,
                "password_hash": "" # Will set below
            })
            admin.set_password("admin") # Default password
            admin.save()
            logger.info("Created default superadmin user (admin/admin)")
