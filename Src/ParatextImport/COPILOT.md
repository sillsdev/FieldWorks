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
