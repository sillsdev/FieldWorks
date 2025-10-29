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

--------------------------------------------------------------------------------

## Repository layout (focused)

Top-level items you’ll use most often:

- .editorconfig — code formatting rules
- .github/ — GitHub workflows and configuration (CI runs from here)
- Build/ — build scripts, targets, and shared build infrastructure
- DistFiles/ — packaging inputs and distribution artifacts
- FLExInstaller/ — WiX-based installer project
- Include/ — shared headers/includes for native components
- Lib/ — third-party or prebuilt libraries used by the build
- Src/ — all application, library, and tooling source (see .github/src-catalog.md for folder descriptions; each folder has a COPILOT.md file with detailed documentation)
- TestLangProj/ — test data/projects used by tests and integration scenarios
- ReadMe.md — links to developer documentation wiki
- License.htm — license information
- fw.code-workspace — VS Code workspace settings

Src/ folder structure:
- For a quick overview of all Src/ folders, see `.github/src-catalog.md`
- For detailed information about any specific folder, see its `Src/<FolderName>/COPILOT.md` file
- Each COPILOT.md contains: purpose, key components, dependencies, build/test information, and relationships to other folders

Tip: Use the top-level solution or build scripts instead of building projects individually; this avoids dependency misconfiguration.

--------------------------------------------------------------------------------

## Build, test, run, lint

Always start from a clean environment, then follow the steps below. If a step fails, do not probe randomly; re-read this section and the developer docs wiki linked above.

Prerequisites (Windows development machine):
- Visual Studio 2022 (or 2019 if indicated by the developer docs) with workloads:
  - .NET desktop development
  - Desktop development with C++
  - MSBuild Tools
  - Windows 10/11 SDK
- WiX Toolset 3.11.x (required for installer under FLExInstaller)
- Git
- Crowdin CLI is not required to build but is used for localization sync

Bootstrap:
1) Open a “Developer Command Prompt for VS” to ensure MSBuild/SDKs are on PATH.
2) Ensure any required environment variables are set. When using script-based builds, source the appropriate environment file:
   - For Bash: source ./environ
   - Some build targets may require: source ./environ-xulrunner or ./environ-other
3) Restore dependencies:
   - Managed code: NuGet restore is usually handled automatically by MSBuild for SDK-style projects. If needed: nuget restore <Solution>.sln
   - Native deps: Ensure Lib/ and Include/ contents are present (committed in repo).

Build (preferred approaches):
- Scripted CI-style build (repeatable, fewer surprises):
  - Bash (CI/local Bash on Windows such as Git Bash):
    - source ./environ
    - bash ./agent-build-fw.sh
  - Notes:
    - This script encapsulates the CI build. Use it to mirror CI locally.
    - If it references other scripts under Build/ or Bld/, allow them to run unchanged.
- Visual Studio/MSBuild build:
  - Open the main solution in Visual Studio (look for FW.sln at repo root or solutions under Src).
  - Build Configurations: Debug or Release for x86/x64 as needed.
  - Command line:
    - msbuild <PathToSolution.sln> /m /p:Configuration=Debug
    - msbuild <PathToSolution.sln> /m /p:Configuration=Release

Testing:
- Unit/integration tests are typically under Src/… with names ending in .Tests or similar.
- In Visual Studio: Test Explorer -> Run All
- Command line (typical patterns):
  - If tests are SDK-style: dotnet test <PathToSolutionOrTestProj> -c Debug
  - If NUnit 3 is used with .NET Framework, run via NUnit Console:
    - packages/NUnit.ConsoleRunner*/tools/nunit3-console.exe <TestAssembly>.dll
  - Prefer running tests through the same mechanism used by CI (see .github/workflows).

Run (local application debug):
- Launchable applications reside in Src/ under app projects. Set the desired app as startup project in Visual Studio and F5.
- Some apps may depend on native components; ensure the environment (PATH) includes Bin/ and relevant build outputs.

Lint/format/style:
- .editorconfig enforces formatting; configure VS to respect it.
- JetBrains settings (FW.sln.DotSettings) define ReSharper rules; follow warnings.
- If any Roslyn analyzers are included in csproj, build will surface them.

Known gotchas and guidance:
- Always run the environment setup (source ./environ) before script-based builds; missing env vars are a common cause of msbuild/wix failures.
- Installer/packaging (FLExInstaller) requires WiX; skip installer builds for quick iteration unless changing installer logic.
- Localization: crowdin.json indicates strings are synchronized externally. Avoid hardcoding user-visible strings; follow existing localization patterns.
- Native code link errors often indicate missing Include/ or Lib/ configuration—build via the top-level scripts/solutions to ensure correct paths/props are loaded.

Timing:
- This is a large solution; first-time builds can be lengthy. Prefer incremental builds for inner-loop development. Avoid cleaning unless necessary.

--------------------------------------------------------------------------------

## CI and validation

- GitHub Actions are defined under .github/workflows/. Pull Requests trigger validation builds and tests.
- To replicate CI locally:
  - Use: source ./environ && bash ./agent-build-fw.sh
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

--------------------------------------------------------------------------------

## Confidence checklist for agents

- Prefer the top-level build flow (agent-build-fw.sh or solution-wide MSBuild) over piecemeal project builds.
- Always initialize environment via ./environ before script-based builds.
- Validate with tests in Visual Studio or via the same runners CI uses.
- Keep coding style consistent (.editorconfig, ReSharper settings).
- Touch installer/localization only when necessary, and validate those paths explicitly.
- Trust this guide; only search the repo if a command here fails or a path is missing.
--------------------------------------------------------------------------------

## Maintaining Src/ Folder Documentation

Each folder under Src/ has a COPILOT.md file that documents its purpose, components, and relationships. These files are essential for understanding the codebase.

**When to update COPILOT.md files:**
- When making significant architectural changes to a folder
- When adding new major components or subprojects
- When changing the purpose or scope of a folder
- When discovering discrepancies between documentation and reality

**How to update COPILOT.md files:**
1. Read the existing COPILOT.md file for the folder you're working in
2. If you notice discrepancies (e.g., missing components, outdated descriptions, incorrect dependencies):
   - Update the COPILOT.md file to reflect the current state
   - Update cross-references in related folders' COPILOT.md files if relationships changed
   - Update `.github/src-catalog.md` with the new concise description
3. Keep documentation concise but informative:
   - Purpose: What the folder is for (1-2 sentences)
   - Key Components: Major files, subprojects, or features
   - Technology Stack: Primary languages and frameworks
   - Dependencies: What it depends on and what uses it
   - Build Information: How to build and test
   - Entry Points: How the code is used or invoked
   - Related Folders: Cross-references to other Src/ folders

**Example scenarios requiring COPILOT.md updates:**
- Adding a new C# project to a folder → Update "Key Components" and "Build Information"
- Discovering a folder depends on another folder not listed → Update "Dependencies" and "Related Folders"
- Finding that a folder's description is inaccurate → Update "Purpose" section
- Adding new test projects → Update "Build Information" and "Testing" sections

Always validate that your code changes align with the documented architecture. If they don't, either adjust your changes or update the documentation to reflect the new architecture.
