# SPC - Integral Management Software

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)](https://blazor.net/)
[![License](https://img.shields.io/badge/License-Proprietary-red)](LICENSE)

Enterprise Resource Planning (ERP) system for battery distribution company. Migration from legacy VB6 + Access to modern .NET stack.

## Features

- **Customer Management** - Full CRUD with multiple addresses
- **Product Catalog** - Stock control across multiple warehouses
- **Invoicing** - Electronic invoicing with AFIP integration (Argentina)
- **Delivery Notes** - Shipping management
- **Quotes** - Budget generation
- **Credit/Debit Notes** - Adjustments and corrections
- **Current Accounts** - Customer balance tracking
- **IIBB Withholdings** - ARBA integration (Buenos Aires province)

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Blazor Server |
| Backend | ASP.NET Core 10 (Minimal APIs) |
| Database | SQLite (dev) / SQL Server (prod) |
| ORM | Entity Framework Core 10 |
| Auth | Windows Authentication |

## Project Structure

```
spc-software/
├── SPC.API/           # REST API backend
├── SPC.Shared/        # Shared models and DTOs
├── SPC.Web/           # Blazor Server frontend
└── docs/              # Documentation
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- SQL Server (for production)

### Installation

```bash
# Clone the repository
git clone https://github.com/salamonepablo/spc-software.git
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
```

**Web (Frontend)**
```bash
cd SPC.Web
dotnet run
# Web app available at: https://localhost:5002
```

### Running Tests

```bash
dotnet test
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers` | List all customers |
| GET | `/api/customers/{id}` | Get customer by ID |
| GET | `/api/customers/search?name=xxx` | Search customers |
| POST | `/api/customers` | Create customer |
| PUT | `/api/customers/{id}` | Update customer |
| DELETE | `/api/customers/{id}` | Soft delete customer |
| GET | `/api/products` | List all products |
| GET | `/api/vendors` | List sales representatives |

## Configuration

### Development

Configuration is stored in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SPC.db"
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

## Documentation

- [CHANGELOG](CHANGELOG.md) - Version history and changes
- [docs/](docs/) - Original Access database documentation

## Contributing

This is a proprietary project. Contact the development team for contribution guidelines.

## License

Proprietary - All rights reserved.

## Authors

- **Pablo Salamone** - *Development* - [@salamonepablo](https://github.com/salamonepablo)
