---
last-reviewed: 2025-10-31
last-reviewed-tree: 13ccafb9b0da2f9f054da0d53fad5912915e018b62b5ac96b9bf00bb5e6f402a
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
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

# FXT COPILOT summary

## Purpose
FieldWorks Transform (FXT) infrastructure for XML-based data export and import using template-driven transformations. XDumper handles XML export of FieldWorks data with customizable filtering and formatting. XUpdater handles XML import for updating FieldWorks data. FilterStrategy provides filtering logic for selective export/import. ChangedDataItem tracks data changes. FxtExe provides command-line tool for FXT operations. Enables bulk data operations, custom exports, and data synchronization scenarios using declarative XML templates.

## Architecture
C# libraries and executable with XML transformation engine. FxtDll/ contains core library (XDumper, XUpdater, FilterStrategy, ChangedDataItem - 4716 lines), FxtExe/ contains command-line tool (main.cs). FxtReference.doc provides documentation. Test project FxtDllTests validates functionality. Uses template-driven approach: XML templates control which data to export/import and how to format it.

## Key Components
- **XDumper** class (XDumper.cs): XML export engine
  - Exports FieldWorks data to XML using FXT templates
  - Caches custom fields and writing system data for performance
  - ProgressHandler delegate for progress reporting
  - Supports multiple output formats: XML, SFM (Standard Format Marker)
  - Filtering via IFilterStrategy[]
  - WritingSystemAttrStyles enum: controls writing system representation
  - StringFormatOutputStyle enum: controls string formatting
  - Template-driven: XML template defines what to export and structure
- **XUpdater** class (XUpdater.cs): XML import/update engine
  - Imports XML data back into FieldWorks database
  - Applies updates based on FXT templates
  - Reverse of XDumper functionality
- **FilterStrategy** (FilterStrategy.cs): Filter logic interface/implementation
  - IFilterStrategy interface for filtering data during export/import
  - Enables selective operations (e.g., export only changed data)
- **ChangedDataItem** (ChangedDataItem.cs): Change tracking
  - Tracks modifications to FieldWorks data
  - Used by filters to identify changed objects
- **FxtExe** (FxtExe/main.cs): Command-line tool
  - Executable interface to FXT library
  - Runs export/import operations from command line
  - App.ico icon for executable

## Technology Stack
- C# .NET Framework 4.8.x (assumed based on repo)

## Dependencies
- Upstream: Language and Culture Model (LcmCache, ICmObject)
- Downstream: Applications use FXT for data export

## Interop & Contracts
- **IFilterStrategy**: Contract for filtering data during export/import

## Threading & Performance
- **Caching**: XDumper caches custom fields and writing system data per instance

## Config & Feature Flags
- **WritingSystemAttrStyles**: Controls writing system representation in output

## Build Information
- **Project files**: FxtDll/FxtDll.csproj (Library), FxtExe/FxtExe.csproj (Executable)

## Interfaces and Data Models
XDumper, IFilterStrategy, XUpdater, ChangedDataItem.

## Entry Points
- **FxtExe.exe**: Command-line tool for FXT operations

## Test Index
- **Test project**: FxtDll/FxtDllTests/

## Usage Hints
- Create FXT XML template defining export/import structure

## Related Folders
- **Transforms/**: XSLT stylesheets that may complement FXT operations

## References
See `.cache/copilot/diff-plan.json` for file details.
