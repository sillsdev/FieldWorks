<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/Utilities. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


﻿---
last-reviewed: 2025-10-31
last-reviewed-tree: f9667c38932f4933889671a0f084cfb1ab1cbc79e150143423bba28fa564a88c
status: reviewed
---

# Utilities

## Purpose
Organizational parent folder containing 7 utility subfolders: FixFwData (data repair tool), FixFwDataDll (repair library), MessageBoxExLib (enhanced dialogs), Reporting (error reporting), SfmStats (SFM statistics), SfmToXml (Standard Format conversion), and XMLUtils (XML helper library). See individual subfolder COPILOT.md files for detailed documentation.

## Architecture
Organizational parent folder with no direct source files. Contains 7 utility subfolders, each with distinct purpose:
1. **FixFwData/FixFwDataDll**: Data repair tools (WinExe + library)
2. **MessageBoxExLib**: Enhanced dialog library
3. **Reporting**: Error reporting infrastructure
4. **SfmStats**: SFM analysis tool
5. **SfmToXml**: Standard Format converter
6. **XMLUtils**: Core XML utility library

Each subfolder is self-contained with own project files, source, and tests. See individual COPILOT.md files for detailed architecture.

## Key Components
This is an organizational folder. Key components are in subfolders:
- **FixFwData**: WinExe entry point for data repair (Program.cs)
- **FixFwDataDll**: ErrorFixer, FwData, FixErrorsDlg, WriteAllObjectsUtility
- **MessageBoxExLib**: MessageBoxEx, MessageBoxExForm, MessageBoxExManager, MessageBoxExButton
- **Reporting**: ErrorReport, UsageEmailDialog, ReportingStrings
- **SfmStats**: SFM analysis tool (Program.cs, statistics generation)
- **SfmToXml**: Converter, LexImportFields, ClsHierarchyEntry, ConvertSFM tool, Phase3/4 XSLT
- **XMLUtils**: XmlUtils, DynamicLoader, SimpleResolver, SILExceptions, IPersistAsXml

Total: 11 projects, ~52 C# files, 17 data files (XSLT, test data, resources)

## Technology Stack
No direct code at this organizational level. Subfolders use:
- **Languages**: C# (all projects)
- **Target frameworks**: .NET Framework 4.8.x (net48)
- **UI frameworks**: WinForms (FixFwData, FixFwDataDll, MessageBoxExLib, Reporting)
- **Key libraries**:
  - LCModel (FixFwDataDll for data model access)
  - System.Xml (XMLUtils, SfmToXml for XML processing)
  - System.Windows.Forms (UI components)
  - System.Xml.Xsl (XSLT transforms in SfmToXml)
- **Application types**: WinExe (FixFwData, SfmStats), class libraries (others)
- See individual subfolder COPILOT.md files for technology details

## Dependencies
- **Upstream**: Varies by subfolder - LCModel (FixFwDataDll), System.Xml (XMLUtils, SfmToXml), System.Windows.Forms (MessageBoxExLib, FixFwData, Reporting)
- **Downstream consumers**: FixFwData→FixFwDataDll, various apps use MessageBoxExLib/XMLUtils/Reporting as utility libraries, SfmToXml used by import features

## Interop & Contracts
No direct interop at this organizational level. Subfolders provide:
- **FixFwDataDll**: LCModel data repair interfaces (ErrorFixer validates/repairs XML)
- **MessageBoxExLib**: Enhanced MessageBox API (drop-in System.Windows.Forms.MessageBox replacement)
- **Reporting**: Error/usage reporting contracts (ErrorReport dialog, email submission)
- **SfmToXml**: SFM→XML conversion contracts (input: Toolbox files, output: structured XML)
- **XMLUtils**: Core XML contracts (IPersistAsXml, IResolvePath, IAttributeVisitor)
- See individual subfolder COPILOT.md files for interop details

## Threading & Performance
No direct threading at this organizational level. Subfolder characteristics:
- **FixFwData/FixFwDataDll**: UI thread for WinForms, synchronous data validation/repair
- **MessageBoxExLib**: UI thread (WinForms MessageBox replacement), supports timeout timers
- **Reporting**: UI thread for dialogs, async email submission possible
- **SfmStats**: Single-threaded file processing (synchronous)
- **SfmToXml**: Synchronous XSLT transforms, no threading
- **XMLUtils**: Synchronous XML parsing/manipulation, no internal threading
- See individual subfolder COPILOT.md files for performance characteristics

## Config & Feature Flags
No centralized config at this organizational level. Subfolders have:
- **FixFwData**: Command-line flags for data file paths
- **MessageBoxExLib**: Timeout configuration, custom button text
- **Reporting**: Email configuration, crash reporting settings
- **SfmStats**: Command-line options for input file, output format
- **SfmToXml**: Mapping XML files (MoeMap.xml, YiGreenMap.xml), Phase 3/4 XSLT configuration
- **XMLUtils**: Config-driven dynamic loading (DynamicLoader), path resolution
- See individual subfolder COPILOT.md files for configuration details

## Build Information
No direct build at this organizational level. Build via:
- Top-level FieldWorks.sln includes all Utilities subprojects
- Individual subfolders have own .csproj files (11 projects total)
- Outputs: 7 DLLs (libraries), 2 EXEs (FixFwData, SfmStats/ConvertSFM)
- Test projects: MessageBoxExLibTests, Sfm2XmlTests, XMLUtilsTests
- See individual subfolder COPILOT.md files for build details

## Interfaces and Data Models
No interfaces/models at this organizational level. Subfolders define:
- **FixFwDataDll**: FwData (XML data model), ErrorFixer (validation/repair)
- **MessageBoxExLib**: MessageBoxExResult, MessageBoxExButtons, MessageBoxExIcon, TimeoutResult
- **Reporting**: ErrorReport data models, usage feedback models
- **SfmToXml**: LexImportFields, ClsHierarchyEntry (SFM data structures)
- **XMLUtils**:
  - IPersistAsXml: XML serialization contract
  - IResolvePath: Path resolution interface
  - IAttributeVisitor: XML attribute visitor pattern
  - SILExceptions: ConfigurationException, RuntimeConfigurationException
- See individual subfolder COPILOT.md files for interface/model details

## Entry Points
No direct entry points at this organizational level. Subfolder entry points:
- **FixFwData**: `FixFwData.exe` - WinExe for data repair GUI
- **SfmStats**: `SfmStats.exe` - Command-line SFM statistics tool
- **SfmToXml/ConvertSFM**: `ConvertSFM.exe` - Command-line SFM converter
- **Libraries** (consumed programmatically):
  - FixFwDataDll: ErrorFixer.Validate(), ErrorFixer.Fix()
  - MessageBoxExLib: MessageBoxEx.Show()
  - Reporting: ErrorReport.ReportError()
  - SfmToXml: Converter.Convert()
  - XMLUtils: XmlUtils utility methods, DynamicLoader.CreateObject()
- See individual subfolder COPILOT.md files for entry point details

## Test Index
No tests at this organizational level. Test projects in subfolders:
- **MessageBoxExLibTests/MessageBoxExLibTests.csproj**: Tests.cs (MessageBoxEx tests)
- **Sfm2XmlTests/Sfm2XmlTests.csproj**: SFM to XML conversion tests (with test data in TestData/)
- **XMLUtilsTests/XMLUtilsTests.csproj**: DynamicLoaderTests, XmlUtilsTest
- **Test data**: SfmToXml/TestData/ contains:
  - BuildPhase2XSLT.xsl, Phase3.xsl, Phase4.xsl (XSLT transforms)
  - MoeMap.xml, YiGreenMap.xml, TestMapping.xml (mapping files)
- **Test runners**: Visual Studio Test Explorer, `dotnet test`, via FieldWorks.sln
- See individual subfolder COPILOT.md files for test details

## Usage Hints
This is an organizational folder. For usage guidance, see individual subfolder COPILOT.md files:
- **FixFwData/**: How to repair corrupted FLEx XML data files
- **FixFwDataDll/**: ErrorFixer API usage, FixErrorsDlg integration
- **MessageBoxExLib/**: Enhanced MessageBox with custom buttons and timeouts
- **Reporting/**: Error reporting and usage feedback submission
- **SfmStats/**: Analyze Toolbox/SFM files for marker statistics
- **SfmToXml/**: Convert Toolbox/SFM files to XML for import
- **XMLUtils/**: XML utility methods, dynamic loading, path resolution

**Common consumers**:
- FLEx: Uses all utilities (error reporting, MessageBoxEx, XML utils)
- Importers: Use SfmToXml for Toolbox data conversion
- Data repair: FixFwData for XML corruption recovery
- Developers: XMLUtils for XML processing, MessageBoxExLib for enhanced dialogs

## Related Folders
- **MigrateSqlDbs/** - Database migration (related to data repair in FixFwData)
- **ParatextImport/** - May use SfmToXml for Toolbox data import
- **Common/FwUtils/** - Complementary utility library

## References
- **11 project files** across 7 subfolders
- **~52 CS files** total, **17 data files** (XSLT transforms, XML test data, RESX resources)
- See individual subfolder COPILOT.md files for detailed component documentation

## Auto-Generated Project and File References
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
