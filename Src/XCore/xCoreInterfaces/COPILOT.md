---
last-reviewed: 2025-10-31
last-reviewed-tree: bb638469b95784020f72451e340085917d03c08131c070e65da65e14d5f18bb1
status: reviewed
---

# xCoreInterfaces

## Purpose
Core interface definitions and implementations (~7.8K lines) for XCore framework. Provides Mediator (central message broker), PropertyTable (property storage), IxCoreColleague (colleague pattern), ChoiceGroup/Choice (menu/toolbar definitions), Command (command pattern), IUIAdapter (UI adapter contracts), and IdleQueue (idle-time processing). Foundation for plugin-based extensibility across FieldWorks.

## Architecture
TBD - populate from code. See auto-generated hints below.

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
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **Upstream**: Minimal - SIL.Utils, System assemblies (pure interface definitions)
- **Downstream consumers**: XCore/ (Inventory, XWindow), XCore/FlexUIAdapter/, xWorks/, LexText/, all XCore-based apps

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

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
## Test Infrastructure
- **xCoreInterfacesTests/** subfolder
- Tests for: Mediator, PropertyTable, ChoiceGroup, Command

## Code Evidence
*Analysis based on scanning 23 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: XCore
