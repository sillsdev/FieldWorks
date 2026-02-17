---
spec-id: lexicon/export/lift
created: 2026-02-05
status: draft
---

# LIFT Export

## Purpose

Describe the LIFT export workflow for lexicon data.

## User Stories

- As a lexicographer, I want to export LIFT data from shared lexicon UI.
- As a collaborator, I want LIFT exports to preserve entry structure.

## Context

LIFT export uses shared lexicon UI workflows and entry structures.

## Behavior

- LIFT export uses shared lexicon export dialogs.
- Exported data aligns with lexicon entry structures and relations.

### References

- [Lexicon UI](../../../../Src/LexText/Lexicon/AGENTS.md#purpose) — Lexicon entry structures
- [Lexicon controls](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — Shared export dialogs

## Constraints

- Preserve entry structure and relations in LIFT exports.
- Use shared export dialogs to maintain consistency.

## Anti-patterns

- Custom export logic that bypasses shared lexicon settings.

## Open Questions

- Should we include automated LIFT export validation?
