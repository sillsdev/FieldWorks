---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# SfmToXml

## Purpose
Standard Format Marker (SFM) to XML conversion utility. Converts SFM-formatted data files to XML format for processing and import into FieldWorks.

## Key Components
### Key Classes
- **LexImportFields**
- **SfmData**
- **WrnErrInfo**
- **ClsLog**
- **ClsInFieldMarker**
- **ClsHierarchyEntry**
- **ClsPathObject**
- **Converter**
- **AutoFieldInfo**
- **DP**

### Key Interfaces
- **ILexImportFields**
- **ILexImportField**
- **ILexImportCustomField**
- **ILanguageInfoUI**
- **ILexImportOption**

## Technology Stack
- C# .NET
- SFM parsing and processing
- XML generation

## Dependencies
- Depends on: XML utilities, Common utilities
- Used by: Import pipelines and data conversion workflows

## Build Information
- C# application/library
- Build via: `dotnet build Sfm2Xml.csproj`
- Data conversion utility

## Entry Points
- SFM parsing and XML generation
- Field and hierarchy mapping
- In-field marker processing

## Related Folders
- **Utilities/SfmStats/** - SFM statistics tool (related)
- **LexText/LexTextControls/** - Uses SFM import
- **ParatextImport/** - Paratext SFM data import
- **Utilities/XMLUtils/** - XML utilities

## Code Evidence
*Analysis based on scanning 17 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 5 public interfaces
- **Namespaces**: ConvertSFM, Sfm2Xml, Sfm2XmlTests
