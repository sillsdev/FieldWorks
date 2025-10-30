---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Interlinear

## Purpose
Interlinear text analysis and glossing functionality. Provides tools for creating and managing interlinear texts with morpheme-by-morpheme analysis and glossing.

## Key Components
### Key Classes
- **InterlinPrintChild**
- **InterlinPrintVc**
- **ConcordanceControl**
- **OccurrencesOfSelectedUnit**
- **MatchingConcordanceItems**
- **InterlinearTextsRecordClerk**
- **InterlinearExportDialog**
- **InterlinDocChart**
- **ParseIsCurrentFixer**
- **ComplexConcLeafNode**

### Key Interfaces
- **IParaDataLoader**
- **ISelectOccurrence**
- **ISetupLineChoices**
- **IInterlinearTabControl**
- **IStyleSheet**
- **IHandleBookmark**
- **IStTextBookmark**
- **IInterlinConfigurable**

## Technology Stack
- C# .NET WinForms
- Complex text layout and rendering
- Linguistic analysis algorithms

## Dependencies
- Depends on: Cellar (data model), Common (UI and views), LexText/ParserCore
- Used by: LexText application for text analysis

## Build Information
- C# class library project
- Build via: `dotnet build ITextDll.csproj` (Note: project named ITextDll)
- Core component of text analysis features

## Entry Points
- Interlinear text editor
- Morpheme analysis and glossing
- Text import from various formats

## Related Folders
- **LexText/Morphology/** - Morphological parsing for interlinear
- **LexText/ParserCore/** - Parsing engine for analysis
- **LexText/Discourse/** - Discourse analysis on interlinear texts
- **Common/SimpleRootSite/** - View hosting for interlinear display
- **views/** - Native rendering for complex interlinear layout

## Code Evidence
*Analysis based on scanning 108 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 8 public interfaces
- **Namespaces**: SIL.FieldWorks.IText, SIL.FieldWorks.IText.FlexInterlinModel, has, needs, via
