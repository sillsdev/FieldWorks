---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Utilities/SfmToXml

## Purpose
Standard Format Marker (SFM) to XML conversion utility. Converts SFM-formatted data files to XML format for processing and import into FieldWorks.

## Key Components
- **Sfm2Xml.csproj** - SFM to XML converter
- **ClsFieldDescription.cs** - Field definition classes
- **ClsHierarchyEntry.cs** - Hierarchical structure handling
- **ClsInFieldMarker.cs** - In-field marker processing
- **CRC.cs** - Checksum utilities


## Key Classes/Interfaces
- **ILexImportFields**
- **LexImportFields**
- **SfmData**
- **WrnErrInfo**
- **ClsLog**
- **ClsInFieldMarker**
- **ClsHierarchyEntry**
- **ClsPathObject**

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


## References
- **Project Files**: Sfm2Xml.csproj
- **Key C# Files**: CRC.cs, ClsFieldDescription.cs, ClsHierarchyEntry.cs, ClsInFieldMarker.cs, ClsLanguage.cs, Converter.cs, FieldHierarchyInfo.cs, FileReader.cs
