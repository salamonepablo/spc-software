# SPC Software Project Context

## Purpose

SPC Software is an ERP-style business management system built with .NET and C#. It modernizes a legacy VB6 + Microsoft Access operational system while preserving business behavior and improving maintainability, modularity, scalability, and testability.

## Technology Stack

- Language: C#
- Platform: .NET 10
- Backend: ASP.NET Core Minimal APIs
- Frontend: Blazor Server
- Persistence: SQLite with Entity Framework Core 10
- Tests: xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory
- Solution: `SPC.slnx`

Normal project work does not require Node.js/npm or Python tooling.

## Repository Layout

- `SPC.API/`: backend API, DTO contracts, endpoints, services, EF Core DbContext, migrations, composition root.
- `SPC.Web/`: Blazor Server UI and API service client.
- `SPC.Shared/`: shared domain models and licensing types.
- `SPC.Tests/`: unit and integration tests.
- `SPC.Migration/`: C# migration tooling for legacy data modernization.
- `docs/`: architecture and process documentation.
- `context/`: session continuity logs.

## Architecture Rules

- Preserve Clean Architecture dependency direction: Presentation -> Application -> Domain -> Infrastructure.
- Business rules belong in services/use cases, not in endpoint handlers or UI components.
- Domain/shared models must not depend on infrastructure concerns.
- Keep changes minimal, scoped, and backward-compatible unless explicitly requested otherwise.
- Preserve Spanish API routes when required for legacy compatibility.
- All code identifiers, table names, columns, DTOs, classes, methods, constants, and filenames should use English.

## TDD and Validation

Strict TDD is expected for medium/high-impact code changes.

Default commands:

```bash
dotnet build SPC.slnx -c Release
dotnet test SPC.Tests/SPC.Tests.csproj -c Release
```

Coverage command when needed:

```bash
dotnet test SPC.Tests/SPC.Tests.csproj -c Release --collect:"XPlat Code Coverage"
```

## Current Functional Areas

- Customers
- Products and stock
- Invoices
- Quotes
- Credit notes
- Debit notes
- Delivery notes
- Payments
- Current accounts
- IIBB withholdings
- Licensing
- Legacy data migration

## Current Baseline Notes

- Release documentation marks version 1.0.0 as the Current Account module completion milestone.
- Session context reports 299 passing tests as of 2026-06-02.
- Current account navigation metadata uses resolved document type short codes, so historical movements stored as `DocumentType.Other` can still navigate when inference resolves them to an openable type.

## SDD Defaults

- Execution mode: interactive
- Artifact store: OpenSpec files in this repository
- PR strategy: single PR by default
- Review budget: 400 changed lines

## Memory

Engram is available on this machine and has existing `spc-software` memories from prior Claude Code/OpenCode sessions.

Use Engram for:

- session restart context;
- important decisions and discoveries;
- completed SDD phase summaries when useful;
- project continuity across Pi sessions.

OpenSpec remains the primary local artifact store for SDD files unless the user explicitly switches to hybrid/Engram artifact mode.
