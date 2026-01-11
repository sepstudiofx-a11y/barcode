"""
Configuration Manager - Loads and manages system configuration

Supports metadata-driven configuration for country/customer customization.
"""

import json
import logging
from pathlib import Path
from typing import Dict, Any, Optional

logger = logging.getLogger(__name__)


class ConfigManager:
    """
    Manages application configuration with support for:
    - Base configuration
    - Country-specific overrides
    - Customer-specific overrides
    - Environment variables
    """
    
    def __init__(self, config_path: Optional[Path] = None):
        self.config_path = config_path
        self._config: Dict[str, Any] = {}
        self._load_config()
    
    def _load_config(self) -> None:
        """Load configuration from file or use defaults"""
        # Default configuration
        self._config = {
            'app_name': 'MindZen ERP',
            'version': '0.1.0',
            'debug': True,
            'database': {
                'type': 'sqlite',
                'path': 'mindzen_erp.db'
            },
            'modules': {
                'auto_discover': True,
                'auto_install': []
            },
            'multi_tenant': {
                'enabled': False,
                'schema_prefix': 'customer_'
            },
            'logging': {
                'level': 'INFO',
                'format': '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
            }
        }
        
        # Load from file if provided
        if self.config_path and self.config_path.exists():
            try:
                with open(self.config_path, 'r') as f:
                    file_config = json.load(f)
                self._merge_config(file_config)
                logger.info(f"Configuration loaded from: {self.config_path}")
            except Exception as e:
                logger.error(f"Error loading config file: {e}")
        else:
            logger.info("Using default configuration")
    
    def _merge_config(self, override: Dict[str, Any]) -> None:
        """Recursively merge override config into base config"""
        for key, value in override.items():
            if key in self._config and isinstance(self._config[key], dict) and isinstance(value, dict):
                self._merge_config_dict(self._config[key], value)
            else:
                self._config[key] = value
    
    def _merge_config_dict(self, base: Dict, override: Dict) -> None:
        """Helper to merge nested dictionaries"""
        for key, value in override.items():
            if key in base and isinstance(base[key], dict) and isinstance(value, dict):
                self._merge_config_dict(base[key], value)
            else:
                base[key] = value
    
    def get(self, key: str, default: Any = None) -> Any:
        """
        Get a configuration value using dot notation.
        
        Args:
            key: Configuration key (e.g., 'database.type')
            default: Default value if key not found
            
        Returns:
            Configuration value
        """
        keys = key.split('.')
        value = self._config
        
        for k in keys:
            if isinstance(value, dict) and k in value:
                value = value[k]
            else:
                return default
        
        return value
    
    def set(self, key: str, value: Any) -> None:
        """
        Set a configuration value using dot notation.
        
        Args:
            key: Configuration key (e.g., 'database.type')
            value: Value to set
        """
        keys = key.split('.')
        config = self._config
        
        for k in keys[:-1]:
            if k not in config:
                config[k] = {}
            config = config[k]
        
        config[keys[-1]] = value
    
    def load_country_config(self, country_code: str) -> None:
        """
        Load country-specific configuration overrides.
        
        Args:
            country_code: ISO country code (e.g., 'IN', 'US', 'DE')
        """
        config_dir = Path(__file__).parent.parent.parent.parent / 'config' / 'countries'
        country_file = config_dir / f"{country_code.lower()}.json"
        
        if country_file.exists():
            try:
                with open(country_file, 'r') as f:
                    country_config = json.load(f)
                self._merge_config(country_config)
                logger.info(f"Loaded country configuration: {country_code}")
            except Exception as e:
                logger.error(f"Error loading country config for {country_code}: {e}")
        else:
            logger.warning(f"Country configuration not found: {country_code}")
    
    def get_all(self) -> Dict[str, Any]:
        """Get entire configuration dictionary"""
        return self._config.copy()
    
    def save(self, path: Optional[Path] = None) -> None:
        """
        Save current configuration to file.
        
        Args:
            path: Path to save to (uses config_path if not provided)
        """
        save_path = path or self.config_path
        
        if not save_path:
            logger.error("No path specified for saving configuration")
            return
        
        try:
            save_path.parent.mkdir(parents=True, exist_ok=True)
            with open(save_path, 'w') as f:
                json.dump(self._config, f, indent=2)
            logger.info(f"Configuration saved to: {save_path}")
        except Exception as e:
            logger.error(f"Error saving configuration: {e}")
