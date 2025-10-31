---
last-reviewed: 2025-10-31
last-verified-commit: b2590c1
status: reviewed
---

# ParserUI

## Purpose
Parser configuration and testing UI components. Provides TryAWordDlg for interactive single-word parsing with trace visualization, ParserReportsDialog for viewing parse batch results and statistics, ImportWordSetDlg for bulk wordlist import, ParserParametersDlg for parser configuration, and XAmpleWordGrammarDebugger for grammar file debugging. Enables linguists to refine and validate morphological descriptions by testing parser behavior, viewing parse traces (HC XML or XAmple SGML), managing parser settings, and debugging morphological analyses.

## Architecture
C# library (net462) with 28 source files (~5.9K lines). Mix of WinForms (TryAWordDlg, ImportWordSetDlg, ParserParametersDlg) and WPF/XAML (ParserReportsDialog, ParserReportDialog) with MVVM view models. Integrates Gecko WebBrowser control for HTML trace display via GeneratedHtmlViewer.

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

## Dependencies
- **External**: XCore (Mediator, PropertyTable, IxCoreColleague, message handling), LexText/ParserCore (ParserScheduler, ParserWorker, ParseFiler, IParser, ParserReport, ParseResult), Common/RootSites (SimpleRootSite, TryAWordRootSite base), Common/Widgets (FwTextBox), Common/FwUtils (PersistenceProvider, FlexHelpProvider), XWorks (GeneratedHtmlViewer, WebPageInteractor for Gecko), LCModel (LcmCache, IWfiWordform, IWfiAnalysis), Gecko (GeckoWebBrowser for HTML trace display), System.Windows.Forms (WinForms controls), System.Windows (WPF/XAML for reports dialogs)
- **Internal (upstream)**: ParserCore (all parser operations), RootSite (Views rendering), XCore (colleague pattern integration)
- **Consumed by**: LexText/LexTextDll (via XCore listeners - ImportWordSetListener, ParserListener, ParserParametersListener invoked from menu/toolbar commands)

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ParserUI.csproj` or `dotnet build` (from FW.sln)
- Output: ParserUI.dll
- Dependencies: Gecko WebBrowser (via NuGet), XCore, ParserCore, RootSites, FwUtils, LCModel
- UI technologies: WinForms (dialogs, controls), WPF/XAML (reports dialogs with MVVM), Gecko WebBrowser (HTML trace display)

## Test Information
- Test project: ParserUITests (if present)
- Manual testing: Launch TryAWordDlg via Tools→Parser→Try A Word menu in FLEx, enter wordform, click "Try It", verify parse results display and trace HTML renders correctly in Gecko browser
- Test scenarios: Parse valid word (expect analyses), parse invalid word (expect errors), trace enabled (expect HTML trace), select morphs to trace (expect filtered trace), import word set (expect wordforms created), view parser reports (expect statistics)

## Related Folders
- **LexText/ParserCore/**: Parser engine consumed by all UI components via ParserConnection/ParserScheduler
- **LexText/LexTextDll/**: Application host, XCore listeners integration point (ImportWordSetListener, ParserListener registered in LexTextDll)
- **LexText/Interlinear/**: May invoke parser for text analysis (uses ParseFiler from ParserCore)
- **Common/RootSites/**: Base classes for TryAWordRootSite Views rendering
- **XWorks/**: GeneratedHtmlViewer, WebPageInteractor for Gecko HTML display

## References
- **Source files**: 28 C# files (~5.9K lines), 3 XAML files (ParserReportDialog.xaml, ParserReportsDialog.xaml), 3 .resx resource files
- **Project file**: ParserUI.csproj
- **Key dialogs**: TryAWordDlg (WinForms), ParserReportsDialog (WPF), ImportWordSetDlg (WinForms), ParserParametersDlg (WinForms)
- **Key listeners**: ParserListener (main coordinator), ImportWordSetListener, ParserParametersListener (XCore colleagues)
- **Key interfaces**: IParserTrace (HCTrace, XAmpleTrace implementations)
- **View models**: ParserReportViewModel, ParserReportsViewModel (WPF MVVM pattern)
- **Converters**: FileTimeToDateTimeConverter, MillisecondsToTimeSpanConverter, PositiveIntToRedBrushConverter (WPF value converters)
- **Target framework**: net462
