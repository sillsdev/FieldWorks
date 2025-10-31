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

## Architecture
C# library with 2 source files.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library
- Build with MSBuild or Visual Studio
- Performance-critical rendering code

## Interfaces and Data Models

- **VwDrawRootBuffered** (class)
  - Path: `VwDrawRootBuffered.cs`
  - Public class implementation

## Entry Points
- Provides buffered drawing surfaces for views
- Used by view infrastructure for rendering

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **views/** - Native view layer that works with buffered rendering
- **ManagedVwWindow/** - Window management that uses buffered drawing
- **Common/RootSite/** - Root site components using buffered rendering
- **Common/SimpleRootSite/** - Simplified sites using this infrastructure

## References

- **Project files**: ManagedVwDrawRootBuffered.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, VwDrawRootBuffered.cs
- **Source file count**: 2 files
- **Data file count**: 0 files

## References (auto-generated hints)
- Project files:
  - Src/ManagedVwDrawRootBuffered/ManagedVwDrawRootBuffered.csproj
- Key C# files:
  - Src/ManagedVwDrawRootBuffered/AssemblyInfo.cs
  - Src/ManagedVwDrawRootBuffered/VwDrawRootBuffered.cs
## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 1 public classes
- **Namespaces**: SIL.FieldWorks.Views
