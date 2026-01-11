# MindZen ERP

A scalable, modular Python ERP platform with microkernel architecture and pluggable modules for small-scale industry.

## Features

✨ **Microkernel Architecture** - Lightweight core with pluggable modules  
🔌 **Module System** - Dynamic module discovery and loading  
📡 **Event Bus** - Pub/sub communication between modules  
🪝 **Hooks System** - Conditional module integration  
🌍 **Multi-Country Support** - Metadata-driven configuration  
🏢 **Multi-Tenant Ready** - Schema isolation per customer  

## Project Structure

```
mindzen-erp/
├── src/
│   └── mindzen_erp/
│       ├── core/              # Core engine
│       │   ├── engine.py      # Main microkernel
│       │   ├── module_registry.py
│       │   ├── event_bus.py
│       │   ├── hooks.py
│       │   └── config.py
│       ├── modules/           # Pluggable modules
│       │   ├── crm/
│       │   ├── sales/
│       │   ├── inventory/
│       │   ├── purchase/
│       │   ├── manufacturing/
│       │   └── accounting/
│       └── main.py            # Demo script
├── config/
│   ├── countries/             # Country-specific configs
│   └── metadata/              # Field metadata
├── tests/
└── requirements.txt
```

## Quick Start

### 1. Setup Environment

```bash
# Create virtual environment
python -m venv venv

# Activate (Windows)
venv\Scripts\activate

# Activate (Linux/Mac)
source venv/bin/activate

# Install dependencies
pip install -r requirements.txt
```

### 2. Run Demo

```bash
python -m mindzen_erp.main
```

## Core Engine Demo

The demo script demonstrates:
- Engine initialization
- Configuration management
- Module discovery
- Event bus pub/sub
- Hook system execution

## Architecture

Based on Odoo-like microkernel design:

- **Engine (Microkernel)**: Coordinates all services
- **Module Registry**: Discovers and loads modules with dependency resolution
- **Event Bus**: Decoupled module communication
- **Hook Manager**: Conditional cross-module integration
- **Config Manager**: Hierarchical configuration with country overrides

## Development Status

- [x] Core engine implementation
- [x] Module registry with dependency resolution
- [x] Event bus for pub/sub messaging
- [x] Hook system for module integration
- [x] Configuration management
- [ ] ORM/Data layer
- [ ] Authentication & RBAC
- [ ] Multi-tenant schema router
- [ ] Core modules (CRM, Sales, Inventory, etc.)

## License

MIT License
