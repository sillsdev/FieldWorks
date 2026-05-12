# Implementation Plan: WiX v6 Migration

**Branch**: 001-wix-v6-migration | **Date**: 2025-12-11 | **Spec**: [specs/001-wix-v6-migration/spec.md](specs/001-wix-v6-migration/spec.md)
**Input**: Feature specification from specs/001-wix-v6-migration/spec.md

**Note**: This template is filled in by the /speckit.plan command. See .specify/templates/commands/plan.md for the execution workflow.

## Summary

Finish the WiX 6 installer migration while keeping the WiX 3 path available as a temporary fallback. The current repo still defaults installer builds to WiX 3 through `Build/InstallerBuild.proj`, but the migration target is WiX 6 as the primary/default path once validation gates pass. WiX 6 authoring lives under `FLExInstaller/wix6/` and uses SDK-style projects, MSBuild targets, and in-tree shared code.

To keep the migration maintainable, we also standardize on C# 8 language features for managed code (while keeping nullable reference type analysis disabled initially to avoid warnings-as-errors churn).

Installer testing should prefer clean VM evidence for release decisions. Local development PC runs are useful for fast triage, but they are not enough to sign off upgrade, ARP, uninstall, or offline behavior.

## Transition Plan: Parallel WiX 3 + WiX 6 Installers (NEW)

### Objectives

- Keep the WiX 3 fallback buildable only while WiX 6 validation is incomplete.
- Add a **toolset selection switch** (currently default **WiX 3**, migration target **WiX 6**).
- Keep WiX 3 inputs isolated from WiX 6 schema changes.
- Add a tracked decision point for switching the default to WiX 6.

### Known changes that can break WiX 3 (must be reversed or isolated)

- `FLExInstaller/*.wxi` now contain WiX 4+/v6 namespaces and constructs (breaks WiX 3).
- `Build/Installer.targets` was rewritten for the WiX 6 MSBuild pipeline (WiX 3 batch flow removed).
- Legacy `PatchableInstaller` expectations were removed/quarantined. `PatchableInstaller/` is not present in this worktree; some legacy CI workflows still checkout `sillsdev/genericinstaller` into that path.
- Custom action wiring switched to WiX 4+ binaries (e.g., `Wix4UtilCA_X64`).

### Current legacy fallback inputs

The current worktree has these legacy fallback pieces:

- `Build/Installer.Wix3.targets` (legacy WiX 3 orchestration + batch script invocation)
- `FLExInstaller/*.wxi` (WiX 3-compatible includes, preserved in root)
- No in-tree `PatchableInstaller/` folder. Existing base/patch installer workflows still checkout the external genericinstaller repo for legacy jobs and must be cleaned up or explicitly labeled as legacy.

### Build + Documentation updates

- Add `InstallerToolset=Wix3|Wix6` (current default **Wix3**) to `build.ps1` and `Build/InstallerBuild.proj`.
- Route `BuildInstaller` through the selected imported target and keep `BuildInstallerWix6` as an explicit WiX 6 entry point.
- Move WiX 6 assets to `FLExInstaller/wix6/` (projects + shared authoring) and keep WiX 3 in root.
- Split WiX 3 vs WiX 6 include paths (root vs `FLExInstaller/wix6/Shared/`) to avoid toolset conflicts.
- Update docs (`ReadMe.md`, `specs/001-wix-v6-migration/quickstart.md`, `FLExInstaller/COPILOT.md`) to describe WiX 6 as the migration target and WiX 3 as fallback.
- Update CI to add a WiX 6 installer lane with artifact upload, and retire or explicitly label legacy genericinstaller-dependent jobs.

## Technical Context

**Language/Version**: WiX Toolset v6, MSBuild (VS 2022), C# 8 (Custom Actions; nullable analysis disabled initially)
**Primary Dependencies**: WixToolset.Sdk, WixToolset.UI.wixext, WixToolset.Util.wixext, WixToolset.NetFx.wixext, WixToolset.Bal.wixext
**Storage**: N/A (Installer)
**Testing**: Snapshot/compare-based evidence collection; clean VM runs for release gates; local development PC runs for triage; WixToolset.Heat v6 for harvesting
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
- **VI. Documentation Fidelity**: `FLExInstaller` docs and this spec must describe the actual WiX 6 route, artifact paths, and legacy fallback state.

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

Build/
 Installer.targets         # WiX 6 targets
 Installer.Wix3.targets    # WiX 3 targets
 ...
`

**Structure Decision**: Keep WiX 3 authoring in `FLExInstaller/` root as fallback and WiX 6 authoring under `FLExInstaller/wix6/`. `PatchableInstaller/` is not restored in-tree; any remaining genericinstaller dependency must be migrated into WiX 6 or isolated in explicitly legacy CI jobs.

## Complexity Tracking

N/A - No constitution violations.

## Installer Verification

Use local development PC validation for fast feedback, but use clean VM evidence for release-critical claims: WiX 3 to WiX 6 upgrade, ARP entry count, uninstall cleanup, online prerequisite behavior, and offline disconnected installs. Evidence capture follows the log collection conventions in `verification-matrix.md` and `golden-install-checklist.md`.
