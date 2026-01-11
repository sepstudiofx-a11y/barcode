"""
Event Bus - Pub/Sub system for module communication

Allows modules to communicate without tight coupling.
"""

import logging
from typing import Dict, List, Callable, Any
from collections import defaultdict

logger = logging.getLogger(__name__)


class EventBus:
    """
    Publish-Subscribe event system for decoupled module communication.
    
    Example:
        # Module A subscribes to an event
        engine.events.subscribe('sales.order.created', on_order_created)
        
        # Module B publishes the event
        engine.events.publish('sales.order.created', {'order_id': 123})
    """
    
    def __init__(self):
        self._subscribers: Dict[str, List[Callable]] = defaultdict(list)
        logger.info("Event bus initialized")
    
    def subscribe(self, event_name: str, callback: Callable) -> None:
        """
        Subscribe to an event.
        
        Args:
            event_name: Name of the event (e.g., 'sales.order.created')
            callback: Function to call when event is published
        """
        self._subscribers[event_name].append(callback)
        logger.debug(f"Subscribed to event: {event_name}")
    
    def unsubscribe(self, event_name: str, callback: Callable) -> None:
        """
        Unsubscribe from an event.
        
        Args:
            event_name: Name of the event
            callback: The callback function to remove
        """
        if event_name in self._subscribers:
            try:
                self._subscribers[event_name].remove(callback)
                logger.debug(f"Unsubscribed from event: {event_name}")
            except ValueError:
                logger.warning(f"Callback not found for event: {event_name}")
    
    def publish(self, event_name: str, data: Any = None) -> None:
        """
        Publish an event to all subscribers.
        
        Args:
            event_name: Name of the event
            data: Event data to pass to subscribers
        """
        if event_name not in self._subscribers:
            logger.debug(f"No subscribers for event: {event_name}")
            return
        
        logger.debug(f"Publishing event: {event_name}")
        
        for callback in self._subscribers[event_name]:
            try:
                callback(data)
            except Exception as e:
                logger.error(f"Error in event callback for '{event_name}': {e}", exc_info=True)
    
    def clear(self, event_name: Optional[str] = None) -> None:
        """
        Clear subscribers for an event or all events.
        
        Args:
            event_name: Event to clear, or None to clear all
        """
        if event_name:
            if event_name in self._subscribers:
                del self._subscribers[event_name]
                logger.debug(f"Cleared subscribers for: {event_name}")
        else:
            self._subscribers.clear()
            logger.debug("Cleared all event subscribers")
    
    def get_event_names(self) -> List[str]:
        """Get list of all event names with subscribers"""
        return list(self._subscribers.keys())
    
    def get_subscriber_count(self, event_name: str) -> int:
        """Get number of subscribers for an event"""
        return len(self._subscribers.get(event_name, []))
