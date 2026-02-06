---
spec-id: architecture/interop/com-contracts
created: 2026-02-05
status: draft
---

# COM Contracts

## Purpose

Describe the registration-free COM contracts used across FieldWorks components so that native and managed layers can interoperate without global registry changes.

## Context

FieldWorks relies on registration-free COM to avoid global registry pollution. Native COM interfaces and proxy/stub DLLs are authored in native components, while managed layers consume these interfaces through C++/CLI and COM interop. This spec captures cross-cutting constraints and the contract boundaries; component-specific details remain in AGENTS.md.

## Behavior

### COM Registration Patterns

- COM interfaces are authored in native projects (IDL, GUIDs) and built into proxy/stub DLLs.
- Managed components consume COM interfaces through interop boundaries and wrapper libraries.
- Registration-free COM manifests are generated and distributed via build targets; avoid global registration.

#### References

- [Kernel COM implementation](../../../../Src/Kernel/AGENTS.md#interop--contracts) — Proxy/stub GUIDs and core COM identifiers
- [FwCoreDlgs COM usage](../../../../Src/FwCoreDlgs/AGENTS.md#interop--contracts) — Managed dialogs consuming COM interfaces
- [Views rendering contracts](../../../../Src/Views/AGENTS.md#interop--contracts) — Native rendering COM interfaces
- [Generic COM helpers](../../../../Src/Generic/AGENTS.md#interop--contracts) — Shared COM base utilities
- [RootSite COM contracts](../../../../Src/Common/RootSite/AGENTS.md#interop--contracts) — Managed view environment contracts

## Constraints

- Do not introduce global COM registration or registry edits for FieldWorks components.
- Update COM manifests through the existing build targets (Build/RegFree.targets).
- Keep COM interface GUIDs stable once released.

## Anti-patterns

- Registering COM components globally during development or install.
- Breaking COM interface signatures without coordinated updates in consumers.

## Open Questions

- Are any COM interfaces still registered globally outside the reg-free manifests?
