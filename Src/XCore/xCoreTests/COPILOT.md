---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# xCoreTests

## Purpose
Test suite for XCore framework functionality.
Provides comprehensive tests validating XCore's command handling, property table behavior,
mediator functionality, and plugin infrastructure. Ensures the foundational framework
behaves correctly as it underpins all FieldWorks applications.

## Architecture
Test project with 2 test files.

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

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# test project
- Build via: `dotnet build xCoreTests.csproj`
- Run tests: `dotnet test xCoreTests.csproj`

## Interfaces and Data Models
See code analysis sections above for key interfaces and data models. Additional interfaces may be documented in source files.

## Entry Points
- Test fixtures for XCore components
- Validation of framework behavior

## Test Index
Test projects: xCoreTests. 2 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Test project. Run tests to validate functionality. See Test Index section for details.

## Related Folders
- **XCore/** - Framework being tested
- **XCore/xCoreInterfaces/** - Interfaces being tested
- **XCore/FlexUIAdapter/** - May have related tests

## References

- **Project files**: xCoreTests.csproj
- **Target frameworks**: net462
- **Key C# files**: IncludeXmlTests.cs, InventoryTests.cs, Resources.Designer.cs
- **XML data/config**: CreateOverrideTestData.xml, IncludeXmlTestSource.xml, IncludeXmlTestSourceB.xml, basicTest.xml, includeTest.xml
- **Source file count**: 3 files
- **Data file count**: 12 files

## References (auto-generated hints)
- Project files:
  - XCore/xCoreTests/BuildInclude.targets
  - XCore/xCoreTests/xCoreTests.csproj
- Key C# files:
  - XCore/xCoreTests/IncludeXmlTests.cs
  - XCore/xCoreTests/InventoryTests.cs
  - XCore/xCoreTests/Properties/Resources.Designer.cs
- Data contracts/transforms:
  - XCore/xCoreTests/CreateOverrideTestData.xml
  - XCore/xCoreTests/IncludeXmlTestSource.xml
  - XCore/xCoreTests/IncludeXmlTestSourceB.xml
  - XCore/xCoreTests/InventoryBaseTestFiles/Base1Layouts.xml
  - XCore/xCoreTests/InventoryBaseTestFiles/Base2Layouts.xml
  - XCore/xCoreTests/InventoryLaterTestFiles/Override1Layouts.xml
  - XCore/xCoreTests/Properties/Resources.resx
  - XCore/xCoreTests/basicTest.xml
  - XCore/xCoreTests/food/fruit/sortOfFruitInclude.xml
  - XCore/xCoreTests/food/veggiesInclude.xml
  - XCore/xCoreTests/food/veggiesIncludeWithSubInclude.xml
  - XCore/xCoreTests/includeTest.xml
## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 3 public classes
- **Namespaces**: XCore
