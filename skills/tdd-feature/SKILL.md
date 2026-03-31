# Skill: TDD Feature Delivery

## Purpose
Deliver new behavior using RED -> GREEN -> REFACTOR with regression safety.

## Use When
- Implementing a feature.
- Fixing a bug with reproducible behavior.

## Workflow
1. RED
   - Add failing unit/integration test that captures expected behavior.
2. GREEN
   - Implement the minimal code to pass tests.
3. REFACTOR
   - Improve readability/design while keeping tests green.
4. VERIFY
   - Run impacted tests (and broader suite if risk is high).

## Rules
- Keep code identifiers in English.
- Keep tests readable and behavior-oriented.
- Preserve backward-compatible API routes when required.

## Deliverables
- Test evidence
- Changed files summary
- Remaining risks
