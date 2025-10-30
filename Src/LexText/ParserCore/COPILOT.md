---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ParserCore

## Purpose
Core parsing engine for morphological analysis. Provides the Hermit Crab (HC) parser implementation for analyzing words into morphemes based on morphological rules.

## Key Components
### Key Classes
- **ParserModelChangeListener**
- **HCLoader**
- **HCParser**
- **ParserReport**
- **ParseReport**
- **ParseResult**
- **ParseAnalysis**
- **ParseMorph**
- **WordformUpdatedEventArgs**
- **ParseFiler**

### Key Interfaces
- **IParser**
- **IHCLoadErrorLogger**
- **IXAmpleWrapper**

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

## Code Evidence
*Analysis based on scanning 41 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 3 public interfaces
- **Namespaces**: SIL.FieldWorks.WordWorks.Parser, XAmpleManagedWrapper, XAmpleManagedWrapperTests
