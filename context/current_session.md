# Current Session Context

**Last Updated:** 2026-02-28 22:10
**Branch:** develop
**Version:** 0.1.0

---

## Session Summary

Full session: infrastructure setup, documentation, Git workflow, API testing, bug fixes, and Swagger integration.

## What Was Done Today

1. **Repository Setup**
   - Moved project from `C:\Programmes\C#\SPC` to `C:\Programmes\spc-software`
   - Connected to GitHub: https://github.com/salamonepablo/spc-software
   - Created `.gitignore` for .NET projects

2. **Documentation Created**
   - `README.md` - Project documentation
   - `CHANGELOG.md` - Version history (SemVer)
   - `CLAUDE.md` - AI guidelines (SOLID, Clean Architecture, TDD, OWASP, Git Flow)

3. **Git Flow Implemented**
   - `main` branch - production (tagged v0.1.0)
   - `develop` branch - integration (current)

4. **Context System**
   - Created `context/` folder for session recovery
   - `current_session.md` + daily backups

5. **API Testing & Fixes**
   - Ran API successfully (`dotnet run`)
   - EF Core created SQLite database with all tables
   - Seed data inserted (CondicionesIva, Rubros, UnidadesMedida, Depositos)
   - Created customer "Baterías del Norte SRL" (ID=1)
   - Created product "Batería 12V 75Ah Auto" (ID=1)
   - **Bug fixed**: Circular reference in JSON serialization (ReferenceHandler.IgnoreCycles)

6. **Swagger/OpenAPI Added**
   - Installed `Swashbuckle.AspNetCore` package
   - Swagger UI available at `/swagger`
   - OpenAPI spec at `/openapi/v1.json`

## Current State

- API running on `http://localhost:5233`
- Swagger UI at `http://localhost:5233/swagger`
- SQLite database created with seed data
- 1 customer, 1 product in database
- Blazor Web has template pages only
- No automated tests yet

## Pending / Next Steps

1. [ ] Delete old folder `C:\Programmes\C#\SPC` (was locked by process)
2. [ ] Add automated tests (xUnit) - TDD approach
3. [ ] Implement Blazor UI for Customers
4. [ ] Start first feature branch

## Important Decisions Made

| Decision | Rationale |
|----------|-----------|
| Windows Auth | Users have no login habit, use AD credentials |
| English code | Professional standard, Spanish comments OK |
| Soft delete | Never lose data, use IsActive flag |
| SQLite dev / SQL Server prod | Easy local dev, robust production |
| Git Flow | Industry standard, good for portfolio |

## Developer Context

- **Pablo Salamone**
- Background: VB6 + Access
- Learning: C# / ASP.NET Core
- Goal: Backend job in Spain (Cantabria)
- This is both a real project AND a portfolio piece

## Files to Read for Full Context

1. `CLAUDE.md` - Complete AI guidelines
2. `README.md` - Project overview
3. `CHANGELOG.md` - Version history
4. `docs/doc_DB_SPC_SI_Tables.txt` - Original Access DB structure
