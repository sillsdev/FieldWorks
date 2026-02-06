---
spec-id: grammar/parsing/troubleshooting
created: 2026-02-05
status: draft
---

# Parser Troubleshooting

## Purpose

Capture common troubleshooting workflows for morphological parsing.

## User Stories

- As a linguist, I want to debug parsing failures using shared tools.
- As a maintainer, I want trace outputs to be consistent across parser engines.

## Context

Parser troubleshooting relies on ParserUI trace tools and ParserCore logging.

## Behavior

- Use ParserUI Try A Word and trace outputs for debugging.
- Use transforms to format trace output for readability.

### References

- [ParserUI troubleshooting tools](../../../../Src/LexText/ParserUI/AGENTS.md#key-components) — Try A Word and trace viewer
- [Transforms](../../../../Src/Transforms/AGENTS.md#key-components) — Trace formatting transforms

## Constraints

- Keep trace output formatting aligned with shared transforms.
- Avoid creating separate trace viewers outside ParserUI.

## Anti-patterns

- Debugging parser output without trace logs.

## Open Questions

- Should parser tracing be standardized across engines?
