---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# RootSite

## Purpose
Root-level site management infrastructure for hosting FieldWorks views.
Implements RootSite classes that provide the top-level container for the Views rendering system,
handle view lifecycle management, coordinate printing, manage selection and editing, and bridge
between Windows Forms and the native Views architecture. Critical foundation for all text
display and editing functionality.

## Architecture
C# library with 31 source files. Contains 1 subprojects: RootSite.

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

## Interop & Contracts
Uses Marshaling, COM for cross-boundary calls.

## Threading & Performance
Threading model: explicit threading, UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build RootSite.csproj`
- Bridge between managed and native views

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

## Entry Points
- Base classes for view hosting
- Interfaces for view management
- Graphics coordination

## Test Index
Test projects: RootSiteTests. 11 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Common/SimpleRootSite/** - Simplified root site built on RootSite
- **Common/ViewsInterfaces/** - Interfaces used by RootSite
- **views/** - Native view layer that RootSite manages
- **ManagedVwWindow/** - Window management using root sites
- **xWorks/** - Uses root sites for data display
- **LexText/** - Uses root sites for text views

## References

- **Project files**: RootSite.csproj, RootSiteTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, CollectorEnv.cs, IRootSiteSlave.cs, IVwGraphicsNet.cs, RequestSelectionHelper.cs, RootSite.cs, RootSiteControl.cs, SpellCheckHelper.cs, UndoActions.cs, UndoTaskHelper.cs
- **XML data/config**: RootSiteDataProviderCacheModel.xml
- **Source file count**: 31 files
- **Data file count**: 7 files

## References (auto-generated hints)
- Project files:
  - Common/RootSite/RootSite.csproj
  - Common/RootSite/RootSiteTests/RootSiteTests.csproj
- Key C# files:
  - Common/RootSite/AssemblyInfo.cs
  - Common/RootSite/CollectorEnv.cs
  - Common/RootSite/FwBaseVc.cs
  - Common/RootSite/IApp.cs
  - Common/RootSite/IHeightEstimator.cs
  - Common/RootSite/IRootSiteGroup.cs
  - Common/RootSite/IRootSiteSlave.cs
  - Common/RootSite/IVwGraphicsNet.cs
  - Common/RootSite/PictureWrapper.cs
  - Common/RootSite/Properties/Resources.Designer.cs
  - Common/RootSite/RequestSelectionHelper.cs
  - Common/RootSite/RootSite.cs
  - Common/RootSite/RootSiteControl.cs
  - Common/RootSite/RootSiteEditingHelper.cs
  - Common/RootSite/RootSiteStrings.Designer.cs
  - Common/RootSite/RootSiteTests/BasicViewTestsBase.cs
  - Common/RootSite/RootSiteTests/CollectorEnvTests.cs
  - Common/RootSite/RootSiteTests/DummyBasicView.cs
  - Common/RootSite/RootSiteTests/DummyBasicViewVc.cs
  - Common/RootSite/RootSiteTests/MoreRootSiteTests.cs
  - Common/RootSite/RootSiteTests/PrintRootSiteTests.cs
  - Common/RootSite/RootSiteTests/Properties/Resources.Designer.cs
  - Common/RootSite/RootSiteTests/RootSiteEditingHelperTests.cs
  - Common/RootSite/RootSiteTests/RootSiteGroupTests.cs
  - Common/RootSite/RootSiteTests/RootsiteBasicViewTestsBase.cs
- Data contracts/transforms:
  - Common/RootSite/Properties/Resources.resx
  - Common/RootSite/RootSite.resx
  - Common/RootSite/RootSiteControl.resx
  - Common/RootSite/RootSiteStrings.resx
  - Common/RootSite/RootSiteTests/DummyBasicView.resx
  - Common/RootSite/RootSiteTests/Properties/Resources.resx
  - Common/RootSite/RootSiteTests/RootSiteDataProviderCacheModel.xml
## Code Evidence
*Analysis based on scanning 28 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 6 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.RootSites
