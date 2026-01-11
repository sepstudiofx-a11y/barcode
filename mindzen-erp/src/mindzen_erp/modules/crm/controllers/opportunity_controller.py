"""
Opportunity Controller - Business logic for opportunities
"""

import logging
from typing import List, Optional, Dict, Any
from decimal import Decimal

from ..models.opportunity import Opportunity

logger = logging.getLogger(__name__)


class OpportunityController:
    """
    Business logic for opportunity management.
    
    Handles:
    - Opportunity creation and updates
    - Stage management
    - Win/loss tracking
    - Revenue calculations
    """
    
    def __init__(self, engine=None):
        self.engine = engine
    
    def create_opportunity(self, data: Dict[str, Any]) -> Opportunity:
        """Create a new opportunity"""
        # Ensure amount is Decimal
        if 'amount' in data and not isinstance(data['amount'], Decimal):
            data['amount'] = Decimal(str(data['amount']))
        
        opportunity = Opportunity.create(data)
        opportunity.calculate_expected_revenue()
        opportunity.save()
        
        logger.info(f"Created opportunity: {opportunity.name} (ID: {opportunity.id})")
        
        # Publish event
        if self.engine:
            self.engine.events.publish('crm.opportunity.created', {
                'opportunity_id': opportunity.id,
                'name': opportunity.name,
                'amount': float(opportunity.amount)
            })
        
        return opportunity
    
    def update_opportunity(self, opp_id: int, data: Dict[str, Any]) -> Optional[Opportunity]:
        """Update an existing opportunity"""
        opportunity = Opportunity.find_by_id(opp_id)
        if not opportunity:
            logger.warning(f"Opportunity not found: {opp_id}")
            return None
        
        # Update fields
        for key, value in data.items():
            if hasattr(opportunity, key) and key not in ['id', 'created_at']:
                if key == 'amount' and not isinstance(value, Decimal):
                    value = Decimal(str(value))
                setattr(opportunity, key, value)
        
        opportunity.calculate_expected_revenue()
        opportunity.save()
        
        logger.info(f"Updated opportunity: {opportunity.name} (ID: {opportunity.id})")
        return opportunity
    
    def get_opportunity(self, opp_id: int) -> Optional[Opportunity]:
        """Get opportunity by ID"""
        return Opportunity.find_by_id(opp_id)
    
    def list_opportunities(self, stage: str = None, limit: int = 100) -> List[Opportunity]:
        """List opportunities with optional filtering"""
        if stage:
            return Opportunity.find_by(stage=stage)[:limit]
        return Opportunity.find_all(limit)
    
    def move_to_stage(self, opp_id: int, stage: str) -> Optional[Opportunity]:
        """Move opportunity to a different stage"""
        opportunity = Opportunity.find_by_id(opp_id)
        if not opportunity:
            return None
        
        old_stage = opportunity.stage
        opportunity.move_to_stage(stage)
        
        logger.info(f"Moved opportunity {opp_id} from '{old_stage}' to '{stage}'")
        
        # Publish event
        if self.engine:
            self.engine.events.publish('crm.opportunity.stage_changed', {
                'opportunity_id': opp_id,
                'old_stage': old_stage,
                'new_stage': stage
            })
        
        return opportunity
    
    def mark_as_won(self, opp_id: int) -> Optional[Opportunity]:
        """Mark opportunity as won"""
        opportunity = Opportunity.find_by_id(opp_id)
        if not opportunity:
            return None
        
        opportunity.mark_as_won()
        
        logger.info(f"Opportunity {opp_id} marked as WON (${opportunity.amount})")
        
        # Publish event for integration with Sales module
        if self.engine:
            self.engine.events.publish('crm.opportunity.won', {
                'opportunity_id': opp_id,
                'amount': float(opportunity.amount),
                'lead_id': opportunity.lead_id
            })
        
        return opportunity
    
    def mark_as_lost(self, opp_id: int) -> Optional[Opportunity]:
        """Mark opportunity as lost"""
        opportunity = Opportunity.find_by_id(opp_id)
        if not opportunity:
            return None
        
        opportunity.mark_as_lost()
        
        logger.info(f"Opportunity {opp_id} marked as LOST")
        
        # Publish event
        if self.engine:
            self.engine.events.publish('crm.opportunity.lost', {
                'opportunity_id': opp_id
            })
        
        return opportunity
    
    def get_statistics(self) -> Dict[str, Any]:
        """Get opportunity statistics"""
        all_opps = Opportunity.find_all(limit=10000)
        
        stats = {
            'total': len(all_opps),
            'by_stage': {},
            'total_amount': Decimal('0.00'),
            'total_expected_revenue': Decimal('0.00'),
            'won_count': 0,
            'won_amount': Decimal('0.00'),
            'lost_count': 0
        }
        
        for opp in all_opps:
            # Count by stage
            stats['by_stage'][opp.stage] = stats['by_stage'].get(opp.stage, 0) + 1
            
            # Sum amounts
            stats['total_amount'] += opp.amount
            stats['total_expected_revenue'] += opp.expected_revenue
            
            # Track wins/losses
            if opp.stage == 'won':
                stats['won_count'] += 1
                stats['won_amount'] += opp.amount
            elif opp.stage == 'lost':
                stats['lost_count'] += 1
        
        # Convert Decimals to float for JSON serialization
        stats['total_amount'] = float(stats['total_amount'])
        stats['total_expected_revenue'] = float(stats['total_expected_revenue'])
        stats['won_amount'] = float(stats['won_amount'])
        
        return stats
