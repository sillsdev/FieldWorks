---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexText

## Purpose
Lexicon/Dictionary application and related components. This is one of the major application areas of FieldWorks, providing comprehensive lexicon editing, dictionary configuration, interlinear text analysis, morphology, and discourse features.

## Key Components

### Subprojects
Each subfolder has its own COPILOT.md file with detailed documentation:

- **LexTextExe/** - Lexicon application executable (see LexTextExe/COPILOT.md)
- **LexTextDll/** - Core lexicon functionality library (see LexTextDll/COPILOT.md)
- **LexTextControls/** - Lexicon UI controls (see LexTextControls/COPILOT.md)
- **Lexicon/** - Lexicon editing components (see Lexicon/COPILOT.md)
- **Interlinear/** - Interlinear text analysis and glossing (see Interlinear/COPILOT.md)
- **Morphology/** - Morphological analysis and parsing (see Morphology/COPILOT.md)
- **Discourse/** - Discourse analysis features (see Discourse/COPILOT.md)
- **ParserCore/** - Parsing engine components (see ParserCore/COPILOT.md)
- **ParserUI/** - Parser user interface (see ParserUI/COPILOT.md)
- **FlexPathwayPlugin/** - Pathway publishing integration (see FlexPathwayPlugin/COPILOT.md)

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
