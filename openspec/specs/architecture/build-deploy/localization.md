---
spec-id: architecture/build-deploy/localization
created: 2026-02-05
status: draft
---

# Localization

## Purpose

Document shared localization practices for FieldWorks.

## Context

Localization relies on .resx resources and shared resource assemblies. Component-specific resources are documented in Repository.Intelligence.Graph.json and minimal AGENTS guidance.

## Localization Patterns

- Use .resx resource files for UI strings and assets.
- Shared resources live in FwResources for consistent localization.
- Build-time localization is handled by `Build/Localize.targets` (Crowdin integration).
- `crowdin.json` at the repo root defines file mappings for the Crowdin translation platform.

### References


## Constraints

- Avoid hardcoded UI strings outside resource files.
- Keep shared resources centralized in FwResources.

## Anti-patterns

- Module-specific resource systems that bypass FwResources.

## Open Questions

- Should localization validation be enforced during builds?
