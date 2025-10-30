---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# xCoreTests

## Purpose
Test suite for XCore framework functionality. 
Provides comprehensive tests validating XCore's command handling, property table behavior, 
mediator functionality, and plugin infrastructure. Ensures the foundational framework 
behaves correctly as it underpins all FieldWorks applications.

## Key Components
### Key Classes
- **IncludeXmlTests**
- **InventoryTests**
- **CreateOverrideTests**

## Technology Stack
- C# .NET with NUnit or similar
- Unit testing framework
- XML test data

## Dependencies
- Depends on: XCore (framework being tested), XCore/xCoreInterfaces
- Used by: Build and CI systems for validation

## Build Information
- C# test project
- Build via: `dotnet build xCoreTests.csproj`
- Run tests: `dotnet test xCoreTests.csproj`

## Entry Points
- Test fixtures for XCore components
- Validation of framework behavior

## Related Folders
- **XCore/** - Framework being tested
- **XCore/xCoreInterfaces/** - Interfaces being tested
- **XCore/FlexUIAdapter/** - May have related tests

## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 3 public classes
- **Namespaces**: XCore

## References

- **Project files**: xCoreTests.csproj
- **Target frameworks**: net462
- **Key C# files**: IncludeXmlTests.cs, InventoryTests.cs, Resources.Designer.cs
- **XML data/config**: CreateOverrideTestData.xml, IncludeXmlTestSource.xml, IncludeXmlTestSourceB.xml, basicTest.xml, includeTest.xml
- **Source file count**: 3 files
- **Data file count**: 12 files
