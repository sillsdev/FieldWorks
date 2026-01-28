# Implementation Plan: WiX 3.14 Installer Upgrade

**Branch**: `007-wix-314-installer` | **Date**: December 2, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-wix-314-installer/spec.md`

## Summary

Remove the WiX 3.11.2 downgrade workaround from CI workflows and validate that FieldWorks installers (base and patch) build correctly using the pre-installed WiX 3.14.x on `windows-latest` GitHub runners. Update documentation to reflect WiX 3.14.x as the current tooling version.

## Technical Context

**Language/Version**: WiX 3.14.x, PowerShell 5.1+, GitHub Actions YAML
**Primary Dependencies**: WiX Toolset 3.14.x (pre-installed on windows-latest), MSBuild, sillsdev/genericinstaller
**Storage**: N/A (installer artifacts published to S3 and GitHub Releases)
**Testing**: Manual validation of installer execution; CI workflow success verification
**Target Platform**: Windows 10/11 (x64), GitHub Actions `windows-latest` runners
**Project Type**: Build infrastructure / CI configuration
**Performance Goals**: CI workflow execution time decrease ≥30s (chocolatey downgrade removal)
**Constraints**: Backward compatibility with WiX 3.11.x-built base installers (patches must apply)
**Scale/Scope**: 2 CI workflows, 3 documentation files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: ✅ PASS - Installers do not modify data schemas. User data preservation is existing MSI/WiX behavior and will be validated during testing.
- **Test evidence**: ✅ PASS - CI workflow success (build completion, artifact production, `insignia` burn engine extraction/reattachment) serves as the required automated test evidence per Constitution Principle II. Manual validation of installer execution on Windows 10/11 provides additional integration testing before merge.
- **I18n/script correctness**: ✅ N/A - No text processing or rendering changes; installer localization is unchanged.
- **Licensing**: ✅ PASS - WiX 3.14.x uses MS-RL (Microsoft Reciprocal License), same as WiX 3.11.x. No new dependencies introduced.
- **Stability/performance**: ✅ LOW RISK - WiX 3.14.x is the stable version on GitHub runners; removal of downgrade step simplifies CI. Staged rollout via feature branch testing.

## Project Structure

### Documentation (this feature)

```text
specs/007-wix-314-installer/
├── plan.md              # This file
├── research.md          # Phase 0 output - WiX 3.14 compatibility findings
├── data-model.md        # Phase 1 output - N/A (no data model changes)
├── quickstart.md        # Phase 1 output - Local installer build guide
├── contracts/           # Phase 1 output - N/A (no API contracts)
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# Files to modify
.github/workflows/
├── base-installer-cd.yml    # Remove WiX downgrade step
└── patch-installer-cd.yml   # Remove WiX downgrade step

.github/instructions/
└── installer.instructions.md  # Update WiX version reference

.github/
└── AGENTS.md    # Already references 3.14.x (no change needed)

# Files to create/update for documentation
Docs/
└── installer-build-guide.md   # New: Local installer build documentation
```

**Structure Decision**: This is a CI/documentation change with no application code modifications. Changes are limited to workflow YAML files and instruction markdown files.

## Complexity Tracking

> No Constitution Check violations requiring justification. All gates pass.
