# MigrateSqlDbs

## Purpose
Database migration and upgrade tooling for FieldWorks. Handles schema migrations, data transformations, and version upgrades when moving between different versions of FieldWorks.

## Key Components
- **MigrateSqlDbs.csproj** - Database migration tool

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
