---
last-reviewed: 2025-10-31
last-reviewed-tree: 7429723c263755549ddf40ff3f313eec90f6ba20928a4451597ffe4e28116b78
status: reviewed
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - migration-entry-point
  - user-dialogs
  - migration-logic-importfrom60
  - ldml-writing-system-migration
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
  - Outputs: Migration progress dialog, converted XML databases, return code for installer

### User Dialogs
- **MigrateProjects**: Main dialog listing SQL Server projects for migration. Uses ExistingProjectDlg for project enumeration, provides checkboxes for multi-project selection, invokes ImportFrom6_0 converter for each selected project. Shows progress via ProgressDialogWithTask.
- **ExistingProjectDlg**: Dialog enumerating existing FW6 SQL Server projects from registry/database queries
- **FWVersionTooOld**: Warning dialog when FW6 version < 5.4 detected (too old to migrate)

### Migration Logic (ImportFrom6_0)
- **ImportFrom6_0**: Handles actual SQL→XML conversion. Invoked from MigrateProjects. Launches ConverterConsole.exe (external process) to perform database export/import. Checks for SQL Server installation, validates FW version compatibility. Located in LCModel.DomainServices.DataMigration (dependency, not in this project).
  - Inputs: SQL Server connection strings, target XML file paths, ProgressDialogWithTask for UI feedback
  - Executables: ConverterConsole.exe (FwDirectoryFinder.ConverterConsoleExe), db.exe (FwDirectoryFinder.DbExe)

### LDML Writing System Migration
- **LdmlInFolderWritingSystemRepositoryMigrator**: Migrates global writing system LDML files from version 1→2. Runs before project migration to ensure compatibility. Targets OldGlobalWritingSystemStoreDirectory.
  - Note: Comment mentions TODO for migrating to version 3

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Application type**: WinExe (Windows GUI application with console fallback)
- **UI framework**: System.Windows.Forms (WinForms dialogs)
- **Key libraries**:
  - LCModel (LcmCache, ProjectId, LcmFileHelper)
  - LCModel.DomainServices.DataMigration (ImportFrom6_0)
  - SIL.WritingSystems.Migration (LDML migration)
  - System.Data.SqlClient (SQL Server connectivity)
  - Common/Controls (ProgressDialogWithTask, ThreadHelper)
  - Common/FwUtils (FwRegistryHelper, FwDirectoryFinder)
  - Microsoft.Win32 (Registry access for FW6 project discovery)

## Dependencies
- **External**: LCModel (LcmCache, LcmFileHelper, ProjectId), LCModel.DomainServices.DataMigration (ImportFrom6_0), SIL.WritingSystems.Migration (LdmlInFolderWritingSystemRepositoryMigrator), Common/Controls (ProgressDialogWithTask, ThreadHelper), Common/FwUtils (FwRegistryHelper, FwDirectoryFinder), System.Data.SqlClient (SQL Server connectivity), System.Windows.Forms (WinForms dialogs), Microsoft.Win32 (Registry access)
- **Internal (upstream)**: LCModel (data migration infrastructure), Common/Controls (progress dialogs), Common/FwUtils (FW configuration)
- **Consumed by**: FieldWorks installer (FLExInstaller launches MigrateSqlDbs.exe during FW6→FW7 upgrade), standalone execution for manual migrations

## Interop & Contracts
- **External process invocation**: Launches ConverterConsole.exe for SQL→XML conversion
  - Process: ConverterConsole.exe (via FwDirectoryFinder.ConverterConsoleExe)
  - Purpose: External utility handling actual database export/import
  - Communication: Command-line arguments, process exit code, standard output/error
- **SQL Server connectivity**: System.Data.SqlClient for database queries
  - Purpose: Enumerate FW6 SQL Server projects, validate versions
  - Queries: Project metadata from SQL Server system tables
- **Registry access**: Microsoft.Win32.Registry for FW6 installation discovery
  - Purpose: Locate SQL Server instances, enumerate registered FW6 projects
  - Keys: HKLM\\Software\\SIL\\FieldWorks (FW6 installation paths)
- **File system contracts**:
  - XML project files: Output from migration (FW7 XML format)
  - LDML files: Global writing system definitions (version 1→2 migration)
- **Return codes**: Program.Main() exit codes for installer automation
  - -1: No SQL Server detected
  - 0: Success or nothing to migrate
  - >0: Number of failed migrations
- **UI contracts**: ProgressDialogWithTask for long-running operations (importation feedback)

## Threading & Performance
UI thread for dialogs. ProgressDialogWithTask marshals migration to background thread. ConverterConsole.exe runs as external process.

## Config & Feature Flags
Command-line flags: -debug, -autoclose, -chars (deprecated). Registry configuration via FwRegistryHelper. Version checks for FW6 >= 5.4.

## Build Information
C# WinExe (net48). Build via `msbuild MigrateSqlDbs.csproj`. Output: MigrateSqlDbs.exe.

## Interfaces and Data Models
ProjectId for project identification. ImportFrom6_0 for SQL→XML migration. UI forms: MigrateProjects, ExistingProjectDlg, FWVersionTooOld. ConverterConsole.exe external process.

## Entry Points
Program.Main() entry point. Invoked by FLExInstaller during FW6→FW7 upgrade. Workflow: check SQL Server, validate FW6 version, launch MigrateProjects dialog, migrate selected projects.

## Test Index
- **No automated tests**: Legacy migration tool without dedicated test project
- **Manual testing approach**:
  - Setup: Install FW6 with SQL Server, create test projects
  - Execution: Run MigrateSqlDbs.exe, verify XML databases created
  - Validation: Open migrated projects in FW7+, verify data integrity
- **Test scenarios** (historical):
  - Single project migration: Select one FW6 project, verify successful conversion
  - Multi-project migration: Select multiple projects, verify batch processing
  - Version check: Install FW6 < 5.4, verify FWVersionTooOld dialog
  - LDML migration: Verify global writing system files migrated v1→2
  - SQL Server missing: Verify -1 exit code when SQL Server not installed
  - Command-line flags: Test -debug, -autoclose, -chars deprecated warning
- **Integration testing**: Embedded in FLExInstaller upgrade tests (FW6→FW7 upgrade path)
- **Current status**: Legacy tool; no active testing (SQL Server backend deprecated since FW7)

## Usage Hints
- **Historical context**: This tool is for FW6→FW7 upgrade only
  - FW7+ uses XML backend exclusively
  - Subsequent migrations (7.x→8.x→9.x) handled by DataMigration infrastructure, not this tool
- **Typical usage** (historical):
  1. Install FW7 over FW6 (installer invokes MigrateSqlDbs.exe automatically)
  2. Or run manually: `MigrateSqlDbs.exe` from command line
  3. Dialog lists SQL Server projects found in registry
  4. Check projects to migrate, click OK
  5. Progress dialog shows conversion status
  6. Open migrated projects in FW7+
- **Command-line options**:
  - Debug mode: `MigrateSqlDbs.exe -debug` for verbose logging
  - Auto-close: `MigrateSqlDbs.exe -autoclose` for unattended migration (installer use)
- **Prerequisites**:
  - SQL Server with FW6 databases
  - FW6 version >= 5.4 (earlier versions not supported)
  - Sufficient disk space for XML output (XML files larger than SQL databases)
- **Migration time**: Depends on database size
  - Small projects (<10K lexical entries): Minutes
  - Large projects (>100K lexical entries): Hours
- **Troubleshooting**:
  - Migration fails: Check SQL Server connectivity, verify FW6 version
  - Projects not listed: Verify registry entries, check SQL Server installation
  - Slow migration: Normal for large databases; external ConverterConsole.exe handles heavy lifting
- **Common pitfalls**:
  - Running on FW7+ installation (no SQL Server projects to migrate)
  - Insufficient disk space (XML files require ~2-3x SQL database size)
  - Interrupted migration (may leave partial XML file; re-run to retry)
- **Preservation**: Tool kept in codebase for archival/reference, not actively maintained

## Related Folders
- **LCModel/DomainServices/DataMigration/**: ImportFrom6_0
- **Common/Controls/**: ProgressDialogWithTask
- **FLExInstaller/**: Launcher

## References
11 C# files (~1.1K lines). Key: Program.cs, MigrateProjects.cs, ExistingProjectDlg.cs. See `.cache/copilot/diff-plan.json` for file listings.
