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
- Windows RootSite input uses native VwTextStore, and selection/page geometry uses the RootSite HWND with Win32 client-rectangle APIs.
- The retired Linux-era ManagedVwWindow shim is not part of the Windows rendering path.

### References


## Constraints

- Rendering must run on the UI thread; marshal background work before drawing.
- Managed root sites must coordinate selection and data update notifications.

## Anti-patterns

- Direct calls into native Views without COM wrapper lifetime handling.
- Creating view hosts without RootSite/SimpleRootSite patterns.

## Open Questions

- Should buffered rendering guidelines be captured here (ManagedVwDrawRootBuffered)?
