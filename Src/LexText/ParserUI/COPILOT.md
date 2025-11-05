---
last-reviewed: 2025-10-31
last-reviewed-tree: c17511bd9bdcdbda3ea395252447efac41d9c4b5ef7ad360afbd374ff008585b
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

## Technology Stack
- **Languages**: C#
- **Target framework**: .NET Framework 4.6.2 (net462)
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
- **UI thread affinity**: All UI components (dialogs, controls) must run on main UI thread
  - WinForms: TryAWordDlg, ImportWordSetDlg, ParserParametersDlg
  - WPF: ParserReportsDialog, ParserReportDialog (WPF Dispatcher thread)
- **Background parsing**: ParserConnection wraps ParserScheduler which runs parsing on background thread
  - Parser results marshaled back to UI thread via IdleQueue (XCore mechanism)
  - TryAWordDlg uses Timer to poll for parser completion (m_timer.Tick event)
- **Async event handling**: TaskUpdateEventArgs events from ParserScheduler delivered to ParserListener on UI thread
- **Gecko WebBrowser**: Gecko control initialization and rendering must be on UI thread
  - HTML trace generation (XSLT transform) can be off-thread, display must be on UI
- **Performance considerations**:
  - Trace generation: XSLT transform for HC XML trace can be expensive for complex parses
  - HTML rendering: Gecko WebBrowser load time for large trace HTML
  - Sandbox rendering: Views-based TryAWordSandbox can be slow for many analyses
- **No manual threading**: No explicit Thread/Task creation; relies on ParserScheduler background worker and UI thread marshaling

## Config & Feature Flags
- **Trace options** (TryAWordDlg):
  - "Trace parse" checkbox: Enable/disable trace generation
  - "Select morphs to trace" button: Filter HC trace to specific morphemes (granular debugging)
- **Parser selection**: Active parser (HC vs XAmple) determined by MorphologicalDataOA.ActiveParser setting (from ParserCore)
  - UI adapts based on active parser (HC shows XML trace, XAmple shows SGML trace)
- **Parser parameters**: ParserParametersDlg exposes parser-specific settings
  - HC: GuessRoots, MergeAnalyses, MaxCompoundRules (via HCMaxCompoundRulesDlg)
  - XAmple: Legacy AmpleOptions (TraceOff, TraceMorphs, etc.)
- **Persistence**: PersistenceProvider saves/restores TryAWordDlg state (window position, size, last word entered)
- **PropertyTable**: XCore PropertyTable used for persisting parser settings and UI preferences
- **Report retention**: ParserReportsDialog allows deleting old batch reports (stored in LCModel as ParserReport objects)

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild ParserUI.csproj` or `dotnet build` (from FieldWorks.sln)
- Output: ParserUI.dll
- Dependencies: Gecko WebBrowser (via NuGet), XCore, ParserCore, RootSites, FwUtils, LCModel
- UI technologies: WinForms (dialogs, controls), WPF/XAML (reports dialogs with MVVM), Gecko WebBrowser (HTML trace display)

## Interfaces and Data Models

### Interfaces
- **IParserTrace** (path: Src/LexText/ParserUI/IParserTrace.cs)
  - Purpose: Abstract trace viewer for displaying parser diagnostic output
  - Inputs: string htmlFilename (path to trace HTML), string title (dialog title)
  - Outputs: None (side effect: displays trace in dialog)
  - Implementations: HCTrace (HermitCrab), XAmpleTrace (legacy)
  - Notes: Trace format varies by parser; HC uses XML→HTML XSLT, XAmple uses SGML→HTML

### Data Models (View Models)
- **ParserReportViewModel** (path: Src/LexText/ParserUI/ParserReportViewModel.cs)
  - Purpose: WPF MVVM view model wrapping ParserReport from ParserCore
  - Shape: Date, Name, Comment (editable), TimeToParseAllWordforms, TimeToLoadGrammar, NumberOfWordformsParsed, errors/stats
  - Consumers: ParserReportDialog.xaml data binding
  - Notes: Implements INotifyPropertyChanged for two-way binding

- **ParserReportsViewModel** (path: Src/LexText/ParserUI/ParserReportsViewModel.cs)
  - Purpose: WPF MVVM view model for collection of parser reports
  - Shape: ObservableCollection<ParserReportViewModel>, DeleteReportCommand
  - Consumers: ParserReportsDialog.xaml ListBox/DataGrid binding
  - Notes: Manages report list lifecycle, delete operations

### Value Converters (WPF Binding)
- **FileTimeToDateTimeConverter** (path: Src/LexText/ParserUI/FileTimeToDateTimeConverter.cs)
  - Purpose: Convert file time (long) to DateTime for XAML display
  - Inputs: long (file time ticks)
  - Outputs: DateTime or formatted string

- **MillisecondsToTimeSpanConverter** (path: Src/LexText/ParserUI/MillisecondsToTimeSpanConverter.cs)
  - Purpose: Convert milliseconds (int) to TimeSpan for readable duration display
  - Inputs: int (milliseconds)
  - Outputs: TimeSpan or formatted string

- **PositiveIntToRedBrushConverter** (path: Src/LexText/ParserUI/PositiveIntToRedBrushConverter.cs)
  - Purpose: Highlight error counts in red when > 0
  - Inputs: int (error count)
  - Outputs: Brush (Red if > 0, Black if 0)

### XAML UI Contracts
- **ParserReportDialog.xaml** (path: Src/LexText/ParserUI/ParserReportDialog.xaml)
  - Purpose: WPF dialog for single parser report details
  - Shape: TextBox (comment), DataGrid (statistics), buttons
  - Consumers: Instantiated by ParserReportsDialog when viewing report details

- **ParserReportsDialog.xaml** (path: Src/LexText/ParserUI/ParserReportsDialog.xaml)
  - Purpose: WPF dialog for list of parser batch reports
  - Shape: ListBox (reports list), buttons (View/Delete/Close)
  - Consumers: Invoked by ParserListener.OnParserReports message handler

## Entry Points
- **XCore message handlers** (ParserListener):
  - OnTryAWord: Tools→Parser→Try A Word menu → opens TryAWordDlg
  - OnImportWordSet: Tools→Parser→Import Word Set menu → opens ImportWordSetDlg (via ImportWordSetListener)
  - OnParserParameters: Tools→Parser→Parser Parameters menu → opens ParserParametersDlg (via ParserParametersListener)
  - OnParserReports: Tools→Parser→View Reports menu → opens ParserReportsDialog
  - OnBulkParseWordforms: Bulk parse command → schedules batch parsing via ParserConnection
  - OnRefreshParser: Forces parser grammar/lexicon reload
  - OnCheckParser: Validates parser state
- **Dialog instantiation**:
  - TryAWordDlg: Created by ParserListener, managed lifecycle (singleton-like within session)
  - ParserReportsDialog: Created on demand by ParserListener.OnParserReports
  - ImportWordSetDlg: Created by ImportWordSetListener.OnImportWordSet
  - ParserParametersDlg: Created by ParserParametersListener.OnParserParameters
- **Programmatic access**: ParserConnection wraps ParserScheduler for non-UI consumers
  - TryAWord(string word): One-off word parse
  - ScheduleWordformsForUpdate(): Bulk wordform parsing
- **Colleague registration**: Listeners registered in XCore Mediator (typically in LexTextDll initialization)

## Test Index
- **Test project**: ParserUITests/ParserUITests.csproj
- **Key test file**: WordGrammarDebuggingTests.cs
  - Tests XAmpleWordGrammarDebugger functionality
  - Verifies grammar file debugging and XSLT transforms
- **Test data**: ParserUITests/WordGrammarDebuggingInputsAndResults/
  - 14 XML files for grammar debugging scenarios (EmptyWord.xml, M3FXTDump.xml, bilikeszi*.xml, bili*BadInflection.xml)
  - 3 XSLT files for grammar transform testing (TestUnificationViaXSLT.xsl, RequiredOptionalPrefixSlotsWordGrammarDebugger.xsl, TLPSameSlotTwiceWordGrammarDebugger.xsl)
- **Test approach**:
  - Unit tests for grammar debugging logic
  - XML-based test scenarios for XSLT transforms
  - Mock/fake LcmCache for isolated testing
- **Manual testing scenarios** (from COPILOT.md):
  - Launch TryAWordDlg via Tools→Parser→Try A Word
  - Enter wordform, click "Try It", verify analyses display
  - Enable "Trace parse", verify HTML trace renders in Gecko browser
  - Import word set, verify wordforms created in database
  - View parser reports, verify statistics display correctly
- **Test runners**:
  - Visual Studio Test Explorer
  - Via FieldWorks.sln top-level build

## Usage Hints
- **Interactive word testing**:
  1. Open Tools→Parser→Try A Word (invokes OnTryAWord message handler)
  2. Enter word in FwTextBox, click "Try It" button
  3. View analyses in TryAWordSandbox (morpheme breakdown via Views rendering)
  4. Enable "Trace parse" to see diagnostic output in Gecko HTML viewer
  5. Use "Select morphs to trace" for focused HC trace debugging
- **Bulk word import**:
  1. Tools→Parser→Import Word Set (via ImportWordSetListener)
  2. Select wordlist file (text file, one word per line)
  3. WordImporter creates IWfiWordform objects in database
  4. Optionally schedule bulk parsing after import
- **Parser configuration**:
  - Tools→Parser→Parser Parameters (via ParserParametersListener)
  - HC: Set GuessRoots, MergeAnalyses, MaxCompoundRules (HCMaxCompoundRulesDlg)
  - XAmple: Legacy AmpleOptions configuration
- **Viewing batch reports**:
  - Tools→Parser→View Reports (OnParserReports)
  - ParserReportsDialog shows list of historical batch runs
  - Double-click report to view details (ParserReportDialog)
  - Delete old reports to clean up database
- **Trace debugging tips**:
  - HC XML trace: XSLT-transformed to HTML, shows rule application, feature unification, phonological processes
  - XAmple SGML trace: Legacy format, basic HTML rendering
  - Save trace HTML via Gecko context menu for offline analysis
- **Extension points**:
  - Implement IParserTrace for new trace format viewers
  - Extend ParserListener for custom parser integration scenarios
  - Add XCore message handlers for new parser commands
- **Common pitfalls**:
  - Gecko WebBrowser requires proper initialization; ensure Gecko binaries are deployed
  - TryAWordDlg persists state via PersistenceProvider; clear settings if behavior unexpected
  - Parser must be loaded before Try A Word works; grammar reload can take seconds
  - WPF dialogs (reports) use different threading model than WinForms dialogs; don't mix dispatcher contexts

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

## Auto-Generated Project and File References
- Project files:
  - LexText/ParserUI/ParserUI.csproj
  - LexText/ParserUI/ParserUITests/ParserUITests.csproj
- Key C# files:
  - LexText/ParserUI/AssemblyInfo.cs
  - LexText/ParserUI/FileTimeToDateTimeConverter.cs
  - LexText/ParserUI/HCMaxCompoundRulesDlg.Designer.cs
  - LexText/ParserUI/HCMaxCompoundRulesDlg.cs
  - LexText/ParserUI/HCTrace.cs
  - LexText/ParserUI/IParserTrace.cs
  - LexText/ParserUI/ImportWordSetDlg.cs
  - LexText/ParserUI/ImportWordSetListener.cs
  - LexText/ParserUI/MillisecondsToTimeSpanConverter.cs
  - LexText/ParserUI/ParserConnection.cs
  - LexText/ParserUI/ParserListener.cs
  - LexText/ParserUI/ParserParametersBase.cs
  - LexText/ParserUI/ParserParametersDlg.cs
  - LexText/ParserUI/ParserReportDialog.xaml.cs
  - LexText/ParserUI/ParserReportViewModel.cs
  - LexText/ParserUI/ParserReportsDialog.xaml.cs
  - LexText/ParserUI/ParserReportsViewModel.cs
  - LexText/ParserUI/ParserTraceUITransform.cs
  - LexText/ParserUI/ParserUIStrings.Designer.cs
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingTests.cs
  - LexText/ParserUI/PositiveIntToRedBrushConverter.cs
  - LexText/ParserUI/TryAWordDlg.cs
  - LexText/ParserUI/TryAWordRootSite.cs
  - LexText/ParserUI/TryAWordSandbox.cs
  - LexText/ParserUI/WebPageInteractor.cs
- Data contracts/transforms:
  - LexText/ParserUI/HCMaxCompoundRulesDlg.resx
  - LexText/ParserUI/ImportWordSetDlg.resx
  - LexText/ParserUI/ParserParametersDlg.resx
  - LexText/ParserUI/ParserReportDialog.xaml
  - LexText/ParserUI/ParserReportsDialog.xaml
  - LexText/ParserUI/ParserUIStrings.resx
  - LexText/ParserUI/ParserUITests/TestUnificationViaXSLT.xsl
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/EmptyWord.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDump.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDumpAffixAlloFeats.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDumpNoCompoundRules.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDumpStemNames.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTRequiredOptionalPrefixSlots.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/RequiredOptionalPrefixSlotsWordGrammarDebugger.xsl
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/TLPSameSlotTwiceWordGrammarDebugger.xsl
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/TestFeatureStructureUnification.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/biliStep00BadInflection.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/biliStep01BadInflection.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/biliStep02BadInflection.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep00.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep01.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep02.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep03.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep04.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep05.xml
## Test Information
- Test project: ParserUITests (if present)
- Manual testing: Launch TryAWordDlg via Tools→Parser→Try A Word menu in FLEx, enter wordform, click "Try It", verify parse results display and trace HTML renders correctly in Gecko browser
- Test scenarios: Parse valid word (expect analyses), parse invalid word (expect errors), trace enabled (expect HTML trace), select morphs to trace (expect filtered trace), import word set (expect wordforms created), view parser reports (expect statistics)
