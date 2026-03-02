# OpenSpec Instructions

These instructions are for AI assistants working in the FieldWorks repository.

<!-- OPENSPEC:START -->
Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.
<!-- OPENSPEC:END -->

## Quick workflow

1. For new capability or architecture changes, create an OpenSpec change first.
2. Keep specs concise, testable, and implementation-agnostic.
3. Track execution in your issue/PR workflow and keep OpenSpec tasks aligned.
4. Archive the change after implementation and verification complete.

## References

- [OpenSpec Index](specs/README.md)
- [Root AGENTS guidance](../AGENTS.md)
