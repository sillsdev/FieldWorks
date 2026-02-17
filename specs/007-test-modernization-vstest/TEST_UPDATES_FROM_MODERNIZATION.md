# TEST_UPDATES_FROM_MODERNIZATION

Branch/worktree: `007-test-modernization-vstest`
Baseline: `origin/release/9.3` (noted in specs as commit `6a2d976e`)

Related: [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) (exhaustive commit×file classification for `3c2e41b9..HEAD`).

## Substantive change analysis (post-`3c2e41b9`)

This section answers the primary question: for each **substantive** test change after `3c2e41b9`, why it passed on `release/9.3`, why the modernization/VSTest migration broke it (or made it flaky), and why the fix is the right root-cause fix.

This narrative uses the same bucket labels as the ledger tags in [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md): `[A]`, `[B1]`, … `[D1]`.

### A) Baseline-ignore enforcement (policy compliance)

Several commits in `3c2e41b9..HEAD` attempted to “clear ignores” by re-enabling tests that were already ignored on `origin/release/9.3`. That violates the branch’s hard constraint, so the working tree intentionally re-applies the baseline `[Ignore(...)]` attributes.

Baseline-ignored tests that were temporarily re-enabled and then intentionally re-ignored:

- By-hand integration tests:
  - [Build/Src/FwBuildTasks/FwBuildTasksTests/GoldEticToXliffTests.cs](Build/Src/FwBuildTasks/FwBuildTasksTests/GoldEticToXliffTests.cs)
  - [Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeListsTests.cs](Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeListsTests.cs)
- Not-implemented / historically disabled tests:
  - [Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs](Build/Src/FwBuildTasks/FwBuildTasksTests/LocalizeFieldWorksTests.cs)
  - [Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs](Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs)
  - [Src/Common/FieldWorks/FieldWorksTests/ProjectIDTests.cs](Src/Common/FieldWorks/FieldWorksTests/ProjectIDTests.cs)
  - [Src/Common/RootSite/RootSiteTests/StVcTests.cs](Src/Common/RootSite/RootSiteTests/StVcTests.cs)
  - [Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs](Src/FwCoreDlgs/FwCoreDlgsTests/FwFindReplaceDlgTests.cs)
  - [Src/LexText/LexTextControls/LexTextControlsTests/LiftMergerTests.cs](Src/LexText/LexTextControls/LexTextControlsTests/LiftMergerTests.cs)

For the complete classification of “baseline ignore was removed/reintroduced”, see [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md).

### B) Environment/state dependencies surfaced by VSTest

#### B1) Registry-backed directory discovery (ProjectsDirectory)

- Why it passed on `release/9.3`:
  - Many dev machines (and legacy runners) had the FieldWorks registry state already present, so relative project names could be resolved via `FwDirectoryFinder.ProjectsDirectory`.
- Why modernization broke it:
  - VSTest runs are designed to be more hermetic (clean worktrees, CI machines, and “no global state” expectations). Tests that depended on registry-backed discovery became non-deterministic or failed.
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `a76869f…`, `FieldWorksTests` was changed to use rooted temp paths so `ProjectId.CleanUpNameForType` doesn’t consult the registry.

#### B2) COM lifetime + teardown ordering under VSTest

- Why it passed on `release/9.3`:
  - Legacy runners and process lifetimes often masked COM-release timing problems; finalizers ran while native DLLs were still loaded.
- Why modernization broke it:
  - VSTest process teardown can expose timing-sensitive COM finalization crashes (native DLL unload before RCW finalizer thread runs).
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `a76869f…`, `IVwCacheDaTests` explicitly releases COM objects and forces GC/finalizers in teardown to ensure cleanup occurs while DLLs are loaded.
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commits `65f81dc…` and `9de50f84…`, production code was also hardened against teardown timing:
    - `SimpleRootSite.CloseRootBox()` best-effort unregisters notifications to avoid post-dispose callbacks.
    - `RecordClerk` finalizer now swallows exceptions to avoid testhost crashes during GC/finalization.

#### B3) Registry-backed SilEncConverters40 (HKLM dependency)

- Why it passed on `release/9.3`:
  - Developer machines often had EncConverters installed/configured, so tests could create/use converters via global registry-backed state.
- Why modernization broke it:
  - CI/worktree machines frequently lack those HKLM keys, making tests fail or become non-repeatable.
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `7c861a2…`, tests and code paths were adjusted to use fakes and lazy-init seams so EncConverters are only created when actually required.
  - Importantly, this was not “tests only”: production code was changed to avoid eager EncConverters creation (e.g., `SCTextEnum`, `CnvtrPropertiesCtrl`) and to support injection (e.g., `Sfm2FlexText`) so tests can supply fakes.
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `9de50f84…`, the SFM-to-XML converter was further adjusted to lazy-init EncConverters only when a mapping requires it.

#### B4) UI-threading assumptions (STA + layout timing)

- Why it passed on `release/9.3`:
  - NUnit-console execution patterns and apartment defaults often masked missing UI layout/setup steps.
- Why modernization broke it:
  - VSTest + NUnit adapter are less forgiving if a WinForms control/RootBox isn’t fully initialized before selections are created.
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `65f81dc…`, WinForms-heavy fixtures (e.g., `StVcTests`, `FwFindReplaceDlgTests`) were moved toward STA and explicit layout/root creation.
  - In the same commit, the underlying find/replace implementation was adjusted (`FindCollectorEnv`, `FwFindReplaceDlg`) to make selection reconstruction and WS-only search/replace behavior more deterministic under isolation.
  - UI-state defaults that influence tests were also tightened (e.g., `Slice` expansion now only auto-expands when explicitly marked).
  - Follow-up: `FwFindReplaceDlgTests` are now passing under VSTest after refining `FindCollectorEnv` start-location semantics for the `AddString(...)` rendering path and ensuring the start offset only applies to the exact selected occurrence of the string property.

#### B5) ICU data discovery (ICU_DATA)

- Why it passed on `release/9.3`:
  - Many dev machines had `ICU_DATA` configured via install tooling.
- Why modernization broke it:
  - Clean machines/worktrees can lack `ICU_DATA`, causing ICU-dependent tests to fail even when DistFiles contains ICU data.
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `44e74010…`, `FwUtils.InitializeIcu()` adds a best-effort fallback to the repo/worktree DistFiles ICU payload when `ICU_DATA` is missing.

#### B6) Scripture provider discovery (plugin presence)

- Why it passed on `release/9.3`:
  - Developer machines often had Paratext plugins installed/discoverable via MEF.
- Why modernization broke it:
  - VSTest/CI runs may not have plugins present, and brittle “MEF must discover a provider” assumptions can make tests fail for environmental reasons.
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `aa4332a8…`, `ScriptureProvider` gained a test override hook (AppDomain data) plus a safe fallback provider when discovery fails.

#### B7) Filesystem hermeticity (temp paths, avoid drive roots)

- Why it passed on `release/9.3`:
  - Some tests assumed they could write to fixed paths (including drive roots) or rely on stable machine locations.
- Why modernization broke it:
  - VSTest/CI runs are much more likely to run without elevated privileges and on machines where writing to `C:\` (or similar) is restricted or undesirable.
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commits `44e74010…` and `12fa4aa9…`, tests were updated to use temp folders and, where needed, virtual drive mappings (e.g., `DefineDosDevice`) so “rooted without a drive letter” scenarios don’t hit real drive roots.

> **Review required**: The `DefineDosDevice` approach introduces new risks:
> 1. **Leaked mappings**: If a test crashes, the virtual drive persists until reboot
> 2. **Concurrency conflicts**: Parallel test runs may compete for the same drive letter
> 3. **Permission variance**: Behavior differs under UAC or restricted accounts
> 4. **Semantic change**: Tests writing to `C:\` may have been *intentional* integration tests validating real filesystem behavior
>
> Recommend: Document which tests used real paths *by design* (integration) vs. *by accident* (poor isolation). For the latter, temp folders suffice. For the former, consider a dedicated integration test category that runs in isolation with appropriate permissions, rather than virtualizing the filesystem.

#### B8) Culture/locale sensitivity

- Why it passed on `release/9.3`:
  - Developer machines often matched the culture/locale assumed by the test.
- Why modernization broke it:
  - CI and “clean” test runs can have different user/system cultures, changing parsing/formatting behavior.
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `44e74010…`, `DateTimeMatcherTests` re-enables the German culture fixture as an explicit culture-sensitive test.

#### B9) Third-party library variability (ExCSS, XSL include/import resolution)

- Why it passed on `release/9.3`:
  - The parsing behavior (or error modes) of dependencies like ExCSS and XSL resolvers may have been effectively “stable enough” in the prior runner/tooling environment.
- Why modernization broke it:
  - VSTest migration surfaced brittle assumptions (e.g., unhandled parser exceptions, different URI base behavior).
- Fix rationale (root cause):
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `65f81dc…`, CSS generation was adjusted to avoid ExCSS crashes and tests were hardened to isolate parse failures.
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `9de50f84…`, `XmlUtils` XSL include/import resolution was made more robust across absolute/rooted paths.

### C) Behavior/expectation shifts (not purely runner mechanics)

#### C1) Build/localization behavior shifts

- Observed changes:
  - In [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md) under commit `65f81dc…`, `LocalizeFieldWorksTests` and `NormalizeLocalesTests` were updated to match newer behavior (e.g., “resx mismatch no longer fails the build”, and how Malay `ms` is treated).
- Why this is bucketed separately:
  - These aren’t directly “VSTest isolation” issues; they’re closer to “the build task behavior now differs, so the test expectation had to follow.”

#### C2) Domain expectation corrections

- Observed changes:
  - Multiple tests were updated to reflect intended/actual application semantics rather than runner behavior (examples in the ledger include `AddWordsToLexiconTests`, `InterlinearExporterTests`, `ParseFilerProcessingTests`, `BulkEditBarTests`, `DictionaryConfigManagerTests`, and parts of `DataTreeTests`).
- Why modernization exposed them:
  - Runner changes (isolation order, stricter teardown, reduced ambient state) often convert “quietly wrong but passing” expectations into visible failures, forcing correctness decisions.

### D) Mechanical changes (runner/tooling compatibility)

#### D1) Mock framework + assertion mechanics

- What changed:
  - Broad Rhino.Mocks/NSubstitute → Moq migration, assertion direction fixes, comment-only adjustments, and compiled `[Ignore]` placeholders to keep disabled tests visible in TRX.
- Why this is treated as mechanical:
  - These changes generally don’t alter product behavior; they’re required to keep tests buildable/runnable and reporting correctly under the modern runner.

#### D1) VSTest multi-assembly exit code `-1` (false fail)

- What we observed:
  - A full-suite run that invoked `vstest.console.exe` with multiple test assemblies returned exit code `-1`, causing `test.ps1` to report `[FAIL] ... exit code: -1`.
  - The generated TRX indicated `failed="0"` (i.e., no failing tests), suggesting the exit code was unreliable for that scenario.
- Why modernization surfaced it:
  - Moving from prior runners to VSTest changes process topology (testhost/execution isolation) and how failures are reported. In practice, “multi-assembly + VSTest” occasionally yields a non-specific `-1` even without test failures.
- Fix rationale (root cause):
  - We treat the TRX/test results as the source of truth and make the orchestration resilient: when the multi-assembly invocation returns `-1`, `test.ps1` retries per assembly and aggregates exit codes.
  - This preserves strictness (real failures still fail the run) while avoiding false negatives caused by a runner edge case.
  - The per-assembly retry also produces actionable evidence: per-assembly TRX (`<AssemblyName>_<timestamp>.trx`) and per-assembly console logs (`vstest.<AssemblyName>.console.log`) in `Output\<Configuration>\TestResults`.

## Fishy / uncertain mappings (worth a quick human review)

Nothing here looks *obviously* off-scope for “stabilize tests under VSTest”, but these items do touch behavior enough that it’s reasonable to sanity-check intent:

- `Slice` default expansion behavior change (ledger `[B4]` under `65f81dc…`): could affect UI defaults beyond tests; confirm intended.
- `RegFreeCreator` manifest node selection fix (ledger `[D1]` under `65f81dc…`): plausibly unrelated to test modernization; confirm it was required (or at least safe) to land in this branch.
- Localization expectations (`LocalizeFieldWorksTests`, `NormalizeLocalesTests`) (ledger `[C1]`): these encode product/build decisions; confirm the “new behavior” is intended and not a regression.
- `DictionaryConfigManager` “protected rename” defensiveness (ledger `[C2]`): confirm it matches UX/requirements (it’s more than a test-only stabilization).

## Supporting references (where the details live)

- The authoritative “what changed, per file, per commit” view is in [TEST_CHANGE_LEDGER.md](TEST_CHANGE_LEDGER.md).
- Specs and checklists:
  - `specs/007-test-modernization-vstest/spec.md`
  - `specs/007-test-modernization-vstest/quickstart.md`
  - `specs/007-test-modernization-vstest/IGNORED_TESTS.md`
- Runner configuration:
  - `Test.runsettings`
  - `test.ps1` / `build.ps1`
- Evidence cache used to build the ledger (commit messages + per-commit patches):
  - `.cache/test-ledger/`

## Current failing tests (TODO)

As of the most recent TRX in `Output\Debug\TestResults` (see `Output\Debug\TestResults\vstest.console.log`), there are no remaining failures in `FwFindReplaceDlgTests`.

Note: if a full-suite run ever reports `[FAIL] ... exit code: -1` while TRX shows 0 failures, `test.ps1` will retry per-assembly and the per-assembly TRX/logs should be used to confirm whether there are any real failures.

Baseline-ignored tests remain ignored (per branch policy).

## “No popups ever” plan (with external citations)

Goal: test runs should never block CI/dev runs with modal UI dialogs.

1) **Run VSTest in isolation and centralize testhost configuration**
   - Use `InIsolation=true` in runsettings (already set in `Test.runsettings`).
   - Rationale: isolated execution makes the `vstest.console.exe` process less likely to be taken down by testhost crashes.
   - Citation: VSTest console `/InIsolation` description: https://learn.microsoft.com/en-us/visualstudio/test/vstest-console-options?view=vs-2022

2) **Use runsettings as the canonical place to push “no dialogs” environment variables**
   - Put all testhost environment variables under `<RunConfiguration><EnvironmentVariables>`.
   - Rationale: the test platform supports configuring the run through runsettings and (optionally) overriding elements via command line.
   - Citation: runsettings configuration principles and examples: https://raw.githubusercontent.com/microsoft/vstest-docs/main/docs/configure.md

3) **Ensure UI-threading expectations are explicit for tests that touch UI/COM threading**
   - For any tests requiring STA, use NUnit’s `[Apartment(ApartmentState.STA)]` at the fixture/test level (or assembly level if appropriate).
   - This reduces “wrong apartment” behavior that can sometimes manifest as UI/COM failures.
   - Citation: NUnit Apartment attribute documentation: https://docs.nunit.org/articles/nunit/writing-tests/attributes/apartment.html

4) **FieldWorks-specific assertion UI suppression**
   - Continue using `AssertUiEnabled=false` (already present in `Test.runsettings`).
   - Keep `AssertExceptionEnabled=true` so assertion failures surface as failing tests rather than dialogs.

## Appendix: Changed paths (raw)

The raw list of “test-ish” paths changed on this branch (used earlier for the broad catalog view) is maintained in `.cache/changed_testish_paths.txt`.
