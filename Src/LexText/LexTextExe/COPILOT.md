---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# LexTextExe

## Purpose
Main executable entry point for FieldWorks Language Explorer (FLEx).
Provides the startup code, application initialization, and hosting environment for the LexText
application. Minimal code here as most functionality resides in LexTextDll and other libraries.

## Architecture
C# library with 2 source files.

## Key Components
### Key Classes
- **LexText**

## Technology Stack
- C# .NET WinForms
- Application executable
- XCore framework integration

## Dependencies
- Depends on: LexText/LexTextDll (core logic), XCore (framework), all dependencies
- Used by: End users launching FLEx application

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# Windows application executable
- Build via: `dotnet build LexTextExe.csproj`
- Produces LexText.exe (FLEx application)

## Interfaces and Data Models

- **LexText** (class)
  - Path: `LexText.cs`
  - Public class implementation

## Entry Points
- Main() method - application entry point
- Initializes XCore framework and loads LexTextDll

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Console application. Build and run via command line or Visual Studio. See Entry Points section.

## Related Folders
- **LexText/LexTextDll/** - Core application logic loaded by this executable
- **XCore/** - Application framework
- **xWorks/** - Shared application infrastructure

## References

- **Project files**: LexTextExe.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, LexText.cs
- **Source file count**: 2 files
- **Data file count**: 0 files

## References (auto-generated hints)
- Project files:
  - LexText/LexTextExe/LexTextExe.csproj
- Key C# files:
  - LexText/LexTextExe/AssemblyInfo.cs
  - LexText/LexTextExe/LexText.cs
## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 1 public classes
- **Namespaces**: SIL.FieldWorks.XWorks.LexText
