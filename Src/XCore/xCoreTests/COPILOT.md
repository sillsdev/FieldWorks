---
last-reviewed: 2025-11-01
last-reviewed-tree: 0b658dd47c2b01012c78f055e17a2666d65671afb218a1dab78c3fcfee0a68a1
status: production
---

# xCoreTests

## Purpose
Test suite for XCore framework. Validates command handling, PropertyTable behavior, Mediator functionality, and plugin infrastructure (Inventory XML processing). Includes IncludeXmlTests (XML include/override directives), InventoryTests (plugin loading), and CreateOverrideTests. Ensures XCore foundation works correctly for all FieldWorks applications.

## Architecture
TBD - populate from code. See auto-generated hints below.

## Key Components

### Test Classes (~500 lines)
- **IncludeXmlTests**: Tests XML `<include>` directive processing in configuration files
  - Validates recursive includes, path resolution, error handling
- **InventoryTests**: Tests Inventory.xml plugin loading and configuration
  - DynamicLoader object creation, assembly loading, parameter passing
- **CreateOverrideTests**: Tests configuration override mechanisms
  - XML override merging, attribute replacement, node insertion

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **XCore/**: Mediator, PropertyTable, Inventory (systems under test)
- **XCore/xCoreInterfaces/**: IxCoreColleague, ChoiceGroup
- **NUnit**: Test framework
- **Consumer**: Build/CI systems

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

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
