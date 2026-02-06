---
last-reviewed: 2025-11-21
last-reviewed-tree: ca3afb4763cf119a739ed99e322e35066e1732903f4ea048c315d27aa86c62ff
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - try-a-word-dialog
  - parser-reports
  - import-word-set
  - parser-configuration
  - parser-coordination
  - trace-display
  - debugging-tools
  - referenced-by
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
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

# ParserUI

## Purpose
Parser configuration and testing UI components. Provides TryAWordDlg for interactive single-word parsing with trace visualization, ParserReportsDialog for viewing parse batch results and statistics, ImportWordSetDlg for bulk wordlist import, ParserParametersDlg for parser configuration, and XAmpleWordGrammarDebugger for grammar file debugging. Enables linguists to refine and validate morphological descriptions by testing parser behavior, viewing parse traces (HC XML or XAmple SGML), managing parser settings, and debugging morphological analyses.

## Architecture
C# library (net48) with 28 source files (~5.9K lines). Mix of WinForms (TryAWordDlg, ImportWordSetDlg, ParserParametersDlg) and WPF/XAML (ParserReportsDialog, ParserReportDialog) with MVVM view models. Integrates Gecko WebBrowser control for HTML trace display via GeneratedHtmlViewer.

## Key Components

### Try A Word Dialog
- **TryAWordDlg**: Main parser testing dialog. Allows entering a wordform, invoking parser via ParserListener→ParserConnection→ParserScheduler, displaying analyses in TryAWordSandbox, and showing trace in HTML viewer (Gecko WebBrowser). Supports "Trace parse" checkbox and "Select morphs to trace" for granular HC trace control. Persists state via PersistenceProvider. Implements IMediatorProvider, IPropertyTableProvider for XCore integration.
  - Inputs: LcmCache, Mediator, PropertyTable, word string, ParserListener
  - UI Controls: FwTextBox (wordform input), TryAWordSandbox (analysis display), HtmlControl (Gecko trace viewer), CheckBox (trace options), Timer (async status updates)
  - Methods: SetDlgInfo(), TryItHandler(), OnParse(), DisplayTrace()
- **TryAWordSandbox**: Sandbox control for displaying parse results within TryAWordDlg. Extends InterlinLineChoices for analysis display. Uses TryAWordRootSite for Views rendering.
- **TryAWordRootSite**: Root site for Views-based analysis display in sandbox. Extends SimpleRootSite.

### Parser Reports
- **ParserReportsDialog**: WPF dialog showing list of parser batch reports. Uses ObservableCollection<ParserReportViewModel> data binding. Allows viewing individual report details via ParserReportDialog, deleting reports.
  - UI: XAML with ListBox, DataGrid, buttons for View/Delete/Close
  - ViewModel: ParserReportsViewModel (manages collection of reports)
- **ParserReportDialog**: WPF dialog showing details of single ParserReport. Displays statistics (words parsed, time taken, errors), comment editing.
  - UI: XAML with TextBox, DataGrid
  - ViewModel: ParserReportViewModel (wraps ParserReport from ParserCore)
- **ParserReportViewModel**: View model for single ParserReport. Exposes Date, Name, Comment, TimeToParseAllWordforms, TimeToLoadGrammar, NumberOfWordformsParsed, etc. Implements INotifyPropertyChanged.
  - Converters: FileTimeToDateTimeConverter, MillisecondsToTimeSpanConverter, PositiveIntToRedBrushConverter (for highlighting errors)

### Import Word Set
- **ImportWordSetDlg**: Dialog for importing wordlists for bulk parsing. Uses WordImporter to load words from file, creates IWfiWordform objects. Integrated with ParserListener for parsing after import.
  - Inputs: PropertyTable, Mediator
  - Methods: ImportWordSet(), HandleImport()
- **ImportWordSetListener**: XCore colleague for triggering ImportWordSetDlg from menu/toolbar (OnImportWordSet message handler)
- **WordImporter**: Utility class for reading wordlists from text files, creating IWfiWordform objects in LcmCache

### Parser Configuration
- **ParserParametersDlg**: Dialog for configuring parser settings (parameters vary by active parser - HC or XAmple)
  - Base class: ParserParametersBase
- **ParserParametersListener**: XCore colleague for triggering ParserParametersDlg (OnParserParameters message handler)

### Parser Coordination
- **ParserListener**: XCore colleague coordinating parser operations. Implements IxCoreColleague, IVwNotifyChange for data change notifications. Manages ParserConnection (wraps ParserScheduler), TryAWordDlg state, ParserReportsDialog lifecycle. Tracks m_checkParserResults for validation, manages bulk parsing via ParseTextWordforms/ParseWordforms.
  - Properties: ParserConnection (ParserScheduler wrapper), TryAWordDlg reference, ParserReportsDialog reference
  - Message handlers: OnTryAWord, OnParserParameters, OnParserReports, OnBulkParseWordforms, OnRefreshParser, OnCheckParser
  - Events: Handles TaskUpdateEventArgs from ParserScheduler
- **ParserConnection**: Wrapper around ParserScheduler from ParserCore. Provides convenient access to parser operations, manages ParserScheduler lifecycle. Implements IDisposable.
  - Properties: ParserScheduler reference
  - Methods: TryAWord(), ScheduleWordformsForUpdate(), Refresh()

### Trace Display
- **IParserTrace** (interface): Abstract trace viewer interface
  - Methods: DisplayTrace(string htmlFilename, string title)
- **HCTrace** (IParserTrace): HermitCrab trace display. Transforms HC XML trace to HTML via ParserTraceUITransform XSLT, displays in WebBrowser (Gecko via WebPageInteractor).
- **XAmpleTrace** (IParserTrace): XAmple trace display. Converts SGML trace to HTML (basic formatting), displays in WebBrowser.
- **ParserTraceUITransform**: XSLT stylesheet wrapper for transforming HC XML trace to readable HTML
- **WebPageInteractor**: Bridge between C# and Gecko WebBrowser for HTML display, JavaScript interaction (used by GeneratedHtmlViewer)

### Debugging Tools
- **XAmpleWordGrammarDebugger**: Tool for debugging XAmple grammar files (ana, dictOrtho, dictPrefix files). Displays grammar contents for manual inspection.

### Referenced By

- [Parser Configuration](../../../openspec/specs/grammar/parsing/configuration.md#behavior) — Parser settings dialogs
- [Parser Troubleshooting](../../../openspec/specs/grammar/parsing/troubleshooting.md#behavior) — Try A Word and trace tools

## Technology Stack
- **Languages**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **UI frameworks**:
  - Windows Forms (TryAWordDlg, ImportWordSetDlg, ParserParametersDlg)
  - WPF/XAML (ParserReportsDialog, ParserReportDialog with MVVM pattern)
  - Gecko WebBrowser (GeckoFx NuGet) for HTML trace display
- **Key libraries**:
  - XCore (Mediator, PropertyTable, IxCoreColleague - colleague pattern)
  - ParserCore (ParserScheduler, ParseResult, ParserReport)
  - LCModel (LcmCache, IWfiWordform, IWfiAnalysis)
  - Common/RootSites (SimpleRootSite base for Views rendering)
  - Common/FwUtils (PersistenceProvider, FlexHelpProvider)
  - System.Windows.Forms, System.Windows (WinForms and WPF controls)
- **XSLT**: ParserTraceUITransform for HC trace XML to HTML conversion

## Dependencies
- **External**: XCore (Mediator, PropertyTable, IxCoreColleague, message handling), LexText/ParserCore (ParserScheduler, ParserWorker, ParseFiler, IParser, ParserReport, ParseResult), Common/RootSites (SimpleRootSite, TryAWordRootSite base), Common/Widgets (FwTextBox), Common/FwUtils (PersistenceProvider, FlexHelpProvider), XWorks (GeneratedHtmlViewer, WebPageInteractor for Gecko), LCModel (LcmCache, IWfiWordform, IWfiAnalysis), Gecko (GeckoWebBrowser for HTML trace display), System.Windows.Forms (WinForms controls), System.Windows (WPF/XAML for reports dialogs)
- **Internal (upstream)**: ParserCore (all parser operations), RootSite (Views rendering), XCore (colleague pattern integration)
- **Consumed by**: LexText/LexTextDll (via XCore listeners - ImportWordSetListener, ParserListener, ParserParametersListener invoked from menu/toolbar commands)

## Interop & Contracts
- **XCore colleague pattern**: ParserListener, ImportWordSetListener, ParserParametersListener implement IxCoreColleague
  - Message handlers: OnTryAWord, OnImportWordSet, OnParserParameters, OnBulkParseWordforms, OnParserReports
  - Registered in XCore Mediator for menu/toolbar command routing
- **HTML/JavaScript interop**: WebPageInteractor bridges C# to Gecko WebBrowser JavaScript context
  - Used by GeneratedHtmlViewer for HTML trace display
  - Gecko control hosts HTML rendered from HC XML trace via XSLT
- **Views interop**: TryAWordRootSite extends SimpleRootSite for Views-based sandbox rendering
  - Integrates with Views subsystem for morpheme breakdown display
- **Data contracts**:
  - IParserTrace interface: DisplayTrace(string htmlFilename, string title)
  - ParserReportViewModel: WPF MVVM view model for parser batch reports
  - Value converters: FileTimeToDateTimeConverter, MillisecondsToTimeSpanConverter, PositiveIntToRedBrushConverter (WPF bindings)
- **XAML UI contracts**: ParserReportDialog.xaml, ParserReportsDialog.xaml define WPF UI structure
- **Resource contracts**: .resx files for localized strings (ParserUIStrings.resx)

## Threading & Performance
UI thread required for all dialogs and controls. Background parsing via ParserScheduler with results marshaled via XCore IdleQueue.

## Config & Feature Flags
Trace options ("Trace parse" checkbox), parser selection (HC vs XAmple), parser-specific parameters (GuessRoots, MergeAnalyses for HC). State persisted via PersistenceProvider and XCore PropertyTable.

## Build Information
C# library (net48). Build via `msbuild ParserUI.csproj` from FieldWorks.sln. Output: ParserUI.dll.

## Interfaces and Data Models
- **IParserTrace**: Abstract trace viewer (implementations: HCTrace, XAmpleTrace)
- **View Models**: ParserReportViewModel, ParserReportsViewModel (WPF MVVM)
- **Value Converters**: FileTimeToDateTimeConverter, MillisecondsToTimeSpanConverter, PositiveIntToRedBrushConverter
- **XAML**: ParserReportDialog.xaml, ParserReportsDialog.xaml

## Entry Points
XCore message handlers via ParserListener (OnTryAWord, OnImportWordSet, OnParserParameters, OnParserReports). Dialogs accessed via Tools→Parser menu.

## Test Index
ParserUITests project with WordGrammarDebuggingTests.cs. Test data in ParserUITests/WordGrammarDebuggingInputsAndResults/.

## Usage Hints
Use Tools→Parser menu for Try A Word (interactive testing), Import Word Set (bulk), Parser Parameters (configuration), and View Reports. Enable "Trace parse" for debugging output.

## Related Folders
- **ParserCore/**: Parser engine
- **LexTextDll/**: XCore listeners integration
- **RootSites/**: Views rendering base classes

## References
28 C# files, 3 XAML files (ParserReportDialog.xaml, ParserReportsDialog.xaml). Key: TryAWordDlg, ParserListener, ParserReportsDialog. See `.cache/copilot/diff-plan.json` for file listings.
