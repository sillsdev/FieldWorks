---
last-reviewed: 2025-10-31
last-reviewed-tree: 502aff976dc0df125c9c0a36a8ec3d95a2bb1f3d898e43c8cca93afe2b01fd03
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# XCore

## Purpose
Cross-cutting application framework (~9.8K lines in main folder + 4 subfolders) providing plugin architecture, command routing (Mediator), XML-driven UI composition (Inventory, XWindow), and extensibility infrastructure for FieldWorks applications. Implements colleague pattern (IxCoreColleague), UI adapters (IUIAdapter), property propagation (PropertyTable), and choice management. See subfolder COPILOT.md files for xCoreInterfaces/, FlexUIAdapter/, SilSidePane/, xCoreTests/ details.

## Architecture
Plugin-based application framework (~9.8K lines main + 4 subfolders) with XML-driven UI composition. Three-tier design: 1) Core framework (Mediator, PropertyTable, Inventory XML processor), 2) UI components (XWindow, CollapsingSplitContainer, MultiPane, RecordBar), 3) Plugin interfaces (IxCoreColleague, IUIAdapter). Implements colleague pattern for extensible command routing and view coordination across all FieldWorks applications.

## Key Components

### Core Framework (main folder)
- **Inventory** (Inventory.cs) - XML configuration aggregation with base/derived unification
  - `GetElement(string xpath)` - Retrieves unified config elements (layouts, parts)
  - `LoadElements(string path, string xpath)` - Loads XML from files with key attribute merging
  - Handles derivation: elements with `base` attribute unified with base elements
- **XWindow** (xWindow.cs) - Main application window implementing IxCoreColleague, IxWindow
  - Manages: m_mainSplitContainer (CollapsingSplitContainer), m_sidebar, m_recordBar, m_mainContentControl
  - Properties: ShowSidebar, ShowRecordList, persistent splitter distances
  - `Init(Mediator mediator, PropertyTable propertyTable, XmlNode config)` - XML-driven window initialization
- **Mediator** - Central command routing and colleague coordination (referenced throughout)
- **PropertyTable** - Centralized property storage and change notification

### UI Components
- **CollapsingSplitContainer** (CollapsingSplitContainer.cs) - Enhanced SplitContainer with panel collapse
- **RecordBar** (RecordBar.cs) - Navigation bar for record lists
- **MultiPane** (MultiPane.cs) - Tab control equivalent for area switching
- **PaneBarContainer** (PaneBarContainer.cs) - Container for pane bars and content
- **AdapterMenuItem** (AdapterMenuItem.cs) - Menu item with command routing

### Supporting Infrastructure
- **HtmlViewer**, **HtmlControl** (HtmlViewer.cs, HtmlControl.cs) - Embedded HTML display (Gecko/WebBrowser wrappers)
- **ImageCollection**, **ImageContent** (ImageCollection.cs, ImageContent.cs) - Image resource management
- **IconHolder** (IconHolder.cs) - Icon wrapper for UI elements
- **NotifyWindow** (NotifyWindow.cs) - Toast/notification popup
- **Ticker** (Ticker.cs) - Timer-based UI updates
- **AreaManager** (AreaManager.cs) - Area switching and configuration with DlgListenerBase
- **IncludeXml** (IncludeXml.cs) - XML inclusion helper
- **XMessageBoxExManager** (XMessageBoxExManager.cs) - MessageBoxEx adapter
- **xCoreUserControl** (xCoreUserControl.cs) - Base class for XCore-aware user controls implementing IXCoreUserControl

## Technology Stack
Language - C#

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- IxCoreColleague: Plugin interface for command handling and property access

## Threading & Performance
- UI thread: All XCore operations on main UI thread (WinForms single-threaded model)

## Config & Feature Flags
- Inventory XML: Configuration files define UI structure (layouts, commands, choices)

## Build Information
- Project type: C# class library (net48)

## Interfaces and Data Models
IxCoreColleague, IxWindow, IUIAdapter, PropertyTable, Mediator, Command.

## Entry Points
- Provides framework base classes for applications

## Test Index
Test projects: xCoreTests, xCoreInterfacesTests, SilSidePaneTests. 11 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- xWorks/ - Primary application built on XCore framework

## References
See `.cache/copilot/diff-plan.json` for file details.

## Subfolders (detailed docs in individual COPILOT.md files)
- xCoreInterfaces/ - Core interfaces: IxCoreColleague, IUIAdapter, IxCoreContentControl, etc.

## Code Evidence
*Analysis based on scanning 78 source files*
