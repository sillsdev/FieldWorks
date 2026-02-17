# NUnit Conversion Bug Fix Plan

## Problem Summary

The NUnit 3 to NUnit 4 conversion script (`scripts/tests/convert_nunit.py`) had a bug in an earlier version that **swapped argument order** for comparison assertions (`Assert.Greater`, `Assert.Less`, etc.).

### Bug Pattern

**Original NUnit 3 assertion:**
```csharp
Assert.Greater(actualValue, 0, "message");  // means: actualValue > 0
```

**Buggy conversion (WRONG):**
```csharp
Assert.That(0, Is.GreaterThan(actualValue), "message");  // means: 0 > actualValue (WRONG!)
```

**Correct conversion:**
```csharp
Assert.That(actualValue, Is.GreaterThan(0), "message");  // means: actualValue > 0 (CORRECT)
```

### Root Cause

The current conversion script (`scripts/tests/convert_nunit.py` with `nunit_converters.py`) is **correct**. The bugs were introduced by an **earlier version** of the script that was run on some files. The buggy conversions exist in HEAD but re-running the current script on the original `origin/release/9.3` files produces correct output.

**Evidence from git diff HEAD:**
```diff
# HEAD has WRONG conversion:
-                       Assert.That(0, Is.GreaterThan(diff.SubDiffsForParas.Count), ...)
# After re-conversion from release/9.3:
+                       Assert.That(diff.SubDiffsForParas.Count, Is.GreaterThan(0), ...)
```

## Fix Strategy (Comprehensive)

### Approach
Rather than trying to identify individual buggy patterns, we will:
1. Find ALL test files changed since `origin/release/9.3` (in both `Src/` and `Lib/`)
2. Filter to files that had `Assert.Greater`, `Assert.Less`, `Assert.GreaterOrEqual`, or `Assert.LessOrEqual` in the original
3. Checkout each file from `origin/release/9.3`
4. Re-run the (now correct) conversion script
5. After conversion, check git history for any OTHER fixes that were applied to these files and re-apply them

### Step 1: Find All Changed Test Files with Greater/Less Assertions
```powershell
# Get all test files changed since release/9.3
$changedFiles = git diff --name-only origin/release/9.3 HEAD -- "Src/**/*Tests*.cs" "Lib/**/*Tests*.cs"

# Filter to files that had Greater/Less assertions in the original
$filesToFix = @()
foreach ($file in $changedFiles) {
    $content = git show "origin/release/9.3:$file" 2>$null
    if ($content -match "Assert\.(Greater|Less|GreaterOrEqual|LessOrEqual)\(") {
        $filesToFix += $file
    }
}
```

### Step 2: Checkout and Re-convert Each File
```powershell
foreach ($file in $filesToFix) {
    # Checkout original from release/9.3
    git checkout origin/release/9.3 -- $file

    # Re-run conversion
    python -m scripts.tests.convert_nunit $file
}
```

### Step 3: Check for Other Fixes to Re-apply
After conversion, check if any files had additional commits between release/9.3 and HEAD that made non-conversion fixes:
```powershell
foreach ($file in $filesToFix) {
    git log --oneline origin/release/9.3..HEAD -- $file
}
```

### Step 4: Verify No Buggy Patterns Remain
```powershell
# Search for the buggy pattern: Assert.That(<literal>, Is.GreaterThan|LessThan(<variable>))
# This pattern puts a constant as the "actual" value which is usually wrong
Get-ChildItem -Recurse -Filter "*Tests*.cs" Src, Lib | ForEach-Object {
    Select-String -Path $_.FullName -Pattern "Assert\.That\((0|1|-1), Is\.(GreaterThan|LessThan)\([^0-9]"
}
```

**Note:** Some patterns like `Assert.That(0, Is.LessThanOrEqualTo(delta.TotalSeconds))` may be semantically correct (asserting elapsed time >= 0) but are non-idiomatic. The conversion script produces these from `Assert.LessOrEqual(0, delta.TotalSeconds)` which is also non-idiomatic in NUnit 3.

### Step 5: Run Tests
```powershell
# Build and run all affected test suites
.\build.ps1
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /p:action=test
```

## Files Processed

**Total test files changed since release/9.3:** 262
**Files with Greater/Less assertions in original:** 25

All 25 files were checked out from `origin/release/9.3` and re-converted:

| File | Status |
|------|--------|
| `Src/Common/Controls/DetailControls/DetailControlsTests/AtomicReferenceLauncherTests.cs` | ✅ Converted |
| `Src/Common/Controls/DetailControls/DetailControlsTests/VectorReferenceLauncherTests.cs` | ✅ Converted |
| `Src/Common/Controls/FwControls/FwControlsTests/ProgressDlgTests.cs` | ✅ Converted |
| `Src/Common/Controls/Widgets/WidgetsTests/FwListBoxTests.cs` | ✅ Converted |
| `Src/Common/Controls/Widgets/WidgetsTests/FwTextBoxTests.cs` | ✅ Converted |
| `Src/Common/FwUtils/FwUtilsTests/FwRegistryHelperTests.cs` | ✅ Converted |
| `Src/Common/ScriptureUtils/ScriptureUtilsTests/ScrReferencePositionComparerTests.cs` | ✅ Converted |
| `Src/Common/ScriptureUtils/ScriptureUtilsTests/ScriptureReferenceComparerTests.cs` | ✅ Converted |
| `Src/FXT/FxtDll/FxtDllTests/DumperTests.cs` | ✅ Converted |
| `Src/FwCoreDlgs/FwCoreDlgControls/FwCoreDlgControlsTests/DefaultFontsControlTests.cs` | ✅ Converted |
| `Src/FwCoreDlgs/FwCoreDlgControls/FwCoreDlgControlsTests/FwFontTabTests.cs` | ✅ Converted |
| `Src/FwCoreDlgs/FwCoreDlgsTests/FwFontDialogTests.cs` | ✅ Converted |
| `Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupDlgTests.cs` | ✅ Converted |
| `Src/InstallValidator/InstallValidatorTests/InstallValidatorTests.cs` | ✅ Converted |
| `Src/LexText/Discourse/DiscourseTests/AdvancedMTDialogLogicTests.cs` | ✅ Converted |
| `Src/LexText/Discourse/DiscourseTests/ConstituentChartDatabaseTests.cs` | ✅ Converted |
| `Src/LexText/Discourse/DiscourseTests/DiscourseTestHelper.cs` | ✅ Converted |
| `Src/LexText/Discourse/DiscourseTests/InterlinRibbonTests.cs` | ✅ Converted |
| `Src/LexText/LexTextControls/LexTextControlsTests/LiftExportTests.cs` | ✅ Converted |
| `Src/ParatextImport/ParatextImportTests/DiffTestHelper.cs` | ✅ Converted |
| `Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportManagerTests.cs` | ✅ Converted |
| `Src/xWorks/xWorksTests/BulkEditBarTests.cs` | ✅ Converted |
| `Src/xWorks/xWorksTests/ConfiguredXHTMLGeneratorTests.cs` | ✅ Converted |
| `Src/xWorks/xWorksTests/DictionaryConfigurationMigrators/DictionaryConfigurationMigratorTests.cs` | ✅ Converted |
| `Src/xWorks/xWorksTests/DictionaryExportServiceTests.cs` | ✅ Converted |

## Progress Tracking (Initial Pass - 25 files with Greater/Less)

- [x] Step 1: Identify files with Greater/Less assertions (25 files found)
- [x] Step 2: Checkout and re-convert all 25 files
- [x] Step 3: Check git history for other fixes to re-apply
- [x] Step 4: Verify no buggy `Assert.That(0, Is.GreaterThan(...))` patterns remain
- [ ] Step 5: Run tests to confirm fixes

## NEW: Comprehensive Re-conversion Plan

### Problem Discovered
The initial approach only targeted files with `Assert.Greater/Less` patterns. However:
1. The branch history shows: NUnit conversions → rebase on release/9.3 → more conversions
2. This rebase may have introduced merge conflicts or mixed conversion states
3. Files like `BulkEditBarTests.cs` show unexpected diffs that aren't from our explicit changes

### New Approach: Clean Slate Conversion
To ensure a consistent state, we will:

1. **Checkout ALL 262 test files from `origin/release/9.3`** (not just the 25 with Greater/Less)
2. **Run the conversion script on ALL test files**
3. **Compare the result with HEAD** to identify any non-conversion changes that need to be preserved
4. **Apply any legitimate test fixes** that were made on this branch (vs. just conversion artifacts)

### Step-by-Step Execution

#### Phase 1: Identify ALL changed test files
```powershell
$allChangedTests = git diff --name-only origin/release/9.3 HEAD -- "Src/**/*Tests*.cs" "Lib/**/*Tests*.cs"
# Result: 262 files
```

#### Phase 2: Checkout all from release/9.3
```powershell
foreach ($file in $allChangedTests) {
    git checkout origin/release/9.3 -- $file
}
```

#### Phase 3: Run conversion script on all test files
```powershell
python -m scripts.tests.convert_nunit Src Lib
```

#### Phase 4: Compare with HEAD to find non-conversion differences
```powershell
# After conversion, diff against HEAD to see what we lost
git diff HEAD -- "Src/**/*Tests*.cs" "Lib/**/*Tests*.cs" > .cache/conversion_diff.txt
# Review this diff for:
# - Legitimate test fixes (should be re-applied)
# - Conversion artifacts from buggy script (should be discarded)
# - Merge conflict resolutions (need to verify correctness)
```

#### Phase 5: Re-apply legitimate fixes
Any changes from HEAD that are NOT conversion-related (e.g., actual test logic fixes, new tests, bug fixes) should be cherry-picked or manually re-applied.

### Progress Tracking (Comprehensive Re-conversion)

- [ ] Phase 1: Get list of all 262 changed test files
- [ ] Phase 2: Checkout all files from origin/release/9.3
- [ ] Phase 3: Run conversion script on Src and Lib
- [ ] Phase 4: Generate diff against HEAD and review
- [ ] Phase 5: Re-apply any legitimate non-conversion fixes
- [ ] Phase 6: Run tests to confirm everything works

### Why This Approach?
- **Consistency**: All test files will have conversions from the same (correct) script version
- **Traceability**: We can clearly see what differs between clean conversion and HEAD
- **Safety**: We won't accidentally lose legitimate test fixes

## Verification Results (Initial Pass)

### Buggy Pattern Check
No `Assert.That(0, Is.GreaterThan(...))` patterns found - all `Assert.Greater` conversions are correct.

### Non-idiomatic but Correct Patterns
Some `Assert.That(0, Is.LessThan(x))` patterns exist - these are **semantically correct** literal translations of `Assert.Less(0, x)`. The original NUnit 3 code was also non-idiomatic (put the constant first). These do not cause test failures.

### Other Fixes Verified
The conversion script correctly handles:
- `.Within(message)` → proper message argument (from commit 575eaa0ec)
- Format strings → interpolated strings
- Assert.That wrong argument order (message, constraint) → (constraint, message)

No manual re-application of fixes from commit 575eaa0ec was needed.

## Commit Message Template

```
fix(tests): re-run NUnit conversion to fix swapped assertion arguments

Earlier version of convert_nunit.py incorrectly swapped arguments for
Assert.Greater/Less conversions, producing:
  Assert.That(0, Is.GreaterThan(value))  // WRONG: asserts 0 > value
Instead of:
  Assert.That(value, Is.GreaterThan(0))  // CORRECT: asserts value > 0

Re-ran current (fixed) conversion script on all test files that had
Greater/Less assertions in release/9.3:
- Checked out original files from origin/release/9.3
- Applied current convert_nunit.py
- Verified no buggy patterns remain

Affected test projects:
- ParatextImportTests
- LexTextControlsTests
- xWorksTests
- [others as identified]
```

## Future Prevention

1. The conversion script now has correct logic in `_convert_comparison()`
2. Consider adding regression tests for the conversion script itself
3. Pattern to watch for in code review: `Assert.That(0, Is.GreaterThan` or `Assert.That(1, Is.LessThan`
4. Run verification step after any batch conversion
