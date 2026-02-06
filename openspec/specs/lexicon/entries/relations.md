---
spec-id: lexicon/entries/relations
created: 2026-02-05
status: draft
---

# Lexical Relations

## Purpose

Describe shared patterns for lexical relations (references, variants, and linkages).

## User Stories

- As a lexicographer, I want to manage lexical relations consistently across entries.
- As a linguist, I want relation editing to reuse shared UI components.

## Context

Lexical relation editing is handled by Lexicon UI slices and shared lexicon controls. Some relation patterns intersect with morphology features.

## Behavior

- Lexical relations are edited through shared reference slices and launchers.
- Relation changes follow shared UI patterns and validation.

### References

- [Lexicon UI](../../../../Src/LexText/Lexicon/AGENTS.md#key-components) — LexReference slices and launchers
- [Lexicon controls](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — Shared reference dialogs
- [Morphology UI](../../../../Src/LexText/Morphology/AGENTS.md#key-components) — Morphology relation editors

## Constraints

- Use shared reference UI for relation edits.
- Avoid introducing relation types without shared UI support.

## Anti-patterns

- Custom relation editors that duplicate reference logic.

## Open Questions

- Do relation types require centralized validation rules?
