"""
CRM Module - Customer Relationship Management

Manage leads, opportunities, and sales pipeline.
"""

import logging

logger = logging.getLogger(__name__)

__version__ = "1.0.0"

def post_install():
    """Called after module installation"""
    logger.info("CRM module installed successfully")
    # Create default pipelines, stages, etc.

def pre_uninstall():
    """Called before module uninstallation"""
    logger.info("Preparing to uninstall CRM module")

def shutdown():
    """Called when module is being shut down"""
    logger.debug("CRM module shutting down")
