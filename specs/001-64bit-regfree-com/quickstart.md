# Quickstart: 64-bit only + Registration-free COM

**Feature Branch**: `001-64bit-regfree-com` | **Status**: Phase 3-4 Complete
**Related**: [spec.md](spec.md) | [plan.md](plan.md) | [tasks.md](tasks.md)

This guide shows how to build and validate the feature locally.

## Prerequisites
- Visual Studio 2022 with .NET desktop and Desktop C++ workloads
- Windows x64 (Windows 10/11)
- WiX 3.11.x (only if building installer)
- Initialize environment: `source ./environ` (from a Developer Command Prompt)

## Phases 1-4 Complete: x64-only + Reg-free COM

### Building

**Modern build (using Traversal SDK)**:
```powershell
.\build.ps1 -Configuration Debug -Platform x64
```

**With tests**:
```powershell
.\build.ps1 -Configuration Debug -Platform x64 -MsBuildArgs @('/m', '/p:action=test')
```

**Solution only**:
```cmd
msbuild FieldWorks.sln /m /p:Configuration=Debug /p:Platform=x64
```
**Visual Studio**:
- Open FieldWorks.sln
- Select **x64** platform (only option available)
- Build solution (F7)

### What's New Through Phase 4
✅ **x64 defaults**: Directory.Build.props enforces `<PlatformTarget>x64</PlatformTarget>`
✅ **Win32/x86 removed**: Solution and native projects support x64 only
✅ **CI enforces x64**: workflows call `msbuild Build/FieldWorks.proj /p:Platform=x64`
✅ **No COM registration**: Builds and installer do not write to registry
✅ **Registration-free COM**: Manifests enable COM activation without registry
✅ **Installer packages manifests**: FieldWorks.exe.manifest and dependent assembly manifests (.X.manifest) included

### Running Applications
```cmd
cd Output\Debug
FieldWorks.exe
```

**Note**: No administrator privileges required for building or running. COM activates via manifests co-located with executables.

## Registration-free COM Validation

### Validate Manifests Generated (Phase 3)
1. Build Debug|x64
2. **Check**:
   - `Output/Debug/FieldWorks.exe.manifest` exists and references dependent assemblies
   - `Output/Debug/FwKernel.X.manifest` exists with COM proxy stubs
   - `Output/Debug/Views.X.manifest` exists with 27+ COM class registrations
3. **Expected**: Manifests contain `<file>/<comClass>/<typelib>` entries with type="x64"

### Validate Clean Machine Launch (Phase 3)
1. Ensure no FieldWorks COM registrations exist (clean VM or unregister: `regsvr32 /u Views.dll`)
2. Launch `Output/Debug/FieldWorks.exe`
3. **Expected**: App runs normally without class-not-registered errors
4. **Verify**: Process Monitor shows no registry lookups under HKCR\CLSID for FieldWorks components

### Validate Installer (Phase 4)
1. Build installer per installer documentation
2. Install on clean machine
3. **Check**:
   - FieldWorks.exe, FieldWorks.exe.manifest, FwKernel.X.manifest, Views.X.manifest installed to same directory
   - Native COM DLLs (Views.dll, FwKernel.dll) co-located with manifests
4. Launch installed FieldWorks.exe
5. **Expected**: COM activation succeeds without registry writes

## Artifacts to Verify

### Phase 3 (Build-time manifests)
- `Output/Debug/FieldWorks.exe.manifest`: Main EXE manifest with dependentAssembly references
- `Output/Debug/FwKernel.X.manifest`: COM interface proxy stubs
- `Output/Debug/Views.X.manifest`: 27+ COM class entries (VwGraphicsWin32, LgLineBreaker, VwRootBox, TsStrFactory, etc.)
- Build logs show RegFree target execution

### Phase 4 (Installer artifacts)
- `FLExInstaller/CustomComponents.wxi` includes manifest File entries
- `Build/Installer.targets` adds manifests to CustomInstallFiles
- No COM registration actions in installer (CustomActionSteps.wxi, CustomComponents.wxi)
- Install directory layout: all EXEs, manifests, and COM DLLs in single folder

## Test Host for COM Activation (Phase 6 - Pending)
- Run COM-activating tests under the shared manifest-enabled host
- No admin rights or COM registration needed
- Location: `Src/Utilities/ComManifestTestHost/`

## Troubleshooting

### Build Errors
**"Platform 'Win32' not found"**: Ensure you're using x64 platform. Solution no longer contains Win32 configurations.

**"Could not load file or assembly"**: Verify `/p:Platform=x64` is set. Clean and rebuild.

### COM Activation Errors
**"Class not registered"** or **"0x80040154" (REGDB_E_CLASSNOTREG)**:

**Manifest not found**:

## Current Phase Status

| Phase                     | Status     | Tasks                                         |
| ------------------------- | ---------- | --------------------------------------------- |
| **Phase 1: Setup**        | ✅ Complete | T001-T006                                     |
| **Phase 2: Foundational** | ✅ Complete | T007-T010                                     |
| **Phase 3: User Story 1** | ✅ Complete | T011-T015 (T016 skipped)                      |
| **Phase 4: User Story 2** | ✅ Complete | T017-T021                                     |
| **Phase 5: User Story 3** | 🔄 Next     | T022-T024 (T022-T023 done, T024 pending)      |
| **Phase 6: Test Host**    | 🔄 Partial  | T025-T030 (T025-T027 done, T028-T030 pending) |
| **Final: Polish**         | ⏳ Pending  | T031-T033                                     |

## Developer Impact

### What Works Now (Phases 1-4)
- ✅ Build x64-only from Visual Studio or command line
- ✅ Run applications without admin rights using registration-free COM
- ✅ Manifests automatically generated for EXE projects
- ✅ CI builds x64 exclusively and uploads manifests
- ✅ Installer packages manifests and skips COM registration

### What's Coming (Phases 5-6)
- ⏳ CI smoke test for reg-free COM activation
- ⏳ Test host integration for COM-activating tests
- ⏳ Final documentation updates

## Support

For questions or issues:
- Review [tasks.md](tasks.md) for current progress
- Check commit history for recent changes
- Consult [plan.md](plan.md) for technical details
