# SPC Orchestrator - AGENTS.md

This file is the primary orchestration guide for AI assistants working in this repository.

## Mission

Route each request to the most appropriate execution path while preserving:
- Clean Architecture
- SOLID principles
- TDD workflow
- English-only code identifiers (classes, methods, variables, DTOs, entities, table names, column names, constants)

## Mandatory Project Rules

### 1) Clean Architecture
- Dependency direction must remain inward:
  - Presentation -> Application -> Domain -> Infrastructure
- Domain layer must not depend on frameworks or infrastructure details.
- Business rules belong in services/use cases, not in endpoints or UI.

### 2) SOLID
- Enforce SRP, OCP, LSP, ISP, DIP for all medium/large changes.
- Prefer interfaces and small, testable units.

### 3) TDD
- Follow RED -> GREEN -> REFACTOR whenever feasible.
- For each medium/large feature or bug fix:
  - Add or update tests first (or in the same change set if tooling constraints apply).
  - Ensure tests capture expected behavior and regressions.

### 4) English Code Convention
- All code artifacts must use English names.
- Spanish is allowed only for API compatibility routes and user-facing legacy labels when required.

### 5) Response Language
- The assistant MUST respond to the user in **Spanish** (Rioplatense/Argentine informal style).
- Technical terms (Clean Architecture, SOLID, TDD, DIP, SRP, etc.) may remain in English.
- Code identifiers, commit messages, and documentation files remain in English (rule 4 still applies).
- This rule applies to all conversational output: explanations, plans, summaries, routing decisions, and session logs directed at the user.

## Orchestration Flow

For every non-trivial task, execute in this order:
1. Context Sync
2. Route Selection
3. Safe Implementation
4. Verification
5. Session Logging

### 1) Context Sync (always first)
Read:
- context/current_session.md
- latest context/session_*.md files (most recent first)
- README.md and CHANGELOG.md when scope is broad

If session docs are outdated, refresh them before ending the task.

### 2) Route Selection Matrix
- Codebase discovery, dependency tracing, architecture mapping:
  - Use Explore agent.
- Agent/instructions customization tasks:
  - Use skill agent-customization.
- GitHub issue/PR/notification summarization:
  - Use skill summarize-github-issue-pr-notification.
- GitHub query building:
  - Use skill form-github-search-query.
- GitHub search rendering:
  - Use skill show-github-search-result.
- Issue solution proposal:
  - Use skill suggest-fix-issue.

If the request is ambiguous, ask one short clarification question.

### 3) Safe Implementation Rules
- Keep changes minimal and scoped.
- Do not break public contracts unless explicitly requested.
- Preserve backward compatibility for Spanish API routes.
- Never use destructive git commands unless the user explicitly requests it.

### 4) Verification Rules
Before completion, run relevant checks when available:
- Build affected projects.
- Run impacted tests (or full test suite when needed).
- Confirm no architecture boundary violations.
- Apply docs/pr-checklist.md before merge.

### 5) Session Logging (mandatory for important changes)
For each medium/high-impact change, append:
- context/current_session.md
- context/session_2026-03.md (monthly log; continue with same pattern session_YYYY-MM.md)

Log template:
- Date
- Scope
- Files changed
- Architectural impact
- Tests added/updated
- Validation results
- Follow-ups

Template file:
- context/session_entry_template.md

## Definition of Important Change
A change is medium/high-impact if it modifies one or more of:
- Domain models, contracts, business rules
- Persistence strategy or EF mappings
- API behavior/endpoints
- Cross-cutting architecture, dependency direction, or security
- Test strategy or quality gates

## Default Output Format
When answering users:
1. Routing decision
2. Actions performed
3. Result summary
4. Validation evidence
5. Next options

## Repository Skills (local)
- skills/session-sync/SKILL.md
- skills/architecture-guard/SKILL.md
- skills/tdd-feature/SKILL.md

Use these local skills as procedural checklists whenever relevant.

## Additional Guides
- docs/pr-checklist.md
- context/session_entry_template.md
- AGENTS_BILINGUAL.md
