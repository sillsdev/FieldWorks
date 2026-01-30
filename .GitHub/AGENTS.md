# FieldWorks Agentic Instructions

## Purpose & Scope
- Give AI coding agents a fast, reliable playbook for FieldWorks—what the repo contains, how to build/test, and how to keep documentation accurate.
- Assume nothing beyond this file and linked instructions; only search the repo when a referenced step fails or is missing.

See `.github/AI_GOVERNANCE.md` for the documentation taxonomy and “source of truth” rules.

## Repository Snapshot
- Product: FieldWorks (FLEx) — Windows-first linguistics suite maintained by SIL International.
- Languages & tech: C#, C++/CLI, native C++, WiX, PowerShell, XML, JSON, XAML/WinForms.
- Tooling: Visual Studio 2022 (Desktop workloads), MSBuild Traversal (`FieldWorks.proj`), WiX 3.14.x, NUnit-style tests, Crowdin localization.
- Docs: `ReadMe.md` → https://github.com/sillsdev/FwDocumentation/wiki for deep dives; `.github/src-catalog.md` + per-folder `AGENTS.md` describe Src/ layout.

## Core Rules
- Prefer `./build.ps1`; avoid ad-hoc project builds that skip traversal ordering.
- Run tests relevant to your change before pushing; do not assume CI coverage.
- Keep localization via `.resx` and respect `crowdin.json`; never hardcode translatable strings.
- Avoid COM/registry edits without a test plan.
- Stay within documented tooling—no surprise dependencies or scripts without updating instructions.
- **Terminal commands**: **ALWAYS use `scripts/Agent/` wrapper scripts** for git or file reading requiring pipes/filters. See `.github/instructions/terminal.instructions.md` for the transformation table.

## Build & Test Essentials
- Prerequisites: install VS 2022 Desktop workloads, WiX 3.14.x (pre-installed on windows-latest), Git, LLVM/clangd + standalone OmniSharp (for Serena C++/C# support), and optional Crowdin CLI only when needed.
- Verify your environment: `.\Build\Agent\Verify-FwDependencies.ps1 -IncludeOptional`
- Common commands:
  ```powershell
  # Full traversal build (Debug/x64 defaults)
  .\build.ps1

  # Run tests
  .\test.ps1
  ```
- Tests: follow `.github/instructions/testing.instructions.md`; use VS Test Explorer or `vstest.console.exe` for managed tests.
- Installer edits must follow `.github/instructions/installer.instructions.md` plus WiX validation before PR.
- Installer builds: use `.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly` to check prerequisites, `-SetupPatch` for patch builds.

## Workflow Shortcuts
| Task | Reference |
| --- | --- |
| Build/test rules | `.github/instructions/build.instructions.md`, `.github/instructions/testing.instructions.md` |
| Debugging | `.github/instructions/debugging.instructions.md` |
| Managed / Native / Installer guidance | `.github/instructions/managed.instructions.md`, `.github/instructions/native.instructions.md`, `.github/instructions/installer.instructions.md` |
| Security & PowerShell rules | `.github/instructions/security.instructions.md`, `.github/instructions/powershell.instructions.md` |
| Guidance governance | `.github/AI_GOVERNANCE.md` |
| **Agent wrapper scripts** | `scripts/Agent/` - build, test, and git helpers for auto-approval |
| Prompts & specs | `.github/prompts/*.prompt.md`, `.github/spec-templates/`, `.github/recipes/` |
| Chat modes | `.github/chatmodes/*.chatmode.md` |

## Instruction & Prompt Expectations
- Instruction files live under `.github/instructions/` with `applyTo`, `name`, and `description` frontmatter only; keep content ≤ 200 lines with Purpose/Scope, Key Rules, Examples.
- Chat modes constrain role-specific behavior (managed/native/installer/technical-writer) and should be referenced when invoking agents.

**Context7 Guidance:** When requesting API references, code examples, or library-specific patterns, consult Context7 first (for example, call `resolve-library-id` then `get-library-docs` or `search-code`). Prefer the Context7 libraries listed in `.vscode/context7-configuration.json` and include the resolved library ID in your prompt when possible. Context7 lookups are considered safe and are configured for auto-approval in this workspace.

## AGENTS.md Maintenance
1. **Detect** stale folders: `python .github/detect_copilot_needed.py --strict --base origin/<branch> --json .cache/copilot/detect.json`.
2. **Plan** diffs + reference groups: `python .github/plan_copilot_updates.py --detect-json .cache/copilot/detect.json --out .cache/copilot/diff-plan.json`.
3. **Scaffold** (optional) when a file drifts from the canonical layout: `python .github/scaffold_copilot_markdown.py --folders Src/<Folder>`.
4. **Apply** the auto change-log from the planner: `python .github/copilot_apply_updates.py --plan .cache/copilot/diff-plan.json --folders Src/<Folder>`.
5. **Edit narrative sections** using the planner JSON (change counts, commit log, `reference_groups`), keeping human guidance short and linking to subfolder docs where possible.
6. **Validate** with `python .github/check_copilot_docs.py --only-changed --fail` (or use `--paths Src/Foo/AGENTS.md` for targeted checks).
7. When documentation exceeds ~200 lines or acts as a parent index, migrate to `.github/templates/organizational-copilot.template.md` and keep the parent doc as a navigation index.
8. Run `.github/prompts/copilot-folder-review.prompt.md` with the updated plan slice to simulate an agent review before committing.

## CI & Validation Requirements
- GitHub Actions workflows live under `.github/workflows/`; keep them passing.
- Local parity checks:
  ```powershell
  # Commit messages (gitlint)
  python -m pip install --upgrade gitlint
  git fetch origin
  gitlint --ignore body-is-missing --commits origin/<base>..

  # Whitespace
  git log --check --pretty=format:"---% h% s" origin/<base>..
  git diff --check --cached
  ```
- Before PRs, ensure:
  - Build + relevant tests succeed locally.
  - Installer/config changes validated with WiX tooling.
  - Analyzer/lint warnings addressed.

### Build & Test Commands (ALWAYS use the scripts)
```powershell
# Build
.\build.ps1
.\build.ps1 -Configuration Release
.\build.ps1 -BuildTests

# Test
.\test.ps1
.\test.ps1 -TestFilter "TestCategory!=Slow"
.\test.ps1 -TestProject "Src/Common/FwUtils/FwUtilsTests"
.\test.ps1 -NoBuild  # Skip build, use existing binaries

# Both scripts automatically:
# - Clean stale obj/ folders and conflicting processes
# - Set up VS environment
```

**DO NOT** use raw `msbuild` directly - let the scripts handle it.

## Where to Make Changes
- Source: `Src/` contains managed/native projects—mirror existing patterns and keep tests near the code (`Src/<Component>.Tests`).
- Installer: `FLExInstaller/` with WiX artifacts.
- Shared headers/libs: `Include/`, `Lib/` (avoid committing large binaries unless policy allows).
- Localization: update `.resx` files; never edit `crowdin.json` unless you understand Crowdin flows.
- Build infrastructure: `Build/` + `Bld/` orchestrate targets/props—change sparingly and document impacts.

## JIRA Integration

**LT-prefixed tickets** (e.g., `LT-22382`) are JIRA issues from `https://jira.sil.org/`.

⚠️ **NEVER browse to `jira.sil.org` URLs** - requires authentication. **ALWAYS use Python scripts:**

```powershell
# Get issue details (inline Python)
python -c "import sys; sys.path.insert(0, '.github/skills/atlassian-readonly-skills/scripts'); from jira_issues import jira_get_issue; print(jira_get_issue('LT-22382'))"

# Or export your assigned issues to JSON
python .github/skills/jira-to-beads/scripts/export_jira_assigned.py
```

| Scenario | Skill |
|----------|-------|
| Read issue details | `atlassian-readonly-skills` (default) |
| Create/update/comment | `atlassian-skills` (only when user explicitly requests) |
| Bulk import to Beads | `jira-to-beads` |

See `/AGENTS.md` → "Atlassian / JIRA Skills" section for full configuration and details.

## Confidence Checklist
- [ ] Prefer traversal builds over per-project compile hacks.
- [ ] Keep coding style aligned with `.editorconfig` and existing patterns.
- [ ] Validate installer/localization changes before PR.
- [ ] Record uncertainties with `FIXME(<topic>)` and resolve them when evidence is available.
- [ ] Refer back to this guide whenever you need repo-wide ground truth.



