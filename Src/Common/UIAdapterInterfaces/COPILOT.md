---
last-reviewed: 2025-10-31
last-reviewed-tree: a6f7b672b53b6c5e4e93be09912da26037f2e287b213af0e06f6da32d6155e27
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# UIAdapterInterfaces COPILOT summary

## Purpose
UI adapter pattern interfaces for abstraction and testability in FieldWorks applications. Defines contracts ISIBInterface (Side Bar and Information Bar Interface) and ITMInterface (Tool Manager Interface) that allow UI components to be adapted to different implementations or replaced with test doubles for unit testing. Helper classes (SBTabProperties, SBTabItemProperties, ITMAdapter) support adapter implementations. Enables dependency injection, testing of UI-dependent code without actual UI, and flexibility in UI component selection.

## Architecture
C# interface library (.NET Framework 4.8.x) defining UI adapter contracts. Pure interface definitions with supporting helper classes for property transfer. No implementations in this project - implementations reside in consuming projects (e.g., XCore provides concrete adapters).

## Key Components
- **ISIBInterface** (SIBInterface.cs): Side Bar and Information Bar contract
  - Initialize(): Set up sidebar and info bar with containers and mediator
  - AddTab(): Add category tab to sidebar (SBTabProperties)
  - AddTabItem(): Add item to category tab (SBTabItemProperties)
  - SetCurrentTab(): Switch active tab
  - SetCurrentTabItem(): Switch active tab item
  - SetupSideBarMenus(): Configure sidebar menus
  - RefreshTab(): Refresh tab items
  - CurrentTab, CurrentTabItem: Properties for current selections
  - TabCount: Number of tabs
- **ITMInterface** (TMInterface.cs): Tool Manager contract
  - Initialize(): Set up tool manager with container and mediator
  - AddTool(): Add tool to manager
  - SetCurrentTool(): Switch active tool
  - CurrentTool: Property for current tool
  - Tools: Collection of available tools
- **SBTabProperties** (HelperClasses.cs): Sidebar tab properties
  - Name, Text, ImageList, DefaultIconIndex
  - Properties for tab appearance and behavior
- **SBTabItemProperties** (HelperClasses.cs): Sidebar tab item properties
  - Name, Text, Tag, IconIndex, Message
  - Properties for tab item appearance and behavior
- **ITMAdapter** (HelperClasses.cs): Tool manager adapter interface
  - GetToolAdapter(): Retrieve adapter for tool
  - Tool management abstraction
- **HelperClasses** (HelperClasses.cs): Supporting classes
  - Property classes for UI element configuration
- **UIAdapterInterfacesStrings** (UIAdapterInterfacesStrings.Designer.cs): Localized strings

## Technology Stack
- C# .NET Framework 4.8.x (net8)

## Dependencies
- Upstream: Mediator for command routing
- Downstream: Provides concrete adapter implementations (SIBAdapter, etc.)

## Interop & Contracts
- **ISIBInterface**: Contract for side bar and information bar adapters

## Threading & Performance
Interface definitions have no threading implications. Implementations must handle threading appropriately.

## Config & Feature Flags
No configuration in interface library. Behavior determined by implementations.

## Build Information
- **Project file**: UIAdapterInterfaces.csproj (net48, OutputType=Library)

## Interfaces and Data Models
ISIBInterface, ITMInterface, SBTabProperties, SBTabItemProperties, ITMAdapter.

## Entry Points
Referenced by UI components and XCore for adapter pattern implementation. No executable entry point.

## Test Index
No test project for interface library. Implementations tested in consuming projects using these interfaces.

## Usage Hints
- Define ISIBInterface and ITMInterface in business logic for dependency injection

## Related Folders
- **XCore/**: Provides concrete adapter implementations

## References
See `.cache/copilot/diff-plan.json` for file details.
