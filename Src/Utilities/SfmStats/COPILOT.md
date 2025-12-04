---
last-reviewed: 2025-11-01
last-reviewed-tree: 9be40fcf0586972031abe58bc18e05fb49c961b114494509a3fb1f1b4dc9df3c
status: production
---

# SfmStats

## Purpose
Command-line tool for analyzing Standard Format Marker (SFM) files. Generates statistics about marker usage, frequency, byte counts (excluding SFMs), and marker pair patterns. Created for "Import Wizard" KenZ to understand legacy SFM data structure before import into FieldWorks. Uses Sfm2Xml underlying classes.

## Architecture
Simple command-line tool (~299 lines, single Program.cs) for SFM file analysis. Parses SFM files using Sfm2Xml infrastructure, generates three statistical reports: byte counts by character, SFM marker frequency, and SFM pair patterns. Created for Import Wizard development to understand legacy data structure.

## Key Components

### Program.cs (~299 lines)
- **Main(string[] args)**: Entry point
  - Input: SFM file path, optional output file path
  - Outputs three report types:
    1. Byte count histogram by character code (e.g., [0xE9] = 140 bytes)
    2. SFM usage frequency (e.g., \v = 48 occurrences)
    3. SFM pair patterns (e.g., \p - \v = 6 times)
  - **Usage()**: Prints command-line help
  - Uses Sfm2Xml parsing infrastructure
- Excludes inline SFMs from counts

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Application type**: Console executable
- **Key libraries**: Sfm2Xml (SFM parsing), System.IO, System.Text.Encoding
- **Output format**: Plain text statistical reports

## Dependencies
- **Sfm2Xml**: SFM parsing classes (Utilities/SfmToXml or external library)
- **Consumer**: Data migration specialists analyzing legacy SFM files before import

## Interop & Contracts
- **Command-line**: `SfmStats.exe <inputfile> [outputfile]`
- **Input**: SFM file path (required)
- **Output**: Statistical reports to file or console
- **Reports**: 1) Byte count histogram, 2) SFM usage frequency, 3) SFM pair patterns
- **Exit code**: 0 = success, non-zero = error

## Threading & Performance
- **Single-threaded**: Synchronous file processing
- **Performance**: Fast for typical SFM files (<1 second for most files)
- **Memory**: Loads entire file into memory for analysis
- **No caching**: One-pass analysis per execution

## Config & Feature Flags
- **No configuration files**: All behavior from command-line arguments
- **Inline SFM exclusion**: Automatically excludes inline markers from byte counts
- **Output destination**: Console (default) or file (optional second argument)

## Build Information
- **Project**: SfmStats.csproj
- **Type**: Console (.NET Framework 4.8.x)
- **Output**: SfmStats.exe
- **Namespace**: SfmStats
- **Source files**: Program.cs, AssemblyInfo.cs (2 files, ~299 lines)

## Interfaces and Data Models
- **Main(string[] args)**: Entry point parsing command-line arguments
- **Usage()**: Prints command-line help
- **Statistical reports**: Hashtables for counts, console output formatting

## Entry Points
- **Command-line**: `SfmStats.exe myfile.sfm` (console output)
- **With output file**: `SfmStats.exe myfile.sfm stats.txt`
- **Typical usage**: Analyze SFM before import to understand structure
- **Import Wizard workflow**: Run SfmStats → review reports → configure import mapping

## Test Index
No test project found.

## Usage Hints
- **Basic**: `SfmStats.exe mydata.sfm` shows stats on console
- **Save output**: `SfmStats.exe mydata.sfm report.txt`
- **Interpreting results**:
  - Byte histogram shows character distribution
  - SFM frequency shows which markers are used most
  - SFM pairs show common marker sequences (helps understand structure)
- **Best practice**: Run before import to validate SFM structure matches expectations

## Related Folders
- **Utilities/SfmToXml/**: SFM to XML conversion (uses shared Sfm2Xml parsing)
- **LexText/LexTextControls/**: LexImportWizard (SFM import functionality)
- **ParatextImport/**: USFM/SFM parsing

## References
- **Sfm2Xml namespace**: SFM parsing infrastructure
- **System.IO.File**: File reading
- **System.Text.Encoding**: Character encoding analysis

## References (auto-generated hints)
- Project files:
  - Utilities/SfmStats/SfmStats.csproj
- Key C# files:
  - Utilities/SfmStats/Program.cs
  - Utilities/SfmStats/Properties/AssemblyInfo.cs
