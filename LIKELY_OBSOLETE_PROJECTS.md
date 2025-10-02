# Likely Obsolete Projects in FieldWorks Source Tree

This document lists projects that appear to be obsolete based on the criteria defined in `OBSOLETE_PROJECT_CRITERIA.md`.

## Strongly Recommended for Removal

### 1. **FxtExe** (`Src/FXT/FxtExe`)
**Criteria Met:** Primary #4 (Marked as Experimental)
- **Reason**: Explicitly excluded from normal builds in `CollectTargets.cs` with comment "These projects are experimental"
- **Current References**: Referenced by `LCMBrowser` but LCMBrowser itself may be obsolete (see below)
- **Original Purpose**: Executable for FXT (FieldWorks XML Transformation) processing
- **Risk Assessment**: Low - already excluded from builds
- **Recommendation**: Remove or move to archive

### 2. **ProjectUnpacker** (`Src/ProjectUnpacker`)
**Criteria Met:** Primary #4 (Marked as Experimental)
- **Reason**: Explicitly excluded from normal builds in `CollectTargets.cs` with comment "This is only used in tests"
- **Current References**: Referenced only by test projects (`ScriptureUtilsTests`, `ParatextImportTests`)
- **Original Purpose**: Test utility to unpack project data for testing
- **Risk Assessment**: Low - only used in test scenarios, can be replaced with modern test data management
- **Recommendation**: Remove or consolidate test data approach

### 3. **MSSQLMigration/OldMigrationScripts** (`DistFiles/MSSQLMigration/OldMigrationScripts`)
**Criteria Met:** Primary #2 (Platform Obsolescence), Secondary #6 (Documentation Indicators - "Old" in path)
- **Reason**: Historical migration scripts for database versions (200185-200241) no longer supported
- **Current References**: None - directory contains only old SQL migration scripts
- **Original Purpose**: Migrate SQL Server databases from ancient versions of FieldWorks
- **Current Status**: These migrations have been completed and are no longer needed
- **Risk Assessment**: Very low - historical artifacts only
- **Recommendation**: Archive for historical reference, remove from active codebase

### 4. **Bin/nmock** (`Bin/nmock/src/`)
**Criteria Met:** Primary #3 (Functionality Replaced), Secondary #5 (Limited codebase)
- **Reason**: Old mocking framework (NMock v1) bundled in repository; modern alternatives available
- **Current References**: Only referenced by `FlexPathwayPluginTests` (1 test project)
- **Original Purpose**: Provide mocking capabilities for unit tests
- **Replacement**: Modern alternatives like NSubstitute, Moq, or FakeItEasy
- **Risk Assessment**: Low - single test project can be migrated
- **Recommendation**: Migrate the one test project to modern mocking framework and remove

### 5. **Bin/nunitforms** (`Bin/nunitforms/source/`)
**Criteria Met:** Primary #3 (Functionality Replaced), Secondary #5 (Limited usage)
- **Reason**: Old WinForms testing framework bundled in repository
- **Current References**: Only referenced by `MessageBoxExLibTests` (1 test project)
- **Original Purpose**: Test Windows Forms UI components
- **Replacement**: Modern UI testing approaches or manual testing
- **Risk Assessment**: Low - single test project can be updated
- **Recommendation**: Evaluate if MessageBoxExLib tests are still needed; update or remove

## Recommended for Investigation

### 6. **SfmStats** (`Src/Utilities/SfmStats`)
**Criteria Met:** Primary #1 (Not Referenced), Secondary #5 (Limited codebase)
- **Reason**: No references found in any active project files
- **Current References**: None detected
- **Original Purpose**: Likely statistics/analysis tool for SFM (Standard Format Marker) files
- **Investigation Needed**: Verify if this is a standalone utility still used outside the build system
- **Recommendation**: If not actively used, remove

### 7. **GenerateHCConfig** (`Src/GenerateHCConfig`)
**Criteria Met:** Primary #1 (Not Referenced)
- **Reason**: No references found in any active project files
- **Current References**: None detected
- **Original Purpose**: Generate Help Compiler configuration files
- **Investigation Needed**: Check if this is run as a standalone tool during documentation builds
- **Recommendation**: If not part of active documentation workflow, remove

### 8. **LCMBrowser** (`Src/LCMBrowser`)
**Criteria Met:** Primary #1 (Limited References), Secondary #5 (Depends on potentially obsolete ObjectBrowser)
- **Reason**: Only referenced by FxtExe, which is itself marked experimental
- **Current References**: Referenced only by `FxtExe` (experimental project)
- **Original Purpose**: Browser/inspector for Language and Culture Model data
- **Dependencies**: Depends on `ObjectBrowser` from Lib/src
- **Investigation Needed**: Determine if this is a useful debugging tool or obsolete
- **Recommendation**: If FxtExe is removed, evaluate if LCMBrowser has independent value

### 9. **ObjectBrowser** (`Lib/src/ObjectBrowser`)
**Criteria Met:** Primary #1 (Limited References)
- **Reason**: Only referenced by LCMBrowser
- **Current References**: Only `LCMBrowser` references this
- **Original Purpose**: Generic object inspection/browsing utility
- **Investigation Needed**: Determine if this is used as a standalone debugging tool
- **Recommendation**: If LCMBrowser is removed and no standalone usage, remove

### 10. **InstallValidator** (`Src/InstallValidator`)
**Criteria Met:** Primary #1 (Not Referenced except by own tests)
- **Reason**: Not referenced by any projects except its own test project
- **Current References**: Only `InstallValidatorTests` references it
- **Original Purpose**: Validate FieldWorks installation
- **Investigation Needed**: Check if this is run as standalone utility during installation/setup
- **Recommendation**: If not part of active installation process, remove

### 11. **Converter Projects** (`Lib/src/Converter/`)
**Criteria Met:** Unclear - requires investigation
- **Projects**: `ConvertLib`, `Converter`, `ConverterConsole`
- **Reason**: No direct project references found in Src/ projects
- **Note**: Some projects reference `SilEncConverters40` (external encoding converter)
- **Investigation Needed**: Determine if these are standalone tools or if they're obsolete
- **Recommendation**: Verify purpose and current usage

### 12. **CacheLight** (`Src/CacheLight`)
**Criteria Met:** Primary #1 (Limited References - only test projects)
- **Reason**: Only referenced by test projects (`SimpleRootSiteTests`, `XMLViewsTests`, and own `CacheLightTests`)
- **Current References**: 2 test projects outside of its own tests
- **Original Purpose**: Lightweight cache implementation for testing scenarios
- **Investigation Needed**: Determine if test projects still need this or can use alternatives
- **Recommendation**: Evaluate if test projects can be updated to not need this

### 13. **Design** (`Src/Common/Controls/Design`)
**Criteria Met:** Unclear - limited usage
- **Reason**: Contains design-time components for Visual Studio/WinForms designer
- **Current References**: No project references found (used at design time only)
- **Original Purpose**: Custom designers for WinForms controls
- **Investigation Needed**: Verify if custom designers are still needed for development
- **Recommendation**: If team no longer uses WinForms designer extensively, may be obsolete

### 14. **FormLanguageSwitch** (`Lib/src/FormLanguageSwitch`)
**Criteria Met:** Primary #1 (Single Reference)
- **Reason**: Only referenced by `LexTextControls`
- **Current References**: Only `LexTextControls` uses this
- **Original Purpose**: Switch UI language in forms
- **Investigation Needed**: Verify if this functionality is still needed or has been replaced
- **Recommendation**: If functionality is still needed, consider consolidating into LexTextControls

### 15. **MigrateSqlDbs** (`Src/MigrateSqlDbs`)
**Criteria Met:** Primary #2 (Platform Obsolescence - SQL Server migration)
- **Reason**: Windows-only migration tool for SQL Server databases
- **Current References**: Conditionally built only on Windows (marked in CollectTargets.cs)
- **Original Purpose**: Migrate projects from SQL Server to XML backend
- **Current Status**: Migration was for FW 7.0 transition, completed years ago
- **Investigation Needed**: Verify if any users still need to migrate from SQL Server
- **Recommendation**: If migration period is complete, remove; otherwise document end-of-life plan

## Summary Statistics

- **Strong candidates for removal**: 5 projects
- **Recommended for investigation**: 10 projects
- **Total projects evaluated**: 15 (out of 125+ total projects)

## Next Steps

For each project listed above:

1. **Verify current usage** - Check with team if any projects are still used as standalone tools
2. **Plan migration** - For projects with dependencies, plan migration path
3. **Create removal issues** - Create individual GitHub issues for each removal decision
4. **Archive vs. Delete** - Decide appropriate disposition for each project
5. **Document decisions** - Update this document with final decisions and actions taken

## Notes

- Some projects may be standalone utilities not referenced in csproj files but still used
- Projects in `Bin/` directory are typically bundled third-party libraries
- Test-only projects have lower risk for removal
- Build-time and design-time tools may not show up in project references
