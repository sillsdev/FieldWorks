---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Utilities

## Purpose
Miscellaneous utilities used across the FieldWorks repository. Contains various standalone tools, helper applications, and utility libraries that don't fit into other specific categories.

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
