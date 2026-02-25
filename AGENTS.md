# AI Agent Playbook for FieldWorks

Minimal, high-signal guidance for coding agents in this repository.

## Non-negotiable defaults

- Platform is Windows/x64.
- Build with `.\build.ps1`.
- Test with `.\test.ps1`.
- Do not bypass repository scripts for normal build/test work.

## Critical constraints

- Native C++ must build before managed projects (enforced by `FieldWorks.proj` + `build.ps1`).
- FieldWorks uses registration-free COM; do not register COM globally and do not add registry hacks.
- Keep localization in `.resx`; do not hardcode translatable UI strings.

## Context model

- Use `.github/instructions/*.instructions.md` for prescriptive rules.
- Use `Repository.Intelligence.Graph.json` as the deterministic architecture/build/test map.
- Use `Src/AGENTS.md`, `.github/AGENTS.md`, `FLExInstaller/AGENTS.md`, and `openspec/AGENTS.md` for area-specific guidance.

## RIG and Serena

- Prefer Serena symbolic tools for code discovery/navigation before broad file reads.
- Use `Repository.Intelligence.Graph.json` for project graph/build/test topology, then drill into code with Serena tools.
- Commit `Repository.Intelligence.Graph.json` to source control.
- Regenerate it deterministically with `Build/Agent/Generate-RepositoryIntelligenceGraph.ps1` when project structure/build topology changes.
- Do not gitignore the RIG file.

## Issue tracking and Jira

- Use GitHub issues/PRs (and Jira when required) for issue workflow.
- For `LT-` Jira tickets, use the Atlassian Python skill scripts under `.github/skills/atlassian-readonly-skills/scripts`.
- Do not attempt direct web access to Jira pages from agent tooling.

## Validation checklist

1. Run the relevant build/test scripts for touched areas.
2. Keep edits scoped and avoid unrelated refactors.
3. Update docs only when behavior/contracts/process changed.
