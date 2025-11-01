---
last-reviewed: 2025-11-01
last-verified-commit: HEAD
status: production
---

# SfmStats

## Purpose
Command-line tool for analyzing Standard Format Marker (SFM) files. Generates statistics about marker usage, frequency, byte counts (excluding SFMs), and marker pair patterns. Created for "Import Wizard" KenZ to understand legacy SFM data structure before import into FieldWorks. Uses Sfm2Xml underlying classes.

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

## Dependencies
- **Sfm2Xml**: SFM parsing classes (Utilities/SfmToXml or external library)
- **Consumer**: Data migration specialists analyzing legacy SFM files before import

## Build Information
- **Project**: SfmStats.csproj
- **Type**: Console (.NET Framework 4.6.2)
- **Output**: SfmStats.exe
- **Namespace**: SfmStats
- **Source files**: Program.cs, AssemblyInfo.cs (2 files, ~299 lines)

## Test Index
No test project found.

## Related Folders
- **Utilities/SfmToXml/**: SFM to XML conversion (uses shared Sfm2Xml parsing)
- **LexText/LexTextControls/**: LexImportWizard (SFM import functionality)
- **ParatextImport/**: USFM/SFM parsing

## References
- **Sfm2Xml namespace**: SFM parsing infrastructure
- **System.IO.File**: File reading
- **System.Text.Encoding**: Character encoding analysis
