# Requirements Checklist: WiX 3.14 Installer Upgrade

## Functional Requirements

| ID | Requirement | Status | Notes |
|----|-------------|--------|-------|
| FR-001 | CI workflows MUST build installers using WiX 3.14.x pre-installed on `windows-latest` runners | ⬜ Not Started | Remove `choco install wixtoolset` steps |
| FR-002 | CI workflows MUST NOT include WiX downgrade steps | ⬜ Not Started | Delete the chocolatey downgrade lines |
| FR-003 | Base installers MUST be buildable using `msbuild Build/Orchestrator.proj /t:BuildBaseInstaller` | ⬜ Not Started | Test after removing downgrade |
| FR-004 | Patch installers MUST be buildable using `msbuild Build/Orchestrator.proj /t:BuildPatchInstaller` | ⬜ Not Started | Test after removing downgrade |
| FR-005 | The `insignia` tool MUST work correctly with WiX 3.14.x for burn engine extraction/reattachment | ⬜ Not Started | Critical for code signing workflow |
| FR-006 | Installer documentation MUST exist in the repository explaining the build process | ⬜ Not Started | Create `Docs/installer-build.md` or update existing |
| FR-007 | All Copilot instructions MUST reference WiX 3.14.x instead of 3.11.x | ⬜ Not Started | Update `installer.instructions.md` |
| FR-008 | Patches built with WiX 3.14.x MUST apply successfully to base installations built with WiX 3.11.x | ⬜ Not Started | Backward compatibility validation |

## Success Criteria

| ID | Criterion | Status | Measurement |
|----|-----------|--------|-------------|
| SC-001 | CI workflow execution time decreases by at least 30 seconds | ⬜ Not Started | Compare before/after workflow runs |
| SC-002 | 100% of installer builds complete successfully using WiX 3.14.x | ⬜ Not Started | CI build logs |
| SC-003 | Base installers install successfully on Windows 10 and Windows 11 | ⬜ Not Started | Manual testing |
| SC-004 | Patch installers apply successfully to both WiX 3.11.x and 3.14.x bases | ⬜ Not Started | Manual testing |
| SC-005 | Developer can build installer locally within 30 minutes using docs | ⬜ Not Started | Walkthrough test |
| SC-006 | Zero references to WiX 3.11 remain in active documentation | ⬜ Not Started | Grep search |

## Implementation Tasks

### Phase 1: Remove WiX Downgrade Workaround

- [x] Create `Build/Agent/Setup-InstallerBuild.ps1` to validate prerequisites
- [ ] Test base installer build locally with WiX 3.14.x (without downgrade)
- [ ] Test patch installer build locally with WiX 3.14.x (without downgrade)
- [ ] Verify `insignia` tool works correctly with WiX 3.14.x
- [x] Remove downgrade step from `base-installer-cd.yml`
- [x] Remove downgrade step from `patch-installer-cd.yml`
- [ ] Push changes and verify CI builds succeed

### Phase 2: Local Validation (before CI)

- [ ] Run `.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly`
- [ ] Open VS Developer Command Prompt: `Launch-VsDevShell.ps1 -Arch amd64`
- [ ] Run `msbuild Build/Orchestrator.proj /t:RestorePackages`
- [ ] Build base installer locally with WiX 3.14.x
- [ ] Verify `BuildDir/FieldWorks_*_Offline_x64.exe` created
- [ ] Run `.\Build\Agent\Setup-InstallerBuild.ps1 -SetupPatch`
- [ ] Build patch installer locally with WiX 3.14.x
- [ ] Verify `BuildDir/FieldWorks_*.msp` created

### Phase 3: CI Validation Testing

- [ ] Build base installer (online variant) and test installation on Windows 10
- [ ] Build base installer (offline variant) and test installation on Windows 11
- [ ] Build patch installer and apply to WiX 3.11.x-built base (build 1188)
- [ ] Build patch installer and apply to WiX 3.14.x-built base
- [ ] Verify FieldWorks launches and works correctly after each installation

### Phase 3: Documentation Updates

- [ ] Create/update installer build documentation
- [ ] Update `installer.instructions.md` WiX version references
- [ ] Update `copilot-instructions.md` WiX version references
- [ ] Review and update any other documentation mentioning WiX versions

## Files to Modify

| File | Change |
|------|--------|
| `.github/workflows/base-installer-cd.yml` | Remove `choco install wixtoolset --version 3.11.2` step |
| `.github/workflows/patch-installer-cd.yml` | Remove `choco install wixtoolset --version 3.11.2` step |
| `.github/instructions/installer.instructions.md` | Update WiX version to 3.14.x |
| `.github/copilot-instructions.md` | Update WiX version in tooling section |
| `Docs/installer-build.md` (new or existing) | Add installer build documentation |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| WiX 3.14.x produces incompatible installers | Low | High | Test thoroughly before removing downgrade |
| Patches fail to apply to older bases | Medium | High | Test against build 1188 specifically |
| `insignia` tool behavior changes | Low | High | Test signing workflow in isolation first |
| Runner updates break WiX | Low | Medium | Pin runner version if needed |

## Rollback Plan

If issues are discovered after removing the WiX downgrade:

1. Revert the workflow changes to restore `choco install wixtoolset --version 3.11.2`
2. Document the specific issue encountered
3. File issue with WiX project if bug is identified
4. Monitor WiX releases for fix
