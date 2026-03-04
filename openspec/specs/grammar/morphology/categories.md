---
spec-id: grammar/morphology/categories
created: 2026-02-05
status: draft
---

# Morphology Categories

## Purpose

Describe shared patterns for morphology categories and class lists.

## User Stories

- As a linguist, I want to define morphological categories consistently across tools.
- As a reviewer, I want category editing to use shared UI components.

## Context

Morphology category management uses Morphology UI components and shared lexicon controls.

## Behavior

- Category lists are edited through shared Morphology dialogs and slices.
- Category changes feed into parser configuration and lexicon entry editing.

### References


## Constraints

- Keep category editing aligned with shared UI.
- Avoid custom category stores outside Morphology components.

## Anti-patterns

- Editing categories without updating parser configuration.

## Open Questions

- Should category changes trigger automatic parser refresh?
