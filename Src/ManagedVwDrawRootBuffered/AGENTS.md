---
last-reviewed: 2025-10-31
last-reviewed-tree: bdadc55d0831962324d30020fdc1138f970f046de6b795ce84e404e46dca8ef3
status: reviewed
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - buffered-drawing-engine
  - memory-buffer-management
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# ManagedVwDrawRootBuffered

## Purpose
Managed C# implementation of IVwDrawRootBuffered for double-buffered Views rendering. Eliminates flicker by rendering IVwRootBox content to off-screen bitmap (GDI+ Bitmap), then blitting to screen HDC. Direct port of C++ VwDrawRootBuffered from VwRootBox.cpp. Used by Views infrastructure to provide smooth rendering for complex multi-writing-system text displays with selections, highlighting, and dynamic content updates.

## Architecture
C# library (net48) with 2 source files (~283 lines total). Single class VwDrawRootBuffered implementing IVwDrawRootBuffered, using nested MemoryBuffer class for bitmap management. Integrates with native Views COM infrastructure (IVwRootBox, IVwRootSite, IVwSynchronizer).

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
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Graphics**: System.Drawing (GDI+ Bitmap and Graphics)
- **Key libraries**:
  - Common/ViewsInterfaces (IVwDrawRootBuffered, IVwRootBox, IVwRootSite, IVwSynchronizer)
  - LCModel.Core.Text
  - System.Runtime.InteropServices (COM interop, DllImport for BitBlt)
- **Native interop**: P/Invoke to gdi32.dll for BitBlt operation
- **COM**: Marked [ComVisible] with GUID for Views engine COM callbacks

## Dependencies
- **External**: Common/ViewsInterfaces (IVwDrawRootBuffered, IVwRootBox, IVwRootSite, IVwSynchronizer, Rect), LCModel.Core.Text, System.Drawing (GDI+ Bitmap, Graphics, IntPtr HDC interop), System.Runtime.InteropServices (COM attributes)
- **Internal (upstream)**: ViewsInterfaces (interface contracts)
- **Consumed by**: Common/RootSite (SimpleRootSite, RootSite use buffered drawing), ManagedVwWindow (window hosting Views), views (native Views engine calls back to managed buffered drawer)

## Interop & Contracts
- **COM interface**: IVwDrawRootBuffered from ViewsInterfaces
  - COM GUID: 97199458-10C7-49da-B3AE-EA922EA64859
  - Method: DrawTheRoot(IVwRootBox prootb, IntPtr hdc, Rect rcpDraw, uint bkclr, bool fDrawSel, IVwRootSite pvrs)
  - Purpose: Called by native Views engine to perform buffered rendering
- **P/Invoke**: BitBlt from gdi32.dll
  - Signature: `[DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);`
  - Purpose: Copy bitmap from off-screen buffer to target device context (fast blit operation)
  - ROP: SRCCOPY (0x00CC0020) for direct pixel copy
- **GDI+ to GDI bridge**: Graphics.GetHdc() acquires native HDC from managed Graphics for Views rendering
- **Data contracts**:
  - Rect struct: Drawing rectangle (left, top, right, bottom)
  - uint bkclr: RGB background color (format: 0x00BBGGRR)
  - bool fDrawSel: Whether to render selection highlighting
- **IVwRootBox interop**: Native COM interface called from managed code (prootb.DrawRoot())
- **IVwSynchronizer**: Checks IsExpandingLazyItems to skip rendering during lazy item expansion

## Threading & Performance
UI thread only. Double buffering eliminates flicker; BitBlt very fast. Bitmap allocated/disposed per draw (no caching).

## Config & Feature Flags
fDrawSel (selection rendering), bkclr (background color), synchronizer check (lazy item expansion gate). Behavior controlled by DrawTheRoot() parameters.

## Build Information
Build via FieldWorks.sln or `msbuild`. Output: ManagedVwDrawRootBuffered.dll. COM-visible with GUID.

## Interfaces and Data Models
IVwDrawRootBuffered (COM interface), MemoryBuffer (RAII bitmap wrapper), Rect (draw area), BitBlt P/Invoke (gdi32.dll).

## Entry Points
COM instantiation via GUID 97199458-10C7-49da-B3AE-EA922EA64859. Native Views calls DrawTheRoot() during paint. RootSite registers instance.

## Test Index
No dedicated unit tests. Integration tested via RootSite and Views rendering. Manual testing in FLEx validates flicker elimination.

## Usage Hints
RootSite registers buffered drawer with root box. Not for direct invocation (Views calls automatically). Eliminates flicker during scroll/selection.

## Related Folders
views (native Views engine), ManagedVwWindow (window hosting), Common/RootSite (base classes), Common/ViewsInterfaces (interfaces).

## References
Project file: ManagedVwDrawRootBuffered.csproj (net48). Key files (283 lines): VwDrawRootBuffered.cs, AssemblyInfo.cs. COM GUID: 97199458-10C7-49da-B3AE-EA922EA64859. See `.cache/copilot/diff-plan.json` for details.
