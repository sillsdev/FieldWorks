# Common/FieldWorks

## Purpose
Core FieldWorks-specific utilities and application infrastructure. Provides fundamental application services including settings management, busy dialogs, and application-level helpers.

## Key Components
- **FieldWorks.csproj** - Main utilities library
- **ApplicationBusyDialog** - UI for long-running operations
- **FwRegistrySettings.cs** (in Framework) - Settings management
- Application icons and resources

## Technology Stack
- C# .NET
- Windows registry integration
- Application infrastructure patterns

## Dependencies
- Depends on: Common/Framework, Common/FwUtils
- Used by: All FieldWorks applications (xWorks, LexText)

## Build Information
- C# class library project
- Build via: `dotnet build FieldWorks.csproj`
- Part of Common solution

## Entry Points
- Provides application-level utilities
- Settings and configuration management
- Application state and busy indicators

## Related Folders
- **Common/Framework/** - Application framework components
- **Common/FwUtils/** - General utilities used by FieldWorks
- **xWorks/** - Main application using these utilities
- **XCore/** - Framework that integrates FieldWorks utilities
