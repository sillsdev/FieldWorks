# Implementation Plan: WiX v6 Migration

**Branch**: 001-wix-v6-migration | **Date**: 2025-12-11 | **Spec**: [specs/001-wix-v6-migration/spec.md](specs/001-wix-v6-migration/spec.md)
**Input**: Feature specification from specs/001-wix-v6-migration/spec.md

**Note**: This template is filled in by the /speckit.plan command. See .specify/templates/commands/plan.md for the execution workflow.

## Summary

Run WiX 3 and WiX 6 installers in parallel during a transition period. WiX 3 remains the **default** installer build in the `FLExInstaller/` root, while WiX 6 is moved under `FLExInstaller/wix6/` as an opt-in path. This preserves the release/9.3-compatible layout while continuing the WiX 6 migration work (SDK-style projects, MSBuild targets, and in-tree shared code).

To keep the migration maintainable, we also standardize on C# 8 language features for managed code (while keeping nullable reference type analysis disabled initially to avoid warnings-as-errors churn).

Installer testing will be performed on the **local development PC** only (no Hyper-V or Windows Sandbox lanes).

## Transition Plan: Parallel WiX 3 + WiX 6 Installers (NEW)

### Objectives

- Restore the WiX 3 installer project from `release/9.3` and keep it buildable.
- Add a **toolset selection switch** (default **WiX 3**, opt-in **WiX 6**).
- Keep WiX 3 inputs isolated from WiX 6 schema changes.

### Known changes that can break WiX 3 (must be reversed or isolated)

- `FLExInstaller/*.wxi` now contain WiX 4+/v6 namespaces and constructs (breaks WiX 3).
- `Build/Installer.targets` was rewritten for the WiX 6 MSBuild pipeline (WiX 3 batch flow removed).
- Legacy `PatchableInstaller` expectations were removed/quarantined; WiX 3 requires these inputs.
- Custom action wiring switched to WiX 4+ binaries (e.g., `Wix4UtilCA_X64`).

### Files/folders to pull from `release/9.3`

Pull these **verbatim** from the `release/9.3` worktree to restore WiX 3 support:

- `Build/Installer.targets` (legacy WiX 3 orchestration + batch script invocation)
- `FLExInstaller/*.wxi` (WiX 3-compatible includes, preserved in root)
- `PatchableInstaller/` full tree (BaseInstallerBuild, Common, CustomActions, ProcRunner, CreateUpdatePatch, libs, resources, `Directory.Build.props`, README, `.gitignore`, `.gitattributes`)

### Build + Documentation updates

- Add `InstallerToolset=Wix3|Wix6` (default **Wix3**) to `build.ps1` and `Build/Orchestrator.proj`.
- Add explicit targets: `BuildInstallerWix3` and `BuildInstallerWix6` (with `BuildInstaller` routing to Wix3 by default).
- Move WiX 6 assets to `FLExInstaller/wix6/` (projects + shared authoring) and keep WiX 3 in root.
- Split WiX 3 vs WiX 6 include paths (root vs `FLExInstaller/wix6/Shared/`) to avoid toolset conflicts.
- Update docs (`ReadMe.md`, `specs/001-wix-v6-migration/quickstart.md`, `FLExInstaller/AGENTS.md`) to describe both build paths.
- Update CI to build Wix3 by default, plus an opt-in Wix6 job or flag.

## Technical Context

**Language/Version**: WiX Toolset v6, MSBuild (VS 2022), C# 8 (Custom Actions; nullable analysis disabled initially)
**Primary Dependencies**: WixToolset.Sdk, WixToolset.UI.wixext, WixToolset.Util.wixext, WixToolset.NetFx.wixext, WixToolset.Bal.wixext
**Storage**: N/A (Installer)
**Testing**: Snapshot/compare-based evidence collection on local development PC; WixToolset.Heat for harvesting (if used)
**Target Platform**: Windows (x64/x86)
**Project Type**: Installer (MSI & Bundle)
**Performance Goals**: N/A
**Constraints**: Offline capability, Dual Directory UI (App + Project Data), Major Upgrade support
**Scale/Scope**: ~12 features, multiple locales, ~110 projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Data Integrity**: Installer must perform Major Upgrade to prevent side-by-side version conflicts and potential data corruption. (FR-Upgrade)
- **II. Test and Review Discipline**: Changes to installer require validation. User stories include independent tests for Developer Build, CI Build, Online Install, and Offline Install.
- **III. Internationalization**: Installer must support multiple locales (FR-005).
- **IV. User-Centered Stability**: Offline installation support is critical (FR-004).
- **V. Licensing**: WiX is open source (MS-RL/MIT).
- **VI. Documentation Fidelity**: AGENTS.md in FLExInstaller and PatchableInstaller must be updated.

## Project Structure

### Documentation (this feature)

`	ext
specs/001-wix-v6-migration/
 plan.md              # This file
 research.md          # Phase 0 output
 data-model.md        # Phase 1 output (N/A for installer)
 quickstart.md        # Phase 1 output
 contracts/           # Phase 1 output (N/A for installer)
 tasks.md             # Phase 2 output
`

### Source Code (repository root)

`	ext
FLExInstaller/
 *.wxi                # WiX 3 includes (root, release/9.3-compatible)
 ...

FLExInstaller/wix6/
 FieldWorks.Installer.wixproj
 FieldWorks.Bundle.wixproj
 Shared/
 ...

PatchableInstaller/    # Restored for WiX 3 path (legacy batch pipeline)
 BaseInstallerBuild/
 ...

Build/
 Installer.targets         # WiX 6 targets
 Installer.Wix3.targets    # WiX 3 targets
 ...
`

**Structure Decision**: Keep WiX 3 authoring in `FLExInstaller/` root (minimize release/9.3 diffs) and move WiX 6 authoring under `FLExInstaller/wix6/`. PatchableInstaller is restored in-tree for WiX 3, while WiX 6 continues to use MSBuild + SDK-style projects with in-tree shared code.

## Complexity Tracking

N/A - No constitution violations.

## Installer Verification: Local Development PC Only

Installer validation will be performed on the **local development PC** without sandboxing or Hyper-V. Evidence capture follows the same log collection conventions (bundle/MSI logs and screenshots where needed) but is executed directly on the developer machine.

