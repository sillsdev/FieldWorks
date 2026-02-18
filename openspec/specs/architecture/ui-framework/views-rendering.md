---
spec-id: architecture/ui-framework/views-rendering
created: 2026-02-05
status: draft
---

# Views Rendering

## Purpose

Describe the rendering pipeline used by the Views engine and managed root sites.

## Context

FieldWorks uses a native C++ Views engine for text layout and rendering. Managed components host Views via RootSite/SimpleRootSite and COM interfaces provided by ViewsInterfaces.

## Rendering Pipeline

- Views (native) provides layout, selection, and rendering engines.
- ViewsInterfaces exposes COM interfaces to managed code.
- RootSite and SimpleRootSite host root boxes and manage view lifecycle.
- ManagedVwWindow bridges WinForms controls to IVwWindow for native rendering.

### References

- [Views engine](../../../../Src/Views/AGENTS.md#architecture) — Native layout and rendering engine
- [Views interfaces](../../../../Src/Common/ViewsInterfaces/AGENTS.md#architecture) — Managed COM interface definitions
- [RootSite hosting](../../../../Src/Common/RootSite/AGENTS.md#architecture) — Advanced root site infrastructure
- [SimpleRootSite hosting](../../../../Src/Common/SimpleRootSite/AGENTS.md#architecture) — Standard view hosting control
- [ManagedVwWindow bridge](../../../../Src/ManagedVwWindow/AGENTS.md#interop--contracts) — HWND bridge to native Views

## Constraints

- Rendering must run on the UI thread; marshal background work before drawing.
- Managed root sites must coordinate selection and data update notifications.

## Anti-patterns

- Direct calls into native Views without COM wrapper lifetime handling.
- Creating view hosts without RootSite/SimpleRootSite patterns.

## Open Questions

- Should buffered rendering guidelines be captured here (ManagedVwDrawRootBuffered)?
