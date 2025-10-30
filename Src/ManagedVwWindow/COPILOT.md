---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ManagedVwWindow

## Purpose
Managed view window components. Provides .NET wrappers and management for native view windows, bridging the gap between managed UI code and native view rendering.

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
