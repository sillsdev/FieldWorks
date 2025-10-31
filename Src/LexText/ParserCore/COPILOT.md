---
last-reviewed: 2025-10-31
last-verified-commit: 833987a
status: reviewed
---

# ParserCore

## Purpose
Morphological parser infrastructure supporting both HermitCrab and XAmple parsing engines. Implements background parsing scheduler (ParserScheduler/ParserWorker) with priority queue, manages parse result filing (ParseFiler), provides parser model change detection (ParserModelChangeListener), and wraps both HC (HermitCrab via SIL.Machine) and XAmple (legacy C++ parser via COM/managed wrapper) engines. Enables computer-assisted morphological analysis in FLEx by decomposing words into morphemes based on linguistic rules, phonology, morphotactics, and allomorphy defined in the morphology editor.

## Architecture
C# library (net462) with 34 source files (~9K lines total). Contains 3 subprojects: ParserCore (main library), XAmpleManagedWrapper (C# wrapper for XAmple DLL), XAmpleCOMWrapper (C++/CLI COM wrapper for XAmple). Supports pluggable parser architecture via IParser interface (HCParser for HermitCrab, XAmpleParser for legacy XAmple).

## Key Components

### Parser Infrastructure
- **ParserScheduler**: Background thread scheduler with priority queue (5 priority levels: ReloadGrammarAndLexicon, TryAWord, High, Medium, Low). Manages ParserWorker thread, queues ParserWork items, tracks queue counts per priority. Supports TryAWordWork, UpdateWordformWork, BulkParseWork.
  - Inputs: LcmCache, PropertyTable (XCore configuration)
  - Outputs: ParserUpdateEventArgs events for task progress
  - Threading: Single background thread, synchronized queue access
- **ParserWorker**: Executes parse tasks on background thread. Creates active parser (HCParser or XAmpleParser based on MorphologicalDataOA.ActiveParser setting), instantiates ParseFiler for result filing, handles TryAWord() test parses and BulkUpdateOfWordforms() batch processing.
  - Inputs: LcmCache, taskUpdateHandler, IdleQueue, dataDir
  - Manages: IParser instance, ParseFiler, TaskReport progress
- **ParseFiler**: Files parse results to database (creates IWfiAnalysis objects). Manages parse agent (XAmple or HC), handles WordformUpdatedEventArgs, updates wordforms with new analyses, merges duplicate analyses if enabled.
  - Inputs: LcmCache, ICmAgent (parser agent), IdleQueue, TaskReport callback
  - Outputs: IWfiAnalysis objects in database

### Parser Implementations
- **HCParser** (IParser): HermitCrab parser implementation using SIL.Machine.Morphology.HermitCrab. Wraps Morpher and Language from SIL.Machine, manages FwXmlTraceManager for trace output, monitors ParserModelChangeListener for model changes. Supports GuessRoots, MergeAnalyses options. Maps HC Word/ShapeNode results to ParseResult/ParseAnalysis/ParseMorph.
  - Inputs: LcmCache
  - Methods: ParseWord(string), Update(), Reset(), IsUpToDate(), TraceWord(string, TextWriter), TraceWordXml(string, TextWriter)
  - Internal constants: CRuleID, FormID, MsaID, PRuleID, SlotID, TemplateID, IsNull, IsPrefix, Env, etc.
- **XAmpleParser** (IParser): Legacy XAmple parser implementation via XAmpleManagedWrapper COM interop. Converts LCModel data to XAmple format using M3ToXAmpleTransformer and XAmplePropertiesPreparer. Invokes native XAmple DLL via IXAmpleWrapper COM interface.
  - Inputs: LcmCache, dataDir
  - Methods: Same as HCParser (IParser interface)
- **IParser** (interface): Abstract parser contract
  - Methods: ParseWord(string word) → ParseResult, TraceWord(string word, TextWriter writer), TraceWordXml(string word, TextWriter writer), Update(), Reset(), IsUpToDate() → bool

### Model Change Detection
- **ParserModelChangeListener**: Monitors LCModel changes via PropChanged events. Tracks changes to phonological features, environments, natural classes, phonemes, morphemes, allomorphs, morphological rules, templates, adhoc prohibitions. Sets ModelChanged flag when morphology/phonology data changes requiring parser reload.
  - Inputs: LcmCache (subscribes to PropChanged events)
  - Outputs: ModelChanged property, Reset() method
  - Monitored classes: PhPhonemeSet, PhEnvironment, PhNaturalClass, PhPhoneme, MoMorphData, LexDb, MoInflAffixSlot, MoAffixAllomorph, MoStemAllomorph, MoForm, MoMorphType, MoAffixProcess, MoCompoundRule, MoInflAffMsa, MoAdhocProhib, etc.

### Data Structures
- **ParseResult**: Top-level parse result container
  - Properties: ParseAnalyses (list), ErrorMessages (list)
- **ParseAnalysis**: Single analysis for a wordform
  - Properties: ParseMorphs (list), Shape (surface form), ParseSuccess (bool)
- **ParseMorph**: Single morph in an analysis
  - Properties: Form (string), Msa (IMoMorphSynAnalysis reference), Morph (IMoForm reference), MsaPartId (Guid), MorphPartId (Guid)
- **TaskReport**: Progress tracking for parse tasks
  - Properties: TaskPhase (enum: NotStarted, CheckForChanges, LoadGrammar, ParseWordforms, FileResults, Error), PercentComplete, CurrentTasks, TotalTasks
  - Enum TaskPhase values
- **ParserPriority** (enum): Queue priority levels (ReloadGrammarAndLexicon=0, TryAWord=1, High=2, Medium=3, Low=4)
- **ParserUpdateEventArgs**: Event args carrying TaskReport

### XAmple Support (Legacy)
- **XAmpleManagedWrapper/XAmpleWrapper**: C# COM wrapper exposing IXAmpleWrapper interface to native XAmple DLL (XAmpleDLLWrapper P/Invoke calls)
- **XAmpleCOMWrapper**: C++/CLI COM component bridging managed code to native XAmple C++ library (XAmpleWrapperCore)
- **M3ToXAmpleTransformer**: Converts FXT XML export to XAmple grammar format (ANA, DICT files)
- **XAmplePropertiesPreparer**: Prepares XAmple configuration properties (file paths, trace settings)
- **AmpleOptions**: XAmple parser options (TraceOff, TraceMorphs, TraceAnalysis, etc.)

### Reporting and Diagnostics
- **ParserReport**: Report on overall parsing status
- **FwXmlTraceManager**: Manages XML trace output for HermitCrab parser diagnostics

## Dependencies
- **External**: SIL.Machine.Morphology.HermitCrab (HermitCrab parser engine), native XAmple DLL (legacy parser), SIL.LCModel (morphology data model), SIL.LCModel.Core (ILgWritingSystem, ITsString), SIL.ObjectModel (DisposableBase), XCore (PropertyTable, IdleQueue), System.Xml.Linq (XML processing)
- **Internal (upstream)**: None (this is a leaf component consumed by UI layers)
- **Consumed by**: LexText/ParserUI (parser UI and testing tools), LexText/Interlinear (automatic parsing of interlinear texts), LexText/Morphology (parser configuration and management)

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ParserCore.csproj` or `dotnet build` (from FW.sln)
- Output: ParserCore.dll, XAmpleManagedWrapper.dll, XAmpleCOMWrapper.dll (native C++/CLI)
- Dependencies: SIL.Machine.Morphology.HermitCrab NuGet package
- Subprojects:
  - ParserCore: Main C# library
  - XAmpleManagedWrapper: C# COM wrapper for XAmple (legacy)
  - XAmpleCOMWrapper: C++/CLI COM component for XAmple (legacy)

## Test Information
- Test projects: ParserCoreTests (18 test files, ~2.7K lines), XAmpleManagedWrapperTests
- Run: `dotnet test` or Test Explorer in Visual Studio
- Test coverage: HCLoaderTests, M3ToXAmpleTransformerTests (18 XML test data files in M3ToXAmpleTransformerTestsDataFiles/), ParseFilerProcessingTests, ParseWorkerTests, ParserReportTests, XAmpleParserTests
- Test data: Abaza-OrderclassPlay.xml, CliticEnvsParserFxtResult.xml, ConceptualIntroTestParserFxtResult.xml, IrregularlyInflectedFormsParserFxtResult.xml, QuechuaMYLFxtResult.xml, emi-flexFxtResult.xml, etc.

## Related Folders
- **LexText/ParserUI/**: Parser UI components (TryAWord dialog, parser configuration), consumes ParserScheduler
- **LexText/Interlinear/**: Automatic text analysis, consumes ParseFiler and ParserWorker
- **LexText/Morphology/**: Morphology editor defining rules consumed by parsers, triggers ParserModelChangeListener
- **LexText/Lexicon/**: Lexicon data (lexemes, allomorphs) consumed by parsers

## References
- **Source files**: 34 C# files (~9K lines), 4 C++ files (XAmpleCOMWrapper), 18 XML test data files
- **Project files**: ParserCore.csproj, ParserCoreTests/ParserCoreTests.csproj, XAmpleManagedWrapper/XAmpleManagedWrapper.csproj, XAmpleManagedWrapper/XAmpleManagedWrapperTests/XAmpleManagedWrapperTests.csproj, XAmpleCOMWrapper/XAmpleCOMWrapper.vcxproj
- **Key classes**: ParserScheduler, ParserWorker, ParseFiler, HCParser, XAmpleParser, ParserModelChangeListener, FwXmlTraceManager, M3ToXAmpleTransformer
- **Key interfaces**: IParser, IHCLoadErrorLogger, IXAmpleWrapper
- **Enums**: ParserPriority (5 levels), TaskPhase (6 states), AmpleOptions
- **Target framework**: net462
