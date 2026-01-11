"""
CRM Module Hooks - Integration with other modules
"""

import logging

logger = logging.getLogger(__name__)


def on_module_installed(module_name):
    """
    Called when any module is installed.
    
    Integrates CRM with other modules.
    """
    if module_name == "sales":
        logger.info("Sales module detected - enabling CRM-Sales integration")
        # Subscribe to opportunity.won event to create sales orders
        # This will be implemented when Sales module exists
    
    elif module_name == "accounting":
        logger.info("Accounting module detected - enabling CRM-Accounting integration")
        # Track revenue from won opportunities


def on_opportunity_won(data):
    """
    Hook called when an opportunity is won.
    
    Can trigger:
    - Sales order creation (if Sales module installed)
    - Revenue recognition (if Accounting module installed)
    """
    opportunity_id = data.get('opportunity_id')
    amount = data.get('amount')
    
    logger.info(f"Opportunity {opportunity_id} won with amount ${amount}")
    
    # This hook can be extended by other modules
    # Example: Sales module can subscribe to 'crm.opportunity.won' event
    # and automatically create a sales order


def on_lead_converted(data):
    """
    Hook called when a lead is converted to opportunity.
    
    Can trigger:
    - Notifications
    - Automated follow-up tasks
    """
    lead_id = data.get('lead_id')
    opportunity_id = data.get('opportunity_id')
    
    logger.info(f"Lead {lead_id} converted to opportunity {opportunity_id}")
