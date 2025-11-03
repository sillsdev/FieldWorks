---
last-reviewed: 2025-10-31
last-reviewed-tree: f9667c38932f4933889671a0f084cfb1ab1cbc79e150143423bba28fa564a88c
status: reviewed
---

# Utilities

## Purpose
Organizational parent folder containing 7 utility subfolders: FixFwData (data repair tool), FixFwDataDll (repair library), MessageBoxExLib (enhanced dialogs), Reporting (error reporting), SfmStats (SFM statistics), SfmToXml (Standard Format conversion), and XMLUtils (XML helper library). See individual subfolder COPILOT.md files for detailed documentation.

## Architecture
TBD - populate from code. See auto-generated hints below.

## Key Components
TBD - populate from code. See auto-generated hints below.

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **Upstream**: Varies by subfolder - LCModel (FixFwDataDll), System.Xml (XMLUtils, SfmToXml), System.Windows.Forms (MessageBoxExLib, FixFwData, Reporting)
- **Downstream consumers**: FixFwData→FixFwDataDll, various apps use MessageBoxExLib/XMLUtils/Reporting as utility libraries, SfmToXml used by import features

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
TBD - populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **MigrateSqlDbs/** - Database migration (related to data repair in FixFwData)
- **ParatextImport/** - May use SfmToXml for Toolbox data import
- **Common/FwUtils/** - Complementary utility library

## References
- **11 project files** across 7 subfolders
- **~52 CS files** total, **17 data files** (XSLT transforms, XML test data, RESX resources)
- See individual subfolder COPILOT.md files for detailed component documentation

## References (auto-generated hints)
- Project files:
  - Src/Utilities/FixFwData/FixFwData.csproj
  - Src/Utilities/FixFwDataDll/FixFwDataDll.csproj
  - Src/Utilities/MessageBoxExLib/MessageBoxExLib.csproj
  - Src/Utilities/MessageBoxExLib/MessageBoxExLibTests/MessageBoxExLibTests.csproj
  - Src/Utilities/Reporting/Reporting.csproj
  - Src/Utilities/SfmStats/SfmStats.csproj
  - Src/Utilities/SfmToXml/ConvertSFM/ConvertSFM.csproj
  - Src/Utilities/SfmToXml/Sfm2Xml.csproj
  - Src/Utilities/SfmToXml/Sfm2XmlTests/Sfm2XmlTests.csproj
  - Src/Utilities/XMLUtils/XMLUtils.csproj
  - Src/Utilities/XMLUtils/XMLUtilsTests/XMLUtilsTests.csproj
- Key C# files:
  - Src/Utilities/FixFwData/Program.cs
  - Src/Utilities/FixFwData/Properties/AssemblyInfo.cs
  - Src/Utilities/FixFwDataDll/ErrorFixer.cs
  - Src/Utilities/FixFwDataDll/FixErrorsDlg.Designer.cs
  - Src/Utilities/FixFwDataDll/FixErrorsDlg.cs
  - Src/Utilities/FixFwDataDll/FwData.cs
  - Src/Utilities/FixFwDataDll/Properties/AssemblyInfo.cs
  - Src/Utilities/FixFwDataDll/Strings.Designer.cs
  - Src/Utilities/FixFwDataDll/WriteAllObjectsUtility.cs
  - Src/Utilities/MessageBoxExLib/AssemblyInfo.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxEx.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxExButton.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxExButtons.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxExForm.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxExIcon.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxExLibTests/Tests.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxExManager.cs
  - Src/Utilities/MessageBoxExLib/MessageBoxExResult.cs
  - Src/Utilities/MessageBoxExLib/TimeoutResult.cs
  - Src/Utilities/Reporting/AssemblyInfo.cs
  - Src/Utilities/Reporting/ErrorReport.cs
  - Src/Utilities/Reporting/ReportingStrings.Designer.cs
  - Src/Utilities/Reporting/UsageEmailDialog.cs
  - Src/Utilities/SfmStats/Program.cs
  - Src/Utilities/SfmStats/Properties/AssemblyInfo.cs
- Data contracts/transforms:
  - Src/Utilities/FixFwDataDll/FixErrorsDlg.resx
  - Src/Utilities/FixFwDataDll/Strings.resx
  - Src/Utilities/MessageBoxExLib/MessageBoxExForm.resx
  - Src/Utilities/MessageBoxExLib/Resources/StandardButtonsText.resx
  - Src/Utilities/Reporting/App.config
  - Src/Utilities/Reporting/ErrorReport.resx
  - Src/Utilities/Reporting/ReportingStrings.resx
  - Src/Utilities/Reporting/UsageEmailDialog.resx
  - Src/Utilities/SfmToXml/Sfm2XmlStrings.resx
  - Src/Utilities/SfmToXml/TestData/BuildPhase2XSLT.xsl
  - Src/Utilities/SfmToXml/TestData/MoeMap.xml
  - Src/Utilities/SfmToXml/TestData/Phase3.xsl
  - Src/Utilities/SfmToXml/TestData/Phase4.xsl
  - Src/Utilities/SfmToXml/TestData/TestMapping.xml
  - Src/Utilities/SfmToXml/TestData/YiGreenMap.xml
  - Src/Utilities/XMLUtils/XmlUtilsStrings.resx
## Subfolders

### FixFwData/
Command-line WinExe entry point for FixFwDataDll repair functionality. Launches FixErrorsDlg GUI for identifying and fixing XML data file corruption. See **FixFwData/COPILOT.md**.

### FixFwDataDll/
Library implementing FwData XML validation and ErrorFixer repair logic. Contains FixErrorsDlg WinForms dialog, WriteAllObjectsUtility, and error detection/fixing algorithms. See **FixFwDataDll/COPILOT.md**.

### MessageBoxExLib/
Enhanced MessageBox replacement with custom buttons, timeout support, and manager pattern (MessageBoxExManager). Provides MessageBoxEx static class, MessageBoxExForm, MessageBoxExButton. See **MessageBoxExLib/COPILOT.md**.

### Reporting/
Error reporting infrastructure with ErrorReport dialog and UsageEmailDialog. Supports crash reporting and usage feedback submission. See **Reporting/COPILOT.md**.

### SfmStats/
Command-line tool for analyzing Standard Format Marker (SFM) files. Generates statistics on marker usage, frequency, and structure. See **SfmStats/COPILOT.md**.

### SfmToXml/
Library and command-line tool (ConvertSFM) for converting SFM/Toolbox data to XML. Contains Converter class, LexImportFields, ClsHierarchyEntry, Phase3/Phase4 XSLT transforms. Includes Sfm2Xml library and Sfm2XmlTests. See **SfmToXml/COPILOT.md**.

### XMLUtils/
Core XML utility library with XmlUtils static class, DynamicLoader, SimpleResolver, and SILExceptions (ConfigurationException, RuntimeConfigurationException). Provides IAttributeVisitor, IResolvePath, IPersistAsXml interfaces. See **XMLUtils/COPILOT.md**.

## Test Infrastructure
- **MessageBoxExLibTests/** - Tests for MessageBoxExLib
- **Sfm2XmlTests/** - Tests for SfmToXml library
- **XMLUtilsTests/** - Tests for XMLUtils (DynamicLoaderTests, XmlUtilsTest)
