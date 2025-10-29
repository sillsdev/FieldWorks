# LexText

## Purpose
Lexicon/Dictionary application and related components. This is one of the major application areas of FieldWorks, providing comprehensive lexicon editing, dictionary configuration, interlinear text analysis, morphology, and discourse features.

## Key Components

### Subprojects
- **LexTextExe/** (LexTextExe.csproj) - Lexicon application executable
- **LexTextDll/** (LexTextDll.csproj) - Core lexicon functionality library
- **LexTextControls/** (LexTextControls.csproj) - Lexicon UI controls
- **Lexicon/** (LexEdDll.csproj) - Lexicon editing components
- **Interlinear/** - Interlinear text analysis and glossing
- **Morphology/** - Morphological analysis and parsing
- **Discourse/** - Discourse analysis features
- **ParserCore/** - Parsing engine components
- **ParserUI/** - Parser user interface
- **FlexPathwayPlugin/** (FlexPathwayPlugin.csproj) - Pathway publishing integration

## Technology Stack
- C# .NET WinForms/WPF
- Complex linguistic data processing
- Dictionary and lexicon management
- Interlinear glossing and text analysis

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), FdoUi (data object UI), XCore (framework), xWorks (shared app infrastructure)
- Used by: Linguists and language workers for lexicon and text work

## Build Information
- Multiple C# projects comprising the lexicon application
- Build entire suite via solution or individual projects
- Build with MSBuild or Visual Studio

## Entry Points
- **LexTextExe** - Main lexicon application executable
- **LexTextDll** - Core functionality exposed to other components

## Related Folders
- **xWorks/** - Shared application infrastructure for LexText
- **XCore/** - Framework components used by LexText
- **FdoUi/** - Data object UI used in lexicon editing
- **Cellar/** - Data model for lexicon data
- **FwParatextLexiconPlugin/** - Exposes LexText data to Paratext
- **ParatextImport/** - Imports Paratext data into LexText
