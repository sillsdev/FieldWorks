---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ManagedLgIcuCollator

## Purpose
Managed wrapper for ICU collation services. Provides .NET-friendly access to ICU (International Components for Unicode) collation and sorting functionality for proper linguistic text ordering.

## Key Components
### Key Classes
- **ManagedLgIcuCollator**
- **ManagedLgIcuCollatorTests**

## Technology Stack
- C# .NET with P/Invoke or C++/CLI
- ICU library integration
- Unicode collation algorithms

## Dependencies
- Depends on: ICU libraries (in Lib/), Common utilities
- Used by: Components needing proper linguistic sorting (LexText, xWorks)

## Build Information
- C# class library with native interop
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Provides collation services for linguistic text sorting
- Used by UI components displaying sorted linguistic data

## Related Folders
- **Lib/** - Contains ICU native libraries
- **Kernel/** - May provide low-level string utilities
- **LexText/** - Uses collation for lexicon entry sorting
- **xWorks/** - Uses collation in various data displays

## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 2 public classes
- **Namespaces**: SIL.FieldWorks.Language

## Interfaces and Data Models

- **ManagedLgIcuCollator** (class)
  - Path: `LgIcuCollator.cs`
  - Public class implementation

## References

- **Project files**: ManagedLgIcuCollator.csproj, ManagedLgIcuCollatorTests.csproj
- **Target frameworks**: net462
- **Key C# files**: LgIcuCollator.cs, ManagedLgIcuCollatorTests.cs
- **Source file count**: 2 files
- **Data file count**: 0 files
