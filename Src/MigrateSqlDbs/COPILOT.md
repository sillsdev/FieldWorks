---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# MigrateSqlDbs

## Purpose
Database migration and versioning infrastructure. 
Handles schema migrations, data transformations, and version upgrades between different 
FieldWorks releases. Ensures smooth upgrades by automatically transforming user data 
to match the current schema version while preserving data integrity.

## Key Components
### Key Classes
- **ExistingProjectDlg**
- **FWVersionTooOld**
- **Settings**
- **MigrateProjects**

## Technology Stack
- C# .NET
- SQL database operations
- Schema migration and data transformation
- Version upgrade logic

## Dependencies
- Depends on: Cellar (data model), DbExtend (schema extensions)
- Used by: Application startup and upgrade process

## Build Information
- C# executable or library
- Critical for database version compatibility
- Build with MSBuild or Visual Studio

## Entry Points
- Command-line tool or library for database migrations
- Invoked during application upgrades

## Related Folders
- **Cellar/** - Core data model being migrated
- **DbExtend/** - Schema extensions handled during migration
- **InstallValidator/** - May check database version compatibility

## Code Evidence
*Analysis based on scanning 5 source files*

- **Classes found**: 4 public classes
- **Namespaces**: SIL.FieldWorks.MigrateSqlDbs.MigrateProjects, SIL.FieldWorks.MigrateSqlDbs.MigrateProjects.Properties

## References

- **Project files**: MigrateSqlDbs.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ExistingProjectDlg.Designer.cs, ExistingProjectDlg.cs, FWVersionTooOld.Designer.cs, FWVersionTooOld.cs, MigrateProjects.Designer.cs, MigrateProjects.cs, Program.cs, Settings.Designer.cs, Settings.cs
- **Source file count**: 11 files
- **Data file count**: 4 files
