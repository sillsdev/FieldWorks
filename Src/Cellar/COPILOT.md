---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

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
- Expat XML parser (Include/xmlparse.h)
- Target: Windows native C++ (integrated into consumer projects via include paths)

## Dependencies

### Upstream (consumes)
- **Include/xmlparse.h**: Expat XML parser library (Thai Open Source Software Center Ltd)
- **Kernel**: Low-level infrastructure (referenced as include path in Kernel.vcxproj)
- **Generic**: Generic utilities (referenced as include path)

### Downstream (consumed by)
- **views**: Main consumer via views/Main.h which includes FwXml.h
- **Kernel**: Include search path references Cellar directory
- Any C++ code that needs to parse FieldWorks XML formatted text representations

## Interop & Contracts
No COM/PInvoke boundaries. Pure native C++ code consumed via `#include` directives by other native C++ components. The FwXmlString.cpp file expects the consuming code to define the FwXmlImportData class, creating a compile-time contract between Cellar and its consumers.

## Threading & Performance
Thread-agnostic code. No explicit threading, synchronization, or thread-local storage. Parsing operations are stateless utility functions or depend on caller-provided state. Performance-sensitive binary search for element type lookup (`BasicType()`) and property management.

## Config & Feature Flags
No configuration files or feature flags. Behavior is determined by XML content and caller-provided data structures.

## Build Information
- No standalone project file; this is a header-only library consumed via include paths
- Build using the top-level FW.sln (Visual Studio/MSBuild) or run: `bash ./agent-build-fw.sh`
- Consumer projects (e.g., Kernel, views) reference Cellar via NMakeIncludeSearchPath
- Do not attempt to build Cellar in isolation; it is included directly into consumer C++ projects

## Interfaces and Data Models

- **BasicRunInfo** (FwXml.h)
  - Purpose: Stores starting offset and formatting offset for a text run in formatted strings
  - Inputs: m_ichMin (character offset), m_ibProp (property data offset)
  - Outputs: Used by consumers to track run boundaries and associated formatting

- **TextGuidValuedProp** (FwXml.h)
  - Purpose: Represents GUID-valued text properties (tags or object data)
  - Inputs: m_tpt (property code: kstpTags or kstpObjData), m_chType (subtype), m_vguid (GUID values)
  - Outputs: Property data consumed by formatted string rendering

- **RunPropInfo** (FwXml.h)
  - Purpose: Stores property counts and binary property data for a text run
  - Inputs: m_ctip (int property count), m_ctsp (string property count), m_vbRawProps (binary data)
  - Outputs: Complete property information for a single run

- **XML String Handlers** (FwXml.h)
  - Purpose: Expat-compatible SAX-style handlers for parsing FieldWorks XML strings
  - Inputs: pvUser (user context), pszName (element name), prgpszAtts (attributes), prgch/cch (character data)
  - Outputs: Parsed string data populated into FwXmlImportData structures
  - Notes: Designed for use with Expat's XML_SetElementHandler and XML_SetCharacterDataHandler

- **BasicType element mapping** (FwXml.cpp)
  - Purpose: Maps XML element names to FieldWorks type codes (kcptMultiString, kcptBoolean, kcptInteger, etc.)
  - Inputs: XML element name string
  - Outputs: Integer type code (kcptXxx constants) or -1 if not found
  - Notes: Uses binary search on sorted element table for O(log n) lookup

## Entry Points
- Included via `#include "../Cellar/FwXml.h"` in consumer C++ code (primarily views/Main.h)
- XML parsing functions called by code that deserializes FieldWorks formatted strings
- Expat parser integration via `XML_SetElementHandler`, `XML_SetCharacterDataHandler` callback registration

## Test Index
No tests found in this folder. Tests may be in consumer projects or separate test assemblies.

## Usage Hints
- Include FwXml.h in C++ code that needs to parse FieldWorks XML formatted strings
- FwXmlString.cpp must be `#include`d (not compiled separately) and requires FwXmlImportData class definition
- Use `BasicType()` to map XML element names to FieldWorks type constants
- Use `GetAttributeValue()` to extract attributes from Expat attribute arrays
- Register `HandleStringStartTag`, `HandleStringEndTag`, `HandleCharData` with Expat parser for formatted text

## Related Folders
- **views/**: Primary consumer; includes FwXml.h via Main.h
- **Kernel/**: References Cellar in include search paths
- **Generic/**: Peer low-level utilities folder

## References
- **Project files**: None (header-only library)
- **Key C++ files**: FwXml.cpp, FwXmlString.cpp
- **Key headers**: FwXml.h
- **External dependencies**: Include/xmlparse.h (Expat XML parser)
- **Include search path**: Referenced by Kernel.vcxproj (..\Cellar)
- **Consumer references**: Src/views/Main.h includes "../Cellar/FwXml.h"
- **Total lines of code**: 1800 (299 in FwXml.cpp, 1414 in FwXmlString.cpp, 87 in FwXml.h)
