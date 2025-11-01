---
last-reviewed: 2025-10-31
last-verified-commit: 32cc17e
status: reviewed
---

# ProjectUnpacker

## Purpose
Test infrastructure utility (~427 lines) for unpacking embedded ZIP resources containing Paratext and FLEx test projects. Provides **Unpacker** static class with methods to extract test data from embedded .resx files to temporary directories, and **RegistryData** for managing Paratext registry settings during tests. Used exclusively in test fixtures, not production code.

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

## Test Data Resources (embedded .resx)
- **ZippedParatextPrj.resx** - Standard Paratext project ZIP
- **ZippedParaPrjWithMissingFiles.resx** - Test case for missing file handling
- **ZippedTEVTitusWithUnmappedStyle.resx** - TE Vern/Titus project with style issues

## Dependencies
- **Upstream**: ICSharpCode.SharpZipLib.Zip (ZIP extraction), Microsoft.Win32 (registry), NUnit.Framework (test attributes), Common/FwUtils (utilities), SIL.PlatformUtilities
- **Downstream consumers**: ParatextImportTests, other test projects needing Paratext/FLEx project data
- **Note**: This is a test-only library (namespace SIL.FieldWorks.Test.ProjectUnpacker)

## Related Folders
- **ParatextImport/** - Main consumer for Paratext test projects
- **Common/ScriptureUtils/** - May use for Paratext integration tests
- **MigrateSqlDbs/** - Could use for migration test scenarios

## References
- **Project**: ProjectUnpacker.csproj (.NET Framework 4.6.2 library)
- **3 CS files**: Unpacker.cs (~300 lines), RegistryData.cs (~60 lines), AssemblyInfo.cs
- **3 embedded resources**: ZippedParatextPrj.resx, ZippedParaPrjWithMissingFiles.resx, ZippedTEVTitusWithUnmappedStyle.resx
