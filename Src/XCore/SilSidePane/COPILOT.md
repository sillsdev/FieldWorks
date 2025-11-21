---
last-reviewed: 2025-11-01
last-reviewed-tree: 5c0428af86d4d8c6b7829d245dd8bd3a610718aca9563315255e6c5b43a1e58e
status: production
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# SilSidePane

## Purpose
Side pane navigation control for FieldWorks multi-area interface. Provides SidePane, Tab, and Item classes implementing hierarchical navigation sidebar (similar to Outlook bar). Enables area/tool switching in FLEx and other FieldWorks apps. Includes OutlookBarButton rendering, drag-and-drop tab reordering, and NavPaneOptionsDlg customization.

## Architecture
Side pane navigation control library (~3K lines) implementing hierarchical sidebar (Outlook bar style). Provides SidePane, Tab, Item classes with OutlookBarButton custom rendering, drag-drop tab reordering, NavPaneOptionsDlg customization. Enables area/tool switching in FieldWorks multi-area interface.

## Key Components

### Core Classes (~2K lines)
- **SidePane**: Main side pane control (UserControl)
  - Manages Tab collection, item selection, context menus
  - Supports button/list/strip-list display modes
  - Drag-and-drop tab reordering
- **Tab**: Individual tab (area) in side pane
  - Contains Item collection, icon, label
- **Item**: Individual navigation item within tab
  - Represents tool/view, click handling, icon
- **OutlookBarButton**: Custom-drawn navigation button

### Supporting (~1K lines)
- **NavPaneOptionsDlg**: Customization dialog (show/hide tabs, reorder)
- **ItemClickedEventArgs**: Item click event data
- **PanelPosition**: Enum (top, bottom)

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **UI framework**: System.Windows.Forms (UserControl, custom GDI+ painting)
- **Key features**: Custom button rendering, drag-drop, context menus
- **Display modes**: Button, list, strip-list layouts

## Dependencies
- **System.Windows.Forms**: UserControl, custom painting
- **Consumer**: xWorks (FwXWindow), LexText (area navigation)

## Interop & Contracts
- **SidePane control**: Embeddable UserControl for navigation
- **Tab/Item hierarchy**: Tab contains Items (tools/views)
- **Events**: ItemClicked event for navigation handling
- **Drag-drop**: Tab reordering via mouse drag
- **NavPaneOptionsDlg**: Customization dialog (show/hide tabs, reorder)

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build SilSidePane.csproj`
- Reusable navigation control

## Interfaces and Data Models

- **Item** (class)
  - Path: `Item.cs`
  - Public class implementation

- **SidePane** (class)
  - Path: `SidePane.cs`
  - Public class implementation

- **Tab** (class)
  - Path: `Tab.cs`
  - Public class implementation

- **SidePaneItemAreaStyle** (enum)
  - Path: `SidePaneItemAreaStyle.cs`

## Entry Points
- Side pane control for application navigation
- Configuration dialog for pane options
- Banner and item area components

## Test Index
Test projects: SilSidePaneTests. 6 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **XCore/** - Framework hosting side pane
- **Common/Controls/** - Base control infrastructure
- **xWorks/** - Uses side pane for navigation
- **LexText/** - Uses side pane for area selection

## References

- **Project files**: SilSidePane.csproj, SilSidePaneTests.csproj
- **Target frameworks**: net48
- **Key C# files**: Banner.cs, IItemArea.cs, Item.cs, NavPaneOptionsDlg.Designer.cs, OutlookBarButtonCollection.cs, SidePane.cs, SidePaneItemAreaStyle.cs, SilSidePane.Designer.cs, StripListItemArea.cs, Tab.cs
- **Source file count**: 26 files
- **Data file count**: 3 files

## Code Evidence
*Analysis based on scanning 21 source files*

- **Classes found**: 12 public classes
- **Namespaces**: SIL.SilSidePane
