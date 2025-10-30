---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexTextDll

## Purpose
Core lexicon application functionality library. Provides the main application logic and infrastructure for the LexText (FLEx) lexicon and dictionary application.

## Key Components
### Key Classes
- **FlexHelpTopicProvider**
- **AreaListener**
- **SampleCitationFormTransducer**
- **RestoreDefaultsDlg**
- **ImageHolder**
- **LexTextApp**
- **AreaListenerTests**

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

## Code Evidence
*Analysis based on scanning 8 source files*

- **Classes found**: 7 public classes
- **Namespaces**: LexTextDllTests, SIL.FieldWorks.XWorks.LexText
