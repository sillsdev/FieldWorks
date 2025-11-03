---
last-reviewed: 2025-10-31
last-reviewed-tree: e70343c535764f54c8cdff93d336266d5d0a05725940ea0e83cf6264a4c44616
status: reviewed
---

# ManagedVwDrawRootBuffered

## Purpose
Managed C# implementation of IVwDrawRootBuffered for double-buffered Views rendering. Eliminates flicker by rendering IVwRootBox content to off-screen bitmap (GDI+ Bitmap), then blitting to screen HDC. Direct port of C++ VwDrawRootBuffered from VwRootBox.cpp. Used by Views infrastructure to provide smooth rendering for complex multi-writing-system text displays with selections, highlighting, and dynamic content updates.

## Architecture
C# library (net462) with 2 source files (~283 lines total). Single class VwDrawRootBuffered implementing IVwDrawRootBuffered, using nested MemoryBuffer class for bitmap management. Integrates with native Views COM infrastructure (IVwRootBox, IVwRootSite, IVwSynchronizer).

## Key Components

### Buffered Drawing Engine
- **VwDrawRootBuffered**: Implements IVwDrawRootBuffered.DrawTheRoot() for double-buffered rendering. Creates off-screen bitmap via MemoryBuffer (wraps GDI+ Bitmap + Graphics), invokes IVwRootBox.DrawRoot() to render to bitmap HDC, copies bitmap to target HDC via BitBlt. Handles synchronizer checks (skips draw if IsExpandingLazyItems), selection rendering (fDrawSel parameter), background color fill (bkclr parameter).
  - Inputs: IVwRootBox (root box to render), IntPtr hdc (target device context), Rect rcpDraw (drawing rectangle), uint bkclr (background color RGB), bool fDrawSel (render selection), IVwRootSite pvrs (root site for callbacks)
  - Methods: DrawTheRoot(...) - main rendering entry point
  - Internal: MemoryBuffer nested class for bitmap lifecycle
  - COM GUID: 97199458-10C7-49da-B3AE-EA922EA64859

### Memory Buffer Management
- **MemoryBuffer** (nested class): Manages off-screen GDI+ Bitmap and Graphics for double buffering. Creates Bitmap(width, height), gets Graphics from bitmap, acquires HDC via Graphics.GetHdc() for Views rendering, releases HDC on dispose. Implements IDisposable with proper finalizer for deterministic cleanup.
  - Properties: Bitmap (GDI+ Bitmap), Graphics (GDI+ Graphics with acquired HDC)
  - Lifecycle: Created per DrawTheRoot() call, disposed after BitBlt to screen

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **External**: Common/ViewsInterfaces (IVwDrawRootBuffered, IVwRootBox, IVwRootSite, IVwSynchronizer, Rect), LCModel.Core.Text, System.Drawing (GDI+ Bitmap, Graphics, IntPtr HDC interop), System.Runtime.InteropServices (COM attributes)
- **Internal (upstream)**: ViewsInterfaces (interface contracts)
- **Consumed by**: Common/RootSite (SimpleRootSite, RootSite use buffered drawing), ManagedVwWindow (window hosting Views), views (native Views engine calls back to managed buffered drawer)

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ManagedVwDrawRootBuffered.csproj` or `dotnet build` (from FW.sln)
- Output: ManagedVwDrawRootBuffered.dll
- Dependencies: ViewsInterfaces, LCModel.Core, System.Drawing (GDI+)
- COM attributes: [ComVisible], GUID for COM registration

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **views/**: Native Views C++ engine, calls IVwDrawRootBuffered for managed buffering
- **ManagedVwWindow/**: Window management hosting Views with buffered rendering
- **Common/RootSite/**: RootSite base classes using buffered drawer
- **Common/SimpleRootSite/**: SimpleRootSite subclasses using buffered rendering
- **Common/ViewsInterfaces/**: Defines IVwDrawRootBuffered, IVwRootBox interfaces

## References
- **Source files**: 2 C# files (~283 lines): VwDrawRootBuffered.cs, AssemblyInfo.cs
- **Project file**: ManagedVwDrawRootBuffered.csproj
- **Key class**: VwDrawRootBuffered (implements IVwDrawRootBuffered, nested MemoryBuffer)
- **Key interface**: IVwDrawRootBuffered (from ViewsInterfaces)
- **COM GUID**: 97199458-10C7-49da-B3AE-EA922EA64859
- **Namespace**: SIL.FieldWorks.Views
- **Target framework**: net462

## References (auto-generated hints)
- Project files:
  - Src/ManagedVwDrawRootBuffered/ManagedVwDrawRootBuffered.csproj
- Key C# files:
  - Src/ManagedVwDrawRootBuffered/AssemblyInfo.cs
  - Src/ManagedVwDrawRootBuffered/VwDrawRootBuffered.cs
## Test Information
- No dedicated test project found in this folder
- Integration tested via RootSite tests (Common/RootSite tests exercise buffered rendering)
- Manual testing: Any FLEx view with text (lexicon, interlinear, browse views) uses buffered rendering to eliminate flicker during scroll/selection

## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 1 public classes
- **Namespaces**: SIL.FieldWorks.Views
