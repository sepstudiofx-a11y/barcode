"""
Hook Manager - Executes hooks.py logic for module interactions

Implements the metadata-driven hook system shown in the architecture.
Example: "If Inventory installed, run inventory.create.picking"
"""

import logging
from typing import Dict, Any, Callable, List, Optional

logger = logging.getLogger(__name__)


class HookManager:
    """
    Manages execution of module hooks for cross-module integration.
    
    Hooks allow modules to extend or modify behavior of other modules.
    Example from architecture:
        - When Sales Order is confirmed → trigger Inventory picking
        - When Opportunity is won → create Sales Order
    """
    
    def __init__(self, event_bus):
        self.event_bus = event_bus
        self._hooks: Dict[str, List[Callable]] = {}
        self._module_hooks: Dict[str, Any] = {}
        logger.info("Hook manager initialized")
    
    def register_hook(self, hook_name: str, callback: Callable) -> None:
        """
        Register a hook callback.
        
        Args:
            hook_name: Name of the hook (e.g., 'on_module_installed')
            callback: Function to execute when hook is triggered
        """
        if hook_name not in self._hooks:
            self._hooks[hook_name] = []
        
        self._hooks[hook_name].append(callback)
        logger.debug(f"Registered hook: {hook_name}")
    
    def register_module_hooks(self, module_name: str, hooks_module: Any) -> None:
        """
        Register all hooks from a module's hooks.py file.
        
        Args:
            module_name: Name of the module
            hooks_module: The imported hooks module
        """
        self._module_hooks[module_name] = hooks_module
        
        # Auto-register standard hooks if they exist
        standard_hooks = [
            'on_module_installed',
            'on_module_uninstalled',
            'post_install',
            'pre_uninstall'
        ]
        
        for hook_name in standard_hooks:
            if hasattr(hooks_module, hook_name):
                callback = getattr(hooks_module, hook_name)
                self.register_hook(hook_name, callback)
                logger.debug(f"  Registered {module_name}.{hook_name}")
    
    def execute(self, hook_name: str, **kwargs) -> List[Any]:
        """
        Execute all callbacks registered for a hook.
        
        Args:
            hook_name: Name of the hook to execute
            **kwargs: Arguments to pass to hook callbacks
            
        Returns:
            List of return values from all callbacks
        """
        if hook_name not in self._hooks:
            logger.debug(f"No callbacks for hook: {hook_name}")
            return []
        
        logger.debug(f"Executing hook: {hook_name}")
        results = []
        
        for callback in self._hooks[hook_name]:
            try:
                result = callback(**kwargs)
                results.append(result)
            except Exception as e:
                logger.error(f"Error executing hook '{hook_name}': {e}", exc_info=True)
        
        return results
    
    def check_condition(self, condition: str) -> bool:
        """
        Check if a conditional hook should execute.
        
        Example from architecture:
            "If Inventory installed, run inventory.create.picking"
            
        Args:
            condition: Condition string to evaluate
            
        Returns:
            True if condition is met
        """
        # Simple implementation - check if module is installed
        if condition.startswith("module_installed:"):
            module_name = condition.split(":", 1)[1].strip()
            return module_name in self._module_hooks
        
        return False
    
    def execute_conditional(self, condition: str, hook_name: str, **kwargs) -> Optional[List[Any]]:
        """
        Execute a hook only if condition is met.
        
        Args:
            condition: Condition to check
            hook_name: Hook to execute if condition is true
            **kwargs: Arguments for the hook
            
        Returns:
            Hook results if executed, None otherwise
        """
        if self.check_condition(condition):
            logger.debug(f"Condition '{condition}' met, executing hook '{hook_name}'")
            return self.execute(hook_name, **kwargs)
        else:
            logger.debug(f"Condition '{condition}' not met, skipping hook '{hook_name}'")
            return None
    
    def get_module_hook(self, module_name: str, hook_name: str) -> Optional[Callable]:
        """
        Get a specific hook function from a module.
        
        Args:
            module_name: Name of the module
            hook_name: Name of the hook function
            
        Returns:
            The hook function if it exists
        """
        if module_name not in self._module_hooks:
            return None
        
        hooks_module = self._module_hooks[module_name]
        return getattr(hooks_module, hook_name, None)
    
    def clear_hooks(self, hook_name: Optional[str] = None) -> None:
        """
        Clear registered hooks.
        
        Args:
            hook_name: Specific hook to clear, or None to clear all
        """
        if hook_name:
            if hook_name in self._hooks:
                del self._hooks[hook_name]
                logger.debug(f"Cleared hook: {hook_name}")
        else:
            self._hooks.clear()
            logger.debug("Cleared all hooks")
