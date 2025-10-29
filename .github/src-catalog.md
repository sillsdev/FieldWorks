# FieldWorks Src/ Folder Catalog

This catalog provides brief descriptions of each folder under `Src/`. For detailed information about any folder, see its `COPILOT.md` file.

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
