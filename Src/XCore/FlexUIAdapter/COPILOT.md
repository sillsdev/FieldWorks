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

## Architecture
C# library with 12 source files.

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

## Interop & Contracts
Uses P/Invoke for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build FlexUIAdapter.csproj`
- UI adapter implementation

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

## Entry Points
- Adapter base classes for UI components
- Context helpers for command routing
- Accessibility support

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **XCore/xCoreInterfaces/** - Interfaces implemented by adapters
- **Common/UIAdapterInterfaces/** - Additional adapter interfaces
- **XCore/** - Framework using these adapters
- **xWorks/** - Application using UI adapters

## References

- **Project files**: FlexUIAdapter.csproj
- **Target frameworks**: net462
- **Key C# files**: AdapterBase.cs, AdapterStrings.Designer.cs, AssemblyInfo.cs, BarAdapterBase.cs, ContextHelper.cs, MenuAdapter.cs, NavBarAdapter.cs, PaneBar.cs, PanelMenu.cs, ToolbarAdapter.cs
- **Source file count**: 12 files
- **Data file count**: 2 files

## References (auto-generated hints)
- Project files:
  - XCore/FlexUIAdapter/FlexUIAdapter.csproj
- Key C# files:
  - XCore/FlexUIAdapter/AdapterBase.cs
  - XCore/FlexUIAdapter/AdapterStrings.Designer.cs
  - XCore/FlexUIAdapter/AssemblyInfo.cs
  - XCore/FlexUIAdapter/BarAdapterBase.cs
  - XCore/FlexUIAdapter/ContextHelper.cs
  - XCore/FlexUIAdapter/MenuAdapter.cs
  - XCore/FlexUIAdapter/NavBarAdapter.cs
  - XCore/FlexUIAdapter/PaneBar.cs
  - XCore/FlexUIAdapter/PanelButton.cs
  - XCore/FlexUIAdapter/PanelMenu.cs
  - XCore/FlexUIAdapter/SidebarAdapter.cs
  - XCore/FlexUIAdapter/ToolbarAdapter.cs
- Data contracts/transforms:
  - XCore/FlexUIAdapter/AdapterStrings.resx
  - XCore/FlexUIAdapter/PaneBar.resx
## Code Evidence
*Analysis based on scanning 11 source files*

- **Classes found**: 9 public classes
- **Namespaces**: XCore, XCoreUnused
