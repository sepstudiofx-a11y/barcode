"""
Demo script to test CRM module installation and functionality
"""

import logging
from pathlib import Path

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

from mindzen_erp.core import Engine
from mindzen_erp.modules.crm.controllers import LeadController, OpportunityController

def main():
    """Demonstrate CRM module functionality"""
    print("=" * 70)
    print("MindZen ERP - CRM Module Demo")
    print("=" * 70)
    print()
    
    # Initialize engine
    print("1. Initializing engine...")
    engine = Engine()
    engine.initialize()
    print()
    
    # Discover and install CRM module
    print("2. Discovering modules...")
    engine.discover_modules()
    available = engine.modules.get_available()
    print(f"   Available modules: {available}")
    print()
    
    if 'crm' in available:
        print("3. Installing CRM module...")
        success = engine.install_module('crm')
        if success:
            print("   [OK] CRM module installed successfully")
        else:
            print("   [FAIL] Failed to install CRM module")
            return
        print()
    else:
        print("   [WARNING] CRM module not found in available modules")
        print()
    
    # Test Lead functionality
    print("4. Testing Lead Management...")
    lead_controller = LeadController(engine)
    
    # Create leads
    lead1 = lead_controller.create_lead({
        'name': 'John Doe',
        'email': 'john@example.com',
        'company': 'Acme Corp',
        'phone': '+1-555-0100',
        'status': 'new',
        'source': 'website',
        'expected_revenue': 50000.0
    })
    print(f"   Created: {lead1}")
    
    lead2 = lead_controller.create_lead({
        'name': 'Jane Smith',
        'email': 'jane@techcorp.com',
        'company': 'TechCorp',
        'status': 'contacted',
        'source': 'referral',
        'expected_revenue': 75000.0
    })
    print(f"   Created: {lead2}")
    
    # List leads
    all_leads = lead_controller.list_leads()
    print(f"   Total leads: {len(all_leads)}")
    
    # Get statistics
    stats = lead_controller.get_statistics()
    print(f"   Lead statistics: {stats}")
    print()
    
    # Test Opportunity functionality
    print("5. Testing Opportunity Management...")
    opp_controller = OpportunityController(engine)
    
    # Convert lead to opportunity
    opportunity = lead_controller.convert_to_opportunity(
        lead1.id,
        {'amount': 50000, 'probability': 70}
    )
    print(f"   Converted lead to: {opportunity}")
    print(f"   Expected revenue: ${opportunity.expected_revenue}")
    
    # Create another opportunity
    opp2 = opp_controller.create_opportunity({
        'name': 'Big Deal with TechCorp',
        'amount': 100000,
        'probability': 50,
        'stage': 'proposal'
    })
    print(f"   Created: {opp2}")
    
    # Move through stages
    print(f"   Moving opportunity through pipeline...")
    opp_controller.move_to_stage(opportunity.id, 'proposal')
    opp_controller.move_to_stage(opportunity.id, 'negotiation')
    
    # Mark as won
    won_opp = opp_controller.mark_as_won(opportunity.id)
    print(f"   [WON] Opportunity WON: ${won_opp.amount}")
    
    # Get statistics
    opp_stats = opp_controller.get_statistics()
    print(f"   Opportunity statistics: {opp_stats}")
    print()
    
    # Test event bus integration
    print("6. Testing Event Bus Integration...")
    
    def on_opportunity_won(data):
        print(f"   [EVENT] Event received: Opportunity {data['opportunity_id']} won!")
        print(f"      Amount: ${data['amount']}")
    
    engine.events.subscribe('crm.opportunity.won', on_opportunity_won)
    
    # Create and win another opportunity to trigger event
    opp3 = opp_controller.create_opportunity({
        'name': 'Quick Win',
        'amount': 25000,
        'probability': 90,
        'stage': 'negotiation'
    })
    opp_controller.mark_as_won(opp3.id)
    print()
    
    # Show final status
    print("7. Final Status:")
    print(f"   Installed modules: {engine.get_installed_modules()}")
    print(f"   Total leads: {len(lead_controller.list_leads())}")
    print(f"   Total opportunities: {len(opp_controller.list_opportunities())}")
    print(f"   Won opportunities: {opp_stats['won_count']}")
    print(f"   Total won amount: ${opp_stats['won_amount']}")
    print()
    
    # Shutdown
    print("8. Shutting down...")
    engine.shutdown()
    print()
    
    print("=" * 70)
    print("[SUCCESS] CRM Module Demo Complete!")
    print("=" * 70)

if __name__ == "__main__":
    main()
