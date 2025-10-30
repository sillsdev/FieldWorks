---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# InstallValidator

## Purpose
Utilities to validate installation prerequisites for FieldWorks. Checks system requirements, installed components, and configuration before allowing installation or application startup.

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
