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
Language - C#

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- ITMAdapter: Menu/toolbar adapter interface (PopulateNow, CreateUIElement methods)

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project

## Interfaces and Data Models
AdapterBase, BarAdapterBase, ContextHelper, MenuAdapter, PaneBar, PanelCollection, ReBarAdapter, SidebarAdapter, ToolStripManager.

## Entry Points
- Adapter base classes for UI components

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- XCore/xCoreInterfaces/ - Interfaces implemented by adapters

## References
See `.cache/copilot/diff-plan.json` for file details.

## Code Evidence
*Analysis based on scanning 11 source files*
