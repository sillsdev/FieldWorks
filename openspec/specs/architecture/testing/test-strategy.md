---
spec-id: architecture/testing/test-strategy
created: 2026-02-05
status: draft
---

# Test Strategy

## Purpose

Describe the shared testing strategy across FieldWorks components.

## Context

Testing spans managed and native components with shared guidance in minimal AGENTS guidance. This spec captures the high-level approach.

## Strategy

- Managed tests use NUnit or VSTest and live alongside source projects.
- Native tests use Unit++ test runners.
- Use repository test scripts for consistent execution.

### References


## Constraints

- Use repo test scripts for consistent setup.
- Keep tests deterministic and data-driven where possible.

## Anti-patterns

- Ad-hoc test execution that bypasses repo scripts.

## Open Questions

- Should we standardize test categories for faster CI filtering?
