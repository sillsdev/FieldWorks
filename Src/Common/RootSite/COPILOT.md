---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# RootSite

## Purpose
Root-level site management for views. Provides the base infrastructure for hosting and managing FieldWorks' view system, including view composition and rendering coordination.

## Key Components
### Key Classes
- **CollectorEnv**
- **PrevPropCounter**
- **StackItem**
- **LocationInfo**
- **PointsOfInterestCollectorEnv**
- **StringCollectorEnv**
- **StringMeasureEnv**
- **MaxStringWidthForColumnEnv**
- **TestCollectorEnv**
- **TsStringCollectorEnv**

### Key Interfaces
- **ICollectPicturePathsOnly**
- **IVwGraphicsNet**
- **IRootSiteSlave**
- **IRootSiteGroup**
- **IHeightEstimator**
- **IApp**

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

## Code Evidence
*Analysis based on scanning 28 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 6 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.RootSites
