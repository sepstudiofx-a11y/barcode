"""
Lead Controller - Business logic for leads
"""

import logging
from typing import List, Optional, Dict, Any

from ..models.lead import Lead
from ..models.opportunity import Opportunity

logger = logging.getLogger(__name__)


class LeadController:
    """
    Business logic for lead management.
    
    Handles:
    - Lead creation and updates
    - Lead qualification
    - Conversion to opportunities
    - Lead assignment
    """
    
    def __init__(self, engine=None):
        self.engine = engine
    
    def create_lead(self, data: Dict[str, Any]) -> Lead:
        """
        Create a new lead.
        
        Args:
            data: Lead data dictionary
            
        Returns:
            Created Lead instance
        """
        lead = Lead.create(data)
        logger.info(f"Created lead: {lead.name} (ID: {lead.id})")
        
        # Publish event
        if self.engine:
            self.engine.events.publish('crm.lead.created', {
                'lead_id': lead.id,
                'name': lead.name
            })
        
        return lead
    
    def update_lead(self, lead_id: int, data: Dict[str, Any]) -> Optional[Lead]:
        """Update an existing lead"""
        lead = Lead.find_by_id(lead_id)
        if not lead:
            logger.warning(f"Lead not found: {lead_id}")
            return None
        
        # Update fields
        for key, value in data.items():
            if hasattr(lead, key) and key not in ['id', 'created_at']:
                setattr(lead, key, value)
        
        lead.save()
        logger.info(f"Updated lead: {lead.name} (ID: {lead.id})")
        
        return lead
    
    def get_lead(self, lead_id: int) -> Optional[Lead]:
        """Get lead by ID"""
        return Lead.find_by_id(lead_id)
    
    def list_leads(self, status: str = None, limit: int = 100) -> List[Lead]:
        """
        List leads with optional filtering.
        
        Args:
            status: Filter by status (optional)
            limit: Maximum number of results
            
        Returns:
            List of Lead instances
        """
        if status:
            return Lead.find_by(status=status)[:limit]
        return Lead.find_all(limit)
    
    def assign_lead(self, lead_id: int, user_id: int) -> Optional[Lead]:
        """Assign lead to a user"""
        lead = Lead.find_by_id(lead_id)
        if not lead:
            return None
        
        lead.assigned_to = user_id
        lead.save()
        
        logger.info(f"Assigned lead {lead_id} to user {user_id}")
        return lead
    
    def convert_to_opportunity(self, lead_id: int, opportunity_data: Dict[str, Any] = None) -> Optional[Opportunity]:
        """
        Convert a lead to an opportunity.
        
        Args:
            lead_id: ID of the lead to convert
            opportunity_data: Additional data for the opportunity (optional)
            
        Returns:
            Created Opportunity instance
        """
        lead = Lead.find_by_id(lead_id)
        if not lead:
            logger.warning(f"Lead not found: {lead_id}")
            return None
        
        # Prepare opportunity data
        opp_data = opportunity_data or {}
        opp_data.setdefault('name', f"Opportunity from {lead.name}")
        opp_data.setdefault('lead_id', lead_id)
        opp_data.setdefault('assigned_to', lead.assigned_to)
        opp_data.setdefault('stage', 'qualification')
        
        if lead.expected_revenue > 0:
            from decimal import Decimal
            opp_data.setdefault('amount', Decimal(str(lead.expected_revenue)))
        
        # Create opportunity
        opportunity = Opportunity.create(opp_data)
        
        # Mark lead as qualified
        lead.mark_as_qualified()
        
        logger.info(f"Converted lead {lead_id} to opportunity {opportunity.id}")
        
        # Publish event
        if self.engine:
            self.engine.events.publish('crm.lead.converted', {
                'lead_id': lead_id,
                'opportunity_id': opportunity.id
            })
        
        return opportunity
    
    def delete_lead(self, lead_id: int) -> bool:
        """Delete a lead"""
        lead = Lead.find_by_id(lead_id)
        if not lead:
            return False
        
        lead.delete()
        logger.info(f"Deleted lead: {lead_id}")
        return True
    
    def get_statistics(self) -> Dict[str, Any]:
        """Get lead statistics"""
        all_leads = Lead.find_all(limit=10000)
        
        stats = {
            'total': len(all_leads),
            'by_status': {},
            'by_source': {},
            'total_expected_revenue': 0.0
        }
        
        for lead in all_leads:
            # Count by status
            stats['by_status'][lead.status] = stats['by_status'].get(lead.status, 0) + 1
            
            # Count by source
            stats['by_source'][lead.source] = stats['by_source'].get(lead.source, 0) + 1
            
            # Sum expected revenue
            stats['total_expected_revenue'] += lead.expected_revenue
        
        return stats
