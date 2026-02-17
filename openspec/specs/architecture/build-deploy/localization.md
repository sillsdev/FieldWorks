---
spec-id: architecture/build-deploy/localization
created: 2026-02-05
status: draft
---

# Localization

## Purpose

Document shared localization practices for FieldWorks.

## Context

Localization relies on .resx resources and shared resource assemblies. Component-specific resources are documented in AGENTS.md.

## Localization Patterns

- Use .resx resource files for UI strings and assets.
- Shared resources live in FwResources for consistent localization.
- Build-time localization is handled by `Build/Localize.targets` (Crowdin integration).
- `crowdin.json` at the repo root defines file mappings for the Crowdin translation platform.

### References

- [Shared resources](../../../../Src/FwResources/AGENTS.md#purpose) — Centralized strings and images
- [Framework localization](../../../../Src/Common/Framework/AGENTS.md#technology-stack) — Framework resx usage

## Constraints

- Avoid hardcoded UI strings outside resource files.
- Keep shared resources centralized in FwResources.

## Anti-patterns

- Module-specific resource systems that bypass FwResources.

## Open Questions

- Should localization validation be enforced during builds?
