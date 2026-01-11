"""
Sales Module Init
"""
import logging

logger = logging.getLogger(__name__)

def post_install():
    logger.info("Sales module installed")
