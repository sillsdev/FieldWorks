---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ParserCore

## Purpose
Core morphological parsing engine (Hermit Crab).
Implements the HC (Hermit Crab) parser that analyzes words into component morphemes based on
linguistic rules defined in the morphology editor. Applies phonological rules, morphotactics,
and allomorphy to decompose words and propose analyses. Critical for computer-assisted
morphological analysis in FLEx.

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

## Interfaces and Data Models

- **IHCLoadErrorLogger** (interface)
  - Path: `IHCLoadErrorLogger.cs`
  - Public interface definition

- **IParser** (interface)
  - Path: `IParser.cs`
  - Public interface definition

- **IXAmpleWrapper** (interface)
  - Path: `XAmpleManagedWrapper/IXAmpleWrapper.cs`
  - Public interface definition

- **AmpleOptions** (class)
  - Path: `XAmpleManagedWrapper/AmpleOptions.cs`
  - Public class implementation

- **HCLoader** (class)
  - Path: `HCLoader.cs`
  - Public class implementation

- **HCParser** (class)
  - Path: `HCParser.cs`
  - Public class implementation

- **ParseAnalysis** (class)
  - Path: `ParseResult.cs`
  - Public class implementation

- **ParseFiler** (class)
  - Path: `ParseFiler.cs`
  - Public class implementation

- **ParseMorph** (class)
  - Path: `ParseResult.cs`
  - Public class implementation

- **ParseReport** (class)
  - Path: `ParserReport.cs`
  - Public class implementation

- **ParseResult** (class)
  - Path: `ParseResult.cs`
  - Public class implementation

- **ParserModelChangeListener** (class)
  - Path: `ParserModelChangeListener.cs`
  - Public class implementation

- **ParserReport** (class)
  - Path: `ParserReport.cs`
  - Public class implementation

- **ParserScheduler** (class)
  - Path: `ParserScheduler.cs`
  - Public class implementation

- **ParserUpdateEventArgs** (class)
  - Path: `ParserScheduler.cs`
  - Public class implementation

- **ParserWorker** (class)
  - Path: `ParserWorker.cs`
  - Public class implementation

- **TaskReport** (class)
  - Path: `TaskReport.cs`
  - Public class implementation

- **WordformUpdatedEventArgs** (class)
  - Path: `ParseFiler.cs`
  - Public class implementation

- **XAmpleDLLWrapper** (class)
  - Path: `XAmpleManagedWrapper/XAmpleDLLWrapper.cs`
  - Public class implementation

- **XAmpleParser** (class)
  - Path: `XAmpleParser.cs`
  - Public class implementation

- **XAmplePropertiesPreparer** (class)
  - Path: `XAmplePropertiesPreparer.cs`
  - Public class implementation

- **XAmplePropertiesXAmpleDataFilesAugmenter** (class)
  - Path: `XAmplePropertiesXAmpleDataFilesAugmenter.cs`
  - Public class implementation

- **XAmpleWrapper** (class)
  - Path: `XAmpleManagedWrapper/XAmpleWrapper.cs`
  - Public class implementation

- **ParserPriority** (enum)
  - Path: `ParserScheduler.cs`

- **TaskPhase** (enum)
  - Path: `TaskReport.cs`

## References

- **Project files**: ParserCore.csproj, ParserCoreTests.csproj, XAmpleCOMWrapper.vcxproj, XAmpleManagedWrapper.csproj, XAmpleManagedWrapperTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, FwXmlTraceManager.cs, HCLoader.cs, HCParser.cs, IParser.cs, InvalidReduplicationFormException.cs, ParseResult.cs, ParserCoreStrings.Designer.cs, ParserModelChangeListener.cs, ParserReport.cs
- **Key C++ files**: XAmpleCOMWrapper.cpp, XAmpleWrapper.cpp, XAmpleWrapperCore.cpp, stdafx.cpp
- **Key headers**: Resource.h, XAmpleWrapperCore.h, stdafx.h, xamplewrapper.h
- **XML data/config**: Failures.xml, IrregularlyInflectedFormsParserFxtResult.xml, M3FXTDump.xml, QuechuaMYLFxtResult.xml, emi-flexFxtResult.xml
- **Source file count**: 43 files
- **Data file count**: 20 files
