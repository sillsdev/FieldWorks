---
last-reviewed: 2025-10-31
last-reviewed-tree: 69fbeb49f36d20492fc9c2122ebc9465c11383be6a10ef3914ebe13cbcadbb21
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Cellar COPILOT summary

## Purpose
Provides XML parsing helpers for FieldWorks-specific XML string representations using the Expat parser. Specifically handles parsing of formatted text strings with runs, text properties (integer-valued, string-valued, and GUID-valued), and embedded objects/pictures. These utilities support the serialization and deserialization of rich text data in FieldWorks' XML format.

## Architecture
C++ native header-only library with inline implementation files. The code is designed to be included into consumer projects rather than built as a standalone library. FwXml.h declares data structures (BasicRunInfo, TextGuidValuedProp, RunPropInfo) and parsing functions. FwXmlString.cpp is designed to be `#include`d in master C++ files and depends on the FwXmlImportData class defined by the consuming code.

## Key Components
- **FwXml.h**: Header declaring XML parsing functions and data structures for formatted strings
  - `BasicRunInfo`: Entry for array of basic run information in formatted strings
  - `TextGuidValuedProp`: GUID-valued text properties (tags, object data)
  - `RunPropInfo`: Property information for text runs
  - `RunDataType`: Enum distinguishing data types (characters, pictures)
  - XML parsing functions: `HandleStringStartTag`, `HandleStringEndTag`, `HandleCharData`
  - Utility functions: `GetAttributeValue`, `ParseGuid`, `BasicType`
- **FwXml.cpp**: Implementation of basic XML parsing utilities (299 lines)
  - `BasicType()`: Binary search mapping of XML element names to field types
  - `GetAttributeValue()`: Attribute extraction from XML element arrays
  - Basic element type table (g_rgbel) mapping XML tags to FieldWorks type codes
- **FwXmlString.cpp**: String property parsing implementation (1414 lines, designed for inclusion)
  - `SetIntegerProperty()`, `SetStringProperty()`, `SetGuidProperty()`: Property management
  - `VerifyDataLength()`: Dynamic buffer management for large strings
  - Formatted string parsing with run-based text properties

## Technology Stack
- C++ native code (no project file; header-only/include-based library)

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
No COM/PInvoke boundaries. Pure native C++ code consumed via `#include` directives by other native C++ components. The FwXmlString.cpp file expects the consuming code to define the FwXmlImportData cla

## Threading & Performance
Thread-agnostic code. No explicit threading, synchronization, or thread-local storage. Parsing operations are stateless utility functions or depend on caller-provided state. Performance-sensitive bina

## Config & Feature Flags
No configuration files or feature flags. Behavior is determined by XML content and caller-provided data structures.

## Build Information
- No standalone project file; this is a header-only library consumed via include paths

## Interfaces and Data Models
BasicRunInfo, TextGuidValuedProp, RunPropInfo.

## Entry Points
- Included via `#include "../Cellar/FwXml.h"` in consumer C++ code (primarily views/Main.h)

## Test Index
No tests found in this folder. Tests may be in consumer projects or separate test assemblies.

## Usage Hints
- Include FwXml.h in C++ code that needs to parse FieldWorks XML formatted strings

## Related Folders
- views/: Primary consumer; includes FwXml.h via Main.h

## References
See `.cache/copilot/diff-plan.json` for file details.
