---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# xWorks

## Purpose
Primary FieldWorks application shell and modules. Provides the main application infrastructure, dictionary configuration, data tree navigation, and shared functionality used across all FieldWorks work areas. This is the core application framework that hosts LexText and other modules.

## Key Components
- **xWorks.csproj** - Main application library and shell
- **xWorksTests/xWorksTests.csproj** - Comprehensive application tests
- **Archiving/** - Data archiving and export features

### Key Classes/Features
- **DTMenuHandler.cs** - Data tree menu handling
- **DictionaryConfigManager.cs** - Dictionary configuration management
- **ConfigurableDictionaryNode.cs** - Dictionary node configuration
- **ConfiguredLcmGenerator.cs** - LCM-based content generation
- **CssGenerator.cs** - CSS generation for styled output
- **AddCustomFieldDlg.cs** - Custom field addition dialogs
- **CustomListDlg.cs** - Custom list management
- **InterestingTextList.cs** - Text list management
- **XmlDocConfigureDlg.cs** - XML document configuration

## Technology Stack
- C# .NET WinForms/WPF
- Application shell architecture
- Dictionary and data visualization
- XCore-based plugin framework

## Dependencies
- Depends on: XCore (framework), Cellar (data model), Common (UI), FdoUi (data UI), FwCoreDlgs (dialogs), views (rendering)
- Used by: End users as the main FieldWorks application

## Build Information
- C# application with extensive test suite
- Build with MSBuild or Visual Studio
- Primary executable for FieldWorks

## Testing
- Run tests: `dotnet test xWorks/xWorksTests/xWorksTests.csproj`
- Tests cover application functionality, configuration, and UI

## Entry Points
- Main application executable
- Application shell hosting various modules (LexText, etc.)
- Dictionary and data tree interfaces

## Related Folders
- **XCore/** - Application framework that xWorks is built on
- **LexText/** - Major module hosted by xWorks
- **Common/** - UI infrastructure used throughout xWorks
- **FdoUi/** - Data object UI components
- **FwCoreDlgs/** - Dialogs used in xWorks
- **views/** - Native rendering engine for data display
- **ManagedVwWindow/** - View window management
- **Cellar/** - Data model accessed by xWorks
- **FwResources/** - Resources used in xWorks UI


## References
- **Project Files**: xWorks.csproj
- **Key C# Files**: AddCustomFieldDlg.cs, ConcDecorator.cs, ConfigurableDictionaryNode.cs, ConfiguredLcmGenerator.cs, CssGenerator.cs, CustomListDlg.cs, DTMenuHandler.cs, DataTreeImages.cs
