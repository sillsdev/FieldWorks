---
spec-id: lexicon/entries/structure
created: 2026-02-05
status: draft
---

# Entry Structure

## Purpose

Describe the shared structure of lexicon entries and senses in FLEx.

## Summary

- Describes the entry and sense hierarchy used across Lexicon tools.
- Emphasizes shared UI controls for consistent editing.
- Points to Lexicon/LexText components for details.

## User Stories

- As a lexicographer, I want to understand the entry and sense hierarchy so that data is consistent.
- As a linguist, I want entry structure to align with shared editing controls.

## Context

Lexicon entry structure is defined by LexEdDll UI patterns and shared lexicon controls.

## Behavior

- Entries include senses, variants, and references managed through shared slices and launchers.
- Entry UI reuses shared lexicon controls for consistent editing.

### References

- [Lexicon UI](../../../../Src/LexText/Lexicon/AGENTS.md#key-components) — Entry UI slices and launchers
- [Lexicon controls](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — Shared entry dialogs
- [LexText overview](../../../../Src/LexText/AGENTS.md#purpose) — Lexicon module context

## Constraints

- Maintain entry structure consistency across Lexicon and LexText UI.
- Use shared controls for entry editing workflows.

## Anti-patterns

- Creating alternate entry structures in module-specific code.

## Open Questions

- Should entry structure constraints be validated centrally?
