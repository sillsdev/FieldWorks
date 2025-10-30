---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# XMLUtils

## Purpose
XML processing utilities and helper functions. 
Provides common XML handling functionality including dynamic assembly loading (DynamicLoader), 
exception handling, validation, and XML manipulation helpers. Supports XML-based configuration 
and data processing throughout FieldWorks.

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

## Technology Stack
- C# .NET
- XML processing
- Dynamic loading and reflection

## Dependencies
- Depends on: System.Xml, Common utilities
- Used by: Many FieldWorks components for XML processing

## Build Information
- C# class library project
- Build via: `dotnet build XMLUtils.csproj`
- Includes test suite

## Entry Points
- XML utility methods
- Dynamic loader for plugins
- Path resolution utilities
- Custom exceptions

## Related Folders
- **Utilities/SfmToXml/** - Uses XML utilities
- **Cellar/** - XML serialization using these utilities
- **Transforms/** - XSLT processing with XML utilities
- **FXT/** - Transform tool using XML utilities

## Code Evidence
*Analysis based on scanning 7 source files*

- **Classes found**: 10 public classes
- **Interfaces found**: 4 public interfaces
- **Namespaces**: SIL.Utils

## Interfaces and Data Models

- **IAttributeVisitor** (interface)
  - Path: `XmlUtils.cs`
  - Public interface definition

- **IPersistAsXml** (interface)
  - Path: `DynamicLoader.cs`
  - Public interface definition

- **IResolvePath** (interface)
  - Path: `ResolveDirectory.cs`
  - Public interface definition

- **ConfigurationException** (class)
  - Path: `SILExceptions.cs`
  - Public class implementation

- **DynamicLoader** (class)
  - Path: `DynamicLoader.cs`
  - Public class implementation

- **ReplaceSubstringInAttr** (class)
  - Path: `XmlUtils.cs`
  - Public class implementation

- **RuntimeConfigurationException** (class)
  - Path: `SILExceptions.cs`
  - Public class implementation

- **SimpleResolver** (class)
  - Path: `ResolveDirectory.cs`
  - Public class implementation

- **XmlUtils** (class)
  - Path: `XmlUtils.cs`
  - Public class implementation

## References

- **Project files**: XMLUtils.csproj, XMLUtilsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, DynamicLoader.cs, DynamicLoaderTests.cs, ResolveDirectory.cs, SILExceptions.cs, XmlUtils.cs, XmlUtilsStrings.Designer.cs, XmlUtilsTest.cs
- **Source file count**: 8 files
- **Data file count**: 1 files
