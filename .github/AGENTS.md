# FieldWorks GitHub Compatibility

This folder exists for GitHub-required repository configuration and Copilot
entrypoints.

## Build and test

- Build with `.\build.ps1`.
- Test with `.\test.ps1`.
- Use `Build/Agent/` or `scripts/Agent/` wrappers instead of custom shell pipelines where wrappers exist.

## Documentation model

- Shared standing guidance lives in `../AGENTS.md`.
- GitHub-required custom instructions live in `.github/copilot-instructions.md`
	and `.github/instructions/*.instructions.md`.
- Claude-only workflows live in `../.claude/skills/` and should not be
	duplicated under `.github/`.

## Serena operating model

- Use Serena tools first for symbol-aware exploration and edits.

## Key constraints

- Preserve native-before-managed build ordering.
- Preserve registration-free COM behavior.
- Keep localization in `.resx`.

## Jira and issue flow

- Use GitHub issues/PRs for local issue flow.
- For `LT-` tickets, use the Atlassian skill scripts; do not attempt direct Jira URL browsing.
- GitHub Copilot for Jira may be used with authenticated Jira Data Center access when the approved service-user/API policy limits which issues are exposed.
- Setup and policy notes: `.github/copilot-jira-setup.md`

## Relevant files

- `../AGENTS.md`
- `../CLAUDE.md`
- `.github/instructions/build.instructions.md`
- `.github/instructions/navigation.instructions.md`
- `.github/instructions/testing.instructions.md`
- `.github/instructions/managed.instructions.md`
- `.github/instructions/native.instructions.md`
- `.github/instructions/installer.instructions.md`
- `.github/rubrics/*.yaml`
- `../.claude/skills/rubric-verify/SKILL.md`
- `Src/AGENTS.md`
- `FLExInstaller/AGENTS.md`



