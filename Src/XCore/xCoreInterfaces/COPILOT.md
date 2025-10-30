---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# xCoreInterfaces

## Purpose
Core interface definitions for the XCore framework.
Declares fundamental contracts for command handling (IxCoreColleague), choice management
(ChoiceGroup), property tables (PropertyTable), mediator pattern (Mediator), and UI component
integration. These interfaces define the extensibility architecture that enables plugin-based
application composition in FieldWorks.

## Key Components
### Key Classes
- **PropertyTable**
- **BaseContextHelper**
- **ReadOnlyPropertyTable**
- **ChoiceRelatedClass**
- **ChoiceGroupCollection**
- **ChoiceGroup**
- **MediatorDisposeAttribute**
- **Mediator**
- **RecordFilterListProvider**
- **AdapterAssemblyFactory**

### Key Interfaces
- **IFeedbackInfoProvider**
- **IContextHelper**
- **IUIAdapter**
- **IUIAdapterForceRegenerate**
- **IUIMenuAdapter**
- **ITestableUIAdapter**
- **IImageCollection**
- **IxCoreColleague**

## Technology Stack
- C# .NET
- Interface-based design
- Mediator and command patterns

## Dependencies
- Depends on: Minimal (pure interface definitions)
- Used by: XCore, XCore/FlexUIAdapter, all XCore-based applications

## Build Information
- C# interface library project
- Build via: `dotnet build xCoreInterfaces.csproj`
- Pure interface definitions

## Entry Points
- Framework interface contracts
- Command and choice abstractions
- UI component interfaces

## Related Folders
- **XCore/** - Framework implementing these interfaces
- **XCore/FlexUIAdapter/** - Implements UI interfaces
- **Common/UIAdapterInterfaces/** - Related adapter interfaces
- **xWorks/** - Uses XCore interfaces
- **LexText/** - Uses XCore interfaces

## Code Evidence
*Analysis based on scanning 23 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: XCore

## Interfaces and Data Models

- **ICommandUndoRedoText** (interface)
  - Path: `Command.cs`
  - Public interface definition

- **IContextHelper** (interface)
  - Path: `BaseContextHelper.cs`
  - Public interface definition

- **IFeedbackInfoProvider** (interface)
  - Path: `IFeedbackInfoProvider.cs`
  - Public interface definition

- **IImageCollection** (interface)
  - Path: `IImageCollection.cs`
  - Public interface definition

- **IMediatorProvider** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **IPaneBar** (interface)
  - Path: `IPaneBar.cs`
  - Public interface definition

- **IPaneBarUser** (interface)
  - Path: `IPaneBar.cs`
  - Public interface definition

- **IPersistenceProvider** (interface)
  - Path: `IPersistenceProvider.cs`
  - Public interface definition

- **IPropertyRetriever** (interface)
  - Path: `IPropertyRetriever.cs`
  - Public interface definition

- **IPropertyTableProvider** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **ISnapSplitPosition** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **ITestableUIAdapter** (interface)
  - Path: `IUIAdapter.cs`
  - Public interface definition

- **IUIAdapter** (interface)
  - Path: `IUIAdapter.cs`
  - Public interface definition

- **IUIAdapterForceRegenerate** (interface)
  - Path: `IUIAdapter.cs`
  - Public interface definition

- **IUIMenuAdapter** (interface)
  - Path: `IUIAdapter.cs`
  - Public interface definition

- **IXCoreUserControl** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **IxCoreColleague** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **IxCoreContentControl** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **IxCoreCtrlTabProvider** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **IxWindow** (interface)
  - Path: `IxCoreColleague.cs`
  - Public interface definition

- **AdapterAssemblyFactory** (class)
  - Path: `IUIAdapter.cs`
  - Public class implementation

- **BaseContextHelper** (class)
  - Path: `BaseContextHelper.cs`
  - Public class implementation

- **BoolPropertyChoice** (class)
  - Path: `Choice.cs`
  - Public class implementation

- **ChoiceBase** (class)
  - Path: `Choice.cs`
  - Public class implementation

- **ChoiceGroup** (class)
  - Path: `ChoiceGroup.cs`
  - Public class implementation

- **ChoiceGroupCollection** (class)
  - Path: `ChoiceGroup.cs`
  - Public class implementation

- **ChoiceRelatedClass** (class)
  - Path: `ChoiceGroup.cs`
  - Public class implementation

- **CommandChoice** (class)
  - Path: `Choice.cs`
  - Public class implementation

- **List** (class)
  - Path: `List.cs`
  - Public class implementation

- **ListItem** (class)
  - Path: `List.cs`
  - Public class implementation

- **Mediator** (class)
  - Path: `Mediator.cs`
  - Public class implementation

- **MediatorDisposeAttribute** (class)
  - Path: `Mediator.cs`
  - Public class implementation

- **PropertyTable** (class)
  - Path: `PropertyTable.cs`
  - Public class implementation

- **ReadOnlyPropertyTable** (class)
  - Path: `ReadOnlyPropertyTable.cs`
  - Public class implementation

- **RecordFilterListProvider** (class)
  - Path: `RecordFilterListProvider.cs`
  - Public class implementation

- **SeparatorItem** (class)
  - Path: `List.cs`
  - Public class implementation

- **TemporaryColleagueParameter** (class)
  - Path: `IUIAdapter.cs`
  - Public class implementation

- **ToolTipHolder** (class)
  - Path: `IUIAdapter.cs`
  - Public class implementation

- **UIItemDisplayProperties** (class)
  - Path: `IUIAdapter.cs`
  - Public class implementation

- **UIListDisplayProperties** (class)
  - Path: `IUIAdapter.cs`
  - Public class implementation

- **ColleaguePriority** (enum)
  - Path: `IxCoreColleague.cs`

- **SettingsGroup** (enum)
  - Path: `PropertyTable.cs`

## References

- **Project files**: xCoreInterfaces.csproj, xCoreInterfacesTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, BaseContextHelper.cs, ChoiceGroup.cs, IFeedbackInfoProvider.cs, IImageCollection.cs, IUIAdapter.cs, Mediator.cs, PropertyTable.cs, ReadOnlyPropertyTable.cs, RecordFilterListProvider.cs
- **XML data/config**: Settings.xml, db_TestLocal_Settings.xml
- **Source file count**: 26 files
- **Data file count**: 4 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.
