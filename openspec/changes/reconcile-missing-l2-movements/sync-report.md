# Sync Report: reconcile-missing-l2-movements

## Status

**synced**

## Executive summary

The final file-backed `current-account-remediation` specification has been synchronized into the canonical OpenSpec location. The change remains active; it was not archived. The authoritative remediation contract covers exactly **6 movements across 3 customers**; historical 9/4 material is superseded.

## Domains and canonical files updated

| Domain | Change spec | Canonical spec | Result |
| --- | --- | --- | --- |
| `current-account-remediation` | `openspec/changes/reconcile-missing-l2-movements/specs/current-account-remediation/spec.md` | `openspec/specs/current-account-remediation/spec.md` | Created from the change specification |

## Requirement changes

The canonical domain specification did not previously exist, so the complete change specification was created as the canonical baseline.

- **ADDED:** Repository-safe operational artifacts
- **ADDED:** Deterministic 6/3 manifest qualification
- **ADDED:** Atomic bounded correction
- **ADDED:** Affected-account balance scope
- **ADDED:** External validation and operational gates
- **MODIFIED:** none
- **REMOVED:** none

No `RENAMED Requirements` delta was present. No destructive removal or large modification was performed; no special destructive-sync approval was required.

## Collision and guardrail findings

- Active same-domain collisions: none found.
- Legacy flat change spec: none found; the authoritative domain spec exists under `specs/current-account-remediation/spec.md`.
- Canonical target is inside the authoritative workspace: confirmed.
- `openspec/config.yaml` contains no `rules.sync` override.
- Existing dirty-tree changes were preserved; this phase wrote only the canonical spec and this sync report.

## Verification and status findings

- **Native status (authoritative, supplied by orchestrator):** `apply: all_done`; `verify: all_done`; `sync: ready`; `archive: blocked`.
- **Artifact store:** `both`; filesystem sync is applicable and this report is also synchronized to Engram.
- **Action context:** no restrictive workspace-planning context was supplied; the requested workspace is `/home/pablo/Programmes/spc-software`, and all edited paths are within it.
- `verify-report.md` is present and clearly reports PASS for the package suites. Its two .NET failures are documented Linux/LocalDB environment-only integration failures, unrelated to this remediation package.
- The three unchecked tasks are explicitly future PR3 operational-runner work and remain out of scope; no runner work was implemented.

## Checks performed

- Confirmed the change has one domain spec and no legacy flat `spec.md`.
- Confirmed no other active change declares the same domain spec.
- Confirmed no unsupported renamed-requirements delta is present.
- Copied the authoritative change domain specification to the absent canonical target.
- Ran `git diff --check` successfully after sync.

## Next recommended phase

` sdd-archive ` when the orchestrator permits archive. Do not archive during sync.
