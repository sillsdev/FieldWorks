# Agent docs refresh workflow

FieldWorks uses a minimal AGENTS model. There is no hash/frontmatter pipeline and no AGENTS CI gate.

## Current model

- Keep only the core always-on files: `AGENTS.md`, `CLAUDE.md`,
	`.github/AGENTS.md`, `Src/AGENTS.md`, `FLExInstaller/AGENTS.md`,
	`openspec/AGENTS.md`.
- Keep content requirement-only and short.
- Use `.github/instructions/*.instructions.md` only for GitHub-required,
	path-scoped Copilot rules.
- Put Claude-only procedures under `.claude/skills/` instead of inflating the
	always-on docs.

## Refresh process

1. Update one of the core AGENTS files only when behavior or process changed.
2. Run relevant build/test validation for touched areas.
3. Commit docs together when guidance or structure changed.

## What not to do

- Do not reintroduce per-folder AGENTS sprawl.
- Do not add hash metadata or stale-check scripts.
- Do not block CI on AGENTS freshness.
