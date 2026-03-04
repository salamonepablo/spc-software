# SPC - Integral Management Software

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)](https://blazor.net/)
[![Tests](https://img.shields.io/badge/Tests-39%20tests%20passing-brightgreen)]()
[![License](https://img.shields.io/badge/License-Proprietary-red)](LICENSE)

## Overview

SPC is an **ERP-style business management system** built with .NET and designed for real operational environments.

The project demonstrates the **modernization of a legacy VB6 + Microsoft Access system into a modern .NET architecture**, using ASP.NET Core, Blazor and Entity Framework Core.

The system manages real business processes such as **customers, invoicing, stock control, delivery management and financial accounts**, following modern backend and architectural practices.

---

## Why this project exists

This project was created to **modernize a real business management system originally developed in VB6** and used in operational environments.

The goal is to migrate the legacy architecture to a modern backend while preserving the existing business logic and improving:

- maintainability
- modularity
- scalability
- testing capabilities

---

## Key Capabilities

• Clean Architecture implementation  
• Minimal APIs backend design  
• Entity Framework Core data layer  
• Integration and unit testing with xUnit  
• Legacy system modernization strategy  
• Modular business logic and domain entities  

---

## Features

- **Customer Management**  
  Full CRUD with soft delete and multiple addresses.

- **Product Catalog**  
  Stock control across multiple warehouses.

- **Invoicing**  
  Electronic invoicing with ARCA integration (Argentina).

- **Delivery Notes**  
  Shipping management.

- **Quotes**  
  Budget generation.

- **Credit/Debit Notes**  
  Adjustments and corrections.

- **Current Accounts**  
  Customer balance tracking.

- **IIBB Withholdings**  
  ARBA integration (Buenos Aires province).

- **License System**  
  Modular feature licensing (Base / Premium / Enterprise).

---

## Tech Stack

| Layer | Technology |
|------|------------|
| Frontend | Blazor Server |
| Backend | ASP.NET Core 10 (Minimal APIs) |
| Database | SQLite (dev) / SQL Server (prod) |
| ORM | Entity Framework Core 10 |
| Auth | Windows Authentication |
| Testing | xUnit + FluentAssertions |

---

## Architecture

The project follows **Clean Architecture principles** with clear separation of concerns.

```

┌─────────────────────────────────────────────────────────────┐
│                      PRESENTATION                           │
│              (Blazor Pages, API Endpoints)                  │
├─────────────────────────────────────────────────────────────┤
│                      APPLICATION                            │
│                (Services, DTOs / Contracts)                 │
├─────────────────────────────────────────────────────────────┤
│                        DOMAIN                               │
│              (Entities in SPC.Shared)                       │
├─────────────────────────────────────────────────────────────┤
│                    INFRASTRUCTURE                           │
│              (EF Core, SPCDbContext)                        │
└─────────────────────────────────────────────────────────────┘

```

---

## Project Structure

```

spc-software/
├── SPC.API/                    # REST API backend
│   ├── Contracts/              # DTOs (Request/Response)
│   ├── Data/                   # Entity Framework context
│   ├── Endpoints/              # Minimal API endpoint modules
│   ├── Services/               # Business logic layer
│   └── Program.cs
│
├── SPC.Shared/                 # Shared domain models
│
├── SPC.Web/                    # Blazor Server frontend
│
├── SPC.Tests/                  # Test suite
│   ├── Integration/
│   └── Unit/
│
└── docs/                       # Documentation
├── adr/                    # Architecture Decision Records
└── *.md                    # Technical documentation

````

---

## Getting Started

### Prerequisites

- .NET 10 SDK  
- Visual Studio 2022 or VS Code  
- SQL Server (for production)

---

### Installation

```bash
git clone https://github.com/salamonepablo/spc-software.git
cd spc-software

dotnet restore
dotnet build
````

---

### Running the Application

**API (Backend)**

```bash
cd SPC.API
dotnet run
```

API available at:

```
https://localhost:5001
```

Swagger:

```
https://localhost:5001/swagger
```

---

**Web (Frontend)**

```bash
cd SPC.Web
dotnet run
```

Application available at:

```
https://localhost:5002
```

---

### Running Tests

Run all tests:

```bash
dotnet test
```

Run with detailed output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## API Endpoints

### Customers

| Method | Endpoint               | Description               |
| ------ | ---------------------- | ------------------------- |
| GET    | `/api/clientes`        | List all active customers |
| GET    | `/api/clientes/{id}`   | Get customer by ID        |
| GET    | `/api/clientes/buscar` | Search customers          |
| POST   | `/api/clientes`        | Create customer           |
| PUT    | `/api/clientes/{id}`   | Update customer           |
| DELETE | `/api/clientes/{id}`   | Soft delete               |

---

### Products

| Method | Endpoint                | Description     |
| ------ | ----------------------- | --------------- |
| GET    | `/api/productos`        | List products   |
| GET    | `/api/productos/{id}`   | Get product     |
| GET    | `/api/productos/buscar` | Search products |
| POST   | `/api/productos`        | Create product  |
| PUT    | `/api/productos/{id}`   | Update product  |

---

## Architecture Decisions

Key architectural decisions are documented in **docs/adr**.

Examples:

* Minimal APIs vs Controllers
* Clean Architecture approach
* Soft Delete strategy
* Database strategy (SQLite dev / SQL Server prod)
* Modular licensing system

---

## Future Work

Planned improvements:

* Blazor UI modules
* Authentication and role management
* Multi-company support
* REST API expansion
* Docker deployment

---

## License

Proprietary – All rights reserved.

---

## Author

**Pablo Salamone**
Software Developer
GitHub: [https://github.com/salamonepablo](https://github.com/salamonepablo)

````

---

## Screenshots
````

con 2-3 imágenes.

---

