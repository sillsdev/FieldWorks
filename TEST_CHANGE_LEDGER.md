# TEST_CHANGE_LEDGER

Range: `3c2e41b9..HEAD`

Note: the working tree currently includes additional, uncommitted changes (not represented in the git range above). Those are called out inline where relevant.

Purpose: An exhaustive ledger of every commit in-range that touched `**/*Tests.cs`, with a per-file classification:

- **Mechanical**: refactors/rewrites to keep tests compiling/running under new tooling (NUnit API changes, adapter-friendly asserts, mock framework swaps, formatting/cleanup) without materially changing what the test is trying to prove.
- **Substantive**: changes that alter test behavior/assumptions or remove real external dependencies (registry/COM/file system/global state), or that change what is validated.
- **Mixed**: both in the same file change.

Analysis mapping key (used as `[A]`, `[B1]`, … in this ledger):

- **[A] Baseline-ignore enforcement** (policy compliance; baseline ignored tests remain ignored)
- **[B1] Registry-backed directory discovery** (ProjectsDirectory / registry-seeded paths)
- **[B2] Teardown/finalization/COM lifetime** (Dispose ordering, notifications, finalizers)
- **[B3] EncConverters registry dependency** (SilEncConverters40; lazy-init/injection)
- **[B4] UI-threading/layout/selection determinism** (STA, layout, selection reconstruction)
- **[B5] ICU data + collation variability** (`ICU_DATA`, ICU zip discovery, sort-key variability)
- **[B6] External install/plugin presence** (Paratext/FlexBridge/plugins/MEF discovery)
- **[B7] Filesystem hermeticity** (temp paths, avoid drive roots, virtual drives)
- **[B8] Culture/locale sensitivity** (CultureInfo-dependent expectations)
- **[B9] Third-party library variability** (ExCSS parsing quirks, XSL URI resolution)
- **[C1] Build/localization behavior shift** (task behavior/expectations changed during modernization)
- **[C2] Domain expectation corrections** (test assertions updated to match intended/real behavior)
- **[D1] Mechanical runner-compat** (mock-framework swaps, assertion direction, compiled placeholders)

Hard constraint reminder: tests ignored on `origin/release/9.3` must remain ignored. Some commits in this range temporarily removed baseline ignores; those were re-applied in the working tree to keep the branch compliant.

---

## Commits touching `**/*Tests.cs` (8)

### 2025-12-19 — `65f81dca8c5b19c652cc921d865e9d08f9f84289` — In process test fixing commit

- [C1] Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs — **Substantive** — updates expectations to match new “resx mismatch no longer fails build” behavior; broadens error-message match.
- [C1] Build/Src/FwBuildTasks/FwBuildTasksTests/NormalizeLocalesTests.cs — **Substantive** — updates Malay (“ms”) expectation: treat as already-normalized (no “rename away” behavior).
- [B4] Src/Common/RootSite/RootSiteTests/StVcTests.cs — **Substantive** — enforces STA and explicitly creates/lays out RootBox before creating selections.
- [D1] Src/Common/ViewsInterfaces/ViewsInterfacesTests/ExtraComInterfacesTests.cs — **Mechanical** — converts `#if false` historical Mono COM test into a compiled `[Ignore]` placeholder (TRX-visible).
- [B4] Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs — **Substantive** — enforces STA and adjusts test data setup (`IScrTxtParaFactory.CreateWithStyle`) to make selection behavior consistent.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupDlgTests.cs — **Mechanical** — replaces `#if false` with compiled `[Ignore]` placeholder + archives legacy tests under `RUN_LW_LEGACY_TESTS`.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/RestoreProjectPresenterTests.cs — **Mechanical** — same pattern: compiled `[Ignore]` placeholder + legacy block under `RUN_LW_LEGACY_TESTS`.
- [C2] Src/LexText/Interlinear/ITextDllTests/AddWordsToLexiconTests.cs — **Substantive** — changes expectation for `Sandbox.UsingGuess` (real analysis is not treated as a computed “guess” in this test context).
- [C2] Src/LexText/Interlinear/ITextDllTests/InterlinearExporterTests.cs — **Substantive** — tightens `LexGloss` export expectation (avoid duplicate lines for same WS).
- [C2] Src/LexText/ParserCore/ParserCoreTests/ParseFilerProcessingTests.cs — **Substantive** — splits parse results per wordform and processes each, instead of reusing one `ParseResult`.
- [B9] Src/xWorks/xWorksTests/CssGeneratorTests.cs — **Substantive** — updates link CSS expectations (no underline + `unset|currentColor`) and adds failure isolation for ExCSS parse exceptions.
- [B2] Src/xWorks/xWorksTests/InterestingTextsTests.cs — **Substantive** — exercises deletion path by removing texts via the mock repository so `PropChanged`/cleanup is realistic.

Supporting non-test `.cs` changes in this commit:

- [B4] Src/Common/Controls/DetailControls/Slice.cs — **Substantive** — changes default expansion behavior so slices only auto-expand when explicitly marked `expansion="expanded"` (reduces UI-state surprises in tests).
- [B1] Src/Common/FieldWorks/ProjectId.cs — **Substantive** — improves relative-path handling so paths with a directory component resolve under ProjectsDirectory without the “double nest” behavior.
- [B2] Src/Common/SimpleRootSite/SimpleRootSite.cs — **Substantive** — best-effort unregisters `IVwNotifyChange` notifications during close/dispose to prevent post-dispose `PropChanged` callbacks destabilizing tests.
- [B4] Src/FwCoreDlgs/FindCollectorEnv.cs — **Substantive** — fixes start-location semantics across duplicate renderings, ensures the start offset applies only to the exact selected occurrence (tag + `cpropPrev`) including strings emitted via `AddString(...)`, and captures WS at match offsets (stabilizes find/replace selection reconstruction).

Working tree follow-up (uncommitted):

- [B4] Src/FwCoreDlgs/FindCollectorEnv.cs — **Substantive** — further refines start-location application to require an exact occurrence match and avoids clearing the start location prematurely in the `AddString(...)` path; resolves the remaining `FwFindReplaceDlgTests` failures under VSTest.
- [B4] Src/FwCoreDlgs/FwFindReplaceDlg.cs — **Substantive** — TE-1658: WS-only replace behavior (empty Find text + Match WS) no longer advances incorrectly; adds selection restoration + WS propagation.
- [B9] Src/xWorks/CssGenerator.cs — **Substantive** — avoids ExCSS crashes on `inherit` by using `none`/`unset` (and correct `UnitType.Ident`) to keep CSS generation deterministic.
- [C2] Src/xWorks/DictionaryConfigManager.cs — **Substantive** — keeps rename operation defensive (revert/refresh on protected items) to match test expectations.
- [D1] Build/Src/FwBuildTasks/RegFreeCreator.cs — **Mechanical** — fixes a node selection bug when adding/replacing CLR class entries in reg-free manifests.
- [D1] test.ps1 — **Mechanical** — mitigates a VSTest multi-assembly edge case where `vstest.console.exe` returns exit code `-1` even when TRX reports 0 failures, by retrying per-assembly and aggregating exit codes; emits per-assembly TRX + console logs to aid isolation.

### 2025-12-17 — `44e740104481e4a0c2d9cdda2dc615931b4e9fbf` — Clear ignore fixes

- [A,B7] Build/Src/FwBuildTasks/FwBuildTasksTests/GoldEticToXliffTests.cs — **Substantive** — reworks integration test to run hermetically (temp output dir + discover `DistFiles/Templates/GOLDEtic.xml`); **baseline was ByHand/ignored**.
- [A] Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs — **Policy-only** — temporarily un-ignored `InsideOutBracesReported`; **baseline ignored must remain ignored** (re-applied in working tree).
- [A,B7] Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeListsTests.cs — **Substantive** — makes integration test self-contained (writes XML to temp dir and round-trips); **baseline was ByHand/ignored**.
- [A] Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs — **Policy-only** — temporarily un-ignored `NestedCollapsedPart`; **baseline ignored must remain ignored** (re-applied).
- [A,B1] Src/Common/FieldWorks/FieldWorksTests/ProjectIDTests.cs — **Policy-only** — temporarily un-ignored `CleanUpNameForType_XML_RelativePath`; **baseline ignored must remain ignored** (re-applied).
- [B8] Src/Common/Filters/FiltersTests/DateTimeMatcherTests.cs — **Substantive** — re-enables the German culture fixture by removing the ignore (“demonstrates FWR-2942”).
- [C2] Src/Common/FwUtils/FwUtilsTests/IVwCacheDaTests.cs — **Substantive** — re-enables “missing writing system” tests by ensuring WS engines exist and setting `ktptWs` correctly.
- [A,B4] Src/Common/RootSite/RootSiteTests/StVcTests.cs — **Policy-only** — temporarily un-ignored `ReadOnlySpaceAfterFootnoteMarker`; **baseline ignored must remain ignored** (re-applied).
- [B7] Src/FXT/FxtDll/FxtDllTests/StandFormatExportTests.cs — **Substantive** — re-enables Standard Format export tests and stops writing to `C:\` by using temp file paths.
- [A,B4] Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs — **Policy-only** — temporarily un-ignored multiple tests; **baseline ignored must remain ignored** (re-applied).
- [C2] Src/LexText/Interlinear/ITextDllTests/AddWordsToLexiconTests.cs — **Substantive** — re-enables the “polymorphemic guess” glossing test (was waiting on analyst input).
- [C2] Src/LexText/Interlinear/ITextDllTests/InterlinearExporterTests.cs — **Substantive** — re-enables “multi Eng WS gloss” export test (previously noted as low priority bug).
- [C2] Src/LexText/ParserCore/ParserCoreTests/ParseFilerProcessingTests.cs — **Substantive** — re-enables “TwoWordforms” parse case.
- [B5] Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs — **Mechanical** — changes missing ICU zip handling from `Assert.Ignore` to `Assume.That(false, ...)` (inconclusive).
- [C2] Src/xWorks/xWorksTests/BulkEditBarTests.cs — **Substantive** — adds pending-item processing before assertions and re-enables override tests that were marked “no need to test again.”
- [B9] Src/xWorks/xWorksTests/CssGeneratorTests.cs — **Substantive** — re-enables CSS parse validity test for the default configuration.
- [C2] Src/xWorks/xWorksTests/DictionaryConfigManagerTests.cs — **Substantive** — re-enables “protected rename” test (guard now prevents edit).
- [B2] Src/xWorks/xWorksTests/InterestingTextsTests.cs — **Substantive** — re-enables Scripture include/remove behavior tests (previously disabled pending PropChanged semantics).

Supporting non-test `.cs` changes in this commit:

- [B5] Src/Common/FwUtils/FwUtils.cs — **Substantive** — adds a “dev/worktree” fallback for `ICU_DATA` (uses DistFiles payload) so ICU-dependent tests can run on clean machines without a machine install.

### 2025-12-17 — `12fa4aa9a1f4296fc2dae49fe7548d77d6404124` — Ingored files audit - none were ignored before

- [A,C2] Src/LexText/LexTextControls/LexTextControlsTests/LiftMergerTests.cs — **Mixed** — attempts to de-flake by (a) clearing per-run custom-field caches in setup, (b) generating unique custom field names, (c) creating custom fields via `FieldDescription.UpdateCustomField()`; **also temporarily un-ignored baseline-flaky tests** (re-applied).
- [B7] Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportTests.cs — **Substantive** — uses `DefineDosDevice` to map a temp drive so “rooted without drive letter” tests don’t write to the real drive root.
- [B7] Src/XCore/xCoreInterfaces/xCoreInterfacesTests/PropertyTableTests.cs — **Substantive** — replaces `[Ignore("Need to write.")]` with a real persistence test using a temp settings folder.

### 2025-12-16 — `9de50f848d2630f6db661cd189272c7661ad6e0b` — VSTest migration + managed test stabilization

- [C2] Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs — **Substantive** — moves metadata/custom-field + entry creation from fixture-level into per-test setup to avoid nested unit-of-work and improves disposal.
- [B6] Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs — **Substantive** — replaces Paratext API objects with lightweight in-memory `IScrText` test doubles (no PT disposal/interop) and adjusts project-type semantics.
- [D1] Src/LexText/Interlinear/ITextDllTests/InterlinMasterTests.cs — **Mechanical** — removes a brittle debug string that dereferenced writing system IDs during dispose.
- [D1] Src/LexText/LexTextControls/LexTextControlsTests/LiftMergerRelationTests.cs — **Mechanical** — fixes `Does.Contain` assertion direction.
- [B5] Src/ManagedLgIcuCollator/ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.cs — **Substantive** — stops expecting `Close()` to throw and replaces byte-for-byte ICU sort-key assertions with invariants.
- [B5] Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs — **Substantive** — makes ICU zip discovery explicit (DistFiles + optional `FW_ICU_ZIP`) and skips tests when data is unavailable.
- [C2] Src/xWorks/xWorksTests/BulkEditBarTests.cs — **Substantive** — adjusts test to target the correct `MoForm` records and avoid brittle list-size expectations.
- [D1] Src/xWorks/xWorksTests/DictionaryConfigurationUtilsTests.cs — **Mechanical** — fixes `Does.Contain` assertion direction.
- [D1] Src/xWorks/xWorksTests/XhtmlDocViewTests.cs — **Mechanical** — fixes multiple `Does.Contain` assertion directions.

Supporting non-test `.cs` changes in this commit:

- [C2] Src/LexText/Morphology/RespellerDlg.cs — **Substantive** — makes the undo action resilient when the “special” SDA/metadata cache isn’t available by reconstructing from managed MDC; throws a clear error if impossible.
- [B5] Src/ManagedLgIcuCollator/LgIcuCollator.cs — **Substantive** — implements stable managed `get_SortKey` semantics and a safer sort-key comparison to remove brittle assumptions.
- [B3] Src/Utilities/SfmToXml/Converter.cs — **Substantive** — stops constructing EncConverters by default; lazy-inits only when a mapping actually references a converter.
- [B9] Src/Utilities/XMLUtils/XmlUtils.cs — **Substantive** — makes XSL include/import URI resolution robust (absolute URI + rooted file path support; safe fallback on invalid paths).
- [B2] Src/xWorks/RecordClerk.cs — **Substantive** — prevents finalizer exceptions from escaping (avoids runner/testhost crashes during teardown).

### 2025-12-16 — `7c861a2427973c7ca88b7bf80241ba59c8a19079` — feat: drop test-time dependency on registry-backed SilEncConverters40 EncConverters

- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/AdvancedScriptRegionVariantModelTests.cs — **Mechanical** (assertion order fix)
- [B3] Src/FwCoreDlgs/FwCoreDlgsTests/CnvtrPropertiesCtrlTests.cs — **Substantive** (replaces registry-backed EncConverters with test-local “UndefinedConverters” data)
- [B3] Src/LexText/Interlinear/ITextDllTests/InterlinSfmImportTests.cs — **Substantive** (removes registry/global dependency; uses fakes)
- [B3] Src/ParatextImport/ParatextImportTests/SCTextEnumTests.cs — **Substantive** (lazy-init EncConverters, only when needed)

Supporting non-test `.cs` changes in this commit:

- [B3] Src/FwCoreDlgs/CnvtrPropertiesCtrl.cs — **Substantive** — avoids instantiating EncConverters during control `Load`; moves creation to lazy-init so tests don’t trigger registry/repository IO unexpectedly.
- [B3] Src/LexText/Interlinear/Sfm2FlexText.cs — **Substantive** — adds an `IEncConverters` injection constructor so tests can supply fakes.
- [B3] Src/LexText/LexTextControls/Sfm2FlexTextWords.cs — **Substantive** — replaces concrete EncConverters dependency with `IEncConverters` and adds constructor injection for tests.
- [B3] Src/ParatextImport/SCScriptureText.cs — **Substantive** — removes eager EncConverters creation from `TextEnum(...)`; defers to SCTextEnum.
- [B3] Src/ParatextImport/SCTextEnum.cs — **Substantive** — lazy-inits EncConverters only when a `LegacyMapping` requires it (keeps the common path registry-free and deterministic).

### 2025-12-15 — `bfc34af4b7b1a555099cd33346f5046aa7077d30` — Remove Docker and container references from FieldWorks build system

- [D1] Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.cs — **Mechanical** — comment-only adjustment: “Linux/Docker” → “Linux”.

### 2025-12-11 — `aa4332a802c1d3f060c075192437e8499ce3f6aa` — Build: stabilize container/native pipeline and test stack

(High churn / broad changes; many are runner/tooling-driven but some are substantive environment seams.)

- [D1] Src/Common/Controls/Widgets/WidgetsTests/FwListBoxTests.cs — **Mechanical** — removes unused fixture field.
- [D1] Src/Common/Controls/Widgets/WidgetsTests/FwTextBoxTests.cs — **Mechanical** — removes unused fixture field.
- [D1] Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs — **Mechanical** — Rhino.Mocks → Moq conversion (incl. out-parameter setup and selection helper mocking).
- [B6] Src/Common/FwUtils/FwUtilsTests/FLExBridgeHelperTests.cs — **Substantive** — gates tests on FlexBridge presence (skip rather than fail when not installed).
- [D1] Src/Common/FwUtils/FwUtilsTests/FwRegistryHelperTests.cs — **Mechanical** — cleanup / runner-compat.
- [D1] Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs — **Mechanical** — Rhino.Mocks → Moq conversion.
- [D1] Src/Common/RootSite/RootSiteTests/RootSiteGroupTests.cs — **Mechanical** — stabilization / mock behavior adjustments.
- [B6] Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs — **Substantive** — introduces an injectable ScriptureProvider seam for better test isolation.
- [B6] Src/Common/SimpleRootSite/SimpleRootSiteTests/IbusRootSiteEventHandlerTests.cs — **Substantive** — removes Linux/IBus-specific test file.
- [D1] Src/Common/SimpleRootSite/SimpleRootSiteTests/SimpleRootSiteTests_IsSelectionVisibleTests.cs — **Mechanical** — stabilization.
- [D1] Src/Common/ViewsInterfaces/ViewsInterfacesTests/ExtraComInterfacesTests.cs — **Mechanical** — runner-compat changes around obsolete interface usage.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupDlgTests.cs — **Mechanical** — archives obsolete dialog tests behind a skip/guard pattern.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupModelTests.cs — **Mechanical** — runner-compat.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/RestoreProjectPresenterTests.cs — **Mechanical** — archives obsolete presenter tests behind a skip/guard pattern.
- [D1] Src/LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.cs — **Mechanical** — runner-compat.
- [D1] Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs — **Mechanical** — runner-compat.
- [D1] Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs — **Mechanical** — runner-compat.
- [D1] Src/LexText/Interlinear/ITextDllTests/InterlinDocForAnalysisTests.cs — **Mechanical** — runner-compat.
- [D1] Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs — **Mixed** — adjusts mediator usage due to sealed class constraints.
- [D1] Src/Utilities/MessageBoxExLib/MessageBoxExLibTests/Tests.cs — **Mechanical** — runner-compat.
- [D1] Src/xWorks/xWorksTests/InterestingTextsTests.cs — **Mechanical** — runner-compat.

Supporting non-test `.cs` changes in this commit:

- [B6] Src/Common/ScriptureUtils/ScriptureProvider.cs — **Substantive** — adds a test override hook (`AppDomain` data) and a safe fallback provider when MEF discovery fails; enables Paratext-facing tests to run without installed plugins.

### 2025-12-11 — `a76869f09a6b50d398ab9218af4ffe468ea9b168` — fix(tests): apply VSTest and test failure fixes from branch

- [B1,B7] Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.cs — **Substantive** (rooted temp paths to avoid registry-based ProjectsDirectory lookup)
- [B2] Src/Common/FwUtils/FwUtilsTests/IVwCacheDaTests.cs — **Substantive** (explicit COM teardown/GC to prevent AV during VSTest cleanup)
- [D1] Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs — **Substantive** (stop mocking sealed Mediator; use real)
- [D1] Src/ParatextImport/ParatextImportTests/SCTextEnumTests.cs — **Substantive** (Moq migration; avoids brittle mocking approach)
- [B5] Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs — **Substantive** (DistFiles path resolution made robust)
