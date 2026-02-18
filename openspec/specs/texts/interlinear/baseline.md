---
spec-id: texts/interlinear/baseline
created: 2026-02-05
status: draft
---

# Interlinear Baseline

## Purpose

Describe baseline text handling in interlinear workflows.

## User Stories

- As a linguist, I want baseline text lines to remain consistent across interlinear views.
- As an editor, I want baseline text to use shared configuration controls.

## Context

Baseline text is rendered by Interlinear components and configured through shared dialogs.

## Behavior

- Baseline lines are rendered by Interlinear view constructors.
- Baseline configuration is managed through shared interlinear dialogs.

### References

- [Interlinear components](../../../../Src/LexText/Interlinear/AGENTS.md#key-components) — Interlinear view constructors
- [Lexicon controls](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — Interlinear configuration dialogs

## Constraints

- Baseline rendering must stay consistent across interlinear views.
- Avoid customizing baseline rendering outside shared components.

## Anti-patterns

- Module-specific baseline renderers that bypass Interlinear settings.

## Open Questions

- Should baseline rendering support alternate display modes by default?
