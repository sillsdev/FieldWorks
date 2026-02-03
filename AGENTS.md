# AI Agent Playbook for FieldWorks

This document explains how AI agents (coding and chat modes) should operate inside the FieldWorks mono-repo and how we keep their instructions synchronized.

> **Need IDE-facing guidance?** See `.github/AGENTS.md`. For governance/process details refer to `Docs/AI_AGENT_GOVERNANCE.md`.

## Quick Start (All Agents)

- Run on Windows (`windows-latest` runners or local VS Code workspaces). Do not create new worktrees or branches unless explicitly requested.
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

## Issue Tracking

This project uses **bd (beads)** for issue tracking.
Run `bd prime` for workflow context.

**Quick reference:**
- `bd ready` - Find unblocked work
- `bd create "Title" --type task --priority 2` - Create issue
- `bd close <id>` - Complete work
- `bd sync` - Sync with git (run at session end)
- see [.github/skills/beads/SKILL.md](.github/skills/beads/SKILL.md)

## Atlassian / JIRA Skills

### Recognizing JIRA Tickets

**LT-prefixed tickets** (e.g., `LT-22382`, `LT-19288`) are JIRA issues from SIL's JIRA instance:
- **Base URL:** `https://jira.sil.org/`
- **Browse URL pattern:** `https://jira.sil.org/browse/LT-XXXXX`
- **Project key:** `LT` (Language Technology)

When you encounter an LT-prefixed identifier in:
- User queries (e.g., "look up LT-22382")
- Code comments (e.g., `// See LT-18363`)
- Commit messages or PR descriptions
- Git log output

**→ Use the Atlassian skill Python scripts** to fetch issue details.

### ⚠️ Critical: Always Use Python Scripts

**NEVER** attempt to:
- Browse to `jira.sil.org` URLs directly (requires authentication)
- Use `fetch_webpage` or similar tools on JIRA URLs
- Use GitHub issue tools for LT-* tickets

**ALWAYS** use the Python scripts from the Atlassian skills:

```powershell
# Get a single issue
python -c "import sys; sys.path.insert(0, '.github/skills/atlassian-readonly-skills/scripts'); from jira_issues import jira_get_issue; print(jira_get_issue('LT-22382'))"

# Search for issues
python -c "import sys; sys.path.insert(0, '.github/skills/atlassian-readonly-skills/scripts'); from jira_search import jira_search; print(jira_search('project = LT AND status = Open'))"
```

Or use the helper scripts in `jira-to-beads` which have CLI entry points:

```powershell
# Export your assigned issues to JSON
python .github/skills/jira-to-beads/scripts/export_jira_assigned.py

# Then read the JSON file
Get-Content .cache/jira_assigned.json | ConvertFrom-Json
```

### When to Use Which Skill

| Scenario | Skill |
|----------|-------|
| Read issue details | [atlassian-readonly-skills](.github/skills/atlassian-readonly-skills/SKILL.md) |
| Search issues | [atlassian-readonly-skills](.github/skills/atlassian-readonly-skills/SKILL.md) |
| Create/update issues | [atlassian-skills](.github/skills/atlassian-skills/SKILL.md) (only when user explicitly requests) |
| Bulk import to Beads | [jira-to-beads](.github/skills/jira-to-beads/SKILL.md) |

### Configuration

Configure via environment variables or agent credentials; see the example template in
[.github/skills/atlassian-readonly-skills/.env.example](.github/skills/atlassian-readonly-skills/.env.example).

**SIL JIRA configuration (data center):**
```bash
JIRA_URL=https://jira.sil.org
JIRA_PAT_TOKEN=<your-personal-access-token>
```


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
