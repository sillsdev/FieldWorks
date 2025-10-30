---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Transforms

## Purpose
Transformation assets (e.g., XSLT) and helpers for FieldWorks. Contains XSLT stylesheets and transformation definitions used for converting data between formats, generating reports, and exporting content.

## Key Components
No major public classes identified.

## Technology Stack
- XSLT (eXtensible Stylesheet Language Transformations)
- XML processing
- Template-based document generation

## Dependencies
- Depends on: XML data model
- Used by: FXT (transform tool), export features, report generation

## Build Information
- Resource files and XSLT templates
- No compilation required (data files)
- Packaged with application for runtime use

## Entry Points
- XSLT files loaded by transformation engine
- Used during export, report generation, and data conversion

## Related Folders
- **FXT/** - FieldWorks transform tool that applies these XSLT files
- **DocConvert/** - Document conversion using transformations
- **ParatextImport/** - May use transforms for data mapping
- **LexText/** - Uses transforms for dictionary export and formatting

## Code Evidence
*Analysis based on scanning 0 source files*

## Interfaces and Data Models

- **BoundaryMarkerGuids** (xslt)
  - Path: `Application/BoundaryMarkerGuids.xsl`
  - XSLT transformation template

- **CalculateStemNamesUsedInLexicalEntries** (xslt)
  - Path: `Application/CalculateStemNamesUsedInLexicalEntries.xsl`
  - XSLT transformation template

- **FormatCommon** (xslt)
  - Path: `Presentation/FormatCommon.xsl`
  - XSLT transformation template

- **FormatHCTrace** (xslt)
  - Path: `Presentation/FormatHCTrace.xsl`
  - XSLT transformation template

- **FormatXAmpleParse** (xslt)
  - Path: `Presentation/FormatXAmpleParse.xsl`
  - XSLT transformation template

- **FormatXAmpleTrace** (xslt)
  - Path: `Presentation/FormatXAmpleTrace.xsl`
  - XSLT transformation template

- **FormatXAmpleWordGrammarDebuggerResult** (xslt)
  - Path: `Presentation/FormatXAmpleWordGrammarDebuggerResult.xsl`
  - XSLT transformation template

- **FxtM3MorphologySketch** (xslt)
  - Path: `Application/FxtM3MorphologySketch.xsl`
  - XSLT transformation template

- **FxtM3ParserCommon** (xslt)
  - Path: `Application/FxtM3ParserCommon.xsl`
  - XSLT transformation template

- **FxtM3ParserToGAFAWS** (xslt)
  - Path: `Application/FxtM3ParserToGAFAWS.xsl`
  - XSLT transformation template

- **FxtM3ParserToToXAmpleGrammar** (xslt)
  - Path: `Application/FxtM3ParserToToXAmpleGrammar.xsl`
  - XSLT transformation template

- **FxtM3ParserToXAmpleADCtl** (xslt)
  - Path: `Application/FxtM3ParserToXAmpleADCtl.xsl`
  - XSLT transformation template

- **FxtM3ParserToXAmpleLex** (xslt)
  - Path: `Application/FxtM3ParserToXAmpleLex.xsl`
  - XSLT transformation template

- **FxtM3ParserToXAmpleWordGrammarDebuggingXSLT** (xslt)
  - Path: `Application/FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl`
  - XSLT transformation template

- **JSFunctions** (xslt)
  - Path: `Presentation/JSFunctions.xsl`
  - XSLT transformation template

- **MorphTypeGuids** (xslt)
  - Path: `Application/MorphTypeGuids.xsl`
  - XSLT transformation template

- **UnifyTwoFeatureStructures** (xslt)
  - Path: `Application/UnifyTwoFeatureStructures.xsl`
  - XSLT transformation template

- **XAmpleTemplateVariables** (xslt)
  - Path: `Application/XAmpleTemplateVariables.xsl`
  - XSLT transformation template

- **XLingPap1** (xslt)
  - Path: `Presentation/XLingPap1.xsl`
  - XSLT transformation template

## References

- **XSLT transforms**: CalculateStemNamesUsedInLexicalEntries.xsl, FxtM3MorphologySketch.xsl, FxtM3ParserCommon.xsl, FxtM3ParserToToXAmpleGrammar.xsl, FxtM3ParserToXAmpleADCtl.xsl
- **Source file count**: 0 files
- **Data file count**: 19 files
