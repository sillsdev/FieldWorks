---
spec-id: architecture/interop/external-apis
created: 2026-02-05
status: draft
---

# External APIs

## Purpose

Describe the integration patterns for external SDKs and services (e.g., Paratext).

## Context

External integrations rely on adapter layers and plugins to isolate SDK dependencies. These integrations must remain optional and version-tolerant.

## External Integration Patterns

- Use adapter interfaces to decouple external SDKs from core application code.
- Load external integrations via plugins or reflection to keep them optional.
- Keep integration contracts in clearly scoped assemblies.

### References


## Constraints

- External SDK dependencies must remain optional at runtime.
- Avoid leaking external types into core assemblies.
- Ensure adapter interfaces remain stable across SDK versions.

## Anti-patterns

- Directly referencing external SDK types in core framework code.
- Assuming external SDK installation without checks.

## Open Questions

- Do we need a unified adapter registry for all external SDK integrations?
