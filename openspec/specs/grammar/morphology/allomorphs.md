---
spec-id: grammar/morphology/allomorphs
created: 2026-02-05
status: draft
---

# Allomorphs

## Purpose

Describe shared patterns for allomorph editing and validation.

## User Stories

- As a linguist, I want to edit allomorphs consistently across the UI.
- As a parser maintainer, I want allomorph changes to feed parser configuration.

## Context

Allomorph editing uses Morphology UI components, and parser integration relies on accurate allomorph data.

## Behavior

- Allomorphs are edited through shared Morphology slices and dialogs.
- Parser configuration is refreshed when allomorph definitions change.

### References

- [Morphology UI](../../../../Src/LexText/Morphology/AGENTS.md#key-components) — Allomorph editors
- [ParserCore integration](../../../../Src/LexText/ParserCore/AGENTS.md#dependencies) — Parser uses allomorph data

## Constraints

- Keep allomorph data aligned with parser configuration.
- Avoid separate allomorph editors outside shared Morphology components.

## Anti-patterns

- Defining allomorphs without updating parser data.

## Open Questions

- Should allomorph changes trigger parser reparse tasks?
