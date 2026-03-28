# SPC - AI Assistant Context

This project uses **AGENTS.md** as the primary context file for AI assistants.

Please read **AGENTS.md** for complete project context, architecture, and guidelines.

---

## Quick Links

- [AGENTS.md](./AGENTS.md) - Complete project documentation
- [README.md](./README.md) - Project overview
- [CHANGELOG.md](./CHANGELOG.md) - Version history
- [context/current_session.md](./context/current_session.md) - Current session state

---

## Development Rules (MANDATORY)

### 1. Clean Architecture

```
PRESENTATION → APPLICATION → DOMAIN → INFRASTRUCTURE
```

- Dependencies point INWARD only
- Domain has NO external dependencies
- Services contain business logic, not endpoints

### 2. SOLID Principles

| Principle | Rule |
|-----------|------|
| **S** | One class = one responsibility |
| **O** | Extend via interfaces, don't modify |
| **L** | Subtypes must be substitutable |
| **I** | Small, specific interfaces |
| **D** | Depend on abstractions |

### 3. Test-Driven Development (TDD)

```
RED → GREEN → REFACTOR → Repeat
```

- Write tests BEFORE implementation
- Each feature starts with a failing test
- Tests document expected behavior

### 4. Code Quality Standards

| Metric | Minimum | Target |
|--------|---------|--------|
| **Line Coverage (Core)** | 80% | 90% |
| **Line Coverage (Global)** | 60% | 80% |
| **Branch Coverage** | 50% | 70% |
| **Tests Passing** | 100% | 100% |

**Core Services (coverage required):**
- `SPC.API.Services.PricingService`
- `SPC.API.Services.InvoicesService`
- `SPC.API.Services.QuotesService`

**Quality Gates:**
1. All tests must pass before commit
2. New features require tests (TDD preferred)
3. Core coverage must stay above 80%

---

## Current Phase

**Phase 3: Operations (In Progress)**
- [x] Full CRUD Customers
- [x] Full CRUD Products
- [x] Quotes CRUD
- [x] Invoice CRUD with business rules
- [x] Credit/Debit Notes CRUD
- [ ] Blazor UI for Products
- [ ] Blazor UI for Invoicing
- [ ] Stock movements
- [ ] Delivery Notes

---

## Useful Commands

```bash
# Run API
cd SPC.API && dotnet run

# Run Web
cd SPC.Web && dotnet run

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Generate coverage report (core)
reportgenerator -reports:"SPC.Tests/TestResults/**/coverage.cobertura.xml" \
  -targetdir:"SPC.Tests/TestResults/coverage-report-core" \
  -reporttypes:Html \
  -classfilters:"+SPC.API.Services.PricingService;+SPC.API.Services.InvoicesService;+SPC.API.Services.QuotesService"
```
