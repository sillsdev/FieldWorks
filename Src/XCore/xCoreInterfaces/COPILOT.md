---
last-reviewed: 2025-10-31
last-verified-commit: e38a4e9
status: reviewed
---

# xCoreInterfaces

## Purpose
Core interface definitions and implementations (~7.8K lines) for XCore framework. Provides Mediator (central message broker), PropertyTable (property storage), IxCoreColleague (colleague pattern), ChoiceGroup/Choice (menu/toolbar definitions), Command (command pattern), IUIAdapter (UI adapter contracts), and IdleQueue (idle-time processing). Foundation for plugin-based extensibility across FieldWorks.

## Key Components

### Core Patterns
- **Mediator** (Mediator.cs) - Central command routing and colleague coordination (~2.4K lines)
  - `BroadcastMessage(string message, object parameter)` - Message broadcast
  - `SendMessage(string message, object parameter)` - Direct message send
  - Manages: Colleague registration, property table, idle queue
- **PropertyTable**, **ReadOnlyPropertyTable** (PropertyTable.cs, ReadOnlyPropertyTable.cs) - Property storage with change notification
  - `SetProperty(string name, object value, bool doSetPropertyEvents)` - Set property with optional events
  - `GetValue<T>(string name)` - Strongly-typed property retrieval
- **IxCoreColleague** (IxCoreColleague.cs) - Colleague pattern interface
  - `IxCoreContentControl`, `IXCoreUserControl` - Specialized colleague interfaces
- **Command** (Command.cs) - Command pattern with undo/redo support
  - `ICommandUndoRedoText` interface for undo text customization

### UI Abstractions
- **ChoiceGroup**, **Choice**, **ChoiceGroupCollection** (ChoiceGroup.cs, Choice.cs) - Menu/toolbar definitions
  - XML-driven choice loading from Inventory
- **IUIAdapter**, **IUIMenuAdapter**, **ITestableUIAdapter** (IUIAdapter.cs) - UI adapter contracts
  - `IUIAdapterForceRegenerate` - Forces UI regeneration
  - `AdapterAssemblyFactory` - Creates UI adapters from assemblies

### Supporting Services
- **IdleQueue** (IdleQueue.cs) - Idle-time work queue
  - `AddTask(Task task)` - Queue work for idle execution
- **MessageSequencer** (MessageSequencer.cs) - Message sequencing and filtering
- **PersistenceProvider**, **IPersistenceProvider** (PersistenceProvider.cs, IPersistenceProvider.cs) - Settings persistence
- **BaseContextHelper**, **IContextHelper** (BaseContextHelper.cs) - Context-aware help
- **IFeedbackInfoProvider** (IFeedbackInfoProvider.cs) - User feedback interface
- **IImageCollection** (IImageCollection.cs) - Image resource access
- **RecordFilterListProvider** (RecordFilterListProvider.cs) - Record filtering support
- **IPaneBar** (IPaneBar.cs) - Pane bar interface
- **IPropertyRetriever** (IPropertyRetriever.cs) - Property access abstraction
- **List** (List.cs) - Generic list utilities

## Dependencies
- **Upstream**: Minimal - SIL.Utils, System assemblies (pure interface definitions)
- **Downstream consumers**: XCore/ (Inventory, XWindow), XCore/FlexUIAdapter/, xWorks/, LexText/, all XCore-based apps

## Test Infrastructure
- **xCoreInterfacesTests/** subfolder
- Tests for: Mediator, PropertyTable, ChoiceGroup, Command

## Related Folders
- **XCore/** - Main framework implementation using these interfaces
- **XCore/FlexUIAdapter/** - UI adapter implementations
- **xWorks/** - Application shell built on XCore interfaces

## References
- **Project**: xCoreInterfaces.csproj (.NET Framework 4.6.2 class library)
- **Test project**: xCoreInterfacesTests/xCoreInterfacesTests.csproj
- **~22 CS files** (~7.8K lines): Mediator.cs, PropertyTable.cs, IxCoreColleague.cs, ChoiceGroup.cs, Command.cs, IUIAdapter.cs, IdleQueue.cs, etc.
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

## Entry Points
- Framework interface contracts
- Command and choice abstractions
- UI component interfaces

## Test Index
Test projects: xCoreInterfacesTests. 3 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **XCore/** - Framework implementing these interfaces
- **XCore/FlexUIAdapter/** - Implements UI interfaces
- **Common/UIAdapterInterfaces/** - Related adapter interfaces
- **xWorks/** - Uses XCore interfaces
- **LexText/** - Uses XCore interfaces

## References

- **Project files**: xCoreInterfaces.csproj, xCoreInterfacesTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, BaseContextHelper.cs, ChoiceGroup.cs, IFeedbackInfoProvider.cs, IImageCollection.cs, IUIAdapter.cs, Mediator.cs, PropertyTable.cs, ReadOnlyPropertyTable.cs, RecordFilterListProvider.cs
- **XML data/config**: Settings.xml, db_TestLocal_Settings.xml
- **Source file count**: 26 files
- **Data file count**: 4 files

## References (auto-generated hints)
- Project files:
  - XCore/xCoreInterfaces/xCoreInterfaces.csproj
  - XCore/xCoreInterfaces/xCoreInterfacesTests/xCoreInterfacesTests.csproj
- Key C# files:
  - XCore/xCoreInterfaces/AssemblyInfo.cs
  - XCore/xCoreInterfaces/BaseContextHelper.cs
  - XCore/xCoreInterfaces/Choice.cs
  - XCore/xCoreInterfaces/ChoiceGroup.cs
  - XCore/xCoreInterfaces/Command.cs
  - XCore/xCoreInterfaces/IFeedbackInfoProvider.cs
  - XCore/xCoreInterfaces/IImageCollection.cs
  - XCore/xCoreInterfaces/IPaneBar.cs
  - XCore/xCoreInterfaces/IPersistenceProvider.cs
  - XCore/xCoreInterfaces/IPropertyRetriever.cs
  - XCore/xCoreInterfaces/IUIAdapter.cs
  - XCore/xCoreInterfaces/IdleQueue.cs
  - XCore/xCoreInterfaces/IxCoreColleague.cs
  - XCore/xCoreInterfaces/List.cs
  - XCore/xCoreInterfaces/Mediator.cs
  - XCore/xCoreInterfaces/MessageSequencer.cs
  - XCore/xCoreInterfaces/PersistenceProvider.cs
  - XCore/xCoreInterfaces/PropertyTable.cs
  - XCore/xCoreInterfaces/ReadOnlyPropertyTable.cs
  - XCore/xCoreInterfaces/RecordFilterListProvider.cs
  - XCore/xCoreInterfaces/xCoreInterfaces.Designer.cs
  - XCore/xCoreInterfaces/xCoreInterfacesTests/Properties/AssemblyInfo.cs
  - XCore/xCoreInterfaces/xCoreInterfacesTests/Properties/Resources.Designer.cs
  - XCore/xCoreInterfaces/xCoreInterfacesTests/PropertyTableTests.cs
  - XCore/xCoreInterfaces/xCoreInterfacesTests/TestMessageSequencer.cs
- Data contracts/transforms:
  - XCore/xCoreInterfaces/xCoreInterfaces.resx
  - XCore/xCoreInterfaces/xCoreInterfacesTests/Properties/Resources.resx
  - XCore/xCoreInterfaces/xCoreInterfacesTests/settingsBackup/Settings.xml
  - XCore/xCoreInterfaces/xCoreInterfacesTests/settingsBackup/db_TestLocal_Settings.xml
## Code Evidence
*Analysis based on scanning 23 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: XCore
