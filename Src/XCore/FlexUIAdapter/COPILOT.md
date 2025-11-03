---
last-reviewed: 2025-11-01
last-reviewed-tree: 2e44e70b55e127e774e0b3bb925e15cc49637efb7b222758f1e3f8f503ae8a68
status: production
---

# FlexUIAdapter

## Purpose
FLEx implementation of XCore UI adapter interfaces. Provides concrete adapters (MenuAdapter, ToolStripManager, ReBarAdapter, SidebarAdapter, PaneBar) connecting FLEx WinForms UI to XCore's command/choice framework. Implements Common/UIAdapterInterfaces (ITMAdapter, ISIBInterface) enabling XCore Mediator integration with Windows Forms controls.

## Architecture
TBD - populate from code. See auto-generated hints below.

## Key Components

### Adapter Classes (~3K lines)
- **AdapterBase**: Base adapter with IxCoreColleague integration
- **MenuAdapter**: MenuStrip/ContextMenuStrip→XCore command binding
- **ToolStripManager**: ToolStrip→XCore command integration
- **ReBarAdapter**: Rebar/toolbar management
- **SidebarAdapter**: Sidebar button/item control
- **PaneBar**: Pane bar UI element
- **BarAdapterBase**: Base for bar-style adapters
- **ContextHelper**: Context menu helpers

### Supporting (~200 lines)
- **PanelCollection**: Panel container management

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **XCore/xCoreInterfaces**: Mediator, IxCoreColleague, ChoiceGroup
- **Common/UIAdapterInterfaces**: ITMAdapter, ISIBInterface
- **System.Windows.Forms**: MenuStrip, ToolStrip, Button controls
- **Consumer**: xWorks, LexText (FLEx UI integration)

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

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
