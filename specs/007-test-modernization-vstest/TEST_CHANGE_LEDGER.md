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

> **Taxonomy improvement needed**: The current bucket labels group by symptom/domain rather than risk level. Consider adding a secondary classification:
> - **Risk: Low** — Mechanical/attribute-only changes (STA declaration, mock framework swap)
> - **Risk: Medium** — Test-environment adaptations (layout timing, temp path usage)
> - **Risk: High** — Production code changes or expectation reversals (FindCollectorEnv, domain assertions)
> This would allow reviewers to filter for high-risk items without reading every [B4] entry. Alternatively, split [B4] into [B4a] STA/threading attributes, [B4b] layout/init ordering, [B4c] selection/navigation semantics.

Hard constraint reminder: tests ignored on `origin/release/9.3` should not be un-ignored *by default*. If we want to enable one, we must also fix the underlying behavior (and rerun under `test.ps1`). Some commits in this range temporarily removed baseline ignores during investigation; as of HEAD, multiple baseline ignores are still present in source (see `IGNORED_TESTS.md` for the corrected status).

End-game conclusions (audit vs `release/9.3`):

- Verified against `release/9.3` source: `NestedCollapsedPart`, `CleanUpNameForType_XML_RelativePath`, `ReadOnlySpaceAfterFootnoteMarker`, `InitialFindPrevWithMatch`, `InitialFindPrevWithMatchAfterWrap`, `ReplaceWithMatchWs_EmptyFindText`, `TestLiftImport9C*`, `TestLiftImport9D*`, and the `ByHand` integration tests in `GoldEticToXliffTests` and `LocalizeListsTests` all have baseline `[Ignore]` markers.
- Current-branch reality: most of those baseline `[Ignore]` markers still exist in source today; the earlier “fixed/unignored” narrative was aspirational and has been corrected in `IGNORED_TESTS.md`.
- One exception: the `BulkEditCheckBoxBehaviorTestsWithFilterChanges` `CheckboxBehavior_*` overrides were baseline-ignored but are empty stubs; removing those ignores is safe and does not materially change test coverage (the base class provides the real assertions).
- Uncertainty: this ledger does not prove that any baseline-ignored test would pass if enabled; treat “fix approach” notes as hypotheses until a rerun confirms them.

---

## Commits touching `**/*Tests.cs` (8)

### 2025-12-19 — `65f81dca8c5b19c652cc921d865e9d08f9f84289` — In process test fixing commit

- [C1] Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-e8a22e5cb4692406527751071ac72689)) — **Substantive** — updates expectations to match new “resx mismatch no longer fails build” behavior; broadens error-message match.
- [C1] Build/Src/FwBuildTasks/FwBuildTasksTests/NormalizeLocalesTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-e7f2eccdeedc297503d3daf3be228e90)) — **Substantive** — updates Malay (“ms”) expectation: treat as already-normalized (no “rename away” behavior).
- [B4] Src/Common/RootSite/RootSiteTests/StVcTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-1f9a7891c279ab1b4e217e0e2177b9f4)) — **Substantive** — enforces STA and explicitly creates/lays out RootBox before creating selections.
- [D1] Src/Common/ViewsInterfaces/ViewsInterfacesTests/ExtraComInterfacesTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-35c894eabac624714924604910979dfc)) — **Mechanical** — converts `#if false` historical Mono COM test into a compiled `[Ignore]` placeholder (TRX-visible).
- [B4] Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-959c8e422d5d29a926b016be44382c8c)) — **Substantive** — enforces STA and adjusts test data setup (`IScrTxtParaFactory.CreateWithStyle`) to make selection behavior consistent.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupDlgTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-bd909d32896beb5b6907daa6af5cdeac)) — **Mechanical** — replaces `#if false` with compiled `[Ignore]` placeholder + archives legacy tests under `RUN_LW_LEGACY_TESTS`.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/RestoreProjectPresenterTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-2fa5ee3f71c7bace21fb8382aea0b0db)) — **Mechanical** — same pattern: compiled `[Ignore]` placeholder + legacy block under `RUN_LW_LEGACY_TESTS`.
- [C2] Src/LexText/Interlinear/ITextDllTests/AddWordsToLexiconTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-2d4b7775e8395bacffc075956c4fa7f8)) — **Substantive** — changes expectation for `Sandbox.UsingGuess` (real analysis is not treated as a computed “guess” in this test context).

> **Review required**: Changed expectation from "real analysis counts as guess" to "real analysis is not a guess." Hypothesis: the test previously passed because ambient state (shared fixtures, prior test side effects) left the Sandbox in a "guess" state that wasn't reset. VSTest isolation exposed that the expectation was testing pollution, not intent. Verify with domain expert whether `UsingGuess` should return true when a real analysis exists.

- [C2] Src/LexText/Interlinear/ITextDllTests/InterlinearExporterTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-92fb673162de4e871b77cfa9af14b192)) — **Substantive** — tightens `LexGloss` export expectation (avoid duplicate lines for same WS).

> **Review required**: Tightened expectation to "avoid duplicate lines for same WS." Hypothesis: the old test tolerated duplicates because export ordering was non-deterministic (dictionary iteration, collection ordering). VSTest or .NET 4.8 changed iteration order, exposing duplicates that were always emitted but previously appeared in expected order by accident. Verify whether duplicate glosses for the same writing system are valid export output or a bug in the exporter.

- [C2] Src/LexText/ParserCore/ParserCoreTests/ParseFilerProcessingTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-4ecc9a59a23320450521af65f67cc171)) — **Substantive** — splits parse results per wordform and processes each, instead of reusing one `ParseResult`.

> **Review required**: Changed from reusing one `ParseResult` across wordforms to processing each separately. Hypothesis: the old test was incorrectly reusing a mutable object, and earlier runners happened to execute in an order that masked the state pollution. VSTest parallel/isolated execution surfaced the mutation. Verify whether `ParseResult` is designed to be reusable or if the old test was fundamentally incorrect about the API contract.

- [B9] Src/xWorks/xWorksTests/CssGeneratorTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-af8ac1fdf5b8e96c8674f0cda060f3bd)) — **Substantive** — updates link CSS expectations (no underline + `unset|currentColor`) and adds failure isolation for ExCSS parse exceptions.
- [B2] Src/xWorks/xWorksTests/InterestingTextsTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-05febd98e39d54cd28a83d83e95b8445)) — **Substantive** — exercises deletion path by removing texts via the mock repository so `PropChanged`/cleanup is realistic.

Supporting non-test `.cs` changes in this commit:

- [B4] Src/Common/Controls/DetailControls/Slice.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-aaeadd3ea59d30055646f15563cf84fa)) — **Substantive** — changes default expansion behavior so slices only auto-expand when explicitly marked `expansion="expanded"` (reduces UI-state surprises in tests).
- [B1] Src/Common/FieldWorks/ProjectId.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-3f2f5a8ab43afb6284a241fedda0227a)) — **Substantive** — improves relative-path handling so paths with a directory component resolve under ProjectsDirectory without the “double nest” behavior.
- [B2] Src/Common/SimpleRootSite/SimpleRootSite.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-2cb4e58c680f3128196e9a009ed4d3b4)) — **Substantive** — best-effort unregisters `IVwNotifyChange` notifications during close/dispose to prevent post-dispose `PropChanged` callbacks destabilizing tests.

> **Review required**: "Best-effort unregister" implies exception swallowing. Verify:
> 1. What happens when unregistration fails? Are exceptions logged or silently dropped?
> 2. If unregistration fails, do notification handlers hold references that prevent garbage collection?
> 3. For long-running FLEx sessions (hours), does this create observable memory growth?
> Consider: add telemetry or debug logging when unregistration fails, so silent leaks become visible. The test crash was a symptom of a real lifecycle problem—suppressing it may just relocate the bug.

- [B4] Src/FwCoreDlgs/FindCollectorEnv.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/65f81dca8c5b19c652cc921d865e9d08f9f84289#diff-d272719a2a252bf53acf6ea5a7dd98ef)) — **Substantive** — fixes start-location semantics across duplicate renderings, ensures the start offset applies only to the exact selected occurrence (tag + `cpropPrev`) including strings emitted via `AddString(...)`, and captures WS at match offsets (stabilizes find/replace selection reconstruction).

> **Review required**: The [B4] classification may be incorrect. Changes to `FindCollectorEnv.cs` modify production find/replace semantics (start-location tracking, occurrence matching in `AddString(...)` path), not just test environment adaptation. A developer should verify whether:
> 1. The old behavior was always buggy (making this a bug fix, not a test fix), or
> 2. The new behavior introduces a functional regression in find/replace for end users.
> If (1), reclassify as [C2] Domain expectation correction. If (2), revert and find a test-only solution.

Working tree follow-up (uncommitted):

- [B4] Src/FwCoreDlgs/FindCollectorEnv.cs — **Substantive** — further refines start-location application to require an exact occurrence match and avoids clearing the start location prematurely in the `AddString(...)` path; resolves the remaining `FwFindReplaceDlgTests` failures under VSTest.
- [B4] Src/FwCoreDlgs/FwFindReplaceDlg.cs — **Substantive** — TE-1658: WS-only replace behavior (empty Find text + Match WS) no longer advances incorrectly; adds selection restoration + WS propagation.
- [B9] Src/xWorks/CssGenerator.cs — **Substantive** — avoids ExCSS crashes on `inherit` by using `none`/`unset` (and correct `UnitType.Ident`) to keep CSS generation deterministic.

> **Review required**: Changing CSS output from `inherit` to `none`/`unset` alters browser rendering behavior:
> - `inherit` explicitly inherits from parent
> - `none` resets to initial/default
> - `unset` behaves like `inherit` for inherited properties, `initial` for others
>
> These are semantically different in CSS cascade contexts. This is a **user-visible output change**, not a test fix. Verify:
> 1. Has dictionary/publication CSS export been tested in target browsers (Chrome, Firefox, print renderers)?
> 2. Would pinning or upgrading ExCSS avoid the crash without changing output semantics?
> 3. If `inherit` genuinely isn't needed, document why—otherwise this is a workaround masquerading as a fix.

- [C2] Src/xWorks/DictionaryConfigManager.cs — **Substantive** — keeps rename operation defensive (revert/refresh on protected items) to match test expectations.
- [D1] Build/Src/FwBuildTasks/RegFreeCreator.cs — **Mechanical** — fixes a node selection bug when adding/replacing CLR class entries in reg-free manifests.
- [D1] test.ps1 — **Mechanical** — mitigates a VSTest multi-assembly edge case where `vstest.console.exe` returns exit code `-1` even when TRX reports 0 failures, by retrying per-assembly and aggregating exit codes; emits per-assembly TRX + console logs to aid isolation.

> **Review required**: The retry-per-assembly workaround assumes VSTest exit code `-1` is a false positive when TRX reports 0 failures. This assumption is unverified. Possible masked failures include:
> - Test host crashes after test completion (finalizer/COM teardown explosions)
> - Assembly load failures for DLLs that never executed
> - Infrastructure timeouts or resource exhaustion
> Recommend: capture and analyze the actual stderr/diagnostic output when `-1` occurs. If it's a known VSTest bug, document the issue number. If it's a real post-test crash, the retry logic is hiding instability that will surface in production.

> **Audit gap**: These changes are documented but not committed. Risks:
> 1. `git log`/`git diff` against the documented range (`3c2e41b9..HEAD`) won't include them
> 2. Working tree reset, stash, or worktree deletion loses these changes permanently
> 3. Reviewers may assume these are committed and auditable when they are not
>
> Action required: Either commit these changes (updating the ledger range to reflect the new HEAD) or explicitly mark this section as "PENDING COMMIT — do not consider audited." Production code changes (`FindCollectorEnv.cs`, `FwFindReplaceDlg.cs`, `CssGenerator.cs`) should not remain uncommitted while being documented as analysis-complete.

### 2025-12-17 — `44e740104481e4a0c2d9cdda2dc615931b4e9fbf` — Clear ignore fixes

- [A,B7] Build/Src/FwBuildTasks/FwBuildTasksTests/GoldEticToXliffTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-69ee8aa0a56461ab6aef03eeff5886ce)) — **Substantive** — keeps the `ByHand` integration test ignored but makes it hermetic (temp output dir + discover `DistFiles/Templates/GOLDEtic.xml`) so manual runs are reliable.
- [A] Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-e8a22e5cb4692406527751071ac72689)) — **Policy-only** — `InsideOutBracesReported` is baseline-ignored (“not implemented”) and remains ignored as of HEAD; this commit only reflects a temporary toggle during investigation.
- [A,B7] Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeListsTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-9ec625ad735d0fb8defc1ca45aa6ea20)) — **Substantive** — keeps the `ByHand` integration test ignored but makes its inputs/outputs self-contained (writes XML to temp dir and round-trips) for dependable manual inspection.
- [A] Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-dd1d429190074f904e4ecb2b48a8ba02)) — **Policy-only** — `NestedCollapsedPart` is baseline-ignored (“collapsed nodes … not implemented”) and is still ignored as of HEAD; this commit only reflects a temporary toggle.
- [A,B1] Src/Common/FieldWorks/FieldWorksTests/ProjectIDTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-3e0d2693e0a6418ed7dca363084b766e)) — **Policy-only** — `CleanUpNameForType_XML_RelativePath` is baseline-ignored (“desired behavior?”) and remains ignored as of HEAD; unignoring requires a product decision.
- [B8] Src/Common/Filters/FiltersTests/DateTimeMatcherTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-03b6220247ed037923f15e3e84b29baa)) — **Substantive** — re-enables the German culture fixture by removing the ignore (“demonstrates FWR-2942”).
- [C2] Src/Common/FwUtils/FwUtilsTests/IVwCacheDaTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-52b8fe853e2e67edf50b0f570cd343f9)) — **Substantive** — re-enables “missing writing system” tests by ensuring WS engines exist and setting `ktptWs` correctly.
- [A,B4] Src/Common/RootSite/RootSiteTests/StVcTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-1f9a7891c279ab1b4e217e0e2177b9f4)) — **Policy-only** — `ReadOnlySpaceAfterFootnoteMarker` is baseline-ignored (TE-932) and remains ignored as of HEAD; enabling it would require validating the read-only behavior under VSTest.
- [B7] Src/FXT/FxtDll/FxtDllTests/StandFormatExportTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-b10ff8232dcb0c246d55b1e8f9ca5731)) — **Substantive** — re-enables Standard Format export tests and stops writing to `C:\` by using temp file paths.
- [A,B4] Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-959c8e422d5d29a926b016be44382c8c)) — **Policy-only** — several tests here are baseline-ignored (`InitialFindPrevWithMatch*`, `ReplaceWithMatchWs_EmptyFindText`, `ReplaceTextAfterFootnote`) and remain ignored as of HEAD; unignoring requires either finishing the underlying behavior (“find previous”) or an analyst decision (TE-1658).
- [C2] Src/LexText/Interlinear/ITextDllTests/AddWordsToLexiconTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-2d4b7775e8395bacffc075956c4fa7f8)) — **Substantive** — re-enables the “polymorphemic guess” glossing test (was waiting on analyst input).
- [C2] Src/LexText/Interlinear/ITextDllTests/InterlinearExporterTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-92fb673162de4e871b77cfa9af14b192)) — **Substantive** — re-enables “multi Eng WS gloss” export test (previously noted as low priority bug).
- [C2] Src/LexText/ParserCore/ParserCoreTests/ParseFilerProcessingTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-4ecc9a59a23320450521af65f67cc171)) — **Substantive** — re-enables “TwoWordforms” parse case.
- [B5] Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-667001eee6d9b65101a87ea61178fff5)) — **Mechanical** — changes missing ICU zip handling from `Assert.Ignore` to `Assume.That(false, ...)` (inconclusive).
- [C2] Src/xWorks/xWorksTests/BulkEditBarTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-9cd4fdf5807524eb9e1d0f662556f171)) — **Substantive** — adds pending-item processing before assertions and un-ignores redundant override stubs (“no need to test again”). The enabled overrides are intentionally empty; the real behavioral assertions are in the base class.
- [B9] Src/xWorks/xWorksTests/CssGeneratorTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-af8ac1fdf5b8e96c8674f0cda060f3bd)) — **Substantive** — re-enables CSS parse validity test for the default configuration.
- [C2] Src/xWorks/xWorksTests/DictionaryConfigManagerTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-8465053087bfdbb8fecfdfaa32c5c9b0)) — **Substantive** — re-enables “protected rename” test (guard now prevents edit).
- [B2] Src/xWorks/xWorksTests/InterestingTextsTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-05febd98e39d54cd28a83d83e95b8445)) — **Substantive** — re-enables Scripture include/remove behavior tests (previously disabled pending PropChanged semantics).

Supporting non-test `.cs` changes in this commit:

- [B5] Src/Common/FwUtils/FwUtils.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/44e740104481e4a0c2d9cdda2dc615931b4e9fbf#diff-a99aba0fd9f2dddf3321cc384adb5a7d)) — **Substantive** — adds a “dev/worktree” fallback for `ICU_DATA` (uses DistFiles payload) so ICU-dependent tests can run on clean machines without a machine install.

### 2025-12-17 — `12fa4aa9a1f4296fc2dae49fe7548d77d6404124` — Ingored files audit - none were ignored before

- [A,C2] Src/LexText/LexTextControls/LexTextControlsTests/LiftMergerTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/12fa4aa9a1f4296fc2dae49fe7548d77d6404124#diff-e98f24ab5faaac63c4bb2d4eb2335c94)) — **Mixed** — attempts to de-flake by (a) clearing per-run custom-field caches in setup, (b) generating unique custom field names, (c) creating custom fields via `FieldDescription.UpdateCustomField()`. As of HEAD, the key baseline-ignored tests (`TestLiftImport9C*`/`9D*`) are still `[Ignore]` in source; enabling them remains unverified.
- [B7] Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/12fa4aa9a1f4296fc2dae49fe7548d77d6404124#diff-f6936e5682f5bd920c59fd1e951ae510)) — **Substantive** — uses `DefineDosDevice` to map a temp drive so “rooted without drive letter” tests don’t write to the real drive root.
- [B7] Src/XCore/xCoreInterfaces/xCoreInterfacesTests/PropertyTableTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/12fa4aa9a1f4296fc2dae49fe7548d77d6404124#diff-7290bc5e2af1556d8392835c0a6d8b1a)) — **Substantive** — replaces `[Ignore("Need to write.")]` with a real persistence test using a temp settings folder.

### 2025-12-16 — `9de50f848d2630f6db661cd189272c7661ad6e0b` — VSTest migration + managed test stabilization

- [C2] Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-dd1d429190074f904e4ecb2b48a8ba02)) — **Substantive** — moves metadata/custom-field + entry creation from fixture-level into per-test setup to avoid nested unit-of-work and improves disposal.
- [B6] Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-a219e23bb5a8fb11890c49acf689b43f)) — **Substantive** — replaces Paratext API objects with lightweight in-memory `IScrText` test doubles (no PT disposal/interop) and adjusts project-type semantics.
- [D1] Src/LexText/Interlinear/ITextDllTests/InterlinMasterTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-7703f11c685e735a4ec09abda8b313b9)) — **Mechanical** — removes a brittle debug string that dereferenced writing system IDs during dispose.
- [D1] Src/LexText/LexTextControls/LexTextControlsTests/LiftMergerRelationTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-80d1cfe5d4323e4ff2dc3007f7af3887)) — **Mechanical** — fixes `Does.Contain` assertion direction.
- [B5] Src/ManagedLgIcuCollator/ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-e6190fcd6ff18b806f5bd92bc9ef2d73)) — **Substantive** — stops expecting `Close()` to throw and replaces byte-for-byte ICU sort-key assertions with invariants.
- [B5] Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-667001eee6d9b65101a87ea61178fff5)) — **Substantive** — makes ICU zip discovery explicit (DistFiles + optional `FW_ICU_ZIP`) and skips tests when data is unavailable.
- [C2] Src/xWorks/xWorksTests/BulkEditBarTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-9cd4fdf5807524eb9e1d0f662556f171)) — **Substantive** — adjusts test to target the correct `MoForm` records and avoid brittle list-size expectations.
- [D1] Src/xWorks/xWorksTests/DictionaryConfigurationUtilsTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-6da0f6dd341efcb8164c5e58fdceb851)) — **Mechanical** — fixes `Does.Contain` assertion direction.
- [D1] Src/xWorks/xWorksTests/XhtmlDocViewTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-fdd0947d0873d178bc3e9a14cea88bc5)) — **Mechanical** — fixes multiple `Does.Contain` assertion directions.

Supporting non-test `.cs` changes in this commit:

- [C2] Src/LexText/Morphology/RespellerDlg.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-b4720101ad95a79b4765619306fb0def)) — **Substantive** — makes the undo action resilient when the “special” SDA/metadata cache isn’t available by reconstructing from managed MDC; throws a clear error if impossible.
- [B5] Src/ManagedLgIcuCollator/LgIcuCollator.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-8087e87256bf9f926da6c974a08b7c1f)) — **Substantive** — implements stable managed `get_SortKey` semantics and a safer sort-key comparison to remove brittle assumptions.
- [B3] Src/Utilities/SfmToXml/Converter.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-6b0dcb9ccbd7cc073da750b193a448d7)) — **Substantive** — stops constructing EncConverters by default; lazy-inits only when a mapping actually references a converter.
- [B9] Src/Utilities/XMLUtils/XmlUtils.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-2f2379d324a6efcaba0c830b3d8c4e8c)) — **Substantive** — makes XSL include/import URI resolution robust (absolute URI + rooted file path support; safe fallback on invalid paths).
- [B2] Src/xWorks/RecordClerk.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/9de50f848d2630f6db661cd189272c7661ad6e0b#diff-30e55bd28aa849f9e754c4f4bb5d4ee1)) — **Substantive** — prevents finalizer exceptions from escaping (avoids runner/testhost crashes during teardown).

### 2025-12-16 — `7c861a2427973c7ca88b7bf80241ba59c8a19079` — feat: drop test-time dependency on registry-backed SilEncConverters40 EncConverters

- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/AdvancedScriptRegionVariantModelTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-0d6ca466bbe1b15cf48db447f225c537)) — **Mechanical** (assertion order fix)
- [B3] Src/FwCoreDlgs/FwCoreDlgsTests/CnvtrPropertiesCtrlTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-8634a15269b4afa7f10ffc646b7c3e36)) — **Substantive** (replaces registry-backed EncConverters with test-local “UndefinedConverters” data)
- [B3] Src/LexText/Interlinear/ITextDllTests/InterlinSfmImportTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-fb28da4aa101fdb24f30c27a0265c3a3)) — **Substantive** (removes registry/global dependency; uses fakes)
- [B3] Src/ParatextImport/ParatextImportTests/SCTextEnumTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-afea1319984606b241e6a114320555c0)) — **Substantive** (lazy-init EncConverters, only when needed)

Supporting non-test `.cs` changes in this commit:

- [B3] Src/FwCoreDlgs/CnvtrPropertiesCtrl.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-0d65a68e1b58b22b8b1325cab54d1ffe)) — **Substantive** — avoids instantiating EncConverters during control `Load`; moves creation to lazy-init so tests don’t trigger registry/repository IO unexpectedly.
- [B3] Src/LexText/Interlinear/Sfm2FlexText.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-cab3fd558235eeffa4bc1a4cc5da6d07)) — **Substantive** — adds an `IEncConverters` injection constructor so tests can supply fakes.
- [B3] Src/LexText/LexTextControls/Sfm2FlexTextWords.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-5181bd2513ee5f9013155525588f5f52)) — **Substantive** — replaces concrete EncConverters dependency with `IEncConverters` and adds constructor injection for tests.
- [B3] Src/ParatextImport/SCScriptureText.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-a564404694a67ffeeab876f3dc43aafa)) — **Substantive** — removes eager EncConverters creation from `TextEnum(...)`; defers to SCTextEnum.
- [B3] Src/ParatextImport/SCTextEnum.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/7c861a2427973c7ca88b7bf80241ba59c8a19079#diff-83561bdd9d5275526667ca5c0c7577b1)) — **Substantive** — lazy-inits EncConverters only when a `LegacyMapping` requires it (keeps the common path registry-free and deterministic).

### 2025-12-15 — `bfc34af4b7b1a555099cd33346f5046aa7077d30` — Remove Docker and container references from FieldWorks build system

- [D1] Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/bfc34af4b7b1a555099cd33346f5046aa7077d30#diff-58dc5eb80dfe78cae3f74c984e72bd28)) — **Mechanical** — comment-only adjustment: “Linux/Docker” → “Linux”.

### 2025-12-11 — `aa4332a802c1d3f060c075192437e8499ce3f6aa` — Build: stabilize container/native pipeline and test stack

(High churn / broad changes; many are runner/tooling-driven but some are substantive environment seams.)

- [D1] Src/Common/Controls/Widgets/WidgetsTests/FwListBoxTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-e5924b9230d13b535bc85ce34576a560)) — **Mechanical** — removes unused fixture field.
- [D1] Src/Common/Controls/Widgets/WidgetsTests/FwTextBoxTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-c3e2abec9cf8d9fe2699723e8228609a)) — **Mechanical** — removes unused fixture field.
- [D1] Src/Common/Framework/FrameworkTests/FwEditingHelperTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-d1becd034aaba51af2bb509fc42e927f)) — **Mechanical** — Rhino.Mocks → Moq conversion (incl. out-parameter setup and selection helper mocking).
- [B6] Src/Common/FwUtils/FwUtilsTests/FLExBridgeHelperTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-139cd9359a1cdaec29556d1afbeee1b6)) — **Substantive** — gates tests on FlexBridge presence (skip rather than fail when not installed).
- [D1] Src/Common/FwUtils/FwUtilsTests/FwRegistryHelperTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-21aa8770e02178d8242a1bb3b56e2593)) — **Mechanical** — cleanup / runner-compat.
- [D1] Src/Common/RootSite/RootSiteTests/MoreRootSiteTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-8e55a737e40ed20f1c9f26920254aa06)) — **Mechanical** — Rhino.Mocks → Moq conversion.
- [D1] Src/Common/RootSite/RootSiteTests/RootSiteGroupTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-a04f7a00cd704b063f2247842a128dc3)) — **Mechanical** — stabilization / mock behavior adjustments.
- [B6] Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-a219e23bb5a8fb11890c49acf689b43f)) — **Substantive** — introduces an injectable ScriptureProvider seam for better test isolation.
- [B6] Src/Common/SimpleRootSite/SimpleRootSiteTests/IbusRootSiteEventHandlerTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-1c72eee6c495843b82196aaa1cee5869)) — **Substantive** — removes Linux/IBus-specific test file.
- [D1] Src/Common/SimpleRootSite/SimpleRootSiteTests/SimpleRootSiteTests_IsSelectionVisibleTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-2d5ba976e3ff42055bff57a0d907a222)) — **Mechanical** — stabilization.
- [D1] Src/Common/ViewsInterfaces/ViewsInterfacesTests/ExtraComInterfacesTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-35c894eabac624714924604910979dfc)) — **Mechanical** — runner-compat changes around obsolete interface usage.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupDlgTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-bd909d32896beb5b6907daa6af5cdeac)) — **Mechanical** — archives obsolete dialog tests behind a skip/guard pattern.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/FwWritingSystemSetupModelTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-17bc2d9390176685dd9a8ec31a092753)) — **Mechanical** — runner-compat.
- [D1] Src/FwCoreDlgs/FwCoreDlgsTests/RestoreProjectPresenterTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-2fa5ee3f71c7bace21fb8382aea0b0db)) — **Mechanical** — archives obsolete presenter tests behind a skip/guard pattern.
- [D1] Src/LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-374b4e178405a4081b1633ee715af2c6)) — **Mechanical** — runner-compat.
- [D1] Src/LexText/Interlinear/ITextDllTests/ComboHandlerTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-487758f6c63ac56c4959c189e9445568)) — **Mechanical** — runner-compat.
- [D1] Src/LexText/Interlinear/ITextDllTests/GlossToolLoadsGuessContentsTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-133451f1e9ca64147a18153352f0713a)) — **Mechanical** — runner-compat.
- [D1] Src/LexText/Interlinear/ITextDllTests/InterlinDocForAnalysisTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-fbdb59e3302390eb22fc9ff0c78dc870)) — **Mechanical** — runner-compat.
- [D1] Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-3fa48bf6b79785979810c8434ffacfb4)) — **Mixed** — adjusts mediator usage due to sealed class constraints.
- [D1] Src/Utilities/MessageBoxExLib/MessageBoxExLibTests/Tests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-f52a8ae4d9b9f78d94a848945754b218)) — **Mechanical** — runner-compat.
- [D1] Src/xWorks/xWorksTests/InterestingTextsTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-05febd98e39d54cd28a83d83e95b8445)) — **Mechanical** — runner-compat.

Supporting non-test `.cs` changes in this commit:

- [B6] Src/Common/ScriptureUtils/ScriptureProvider.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/aa4332a802c1d3f060c075192437e8499ce3f6aa#diff-c2959b2b9ff1ef2765cc09b2baf97443)) — **Substantive** — adds a test override hook (`AppDomain` data) and a safe fallback provider when MEF discovery fails; enables Paratext-facing tests to run without installed plugins.

### 2025-12-11 — `a76869f09a6b50d398ab9218af4ffe468ea9b168` — fix(tests): apply VSTest and test failure fixes from branch

- [B1,B7] Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/a76869f09a6b50d398ab9218af4ffe468ea9b168#diff-58dc5eb80dfe78cae3f74c984e72bd28)) — **Substantive** (rooted temp paths to avoid registry-based ProjectsDirectory lookup)
- [B2] Src/Common/FwUtils/FwUtilsTests/IVwCacheDaTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/a76869f09a6b50d398ab9218af4ffe468ea9b168#diff-52b8fe853e2e67edf50b0f570cd343f9)) — **Substantive** (explicit COM teardown/GC to prevent AV during VSTest cleanup)
- [D1] Src/LexText/Morphology/MorphologyEditorDllTests/RespellingTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/a76869f09a6b50d398ab9218af4ffe468ea9b168#diff-3fa48bf6b79785979810c8434ffacfb4)) — **Substantive** (stop mocking sealed Mediator; use real)
- [D1] Src/ParatextImport/ParatextImportTests/SCTextEnumTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/a76869f09a6b50d398ab9218af4ffe468ea9b168#diff-afea1319984606b241e6a114320555c0)) — **Substantive** (Moq migration; avoids brittle mocking approach)
- [B5] Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs ([diff](https://github.com/sillsdev/FieldWorks/commit/a76869f09a6b50d398ab9218af4ffe468ea9b168#diff-667001eee6d9b65101a87ea61178fff5)) — **Substantive** (DistFiles path resolution made robust)
