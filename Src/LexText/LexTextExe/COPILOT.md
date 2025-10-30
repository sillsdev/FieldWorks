---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexTextExe

## Purpose
Main executable entry point for FieldWorks Language Explorer (FLEx). 
Provides the startup code, application initialization, and hosting environment for the LexText 
application. Minimal code here as most functionality resides in LexTextDll and other libraries.

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

## Build Information
- C# Windows application executable
- Build via: `dotnet build LexTextExe.csproj`
- Produces LexText.exe (FLEx application)

## Entry Points
- Main() method - application entry point
- Initializes XCore framework and loads LexTextDll

## Related Folders
- **LexText/LexTextDll/** - Core application logic loaded by this executable
- **XCore/** - Application framework
- **xWorks/** - Shared application infrastructure

## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 1 public classes
- **Namespaces**: SIL.FieldWorks.XWorks.LexText

## Interfaces and Data Models

- **LexText** (class)
  - Path: `LexText.cs`
  - Public class implementation

## References

- **Project files**: LexTextExe.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, LexText.cs
- **Source file count**: 2 files
- **Data file count**: 0 files
