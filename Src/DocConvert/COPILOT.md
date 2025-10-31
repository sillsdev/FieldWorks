---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# DocConvert

## Purpose
Document and data format conversion utilities. Handles transformation between
different document formats and data representations used in FieldWorks. Supports importing
and exporting linguistic data in various industry-standard formats.

## Architecture
No source files found in this folder.

## Key Components
No major public classes identified.

## Technology Stack
- Mix of native and managed code
- Document processing
- Format conversion pipelines

## Dependencies
- Depends on: Common utilities, data model (Cellar)
- Used by: Import/export functionality in applications

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- No standalone project file in root (built as part of solution)
- May contain utilities and libraries used by other components

## Interfaces and Data Models
See code analysis sections above for key interfaces and data models. Additional interfaces may be documented in source files.

## Entry Points
- Provides document conversion APIs
- Used during import/export operations

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Transforms/** - XSLT and transformation assets for document conversion
- **ParatextImport/** - Specialized import for Paratext data
- **FXT/** - FieldWorks transform tools that may use DocConvert

## References

- **Source file count**: 0 files
- **Data file count**: 0 files

## References (auto-generated hints)
## Code Evidence
*Analysis based on scanning 0 source files*
