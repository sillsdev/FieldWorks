---
last-reviewed: 2025-11-01
last-reviewed-tree: 5c0428af86d4d8c6b7829d245dd8bd3a610718aca9563315255e6c5b43a1e58e
status: production
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - core-classes-2k-lines
  - supporting-1k-lines
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references
  - code-evidence

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
Language - C#

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- SidePane control: Embeddable UserControl for navigation

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project

## Interfaces and Data Models
Item, SidePane, Tab, SidePaneItemAreaStyle.

## Entry Points
- Side pane control for application navigation

## Test Index
Test projects: SilSidePaneTests. 6 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- XCore/ - Framework hosting side pane

## References
See `.cache/copilot/diff-plan.json` for file details.

## Code Evidence
*Analysis based on scanning 21 source files*
