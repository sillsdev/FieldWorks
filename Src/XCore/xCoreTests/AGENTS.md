---
last-reviewed: 2025-11-01
last-reviewed-tree: d968a8ce215a359b1d1995cfc33c1f1f08069c660b255ed248c9697b69379a11
status: production
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
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
Language - C#

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- IncludeXmlTests: Tests XML `<include>` directive (recursive includes, path resolution)

## Threading & Performance
- Test execution: Single-threaded NUnit test runner

## Config & Feature Flags
- Test XML files: Embedded test data for Inventory/include tests

## Build Information
- C# test project

## Interfaces and Data Models
See Key Components section above.

## Entry Points
- Test fixtures for XCore components

## Test Index
Test projects: xCoreTests. 2 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Test project. Run tests to validate functionality. See Test Index section for details.

## Related Folders
- XCore/ - Framework being tested

## References
See `.cache/copilot/diff-plan.json` for file details.

## Code Evidence
*Analysis based on scanning 2 source files*
