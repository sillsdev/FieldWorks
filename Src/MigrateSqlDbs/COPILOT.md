---
last-reviewed: 2025-10-31
last-reviewed-tree: 9b9e9a2c7971185d92105247849e0b35f2305f8ae237f4ab3be1681a0b974464
status: reviewed
---

# MigrateSqlDbs

## Purpose
Legacy SQL Server to XML database migration utility for FieldWorks 6.0→7.0 upgrade. Console/GUI application (WinExe) detecting SQL Server FieldWorks projects, converting them to XML format via ImportFrom6_0, migrating LDML writing system files (version 1→2), and providing user selection dialog (MigrateProjects form) for batch migration. Historical tool for one-time FW6→FW7 upgrade; no longer actively used for new migrations but preserved for archival/reference. LCModel now uses XML backend exclusively, handles subsequent migrations (7.x→8.x→9.x) via DataMigration infrastructure.

## Architecture
C# WinExe application (net462) with 11 source files (~1.1K lines). Mix of WinForms dialogs (MigrateProjects, ExistingProjectDlg, FWVersionTooOld) and migration logic (Program.Main, ImportFrom6_0 integration). Command-line flags: -debug, -autoclose, -chars (deprecated).

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
- **Target framework**: .NET Framework 4.6.2 (net462)
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
- **UI thread**: Main WinForms dialogs run on UI thread (MigrateProjects, ExistingProjectDlg)
- **Background work**: ProgressDialogWithTask marshals long-running migration to background thread
  - Migration operations: ImportFrom6_0 execution on worker thread
  - UI updates: Progress callbacks marshaled back to UI thread
- **External process**: ConverterConsole.exe runs as separate process
  - Asynchronous: Application waits for process completion
  - Resource-intensive: SQL export/import can take minutes for large databases
- **Performance characteristics**:
  - SQL queries: Fast (enumerate projects from registry/database metadata)
  - LDML migration: Fast (file copy/rename of writing system definitions)
  - Project migration: Slow (minutes per project, depends on database size)
- **Synchronous operations**: All dialog interactions synchronous, migration progress shown via ProgressDialogWithTask
- **No manual threading**: Relies on ProgressDialogWithTask for background work, Process.WaitForExit() for external process

## Config & Feature Flags
- **Command-line flags**:
  - `-debug`: Enable debug mode (verbose logging, diagnostic output)
  - `-autoclose`: Automatically close dialog after migration completes
  - `-chars`: Deprecated flag (warns user to run UnicodeCharEditor -i instead for character database migration)
- **Registry configuration**: FwRegistryHelper reads FW6 installation paths
  - SQL Server instances: Discovered from registry entries
  - Project locations: Enumerated from FW6 registry keys
- **Version checks**:
  - IsValidOldFwInstalled(): Validates FW6 version >= 5.4 (earlier versions not supported)
  - IsFwSqlServerInstalled(): Checks for SQL Server presence
- **LDML version**: Migrates writing systems from version 1→2
  - TODO comment mentions future migration to version 3
- **Migration scope**:
  - Global writing systems: Always migrated (OldGlobalWritingSystemStoreDirectory)
  - Projects: User-selected via checkboxes in MigrateProjects dialog
- **No config files**: All configuration from registry, command-line args, and hardcoded paths

## Build Information
- Project type: C# WinExe application (net462)
- Build: `msbuild MigrateSqlDbs.csproj` or `dotnet build` (from FieldWorks.sln)
- Output: MigrateSqlDbs.exe (standalone executable)
- Dependencies: LCModel, LCModel.DomainServices.DataMigration, SIL.WritingSystems, Common/Controls, Common/FwUtils, System.Data.SqlClient
- Deployment: Included in FLEx installer for upgrade path support

## Interfaces and Data Models

### Data Models
- **ProjectId** (from LCModel)
  - Purpose: Identifies FieldWorks project (name, path, type)
  - Shape: Name (string), Path (string), Type (ProjectType enum)
  - Consumers: ExistingProjectDlg enumerates projects, MigrateProjects displays for selection

- **ImportFrom6_0** (from LCModel.DomainServices.DataMigration)
  - Purpose: SQL→XML migration coordinator
  - Methods: PerformMigration(), CanMigrate(), LaunchConverterConsole()
  - Inputs: SQL connection string, target XML path, ProgressDialogWithTask
  - Outputs: Success/failure status, converted XML database

### UI Contracts
- **MigrateProjects form**
  - Purpose: Multi-project selection dialog
  - Controls: CheckedListBox (projects), OK/Cancel buttons, progress indicators
  - Data binding: List<ProjectId> for project enumeration

- **ExistingProjectDlg form**
  - Purpose: Enumerate and select FW6 SQL Server projects
  - Methods: GetProjectList(), ValidateSelection()

- **FWVersionTooOld form**
  - Purpose: Warning dialog for incompatible FW6 versions
  - Condition: FW6 version < 5.4

### Process Contracts
- **ConverterConsole.exe**
  - Purpose: External SQL→XML conversion utility
  - Invocation: Process.Start() with command-line arguments
  - Arguments: Source SQL connection, target XML path, options
  - Exit codes: 0 = success, non-zero = failure

## Entry Points
- **Program.Main(string[] args)**: Console/WinExe application entry point
  - Invocation: `MigrateSqlDbs.exe [-debug] [-autoclose] [-chars]`
  - Workflow:
    1. Parse command-line arguments
    2. Initialize FwRegistryHelper
    3. Migrate global LDML writing systems (v1→2)
    4. Check SQL Server installation (IsFwSqlServerInstalled())
    5. Validate FW6 version (IsValidOldFwInstalled())
    6. Launch MigrateProjects dialog for user project selection
    7. Invoke ImportFrom6_0 for each selected project
    8. Return exit code for installer automation
- **Installer invocation**: FLExInstaller launches MigrateSqlDbs.exe during FW6→FW7 upgrade
  - Automated: Installer monitors exit code to determine success/failure
  - User-visible: Progress dialog shown during migration
- **Manual execution**: User can run MigrateSqlDbs.exe standalone for manual migration
  - Typical use: Troubleshooting failed installer migrations, selective project migration
- **Dialog entry points**:
  - MigrateProjects.ShowDialog(): Main project selection UI
  - ExistingProjectDlg.GetProjectList(): Project enumeration
  - FWVersionTooOld.ShowDialog(): Version warning

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
- **LCModel/DomainServices/DataMigration/**: Contains ImportFrom6_0 and data migration infrastructure (ongoing XML-based migrations FW7+)
- **Common/Controls/**: ProgressDialogWithTask, ThreadHelper used for UI feedback
- **Common/FwUtils/**: FwRegistryHelper, FwDirectoryFinder for FW configuration
- **FLExInstaller/**: Launches MigrateSqlDbs.exe during FW6→FW7 upgrade workflow

## References
- **Source files**: 11 C# files (~1.1K lines): Program.cs, MigrateProjects.cs, ExistingProjectDlg.cs, FWVersionTooOld.cs, Settings.cs, Designer files, AssemblyInfo.cs
- **Project file**: MigrateSqlDbs.csproj
- **Key classes**: Program (Main entry point), MigrateProjects (main dialog), ExistingProjectDlg, FWVersionTooOld
- **Key dependencies**: ImportFrom6_0 (LCModel.DomainServices.DataMigration), LdmlInFolderWritingSystemRepositoryMigrator (SIL.WritingSystems.Migration)
- **External executables**: ConverterConsole.exe (SQL export/import), db.exe (database operations)
- **Namespace**: SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
- **Target framework**: net462
- **Return codes**: -1 (no SQL Server), 0 (success), >0 (failures count)
  - Src/MigrateSqlDbs/FWVersionTooOld.Designer.cs
  - Src/MigrateSqlDbs/FWVersionTooOld.cs
  - Src/MigrateSqlDbs/MigrateProjects.Designer.cs
  - Src/MigrateSqlDbs/MigrateProjects.cs
  - Src/MigrateSqlDbs/Program.cs
  - Src/MigrateSqlDbs/Properties/AssemblyInfo.cs
  - Src/MigrateSqlDbs/Properties/Resources.Designer.cs
  - Src/MigrateSqlDbs/Properties/Settings.Designer.cs
  - Src/MigrateSqlDbs/Settings.cs
- Data contracts/transforms:
  - Src/MigrateSqlDbs/ExistingProjectDlg.resx
  - Src/MigrateSqlDbs/FWVersionTooOld.resx
  - Src/MigrateSqlDbs/MigrateProjects.resx
  - Src/MigrateSqlDbs/Properties/Resources.resx

## Auto-Generated Project and File References
- Project files:
  - Src/MigrateSqlDbs/MigrateSqlDbs.csproj
- Key C# files:
  - Src/MigrateSqlDbs/ExistingProjectDlg.Designer.cs
  - Src/MigrateSqlDbs/ExistingProjectDlg.cs
  - Src/MigrateSqlDbs/FWVersionTooOld.Designer.cs
  - Src/MigrateSqlDbs/FWVersionTooOld.cs
  - Src/MigrateSqlDbs/MigrateProjects.Designer.cs
  - Src/MigrateSqlDbs/MigrateProjects.cs
  - Src/MigrateSqlDbs/Program.cs
  - Src/MigrateSqlDbs/Properties/AssemblyInfo.cs
  - Src/MigrateSqlDbs/Properties/Resources.Designer.cs
  - Src/MigrateSqlDbs/Properties/Settings.Designer.cs
  - Src/MigrateSqlDbs/Settings.cs
- Data contracts/transforms:
  - Src/MigrateSqlDbs/ExistingProjectDlg.resx
  - Src/MigrateSqlDbs/FWVersionTooOld.resx
  - Src/MigrateSqlDbs/MigrateProjects.resx
  - Src/MigrateSqlDbs/Properties/Resources.resx
## Test Information
- No dedicated test project found
- Testing: Manual execution against FW6 SQL Server databases
- Historical tool: Active testing only for FW6→FW7 migrations (no longer primary use case)

## Code Evidence
*Analysis based on scanning 5 source files*

- **Classes found**: 4 public classes
- **Namespaces**: SIL.FieldWorks.MigrateSqlDbs.MigrateProjects, SIL.FieldWorks.MigrateSqlDbs.MigrateProjects.Properties
