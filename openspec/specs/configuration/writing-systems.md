---
spec-id: configuration/writing-systems
created: 2026-02-05
status: draft
---

# Writing Systems

## Purpose

Describe the shared configuration patterns for writing systems, keyboards, and script settings.

## Summary

- Covers shared writing system configuration across modules.
- Highlights consistency needs for lexicon and text workflows.
- Links to UI components for implementation details.

## User Stories

- As a project manager, I want to configure writing systems so that data entry uses the correct scripts and keyboards.
- As a linguist, I want to adjust writing system properties to keep analysis and publication consistent.

## Context

Writing system configuration is surfaced through shared dialogs and UI components and is used across lexicon and text workflows.

## Behavior

- Writing system properties are edited through standard dialogs and shared controls.
- Writing system choices are stored in project settings and used by lexicon/text UI.

### References

- [Writing system dialogs](../../../Src/FwCoreDlgs/AGENTS.md#key-components) — Writing system properties dialogs
- [Writing system controls](../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — Add writing system UI components

## Constraints

- Keep writing system configuration consistent across lexicon and text tools.
- Avoid bypassing shared dialogs when updating writing system settings.

## Anti-patterns

- Custom per-module writing system editors that bypass shared configuration.

## Open Questions

- Should we centralize writing system audit logs for troubleshooting?
