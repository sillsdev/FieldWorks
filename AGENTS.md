# AI Agent Playbook for FieldWorks

This document explains how AI agents (coding and chat modes) should operate inside the FieldWorks mono-repo and how we keep their instructions synchronized.

> **Need IDE-facing guidance?** See `.github/AGENTS.md`. For governance/process details refer to `Docs/AI_AGENT_GOVERNANCE.md`.

## Quick Start (All Agents)

- Run on Windows (`windows-latest` runners or local VS Code worktrees).
- Always build through the traversal script: `.\build.ps1` (sets configuration, cleans stale obj, enforces native-first order).
- Always test through `.\test.ps1` (dispatches managed/natives tests, applies VS test settings).
- Use `scripts/Agent/*.ps1` wrappers whenever a command would normally need pipes/filters (`Git-Search`, `Read-FileContent`, etc.).
- Keep localization in `.resx` and respect `crowdin.json`; do not introduce new ad-hoc localization flows.

## Agent Catalog

| Agent | Chatmode file | Primary scope | Key refs |
| --- | --- | --- | --- |
| FieldWorks Managed Dev | `.github/chatmodes/managed-engineer.chatmode.md` | C#, WinForms, services, NUnit | `.github/instructions/managed.instructions.md`, `.github/instructions/testing.instructions.md`, `Src/xWorks`, `Src/LexText/**` |
| FieldWorks Native Dev | `.github/chatmodes/native-engineer.chatmode.md` | C++/CLI bridge, core native libs, perf tracing | `.github/instructions/native.instructions.md`, `Build\Src\NativeBuild`, `specs/003-convergence-regfree-com-coverage` |
| FieldWorks UI (WinForms & future Avalonia) | `.github/chatmodes/fieldworks-ui.chatmode.md` | UI composition, area shells, upcoming Avalonia experiments | `.github/instructions/managed.instructions.md`, `Src/Common/Controls`, `Src/xWorks`, `specs/006-convergence-platform-target` |
| FieldWorks Installer | `.github/chatmodes/installer-engineer.chatmode.md` | WiX packaging, prerequisite validation | `.github/instructions/installer.instructions.md`, `FLExInstaller/**`, `specs/007-wix-314-installer` |
| FieldWorks Tech Writer | `.github/chatmodes/technical-writer.chatmode.md` | AGENTS.md upkeep, wiki migrations, specs | `.github/instructions/technical-writer.instructions.md`, `.github/src-catalog.md` |
| Autonomous Coding Agent | `.github/chatmodes/coding-agent.chatmode.md` | Full-stack changes end-to-end (used in CI agents) | `Docs/AI_AGENT_GOVERNANCE.md`, `.github/instructions/*.md` |

### Choosing the right agent

1. **Identify the surface** (managed code, native code, installer, docs).
2. **Pick the matching chatmode** from the table above.
3. **Load context**: read relevant `AGENTS.md`, `specs/` entries, and instructions referenced in the chatmode.
4. **Escalate**: if a task crosses multiple surfaces (e.g., managed ↔ native), coordinate through the coding agent or split the work between specialized agents.


## Integrating with Beads (dependency-aware task planning)

Beads provides a lightweight, dependency-aware issue database and a CLI (`bd`) for selecting "ready work," setting priorities, and tracking status. It complements MCP Agent Mail's messaging, audit trail, and file-reservation signals. Project: [steveyegge/beads](https://github.com/steveyegge/beads)

Recommended conventions
- **Single source of truth**: Use **Beads** for task status/priority/dependencies; use **Agent Mail** for conversation, decisions, and attachments (audit).
- **Shared identifiers**: Use the Beads issue id (e.g., `bd-123`) as the Mail `thread_id` and prefix message subjects with `[bd-123]`.
- **Reservations**: When starting a `bd-###` task, call `file_reservation_paths(...)` for the affected paths; include the issue id in the `reason` and release on completion.

Typical flow (agents)
1) **Pick ready work** (Beads)
   - `bd ready --json` → choose one item (highest priority, no blockers)
2) **Reserve edit surface** (Mail)
   - `file_reservation_paths(project_key, agent_name, ["src/**"], ttl_seconds=3600, exclusive=true, reason="bd-123")`
3) **Announce start** (Mail)
   - `send_message(..., thread_id="bd-123", subject="[bd-123] Start: <short title>", ack_required=true)`
4) **Work and update**
   - Reply in-thread with progress and attach artifacts/images; keep the discussion in one thread per issue id
5) **Complete and release**
   - `bd close bd-123 --reason "Completed"` (Beads is status authority)
   - `release_file_reservations(project_key, agent_name, paths=["src/**"])`
   - Final Mail reply: `[bd-123] Completed` with summary and links

Mapping cheat-sheet
- **Mail `thread_id`** ↔ `bd-###`
- **Mail subject**: `[bd-###] …`
- **File reservation `reason`**: `bd-###`
- **Commit messages (optional)**: include `bd-###` for traceability

Event mirroring (optional automation)
- On `bd update --status blocked`, send a high-importance Mail message in thread `bd-###` describing the blocker.
- On Mail "ACK overdue" for a critical decision, add a Beads label (e.g., `needs-ack`) or bump priority to surface it in `bd ready`.

Pitfalls to avoid
- Don't create or manage tasks in Mail; treat Beads as the single task queue.
- Always include `bd-###` in message `thread_id` to avoid ID drift across tools.

## MCP Agent Mail: coordination for multi-agent workflows

What it is
- A mail-like layer that lets coding agents coordinate asynchronously via MCP tools and resources.
- Provides identities, inbox/outbox, searchable threads, and advisory file reservations, with human-auditable artifacts in Git.

Why it's useful
- Prevents agents from stepping on each other with explicit file reservations (leases) for files/globs.
- Keeps communication out of your token budget by storing messages in a per-project archive.
- Offers quick reads (`resource://inbox/...`, `resource://thread/...`) and macros that bundle common flows.

How to use effectively
1) Same repository
   - Register an identity: call `ensure_project`, then `register_agent` using this repo's absolute path as `project_key`.
   - Reserve files before you edit: `file_reservation_paths(project_key, agent_name, ["src/**"], ttl_seconds=3600, exclusive=true)` to signal intent and avoid conflict.
   - Communicate with threads: use `send_message(..., thread_id="FEAT-123")`; check inbox with `fetch_inbox` and acknowledge with `acknowledge_message`.
   - Read fast: `resource://inbox/{Agent}?project=<abs-path>&limit=20` or `resource://thread/{id}?project=<abs-path>&include_bodies=true`.
   - Tip: set `AGENT_NAME` in your environment so the pre-commit guard can block commits that conflict with others' active exclusive file reservations.

2) Across different repos in one project (e.g., Next.js frontend + FastAPI backend)
   - Option A (single project bus): register both sides under the same `project_key` (shared key/path). Keep reservation patterns specific (e.g., `frontend/**` vs `backend/**`).
   - Option B (separate projects): each repo has its own `project_key`; use `macro_contact_handshake` or `request_contact`/`respond_contact` to link agents, then message directly. Keep a shared `thread_id` (e.g., ticket key) across repos for clean summaries/audits.

Macros vs granular tools
- Prefer macros when you want speed or are on a smaller model: `macro_start_session`, `macro_prepare_thread`, `macro_file_reservation_cycle`, `macro_contact_handshake`.
- Use granular tools when you need control: `register_agent`, `file_reservation_paths`, `send_message`, `fetch_inbox`, `acknowledge_message`.

Common pitfalls
- "from_agent not registered": always `register_agent` in the correct `project_key` first.
- "FILE_RESERVATION_CONFLICT": adjust patterns, wait for expiry, or use a non-exclusive reservation when appropriate.
- Auth errors: if JWT+JWKS is enabled, include a bearer token with a `kid` that matches server JWKS; static bearer is used only when JWT is disabled.

## Atlassian Skills (Default: Read-only)

- Default to the read-only skill set in [.github/skills/atlassian-readonly-skills/SKILL.md](.github/skills/atlassian-readonly-skills/SKILL.md).
- Only use the full-write skill set in [.github/skills/atlassian-skills/SKILL.md](.github/skills/atlassian-skills/SKILL.md) when the user **explicitly** requests create/update/delete.
- Configure via environment variables or agent credentials; see the example template in
  [.github/skills/atlassian-readonly-skills/.env.example](.github/skills/atlassian-readonly-skills/.env.example).
  

## FieldWorks-Specific Agent Rules

### Build & Test ordering
- Native C++ (Phase 2 of `FieldWorks.proj`) must succeed before managed assemblies build. Let `.\build.ps1` enforce this.
- Managed tests come from NUnit/VSTest. Native tests are driven via `scripts/Agent/Invoke-CppTest.ps1`; use `.\test.ps1 -Native` to stay consistent.

### COM and Registry
- FieldWorks relies on registration-free COM. Do not register COM components globally or edit the Windows registry unless a spec explicitly directs it.
- Update manifests through the established build targets (`Build/RegFree.targets`) and document changes in the relevant `AGENTS.md`.

## Governance & Safety

- **Reference**: `Docs/AI_AGENT_GOVERNANCE.md` details approval boundaries, validation steps, and audit logging expectations for custom agents.
- Always document deviations (e.g., skipping `test.ps1` because tests are flaky) in PR descriptions and agent transcripts.
- Agents must never introduce new external services/dependencies without updating documentation and build scripts.

## File & Instruction Guidance

| Path Pattern | Instruction file |
|--------------|------------------|
| `Src/**/*.cs` | `.github/instructions/managed.instructions.md` |
| `Src/**/*.cpp`, `*.h`, `*.hpp` | `.github/instructions/native.instructions.md` |
| `FLExInstaller/**` | `.github/instructions/installer.instructions.md` |
| `Docs/**/*.md` | `.github/instructions/technical-writer.instructions.md` |

Always review the local `AGENTS.md` before editing a folder—the files summarize architecture, dependencies, and testing requirements.

## Validation Checklist (All Agents)

1. `.\build.ps1` passes in the relevant configuration (Debug by default).
2. `.\test.ps1` (or targeted variants) pass for affected components.
3. `.\Build\Agent\check-and-fix-whitespace.ps1` reports clean output.
4. Commit messages meet repo guidelines (`Build/Agent/commit-messages.ps1`).
5. Updated documentation (`AGENTS.md`, specs, instructions) where behavior changed.

## Extending or Creating New Agents

1. Draft scope/requirements in `Docs/AI_AGENT_GOVERNANCE.md` (new section).
2. Add a chatmode under `.github/chatmodes/` following the existing template.
3. Update the catalog table above with scope, references, and validation steps.
4. Regenerate any IDE hints (e.g., `.github/AGENTS.md` or workspace instructions) if activation steps change.

