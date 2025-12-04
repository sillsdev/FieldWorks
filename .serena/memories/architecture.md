# FieldWorks Architecture

## Build Phases (MSBuild Traversal)
The `FieldWorks.proj` file orchestrates 21 build phases:
1. **Phase 1**: FwBuildTasks (build infrastructure)
2. **Phase 2**: Native C++ (DebugProcs → GenericLib → FwKernel → Views)
   - Generates IDL files: `ViewsTlb.idl`, `FwKernelTlb.json`
3. **Phase 3**: ViewsInterfaces (idlimport: ViewsTlb.idl → Views.cs)
4. **Phases 4-14**: Managed projects in dependency order
5. **Phases 15-21**: Test projects

## Key Subsystems

### Native/Managed Boundary
- **C++/CLI bridges**: Located in `Src/` alongside managed projects
- **ViewsInterfaces** (`Src/Common/ViewsInterfaces`): Auto-generated C# interfaces from native IDL
- **FwKernel** (`Src/FwKernel`): Core native text rendering engine
- **Views** (`Src/Views`): Native view infrastructure

### Application Layer
- **xWorks** (`Src/xWorks`): Shared application framework
- **LexText** (`Src/LexText`): FLEx (Language Explorer) application
- **Common** (`Src/Common`): Shared utilities and controls

### Data Layer
- **FixFwDataDll** (`Src/Utilities/FixFwDataDll`): Data repair utilities
- **ProjectUnpacker** (`Src/ProjectUnpacker`): Project file handling

## File Organization
- `Src/<Component>/` — Source code with per-folder COPILOT.md
- `Src/<Component>.Tests/` — Corresponding test projects
- `Build/` — MSBuild targets and orchestration
- `FLExInstaller/` — WiX installer artifacts
- `Include/` + `Lib/` — Native headers and libraries
- `DistFiles/` — Runtime assets copied to Output/

## Dependency Flow
```
Native C++ (Phase 2)
    ↓ generates IDL
ViewsInterfaces (Phase 3)
    ↓ provides C# interfaces
FwUtils, FwResources, xCore (Phases 4-6)
    ↓ foundation libraries
UI Components (Phases 7-10)
    ↓ controls and widgets
Applications (Phases 11-14)
    ↓ FLEx, utilities
Test Projects (Phases 15-21)
```
