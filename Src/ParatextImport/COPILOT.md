---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ParatextImport

## Purpose
Import pipeline for Paratext data into FieldWorks. Handles importing Scripture texts, notes, and related data from Paratext projects into the FieldWorks data model.

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
