---
spec-id: texts/analysis/discourse
created: 2026-02-05
status: draft
---

# Discourse Analysis

## Purpose

Describe discourse chart analysis workflows.

## User Stories

- As a linguist, I want to chart texts into discourse structures.
- As a researcher, I want discourse workflows to integrate with interlinear text data.

## Context

Discourse analysis builds on Interlinear text data and uses specialized Discourse components.

## Behavior

- Discourse charts use InterlinDocChart-based views and Discourse UI components.
- Chart configuration and export use shared Discourse tooling.

### References

- [Discourse components](../../../../Src/LexText/Discourse/AGENTS.md#key-components) — Constituent chart UI
- [Interlinear components](../../../../Src/LexText/Interlinear/AGENTS.md#key-components) — InterlinDocChart base

## Constraints

- Keep discourse workflows aligned with interlinear data structures.
- Avoid duplicating chart logic outside Discourse components.

## Anti-patterns

- Custom chart rendering without Discourse infrastructure.

## Open Questions

- Should discourse exports share a common template library?
