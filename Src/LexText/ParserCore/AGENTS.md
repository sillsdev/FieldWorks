---
last-reviewed: 2025-11-21
last-reviewed-tree: a27deb8a34df07ceee65bb7e2b8d7fb0107a22c6ac63372832cc9d49be2feecd
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - referenced-by
  - build-information
  - interfaces-and-data-models
  - interfaces
  - data-models
  - xml-data-contracts
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references
  - test-information

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# ParserCore

## Purpose
Morphological parser infrastructure supporting both HermitCrab and XAmple parsing engines. Implements background parsing scheduler (ParserScheduler/ParserWorker) with priority queue, manages parse result filing (ParseFiler), provides parser model change detection (ParserModelChangeListener), and wraps both HC (HermitCrab via SIL.Machine) and XAmple (legacy C++ parser via COM/managed wrapper) engines. Enables computer-assisted morphological analysis in FLEx by decomposing words into morphemes based on linguistic rules, phonology, morphotactics, and allomorphy defined in the morphology editor.

## Architecture
C# library (net48) with 34 source files (~9K lines total). Contains 3 subprojects: ParserCore (main library), XAmpleManagedWrapper (C# wrapper for XAmple DLL), XAmpleCOMWrapper (C++/CLI COM wrapper for XAmple). Supports pluggable parser architecture via IParser interface (HCParser for HermitCrab, XAmpleParser for legacy XAmple).

## Key Components
- **ParserScheduler/ParserWorker**: Background thread scheduler with priority queue, executes parse tasks, manages ParserWork items and ParseFiler for result filing
- **HCParser/XAmpleParser**: IParser implementations for HermitCrab (SIL.Machine) and legacy XAmple (COM interop) engines
- **ParseFiler**: Files parse results to database (IWfiAnalysis objects), merges duplicate analyses
- **ParserModelChangeListener**: Monitors LCModel changes via PropChanged events for parser reload detection
- **XAmple wrappers**: XAmpleManagedWrapper (C#), XAmpleCOMWrapper (C++/CLI), M3ToXAmpleTransformer for legacy parser
- **Data structures**: ParseResult, ParseAnalysis, ParseMorph, TaskReport, ParserPriority (enum)

### Referenced By

- [Parsing Rules](../../../openspec/specs/grammar/parsing/rules.md#behavior) — Parser rule consumption

## Technology Stack
C# (net48) and C++/CLI. Key libraries: SIL.Machine.Morphology.HermitCrab, SIL.LCModel, XCore, native XAmple DLL (COM).

## Dependencies
**Upstream**: SIL.Machine.Morphology.HermitCrab, SIL.LCModel, XCore, native XAmple DLL
**Downstream**: Consumed by ParserUI, Interlinear, Morphology

### Referenced By

- [Morphology Categories](../../../openspec/specs/grammar/morphology/categories.md#behavior) — Parser uses morphology categories
- [Morphological Affixes](../../../openspec/specs/grammar/morphology/affixes.md#behavior) — Parser consumes morphology rules
- [Allomorphs](../../../openspec/specs/grammar/morphology/allomorphs.md#behavior) — Parser uses allomorph data

## Interop & Contracts
XAmpleCOMWrapper (C++/CLI) exposes IXAmpleWrapper COM interface to native XAmple.dll. Data contracts: ParseResult/ParseAnalysis/ParseMorph DTOs, ParserUpdateEventArgs.

## Threading & Performance
Single background thread (ParserWorker) with 5-level priority queue. Results delivered to UI thread via IdleQueue. XAmple requires STA threading.

## Config & Feature Flags
Parser selection via MorphologicalDataOA.ActiveParser (HCParser vs XAmpleParser). Options: GuessRoots, MergeAnalyses (HC); AmpleOptions (XAmple). FwXmlTraceManager for trace generation.

### Referenced By

- [Parser Configuration](../../../openspec/specs/grammar/parsing/configuration.md#behavior) — Parser settings

## Build Information
C# library (net48). Build via `msbuild ParserCore.csproj`. Output: ParserCore.dll, XAmpleManagedWrapper.dll, XAmpleCOMWrapper.dll (C++/CLI).

## Interfaces and Data Models

### Interfaces
- **IParser** (path: Src/LexText/ParserCore/IParser.cs)
  - Purpose: Abstract parser contract for HermitCrab and XAmple implementations
  - Inputs: string word (for ParseWord), TextWriter (for trace methods)
  - Outputs: ParseResult (with analyses and error messages)
  - Methods: ParseWord(), TraceWord(), TraceWordXml(), Update(), Reset(), IsUpToDate()
  - Notes: Thread-safe, reusable across multiple parse operations

- **IHCLoadErrorLogger** (path: Src/LexText/ParserCore/IHCLoadErrorLogger.cs)
  - Purpose: Log errors during HermitCrab grammar/lexicon loading
  - Inputs: Error messages from HCLoader
  - Outputs: Logged error information for debugging
  - Notes: Used by HCLoader to report load failures

- **IXAmpleWrapper** (path: Src/LexText/ParserCore/XAmpleManagedWrapper/*)
  - Purpose: COM interface for native XAmple parser access
  - Inputs: Grammar files, word strings, AmpleOptions
  - Outputs: Parse results in XAmple format
  - Notes: COM STA threading model required, legacy interface

### Data Models
- **ParseResult** (path: Src/LexText/ParserCore/ParseResult.cs)
  - Purpose: Top-level container for parse results
  - Shape: ParseAnalyses (List<ParseAnalysis>), ErrorMessages (List<string>)
  - Consumers: ParseFiler (files to database), UI components (displays results)

- **ParseAnalysis** (path: Src/LexText/ParserCore/ParseResult.cs)
  - Purpose: Single morphological analysis for a wordform
  - Shape: ParseMorphs (List<ParseMorph>), Shape (surface form string), ParseSuccess (bool)
  - Consumers: ParseFiler creates IWfiAnalysis objects from these

- **ParseMorph** (path: Src/LexText/ParserCore/ParseResult.cs)
  - Purpose: Single morpheme in an analysis
  - Shape: Form (string), Msa (IMoMorphSynAnalysis ref), Morph (IMoForm ref), MsaPartId/MorphPartId (Guids)
  - Consumers: ParseFiler maps to database WfiMorphBundle objects

- **TaskReport** (path: Src/LexText/ParserCore/TaskReport.cs)
  - Purpose: Progress tracking for parse operations
  - Shape: TaskPhase (enum), PercentComplete (int), CurrentTasks/TotalTasks (int)
  - Consumers: UI components display progress during bulk parsing

### XML Data Contracts
- **FXT XML** (path: Various test files in ParserCoreTests/M3ToXAmpleTransformerTestsDataFiles/)
  - Purpose: FieldWorks export format for morphology data
  - Shape: XML with morphemes, allomorphs, MSAs, phonological rules, templates
  - Consumers: M3ToXAmpleTransformer converts to XAmple grammar format

## Entry Points
- **ParserScheduler** (primary): Created and managed by UI layer (ParserUI)
  - Instantiation: new ParserScheduler(cache, propertyTable)
  - Usage: ScheduleWork() to queue parse operations, handles background thread lifecycle
- **HCParser/XAmpleParser**: Instantiated by ParserWorker based on ActiveParser setting
  - HCParser: Uses SIL.Machine.Morphology.HermitCrab for parsing
  - XAmpleParser: Uses XAmpleManagedWrapper COM interop for legacy XAmple
- **ParseFiler**: Created by ParserWorker to file parse results to database
  - Instantiation: new ParseFiler(cache, agent, idleQueue, taskUpdateHandler)
  - Usage: ProcessParses() to convert ParseResult objects to IWfiAnalysis database objects
- **Invocation patterns**:
  - Interactive: TryAWord dialog → ParserScheduler.ScheduleTryAWordWork()
  - Batch: Bulk parse command → ParserScheduler.ScheduleWork(BulkParseWork)
  - Background: Text analysis → Interlinear calls ParseFiler directly

## Test Index
- **Test projects**:
  - ParserCoreTests/ParserCoreTests.csproj (~2.7K lines, 18 test files)
  - XAmpleManagedWrapper/XAmpleManagedWrapperTests/XAmpleManagedWrapperTests.csproj
- **Key test files**:
  - HCLoaderTests.cs: HermitCrab grammar/lexicon loading
  - M3ToXAmpleTransformerTests.cs: FXT XML to XAmple format conversion (18 XML test data files)
  - ParseFilerProcessingTests.cs: Parse result filing to database
  - ParseWorkerTests.cs: Background worker thread behavior
  - ParserReportTests.cs: Parser status reporting
  - XAmpleParserTests.cs: Legacy XAmple parser integration
- **Test data**: 18 XML files in ParserCoreTests/M3ToXAmpleTransformerTestsDataFiles/
  - Abaza-OrderclassPlay.xml, CliticEnvsParserFxtResult.xml, ConceptualIntroTestParserFxtResult.xml, etc.
  - Cover various morphological phenomena: clitics, circumfixes, infixes, reduplication, irregular forms
- **Test runners**:
  - Visual Studio Test Explorer
  - `dotnet test ParserCore.sln` (if SDK-style)
  - Via FieldWorks.sln top-level build
- **Test approach**: Unit tests with in-memory LCModel cache, XML-based parser transform tests

### Referenced By

- [Test Strategy](../../../openspec/specs/architecture/testing/test-strategy.md#strategy) — Parser test coverage
- [Fixtures](../../../openspec/specs/architecture/testing/fixtures.md#fixture-patterns) — Parser XML fixtures

## Usage Hints
Use ParserScheduler for all operations. Set ActiveParser ("HC" or "XAmple"). ParserModelChangeListener handles automatic reload. Use TraceWord methods for debugging.

## Related Folders
- **ParserUI/**: Parser UI dialogs
- **Interlinear/**: Text analysis
- **Morphology/**: Morphology editor

## References
34 C# files, 4 C++ files (XAmpleCOMWrapper). Key: ParserScheduler, ParserWorker, ParseFiler, HCParser, XAmpleParser. See `.cache/copilot/diff-plan.json` for file listings.
  - LexText/ParserCore/ParserCoreTests/M3ToXAmpleTransformerTestsDataFiles/TestAffixAllomorphFeatsParserFxtResult.xml
  - LexText/ParserCore/ParserCoreTests/M3ToXAmpleTransformerTestsDataFiles/emi-flexFxtResult.xml

## Test Information
- Test projects: ParserCoreTests (18 test files, ~2.7K lines), XAmpleManagedWrapperTests
- Run: `dotnet test` or Test Explorer in Visual Studio
- Test coverage: HCLoaderTests, M3ToXAmpleTransformerTests (18 XML test data files in M3ToXAmpleTransformerTestsDataFiles/), ParseFilerProcessingTests, ParseWorkerTests, ParserReportTests, XAmpleParserTests
- Test data: Abaza-OrderclassPlay.xml, CliticEnvsParserFxtResult.xml, ConceptualIntroTestParserFxtResult.xml, IrregularlyInflectedFormsParserFxtResult.xml, QuechuaMYLFxtResult.xml, emi-flexFxtResult.xml, etc.
