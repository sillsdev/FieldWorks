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
- **Language**: C#
- **Target framework**: .NET Framework 4.6.2 (net462)
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
- **Thread affinity**: Must run on UI thread (GDI+ and HDC operations require UI thread)
- **Performance characteristics**:
  - **Double buffering**: Eliminates flicker by rendering to off-screen bitmap before copying to screen
  - **BitBlt speed**: Very fast native GDI operation (~nanoseconds for typical view sizes)
  - **Bitmap allocation**: GDI+ Bitmap created per draw (not cached); overhead mitigated by fast allocation
  - **Memory**: Bitmap size = width × height × 4 bytes (ARGB); typical views 800×600 = 1.9 MB
- **Rendering flow**:
  1. Create off-screen Bitmap matching target rectangle size
  2. Acquire HDC from Bitmap's Graphics (via GetHdc())
  3. IVwRootBox.DrawRoot() renders to off-screen HDC
  4. BitBlt copies bitmap to screen HDC (single fast blit)
  5. Dispose bitmap and graphics (automatic via MemoryBuffer.Dispose)
- **Optimization**: Synchronizer check skips expensive rendering during lazy item expansion
- **No caching**: Bitmap created/disposed per DrawTheRoot() call; no persistent off-screen buffer
- **GC pressure**: Moderate (Bitmap allocation per render); mitigated by deterministic disposal

## Config & Feature Flags
- **fDrawSel parameter**: Controls selection rendering
  - true: Render selection highlighting (typical for active views)
  - false: Skip selection rendering (e.g., printing, background rendering)
- **bkclr parameter**: Background color for bitmap fill before rendering
  - Format: RGB as uint (0x00BBGGRR)
  - Applied via Clear(Color.FromArgb(bkclr)) before Views rendering
- **Synchronizer check**: IVwSynchronizer.IsExpandingLazyItems gate
  - If true, skips rendering (returns early to avoid half-rendered state)
  - Ensures consistent display during lazy box expansion
- **No global configuration**: Behavior fully controlled by DrawTheRoot() parameters
- **Deterministic cleanup**: MemoryBuffer implements IDisposable with finalizer for robust resource cleanup

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ManagedVwDrawRootBuffered.csproj` or `dotnet build` (from FW.sln)
- Output: ManagedVwDrawRootBuffered.dll
- Dependencies: ViewsInterfaces, LCModel.Core, System.Drawing (GDI+)
- COM attributes: [ComVisible], GUID for COM registration

## Interfaces and Data Models

### Interfaces
- **IVwDrawRootBuffered** (path: Src/Common/ViewsInterfaces/)
  - Purpose: Double-buffered rendering contract for Views engine
  - Inputs: IVwRootBox (content to render), IntPtr hdc (target DC), Rect (draw area), uint bkclr (background), bool fDrawSel (selection flag), IVwRootSite (callbacks)
  - Outputs: None (side effect: renders to target HDC)
  - Method: DrawTheRoot(...)
  - Notes: COM-visible, called by native Views C++ engine

### Data Models
- **MemoryBuffer** (nested class in VwDrawRootBuffered.cs)
  - Purpose: RAII wrapper for off-screen GDI+ Bitmap and Graphics
  - Shape: Bitmap (GDI+ Bitmap), Graphics (GDI+ Graphics with HDC acquired via GetHdc())
  - Lifecycle: Created in DrawTheRoot(), disposed after BitBlt
  - Notes: Implements IDisposable with finalizer; ensures HDC release via ReleaseHdc()

### Structures
- **Rect** (from ViewsInterfaces)
  - Purpose: Drawing rectangle specification
  - Shape: int left, int top, int right, int bottom
  - Usage: Defines bitmap size and target blit area

### P/Invoke Signatures
- **BitBlt** (gdi32.dll)
  - Signature: `bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop)`
  - Purpose: Fast pixel copy from source DC to destination DC
  - ROP: SRCCOPY (0x00CC0020) for direct pixel transfer

## Entry Points
- **COM instantiation**: Created by native Views engine via COM GUID 97199458-10C7-49da-B3AE-EA922EA64859
- **Invocation**: Native Views C++ code calls IVwDrawRootBuffered.DrawTheRoot() during paint operations
- **Typical call chain**:
  1. User action triggers window repaint (scroll, selection, data change)
  2. RootSite.OnPaint() → IVwRootBox.Draw()
  3. Native Views checks if buffered drawer registered
  4. Calls VwDrawRootBuffered.DrawTheRoot() (via COM)
  5. Buffered rendering executes, BitBlt to screen
- **Registration**: RootSite registers IVwDrawRootBuffered instance with IVwRootBox during initialization
- **Not directly called from C# code**: Invoked by native Views engine as callback

## Test Index
- **No dedicated unit tests**: Integration tested via RootSite and Views rendering
- **Integration test coverage** (via Common/RootSite tests):
  - Buffered rendering eliminates flicker during scroll
  - Selection rendering with fDrawSel=true
  - Background color fill with various bkclr values
  - Synchronizer gate during lazy item expansion
- **Manual testing scenarios**:
  - Scroll large lexicon list → smooth rendering, no flicker
  - Select text in interlinear view → selection highlights correctly
  - Resize window → views redraw smoothly
  - Print preview → renders without selection (fDrawSel=false)
- **Visual validation**: Compare with/without buffered rendering (legacy C++ VwDrawRootBuffered vs managed)
- **Test approach**: End-to-end UI testing in FLEx application
- **No automated unit tests**: Difficult to unit test GDI+ rendering without full Views infrastructure

## Usage Hints
- **Typical usage** (RootSite initialization):
  ```csharp
  // Register buffered drawer with root box
  var bufferedDrawer = new VwDrawRootBuffered();
  rootBox.DrawingErrors = bufferedDrawer;  // or specific registration method
  ```
- **Not for direct invocation**: Native Views engine calls DrawTheRoot() automatically during paint
- **Debugging tips**:
  - Set breakpoint in DrawTheRoot() to diagnose rendering issues
  - Check IsExpandingLazyItems if rendering appears incomplete
  - Verify bitmap size matches target rectangle (width/height must be positive)
- **Performance tuning**:
  - Ensure views are invalidated minimally (only changed regions)
  - Use lazy boxes to defer rendering of off-screen content
  - Monitor bitmap allocation rate (should match repaint rate)
- **Common pitfalls**:
  - Forgetting to release HDC (MemoryBuffer.Dispose handles this automatically)
  - Creating VwDrawRootBuffered on non-UI thread (GDI+ requires UI thread)
  - Not registering buffered drawer with root box (results in direct rendering, potential flicker)
- **Flicker elimination**: Double buffering prevents:
  - Partial updates during complex rendering
  - Flash during scroll operations
  - Selection artifacts during text editing
- **Extension**: Cannot be easily extended; core rendering logic is final
- **Replacement**: C# port replaces C++ VwDrawRootBuffered; functionally equivalent

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

## Auto-Generated Project and File References
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
