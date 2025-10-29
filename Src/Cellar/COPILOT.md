---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
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
