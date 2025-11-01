---
last-reviewed: 2025-11-01
last-verified-commit: HEAD
status: production
---

# FlexUIAdapter

## Purpose
FLEx implementation of XCore UI adapter interfaces. Provides concrete adapters (MenuAdapter, ToolStripManager, ReBarAdapter, SidebarAdapter, PaneBar) connecting FLEx WinForms UI to XCore's command/choice framework. Implements Common/UIAdapterInterfaces (ITMAdapter, ISIBInterface) enabling XCore Mediator integration with Windows Forms controls.

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

## Dependencies
- **XCore/xCoreInterfaces**: Mediator, IxCoreColleague, ChoiceGroup
- **Common/UIAdapterInterfaces**: ITMAdapter, ISIBInterface
- **System.Windows.Forms**: MenuStrip, ToolStrip, Button controls
- **Consumer**: xWorks, LexText (FLEx UI integration)

## Build Information
- **Project**: FlexUIAdapter.csproj
- **Type**: Library (.NET Framework 4.6.2)
- **Namespace**: SIL.FieldWorks.XWorks
- **Source files**: 12 files (~3239 lines)

## Test Index
No dedicated test project (integration tested via xWorks).

## Related Folders
- **XCore/xCoreInterfaces/**: Mediator, IxCoreColleague interfaces
- **Common/UIAdapterInterfaces/**: ITMAdapter, ISIBInterface
- **xWorks/**: Main consumer (FwXApp, FwXWindow)

## References
- **SIL.FieldWorks.XWorks.IxCoreColleague**: XCore colleague pattern
- **SIL.FieldWorks.Common.UIAdapterInterfaces.ITMAdapter**: Adapter interface

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
