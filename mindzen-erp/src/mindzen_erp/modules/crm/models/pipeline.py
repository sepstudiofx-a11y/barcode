from sqlalchemy import Column, String, Boolean, Integer, JSON
from mindzen_erp.core.orm import BaseModel

class Pipeline(BaseModel):
    __tablename__ = 'crm_pipelines'
    
    name = Column(String, nullable=False)
    # Storing stages as JSON list for simplicity in Postgres/SQLite
    # In a strict normalized schema, this might be a separate table 'PipelineStage'
    stages = Column(JSON, default=lambda: ["Qualification", "Proposal", "Negotiation", "Won"])
    is_default = Column(Boolean, default=False)
    is_active = Column(Boolean, default=True)

    def add_stage(self, stage_name: str):
        current_stages = list(self.stages)
        current_stages.append(stage_name)
        self.stages = current_stages
        self.save()
    
    def remove_stage(self, stage_name: str):
        if stage_name in self.stages:
            current_stages = list(self.stages)
            current_stages.remove(stage_name)
            self.stages = current_stages
            self.save()
