---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Kernel

## Purpose
Low-level core services and infrastructure for FieldWorks. Provides fundamental building blocks including memory management, error handling, string processing, and system-level utilities that all other components depend on.

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

