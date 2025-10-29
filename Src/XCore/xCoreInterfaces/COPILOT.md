# XCore/xCoreInterfaces

## Purpose
Core interfaces for the XCore application framework. Defines the contracts for command handling, choice management, UI components, and the mediator pattern used throughout XCore.

## Key Components
- **xCoreInterfaces.csproj** - Framework interface definitions
- **Command.cs** - Command pattern interfaces
- **Choice.cs** - Choice/option interfaces
- **ChoiceGroup.cs** - Choice grouping interfaces
- **BaseContextHelper.cs** - Context helper base class
- **IFeedbackInfoProvider.cs** - Feedback interface
- **IImageCollection.cs** - Image collection interface
- **IPaneBar.cs** - Pane bar interface

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
