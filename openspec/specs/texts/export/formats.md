---
spec-id: texts/export/formats
created: 2026-02-05
status: draft
---

# Text Export Formats

## Purpose

Describe shared export workflows for interlinear and text analysis outputs.

## User Stories

- As a linguist, I want to export interlinear texts in standard formats.
- As a publisher, I want text exports to reuse shared formatting infrastructure.

## Context

Text export relies on Interlinear print/export components and shared framework publishing interfaces.

## Behavior

- Interlinear print/export uses shared PrintLayout and export utilities.
- Export workflows use framework publishing interfaces where applicable.

### References

- [Interlinear components](../../../../Src/LexText/Interlinear/AGENTS.md#key-components) — PrintLayout and export helpers
- [Framework publishing](../../../../Src/Common/Framework/AGENTS.md#key-components) — Publication interfaces

## Constraints

- Keep text exports aligned with shared print/export infrastructure.
- Avoid custom export formatting outside shared components.

## Anti-patterns

- Export pipelines that bypass Interlinear print/export utilities.

## Open Questions

- Should export format support be centralized across modules?
