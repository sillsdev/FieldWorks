# AI Agent Playbook for FieldWorks

Minimal, high-signal guidance for coding agents in this repository.

## Non-negotiable defaults

- Platform is Windows/x64.
- Build with `.\build.ps1 -CommentHygiene -TokenHygiene`.
- Test with `.\test.ps1 -CommentHygiene -TokenHygiene`.
- `-CommentHygiene` is required of agents and not of humans: it fails the run on
	any comment-hygiene violation in the lines your branch adds, so you fix your
	own comments before they reach review. Do not drop the flag to get a build
	through.
- `-TokenHygiene` is required of agents and not of humans locally, and also
	fails CI outright (unlike comment-hygiene, which stays advisory-only in
	CI): it fails the run on any hardcoded color or spacing/sizing literal
	anywhere in the token-hygiene scope (Src/Common/FwAvalonia,
	FwAvaloniaDialogs, FwAvaloniaTheme, FwAvaloniaPreviewHost,
	Src/LexText/LexTextControls/Avalonia, Src/xWorks/Avalonia) -- not
	diff-scoped like comment-hygiene, the whole scoped tree must be clean on
	every run. Do not drop the flag to get a build through.
- Do not bypass repository scripts for normal build/test work.
- Commit messages must pass `gitlint` (CI: `.github/workflows/CommitMessage.yml`):
	title <=72 characters, body lines <=80 characters, blank line between
	them. A heredoc reproduces your line breaks exactly -- wrap body prose
	by hand. See `.claude/skills/commit-messages/SKILL.md`.

## Critical constraints

- Native C++ must build before managed projects (enforced by `FieldWorks.proj` + `build.ps1`).
- FieldWorks uses registration-free COM; do not register COM globally and do not add registry hacks.
- Keep localization in `.resx`; do not hardcode translatable UI strings.
- Follow the code-comment standard in
	`.claude/skills/fieldworks-code-commenting/SKILL.md`.
- Follow the commit-message rules in
	`.claude/skills/commit-messages/SKILL.md` for every commit.

## Context model

- Keep shared repo guidance in this file plus the nearest nested `AGENTS.md`.
- Consult `Docs/lessons/README.md` for indexed, human-reviewed lessons from
	completed, rejected, or retired work before planning in a covered area.
- GitHub Copilot custom instructions still require `.github/copilot-instructions.md`
	and `.github/instructions/*.instructions.md`.
- Claude Code reads `CLAUDE.md`, which imports this file; keep Claude-only
	workflows under `.claude/skills/`.
- Use `Src/AGENTS.md`, `FLExInstaller/AGENTS.md`, `openspec/AGENTS.md`, and
	`.github/AGENTS.md` when touching GitHub-specific repo files.

## External Dependencies (LibLcm)

FieldWorks is built upon the `liblcm` (Language & Culture Model) repository, which provides the main data model and FDO (FieldWorks Data Objects) layers used by FieldWorks. The liblcm library is the core FieldWorks model for language and culture data and includes interfaces like `IScrFootnoteFactory` that FieldWorks consumes. If you cannot find a core data model definition within this workspace, ask for access to the `liblcm` repository to reference the source.

## Serena navigation

- Prefer Serena symbolic tools for code discovery/navigation before broad file reads.

## MCP servers

- `.mcp.json` registers the winforms-mcp server (strict JSON — no comments);
	setup and rationale live in
	`.claude/skills/fieldworks-winapp/references/mcp-setup.md`.

## Issue tracking and Jira

- Use GitHub issues/PRs (and Jira when required) for issue workflow.
- For `LT-` Jira tickets, use the Atlassian Python skill scripts under `.claude/skills/atlassian-readonly-skills/scripts`.
- Do not attempt direct web access to Jira pages from agent tooling.
- GitHub Copilot for Jira may be used with authenticated Jira Data Center access when the approved service-user/API policy limits which issues are exposed.
- See `.github/copilot-jira-setup.md` for setup and secret guidance.

## Validation checklist

1. Run the relevant build/test scripts for touched areas.
2. Keep edits scoped and avoid unrelated refactors.
3. Update docs only when behavior/contracts/process changed.
