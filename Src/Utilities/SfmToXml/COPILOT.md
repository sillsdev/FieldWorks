---
last-reviewed: 2025-11-01
last-verified-commit: HEAD
status: production
---

# SfmToXml

## Purpose
SFM to XML conversion library and command-line utility. Parses Standard Format Marker files (legacy Toolbox/Shoebox linguistic data) into XML for FieldWorks import. Includes Sfm2Xml core library (ClsHierarchyEntry, ClsPathObject, ClsInFieldMarker parsing) and ConvertSFM.exe command-line tool. Used by LexTextControls LexImportWizard for lexicon/interlinear imports.

## Key Components

### Sfm2Xml Library (~7K lines)
- **ClsHierarchyEntry**: SFM hierarchy structure representation
- **ClsPathObject**: Path-based SFM navigation
- **ClsInFieldMarker**: Inline marker handling
- **Converter**: Main SFM→XML transformation engine
- **LexImportFields**: ILexImportFields implementation for field mapping
- **AutoFieldInfo**: Automatic field detection
- **ClsLog, WrnErrInfo**: Error/warning logging

### ConvertSFM.exe (~2K lines)
- **Command-line wrapper** for Sfm2Xml library
- Batch SFM file conversion to XML

## Dependencies
- **Consumer**: LexTextControls/LexImportWizard, ParatextImport (USFM), SfmStats
- **System.Xml**: XML generation

## Build Information
- **Projects**: Sfm2Xml.csproj (library), ConvertSFM.csproj (console)
- **Type**: Library + Console (.NET Framework 4.6.2)
- **Namespace**: Sfm2Xml
- **Source files**: 16 files (~9587 lines)

## Test Index
Test project: Sfm2XmlTests. Run via Test Explorer.

## Related Folders
- **LexText/LexTextControls/**: LexImportWizard (main consumer)
- **Utilities/SfmStats/**: SFM analysis tool (uses Sfm2Xml parsing)
- **ParatextImport/**: USFM import (SFM variant)

## References
- **Sfm2Xml.ILexImportFields**: Field mapping interface
- **Sfm2Xml.Converter.Convert()**: Main conversion entry point
- XML generation

## Dependencies
- Depends on: XML utilities, Common utilities
- Used by: Import pipelines and data conversion workflows

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# application/library
- Build via: `dotnet build Sfm2Xml.csproj`
- Data conversion utility

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

## Entry Points
- SFM parsing and XML generation
- Field and hierarchy mapping
- In-field marker processing

## Test Index
Test projects: Sfm2XmlTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Utilities/SfmStats/** - SFM statistics tool (related)
- **LexText/LexTextControls/** - Uses SFM import
- **ParatextImport/** - Paratext SFM data import
- **Utilities/XMLUtils/** - XML utilities

## References

- **Project files**: ConvertSFM.csproj, Sfm2Xml.csproj, Sfm2XmlTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ClsHierarchyEntry.cs, ClsInFieldMarker.cs, Converter.cs, LanguageInfoUI.cs, LexImportField.cs, LexImportFields.cs, Log.cs, Sfm2XmlStrings.Designer.cs, Statics.cs
- **XSLT transforms**: BuildPhase2XSLT.xsl, Phase3.xsl, Phase4.xsl
- **XML data/config**: MoeMap.xml, TestMapping.xml, TestMapping.xml, YiGreenMap.xml
- **Source file count**: 19 files
- **Data file count**: 8 files

## References (auto-generated hints)
- Project files:
  - Utilities/SfmToXml/ConvertSFM/ConvertSFM.csproj
  - Utilities/SfmToXml/Sfm2Xml.csproj
  - Utilities/SfmToXml/Sfm2XmlTests/Sfm2XmlTests.csproj
- Key C# files:
  - Utilities/SfmToXml/AssemblyInfo.cs
  - Utilities/SfmToXml/CRC.cs
  - Utilities/SfmToXml/ClsFieldDescription.cs
  - Utilities/SfmToXml/ClsHierarchyEntry.cs
  - Utilities/SfmToXml/ClsInFieldMarker.cs
  - Utilities/SfmToXml/ClsLanguage.cs
  - Utilities/SfmToXml/ConvertSFM/ConvertSFM.cs
  - Utilities/SfmToXml/Converter.cs
  - Utilities/SfmToXml/FieldHierarchyInfo.cs
  - Utilities/SfmToXml/FileReader.cs
  - Utilities/SfmToXml/LanguageInfoUI.cs
  - Utilities/SfmToXml/LexImportField.cs
  - Utilities/SfmToXml/LexImportFields.cs
  - Utilities/SfmToXml/LexImportOption.cs
  - Utilities/SfmToXml/Log.cs
  - Utilities/SfmToXml/Sfm2XmlStrings.Designer.cs
  - Utilities/SfmToXml/Sfm2XmlTests/ConverterTests.cs
  - Utilities/SfmToXml/Sfm2XmlTests/Properties/AssemblyInfo.cs
  - Utilities/SfmToXml/Statics.cs
- Data contracts/transforms:
  - Utilities/SfmToXml/Sfm2XmlStrings.resx
  - Utilities/SfmToXml/TestData/BuildPhase2XSLT.xsl
  - Utilities/SfmToXml/TestData/MoeMap.xml
  - Utilities/SfmToXml/TestData/Phase3.xsl
  - Utilities/SfmToXml/TestData/Phase4.xsl
  - Utilities/SfmToXml/TestData/TestMapping.xml
  - Utilities/SfmToXml/TestData/YiGreenMap.xml
## Code Evidence
*Analysis based on scanning 17 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 5 public interfaces
- **Namespaces**: ConvertSFM, Sfm2Xml, Sfm2XmlTests
