# Transforms

## Purpose
Transformation assets (e.g., XSLT) and helpers for FieldWorks. Contains XSLT stylesheets and transformation definitions used for converting data between formats, generating reports, and exporting content.

## Key Components
- XSLT stylesheets for various transformations
- Transformation configuration files
- Export templates
- Report generation templates

## Technology Stack
- XSLT (eXtensible Stylesheet Language Transformations)
- XML processing
- Template-based document generation

## Dependencies
- Depends on: XML data model
- Used by: FXT (transform tool), export features, report generation

## Build Information
- Resource files and XSLT templates
- No compilation required (data files)
- Packaged with application for runtime use

## Entry Points
- XSLT files loaded by transformation engine
- Used during export, report generation, and data conversion

## Related Folders
- **FXT/** - FieldWorks transform tool that applies these XSLT files
- **DocConvert/** - Document conversion using transformations
- **ParatextImport/** - May use transforms for data mapping
- **LexText/** - Uses transforms for dictionary export and formatting
