---
applyTo: "Src/ProjectUnpacker/**"
name: "projectunpacker.instructions"
description: "Auto-generated concise instructions from COPILOT.md for ProjectUnpacker"
---

# ProjectUnpacker (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **ResourceUnpacker** nested class - Extracts single embedded resource to folder
- Constructor: `ResourceUnpacker(String resource, String folder)` - Unpacks resource
- `UnpackedDestinationPath` property - Returns extraction path
- `CleanUp()` - Removes unpacked files
- **PTProjectDirectory** property - Reads Paratext project folder from registry (PT 7/8 support)
- **PTSettingsRegKey** property - Returns `SOFTWARE\ScrChecks\1.0\Settings_Directory` path

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: f0aea6564baf195088feb2b81f2480b04e2b1b96601c7036143611032845ee46
status: reviewed
---

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
- **ToSt
