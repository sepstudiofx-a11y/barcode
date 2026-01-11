from sqlalchemy import Column, String, Float, Integer, ForeignKey, Numeric, Date
from sqlalchemy.orm import relationship
from mindzen_erp.core.orm import BaseModel

class Lead(BaseModel):
    __tablename__ = 'crm_leads'
    
    name = Column(String, nullable=False)
    email = Column(String)
    phone = Column(String)
    company = Column(String)
    status = Column(String, default="new")  # new, contacted, qualified, lost
    source = Column(String, default="website")
    priority = Column(String, default="medium")
    notes = Column(String)
    expected_revenue = Column(Float, default=0.0)
    assigned_to = Column(Integer, nullable=True)

    def is_qualified(self) -> bool:
        return self.status == 'qualified'
    
    def mark_as_qualified(self):
        self.status = 'qualified'
        self.save()
