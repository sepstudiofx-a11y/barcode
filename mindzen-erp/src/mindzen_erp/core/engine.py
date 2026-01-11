"""
Core Engine - The Microkernel

Coordinates all core services and manages the application lifecycle.
"""

import logging
from typing import Dict, Any, Optional
from pathlib import Path

from .module_registry import ModuleRegistry
from .event_bus import EventBus
from .hooks import HookManager
from .config import ConfigManager


logger = logging.getLogger(__name__)


class Engine:
    """
    The microkernel engine that coordinates all core services.
    
    Responsibilities:
    - Initialize and manage core services
    - Load and coordinate modules
    - Provide centralized configuration
    - Manage application lifecycle
    """
    
    _instance: Optional['Engine'] = None
    
    def __new__(cls):
        """Singleton pattern - only one engine instance"""
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance
    
    def __init__(self):
        """Initialize the engine and core services"""
        if hasattr(self, '_initialized'):
            return
            
        self._initialized = False
        self.config: Optional[ConfigManager] = None
        self.modules: Optional[ModuleRegistry] = None
        self.events: Optional[EventBus] = None
        self.hooks: Optional[HookManager] = None
        
        logger.info("MindZen ERP Engine initialized")
    
    def initialize(self, config_path: Optional[Path] = None) -> None:
        """
        Initialize the engine with configuration.
        
        Args:
            config_path: Path to configuration file (optional)
        """
        if self._initialized:
            logger.warning("Engine already initialized")
            return
        
        logger.info("Starting engine initialization...")
        
        # Initialize configuration manager
        self.config = ConfigManager(config_path)
        logger.info("Configuration loaded")
        
        # Initialize event bus
        self.events = EventBus()
        logger.info("Event bus initialized")
        
        # Initialize hook manager
        self.hooks = HookManager(self.events)
        logger.info("Hook manager initialized")
        
        # Initialize module registry
        self.modules = ModuleRegistry(self.config, self.events, self.hooks)
        logger.info("Module registry initialized")
        
        self._initialized = True
        logger.info("✓ Engine initialization complete")
    
    def discover_modules(self) -> None:
        """Discover all available modules"""
        if not self._initialized:
            raise RuntimeError("Engine not initialized. Call initialize() first.")
        
        logger.info("Discovering modules...")
        self.modules.discover()
        logger.info(f"Found {len(self.modules.available_modules)} modules")
    
    def install_module(self, module_name: str) -> bool:
        """
        Install and activate a module.
        
        Args:
            module_name: Name of the module to install
            
        Returns:
            True if installation successful
        """
        if not self._initialized:
            raise RuntimeError("Engine not initialized. Call initialize() first.")
        
        logger.info(f"Installing module: {module_name}")
        success = self.modules.install(module_name)
        
        if success:
            # Trigger hook for module installation
            self.hooks.execute(f"on_module_installed", module_name=module_name)
            self.events.publish("module.installed", {"module": module_name})
            logger.info(f"✓ Module '{module_name}' installed successfully")
        else:
            logger.error(f"✗ Failed to install module '{module_name}'")
        
        return success
    
    def uninstall_module(self, module_name: str) -> bool:
        """
        Uninstall and deactivate a module.
        
        Args:
            module_name: Name of the module to uninstall
            
        Returns:
            True if uninstallation successful
        """
        if not self._initialized:
            raise RuntimeError("Engine not initialized. Call initialize() first.")
        
        logger.info(f"Uninstalling module: {module_name}")
        success = self.modules.uninstall(module_name)
        
        if success:
            self.events.publish("module.uninstalled", {"module": module_name})
            logger.info(f"✓ Module '{module_name}' uninstalled successfully")
        else:
            logger.error(f"✗ Failed to uninstall module '{module_name}'")
        
        return success
    
    def get_installed_modules(self) -> list:
        """Get list of installed module names"""
        if not self._initialized:
            raise RuntimeError("Engine not initialized. Call initialize() first.")
        
        return self.modules.get_installed()
    
    def shutdown(self) -> None:
        """Gracefully shutdown the engine"""
        logger.info("Shutting down engine...")
        
        if self.modules:
            self.modules.shutdown_all()
        
        if self.events:
            self.events.publish("engine.shutdown", {})
        
        logger.info("✓ Engine shutdown complete")
    
    @property
    def is_initialized(self) -> bool:
        """Check if engine is initialized"""
        return self._initialized
    
    def __repr__(self) -> str:
        status = "initialized" if self._initialized else "not initialized"
        return f"<Engine ({status})>"
