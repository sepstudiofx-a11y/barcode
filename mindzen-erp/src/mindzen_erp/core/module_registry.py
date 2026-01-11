"""
Module Registry - Discovers and manages pluggable modules
"""

import json
import logging
import importlib
from pathlib import Path
from typing import Dict, List, Optional, Any

logger = logging.getLogger(__name__)


class ModuleMetadata:
    """Represents module metadata from manifest.json"""
    
    def __init__(self, data: Dict[str, Any]):
        self.name: str = data.get('name', '')
        self.version: str = data.get('version', '0.1.0')
        self.description: str = data.get('description', '')
        self.author: str = data.get('author', '')
        self.depends: List[str] = data.get('depends', [])
        self.category: str = data.get('category', 'other')
        self.installable: bool = data.get('installable', True)
        self.auto_install: bool = data.get('auto_install', False)
    
    def __repr__(self) -> str:
        return f"<ModuleMetadata {self.name} v{self.version}>"


class ModuleRegistry:
    """
    Discovers, loads, and manages pluggable modules.
    
    Each module is a self-contained package with:
    - manifest.json: Module metadata
    - models/: Data models
    - controllers/: Business logic
    - views/: UI templates
    - hooks.py: Integration hooks
    """
    
    def __init__(self, config, events, hooks):
        self.config = config
        self.events = events
        self.hooks = hooks
        
        self.available_modules: Dict[str, ModuleMetadata] = {}
        self.installed_modules: Dict[str, Any] = {}
        self.module_paths: Dict[str, Path] = {}
        
        # Get modules directory from config or use default
        self.modules_dir = Path(__file__).parent.parent / 'modules'
        logger.info(f"Module directory: {self.modules_dir}")
    
    def discover(self) -> None:
        """
        Discover all available modules by scanning the modules directory.
        Each module must have a manifest.json file.
        """
        if not self.modules_dir.exists():
            logger.warning(f"Modules directory not found: {self.modules_dir}")
            self.modules_dir.mkdir(parents=True, exist_ok=True)
            return
        
        logger.info("Scanning for modules...")
        
        for module_path in self.modules_dir.iterdir():
            if not module_path.is_dir():
                continue
            
            manifest_path = module_path / 'manifest.json'
            if not manifest_path.exists():
                logger.debug(f"Skipping {module_path.name} - no manifest.json")
                continue
            
            try:
                with open(manifest_path, 'r') as f:
                    manifest_data = json.load(f)
                
                metadata = ModuleMetadata(manifest_data)
                
                if not metadata.installable:
                    logger.debug(f"Skipping {metadata.name} - not installable")
                    continue
                
                self.available_modules[metadata.name] = metadata
                self.module_paths[metadata.name] = module_path
                
                logger.info(f"  Found module: {metadata.name} v{metadata.version}")
                
            except Exception as e:
                logger.error(f"Error loading manifest for {module_path.name}: {e}")
        
        logger.info(f"Discovery complete: {len(self.available_modules)} modules found")
    
    def install(self, module_name: str) -> bool:
        """
        Install a module and its dependencies.
        
        Args:
            module_name: Name of the module to install
            
        Returns:
            True if installation successful
        """
        if module_name in self.installed_modules:
            logger.warning(f"Module '{module_name}' already installed")
            return True
        
        if module_name not in self.available_modules:
            logger.error(f"Module '{module_name}' not found")
            return False
        
        metadata = self.available_modules[module_name]
        
        # Install dependencies first
        for dep in metadata.depends:
            if dep not in self.installed_modules:
                logger.info(f"Installing dependency: {dep}")
                if not self.install(dep):
                    logger.error(f"Failed to install dependency '{dep}' for '{module_name}'")
                    return False
        
        # Load the module
        try:
            module_path = self.module_paths[module_name]
            
            # Import the module package
            module_package = f"mindzen_erp.modules.{module_name}"
            module = importlib.import_module(module_package)
            
            # Load hooks if they exist
            hooks_path = module_path / 'hooks.py'
            if hooks_path.exists():
                hooks_module = importlib.import_module(f"{module_package}.hooks")
                self.hooks.register_module_hooks(module_name, hooks_module)
                logger.debug(f"  Loaded hooks for {module_name}")
            
            # Store installed module
            self.installed_modules[module_name] = {
                'metadata': metadata,
                'module': module,
                'path': module_path
            }
            
            # Execute post-install hook if defined
            if hasattr(module, 'post_install'):
                module.post_install()
            
            logger.info(f"✓ Module '{module_name}' installed")
            return True
            
        except Exception as e:
            logger.error(f"Error installing module '{module_name}': {e}", exc_info=True)
            return False
    
    def uninstall(self, module_name: str) -> bool:
        """
        Uninstall a module.
        
        Args:
            module_name: Name of the module to uninstall
            
        Returns:
            True if uninstallation successful
        """
        if module_name not in self.installed_modules:
            logger.warning(f"Module '{module_name}' not installed")
            return False
        
        # Check if other modules depend on this one
        for name, metadata in self.available_modules.items():
            if name in self.installed_modules and module_name in metadata.depends:
                logger.error(f"Cannot uninstall '{module_name}' - required by '{name}'")
                return False
        
        try:
            module_info = self.installed_modules[module_name]
            module = module_info['module']
            
            # Execute pre-uninstall hook if defined
            if hasattr(module, 'pre_uninstall'):
                module.pre_uninstall()
            
            # Remove from installed modules
            del self.installed_modules[module_name]
            
            logger.info(f"✓ Module '{module_name}' uninstalled")
            return True
            
        except Exception as e:
            logger.error(f"Error uninstalling module '{module_name}': {e}")
            return False
    
    def get_installed(self) -> List[str]:
        """Get list of installed module names"""
        return list(self.installed_modules.keys())
    
    def get_available(self) -> List[str]:
        """Get list of available module names"""
        return list(self.available_modules.keys())
    
    def get_module_info(self, module_name: str) -> Optional[Dict[str, Any]]:
        """Get information about a module"""
        if module_name in self.installed_modules:
            return self.installed_modules[module_name]
        return None
    
    def shutdown_all(self) -> None:
        """Shutdown all installed modules"""
        logger.info("Shutting down all modules...")
        
        for module_name, module_info in self.installed_modules.items():
            module = module_info['module']
            if hasattr(module, 'shutdown'):
                try:
                    module.shutdown()
                    logger.debug(f"  Shutdown {module_name}")
                except Exception as e:
                    logger.error(f"Error shutting down {module_name}: {e}")
