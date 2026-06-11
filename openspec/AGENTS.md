# OpenSpec Instructions

These instructions are for AI assistants working with the OpenSpec workflow in FieldWorks.

<!-- OPENSPEC:START -->
Use `openspec/AGENTS.md` when the request:
- Mentions OpenSpec, proposals, specs, design, tasks, verification, sync, or archive work
- References `/opsx:*` commands or asks to do the equivalent workflow from a non-Copilot agent such as Claude Code
- Touches `openspec/**`, `.github/prompts/opsx-*`, or `.claude/skills/openspec-*`

Use `openspec/AGENTS.md` to learn:
- The canonical OpenSpec workflow for this repository
- The CLI-first equivalents for agents that do not use GitHub Copilot prompt files
- How to keep change artifacts, main specs, and archive flow aligned

Keep this managed block so `openspec update` can refresh the instructions.
<!-- OPENSPEC:END -->

## Preferred workflow

1. Explore before implementing when scope or approach is unclear.
2. Preferred creation path: propose a change and generate the apply-required artifacts before coding.
3. Implement only after the change is apply-ready.
4. Verify implementation against tasks, specs, and design before archive.
5. If delta specs need syncing, sync them before archive; if sync fails, retry or get explicit approval to archive without syncing.

## Command map

- GitHub Copilot core workflow: `/opsx:propose`, `/opsx:explore`, `/opsx:apply`, `/opsx:archive`
- Additional GitHub Copilot commands: `/opsx:new`, `/opsx:continue`, `/opsx:ff`, `/opsx:verify`, `/opsx:sync`
- CLI equivalents for Claude Code and other agents:
  - Create a change: `openspec new change "<name>"`
  - Check change status: `openspec status --change "<name>" --json`
  - Generate artifact instructions: `openspec instructions <artifact-id> --change "<name>" --json`
  - Load apply-ready context: `openspec instructions apply --change "<name>" --json`
  - Archive a completed change: `openspec archive "<name>"`

## Guardrails

- Keep specs concise, testable, and implementation-agnostic.
- Read every artifact path returned in `contextFiles` rather than assuming one file per artifact type.
- Do not archive a change after a failed sync unless the user explicitly accepts archiving without syncing.
- Keep `openspec/specs/` in sync with approved delta specs when a change is finalized.

## References

- [OpenSpec Index](specs/README.md)
- [Spec authoring notes](MAKE_SPECS.md)
- [Root AGENTS guidance](../AGENTS.md)