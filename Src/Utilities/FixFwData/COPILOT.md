---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FixFwData

## Purpose
Command-line tool for repairing FieldWorks data files. Provides automated data integrity checking and repair functionality for FieldWorks databases.

## Key Components
No major public classes identified.

## Technology Stack
- C# .NET console application
- Data validation and repair algorithms

## Dependencies
- Depends on: Utilities/FixFwDataDll (core repair logic), Cellar (data model)
- Used by: Administrators and support staff for data repair

## Build Information
- C# console application
- Build via: `dotnet build FixFwData.csproj`
- Command-line utility

## Entry Points
- Main() method with command-line argument parsing
- Invokes FixFwDataDll for actual repair work

## Related Folders
- **Utilities/FixFwDataDll/** - Core data repair library
- **Cellar/** - Data model being repaired
- **MigrateSqlDbs/** - Database migration (related functionality)

## Code Evidence
*Analysis based on scanning 1 source files*

- **Namespaces**: FixFwData
