---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Common/RootSite

## Purpose
Root-level site management for views. Provides the base infrastructure for hosting and managing FieldWorks' view system, including view composition and rendering coordination.

## Key Components
- **RootSite.csproj** - Root site library
- **FwBaseVc.cs** - Base view constructor class
- **CollectorEnv.cs** - Environment for collecting view data
- **IApp.cs** - Application interface for root sites
- **IRootSiteGroup.cs** - Root site grouping interface
- **IRootSiteSlave.cs** - Slave site management
- **IVwGraphicsNet.cs** - Graphics interface
- **PictureWrapper.cs** - Picture handling

## Technology Stack
- C# .NET
- View infrastructure
- Graphics and rendering coordination

## Dependencies
- Depends on: views (native view layer), Common/ViewsInterfaces
- Used by: Common/SimpleRootSite, applications with complex views

## Build Information
- C# class library project
- Build via: `dotnet build RootSite.csproj`
- Bridge between managed and native views

## Entry Points
- Base classes for view hosting
- Interfaces for view management
- Graphics coordination

## Related Folders
- **Common/SimpleRootSite/** - Simplified root site built on RootSite
- **Common/ViewsInterfaces/** - Interfaces used by RootSite
- **views/** - Native view layer that RootSite manages
- **ManagedVwWindow/** - Window management using root sites
- **xWorks/** - Uses root sites for data display
- **LexText/** - Uses root sites for text views


## References
- **Project Files**: RootSite.csproj
- **Key C# Files**: CollectorEnv.cs, FwBaseVc.cs, IApp.cs, IRootSiteGroup.cs, IRootSiteSlave.cs, IVwGraphicsNet.cs, PictureWrapper.cs, RequestSelectionHelper.cs
