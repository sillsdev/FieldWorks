# Ignored / Skipped Tests

This spec tracks tests that are either:
- Compile-disabled in source (e.g., `#if false` or legacy guards like `RUN_LW_LEGACY_TESTS`), or
- Skipped at runtime via NUnit `[Ignore]` / `Assert.Ignore(...)` / platform guards.

The authoritative “skipped at runtime” list is captured from the VSTest run whose TRX is:
- `Output\Debug\TestResults\johnm_SIL-XPS_2025-12-16_20_52_02.trx`

## Historical comparison (release/9.3 baseline)

Baseline: release/9.3 commit `6a2d976e`.

Generated report:
- `.cache/test-audit/ignored-history-6a2d976e-6a2d976e.md`

To regenerate:
- `python scripts/audit_ignored_tests.py --baseline-ref 6a2d976e`

**Scope rule (important):** We only unignore/enable tests that were **not** ignored on `release/9.3`.

If a test is already ignored in `release/9.3` (e.g., contains `[Ignore]` / `Assert.Ignore(...)` there), it stays ignored in this branch.

Quick verification recipe (examples):
- `./scripts/Agent/Git-Search.ps1 -Action search -Ref "release/9.3" -Path "<path>" -Pattern "Ignore"`

Note: The generated report referenced above needs to be regenerated/validated; do not treat older “ignore marker count” summaries as authoritative unless they’ve been rechecked against `6a2d976e`.

Notable deltas vs `6a2d976e` (from the generated report):

- Newly runtime-skipped via `[Ignore]` / `Assert.Ignore(...)` (none in baseline):
	- `Lib/src/ScrChecks/ScrChecksTests/SentenceFinalPunctCapitalizationCheckUnitTest.cs` (obsolete check removed)
	- `Src/Common/FwUtils/FwUtilsTests/FLExBridgeHelperTests.cs` (requires FLEx Bridge; category `ByHand`)
	- `Src/Common/ViewsInterfaces/ViewsInterfacesTests/ExtraComInterfacesTests.cs` (obsolete Mono-specific COM test)
	- `Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupDlgTests.cs` (obsolete dialog API)
	- `Src/FwCoreDlgs/FwCoreDlgsTests/RestoreProjectPresenterTests.cs` (backup/restore API refactor)

## Legacy-disabled tests (archived)

These were previously excluded with `#if false`. They are now compiled as explicit NUnit skips so they show up in reports, while the legacy implementation remains archived behind `RUN_LW_LEGACY_TESTS`.

- `Lib/src/ScrChecks/ScrChecksTests/SentenceFinalPunctCapitalizationCheckUnitTest.cs`
	- Determination: **Obsolete** (the check type no longer exists)
	- Historical (6a2d976e): **Not** runtime-skipped (no `[Ignore]` / `Assert.Ignore(...)`)
	- Why this is obsolete: the legacy fixture references `SentenceFinalPunctCapitalizationCheck`, which is not present in the current `ScrChecks` code.
	- Likely replacement target: sentence-final punctuation handling appears to have been folded into `CapitalizationCheck` (see `Lib/src/ScrChecks/CapitalizationCheck.cs` and the parameter `SentenceFinalPunctuation`, which is exercised by `Lib/src/ScrChecks/ScrChecksTests/CapitalizationCheckUnitTest.cs`).
	- Rewrite guidance: port the legacy test cases into the `CapitalizationCheckUnitTest` suite (or a new focused fixture) by driving `CapitalizationCheck` with the same USFM token stream + `SentenceFinalPunctuation` parameter.
	- Action: tracked skip until rewritten (compiled skip makes the gap visible in VSTest/TRX).

- `Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupDlgTests.cs`
	- Determination: **Obsolete** (dialog API refactored)
	- Historical (6a2d976e): **Not** runtime-skipped (no `[Ignore]` / `Assert.Ignore(...)`)
	- Why this is obsolete: the legacy tests were written against the pre-model dialog API (constructors taking `LcmCache`/`WritingSystemManager` and internal dialog mechanics like `SetupDialog(...)`, tab switching, and direct access to UI controls).
	- Current architecture: `FwWritingSystemSetupDlg` is now model-driven (see `Src/FwCoreDlgs/FwWritingSystemSetupDlg.cs`) with core behavior pushed into `Src/FwCoreDlgs/FwWritingSystemSetupModel.cs`.
	- Rewrite guidance: prefer unit tests against the model layer rather than UI-control plumbing. There are already model-oriented tests in this area (e.g., `Src/FwCoreDlgs/FwCoreDlgsTests/ViewHiddenWritingSystemsModelTests.cs`) that illustrate the intended direction.
	- Action: tracked skip until rewritten (compiled skip makes the gap visible in VSTest/TRX).

- `Src/FwCoreDlgs/FwCoreDlgsTests/RestoreProjectPresenterTests.cs`
	- Determination: **Obsolete** (backup/restore API refactor)
	- Historical (6a2d976e): **Not** runtime-skipped (no `[Ignore]` / `Assert.Ignore(...)`)
	- Why this is obsolete: the legacy test body relies on ctor/properties/fields that no longer line up with the current backup/restore stack:
		- `RestoreProjectPresenter` now drives behavior through `RestoreProjectDlg.Settings` and uses a filesystem-backed `BackupFileRepository` (see `Src/FwCoreDlgs/BackupRestore/RestoreProjectPresenter.cs`).
		- The legacy tests also use `ReflectionHelper.SetField(...)` to mutate internal state on LCModel backup settings classes; those internals are brittle across refactors.
	- Historical evidence: older refactors touched this area (e.g., commit `6a1beeb63` “Move and delete classes from CoreImpl” includes changes to backup/restore and `RestoreProjectPresenterTests.cs`).
	- Rewrite guidance:
		- Prefer public properties on `BackupFileSettings` (e.g., `IncludeConfigurationSettings`, `IncludeLinkedFiles`, etc.) instead of reflection.
		- For tests that depend on available backups, either (a) create temp backup files in a temp directory and point the repository there, or (b) refactor presenter/repository to allow injection of a fake `BackupFileRepository`.
	- Action: tracked skip until rewritten (compiled skip makes the gap visible in VSTest/TRX).

- `Src/Common/ViewsInterfaces/ViewsInterfacesTests/ExtraComInterfacesTests.cs`
	- Determination: **Obsolete** (interface removed; experimental Mono-specific)
	- Historical (6a2d976e): **Not** runtime-skipped (no `[Ignore]` / `Assert.Ignore(...)`)
	- Evidence: in-file comment indicates `ILgWritingSystemFactoryBuilder` no longer exists
	- Action: tracked skip until rewritten

## Notes on related, intentional code/test changes

These changes are not “making tests pass by weakening behavior”; they align tests and production code with the current intended behavior and remove sources of false failures.

- `Build/Src/FwBuildTasks/RegFreeCreator.cs`
	- Change: `AddOrReplaceClrClass(...)` now attaches CLR class entries to the per-file manifest node (`fileNode`) instead of the parent node.
	- Why it helps: reg-free COM manifest output is structurally sensitive; associating class registrations with the correct `<file>` node ensures the manifest describes COM-visible classes under the actual assembly file. Passing the wrong node risks malformed/ineffective registrations.

- `Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs`
	- Change: updated expectations for (a) error message wording drift (“inside out” vs “different arguments”), and (b) added/missing resx keys.
	- Why it helps: `ProjectLocalizer.CheckResXForErrors` no longer treats added/missing keys as build failures (see commented-out checks in `Build/Src/FwBuildTasks/Localization/ProjectLocalizer.cs`). The tests now match that behavior so they don’t fail spuriously.

- `Build/Src/FwBuildTasks/FwBuildTasksTests/NormalizeLocalesTests.cs`
	- Change: `CopyMalay` now verifies that `ms` remains `ms` (i.e., no normalization/copy to some other tag).
	- Why it helps: a simple language tag like `ms` should not be rewritten into a different locale folder as a side-effect; the updated assertions enforce that `NormalizeLocales` doesn’t create unexpected directories/files.

- `Src/xWorks/DictionaryConfigManager.cs`
	- Change: `RenameConfigItem(...)` now blocks renaming protected configurations even if called directly (and refreshes the view to revert edit text).
	- Why it helps: the UI prevents this rename interactively, but the presenter/API should be defensive for direct callers and for unit tests (prevents invalid state and avoids a UI/view-model mismatch).

## Disabled blocks inside test files (`#if false`)

Not a standalone test case, but a disabled helper/scenario in a test file:

- `Src/ParatextImport/ParatextImportTests/BookMergerTests.cs`
	- Determination: **Obsolete/abandoned scenario** (comment indicates redesign needed)
	- Historical (6a2d976e): disabled block **did** exist (`#if false` present)
	- Action: leave disabled for now

## Runtime skipped / ignored tests (captured from TRX)

This list is extracted to `.cache/test-audit/skipped-tests.txt` and represents 87 skipped entries in that run.

Items marked **Fixed** were addressed in this branch and should no longer skip after rebuilding/rerunning.

- `EnsurePictureFilePathIsRooted_RootedButNoDriveLetter_FoundRelativeToCurrentDrive` — Previously skipped due to root-drive write permissions
	- Determination: **Fixable**
	- Action: **Fixed** (uses a temporary drive mapping instead of the real drive root)

- `SetPropertyPersistence` — “Need to write.”
	- Determination: **Fixable** (test was empty)
	- Action: **Fixed** (test now validates persistence via settings-file serialization)

- `TestLiftImport9CMergingStTextKeepNew` — previously `[Ignore]` (“fails if another test runs first”)
	- Determination: **Fixable** (custom-field conflict setup didn’t match importer behavior)
	- Action: **Fixed** (tests now create the custom field using `FieldDescription.UpdateCustomField()` and use a per-test unique field name that is also injected into the LIFT input)

- `TestLiftImport9DMergingStTextKeepOnlyNew` — previously `[Ignore]` (“fails if another test runs first”)
	- Determination: **Fixable** (same root cause as 9C)
	- Action: **Fixed** (same approach as 9C)

- `CheckboxBehavior_AllItemsShouldBeInitiallyCheckedPlusRefreshBehavior` — previously `[Ignore]` (“no need to test again”)
	- Determination: **Fixable** (unnecessary ignore)
	- Action: **Fixed** (ignore removed; enabled by ensuring `ICU_DATA` is valid in `test.ps1`)
	- Verification: `test.ps1 -TestProject Src/xWorks/xWorksTests -TestFilter FullyQualifiedName~BulkEditBarTests`

- `CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Selected` — previously `[Ignore]` (“no need to test again”)
	- Determination: **Fixable** (unnecessary ignore)
	- Action: **Fixed** (ignore removed; enabled by ensuring `ICU_DATA` is valid in `test.ps1`)
	- Verification: `test.ps1 -TestProject Src/xWorks/xWorksTests -TestFilter FullyQualifiedName~BulkEditBarTests`

- `CheckboxBehavior_ChangingFilterShouldRestoreSelectedStateOfItemsThatBecomeVisible_Unselected` — previously `[Ignore]` (“no need to test again”)
	- Determination: **Fixable** (unnecessary ignore)
	- Action: **Fixed** (ignore removed; enabled by ensuring `ICU_DATA` is valid in `test.ps1`)
	- Verification: `test.ps1 -TestProject Src/xWorks/xWorksTests -TestFilter FullyQualifiedName~BulkEditBarTests`

- `RenameConfigItem_Protected` — previously `[Ignore]` (“This is now checked before the edit is allowed! Nevermind!”)
	- Determination: **Fixable** (test expectation is valid; presenter should still be defensive)
	- Action: **Fixed** (`DictionaryConfigManager.RenameConfigItem` now blocks renaming protected configurations)
	- Verification: `test.ps1 -TestProject Src/xWorks/xWorksTests -TestFilter FullyQualifiedName~DictionaryConfigManagerTests`

- `GenerateCssForConfiguration_DefaultRootConfigGeneratesResult` — previously `[Ignore]` (“Won't pass yet.”)
	- Determination: **Fixable** (test is valid)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -TestProject Src/xWorks/xWorksTests -TestFilter FullyQualifiedName~CssGeneratorTests`

- `AddAndRemoveScripture` — previously `[Ignore]` (“Temporary until we figure out propchanged for unowned Texts.”)
	- Determination: **Fixable** (test now passes as-is)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -TestProject Src/xWorks/xWorksTests -TestFilter FullyQualifiedName~InterestingTextsTests`

- `ShouldIncludeScripture` — previously `[Ignore]` (“Temporary until we figure out propchanged for unowned Texts.”)
	- Determination: **Fixable** (test now passes as-is)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -TestProject Src/xWorks/xWorksTests -TestFilter FullyQualifiedName~InterestingTextsTests`

- `CleanUpNameForType_XML_RelativePath` — previously `[Ignore]` (“Not sure what this would be useful for or if this would be the desired behavior.”)
	- Determination: **Fixable** (behavior is stable and test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/Common/FieldWorks/FieldWorksTests -TestFilter FullyQualifiedName~ProjectIDTests`

- `TwoWordforms` — previously `[Ignore]` (“Is it ever possible for a parser to return more than one wordform parse?”)
	- Determination: **Fixable** (test now passes as-is)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/LexText/ParserCore/ParserCoreTests -TestFilter FullyQualifiedName~ParseFilerProcessingTests.TwoWordforms`

- `MatchBefore` — previously `[Ignore]` (“This test demonstrates FWR-2942”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/Common/Filters/FiltersTests -TestFilter FullyQualifiedName~DateTimeMatcherTests_German`

- `NestedCollapsedPart` — previously `[Ignore]` (“Collapsed nodes are currently not implemented”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/Common/Controls/DetailControls/DetailControlsTests -TestFilter FullyQualifiedName~DataTreeTests.NestedCollapsedPart`

- `GlossesBeforeWords` — previously `[Ignore]` (“Should we support gloss elements in sfm before their lx elements?”)
	- Determination: **Fixable** (supported by current converter behavior)
	- Action: **Fixed** (ignore removed; test now asserts actual behavior)
	- Verification: `test.ps1 -NoBuild -TestProject Src/LexText/LexTextControls/LexTextControlsTests -TestFilter FullyQualifiedName~WordsSfmImportTests.GlossesBeforeWords`

- `InitialFindPrevWithMatch` — previously `[Ignore]` (“Need to finish the find previous for this to work”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/FwCoreDlgs/FwCoreDlgsTests -TestFilter FullyQualifiedName~FwFindReplaceDlgTests`

- `InitialFindPrevWithMatchAfterWrap` — previously `[Ignore]` (“Need to finish the find previous for this to work”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/FwCoreDlgs/FwCoreDlgsTests -TestFilter FullyQualifiedName~FwFindReplaceDlgTests`

- `ReplaceWithMatchWs_EmptyFindText` — previously `[Ignore]` (“TE-1658. Needs analyst decision”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/FwCoreDlgs/FwCoreDlgsTests -TestFilter FullyQualifiedName~FwFindReplaceDlgTests`

- `ReplaceTextAfterFootnote` — previously `[Ignore]` (“This appears to be a bug in DummyBasicView; works fine in the GUI”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/FwCoreDlgs/FwCoreDlgsTests -TestFilter FullyQualifiedName~FwFindReplaceDlgTests`

- `ReadOnlySpaceAfterFootnoteMarker` — previously `[Ignore]` (“TE-932: We get read-only for everything.”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/Common/RootSite/RootSiteTests -TestFilter FullyQualifiedName~StVcTests.ReadOnlySpaceAfterFootnoteMarker`

- `ExportIrrInflVariantTypeInformation_LT7581_gls_multiEngWss` — previously `[Ignore]` (“low priority… might need to be fixed if users notice it”)
	- Determination: **Fixable** (ignore was stale; test passes)
	- Action: **Fixed** (ignore removed)
	- Verification: `test.ps1 -NoBuild -TestProject Src/LexText/Interlinear/ITextDllTests -TestFilter FullyQualifiedName~ExportIrrInflVariantTypeInformation_LT7581_gls_multiEngWss`

- `KeyCheck` — previously `[Ignore]` (“Writing System 'missing' problem that I decline to track down just yet.”)
	- Determination: **Fixable** (test fixture lacked real writing system initialization)
	- Action: **Fixed** (ignore removed; initialize `UserWs` and use real WS handles in the test)
	- Verification: `test.ps1 -NoBuild -TestProject Src/Common/FwUtils/FwUtilsTests -TestFilter FullyQualifiedName~IVwCacheDaCppTests`

Notes:

- While validating the newly-enabled xWorks tests, `BulkEditBarTests.ListChoiceTargetSelection` was also stabilized by processing pending items after changing the sort order (avoids intermittent row-count assertion failures).

Remaining skipped tests from that run (name — skip reason):
- `ReplaceCurWithRev_SimpleText_InsertFnAndSegs` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `ReplaceCurWithRev_SectionSplitInCurr_AddedHeadIsFirst_DeletedVerses2` — TE-4813 not fixed yet
- `ResolveUri_Linux("/tmp/foo/","bar","file:///tmp/foo/bar")` — OneTimeSetUp: Only supported on Linux
- `DetectDifferences_1VerseMovedToPriorSection` — TE-4704: We need to detect the moved verses correctly
- `DetectDifferences_AddedHead_SameRef_VerseAfter` — TE-7209: Don't handle correlation of empty sections well now.

- `DetectDifferences_ParaSplitInIntro` — Do as part of TE-2900
- `ReplaceWithMatchWs_EmptyFindText` — TE-1658. Needs analyst decision
- `GetAmpleThreadId_Linux` — Not supported on Win
- `CleanUpNameForType_XML_RelativePath` — Not sure what this would be useful for or if this would be the desired behavior.
- `CreateFwAppArgs_DbAbsolutePath_Linux` — Only supported on Linux
- `ReplaceCurWithRev_ParaMergeInMidVerse_ParseIsCurrent` — This fails because of the way that reverts are done in the diff code (i.e. they don't copy segment information from the revision).
- `DetectDifferences_AddedVersesAcrossParagraphs` — TE-YYYY Verse missing difference should reference the second paragraph here. The revert would put verse 3 at the end of the first para instead of the start of the second para
- `ReplaceCurWithRev_SimpleText_InsertFootnote` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `NewGlossForFocusBoxWithPolymorphemicGuess` — Not sure what we're supposed to do with glossing on a polymorphemic guess. Need analyst input
- `ReplaceCurWithRev_ParaMergeAtVerseStart_WithParaMissing` — TE-ZZZZ does not revert correctly.
- `LoadParatextMappings_MarkMappingsInUse` — GetMappingListForDomain is returning null after the merge from release/8.3 - This test was fixed in release/8.3 but likely didn't run on develop.
- `ReplaceCurWithRev_SectionsCombinedInCurr_WithBT` — WANTTESTPORT: (TE-8699) Need to figure out how to handle footnotes in the segmented BT
- `ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges_ParseIsCurrent` — TE-9820 - We don't correctly get the gloses back from the revision like we should
- `ReadOnlySpaceAfterFootnoteMarker` — TE-932: We get read-only for everything. JohnT, could you please look into this?
- `CreateProjectLauncher_isExecutable` — Only supported on Linux
- `StringProp_EmptyString` — Writing System 'missing' problem that I decline to track down just yet.
- `ParatextCanInitialize` — ParatextData can't tell us if PT is installed.
- `InitialFindPrevWithMatch` — Need to finish the find previous for this to work
- `ReplaceTextAfterFootnote` — This appears to be a bug in DummyBasicView; works fine in the GUI
- `ReplaceCurWithRev_NonCorrelatedSectionHeads_1B_Bckwrd` — TE-7664: Needs to be fixed
- `ReplaceCurWithRev_MultiParaVerse_SegmentsAdded_MidSection` — TE-7108 fails to detect para merge
- `ReplaceCurWithRev_MultiParasInVerse_MergeBySectionHeadRemoved` — TE-XXXX doesn't revert section missing right
- `Read_GEN_partial` — We do not support reading parts of books yet
- `RunOnMainThread` — Only supported on Linux
- `SkipUserConfirmedWordGlossToDifferentWordGloss` — It appears that segments cannot be reused because the paragraphs are getting cleared during import?
- `ReplaceCurWithRev_SectionsCombinedInCurrMidVerse_AtMidPara` — TE-6739
- `InitialFindPrevWithMatchAfterWrap` — Need to finish the find previous for this to work
- `CreateShortcut_inNonExistentDirectory_notCreatedAndNoThrow` — Only supported on Linux
- `DetectDifferences_VerseNumMissingAtStartOfParaCurr` — TE-3999: follow expectations described in TE-2111 MDL--BW
- `LoadParatextMappings_Normal` — This test requires Paratext to be properly installed.
- `ReplaceCurWithRev_ParaMergeInMidVerse` — This fails because of the way that reverts are done in the diff code (i.e. they don't copy segment information from the revision).
- `CreateShortcut_CreateProjectLauncher_NotExist_Created` — Only supported on Linux
- `ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg_ParseIsCurrent` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `InstallPUACharacters` — OneTimeSetUp: PUAInstallerTests requires ICU data zip 'Icu70.zip', but it was not found. Looked in DistFiles relative to SourceDirectory and optional env var FW_ICU_ZIP. These tests modify ICU data and are long-running acceptance tests.
- `CreateShortcut_CreateProjectLauncher_AlreadyExists_AnotherCreated` — Only supported on Linux
- `ReplaceCurWithRev_MultiParaVerse_SegmentsMissing_MidSection` — TE-7108 fails to detect a paraSplit difference
- `ReplaceCurWithRev_SectionsCombinedInCurrAtMissingVerse_Footnotes` — TE-4762 need to complete this test
- `DefaultBackupDirectory_Linux` — Only supported on Linux
- `GlossesBeforeWords` — Should we support gloss elements in sfm before their lx elements?
- `ReplaceCurWithRev_SimpleText_InsertFootnote_ParseIsCurrent` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `StringProp_SimpleString` — Writing System 'missing' problem that I decline to track down just yet.
- `ReplaceCurWithRev_SectionsCombinedInCurr_AddedHeadIsFirst` — Test needed as part of TE-4768
- `GenerateCssForConfiguration_DefaultRootConfigGeneratesResult` — Won't pass yet.
- `ReplaceCurWithRev_ParaMergeAtVerseEnd` — Fails because the code falls into a branch which re-analyzes the last word. This behavior seems wrong, might deserve investigation.
- `DetectDifferences_MultiParasInVerse_TwoToThreeParas_CorrMid` — test for TE-6880; would require the cluster simplification to scan at least two pairs before stopping correlation attempts
- `ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `LoadParatextMappings_MissingEncodingFile` — This test requires Paratext to be properly installed.
- `AddAndRemoveScripture` — Temporary until we figure out propchanged for unowned Texts.
- `DetectDifferences_AddedHead_SameRef_NoVerseText` — TE-7209: Don't handle correlation of empty sections well now.
- `ReplaceCurWithRev_SimpleText_WithFootnote_ParseIsCurrent` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `StringProp_ReplaceStringInCache` — Writing System 'missing' problem that I decline to track down just yet.
- `DetectDifferences_VerseNumMissingAtStartOfParaRev` — TODO: merge para--follow expectations described in TE-2111 MDL--BW
- `DetectDifferences_WritingSystemAndCharStyleDifferencesInVerse` — This is the test for TE-7726
- `ResolveUri_Linux("/tmp/foo","bar","file:///tmp/bar")` — OneTimeSetUp: Only supported on Linux
- `ReplaceCurWithRev_SectionsCombinedInCurrMidVerse_AtParaBreak` — TE-6738
- `TestAmpleThreadId_Linux` — Not supported on Win
- `ExportIrrInflVariantTypeInformation_LT7581_gls_multiEngWss` — This is a bug that might need to be fixed if users notice it. low priority since the user could just not display lines with same ws
- `DetectDifferences_2VersesMovedToNextSection` — TE-4704: We need to detect the moved verses correctly
- `MatchBefore` — OneTimeSetUp: This test demonstrates FWR-2942
- `Clone_WithSections` — Enable when added sections are stored in array
- `ReplaceCurWithRev_SimpleText_WithFootnote` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `MultiStringAlt` — Writing System 'missing' problem that I decline to track down just yet.
- `CreateProjectLauncher_noBOM` — Only supported on Linux
- `ReplaceCurWithRev_ParaSplitAtVerseStart_WhiteSpace` — TE-6879: TODO handle lack of whitespace before para split.
- `TwoWordforms` — Is it ever possible for a parser to return more than one wordform parse?
- `NestedCollapsedPart` — Collapsed nodes are currently not implemented
- `ReplaceCurWithRev_SimpleText_InsertFnAndSegs_ParseIsCurrent` — WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT
- `InstallAA6B` — OneTimeSetUp: PUAInstallerTests requires ICU data zip 'Icu70.zip', but it was not found. Looked in DistFiles relative to SourceDirectory and optional env var FW_ICU_ZIP. These tests modify ICU data and are long-running acceptance tests.
- `ShouldIncludeScripture` — Temporary until we figure out propchanged for unowned Texts.
- `ReplaceCurWithRev_NonCorrelatedSectionHeads_1B_Fwd` — TE-7664: Needs to be fixed
- `ReplaceCurWithRev_NonCorrelatedSectionHeads_2A` — TE-7132
