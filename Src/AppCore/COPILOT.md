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
Thread-agnostic code. GDI resources (DCs, fonts, brushes) have thread affinity and require careful handling in multi-threaded scenarios; AppCore does not enforce thread safety. Smart wrapper classes use RAII for deterministic cleanup within a single thread. Performance notes: AfGdi tracking adds overhead in debug builds via counters and optional logging; release builds should disable tracking. ColorTable maintains global singleton (g_ct) initialized at startup.

## Config & Feature Flags
Debug-only resource tracking controlled by static flags:
- `AfGdi::s_fShowDCs`: When true, logs DC allocation/deallocation to debug output
- `AfGdi::s_fShowFonts`: When true, logs font allocation/deallocation to debug output
No runtime configuration files.

## Build Information
- No standalone project file; this is a header-only library consumed via include paths
- Build using the top-level FieldWorks.sln (Visual Studio/MSBuild)
- Consumer projects (Kernel.vcxproj, views.vcxproj) reference AppCore via NMakeIncludeSearchPath
- Do not attempt to build AppCore in isolation; it is included directly into consumer C++ projects

## Interfaces and Data Models

- **AfGfx** (AfGfx.h/cpp)
  - Purpose: Static utility class providing Windows GDI helper functions
  - Inputs: HDC, COLORREF, Rect, HBITMAP, resource IDs
  - Outputs: Modified DC state, drawn graphics, created GDI objects
  - Notes: All methods are static; acts as namespace for GDI utilities

- **AfGdi** (AfGfx.h)
  - Purpose: Tracked wrappers for GDI resource lifecycle with leak detection
  - Inputs: Same parameters as native Windows GDI APIs
  - Outputs: GDI HANDLEs (HDC, HFONT, HBRUSH, HPEN, HRGN)
  - Notes: Debug builds maintain counters (s_cDCs, s_cFonts); check at shutdown for leaks

- **SmartDc** (AfGfx.h)
  - Purpose: RAII wrapper for device context automatic cleanup
  - Inputs: Constructor takes HWND or creates compatible DC
  - Outputs: Provides HDC via conversion operator; releases DC in destructor
  - Notes: Non-copyable; use for automatic GetDC/ReleaseDC pairing

- **SmartPalette** (AfGfx.h)
  - Purpose: RAII wrapper for palette selection with automatic restore
  - Inputs: HDC, HPALETTE
  - Outputs: Selects and realizes palette; restores previous palette in destructor
  - Notes: Non-copyable; use when temporarily changing palette

- **Smart GDI object wrappers** (AfGfx.h)
  - FontWrap, BrushWrap, PenWrap, RgnWrap, ClipRgnWrap
  - Purpose: RAII wrappers for SelectObject/RestoreObject pairing
  - Inputs: HDC, HGDIOBJ (font, brush, pen, region)
  - Outputs: Selects object; restores previous object in destructor
  - Notes: Non-copyable; use for automatic GDI object restoration

- **FwStyledText::ComputeInheritance** (FwStyledText.h/cpp)
  - Purpose: Merges base and override text properties to compute effective properties
  - Inputs: ITsTextProps* pttpBase, ITsTextProps* pttpOverride
  - Outputs: ITsTextProps** ppttpEffect (computed effective properties)
  - Notes: Implements inheritance logic for text properties (soft/hard formatting)

- **FwStyledText::DecodeFontPropsString** (FwStyledText.h/cpp)
  - Purpose: Parses BSTR font property string into structured data
  - Inputs: BSTR bstr (encoded font properties), bool fExplicit
  - Outputs: Vectors of WsStyleInfo, ChrpInheritance, writing system IDs
  - Notes: Complex parsing logic for writing system-specific font properties

- **FwStyledText::EncodeFontPropsString** (FwStyledText.h/cpp)
  - Purpose: Encodes structured font properties into BSTR format
  - Inputs: Vector<WsStyleInfo> vesi, bool fForPara
  - Outputs: StrUni (encoded font property string)
  - Notes: Inverse of DecodeFontPropsString; produces compact encoding

- **ChrpInheritance** (FwStyledText.h)
  - Purpose: Tracks inheritance state of character rendering properties
  - Inputs: Constructed from base and override ITsTextProps
  - Outputs: Fields indicate kxInherited/kxExplicit/kxConflicting for each property
  - Notes: Used by formatting dialogs to show soft vs. hard formatting

- **WsStyleInfo** (FwStyledText.h)
  - Purpose: Stores per-writing-system style information
  - Inputs: Writing system ID, font properties (name, size, bold, italic, etc.)
  - Outputs: Structured representation used in encoding/decoding
  - Notes: Part of complex writing system style inheritance system

- **ColorTable** (AfColorTable.h/cpp)
  - Purpose: Manages application color table with 40 predefined colors
  - Inputs: Color index (0-39), COLORREF values
  - Outputs: COLORREF, string resource IDs, palette entries
  - Notes: Global singleton g_ct; palette created in constructor for legacy hardware

- **ColorTable::RealizePalette** (AfColorTable.h/cpp)
  - Purpose: Maps logical palette to system palette for quality drawing
  - Inputs: HDC
  - Outputs: HPALETTE (old palette, or NULL if device doesn't support palettes)
  - Notes: Only relevant for legacy hardware with <16-bit color depth

## Entry Points
- Included via `#include "AfGfx.h"`, `#include "FwStyledText.h"`, `#include "AfColorTable.h"` in consumer C++ code
- Primary consumer: views/Main.h includes Res/AfAppRes.h for resource IDs
- Kernel and views projects reference AppCore via NMakeIncludeSearchPath
- ColorTable global singleton `g_ct` available after static initialization

## Test Index
No tests found in this folder. Tests may be in consumer projects (views, Kernel) or separate test assemblies.

## Usage Hints
- Include AfGfx.h for Windows GDI utilities and RAII wrappers
- Include FwStyledText.h for writing system style inheritance and font property encoding/decoding
- Include AfColorTable.h for access to predefined color table and global `g_ct` singleton
- Use smart wrappers (SmartDc, SmartPalette, FontWrap, etc.) for automatic GDI resource cleanup
- Enable `AfGdi::s_fShowDCs` or `AfGdi::s_fShowFonts` in debug builds to log resource allocation
- Check `AfGdi::s_cDCs` and `AfGdi::s_cFonts` counters at shutdown to detect GDI leaks
- Use `g_ct` global ColorTable to map color indices to RGB values and string resource IDs
- Use FwStyledText namespace functions to compute style inheritance for multi-writing-system text

## Related Folders
- **views/**: Primary consumer; includes AfAppRes.h for resource IDs
- **Kernel/**: References AppCore in include search paths; provides low-level infrastructure
- **Generic/**: Peer utilities folder; provides base types, vectors, smart pointers

## References
- **Project files**: None (header-only library)
- **Key C++ files**: AfColorTable.cpp (195 lines), AfGfx.cpp (1340 lines), FwStyledText.cpp (1483 lines)
- **Key headers**: AfColorTable.h (110 lines), AfDef.h (196 lines), AfGfx.h (702 lines), FwStyledText.h (218 lines), Res/AfAppRes.h (454 lines)
- **Total lines of code**: 4698
- **Include search paths**: Referenced by Kernel.vcxproj and views.vcxproj (..\AppCore)
- **Consumer references**: Src/views/Main.h includes "../../../Src/AppCore/Res/AfAppRes.h"
- **Global singleton**: ColorTable g_ct (declared extern in AfColorTable.h, defined in AfColorTable.cpp)

## References (auto-generated hints)
- Key C++ files:
  - Src/AppCore/AfColorTable.cpp
  - Src/AppCore/AfGfx.cpp
  - Src/AppCore/FwStyledText.cpp
- Key headers:
  - Src/AppCore/AfColorTable.h
  - Src/AppCore/AfDef.h
  - Src/AppCore/AfGfx.h
  - Src/AppCore/FwStyledText.h
  - Src/AppCore/Res/AfAppRes.h
