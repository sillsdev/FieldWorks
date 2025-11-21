---
last-reviewed: 2025-10-31
last-reviewed-tree: 4807ad69f2046ab660d562c93d6ce51aa6e901f1f80f02835c461cea12d547c0
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Common COPILOT summary

## Purpose
Organizational parent folder containing cross-cutting utilities and shared infrastructure used throughout FieldWorks. Groups together fundamental components including UI controls (Controls/), application services (FieldWorks/), data filtering (Filters/), framework components (Framework/), utility functions (FwUtils/), view site management (RootSite/, SimpleRootSite/), scripture-specific utilities (ScriptureUtils/), UI adapter abstractions (UIAdapterInterfaces/), and view interfaces (ViewsInterfaces/). This folder serves as a container for the most comprehensive collection of shared code, providing building blocks for all FieldWorks applications.

## Architecture
Organizational folder with 10 immediate subfolders. No source files directly in this folder - all code resides in subfolders. Each subfolder has its own COPILOT.md documenting its specific purpose, components, and dependencies.

## Key Components
This folder does not contain source files directly. See subfolder COPILOT.md files for specific components:
- **Controls/**: Shared UI controls library with reusable widgets and XML-based views
- **FieldWorks/**: Core FieldWorks-specific application infrastructure and utilities
- **Filters/**: Data filtering and sorting infrastructure for searchable data views
- **Framework/**: Application framework components providing core infrastructure services
- **FwUtils/**: General FieldWorks utilities library containing wide-ranging helper functions
- **RootSite/**: Root-level site management infrastructure for hosting FieldWorks views
- **ScriptureUtils/**: Scripture-specific utilities and Paratext integration support
- **SimpleRootSite/**: Simplified root site implementation with streamlined API
- **UIAdapterInterfaces/**: UI adapter pattern interfaces for abstraction and testability
- **ViewsInterfaces/**: Managed interface definitions for the native Views rendering engine

## Technology Stack
Mixed C# and native code across subfolders. See individual subfolder COPILOT.md files for specific technologies used in each component.

## Dependencies

### Upstream (consumes)
Dependencies vary by subfolder. Common upstream dependencies include:
- **Kernel**: Low-level infrastructure (referenced by multiple subfolders)
- **Generic**: Generic utilities (referenced by multiple subfolders)
- **views**: Native view layer (interfaced by RootSite, SimpleRootSite, ViewsInterfaces)

### Downstream (consumed by)
Almost all FieldWorks applications and libraries depend on components in Common subfolders:
- **xWorks/**: Major consumer of Common UI controls and utilities
- **LexText/**: Uses Common controls for lexicon UI
- **FwCoreDlgs/**: Dialog components built on Common infrastructure
- **XCore/**: Framework components that work with Common utilities

## Interop & Contracts
Interop boundaries vary by subfolder. Multiple subfolders implement COM-compatible interfaces, use P/Invoke for native code access, and use marshaling for cross-boundary calls. See individual subfolder COPILOT.md files for specific interop details.

## Threading & Performance
Threading models vary by subfolder. Many UI components require UI thread marshaling. See individual subfolder COPILOT.md files for specific threading considerations.

## Config & Feature Flags
Configuration varies by subfolder. See individual subfolder COPILOT.md files for specific configuration mechanisms.

## Build Information
- No project file in this folder; each subfolder has its own .csproj or .vcxproj
- Build via top-level FieldWorks.sln (Visual Studio/MSBuild)
- All subfolder projects are built as part of the main solution
- Each subfolder may have accompanying test projects (e.g., FwUtilsTests/, FrameworkTests/)

## Interfaces and Data Models
See individual subfolder COPILOT.md files for interfaces and data models. This organizational folder does not define interfaces directly.

## Entry Points
See individual subfolder COPILOT.md files for entry points. Common subfolders provide libraries and interfaces rather than executable entry points.

## Test Index
Multiple test projects across subfolders:
- **Controls/**: DetailControlsTests, FwControlsTests, WidgetsTests, XMLViewsTests
- **FieldWorks/**: FieldWorksTests
- **Filters/**: FiltersTests
- **Framework/**: FrameworkTests
- **FwUtils/**: FwUtilsTests
- **RootSite/**: RootSiteTests
- **ScriptureUtils/**: ScriptureUtilsTests
- **SimpleRootSite/**: SimpleRootSiteTests
- **ViewsInterfaces/**: ViewsInterfacesTests

Run tests via: `dotnet test` or Visual Studio Test Explorer

## Usage Hints
This is an organizational folder. For usage guidance, see individual subfolder COPILOT.md files:
- Controls/COPILOT.md for UI control usage
- FieldWorks/COPILOT.md for application infrastructure
- Filters/COPILOT.md for data filtering
- Framework/COPILOT.md for framework services
- FwUtils/COPILOT.md for utility functions
- RootSite/COPILOT.md for advanced view hosting
- ScriptureUtils/COPILOT.md for scripture utilities
- SimpleRootSite/COPILOT.md for simplified view hosting
- UIAdapterInterfaces/COPILOT.md for UI abstraction patterns
- ViewsInterfaces/COPILOT.md for view rendering interfaces

## Related Folders
- **Kernel/**: Provides low-level infrastructure used by Common subfolders
- **Generic/**: Provides generic utilities used by Common subfolders
- **views/**: Native view layer that Common components interface with (RootSite, SimpleRootSite, ViewsInterfaces)
- **XCore/**: Framework components that work with Common utilities
- **xWorks/**: Major consumer of Common UI controls and utilities
- **LexText/**: Uses Common controls for lexicon UI
- **FwCoreDlgs/**: Dialog components built on Common infrastructure

## References
- **Project files**: No project file in this organizational folder; see subfolder COPILOT.md files
- **Subfolders**: Controls/, FieldWorks/, Filters/, Framework/, FwUtils/, RootSite/, ScriptureUtils/, SimpleRootSite/, UIAdapterInterfaces/, ViewsInterfaces/
- **Documentation**: Each subfolder has its own COPILOT.md file with detailed documentation
