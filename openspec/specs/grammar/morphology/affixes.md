---
spec-id: grammar/morphology/affixes
created: 2026-02-05
status: draft
---

# Morphological Affixes

## Purpose

Describe shared editing patterns for affixes and templates.

## User Stories

- As a linguist, I want to define affix templates and rules consistently.
- As a reviewer, I want affix editing to reuse shared UI components.

## Context

Affix editing uses Morphology UI components and parser integration.

## Behavior

- Affix templates are edited via shared Morphology controls.
- Affix rule formulas align with parser expectations.

### References

- [Morphology UI](../../../../Src/LexText/Morphology/AGENTS.md#key-components) — Affix template and rule editors
- [ParserCore integration](../../../../Src/LexText/ParserCore/AGENTS.md#dependencies) — Parser consumes morphology rules

## Constraints

- Keep affix definitions aligned with parser configuration.
- Avoid custom affix editors outside Morphology components.

## Anti-patterns

- Defining affixes without corresponding parser updates.

## Open Questions

- Should affix template validation run automatically on save?
