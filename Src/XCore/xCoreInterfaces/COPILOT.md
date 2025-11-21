---
last-reviewed: 2025-10-31
last-reviewed-tree: aabfae3eb78cb8b4b91e19f7ae790467f34a684e9b51255fc952d305a1a96223
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/XCore/xCoreInterfaces. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# xCoreInterfaces

## Purpose
Core interface definitions and implementations (~7.8K lines) for XCore framework. Provides Mediator (central message broker), PropertyTable (property storage), IxCoreColleague (colleague pattern), ChoiceGroup/Choice (menu/toolbar definitions), Command (command pattern), IUIAdapter (UI adapter contracts), and IdleQueue (idle-time processing). Foundation for plugin-based extensibility across FieldWorks.

## Architecture
Core interface definitions (~7.8K lines) for XCore framework. Provides Mediator (message broker), PropertyTable (property storage), IxCoreColleague (plugin interface), ChoiceGroup/Choice (UI definitions), Command (command pattern), IUIAdapter (UI adapter contracts), IdleQueue (idle processing). Foundation for plugin extensibility across FieldWorks.

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

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Library type**: Pure interface definitions + core implementations
- **Key libraries**: Minimal dependencies (SIL.Utils, System assemblies)
- **Pattern**: Mediator, Command, Observer (property change notification)

## Dependencies
- **Upstream**: Minimal - SIL.Utils, System assemblies (pure interface definitions)
- **Downstream consumers**: XCore/ (Inventory, XWindow), XCore/FlexUIAdapter/, xWorks/, LexText/, all XCore-based apps

## Interop & Contracts
- **Mediator**: BroadcastMessage(), SendMessage() for command routing
- **IxCoreColleague**: Plugin interface (HandleMessage, PropertyValue methods)
- **PropertyTable**: GetValue<T>(), SetProperty() with change notification
- **ChoiceGroup/Choice**: XML-driven menu/toolbar definitions
- **IUIAdapter**: UI adapter interface for framework independence
- **IdleQueue**: AddTask() for idle-time processing

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
TBD - populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

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
- **Target frameworks**: net48
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
## Test Infrastructure
- **xCoreInterfacesTests/** subfolder
- Tests for: Mediator, PropertyTable, ChoiceGroup, Command

## Code Evidence
*Analysis based on scanning 23 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: XCore