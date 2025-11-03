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
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **External**: LCModel (LcmCache, LcmFileHelper, ProjectId), LCModel.DomainServices.DataMigration (ImportFrom6_0), SIL.WritingSystems.Migration (LdmlInFolderWritingSystemRepositoryMigrator), Common/Controls (ProgressDialogWithTask, ThreadHelper), Common/FwUtils (FwRegistryHelper, FwDirectoryFinder), System.Data.SqlClient (SQL Server connectivity), System.Windows.Forms (WinForms dialogs), Microsoft.Win32 (Registry access)
- **Internal (upstream)**: LCModel (data migration infrastructure), Common/Controls (progress dialogs), Common/FwUtils (FW configuration)
- **Consumed by**: FieldWorks installer (FLExInstaller launches MigrateSqlDbs.exe during FW6→FW7 upgrade), standalone execution for manual migrations

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- Project type: C# WinExe application (net462)
- Build: `msbuild MigrateSqlDbs.csproj` or `dotnet build` (from FW.sln)
- Output: MigrateSqlDbs.exe (standalone executable)
- Dependencies: LCModel, LCModel.DomainServices.DataMigration, SIL.WritingSystems, Common/Controls, Common/FwUtils, System.Data.SqlClient
- Deployment: Included in FLEx installer for upgrade path support

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

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

## References (auto-generated hints)
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
