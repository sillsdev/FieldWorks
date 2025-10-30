---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# XMLUtils

## Purpose
XML processing utilities for FieldWorks. Provides common XML handling functionality, dynamic loading, and exception handling for XML operations.

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
