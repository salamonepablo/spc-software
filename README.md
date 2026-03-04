# SPC - Integral Management Software

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)](https://blazor.net/)
[![Tests](https://img.shields.io/badge/Tests-39%20passing-brightgreen)]()
[![License](https://img.shields.io/badge/License-Proprietary-red)](LICENSE)

Enterprise Resource Planning (ERP) system for battery distribution company. Migration from legacy VB6 + Access to modern .NET stack.

## Features

- **Customer Management** - Full CRUD with soft delete and multiple addresses
- **Product Catalog** - Stock control across multiple warehouses
- **Invoicing** - Electronic invoicing with AFIP integration (Argentina)
- **Delivery Notes** - Shipping management
- **Quotes** - Budget generation
- **Credit/Debit Notes** - Adjustments and corrections
- **Current Accounts** - Customer balance tracking
- **IIBB Withholdings** - ARBA integration (Buenos Aires province)
- **License System** - Modular feature licensing (Base/Premium/Enterprise)

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Blazor Server |
| Backend | ASP.NET Core 10 (Minimal APIs) |
| Database | SQLite (dev) / SQL Server (prod) |
| ORM | Entity Framework Core 10 |
| Auth | Windows Authentication |
| Testing | xUnit + FluentAssertions |

## Architecture

The project follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      PRESENTATION                           │
│              (Blazor Pages, API Endpoints)                  │
├─────────────────────────────────────────────────────────────┤
│                      APPLICATION                            │
│                (Services, DTOs/Contracts)                   │
├─────────────────────────────────────────────────────────────┤
│                        DOMAIN                               │
│              (Entities in SPC.Shared)                       │
├─────────────────────────────────────────────────────────────┤
│                    INFRASTRUCTURE                           │
│              (EF Core, SPCDbContext)                        │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
spc-software/
├── SPC.API/                    # REST API backend
│   ├── Contracts/              # DTOs (Request/Response)
│   │   ├── Clientes/
│   │   │   ├── ClienteResponse.cs
│   │   │   ├── CreateClienteRequest.cs
│   │   │   └── UpdateClienteRequest.cs
│   │   └── Productos/
│   │       ├── ProductoResponse.cs
│   │       ├── CreateProductoRequest.cs
│   │       └── UpdateProductoRequest.cs
│   ├── Data/
│   │   └── SPCDbContext.cs     # Entity Framework context
│   ├── Endpoints/              # Minimal API endpoint modules
│   │   ├── ClientesEndpoints.cs
│   │   └── ProductosEndpoints.cs
│   ├── Services/               # Business logic layer
│   │   ├── IClientesService.cs
│   │   ├── ClientesService.cs
│   │   ├── IProductosService.cs
│   │   ├── ProductosService.cs
│   │   └── LicenseService.cs
│   └── Program.cs              # Minimal startup
│
├── SPC.Shared/                 # Shared domain models
│   └── Models/                 # 26 entity classes
│
├── SPC.Web/                    # Blazor Server frontend
│
├── SPC.Tests/                  # Test suite (39 tests)
│   ├── Infrastructure/
│   │   └── SPCWebApplicationFactory.cs
│   ├── Integration/            # API integration tests
│   │   ├── ClientesEndpointsTests.cs
│   │   ├── ProductosEndpointsTests.cs
│   │   ├── AuxiliaryEndpointsTests.cs
│   │   └── LicenseEndpointTests.cs
│   └── Unit/                   # Unit tests
│       └── LicenseServiceTests.cs
│
└── docs/                       # Documentation
    ├── adr/                    # Architecture Decision Records
    └── *.md                    # Technical guides
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- SQL Server (for production)

### Installation

```bash
# Clone the repository
git clone https://github.com/pablosala/spc-software.git
cd spc-software

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### Running the Application

**API (Backend)**
```bash
cd SPC.API
dotnet run
# API available at: https://localhost:5001
# Swagger UI: https://localhost:5001/swagger
```

**Web (Frontend)**
```bash
cd SPC.Web
dotnet run
# Web app available at: https://localhost:5002
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ClientesEndpointsTests"
```

## API Endpoints

### Clientes (Customers)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/clientes` | List all active customers |
| GET | `/api/clientes/{id}` | Get customer by ID |
| GET | `/api/clientes/buscar?nombre=xxx` | Search customers by name |
| POST | `/api/clientes` | Create new customer |
| PUT | `/api/clientes/{id}` | Update customer |
| DELETE | `/api/clientes/{id}` | Soft delete customer |

### Productos (Products)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/productos` | List all active products |
| GET | `/api/productos/{id}` | Get product by ID |
| GET | `/api/productos/buscar?q=xxx` | Search by code or description |
| POST | `/api/productos` | Create new product |
| PUT | `/api/productos/{id}` | Update product |

### Auxiliary Data

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/vendedores` | List sales representatives |
| POST | `/api/vendedores` | Create sales representative |
| GET | `/api/rubros` | List product categories |
| GET | `/api/depositos` | List warehouses |
| GET | `/api/condicionesiva` | List tax conditions |
| GET | `/api/zonasventas` | List sales zones |

### System

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | System info and status |
| GET | `/api/license` | License information |

## Configuration

### Development

Configuration is stored in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SPC.db"
  },
  "License": {
    "Tier": "BASE",
    "EnabledFeatures": []
  }
}
```

### Production

Use environment variables or Azure Key Vault for sensitive configuration:

```bash
# Windows
set ConnectionStrings__DefaultConnection=Server=...;Database=SPC;Trusted_Connection=True;

# Linux/Mac
export ConnectionStrings__DefaultConnection="Server=...;Database=SPC;Trusted_Connection=True;"
```

## Architecture Decisions

Key architectural decisions are documented in [docs/adr/](docs/adr/):

| ADR | Title |
|-----|-------|
| [ADR-001](docs/adr/001-minimal-apis.md) | Use Minimal APIs over Controllers |
| [ADR-002](docs/adr/002-clean-architecture.md) | Clean Architecture with Services and DTOs |
| [ADR-003](docs/adr/003-soft-delete.md) | Soft Delete for Business Entities |
| [ADR-004](docs/adr/004-database-strategy.md) | SQLite for Dev, SQL Server for Prod |
| [ADR-005](docs/adr/005-license-system.md) | Modular License System |

## Documentation

- [Architecture Decision Records](docs/adr/) - Key architectural decisions
- [Minimal APIs vs Controllers](docs/minimal-apis-vs-controllers.md) - Comparison guide
- [CLAUDE.md](CLAUDE.md) - Complete migration plan and context

## Contributing

This is a proprietary project. Contact the development team for contribution guidelines.

## License

Proprietary - All rights reserved.

## Author

- **Pablo Salamone** - *Development* - [@pablosala](https://github.com/pablosala)
