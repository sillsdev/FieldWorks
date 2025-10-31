---
last-reviewed: 2025-10-31
last-verified-commit: 0e76301
status: reviewed
---

# ManagedVwWindow

## Purpose
Managed C# wrapper for IVwWindow interface enabling cross-platform window handle access. Wraps Windows Forms Control (HWND) to provide IVwWindow implementation for native Views engine. Bridges managed UI code (WinForms Controls) with native Views rendering by converting between IntPtr HWNDs and managed Control references, exposing client rectangle geometry. Minimal ~50-line adapter class essential for integrating native Views system into .NET WinForms applications (xWorks, LexText, RootSite-based displays).

## Architecture
C# library (net462) with 3 source files (~58 lines total). Single class ManagedVwWindow implementing IVwWindow COM interface, wrapping System.Windows.Forms.Control. Marked with COM GUID 3fb0fcd2-ac55-42a8-b580-73b89a2b6215 for COM registration.

## Key Components

### Window Wrapper
- **ManagedVwWindow**: Implements IVwWindow for managed Control access. Stores m_control (System.Windows.Forms.Control reference), provides GetClientRectangle() converting Control.ClientRectangle to Views Rect struct, implements Window property setter converting uint HWND to IntPtr and resolving Control via Control.FromHandle().
  - Inputs: uint Window property (HWND as unsigned int)
  - Methods:
    - GetClientRectangle(out Rect clientRectangle): Fills Views Rect with Control's client rectangle (top, left, right, bottom)
    - Window setter: Converts uint HWND to Control via Control.FromHandle(IntPtr)
  - Properties: Window (set-only, uint HWND)
  - Internal: m_control (protected Control field)
  - Throws: ApplicationException if GetClientRectangle() called before Window set
  - COM GUID: 3fb0fcd2-ac55-42a8-b580-73b89a2b6215

## Dependencies
- **External**: Common/ViewsInterfaces (IVwWindow interface, Rect struct), System.Windows.Forms (Control, Control.FromHandle()), System.Runtime.InteropServices (COM attributes)
- **Internal (upstream)**: ViewsInterfaces (interface contract)
- **Consumed by**: Common/RootSite (SimpleRootSite creates ManagedVwWindow for its Control), views (native Views engine calls IVwWindow methods), xWorks (browse views host Controls with ManagedVwWindow), LexText (all view-based displays use ManagedVwWindow wrapper)

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ManagedVwWindow.csproj` or `dotnet build` (from FW.sln)
- Output: ManagedVwWindow.dll
- Dependencies: ViewsInterfaces, System.Windows.Forms
- COM attributes: [ComVisible], GUID for COM registration

## Test Information
- Test project: ManagedVwWindowTests
- Test coverage: Window property setter with valid/invalid HWNDs, GetClientRectangle() with set/unset window, Control.FromHandle() resolution
- Run: `dotnet test` or Test Explorer in Visual Studio

## Related Folders
- **views/**: Native Views C++ engine consuming IVwWindow interface
- **ManagedVwDrawRootBuffered/**: Buffered rendering used alongside ManagedVwWindow
- **Common/RootSite/**: RootSite base classes creating ManagedVwWindow instances
- **Common/SimpleRootSite/**: SimpleRootSite uses ManagedVwWindow for Control wrapping
- **Common/ViewsInterfaces/**: Defines IVwWindow interface and Rect struct
- **xWorks/**: Browse views and data displays use ManagedVwWindow
- **LexText/**: All LexText view-based UI uses ManagedVwWindow

## References
- **Source files**: 3 C# files (~58 lines): ManagedVwWindow.cs, AssemblyInfo.cs, ManagedVwWindowTests/ManagedVwWindowTests.cs
- **Project files**: ManagedVwWindow.csproj, ManagedVwWindowTests/ManagedVwWindowTests.csproj
- **Key class**: ManagedVwWindow (implements IVwWindow)
- **Key interface**: IVwWindow (from ViewsInterfaces)
- **COM GUID**: 3fb0fcd2-ac55-42a8-b580-73b89a2b6215
- **Namespace**: SIL.FieldWorks.Views
- **Target framework**: net462
- Key C# files:
  - Src/ManagedVwWindow/AssemblyInfo.cs
  - Src/ManagedVwWindow/ManagedVwWindow.cs
  - Src/ManagedVwWindow/ManagedVwWindowTests/ManagedVwWindowTests.cs
## Code Evidence
*Analysis based on scanning 3 source files*

- **Classes found**: 2 public classes
- **Namespaces**: SIL.FieldWorks.Language, SIL.FieldWorks.Views
