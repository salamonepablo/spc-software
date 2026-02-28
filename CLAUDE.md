# SPC - Integral Management Software | ERP

## Migration Plan: VB6 to C# ASP.NET Core + Blazor

### Current System (VB6 + Access)
- VB6 + Access database
- Electronic invoicing AFIP (FEAFIP)
- Customers, Products, Stock
- Invoices, Credit Notes, Debit Notes, Delivery Notes, Quotes
- Current Accounts
- IIBB Withholdings (ARBA)

---

## Technology Stack

```
┌─────────────────────────────────────────────────────────────────┐
│                      NEW ARCHITECTURE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   FRONTEND           BACKEND              DATABASE              │
│   ─────────          ───────              ────────              │
│   Blazor Server      ASP.NET Core API     SQL Server            │
│   (C# in browser)    (REST + Services)    (or PostgreSQL)       │
│                                                                 │
│         │                  │                    │               │
│         └──────────────────┴────────────────────┘               │
│                          │                                      │
│                    All in C#                                    │
│                    Single language                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

- **Backend**: ASP.NET Core 10 (Minimal APIs)
- **Frontend**: Blazor Server
- **Database**: SQLite (development) / SQL Server (production)
- **ORM**: Entity Framework Core 10

---

## Project Structure

```
spc-software/
│
├── SPC.API/                         ← Backend REST API
│   ├── Controllers/
│   │   ├── CustomersController.cs
│   │   ├── ProductsController.cs
│   │   ├── InvoicesController.cs
│   │   ├── DeliveryNotesController.cs
│   │   ├── StockController.cs
│   │   └── AFIPController.cs
│   │
│   ├── Services/                    ← Business logic
│   │   ├── InvoicingService.cs
│   │   ├── StockService.cs
│   │   ├── CurrentAccountService.cs
│   │   └── AFIP/
│   │       ├── ElectronicInvoiceService.cs
│   │       └── ARBAPadronService.cs
│   │
│   ├── Data/
│   │   └── SPCDbContext.cs          ← Entity Framework
│   │
│   └── Program.cs
│
├── SPC.Web/                         ← Blazor Frontend
│   ├── Pages/
│   │   ├── Index.razor
│   │   ├── Customers/
│   │   │   ├── CustomerList.razor
│   │   │   └── CustomerEdit.razor
│   │   ├── Products/
│   │   ├── Invoicing/
│   │   │   ├── NewInvoice.razor
│   │   │   ├── InvoiceSearch.razor
│   │   │   └── CreditNote.razor
│   │   ├── Stock/
│   │   └── Reports/
│   │
│   ├── Components/                  ← Reusable components
│   │   ├── CustomerSearch.razor
│   │   ├── ProductGrid.razor
│   │   └── WarehouseSelector.razor
│   │
│   └── Services/
│       └── ApiService.cs            ← API consumer
│
├── SPC.Shared/                      ← Shared models
│   └── Models/
│       ├── Customer.cs
│       ├── Product.cs
│       ├── Invoice.cs
│       ├── InvoiceDetail.cs
│       ├── CreditNote.cs
│       ├── DeliveryNote.cs
│       ├── StockMovement.cs
│       └── DTOs/
│           ├── CustomerDto.cs
│           └── InvoiceCreateDto.cs
│
├── SPC.Desktop/                     ← Optional: Desktop app
│   └── (MAUI or WPF)
│
└── docs/                            ← Original Access DB docs
    ├── doc_DB_SPC_SI_Tables.txt
    └── doc_DB_SPC_SI_Queries.txt
```

---

## VB6 → C# Mapping

### Forms → Blazor Pages

| VB6 (.frm) | Blazor (.razor) |
|------------|-----------------|
| MenuPrincipal.frm | MainLayout.razor |
| Clientes.frm | Customers/CustomerList.razor |
| FormFactura.frm | Invoicing/NewInvoice.razor |
| FormBuscarFactura.frm | Invoicing/InvoiceSearch.razor |
| FormNotaCredito.frm | Invoicing/CreditNote.razor |
| FormRemito.frm | DeliveryNotes/NewDeliveryNote.razor |
| Articulos.frm | Products/ProductList.razor |
| FormConsultarStock.frm | Stock/StockQuery.razor |

### Modules .bas → Services

| VB6 (.bas) | C# Service |
|------------|------------|
| VariablesPublicas.bas | Dependency Injection |
| Declaraciones.bas | SPCDbContext.cs |
| AFIP Functions | AFIPService.cs |
| Stock Functions | StockService.cs |

### Access Tables → C# Entities

```
VB6 - Access          →    C# Entity
─────────────────────────────────────
tClientes             →    Customer
tProductos            →    Product  
tFacturaC             →    Invoice
tFacturaD             →    InvoiceDetail
tNotaCreditoC         →    CreditNote
tStock                →    Stock
tCtaCte               →    CurrentAccountMovement
```

---

## Original Database (Access)

Documentation in `/docs/`:
- `doc_DB_SPC_SI_Tables.txt` - Table structure
- `doc_DB_SPC_SI_Queries.txt` - Existing queries

### Main Tables

| Category | Tables |
|----------|--------|
| Customers | Clientes, DomiciliosClientes, CtaCte |
| Products | Productos, Stock, Rubros, UnidadesMedida, Depositos |
| Invoicing | FacturaC/D, PresupuestoC/D, RemitoC/D |
| Notes | NotaCreditoC/D, NotaDebitoC/D |
| Payments | PagoC/D, RecibosC/D, CodPago |
| Stock | MovIntStockC/D, MovimientosCtaCte |
| Auxiliary | Paises, Provincias, Localidades, CondicionIva |

---

## Development Principles

### SOLID Principles

| Principle | Description | Example |
|-----------|-------------|---------|
| **S** - Single Responsibility | A class should have only one reason to change | `InvoiceService` only handles invoice logic, not PDF generation |
| **O** - Open/Closed | Open for extension, closed for modification | Use interfaces to add new payment methods without changing existing code |
| **L** - Liskov Substitution | Derived classes must be substitutable for their base classes | Any `IPaymentProcessor` implementation works wherever the interface is expected |
| **I** - Interface Segregation | Many specific interfaces are better than one general interface | `IInvoiceReader` and `IInvoiceWriter` instead of one big `IInvoiceService` |
| **D** - Dependency Inversion | Depend on abstractions, not concrete implementations | Controllers receive `ICustomerService`, not `CustomerService` directly |

### Clean Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      PRESENTATION                           │
│              (Blazor Pages, API Controllers)                │
├─────────────────────────────────────────────────────────────┤
│                      APPLICATION                            │
│                (Services, Use Cases, DTOs)                  │
├─────────────────────────────────────────────────────────────┤
│                        DOMAIN                               │
│              (Entities, Business Rules)                     │
├─────────────────────────────────────────────────────────────┤
│                    INFRASTRUCTURE                           │
│         (EF Core, External APIs, File System)               │
└─────────────────────────────────────────────────────────────┘

Dependencies point INWARD - outer layers depend on inner layers
Domain has NO external dependencies
```

### Test-Driven Development (TDD)

```
┌─────────────────────────────────────────┐
│            TDD Cycle                    │
│                                         │
│    1. RED    → Write failing test       │
│    2. GREEN  → Write minimal code       │
│    3. REFACTOR → Improve code           │
│                                         │
│         ↺ Repeat                        │
└─────────────────────────────────────────┘
```

- Write tests BEFORE implementation
- Each feature starts with a test
- Tests document expected behavior
- Refactor with confidence

---

## Security

### Security First Approach

> **Current VB6 system has ZERO security** - no login, no user management.
> New system must be secure from the ground up.

### Authentication Strategy: Windows Authentication

Since users are accustomed to no login, we'll use **Windows Authentication** (Integrated Security):
- Users authenticate with their Windows/AD credentials
- No additional passwords to remember
- Seamless experience for existing users
- IT department manages user access

```csharp
// Program.cs - Windows Authentication setup
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("DOMAIN\\SPC-Admins"));
    options.AddPolicy("SalesTeam", policy => 
        policy.RequireRole("DOMAIN\\SPC-Sales"));
});
```

### OWASP Top 10 - Security Checklist

| # | Vulnerability | Mitigation in SPC |
|---|---------------|-------------------|
| 1 | **Injection** | Parameterized queries via EF Core, never raw SQL concatenation |
| 2 | **Broken Authentication** | Windows Auth + session management |
| 3 | **Sensitive Data Exposure** | HTTPS only, encrypt sensitive fields, secure connection strings |
| 4 | **XML External Entities (XXE)** | Disable DTD processing in XML parsers |
| 5 | **Broken Access Control** | Role-based authorization on every endpoint |
| 6 | **Security Misconfiguration** | Secure defaults, remove debug info in production |
| 7 | **Cross-Site Scripting (XSS)** | Blazor auto-encodes output, validate all inputs |
| 8 | **Insecure Deserialization** | Use System.Text.Json with safe settings |
| 9 | **Known Vulnerabilities** | Keep NuGet packages updated, use `dotnet list package --vulnerable` |
| 10 | **Insufficient Logging** | Structured logging with Serilog, audit trail for sensitive operations |

### Security Implementation

```csharp
// Example: Secure endpoint with authorization
[Authorize(Policy = "SalesTeam")]
app.MapPost("/api/invoices", async (Invoice invoice, IInvoiceService service) =>
{
    // User is authenticated and authorized
    return await service.CreateAsync(invoice);
});

// Example: Audit logging
public class AuditService
{
    public async Task LogAsync(string action, string entity, int entityId, string userId)
    {
        await _db.AuditLogs.AddAsync(new AuditLog
        {
            Action = action,
            Entity = entity,
            EntityId = entityId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetClientIp()
        });
    }
}
```

### Data Protection

- Connection strings in User Secrets (dev) or Azure Key Vault (prod)
- Never commit secrets to git
- Encrypt sensitive customer data (CUIT, financial info)
- Regular security audits

---

## Code Conventions

- All code in English: variable names, entities, properties, and comments
- Spanish comments allowed when clarifying concepts or VB6 transition logic
- References to original VB6 code valid for explaining logic
- New code must follow modern C# and .NET conventions
- Soft delete (Active/IsActive field) instead of physical deletion
- Header/Detail pattern for documents (Invoice/InvoiceDetail)

---

## Code Examples

### VB6 (Access) - Finding a Customer
```vb
Set tClientes = BaseSPC.OpenRecordset("Clientes", dbOpenTable)
tClientes.Index = "PrimaryKey"
tClientes.Seek "=", CodCliente
If Not tClientes.NoMatch Then
    nombreCliente = tClientes!RazonSocial
End If
```

### C# (Entity Framework) - Same Operation
```csharp
public class Customer
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = "";
    public string TaxId { get; set; } = "";  // CUIT
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public int TaxConditionId { get; set; }
    public TaxCondition? TaxCondition { get; set; }
    public int? SalesRepId { get; set; }
    public SalesRep? SalesRep { get; set; }
    
    // Navigation
    public List<Invoice> Invoices { get; set; } = new();
    public List<DeliveryNote> DeliveryNotes { get; set; } = new();
}

// Usage
var customer = await db.Customers.FindAsync(customerId);
var businessName = customer?.BusinessName;
```

### VB6 - Electronic Invoice (AFIP)
```vb
Function FacturaElectronicaSPC(...) As Boolean
    Set wsfev1 = New FEAFIPLib.wsfev1
    wsfev1.CUIT = 30708432543#
    wsfev1.URL = URLWSW
    
    If wsfev1.login("quilplac.crt", "quilplac.key", URLWSAA) Then
        wsfev1.AgregaFactura ...
        wsfev1.AgregaIVA ...
        If wsfev1.Autorizar(PtoVta, TipoComp) Then
            ' Success
        End If
    End If
End Function
```

### C# - Electronic Invoice Service
```csharp
public class AFIPService
{
    private readonly IConfiguration _config;
    
    public AFIPService(IConfiguration config)
    {
        _config = config;
    }
    
    public async Task<AFIPResult> AuthorizeInvoiceAsync(AFIPInvoice invoice)
    {
        var wsfe = new WSFEClient(
            cuit: _config["AFIP:CUIT"],
            certificate: _config["AFIP:Certificate"],
            production: true
        );
        
        var result = await wsfe.AuthorizeAsync(new
        {
            PointOfSale = invoice.PointOfSale,
            VoucherType = invoice.VoucherType,
            // ...
        });
        
        return new AFIPResult
        {
            CAE = result.CAE,
            ExpirationDate = result.ExpirationDate,
            Approved = result.Result == "A"
        };
    }
}
```

---

## Migration Phases

### PHASE 1: Infrastructure (1-2 months)
```
[x] Create .NET solution
[ ] Configure SQL Server
[ ] Migrate Access schema → SQL Server
[ ] Create Entity Framework entities
[ ] Basic API working
[ ] Migrate historical data
```

### PHASE 2: Queries (2 months)
```
[ ] GET endpoints (read-only)
[ ] Basic Blazor frontend
[ ] Lists: Customers, Products, Stock
[ ] Document searches
[ ] Basic reports
[ ] VB6 still operative for CRUD
```

### PHASE 3: Operations (3 months)
```
[ ] Full CRUD Customers
[ ] Full CRUD Products
[ ] Stock movements
[ ] Quotes
[ ] Delivery Notes
[ ] Test in parallel with VB6
```

### PHASE 4: Invoicing (2-3 months)
```
[ ] Integrate AFIP in C#
[ ] Invoice types A and B
[ ] Credit Notes
[ ] Debit Notes
[ ] QR and PDF generation
[ ] IIBB Withholdings
[ ] Exhaustive testing
```

### PHASE 5: Finalization (1-2 months)
```
[ ] Current Accounts
[ ] Payments and Receipts
[ ] Complete reports
[ ] User training
[ ] Retire VB6
```

---

## Useful Commands

```bash
# Run API
cd SPC.API && dotnet run

# Run Web
cd SPC.Web && dotnet run

# Restore packages
dotnet restore

# Build entire solution
dotnet build

# Create migration
cd SPC.API && dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

---

## Existing API Endpoints

- `GET /api/clientes` - List customers
- `GET /api/clientes/{id}` - Get customer
- `GET /api/clientes/buscar?nombre=xxx` - Search
- `POST /api/clientes` - Create
- `PUT /api/clientes/{id}` - Update
- `DELETE /api/clientes/{id}` - Soft delete
- `GET /api/productos`, `GET /api/vendedores`, etc.

---

## New System Advantages

```
✓ Web access (from anywhere)
✓ Multiple simultaneous users
✓ Centralized and secure database
✓ Automatic backups
✓ No Access dependency
✓ Maintainable and testable code
✓ Future mobile app possibility
✓ Better performance
✓ Easier updates
```

---

## Useful Resources

- Blazor docs: https://learn.microsoft.com/aspnet/core/blazor
- EF Core docs: https://learn.microsoft.com/ef/core
- AFIP .NET: Search "Afip SDK .NET" or use interop with FEAFIP
- Access to SQL Server migration: SQL Server Migration Assistant (SSMA)

---

## Pending Tasks

- [ ] Complete models: Quote, CreditNote, Payments, CurrentAccount
- [ ] Blazor UI for Customer and Product management
- [ ] Invoice endpoints with stock logic
- [ ] Authentication and authorization
- [ ] Data migration from Access
