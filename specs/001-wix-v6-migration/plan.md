# Implementation Plan: WiX v6 Migration

**Branch**: 001-wix-v6-migration | **Date**: 2025-12-11 | **Spec**: [specs/001-wix-v6-migration/spec.md](specs/001-wix-v6-migration/spec.md)
**Input**: Feature specification from specs/001-wix-v6-migration/spec.md

**Note**: This template is filled in by the /speckit.plan command. See .specify/templates/commands/plan.md for the execution workflow.

## Summary

Migrate the FieldWorks installer from WiX 3.11 to WiX Toolset v6. This involves converting project files to SDK-style, replacing the genericinstaller submodule with in-tree code, modernizing the build process to use MSBuild targets instead of batch files, and ensuring functional parity including offline support, dual-directory UI, and seamless upgrades.

## Technical Context

**Language/Version**: WiX Toolset v6, MSBuild (VS 2022), C# (Custom Actions)
**Primary Dependencies**: WixToolset.Sdk, WixToolset.UI.wixext, WixToolset.Util.wixext, WixToolset.NetFx.wixext, WixToolset.Bal.wixext
**Storage**: N/A (Installer)
**Testing**: Manual verification (VMs), CI build verification, WixToolset.Heat for harvesting (if used)
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
- **VI. Documentation Fidelity**: COPILOT.md in FLExInstaller and PatchableInstaller must be updated.

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
 *.wxs                # Main installer source (to be migrated)
 *.wxi                # Includes
 ...

PatchableInstaller/    # Likely to be consolidated or refactored
 BaseInstallerBuild/
 ...

Build/
 Installer.targets    # New MSBuild targets for WiX v6
 ...
`

**Structure Decision**: The existing FLExInstaller directory will remain the primary source. PatchableInstaller contents (specifically BaseInstallerBuild and CustomActions) will be analyzed and migrated into FLExInstaller or a new Installer.Shared project to remove the genericinstaller dependency.

## Complexity Tracking

N/A - No constitution violations.
