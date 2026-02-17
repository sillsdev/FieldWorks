---
spec-id: integration/external/paratext
created: 2026-02-05
status: draft
---

# Paratext Integration

## Purpose

Describe integration patterns for Paratext scripture data.

## Summary

- Describes shared Paratext adapter and import patterns.
- Keeps Paratext integration isolated behind plugins.
- Points to provider, import, and helper components.

## User Stories

- As a scripture editor, I want Paratext projects to integrate with FLEx.
- As a maintainer, I want Paratext integration to remain isolated behind adapters.

## Context

Paratext integrations are handled via provider plugins, helpers, and import pipelines.

## Behavior

- Paratext providers expose scripture projects via adapter interfaces.
- Paratext import workflows use shared adapters and helpers.

### References

- [Paratext import](../../../../Src/ParatextImport/AGENTS.md#purpose) — Import pipeline
- [Paratext 8 provider](../../../../Src/Paratext8Plugin/AGENTS.md#purpose) — Provider plugin
- [Scripture utilities](../../../../Src/Common/ScriptureUtils/AGENTS.md#purpose) — Helper interfaces
- [Paratext lexicon plugin](../../../../Src/FwParatextLexiconPlugin/AGENTS.md#purpose) — Lexicon integration

## Constraints

- Keep Paratext adapters optional and versioned.
- Avoid leaking Paratext SDK types into core assemblies.

## Anti-patterns

- Direct Paratext SDK usage in core UI layers.

## Open Questions

- Should Paratext integration diagnostics be standardized?
