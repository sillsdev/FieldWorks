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
- Apply `.github/instructions/navigation.instructions.md` for structural navigation and hidden-dependency handling.
- Use `Src/AGENTS.md`, `.github/AGENTS.md`, `FLExInstaller/AGENTS.md`, and `openspec/AGENTS.md` for area-specific guidance.

## External Dependencies (LibLcm)

FieldWorks is built upon the `liblcm` (Language & Culture Model) repository, which provides the main data model and FDO (FieldWorks Data Objects) layers used by FieldWorks. The liblcm library is the core FieldWorks model for language and culture data and includes interfaces like `IScrFootnoteFactory` that FieldWorks consumes. If you cannot find a core data model definition within this workspace, ask for access to the `liblcm` repository to reference the source.

## Serena navigation

- Prefer Serena symbolic tools for code discovery/navigation before broad file reads.

## Issue tracking and Jira

- Use GitHub issues/PRs (and Jira when required) for issue workflow.
- For `LT-` Jira tickets, use the Atlassian Python skill scripts under `.github/skills/atlassian-readonly-skills/scripts`.
- Do not attempt direct web access to Jira pages from agent tooling.
- GitHub Copilot for Jira may be used with authenticated Jira Data Center access when the approved service-user/API policy limits which issues are exposed.
- See `.github/copilot-jira-setup.md` for setup and secret guidance.

## Validation checklist

1. Run the relevant build/test scripts for touched areas.
2. Keep edits scoped and avoid unrelated refactors.
3. Update docs only when behavior/contracts/process changed.
