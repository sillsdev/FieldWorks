---
last-reviewed: 2025-10-31
last-reviewed-tree: f0aea6564baf195088feb2b81f2480b04e2b1b96601c7036143611032845ee46
status: reviewed
---

# ProjectUnpacker

## Purpose
Test infrastructure utility (~427 lines) for unpacking embedded ZIP resources containing Paratext and FLEx test projects. Provides **Unpacker** static class with methods to extract test data from embedded .resx files to temporary directories, and **RegistryData** for managing Paratext registry settings during tests. Used exclusively in test fixtures, not production code.

## Architecture
TBD - populate from code. See auto-generated hints below.

## Key Components

### Unpacker (static class)
- **ResourceUnpacker** nested class - Extracts single embedded resource to folder
  - Constructor: `ResourceUnpacker(String resource, String folder)` - Unpacks resource
  - `UnpackedDestinationPath` property - Returns extraction path
  - `CleanUp()` - Removes unpacked files
- **PTProjectDirectory** property - Reads Paratext project folder from registry (PT 7/8 support)
- **PTSettingsRegKey** property - Returns `SOFTWARE\ScrChecks\1.0\Settings_Directory` path
- **PtProjectTestFolder** property - Computed test folder path
- **UnpackFile(String resource, String destination)** - Internal extraction using SharpZipLib
- **RemoveFiles(String directory)** - Recursive cleanup
- **PrepareProjectFiles(String folder, String resource)** - Convenience wrapper

### RegistryData (class)
- **RegistryData(String subKey, String name, object value)** - Captures current registry value
  - Stores: `RegistryHive`, `RegistryView`, `SubKey`, `Name`, `Value`
  - Used to save/restore Paratext settings around tests
- **ToString()** - Debug representation

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **Upstream**: ICSharpCode.SharpZipLib.Zip (ZIP extraction), Microsoft.Win32 (registry), NUnit.Framework (test attributes), Common/FwUtils (utilities), SIL.PlatformUtilities
- **Downstream consumers**: ParatextImportTests, other test projects needing Paratext/FLEx project data
- **Note**: This is a test-only library (namespace SIL.FieldWorks.Test.ProjectUnpacker)

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
TBD - populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **ParatextImport/** - Main consumer for Paratext test projects
- **Common/ScriptureUtils/** - May use for Paratext integration tests
- **MigrateSqlDbs/** - Could use for migration test scenarios

## References
- **Project**: ProjectUnpacker.csproj (.NET Framework 4.6.2 library)
- **3 CS files**: Unpacker.cs (~300 lines), RegistryData.cs (~60 lines), AssemblyInfo.cs
- **3 embedded resources**: ZippedParatextPrj.resx, ZippedParaPrjWithMissingFiles.resx, ZippedTEVTitusWithUnmappedStyle.resx

## References (auto-generated hints)
- Project files:
  - Src/ProjectUnpacker/ProjectUnpacker.csproj
- Key C# files:
  - Src/ProjectUnpacker/AssemblyInfo.cs
  - Src/ProjectUnpacker/RegistryData.cs
  - Src/ProjectUnpacker/Unpacker.cs
- Data contracts/transforms:
  - Src/ProjectUnpacker/ZippedParaPrjWithMissingFiles.resx
  - Src/ProjectUnpacker/ZippedParatextPrj.resx
  - Src/ProjectUnpacker/ZippedTEVTitusWithUnmappedStyle.resx
## Test Data Resources (embedded .resx)
- **ZippedParatextPrj.resx** - Standard Paratext project ZIP
- **ZippedParaPrjWithMissingFiles.resx** - Test case for missing file handling
- **ZippedTEVTitusWithUnmappedStyle.resx** - TE Vern/Titus project with style issues
