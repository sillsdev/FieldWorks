---
last-reviewed: 2025-10-31
last-reviewed-tree: d533214a333e8de29f0eaa52ed6bbffd80815cfb0f1f3fac15cd08b96aafb15e
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - upstream-consumes
  - downstream-consumed-by
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
  - Debug flags: `s_fShowDCs`, `s_fShowFonts` enable allocation/deallocation logging
  - `OutputDC()`: Debug output for device context state
- **Smart RAII wrappers** (AfGfx.h): Automatic resource cleanup via destructors
  - `SmartDc`: Device context with automatic GetDC/ReleaseDC pairing
  - `SmartPalette`: Palette selection/realization with automatic restore
  - `FontWrap`, `BrushWrap`, `PenWrap`, `RgnWrap`, `ClipRgnWrap`: GDI object selection with automatic restoration
- **FwStyledText namespace** (FwStyledText.h/cpp): Writing system style inheritance and font property encoding
  - `ComputeInheritance()`: Merges base and override ITsTextProps to compute effective properties
  - `ComputeWsStyleInheritance()`: Computes writing system style string inheritance (BSTR-based)
  - `WsStylesPropList()`: Returns list of text property types used in WS styles
  - `MergeIntProp()`: Merges integer-valued property with inheritance rules
  - `ZapWsStyle()`: Removes specific property from WS style string
  - `DecodeFontPropsString()`: Parses BSTR font property encoding into vectors of WsStyleInfo and ChrpInheritance
  - `EncodeFontPropsString()`: Encodes structured font properties back to BSTR format
  - `ConvertDefaultFontInput()`: Normalizes default font input strings
  - `FontStringMarkupToUi()`, `FontStringUiToMarkup()`: Converts between markup and UI representations
  - `FontUiStrings()`: Returns list of UI-friendly font strings
  - `FontDefaultMarkup()`, `FontDefaultUi()`: Returns default font strings
  - `MatchesDefaultSerifMarkup()`, `MatchesDefaultSansMarkup()`, `MatchesDefaultBodyFontMarkup()`, `MatchesDefaultMonoMarkup()`: Tests if string matches default font markup
  - `FontMarkupToFontName()`: Extracts font name from markup string
  - `RemoveSpuriousOverrides()`: Cleans up WS style string by removing redundant overrides
- **ChrpInheritance** class (FwStyledText.h): Tracks inheritance state of character rendering properties
  - Fields indicate whether each property is kxInherited (soft), kxExplicit (hard), or kxConflicting
  - Used by hard/soft formatting dialogs to display inheritance state
- **WsStyleInfo** class (FwStyledText.h): Stores per-writing-system style information
  - Writing system ID, font properties (name, size, bold, italic, etc.)
  - Used in font property encoding/decoding operations
- **ColorTable** class (AfColorTable.h/cpp): Manages application color table with 40 predefined colors
  - `ColorDefn` struct: Pairs string resource ID (kstidBlack, kstidRed, etc.) with RGB value
  - `Size()`: Returns number of colors (40)
  - `GetColor()`: Returns RGB value for color index
  - `GetColorRid()`: Returns string resource ID for color index
  - `GetIndexFromColor()`: Finds color index from RGB value
  - `RealizePalette()`: Maps logical palette to system palette for quality drawing
  - Global singleton `g_ct` declared as extern
- **AfDef.h**: Command IDs, control IDs, and string resource IDs (196 lines)
  - Command IDs: kcidFileNew, kcidEditCut, kcidFmtFnt, etc.
  - Color string IDs: kstidBlack through kstidWhite (40 colors)
  - Menu/toolbar/accelerator resource IDs
- **Res/AfAppRes.h**: Resource header with bitmap and icon IDs (454 lines)

## Technology Stack
- C++ native code (no project file; header-only/include-based library)
- Windows GDI/GDI+ APIs (HDC, HFONT, HBRUSH, HPEN, HRGN, HBITMAP, HPALETTE)
- ITsTextProps interfaces for text property management (defined in other FieldWorks components)
- Target: Windows native C++ (integrated into consumer projects via include paths)

## Dependencies

### Upstream (consumes)
- **Kernel**: Low-level infrastructure and base types (include search path dependency)
- **Generic**: Generic utilities, vectors, smart pointers (include search path dependency)
- **Windows GDI/GDI+**: System APIs for device contexts, fonts, brushes, pens, regions, palettes
- **ITsTextProps interfaces**: Text property interfaces (likely from views or other FieldWorks components)

### Downstream (consumed by)
- **views**: Primary consumer; views/Main.h includes "../../../Src/AppCore/Res/AfAppRes.h"
- **Kernel**: References AppCore in NMakeIncludeSearchPath (..\AppCore)
- Any native C++ code needing Windows GDI abstraction or styled text property management

## Interop & Contracts
No COM/PInvoke boundaries. Pure native C++ code consumed via `#include` directives. Provides RAII wrappers around Windows GDI HANDLEs to ensure proper cleanup via destructors. Debug builds track GDI resource allocations (s_cDCs, s_cFonts) to detect leaks via static counters.

## Threading & Performance
Thread-agnostic; GDI resources have thread affinity. RAII wrappers ensure cleanup. Debug tracking adds overhead; global ColorTable singleton.

## Config & Feature Flags
Debug flags: AfGdi::s_fShowDCs, AfGdi::s_fShowFonts for allocation logging. No runtime config.

## Build Information
Header-only library consumed via include paths. Build using FieldWorks.sln. Consumer projects reference via NMakeIncludeSearchPath.

## Interfaces and Data Models
AfGfx (GDI utilities), AfGdi (tracked wrappers), Smart RAII wrappers (SmartDc, SmartPalette, FontWrap, etc.), FwStyledText (property inheritance), ColorTable (40 color palette).

## Entry Points
Included via #include directives. Primary consumer: views/Main.h. Global ColorTable singleton g_ct.

## Test Index
No tests in this folder. Tests may be in consumer projects (views, Kernel).

## Usage Hints
Include AfGfx.h (GDI utilities), FwStyledText.h (style inheritance), AfColorTable.h (color table). Use smart wrappers for automatic cleanup.

## Related Folders
views (primary consumer), Kernel (low-level infrastructure), Generic (base types).

## References
Header-only library (4698 lines). Key files: AfColorTable.cpp, AfGfx.cpp, FwStyledText.cpp, AfGfx.h, FwStyledText.h, Res/AfAppRes.h. Global singleton: ColorTable g_ct. See `.cache/copilot/diff-plan.json` for details.
