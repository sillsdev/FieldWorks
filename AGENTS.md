# GitHub Copilot Agent Playbook for FieldWorks

This document explains how Copilot agents (coding and chat modes) should operate inside the FieldWorks mono-repo and how we keep their instructions synchronized.

> **Need IDE-facing guidance?** See `.github/copilot-instructions.md`. For governance/process details refer to `Docs/AI_AGENT_GOVERNANCE.md`.

## Quick Start (All Agents)

- Run on Windows (`windows-latest` runners or local VS Code worktrees).
- Always build through the traversal script: `.\build.ps1` (sets configuration, cleans stale obj, enforces native-first order).
- Always test through `.\test.ps1` (dispatches managed/natives tests, honors containers, applies VS test settings).
- Use `scripts/Agent/*.ps1` wrappers whenever a command would normally need pipes/filters (`Git-Search`, `Read-FileContent`, etc.).
- Keep localization in `.resx` and respect `crowdin.json`; do not introduce new ad-hoc localization flows.

## Agent Catalog

| Agent | Chatmode file | Primary scope | Key refs |
| --- | --- | --- | --- |
| FieldWorks Managed Dev | `.github/chatmodes/managed-engineer.chatmode.md` | C#, WinForms, services, NUnit | `.github/instructions/managed.instructions.md`, `.github/instructions/testing.instructions.md`, `Src/xWorks`, `Src/LexText/**` |
| FieldWorks Native Dev | `.github/chatmodes/native-engineer.chatmode.md` | C++/CLI bridge, core native libs, perf tracing | `.github/instructions/native.instructions.md`, `Build\Src\NativeBuild`, `specs/003-convergence-regfree-com-coverage` |
| FieldWorks UI (WinForms & future Avalonia) | `.github/chatmodes/fieldworks-ui.chatmode.md` | UI composition, area shells, upcoming Avalonia experiments | `.github/instructions/managed.instructions.md`, `Src/Common/Controls`, `Src/xWorks`, `specs/006-convergence-platform-target` |
| FieldWorks Installer | `.github/chatmodes/installer-engineer.chatmode.md` | WiX packaging, prerequisite validation | `.github/instructions/installer.instructions.md`, `FLExInstaller/**`, `specs/007-wix-314-installer` |
| FieldWorks Tech Writer | `.github/chatmodes/technical-writer.chatmode.md` | COPILOT.md upkeep, wiki migrations, specs | `.github/instructions/technical-writer.instructions.md`, `.github/src-catalog.md` |
| Autonomous Coding Agent | `.github/chatmodes/coding-agent.chatmode.md` | Full-stack changes end-to-end (used in CI agents) | `Docs/AI_AGENT_GOVERNANCE.md`, `.github/instructions/*.md` |

### Choosing the right agent

1. **Identify the surface** (managed code, native code, installer, docs).
2. **Pick the matching chatmode** from the table above.
3. **Load context**: read relevant `COPILOT.md`, `specs/` entries, and instructions referenced in the chatmode.
4. **Escalate**: if a task crosses multiple surfaces (e.g., managed ↔ native), coordinate through the coding agent or split the work between specialized agents.

## FieldWorks-Specific Agent Rules

### Build & Test ordering
- Native C++ (Phase 2 of `FieldWorks.proj`) must succeed before managed assemblies build. Let `.\build.ps1` enforce this.
- Managed tests come from NUnit/VSTest. Native tests are driven via `scripts/Agent/Invoke-CppTest.ps1`; use `.\test.ps1 -Native` to stay consistent.

### COM and Registry
- FieldWorks relies on registration-free COM. Do not register COM components globally or edit the Windows registry unless a spec explicitly directs it.
- Update manifests through the established build targets (`Build/RegFree.targets`) and document changes in the relevant `COPILOT.md`.

### Worktrees & Containers
- The repo supports multiple worktrees (`scripts/spin-up-agents.ps1`). Each worktree has its own `.serena/project.yml`.
- When running in VS Code, use the recommended tasks under `.vscode/tasks.json` (already wired to `build.ps1`/`test.ps1`).

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

Always review the local `COPILOT.md` before editing a folder—the files summarize architecture, dependencies, and testing requirements.

## Validation Checklist (All Agents)

1. `.\build.ps1` passes in the relevant configuration (Debug by default).
2. `.\test.ps1` (or targeted variants) pass for affected components.
3. `.\Build\Agent\check-and-fix-whitespace.ps1` reports clean output.
4. Commit messages meet repo guidelines (`Build/Agent/commit-messages.ps1`).
5. Updated documentation (`COPILOT.md`, specs, instructions) where behavior changed.

## Extending or Creating New Agents

1. Draft scope/requirements in `Docs/AI_AGENT_GOVERNANCE.md` (new section).
2. Add a chatmode under `.github/chatmodes/` following the existing template.
3. Update the catalog table above with scope, references, and validation steps.
4. Regenerate any IDE hints (e.g., `.github/copilot-instructions.md` or workspace instructions) if activation steps change.
