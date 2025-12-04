---
last-reviewed: 2025-11-01
last-reviewed-tree: 0b658dd47c2b01012c78f055e17a2666d65671afb218a1dab78c3fcfee0a68a1
status: production
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/XCore/xCoreTests. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# xCoreTests

## Purpose
Test suite for XCore framework. Validates command handling, PropertyTable behavior, Mediator functionality, and plugin infrastructure (Inventory XML processing). Includes IncludeXmlTests (XML include/override directives), InventoryTests (plugin loading), and CreateOverrideTests. Ensures XCore foundation works correctly for all FieldWorks applications.

## Architecture
Test suite (~500 lines) for XCore framework validation. Tests Inventory XML processing (includes, overrides), DynamicLoader plugin instantiation, and configuration merging. Ensures XCore foundation works correctly for all FieldWorks applications using NUnit framework.

## Key Components

### Test Classes (~500 lines)
- **IncludeXmlTests**: Tests XML `<include>` directive processing in configuration files
  - Validates recursive includes, path resolution, error handling
- **InventoryTests**: Tests Inventory.xml plugin loading and configuration
  - DynamicLoader object creation, assembly loading, parameter passing
- **CreateOverrideTests**: Tests configuration override mechanisms
  - XML override merging, attribute replacement, node insertion

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Test framework**: NUnit
- **Systems under test**: Mediator, PropertyTable, Inventory, DynamicLoader
- **Test approach**: Unit tests with mock objects

## Dependencies
- **XCore/**: Mediator, PropertyTable, Inventory (systems under test)
- **XCore/xCoreInterfaces/**: IxCoreColleague, ChoiceGroup
- **NUnit**: Test framework
- **Consumer**: Build/CI systems

## Interop & Contracts
- **IncludeXmlTests**: Tests XML `<include>` directive (recursive includes, path resolution)
- **InventoryTests**: Tests plugin loading (DynamicLoader.CreateObject, assembly loading)
- **CreateOverrideTests**: Tests configuration override merging
- **Test isolation**: Mock objects for Mediator, PropertyTable dependencies

## Threading & Performance
- **Test execution**: Single-threaded NUnit test runner
- **Performance tests**: None (functional correctness only)
- **Test data**: Small XML snippets, mock objects (fast execution)

## Config & Feature Flags
- **Test XML files**: Embedded test data for Inventory/include tests
- **No external config**: All test data in code or embedded resources
- **Test isolation**: Each test independent, no shared state

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
- **Target frameworks**: net48
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
