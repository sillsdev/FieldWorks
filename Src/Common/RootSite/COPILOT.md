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

## Interfaces and Data Models

- **IApp** (interface)
  - Path: `IApp.cs`
  - Public interface definition

- **ICollectPicturePathsOnly** (interface)
  - Path: `CollectorEnv.cs`
  - Public interface definition

- **IHeightEstimator** (interface)
  - Path: `IHeightEstimator.cs`
  - Public interface definition

- **IRootSiteGroup** (interface)
  - Path: `IRootSiteGroup.cs`
  - Public interface definition

- **IRootSiteSlave** (interface)
  - Path: `IRootSiteSlave.cs`
  - Public interface definition

- **IVwGraphicsNet** (interface)
  - Path: `IVwGraphicsNet.cs`
  - Public interface definition

- **AddToDictMenuItem** (class)
  - Path: `SpellCheckHelper.cs`
  - Public class implementation

- **CollectorEnv** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **CollectorEnvServices** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **LocationInfo** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **MaxStringWidthForColumnEnv** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **PointsOfInterestCollectorEnv** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **PrevPropCounter** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **RequestSelectionByHelper** (class)
  - Path: `RequestSelectionHelper.cs`
  - Public class implementation

- **RequestSelectionHelper** (class)
  - Path: `RequestSelectionHelper.cs`
  - Public class implementation

- **RootSite** (class)
  - Path: `RootSite.cs`
  - Public class implementation

- **RootSiteGroup** (class)
  - Path: `RootSite.cs`
  - Public class implementation

- **SpellCheckHelper** (class)
  - Path: `SpellCheckHelper.cs`
  - Public class implementation

- **SpellCorrectMenuItem** (class)
  - Path: `SpellCheckHelper.cs`
  - Public class implementation

- **StackItem** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **StringCollectorEnv** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **StringMeasureEnv** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **TestCollectorEnv** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **TsStringCollectorEnv** (class)
  - Path: `CollectorEnv.cs`
  - Public class implementation

- **UndoSelectionAction** (class)
  - Path: `UndoActions.cs`
  - Public class implementation

- **UndoTaskHelper** (class)
  - Path: `UndoTaskHelper.cs`
  - Public class implementation

- **ContentTypes** (enum)
  - Path: `StVc.cs`

- **DisplayType** (enum)
  - Path: `RootSiteTests/DummyBasicViewVc.cs`

- **Lng** (enum)
  - Path: `RootSiteTests/RootsiteBasicViewTestsBase.cs`

- **SpellCheckStatus** (enum)
  - Path: `RootSiteEditingHelper.cs`

- **StTextFrags** (enum)
  - Path: `StVc.cs`

## References

- **Project files**: RootSite.csproj, RootSiteTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, CollectorEnv.cs, IRootSiteSlave.cs, IVwGraphicsNet.cs, RequestSelectionHelper.cs, RootSite.cs, RootSiteControl.cs, SpellCheckHelper.cs, UndoActions.cs, UndoTaskHelper.cs
- **XML data/config**: RootSiteDataProviderCacheModel.xml
- **Source file count**: 31 files
- **Data file count**: 7 files
