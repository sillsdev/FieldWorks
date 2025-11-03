---
last-reviewed: 2025-11-01
last-reviewed-tree: 9be40fcf0586972031abe58bc18e05fb49c961b114494509a3fb1f1b4dc9df3c
status: production
---

# SfmStats

## Purpose
Command-line tool for analyzing Standard Format Marker (SFM) files. Generates statistics about marker usage, frequency, byte counts (excluding SFMs), and marker pair patterns. Created for "Import Wizard" KenZ to understand legacy SFM data structure before import into FieldWorks. Uses Sfm2Xml underlying classes.

## Architecture
TBD - populate from code. See auto-generated hints below.

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
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **Sfm2Xml**: SFM parsing classes (Utilities/SfmToXml or external library)
- **Consumer**: Data migration specialists analyzing legacy SFM files before import

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- **Project**: SfmStats.csproj
- **Type**: Console (.NET Framework 4.6.2)
- **Output**: SfmStats.exe
- **Namespace**: SfmStats
- **Source files**: Program.cs, AssemblyInfo.cs (2 files, ~299 lines)

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
No test project found.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

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
