---
spec-id: integration/collaboration/multi-user
created: 2026-02-05
status: draft
---

# Multi-User Collaboration

## Purpose

Describe multi-user collaboration expectations in FieldWorks shells and frameworks.

## User Stories

- As a project manager, I want to support multi-user collaboration workflows.
- As a user, I want collaboration UI to be consistent across modules.

## Context

Multi-user coordination is handled by application shells and framework services. This spec captures shared expectations for collaboration support.

## Behavior

- Application shells surface collaboration workflows.
- Framework services coordinate shared settings and lifecycle behaviors for collaboration.

### References

- [xWorks shell](../../../../Src/xWorks/AGENTS.md#purpose) — Collaboration surfaced in shell UI
- [Framework services](../../../../Src/Common/Framework/AGENTS.md#purpose) — Shared lifecycle and settings

## Constraints

- Avoid module-specific collaboration flows outside the shell.
- Keep collaboration state consistent through framework services.

## Anti-patterns

- Collaboration features implemented without shell integration.

## Open Questions

- Should collaboration workflows have dedicated diagnostics?
