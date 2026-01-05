---
applyTo: "Src/Utilities/**"
name: "utilities.instructions"
description: "Auto-generated concise instructions from COPILOT.md for Utilities"
---

# Utilities (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **FixFwData**: WinExe entry point for data repair (Program.cs)
- **FixFwDataDll**: ErrorFixer, FwData, FixErrorsDlg, WriteAllObjectsUtility
- **MessageBoxExLib**: MessageBoxEx, MessageBoxExForm, MessageBoxExManager, MessageBoxExButton
- **Reporting**: ErrorReport, UsageEmailDialog, ReportingStrings
- **SfmStats**: SFM analysis tool (Program.cs, statistics generation)
- **SfmToXml**: Converter, LexImportFields, ClsHierarchyEntry, ConvertSFM tool, Phase3/4 XSLT

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: f9667c38932f4933889671a0f084cfb1ab1cbc79e150143423bba28fa564a88c
status: reviewed
---

# Utilities

## Purpose
Organizational parent folder containing 7 utility subfolders: FixFwData (data repair tool), FixFwDataDll (repair library), MessageBoxExLib (enhanced dialogs), Reporting (error reporting), SfmStats (SFM statistics), SfmToXml (Standard Format conversion), and XMLUtils (XML helper library). See individual subfolder COPILOT.md files for detailed documentation.

## Architecture
Organizational parent folder with no direct source files. Contains 7 utility subfolders, each with distinct purpose:
1. **FixFwData/FixFwDataDll**: Data repair tools (WinExe + library)
2. **MessageBoxExLib**: Enhanced dialog library
3. **Reporting**: Error reporting infrastructure
4. **SfmStats**: SFM analysis tool
5. **SfmToXml**: Standard Format converter
6. **XMLUtils**: Core XML utility library

Each subfolder is self-contained with own project files, source, and tests. See individual COPILOT.md files for detailed architecture.

## Key Components
This is an organizational folder. Key components are in subfolders:
- **FixFwData**: WinExe entry point for data repair (Program.cs)
- **FixFwDataDll**: ErrorFixer, FwData, FixErrorsDlg, WriteAllObjectsUtility
- **MessageBoxExLib**: MessageBoxEx, MessageBoxExForm, MessageBoxExManager, MessageBoxExButton
- **Reporting**: ErrorReport, UsageEmailDialog, ReportingStrings
- **SfmStats**: SFM analysis tool (Program.cs, statistics generation)
- **SfmToXml**: Converter, LexImportFields, ClsHierarchyEntry, ConvertSFM tool, Phase3/4 XSLT
- **XMLUtils**: XmlUtils, DynamicLoader, SimpleResolver, SILExceptions, IPersistAsXml

Total: 11 projects, ~52 C# files, 17 data files (XSLT, test data, resources)

## Technology Stack
No direct code at this organizational level. Subfolders use:
- **Languages**: C# (all projects)
- **Target frameworks**: .NET Framework 4.8.x (net48)
- **UI frameworks**: WinForms (FixFwD
