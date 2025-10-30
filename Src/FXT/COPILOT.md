---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FXT

## Purpose
FieldWorks Transform (FXT) assets and XSLT-based transformation infrastructure.
Provides XSLT stylesheets and transformation utilities for converting and presenting linguistic
data in different formats. Used extensively for generating reports, exports, and formatted output
from the FieldWorks data model.

## Key Components
### Key Classes
- **main**
- **ChangedDataItem**
- **XDumper**
- **XUpdater**
- **FXTElementSearchProperties**
- **XUpdaterException**
- **ConstraintFilterStrategy**
- **FxtTestBase**
- **QuickTests**
- **M3ParserDumpTests**

### Key Interfaces
- **IFilterStrategy**

## Technology Stack
- C# .NET
- XSLT transformation engine
- XML processing

## Dependencies
- Depends on: Common utilities, data model
- Used by: Export/import pipelines, document generation

## Build Information
- Two C# projects: executable (FxtExe) and library (FxtDll)
- Build with MSBuild or Visual Studio
- Command-line transformation tool

## Entry Points
- **FxtExe** - Command-line tool for applying transforms
- **FxtDll** - Library for embedding transformation capabilities

## Related Folders
- **Transforms/** - Contains XSLT files and transformation assets used by FXT
- **DocConvert/** - Document conversion that may use FXT transformations
- **ParatextImport/** - May use FXT for data transformation during import

## Code Evidence
*Analysis based on scanning 13 source files*

- **Classes found**: 13 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.FXT

## Interfaces and Data Models

- **IFilterStrategy** (interface)
  - Path: `FxtDll/FilterStrategy.cs`
  - Public interface definition

- **ChangedDataItem** (class)
  - Path: `FxtDll/ChangedDataItem.cs`
  - Public class implementation

- **ConstraintFilterStrategy** (class)
  - Path: `FxtDll/FilterStrategy.cs`
  - Public class implementation

- **FXTElementSearchProperties** (class)
  - Path: `FxtDll/XUpdater.cs`
  - Public class implementation

- **StandardFormat** (class)
  - Path: `FxtDll/FxtDllTests/StandFormatExportTests.cs`
  - Public class implementation

- **XDumper** (class)
  - Path: `FxtDll/XDumper.cs`
  - Public class implementation

- **XUpdater** (class)
  - Path: `FxtDll/XUpdater.cs`
  - Public class implementation

- **XUpdaterException** (class)
  - Path: `FxtDll/XUpdater.cs`
  - Public class implementation

- **main** (class)
  - Path: `FxtExe/main.cs`
  - Public class implementation

- **StringFormatOutputStyle** (enum)
  - Path: `FxtDll/XDumper.cs`

- **WritingSystemAttrStyles** (enum)
  - Path: `FxtDll/XDumper.cs`

- **NormalizeOutput** (xslt)
  - Path: `FxtDll/FxtDllTests/NormalizeOutput.xsl`
  - XSLT transformation template

## References

- **Project files**: FxtDll.csproj, FxtDllTests.csproj, FxtExe.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, AssemblyInfo.cs, ChangedDataItem.cs, FilterStrategy.cs, FxtTestBase.cs, M3ParserDumpTests.cs, SimpleTests.cs, XDumper.cs, XUpdater.cs, main.cs
- **XSLT transforms**: NormalizeOutput.xsl
- **XML data/config**: Phase1-Sena3-bo-ConfiguredDictionary.xml, Phase2-Sena3-bo-ConfiguredDictionary.xml, TLPParser.xml, TLPSimpleGuidsAnswer.xml, TLPSketchGen.xml
- **Source file count**: 13 files
- **Data file count**: 6 files

## Architecture
C# library with 13 source files. Contains 2 subprojects: FxtExe, FxtDll.

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Threading model: explicit threading, UI thread marshaling, synchronization.

## Config & Feature Flags
Config files: Phase1-Sena3-bo-ConfiguredDictionary.xml, Phase2-Sena3-bo-ConfiguredDictionary.xml.

## Test Index
Test projects: FxtDllTests. 6 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Console application. Build and run via command line or Visual Studio. See Entry Points section.

## References (auto-generated hints)
- Project files:
  - Src\FXT\FxtDll\FxtDll.csproj
  - Src\FXT\FxtDll\FxtDllTests\FxtDllTests.csproj
  - Src\FXT\FxtExe\FxtExe.csproj
- Key C# files:
  - Src\FXT\FxtDll\AssemblyInfo.cs
  - Src\FXT\FxtDll\ChangedDataItem.cs
  - Src\FXT\FxtDll\FilterStrategy.cs
  - Src\FXT\FxtDll\FxtDllTests\DumperTests.cs
  - Src\FXT\FxtDll\FxtDllTests\FxtTestBase.cs
  - Src\FXT\FxtDll\FxtDllTests\M3ParserDumpTests.cs
  - Src\FXT\FxtDll\FxtDllTests\M3SketchDumpTests.cs
  - Src\FXT\FxtDll\FxtDllTests\SimpleTests.cs
  - Src\FXT\FxtDll\FxtDllTests\StandFormatExportTests.cs
  - Src\FXT\FxtDll\XDumper.cs
  - Src\FXT\FxtDll\XUpdater.cs
  - Src\FXT\FxtExe\AssemblyInfo.cs
  - Src\FXT\FxtExe\main.cs
- Data contracts/transforms:
  - Src\FXT\FxtDll\FxtDllTests\ExpectedResults\Phase1-Sena3-bo-ConfiguredDictionary.xml
  - Src\FXT\FxtDll\FxtDllTests\ExpectedResults\Phase2-Sena3-bo-ConfiguredDictionary.xml
  - Src\FXT\FxtDll\FxtDllTests\ExpectedResults\TLPParser.xml
  - Src\FXT\FxtDll\FxtDllTests\ExpectedResults\TLPSimpleGuidsAnswer.xml
  - Src\FXT\FxtDll\FxtDllTests\ExpectedResults\TLPSketchGen.xml
  - Src\FXT\FxtDll\FxtDllTests\NormalizeOutput.xsl
