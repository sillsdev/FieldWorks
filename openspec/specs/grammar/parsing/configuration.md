---
spec-id: grammar/parsing/configuration
created: 2026-02-05
status: draft
---

# Parser Configuration

## Purpose

Describe shared configuration workflows for morphological parsers.

## Summary

- Defines shared parser configuration expectations.
- Keeps HC and XAmple settings aligned with UI dialogs.
- Points to ParserUI and ParserCore for details.

## User Stories

- As a linguist, I want to configure parser settings through shared UI.
- As a maintainer, I want parser configuration to remain consistent across parser engines.

## Context

Parser configuration is handled by ParserUI dialogs and ParserCore settings.

## Behavior

- Parser settings are edited through ParserUI dialogs.
- ParserCore consumes configuration for HC and XAmple engines.

### References

- [ParserUI dialogs](../../../../Src/LexText/ParserUI/AGENTS.md#key-components) — ParserParameters and Try A Word
- [ParserCore settings](../../../../Src/LexText/ParserCore/AGENTS.md#config--feature-flags) — Parser options

## Constraints

- Keep parser configuration aligned with active parser engine.
- Avoid bypassing ParserUI dialogs for settings changes.

## Anti-patterns

- Custom parser settings UI outside shared dialogs.

## Open Questions

- Should parser configuration changes trigger automatic reparse?
