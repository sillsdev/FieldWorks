---
spec-id: integration/external/flextools
created: 2026-02-05
status: draft
---

# FLExTools Integration

## Purpose

Describe integration patterns for FLExTools scripting and automation.

## User Stories

- As a power user, I want scripting integrations to access FLEx data safely.
- As a maintainer, I want scripting integrations to be isolated from core UI.

## Context

Scripting integrations rely on shared utilities and data access contracts. This spec captures the expected isolation and reuse of shared infrastructure.

## Behavior

- Scripting integrations consume shared data access APIs.
- XML-based configuration and transforms are reused for automation workflows.

### References

- [FieldWorks core services](../../../../Src/Common/FieldWorks/AGENTS.md#interop--contracts) — Core service contracts
- [XML utilities](../../../../Src/Utilities/XMLUtils/AGENTS.md#purpose) — XML configuration helpers
- [Transforms](../../../../Src/Transforms/AGENTS.md#purpose) — Shared transform pipeline

## Constraints

- Keep scripting integrations isolated from UI-specific assemblies.
- Avoid coupling scripting workflows to shell-specific logic.

## Anti-patterns

- Direct UI automation that bypasses shared services.

## Open Questions

- Should we formalize scripting extension points and versioning?
