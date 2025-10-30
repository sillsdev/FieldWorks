---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# AppCore

## Purpose
Shared application core helpers and base infrastructure used across FieldWorks applications.
Provides fundamental graphics rendering capabilities through Windows GDI wrappers (AfGfx, AfGdi),
styled text rendering (FwStyledText), and resource management utilities (ColorTable, SmartPalette).
These classes enable consistent rendering behavior and provide abstraction over Windows graphics APIs.

## Key Components
### Key Classes
- **AfGdi**
- **AfGfx**
- **SmartPalette**
- **SmartDc**
- **FontWrap**
- **BrushWrap**
- **PenWrap**
- **RgnWrap**
- **ClipRgnWrap**
- **ColorTable**

## Technology Stack
- C++ native code
- Graphics rendering primitives
- Windows GDI/GDI+ integration

## Dependencies
- Depends on: Kernel (low-level services), Generic (shared components)
- Used by: All major FieldWorks UI applications (xWorks, LexText)

## Build Information
- Native C++ project (no .csproj or .vcxproj in root, built as part of larger solution)
- Compiled as a library/DLL
- Build via top-level solution or Build/ scripts

## Entry Points
- Provides base classes and utilities used by application-level components
- Not directly executable; linked into applications

## Related Folders
- **Kernel/** - Provides low-level infrastructure that AppCore builds upon
- **Generic/** - Shares generic utilities with AppCore
- **xWorks/** - Primary consumer of AppCore functionality
- **LexText/** - Uses AppCore for text rendering in lexicon views

## Code Evidence
*Analysis based on scanning 8 source files*

- **Classes found**: 12 public classes

## Interfaces and Data Models

- **AfGdi** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **AfGfx** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **BrushWrap** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **ChrpInheritance** (class)
  - Path: `FwStyledText.h`
  - Public class implementation

- **ClipRgnWrap** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **ColorTable** (class)
  - Path: `AfColorTable.h`
  - Public class implementation

- **FontWrap** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **PenWrap** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **RgnWrap** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **SmartDc** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **SmartPalette** (class)
  - Path: `AfGfx.h`
  - Public class implementation

- **WsStyleInfo** (class)
  - Path: `FwStyledText.h`
  - Public class implementation

## References

- **Key C++ files**: AfColorTable.cpp, AfGfx.cpp, FwStyledText.cpp
- **Key headers**: AfAppRes.h, AfColorTable.h, AfDef.h, AfGfx.h, FwStyledText.h
- **Source file count**: 8 files
- **Data file count**: 0 files

## Architecture
C++ native library with 3 implementation files and 5 headers.

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## References (auto-generated hints)
- Key C++ files:
  - Src\AppCore\AfColorTable.cpp
  - Src\AppCore\AfGfx.cpp
  - Src\AppCore\FwStyledText.cpp
- Key headers:
  - Src\AppCore\AfColorTable.h
  - Src\AppCore\AfDef.h
  - Src\AppCore\AfGfx.h
  - Src\AppCore\FwStyledText.h
  - Src\AppCore\Res\AfAppRes.h
