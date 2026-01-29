---
last-reviewed: 2025-11-21
last-reviewed-tree: d0f70922cbc37871cd0fdc36494727714da3106bdd3128d9d0a0ddf9acabfe42
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# ProjectUnpacker

## Purpose
Test infrastructure utility (~427 lines) for unpacking embedded ZIP resources containing Paratext and FLEx test projects. Provides **Unpacker** static class with methods to extract test data from embedded .resx files to temporary directories, and **RegistryData** for managing Paratext registry settings during tests. Used exclusively in test fixtures, not production code.

## Architecture
C# test utility library (net48) with 3 source files (~427 lines). Single static class **Unpacker** with nested **ResourceUnpacker** for ZIP extraction from embedded .resx files. **RegistryData** helper for Paratext registry state management. Designed exclusively for test fixtures to provide Paratext/FLEx test project data without requiring external files or Paratext installation.

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
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Namespace**: SIL.FieldWorks.Test.ProjectUnpacker (test-only library)
- **Key libraries**:
  - ICSharpCode.SharpZipLib.Zip (ZIP extraction from embedded resources)
  - Microsoft.Win32 (registry access for Paratext settings)
  - NUnit.Framework (test attributes and fixtures)
  - Common/FwUtils (FW utilities)
  - SIL.PlatformUtilities
- **Resource storage**: Embedded .resx files (ZIP archives base64-encoded)
- **Platform**: Windows-only (registry dependency)

## Dependencies
- **Upstream**: ICSharpCode.SharpZipLib.Zip (ZIP extraction), Microsoft.Win32 (registry), NUnit.Framework (test attributes), Common/FwUtils (utilities), SIL.PlatformUtilities
- **Downstream consumers**: ParatextImportTests, other test projects needing Paratext/FLEx project data
- **Note**: This is a test-only library (namespace SIL.FieldWorks.Test.ProjectUnpacker)

## Interop & Contracts
- **Registry access**: Microsoft.Win32.Registry for Paratext settings
  - Keys: `SOFTWARE\ScrChecks\1.0\Settings_Directory` (Paratext project location)
  - Purpose: Locate Paratext test folder, save/restore registry state during tests
  - RegistryView: Handles 32-bit/64-bit registry redirection
- **Embedded resource extraction**: SharpZipLib.Zip for ZIP decompression
  - Input: Base64-encoded ZIP in .resx files
  - Output: Extracted Paratext/FLEx project files in temp directory
- **Test data contracts**:
  - ZippedParatextPrj.resx: Standard Paratext project ZIP (~6.8MB)
  - ZippedParaPrjWithMissingFiles.resx: Incomplete project for error handling tests (~92KB)
  - ZippedTEVTitusWithUnmappedStyle.resx: TE Vern/Titus with style issues (~9.6KB)
- **Cleanup contract**: ResourceUnpacker.CleanUp() removes extracted files (disposable pattern)
- **No COM interop**: Pure managed code

## Threading & Performance
Synchronous operations. ZIP extraction fast for small projects (~1s), slower for large (~2-3s for 6.8MB). Test-only, single-threaded usage.

## Config & Feature Flags
Registry-based Paratext folder configuration. Three embedded test resources: ZippedParatextPrj, ZippedParaPrjWithMissingFiles, ZippedTEVTitusWithUnmappedStyle. Windows-specific.

## Build Information
C# library (net48). Build via `msbuild ProjectUnpacker.csproj`. Output: ProjectUnpacker.dll (test-only utility). 3 embedded .resx files (~6.9MB).

## Interfaces and Data Models
Unpacker static class with PrepareProjectFiles(), RemoveFiles(). ResourceUnpacker nested class for RAII extraction. RegistryData for registry capture/restore.

## Entry Points
Test fixture pattern: Create ResourceUnpacker in [SetUp], use UnpackedDestinationPath in tests, CleanUp() in [TearDown]. Static Unpacker.PrepareProjectFiles() for one-off extraction.

## Test Index
No dedicated tests. Test infrastructure exercised by ParatextImportTests and other consumers.

## Usage Hints
Create ResourceUnpacker in [SetUp], use UnpackedDestinationPath, CleanUp() in [TearDown]. Three resources: ZippedParatextPrj (standard), ZippedParaPrjWithMissingFiles (error handling), ZippedTEVTitusWithUnmappedStyle (style issues).

## Related Folders
- **ParatextImport/**: Main consumer
- **Common/ScriptureUtils/**: Paratext integration tests

## References
3 C# files (~427 lines). Key: Unpacker.cs, RegistryData.cs. 3 embedded .resx files. See `.cache/copilot/diff-plan.json` for file listings.
