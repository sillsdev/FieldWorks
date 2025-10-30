---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# XCore/SilSidePane

## Purpose
Side pane UI component for navigation. Provides the navigation pane (sidebar) control used in FieldWorks applications for area and view selection.

## Key Components
- **SilSidePane.csproj** - Side pane control library
- **Banner.cs** - Banner/header component
- **Item.cs** - Navigation item representation
- **IItemArea.cs** - Item area interface
- **ListViewItemArea.cs** - List view area implementation
- **NavPaneOptionsDlg** - Navigation pane configuration dialog


## Key Classes/Interfaces
- **Item**
- **SidePaneItemAreaStyle**
- **Tab**
- **SidePane**

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


## References
- **Project Files**: SilSidePane.csproj
- **Key C# Files**: Banner.cs, IItemArea.cs, Item.cs, ListViewItemArea.cs, NavPaneOptionsDlg.cs, OutlookBar.cs, OutlookBarButton.cs, OutlookBarButtonCollection.cs
