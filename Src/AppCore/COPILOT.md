---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# AppCore

## Purpose
Shared application core helpers and base infrastructure used across FieldWorks applications. Provides fundamental graphics and styled text rendering capabilities for the application layer.

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
