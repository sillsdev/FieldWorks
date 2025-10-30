---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ManagedVwWindow

## Purpose
Managed .NET wrappers for native view window components.
Bridges managed UI code with the native views rendering engine, enabling .NET applications
to host sophisticated text display views that leverage the native rendering capabilities.
Essential for integrating the powerful native views system into WinForms applications.

## Key Components
### Key Classes
- **ManagedVwWindow**
- **ManagedVwWindowTests**

## Technology Stack
- C# .NET with C++/CLI interop
- Windows Forms integration
- Native window handle management

## Dependencies
- Depends on: views (native view layer), Common (UI infrastructure), ManagedVwDrawRootBuffered
- Used by: All applications with view-based UI (xWorks, LexText)

## Build Information
- C# class library with native interop
- Includes test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Provides managed window classes for views
- Bridge between managed UI and native rendering

## Related Folders
- **views/** - Native view layer that ManagedVwWindow wraps
- **ManagedVwDrawRootBuffered/** - Buffered rendering used by windows
- **Common/RootSite/** - Root site components using managed windows
- **xWorks/** - Applications using view windows
- **LexText/** - Uses view windows for text display

## Code Evidence
*Analysis based on scanning 3 source files*

- **Classes found**: 2 public classes
- **Namespaces**: SIL.FieldWorks.Language, SIL.FieldWorks.Views

## Interfaces and Data Models

- **ManagedVwWindow** (class)
  - Path: `ManagedVwWindow.cs`
  - Public class implementation

## References

- **Project files**: ManagedVwWindow.csproj, ManagedVwWindowTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ManagedVwWindow.cs, ManagedVwWindowTests.cs
- **Source file count**: 3 files
- **Data file count**: 0 files

## Architecture
C# library with 3 source files. Contains 1 subprojects: ManagedVwWindow.

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
Test projects: ManagedVwWindowTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## References (auto-generated hints)
- Project files:
  - Src\ManagedVwWindow\ManagedVwWindow.csproj
  - Src\ManagedVwWindow\ManagedVwWindowTests\ManagedVwWindowTests.csproj
- Key C# files:
  - Src\ManagedVwWindow\AssemblyInfo.cs
  - Src\ManagedVwWindow\ManagedVwWindow.cs
  - Src\ManagedVwWindow\ManagedVwWindowTests\ManagedVwWindowTests.cs
