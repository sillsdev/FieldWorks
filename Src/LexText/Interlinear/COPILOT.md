---
last-reviewed: 2025-10-31
last-reviewed-tree: 2418c5ec78dacbf805d1e7269d8997de3795a0d63ee38eb26939fb716035ae45
status: draft
---

# Interlinear (ITextDll) COPILOT summary

## Purpose
Comprehensive interlinear text analysis library providing core functionality for glossing, analyzing, and annotating texts word-by-word. Supports interlinear display (baseline text, morphemes, glosses, word categories, free translation), text analysis workflows, concordance search, complex concordance patterns, BIRD format import/export, and text configuration. Central to FLEx text analysis features. Massive 49.6K line library with multiple specialized subsystems: InterlinDocForAnalysis (main analysis UI), Sandbox (word-level editing), ConcordanceControl (search), ComplexConc* (pattern matching), TextTaggingView (tagging), TreebarControl (navigation), PrintLayout (export). Namespace: SIL.FieldWorks.IText (project name ITextDll).

## Architecture
C# library (net48, OutputType=Library) with modular subsystem design. InterlinDocRootSiteBase abstract base for interlinear views. InterlinDocForAnalysis main analysis UI (extends InterlinDocRootSiteBase). Sandbox component for word-level glossing/analysis. ConcordanceControl/ConcordanceWordList for text search. ComplexConc* classes for advanced pattern concordance. TextTaggingView for text tagging/annotation. TreebarControl for text/paragraph navigation. InterlinPrintChild/PrintLayoutView for export. InterlinVc view constructors for rendering. Heavily integrated with LCModel (segments, analyses, glosses), Views rendering, XCore framework.

## Key Components
- **InterlinDocForAnalysis** (InterlinDocForAnalysis.cs, 2.8K lines): Main interlinear analysis UI
  - Extends InterlinDocRootSiteBase
  - Handles word analysis, glossing workflow
  - Right-click context menus (spelling, note delete)
  - AddWordsToLexicon mode (glossing vs browsing)
  - DoSpellCheck integration
- **InterlinDocRootSiteBase** (InterlinDocRootSiteBase.cs, 3.3K lines): Abstract base for interlinear views
  - Extends SimpleRootSite
  - Common interlinear view infrastructure
  - Selection handling, rendering coordination
- **Sandbox** (ITextDll likely has Sandbox*.cs files): Word-level editing component
  - Edit morphemes, glosses, word categories inline
  - Analysis approval/disapproval
- **ConcordanceControl** (ConcordanceControl.cs, 1.9K lines): Concordance search UI
  - Search text occurrences with context
  - Filter by word, morpheme, gloss
  - Export results (ConcordanceResultsExporter)
- **ConcordanceWordList** (ConcordanceWordList.cs, likely hundreds of lines): Word concordance list
  - List occurrences with sorting
- **ComplexConc* classes** (multiple files, 10K+ lines combined): Advanced pattern concordance
  - ComplexConcControl (ComplexConcControl.cs, 770 lines): Pattern editor UI
  - ComplexConcPatternVc (ComplexConcPatternVc.cs, 699 lines): Pattern rendering
  - ComplexConcParagraphData (ComplexConcParagraphData.cs, 386 lines): Search data
  - ComplexConcPatternModel (ComplexConcPatternModel.cs, 265 lines): Pattern model
  - ComplexConcWordDlg, ComplexConcMorphDlg, ComplexConcTagDlg: Pattern element dialogs
  - Node classes: ComplexConcPatternNode, ComplexConcGroupNode, ComplexConcLeafNode, ComplexConcOrNode, ComplexConcMorphNode, ComplexConcWordNode, ComplexConcTagNode, ComplexConcWordBdryNode
  - Complex search patterns (word sequences, morpheme patterns, feature matching)
- **TextTaggingView** (TextTaggingView.cs, likely 1K+ lines): Text tagging/annotation
  - Tag portions of text with categories
  - Tagging UI and display
- **TreebarControl** (TreebarControl*.cs): Text/paragraph navigation
  - Tree view for navigating text structure
  - Paragraph/segment selection
- **PrintLayout** (InterlinPrintChild.cs, PrintLayoutView*.cs, 5K+ lines combined): Export/print
  - Layout for printing/exporting interlinear texts
  - Page breaks, formatting
- **InterlinVc** (InterlinVc*.cs, likely 3K+ lines): View constructors
  - Render interlinear lines (baseline, morpheme, gloss, category, etc.)
  - Styling, column layout
- **BIRDInterlinearImporter** (BIRDInterlinearImporter.cs, 1.8K lines): BIRD format import
  - Import interlinear texts from BIRD XML format
  - Parse analyzed texts, create LCM objects
- **ChooseAnalysisHandler** (ChooseAnalysisHandler.cs, 747 lines): Analysis selection
  - Choose among multiple analyses for words
  - Approval/disapproval logic
- **ConfigureInterlinDialog** (ConfigureInterlinDialog.cs, likely 500+ lines): Interlinear configuration
  - Configure which interlinear lines to display
  - Line ordering, writing systems
- **ChooseTextWritingSystemDlg** (ChooseTextWritingSystemDlg.cs, 74 lines): Writing system chooser
  - Select writing systems for text input

## Technology Stack
- C# .NET Framework 4.8.x (net8)
- OutputType: Library
- Windows Forms (custom controls, dialogs)
- LCModel (data model)
- Views (rendering engine)
- XCore (application framework)

## Dependencies

### Upstream (consumes)
- **LCModel**: Data model (IText, IStText, ISegment, IAnalysis, IWfiGloss, IWfiAnalysis, IWfiWordform)
- **Views**: Rendering engine (IVwRootSite, IVwViewConstructor)
- **XCore**: Application framework (Mediator, IxCoreColleague)
- **Common/RootSites**: SimpleRootSite base
- **Common/ViewsInterfaces**: COM views interfaces
- **Common/FwUtils**: Utilities
- **FwCoreDlgControls**: Dialog controls

### Downstream (consumed by)
- **xWorks**: Interlinear text window
- **LexTextExe**: FLEx application
- **Discourse**: Constituent charts (inherits InterlinDocChart)
- **Linguists**: Text analysis workflows

## Interop & Contracts
- **IText**: LCModel text object (paragraphs, segments)
- **IStText**: Structured text (paragraphs)
- **ISegment**: Text segment (analyses)
- **IAnalysis**: Word analysis (WfiWordform/WfiAnalysis/WfiGloss/PunctuationForm)
- **IWfiWordform**: Word form
- **IWfiAnalysis**: Morphological analysis
- **IWfiGloss**: Word gloss
- **InterlinDocRootSiteBase**: Base class for interlinear views
- **IInterlinConfigurable**: Configuration interface
- **IxCoreColleague**: XCore colleague pattern

## Threading & Performance
- **UI thread**: All operations on UI thread
- **Lazy loading**: Segments/analyses loaded on demand
- **Rendering optimization**: Views engine caching
- **Large texts**: May have performance challenges with very long texts

## Config & Feature Flags
- **AddWordsToLexicon mode**: Glossing vs browsing (ksPropertyAddWordsToLexicon)
- **Interlinear line configuration**: Which lines to display (baseline, morphemes, glosses, categories, translation)
- **DoSpellCheck**: Spell checking enabled/disabled

## Build Information
- **Project file**: ITextDll.csproj (net48, OutputType=Library)
- **Test project**: ITextDllTests/
- **Output**: SIL.FieldWorks.IText.dll
- **Build**: Via top-level FieldWorks.sln or: `msbuild ITextDll.csproj`
- **Run tests**: `dotnet test ITextDllTests/`

## Interfaces and Data Models

- **InterlinDocForAnalysis** (InterlinDocForAnalysis.cs)
  - Purpose: Main interlinear analysis UI
  - Base: InterlinDocRootSiteBase
  - Key features: Word analysis, glossing, context menus, spell check
  - Notes: Partial class (Designer file exists)

- **InterlinDocRootSiteBase** (InterlinDocRootSiteBase.cs)
  - Purpose: Abstract base for interlinear views
  - Base: SimpleRootSite
  - Provides: Common infrastructure, selection handling
  - Notes: Subclassed by InterlinDocForAnalysis, InterlinDocChart (Discourse)

- **ConcordanceControl** (ConcordanceControl.cs)
  - Purpose: Concordance search UI
  - Inputs: Search terms, filters
  - Outputs: Concordance results with context
  - Notes: Export support via ConcordanceResultsExporter

- **ComplexConc* pattern matching**:
  - ComplexConcControl: Pattern editor UI
  - ComplexConcPatternVc: Pattern rendering
  - ComplexConcPatternModel: Pattern data model
  - Node classes: Represent pattern elements (words, morphemes, features, boundaries)
  - Advanced linguistic search patterns

- **Interlinear data model**:
  - IText: Text collection (paragraphs)
  - IStText: Structured text (Title, Contents paragraphs)
  - ISegment: Analyzed segment (collection of analyses)
  - IAnalysis: Word analysis (morphemes, gloss, category)
  - IWfiWordform: Wordform lexicon entry
  - IWfiAnalysis: Analysis with morphemes
  - IWfiGloss: Gloss with category, definition

## Entry Points
Loaded by xWorks interlinear text window. InterlinDocForAnalysis instantiated for text analysis views.

## Test Index
- **Test project**: ITextDllTests/
- **Run tests**: `dotnet test ITextDllTests/`
- **Coverage**: Interlinear logic, concordance, BIRD import, analysis handling

## Usage Hints
- **Open text**: In FLEx, Texts & Words → Analyze tab
- **Analyze words**: Click words to open Sandbox for glossing
- **Approve analyses**: Checkmark icon approves analysis
- **Concordance**: Search for word/morpheme occurrences across texts
- **Complex concordance**: Advanced pattern search (e.g., find sequences, morpheme features)
- **Configure lines**: Choose which interlinear lines to display (Tools → Configure)
- **BIRD import**: Import analyzed texts from BIRD XML format
- **Tagging**: Tag text portions for discourse/syntactic annotation
- **Print/Export**: Use print layout for formatted output
- **Navigation**: Use treebar to navigate paragraphs/segments
- **Large library**: 49.6K lines covering comprehensive interlinear functionality

## Related Folders
- **Discourse/**: Constituent charts (inherits InterlinDocChart)
- **LexTextControls/**: Shared controls
- **LexTextDll/**: Business logic
- **xWorks/**: Application shell

## References
- **Project file**: ITextDll.csproj (net48, OutputType=Library)
- **Key C# files**: InterlinDocRootSiteBase.cs (3.3K), InterlinDocForAnalysis.cs (2.8K), ConcordanceControl.cs (1.9K), BIRDInterlinearImporter.cs (1.8K), ComplexConcControl.cs (770), ChooseAnalysisHandler.cs (747), ComplexConcPatternVc.cs (699), and 100+ more files
- **Test project**: ITextDllTests/
- **Total lines of code**: 49644
- **Output**: SIL.FieldWorks.IText.dll
- **Namespace**: SIL.FieldWorks.IText
- **Subsystems**: Interlinear display, Sandbox editing, Concordance search, Complex concordance patterns, BIRD import, Text tagging, Print layout, Navigation