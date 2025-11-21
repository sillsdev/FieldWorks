---
last-reviewed: 2025-10-31
last-reviewed-tree: 13ccafb9b0da2f9f054da0d53fad5912915e018b62b5ac96b9bf00bb5e6f402a
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/FXT. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
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
- OutputType: FxtDll.dll (Library), FxtExe.exe (Executable)
- SIL.LCModel for data access
- System.Xml for XML processing
- ICU normalization (Icu.Normalization)

## Dependencies

### Upstream (consumes)
- **SIL.LCModel**: Language and Culture Model (LcmCache, ICmObject)
- **SIL.LCModel.Application**: Application services
- **SIL.LCModel.Infrastructure**: Infrastructure layer
- **SIL.LCModel.DomainServices**: Domain services
- **SIL.LCModel.Core.Cellar**: Core data types
- **SIL.LCModel.Core.Text**: Text handling
- **SIL.LCModel.Core.WritingSystems**: Writing systems
- **Common/FwUtils**: FieldWorks utilities
- **System.Xml**: XML parsing and generation

### Downstream (consumed by)
- **Export operations**: Applications use FXT for data export
- **Import operations**: Applications use FXT for data import
- **FxtExe**: Command-line users
- **Custom bulk operations**: Scripts and tools using FXT templates

## Interop & Contracts
- **IFilterStrategy**: Contract for filtering data during export/import
- **ProgressHandler**: Delegate for progress callbacks
- **ICmObject**: FieldWorks data objects exported/imported
- **XML templates**: Declarative contracts for export/import structure

## Threading & Performance
- **Caching**: XDumper caches custom fields and writing system data per instance
  - New XDumper per export recommended if database changes
- **Progress reporting**: ProgressHandler enables responsive UI during long operations
- **Cancellation**: m_cancelNow flag for operation cancellation
- **Performance**: Template-driven approach efficient for bulk operations

## Config & Feature Flags
- **WritingSystemAttrStyles**: Controls writing system representation in output
- **StringFormatOutputStyle**: Controls string formatting (None, etc.)
- **m_outputGuids**: Boolean controlling GUID output
- **m_requireClassTemplatesForEverything**: Strict template enforcement
- **Format**: "xml" or "sf" (Standard Format Marker)

## Build Information
- **Project files**: FxtDll/FxtDll.csproj (Library), FxtExe/FxtExe.csproj (Executable)
- **Test project**: FxtDll/FxtDllTests/
- **Output**: FxtDll.dll, FxtExe.exe
- **Build**: Via top-level FieldWorks.sln
- **Documentation**: FxtReference.doc (127KB)

## Interfaces and Data Models

- **XDumper** (XDumper.cs)
  - Purpose: Export FieldWorks data to XML using FXT templates
  - Inputs: LcmCache, XML template, root object, filters, output format
  - Outputs: XML file or stream with exported data
  - Notes: Create new instance per export for cache freshness

- **IFilterStrategy** (FilterStrategy.cs)
  - Purpose: Contract for filtering objects during export/import
  - Inputs: ICmObject to evaluate
  - Outputs: bool (true to include, false to exclude)
  - Notes: Multiple filters can be applied (m_filters array)

- **XUpdater** (XUpdater.cs)
  - Purpose: Import XML data into FieldWorks database using FXT templates
  - Inputs: LcmCache, XML data file, FXT template
  - Outputs: Updated database
  - Notes: Reverse of XDumper

- **ChangedDataItem** (ChangedDataItem.cs)
  - Purpose: Track data changes for change-based exports
  - Inputs: Object identifier, change type
  - Outputs: Change tracking data
  - Notes: Used by filters to export only changed data

- **ProgressHandler delegate**
  - Purpose: Progress callback during long export/import operations
  - Inputs: object sender
  - Outputs: void (notification only)
  - Notes: Enables responsive UI with progress feedback

## Entry Points
- **FxtExe.exe**: Command-line tool for FXT operations
- **XDumper**: Library entry point for XML export
- **XUpdater**: Library entry point for XML import
- Referenced by applications needing bulk data operations

## Test Index
- **Test project**: FxtDll/FxtDllTests/
- **Run tests**: `dotnet test FxtDll/FxtDllTests/` or Visual Studio Test Explorer
- **Coverage**: XDumper, XUpdater, filtering

## Usage Hints
- Create FXT XML template defining export/import structure
- Instantiate XDumper with LcmCache and template
- Call export method with root object and filters
- Use IFilterStrategy for selective exports (e.g., changed data only)
- FxtExe for command-line batch operations
- See FxtReference.doc for template syntax and examples
- Create new XDumper per export if database changes (caching)

## Related Folders
- **Transforms/**: XSLT stylesheets that may complement FXT operations
- **ParatextImport/**: Specialized import (may use FXT infrastructure)
- Applications using FXT for export/import

## References
- **Project files**: FxtDll/FxtDll.csproj, FxtExe/FxtExe.csproj
- **Test project**: FxtDll/FxtDllTests/
- **Documentation**: FxtReference.doc (127KB reference manual)
- **Key C# files**: FxtDll/XDumper.cs, FxtDll/XUpdater.cs, FxtDll/FilterStrategy.cs, FxtDll/ChangedDataItem.cs, FxtExe/main.cs
- **Total lines (FxtDll)**: 4716
- **Output**: FxtDll.dll, FxtExe.exe
- **Namespace**: SIL.FieldWorks.Common.FXT