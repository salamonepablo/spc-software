# October 2026 — SPC 2.0

## Remitos R5 closure — 2026-10-02

- **Scope:** Closed functional validation for the existing R1–R4 Remitos feature against positively identified local `SPC TEST`; documentation only in this closure update. No application changes.
- **Evidence:** SQL retry/idempotency, duplicate/manual conflict, branch isolation, concurrent numbering, injected rollback, and standalone stock-on were exercised. Headed UI verified list/detail, saved-data 3-copy PDF, and existing linked-note Payments handoff without amount or submit. Full evidence and record IDs are in `odd/tasks/remitos-comunes.md`.
- **Validation:** Focused tests 27 passed/5 SQL-skipped; full suite 333 passed/5 skipped/2 pre-existing LocalDB/WSL failures. Incremental API build 0 errors/2 NU1903 warnings; Web build 0 errors/0 warnings; `git diff --check` passed. Initial SQL run was 4 pass/1 harness failure (password lost from opened `ConnectionString`); after reusing original options, isolated rollback passed 1/1.
- **Environment/follow-up:** Importer baseline remains 203 errors on clean HEAD vs 199 in working tree; not rerun or repaired. Concurrent automatic-number submission rejection is zero-effect and no automatic retry guarantee is claimed. Consider retry behavior only as separately authorized follow-up. API/Web/browser were left running; no production/VB6, cleanup, payment, commit, or push.
- **Next:** Parent owns explicit-path Remitos commit, safe main integration/publication, and deletion of merged local branches; no new branches. Do not claim publication/hash before that work.

---

## Scope split and continuation — 2026-10-02

This is the active SPC 2.0 checkout: /home/pablo/Programmes/spc-software, feature/remitos-comunes, HEAD 2e3833b. Legacy VB6/Access reference and maintenance live under C:\Trabajos Activos (SPC-Core, SPC-Minimal, SPC-Retail). Keep Legacy session logs separate. Generate/review .NET Pi prompts here, under context/Prompts when saved. Read context/SPC_SCOPE_AND_CONTINUITY.md for the full inventory.

Engram #323 confirms the last recorded TEST warehouse finding. User-provided Pi handoff reports a later temporary association of warehouse 1 to Gabriel Peralta (salesperson 3) and 50 fictitious units of product code 110. These later values were not measured today; stock adjustment from the UI remains pending. Verify explicit SPC TEST identity and current quantities before resuming R5. Retry/idempotency, concurrency/duplicate and rollback validation remain open. No source/data changes, builds/tests, commit or push in this organization task.
