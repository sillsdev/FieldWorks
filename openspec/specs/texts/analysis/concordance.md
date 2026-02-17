---
spec-id: texts/analysis/concordance
created: 2026-02-05
status: draft
---

# Concordance

## Purpose

Describe concordance search workflows for text analysis.

## User Stories

- As a linguist, I want to search concordance results across interlinear texts.
- As an analyst, I want concordance workflows to use shared UI and filters.

## Context

Concordance search is provided by Interlinear components and related analysis dialogs.

## Behavior

- Concordance search uses Interlinear concordance controls.
- Concordance filters align with shared analysis settings.

### References

- [Interlinear components](../../../../Src/LexText/Interlinear/AGENTS.md#key-components) — ConcordanceControl and ComplexConc
- [Morphology UI](../../../../Src/LexText/Morphology/AGENTS.md#key-components) — Concordance dialogs in morphology

## Constraints

- Use shared concordance controls across analysis modules.
- Avoid duplicating concordance logic in module-specific code.

## Anti-patterns

- Custom concordance dialogs that bypass shared components.

## Open Questions

- Should concordance exports be standardized across modules?
