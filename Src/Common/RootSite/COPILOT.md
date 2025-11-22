---
last-reviewed: 2025-10-31
last-reviewed-tree: 3505d6ce9dc81bc145584c626f003ba9184ecfa0a9d451b2d288a4edf8c64500
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# RootSite COPILOT summary

## Purpose
Root-level site management infrastructure for hosting FieldWorks views with advanced features. Implements RootSite classes providing top-level container for Views rendering system, view lifecycle management, printing coordination, selection/editing management, and bridge between Windows Forms and native Views architecture. More sophisticated than SimpleRootSite with collector environments for view analysis, testing, and string extraction. Critical foundation for all text display and editing functionality in FieldWorks.

## Architecture
C# class library (.NET Framework 4.8.x) with root site infrastructure. CollectorEnv base class and subclasses implement IVwEnv interface for view collection without actual rendering (testing, string extraction, measurement). Provides abstract RootSite base classes extended by SimpleRootSite. Test project (RootSiteTests) validates root site behavior.

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
- C# .NET Framework 4.8.x (net8)

## Dependencies
- Upstream: Native C++ rendering engine
- Downstream: Extends RootSite classes

## Interop & Contracts
- **IVwEnv**: Environment interface for view construction

## Threading & Performance
- UI thread requirements for view operations

## Config & Feature Flags
No explicit configuration. Behavior determined by view specifications and data.

## Build Information
- **Project file**: RootSite.csproj (net48, OutputType=Library)

## Interfaces and Data Models
CollectorEnv, IVwEnv, StringCollectorEnv, TsStringCollectorEnv, TestCollectorEnv.

## Entry Points
Referenced as library for advanced root site functionality. Extended by SimpleRootSite and used by applications requiring sophisticated view managemen

## Test Index
- **Test project**: RootSiteTests

## Usage Hints
- Extend RootSite classes for custom view hosting

## Related Folders
- **Common/SimpleRootSite**: Simplified root site extending this infrastructure

## References
See `.cache/copilot/diff-plan.json` for file details.
