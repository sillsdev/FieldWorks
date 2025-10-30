---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexText/LexTextDll

## Purpose
Core lexicon application functionality library. Provides the main application logic and infrastructure for the LexText (FLEx) lexicon and dictionary application.

## Key Components
- **LexTextDll.csproj** - Core LexText library
- **LexTextApp.cs** - Main application class
- **AreaListener.cs** - Application area management
- **FlexHelpTopicProvider.cs** - Context-sensitive help
- **ImageHolder.cs** - Image resource management
- Application icons and resources

## Technology Stack
- C# .NET
- Application framework integration
- XCore plugin architecture

## Dependencies
- Depends on: XCore (framework), Cellar (data model), Common, all LexText subprojects
- Used by: LexText/LexTextExe (main executable)

## Build Information
- C# class library project
- Build via: `dotnet build LexTextDll.csproj`
- Core library loaded by LexText executable

## Entry Points
- LexTextApp class (main application logic)
- Area management and navigation
- Help system integration

## Related Folders
- **LexText/LexTextExe/** - Executable that loads LexTextDll
- **XCore/** - Framework hosting LexText
- **LexText/Lexicon/** - Lexicon editing features
- **LexText/Interlinear/** - Interlinear text features
- **LexText/Morphology/** - Morphology features
- All other LexText subfolders


## References
- **Project Files**: LexTextDll.csproj
- **Key C# Files**: AreaListener.cs, FlexHelpTopicProvider.cs, ImageHolder.cs, LexTextApp.cs, RestoreDefaultsDlg.cs, TransductionSample.cs
