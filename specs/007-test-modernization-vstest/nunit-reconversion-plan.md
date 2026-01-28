# NUnit Re-conversion Plan (Two-Commit Approach)

## Problem Summary

The NUnit 3→4 conversion on this branch has bugs where assertion arguments were swapped incorrectly. The SDK migration commit (`a9f323eac`) introduced these errors during a rebase. Commits after `a9f323eac` made targeted fixes but on top of the buggy conversion base.

## Key Commits

- `a9f323eac` - SDK migration commit (rebased, introduced NUnit conversion errors)
- `575eaa0ec` - VSTest execution fixes
- `9eff1d477` - Test failure fixes (branch tip before this work)

## Fix Strategy

### Commit 1: Clean NUnit Conversion from release/9.3 ✅ DONE
1. Checkout ALL test files from `release/9.3` baseline
2. Run the `convert_nunit.py` script to get correct NUnit 4 syntax
3. Restores 3 files unintentionally deleted in SDK migration
4. Committed as: `fix(tests): clean NUnit 3->4 conversion from release/9.3 baseline`

### Commit 2: Re-apply Branch-Specific Fixes
**Scope: Changes between `a9f323eac` and `9eff1d477`** (47 test files)

These are intentional fixes made AFTER the buggy SDK migration:
1. Checkout files from `9eff1d477` (branch tip with fixes)
2. Re-run conversion script to fix any lingering NUnit issues
3. Commit as: `fix(tests): apply VSTest and test failure fixes from branch`

## Files Changed Between a9f323eac..9eff1d477 (47 files)

### Comprehensive Diff Analysis

| File | Assert +/- | Other +/- | Category |
|------|-----------|-----------|----------|
| MetaDataCacheTests.cs | +3/-3 | +0/-0 | Assert-only |
| FontHeightAdjusterTests.cs | +2/-2 | +0/-0 | Assert-only |
| **FieldWorksTests.cs** | +0/-0 | **+18/-13** | **Real fix** (path fixes for cross-platform) |
| **AssemblySetupFixture.cs** | +0/-0 | **+18/-0** | **NEW FILE** (test setup fixture) |
| FwRegistryHelperTests.cs | +7/-7 | +0/-0 | Assert-only |
| **IVwCacheDaTests.cs** | +1/-1 | **+14/-0** | **Real fix** (COM cleanup in teardown) |
| PubSubSystemTests.cs | +55/-55 | +0/-0 | Assert-only |
| TestFwStylesheetTests.cs | +4/-4 | +0/-0 | Assert-only |
| ParatextHelperTests.cs | +0/-0 | +0/-0 | No changes |
| ConstChartRowDecoratorTests.cs | +7/-7 | +0/-0 | Assert-only |
| DiscourseExportTests.cs | +3/-3 | +0/-0 | Assert-only |
| DiscourseTestHelper.cs | +5/-5 | +0/-0 | Assert-only |
| InMemoryLogicTest.cs | +2/-2 | +0/-0 | Assert-only |
| InMemoryMoveEditTests.cs | +1/-1 | +0/-0 | Assert-only |
| LogicTest.cs | +4/-4 | +0/-0 | Assert-only |
| TestCCLogic.cs | +1/-1 | +0/-0 | Assert-only |
| BIRDFormatImportTests.cs | +3/-3 | +0/-0 | Assert-only |
| InterlinTaggingTests.cs | +1/-1 | +0/-0 | Assert-only |
| MorphemeBreakerTests.cs | +11/-11 | +0/-0 | Assert-only |
| TextsTriStateTreeViewTests.cs | +2/-2 | +0/-0 | Assert-only |
| XLingPaperExporterTests.cs | +1/-1 | +0/-0 | Assert-only |
| LiftExportTests.cs | +1/-1 | +0/-0 | Assert-only |
| LiftMergerTests.cs | +5/-5 | +0/-0 | Assert-only |
| MasterCategoryTests.cs | +2/-2 | +0/-0 | Assert-only |
| MsaInflectionFeatureListDlgTests.cs | +1/-1 | +0/-2 | Assert-only |
| **RespellingTests.cs** | +0/-0 | **+11/-22** | **Real fix** (Mock refactoring - sealed class) |
| M3ToXAmpleTransformerTests.cs | +1/-1 | +0/-0 | Assert-only |
| ParseFilerProcessingTests.cs | +2/-2 | +0/-0 | Assert-only |
| ParseWorkerTests.cs | +1/-1 | +0/-0 | Assert-only |
| ParatextImportManagerTests.cs | +1/-1 | +0/-0 | Assert-only |
| **SCTextEnumTests.cs** | +5/-46 | **+10/-59** | **Real fix** (test restructure) |
| **PUAInstallerTests.cs** | +0/-0 | **+1/-1** | **Real fix** (path fix for worktree builds) |
| NavPaneOptionsDlgTests.cs | +1/-1 | +0/-0 | Assert-only |
| SidePaneTests.cs | +4/-4 | +0/-0 | Assert-only |
| PropertyTableTests.cs | +120/-120 | +0/-0 | Assert-only |
| ConfiguredXHTMLGeneratorTests.cs | +6/-6 | +0/-3 | Assert-only |
| CssGeneratorTests.cs | +1/-1 | +0/-0 | Assert-only |
| DictionaryConfigManagerTests.cs | +1/-1 | +0/-0 | Assert-only |
| DictionaryConfigurationControllerTests.cs | +1/-1 | +0/-0 | Assert-only |
| DictionaryConfigurationImportControllerTests.cs | +1/-1 | +0/-0 | Assert-only |
| DictionaryConfigurationManagerControllerTests.cs | +2/-2 | +0/-0 | Assert-only |
| DictionaryConfigurationMigratorTests.cs | +4/-4 | +0/-0 | Assert-only |
| DictionaryConfigurationModelTests.cs | +2/-2 | +0/-0 | Assert-only |
| DictionaryDetailsControllerTests.cs | +5/-5 | +0/-0 | Assert-only |
| **DictionaryExportServiceTests.cs** | +16/-38 | **+0/-80** | **Real fix** (large deletions - test cleanup) |
| ExportDialogTests.cs | +3/-3 | +0/-0 | Assert-only |
| InterestingTextsTests.cs | +3/-3 | +0/-0 | Assert-only |

### Summary

- **40 files**: Assert-only changes → Already handled by Commit 1's clean conversion
- **7 files**: Real fixes that need to be manually applied:
  1. `AssemblySetupFixture.cs` - NEW FILE (checkout entirely)
  2. `FieldWorksTests.cs` - Path fixes for cross-platform temp paths
  3. `IVwCacheDaTests.cs` - COM cleanup in TestTeardown
  4. `RespellingTests.cs` - Mock refactoring (sealed class workaround)
  5. `SCTextEnumTests.cs` - Test restructure
  6. `PUAInstallerTests.cs` - Path fix for worktree/dev builds
  7. `DictionaryExportServiceTests.cs` - Test cleanup (80 lines deleted)

## Commit 2 Execution Strategy

### Step 1: Add the new file
```powershell
git checkout 9eff1d477 -- "Src/Common/FwUtils/FwUtilsTests/AssemblySetupFixture.cs"
```

### Step 2: Apply non-NUnit changes to 6 modified files
For each file, extract and apply ONLY the non-Assert changes from the diff.
Do NOT checkout the entire file (it contains buggy NUnit patterns).

Files to patch:
- `FieldWorksTests.cs` - Add path constants and helper method
- `IVwCacheDaTests.cs` - Add COM cleanup in teardown
- `RespellingTests.cs` - Refactor Mock usage
- `SCTextEnumTests.cs` - Restructure tests
- `PUAInstallerTests.cs` - Fix baseDir path
- `DictionaryExportServiceTests.cs` - Delete obsolete code

### Step 3: Stage and commit
```powershell
git add Src
git commit -m "fix(tests): apply VSTest and test failure fixes from branch"
```

## Progress

- [x] Commit 1: Clean NUnit conversion
  - [x] Checkout files from release/9.3
  - [x] Run conversion script
  - [x] Verify no buggy patterns
  - [x] Commit (d9e269968)
- [x] Commit 2: Re-apply targeted fixes
  - [x] AssemblySetupFixture.cs - Already in HEAD (new file from branch)
  - [x] FieldWorksTests.cs - Path fixes applied
  - [x] IVwCacheDaTests.cs - COM cleanup applied
  - [x] RespellingTests.cs - Mock refactoring applied (7 occurrences)
  - [x] SCTextEnumTests.cs - Moq migration applied (3 occurrences)
  - [x] PUAInstallerTests.cs - Path fix applied
  - [x] DictionaryExportServiceTests.cs - Skipped (duplicate test cleanup not needed in clean version)
  - [ ] Commit changes
- [ ] Final verification: Build and test
