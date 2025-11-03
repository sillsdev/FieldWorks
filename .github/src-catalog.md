# FieldWorks Src/ Folder Catalog

This catalog provides brief descriptions of each folder under `Src/`. For detailed information about any folder, see its `COPILOT.md` file.

Editorial note: See `.github/update-copilot-summaries.md` for how to update and maintain these per-folder summaries (including frontmatter expectations and review cadence).

## Folder Descriptions

### AppCore
Windows GDI wrapper classes and graphics utilities (SmartDc, FontWrap, BrushWrap, SmartPalette) that standardize rendering across native FieldWorks applications and provide writing-system aware styling helpers.

### CacheLight
Lightweight metadata and real-data caches that implement ISilDataAccess/IVwCacheDa, enabling import/export and testing scenarios without loading the full LCModel database layer.

### Cellar
FwXml parsing helpers built on Expat; supplies GUID parsing, element lookup, and run/property builders that turn FieldWorks formatted XML strings into rich-text structures consumed by native components.

### Common
Umbrella for the largest collection of shared infrastructure: reusable controls, application framework services, data filtering layers, scripture utilities, view hosts, and managed/native interface contracts used across every FieldWorks app.

### DbExtend
SQL Server extended stored procedure (`xp_IsMatch`) that adds FieldWorks-specific pattern matching to database queries.

### DebugProcs
Native debug infrastructure providing configurable assertion, warning, and report hooks to streamline troubleshooting during development.

### DocConvert
Legacy placeholder for document conversion utilities; currently only ships shared resources (icon) with active functionality maintained elsewhere.

### FXT
Template-driven FieldWorks Transform (FXT) engine delivering XML export/import via XDumper, XUpdater, filtering strategies, and command-line tooling.

### FdoUi
LCModel-focused UI layer with object-specific editors, bulk editors, dummy/test objects, and integration glue that exposes data model entities to FieldWorks applications.

### FwCoreDlgs
Extensive catalog of shared dialogs covering backup/restore, project setup, writing-system management, converters, find/replace, archiving, and other cross-application tasks.

### FwParatextLexiconPlugin
Paratext plug-in exposing FLEx lexicons via LexiconPlugin interfaces with project pickers, data wrappers, and UI so translators can consume FieldWorks lexical content inside Paratext.

### FwResources
Shared image and string libraries (FwStrings, FwTMStrings, HelpTopicPaths, toolbar assets) plus helper utilities that provide consistent branding and localization across the suite.

### GenerateHCConfig
Command-line tool that loads FLEx morphology/phonology data and exports HermitCrab XML configuration so the parser can run outside FieldWorks.

### Generic
Foundational native C++ helpers—COM smart pointers, collection templates, streams, string types, COM base classes—that everything from Kernel to views depend on.

### InstallValidator
Installer validation utility that compares expected file metadata (CSV-controlled) against an installed FieldWorks build and reports missing, outdated, or corrupt assets.

### Kernel
Defines the generated Cellar constants, property type enums, COM GUIDs, and proxy/stub plumbing that anchor the native side of the data model.

### LCMBrowser
Windows Forms diagnostic tool that lets developers inspect LCModel caches, navigate objects, view properties, and edit data for debugging and QA.

### LexText
Organizational root for the FLEx lexicon and text-analysis stack; see subfolders for lexicon UI, interlinear, discourse, morphology, parser integration, and publishing plugins.

### ManagedLgIcuCollator
Managed ILgCollatingEngine implementation that wraps ICU.NET to deliver locale-aware sorting for FieldWorks writing systems.

### ManagedVwDrawRootBuffered
Managed IVwDrawRootBuffered implementation that renders native Views content to an off-screen bitmap before blitting, eliminating flicker in rich text views.

### ManagedVwWindow
Minimal IVwWindow adapter that maps WinForms controls to native HWND expectations so the Views engine can render inside .NET windows.

### MigrateSqlDbs
Historical SQL→XML migration utility used for the FieldWorks 6→7 transition; kept for archival reference.

### Paratext8Plugin
Paratext 8 scripture provider that wraps the Paratext SDK so FLEx can synchronize back-translation content with Paratext projects.

### ParatextImport
USFM import pipeline with diffing, merge, and undo support that brings Paratext scripture data into FieldWorks while preserving existing content.

### ProjectUnpacker
Test-helper that unpacks embedded ZIP resources (Paratext/FLEx projects) and manages registry fixtures for automated tests.

### Transforms
Library of XSLT stylesheets (application and presentation) used for parser integration, morphology exports, GAFAWS, XLingPap, and other data transforms.

### UnicodeCharEditor
WinForms tool for managing Private Use Area characters—edits CustomChars.xml and installs ICU data so custom character properties propagate through FieldWorks.

### Utilities
Container for shared utilities (FixFwData, Reporting, Sfm tools, XML helpers, MessageBoxEx) that surface as command-line tools or shared libraries.

### XCore
Application framework that supplies the mediator/colleague pattern, inventory-driven UI composition, property propagation, and plugin plumbing for FieldWorks shells.

### views
Native Views engine delivering box-based layout, complex-script rendering, selections, and table/paragraph formatting for all FieldWorks text displays.

### xWorks
Main application shell (FwXApp/FwXWindow, RecordClerk/View hierarchy, configurable dictionary publishing, ToC/area switching) that hosts every FieldWorks work area.

## Common Subfolders

### Common/Controls
Shared UI controls and XML-driven view components (Design, DetailControls, FwControls, Widgets, XMLViews) that deliver consistent data-driven interfaces across FieldWorks.

### Common/FieldWorks
Core application infrastructure: project/session management, startup coordination, installer/remote helpers, busy-state handling, and Phonology Assistant integration that underpin all FieldWorks shells.

### Common/Filters
Matcher and sorter framework (IntMatcher, RegExpMatcher, RecordFilter, RecordSorter, etc.) that powers browse views, filter bars, and searchable lists throughout the suite.

### Common/Framework
Application framework services: FwApp, editing helpers, publication/printing hooks, settings accessors, main window coordination, undo/redo UI, and XHTML export utilities shared by FieldWorks apps.

### Common/FwUtils
Extensive utility toolkit covering registry, XML serialization, audio conversion, clipboard/thread helpers, benchmarking, FLEx Bridge integration, help, and other cross-cutting services.

### Common/RootSite
Advanced root-site host for the native Views engine that manages lifecycle, selections, printing, and bridges between WinForms and the C++ text-rendering architecture.

### Common/ScriptureUtils
Paratext interoperability layer supplying project discovery, scripture text access, helper wrappers, and reference comparison utilities for bidirectional Paratext <→ FieldWorks workflows.

### Common/SimpleRootSite
Simplified IVwRootSite implementation with editing, selection, printing, and activation helpers that makes hosting Views content easy for most UI scenarios.

### Common/UIAdapterInterfaces
Adapter contracts (ITMAdapter, ISIBInterface, helpers) that decouple XCore mediator tooling from concrete WinForms controls and enable UI test doubles.

### Common/ViewsInterfaces
Managed COM interface definitions and wrappers (IVw* families, DispPropOverrideFactory, ComWrapper utilities) that bridge .NET code to the native Views text-rendering engine.

## LexText Subfolders

### LexText/Discourse
Constituent charting engine that lets linguists arrange interlinear text into clause/constituent grids, track discourse structure, and export annotated charts.

### LexText/FlexPathwayPlugin
FLEx utility plug-in that surfaces SIL Pathway in the Tools menu and orchestrates exports so Pathway can publish FieldWorks lexicon and interlinear data.

### LexText/Interlinear
Massive interlinear analysis library powering glossing editors, concordance search, tagging, complex concordance patterns, and print/export pipelines.

### LexText/LexTextControls
Shared lexicon UI dialog/control library (import wizards, search, configuration, feature-structure controls) reused across Lexicon/, Morphology/, and other FLEx areas.

### LexText/LexTextDll
Thin coordination layer that hosts the LexTextApp, area listeners, help providers, and resources bridging the generic xWorks shell with lexicon/text features.

### LexText/LexTextExe
Minimal FLEx executable that calls into shared FieldWorks startup helpers; all functional logic lives in LexTextDll and xWorks.

### LexText/Lexicon
Lexicon-editing slices, launchers, menu handlers, FLExBridge hooks, and supporting dialogs that deliver the primary dictionary editing experience.

### LexText/Morphology
Morphology editor library with templates, rule builders, phoneme/feature editors, concordance tools, and area listeners for the Morphology/Grammar workspace.

### LexText/ParserCore
Parsing infrastructure that schedules background HermitCrab/XAmple runs, watches for model changes, and files parse results for computer-assisted analysis.

### LexText/ParserUI
Parser dialogs for Try-A-Word, batch reports, word-set import, configuration, and XAmple grammar debugging so linguists can validate parser behavior.

## Utilities Subfolders

### Utilities/FixFwData
Command-line wrapper that runs SIL.LCModel.FixData against a project, logs issues, and returns failure when repairs are needed.

### Utilities/FixFwDataDll
Utility plug-in and dialog wiring that hosts the Fix Data engine inside FieldWorks UI and coordinates repair runs.

### Utilities/MessageBoxExLib
Borrowed MessageBoxEx implementation with custom buttons, saved responses, and timeout features used across the UI.

### Utilities/Reporting
SIL.Reporting integration that gathers diagnostics, shows error dialogs, and lets users send feedback when crashes occur.

### Utilities/SfmStats
Console utility that tallies Standard Format Marker usage to support data-cleanup and import preparation.

### Utilities/SfmToXml
Library and command-line converter that parses SFM text into XML for FieldWorks import workflows.

### Utilities/XMLUtils
Static helpers for XML attribute handling, dynamic assembly loading, entity resolution, and configuration exceptions reused throughout the codebase.

## XCore Subfolders

### XCore/FlexUIAdapter
Concrete adapters (menus, toolbars, sidebar, pane bars) that connect FLEx WinForms controls to the XCore command/choice framework.

### XCore/SilSidePane
Side-pane UI implementation with tabs/items, drag-reorder support, and customization dialog used for area switching.

### XCore/xCoreInterfaces
Core mediator, property-table, command, choice, and UI-adapter interfaces that form the heart of the XCore extensibility model.

### XCore/xCoreTests
Automated tests covering mediator routing, property tables, inventory XML loading, and override mechanics to ensure the framework stays stable.
