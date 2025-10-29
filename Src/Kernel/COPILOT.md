# Kernel

## Purpose
Low-level core services and infrastructure for FieldWorks. Provides fundamental building blocks including memory management, error handling, string processing, and system-level utilities that all other components depend on.

## Key Components
- **Kernel.vcxproj** - Core kernel library
- Low-level system abstractions
- Memory management and allocation
- Error handling infrastructure
- String utilities and processing

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
