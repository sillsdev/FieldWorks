---
spec-id: lexicon/export/dictionary
created: 2026-02-05
status: draft
---

# Dictionary Export

## Purpose

Describe the shared dictionary export flow for lexicon data.

## User Stories

- As a lexicographer, I want to export dictionary data using shared configuration.
- As a publisher, I want exports to use consistent entry structures.

## Context

Dictionary export relies on lexicon UI configuration and shell-level export workflows.

## Behavior

- Export configuration uses shared lexicon settings and dictionary options.
- Dictionary export uses shell-level workflows and shared UI dialogs.

### References

- [Lexicon UI](../../../../Src/LexText/Lexicon/AGENTS.md#purpose) — Lexicon entry configuration
- [xWorks shell](../../../../Src/xWorks/AGENTS.md#purpose) — Shell export workflows

## Constraints

- Use shared configuration dialogs for dictionary exports.
- Keep export formatting aligned with lexicon entry structures.

## Anti-patterns

- Module-specific export flows that bypass shared dictionary configuration.

## Open Questions

- Should export presets be centralized across projects?
