---
spec-id: architecture/interop/native-boundary
created: 2026-02-05
status: draft
---

# Native Boundary

## Purpose

Capture the managed-to-native interop boundary patterns used by FieldWorks.

## Context

The Views rendering engine and related interfaces are native C++ COM servers. Managed code uses COM interop interfaces to drive rendering and editing. This spec describes shared marshaling expectations.

## Marshaling Patterns

- Managed components consume Views COM interfaces via ViewsInterfaces.
- RootSite and SimpleRootSite host view objects and implement COM callbacks.
- ManagedVwWindow bridges WinForms controls to native IVwWindow.

### References


## Constraints

- Do not throw exceptions across managed/native boundaries.
- Keep COM GUIDs stable and update manifests via reg-free build targets.
- Marshal strings and structs explicitly to avoid locale-dependent behavior.

## Anti-patterns

- Creating COM objects without lifetime management (Dispose/Release).
- Passing UI-thread objects across background threads.

## Open Questions

- Are there remaining P/Invoke boundaries that should be documented alongside COM?
