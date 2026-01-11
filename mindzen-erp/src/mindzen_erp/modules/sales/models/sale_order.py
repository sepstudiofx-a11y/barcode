from sqlalchemy import Column, String, Integer, Float, ForeignKey, DateTime, func
from sqlalchemy.orm import relationship
from mindzen_erp.core.orm import BaseModel

class SaleOrder(BaseModel):
    __tablename__ = 'sale_orders'
    
    name = Column(String, nullable=False, unique=True)  # SO001
    date_order = Column(DateTime(timezone=True), server_default=func.now())
    
    # Customer info (simple string for now, would link to Contact module later)
    customer_name = Column(String, nullable=False)
    email = Column(String)
    
    # State
    state = Column(String, default='draft')  # draft, sent, limit, sale, cancel
    
    # Totals
    amount_tax = Column(Float, default=0.0)
    amount_total = Column(Float, default=0.0)
    
    # Relational
    opportunity_id = Column(Integer, nullable=True)  # Link to CRM
    
    # Relationships
    lines = relationship("SaleOrderLine", back_populates="order", cascade="all, delete-orphan", lazy="selectin")

    def confirm(self):
        """Confirm quotation to sales order"""
        self.state = 'sale'
        self.save()
        
    def cancel(self):
        """Cancel order"""
        self.state = 'cancel'
        self.save()

class SaleOrderLine(BaseModel):
    __tablename__ = 'sale_order_lines'
    
    order_id = Column(Integer, ForeignKey('sale_orders.id'))
    
    product_name = Column(String, nullable=False)
    quantity = Column(Float, default=1.0)
    price_unit = Column(Float, default=0.0)
    subtotal = Column(Float, default=0.0)
    
    order = relationship("SaleOrder", back_populates="lines")
    
    def calculate_subtotal(self):
        self.subtotal = self.quantity * self.price_unit
