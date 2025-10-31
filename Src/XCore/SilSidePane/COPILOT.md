---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# SilSidePane

## Purpose
Side pane navigation control for FieldWorks applications.
Implements the navigation sidebar (SidePane, Tab, Item classes) that provides hierarchical
navigation between different areas and tools in FieldWorks applications. Enables the
multi-area interface pattern used throughout FLEx and other FieldWorks apps.

## Architecture
C# library with 26 source files. Contains 1 subprojects: SilSidePane.

## Key Components
### Key Classes
- **Item**
- **Tab**
- **SidePane**
- **OutlookBarButtonTests**
- **TabTests**
- **NavPaneOptionsDlgTests**
- **ItemTests**
- **SidePaneTests_Buttons**
- **SidePaneTests_List**
- **SidePaneTests_StripList**

## Technology Stack
- C# .NET WinForms
- Custom control development
- Navigation UI patterns

## Dependencies
- Depends on: Common (UI infrastructure), XCore/xCoreInterfaces
- Used by: xWorks, LexText (for navigation sidebar)

## Interop & Contracts
Uses COM for cross-boundary calls.

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
