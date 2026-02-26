---
spec-id: architecture/data-access/lcm-patterns
created: 2026-02-05
status: draft
---

# LCM Patterns

## Purpose

Define shared patterns for LCModel data access and caching.

## Context

FieldWorks data access is built on LCModel and cache layers used by framework services and view hosts. Data-access specifics are summarized in the minimal AGENTS set plus Repository.Intelligence.Graph.json; this spec captures shared patterns.

## Data Access Patterns

- Use cache-backed data access (LcmCache/RealDataCache) instead of direct file or DB access.
- Keep metadata lookups centralized in metadata caches.
- Maintain UI/data separation by routing updates through framework services.

### References


## Constraints

- Validate data access against metadata where possible.
- Keep data access thread-safe; UI updates must stay on UI thread.

## Anti-patterns

- Bypassing cache layers for direct data mutation.
- Embedding data access logic in UI controls.

## Open Questions

- Should we document cache invalidation strategies across components?
