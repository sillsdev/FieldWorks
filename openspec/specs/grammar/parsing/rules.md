---
spec-id: grammar/parsing/rules
created: 2026-02-05
status: draft
---

# Parsing Rules

## Purpose

Describe shared expectations for parser rule configuration and validation.

## User Stories

- As a linguist, I want to define parsing rules consistently across engines.
- As a maintainer, I want rule exports and transforms to be reusable.

## Context

Parser rules are defined in morphology data and exported via transforms for parser engines.

## Behavior

- Parser rules are exported via shared transforms for HC and XAmple.
- ParserCore consumes exported rules and manages parsing.

### References

- [ParserCore rule handling](../../../../Src/LexText/ParserCore/AGENTS.md#key-components) — Parser rule consumption
- [Transforms](../../../../Src/Transforms/AGENTS.md#key-components) — XSLT exports for parser rules

## Constraints

- Keep parser rule exports aligned with parser engine expectations.
- Avoid custom rule export pipelines.

## Anti-patterns

- Modifying parser rules without updating export transforms.

## Open Questions

- Should parser rule validation be automated during builds?
