---
applyTo: "Src/ManagedVwDrawRootBuffered/**"
name: "managedvwdrawrootbuffered.instructions"
description: "Auto-generated concise instructions from COPILOT.md for ManagedVwDrawRootBuffered"
---

# ManagedVwDrawRootBuffered (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **VwDrawRootBuffered**: Implements IVwDrawRootBuffered.DrawTheRoot() for double-buffered rendering. Creates off-screen bitmap via MemoryBuffer (wraps GDI+ Bitmap + Graphics), invokes IVwRootBox.DrawRoot() to render to bitmap HDC, copies bitmap to target HDC via BitBlt. Handles synchronizer checks (skips draw if IsExpandingLazyItems), selection rendering (fDrawSel parameter), background color fill (bkclr parameter).
- Inputs: IVwRootBox (root box to render), IntPtr hdc (target device context), Rect rcpDraw (drawing rectangle), uint bkclr (background color RGB), bool fDrawSel (render selection), IVwRootSite pvrs (root site for callbacks)
- Methods: DrawTheRoot(...) - main rendering entry point
- Internal: MemoryBuffer nested class for bitmap lifecycle
- COM GUID: 97199458-10C7-49da-B3AE-EA922EA64859
- **MemoryBuffer** (nested class): Manages off-screen GDI+ Bitmap and Graphics for double buffering. Creates Bitmap(width, height), gets Graphics from bitmap, acquires HDC via Graphics.GetHdc() for Views rendering, releases HDC on dispose. Implements IDisposable with proper finalizer for deterministic cleanup.

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: e70343c535764f54c8cdff93d336266d5d0a05725940ea0e83cf6264a4c44616
status: reviewed
---

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
- **MemoryBuffer** (nested class): Manages off-screen GDI+ Bitmap and Graphics for double buffering. Creates Bitmap(width, height), gets Graphics from bitmap, acquires HDC via Graphics.GetHdc() for Views rendering
