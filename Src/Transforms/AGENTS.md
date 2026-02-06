---
last-reviewed: 2025-11-21
last-reviewed-tree: 5fe2afbb8cb54bb9264efdb2cf2de46021c9343855105809e6ea6ee5be4ad9ee
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - application-parser-integration---12-xslt-files
  - presentation-display-formatting---7-xslt-files
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - referenced-by
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Transforms

## Purpose
Collection of 19 XSLT 1.0 stylesheets organized in Application/ and Presentation/ subdirectories. Provides data transforms for parser integration (XAmple, HermitCrab, GAFAWS), morphology export, trace formatting, and linguistic publishing (XLingPap). Used by FXT tool and export features throughout FieldWorks.

### Referenced By

- [FLExTools Integration](../../openspec/specs/integration/external/flextools.md#behavior) — Shared transform pipeline

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

### Referenced By

- [Parsing Rules](../../openspec/specs/grammar/parsing/rules.md#behavior) — Parser rule transforms
- [Parser Troubleshooting](../../openspec/specs/grammar/parsing/troubleshooting.md#behavior) — Trace formatting transforms
- [Grammar Sketch Generation](../../openspec/specs/grammar/sketch/generation.md#behavior) — Morphology sketch transforms

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
XSLT processor runs synchronously on caller's thread. No threading, pure functional transforms. xsl:key optimization critical for performance.

## Config & Feature Flags
No configuration files. xsl:param parameters passed via XsltArgumentList. GUID constants in BoundaryMarkerGuids.xsl, MorphTypeGuids.xsl.

## Build Information
No build step. XSLT files deployed to DistFiles/ and packaged with FLEx installer.

## Interfaces and Data Models
Input: M3Dump XML (LCModel export). Output: XAmple formats (.lex, .ana, .adctl), HTML (trace visualization), GAFAWS, XLingPap. XSLT keys for GUID-based lookups.

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

### Referenced By

- [Fixtures](../../openspec/specs/architecture/testing/fixtures.md#fixture-patterns) — Transform fixtures and reference outputs

## Usage Hints
FXT XDumper exports M3 XML and applies transforms. Edit .xsl files directly (no compilation). Use xsl:key for performance. View traces via Tools→Parser→Try A Word.

## Related Folders
- **FXT/**: XDumper/XUpdater
- **ParserCore/**: Parser consumers
- **ParserUI/**: Trace display

## References
19 XSLT files: 12 in Application/ (parser exports), 7 in Presentation/ (trace formatting). Key: FxtM3ParserToXAmpleLex.xsl, FormatHCTrace.xsl. See `.cache/copilot/diff-plan.json` for file listings.
