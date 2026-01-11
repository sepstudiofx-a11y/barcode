from sqlalchemy import Column, String, Integer, ForeignKey, Numeric, Date, Text, Float
from sqlalchemy.orm import relationship
from mindzen_erp.core.orm import BaseModel
from decimal import Decimal

class Opportunity(BaseModel):
    __tablename__ = 'crm_opportunities'
    
    name = Column(String, nullable=False)
    lead_id = Column(Integer, nullable=True)
    
    # Financials - Using Float for simplicity in this demo, Numeric is better for production
    amount = Column(Float, default=0.0) 
    probability = Column(Integer, default=50)
    expected_revenue = Column(Float, default=0.0)
    
    stage = Column(String, default="qualification")
    assigned_to = Column(Integer, nullable=True)
    notes = Column(Text)
    
    expected_close_date = Column(Date, nullable=True)
    actual_close_date = Column(Date, nullable=True)

    def calculate_expected_revenue(self):
        if self.amount and self.probability:
            self.expected_revenue = float(self.amount) * (self.probability / 100.0)

    def mark_as_won(self):
        self.stage = 'won'
        self.probability = 100
        self.calculate_expected_revenue()
        self.save()
        
    def move_to_stage(self, stage: str):
        self.stage = stage
        self.save()
