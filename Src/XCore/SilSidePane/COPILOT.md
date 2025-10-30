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

## Build Information
- C# class library project
- Build via: `dotnet build SilSidePane.csproj`
- Reusable navigation control

## Entry Points
- Side pane control for application navigation
- Configuration dialog for pane options
- Banner and item area components

## Related Folders
- **XCore/** - Framework hosting side pane
- **Common/Controls/** - Base control infrastructure
- **xWorks/** - Uses side pane for navigation
- **LexText/** - Uses side pane for area selection

## Code Evidence
*Analysis based on scanning 21 source files*

- **Classes found**: 12 public classes
- **Namespaces**: SIL.SilSidePane

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

## References

- **Project files**: SilSidePane.csproj, SilSidePaneTests.csproj
- **Target frameworks**: net462
- **Key C# files**: Banner.cs, IItemArea.cs, Item.cs, NavPaneOptionsDlg.Designer.cs, OutlookBarButtonCollection.cs, SidePane.cs, SidePaneItemAreaStyle.cs, SilSidePane.Designer.cs, StripListItemArea.cs, Tab.cs
- **Source file count**: 26 files
- **Data file count**: 3 files
