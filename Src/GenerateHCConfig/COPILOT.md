---
last-reviewed: 2025-10-31
last-reviewed-tree: 59757c0e914d1f58bd8943ea49adcfcf72cfb9eb5608e3a66ee822925a1aee83
status: draft
---

# GenerateHCConfig COPILOT summary

## Purpose
Build-time command-line utility for generating HermitCrab morphological parser configuration files from FieldWorks projects. Reads phonology and morphology data from FLEx project (.fwdata file), uses HCLoader to load linguistic rules, and exports to HermitCrab XML configuration format via XmlLanguageWriter. Enables computational morphological parsing using data defined in FieldWorks. Command syntax: `generatehcconfig <input-project> <output-config>`. Standalone console application (GenerateHCConfig.exe).

## Architecture
C# console application (.NET Framework 4.6.2) with 350 lines of code. Program.cs main entry point coordinates FLEx project loading, HermitCrab data extraction, and XML export. Helper classes: ConsoleLogger (console output for LCM operations), NullFdoDirectories (minimal directory implementation), NullThreadedProgress (no-op progress), ProjectIdentifier (project file identification). Uses SIL.Machine.Morphology.HermitCrab and SIL.FieldWorks.WordWorks.Parser for linguistic processing.

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
- C# .NET Framework 4.6.2 (net462)
- OutputType: Exe (console application)
- **SIL.Machine.Morphology.HermitCrab**: HermitCrab parser library
- **SIL.FieldWorks.WordWorks.Parser**: FieldWorks parser components
- **SIL.LCModel**: FieldWorks data model access (LcmCache)
- Console application (no GUI)

## Dependencies

### Upstream (consumes)
- **SIL.LCModel**: Language and Culture Model (LcmCache, project loading)
- **SIL.Machine.Morphology.HermitCrab**: HermitCrab parser (Language, XmlLanguageWriter)
- **SIL.Machine.Annotations**: Annotation framework
- **SIL.FieldWorks.WordWorks.Parser**: FieldWorks parser (HCLoader)
- **Common/FwUtils**: Utilities (FwRegistryHelper, FwUtils.InitializeIcu)
- **SIL.WritingSystems**: Writing system support (Sldr)

### Downstream (consumed by)
- **Build process**: May be used during FLEx builds
- **Developers/linguists**: Generate HermitCrab configs from FLEx projects for external parsers
- HermitCrab parser tools consuming generated XML

## Interop & Contracts
- **Command-line interface**: `generatehcconfig <input-project> <output-config>`
- **Exit codes**: 0 = success, 1 = error (file not found, project locked, migration needed)
- **HermitCrab XML format**: Output compatible with HermitCrab morphological parser

## Threading & Performance
- **Single-threaded**: Console application
- **Read-only**: DisableDataMigration=true prevents writes
- **Performance**: Project loading time depends on FLEx project size

## Config & Feature Flags
- **App.config**: Application configuration
- **LcmSettings**: DisableDataMigration=true for read-only access
- No user-configurable settings; all via command-line arguments

## Build Information
- **Project file**: GenerateHCConfig.csproj (net462, OutputType=Exe)
- **Output**: GenerateHCConfig.exe (console tool)
- **Build**: Via top-level FW.sln or: `msbuild GenerateHCConfig.csproj`
- **Usage**: `GenerateHCConfig.exe <project.fwdata> <output.xml>`

## Interfaces and Data Models

- **Program.Main()** (Program.cs)
  - Purpose: Command-line entry point for HC config generation
  - Inputs: args[0] = FLEx project path (.fwdata), args[1] = output HC config path (.xml)
  - Outputs: Exit code 0 (success) or 1 (error); HC XML config file
  - Notes: Validates inputs, loads project, calls HCLoader, writes XML

- **HCLoader.Load()** (from WordWorks.Parser)
  - Purpose: Extract HermitCrab language data from LcmCache
  - Inputs: LcmCache cache, ILogger logger
  - Outputs: Language object (HermitCrab)
  - Notes: Converts FLEx phonology/morphology to HermitCrab structures

- **XmlLanguageWriter.Save()** (from HermitCrab)
  - Purpose: Serialize HermitCrab Language to XML configuration
  - Inputs: Language language, string outputPath
  - Outputs: XML file written
  - Notes: HermitCrab-compatible XML format

- **ConsoleLogger** (ConsoleLogger.cs)
  - Purpose: Log LCM operations to console
  - Inputs: Log messages from LcmCache
  - Outputs: Console output
  - Notes: Provides feedback during project loading

- **Error handling**:
  - LcmFileLockedException: Project open in another app
  - LcmDataMigrationForbiddenException: Project needs migration in FLEx
  - File not found: Project file doesn't exist

## Entry Points
- **GenerateHCConfig.exe**: Command-line executable
- **Main()**: Program entry point

## Test Index
No dedicated test project. Tested via command-line execution with sample FLEx projects.

## Usage Hints
- **Command**: `GenerateHCConfig.exe MyProject.fwdata output.xml`
- **Prerequisites**: FLEx project file (.fwdata) must exist and be up-to-date (no migration needed)
- **Read-only**: Does not modify FLEx project (DisableDataMigration=true)
- **Error messages**: Clear errors for common issues (file not found, project locked, migration needed)
- **Use case**: Export FLEx phonology/morphology data to HermitCrab for external morphological parsing
- **HermitCrab**: Output XML compatible with HermitCrab morphological parser framework

## Related Folders
- **WordWorks/Parser**: Contains HCLoader for data extraction
- Build process may invoke this utility

## References
- **Project files**: GenerateHCConfig.csproj (net462, OutputType=Exe)
- **Configuration**: App.config, BuildInclude.targets
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: Program.cs (83 lines), ConsoleLogger.cs, NullFdoDirectories.cs, NullThreadedProgress.cs, ProjectIdentifier.cs
- **Total lines of code**: 350
- **Output**: GenerateHCConfig.exe (Output/Debug or Output/Release)
- **Namespace**: GenerateHCConfig