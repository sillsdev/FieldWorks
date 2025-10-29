# XCore

## Purpose
Cross-cutting framework base used by multiple FieldWorks applications. Provides the application framework, plugin architecture, command handling, and UI composition infrastructure that all major FieldWorks applications are built upon.

## Key Components

### Subprojects
- **xCore.csproj** - Main framework library
- **xCoreInterfaces/xCoreInterfaces.csproj** - Framework interfaces
- **xCoreTests/xCoreTests.csproj** - Framework tests
- **FlexUIAdapter/FlexUIAdapter.csproj** - UI adapter for FLEx applications
- **SilSidePane/SilSidePane.csproj** - Side pane UI component

## Technology Stack
- C# .NET WinForms
- Plugin/mediator architecture
- Command pattern implementation
- Event-driven UI framework

## Dependencies
- Depends on: Common (UI infrastructure), FwResources (resources)
- Used by: xWorks, LexText (all major applications built on XCore)

## Build Information
- Multiple C# projects comprising the framework
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Testing
- Run tests: `dotnet test XCore/xCoreTests/xCoreTests.csproj`
- Tests cover framework functionality, command handling, plugins

## Entry Points
- Provides framework base classes for applications
- Main application shell infrastructure

## Related Folders
- **xWorks/** - Primary application built on XCore framework
- **LexText/** - Lexicon application using XCore architecture
- **Common/** - Provides lower-level UI components used by XCore
- **FwCoreDlgs/** - Dialogs integrated into XCore applications
- **FwResources/** - Resources used by XCore framework
