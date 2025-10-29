# LexText/ParserCore

## Purpose
Core parsing engine for morphological analysis. Provides the Hermit Crab (HC) parser implementation for analyzing words into morphemes based on morphological rules.

## Key Components
- **ParserCore.csproj** - Parser engine library
- **HCParser.cs** - Main Hermit Crab parser implementation
- **HCLoader.cs** - Parser rule and data loader
- **FwXmlTraceManager.cs** - Parser trace management
- **IParser.cs** - Parser interface
- **IHCLoadErrorLogger.cs** - Error logging interface
- Exception classes for parsing errors

## Technology Stack
- C# .NET
- Hermit Crab parsing algorithm
- Morphological analysis engine

## Dependencies
- Depends on: Cellar (data model for morphology), LexText/Morphology (rules)
- Used by: LexText/Interlinear, LexText/ParserUI

## Build Information
- C# class library project
- Build via: `dotnet build ParserCore.csproj`
- Core parsing engine

## Entry Points
- HCParser class for morphological parsing
- Parser loading and configuration
- Trace and error logging

## Related Folders
- **LexText/ParserUI/** - UI for parser configuration and testing
- **LexText/Morphology/** - Morphology rules used by parser
- **LexText/Interlinear/** - Uses parser for text analysis
- **LexText/Lexicon/** - Lexicon data used in parsing
