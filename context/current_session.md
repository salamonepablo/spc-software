# Current Session Context

**Last Updated:** 2026-02-28 18:30
**Branch:** develop
**Version:** 0.1.0

---

## Session Summary

Setting up project infrastructure, documentation, and Git workflow.

## What Was Done Today

1. **Repository Setup**
   - Moved project from `C:\Programmes\C#\SPC` to `C:\Programmes\spc-software`
   - Connected to GitHub: https://github.com/salamonepablo/spc-software
   - Created `.gitignore` for .NET projects

2. **Documentation Created**
   - `README.md` - Project documentation
   - `CHANGELOG.md` - Version history (SemVer)
   - `CLAUDE.md` - AI guidelines with:
     - Architecture diagrams
     - SOLID principles
     - Clean Architecture
     - TDD approach
     - Security (OWASP Top 10)
     - Windows Authentication strategy
     - Git Flow workflow
     - Conventional Commits

3. **Git Flow Implemented**
   - `main` branch - production (tagged v0.1.0)
   - `develop` branch - integration (current)
   - Documented feature/release/hotfix workflow

4. **Context System**
   - Created `context/` folder for session recovery
   - This file + daily backups

## Current State

- Project compiles and runs
- API has basic CRUD for Customers
- Blazor Web has template pages only
- Using SQLite for development
- No tests yet

## Pending / Next Steps

1. [ ] Delete old folder `C:\Programmes\C#\SPC` (was locked)
2. [ ] Test that API runs correctly
3. [ ] Start first feature branch
4. [ ] Implement Blazor UI for Customers

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
