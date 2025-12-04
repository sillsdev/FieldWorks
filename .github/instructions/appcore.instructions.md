---
applyTo: "Src/AppCore/**"
name: "appcore.instructions"
description: "Auto-generated concise instructions from COPILOT.md for AppCore"
---

# AppCore (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **AfGfx** class (AfGfx.h/cpp): Static utility methods for Windows GDI operations
- `LoadSysColorBitmap()`: Loads system-colored bitmaps from resources
- `FillSolidRect()`: Fills rectangle with solid color using palette
- `InvertRect()`, `CreateSolidBrush()`: Rectangle inversion and brush creation
- `SetBkColor()`, `SetTextColor()`: Palette-aware color setting
- `DrawBitMap()`: Bitmap drawing with source/dest rectangles

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: d533214a333e8de29f0eaa52ed6bbffd80815cfb0f1f3fac15cd08b96aafb15e
status: draft
---

# AppCore COPILOT summary

## Purpose
Provides Windows GDI wrapper classes and graphics utilities for FieldWorks native applications. Includes device context management (SmartDc), GDI object wrappers (FontWrap, BrushWrap, PenWrap, RgnWrap), color palette support (ColorTable with 40 predefined colors, SmartPalette), and writing system style inheritance utilities (FwStyledText namespace). These utilities abstract Windows graphics APIs and provide consistent rendering behavior across FieldWorks.

## Architecture
C++ native header-only library. Headers and implementation files are designed to be included into consumer projects (primarily views) via include search paths rather than built as a standalone library. The code provides three major areas: graphics primitives and GDI abstractions (AfGfx, AfGdi), styled text property management (FwStyledText namespace), and color management (ColorTable global singleton).

## Key Components
- **AfGfx** class (AfGfx.h/cpp): Static utility methods for Windows GDI operations
  - `LoadSysColorBitmap()`: Loads system-colored bitmaps from resources
  - `FillSolidRect()`: Fills rectangle with solid color using palette
  - `InvertRect()`, `CreateSolidBrush()`: Rectangle inversion and brush creation
  - `SetBkColor()`, `SetTextColor()`: Palette-aware color setting
  - `DrawBitMap()`: Bitmap drawing with source/dest rectangles
  - `EnsureVisibleRect()`: Validates/adjusts rectangle visibility
- **AfGdi** class (AfGfx.h): Tracked wrappers for GDI resource creation/destruction with leak detection
  - Device context tracking: `CreateDC()`, `CreateCompatibleDC()`, `DeleteDC()`, `GetDC()`, `ReleaseDC()`
  - Font tracking: `CreateFont()`, `CreateFontIndirect()`, `DeleteObject()` with s_cFonts counter
  - GDI object tracking: `CreatePen()`, `CreateBrush()`, `SelectObject()` with debug counters
  - Debug flags: `s_fSh
