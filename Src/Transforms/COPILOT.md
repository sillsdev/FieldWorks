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
- **BoundaryMarkerGuids.xsl** - GUID definitions for phonological boundary markers (word/morpheme/syllable)
- **MorphTypeGuids.xsl** - GUID definitions for morpheme types (prefix/suffix/stem/infix/etc.)
- **XAmpleTemplateVariables.xsl** - Variable definitions for XAmple template processing
- **FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl** - Word grammar debugging transform generation

### Presentation/ (Display Formatting - 7 XSLT files)
- **FormatXAmpleTrace.xsl** - Formats XAmple parser trace XML to styled HTML with step-by-step parse visualization
- **FormatHCTrace.xsl** - Formats HermitCrab parser trace XML to interactive HTML with collapsible sections
- **FormatXAmpleParse.xsl** - Formats XAmple parse results for display
- **FormatXAmpleWordGrammarDebuggerResult.xsl** - Formats word grammar debugger output to readable HTML
- **FormatCommon.xsl** - Shared formatting templates and CSS generation (imported by other presentation transforms)
- **JSFunctions.xsl** - Generates JavaScript functions for interactive HTML features (expand/collapse, highlighting)
- **XLingPap1.xsl** - Transforms linguistic data to XLingPap format for publication-quality papers

## Technology Stack
- **Language**: XSLT 1.0 (W3C Recommendation)
- **Processor**: .NET System.Xml.Xsl.XslCompiledTransform (runtime execution)
- **Input schemas**: M3 XML dump from LCModel (post-FXT processing)
- **Output formats**:
  - XAmple file formats: .lex (lexicon), .ana (grammar), .adctl (control files)
  - GAFAWS format
  - HTML with CSS and JavaScript (presentation)
  - XLingPap XML (linguistic publishing)
- **XSLT features used**:
  - xsl:key for efficient lookups (AffixAlloID, LexEntryID, StemMsaID, POSID keys)
  - xsl:import for code reuse (FxtM3ParserCommon.xsl, FormatCommon.xsl)
  - xsl:template with match patterns and modes
  - xsl:call-template for utility functions
- **No code compilation**: Pure data files, no build step required

## Dependencies
- **Upstream**: XSLT 1.0 processor (System.Xml.Xsl in .NET), LCModel XML export schema
- **Downstream consumers**: FXT/ (XDumper, XUpdater), ParserUI/ (trace display), LexText/Morphology/ (parser config), export features
- **Data contracts**: M3Dump XML schema (from LCModel), XAmple file formats, HermitCrab XML, XLingPap schema

## Interop & Contracts
- **Input contract**: M3 XML dump from LCModel
  - Schema: Post-CleanFWDump.xslt processing (FXT tool chain)
  - Elements: M3Dump root, lexical entries, morphemes, allomorphs, phonology, morphotactics
  - Attributes: GUIDs, feature structures, phonological environments
- **Output contracts**:
  - **XAmple formats**: Legacy parser file formats (.lex unified dictionary, .ana grammar, .adctl control)
  - **GAFAWS format**: Alternative parser input format
  - **HTML**: CSS-styled markup with optional JavaScript for interactive display
  - **XLingPap XML**: Linguistic publishing schema
- **XSLT processing model**: .NET XslCompiledTransform invokes transforms
  - Invocation: `transform.Transform(inputXml, outputWriter)`
  - Parameters: Optional xsl:param values passed at runtime
- **Key lookup optimization**: xsl:key elements enable O(1) GUID-based lookups
  - Example: `key('AffixAlloID', @AffixAllomorphGuid)` for fast allomorph resolution
- **Import dependencies**: Some XSLTs import shared utilities
  - FxtM3ParserCommon.xsl imported by parser export transforms
  - FormatCommon.xsl imported by presentation transforms

## Threading & Performance
- **Execution model**: XSLT processor runs synchronously on caller's thread
- **Performance characteristics**:
  - XslCompiledTransform: First-time compilation overhead (transform loaded and compiled to IL)
  - Subsequent transforms: Fast (compiled IL execution)
  - Large M3 dumps: Can take seconds to minutes for complex morphologies (10K+ entries)
  - xsl:key optimization: Critical for performance; without keys, O(n²) lookups would be prohibitive
- **Memory usage**: In-memory XML DOM for input/output
  - Large M3 dumps (>10MB) can consume 50-100MB during transformation
- **No caching**: XslCompiledTransform caching managed by calling code (FXT tools)
- **No threading**: Pure functional transforms, no shared state, thread-safe if XslCompiledTransform cached properly
- **Bottlenecks**:
  - Complex XPath queries without keys (avoided via xsl:key)
  - Large recursive template calls (minimized via modes and efficient patterns)
  - HTML generation: Fast (string concatenation optimized by XSLT processor)

## Config & Feature Flags
- **No configuration files**: XSLT behavior controlled by input XML content and runtime parameters
- **xsl:param parameters**: Transforms accept optional parameters
  - Example: Debug flags, output formatting options
  - Passed via XsltArgumentList at runtime
- **GUID constants**: BoundaryMarkerGuids.xsl, MorphTypeGuids.xsl define well-known GUIDs
  - Used for GUID-based lookups in M3 XML (morpheme types, boundary markers)
- **Transform selection**: Calling code chooses which XSLT to apply
  - XAmple export: FxtM3ParserToXAmpleLex.xsl, FxtM3ParserToXAmpleADCtl.xsl, etc.
  - Trace formatting: FormatXAmpleTrace.xsl vs FormatHCTrace.xsl (parser-specific)
- **Conditional processing**: xsl:if, xsl:choose for XML-driven behavior
  - Example: Different output based on morpheme type, feature values
- **No global state**: Pure functional transforms, no side effects

## Build Information
- **No build step**: XSLT files are pure data, no compilation required
- **Deployment**: Copied to DistFiles/ directory for distribution
  - Packaged with FLEx installer
  - Deployed to Program Files\SIL\FieldWorks\Transforms\
- **Validation**: XSLT syntax validated by XML parser (well-formedness check)
- **No unit tests**: XSLT correctness validated via integration tests in FXT/ and ParserCore/
- **Versioning**: XSLT files versioned with repository, no separate version metadata
- **Dependencies**: No external dependencies beyond XSLT 1.0 spec
- **Packaging**: Included in FLEx installer via DistFiles/ folder

## Interfaces and Data Models

### Input Data Models (M3 XML Schema)
- **M3Dump** (root element from LCModel export)
  - Purpose: Complete morphological and phonological data from FLEx
  - Shape: Nested elements for lexical entries, allomorphs, rules, environments
  - Key elements: LexEntry, MoForm, MoAffixAllomorph, MoStemAllomorph, PhEnvironment, PhPhoneme
  - Attributes: GUIDs for cross-references, feature structures, phonological representations

### Output Data Models
- **XAmple formats** (legacy parser)
  - **.lex files**: Unified dictionary (lexemes + allomorphs + features)
  - **.ana files**: Grammar rules (morphotactics, co-occurrence restrictions)
  - **.adctl files**: Control files (parser configuration)
  - Format: Custom text format with backslash markers (similar to SFM)

- **HTML output** (presentation transforms)
  - Purpose: Interactive parser trace visualization
  - Shape: HTML with embedded CSS and JavaScript
  - Features: Collapsible sections, syntax highlighting, step-by-step parse display

- **GAFAWS format**
  - Purpose: Alternative parser input (experimental)
  - Format: Custom XML schema

- **XLingPap XML**
  - Purpose: Linguistic paper formatting for publication
  - Format: XLingPap schema (linguistic publishing standard)

### XSLT Keys (Optimization Structures)
- **AffixAlloID**: Fast lookup of affix allomorphs by GUID
- **LexEntryID**: Fast lookup of lexical entries by GUID
- **StemMsaID**: Fast lookup of stem MSAs by GUID
- **POSID**: Fast lookup of parts of speech by GUID
- Many others for efficient cross-referencing

## Entry Points
- **FXT tools** (XDumper): Primary consumers for parser export transforms
  - Invocation: `XslCompiledTransform.Transform(m3XmlPath, outputPath)`
  - Workflow: LCModel → XML dump → FXT XDumper → XSLT transform → Parser files
- **ParserUI** (trace formatting): Applies presentation transforms
  - Invocation: `transform.Transform(traceXml, htmlOutput)`
  - Display: HTML rendered in Gecko browser (ParserUI/TryAWordDlg)
- **Export features**: XLingPap export for linguistic papers
- **Typical usage** (parser export):
  1. FLEx user configures parser (HermitCrab or XAmple)
  2. FXT XDumper exports M3 XML from LCModel
  3. XDumper applies FxtM3ParserToXAmpleLex.xsl (and other transforms)
  4. Output: XAmple .lex, .ana, .adctl files for parser
- **Typical usage** (trace formatting):
  1. User invokes Try A Word in parser UI
  2. Parser generates XML trace output
  3. ParserUI applies FormatHCTrace.xsl or FormatXAmpleTrace.xsl
  4. HTML rendered in dialog for user review

## Test Index
- **No dedicated XSLT tests**: XSLT correctness validated via integration tests
- **Integration test coverage**:
  - **FXT tests**: Validate M3 → XAmple export transforms
  - **ParserCore tests**: M3ToXAmpleTransformerTests.cs verifies transform outputs
  - **ParserUI tests**: WordGrammarDebuggingTests.cs validates debugging transforms
- **Test approach**: Compare transform output against known-good reference files
  - Input: M3 XML test files (ParserCoreTests/M3ToXAmpleTransformerTestsDataFiles/)
  - Expected output: XAmple .lex, .ana files
  - Validation: String comparison or XAmple parser invocation
- **Manual testing**:
  - Run FLEx parser configuration → FXT export → verify XAmple files
  - Try A Word dialog → verify HTML trace display
- **Test data**: 18+ XML test files in ParserCoreTests cover various morphological scenarios
  - Circumfixes, infixes, reduplication, irregular forms, stem names, clitics
- **No automated XSLT unit tests**: Would require XSLT test framework (not in place)

## Usage Hints
- **Parser configuration workflow**:
  1. Configure parser in FLEx (Tools → Configure → Parser)
  2. FXT XDumper exports M3 XML
  3. XDumper applies appropriate transforms (XAmple or GAFAWS)
  4. Parser files generated in project directory
- **Trace viewing**:
  - Tools → Parser → Try A Word
  - Enable "Trace parse" checkbox
  - FormatHCTrace.xsl or FormatXAmpleTrace.xsl applied to trace XML
  - HTML displayed in Gecko browser
- **Modifying transforms**:
  - Edit .xsl files in Src/Transforms/
  - Test via FXT or ParserUI
  - No compilation needed (changes take effect immediately)
- **Debugging transforms**:
  - Add xsl:message elements for debug output
  - Use XSLT debugger (Visual Studio XSLT debugging)
  - Compare output with reference files
- **Common pitfalls**:
  - Forgetting xsl:key definitions (severe performance degradation)
  - XSLT 1.0 limitations (no regex, limited string functions)
  - Namespace handling (M3 XML has default namespace)
  - GUID matching (case-sensitive string comparison)
- **Performance tips**:
  - Always use xsl:key for repeated lookups
  - Avoid // (descendant axis) in performance-critical paths
  - Cache XslCompiledTransform instances in calling code
- **Extension points**:
  - Add new parser format: Create new FxtM3ParserTo*.xsl transform
  - Custom trace formatting: Modify or create new Format*.xsl transform
  - New linguistic export: Adapt XLingPap1.xsl or create new export transform

## Related Folders
- **FXT/** - Applies these transforms via XDumper/XUpdater
- **LexText/ParserCore/** - HermitCrab and XAmple parsers consume generated files
- **LexText/ParserUI/** - Displays formatted parser traces using FormatHCTrace.xsl, FormatXAmpleTrace.xsl
- **LexText/Morphology/** - Morphology editors generate data consumed by parser transforms

## References
- **19 XSLT files** total: 12 in Application/, 7 in Presentation/
- **No compilation** - Pure data files, loaded at runtime by XSLT processor
- **Packaged with DistFiles** for distribution

## Auto-Generated Project and File References
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
