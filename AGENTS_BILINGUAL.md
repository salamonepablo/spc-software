# SPC Orchestrator - Bilingual Guide (ES/EN)

This document is for collaborators who prefer Spanish guidance while keeping technical rules in English.

## Mission / Mision
- EN: Route each request to the best execution path while preserving Clean Architecture, SOLID, TDD, and English code naming.
- ES: Enrutar cada solicitud al mejor flujo de ejecucion, preservando Clean Architecture, SOLID, TDD y nombres de codigo en ingles.

## Mandatory Rules / Reglas Obligatorias

### Clean Architecture
- EN: Dependencies must point inward: Presentation -> Application -> Domain -> Infrastructure.
- ES: Las dependencias deben apuntar hacia adentro: Presentacion -> Application -> Domain -> Infrastructure.

### SOLID
- EN: Apply SRP, OCP, LSP, ISP, DIP on medium/large changes.
- ES: Aplicar SRP, OCP, LSP, ISP, DIP en cambios medianos/grandes.

### TDD
- EN: Follow RED -> GREEN -> REFACTOR whenever feasible.
- ES: Seguir RED -> GREEN -> REFACTOR siempre que sea posible.

### English Code Naming
- EN: All code identifiers must be English.
- ES: Todos los identificadores de codigo deben estar en ingles.
- EN/ES: Spanish is allowed only for legacy API route compatibility and required labels.

### Response Language / Idioma de Respuesta
- EN: The assistant MUST respond in Spanish (Rioplatense/Argentine informal). Technical terms may stay in English. Code identifiers, commits, and docs remain in English.
- ES: El asistente DEBE responder en español rioplatense (informal argentino). Los terminos tecnicos pueden quedar en ingles. Identificadores de codigo, commits y archivos de documentacion siguen en ingles.

## Execution Flow / Flujo de Ejecucion
1. Context Sync / Sincronizacion de contexto
2. Route Selection / Seleccion de ruta
3. Safe Implementation / Implementacion segura
4. Verification / Verificacion
5. Session Logging / Registro de sesion

## Route Matrix / Matriz de Enrutamiento
- Explore codebase or dependencies:
  - EN: Use Explore agent.
  - ES: Usar agente Explore.
- Agent/customization work:
  - EN: Use skill `agent-customization`.
  - ES: Usar skill `agent-customization`.
- GitHub issue/PR/notification summary:
  - EN/ES: Use `summarize-github-issue-pr-notification`.

## Session Logging / Registro de Sesion
For medium/high-impact changes update both:
- `context/current_session.md`
- `context/session_YYYY-MM.md`

Required fields:
- Date
- Scope
- Files changed
- Architectural impact
- Tests added/updated
- Validation results
- Follow-ups

## Quick References
- `AGENTS.md`
- `context/session_entry_template.md`
- `docs/pr-checklist.md`
- `skills/session-sync/SKILL.md`
- `skills/architecture-guard/SKILL.md`
- `skills/tdd-feature/SKILL.md`
