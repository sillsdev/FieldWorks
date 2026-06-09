# AI guidance governance

## Purpose
This repo targets **GitHub Copilot** and **Claude Code** with a minimal,
two-tool documentation model:
- Shared standing guidance lives in root and nested `AGENTS.md` files.
- GitHub-required custom instructions stay in `.github/` only where GitHub
	requires exact paths.
- Claude-only workflows live in `.claude/skills/`.
- Agent definitions in `.github/agents/` and role chatmodes in
	`.github/chatmodes/` describe behavior/persona, not system architecture.

## Source of truth
- **Shared repo workflow**: `AGENTS.md` plus the nearest nested `AGENTS.md`
- **Component architecture & entry points**: `Src/<Component>/AGENTS.md`
- **GitHub Copilot custom instructions**: `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md`
- **Claude Code workflows**: `.claude/skills/*/SKILL.md`

## No duplication rule
- Do not duplicate shared standing guidance across `AGENTS.md`, `CLAUDE.md`,
	and GitHub compatibility files.
- Keep each Claude skill in exactly one place: `.claude/skills/`.
- GitHub prompt files may reference Claude skill paths, but do not keep a
  mirrored `.github/skills/` tree.
- If a rule must be auto-applied by GitHub Copilot, keep it in
	`.github/instructions/*.instructions.md`; otherwise prefer the relevant
	`AGENTS.md` or a Claude skill.

## What goes where

### `AGENTS.md`
Use for:
- One-page onboarding for both tools: build/test commands, repo constraints,
  and links.
- Stable, always-on guidance that should apply across sessions.

### `.github/instructions/*.instructions.md`
Use for:
- Prescriptive constraints that must be applied during editing/review.
- Cross-cutting rules that GitHub Copilot needs through its official,
  path-scoped instruction mechanism.

**Curated keep set (intentionally small):**
- `build.instructions.md`
- `debugging.instructions.md`
- `installer.instructions.md`
- `managed.instructions.md`
- `navigation.instructions.md`
- `native.instructions.md`
- `powershell.instructions.md`
- `repo.instructions.md`
- `security.instructions.md`
- `terminal.instructions.md`
- `testing.instructions.md`

### `Src/**/AGENTS.md`
Use for:
- Where to start (entry points, key projects, typical workflows).
- Dependencies and cross-component links.
- Tests (where they live, how to run them).

Baseline expectations for a component agent doc:
- **Where to start** (projects, primary entry points)
- **Dependencies** (other components/layers)
- **Tests** (test projects and the recommended `./test.ps1` invocation)

### `.claude/skills/`
Use for:
- Claude-only workflows, repeatable procedures, and task-specific guidance.
- Put multi-step procedures here instead of inflating `CLAUDE.md`.

### `.github/agents/` and `.github/chatmodes/`
Use for:
- GitHub Copilot role definitions, boundaries, and tool preferences.
- Do not put component architecture here; link to the component `AGENTS.md`.

## External standards alignment (post-2025)
- **AGENTS.md**: Supported as a simple, vendor-neutral instruction format by multiple tools (for example, Cursor’s project rules). Use plain Markdown with clear headings and concise rules.
- **MCP (Model Context Protocol)**: Use MCP for tool/data integration rather than vendor-specific plugins; MCP provides a standardized, versioned protocol for AI tool connectivity.

## Adding a new scoped instruction file
Add a new `.github/instructions/<name>.instructions.md` only when:
- The guidance is prescriptive (MUST/DO NOT), and
- It applies broadly or to a subtree, and
- It would be harmful if Copilot ignored it.

Otherwise, update the appropriate `AGENTS.md` or add a Claude skill under
`.claude/skills/`.

