# Diff Analysis: Fresh Conversion vs HEAD

This document analyzes the 39 files with semantic differences between our fresh NUnit conversion (from release/9.3) and HEAD.

## Legend
- **CONVERSION**: NUnit assertion conversion differences (our fresh conversion is correct)
- **MOQFIX**: Moq → Rhino.Mocks reversion (HEAD goes back to older mocking)
- **TESTLOGIC**: Actual test logic changes (bug fixes, new tests)
- **FORMATTING**: Code formatting/style changes (wrapping, etc.)
- **STYLEFIX**: FunctionValues/StructureValues enum changes
- **STRINGFORMAT**: String interpolation → String.Format changes
- **BOM**: Byte Order Mark differences
- **USING**: Using statement order changes
- **NEW**: New test methods or classes added in HEAD

## Analysis by File

### 1. Lib/src/ScrChecks/ScrChecksTests/ChapterVerseTests.cs
**Category**: FORMATTING
**Assessment**: Keep fresh conversion
**Details**: Only formatting differences (line wrapping)

### 2. Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckUnitTest.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: Assert argument order fix

### 3. Src/CacheLight/CacheLightTests/RealDataCacheTests.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: `Assert.That(2, Is.EqualTo(tsms.StringCount))` → `Assert.That(tsms.StringCount, Is.EqualTo(2))`

### 4. Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: Assert argument order fix

### 5. Src/Common/Controls/XMLViews/XMLViewsTests/ConfiguredExportTests.cs
**Category**: CONVERSION + FORMATTING
**Assessment**: Keep fresh conversion
**Details**: Argument order fixes plus formatting

### 6. Src/Common/Controls/XMLViews/XMLViewsTests/TestColumnConfigureDialog.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: Assert argument order fix

### 7. Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: HEAD reverts Moq back to Rhino.Mocks:
```csharp
// Fresh (Moq):
var mockRegistry = new Mock<IFwRegistryHelper>();
mockRegistry.SetupGet(r => r.UserLocaleValueName).Returns("Locale");

// HEAD (Rhino.Mocks):
var mockRegistry = MockRepository.GenerateMock<IFwRegistryHelper>();
mockRegistry.Stub(r => r.UserLocaleValueName).Return("Locale");
```

### 8. Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs
**Category**: FORMATTING + OTHER
**Assessment**: ⚠️ NEED INPUT
**Details**: Large diff (405 lines). Need to check if there are test logic changes beyond formatting.

### 9. Src/Common/FwUtils/FwUtilsTests/IVwCacheDaTests.cs
**Category**: TESTLOGIC
**Assessment**: ⚠️ NEED INPUT
**Details**: Appears to have some test removals or changes. Need review.

### 10. Src/Common/FwUtils/FwUtilsTests/StringTableTests.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: Assert argument order fix

### 11. Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs
**Category**: MOQFIX + FORMATTING
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion plus formatting. Large diff (1063 lines).

### 12. Src/Common/RootSite/RootSiteTests/RootSiteGroupTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 13. Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 14. Src/Common/SimpleRootSite/SimpleRootSiteTests/IbusRootSiteEventHandlerTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 15. Src/Common/SimpleRootSite/SimpleRootSiteTests/SimpleRootSiteTests_IsSelectionVisibleTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 16. Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupModelTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 17. Src/FwCoreDlgs/FwCoreDlgsTests/RestoreProjectPresenterTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 18. Src/LexText/Discourse/DiscourseTests/ConstChartRowDecoratorTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 19. Src/LexText/Discourse/DiscourseTests/DiscourseTestHelper.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 20. Src/LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 21. Src/LexText/Interlinear/ITextDllTests/BIRDFormatImportTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 22. Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 23. Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: Small delta, likely just assertion order

### 24. Src/LexText/Interlinear/ITextDllTests/InterlinDocForAnalysisTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 25. Src/LexText/Interlinear/ITextDllTests/MorphemeBreakerTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 26. Src/LexText/LexTextControls/LexTextControlsTests/LiftMergerTests.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: Assert argument order fix

### 27. Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 28. Src/ParatextImport/ParatextImportTests/DiffTestHelper.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 29. Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportManagerTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 30. Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportTests.cs
**Category**: MOQFIX
**Assessment**: ⚠️ NEED INPUT
**Details**: Moq → Rhino.Mocks reversion

### 31. Src/ParatextImport/ParatextImportTests/SCTextEnumTests.cs
**Category**: MOQFIX + TESTLOGIC
**Assessment**: ⚠️ NEED INPUT
**Details**: Large diff (5152 chars). Moq → Rhino.Mocks reversion PLUS new test method `BooksInFile()` added in HEAD.

### 32. Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs
**Category**: TESTLOGIC
**Assessment**: ⚠️ NEED INPUT
**Details**: HEAD changes path resolution logic:
```csharp
// Fresh (complex path):
var baseDir = Path.Combine(Path.GetDirectoryName(FwDirectoryFinder.SourceDirectory), "DistFiles");

// HEAD (simple path):
var baseDir = FwDirectoryFinder.DataDirectory;
```

### 33. Src/Utilities/MessageBoxExLib/MessageBoxExLibTests/Tests.cs
**Category**: TESTLOGIC + BOM
**Assessment**: ⚠️ NEED INPUT
**Details**: HEAD has completely different tests:
- Adds NUnitFormTest setup/teardown
- New tests: `TimeoutOfNewBox()`, `RememberOkBox()`
- Removes: `ShowReturnsSavedResponseWithoutShowingDialog()`

### 34. Src/XCore/xCoreInterfaces/xCoreInterfacesTests/PropertyTableTests.cs
**Category**: STRINGFORMAT
**Assessment**: ⚠️ NEED INPUT
**Details**: HEAD changes from simple strings to String.Format:
```csharp
// Fresh:
Assert.That(gpia, Is.EqualTo(253), "Invalid value for global IntegerPropertyA.");

// HEAD:
Assert.That(gpia, Is.EqualTo(253), String.Format("Invalid value for {0} {1}.", "global", "IntegerPropertyA"));
```

### 35. Src/xWorks/xWorksTests/BulkEditBarTests.cs
**Category**: TESTLOGIC
**Assessment**: **RESTORE FROM HEAD**
**Details**: HEAD has legitimate test fixes:
- Changes `firstAllomorph` → `firstEntryWithAllomorph.LexemeFormOA`
- Adds `+ 1` to expected counts
- Updates comments to reflect correct behavior

### 36. Src/xWorks/xWorksTests/DictionaryConfigurationImportControllerTests.cs
**Category**: STYLEFIX + BOM
**Assessment**: ⚠️ NEED INPUT
**Details**: HEAD changes style factory parameters:
- `StructureValues.Undefined` → `StructureValues.Body`
- `FunctionValues.Line` → `FunctionValues.Prose`

### 37. Src/xWorks/xWorksTests/DictionaryDetailsControllerTests.cs
**Category**: STRINGFORMAT
**Assessment**: Keep fresh conversion
**Details**: Conversion changed interpolated string to String.Format (HEAD had interpolated)

### 38. Src/xWorks/xWorksTests/InterestingTextsTests.cs
**Category**: FORMATTING + USING
**Assessment**: Keep fresh conversion
**Details**: Only formatting (line wrapping) and using statement order changes

### 39. Src/xWorks/xWorksTests/ReversalIndexServicesTests.cs
**Category**: CONVERSION
**Assessment**: Keep fresh conversion
**Details**: `Assert.That("English", Is.EqualTo(enWsLabel))` → `Assert.That(enWsLabel, Is.EqualTo("English"))`

---

## Summary

### Files to definitely keep fresh conversion (12):
1. ChapterVerseTests.cs - FORMATTING only
2. RepeatedWordsCheckUnitTest.cs - CONVERSION
3. RealDataCacheTests.cs - CONVERSION
4. DataTreeTests.cs - CONVERSION
5. ConfiguredExportTests.cs - CONVERSION
6. TestColumnConfigureDialog.cs - CONVERSION
7. StringTableTests.cs - CONVERSION
8. GlossToolLoadsGuessContentsTests.cs - CONVERSION
9. LiftMergerTests.cs - CONVERSION
10. DictionaryDetailsControllerTests.cs - CONVERSION (interpolation ok)
11. InterestingTextsTests.cs - FORMATTING only
12. ReversalIndexServicesTests.cs - CONVERSION

### Files to definitely restore from HEAD (1):
1. BulkEditBarTests.cs - TESTLOGIC fix

### Files needing review - Moq → Rhino.Mocks (20):
These all have Moq syntax that HEAD reverts to Rhino.Mocks. **Question: Which mocking framework should we use?**

1. FieldWorksTests.cs
2. MoreRootSiteTests.cs
3. RootSiteGroupTests.cs
4. ParatextHelperTests.cs
5. IbusRootSiteEventHandlerTests.cs
6. SimpleRootSiteTests_IsSelectionVisibleTests.cs
7. FwWritingSystemSetupModelTests.cs
8. RestoreProjectPresenterTests.cs
9. ConstChartRowDecoratorTests.cs
10. DiscourseTestHelper.cs
11. FlexPathwayPluginTests.cs
12. BIRDFormatImportTests.cs
13. ComboHandlerTests.cs
14. InterlinDocForAnalysisTests.cs
15. MorphemeBreakerTests.cs
16. RespellingTests.cs
17. DiffTestHelper.cs
18. ParatextImportManagerTests.cs
19. ParatextImportTests.cs
20. SCTextEnumTests.cs (also has new test)

### Files needing review - Other issues (6):
1. FwEditingHelperTests.cs - Large diff, needs manual review
2. IVwCacheDaTests.cs - Possible test removals
3. PUAInstallerTests.cs - Path resolution change
4. MessageBoxExLibTests.cs - Completely different tests
5. PropertyTableTests.cs - String.Format changes
6. DictionaryConfigurationImportControllerTests.cs - StyleValues changes

---

## Questions for You

1. **Moq vs Rhino.Mocks**: The fresh conversion uses Moq syntax. HEAD reverts to Rhino.Mocks. Which should we use?
   - If **Moq**: Keep fresh conversion for 20 files
   - If **Rhino.Mocks**: Restore from HEAD for 20 files

2. **String.Format vs interpolation**: Some files have `String.Format` in HEAD vs interpolated strings in fresh. Preference?

3. **StyleValues changes** in DictionaryConfigurationImportControllerTests.cs: Is the change from `Undefined/Line` to `Body/Prose` intentional?

4. **New tests in HEAD**:
   - SCTextEnumTests.cs has new `BooksInFile()` test
   - MessageBoxExLibTests.cs has completely restructured tests
   Should we preserve these?

5. **PUAInstallerTests.cs path change**: HEAD uses `FwDirectoryFinder.DataDirectory` vs the fresh version's more complex path. Which is correct?
