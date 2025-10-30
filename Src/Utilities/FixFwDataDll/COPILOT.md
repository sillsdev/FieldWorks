---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Utilities/FixFwDataDll

## Purpose
Core data repair library for FieldWorks. Provides the implementation of data validation, error detection, and automatic repair functionality for FieldWorks databases.

## Key Components
- **FixFwDataDll.csproj** - Data repair library
- **ErrorFixer.cs** - Error detection and repair logic
- **FixErrorsDlg** - UI dialog for interactive repair
- **FwData.cs** - FieldWorks data access and manipulation


## Key Classes/Interfaces
- **FwData**
- **WriteAllObjectsUtility**
- **ErrorFixer**

## Technology Stack
- C# .NET
- Data validation algorithms
- Database integrity checking

## Dependencies
- Depends on: Cellar (data model), Common utilities
- Used by: Utilities/FixFwData (command-line tool), applications for data validation

## Build Information
- C# class library project
- Build via: `dotnet build FixFwDataDll.csproj`
- Core repair functionality

## Entry Points
- ErrorFixer class for data repair
- FixErrorsDlg for interactive repair
- Data validation and integrity checking APIs

## Related Folders
- **Utilities/FixFwData/** - Command-line wrapper
- **Cellar/** - Data model accessed and repaired
- **MigrateSqlDbs/** - Database migration (related to data integrity)


## References
- **Project Files**: FixFwDataDll.csproj
- **Key C# Files**: ErrorFixer.cs, FixErrorsDlg.cs, FwData.cs, WriteAllObjectsUtility.cs
