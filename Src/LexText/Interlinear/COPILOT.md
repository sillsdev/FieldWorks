---
last-reviewed: 2025-10-31
last-reviewed-tree: ee01db01870be87f00099a688e138fa3962fb595e5c981c42fa6412e24b9acd5
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

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
C# .NET Framework 4.8.x, Windows Forms, LCModel, Views rendering engine, XCore framework.

## Dependencies
Consumes: LCModel (IText/ISegment/IAnalysis), Views, XCore, SimpleRootSite, Common utilities. Used by: xWorks interlinear window, Discourse (InterlinDocChart).

## Interop & Contracts
IText/IStText/ISegment/IAnalysis for text data model. InterlinDocRootSiteBase base class. IInterlinConfigurable for configuration.

## Threading & Performance
UI thread operations. Lazy loading for segments/analyses. Views engine caching. Large texts may have performance challenges.

## Config & Feature Flags
AddWordsToLexicon mode (glossing vs browsing), interlinear line configuration (baseline/morphemes/glosses/categories/translation), DoSpellCheck flag.

## Build Information
ITextDll.csproj (net48, Library). Test project: ITextDllTests/. Output: SIL.FieldWorks.IText.dll.

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
InterlinDocForAnalysis loaded by xWorks interlinear text window for text analysis views.

## Test Index
ITextDllTests/ covers interlinear logic, concordance, BIRD import, analysis handling.

## Usage Hints
Access via FLEx Texts & Words → Analyze tab. Click words for Sandbox glossing, use concordance for searches, configure interlinear lines via Tools menu, import BIRD XML, tag text portions, print/export with layout tools. Massive 49.6K line library with comprehensive subsystems (Sandbox, Concordance, ComplexConc patterns, BIRD import, Tagging, Print layout).

## Related Folders
Discourse/ (InterlinDocChart), LexTextControls/, LexTextDll/, xWorks/.

## References
ITextDll.csproj (net48). Key files: InterlinDocRootSiteBase.cs (3.3K), InterlinDocForAnalysis.cs (2.8K), ConcordanceControl.cs (1.9K), BIRDInterlinearImporter.cs (1.8K), 100+ files total (49.6K lines). See `.cache/copilot/diff-plan.json` for complete file listing.
