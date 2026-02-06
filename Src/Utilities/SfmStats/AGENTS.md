---
last-reviewed: 2025-11-21
last-reviewed-tree: 21276ebc360840097a54dc037be9cd230a22c03262dc075dd198bfb93941163b
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - programcs-299-lines
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

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
Language - C#

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- Command-line: `SfmStats.exe <inputfile> [outputfile]`

## Threading & Performance
- Single-threaded: Synchronous file processing

## Config & Feature Flags
- No configuration files: All behavior from command-line arguments

## Build Information
- Project: SfmStats.csproj

## Interfaces and Data Models
See Key Components section above.

## Entry Points
- Command-line: `SfmStats.exe myfile.sfm` (console output)

## Test Index
No test project found.

## Usage Hints
- Basic: `SfmStats.exe mydata.sfm` shows stats on console

## Related Folders
- Utilities/SfmToXml/: SFM to XML conversion (uses shared Sfm2Xml parsing)

## References
See `.cache/copilot/diff-plan.json` for file details.
