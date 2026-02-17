---
last-reviewed: 2025-11-01
last-reviewed-tree: 5c1d3914898abc62296c4b5432435b3886ed57aa2e1d45de68feb95398a3c6c8
status: production
---
anchors:
  - change-log-auto
  - purpose
  - referenced-by
  - architecture
  - key-components
  - sfm2xml-library-7k-lines
  - convertsfmexe-2k-lines
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

# SfmToXml

## Purpose
SFM to XML conversion library and command-line utility. Parses Standard Format Marker files (legacy Toolbox/Shoebox linguistic data) into XML for FieldWorks import. Includes Sfm2Xml core library (ClsHierarchyEntry, ClsPathObject, ClsInFieldMarker parsing) and ConvertSFM.exe command-line tool. Used by LexTextControls LexImportWizard for lexicon/interlinear imports.

### Referenced By

- [SFM Import](../../../openspec/specs/lexicon/import/sfm.md#behavior) — SFM conversion pipeline
- [Encoding Integration](../../../openspec/specs/integration/external/encoding.md#behavior) — Encoding-sensitive conversions

## Architecture
Two-component system: 1) Sfm2Xml library (~7K lines) with ClsHierarchyEntry, Converter, LexImportFields for SFM→XML transformation, 2) ConvertSFM.exe (~2K lines) command-line wrapper. Parser handles SFM hierarchy, inline markers, field mapping, and XML generation for FieldWorks import pipelines.

## Key Components

### Sfm2Xml Library (~7K lines)
- **ClsHierarchyEntry**: SFM hierarchy structure representation
- **ClsPathObject**: Path-based SFM navigation
- **ClsInFieldMarker**: Inline marker handling
- **Converter**: Main SFM→XML transformation engine
- **LexImportFields**: ILexImportFields implementation for field mapping
- **AutoFieldInfo**: Automatic field detection
- **ClsLog, WrnErrInfo**: Error/warning logging

### ConvertSFM.exe (~2K lines)
- **Command-line wrapper** for Sfm2Xml library
- Batch SFM file conversion to XML

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Components**: Sfm2Xml.dll (library), ConvertSFM.exe (CLI tool)
- **Key libraries**: System.Xml (XML generation), Common utilities
- **Input format**: SFM/Toolbox text files
- **Output format**: Structured XML for FLEx import

## Dependencies
- Depends on: XML utilities, Common utilities
- Used by: Import pipelines and data conversion workflows

## Interop & Contracts
COM for cross-boundary calls.

## Threading & Performance
Single-threaded, thread-agnostic.

## Config & Feature Flags
None detected.

## Build Information
Build via `dotnet build Sfm2Xml.csproj` or FieldWorks.sln.

## Interfaces and Data Models
5 interfaces (ILanguageInfoUI, ILexImportField, ILexImportFields, etc.), 20 classes (ClsHierarchyEntry, Converter, LexImportFields, etc.), 3 XSLT transforms (BuildPhase2XSLT, Phase3, Phase4).

## Entry Points
SFM parsing, XML generation, field/hierarchy mapping, inline marker processing. ConvertSFM.exe for command-line usage.

## Test Index
Test project: Sfm2XmlTests. Run via `dotnet test` or Test Explorer.

## Usage Hints
Used by LexTextControls LexImportWizard for Toolbox/SFM import. Command-line tool: ConvertSFM.exe for batch conversion.

## Related Folders
Utilities/SfmStats (SFM statistics), LexText/LexTextControls (import wizard), ParatextImport (Paratext SFM), Utilities/XMLUtils (XML utilities).

## References
Projects: ConvertSFM.csproj, Sfm2Xml.csproj, Sfm2XmlTests.csproj (net48). Key files (19 C#, 8 data): ClsHierarchyEntry.cs, Converter.cs, LexImportFields.cs, Phase3/4 XSLT. See `.cache/copilot/diff-plan.json` for details.
