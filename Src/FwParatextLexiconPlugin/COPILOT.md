---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FwParatextLexiconPlugin

## Purpose
Paratext lexicon integration plugin. Enables FieldWorks lexicon data to be accessed and used within Paratext Bible translation software, providing seamless integration between the two systems.

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
