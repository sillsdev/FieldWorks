---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# UIAdapterInterfaces

## Purpose
UI adapter pattern interfaces for abstraction and testability.
Defines contracts that allow UI components to be adapted to different technologies or replaced
with test doubles. Enables better separation of concerns between business logic and UI presentation,
and facilitates unit testing of UI-dependent code.

## Key Components
### Key Classes
- **TMItemProperties**
- **TMBarProperties**
- **WindowListInfo**
- **SBTabProperties**
- **SBTabItemProperties**

### Key Interfaces
- **ISIBInterface**
- **ITMAdapter**

## Technology Stack
- C# .NET
- Interface-based design
- Adapter pattern implementation

## Dependencies
- Depends on: Minimal (interface definitions)
- Used by: XCore, UI adapter implementations

## Build Information
- C# interface library project
- Build via: `dotnet build UIAdapterInterfaces.csproj`
- Pure interface definitions

## Entry Points
- Interface contracts for UI adapters
- Abstraction layer for UI technologies

## Related Folders
- **XCore/** - Uses these interfaces extensively
- **XCore/FlexUIAdapter/** - Implements these interfaces
- **Common/Controls/** - Controls that work with adapters

## Code Evidence
*Analysis based on scanning 4 source files*

- **Classes found**: 5 public classes
- **Interfaces found**: 2 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.UIAdapters

## Interfaces and Data Models

- **ISIBInterface** (interface)
  - Path: `SIBInterface.cs`
  - Public interface definition

- **ITMAdapter** (interface)
  - Path: `TMInterface.cs`
  - Public interface definition

- **SBTabItemProperties** (class)
  - Path: `HelperClasses.cs`
  - Public class implementation

- **SBTabProperties** (class)
  - Path: `HelperClasses.cs`
  - Public class implementation

- **TMBarProperties** (class)
  - Path: `HelperClasses.cs`
  - Public class implementation

- **TMItemProperties** (class)
  - Path: `HelperClasses.cs`
  - Public class implementation

- **WindowListInfo** (class)
  - Path: `HelperClasses.cs`
  - Public class implementation

## References

- **Project files**: UIAdapterInterfaces.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, HelperClasses.cs, SIBInterface.cs, TMInterface.cs, UIAdapterInterfacesStrings.Designer.cs
- **Source file count**: 5 files
- **Data file count**: 1 files

## Architecture
C# library with 5 source files.

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.
