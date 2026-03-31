# Architecture Decision Records (ADR)

This directory contains Architecture Decision Records for the SPC project.

## What is an ADR?

An ADR is a document that captures an important architectural decision made along with its context and consequences.

## ADR Index

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [001](001-minimal-apis.md) | Use Minimal APIs over Controllers | Accepted | 2024 |
| [002](002-clean-architecture.md) | Clean Architecture with Services and DTOs | Accepted | 2024 |
| [003](003-soft-delete.md) | Soft Delete for Business Entities | Accepted | 2024 |
| [004](004-database-strategy.md) | SQLite for Dev, SQL Server for Prod | Accepted | 2024 |
| [005](005-license-system.md) | Modular License System | Accepted | 2024 |

## ADR Template

When creating a new ADR, use this template:

```markdown
# ADR-XXX: Title

## Status

**Proposed** | **Accepted** | **Deprecated** | **Superseded by ADR-XXX**

## Context

What is the issue that we're seeing that is motivating this decision?

## Decision

What is the change that we're proposing and/or doing?

## Rationale

Why is this decision being made? What alternatives were considered?

## Consequences

What becomes easier or more difficult because of this change?

## References

- Links to relevant documentation
```

## Status Definitions

- **Proposed** - Under discussion
- **Accepted** - Decision made and implemented
- **Deprecated** - No longer relevant
- **Superseded** - Replaced by another ADR

## References

- [ADR GitHub Organization](https://adr.github.io/)
- [Michael Nygard's ADR Article](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
