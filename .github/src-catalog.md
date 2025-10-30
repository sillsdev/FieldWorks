# FieldWorks Src/ Folder Catalog

This catalog provides brief descriptions of each folder under `Src/`. For detailed information about any folder, see its `COPILOT.md` file.

Editorial note: See `.github/update-copilot-summaries.md` for how to update and maintain these per-folder summaries (including frontmatter expectations and review cadence).

## Folder Descriptions

### AppCore
Shared application core helpers and base infrastructure used across FieldWorks applications.

### CacheLight
Lightweight caching services providing efficient in-memory data access for FieldWorks components.

### Cellar
Core data model infrastructure providing XML processing and serialization services.

### Common
Cross-cutting utilities and shared infrastructure used throughout FieldWorks. Contains fundamental UI controls (Controls), framework components (Framework), shared managed 
code (FwUtils), native c...

### DbExtend
Database schema extension and runtime customization infrastructure. Provides mechanisms for extending the base FieldWorks data model with custom fields, 
properties, and relationships at runtime.

### DebugProcs
Developer diagnostics and debugging utilities for troubleshooting FieldWorks issues.

### DocConvert
Document and data format conversion utilities. Handles transformation between 
different document formats and data representations used in FieldWorks.

### FXT
FieldWorks Transform (FXT) assets and XSLT-based transformation infrastructure. Provides XSLT stylesheets and transformation utilities for converting and presenting linguistic 
data in different fo...

### FdoUi
User interface components for FieldWorks Data Objects (FDO/LCM). Provides specialized UI controls for editing and displaying data model objects, including 
custom field management dialogs (CustomFi...

### FwCoreDlgs
Common dialogs and UI components shared across FieldWorks applications. Includes standardized dialog boxes for file operations (BackupProjectDlg, RestoreProjectDlg), 
writing system configuration (...

### FwParatextLexiconPlugin
Integration plugin enabling Paratext Bible translation software to access 
FieldWorks lexicon data.

### FwResources
Centralized resource management for FieldWorks applications. Contains shared images, icons, localized strings, and other UI assets used throughout the application suite.

### GenerateHCConfig
Build-time utilities for generating help system configuration files. Creates configuration artifacts and metadata files needed by the FieldWorks help system.

### Generic
Generic utility components and low-level helper classes that don't fit into more 
specific categories.

### InstallValidator
Installation prerequisite validation utilities. Checks system requirements, validates configuration, and verifies that necessary dependencies 
are present before FieldWorks installation or startup.

### Kernel
Low-level core services and fundamental infrastructure for all FieldWorks native code.

### LCMBrowser
Development tool for exploring and debugging the FieldWorks data model (LCM). Provides a browser interface for navigating object relationships, inspecting properties, 
and understanding the structu...

### LexText
Lexicon/Dictionary application suite and related components. Encompasses the FieldWorks Language Explorer (FLEx) application including lexicon editing (Lexicon), 
dictionary configuration (LexTextD...

### ManagedLgIcuCollator
Managed .NET wrapper for ICU (International Components for Unicode) collation services.

### ManagedVwDrawRootBuffered
Managed wrapper for buffered view rendering infrastructure. Implements double-buffered drawing to eliminate flicker in complex text displays.

### ManagedVwWindow
Managed .NET wrappers for native view window components. Bridges managed UI code with the native views rendering engine, enabling .NET applications 
to host sophisticated text display views that le...

### MigrateSqlDbs
Database migration and versioning infrastructure. Handles schema migrations, data transformations, and version upgrades between different 
FieldWorks releases.

### Paratext8Plugin
Bidirectional integration plugin for Paratext 8 Bible translation software. Enables data exchange and synchronization between FieldWorks and Paratext 8 projects.

### ParatextImport
Import pipeline for bringing Paratext Scripture data into FieldWorks. Handles parsing and conversion of Paratext USFM texts, notes, and associated data into 
the FieldWorks data model.

### ProjectUnpacker
Utilities for extracting and decompressing FieldWorks project archives. Handles unpacking of .fwbackup files and other compressed project formats.

### Transforms
Collection of XSLT transformation stylesheets and supporting utilities. Contains templates for data conversion, report generation, and content export across 
various formats.

### UnicodeCharEditor
Specialized tool for viewing and editing Unicode character properties. Provides detailed information about Unicode characters, their properties, and rendering 
characteristics.

### Utilities
Miscellaneous utilities and standalone tools. Contains data repair utilities (FixFwData, FixFwDataDll), format conversion tools (SfmToXml, SfmStats), 
XML helpers (XMLUtils), error reporting (Repor...

### XCore
Cross-cutting application framework and plugin architecture. Provides command handling, choice management, property propagation, UI composition, and extensibility 
infrastructure (xCoreInterfaces, ...

### views
C++ native rendering engine providing sophisticated text display capabilities for complex 
writing systems.

### xWorks
Primary FieldWorks application shell and module hosting infrastructure. Implements the main application framework (xWorks) that hosts LexText and other work areas, 
provides dictionary configuratio...

## Common Subfolders

### Common/Controls
Shared UI controls library providing reusable widgets and XML-based view components.

### Common/FieldWorks
Core FieldWorks-specific application infrastructure and utilities. Provides fundamental application services including project management (FieldWorksManager, ProjectId), 
settings management (FwRes...

### Common/Filters
Data filtering and sorting infrastructure for searchable data views. Implements various matcher types (IntMatcher, RangeIntMatcher, ExactMatcher, BeginMatcher) 
and filtering logic (RecordFilter, P...

### Common/Framework
Application framework components providing core infrastructure services. Includes editing helpers (FwEditingHelper), publication interfaces (IPublicationView, IPageSetupDialog), 
settings managemen...

### Common/FwUtils
General FieldWorks utilities library containing wide-ranging helper functions. Provides utilities for image handling (ManagedPictureFactory), registry access (IFwRegistryHelper), 
XML serialization...

### Common/RootSite
Root-level site management infrastructure for hosting FieldWorks views. Implements RootSite classes that provide the top-level container for the Views rendering system, 
handle view lifecycle manag...

### Common/ScriptureUtils
Scripture-specific utilities and Paratext integration support. Provides specialized handling for biblical text references, Paratext project integration, 
scripture navigation, and biblical text str...

### Common/SimpleRootSite
Simplified root site implementation with streamlined API. Provides a more accessible interface to the Views system for common scenarios, handling 
standard view hosting patterns with pre-configured...

### Common/UIAdapterInterfaces
UI adapter pattern interfaces for abstraction and testability. Defines contracts that allow UI components to be adapted to different technologies or replaced 
with test doubles.

### Common/ViewsInterfaces
Managed interface definitions for the native Views rendering engine. Declares .NET interfaces corresponding to native COM interfaces in the Views system, enabling 
managed code to interact with the...

## LexText Subfolders

### LexText/Discourse
Discourse analysis and charting functionality for linguistic text analysis. Implements tools for analyzing and visualizing discourse structure, including constituent charts, 
template-based analysi...

### LexText/FlexPathwayPlugin
Pathway publishing system integration for FLEx. Provides plugin infrastructure to export lexicon and interlinear data using SIL's Pathway 
publishing system.

### LexText/Interlinear
Interlinear text analysis and morpheme-by-morpheme glossing functionality. Implements the interlinear text editor (InterlinDocChart, InterlinVc), morpheme analysis tools, 
glossing interfaces, conc...

### LexText/LexTextControls
Lexicon UI controls library for FLEx editing interfaces. Provides specialized controls and dialogs for lexicon management including import/export wizards 
(LexImportWizard, FlexLiftMerger), entry e...

### LexText/LexTextDll
Core LexText application logic and infrastructure. Implements the main application coordination, module initialization, integration of various 
areas (lexicon, morphology, interlinear, discourse), ...

### LexText/LexTextExe
Main executable entry point for FieldWorks Language Explorer (FLEx). Provides the startup code, application initialization, and hosting environment for the LexText 
application.

### LexText/Lexicon
Lexicon editing and entry management components. Implements the core lexical database editing interface including entry forms, reference management, 
sense hierarchies, and FLEx Bridge integration ...

### LexText/Morphology
Morphological analysis and morphology editor infrastructure. Provides tools for defining morphological rules, allomorph conditions, phonological features, 
natural classes, and morpheme environments.

### LexText/ParserCore
Core morphological parsing engine (Hermit Crab). Implements the HC (Hermit Crab) parser that analyzes words into component morphemes based on 
linguistic rules defined in the morphology editor.

### LexText/ParserUI
User interface components for parser configuration and testing. Provides UI for configuring the morphological parser, testing parser behavior, viewing parse traces, 
managing parser settings, and d...

## Utilities Subfolders

### Utilities/FixFwData
Command-line tool for repairing FieldWorks project data files. Provides automated data integrity checking, error detection, and repair functionality that can be 
run outside the main FieldWorks app...

### Utilities/FixFwDataDll
Core data repair library implementing validation and fix logic. Contains the actual implementation of data integrity checks, error detection algorithms, and 
automatic repair routines.

### Utilities/MessageBoxExLib
Enhanced message box library with extended functionality. Provides message boxes with additional features beyond standard Windows message boxes, 
such as custom button layouts, checkboxes for "don'...

### Utilities/Reporting
Error reporting and diagnostic information collection infrastructure. Implements functionality for gathering error details, capturing system state, displaying error 
reports to users, and optionall...

### Utilities/SfmStats
Standard Format Marker (SFM) statistics and analysis tool. Analyzes SFM-formatted data files to provide statistics about marker usage, frequency, 
and patterns.

### Utilities/SfmToXml
SFM to XML data conversion utility and library. Converts Standard Format Marker files (legacy linguistic data format) into XML format 
for processing and import into FieldWorks.

### Utilities/XMLUtils
XML processing utilities and helper functions. Provides common XML handling functionality including dynamic assembly loading (DynamicLoader), 
exception handling, validation, and XML manipulation h...

## XCore Subfolders

### XCore/FlexUIAdapter
FLEx implementation of XCore UI adapter interfaces. Provides concrete adapter implementations that connect FLEx application components to the 
XCore framework's command handling, choice management,...

### XCore/SilSidePane
Side pane navigation control for FieldWorks applications. Implements the navigation sidebar (SidePane, Tab, Item classes) that provides hierarchical 
navigation between different areas and tools in...

### XCore/xCoreInterfaces
Core interface definitions for the XCore framework. Declares fundamental contracts for command handling (IxCoreColleague), choice management 
(ChoiceGroup), property tables (PropertyTable), mediato...

### XCore/xCoreTests
Test suite for XCore framework functionality. Provides comprehensive tests validating XCore's command handling, property table behavior, 
mediator functionality, and plugin infrastructure.
