# FieldWorks Agentic Instructions

Short repo-level instructions for agents.

## Build and test

- Build with `.\build.ps1`.
- Test with `.\test.ps1`.
- Use `Build/Agent/` or `scripts/Agent/` wrappers instead of custom shell pipelines where wrappers exist.

## Documentation model

- Keep AGENTS guidance minimal and requirement-only.
- Source of prescriptive constraints is `.github/instructions/*.instructions.md`.

## Serena operating model

- Use Serena tools first for symbol-aware exploration and edits.

## Key constraints

- Preserve native-before-managed build ordering.
- Preserve registration-free COM behavior.
- Keep localization in `.resx`.

## Jira and issue flow

- Use GitHub issues/PRs for local issue flow.
- For `LT-` tickets, use the Atlassian skill scripts; do not attempt direct Jira URL browsing.

## Relevant files

- `.github/instructions/build.instructions.md`
- `.github/instructions/navigation.instructions.md`
- `.github/instructions/testing.instructions.md`
- `.github/instructions/managed.instructions.md`
- `.github/instructions/native.instructions.md`
- `.github/instructions/installer.instructions.md`
- `.github/rubrics/*.yaml`
- `.github/skills/rubric-verify/SKILL.md`
- `Src/AGENTS.md`
- `FLExInstaller/AGENTS.md`



