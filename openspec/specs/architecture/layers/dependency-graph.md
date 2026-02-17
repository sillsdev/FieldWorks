---
spec-id: architecture/layers/dependency-graph
created: 2026-02-05
status: draft
---

# Dependency Graph

## Purpose

Define allowed dependencies between major layers and core frameworks in FieldWorks.

## Context

AGENTS.md files describe component-specific dependencies. This spec captures the cross-cutting rules that keep the dependency graph stable and understandable.

## Dependency Rules

- Application shells depend on the framework, XCore, and data access libraries.
- The framework depends on LCModel services and common utilities, not on application shells.
- XCore provides mediator and plugin infrastructure consumed by application shells and the framework.

### References

- [Framework dependencies](../../../../Src/Common/Framework/AGENTS.md#dependencies) — Framework upstream and downstream usage
- [xWorks dependencies](../../../../Src/xWorks/AGENTS.md#dependencies) — Shell dependencies on frameworks and views
- [XCore dependencies](../../../../Src/XCore/AGENTS.md#dependencies) — Core mediator framework dependencies

## Constraints

- Avoid introducing cyclic dependencies between XCore, Framework, and application shells.
- Keep shared utilities (Common, FwUtils) dependency-light to reduce coupling.

## Anti-patterns

- Application shells referencing each other directly.
- Utilities that take dependencies on UI assemblies.

## Open Questions

- Should we formalize dependency validation in build scripts?
