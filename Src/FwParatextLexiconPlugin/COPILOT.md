---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FwParatextLexiconPlugin

## Purpose
Paratext lexicon integration plugin. Enables FieldWorks lexicon data to be accessed and used within Paratext Bible translation software, providing seamless integration between the two systems.

## Key Components
- **FwParatextLexiconPlugin.csproj** - Main plugin library
- **FwParatextLexiconPluginTests/FwParatextLexiconPluginTests.csproj** - Plugin tests

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

## Testing
- Run tests: `dotnet test FwParatextLexiconPlugin/FwParatextLexiconPluginTests/FwParatextLexiconPluginTests.csproj`
- Tests cover plugin integration and data access

## Entry Points
- Plugin entry points defined by Paratext plugin architecture
- Provides lexicon lookup and access from Paratext

## Related Folders
- **Paratext8Plugin/** - Plugin for Paratext 8 (different version)
- **ParatextImport/** - Imports data from Paratext into FieldWorks
- **LexText/** - Lexicon application whose data is exposed to Paratext


## References
- **Project Files**: FwParatextLexiconPlugin.csproj
- **Key C# Files**: ChooseFdoProjectForm.cs, FdoLanguageText.cs, FdoLexEntryLexeme.cs, FdoLexemeAddedEventArgs.cs, FdoLexicalRelation.cs, FdoLexicon.cs, FdoLexiconGlossAddedEventArgs.cs, FdoLexiconSenseAddedEventArgs.cs
