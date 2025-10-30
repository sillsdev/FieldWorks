---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Kernel

## Purpose
Low-level core services and fundamental infrastructure for all FieldWorks native code.
Provides essential building blocks including memory management abstractions, string processing utilities,
error handling mechanisms, and system-level services. Acts as the foundation layer that all other
native components depend on. Contains core definitions, constants, and fundamental types used throughout.

## Key Components
No major public classes identified.

## Technology Stack
- C++ native code
- Windows system APIs
- Low-level abstractions

## Dependencies
- Depends on: System libraries, STL, Windows SDK
- Used by: All FieldWorks components (acts as foundation layer)

## Build Information
- Native C++ project (vcxproj)
- Critical foundation library
- Build with Visual Studio or MSBuild

## Entry Points
- Provides fundamental classes and utilities
- Base layer for all other native code

## Related Folders
- **Generic/** - Builds on Kernel infrastructure
- **AppCore/** - Application-level code built on Kernel
- **views/** - Native views using Kernel services
- **DebugProcs/** - Debugging utilities that instrument Kernel
- All other native components depend on Kernel

## Code Evidence
*Analysis based on scanning 4 source files*

## References

- **Project files**: Kernel.vcxproj
- **Key C++ files**: FwKernel_GUIDs.cpp
- **Key headers**: CellarBaseConstants.h, CellarConstants.vm.h
- **Source file count**: 5 files
- **Data file count**: 0 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\Kernel\Kernel.vcxproj
- Key C++ files:
  - Src\Kernel\FwKernel_GUIDs.cpp
  - Src\Kernel\dlldatax.c
- Key headers:
  - Src\Kernel\CellarBaseConstants.h
  - Src\Kernel\CellarConstants.vm.h
