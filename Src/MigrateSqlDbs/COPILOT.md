---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
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

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\MigrateSqlDbs\MigrateSqlDbs.csproj
- Key C# files:
  - Src\MigrateSqlDbs\ExistingProjectDlg.Designer.cs
  - Src\MigrateSqlDbs\ExistingProjectDlg.cs
  - Src\MigrateSqlDbs\FWVersionTooOld.Designer.cs
  - Src\MigrateSqlDbs\FWVersionTooOld.cs
  - Src\MigrateSqlDbs\MigrateProjects.Designer.cs
  - Src\MigrateSqlDbs\MigrateProjects.cs
  - Src\MigrateSqlDbs\Program.cs
  - Src\MigrateSqlDbs\Properties\AssemblyInfo.cs
  - Src\MigrateSqlDbs\Properties\Resources.Designer.cs
  - Src\MigrateSqlDbs\Properties\Settings.Designer.cs
  - Src\MigrateSqlDbs\Settings.cs
- Data contracts/transforms:
  - Src\MigrateSqlDbs\ExistingProjectDlg.resx
  - Src\MigrateSqlDbs\FWVersionTooOld.resx
  - Src\MigrateSqlDbs\MigrateProjects.resx
  - Src\MigrateSqlDbs\Properties\Resources.resx
