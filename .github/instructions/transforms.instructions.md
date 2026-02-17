---
applyTo: "Src/Transforms/**"
name: "transforms.instructions"
description: "Auto-generated concise instructions from COPILOT.md for Transforms"
---

# Transforms (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **Application/** (12 XSLT files): Parser integration and morphology export transforms
- **Presentation/** (7 XSLT files): HTML formatting and trace visualization
- **FxtM3ParserToXAmpleLex.xsl** - Converts M3 lexicon XML to XAmple unified dictionary format
- **FxtM3ParserToXAmpleADCtl.xsl** - Generates XAmple AD control files (analysis data configuration)
- **FxtM3ParserToToXAmpleGrammar.xsl** - Exports M3 grammar rules to XAmple grammar format
- **FxtM3ParserToGAFAWS.xsl** - Transforms M3 data to GAFAWS format (alternative parser)

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: 5fe2afbb8cb54bb9264efdb2cf2de46021c9343855105809e6ea6ee5be4ad9ee
status: reviewed
---

# Transforms

## Purpose
Collection of 19 XSLT 1.0 stylesheets organized in Application/ and Presentation/ subdirectories. Provides data transforms for parser integration (XAmple, HermitCrab, GAFAWS), morphology export, trace formatting, and linguistic publishing (XLingPap). Used by FXT tool and export features throughout FieldWorks.

## Architecture
Pure XSLT 1.0 stylesheet collection (no code compilation). Two subdirectories:
- **Application/** (12 XSLT files): Parser integration and morphology export transforms
- **Presentation/** (7 XSLT files): HTML formatting and trace visualization

No executable components; XSLTs loaded at runtime by .NET System.Xml.Xsl processor or external XSLT engines. Transforms applied by FXT tools (XDumper) and parser UI components to convert between M3 XML schema and parser-specific formats (XAmple, GAFAWS) or generate presentation HTML.

## Key Components

### Application/ (Parser Integration - 12 XSLT files)
- **FxtM3ParserToXAmpleLex.xsl** - Converts M3 lexicon XML to XAmple unified dictionary format
- **FxtM3ParserToXAmpleADCtl.xsl** - Generates XAmple AD control files (analysis data configuration)
- **FxtM3ParserToToXAmpleGrammar.xsl** - Exports M3 grammar rules to XAmple grammar format
- **FxtM3ParserToGAFAWS.xsl** - Transforms M3 data to GAFAWS format (alternative parser)
- **FxtM3MorphologySketch.xsl** - Generates morphology sketch documents for linguistic documentation
- **FxtM3ParserCommon.xsl** - Shared templates and utility functions for M3 parser exports (imported by other transforms)
- **CalculateStemNamesUsedInLexicalEntries.xsl** - Analyzes stem name usage across lexical entries
- **UnifyTwoFeatureStructures.xsl** - Feature structure unification logic (linguistic feature matching)
- **BoundaryMarkerGuids.xsl** - GUID definitions for phonological boundary markers (word/morpheme/syllabl
