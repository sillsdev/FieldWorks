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
