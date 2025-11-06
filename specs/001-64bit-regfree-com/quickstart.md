# Quickstart: 64-bit only + Registration-free COM

**Feature Branch**: `001-64bit-regfree-com` | **Status**: Phase 1 Complete
**Related**: [spec.md](spec.md) | [plan.md](plan.md) | [tasks.md](tasks.md)

This guide shows how to build and validate the feature locally.

## Prerequisites
- Visual Studio 2022 with .NET desktop and Desktop C++ workloads
- Windows x64 (Windows 10/11)
- WiX 3.11.x (only if building installer)
- Initialize environment: `source ./environ` (from a Developer Command Prompt)

## Phase 1 Complete: x64-only builds

### Building

**Using build script (recommended)**:
```cmd
cd Build
build64.bat
```

**Using MSBuild directly**:
```cmd
msbuild FieldWorks.sln /m /p:Configuration=Debug /p:Platform=x64
```

**From Visual Studio**:
- Open FieldWorks.sln
- Select **x64** platform (only option available after Phase 1)
- Build solution (F7)

### What's New in Phase 1
✅ **x64 defaults**: Directory.Build.props enforces `<PlatformTarget>x64</PlatformTarget>`
✅ **Win32/x86 removed**: Solution and native projects support x64 only
✅ **CI enforces x64**: build64.bat used throughout CI pipeline
✅ **No COM registration in builds**: Build scripts audited and clean

### Running Applications
```cmd
cd Output\Debug
FieldWorks.exe
```

**Note**: No administrator privileges required for building or running (once Phase 2+ is complete).

## Phase 2+ (In Progress): Registration-free COM

### Validate registration-free COM (when Phase 3 complete)
1. Ensure no FieldWorks COM registrations exist on the machine (clean VM recommended)
2. Launch `Output/Debug/FieldWorks.exe`
3. **Expected**: No class-not-registered errors; app runs normally
4. **Check**: Manifests exist alongside executables

### Test host for COM activation (when Phase 6 complete)
- Run COM-activating tests under the shared manifest-enabled host
- No admin rights or COM registration needed
- Location: `Src/Utilities/ComManifestTestHost/`

### Artifacts to verify (when Phase 3+ complete)
- `Output/Debug/FieldWorks.exe.manifest` with `<file>/<comClass>/<typelib>` entries
- `Output/Debug/Flex.exe.manifest` with COM activation entries
- Build logs show RegFree task execution
- Native COM DLLs co-located with executables

## Troubleshooting

### Build Errors
**"Platform 'Win32' not found"**: Ensure you're using x64 platform. Solution no longer contains Win32 configurations.

**"Could not load file or assembly"**: Verify `/p:Platform=x64` is set. Clean and rebuild.

### COM Activation (Phase 3+)
**"Class not registered"** (after Phase 3): 
- Verify manifest file exists next to executable
- Check native COM DLLs are in same directory
- Rebuild to regenerate manifests

## Current Phase Status

| Phase | Status | Tasks |
|-------|--------|-------|
| **Phase 1: Setup** | ✅ Complete | T001-T006 |
| **Phase 2: Foundational** | 🔄 Next | T007-T010 |
| **Phase 3: User Story 1** | ⏳ Pending | T011-T016 |
| **Phase 4: User Story 2** | ⏳ Pending | T017-T021 |
| **Phase 5: User Story 3** | ⏳ Pending | T022-T024 |
| **Phase 6: Test Host** | ⏳ Pending | T025-T030 |
| **Final: Polish** | ⏳ Pending | T031-T033 |

## Developer Impact

### What Works Now (Phase 1)
- ✅ Build x64-only from Visual Studio or command line
- ✅ Run applications (still using registry COM for now)
- ✅ CI builds x64 exclusively

### What's Coming (Phase 2+)
- ⏳ Registration-free COM activation via manifests
- ⏳ No admin rights required for development
- ⏳ Installer ships without COM registration
- ⏳ Test host for COM-activating tests

## Support

For questions or issues:
- Review [tasks.md](tasks.md) for current progress
- Check commit history for recent changes
- Consult [plan.md](plan.md) for technical details
