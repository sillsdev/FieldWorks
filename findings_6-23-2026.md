# FieldWorks Avalonia Phase-1 — Cross-Cutting Review Findings

**Branch:** `010-advanced-entry-view-phase-1-2` &nbsp;•&nbsp; **Date:** 2026-06-23 &nbsp;•&nbsp; **Reviewer:** 10 parallel read-only review agents + synthesis
**Scope reviewed:** 39 commits vs `main` — **1,260 files, +147,794 / −3,082** (≈111k of it C#/XAML; 473 md docs; 173 PNGs)
**Safety posture:** Every Avalonia surface is gated behind the `UIMode` app-setting (default `"Legacy"`, `Settings.Designer.cs:82`). **Default users see no behavior change.** Nothing in this branch alters the shipped Legacy code path at runtime.

---

## 1. Verdict

**The Phase-1 lexicon spine is landable.** Architecture is sound, the LCModel-free view boundary is *compile-enforced*, write-back/undo/disposal are correct (the prior-review "missing rollback" was confirmed a false positive), and test coverage of the load-bearing paths is real and can-fail.

**The true blockers to merge are process gates, not code defects:**
1. **§19h.4 human-tester burn-down** — requires human testers; cannot be closed autonomously. *(SCOPE-09 / TEST-07)*
2. **Full native+managed `build.ps1` + `test.ps1` traversal has not been run this pass** (tasks.md 18.12 / 10.7). *(SCOPE-09)*

Everything else is `BlocksLanding: no` — quality, parity-polish, or follow-up-PR scoping, all behind the Legacy gate.

### Severity tally (43 findings across 10 dimensions)

| Severity | Count | Notes |
|---|---|---|
| **Blocks landing** | 2 | Both process gates (human testers + full CI run) |
| High (non-blocking) | 3 | A11Y dialog-bridge untested; §20 scope over-reach; full-CI evidence |
| Medium | 14 | Parity polish, keyboard a11y, teardown symmetry, docs drift |
| Low / positive-confirmation | 24 | Includes 7 explicit "verified correct" confirmations |

---

## 2. Validated PR decomposition (Path-2, confirmed against current code)

The agreed plan-of-record holds. The split is **mechanically trivial** — interlinear + rule-formula have *zero* back-references into the composer/region/seam infra (SCOPE-01).

1. **PR-1 — Phase-1 spine (the landing PR).** `lexiconBrowse` (table), `lexiconEdit`/`notebookEdit`/`posEdit` composer, KEEP dialogs (Chooser, Options, InsertEntry, EntryGo, MessageBox), all primitive controls + composer/region/seam infra + **shared dialog types** (`FwMsaGroupBox`, `FwFeatureStructureEditor`, `FwSandboxMsa`, `InMemoryRegionEditContext` — these must survive any back-out; KEEP InsertEntryDialog depends on them, SCOPE-03). Gated `UIMode=New` (default Legacy). **Gated only on the 2 process blockers above.**
2. **PR-2 — `avalonia-interlinear-editor` split.** Move interlinear files + OpenSpec change; drop 1 `registry.Register(...)` line (`RegionEditorPlugins.cs`) + 1 census entry (`LexemeEditorBurnDownTests`).
3. **PR-3 — `avalonia-rule-formula-editor` split.** Move 5 rule/env/IPA plugin files + OpenSpec change; drop 5 registry lines + census entries. Independent of PR-2.
4. **PR-4…N — back-out follow-ups (batchable).** Delete the **7 genuinely-unwired** dialogs (no call-site work): SpecialCharacter, WritingSystemProperties, ConfigureColumns, DateRange, FilterFor, FindReplace, PictureProperties. The **6 "wired" dialogs are all `UIMode`-gated with a working legacy `else` branch** (SCOPE-02) — back-out is review-scope reduction, *not* correctness-required. Each carries md + real LT JIRA + before/after PNG.
5. **Housekeeping (docs-only, any time).** Archive the 3 superseded OpenSpec changes (SCOPE-06); fix/clarify `INVENTORY.md` generation (SCOPE-08); reconcile §20 "DO NOT DEFER" wording with Path-2 (SCOPE-05).

> **Sequencing:** PR-1 can land *before* PR-2/3/4 as long as it stays `UIMode`-gated. Splits and back-outs are reviewer-scope hygiene, not prerequisites for spine correctness.

---

## 3. Prioritized work-off queue

### Tier A — fix now (safe, cheap, high-confidence; no scope risk)
| ID | Title | Action |
|---|---|---|
| **BUILD-01** | Committed NuGet artifacts (`Obj/FwBuildTasks/*.nuget.g.{props,targets}`) with stale cross-worktree absolute path | `git rm --cached` + add `Obj/` to `.gitignore` |
| **LOC-01** | Hardcoded `"Delete"`/`"Insert before"` menu labels (`RuleFormulaRegionEditor.cs:172,174`) | Route through `FwAvaloniaStrings` |
| **ARCH-04** | `_itemMenuBindings` cache key choice-unsafe (latent; safe today) | Add guard comment now; fix key with ARCH-03 |
| **NETFX-01** | `LangVersion=latest` on net48 lets net8-only syntax compile past review | Pin/document language version |
| **ARCH-02** | `seam-catalog.md` claims `IValidationService`/`IUndoRedoCoordinator`/`IHostSurface` that don't exist | Update docs to real shape |
| **SCOPE-09** | Full `build.ps1`+`test.ps1` not run this pass | **Run it** (evidence, no code change) |

### Tier B — gated decision (larger; needs your go-ahead — Section 6)
| ID | Title | Disposition |
|---|---|---|
| WIRE-01 | RecordEditView flip-to-legacy leaves Avalonia listeners attached (dormant/guarded) until Dispose | split-to-follow-up |
| A11Y-01/02/03 | No mnemonics; inverted dialog Tab order; no initial focus | split-to-follow-up |
| A11Y-04 | WinForms-hosted dialog UIA bridge untested (high) | split-to-follow-up |
| TEST-01 | Migration-doc `-after` PNG baselines missing (53 `-before`, 0 `-after`) | split-to-follow-up |
| TEST-04 | Product host-switch verified by private-field state, not realized window UIA | split-to-follow-up |
| FP-01 | Morph-type stem↔affix conversion rejected (legacy allowed, behind data-loss prompt) | split-to-follow-up |
| AV-02/03/04 | Hardcoded validation colors; PicturePropertiesDialog deviates from kit; ConfigureColumns no item template | split (note: AV-03/04 are back-out dialogs) |
| AV-01 | Owned `IDisposable` field controls not deterministically disposed by VMs | split-to-follow-up |

### Tier C — defer to Phase 2 / docs-only
ARCH-03 (choiceGuid descent), ARCH-05 (extract WinForms host bridge), A11Y-05 (IME), A11Y-06 (LabeledBy), LOC-02/03, NETFX-02/03/04, TEST-02/03/05, BUILD-02/03/04, WIRE-02, SCOPE-02/03/05/06/07/08.

---

## 4. Findings by dimension

> Format per finding: **Severity** · BlocksLanding · Confidence · Disposition · `evidence`

### 4.1 Feature Parity
The kept Avalonia lexicon spine has **strong parity**. The composer (`FullEntryRegionComposer`, 3876 lines) dispatches every legacy field type with LCModel-bound write-back through a fenced session; browse columns/sort/filter/in-cell-edit/bulk/CSV match; context-menus route through the real xCore `ChoiceGroup` machinery. One genuine behavioral gap; everything else is implemented or a documented deferral.

- **FP-01** · medium · no · high · *split-to-follow-up* — Morph-type **stem↔affix conversion is rejected** in the Avalonia detail pane (`FullEntryRegionComposer.cs:1309-1324`); legacy performs it behind a data-loss prompt (`MorphTypeAtomicLauncher.cs:156-262`). Data never corrupted (rejected, not mis-written), but a legacy capability is unavailable. The in-code comment already names the missing "class-conversion lane."
- *Verified NON-gaps (intentional deferrals, do not "fix"):* MSA/inflection "Specify Details" wiring; chooser inline "Add new item" (all 95 lexeme chooserLinks are `type="goto"` — legacy had no inline-create here either); virtual back-ref vectors read-only (D3 follow-up); cross-platform audio record; FwStylesDlg management (style *apply* ships); Graphite shaping; ORC/unsupported-run read-only (lossless safety).

### 4.2 Architecture Soundness
Core seam architecture is **sound and matches intent**. View layer is *compile-enforced* LCModel-free; write-back isolated in xWorks; caching is choice-safe where it matters.

- **ARCH-01** · low · no · high · *intentional* — View layer is **compile-enforced LCModel-free** (`FwAvalonia.csproj` references zero LCModel assemblies). Strongest part of the design. *Preserve: reject any PR adding an LCModel ref to `FwAvalonia.csproj`.*
- **ARCH-02** · medium · no · high · *docs-only* — Documented seams `IValidationService`/`IUndoRedoCoordinator`/`IHostSurface` (`seam-catalog.md` rows 25/26/33, §8–9) **do not exist as abstractions**. Real validation is a `virtual Validate()` over live LCModel (`RegionEditContextBase.cs:101-128`); undo is hardwired to WinForms `Form.Deactivate` (`RegionEditContextHolder.cs:121-179`). Works, but docs overstate the abstraction. **Update the catalog to the real shape.**
- **ARCH-03** · medium · no · high · *defer-phase-2* — `choiceGuid` is threaded only for the **root** object, never through descent (`FullEntryRegionComposer.cs:146-148` vs `:521-522`, `:3529-3531`). Dormant today (only `LexEntry` wired, `layoutChoiceField=null`); becomes a silent-wrong-layout bug when a choice-driven tool (Notebook/Lists) with type-selected children is wired.
- **ARCH-04** · low · no · high · *split-to-follow-up* — `_itemMenuBindings` cache keyed `(ClassId, LayoutName)` **without choiceGuid** (`:349-350`, `:3328`, `:3344`). Consistent today (descent compiles with `choiceGuid=null`); becomes a wrong-menu bug the instant ARCH-03 is fixed. **Add a guard comment now; fix the key with ARCH-03.**
- **ARCH-05** · medium · no · high · *defer-phase-2* — WinForms-free-view boundary is **convention-only** (whole `FwAvalonia.csproj` references `System.Windows.Forms`; real use confined to 3 host-bridge files). Phase 2: extract `FwAvalonia.WinFormsHost` so retirement is a project-ref deletion, not a file audit.
- **ARCH-06** · low · no · high · *intentional* — Edit-session lifecycle/disposal/undo re-entrancy **all handled correctly** (`RegionEditContextHolder.Replace/Settle`, `RegionEditContextBase.Stage`, `OnDoingUndoOrRedo`, `TearDownAvaloniaSurface`). **Confirms the prior "missing rollback" finding was a false positive.**
- **ARCH-07** · low · no · high · *intentional* — Process-wide compiled-model cache is **correct**: no project-specific state baked in (custom fields expanded at walk time from live MDC; overrides applied as a pure out-transform).

### 4.3 Avalonia Best Practices
High-quality MVVM kit: every XAML view sets `x:DataType`, compiled bindings on by default, spacing/borders tokenized through one `DialogTheme.axaml`, clean OK/Cancel/Help/dispose contract. Findings minor.

- **AV-01** · low · no · high · *split-to-follow-up* — VMs create owned `IDisposable` field controls (`FwMultiWsTextField`) but aren't `IDisposable` themselves; host disposes VM only `as IDisposable` (`InsertEntryDialogViewModel.cs:94-97`, `AvaloniaDialogHost.cs:128-132`). Not a leak in practice (modal graph collected together), but violates deterministic-disposal expectation.
- **AV-02** · low · no · high · *split-to-follow-up* — Hardcoded `Foreground="Red"` / warning `#FFC07000` across 7 dialog views instead of theme tokens. Add `DialogErrorBrush`/`DialogWarningBrush` to `DialogTheme.axaml`.
- **AV-03** · medium · no · high · *split-to-follow-up* — `PicturePropertiesDialogView.cs` is **pure-C#, hand-builds control tree + manual two-way text flow** (`:88-103`), deviating from the XAML+compiled-binding kit. *(Note: PictureProperties is a back-out dialog — may be moot if removed.)*
- **AV-04** · low · no · medium · *split-to-follow-up* — ConfigureColumns lists render via `ToString()` not an item template (`ConfigureColumnsDialogView.axaml:30-54`); no per-row `AutomationProperties.Name`. *(Also a back-out dialog.)*
- **AV-05** · low · no · medium · *defer-phase-2* — Untyped `#Root.DataContext.IsMultiSelect` hop in Chooser row templates (`ChooserDialogView.axaml:70,97`) falls back to reflection (not compiled). Functionally correct.
- **AV-06** · low · no · high · *docs-only* — Code-behind event subscriptions without explicit unsubscribe **reviewed and confirmed NOT leaks** (publisher+subscriber share the short-lived modal graph). Revisit only if a view becomes long-lived/non-modal.

### 4.4 Test Coverage & Parity Evidence
Kept spine is **genuinely well-tested**: write-back, field-type composers, layout-choiceGuid all have real-LCModel commit + re-project + atomic-undo tests. No `[Explicit]`/`[Ignore]` hiding gaps; no tautological asserts; explicit anti-tautology guards exist. Counts reconcile (~841 FwAvaloniaTests + 311 DialogsTests). Residuals are evidentiary.

- **TEST-01** · medium · no · high · *split-to-follow-up* — Migration-doc visual **`-after` baselines entirely missing**: 53 `*-before.png` committed, **0 `*-after.png`** (skill defines the pair as the human-facing parity summary). Render via existing headless `DialogSnapshot.Capture` and commit, or record per-doc deferral.
- **TEST-02** · low · no · high · *docs-only* — Test-lane visual snapshots are ephemeral (`Output/Snapshots`, gitignored); only `AssertNoCrowding` tripwire, no pixel diff. By design (semantic snapshot is authoritative).
- **TEST-03** · low · no · medium · *docs-only* — Semantic snapshots assert hand-authored expectations, not captured legacy output. Meaningful for composition correctness; legacy fidelity rests on TEST-01 + human gate.
- **TEST-04** · medium · no · high · *split-to-follow-up* — Product host-switch verified by **private-field state** (`RecordEditViewSwitchTests.cs:73-92`) via a real PropertyTable broadcast (good), but no desktop-UIA test exercises the realized RecordEditView swap inside FLEx — only the standalone preview host.
- **TEST-05** · low · no · medium · *defer-phase-2* — `HoverRevealTests.cs:121-131` uses bounded wall-clock polling (`Thread.Sleep(20)` ≤2000ms). Only such pattern; prefer virtual-time clock if CI flake appears.
- **TEST-06** · low · no · high · *docs-only* — **Positive:** spine coverage + can-fail signals solid (real-cache write-back, atomic-undo, negative cases, clean headless/desktop lane separation).
- **TEST-07** · medium · **YES** · high · *defer-phase-2* — **§19h.4 human-tester burn-down is the hard, un-automatable landing gate.** Reinforced by TEST-01/04.

### 4.5 UI Wiring & Host Switching
Well-architected: single resolution point (`LexicalEditSurfaceSelectionService.Decide`), default Legacy end-to-end (persistence off), explicit non-silent fallback, enforced active-host contract. The open checklist box resolves favorably by inspection.

- **WIRE-01** · low · no · high · *split-to-follow-up* — RecordEditView **flip-to-legacy leaves Avalonia listeners attached** (dormant + guarded) until Dispose (`RecordEditView.cs:212,376-389`; `AvaloniaRegionRefreshController.cs:69/200`). No double-fire/data hazard (all side effects neutralized), but asymmetric vs `RecordBrowseView` which tears down fully on flip. **Mirror RecordBrowseView's teardown on the swap + add a switch test.**
- **WIRE-02** · low · no · high · *docs-only* — "Symmetric subscribe/unsubscribe" open box is **closed by inspection** (colleague Add/Remove symmetric; hidden DataTree rides parent registration, re-queried per dispatch, gated on `m_legacySurfaceInitialized`). Add a dispose-routing test + a comment that `m_legacySurfaceInitialized` is intentionally sticky, then close the box.
- **WIRE-03** · low · no · high · *docs-only* — **Positive:** default-safety verified; `UIMode` reaches every decision point; no hardcoded host paths; Avalonia surface never instantiated under Legacy.
- **WIRE-04** · low · no · medium · *docs-only* — **Positive:** preview-vs-product boundary clean; product composes typed `LexicalEditRegionModel`, degrades gracefully on composition failure; browse opt-in correctly stricter (allowlist) than edit.

### 4.6 Localization
Very good shape: chrome strings route through `FwAvaloniaDialogsStrings`/`FwAvaloniaStrings` (L10NSharp `GetDynamicString` + English-seed fallback); field labels carry `LocalizationKey` in the IR, resolved via StringTable at compose time. One genuine hardcoded item.

- **LOC-01** · medium · no · high · *fix-now* — Hardcoded `"Delete"`/`"Insert before"` context-menu labels (`RuleFormulaRegionEditor.cs:172,174`) — the only hardcoded translatable text found. Route through `FwAvaloniaStrings`.
- **LOC-02** · low · no · high · *defer-phase-2* — `"{0} item(s)"` baked-English pluralization (`FwAvaloniaStrings.cs:343`); parity-faithful with legacy `XMLViewsStrings`. Acceptable.
- **LOC-03** · low · no · medium · *split-to-follow-up* — New `.resx` files use `ks`-prefixed names that **don't match the runtime XLIFF ids** the accessor calls — benign dead-weight that could mislead maintainers. Remove or mark non-runtime.

### 4.7 Managed .NET FX Boundary
**No real net48↔net8 split:** all 17 changed `.csproj` target `net48` (including the FwAvalonia projects, via Avalonia 11.3.x netstandard2.0). New + modified legacy code verified C# 7.3-clean. Seam marshals to UI thread, disposes deterministically; no global COM registration introduced. *(Build not run — reasoned from diff.)*

- **NETFX-01** · medium · no · high · *docs-only* — `LangVersion=latest` on net48 projects lets net8-only features (records/`init`/`Index`/`Range`/DIMs/`IAsyncEnumerable`) **compile past review** and fail only at build/runtime. Current code clean. **Pin/document the language version.**
- **NETFX-02** · low · no · medium · *defer-phase-2* — `AvaloniaRegionHostControl` ctor calls `EnsureInitialized()` without a UI-thread/STA assertion (`FwAvaloniaRuntime.cs:39-54`). Safe today (WinForms ctors are STA); mirror the dialog host's guard.
- **NETFX-03** · low · no · medium · *fix-now (run build, no code change)* — Compile-only Avalonia ref + `NoWarn AVA2001` in legacy projects relies on runtime assemblies shipping from the FwAvalonia sibling into shared Output. Reasonable; `build.ps1` is the only verifier — run the full traversal.
- **NETFX-04** · low · no · high · *docs-only* — Dev-harness `Resolve-FieldWorksDevRegistry.ps1` writes HKCU FieldWorks paths — dev tooling under `.claude/skills/`, not product/build path. No registration-free-COM violation. Keep it out of build targets.
- **NETFX-05** · low · no · high · *docs-only* — **Positive:** uniform net48 NUnit test discovery; new projects in sln + traversal; test-only headless `[SetUpFixture]` keeps the production DLL Headless-free.

### 4.8 Accessibility & UIA2 Parity
Strong automation-identity discipline (stable nonlocalized `AutomationId` + localized `Name`, enforced by `OwnedControlAutomationConventionTests`; real custom browse peers; solid headless RTL/bidi coverage). Serious gaps are in **keyboard parity and desktop verification**.

- **A11Y-04** · high · no · medium · *split-to-follow-up* — **WinForms-hosted dialog UIA bridge has no desktop automation coverage.** Production dialogs show via `WinFormsAvaloniaControlHost`; the only realized-window UIA test runs against a *native* Avalonia window (`PreviewHostUiaTests.cs`). Embedded-in-WinForms UIA bridging is known-fragile; stable AutomationIds are necessary but unverified on the realized path. **Add one desktop UIA test asserting a known AutomationId + InvokePattern via `AutomationElement.FromHandle`.**
- **A11Y-01** · medium · no · high · *split-to-follow-up* — **No mnemonics/access keys** anywhere (localized strings contain no `_`); legacy `&OK`/`&Cancel`/field mnemonics lost. Keyboard + screen-reader regression.
- **A11Y-02** · medium · no · medium · *split-to-follow-up* — **Dialog Tab order inverted**: button strip declared first → Tab reaches OK/Cancel before fields; no `TabIndex`/`TabNavigation` anywhere. Reorder children or set TabIndex.
- **A11Y-03** · medium · no · high · *split-to-follow-up* — **No initial focus** set on dialog open (`AvaloniaDialogHost.ShowModal` only restores prior focus on close). Legacy focused the first input.
- **A11Y-05** · medium · no · high · *defer-phase-2* — **IME composition / complex-script input** is an explicitly documented open gap, unautomated (Khmer/CJK/Indic). Keep as a tracked manual deferral; don't checkbox "done" on headless RTL evidence.
- **A11Y-06** · low · no · medium · *docs-only* — Plain dialog label↔editor pairs lack `AutomationProperties.LabeledBy`. Largely mitigated (owned fields self-name via `SetName`); residual is the TextBlock-over-host dialog pattern.

### 4.9 Build / CI / Packaging & Hygiene
Wiring sound: all six FwAvalonia projects in `FieldWorks.sln` with full config-platform mappings, build via the `FieldWorks.proj` `Src/**` traversal, run on pinned `windows-2022`. Installer auto-harvests product DLLs; preview-host exe correctly excluded; RenderBenchmark exclusion intact.

- **BUILD-01** · low · no · high · *fix-now* — **Committed NuGet build artifacts** `Obj/FwBuildTasks/FwBuildTasks.csproj.nuget.g.{props,targets}` (net-new; not on main; `.props` embeds a *different* worktree's absolute path → staged by accident). `git rm --cached` + add `Obj/` to `.gitignore`.
- **BUILD-02** · medium · no · high · *split-to-follow-up* — **13-dialog back-out is interwoven with kept code**, not file-deletable: ConfigureColumns→`RecordBrowseView.cs` (a KEPT file), DeleteConfirmation→shared `LexReferenceMultiSlice`. Revert launcher + call site as one unit, rebuild after each. (Blocks the clean-revert *plan*, not compilation.)
- **BUILD-03** · low · no · medium · *defer-phase-2* — Avalonia test DLLs + Skia/NUnit land in harvested Output and ship in the installer. **Pre-existing repo pattern** (every `*Tests.dll` already does), not a regression.
- **BUILD-04** · low · no · medium · *defer-phase-2* — Orphaned stale `FLExInstaller/wix6/CustomComponents.wxi` pins encoding-converters `0.2.28` off the active include path. **Pre-existing.** (The branch's new EC version-token chain is verified sound.)
- **BUILD-05** · low · no · high · *docs-only* — 173 PNGs confirmed intentional: 150 migration-doc screenshots + 23 modified render-parity baselines. No stray binaries.
- **BUILD-06** · low · no · high · *docs-only* — **Positive:** CI runs Avalonia net48 headless tests; RenderBenchmark exclusion not regressed; runner pin consistent.

### 4.10 PR Scope, Landability & Docs
The spine is landable; plan-of-record matches code with two refinements. (PR decomposition in Section 2.)

- **SCOPE-09** · high · **YES** · high · *fix-now* — **TRUE landing blockers:** §19h.4 human-tester gate (tasks.md:413) + un-run full `build.ps1`+`test.ps1` traversal & `CI: Full local check` (tasks.md:302, :147). Independent of all code-scope decisions.
  - **UPDATE 2026-06-23:** Full `build.ps1 -BuildTests -RunTests` (Debug/x64) executed. **Build: 0 errors.** All spine + touched-area managed tests **green**: `FwAvaloniaTests` 841/841, `FwAvaloniaDialogsTests` 311/311, `xWorksTests` 1686/1688 (2 skipped), `LexTextControlsTests` 255/258, `DetailControlsTests` 110/111, `XMLViewsTests` 113/113, `FwCoreDlgsTests` 347/352. **The only failures (38) are the entire `RenderBenchmark` suite** (`RootSiteTests/RenderBenchmark*`, `RenderVerification/*` — `RenderHarness`/`RunBenchmark`/`RenderSnapshotVerifier`/`RenderEnvironmentValidator`/`RenderDiagnosticsToggle`), all the same `InvalidOperationException: …'.fieldworks-real-data-test-project' sentinel is missing`.
  - **ROOT CAUSE (verified, definitive):** these `[Category("RenderBenchmark")]` tests derive from `RealDataTestsBase`, whose `TestSetup` calls `DeleteProjectDirectory` *before* creating the project (`RealDataTestsBase.cs:43`); that method **throws** if a pre-existing `integration_test_data` dir lacks the sentinel (`:303-312`). A **stale, sentinel-less `C:\Users\johnm\Documents\repos\FieldWorks\DistFiles\Projects\integration_test_data`** (dated Jun 10, in the *main* repo's shared Projects dir) trips the guard → setup throws for all 38. This is a **local environment artifact, not a code or CI defect.**
  - **CI disposition (definitive):** a clean CI runner has **no** pre-existing `integration_test_data`, so `DeleteProjectDirectory` returns early (`:297`) and the tests **self-provision a sentinel'd project and pass + clean up**. So even though `CI.yml`'s filter does **not** exclude `RenderBenchmark` (only the installer-CD lanes do, BUILD-06), CI is **not** inherently red from these. **No `CI.yml` change is warranted** — excluding the category would mask real render coverage.
  - **Local fix to get a fully green local run:** remove the stale `…\FieldWorks\DistFiles\Projects\integration_test_data` (it lacks the sentinel by definition). *Left to the user:* the safety guard exists precisely to require human judgment before deleting a real-looking project dir in the main repo.
  - **Net:** the build-half of the landing gate is satisfied (0 errors, spine green); the 38 failures are a local stale-dir artifact with a known one-line fix; the human-tester gate (19h.4) remains the real blocker.
- **SCOPE-05** · high · no · high · *defer-phase-2* — **lexical-edit §20 (whole-tool generalization, 34 open boxes, "DO NOT DEFER") is the real scope over-reach** — contradicts the Path-2 decision. Lift §20 into `avalonia-end-game` (Phase 2); the landed-but-latent 20.1.3 can stay.
- **SCOPE-01** · low · no · high · *split-to-follow-up* — Interlinear + rule-formula split is **clean**; costs 2 file edits (drop registry lines + census entries). Both OpenSpec changes 100% closed with honest `[~] DEFERRED` markers.
- **SCOPE-02** · medium · no · high · *defer-phase-2* — The 6 "wired" back-out dialogs are **all `UIMode`-gated with a legacy `else`** → no default regression; back-out is review-scope reduction, not correctness.
- **SCOPE-03** · medium · no · high · *docs-only* — **Shared dialog infra types must survive back-out** (`FwMsaGroupBox`/`FwFeatureStructureEditor`/`FwSandboxMsa`/`InMemoryRegionEditContext` used by KEPT InsertEntryDialog). Scope each revert to the named dialog's files only.
- **SCOPE-04** · low · no · high · *split-to-follow-up* — Verified the 7 unwired dialogs are caller-free → clean delete; branch's own §19i self-audit records prior overclaims as corrections (good hygiene).
- **SCOPE-06** · low · no · high · *docs-only* — 3 superseded OpenSpec changes carry banners but aren't archived (`datatree-model-view-separation`, `fieldworks-avalonia-shell-migration`, `graphite-transition-support`). Keep `shared-editable-virtualized-table` (active Phase-1 dependency).
- **SCOPE-07** · medium · no · high · *docs-only* — Back-out docs are stubs: placeholder `LT-XXXXX`, uneven PNGs. Deliverable of the back-out PRs, not the spine PR.
- **SCOPE-08** · low · no · medium · *docs-only* — `INVENTORY.md` labelled "auto-generated" with no discoverable generator. Commit the generator or relabel "manually maintained."
- **SCOPE-10** · low · no · medium · *docs-only* — **Positive:** checked-task evidence language is honest; §19i self-correction lane is a trust signal (contradicts the standing "stale checkbox" warning for this branch).

---

## 5. Cross-cutting observations

1. **The Legacy gate is the safety net.** Repeated independently across wiring, scope, parity, and build dimensions: default users hit zero Avalonia code. This converts almost every finding from "blocker" to "polish/follow-up."
2. **Two latent bugs are coupled and should be fixed together** (ARCH-03 + ARCH-04): both dormant only because descent compiles with `choiceGuid=null`. Fixing one without the other turns the other into an active bug. Fix when wiring the first choice-driven tool.
3. **Keyboard accessibility is the weakest real area** (A11Y-01/02/03): mnemonics, tab order, initial focus — all genuine regressions vs legacy WinForms, all behind the gate. Worth one focused follow-up PR.
4. **Two prior-review claims were re-checked and overturned:** the "missing rollback" false positive (ARCH-06) and the "stale checkbox" assumption (SCOPE-10 — this branch's checkboxes are honest). Confirms the advisor's verify-gate discipline.

---

## 6. Work-off log (2026-06-23)

### Tier A — DONE & build-verified (the full `build.ps1 -BuildTests -RunTests` at SCOPE-09 included all of these; 0 build errors, all spine tests green)
- **BUILD-01** ✅ — `git rm --cached` the two `Obj/FwBuildTasks/*.nuget.g.*` artifacts; confirmed `.gitignore`'s `[Oo]bj/` now covers them.
- **LOC-01** ✅ — added `FwAvaloniaStrings.RuleCellDelete`/`RuleCellInsertBefore` accessors (seeded "Delete"/"Insert before"); `RuleFormulaRegionEditor.cs:172,174` now uses them.
- **ARCH-04** ✅ — added the choice-unsafe guard comment on `_itemMenuBindings` (`FullEntryRegionComposer.cs`), tying the fix to ARCH-03.
- **NETFX-01** ✅ — documented the net48 BCL constraint at `LangVersion` across all 6 FwAvalonia `.csproj` (did **not** down-pin: would risk breaking the build on C# 8 pure-syntax already in use; disposition was docs-only).
- **ARCH-02** ✅ — reconciled `seam-catalog.md` rows 25/26/33 + architecture-patterns §8/§9 with the real (thinner) `virtual Validate()` / holder-undo-guard shape; marked the three seams "planned, not extracted."
- **SCOPE-09** ✅ (build half) — full traversal run; see the UPDATE under SCOPE-09 in §4.10. Human-tester gate (19h.4) remains.

### Tier B — resolved at correct altitude (user asked for all four; outcome: 2 shipped+verified, 2 deferred with evidence)
- **A11Y-02 (tab order)** ✅ **shipped + headless-verified (native traversal proven)** — `TabIndex="1"` on the bottom button strip of the 4 kept dialog views (Chooser/Options/InsertEntry/EntryGo) so fields tab before OK/Cancel; layout unchanged. **`NativeTabTraversal_FirstStop_IsContentNotButtonStrip` exercises Avalonia's real `KeyboardNavigationHandler` and confirms the container-level TabIndex genuinely reorders traversal when content is focusable** (so InsertEntry/EntryGo tab fields-first). **Caveat (verified):** picker-driven dialogs (Chooser/Options) have NO tabbable content — their `FwOptionPicker` is `Focusable=false` and key-driven — so Tab there begins on the button strip regardless; that is inherent to the picker design and pre-existing, neither introduced nor removable by this change. All 30 existing Chooser tests stayed green.
- **A11Y-03 (initial focus)** ✅ **shipped + headless-verified** — added testable `AvaloniaDialogHost.FocusInitialControl` (first focusable **non-command-button** input, in effective-tab order), wired on the host's `Shown`. **Empirical finding:** the kit's owned `FwOptionPicker` is deliberately `Focusable=false` (handles keys directly, `FwOptionPicker.cs:152`), so picker-driven dialogs (Chooser/Options) have no focusable field — the helper correctly focuses **nothing** there rather than auto-focusing OK (where Enter would accept), and focuses the first text field for text-first dialogs (InsertEntry/EntryGo). Tests: `FocusInitialControl_PrefersTextInput_OverCommandButton`, `FocusInitialControl_PickerDialog_DoesNotFocusOkButton` — green.
- **A11Y-01 (mnemonics)** ⏸ **deferred (evidence)** — the kit's OK/Cancel/Help reuse the **shared `Common.OK/Cancel/Help`** localization ids (Palaso/Chorus), which WinForms consumes with `&`-mnemonics; adding Avalonia `_`-mnemonics to those shared seeds would fork/contaminate the shared catalog. Mnemonics need a dialog-local string lane or per-button `RecognizesAccessKey`, not a seed edit. Enter/Escape already work via `IsDefault`/`IsCancel`. → follow-up PR.
- **WIRE-01 (teardown symmetry)** ⏸ **deferred (evidence — would regress the spine)** — `TearDownAvaloniaSurface()` disposes `m_avaloniaEntryForm` but does **not** null it (only `Dispose()` does, `RecordEditView.cs:218`); `EnsureAvaloniaSurfaceActive()` guards `if (m_avaloniaEntryForm == null)` (`:1417`) so a flip New→Legacy→New would `.Show()` a **disposed** control (same for the refresh controller, disposed-not-nulled, guarded on null). Calling teardown on flip today turns a dormant low-severity non-bug into a crash. A correct fix needs a *partial* teardown (detach listeners + dispose-and-null controller, keep/recreate host) + a flip-back test — disproportionate for a finding the review itself rated low / "no data hazard." → follow-up PR.
- **FP-01 (morph-type stem↔affix conversion)** ⏸ **deferred (architecture + data-risk; documented design)** — the composer is deliberately UI-free and the morph-type `OptionSetter` returns `bool` inside a fenced session (`FullEntryRegionComposer.cs:1308-1330`); it has **no mechanism to raise the legacy data-loss confirmation** (`MorphTypeAtomicLauncher.CheckForAffixDataLoss`) nor to perform the `MoStemAllomorph`↔`MoAffixAllomorph` **object-class swap** (`SwapValues`). The current guard *rejects* boundary-crossing assignments precisely to avoid the model-invalid combination — doing the conversion wrong corrupts data. The real fix = (1) a confirmation seam threaded into the composer/host (like the existing `ILegacyDialogLauncher`) + (2) the class-swap preserving common fields and fixing the owning sequence, with an atomic-undo test. This is the "class-conversion lane" the in-code comment already defers. → its own OpenSpec/JIRA follow-up (matches the review's `split-to-follow-up`).
- **TEST-01 (after-image baselines)** ⏸ **scoped to a precise follow-up** — the capture harness exists (`DialogSnapshot.Capture`; the dialog tests already invoke it into the gitignored `Output/Snapshots`). The remaining work is **per-surface**, not batch: each doc must be paired with the *correct* Avalonia surface (e.g. `dialogs/chooser.md` documents legacy `FwChooserDlg`, a different surface than the reusable `ChooserDialogView`; it already references a not-yet-generated `fw-chooser-after.png`). A mismatched after-image is worse than none. Recipe for the follow-up: for each kept/deferred doc, construct its mapped Avalonia view with the seeded data, `DialogSnapshot.Capture`, visually verify, copy the PNG to that doc's `images/<name>-after.png`, commit. Scope to the kept spine + the surfaces whose JIRA is being picked up (per the "PNG capture is per-pickup" plan), not all 76. → follow-up PR(s).

### Net Tier-B outcome
**Shipped + verified now:** A11Y-02, A11Y-03 (3 new green headless tests; kept-spine dialogs only; no regressions). **Deferred with evidence to follow-up PRs:** A11Y-01 (shared-catalog entanglement), WIRE-01 (flip-back recreation is broken — would regress), FP-01 (UI-free composer can't host the confirmation + class-swap; data-risk), TEST-01 (per-surface mapping; harness exists). None block landing; all are behind the `UIMode=Legacy` gate.

**Process gates (only true blockers, human-owned):** §19h.4 tester burn-down; full CI green (build half satisfied locally).
