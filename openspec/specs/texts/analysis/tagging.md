---
spec-id: texts/analysis/tagging
created: 2026-02-05
status: draft
---

# Text Tagging

## Purpose

Describe text tagging workflows for interlinear analysis.

## User Stories

- As a linguist, I want to tag portions of text for analysis.
- As a reviewer, I want tagging tools to reuse shared UI components.

## Context

Text tagging uses Interlinear components (TextTaggingView) and is related to Discourse workflows.

## Behavior

- Tagging uses shared Interlinear tagging components.
- Tagging workflows integrate with Discourse and interlinear analysis views.

### References

- [Interlinear components](../../../../Src/LexText/Interlinear/AGENTS.md#key-components) — TextTaggingView and tagging workflows
- [Discourse components](../../../../Src/LexText/Discourse/AGENTS.md#purpose) — Discourse analysis context

## Constraints

- Use shared tagging components for consistency.
- Avoid ad-hoc tagging UIs.

## Anti-patterns

- Tagging workflows that bypass shared interlinear infrastructure.

## Open Questions

- Should tagging categories be shared with discourse templates?
