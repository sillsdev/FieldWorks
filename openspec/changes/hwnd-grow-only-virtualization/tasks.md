## Phase 0: Measurement Baseline (1 week)

Establish exact HWND costs and automated regression detection before any behavioral changes.

- [x] 0.1 Add `GetGuiResources()` P/Invoke wrapper to count USER handles per process. File: `Src/Common/Controls/DetailControls/HwndDiagnostics.cs`. [Managed C#, ~30 min]
- [x] 0.2 Add HWND counter integration to `DataTreeRenderHarness` — log HWND count before/after `ShowObject()`. File: `Src/Common/RenderVerification/DataTreeRenderHarness.cs`. [Managed C#, ~30 min]
- [x] 0.3 Add `Slice.Install()` counter — increment a static or DataTree-level counter each time a slice creates HWNDs. File: `Src/Common/Controls/DetailControls/Slice.cs`, `DataTree.cs`. [Managed C#, ~30 min]
- [x] 0.4 Create `DataTreeHwndCountTest` — NUnit test that loads a known entry (pathological 253-slice or equivalent) and asserts HWND count stays below threshold. File: `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeHwndCountTest.cs`. [Managed C#, ~1 hr]
- [ ] 0.5 Record baseline numbers in `research/baseline-measurements.md` for: Lexicon Edit (253-slice entry), Grammar Edit (typical category), Lists Edit (typical item). [Documentation, ~1 hr]
- [x] 0.6 Run `.\build.ps1 -BuildTests` and `.\test.ps1 -TestProject DetailControlsTests` to verify baseline infrastructure. [Verification]
	- Render snapshots are immutable for this change; any mismatch is a regression to fix in code.

## Phase 1: Defer HWND Creation (2-3 weeks)

Extend the existing `BecomeRealInPlace` pattern so no slice creates any HWND until it scrolls into view.

- [x] 1.1 Add `EnsureHwndCreated()` method to `Slice` — creates SplitContainer, SliceTreeNode, adds to `DataTree.Controls`, subscribes HWND events. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
- [x] 1.2 Refactor `Slice` constructor — move SplitContainer creation from constructor to `EnsureHwndCreated()`. Store configuration (dock style, splitter distance hints) in plain fields. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
- [x] 1.3 Refactor `Slice.Install()` — split into logical install (add to `Slices` list, set label/indent/weight/expansion) and defer physical install (SplitContainer/SliceTreeNode creation, `Controls.Add`). File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~3 hrs]
- [x] 1.4 Add null guards to `Label`, `Indent`, `Weight`, `Expansion` property setters for `m_treeNode` and `SplitCont` that may not yet exist. Store values in backing fields. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~1 hr]
- [x] 1.5 Update `DataTree.MakeSliceVisible()` — call `EnsureHwndCreated()` on the target slice (and high-water-mark predecessors) before setting `Visible`. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
- [x] 1.6 Update `DataTree.HandleLayout1()` — only call `Handle`-forcing and `MakeSliceVisible` for slices where `fSliceIsVisible` is true. Non-visible slices skip HWND creation entirely. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~2 hrs]
- [x] 1.7 Guard `CurrentSlice` setter — wrap `Validate()` call with `if (m_currentSlice.IsHandleCreated)`. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~30 min]
	- Incremental scaffold landed: `DeferSliceHwndCreationEnabled` flag, `Slice.EnsureHwndCreated()`, and visibility/layout call sites now branch through deferred hooks when flag is enabled (default remains eager/off).
- [ ] 1.8 Audit all `SplitCont` / `TreeNode` / `Control` accesses across all 73 slice subclasses — add null guards or `EnsureHwndCreated()` calls where needed. Files: multiple across `Src/Common/Controls/DetailControls/` and `Src/LexText/`. [Managed C#, ~4 hrs]
	- Progress: added deferred-install guards in `AtomicReferenceSlice`, `ReferenceVectorSlice`, `PictureSlice`, `EnumComboSlice`, `MSAReferenceComboBoxSlice`, and `ViewSlice` event/layout paths.
	- Progress: added deferred-install guards in `Src/LexText/Morphology` (`InflAffixTemplateControl`, `AnalysisInterlinearRS`, `InterlinearSlice`) for parent DataTree/slice lifecycle edge cases.
	- Progress: added deferred-safe `ContainingDataTree` guards in `BasicTypeSlices`, `SummarySlice`, `SummaryCommandControl`, `DerivMSAReferenceSlice`, `InflMSAReferenceSlice`, `GhostStringSlice`, and `AutoMenuHandler`; tightened `Slice` focus/context-menu/mouse paths for null-safe pre-install behavior.
	- Progress: added additional deferred-safe guards in `AudioVisualSlice`, `MorphTypeAtomicLauncher`, and `MultiStringSlice` cross-slice refresh/update flows.
	- Progress: added deferred-safe install/menu guards in `RuleFormulaSlice`, `MSADlgLauncherSlice`, `MsaInflectionFeatureListDlgLauncherSlice`, and `LexEntryMenuHandler` (`Src/LexText/**`) to avoid direct pre-install `ContainingDataTree` dereferences.
	- Refactor: extracted virtualization orchestration into `SliceVirtualizationCoordinator` (inside `Slice`) to separate lifecycle coordination from slice behavior without broad API changes.
- [x] 1.9 Update `ViewSlice.Install()` — move RootSite event subscriptions (`LayoutSizeChanged`, `Enter`, `SizeChanged`) to `EnsureHwndCreated()` or `BecomeRealInPlace()`. File: `Src/Common/Controls/DetailControls/ViewSlice.cs`. [Managed C#, ~1 hr]
- [ ] 1.10 Run full `DetailControlsTests` suite, render benchmark, and manual smoke test. Verify HWND count reduction matches expectations. [Verification, ~2 hrs]
	- Automated status: `./test.ps1 -TestProject DetailControlsTests` and `./test.ps1 -NoBuild -TestProject DetailControlsTests` passed after current changes (includes render/timing test coverage); `HwndVirtualization` and `RenderBaseline` targeted gates also passing.
	- Remaining: manual smoke checks in 1.T4–1.T8.

### Phase 1 Test Gates

- [x] 1.T1 NUnit: `DataTreeHwndCountTest` passes with HWND count ≤ (visible_slices × 8) for pathological entry.
- [x] 1.T2 NUnit: All existing `DetailControlsTests` pass (no regressions).
- [x] 1.T3 NUnit: Render baseline snapshots match exactly (no baseline updates allowed).
- [ ] 1.T4 Manual: Open 253-slice entry, scroll to bottom, verify all slices render correctly.
- [ ] 1.T5 Manual: Tab forward/backward through all slices — no crashes, all fields accessible.
- [ ] 1.T6 Manual: Screen reader (NVDA) can enumerate visible controls.
- [ ] 1.T7 Manual: Ctrl+End jumps to last field correctly.
- [ ] 1.T8 Manual: Find/Replace highlights a non-visible field — field becomes visible and highlighted.

## Phase 2: Deferred SplitContainer & SliceTreeNode (2 weeks)

Eliminate internal control allocation overhead for slices that haven't been scrolled into view.

- [x] 2.1 Make SliceTreeNode creation lazy — create in `EnsureHwndCreated()` instead of `Install()`. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~1 hr]
- [x] 2.2 Store slice configuration (indent level, label text, font, weight, expansion state) in plain fields on Slice, applied to controls in `EnsureHwndCreated()`. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
	- Verified in coordinator-based flow: state remains in slice backing fields (`m_indent`, `m_strLabel`, `m_weight`, `m_expansion`) and is applied during physical install (`ApplySplitAppearance`/`SetSplitPosition`) while remaining safe before HWND creation.
- [x] 2.3 Audit `ShowContextMenu` event wiring — defer to `EnsureHwndCreated()` since it's wired through `SplitCont.Panel2.Click`. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~1 hr]
	- Audit result: current context-menu invocation path is `HandleMouseDown`/`TreeNodeEventArgs` (no direct `SplitCont.Panel2.Click` dependency in `Slice`), so no additional deferral hook was required.
- [x] 2.4 Update `Slice.Dispose()` — handle case where SplitContainer/TreeNode were never created (skip event unsubscription, skip control disposal). File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~30 min]
	- Added defensive unsubscription guards for splitter and tree-node mouse events so partial/deferred materialization disposes cleanly.
- [x] 2.5 Add NUnit test: create 100 slices, verify only ~15 visible ones have non-null `SplitCont`. File: `DetailControlsTests/DataTreeHwndCountTest.cs`. [Managed C#, ~1 hr]
	- Added `ShowObject_WithDeferredCreation_NotAllSlicesArePhysicallyInstalled` in `DataTreeHwndCountTest` to assert partial physical install in deferred mode (subset has `Parent == DataTree`, `TreeNode != null`, and `IsHandleCreated`).

### Phase 2 Test Gates

- [x] 2.T1 NUnit: `DataTreeHwndCountTest` passes — deferred mode physically installs only a subset of slices after initial layout.
- [x] 2.T2 NUnit: All `DetailControlsTests` pass.
	- Verified with `./test.ps1 -NoBuild -TestProject "Src/Common/Controls/DetailControls/DetailControlsTests"` (full suite pass).
- [ ] 2.T3 Manual: Right-click context menus work on all slice types after scrolling them into view.
- [ ] 2.T4 Manual: Expand/collapse tree nodes for slices with children.

## Phase 3: Virtual Layout Model (2 weeks)

Replace WinForms `Top`/`Height` properties with virtual arrays for non-HWND'd slices.

- [x] 3.1 Add `int[] _sliceTop` and `int[] _sliceHeight` parallel arrays to DataTree. Resize on slice list changes. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
	- Added `m_sliceTop`/`m_sliceHeight` with capacity/invalidation helpers and hooked invalidation into slice-list mutation paths (`InsertSlice`, `RemoveSlice`, `RemoveDisposedSlice`, `Reset`).
- [x] 3.2 Update `HandleLayout1` — for non-HWND slices, write position to `_sliceTop[i]` instead of `tci.Top`. Use `GetDesiredHeight()` or XML-hint-based estimate for `_sliceHeight[i]`. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~3 hrs]
	- `HandleLayout1` now records `m_sliceTop[i]`/`m_sliceHeight[i]` for all iterated slices, uses cached/estimated heights as the pre-materialization estimate, and only writes `tci.Top` for physically installed slices (`Parent == DataTree`).
- [x] 3.3 Update `FindFirstPotentiallyVisibleSlice` — binary search over `_sliceTop[]` instead of `tci.Top` for non-HWND'd slices. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
	- Binary search now uses array-backed `GetSliceTop`/`GetSliceHeight` when layout arrays are valid; otherwise safely falls back to linear scan start.
- [x] 3.4 Compute `AutoScrollMinSize` from sum of `_sliceHeight[]` values. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
	- Full-layout path now derives `AutoScrollMinSize.Height` from virtual layout arrays (`GetVirtualLayoutBottom`) with fallback to the measured layout bottom.
- [x] 3.5 Add height estimation heuristics — defaults based on slice type (single-line: 20px, multi-string: 40px, sttext: 60px, summary: 30px). Source hints from XML `<slice>` configuration. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
	- Added `Slice.GetEstimatedHeightForVirtualLayout()` with XML editor hints (`multistring`, `sttext`) plus type-based defaults (`SummarySlice`, `StTextSlice`, `MultiStringSlice`, `ViewSlice`), consumed by `DataTree` for deferred/non-HWND height estimation.
- [x] 3.6 When a slice gets its HWND and actual height, update `_sliceHeight[i]` and trigger partial re-layout (invalidate from that position downward). File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~2 hrs]
	- `HandleLayout1` now tracks realized-vs-estimated height changes for visible deferred slices and invalidates from the earliest changed top downward; `MakeSliceRealAt` now syncs `m_sliceHeight[i]` to the actual realized height and invalidates from that slice downward.

### Phase 3 Test Gates

- [x] 3.T1 Automated: After scrolling/materializing all slices into view, total virtual layout height matches realized layout height (within 1px).
	- Added `DeferredLayout_AfterAllSlicesMaterialized_VirtualHeightMatchesActualHeight` in `DataTreeHwndCountTest`; verifies all slices materialize and `AutoScrollMinSize.Height` matches realized content bottom within 1px.
- [ ] 3.T2 Manual: Scrollbar thumb position accuracy — scroll to bottom of 253-slice entry, thumb is at bottom.
- [ ] 3.T3 Manual: Ctrl+End jumps to last slice correctly.
- [ ] 3.T4 Manual: Mouse wheel scrolling is smooth (no large jumps).
- [ ] 3.T5 Manual: Resize window width — all slices that need re-layout get correct heights.

## Phase 4: Future — HWND Recycling (deferred, out of scope)

Documented here for completeness. This phase is **not part of this change** due to high risk and diminishing returns.

- [ ] 4.1 Define "reclaim distance" — slices > 2 viewports away are candidates for HWND reclamation.
- [ ] 4.2 Implement selection save/restore for ViewSlice — save `VwSelLevInfo[]` before HWND destruction, restore via `MakeTextSelection` after recreation.
- [ ] 4.3 Implement state save/restore for WinForms controls (TextBox.Text, ComboBox.SelectedIndex, CheckBox.Checked).
- [ ] 4.4 Handle IME composition guard — never reclaim an HWND during active IME composition (check `ImmGetCompositionString`).
- [ ] 4.5 Handle accessor tree stability — implement UI Automation provider at DataTree level for stable accessibility regardless of HWND presence.
- [ ] 4.6 Event unsubscribe/resubscribe lifecycle for all slice types.

**Why deferred:** Phase 4 introduces the full risk surface (data loss, IME loss, screen reader instability) for only marginal benefit beyond Phase 1-3. It's only needed if users routinely scroll through ALL fields of massive entries, which is a rare usage pattern that peak at the same HWND count as today.

## Validation Checklist (All Phases)

- [x] `.\build.ps1` passes in Debug configuration.
- [x] `.\test.ps1 -TestProject DetailControlsTests` passes.
- [x] `.\test.ps1 -TestFilter RenderBaseline` passes (visual regression gate; do not update `.verified.png` files).
- [ ] `.\Build\Agent\check-and-fix-whitespace.ps1` reports clean output.
- [x] HWND count regression gate test passes.
- [ ] No new compiler warnings introduced.
