---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# SilSidePane

## Purpose
Side pane UI component for navigation. Provides the navigation pane (sidebar) control used in FieldWorks applications for area and view selection.

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
