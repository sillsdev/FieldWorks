---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# MigrateSqlDbs

## Purpose
Database migration and upgrade tooling for FieldWorks. Handles schema migrations, data transformations, and version upgrades when moving between different versions of FieldWorks.

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
