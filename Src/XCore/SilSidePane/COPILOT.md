---
last-reviewed: 2025-11-01
last-reviewed-tree: a872637d37e3a95e66b9a0bc325c7a1b32b47fbd3c36dd4fdab463b96aca720c
status: production
---

# SilSidePane

## Purpose
Side pane navigation control for FieldWorks multi-area interface. Provides SidePane, Tab, and Item classes implementing hierarchical navigation sidebar (similar to Outlook bar). Enables area/tool switching in FLEx and other FieldWorks apps. Includes OutlookBarButton rendering, drag-and-drop tab reordering, and NavPaneOptionsDlg customization.

## Architecture
TBD - populate from code. See auto-generated hints below.

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
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **System.Windows.Forms**: UserControl, custom painting
- **Consumer**: xWorks (FwXWindow), LexText (area navigation)

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

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
- **Target frameworks**: net462
- **Key C# files**: Banner.cs, IItemArea.cs, Item.cs, NavPaneOptionsDlg.Designer.cs, OutlookBarButtonCollection.cs, SidePane.cs, SidePaneItemAreaStyle.cs, SilSidePane.Designer.cs, StripListItemArea.cs, Tab.cs
- **Source file count**: 26 files
- **Data file count**: 3 files

## References (auto-generated hints)
- Project files:
  - XCore/SilSidePane/SilSidePane.csproj
  - XCore/SilSidePane/SilSidePaneTests/BuildInclude.targets
  - XCore/SilSidePane/SilSidePaneTests/SilSidePaneTests.csproj
- Key C# files:
  - XCore/SilSidePane/Banner.cs
  - XCore/SilSidePane/IItemArea.cs
  - XCore/SilSidePane/Item.cs
  - XCore/SilSidePane/ListViewItemArea.cs
  - XCore/SilSidePane/NavPaneOptionsDlg.Designer.cs
  - XCore/SilSidePane/NavPaneOptionsDlg.cs
  - XCore/SilSidePane/OutlookBar.cs
  - XCore/SilSidePane/OutlookBarButton.cs
  - XCore/SilSidePane/OutlookBarButtonCollection.cs
  - XCore/SilSidePane/OutlookBarSubButtonPanel.cs
  - XCore/SilSidePane/OutlookButtonPanel.cs
  - XCore/SilSidePane/OutlookButtonPanelItemArea.cs
  - XCore/SilSidePane/Properties/AssemblyInfo.cs
  - XCore/SilSidePane/Properties/Resources.Designer.cs
  - XCore/SilSidePane/Properties/Settings.Designer.cs
  - XCore/SilSidePane/SidePane.cs
  - XCore/SilSidePane/SidePaneItemAreaStyle.cs
  - XCore/SilSidePane/SilSidePane.Designer.cs
  - XCore/SilSidePane/SilSidePaneTests/ItemTests.cs
  - XCore/SilSidePane/SilSidePaneTests/NavPaneOptionsDlgTests.cs
  - XCore/SilSidePane/SilSidePaneTests/OutlookBarButtonTests.cs
  - XCore/SilSidePane/SilSidePaneTests/SidePaneTests.cs
  - XCore/SilSidePane/SilSidePaneTests/TabTests.cs
  - XCore/SilSidePane/SilSidePaneTests/TestUtilities.cs
  - XCore/SilSidePane/StripListItemArea.cs
- Data contracts/transforms:
  - XCore/SilSidePane/NavPaneOptionsDlg.resx
  - XCore/SilSidePane/Properties/Resources.resx
  - XCore/SilSidePane/SilSidePane.resx
## Code Evidence
*Analysis based on scanning 21 source files*

- **Classes found**: 12 public classes
- **Namespaces**: SIL.SilSidePane
