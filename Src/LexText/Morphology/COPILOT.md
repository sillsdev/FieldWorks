# LexText/Morphology

## Purpose
Morphological analysis and morphology editor. Provides tools for defining morphological rules, allomorph conditions, and phonological features.

## Key Components
- **MorphologyEditorDll.csproj** - Morphology editor library
- **AdhocCoProhibAtomicLauncher** - Co-prohibition rule editing (atomic)
- **AdhocCoProhibVectorLauncher** - Co-prohibition rule editing (vector)
- Morpheme and allomorph management
- Phonological rule editing
- Morphological feature management

## Technology Stack
- C# .NET WinForms
- Linguistic rule system
- Morphological analysis engine integration

## Dependencies
- Depends on: Cellar (data model), LexText/ParserCore, Common (UI)
- Used by: LexText/LexTextDll, LexText/Interlinear (for parsing)

## Build Information
- C# class library project
- Build via: `dotnet build MorphologyEditorDll.csproj`
- Morphology editing and rule management

## Entry Points
- Morphology editor interface
- Rule and feature editors
- Allomorph condition management

## Related Folders
- **LexText/ParserCore/** - Parsing engine using morphology rules
- **LexText/ParserUI/** - Parser UI for morphology
- **LexText/Interlinear/** - Uses morphology for text analysis
- **LexText/Lexicon/** - Lexicon data used in morphology
