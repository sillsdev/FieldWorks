---
spec-id: integration/external/encoding
created: 2026-02-05
status: draft
---

# Encoding Integration

## Purpose

Describe integration patterns for encoding converters and text encoding utilities.

## User Stories

- As a data converter, I want encoding converters to be accessible through shared utilities.
- As a maintainer, I want encoding workflows to reuse shared XML helpers.

## Context

Encoding conversion relies on shared utilities and XML configuration for converters.

## Behavior

- Encoding conversions use shared utilities and XML-configured converters.
- Encoding metadata is stored in shared resource/config files.

### References

- [FwUtils utilities](../../../../Src/Common/FwUtils/AGENTS.md#key-components) — Utility helpers and registry settings
- [XML utilities](../../../../Src/Utilities/XMLUtils/AGENTS.md#purpose) — XML configuration helpers
- [Sfm2Xml utilities](../../../../Src/Utilities/SfmToXml/AGENTS.md#purpose) — Encoding-sensitive conversions

## Constraints

- Keep encoding converters configurable via shared XML utilities.
- Avoid hardcoded encoding conversions in module-specific code.

## Anti-patterns

- Encoding conversion logic duplicated across modules.

## Open Questions

- Should we centralize encoding converter registry configuration?
