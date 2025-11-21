---
last-reviewed: 2025-11-01
last-reviewed-tree: 064a59bc4e8258fbf731ef92c4d440eebf44dd7da2d0e4963a5ba4fe942ec067
status: production
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# FlexUIAdapter

## Purpose
FLEx implementation of XCore UI adapter interfaces. Provides concrete adapters (MenuAdapter, ToolStripManager, ReBarAdapter, SidebarAdapter, PaneBar) connecting FLEx WinForms UI to XCore's command/choice framework. Implements Common/UIAdapterInterfaces (ITMAdapter, ISIBInterface) enabling XCore Mediator integration with Windows Forms controls.

## Architecture
UI adapter implementation library (~3K lines, 9 C# files) connecting XCore framework to WinForms controls. Provides MenuAdapter, ToolStripManager, ReBarAdapter, SidebarAdapter implementing ITMAdapter/ISIBInterface. Enables XCore Mediator command routing to MenuStrip, ToolStrip, and other WinForms UI elements for FLEx applications.

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
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **UI framework**: System.Windows.Forms (MenuStrip, ToolStrip, Button controls)
- **Key libraries**: XCore/xCoreInterfaces (Mediator, IxCoreColleague), Common/UIAdapterInterfaces
- **Pattern**: Adapter pattern (WinForms ↔ XCore command system)

## Dependencies
- **XCore/xCoreInterfaces**: Mediator, IxCoreColleague, ChoiceGroup
- **Common/UIAdapterInterfaces**: ITMAdapter, ISIBInterface
- **System.Windows.Forms**: MenuStrip, ToolStrip, Button controls
- **Consumer**: xWorks, LexText (FLEx UI integration)

## Interop & Contracts
- **ITMAdapter**: Menu/toolbar adapter interface (PopulateNow, CreateUIElement methods)
- **ISIBInterface**: Sidebar interface
- **IxCoreColleague**: Colleague pattern integration
- **Command binding**: Maps WinForms Click events to XCore Mediator messages
- **Dynamic UI**: Adapters rebuild UI from ChoiceGroup definitions

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
- **Target frameworks**: net48
- **Key C# files**: AdapterBase.cs, AdapterStrings.Designer.cs, AssemblyInfo.cs, BarAdapterBase.cs, ContextHelper.cs, MenuAdapter.cs, NavBarAdapter.cs, PaneBar.cs, PanelMenu.cs, ToolbarAdapter.cs
- **Source file count**: 12 files
- **Data file count**: 2 files

## Code Evidence
*Analysis based on scanning 11 source files*

- **Classes found**: 9 public classes
- **Namespaces**: XCore, XCoreUnused
