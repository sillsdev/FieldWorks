---
spec-id: lexicon/export/pathway
created: 2026-02-05
status: draft
---

# Pathway Export

## Purpose

Document the Pathway publishing export workflow for lexicon data.

## User Stories

- As a publisher, I want to export lexicon data to Pathway for publication workflows.
- As a lexicographer, I want Pathway exports to reuse shared configuration and dialogs.

## Context

Pathway integration is handled by the FlexPathwayPlugin utility and shared lexicon data structures.

## Behavior

- Pathway export uses the FlexPathwayPlugin utility via Tools configuration.
- Export preparation reuses shared lexicon data structures.

### References

- [Pathway plugin](../../../../Src/LexText/FlexPathwayPlugin/AGENTS.md#purpose) — Pathway integration utility
- [Lexicon UI](../../../../Src/LexText/Lexicon/AGENTS.md#purpose) — Lexicon data configuration

## Constraints

- Keep Pathway export aligned with shared lexicon structures.
- Avoid bypassing the FlexPathwayPlugin utility for publishing.

## Anti-patterns

- Direct Pathway export without using shared plugin workflows.

## Open Questions

- Should Pathway export capture additional diagnostics for support?
