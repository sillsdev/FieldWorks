---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FixFwDataDll

## Purpose
Core data repair library for FieldWorks. Provides the implementation of data validation, error detection, and automatic repair functionality for FieldWorks databases.

## Key Components
### Key Classes
- **FixErrorsDlg**
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

## Code Evidence
*Analysis based on scanning 4 source files*

- **Classes found**: 4 public classes
- **Namespaces**: SIL.FieldWorks.FixData

## Interfaces and Data Models

- **ErrorFixer** (class)
  - Path: `ErrorFixer.cs`
  - Public class implementation

- **FwData** (class)
  - Path: `FwData.cs`
  - Public class implementation

- **WriteAllObjectsUtility** (class)
  - Path: `WriteAllObjectsUtility.cs`
  - Public class implementation

## References

- **Project files**: FixFwDataDll.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ErrorFixer.cs, FixErrorsDlg.Designer.cs, FixErrorsDlg.cs, FwData.cs, Strings.Designer.cs, WriteAllObjectsUtility.cs
- **Source file count**: 7 files
- **Data file count**: 2 files
