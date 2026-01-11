
import os
import sys
import logging
from typing import Dict, Any

# Add src to path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../src')))

from mindzen_erp.core.orm import Database, BaseModel
from mindzen_erp.modules.crm.controllers.lead_controller import LeadController
from mindzen_erp.modules.sales.controllers.sale_controller import SaleController
from mindzen_erp.modules.sales.models.sale_order import SaleOrderLine

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("verify_modules")

def cleanup():
    if os.path.exists("verify_modules.db"):
        os.remove("verify_modules.db")

def test_crm_module():
    logger.info("Testing CRM Module...")
    controller = LeadController()
    
    # Test Create
    lead_data = {
        "name": "Test Lead",
        "email": "test@example.com",
        "company": "Test Co",
        "expected_revenue": 5000.0,
        "status": "new" 
    }
    lead = controller.create_lead(lead_data)
    assert lead is not None
    assert lead.id is not None
    assert lead.name == "Test Lead"
    logger.info(f"✅ Created Lead: {lead.id}")
    
    # Test Read
    fetched_lead = controller.get_lead(lead.id)
    assert fetched_lead is not None
    assert fetched_lead.email == "test@example.com"
    logger.info(f"✅ Read Lead: {fetched_lead.id}")
    
    # Test Update
    update_data = {"status": "qualified", "expected_revenue": 6000.0}
    updated_lead = controller.update_lead(lead.id, update_data)
    assert updated_lead.status == "qualified"
    assert updated_lead.expected_revenue == 6000.0
    logger.info(f"✅ Updated Lead: {updated_lead.id}")
    
    return lead

def test_sales_module():
    logger.info("Testing Sales Module...")
    controller = SaleController()
    
    # Test Create Order with Lines
    order_data = {
        "customer_name": "Test Customer",
        "lines": [
            {
                "product_name": "Product A",
                "quantity": 2,
                "price_unit": 100.0
            },
             {
                "product_name": "Product B",
                "quantity": 1,
                "price_unit": 50.0
            }
        ]
    }
    
    logger.info(f"Attempting to create order with {len(order_data['lines'])} lines...")
    order = controller.create_order(order_data)
    
    assert order is not None
    assert order.id is not None
    assert order.name.startswith("SO")
    logger.info(f"✅ Created Order: {order.name} (ID: {order.id})")
    
    # Verify Lines
    # We need to re-fetch to be sure, although ORM might have them in session
    fetched_order = controller.get_order(order.id)
    
    logger.info(f"Order Lines Count: {len(fetched_order.lines)}")
    
    if len(fetched_order.lines) == 0:
        logger.error("❌ Order lines were NOT created! This is a bug in the ORM or Controller.")
        raise AssertionError("Order lines were not created")
    else:
        logger.info("✅ Order lines created successfully.")
        
    return order

def run_tests():
    cleanup()
    
    # Setup DB
    db_url = "sqlite:///./verify_modules.db"
    Database().connect(db_url)
    
    try:
        test_crm_module()
        test_sales_module()
    except AssertionError as e:
        logger.error(f"❌ Test Failed: {e}")
        sys.exit(1)
    except Exception as e:
        logger.error(f"❌ An error occurred: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
    else:
        logger.info("✨ All tests passed!")
    finally:
        # cleanup() 
        pass

if __name__ == "__main__":
    run_tests()
