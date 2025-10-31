---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# LexTextDll

## Purpose
Core LexText application logic and infrastructure.
Implements the main application coordination, module initialization, integration of various
areas (lexicon, morphology, interlinear, discourse), and shared services for the FLEx application.
Serves as the integration layer that brings together all FLEx components into a cohesive application.

## Architecture
C# library with 10 source files. Contains 1 subprojects: LexTextDll.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build LexTextDll.csproj`
- Core library loaded by LexText executable

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

## Entry Points
- LexTextApp class (main application logic)
- Area management and navigation
- Help system integration

## Test Index
Test projects: LexTextDllTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **LexText/LexTextExe/** - Executable that loads LexTextDll
- **XCore/** - Framework hosting LexText
- **LexText/Lexicon/** - Lexicon editing features
- **LexText/Interlinear/** - Interlinear text features
- **LexText/Morphology/** - Morphology features
- All other LexText subfolders

## References

- **Project files**: LexTextDll.csproj, LexTextDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AreaListener.cs, AreaListenerTests.cs, AssemblyInfo.cs, FlexHelpTopicProvider.cs, ImageHolder.cs, LexTextApp.cs, LexTextStrings.Designer.cs, RestoreDefaultsDlg.Designer.cs, RestoreDefaultsDlg.cs, TransductionSample.cs
- **Source file count**: 10 files
- **Data file count**: 4 files

## References (auto-generated hints)
- Project files:
  - LexText/LexTextDll/LexTextDll.csproj
  - LexText/LexTextDll/LexTextDllTests/LexTextDllTests.csproj
- Key C# files:
  - LexText/LexTextDll/AreaListener.cs
  - LexText/LexTextDll/AssemblyInfo.cs
  - LexText/LexTextDll/FlexHelpTopicProvider.cs
  - LexText/LexTextDll/ImageHolder.cs
  - LexText/LexTextDll/LexTextApp.cs
  - LexText/LexTextDll/LexTextDllTests/AreaListenerTests.cs
  - LexText/LexTextDll/LexTextStrings.Designer.cs
  - LexText/LexTextDll/RestoreDefaultsDlg.Designer.cs
  - LexText/LexTextDll/RestoreDefaultsDlg.cs
  - LexText/LexTextDll/TransductionSample.cs
- Data contracts/transforms:
  - LexText/LexTextDll/HelpTopicPaths.resx
  - LexText/LexTextDll/ImageHolder.resx
  - LexText/LexTextDll/LexTextStrings.resx
  - LexText/LexTextDll/RestoreDefaultsDlg.resx
## Code Evidence
*Analysis based on scanning 8 source files*

- **Classes found**: 7 public classes
- **Namespaces**: LexTextDllTests, SIL.FieldWorks.XWorks.LexText
