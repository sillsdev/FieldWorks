---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# UIAdapterInterfaces COPILOT summary

## Purpose
UI adapter pattern interfaces for abstraction and testability in FieldWorks applications. Defines contracts ISIBInterface (Side Bar and Information Bar Interface) and ITMInterface (Tool Manager Interface) that allow UI components to be adapted to different implementations or replaced with test doubles for unit testing. Helper classes (SBTabProperties, SBTabItemProperties, ITMAdapter) support adapter implementations. Enables dependency injection, testing of UI-dependent code without actual UI, and flexibility in UI component selection.

## Architecture
C# interface library (.NET Framework 4.6.2) defining UI adapter contracts. Pure interface definitions with supporting helper classes for property transfer. No implementations in this project - implementations reside in consuming projects (e.g., XCore provides concrete adapters).

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
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Interface definitions only (no UI framework dependency)
- XCore integration (Mediator references)

## Dependencies

### Upstream (consumes)
- **XCore**: Mediator for command routing
- **System.Windows.Forms**: Control references (containers)
- Minimal dependencies (interface library)

### Downstream (consumed by)
- **XCore**: Provides concrete adapter implementations (SIBAdapter, etc.)
- **UI components**: Implement these interfaces for testability
- **Test projects**: Use test doubles implementing these interfaces
- Any component requiring adaptable UI patterns

## Interop & Contracts
- **ISIBInterface**: Contract for side bar and information bar adapters
- **ITMInterface**: Contract for tool manager adapters
- Enables test doubles and dependency injection
- Decouples UI component selection from business logic

## Threading & Performance
Interface definitions have no threading implications. Implementations must handle threading appropriately.

## Config & Feature Flags
No configuration in interface library. Behavior determined by implementations.

## Build Information
- **Project file**: UIAdapterInterfaces.csproj (net462, OutputType=Library)
- **Output**: UIAdapterInterfaces.dll
- **Build**: Via top-level FW.sln or: `msbuild UIAdapterInterfaces.csproj /p:Configuration=Debug`
- **No test project**: Interface library; implementations tested in consuming projects

## Interfaces and Data Models

- **ISIBInterface** (SIBInterface.cs)
  - Purpose: Contract for side bar and information bar UI components
  - Inputs: Container controls, mediator, tab/item properties
  - Outputs: Tab management, item selection, menu setup
  - Notes: Enables different side bar implementations (e.g., native, cross-platform, test doubles)

- **ITMInterface** (TMInterface.cs)
  - Purpose: Contract for tool manager UI components
  - Inputs: Container controls, mediator, tool specifications
  - Outputs: Tool management, tool selection
  - Notes: Abstracts tool window management for testing and flexibility

- **SBTabProperties** (HelperClasses.cs)
  - Purpose: Data class for sidebar tab configuration
  - Inputs: Name, Text, ImageList, DefaultIconIndex
  - Outputs: Property values for tab creation
  - Notes: DTO for tab properties

- **SBTabItemProperties** (HelperClasses.cs)
  - Purpose: Data class for sidebar tab item configuration
  - Inputs: Name, Text, Tag, IconIndex, Message
  - Outputs: Property values for tab item creation
  - Notes: DTO for tab item properties

- **ITMAdapter** (HelperClasses.cs)
  - Purpose: Contract for tool manager adapter retrieval
  - Inputs: Tool identifier
  - Outputs: Adapter instance for tool
  - Notes: Supports tool-specific adapters

## Entry Points
Referenced by UI components and XCore for adapter pattern implementation. No executable entry point.

## Test Index
No test project for interface library. Implementations tested in consuming projects using these interfaces.

## Usage Hints
- Define ISIBInterface and ITMInterface in business logic for dependency injection
- XCore provides concrete implementations (SIBAdapter, TMAdapter)
- Create test doubles implementing these interfaces for unit testing
- Use SBTabProperties and SBTabItemProperties for property transfer
- Adapter pattern enables UI flexibility and testability

## Related Folders
- **XCore/**: Provides concrete adapter implementations
- **XCore/SilSidePane/**: Side pane UI using these adapters
- Test projects: Use test doubles implementing these interfaces

## References
- **Project files**: UIAdapterInterfaces.csproj (net462)
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: SIBInterface.cs, TMInterface.cs, HelperClasses.cs, UIAdapterInterfacesStrings.Designer.cs, AssemblyInfo.cs
- **Total lines of code**: 1395
- **Output**: Output/Debug/UIAdapterInterfaces.dll
- **Namespace**: SIL.FieldWorks.Common.UIAdapters
