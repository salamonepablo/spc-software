# Skill: Session Sync

## Purpose
Keep assistant context aligned across working sessions and ensure important changes are documented.

## Use When
- Starting a new coding task.
- Finishing medium/high-impact work.
- User asks for traceability/history.

## Inputs
- Task scope and changed files.
- Validation results (build/tests).
- Architectural impact.

## Procedure
1. Read context/current_session.md.
2. Read the most recent context/session_*.md files.
3. Detect stale information and update notes.
4. Append a concise entry to:
   - context/current_session.md
   - context/session_YYYY-MM.md

## Entry Template
- Date:
- Scope:
- Files changed:
- Architectural impact:
- Tests added/updated:
- Validation:
- Next actions:

## Do Not
- Duplicate entire logs.
- Omit test/validation status for important changes.
