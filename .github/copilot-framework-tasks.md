# AI agent framework tasks

This checklist tracks repository updates that improve AI workflows using agentic primitives, context engineering, and spec-first development.

## Option 1 — Docs-first primitives (low effort, high ROI)

- [x] Create domain instructions files:
  - [x] .github/instructions/managed.instructions.md
  - [x] .github/instructions/native.instructions.md
  - [x] .github/instructions/installer.instructions.md
  - [x] .github/instructions/testing.instructions.md
  - [x] .github/instructions/build.instructions.md
- [x] Add role-scoped chat modes with tool boundaries:
  - [x] .github/chatmodes/managed-engineer.chatmode.md
  - [x] .github/chatmodes/native-engineer.chatmode.md
  - [x] .github/chatmodes/installer-engineer.chatmode.md
  - [x] .github/chatmodes/technical-writer.chatmode.md
- [x] Add context and memory anchors:
  - [x] .github/context/codebase.context.md
  - [x] .github/memory.md
- [x] Reference these entry points from onboarding:
  - [x] Link instructions, chat modes, and context in .github/AGENTS.md

## Option 2 — Agentic workflows + spec-first flow (moderate effort)

- [ ] Prompts in .github/prompts/:
  - [ ] feature-spec.prompt.md (spec → plan → implement with gates; uses spec-kit)
  - [ ] bugfix.prompt.md (triage → RCA → fix plan → patch + tests)
  - [ ] test-failure-debug.prompt.md (parse NUnit output → targeted fixes)
- [ ] Specification templates:
  - [ ] .github/spec-templates/spec.md and plan.md (or link to spec-kit)
  - [ ] .github/recipes/*.md playbooks for common tasks
- [ ] Fast inner-loop tasks:
  - [ ] Extend .vscode/tasks.json: quick builds (managed/native), smoke tests, whitespace/gitlint

## Option 3 — Outer-loop automation + MCP integration (higher effort)

- [ ] Agent CLI/APM scaffolding:
  - [ ] apm.yml: map scripts to prompts and declare MCP dependencies
  - [ ] Document local usage: `apm install`, `apm run agent-feature-spec --param specFile=...`
  - [ ] GH Action to run chosen prompt on PR, post summary/comments
- [ ] MCP servers & boundaries:
  - [ ] Add GitHub MCP server and Filesystem MCP (pilot set); restrict by chat mode
  - [ ] Capture list and policies in `.github/context/mcp.servers.md`
- [ ] CI governance:
  - [ ] lint-docs job to verify AGENTS.md presence/links and src-catalog consistency
  - [ ] prompt validation job to parse `.prompt.md` frontmatter/structure
- [ ] Security & secrets:
  - [ ] Use least-privilege tokens (e.g., `secrets.AGENT_CLI_PAT`)
  - [ ] Add a security review checklist for enabling new tools/servers
- [ ] Rollout strategy:
  - [ ] Pilot a no-write prompt (`test-failure-debug.prompt.md`) on PRs
  - [ ] Iterate then enable selective write-capable workflows

See: `.github/option3-plan.md` for details.

## Notes
- Keep instructions concise and domain-scoped (use `applyTo` when appropriate).
- Follow the canonical agent-doc skeleton described in `Docs/agent-docs-refresh.md` (detect → plan → validate workflow) and remove scaffold leftovers when editing docs.
- Prefer fast inner-loop build/test paths for agents; reserve installer builds for when necessary.


## small but high-impact extras
- [ ] Add mermaid diagrams in .github/docs/architecture.md showing component relationships (Cellar/Common/XCore/xWorks), so agents can parse text-based diagrams.
- [ ] Create tests.index.md that maps each major component to its test assemblies and common scenarios (fast lookup for agents).
- [ ] Enrich each AGENTS.md with section headers that match your instructions architecture: Responsibilities, Entry points, Dependencies, Tests, Pitfalls, Extension points. Agents recognize consistent structures quickly.
- [ ] Link your CI checks in the instructions: we already added commit/whitespace/build rules and a PR template—keep those links at the top of AGENTS.md.
