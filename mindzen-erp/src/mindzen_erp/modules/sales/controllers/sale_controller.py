"""
Sales Controller
"""

import logging
from typing import List, Optional, Dict, Any
from datetime import datetime

from ..models.sale_order import SaleOrder, SaleOrderLine

logger = logging.getLogger(__name__)

class SaleController:
    def __init__(self, engine=None):
        self.engine = engine
        
    def create_order(self, data: Dict[str, Any]) -> SaleOrder:
        """Create a new quotation/order"""
        # Generate sequence name if not provided
        if not data.get('name'):
            count = len(SaleOrder.find_all()) + 1
            data['name'] = f"SO{str(count).zfill(3)}"
            
        order = SaleOrder.create(data)
        logger.info(f"Created order: {order.name}")
        return order

    def update_order(self, order_id: int, data: Dict[str, Any]) -> Optional[SaleOrder]:
        order = SaleOrder.find_by_id(order_id)
        if not order:
            return None
        
        # Update scalar fields
        for key, value in data.items():
            if hasattr(order, key) and key not in ['id', 'lines', 'created_at']:
                setattr(order, key, value)
        
        order.save()
        return order

    def list_orders(self, state: str = None) -> List[SaleOrder]:
        if state:
            return SaleOrder.find_by(state=state)
        return SaleOrder.find_all()
        
    def get_order(self, order_id: int) -> Optional[SaleOrder]:
        return SaleOrder.find_by_id(order_id)
        
    def confirm_order(self, order_id: int):
        order = SaleOrder.find_by_id(order_id)
        if order:
            order.confirm()
            logger.info(f"Confirmed order: {order.name}")
            return order
