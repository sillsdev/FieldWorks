---
spec-id: architecture/testing/fixtures
created: 2026-02-05
status: draft
---

# Fixtures

## Purpose

Document shared expectations for test fixtures and test data sets.

## Context

Many components rely on XML and data fixtures (parser transforms, interlinear data, etc.). This spec highlights shared fixture patterns.

## Fixture Patterns

- XML fixtures are stored alongside test projects and reused across parser and transform tests.
- Use known-good reference files for transforms and parser outputs.

### References

- [ParserCore fixtures](../../../../Src/LexText/ParserCore/AGENTS.md#test-index) — Parser XML fixtures
- [Transforms fixtures](../../../../Src/Transforms/AGENTS.md#test-index) — XSLT transform inputs/outputs

## Constraints

- Keep fixtures versioned with the component that consumes them.
- Avoid brittle fixtures that depend on external state.

## Anti-patterns

- Embedding large fixture blobs in source code.

## Open Questions

- Should we centralize fixture directories for cross-component reuse?
