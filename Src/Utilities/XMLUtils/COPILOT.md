---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Utilities/XMLUtils

## Purpose
XML processing utilities for FieldWorks. Provides common XML handling functionality, dynamic loading, and exception handling for XML operations.

## Key Components
- **XMLUtils.csproj** - XML utilities library
- **DynamicLoader.cs** - Dynamic assembly and plugin loading
- **ResolveDirectory.cs** - Path and directory resolution
- **SILExceptions.cs** - Custom exception types
- **XMLUtilsTests/** - Test suite

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

## Testing
- Run tests: `dotnet test XMLUtils/XMLUtilsTests/`
- Tests cover XML utilities and dynamic loading

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


## References
- **Project Files**: XMLUtils.csproj
- **Key C# Files**: DynamicLoader.cs, ResolveDirectory.cs, SILExceptions.cs, XmlUtils.cs
