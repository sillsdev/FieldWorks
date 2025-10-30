---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ManagedVwDrawRootBuffered

## Purpose
Managed wrapper for buffered view rendering infrastructure.
Implements double-buffered drawing to eliminate flicker in complex text displays.
Provides smooth rendering experience for views containing large amounts of formatted text
with multiple writing systems and complex layouts.

## Key Components
### Key Classes
- **VwDrawRootBuffered**

## Technology Stack
- C# .NET with GDI+ or DirectX
- Double-buffered rendering
- View drawing optimization

## Dependencies
- Depends on: views (native view layer), Common (UI infrastructure)
- Used by: All view-based UI components requiring smooth rendering

## Build Information
- C# class library
- Build with MSBuild or Visual Studio
- Performance-critical rendering code

## Entry Points
- Provides buffered drawing surfaces for views
- Used by view infrastructure for rendering

## Related Folders
- **views/** - Native view layer that works with buffered rendering
- **ManagedVwWindow/** - Window management that uses buffered drawing
- **Common/RootSite/** - Root site components using buffered rendering
- **Common/SimpleRootSite/** - Simplified sites using this infrastructure

## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 1 public classes
- **Namespaces**: SIL.FieldWorks.Views

## Interfaces and Data Models

- **VwDrawRootBuffered** (class)
  - Path: `VwDrawRootBuffered.cs`
  - Public class implementation

## References

- **Project files**: ManagedVwDrawRootBuffered.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, VwDrawRootBuffered.cs
- **Source file count**: 2 files
- **Data file count**: 0 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\ManagedVwDrawRootBuffered\ManagedVwDrawRootBuffered.csproj
- Key C# files:
  - Src\ManagedVwDrawRootBuffered\AssemblyInfo.cs
  - Src\ManagedVwDrawRootBuffered\VwDrawRootBuffered.cs
