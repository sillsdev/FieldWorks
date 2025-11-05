---
last-reviewed: 2025-11-01
last-reviewed-tree: c4dabaab932c5c8839a003cb0c26dfa70f6ee4c1e70cf1f07e62c6558ec001f7
status: production
---

# XMLUtils

## Purpose
XML processing utilities and helper functions. Provides XmlUtils static helpers (GetMandatoryAttributeValue, AppendAttribute, etc.), DynamicLoader for XML-configured assembly loading, SimpleResolver for XML entity resolution, and Configuration exceptions. Used throughout FieldWorks for XML-based configuration, data files, and dynamic plugin loading.

## Architecture
Core XML utility library with 1) XmlUtils (~600 lines) static helpers for XML manipulation, 2) DynamicLoader (~400 lines) for XML-configured object instantiation, 3) Supporting classes (~500 lines) including SimpleResolver, ConfigurationException, IPersistAsXml. Foundation for XML-based configuration and plugin loading across FieldWorks.

## Key Components

### XmlUtils.cs (~600 lines)
- **XmlUtils**: Static XML helper methods
  - GetMandatory/OptionalAttributeValue, AppendAttribute, GetLocalizedAttributeValue
  - FindNode, GetAttributes manipulation
  - XML validation helpers

### DynamicLoader.cs (~400 lines)
- **DynamicLoader**: XML-configured assembly/type loading
  - **CreateObject(XmlNode)**: Instantiates objects from XML assembly/class specifications
  - Supports constructor arguments from XML attributes
  - Used by XCore Inventory for plugin loading

### Supporting Classes (~500 lines)
- **SimpleResolver**: IXmlResourceResolver implementation
- **ConfigurationException, RuntimeConfigurationException**: XML config errors
- **ReplaceSubstringInAttr**: IAttributeVisitor for XML transformation
- **IPersistAsXml, IResolvePath**: Persistence interfaces

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.6.2 (net462)
- **Library type**: Core utility DLL
- **Key libraries**: System.Xml (XmlDocument, XmlNode, XPath), System.Reflection (dynamic loading)
- **Used by**: XCore Inventory, configuration systems, plugin loaders throughout FieldWorks

## Dependencies
- Depends on: System.Xml, Common utilities
- Used by: Many FieldWorks components for XML processing

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build XMLUtils.csproj`
- Includes test suite

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

## Entry Points
- XML utility methods
- Dynamic loader for plugins
- Path resolution utilities
- Custom exceptions

## Test Index
Test projects: XMLUtilsTests. 2 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Utilities/SfmToXml/** - Uses XML utilities
- **Cellar/** - XML serialization using these utilities
- **Transforms/** - XSLT processing with XML utilities
- **FXT/** - Transform tool using XML utilities

## References

- **Project files**: XMLUtils.csproj, XMLUtilsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, DynamicLoader.cs, DynamicLoaderTests.cs, ResolveDirectory.cs, SILExceptions.cs, XmlUtils.cs, XmlUtilsStrings.Designer.cs, XmlUtilsTest.cs
- **Source file count**: 8 files
- **Data file count**: 1 files

## References (auto-generated hints)
- Project files:
  - Utilities/XMLUtils/XMLUtils.csproj
  - Utilities/XMLUtils/XMLUtilsTests/XMLUtilsTests.csproj
- Key C# files:
  - Utilities/XMLUtils/AssemblyInfo.cs
  - Utilities/XMLUtils/DynamicLoader.cs
  - Utilities/XMLUtils/ResolveDirectory.cs
  - Utilities/XMLUtils/SILExceptions.cs
  - Utilities/XMLUtils/XMLUtilsTests/DynamicLoaderTests.cs
  - Utilities/XMLUtils/XMLUtilsTests/XmlUtilsTest.cs
  - Utilities/XMLUtils/XmlUtils.cs
  - Utilities/XMLUtils/XmlUtilsStrings.Designer.cs
- Data contracts/transforms:
  - Utilities/XMLUtils/XmlUtilsStrings.resx
## Code Evidence
*Analysis based on scanning 7 source files*

- **Classes found**: 10 public classes
- **Interfaces found**: 4 public interfaces
- **Namespaces**: SIL.Utils
