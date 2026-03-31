# Skill: Architecture Guard

## Purpose
Protect Clean Architecture boundaries and SOLID compliance during code changes.

## Use When
- Modifying services, endpoints, DbContext, shared models, contracts.
- Reviewing medium/large PRs.

## Checklist
1. Dependency direction remains inward:
   - Presentation -> Application -> Domain -> Infrastructure
2. Domain has no infrastructure/framework dependencies.
3. Business rules stay in services/use cases (not endpoint handlers/UI).
4. Interfaces are used where extension/substitution is expected.
5. Classes keep single responsibility.
6. No accidental coupling to concrete implementations where abstractions are expected.

## Output
- Violations found (with file references)
- Required fixes
- Validation summary
