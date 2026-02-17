---
last-reviewed: 2025-11-01
last-reviewed-tree: 45264aa52a130d0ada04e62bc7c52a5fca0e5e5cc7994855047cd3f4b2067c7e
status: production
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

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
Language - C#

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project

## Interfaces and Data Models
IAttributeVisitor, IPersistAsXml, IResolvePath, ConfigurationException, DynamicLoader, ReplaceSubstringInAttr, RuntimeConfigurationException, SimpleResolver, XmlUtils.

## Entry Points
- XML utility methods

## Test Index
Test projects: XMLUtilsTests. 2 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- Utilities/SfmToXml/ - Uses XML utilities

## References
See `.cache/copilot/diff-plan.json` for file details.

## Code Evidence
*Analysis based on scanning 7 source files*
