---
spec-id: architecture/build-deploy/build-phases
created: 2026-02-05
status: draft
---

# Build Phases

## Purpose

Document the ordered build phases and constraints for FieldWorks builds.

## Summary

- Defines traversal build ordering (native before managed).
- Notes installer dependency on traversal outputs.
- Points to build entry points for component specifics.

## Context

FieldWorks uses a traversal build with ordered phases and native-first requirements.

## Build Ordering

- Native C++ components must build before managed components.
- Installer builds consume outputs from the main traversal build.

### References

- [FieldWorks build entry points](../../../../Src/Common/FieldWorks/AGENTS.md#build-information) — Core build targets and outputs
- [Framework build info](../../../../Src/Common/Framework/AGENTS.md#build-information) — Managed build targets

## Constraints

- Do not bypass traversal build ordering when adding new projects.
- Ensure native outputs are available before managed code generation.

## Anti-patterns

- Invoking ad-hoc builds that skip native phases.

## Open Questions

- Should build ordering be enforced by CI validation scripts?
