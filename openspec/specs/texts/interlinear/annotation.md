---
spec-id: texts/interlinear/annotation
created: 2026-02-05
status: draft
---

# Interlinear Annotation

## Purpose

Describe the shared interlinear annotation workflow (analysis, glossing, tagging).

## Summary

- Describes shared interlinear annotation workflows.
- Emphasizes reuse of shared lexicon dialogs and controls.
- Points to Interlinear components for implementation details.

## User Stories

- As a linguist, I want to annotate texts with interlinear glosses and analyses.
- As a reviewer, I want interlinear annotations to reuse shared UI and controls.

## Context

Interlinear annotation uses Interlinear (ITextDll) components and shared lexicon controls.

## Behavior

- Interlinear annotation uses InterlinDocForAnalysis and Sandbox workflows.
- Shared dialogs and controls support entry selection and glossing.

### References


## Constraints

- Keep annotation workflows aligned with shared lexicon and text controls.
- Avoid alternative annotation flows outside Interlinear components.

## Anti-patterns

- Custom glossing workflows that bypass shared interlinear infrastructure.

## Open Questions

- Should annotation state be auditable across sessions?
