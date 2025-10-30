---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexTextDll

## Purpose
Core LexText application logic and infrastructure. 
Implements the main application coordination, module initialization, integration of various 
areas (lexicon, morphology, interlinear, discourse), and shared services for the FLEx application. 
Serves as the integration layer that brings together all FLEx components into a cohesive application.

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

## Interfaces and Data Models

- **AreaListener** (class)
  - Path: `AreaListener.cs`
  - Public class implementation

- **FlexHelpTopicProvider** (class)
  - Path: `FlexHelpTopicProvider.cs`
  - Public class implementation

- **ImageHolder** (class)
  - Path: `ImageHolder.cs`
  - Public class implementation

- **LexTextApp** (class)
  - Path: `LexTextApp.cs`
  - Public class implementation

- **LexTextStrings** (class)
  - Path: `LexTextStrings.Designer.cs`
  - Public class implementation

- **SampleCitationFormTransducer** (class)
  - Path: `TransductionSample.cs`
  - Public class implementation

## References

- **Project files**: LexTextDll.csproj, LexTextDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AreaListener.cs, AreaListenerTests.cs, AssemblyInfo.cs, FlexHelpTopicProvider.cs, ImageHolder.cs, LexTextApp.cs, LexTextStrings.Designer.cs, RestoreDefaultsDlg.Designer.cs, RestoreDefaultsDlg.cs, TransductionSample.cs
- **Source file count**: 10 files
- **Data file count**: 4 files
