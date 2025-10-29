# Generic

## Purpose
Generic/shared components that don't fit a single application. Provides low-level utility classes, algorithms, and helper functions used throughout the FieldWorks codebase.

## Key Components
- **Generic.vcxproj** - Main generic utilities library
- **Test/TestGeneric.vcxproj** - Test suite for generic components

## Technology Stack
- C++ native code
- Generic algorithms and data structures
- Cross-cutting utilities

## Dependencies
- Depends on: Kernel (low-level services)
- Used by: Almost all FieldWorks components (AppCore, Common, views, etc.)

## Build Information
- Native C++ project (vcxproj)
- Includes comprehensive test suite
- Build with Visual Studio or MSBuild

## Testing
- Tests in Test/TestGeneric.vcxproj
- Unit tests for generic utilities and algorithms

## Entry Points
- Provides utility classes and functions
- Header-only or library components used throughout codebase

## Related Folders
- **Kernel/** - Lower-level infrastructure that Generic builds upon
- **AppCore/** - Application-level code that uses Generic utilities
- **Common/** - Managed code that interfaces with Generic components
- **views/** - Native views that use Generic utilities
