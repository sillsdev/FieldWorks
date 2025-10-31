---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# RootSite COPILOT summary

## Purpose
Root-level site management infrastructure for hosting FieldWorks views with advanced features. Implements RootSite classes providing top-level container for Views rendering system, view lifecycle management, printing coordination, selection/editing management, and bridge between Windows Forms and native Views architecture. More sophisticated than SimpleRootSite with collector environments for view analysis, testing, and string extraction. Critical foundation for all text display and editing functionality in FieldWorks.

## Architecture
C# class library (.NET Framework 4.6.2) with root site infrastructure. CollectorEnv base class and subclasses implement IVwEnv interface for view collection without actual rendering (testing, string extraction, measurement). Provides abstract RootSite base classes extended by SimpleRootSite. Test project (RootSiteTests) validates root site behavior.

## Key Components
- **CollectorEnv** class (CollectorEnv.cs): Base for IVwEnv implementations
  - Implements IVwEnv for non-rendering view collection
  - Used for testing blank displays, extracting strings, measuring
  - PrevPropCounter: Tracks property occurrence counts
  - StackItem: Stack management for nested displays
  - LocationInfo: Position tracking
- **StringCollectorEnv**: Collects strings from view
- **TsStringCollectorEnv**: Collects formatted ITsString objects
- **PointsOfInterestCollectorEnv**: Collects specific points of interest
- **StringMeasureEnv**: Measures string dimensions
- **MaxStringWidthForColumnEnv**: Calculates column widths
- **TestCollectorEnv**: Tests for blank displays
- **FwBaseVc**: Base view constructor class
- **ICollectPicturePathsOnly**: Interface for picture path collection
- **IVwGraphicsNet**: .NET graphics interface
- **IRootSiteSlave, IRootSiteGroup**: Root site coordination
- **IHeightEstimator**: Height estimation interface
- **IApp**: Application interface

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Views rendering engine integration (COM interop)
- Windows Forms integration

## Dependencies

### Upstream (consumes)
- **views**: Native C++ rendering engine
- **Common/ViewsInterfaces**: View interfaces (IVwEnv, IVwGraphics)
- **SIL.LCModel**: Data model
- **SIL.LCModel.Application**: Application services
- **Windows Forms**: UI framework

### Downstream (consumed by)
- **Common/SimpleRootSite**: Extends RootSite classes
- **xWorks**: Complex view hosting
- **LexText**: Lexicon views
- All components requiring advanced view management

## Interop & Contracts
- **IVwEnv**: Environment interface for view construction
- **COM interop**: Bridges to native Views engine
- **Marshaling**: Cross-boundary calls to native code

## Threading & Performance
- UI thread requirements for view operations
- Performance considerations for view measurement and collection
- CollectorEnv avoids rendering overhead for testing/analysis

## Config & Feature Flags
No explicit configuration. Behavior determined by view specifications and data.

## Build Information
- **Project file**: RootSite.csproj (net462, OutputType=Library)
- **Test project**: RootSiteTests/RootSiteTests.csproj
- **Output**: RootSite.dll
- **Build**: Via top-level FW.sln or: `msbuild RootSite.csproj /p:Configuration=Debug`
- **Run tests**: `dotnet test RootSiteTests/RootSiteTests.csproj`

## Interfaces and Data Models

- **CollectorEnv** (CollectorEnv.cs)
  - Purpose: Base class for IVwEnv implementations that collect view information without rendering
  - Inputs: View specifications, data objects
  - Outputs: Collected strings, measurements, test results
  - Notes: Subclasses override methods for specific collection purposes

- **IVwEnv** (implemented by CollectorEnv)
  - Purpose: Environment interface for view construction
  - Inputs: Display specifications, property tags, objects
  - Outputs: View structure information
  - Notes: Core interface for view construction; CollectorEnv provides non-rendering implementation

- **StringCollectorEnv** (CollectorEnv.cs)
  - Purpose: Collects plain strings from view construction
  - Inputs: View specifications
  - Outputs: Concatenated string representation of view
  - Notes: Useful for testing, exporting, accessibility

- **TsStringCollectorEnv** (CollectorEnv.cs)
  - Purpose: Collects formatted ITsString objects preserving formatting
  - Inputs: View specifications
  - Outputs: Formatted text with properties
  - Notes: Maintains text formatting information

- **TestCollectorEnv** (CollectorEnv.cs)
  - Purpose: Tests whether view construction produces blank/empty output
  - Inputs: View specifications
  - Outputs: Boolean indicating blank status
  - Notes: Optimization for conditionally displaying views

## Entry Points
Referenced as library for advanced root site functionality. Extended by SimpleRootSite and used by applications requiring sophisticated view management.

## Test Index
- **Test project**: RootSiteTests
- **Run tests**: `dotnet test RootSiteTests/RootSiteTests.csproj`
- **Coverage**: Root site behavior, CollectorEnv subclasses

## Usage Hints
- Extend RootSite classes for custom view hosting
- Use CollectorEnv subclasses for view analysis without rendering
- StringCollectorEnv for extracting text from views
- TestCollectorEnv to check if view would be blank
- More advanced than SimpleRootSite; use SimpleRootSite for standard scenarios

## Related Folders
- **Common/SimpleRootSite**: Simplified root site extending this infrastructure
- **Common/ViewsInterfaces**: Interfaces implemented by RootSite
- **views/**: Native rendering engine
- **xWorks, LexText**: Applications using root site infrastructure

## References
- **Project files**: RootSite.csproj (net462), RootSiteTests/RootSiteTests.csproj
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: CollectorEnv.cs, FwBaseVc.cs, and others
- **Total lines of code**: 9274
- **Output**: Output/Debug/RootSite.dll
- **Namespace**: SIL.FieldWorks.Common.RootSites
