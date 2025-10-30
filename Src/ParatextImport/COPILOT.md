---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ParatextImport

## Purpose
Import pipeline for bringing Paratext Scripture data into FieldWorks.
Handles parsing and conversion of Paratext USFM texts, notes, and associated data into
the FieldWorks data model. Supports importing both Scripture texts and related project
information while preserving markup and structure.

## Key Components
### Key Classes
- **ScrAnnotationInfo**
- **Cluster**
- **OverlapInfo**
- **ClusterListHelper**
- **SectionHeadCorrelationHelper**
- **ImportedBooks**
- **UndoImportManager**
- **ImportStyleProxy**
- **ParatextImportManager**
- **DifferenceList**

### Key Interfaces
- **ISCTextSegment**
- **ISCScriptureText**
- **IBookVersionAgent**
- **ISCTextEnum**

## Technology Stack
- C# .NET
- Paratext data format parsing
- Data transformation and mapping
- Import pipeline architecture

## Dependencies
- Depends on: Cellar (data model), Paratext SDK, Common utilities
- Used by: FieldWorks applications importing Paratext data

## Build Information
- C# class library
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Provides import APIs and wizards
- Used through UI import commands in applications

## Related Folders
- **Paratext8Plugin/** - Bidirectional Paratext 8 integration
- **FwParatextLexiconPlugin/** - Lexicon data sharing with Paratext
- **LexText/** - Target for imported lexical data
- **DocConvert/** - Document conversion utilities used in import
- **FXT/** - May use transformations during import

## Code Evidence
*Analysis based on scanning 41 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 4 public interfaces
- **Namespaces**: ParatextImport, ParatextImport.ImportTests

## Interfaces and Data Models

- **IBookVersionAgent** (interface)
  - Path: `IBookVersionAgent.cs`
  - Public interface definition

- **ISCScriptureText** (interface)
  - Path: `ISCScriptureText.cs`
  - Public interface definition

- **ISCTextEnum** (interface)
  - Path: `ISCTextEnum.cs`
  - Public interface definition

- **ISCTextSegment** (interface)
  - Path: `ISCTextSegment.cs`
  - Public interface definition

- **BookMerger** (class)
  - Path: `BookMerger.cs`
  - Public class implementation

- **Cluster** (class)
  - Path: `Cluster.cs`
  - Public class implementation

- **ClusterListHelper** (class)
  - Path: `Cluster.cs`
  - Public class implementation

- **Comparison** (class)
  - Path: `Difference.cs`
  - Public class implementation

- **DiffLocation** (class)
  - Path: `DiffLocation.cs`
  - Public class implementation

- **Difference** (class)
  - Path: `Difference.cs`
  - Public class implementation

- **DifferenceList** (class)
  - Path: `Difference.cs`
  - Public class implementation

- **ImportStyleProxy** (class)
  - Path: `ImportStyleProxy.cs`
  - Public class implementation

- **ImportedBooks** (class)
  - Path: `ImportedBooks.cs`
  - Public class implementation

- **OverlapInfo** (class)
  - Path: `Cluster.cs`
  - Public class implementation

- **ParaCorrelationInfo** (class)
  - Path: `BookMerger.cs`
  - Public class implementation

- **ParaCorrelationInfoSorter** (class)
  - Path: `BookMerger.cs`
  - Public class implementation

- **ParatextImportManager** (class)
  - Path: `ParatextImportManager.cs`
  - Public class implementation

- **ReplaceInFilterFixer** (class)
  - Path: `ReplaceInFilterFixer.cs`
  - Public class implementation

- **SCScriptureText** (class)
  - Path: `SCScriptureText.cs`
  - Public class implementation

- **SCTextEnum** (class)
  - Path: `SCTextEnum.cs`
  - Public class implementation

- **ScrAnnotationInfo** (class)
  - Path: `ScrAnnotationInfo.cs`
  - Public class implementation

- **ScrObjWrapper** (class)
  - Path: `ScrObjWrapper.cs`
  - Public class implementation

- **SectionHeadCorrelationHelper** (class)
  - Path: `Cluster.cs`
  - Public class implementation

- **UndoImportManager** (class)
  - Path: `UndoImportManager.cs`
  - Public class implementation

- **BtFootnoteBldrInfo** (struct)
  - Path: `ParatextSfmImporter.cs`

- **ClusterType** (enum)
  - Path: `Cluster.cs`

- **DifferenceType** (enum)
  - Path: `Difference.cs`

## References

- **Project files**: ParatextImport.csproj, ParatextImportTests.csproj
- **Target frameworks**: net462
- **Key C# files**: Cluster.cs, Difference.cs, ImportStyleProxy.cs, ImportedBooks.cs, ParatextImportManager.cs, ReplaceInFilterFixer.cs, SCTextEnum.cs, ScrAnnotationInfo.cs, ScrObjWrapper.cs, UndoImportManager.cs
- **Source file count**: 43 files
- **Data file count**: 2 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\ParatextImport\ParatextImport.csproj
  - Src\ParatextImport\ParatextImportTests\ParatextImportTests.csproj
- Key C# files:
  - Src\ParatextImport\BookMerger.cs
  - Src\ParatextImport\Cluster.cs
  - Src\ParatextImport\DiffLocation.cs
  - Src\ParatextImport\Difference.cs
  - Src\ParatextImport\IBookVersionAgent.cs
  - Src\ParatextImport\ISCScriptureText.cs
  - Src\ParatextImport\ISCTextEnum.cs
  - Src\ParatextImport\ISCTextSegment.cs
  - Src\ParatextImport\ImportStyleProxy.cs
  - Src\ParatextImport\ImportedBooks.cs
  - Src\ParatextImport\ParatextImportExtensions.cs
  - Src\ParatextImport\ParatextImportManager.cs
  - Src\ParatextImport\ParatextImportTests\AutoMergeTests.cs
  - Src\ParatextImport\ParatextImportTests\BookMergerTests.cs
  - Src\ParatextImport\ParatextImportTests\BookMergerTestsBase.cs
  - Src\ParatextImport\ParatextImportTests\ClusterTests.cs
  - Src\ParatextImport\ParatextImportTests\DiffTestHelper.cs
  - Src\ParatextImport\ParatextImportTests\DifferenceTests.cs
  - Src\ParatextImport\ParatextImportTests\ImportTests\ImportStyleProxyTests.cs
  - Src\ParatextImport\ParatextImportTests\ImportTests\ParatextImportBtInterleaved.cs
  - Src\ParatextImport\ParatextImportTests\ImportTests\ParatextImportBtNonInterleaved.cs
  - Src\ParatextImport\ParatextImportTests\ImportTests\ParatextImportManagerTests.cs
  - Src\ParatextImport\ParatextImportTests\ImportTests\ParatextImportNoUi.cs
  - Src\ParatextImport\ParatextImportTests\ImportTests\ParatextImportParatext6Tests.cs
  - Src\ParatextImport\ParatextImportTests\ImportTests\ParatextImportTests.cs
- Data contracts/transforms:
  - Src\ParatextImport\Difference.resx
  - Src\ParatextImport\Properties\Resources.resx
