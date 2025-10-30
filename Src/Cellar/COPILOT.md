---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Cellar

## Purpose
Core data model infrastructure providing XML processing and serialization services.
Contains FwXml and FwXmlString utilities for handling XML data structures used throughout FieldWorks.
While much of the core data model (LCM) has moved to dedicated libraries, this folder maintains
fundamental XML handling and low-level data utilities that support the persistence layer.

## Key Components
No major public classes identified.

## Technology Stack
- C++ native code
- XML processing and serialization
- Data model persistence

## Dependencies
- Depends on: Kernel (low-level infrastructure), Generic (utilities)
- Used by: All data-driven components across FieldWorks (FdoUi, LexText, xWorks)

## Build Information
- Build using the top-level FW.sln (Visual Studio/MSBuild) or run: `bash ./agent-build-fw.sh`
- Avoid building this project in isolation; the solution load ensures repo props/targets and interop settings are applied.
-

## Entry Points
- Provides data model base classes and XML serialization
- Core persistence layer for all FieldWorks data

## Related Folders
- **DbExtend/** - Extends database schema and data model capabilities
- **CacheLight/** - Provides caching for data model objects
- **FdoUi/** - UI components for FieldWorks Data Objects built on Cellar
- **LCMBrowser/** - Browser tool for exploring the LCM data model
- **Common/FieldWorks/** - Contains higher-level data access built on Cellar

## Code Evidence
*Analysis based on scanning 3 source files*

## References

- **Key C++ files**: FwXml.cpp, FwXmlString.cpp
- **Key headers**: FwXml.h
- **Source file count**: 3 files
- **Data file count**: 0 files

## Architecture
C++ native library with 2 implementation files and 1 headers.

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Interfaces and Data Models
See code analysis sections above for key interfaces and data models. Additional interfaces may be documented in source files.

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## References (auto-generated hints)
- Key C++ files:
  - Src\Cellar\FwXml.cpp
  - Src\Cellar\FwXmlString.cpp
- Key headers:
  - Src\Cellar\FwXml.h
