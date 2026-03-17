# AI Agent Playbook for FieldWorks

Minimal, high-signal guidance for coding agents in this repository.

## Non-negotiable defaults

- Platform is Windows/x64.
- Build with `.\build.ps1`.
- Test with `.\test.ps1`.
- Do not bypass repository scripts for normal build/test work.
- Before committing or pushing, run the existing VS Code task `CI: Full local check`.
- After any rebase, merge, cherry-pick, or manual conflict resolution, run `CI: Whitespace check` before committing.
- If `CI: Whitespace check` rewrites files, review and restage those files, then rerun the task until it passes cleanly.
- When commit history changes, run `CI: Commit messages` before pushing.

## Critical constraints

- Native C++ must build before managed projects (enforced by `FieldWorks.proj` + `build.ps1`).
- FieldWorks uses registration-free COM; do not register COM globally and do not add registry hacks.
- Keep localization in `.resx`; do not hardcode translatable UI strings.

## Context model

- Use `.github/instructions/*.instructions.md` for prescriptive rules.
- Apply `.github/instructions/navigation.instructions.md` for structural navigation and hidden-dependency handling.
- Use `Src/AGENTS.md`, `.github/AGENTS.md`, `FLExInstaller/AGENTS.md`, and `openspec/AGENTS.md` for area-specific guidance.

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
2. Run `CI: Full local check` before commit/push; use `CI: Whitespace check` immediately after conflict resolution.
3. Keep edits scoped and avoid unrelated refactors.
4. Update docs only when behavior/contracts/process changed.
