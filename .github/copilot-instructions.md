# Copilot coding agent onboarding guide

This document gives you the shortest path to understand, build, test, and validate changes in this repository without exploratory searching. Trust these instructions; only search the repo if a step here is incomplete or produces an error.

--------------------------------------------------------------------------------

## What this repository is

FieldWorks (often referred to as FLEx) is a large, Windows-focused linguistics and language data management suite developed by SIL International. It includes a mix of C#/.NET managed code and native C++/C++‑CLI components, UI applications, shared libraries, installers, test assets, and localization resources.

High-level facts:
- Project type: Large mono-repo with multiple applications/libraries and an installer
- Primary OS target: Windows
- Languages likely present: C#, C++/CLI, C++, XAML/WinForms, XML, WiX, scripts (PowerShell/Bash), JSON
- Tooling and runtimes:
  - Visual Studio (C#/.NET Framework and Desktop C++ workloads)
  - MSBuild
  - WiX Toolset for installer (presence of FLExInstaller)
  - NUnit-style test projects are common in SIL repos (expect unit/integration tests)
  - Crowdin localization (crowdin.json present)

Documentation:
- Root ReadMe directs to developer docs wiki:
  - Developer documentation: https://github.com/sillsdev/FwDocumentation/wiki

Repo size and structure:
- This is a large codebase with many projects. Prefer building via the provided build scripts or top-level solutions instead of ad-hoc builds of individual projects.

## Workflow quick links

| Focus | Primary resources |
| --- | --- |
| Build & test | `.github/instructions/build.instructions.md`, FieldWorks.sln |
| Managed code rules | `.github/instructions/managed.instructions.md`, `.github/chatmodes/managed-engineer.chatmode.md` |
| Native code rules | `.github/instructions/native.instructions.md`, `.github/chatmodes/native-engineer.chatmode.md` |
| Installer work | `.github/instructions/installer.instructions.md`, `.github/chatmodes/installer-engineer.chatmode.md` |
| Documentation upkeep | `.github/update-copilot-summaries.md` (three-pass workflow), COPILOT VS Code tasks |
| Specs & plans | `.github/prompts/`, `.github/spec-templates/`, `.specify/templates/` |

--------------------------------------------------------------------------------

## Repository layout (focused)

Top-level items you’ll use most often:

- .editorconfig — code formatting rules
- .github/ — GitHub workflows and configuration (CI runs from here)
- Build/ — build scripts, targets, and shared build infrastructure
- DistFiles/ — packaging inputs and distribution artifacts
- Downloads/ - pdb and symbol files for debugging
- FLExInstaller/ — WiX-based installer project
- Include/ — shared headers/includes for native components
- Lib/ — third-party or prebuilt libraries used by the build
- Output/ — build output folders (binaries, logs, etc.)
- packages/ — NuGet packages restored during build
- Src/ — all application, library, and tooling source (see .github/src-catalog.md for folder descriptions; each folder has a COPILOT.md file with detailed documentation)
- TestLangProj/ — test data/projects used by tests and integration scenarios
- ReadMe.md — links to developer documentation wiki
- License.htm — license information
- fw.code-workspace — VS Code workspace settings

Src/ folder structure:
- For a quick overview of all Src/ folders and subfolders, see `.github/src-catalog.md`
- For detailed information about any specific folder, see its `Src/<FolderName>/COPILOT.md` file
- Some folders (Common, LexText, Utilities, XCore) have subfolders, each with their own COPILOT.md file (e.g., `Src/Common/Controls/COPILOT.md`)
- Each COPILOT.md contains: purpose, key components, dependencies, build/test information, and relationships to other folders

Tip: Use the top-level solution or build scripts instead of building projects individually; this avoids dependency misconfiguration.

--------------------------------------------------------------------------------

## AI agent entry points

Use these pre-scoped instructions and modes to keep agents focused and reliable:

- Instructions (domain-specific rules):
  - Managed (C# and .NET): `.github/instructions/managed.instructions.md`
  - Native (C++ and C++/CLI): `.github/instructions/native.instructions.md`
  - Installer (WiX): `.github/instructions/installer.instructions.md`
  - Testing: `.github/instructions/testing.instructions.md`
  - Build: `.github/instructions/build.instructions.md`
- Chat modes (role boundaries):
  - Managed engineer: `.github/chatmodes/managed-engineer.chatmode.md`
  - Native engineer: `.github/chatmodes/native-engineer.chatmode.md`
  - Installer engineer: `.github/chatmodes/installer-engineer.chatmode.md`
  - Technical writer: `.github/chatmodes/technical-writer.chatmode.md`
- Context helpers and memory:
  - High-signal context links: `.github/context/codebase.context.md`
  - Repository memory (decisions/pitfalls): `.github/memory.md`

Machine / user specific instructions are available in `.github/machine-specific.md`.  The file can be created if needed.

--------------------------------------------------------------------------------

## CI checks you must satisfy

These run on every PR. Run the quick checks locally before pushing to avoid churn.

**Commit messages (gitlint)**
- Subject ≤ 72 chars, no trailing punctuation, no tabs/leading/trailing whitespace.
- If you include a body: add a blank line after the subject; body lines ≤ 80 chars.
- Quick check (Windows PowerShell):
  ```powershell
  python -m pip install --upgrade gitlint
  git fetch origin
  # Replace <base> with your target branch (e.g., release/9.3, develop)
  gitlint --ignore body-is-missing --commits origin/<base>..
  ```
- Full rules: see `.github/commit-guidelines.md`

**Whitespace in diffs (git log --check)**
- No trailing whitespace, no space-before-tab in indentation; end files with a newline.
- Quick checks:
  ```powershell
  git fetch origin
  # Review all commits in your PR for whitespace errors
  git log --check --pretty=format:"---% h% s" origin/<base>..
  # Also check staged changes before committing
  git diff --check --cached
  ```
- Configure your editor to trim trailing whitespace and insert a final newline.

**Build and tests**
- Build and test locally before PR to avoid CI failures:
  ```powershell
  # From a Developer Command Prompt for VS or with env set
  msbuild FieldWorks.sln /m /p:Configuration=Debug
  ```
- If you change installer/config, validate those paths explicitly per the sections below.

--------------------------------------------------------------------------------

## Build, test, run, lint

Use the build guides in `.github/instructions/build.instructions.md` for full detail. Key reminders:

- Prerequisites: Visual Studio 2022 with .NET desktop + Desktop C++ workloads, WiX 3.11.x, Git. Install optional tooling (Crowdin CLI, etc.) only when needed.
- Bootstrap: open a Developer Command Prompt, run `source ./environ`, then call FieldWorks.sln with MSBuild/VS.
- Tests: follow `.github/instructions/testing.instructions.md`; run via Visual Studio Test Explorer, `dotnet test`, or `nunit3-console` as appropriate.
- Installer or config changes: execute the WiX validation steps documented in `FLExInstaller` guidance before posting a PR.
- Formatting/localization: respect `.editorconfig`, reuse existing localization patterns, and prefer incremental builds to shorten iteration.

## Agentic workflows (prompts) and specs

- Prompts (agentic workflows): `.github/prompts/`
  - `feature-spec.prompt.md` — spec → plan → implement with validation gates
  - `bugfix.prompt.md` — triage → root cause → minimal fix with gate
  - `test-failure-debug.prompt.md` — parse failures and propose targeted fixes (no file edits)
- Specification templates: `.github/spec-templates/`
  - `spec.md` — problem, approach, components, risks, tests, rollout
  - `plan.md` — implementation plan with gates and rollback
- Recipes/playbooks: `.github/recipes/` — guided steps for common scenarios (e.g., add xWorks dialog, extend Cellar schema)

--------------------------------------------------------------------------------

## CI and validation

- GitHub Actions are defined under .github/workflows/. Pull Requests trigger validation builds and tests.
- To replicate CI locally:
  - Use: `msbuild FieldWorks.sln /m /p:Configuration=Debug 2>&1`
  - Or run the same msbuild/test steps referenced by the workflow YAMLs.
- Pre-merge checklist the CI approximates:
  - Successful build for all targeted configurations
  - Unit tests pass
  - Packaging (if part of CI)
  - Lint/analyzer warnings within policy thresholds

Before submitting a PR:
- Build locally using the CI-style script if possible.
- Run unit tests relevant to your changes.
- If you touched installer/config files, verify the installer build (requires WiX).
- Ensure formatting follows .editorconfig; fix obvious analyzer/lint issues.

--------------------------------------------------------------------------------

## Where to make changes

- Core source: Src/ contains the primary C# and C++ projects. Mirror existing patterns for new code.
- Tests: Keep tests close to the code they cover (e.g., Src/<Component>.Tests). Add or update tests with behavioral changes.
- Installer changes: FLExInstaller/.
- Shared headers/libs: Include/ and Lib/ (be cautious and avoid committing large binaries unless policy allows).
- Localization: Follow existing string resource usage; do not modify crowdin.json.

Dependencies and hidden coupling:
- Some components bridge managed and native layers (C# ↔ C++/CLI ↔ C++). When changing type definitions or interfaces at these boundaries, expect to update both managed and native code and ensure marshaling or COM interop stays correct.
- Build props/targets in Build/ and Bld/ may inject include/lib paths and compiler options; avoid bypassing these by building projects in isolation.

### Documentation discipline
- Follow `.github/update-copilot-summaries.md` and its three-pass workflow whenever code or data changes impact a folder’s documentation.
- Use the COPILOT VS Code tasks (Detect → Propose → Validate) to keep section order canonical.
- Keep `last-reviewed-tree` in each `COPILOT.md` aligned with the folder’s current git tree (`python .github/fill_copilot_frontmatter.py --status draft --ref HEAD`).
- Record uncertainties with `FIXME(<topic>)` markers instead of guessing, and clear them only after verifying against actual sources.

--------------------------------------------------------------------------------

## Confidence checklist for agents

- Prefer the top-level build flow (MSBuild) over piecemeal project builds.
- Always initialize environment via ./environ before script-based builds.
- Validate with tests in Visual Studio or via the same runners CI uses.
- Keep coding style consistent (.editorconfig, ReSharper settings).
- Touch installer/localization only when necessary, and validate those paths explicitly.
- Trust this guide; only search the repo if a command here fails or a path is missing.
--------------------------------------------------------------------------------

## Maintaining Src/ Folder Documentation

Reference `.github/update-copilot-summaries.md` for the canonical skeleton and three-pass workflow. Update the relevant `COPILOT.md` whenever architecture, public contracts, or dependencies change, and leave explicit `FIXME(<topic>)` markers only for facts pending verification.
