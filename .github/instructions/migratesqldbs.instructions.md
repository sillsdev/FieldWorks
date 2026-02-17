---
applyTo: "Src/MigrateSqlDbs/**"
name: "migratesqldbs.instructions"
description: "Auto-generated concise instructions from COPILOT.md for MigrateSqlDbs"
---

# MigrateSqlDbs (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **Program.Main()**: Application entry point. Parses command-line args (-debug, -autoclose, -chars deprecated), initializes FwRegistryHelper, migrates global LDML writing systems (LdmlInFolderWritingSystemRepositoryMigrator v1→2), creates ImportFrom6_0 instance, checks for SQL Server installation (IsFwSqlServerInstalled()), validates FW6 version (IsValidOldFwInstalled()), launches MigrateProjects dialog for user project selection. Returns: -1 (no SQL Server), 0 (success or nothing to migrate), >0 (number of failed migrations).
- Command-line flags:
- `-debug`: Enables debug mode for verbose logging
- `-autoclose`: Automatically close dialog after migration
- `-chars`: Deprecated flag (warns user to run UnicodeCharEditor -i instead)
- Inputs: Command-line args, FW6 SQL Server database registry entries

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: 9b9e9a2c7971185d92105247849e0b35f2305f8ae237f4ab3be1681a0b974464
status: reviewed
---

# MigrateSqlDbs

## Purpose
Legacy SQL Server to XML database migration utility for FieldWorks 6.0→7.0 upgrade. Console/GUI application (WinExe) detecting SQL Server FieldWorks projects, converting them to XML format via ImportFrom6_0, migrating LDML writing system files (version 1→2), and providing user selection dialog (MigrateProjects form) for batch migration. Historical tool for one-time FW6→FW7 upgrade; no longer actively used for new migrations but preserved for archival/reference. LCModel now uses XML backend exclusively, handles subsequent migrations (7.x→8.x→9.x) via DataMigration infrastructure.

## Architecture
C# WinExe application (net48) with 11 source files (~1.1K lines). Mix of WinForms dialogs (MigrateProjects, ExistingProjectDlg, FWVersionTooOld) and migration logic (Program.Main, ImportFrom6_0 integration). Command-line flags: -debug, -autoclose, -chars (deprecated).

## Key Components

### Migration Entry Point
- **Program.Main()**: Application entry point. Parses command-line args (-debug, -autoclose, -chars deprecated), initializes FwRegistryHelper, migrates global LDML writing systems (LdmlInFolderWritingSystemRepositoryMigrator v1→2), creates ImportFrom6_0 instance, checks for SQL Server installation (IsFwSqlServerInstalled()), validates FW6 version (IsValidOldFwInstalled()), launches MigrateProjects dialog for user project selection. Returns: -1 (no SQL Server), 0 (success or nothing to migrate), >0 (number of failed migrations).
  - Command-line flags:
    - `-debug`: Enables debug mode for verbose logging
    - `-autoclose`: Automatically close dialog after migration
    - `-chars`: Deprecated flag (warns user to run UnicodeCharEditor -i instead)
  - Inputs: Command-line args, FW6 SQL Server database registry entries
  - Outputs: Migration progress dialog, converted XML databases, return code for install
