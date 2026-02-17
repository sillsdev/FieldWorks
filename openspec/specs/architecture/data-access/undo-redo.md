---
spec-id: architecture/data-access/undo-redo
created: 2026-02-05
status: draft
---

# Undo/Redo

## Purpose

Describe the shared undo/redo coordination patterns used by FieldWorks applications.

## Context

Undo/redo is coordinated through framework services so that application shells and editing helpers remain consistent. This spec captures the common patterns used by FwApp and editing helpers.

## Undo/Redo Flow

- FwApp coordinates undo/redo stacks and exposes UI controls for multi-level undo.
- Editing helpers integrate clipboard and undo/redo operations.
- Shells use framework-provided undo/redo controls rather than custom stacks.

### References

- [Framework undo/redo components](../../../../Src/Common/Framework/AGENTS.md#key-components) — UndoRedoDropDown and editing helpers

## Constraints

- Always use the shared undo/redo stack in framework services.
- Avoid creating isolated undo stacks in module-level code.

## Anti-patterns

- Direct manipulation of LCModel actions without framework coordination.
- Custom undo UI controls outside the shared dropdown.

## Open Questions

- Should undo/redo telemetry or diagnostics be standardized?
