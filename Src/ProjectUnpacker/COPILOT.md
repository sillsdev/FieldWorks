---
last-reviewed: 2025-11-21
last-reviewed-tree: d0f70922cbc37871cd0fdc36494727714da3106bdd3128d9d0a0ddf9acabfe42
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/ProjectUnpacker. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
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
- **Thread safety**: Not thread-safe; test fixtures typically run single-threaded
- **Synchronous operations**: All extraction and registry access synchronous
- **Performance characteristics**:
  - ZIP extraction: Fast for small projects (<1 second), slower for large (~6.8MB ZippedParatextPrj takes ~2-3 seconds)
  - Registry access: Fast (milliseconds)
  - Cleanup: RemoveFiles() recursively deletes directories (fast for typical test projects)
- **Resource overhead**: Embedded .resx files increase assembly size (~6.9MB total for 3 ZIP files)
- **Temporary files**: Extracted to system temp directory, cleaned up via CleanUp() or test teardown
- **No caching**: Each ResourceUnpacker instance extracts fresh copy
- **Typical usage pattern**: SetUp extracts, TearDown cleans up

## Config & Feature Flags
- **Registry-based configuration**: Paratext project folder from registry
  - PTProjectDirectory: Reads from `SOFTWARE\ScrChecks\1.0\Settings_Directory`
  - Supports Paratext 7 and 8 registry locations
- **Test resource selection**: Caller specifies which embedded .resx to extract
  - "ZippedParatextPrj" for standard project
  - "ZippedParaPrjWithMissingFiles" for error case testing
  - "ZippedTEVTitusWithUnmappedStyle" for style handling tests
- **Extraction destination**: Configurable via PrepareProjectFiles(folder, resource)
  - Default: PtProjectTestFolder (derived from registry + "Test" suffix)
- **Cleanup behavior**: Manual via CleanUp() or automatic in test TearDown
- **No global state**: Each ResourceUnpacker instance independent
- **Windows-specific**: Registry dependency limits to Windows platform

## Build Information
- **Project type**: C# class library (net48)
- **Build**: `msbuild ProjectUnpacker.csproj` or `dotnet build` (from FieldWorks.sln)
- **Output**: ProjectUnpacker.dll (test utility library)
- **Dependencies**:
  - ICSharpCode.SharpZipLib.Zip (NuGet package for ZIP extraction)
  - Microsoft.Win32 (registry access)
  - NUnit.Framework (test infrastructure)
  - Common/FwUtils
  - SIL.PlatformUtilities
- **Embedded resources**: 3 .resx files (~6.9MB total) embedded in assembly
- **Target consumers**: Test projects only (ParatextImportTests, etc.)
- **Not deployed**: Test-only artifact, not included in production installer

## Interfaces and Data Models

### Classes
- **Unpacker** (static class, path: Src/ProjectUnpacker/Unpacker.cs)
  - Purpose: Extract embedded test project ZIPs to temporary directories
  - Methods:
    - PrepareProjectFiles(string folder, string resource): Extract resource to folder
    - UnpackFile(string resource, string destination): Internal ZIP extraction
    - RemoveFiles(string directory): Recursive cleanup
  - Properties:
    - PTProjectDirectory: Paratext project folder from registry
    - PTSettingsRegKey: Registry key path for Paratext settings
    - PtProjectTestFolder: Computed test folder path
  - Notes: All static members, no instantiation required

- **ResourceUnpacker** (nested class)
  - Purpose: RAII wrapper for single resource extraction
  - Constructor: ResourceUnpacker(string resource, string folder) - Extracts on construction
  - Properties: UnpackedDestinationPath (string) - Returns extraction path
  - Methods: CleanUp() - Removes extracted files
  - Usage: Create in test SetUp, call CleanUp() in TearDown

- **RegistryData** (class, path: Src/ProjectUnpacker/RegistryData.cs)
  - Purpose: Capture/restore registry state for Paratext tests
  - Constructor: RegistryData(string subKey, string name, object value)
  - Properties: RegistryHive, RegistryView, SubKey, Name, Value
  - Methods: ToString() - Debug representation
  - Usage: Save registry value before test, restore after

### Data Models (Embedded Resources)
- **ZippedParatextPrj.resx** - Standard Paratext project (~6.8MB ZIP)
- **ZippedParaPrjWithMissingFiles.resx** - Incomplete project for error testing (~92KB)
- **ZippedTEVTitusWithUnmappedStyle.resx** - TE Vern/Titus with style issues (~9.6KB)

## Entry Points
- **Test fixture usage** (typical pattern):
  ```csharp
  [TestFixture]
  public class ParatextImportTests
  {
      private Unpacker.ResourceUnpacker m_unpacker;

      [SetUp]
      public void Setup()
      {
          m_unpacker = new Unpacker.ResourceUnpacker("ZippedParatextPrj", Unpacker.PtProjectTestFolder);
          // m_unpacker.UnpackedDestinationPath now contains extracted project
      }

      [TearDown]
      public void TearDown()
      {
          m_unpacker.CleanUp();
      }

      [Test]
      public void ImportParatextProject_Success()
      {
          // Test uses files from m_unpacker.UnpackedDestinationPath
      }
  }
  ```
- **Static method access**: Unpacker.PrepareProjectFiles() for one-off extraction
- **Registry state management**:
  ```csharp
  var savedValue = new RegistryData(keyPath, valueName, currentValue);
  // Modify registry for test
  // Restore: Registry.SetValue(savedValue.SubKey, savedValue.Name, savedValue.Value)
  ```
- **Common consumers**:
  - ParatextImportTests: Extract Paratext projects for import testing
  - MigrateSqlDbs tests: Provide test project data
  - Any test requiring Paratext/FLEx project files

## Test Index
- **No dedicated tests**: ProjectUnpacker is test infrastructure, not tested itself
- **Integration testing**: Exercised by test projects that consume it
  - ParatextImportTests: Primary consumer, validates extraction and project loading
  - Tests verify: ZIP extraction works, files accessible, cleanup removes all files
- **Manual validation**:
  - Run ParatextImportTests with breakpoint after SetUp
  - Verify extracted files exist in m_unpacker.UnpackedDestinationPath
  - Verify CleanUp() removes all files in TearDown
- **Test data validation**:
  - ZippedParatextPrj: Should extract to valid Paratext project structure
  - ZippedParaPrjWithMissingFiles: Should extract partial project (expected missing files)
  - ZippedTEVTitusWithUnmappedStyle: Should extract with unmapped style markers
- **Implicit testing**: Any test using Unpacker verifies its correctness

## Usage Hints
- **Basic usage** (test fixture pattern):
  1. Create ResourceUnpacker in [SetUp]: `m_unpacker = new Unpacker.ResourceUnpacker("ZippedParatextPrj", folder)`
  2. Use extracted files in tests: `var projectPath = m_unpacker.UnpackedDestinationPath`
  3. Clean up in [TearDown]: `m_unpacker.CleanUp()`
- **Resource selection**:
  - "ZippedParatextPrj": Standard complete Paratext project
  - "ZippedParaPrjWithMissingFiles": Incomplete project for error handling tests
  - "ZippedTEVTitusWithUnmappedStyle": TE Vern/Titus with style issues
- **Registry management**:
  - Save before test: `var saved = new RegistryData(key, name, Registry.GetValue(...))`
  - Restore after test: `Registry.SetValue(saved.SubKey, saved.Name, saved.Value)`
- **Folder selection**:
  - Use Unpacker.PtProjectTestFolder for default test location
  - Or specify custom folder for isolation
- **Common pitfalls**:
  - Forgetting CleanUp(): Leaves files in temp directory (disk space leak)
  - Parallel tests: Multiple tests extracting to same folder (use unique folder per fixture)
  - Large ZIP: ZippedParatextPrj is ~6.8MB (extraction takes 2-3 seconds)
- **Debugging tips**:
  - Set breakpoint after ResourceUnpacker construction
  - Inspect UnpackedDestinationPath in debugger
  - Manually browse extracted files to verify structure
- **Performance**: Extract once per fixture (SetUp/TearDown), not per test
- **Windows-only**: Registry dependency limits to Windows platform

## Related Folders
- **ParatextImport/** - Main consumer for Paratext test projects
- **Common/ScriptureUtils/** - May use for Paratext integration tests
- **MigrateSqlDbs/** - Could use for migration test scenarios

## References
- **Project**: ProjectUnpacker.csproj (.NET Framework 4.8.x library)
- **3 CS files**: Unpacker.cs (~300 lines), RegistryData.cs (~60 lines), AssemblyInfo.cs
- **3 embedded resources**: ZippedParatextPrj.resx, ZippedParaPrjWithMissingFiles.resx, ZippedTEVTitusWithUnmappedStyle.resx

## Auto-Generated Project and File References
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
