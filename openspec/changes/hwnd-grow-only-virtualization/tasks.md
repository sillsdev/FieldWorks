## Phase 0: Measurement Baseline (1 week)

Establish exact HWND costs and automated regression detection before any behavioral changes.

- [x] 0.1 Add `GetGuiResources()` P/Invoke wrapper to count USER handles per process. File: `Src/Common/Controls/DetailControls/HwndDiagnostics.cs`. [Managed C#, ~30 min]
- [x] 0.2 Add HWND counter integration to `DataTreeRenderHarness` — log HWND count before/after `ShowObject()`. File: `Src/Common/RenderVerification/DataTreeRenderHarness.cs`. [Managed C#, ~30 min]
- [x] 0.3 Add `Slice.Install()` counter — increment a static or DataTree-level counter each time a slice creates HWNDs. File: `Src/Common/Controls/DetailControls/Slice.cs`, `DataTree.cs`. [Managed C#, ~30 min]
- [x] 0.4 Create `DataTreeHwndCountTest` — NUnit test that loads a known entry (pathological 253-slice or equivalent) and asserts HWND count stays below threshold. File: `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeHwndCountTest.cs`. [Managed C#, ~1 hr]
- [ ] 0.5 Record baseline numbers in `research/baseline-measurements.md` for: Lexicon Edit (253-slice entry), Grammar Edit (typical category), Lists Edit (typical item). [Documentation, ~1 hr]
- [x] 0.6 Run `.\build.ps1 -BuildTests` and `.\test.ps1 -TestProject DetailControlsTests` to verify baseline infrastructure. [Verification]
	- Updated render baselines (`DataTreeRender_Deep`, `DataTreeRender_Expanded`, `DataTreeRender_Extreme`) and re-ran full `DetailControlsTests` successfully.

## Phase 1: Defer HWND Creation (2-3 weeks)

Extend the existing `BecomeRealInPlace` pattern so no slice creates any HWND until it scrolls into view.

- [ ] 1.1 Add `EnsureHwndCreated()` method to `Slice` — creates SplitContainer, SliceTreeNode, adds to `DataTree.Controls`, subscribes HWND events. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
- [ ] 1.2 Refactor `Slice` constructor — move SplitContainer creation from constructor to `EnsureHwndCreated()`. Store configuration (dock style, splitter distance hints) in plain fields. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
- [ ] 1.3 Refactor `Slice.Install()` — split into logical install (add to `Slices` list, set label/indent/weight/expansion) and defer physical install (SplitContainer/SliceTreeNode creation, `Controls.Add`). File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~3 hrs]
- [ ] 1.4 Add null guards to `Label`, `Indent`, `Weight`, `Expansion` property setters for `m_treeNode` and `SplitCont` that may not yet exist. Store values in backing fields. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~1 hr]
- [ ] 1.5 Update `DataTree.MakeSliceVisible()` — call `EnsureHwndCreated()` on the target slice (and high-water-mark predecessors) before setting `Visible`. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
- [ ] 1.6 Update `DataTree.HandleLayout1()` — only call `Handle`-forcing and `MakeSliceVisible` for slices where `fSliceIsVisible` is true. Non-visible slices skip HWND creation entirely. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~2 hrs]
- [x] 1.7 Guard `CurrentSlice` setter — wrap `Validate()` call with `if (m_currentSlice.IsHandleCreated)`. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~30 min]
	- Incremental scaffold landed: `DeferSliceHwndCreationEnabled` flag, `Slice.EnsureHwndCreated()`, and visibility/layout call sites now branch through deferred hooks when flag is enabled (default remains eager/off).
- [ ] 1.8 Audit all `SplitCont` / `TreeNode` / `Control` accesses across all 73 slice subclasses — add null guards or `EnsureHwndCreated()` calls where needed. Files: multiple across `Src/Common/Controls/DetailControls/` and `Src/LexText/`. [Managed C#, ~4 hrs]
- [ ] 1.9 Update `ViewSlice.Install()` — move RootSite event subscriptions (`LayoutSizeChanged`, `Enter`, `SizeChanged`) to `EnsureHwndCreated()` or `BecomeRealInPlace()`. File: `Src/Common/Controls/DetailControls/ViewSlice.cs`. [Managed C#, ~1 hr]
- [ ] 1.10 Run full `DetailControlsTests` suite, render benchmark, and manual smoke test. Verify HWND count reduction matches expectations. [Verification, ~2 hrs]

### Phase 1 Test Gates

- [ ] 1.T1 NUnit: `DataTreeHwndCountTest` passes with HWND count ≤ (visible_slices × 8) for pathological entry.
- [ ] 1.T2 NUnit: All existing `DetailControlsTests` pass (no regressions).
- [ ] 1.T3 NUnit: Render baseline snapshots match (no visual regressions).
- [ ] 1.T4 Manual: Open 253-slice entry, scroll to bottom, verify all slices render correctly.
- [ ] 1.T5 Manual: Tab forward/backward through all slices — no crashes, all fields accessible.
- [ ] 1.T6 Manual: Screen reader (NVDA) can enumerate visible controls.
- [ ] 1.T7 Manual: Ctrl+End jumps to last field correctly.
- [ ] 1.T8 Manual: Find/Replace highlights a non-visible field — field becomes visible and highlighted.

## Phase 2: Deferred SplitContainer & SliceTreeNode (2 weeks)

Eliminate internal control allocation overhead for slices that haven't been scrolled into view.

- [ ] 2.1 Make SliceTreeNode creation lazy — create in `EnsureHwndCreated()` instead of `Install()`. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~1 hr]
- [ ] 2.2 Store slice configuration (indent level, label text, font, weight, expansion state) in plain fields on Slice, applied to controls in `EnsureHwndCreated()`. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
- [ ] 2.3 Audit `ShowContextMenu` event wiring — defer to `EnsureHwndCreated()` since it's wired through `SplitCont.Panel2.Click`. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~1 hr]
- [ ] 2.4 Update `Slice.Dispose()` — handle case where SplitContainer/TreeNode were never created (skip event unsubscription, skip control disposal). File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~30 min]
- [ ] 2.5 Add NUnit test: create 100 slices, verify only ~15 visible ones have non-null `SplitCont`. File: `DetailControlsTests/DataTreeHwndCountTest.cs`. [Managed C#, ~1 hr]

### Phase 2 Test Gates

- [ ] 2.T1 NUnit: `DataTreeHwndCountTest` passes — non-visible slices have null `SplitCont`.
- [ ] 2.T2 NUnit: All `DetailControlsTests` pass.
- [ ] 2.T3 Manual: Right-click context menus work on all slice types after scrolling them into view.
- [ ] 2.T4 Manual: Expand/collapse tree nodes for slices with children.

## Phase 3: Virtual Layout Model (2 weeks)

Replace WinForms `Top`/`Height` properties with virtual arrays for non-HWND'd slices.

- [ ] 3.1 Add `int[] _sliceTop` and `int[] _sliceHeight` parallel arrays to DataTree. Resize on slice list changes. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
- [ ] 3.2 Update `HandleLayout1` — for non-HWND slices, write position to `_sliceTop[i]` instead of `tci.Top`. Use `GetDesiredHeight()` or XML-hint-based estimate for `_sliceHeight[i]`. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~3 hrs]
- [ ] 3.3 Update `FindFirstPotentiallyVisibleSlice` — binary search over `_sliceTop[]` instead of `tci.Top` for non-HWND'd slices. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
- [ ] 3.4 Compute `AutoScrollMinSize` from sum of `_sliceHeight[]` values. File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~1 hr]
- [ ] 3.5 Add height estimation heuristics — defaults based on slice type (single-line: 20px, multi-string: 40px, sttext: 60px, summary: 30px). Source hints from XML `<slice>` configuration. File: `Src/Common/Controls/DetailControls/Slice.cs`. [Managed C#, ~2 hrs]
- [ ] 3.6 When a slice gets its HWND and actual height, update `_sliceHeight[i]` and trigger partial re-layout (invalidate from that position downward). File: `Src/Common/Controls/DetailControls/DataTree.cs`. [Managed C#, ~2 hrs]

### Phase 3 Test Gates

- [ ] 3.T1 Automated: After scrolling all slices into view, total layout height matches sum of actual heights (within 1px).
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

- [ ] `.\build.ps1` passes in Debug configuration.
- [ ] `.\test.ps1 -TestProject DetailControlsTests` passes.
- [ ] `.\test.ps1 -TestFilter RenderBaseline` passes (visual regression gate).
- [ ] `.\Build\Agent\check-and-fix-whitespace.ps1` reports clean output.
- [ ] HWND count regression gate test passes.
- [ ] No new compiler warnings introduced.
