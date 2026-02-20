---
spec-id: architecture/layers/entry-points
created: 2026-02-05
status: draft
---

# Entry Points

## Purpose

Document the canonical entry points for FieldWorks applications and framework services.

## Context

Entry points are distributed across the application shell (xWorks), the mediator framework (XCore), and the application framework (Common/Framework). These entry points define how modules are initialized and hosted.

## Application Entry Points

- xWorks initializes the main application shell and loads work areas.
- XCore provides mediator and colleague registration for extensible command routing.
- Framework base classes (FwApp) are the starting point for application lifecycle and cache setup.

### References

- [xWorks entry points](../../../../Src/xWorks/AGENTS.md#entry-points) — Application shell startup
- [XCore entry points](../../../../Src/XCore/AGENTS.md#entry-points) — Mediator and window initialization
- [Framework entry points](../../../../Src/Common/Framework/AGENTS.md#entry-points) — FwApp-based lifecycle

## Constraints

- Application startup should flow through FwApp and mediator initialization.
- Avoid ad-hoc static initialization that bypasses framework lifecycle.

## Anti-patterns

- Creating windows before mediator and property table setup.
- Hidden entry points that do not register with the application shell.

## Open Questions

- Do we need a documented CLI or automation entry point for headless runs?
