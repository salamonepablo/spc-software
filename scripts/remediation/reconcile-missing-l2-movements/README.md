# Missing L2 movement remediation — repository-safe preflight

This package contains generic tooling only. `manifest.template.json` is a data-free shape, not a manifest. Test fixtures are explicitly synthetic.

## Repository boundary

Do not create `manifest.json`, an evidence directory, a captured command output, an approval record, or a checksum derived from operational data in this repository. Do not place customer, movement, document, quote, or financial values here.

An operator must supply an explicit protected directory outside the canonical repository root. That protected location—not this package—holds the approved manifest, approvals, backup/restore material, reports, provenance, and review evidence. Launchers reject repository-contained paths (including aliases) before reading a manifest or connecting to a database. Console messages are limited to a stage, opaque execution ID, and non-sensitive error category.

## PR 1 preflight boundary

`preflight.ps1` only validates protected paths and external manifest controls. Its protected report uses `result=local-validation-passed` and `databaseQualification=pending`; it never labels local-only validation as database qualification or SQL execution. `preflight.sql` is SELECT-only and is only a query artifact for a separately authorized SQL client to bind externally validated targets into session-local staging. It does not discover targets or expand scope. This PR does not connect to a database, back up data, control services, or mutate data.

Run operational tooling only after separate authorization with external evidence. This repository intentionally contains no live invocation instructions or paths.
