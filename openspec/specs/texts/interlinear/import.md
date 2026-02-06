---
spec-id: texts/interlinear/import
created: 2026-02-05
status: draft
---

# Interlinear Import

## Purpose

Describe the import workflow for interlinear text data.

## User Stories

- As a linguist, I want to import interlinear texts into analysis workflows.
- As a project manager, I want imports to use shared interlinear infrastructure.

## Context

Interlinear import uses Interlinear components (BIRD importer) and shared text configuration.

## Behavior

- Interlinear imports use shared importers and data mapping.
- Imported texts integrate with Interlinear analysis views.

### References

- [Interlinear components](../../../../Src/LexText/Interlinear/AGENTS.md#key-components) — BIRD interlinear importer
- [Lexicon controls](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — Import configuration dialogs

## Constraints

- Avoid custom import pipelines outside shared Interlinear components.
- Ensure imported data aligns with interlinear analysis structures.

## Anti-patterns

- Importing interlinear data without using shared importers.

## Open Questions

- Should we add validation steps for imported interlinear data?
