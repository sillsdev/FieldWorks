---
last-reviewed: 2025-10-31
last-verified-commit: 677b8f2
status: reviewed
---

# Utilities

## Purpose
Organizational parent folder containing 7 utility subfolders: FixFwData (data repair tool), FixFwDataDll (repair library), MessageBoxExLib (enhanced dialogs), Reporting (error reporting), SfmStats (SFM statistics), SfmToXml (Standard Format conversion), and XMLUtils (XML helper library). See individual subfolder COPILOT.md files for detailed documentation.

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

## Dependencies
- **Upstream**: Varies by subfolder - LCModel (FixFwDataDll), System.Xml (XMLUtils, SfmToXml), System.Windows.Forms (MessageBoxExLib, FixFwData, Reporting)
- **Downstream consumers**: FixFwData→FixFwDataDll, various apps use MessageBoxExLib/XMLUtils/Reporting as utility libraries, SfmToXml used by import features

## Test Infrastructure
- **MessageBoxExLibTests/** - Tests for MessageBoxExLib
- **Sfm2XmlTests/** - Tests for SfmToXml library
- **XMLUtilsTests/** - Tests for XMLUtils (DynamicLoaderTests, XmlUtilsTest)

## Related Folders
- **MigrateSqlDbs/** - Database migration (related to data repair in FixFwData)
- **ParatextImport/** - May use SfmToXml for Toolbox data import
- **Common/FwUtils/** - Complementary utility library

## References
- **11 project files** across 7 subfolders
- **~52 CS files** total, **17 data files** (XSLT, XML test data, .resx)
- See individual subfolder COPILOT.md files for detailed component documentation
