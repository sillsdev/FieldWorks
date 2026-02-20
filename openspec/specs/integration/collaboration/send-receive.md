---
spec-id: integration/collaboration/send-receive
created: 2026-02-05
status: draft
---

# Send/Receive Collaboration

## Purpose

Describe the Send/Receive collaboration workflows across lexicon and scripture integrations.

## User Stories

- As a project manager, I want Send/Receive workflows to synchronize lexical and scripture data.
- As a collaborator, I want Send/Receive to use shared integration adapters.

## Context

Send/Receive uses Lexicon collaboration integration (FLExBridge) and scripture provider adapters. This spec captures shared expectations for collaboration.

## Behavior

- Lexicon Send/Receive is coordinated through shared Lexicon collaboration components.
- Scripture Send/Receive uses Paratext provider adapters and helper utilities.

### References

- [Lexicon collaboration](../../../../Src/LexText/Lexicon/AGENTS.md#key-components) — FLExBridge integration
- [Scripture utilities](../../../../Src/Common/ScriptureUtils/AGENTS.md#purpose) — Paratext helper and scripture providers

## Constraints

- Keep collaboration workflows aligned with shared adapters.
- Avoid bypassing Send/Receive infrastructure when integrating collaboration tools.

## Anti-patterns

- Direct project synchronization without using shared collaboration helpers.

## Open Questions

- Should Send/Receive logs be standardized across lexicon and scripture workflows?
