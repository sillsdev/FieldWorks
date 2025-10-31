---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FixFwDataDll

## Purpose
Core data repair library implementing validation and fix logic.
Contains the actual implementation of data integrity checks, error detection algorithms, and
automatic repair routines. Used by both the FixFwData command-line tool and potentially by
the main applications for data validation.

## Architecture
C# library with 7 source files.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build FixFwDataDll.csproj`
- Core repair functionality

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

## Entry Points
- ErrorFixer class for data repair
- FixErrorsDlg for interactive repair
- Data validation and integrity checking APIs

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Utilities/FixFwData/** - Command-line wrapper
- **Cellar/** - Data model accessed and repaired
- **MigrateSqlDbs/** - Database migration (related to data integrity)

## References

- **Project files**: FixFwDataDll.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ErrorFixer.cs, FixErrorsDlg.Designer.cs, FixErrorsDlg.cs, FwData.cs, Strings.Designer.cs, WriteAllObjectsUtility.cs
- **Source file count**: 7 files
- **Data file count**: 2 files

## References (auto-generated hints)
- Project files:
  - Utilities/FixFwDataDll/FixFwDataDll.csproj
- Key C# files:
  - Utilities/FixFwDataDll/ErrorFixer.cs
  - Utilities/FixFwDataDll/FixErrorsDlg.Designer.cs
  - Utilities/FixFwDataDll/FixErrorsDlg.cs
  - Utilities/FixFwDataDll/FwData.cs
  - Utilities/FixFwDataDll/Properties/AssemblyInfo.cs
  - Utilities/FixFwDataDll/Strings.Designer.cs
  - Utilities/FixFwDataDll/WriteAllObjectsUtility.cs
- Data contracts/transforms:
  - Utilities/FixFwDataDll/FixErrorsDlg.resx
  - Utilities/FixFwDataDll/Strings.resx
## Code Evidence
*Analysis based on scanning 4 source files*

- **Classes found**: 4 public classes
- **Namespaces**: SIL.FieldWorks.FixData
