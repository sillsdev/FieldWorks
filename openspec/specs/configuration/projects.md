---
spec-id: configuration/projects
created: 2026-02-05
status: draft
---

# Projects

## Purpose

Define shared project configuration patterns (creation, backup, and restore).

## User Stories

- As an administrator, I want to configure project settings so that backups and restores are consistent.
- As a user, I want project configuration dialogs to match across tools.

## Context

Project settings flow through framework services and shared dialogs. This spec captures cross-cutting expectations and references component-specific details in AGENTS.md.

## Behavior

- Project settings are handled through shared dialogs and framework settings helpers.
- Backup and restore workflows use standard dialogs rather than module-specific UI.

### References

- [Framework settings](../../../Src/Common/Framework/AGENTS.md#key-components) — Framework settings infrastructure
- [Project dialogs](../../../Src/FwCoreDlgs/AGENTS.md#key-components) — Backup/restore and project dialogs
- [Utility helpers](../../../Src/Common/FwUtils/AGENTS.md#key-components) — Directory and settings utilities

## Constraints

- Use shared dialogs for backup/restore flows.
- Keep project settings consistent across all shells.

## Anti-patterns

- Module-specific backup UI that bypasses shared settings.

## Open Questions

- Should project configuration enforce validation rules across all modules?
