---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# DebugProcs

## Purpose
Developer diagnostics and debugging utilities for troubleshooting FieldWorks issues.
Provides specialized debugging helpers, diagnostic output, and instrumentation code used during
development and support scenarios. Not typically used in production builds but invaluable
for diagnosing complex issues in the codebase.

## Key Components
No major public classes identified.

## Technology Stack
- C++ native code
- Debugging and diagnostic APIs
- Windows debugging infrastructure

## Dependencies
- Depends on: Kernel, Generic (core infrastructure)
- Used by: All components during development and debugging

## Build Information
- Native C++ project (vcxproj)
- Typically built in Debug configuration
- May not be included in Release builds

## Entry Points
- Provides debugging macros, assertion handlers, and diagnostic utilities
- Integrated into debug builds of applications

## Related Folders
- **Kernel/** - Core infrastructure that DebugProcs instruments
- **Generic/** - Generic utilities that may include debug support

## Code Evidence
*Analysis based on scanning 2 source files*

## References

- **Project files**: DebugProcs.vcxproj
- **Key C++ files**: DebugProcs.cpp
- **Key headers**: DebugProcs.h
- **Source file count**: 2 files
- **Data file count**: 0 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\DebugProcs\DebugProcs.vcxproj
- Key C++ files:
  - Src\DebugProcs\DebugProcs.cpp
- Key headers:
  - Src\DebugProcs\DebugProcs.h
