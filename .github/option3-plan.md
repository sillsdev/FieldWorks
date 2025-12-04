# Option 3 plan: Outer-loop automation and MCP integration (pilot later)

This plan is mothballed for now. It captures the steps to bring our agent workflows into CI/CD with safe tool boundaries.

## Goals
- Run selected prompts reliably in CI (e.g., spec validation, test failure triage)
- Use least-privilege MCP tools per role/chat mode
- Package agent primitives for sharing and repeatability

## Steps

### 1) Copilot CLI and APM scaffold
- Add `apm.yml` with scripts mapping to our prompts (e.g., `copilot-feature-spec` → feature-spec.prompt.md)
- Include MCP dependencies (e.g., `ghcr.io/github/github-mcp-server`)
- Document local usage in README: `apm install`, `apm run copilot-feature-spec --param specFile=...`

### 2) GitHub Action to run a prompt on PR
- Create `.github/workflows/agent-workflow.yml`
- Matrix run for selected scripts (e.g., spec validation, debug mode)
- Permissions: `pull-requests: write`, `contents: read`, `models: read`
- Post results as PR comments or check summaries

### 3) MCP servers and boundaries
- Start with GitHub MCP server for PR/issue context and Filesystem MCP for repo search
- Restrict tools by chat mode (e.g., installer mode cannot edit native code)
- Maintain a curated list in `.github/context/mcp.servers.md` (to be created when piloting)

### 4) Security and secrets
- Use `secrets.COPILOT_CLI_PAT` for Copilot CLI (if needed)
- Principle of least privilege for tokens and tool scopes
- Add a security review checklist for new tools/servers

### 5) Governance and validation
- Add a `lint-docs` CI job to verify presence and links for:
  - `.github/instructions/*.instructions.md`
  - `Src/*/COPILOT.md`
  - `.github/src-catalog.md`
- Add a `prompt-validate` job: checks frontmatter structure for `.prompt.md`

### 6) Rollout strategy
- Pilot a single prompt (e.g., `test-failure-debug.prompt.md`) that makes no file edits and only posts analysis
- Gather feedback and iterate before enabling write-capable workflows

## References
- `.github/copilot-instructions.md` (entry points)
- `.github/prompts/` (agent workflows)
- `.github/instructions/` (domain rules)
- `.github/chatmodes/` (role boundaries)
- `.github/context/` and `.github/memory.md` (signals and decisions)
