---
spec-id: lexicon/import/lift
created: 2026-02-05
status: draft
---

# LIFT Import

## Purpose

Describe the LIFT import workflow for lexicon data.

## User Stories

- As a lexicographer, I want to import LIFT data using shared tools.
- As a project manager, I want LIFT import to preserve entry structure and relations.

## Context

LIFT import is driven through shared lexicon import UI and lexicon entry handling.

## Behavior

- LIFT files are imported through the LexImportWizard pipeline.
- Imported data is mapped to shared lexicon entry structures.

### References

- [Lexicon import wizard](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — LexImportWizard UI
- [Lexicon UI](../../../../Src/LexText/Lexicon/AGENTS.md#purpose) — Entry structure handling

## Constraints

- Keep LIFT mapping aligned with current lexicon entry structures.
- Avoid alternate import pipelines that bypass shared UI.

## Anti-patterns

- Importing LIFT without validating entry structure.

## Open Questions

- Should we standardize LIFT import diagnostics output?
