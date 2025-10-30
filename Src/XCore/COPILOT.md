---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# XCore

## Purpose
Cross-cutting framework base used by multiple FieldWorks applications. Provides the application framework, plugin architecture, command handling, and UI composition infrastructure that all major FieldWorks applications are built upon.

## Key Components
- **AdapterMenuItem.cs**
- **AreaManager.cs**
- **CollapsingSplitContainer.cs**
- **HtmlControl.cs**
- **HtmlViewer.cs**


### Subprojects
Each subfolder has its own COPILOT.md file with detailed documentation:

- **xCore.csproj** - Main framework library (in this folder)
- **xCoreInterfaces/** - Framework interfaces (see xCoreInterfaces/COPILOT.md)
- **xCoreTests/** - Framework tests (see xCoreTests/COPILOT.md)
- **FlexUIAdapter/** - UI adapter for FLEx applications (see FlexUIAdapter/COPILOT.md)
- **SilSidePane/** - Side pane UI component (see SilSidePane/COPILOT.md)

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


## References
- **Project Files**: xCore.csproj
- **Key C# Files**: AdapterMenuItem.cs, AreaManager.cs, CollapsingSplitContainer.cs, HtmlControl.cs, HtmlViewer.cs, IconHolder.cs, ImageCollection.cs, ImageContent.cs
