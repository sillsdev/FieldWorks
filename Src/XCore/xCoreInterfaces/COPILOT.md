---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# xCoreInterfaces

## Purpose
Core interfaces for the XCore application framework. Defines the contracts for command handling, choice management, UI components, and the mediator pattern used throughout XCore.

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
