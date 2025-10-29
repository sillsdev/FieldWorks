# InstallValidator

## Purpose
Utilities to validate installation prerequisites for FieldWorks. Checks system requirements, installed components, and configuration before allowing installation or application startup.

## Key Components
- **InstallValidator.csproj** - Main validation library
- **InstallValidatorTests/InstallValidatorTests.csproj** - Validation tests

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

## Testing
- Run tests: `dotnet test InstallValidator/InstallValidatorTests/InstallValidatorTests.csproj`
- Tests cover validation logic and system checks

## Entry Points
- Invoked by installer and application startup
- Validates prerequisites before installation or launch

## Related Folders
- **FLExInstaller/** - Installer that uses InstallValidator
- **Kernel/** - May check for system-level dependencies
