---
last-reviewed: 2025-10-31
last-reviewed-tree: 5fe2afbb8cb54bb9264efdb2cf2de46021c9343855105809e6ea6ee5be4ad9ee
status: reviewed
---

# Transforms

## Purpose
Collection of 19 XSLT 1.0 stylesheets organized in Application/ and Presentation/ subdirectories. Provides data transforms for parser integration (XAmple, HermitCrab, GAFAWS), morphology export, trace formatting, and linguistic publishing (XLingPap). Used by FXT tool and export features throughout FieldWorks.

## Architecture
TBD - populate from code. See auto-generated hints below.

## Key Components
TBD - populate from code. See auto-generated hints below.

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **Upstream**: XSLT 1.0 processor (System.Xml.Xsl in .NET), LCModel XML export schema
- **Downstream consumers**: FXT/ (XDumper, XUpdater), ParserUI/ (trace display), LexText/Morphology/ (parser config), export features
- **Data contracts**: M3Dump XML schema (from LCModel), XAmple file formats, HermitCrab XML, XLingPap schema

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
TBD - populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **FXT/** - Applies these transforms via XDumper/XUpdater
- **LexText/ParserCore/** - HermitCrab and XAmple parsers consume generated files
- **LexText/ParserUI/** - Displays formatted parser traces using FormatHCTrace.xsl, FormatXAmpleTrace.xsl
- **LexText/Morphology/** - Morphology editors generate data consumed by parser transforms

## References
- **19 XSLT files** total: 12 in Application/, 7 in Presentation/
- **No compilation** - Pure data files, loaded at runtime by XSLT processor
- **Packaged with DistFiles** for distribution

## References (auto-generated hints)
- Data contracts/transforms:
  - Src/Transforms/Application/BoundaryMarkerGuids.xsl
  - Src/Transforms/Application/CalculateStemNamesUsedInLexicalEntries.xsl
  - Src/Transforms/Application/FxtM3MorphologySketch.xsl
  - Src/Transforms/Application/FxtM3ParserCommon.xsl
  - Src/Transforms/Application/FxtM3ParserToGAFAWS.xsl
  - Src/Transforms/Application/FxtM3ParserToToXAmpleGrammar.xsl
  - Src/Transforms/Application/FxtM3ParserToXAmpleADCtl.xsl
  - Src/Transforms/Application/FxtM3ParserToXAmpleLex.xsl
  - Src/Transforms/Application/FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl
  - Src/Transforms/Application/MorphTypeGuids.xsl
  - Src/Transforms/Application/UnifyTwoFeatureStructures.xsl
  - Src/Transforms/Application/XAmpleTemplateVariables.xsl
  - Src/Transforms/Presentation/FormatCommon.xsl
  - Src/Transforms/Presentation/FormatHCTrace.xsl
  - Src/Transforms/Presentation/FormatXAmpleParse.xsl
  - Src/Transforms/Presentation/FormatXAmpleTrace.xsl
  - Src/Transforms/Presentation/FormatXAmpleWordGrammarDebuggerResult.xsl
  - Src/Transforms/Presentation/JSFunctions.xsl
  - Src/Transforms/Presentation/XLingPap1.xsl
## Subfolders

### Application/ (12 XSLT files)
Parser and morphology data generation transforms:
- **FxtM3ParserToXAmpleLex.xsl** - M3 to XAmple unified dictionary export
- **FxtM3ParserToXAmpleADCtl.xsl** - M3 to XAmple AD control file generation
- **FxtM3ParserToToXAmpleGrammar.xsl** - M3 to XAmple grammar export
- **FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl** - Word grammar debugging transforms
- **FxtM3ParserToGAFAWS.xsl** - M3 to GAFAWS format export
- **FxtM3MorphologySketch.xsl** - Morphology sketch document generation
- **FxtM3ParserCommon.xsl** - Shared templates and utilities for M3 parser exports
- **CalculateStemNamesUsedInLexicalEntries.xsl** - Stem name usage analysis
- **UnifyTwoFeatureStructures.xsl** - Feature unification logic
- **BoundaryMarkerGuids.xsl** - GUID definitions for phonological boundary markers
- **MorphTypeGuids.xsl** - GUID definitions for morpheme types
- **XAmpleTemplateVariables.xsl** - XAmple template variable definitions

### Presentation/ (7 XSLT files)
Formatting and display transforms:
- **FormatXAmpleTrace.xsl** - XAmple parser trace HTML formatting
- **FormatHCTrace.xsl** - HermitCrab parser trace HTML formatting
- **FormatXAmpleParse.xsl** - XAmple parse result formatting
- **FormatXAmpleWordGrammarDebuggerResult.xsl** - Word grammar debugger output formatting
- **FormatCommon.xsl** - Shared formatting templates and utilities
- **JSFunctions.xsl** - JavaScript function generation for interactive HTML
- **XLingPap1.xsl** - XLingPap linguistic paper formatting (publication-quality output)

## Key Transform Patterns

### M3 Parser Exports (Application/)
- Input: XML dump from LCModel M3 parser server (post CleanFWDump.xslt processing)
- Output: XAmple lexicon, grammar, AD control files for legacy XAmple parser
- Uses extensive XSL keys for efficient lookup: `AffixAlloID`, `LexEntryID`, `StemMsaID`, `POSID`, etc.
- Handles: Affixes, stems, allomorphs, inflection classes, feature structures, phonological environments

### Trace Formatters (Presentation/)
- Input: XML trace output from HermitCrab or XAmple parsers
- Output: Styled HTML with CSS and optional JavaScript for interactive exploration
- Provides: Collapsible sections, syntax highlighting, step-by-step parse visualization
