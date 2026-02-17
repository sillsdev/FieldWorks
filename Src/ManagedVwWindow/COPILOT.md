---
last-reviewed: 2025-10-31
last-reviewed-tree: 5dbbc7b24d9d7da0683afe68327e42e483e3eeb039f2ad526b2f844fc8921cd6
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# ManagedVwWindow

## Purpose
Managed C# wrapper for IVwWindow interface enabling cross-platform window handle access. Wraps Windows Forms Control (HWND) to provide IVwWindow implementation for native Views engine. Bridges managed UI code (WinForms Controls) with native Views rendering by converting between IntPtr HWNDs and managed Control references, exposing client rectangle geometry. Minimal ~50-line adapter class essential for integrating native Views system into .NET WinForms applications (xWorks, LexText, RootSite-based displays).

## Architecture
C# library (net48) with 3 source files (~58 lines total). Single class ManagedVwWindow implementing IVwWindow COM interface, wrapping System.Windows.Forms.Control. Marked with COM GUID 3fb0fcd2-ac55-42a8-b580-73b89a2b6215 for registration-free COM activation via application manifests (no registry registration required).

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

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **UI framework**: System.Windows.Forms (Control class)
- **Key libraries**:
  - Common/ViewsInterfaces (IVwWindow interface, Rect struct)
  - System.Runtime.InteropServices (COM interop attributes)
- **COM**: Marked [ComVisible] with GUID for native Views engine access
- **Platform**: Windows-specific (relies on HWND and WinForms Control)

## Dependencies
- **External**: Common/ViewsInterfaces (IVwWindow interface, Rect struct), System.Windows.Forms (Control, Control.FromHandle()), System.Runtime.InteropServices (COM attributes)
- **Internal (upstream)**: ViewsInterfaces (interface contract)
- **Consumed by**: Common/RootSite (SimpleRootSite creates ManagedVwWindow for its Control), views (native Views engine calls IVwWindow methods), xWorks (browse views host Controls with ManagedVwWindow), LexText (all view-based displays use ManagedVwWindow wrapper)

## Interop & Contracts
- **COM interface**: IVwWindow from ViewsInterfaces
  - COM GUID: 3fb0fcd2-ac55-42a8-b580-73b89a2b6215
  - Property: Window (uint HWND, set-only)
  - Method: GetClientRectangle(out Rect clientRectangle)
  - Purpose: Provide window handle and geometry to native Views engine
- **HWND conversion**: uint HWND → IntPtr → Control.FromHandle()
  - Native Views passes HWND as uint
  - Managed code converts to IntPtr for WinForms Control lookup
- **Data contracts**:
  - Rect struct: Views geometry (int left, top, right, bottom)
  - ClientRectangle: WinForms Control.ClientRectangle → Views Rect
- **Cross-platform bridge**: Connects managed WinForms Control to native Views COM interface
- **Lifetime**: ManagedVwWindow instance typically matches Control lifetime

## Threading & Performance
UI thread only. Minimal overhead (fast HWND lookup, direct property access). Created once per Control, reused.

## Config & Feature Flags
No configuration. Window property must be set before GetClientRectangle(). Behavior determined by wrapped Control.

## Build Information
Build via FieldWorks.sln or `msbuild`. Output: ManagedVwWindow.dll. COM-visible with GUID.

## Interfaces and Data Models
IVwWindow (COM interface), Rect (geometry struct), Control (WinForms control wrapper).

## Entry Points
Created by RootSite or view-hosting code. Native Views calls GetClientRectangle() during layout. COM GUID: 3fb0fcd2-ac55-42a8-b580-73b89a2b6215.

## Test Index
Test project: ManagedVwWindowTests. Run via Test Explorer or FieldWorks.sln.

## Usage Hints
Set Window property before GetClientRectangle(). RootSite creates instance during initialization. All Views-based UI uses wrapper.

## Related Folders
views (native Views engine), ManagedVwDrawRootBuffered (buffered rendering), Common/RootSite (base classes), Common/ViewsInterfaces (interfaces), xWorks/LexText (consumers).

## References
Projects: ManagedVwWindow.csproj, ManagedVwWindowTests (net48). Key files (58 lines): ManagedVwWindow.cs, AssemblyInfo.cs, tests. COM GUID: 3fb0fcd2-ac55-42a8-b580-73b89a2b6215. See `.cache/copilot/diff-plan.json` for details.
