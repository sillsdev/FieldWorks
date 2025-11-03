---
last-reviewed: 2025-10-31
last-reviewed-tree: e670f4da389631b90d2583d7a978a855adf912e60e1397eb06af2937e30b1c74
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

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.6.2 (net462)
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
- **Thread affinity**: Must be used on UI thread (Control.FromHandle() requires UI thread)
- **Performance**: Minimal overhead (~2 pointer dereferences + struct copy)
  - Window setter: HWND lookup via Control.FromHandle() (fast dictionary lookup)
  - GetClientRectangle(): Direct property access + struct copy (nanoseconds)
- **No blocking operations**: All operations synchronous and fast
- **Thread safety**: Not thread-safe (relies on WinForms Control which is UI-thread-only)
- **GC pressure**: Minimal (no allocations except ManagedVwWindow instance itself)
- **Typical usage pattern**: Created once per Control, reused for lifetime of view

## Config & Feature Flags
- **No configuration**: Behavior entirely determined by wrapped Control
- **Window property**: Must be set before GetClientRectangle() called
  - Throws ApplicationException if accessed before initialization
- **Control resolution**: Control.FromHandle() automatically resolves HWND to Control
  - Returns null if HWND invalid (caller responsible for null check)
- **Client rectangle**: Always reflects current Control.ClientRectangle (no caching)
- **No global state**: Each ManagedVwWindow instance is independent

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ManagedVwWindow.csproj` or `dotnet build` (from FW.sln)
- Output: ManagedVwWindow.dll
- Dependencies: ViewsInterfaces, System.Windows.Forms
- COM attributes: [ComVisible], GUID for COM registration

## Interfaces and Data Models

### Interfaces
- **IVwWindow** (path: Src/Common/ViewsInterfaces/)
  - Purpose: Expose window handle and geometry to native Views engine
  - Property: Window (uint HWND, set-only)
  - Method: GetClientRectangle(out Rect clientRectangle)
  - Notes: COM-visible, called by native Views C++ code

### Data Models
- **Rect** (from ViewsInterfaces)
  - Purpose: Views geometry specification
  - Shape: int left, int top, int right, int bottom
  - Usage: Output parameter for GetClientRectangle()
  - Notes: Matches native Views Rect structure

### Structures
- **Control** (System.Windows.Forms.Control)
  - Purpose: Managed WinForms control being wrapped
  - Properties: Handle (IntPtr HWND), ClientRectangle (Rectangle)
  - Notes: Resolved via Control.FromHandle(IntPtr)

## Entry Points
- **Instantiation**: Created by RootSite or view-hosting code
  ```csharp
  var vwWindow = new ManagedVwWindow();
  vwWindow.Window = (uint)control.Handle.ToInt32();  // Set HWND
  ```
- **COM access**: Native Views engine calls via IVwWindow COM interface
- **Typical usage** (RootSite initialization):
  1. Create ManagedVwWindow: `var vwWindow = new ManagedVwWindow()`
  2. Set Window property: `vwWindow.Window = (uint)this.Handle`
  3. Pass to Views engine: Register with IVwRootBox or layout code
  4. Native Views calls GetClientRectangle() during layout
- **Common consumers**:
  - RootSite.OnHandleCreated(): Sets up ManagedVwWindow for root site
  - Browse views: xWorks browse columns use ManagedVwWindow for view geometry
  - Lexicon/Interlinear displays: All Views-based UI uses ManagedVwWindow wrapper

## Test Index
- **Test project**: ManagedVwWindowTests/ManagedVwWindowTests.csproj
- **Test file**: ManagedVwWindowTests.cs
- **Test coverage**:
  - Window property setter: HWND → Control conversion
  - GetClientRectangle(): Correct Rect output matching Control.ClientRectangle
  - Exception handling: ApplicationException when GetClientRectangle() called before Window set
  - Null HWND: Behavior when Control.FromHandle() returns null
- **Test approach**: Unit tests with WinForms Control instances
- **Test runners**:
  - Visual Studio Test Explorer
  - Via FW.sln top-level build
- **Manual testing**: Any FLEx view (lexicon, interlinear, browse) exercises ManagedVwWindow via Views rendering

## Usage Hints
- **Typical usage pattern**:
  ```csharp
  var vwWindow = new ManagedVwWindow();
  vwWindow.Window = (uint)myControl.Handle.ToInt32();
  Rect clientRect;
  vwWindow.GetClientRectangle(out clientRect);
  // clientRect now contains control's client area geometry
  ```
- **Common pitfall**: Forgetting to set Window property before calling GetClientRectangle()
  - Always set Window property in Control.OnHandleCreated() or after Handle is valid
- **HWND validity**: Ensure Control.Handle is created before passing to Window property
  - WinForms Controls don't create HWND until Control.CreateHandle() or first access
- **Lifetime**: Keep ManagedVwWindow alive while Control is in use
  - Typically stored as field in RootSite or view-hosting class
- **COM registration**: GUID 3fb0fcd2-ac55-42a8-b580-73b89a2b6215 must be registered for native Views access
- **Debugging tips**:
  - Verify Control.Handle != IntPtr.Zero before setting Window property
  - Check Control.ClientRectangle matches output Rect
  - Ensure UI thread affinity (Control.InvokeRequired should be false)
- **Extension**: Minimal class; no easy extension points
- **Replacement**: Direct port of C++ VwWindow wrapper; functionally equivalent

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

## Auto-Generated Project and File References
- Project files:
  - Src/ManagedVwWindow/ManagedVwWindow.csproj
  - Src/ManagedVwWindow/ManagedVwWindowTests/ManagedVwWindowTests.csproj
- Key C# files:
  - Src/ManagedVwWindow/AssemblyInfo.cs
  - Src/ManagedVwWindow/ManagedVwWindow.cs
  - Src/ManagedVwWindow/ManagedVwWindowTests/ManagedVwWindowTests.cs
## Test Information
- Test project: ManagedVwWindowTests
- Test coverage: Window property setter with valid/invalid HWNDs, GetClientRectangle() with set/unset window, Control.FromHandle() resolution
- Run: `dotnet test` or Test Explorer in Visual Studio

## Code Evidence
*Analysis based on scanning 3 source files*

- **Classes found**: 2 public classes
- **Namespaces**: SIL.FieldWorks.Language, SIL.FieldWorks.Views
