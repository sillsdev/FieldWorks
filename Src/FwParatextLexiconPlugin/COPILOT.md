---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FwParatextLexiconPlugin

## Purpose
Integration plugin enabling Paratext Bible translation software to access
FieldWorks lexicon data. Implements the Paratext plugin interface to expose FLEx lexical database
as a searchable lexicon resource within Paratext, facilitating translation workflows that leverage
comprehensive lexical information.

## Key Components
### Key Classes
- **FilesToRestoreAreOlder**
- **FwLexiconPlugin**
- **FdoLexiconTests**

## Technology Stack
- C# .NET
- Paratext plugin API
- Lexicon data integration

## Dependencies
- Depends on: Cellar (data model), FdoUi (UI components), Paratext SDK
- Used by: Paratext application when accessing FieldWorks lexicons

## Build Information
- C# class library implementing Paratext plugin interface
- Includes test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Plugin entry points defined by Paratext plugin architecture
- Provides lexicon lookup and access from Paratext

## Related Folders
- **Paratext8Plugin/** - Plugin for Paratext 8 (different version)
- **ParatextImport/** - Imports data from Paratext into FieldWorks
- **LexText/** - Lexicon application whose data is exposed to Paratext

## Code Evidence
*Analysis based on scanning 25 source files*

- **Classes found**: 3 public classes
- **Namespaces**: SIL.FieldWorks.ParatextLexiconPlugin

## Interfaces and Data Models

- **FwLexiconPlugin** (class)
  - Path: `FwLexiconPlugin.cs`
  - Public class implementation

## References

- **Project files**: FwParatextLexiconPlugin.csproj, FwParatextLexiconPluginTests.csproj
- **Target frameworks**: net462
- **Key C# files**: ChooseFdoProjectForm.cs, FdoLexEntryLexeme.cs, FdoWordAnalysis.cs, FwLexiconPluginV2.cs, LexemeKey.cs, ParatextLexiconPluginLcmUI.cs, ParatextLexiconPluginRegistryHelper.template.cs, ParatextLexiconPluginThreadedProgress.cs, ProjectExistsForm.cs, Strings.Designer.cs
- **Source file count**: 32 files
- **Data file count**: 5 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\FwParatextLexiconPlugin\FwParatextLexiconPlugin.csproj
  - Src\FwParatextLexiconPlugin\FwParatextLexiconPluginTests\FwParatextLexiconPluginTests.csproj
  - Src\FwParatextLexiconPlugin\ILRepack.targets
- Key C# files:
  - Src\FwParatextLexiconPlugin\ChooseFdoProjectForm.Designer.cs
  - Src\FwParatextLexiconPlugin\ChooseFdoProjectForm.cs
  - Src\FwParatextLexiconPlugin\FdoLanguageText.cs
  - Src\FwParatextLexiconPlugin\FdoLexEntryLexeme.cs
  - Src\FwParatextLexiconPlugin\FdoLexemeAddedEventArgs.cs
  - Src\FwParatextLexiconPlugin\FdoLexicalRelation.cs
  - Src\FwParatextLexiconPlugin\FdoLexicon.cs
  - Src\FwParatextLexiconPlugin\FdoLexiconGlossAddedEventArgs.cs
  - Src\FwParatextLexiconPlugin\FdoLexiconSenseAddedEventArgs.cs
  - Src\FwParatextLexiconPlugin\FdoSemanticDomain.cs
  - Src\FwParatextLexiconPlugin\FdoWordAnalysis.cs
  - Src\FwParatextLexiconPlugin\FdoWordAnalysisV2.cs
  - Src\FwParatextLexiconPlugin\FdoWordformLexeme.cs
  - Src\FwParatextLexiconPlugin\FilesToRestoreAreOlder.Designer.cs
  - Src\FwParatextLexiconPlugin\FilesToRestoreAreOlder.cs
  - Src\FwParatextLexiconPlugin\FwLexiconPlugin.cs
  - Src\FwParatextLexiconPlugin\FwLexiconPluginV2.cs
  - Src\FwParatextLexiconPlugin\FwParatextLexiconPluginTests\DummyLcmUI.cs
  - Src\FwParatextLexiconPlugin\FwParatextLexiconPluginTests\FdoLexiconTests.cs
  - Src\FwParatextLexiconPlugin\FwParatextLexiconPluginTests\Properties\AssemblyInfo.cs
  - Src\FwParatextLexiconPlugin\GeneratedParatextLexiconPluginRegistryHelper.cs
  - Src\FwParatextLexiconPlugin\LexemeKey.cs
  - Src\FwParatextLexiconPlugin\ParatextLexiconPluginDirectoryFinder.cs
  - Src\FwParatextLexiconPlugin\ParatextLexiconPluginLcmUI.cs
  - Src\FwParatextLexiconPlugin\ParatextLexiconPluginProjectId.cs
- Data contracts/transforms:
  - Src\FwParatextLexiconPlugin\ChooseFdoProjectForm.resx
  - Src\FwParatextLexiconPlugin\FilesToRestoreAreOlder.resx
  - Src\FwParatextLexiconPlugin\ProjectExistsForm.resx
  - Src\FwParatextLexiconPlugin\Properties\Resources.resx
  - Src\FwParatextLexiconPlugin\Strings.resx
