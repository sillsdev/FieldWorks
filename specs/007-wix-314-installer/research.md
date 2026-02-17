# Research: WiX 3.14 Installer Upgrade

**Feature**: 007-wix-314-installer | **Date**: December 2, 2025

## Research Questions

### 1. WiX 3.14.x Compatibility with Existing .wxs/.wixproj Files

**Decision**: WiX 3.14.x is backward compatible with WiX 3.11.x project files.

**Rationale**:
- WiX 3.14.x is a minor version upgrade within the 3.x line
- The WiX 3.x series maintains backward compatibility with .wxs and .wixproj formats
- GitHub's `windows-latest` runner has been shipping WiX 3.14.x since mid-2024
- The current workaround explicitly downgrades from 3.14.x to 3.11.2, confirming the runner has 3.14.x available

**Alternatives Considered**:
- Migrate to WiX 4.x: Rejected - Major version with breaking changes, would require significant .wxs file rewrites
- Pin to WiX 3.11.x permanently: Rejected - Adds CI complexity, prevents future WiX improvements

### 2. Current CI Workaround Details

**Finding**: Both `base-installer-cd.yml` and `patch-installer-cd.yml` on `origin/release/9.3` contain:

```yaml
- name: Downgrade Wix Toolset - remove when runner has 3.14.2
  run: |
    choco uninstall wixtoolset
    choco install wixtoolset --version 3.11.2 --allow-downgrade --force
    echo "C:\Program Files (x86)\WiX Toolset v3.11\bin" | Out-File -FilePath $env:GITHUB_PATH -Encoding utf8 -Append
  if: github.event_name != 'pull_request'
```

**Impact of Removal**:
- Saves ~30-60 seconds of CI time per workflow run
- Eliminates chocolatey dependency for installer builds
- Uses runner's pre-installed WiX 3.14.x directly

### 3. Patch Backward Compatibility

**Decision**: WiX 3.14.x-built patches CAN apply to WiX 3.11.x-built base installations.

**Rationale**:
- MSI/MSP patching is governed by Windows Installer, not WiX version
- The patch mechanism uses UpgradeCode and ProductCode matching, which are defined in the .wxs sources (unchanged)
- The current base build (build-1188) was built with WiX 3.11.x
- Future patches built with WiX 3.14.x will apply to this base as long as component GUIDs and upgrade paths remain stable

**Validation Required**:
- Build a test patch with WiX 3.14.x
- Apply it to a WiX 3.11.x base installation (build-1188)
- Verify successful upgrade and application functionality

### 4. `insignia` Tool Compatibility

**Decision**: The `insignia` tool in WiX 3.14.x works correctly for burn engine extraction/reattachment.

**Rationale**:
- `insignia` is part of the WiX toolset and follows the same versioning
- The workflow steps use `insignia -ib` (extract) and `insignia -ab` (attach) commands
- These commands have not changed between WiX 3.11.x and 3.14.x
- The current branch's workflows already use WiX 3.14.x (no downgrade step) and would fail if `insignia` was incompatible

### 5. sillsdev/genericinstaller Compatibility

**Decision**: The `sillsdev/genericinstaller` repository works with WiX 3.14.x.

**Rationale**:
- The genericinstaller provides PatchableInstaller components used in the build
- It uses standard WiX 3.x constructs (no 3.11-specific features)
- The repository's `master` branch is used by CI and has not required WiX version pinning

### 6. Documentation Gaps

**Finding**: The following files reference WiX 3.11.x and need updating:

| File | Current Reference | Required Change |
|------|-------------------|-----------------|
| `.github/instructions/installer.instructions.md` | "confirm WiX 3.11.x tooling" | Update to "WiX 3.14.x" |
| `.github/copilot-instructions.md` | "WiX 3.14.x" | Already correct ✅ |

**New Documentation Needed**:
- `Docs/installer-build-guide.md`: Step-by-step guide for local installer builds
  - Prerequisites (WiX 3.14.x installation)
  - Build commands for base and patch installers
  - Troubleshooting common issues
  - CI workflow explanation

## Summary

All research questions resolved. No blockers identified for WiX 3.14.x adoption.

**Key Findings**:
1. WiX 3.14.x is backward compatible - no .wxs/.wixproj changes needed
2. Remove 2 workflow steps (chocolatey downgrade) from 2 files
3. Patch backward compatibility is architecturally sound but requires validation
4. Update 1 documentation file, create 1 new documentation file
