---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ManagedLgIcuCollator

## Purpose
Managed .NET wrapper for ICU (International Components for Unicode) collation services.
Provides proper linguistic text sorting and comparison functionality for multiple writing systems.
Enables correct alphabetical ordering of text in various languages by bridging ICU native libraries
with .NET code through a managed interface.

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

## Architecture
C# library with 2 source files. Contains 1 subprojects: ManagedLgIcuCollator.

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
Test projects: ManagedLgIcuCollatorTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## References (auto-generated hints)
- Project files:
  - Src\ManagedLgIcuCollator\ManagedLgIcuCollator.csproj
  - Src\ManagedLgIcuCollator\ManagedLgIcuCollatorTests\ManagedLgIcuCollatorTests.csproj
- Key C# files:
  - Src\ManagedLgIcuCollator\LgIcuCollator.cs
  - Src\ManagedLgIcuCollator\ManagedLgIcuCollatorTests\ManagedLgIcuCollatorTests.cs
