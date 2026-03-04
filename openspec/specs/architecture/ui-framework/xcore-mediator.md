---
spec-id: architecture/ui-framework/xcore-mediator
created: 2026-02-05
status: draft
---

# XCore Mediator

## Purpose

Describe the mediator and colleague pattern used for command routing and UI composition.

## Context

XCore is the extensibility framework that coordinates commands, properties, and UI layout across FieldWorks applications. This spec captures the shared mediator patterns used by shells and framework services.

## Mediator Routing

- Mediator coordinates commands and property table updates.
- Colleagues register command handlers and UI components.
- Inventory XML defines the UI layout consumed by XCore.

### References


## Constraints

- Colleagues must register with the mediator before handling commands.
- Avoid direct command routing outside mediator infrastructure.

## Anti-patterns

- Hard-coded UI layout without inventory configuration.
- Command handlers that bypass mediator state.

## Open Questions

- Should mediator tracing be standardized in specs?
