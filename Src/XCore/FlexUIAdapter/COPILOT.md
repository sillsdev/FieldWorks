---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FlexUIAdapter

## Purpose
FLEx implementation of XCore UI adapter interfaces.
Provides concrete adapter implementations that connect FLEx application components to the
XCore framework's command handling, choice management, and UI composition systems. Enables
FLEx to leverage XCore's plugin architecture and extensibility features.

## Key Components
### Key Classes
- **ToolStripManager**
- **ReBarAdapter**
- **ContextHelper**
- **SidebarAdapter**
- **MenuAdapter**
- **BarAdapterBase**
- **PaneBar**
- **AdapterBase**
- **PanelCollection**

## Technology Stack
- C# .NET WinForms
- Adapter pattern implementation
- XCore framework integration

## Dependencies
- Depends on: XCore/xCoreInterfaces, Common/UIAdapterInterfaces
- Used by: xWorks, LexText (for UI integration)

## Build Information
- C# class library project
- Build via: `dotnet build FlexUIAdapter.csproj`
- UI adapter implementation

## Entry Points
- Adapter base classes for UI components
- Context helpers for command routing
- Accessibility support

## Related Folders
- **XCore/xCoreInterfaces/** - Interfaces implemented by adapters
- **Common/UIAdapterInterfaces/** - Additional adapter interfaces
- **XCore/** - Framework using these adapters
- **xWorks/** - Application using UI adapters

## Code Evidence
*Analysis based on scanning 11 source files*

- **Classes found**: 9 public classes
- **Namespaces**: XCore, XCoreUnused

## Interfaces and Data Models

- **AdapterBase** (class)
  - Path: `AdapterBase.cs`
  - Public class implementation

- **BarAdapterBase** (class)
  - Path: `BarAdapterBase.cs`
  - Public class implementation

- **ContextHelper** (class)
  - Path: `ContextHelper.cs`
  - Public class implementation

- **MenuAdapter** (class)
  - Path: `MenuAdapter.cs`
  - Public class implementation

- **PaneBar** (class)
  - Path: `PaneBar.cs`
  - Public class implementation

- **PanelCollection** (class)
  - Path: `SidebarAdapter.cs`
  - Public class implementation

- **ReBarAdapter** (class)
  - Path: `ToolbarAdapter.cs`
  - Public class implementation

- **SidebarAdapter** (class)
  - Path: `NavBarAdapter.cs`
  - Public class implementation

- **ToolStripManager** (class)
  - Path: `ToolbarAdapter.cs`
  - Public class implementation

## References

- **Project files**: FlexUIAdapter.csproj
- **Target frameworks**: net462
- **Key C# files**: AdapterBase.cs, AdapterStrings.Designer.cs, AssemblyInfo.cs, BarAdapterBase.cs, ContextHelper.cs, MenuAdapter.cs, NavBarAdapter.cs, PaneBar.cs, PanelMenu.cs, ToolbarAdapter.cs
- **Source file count**: 12 files
- **Data file count**: 2 files

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
