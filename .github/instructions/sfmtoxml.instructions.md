---
applyTo: "Src/Utilities/SfmToXml/**"
name: "sfmtoxml.instructions"
description: "Auto-generated concise instructions from COPILOT.md for SfmToXml"
---

# SfmToXml (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **ClsHierarchyEntry**: SFM hierarchy structure representation
- **ClsPathObject**: Path-based SFM navigation
- **ClsInFieldMarker**: Inline marker handling
- **Converter**: Main SFM→XML transformation engine
- **LexImportFields**: ILexImportFields implementation for field mapping
- **AutoFieldInfo**: Automatic field detection

## Example (from summary)

---
last-reviewed: 2025-11-01
last-reviewed-tree: 8139d9d97ab82c3dbd6da649e92fbac12e4deb50026946620bb6bd1e7173a4a7
status: production
---

# SfmToXml

## Purpose
SFM to XML conversion library and command-line utility. Parses Standard Format Marker files (legacy Toolbox/Shoebox linguistic data) into XML for FieldWorks import. Includes Sfm2Xml core library (ClsHierarchyEntry, ClsPathObject, ClsInFieldMarker parsing) and ConvertSFM.exe command-line tool. Used by LexTextControls LexImportWizard for lexicon/interlinear imports.

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
