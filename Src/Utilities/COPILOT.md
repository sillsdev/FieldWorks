# Utilities

## Purpose
Miscellaneous utilities used across the FieldWorks repository. Contains various standalone tools, helper applications, and utility libraries that don't fit into other specific categories.

## Key Components

### Subprojects
- **FixFwData/FixFwData.csproj** - Tool for repairing FieldWorks data
- **FixFwDataDll/FixFwDataDll.csproj** - Data repair library
- **XMLUtils/XMLUtils.csproj** - XML processing utilities
- **MessageBoxExLib/MessageBoxExLib.csproj** - Enhanced message box library
- **SfmStats/SfmStats.csproj** - SFM (Standard Format Marker) statistics tool

## Technology Stack
- C# .NET
- Various utility functions
- Data repair and validation
- XML processing

## Dependencies
- Varies by subproject
- Generally depends on: Common utilities, Cellar (for data access)
- Used by: Various components needing utility functionality

## Build Information
- Multiple C# projects in subfolders
- Mix of executables and libraries
- Build with MSBuild or Visual Studio

## Entry Points
- **FixFwData** - Command-line or GUI tool for data repair
- **XMLUtils** - Library for XML operations
- **MessageBoxExLib** - Enhanced dialog library

## Related Folders
- **Cellar/** - Data model that FixFwData works with
- **Common/** - Shared utilities that Utilities extends
- **MigrateSqlDbs/** - Database migration (related to data repair)
