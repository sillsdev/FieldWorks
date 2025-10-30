---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# InstallValidator

## Purpose
Installation prerequisite validation utilities.
Checks system requirements, validates configuration, and verifies that necessary dependencies
are present before FieldWorks installation or startup. Helps prevent installation failures
and provides clear diagnostics when requirements are not met.

## Key Components
### Key Classes
- **InstallValidator**
- **InstallValidatorTests**

## Technology Stack
- C# .NET
- System detection and validation
- Windows API for system checks

## Dependencies
- Depends on: System libraries, minimal FieldWorks dependencies
- Used by: Installer (FLExInstaller), application startup

## Build Information
- C# class library
- Includes test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Invoked by installer and application startup
- Validates prerequisites before installation or launch

## Related Folders
- **FLExInstaller/** - Installer that uses InstallValidator
- **Kernel/** - May check for system-level dependencies

## Code Evidence
*Analysis based on scanning 2 source files*

- **Classes found**: 2 public classes
- **Namespaces**: SIL.InstallValidator

## Interfaces and Data Models

- **InstallValidator** (class)
  - Path: `InstallValidator.cs`
  - Public class implementation

## References

- **Project files**: InstallValidator.csproj, InstallValidatorTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, InstallValidator.cs, InstallValidatorTests.cs
- **Source file count**: 3 files
- **Data file count**: 0 files

## Architecture
C# library with 3 source files. Contains 1 subprojects: InstallValidator.

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
Test projects: InstallValidatorTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Console application. Build and run via command line or Visual Studio. See Entry Points section.

## References (auto-generated hints)
- Project files:
  - Src\InstallValidator\InstallValidator.csproj
  - Src\InstallValidator\InstallValidatorTests\InstallValidatorTests.csproj
- Key C# files:
  - Src\InstallValidator\InstallValidator.cs
  - Src\InstallValidator\InstallValidatorTests\InstallValidatorTests.cs
  - Src\InstallValidator\Properties\AssemblyInfo.cs
