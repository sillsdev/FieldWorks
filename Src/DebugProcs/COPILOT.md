---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# DebugProcs

## Purpose
Developer diagnostics and debug helpers for troubleshooting FieldWorks issues. Provides debugging utilities, diagnostic tools, and development aids.

## Key Components
- **DebugProcs.vcxproj** - Native debugging procedures and helpers

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
