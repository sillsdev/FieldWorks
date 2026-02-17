---
last-reviewed: 2025-10-31
last-reviewed-tree: 8fa47ada007bed7cb5ba541c2284686df6b9b7a7445f7019eb9b4a44483c5dbb
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# GenerateHCConfig COPILOT summary

## Purpose
Build-time command-line utility for generating HermitCrab morphological parser configuration files from FieldWorks projects. Reads phonology and morphology data from FLEx project (.fwdata file), uses HCLoader to load linguistic rules, and exports to HermitCrab XML configuration format via XmlLanguageWriter. Enables computational morphological parsing using data defined in FieldWorks. Command syntax: `generatehcconfig <input-project> <output-config>`. Standalone console application (GenerateHCConfig.exe).

## Architecture
C# console application (.NET Framework 4.8.x) with 350 lines of code. Program.cs main entry point coordinates FLEx project loading, HermitCrab data extraction, and XML export. Helper classes: ConsoleLogger (console output for LCM operations), NullFdoDirectories (minimal directory implementation), NullThreadedProgress (no-op progress), ProjectIdentifier (project file identification). Uses SIL.Machine.Morphology.HermitCrab and SIL.FieldWorks.WordWorks.Parser for linguistic processing.

## Key Components
- **Program** class (Program.cs, 83 lines): Main application logic
  - Main() entry point: Validates args, loads FLEx project, generates HC config
  - Arguments: [0] = FLEx project path (.fwdata), [1] = output config path (.xml)
  - Loads LcmCache with DisableDataMigration=true (read-only)
  - HCLoader.Load(): Extract linguistic data from cache
  - XmlLanguageWriter.Save(): Write HermitCrab XML configuration
  - Error handling: File not found, project locked (LcmFileLockedException), migration needed (LcmDataMigrationForbiddenException)
  - WriteHelp(): Usage instructions
- **ConsoleLogger** (ConsoleLogger.cs, 3487 lines): Console-based LCM logger
  - Implements LCM logging interface
  - Outputs messages to console during project loading
- **NullFdoDirectories** (NullFdoDirectories.cs, 200 lines): Minimal directory implementation
  - Provides required directories for LcmCache creation
- **NullThreadedProgress** (NullThreadedProgress.cs, 1318 lines): No-op progress implementation
  - IThreadedProgress no-operation for non-interactive context
- **ProjectIdentifier** (ProjectIdentifier.cs, 1213 lines): Project file identification
  - Wraps project file path for LcmCache creation

## Technology Stack
- C# .NET Framework 4.8.x (net8)

## Dependencies
- Upstream: Language and Culture Model (LcmCache, project loading)
- Downstream: May be used during FLEx builds

## Interop & Contracts
- **Command-line interface**: `generatehcconfig <input-project> <output-config>`

## Threading & Performance
- **Single-threaded**: Console application

## Config & Feature Flags
- **App.config**: Application configuration

## Build Information
- **Project file**: GenerateHCConfig.csproj (net48, OutputType=Exe)

## Interfaces and Data Models
ConsoleLogger.

## Entry Points
- **GenerateHCConfig.exe**: Command-line executable

## Test Index
No dedicated test project. Tested via command-line execution with sample FLEx projects.

## Usage Hints
- **Command**: `GenerateHCConfig.exe MyProject.fwdata output.xml`

## Related Folders
- **WordWorks/Parser**: Contains HCLoader for data extraction

## References
See `.cache/copilot/diff-plan.json` for file details.
