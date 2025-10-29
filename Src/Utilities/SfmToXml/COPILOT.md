---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# SfmToXml

## Purpose
SFM to XML data conversion utility and library.
Converts Standard Format Marker files (legacy linguistic data format) into XML format
for processing and import into FieldWorks. Handles parsing of SFM structure and mapping
to XML representation while preserving data semantics.

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

## Interfaces and Data Models

- **ILanguageInfoUI** (interface)
  - Path: `LanguageInfoUI.cs`
  - Public interface definition

- **ILexImportCustomField** (interface)
  - Path: `LexImportField.cs`
  - Public interface definition

- **ILexImportField** (interface)
  - Path: `LexImportField.cs`
  - Public interface definition

- **ILexImportFields** (interface)
  - Path: `LexImportFields.cs`
  - Public interface definition

- **ILexImportOption** (interface)
  - Path: `LexImportOption.cs`
  - Public interface definition

- **AutoFieldInfo** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **CRC** (class)
  - Path: `CRC.cs`
  - Public class implementation

- **ClsHierarchyEntry** (class)
  - Path: `ClsHierarchyEntry.cs`
  - Public class implementation

- **ClsInFieldMarker** (class)
  - Path: `ClsInFieldMarker.cs`
  - Public class implementation

- **ClsLog** (class)
  - Path: `Log.cs`
  - Public class implementation

- **ClsPathObject** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **Converter** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **DP** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **ImportObject** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **ImportObjectManager** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **LanguageInfoUI** (class)
  - Path: `LanguageInfoUI.cs`
  - Public class implementation

- **LexImportCustomField** (class)
  - Path: `LexImportField.cs`
  - Public class implementation

- **LexImportField** (class)
  - Path: `LexImportField.cs`
  - Public class implementation

- **LexImportFields** (class)
  - Path: `LexImportFields.cs`
  - Public class implementation

- **LexImportOption** (class)
  - Path: `LexImportOption.cs`
  - Public class implementation

- **STATICS** (class)
  - Path: `Statics.cs`
  - Public class implementation

- **SfmData** (class)
  - Path: `Log.cs`
  - Public class implementation

- **Tree** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **TreeNode** (class)
  - Path: `Converter.cs`
  - Public class implementation

- **WrnErrInfo** (class)
  - Path: `Log.cs`
  - Public class implementation

- **FollowedByInfo** (struct)
  - Path: `FileReader.cs`

- **MultiToWideError** (enum)
  - Path: `Converter.cs`

- **BuildPhase2XSLT** (xslt)
  - Path: `TestData/BuildPhase2XSLT.xsl`
  - XSLT transformation template

- **Phase3** (xslt)
  - Path: `TestData/Phase3.xsl`
  - XSLT transformation template

- **Phase4** (xslt)
  - Path: `TestData/Phase4.xsl`
  - XSLT transformation template

## References

- **Project files**: ConvertSFM.csproj, Sfm2Xml.csproj, Sfm2XmlTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ClsHierarchyEntry.cs, ClsInFieldMarker.cs, Converter.cs, LanguageInfoUI.cs, LexImportField.cs, LexImportFields.cs, Log.cs, Sfm2XmlStrings.Designer.cs, Statics.cs
- **XSLT transforms**: BuildPhase2XSLT.xsl, Phase3.xsl, Phase4.xsl
- **XML data/config**: MoeMap.xml, TestMapping.xml, TestMapping.xml, YiGreenMap.xml
- **Source file count**: 19 files
- **Data file count**: 8 files
