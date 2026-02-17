# NUnit Re-conversion Plan (Two-Commit Approach)

## Problem Summary

The NUnit 3→4 conversion on this branch has bugs where assertion arguments were swapped incorrectly. Additionally, commits after `a9f323ea` made targeted fixes but on top of the buggy conversion base.

## Fix Strategy

### Commit 1: Clean NUnit Conversion from release/9.3
1. Checkout ALL test files from `release/9.3` baseline
2. Run the `convert_nunit.py` script to get correct NUnit 4 syntax
3. Commit as: `fix(tests): Re-run NUnit 3→4 conversion with correct argument order`

### Commit 2: Re-apply Branch-Specific Fixes
1. Extract non-conversion changes from commits after `a9f323ea`
2. Apply: path fixes, Moq syntax, COM cleanup, new tests
3. Commit as: `fix(tests): Re-apply VSTest migration fixes`

## Execution

### Commit 1 Commands

```powershell
# Step 1: Get list of all changed test files
$allTests = git diff --name-only origin/release/9.3 HEAD -- "Src/**/*Tests*.cs" "Lib/**/*Tests*.cs"
$allTests | Out-File .cache/all_test_files.txt

# Step 2: Checkout all from release/9.3
foreach ($f in $allTests) { git checkout origin/release/9.3 -- $f }

# Step 3: Run conversion script
python -m scripts.tests.convert_nunit Src Lib

# Step 4: Stage and commit
git add Src Lib
git commit -m "fix(tests): Re-run NUnit 3→4 conversion with correct argument order

Re-ran NUnit conversion script on all 261 test files from clean release/9.3 baseline.
This fixes swapped assertion arguments where:
  Assert.That(0, Is.GreaterThan(value))  // WRONG
became:
  Assert.That(value, Is.GreaterThan(0))  // CORRECT
"
```

### Commit 2: Identify Changes to Re-apply

Files changed after `a9f323ea` that need targeted fixes re-applied:
1. `FieldWorksTests.cs` - Path fixes (cross-platform temp paths)
2. `IVwCacheDaTests.cs` - COM cleanup in TestTeardown
3. `RespellingTests.cs` - Mock<Mediator> fix (sealed class issue)
4. `PUAInstallerTests.cs` - SourceDirectory-relative path fix
5. `SCTextEnumTests.cs` - New test method `BooksInFile()`
6. Plus VSTest-related fixes in other files

## Progress

- [ ] Commit 1: Clean NUnit conversion
  - [ ] Checkout files from release/9.3
  - [ ] Run conversion script
  - [ ] Verify no buggy patterns
  - [ ] Commit
- [ ] Commit 2: Re-apply targeted fixes
  - [ ] Extract changes from 575eaa0ec and 9eff1d477
  - [ ] Apply non-conversion fixes
  - [ ] Commit
- [ ] Final verification: Build and test
