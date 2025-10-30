# FieldWorks Src/ Folder Catalog

This catalog provides brief descriptions of each folder under `Src/`. For detailed information about any folder, see its `COPILOT.md` file.

Editorial note: See `.github/update-copilot-summaries.md` for how to update and maintain these per-folder summaries (including frontmatter expectations and review cadence).

## Folder Descriptions

### AppCore
Shared application core helpers and base infrastructure. Provides fundamental graphics and styled text rendering capabilities.

### CacheLight
Lightweight caching services used by core components. Efficient in-memory caching for FieldWorks data access.

### Cellar
Core data model and persistence layer (LCM - FieldWorks Language and Culture Model). Foundational data model with XML serialization.

### Common
Cross-cutting utilities and shared managed/native code. Contains fundamental UI controls, framework components, and utility libraries used throughout FieldWorks.

### DbExtend
Database extensions and schema helpers. Provides extensibility mechanisms for customizing database schema at runtime.

### DebugProcs
Developer diagnostics and debug helpers. Provides debugging utilities and diagnostic tools for troubleshooting.

### DocConvert
Document and data conversion tools. Handles conversion between different document formats and data representations.

### FXT
FieldWorks transform assets and related tooling. Provides XSLT-based transformation capabilities for converting linguistic data.

### FdoUi
UI components for FieldWorks Data Objects. Provides user interface elements for interacting with the data model, including custom field management.

### FwCoreDlgs
Common dialogs used across FieldWorks applications. Standardized dialog boxes for file selection, configuration, and user input.

### FwParatextLexiconPlugin
Paratext lexicon integration plugin. Enables FieldWorks lexicon data access from within Paratext Bible translation software.

### FwResources
Shared resources (images, strings, assets) for FieldWorks applications. Centralizes resource management for consistent UI and localization.

### GenerateHCConfig
Build-time configuration generation utilities. Creates help configuration files and build artifacts for the help system.

### Generic
Generic/shared components that don't fit a single application. Low-level utility classes, algorithms, and helper functions.

### InstallValidator
Utilities to validate installation prerequisites. Checks system requirements and configuration before installation or startup.

### Kernel
Low-level core services and infrastructure. Provides fundamental building blocks including memory management, error handling, and string processing.

### LCMBrowser
LCM/Cellar model browser tooling. Development tool for exploring the FieldWorks data model and debugging data structures.

### LexText
Lexicon/Dictionary application and related components. Comprehensive lexicon editing, dictionary configuration, interlinear text analysis, morphology, and discourse features.

### ManagedLgIcuCollator
Managed wrapper for ICU collation services. Provides .NET-friendly access to ICU collation for proper linguistic text ordering.

### ManagedVwDrawRootBuffered
Managed view rendering primitives for buffered drawing. Double-buffered rendering infrastructure to eliminate flicker.

### ManagedVwWindow
Managed view window components. .NET wrappers for native view windows, bridging managed UI and native rendering.

### MigrateSqlDbs
Database migration and upgrade tooling. Handles schema migrations and version upgrades between FieldWorks versions.

### Paratext8Plugin
Integration plugin for Paratext 8. Enables bidirectional integration between FieldWorks and Paratext 8.

### ParatextImport
Import pipeline for Paratext data. Handles importing Scripture texts, notes, and related data from Paratext projects.

### ProjectUnpacker
Utilities for unpacking FieldWorks projects. Handles decompression and extraction of project archives.

### Transforms
Transformation assets (XSLT) and helpers. Contains XSLT stylesheets for data conversion, report generation, and content export.

### UnicodeCharEditor
Unicode character editor tool. Specialized interface for viewing and editing Unicode character properties.

### Utilities
Miscellaneous utilities used across the repository. Contains standalone tools including data repair, XML utilities, and helper libraries.

### XCore
Cross-cutting framework base used by multiple applications. Provides application framework, plugin architecture, command handling, and UI composition infrastructure.

### views
C++ view-layer components and UI view infrastructure. Native rendering engine for sophisticated text display with complex writing systems.

### xWorks
Primary FieldWorks application shell and modules. Main application infrastructure hosting LexText and other work areas, with dictionary configuration and data navigation.

## Common Subfolders

### Common/Controls
Shared UI controls library. Provides reusable widgets and XML-based view components used throughout FieldWorks applications.

### Common/FieldWorks
Core FieldWorks-specific utilities and application infrastructure. Provides fundamental application services including settings management and busy dialogs.

### Common/Filters
Data filtering functionality. Provides filter matchers and sorting capabilities for searching and displaying filtered data sets.

### Common/Framework
Application framework components. Provides core application infrastructure including editing helpers, settings management, and export functionality.

### Common/FwUtils
General FieldWorks utilities library. Provides utility functions, helpers, and extension methods used throughout FieldWorks.

### Common/RootSite
Root-level site management for views. Provides base infrastructure for hosting and managing FieldWorks' view system.

### Common/ScriptureUtils
Scripture-specific utilities. Provides support for Paratext integration, scripture references, and biblical text handling.

### Common/SimpleRootSite
Simplified root site implementation. Provides streamlined API for hosting FieldWorks views with common functionality pre-configured.

### Common/UIAdapterInterfaces
UI adapter pattern interfaces. Defines contracts for adapting different UI technologies and providing abstraction layers.

### Common/ViewsInterfaces
View layer interfaces. Defines managed interfaces for interacting with the native view rendering engine.

## LexText Subfolders

### LexText/Discourse
Discourse analysis features. Provides tools for analyzing and charting discourse structure in texts.

### LexText/FlexPathwayPlugin
Pathway publishing integration plugin. Enables export and publishing of lexicon and text data using SIL's Pathway system.

### LexText/Interlinear
Interlinear text analysis and glossing. Provides tools for creating and managing interlinear texts with morpheme-by-morpheme analysis.

### LexText/LexTextControls
Lexicon UI controls library. Provides specialized controls and dialogs for lexicon editing, including import/export wizards.

### LexText/LexTextDll
Core lexicon application functionality. Provides the main application logic and infrastructure for the LexText (FLEx) application.

### LexText/LexTextExe
Main executable for LexText (FLEx). Provides the entry point for launching the FieldWorks Language Explorer application.

### LexText/Lexicon
Lexicon editing components. Provides the main lexicon entry editing interface, reference management, and FLEx Bridge integration.

### LexText/Morphology
Morphological analysis and morphology editor. Provides tools for defining morphological rules, allomorph conditions, and phonological features.

### LexText/ParserCore
Core parsing engine. Provides the Hermit Crab (HC) parser implementation for analyzing words into morphemes.

### LexText/ParserUI
Parser user interface components. Provides UI for configuring, testing, and tracing the morphological parser.

## Utilities Subfolders

### Utilities/FixFwData
Command-line tool for repairing FieldWorks data files. Provides automated data integrity checking and repair functionality.

### Utilities/FixFwDataDll
Core data repair library. Provides implementation of data validation, error detection, and automatic repair functionality.

### Utilities/MessageBoxExLib
Enhanced message box library. Provides extended message box functionality beyond standard Windows message boxes.

### Utilities/Reporting
Error reporting functionality. Provides infrastructure for collecting, displaying, and submitting error reports.

### Utilities/SfmStats
SFM statistics tool. Analyzes Standard Format Marker files to provide statistics about marker usage.

### Utilities/SfmToXml
SFM to XML conversion utility. Converts SFM-formatted data files to XML format for processing and import.

### Utilities/XMLUtils
XML processing utilities. Provides common XML handling functionality, dynamic loading, and exception handling.

## XCore Subfolders

### XCore/FlexUIAdapter
FLEx UI adapter implementation. Provides concrete implementation of UI adapter interfaces for FieldWorks applications.

### XCore/SilSidePane
Side pane UI component. Provides the navigation pane (sidebar) control used in FieldWorks applications.

### XCore/xCoreInterfaces
Core interfaces for XCore framework. Defines contracts for command handling, choice management, UI components, and mediator pattern.

### XCore/xCoreTests
Test suite for XCore framework. Provides comprehensive tests for XCore framework functionality.
