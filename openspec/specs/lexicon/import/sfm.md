---
spec-id: lexicon/import/sfm
created: 2026-02-05
status: draft
---

# SFM Import

## Purpose

Describe the SFM/Toolbox import pipeline for lexicon data.

## User Stories

- As a lexicographer, I want to import Toolbox/SFM data into FLEx reliably.
- As a project manager, I want imports to reuse shared conversion tools.

## Context

SFM import relies on shared conversion utilities and the lexicon import wizard. This spec captures the cross-cutting flow.

## Behavior

- SFM files are converted to XML using shared Sfm2Xml utilities.
- Lexicon import uses the shared LexImportWizard UI.

### References

- [Sfm2Xml utilities](../../../../Src/Utilities/SfmToXml/AGENTS.md#purpose) — SFM conversion pipeline
- [Lexicon import wizard](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — LexImportWizard UI

## Constraints

- Use shared Sfm2Xml conversion for Toolbox import.
- Avoid bypassing the LexImportWizard pipeline.

## Anti-patterns

- Custom SFM parsing without shared conversion utilities.

## Open Questions

- Should we standardize import validation reports for SFM?
