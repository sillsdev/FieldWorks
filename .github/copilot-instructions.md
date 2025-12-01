# FieldWorks Copilot Instructions

## Purpose & Scope
- Give Copilot agents a fast, reliable playbook for FieldWorks—what the repo contains, how to build/test, and how to keep documentation accurate.
- Assume nothing beyond this file and linked instructions; only search the repo when a referenced step fails or is missing.

## Repository Snapshot
- Product: FieldWorks (FLEx) — Windows-first linguistics suite maintained by SIL International.
- Languages & tech: C#, C++/CLI, native C++, WiX, PowerShell, XML, JSON, XAML/WinForms.
- Tooling: Visual Studio 2022 (Desktop workloads), MSBuild Traversal (`FieldWorks.proj`), WiX 3.11, NUnit-style tests, Crowdin localization.
- Docs: `ReadMe.md` → https://github.com/sillsdev/FwDocumentation/wiki for deep dives; `.github/src-catalog.md` + per-folder `COPILOT.md` describe Src/ layout.

## Core Rules
- Prefer `./build.ps1` or `FieldWorks.sln` builds; avoid ad-hoc project builds that skip traversal ordering.
- Run tests relevant to your change before pushing; do not assume CI coverage.
- Keep localization via `.resx` and respect `crowdin.json`; never hardcode translatable strings.
- Avoid COM/registry edits without a test plan and container-safe execution (see `scripts/spin-up-agents.ps1`).
- Stay within documented tooling—no surprise dependencies or scripts without updating instructions.

## Build & Test Essentials
- Prerequisites: install VS 2022 Desktop workloads, WiX 3.11.x, Git, and optional Crowdin CLI only when needed.
- Common commands:
  ```powershell
  # Full traversal build (Debug/x64 defaults)
  .\build.ps1

  # Direct MSBuild
  msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /m

  # Targeted native rebuild
  msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64
  ```
- Tests: follow `.github/instructions/testing.instructions.md`; use VS Test Explorer, `dotnet test`, or `nunit3-console` depending on the project.
- Installer edits must follow `.github/instructions/installer.instructions.md` plus WiX validation before PR.

## Workflow Shortcuts
| Task | Reference |
| --- | --- |
| Build/test rules | `.github/instructions/build.instructions.md`, `.github/instructions/testing.instructions.md` |
| Managed / Native / Installer guidance | `.github/instructions/managed.instructions.md`, `.github/instructions/native.instructions.md`, `.github/instructions/installer.instructions.md` |
| Security & PowerShell rules | `.github/instructions/security.instructions.md`, `.github/instructions/powershell.instructions.md` |
| **Serena MCP (symbol tools)** | `.github/instructions/serena.instructions.md` |
| Prompts & specs | `.github/prompts/*.prompt.md`, `.github/spec-templates/`, `.github/recipes/` |
| Chat modes | `.github/chatmodes/*.chatmode.md` |

## Instruction & Prompt Expectations
- Instruction files live under `.github/instructions/` with `applyTo`, `name`, and `description` frontmatter only; keep content ≤ 200 lines with Purpose/Scope, Key Rules, Examples.
- Use `.github/prompts/revise-instructions.prompt.md` for any instruction or COPILOT refresh; it covers detect → propose → validate.
- Chat modes constrain role-specific behavior (managed/native/installer/technical-writer) and should be referenced when invoking Copilot agents.

## COPILOT.md Maintenance
1. **Detect** stale folders: `python .github/detect_copilot_needed.py --strict --base origin/<branch> --json .cache/copilot/detect.json`.
2. **Plan** diffs + reference groups: `python .github/plan_copilot_updates.py --detect-json .cache/copilot/detect.json --out .cache/copilot/diff-plan.json`.
3. **Scaffold** (optional) when a file drifts from the canonical layout: `python .github/scaffold_copilot_markdown.py --folders Src/<Folder>`.
4. **Apply** the auto change-log from the planner: `python .github/copilot_apply_updates.py --plan .cache/copilot/diff-plan.json --folders Src/<Folder>`.
5. **Edit narrative sections** using the planner JSON (change counts, commit log, `reference_groups`), keeping human guidance short and linking to subfolder docs where possible.
6. **Validate** with `python .github/check_copilot_docs.py --only-changed --fail` (or use `--paths Src/Foo/COPILOT.md` for targeted checks).
7. When documentation exceeds ~200 lines or acts as a parent index, migrate to `.github/templates/organizational-copilot.template.md` plus `.github/instructions/organizational-folders.instructions.md`.
8. Run `.github/prompts/copilot-folder-review.prompt.md` with the updated plan slice to simulate Copilot review before committing.

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

## Containers & Agent Worktrees
- Paths containing `\worktrees\agent-<N>` must build inside Docker container `fw-agent-<N>`: `docker exec fw-agent-<N> powershell -NoProfile -c "msbuild <solution> /m /p:Configuration=Debug"`.
- Never run MSBuild directly on the host for agent worktrees; COM/registry access must stay containerized.
- Prefer VS Code tasks (Restore + Build Debug) inside worktrees; only use host for read-only git/file operations.
- For the main repo checkout, run `.\build.ps1 -Configuration Debug` or `msbuild dirs.proj` directly.

## Where to Make Changes
- Source: `Src/` contains managed/native projects—mirror existing patterns and keep tests near the code (`Src/<Component>.Tests`).
- Installer: `FLExInstaller/` with WiX artifacts.
- Shared headers/libs: `Include/`, `Lib/` (avoid committing large binaries unless policy allows).
- Localization: update `.resx` files; never edit `crowdin.json` unless you understand Crowdin flows.
- Build infrastructure: `Build/` + `Bld/` orchestrate targets/props—change sparingly and document impacts.

## Confidence Checklist
- [ ] Prefer traversal builds over per-project compile hacks.
- [ ] Keep coding style aligned with `.editorconfig` and existing patterns.
- [ ] Validate installer/localization changes before PR.
- [ ] Record uncertainties with `FIXME(<topic>)` and resolve them when evidence is available.
- [ ] Refer back to this guide whenever you need repo-wide ground truth.

## Maintaining Instruction Tooling
- Run `python scripts/tools/update_instructions.py` after editing instruction files to refresh `inventory.yml`, regenerate `manifest.json`, and execute the structural validator.
- Use the VS Code tasks (`COPILOT: Detect updates needed`, `COPILOT: Propose updates for changed folders`, `COPILOT: Validate COPILOT docs`) or ship the same commands via terminal for consistency.
