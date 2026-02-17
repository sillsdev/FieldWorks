---
spec-id: architecture/ui-framework/winforms-patterns
created: 2026-02-05
status: draft
---

# WinForms Patterns

## Purpose

Document the standard WinForms composition patterns used across FieldWorks applications.

## Context

FieldWorks relies on WinForms for UI while layering mediator-driven composition and shared controls. This spec captures shared patterns for window and control composition.

## UI Composition Patterns

- Application shells derive from FwApp and use MainWindowDelegate for window coordination.
- XCore provides XML-driven UI composition and command routing.
- Shared controls are centralized under Common/Controls to avoid UI duplication.
- UI adapter interfaces isolate shell components from concrete sidebar/tooling implementations.

### References

- [Framework composition](../../../../Src/Common/Framework/AGENTS.md#architecture) — Application lifecycle and window delegation
- [XCore UI framework](../../../../Src/XCore/AGENTS.md#architecture) — XML-driven UI composition
- [Shared controls](../../../../Src/Common/Controls/AGENTS.md#purpose) — Common WinForms control library
- [UI adapter contracts](../../../../Src/Common/UIAdapterInterfaces/AGENTS.md#purpose) — Sidebar/tool manager interfaces

## Constraints

- Keep WinForms UI on the UI thread; marshal long-running work.
- Reuse shared controls rather than duplicating custom widgets.

## Anti-patterns

- Mixing mediator logic into view controls directly.
- Creating bespoke UI adapters instead of reusing interfaces.

## Open Questions

- Should we document WPF/Avalonia experimentation boundaries here?
