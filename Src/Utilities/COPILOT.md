---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Utilities

## Purpose
Miscellaneous utilities and standalone tools.
Contains data repair utilities (FixFwData, FixFwDataDll), format conversion tools (SfmToXml, SfmStats),
XML helpers (XMLUtils), error reporting (Reporting), and enhanced UI components (MessageBoxExLib).
Collection of helper applications and libraries that support but don't fit cleanly into other categories.

## Key Components
### Key Classes
- **XmlUtils**
- **ReplaceSubstringInAttr**
- **SimpleResolver**
- **ConfigurationException**
- **RuntimeConfigurationException**
- **DynamicLoader**
- **DynamicLoaderTests**
- **Test1**
- **XmlUtilsTest**
- **XmlResourceResolverTests**

### Key Interfaces
- **IAttributeVisitor**
- **IResolvePath**
- **IPersistAsXml**
- **ITest1**
- **ILexImportFields**
- **ILexImportField**
- **ILexImportCustomField**
- **ILanguageInfoUI**

## Technology Stack
- C# .NET
- Various utility functions
- Data repair and validation
- XML processing

## Dependencies
- Varies by subproject
- Generally depends on: Common utilities, Cellar (for data access)
- Used by: Various components needing utility functionality

## Build Information
- Multiple C# projects in subfolders
- Mix of executables and libraries
- Build with MSBuild or Visual Studio

## Entry Points
- **FixFwData** - Command-line or GUI tool for data repair
- **XMLUtils** - Library for XML operations
- **MessageBoxExLib** - Enhanced dialog library

## Related Folders
- **Cellar/** - Data model that FixFwData works with
- **Common/** - Shared utilities that Utilities extends
- **MigrateSqlDbs/** - Database migration (related to data repair)

## Code Evidence
*Analysis based on scanning 43 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 9 public interfaces
- **Namespaces**: ConvertSFM, FixFwData, SIL.FieldWorks.FixData, SIL.Utils, Sfm2Xml

## Interfaces and Data Models

- **IAttributeVisitor** (interface)
  - Path: `XMLUtils/XmlUtils.cs`
  - Public interface definition

- **ILanguageInfoUI** (interface)
  - Path: `SfmToXml/LanguageInfoUI.cs`
  - Public interface definition

- **ILexImportCustomField** (interface)
  - Path: `SfmToXml/LexImportField.cs`
  - Public interface definition

- **ILexImportField** (interface)
  - Path: `SfmToXml/LexImportField.cs`
  - Public interface definition

- **ILexImportFields** (interface)
  - Path: `SfmToXml/LexImportFields.cs`
  - Public interface definition

- **ILexImportOption** (interface)
  - Path: `SfmToXml/LexImportOption.cs`
  - Public interface definition

- **IPersistAsXml** (interface)
  - Path: `XMLUtils/DynamicLoader.cs`
  - Public interface definition

- **IResolvePath** (interface)
  - Path: `XMLUtils/ResolveDirectory.cs`
  - Public interface definition

- **ClsHierarchyEntry** (class)
  - Path: `SfmToXml/ClsHierarchyEntry.cs`
  - Public class implementation

- **ClsInFieldMarker** (class)
  - Path: `SfmToXml/ClsInFieldMarker.cs`
  - Public class implementation

- **ClsLog** (class)
  - Path: `SfmToXml/Log.cs`
  - Public class implementation

- **ClsPathObject** (class)
  - Path: `SfmToXml/Converter.cs`
  - Public class implementation

- **ConfigurationException** (class)
  - Path: `XMLUtils/SILExceptions.cs`
  - Public class implementation

- **Converter** (class)
  - Path: `SfmToXml/Converter.cs`
  - Public class implementation

- **DynamicLoader** (class)
  - Path: `XMLUtils/DynamicLoader.cs`
  - Public class implementation

- **ErrorFixer** (class)
  - Path: `FixFwDataDll/ErrorFixer.cs`
  - Public class implementation

- **FwData** (class)
  - Path: `FixFwDataDll/FwData.cs`
  - Public class implementation

- **LexImportFields** (class)
  - Path: `SfmToXml/LexImportFields.cs`
  - Public class implementation

- **MessageBoxEx** (class)
  - Path: `MessageBoxExLib/MessageBoxEx.cs`
  - Public class implementation

- **MessageBoxExButton** (class)
  - Path: `MessageBoxExLib/MessageBoxExButton.cs`
  - Public class implementation

- **MessageBoxExManager** (class)
  - Path: `MessageBoxExLib/MessageBoxExManager.cs`
  - Public class implementation

- **ReplaceSubstringInAttr** (class)
  - Path: `XMLUtils/XmlUtils.cs`
  - Public class implementation

- **RuntimeConfigurationException** (class)
  - Path: `XMLUtils/SILExceptions.cs`
  - Public class implementation

- **SfmData** (class)
  - Path: `SfmToXml/Log.cs`
  - Public class implementation

- **SimpleResolver** (class)
  - Path: `XMLUtils/ResolveDirectory.cs`
  - Public class implementation

- **WriteAllObjectsUtility** (class)
  - Path: `FixFwDataDll/WriteAllObjectsUtility.cs`
  - Public class implementation

- **WrnErrInfo** (class)
  - Path: `SfmToXml/Log.cs`
  - Public class implementation

- **XmlUtils** (class)
  - Path: `XMLUtils/XmlUtils.cs`
  - Public class implementation

- **MessageBoxExResult** (struct)
  - Path: `MessageBoxExLib/MessageBoxExResult.cs`

- **MessageBoxExButtons** (enum)
  - Path: `MessageBoxExLib/MessageBoxExButtons.cs`

- **MessageBoxExIcon** (enum)
  - Path: `MessageBoxExLib/MessageBoxExIcon.cs`

- **MultiToWideError** (enum)
  - Path: `SfmToXml/Converter.cs`

- **TimeoutResult** (enum)
  - Path: `MessageBoxExLib/TimeoutResult.cs`

## References

- **Project files**: ConvertSFM.csproj, FixFwData.csproj, FixFwDataDll.csproj, MessageBoxExLib.csproj, MessageBoxExLibTests.csproj, Reporting.csproj, Sfm2Xml.csproj, Sfm2XmlTests.csproj, SfmStats.csproj, XMLUtils.csproj, XMLUtilsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, AssemblyInfo.cs, DynamicLoader.cs, MessageBoxExButtons.cs, Program.cs, Program.cs, ResolveDirectory.cs, SILExceptions.cs, XmlUtils.cs, XmlUtilsStrings.Designer.cs
- **XSLT transforms**: BuildPhase2XSLT.xsl, Phase3.xsl, Phase4.xsl
- **XML data/config**: MoeMap.xml, TestMapping.xml, TestMapping.xml, YiGreenMap.xml
- **Source file count**: 52 files
- **Data file count**: 17 files

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
  - Src\Utilities\FixFwDataDll\FixFwDataDll.csproj
  - Src\Utilities\FixFwData\FixFwData.csproj
  - Src\Utilities\MessageBoxExLib\MessageBoxExLib.csproj
  - Src\Utilities\MessageBoxExLib\MessageBoxExLibTests\MessageBoxExLibTests.csproj
  - Src\Utilities\Reporting\Reporting.csproj
  - Src\Utilities\SfmStats\SfmStats.csproj
  - Src\Utilities\SfmToXml\ConvertSFM\ConvertSFM.csproj
  - Src\Utilities\SfmToXml\Sfm2Xml.csproj
  - Src\Utilities\SfmToXml\Sfm2XmlTests\Sfm2XmlTests.csproj
  - Src\Utilities\XMLUtils\XMLUtils.csproj
  - Src\Utilities\XMLUtils\XMLUtilsTests\XMLUtilsTests.csproj
- Key C# files:
  - Src\Utilities\FixFwDataDll\ErrorFixer.cs
  - Src\Utilities\FixFwDataDll\FixErrorsDlg.Designer.cs
  - Src\Utilities\FixFwDataDll\FixErrorsDlg.cs
  - Src\Utilities\FixFwDataDll\FwData.cs
  - Src\Utilities\FixFwDataDll\Properties\AssemblyInfo.cs
  - Src\Utilities\FixFwDataDll\Strings.Designer.cs
  - Src\Utilities\FixFwDataDll\WriteAllObjectsUtility.cs
  - Src\Utilities\FixFwData\Program.cs
  - Src\Utilities\FixFwData\Properties\AssemblyInfo.cs
  - Src\Utilities\MessageBoxExLib\AssemblyInfo.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxEx.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxExButton.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxExButtons.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxExForm.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxExIcon.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxExLibTests\Tests.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxExManager.cs
  - Src\Utilities\MessageBoxExLib\MessageBoxExResult.cs
  - Src\Utilities\MessageBoxExLib\TimeoutResult.cs
  - Src\Utilities\Reporting\AssemblyInfo.cs
  - Src\Utilities\Reporting\ErrorReport.cs
  - Src\Utilities\Reporting\ReportingStrings.Designer.cs
  - Src\Utilities\Reporting\UsageEmailDialog.cs
  - Src\Utilities\SfmStats\Program.cs
  - Src\Utilities\SfmStats\Properties\AssemblyInfo.cs
- Data contracts/transforms:
  - Src\Utilities\FixFwDataDll\FixErrorsDlg.resx
  - Src\Utilities\FixFwDataDll\Strings.resx
  - Src\Utilities\MessageBoxExLib\MessageBoxExForm.resx
  - Src\Utilities\MessageBoxExLib\Resources\StandardButtonsText.resx
  - Src\Utilities\Reporting\App.config
  - Src\Utilities\Reporting\ErrorReport.resx
  - Src\Utilities\Reporting\ReportingStrings.resx
  - Src\Utilities\Reporting\UsageEmailDialog.resx
  - Src\Utilities\SfmToXml\Sfm2XmlStrings.resx
  - Src\Utilities\SfmToXml\TestData\BuildPhase2XSLT.xsl
  - Src\Utilities\SfmToXml\TestData\MoeMap.xml
  - Src\Utilities\SfmToXml\TestData\Phase3.xsl
  - Src\Utilities\SfmToXml\TestData\Phase4.xsl
  - Src\Utilities\SfmToXml\TestData\TestMapping.xml
  - Src\Utilities\SfmToXml\TestData\YiGreenMap.xml
  - Src\Utilities\XMLUtils\XmlUtilsStrings.resx
