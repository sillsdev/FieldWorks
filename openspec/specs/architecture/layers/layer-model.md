---
spec-id: architecture/layers/layer-model
created: 2026-02-05
status: draft
---

# Layer Model

## Purpose

Describe the major architectural layers in FieldWorks so cross-cutting guidance is consistent across components.

## Context

FieldWorks splits responsibilities across UI shell, framework services, and data access infrastructure. Components document their specific responsibilities in AGENTS.md; this spec captures the shared layer model.

## Layer Model

- UI shell (xWorks/LexText) hosts work areas and uses the framework to coordinate windows and record lists.
- Application framework provides lifecycle, editing, and settings services (FwApp, MainWindowDelegate).
- Data access services (LCModel/CacheLight) provide cache-backed data access and metadata resolution.

### References

- [Framework layer](../../../../Src/Common/Framework/AGENTS.md#architecture) — Application framework and lifecycle services
- [UI shell layer](../../../../Src/xWorks/AGENTS.md#architecture) — Primary application shell
- [Cache layer](../../../../Src/CacheLight/AGENTS.md#architecture) — Lightweight data access and metadata cache

## Constraints

- Keep UI concerns in the shell layer; do not embed data access logic in UI classes.
- Framework services own lifecycle, undo/redo, and settings coordination.
- Data access components should remain UI-agnostic.

## Anti-patterns

- Direct database or cache access from UI controls.
- Duplicating framework services in application modules.

## Open Questions

- Do we need a dedicated domain services layer beyond LCModel and Framework?
