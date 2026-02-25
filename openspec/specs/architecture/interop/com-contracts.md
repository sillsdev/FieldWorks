---
spec-id: architecture/interop/com-contracts
created: 2026-02-05
status: draft
---

# COM Contracts

## Purpose

Describe the registration-free COM contracts used across FieldWorks components so that native and managed layers can interoperate without global registry changes.

## Context

FieldWorks relies on registration-free COM to avoid global registry pollution. Native COM interfaces and proxy/stub DLLs are authored in native components, while managed layers consume these interfaces through C++/CLI and COM interop. This spec captures cross-cutting constraints and the contract boundaries; component-specific details remain in Repository.Intelligence.Graph.json and minimal AGENTS guidance.

## Behavior

### COM Registration Patterns

- COM interfaces are authored in native projects (IDL, GUIDs) and built into proxy/stub DLLs.
- Managed components consume COM interfaces through interop boundaries and wrapper libraries.
- Registration-free COM manifests are generated and distributed via build targets; avoid global registration.

#### References


## Constraints

- Do not introduce global COM registration or registry edits for FieldWorks components.
- Update COM manifests through the existing build targets (Build/RegFree.targets).
- Keep COM interface GUIDs stable once released.

## Anti-patterns

- Registering COM components globally during development or install.
- Breaking COM interface signatures without coordinated updates in consumers.

## Open Questions

- Are any COM interfaces still registered globally outside the reg-free manifests?
