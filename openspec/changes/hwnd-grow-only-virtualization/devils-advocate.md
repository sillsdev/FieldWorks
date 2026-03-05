# Devil's Advocate Analysis

A systematic risk assessment of the grow-only HWND virtualization approach, organized by component type and by FLEx view. The goal is to identify every way this could fail, estimate whether savings will materialize, and determine how airtight the implementation can be.

---

## Part 1: Per Component Category

### Category 1: ViewSlice Family (~40 classes, ~70% of all slices)

Every ViewSlice descendant embeds a `RootSite` (Views-engine rendering surface backed by `IVwRootBox`). This is the most common slice type: StringSlice, MultiStringSlice, StTextSlice, all ButtonLauncher-based reference slices, SummarySlice, InterlinearSlice, RuleFormulaSlice, etc.

| Risk | Severity | Detail | Grow-Only Impact |
|------|----------|--------|------------------|
| RootBox lifecycle: create-on-scroll | Medium | `BecomeRealInPlace()` forces Handle creation → RootBox creation. For complex views (InterlinearSlice, StTextSlice with large content), this can take 50-200ms. | **Acceptable** — cost is paid once per slice, same as current behavior but shifted from entry-load to scroll-into-view. User perceives faster initial load. |
| RootBox lifecycle: destroy on scroll-away | **N/A** | Would lose selection, undo history, IME composition. | **Not applicable** — grow-only never destroys. |
| Event subscription timing | Medium | ViewSlice subscribes `LayoutSizeChanged`, `Enter`, `SizeChanged` on its RootSite. These must be subscribed when the RootSite HWND exists. | **Manageable** — move subscriptions from `Install()` to `EnsureHwndCreated()`. Verified: all three events only fire on HWND'd controls. |
| SummarySlice composite controls | Medium | Embeds Panel + SummaryXmlView + ExpandCollapseButton + SummaryCommandControl. Expand/collapse state lives in the control tree. | **Manageable** — expand/collapse state can be stored in a field on SummarySlice and applied when HWND is created. Already has `Expansion` property on base Slice. |
| RuleFormulaSlice InsertionControl | Low-Med | Has show/hide InsertionControl triggered by Enter/Leave. | **Non-issue in grow-only** — the InsertionControl only shows when the slice is focused, which means it's visible and HWND'd. |
| `Reuse()` pattern on StringSlice | Low | StringSlice has a `Reuse()` method for recycling. | **Compatible** — Reuse only applies to already-HWND'd slices. |

**Will savings materialize?** Yes, conclusively. In a 253-slice entry, if the user views only the first 15 slices (~60% of entries are edited on the first screen), the ViewSlice family contributes 0 HWNDs for the 238 unseen slices. That's ~238 × 7 = **1,666 HWNDs saved**.

### Category 2: FieldSlice with WinForms Controls (~8 classes, ~10%)

CheckboxSlice (CheckBox), DateSlice (RichTextBox), IntegerSlice (TextBox), EnumComboSlice (ComboBox), MSAReferenceComboBoxSlice (TreeCombo), ReferenceComboBoxSlice (FwComboBox), AtomicReferencePOSSlice (TreeCombo), GenDateSlice (ButtonLauncher).

| Risk | Severity | Detail | Grow-Only Impact |
|------|----------|--------|------------------|
| TextBox/ComboBox lose edit state on destroy | Medium | IntegerSlice commits on LostFocus. Destroying HWND loses the Text value. | **Not applicable** — grow-only never destroys. |
| ComboBox dropdown during scroll | Medium | If dropdown is open during scroll and HWND is destroyed. | **Not applicable** — grow-only never destroys. |
| TreeCombo popup state | Medium | MSAReferenceComboBoxSlice uses TreeCombo with popup managers. | **Not applicable** — grow-only never destroys. |
| CheckBox is trivial | None | Commits immediately. | **Safe** — deferred creation is fine; checked state comes from database. |
| DateSlice is read-only | None | Text rebuilt from database on PropChanged. | **Safe**. |
| Constructor creates controls eagerly | Medium | FieldSlice constructor takes the content control as a parameter (`base(control)`), creating it before Install(). | **Needs adjustment** — either defer control creation to a factory lambda, or accept that the WinForms control is created eagerly but NOT added to a parent HWND. A Control without a parent doesn't allocate an HWND. |

**Will savings materialize?** Yes, but modest absolute numbers. Typical entries have 0-3 FieldSlices. At ~6 HWNDs each = 0-18 HWNDs saved. Small contributor.

### Category 3: Lightweight/Stateless Slices (~5 classes, ~5%)

MessageSlice (Label), CommandSlice (Button), ImageSlice (PictureBox), LexReferenceMultiSlice (header only), DummyObjectSlice (already virtual).

| Risk | Severity | Detail | Grow-Only Impact |
|------|----------|--------|------------------|
| Nothing meaningful | None | All stateless. Label displays static text; Button fires command; PictureBox reloads from file; DummyObjectSlice has no HWND. | **No risk**. Easiest to virtualize. |

**Will savings materialize?** Yes — ~5-15 per entry × 6 HWNDs = 30-90 saved. Meaningful but not the main win.

### Category 4: ButtonLauncher-Based Reference Slices (~25 classes, ~15%)

All AtomicReferenceSlice, ReferenceVectorSlice, CustomReferenceVectorSlice descendants. Each embeds a ButtonLauncher → RootSite (inner view) + chooser Button.

| Risk | Severity | Detail | Grow-Only Impact |
|------|----------|--------|------------------|
| Chooser dialog open during scroll | Low | Modal dialog prevents scrolling. | **Non-issue**. |
| IVwNotifyChange registration | Medium | AtomicReferenceSlice registers for SDA notifications. If HWND never created, the notification subscription is still active but the control doesn't exist for UI updates. | **Needs analysis** — does `PropChanged` on a non-HWND'd slice try to update UI? If yes, need guard `if (IsHandleCreated)`. If no (data-only), safe. |
| LexReference slices with cross-entry data | Medium | ILexReferenceSlice implementations have data relationships that persist beyond HWND lifetime. | **Safe in grow-only** — data relationships are in the model, not in HWNDs. |
| FieldSlice base constructor eagerness | Same as Category 2 | Constructor passes launcher control to `base(control)`. | **Same mitigation as Category 2**. |

**Will savings materialize?** **Yes — this is the biggest win.** A typical lexical entry with senses, parts of speech, and cross-references can have 10-30 reference slices. At 7-8 HWNDs each = 70-240 HWNDs. Most of these are below the fold and will never get HWNDs in grow-only mode.

---

## Part 2: Per Main View

### Lexicon Edit — Highest Impact, Highest Risk

- **Typical slice count:** 40-253 per entry
- **Slice mix:** 60% ViewSlice (StringSlice, MultiStringSlice), 25% Reference slices (launchers), 10% FieldSlice (POS combos, checkboxes), 5% headers/messages
- **User behavior:** Type in StringSlice fields, switch between entries, Tab-navigate. High IME usage for CJK/complex scripts.
- **Expected HWND reduction:** 253 slices × ~6 HWNDs = ~1,500 → ~15 visible × 6 = ~90. **94% reduction for first-screen editing.**
- **If user scrolls through entire entry:** All HWNDs created, reduction = 0%. But entry-switch still benefits because HWNDs were allocated incrementally during use rather than all at once on entry load.
- **Key regression risks:**
  - Tab navigation must seamlessly create HWNDs for the target slice
  - Find/Replace that highlights a non-visible slice must create its HWND
  - Ctrl+End (jump to last field) must work — creates HWND for last slice
  - Screen reader enumeration of visible-only controls (acceptable: matches current `Visible=false` behavior)
- **Confidence level:** 8/10

### Grammar Edit — Medium Impact, Medium Risk

- **Typical slice count:** 20-60
- **Slice mix:** Heavy on specialized slices (InflAffixTemplateSlice, RuleFormulaSlice, PhonologicalFeatureListDlgLauncherSlice)
- **Expected HWND reduction:** ~200 HWNDs → ~90. **55% reduction.**
- **Key regression risks:**
  - InflAffixTemplateSlice renders complex template grid via custom RootSiteControl — expensive to create on first scroll. User may notice a pause.
  - RuleFormulaSlice InsertionControl show/hide is focus-dependent — only applies to visible slices, safe.
- **Confidence level:** 8/10

### Notebook Edit — Low Impact, Low Risk

- **Typical slice count:** 15-30
- **Slice mix:** Mostly StTextSlice (large, typically taller than viewport), a few DateSlice, some reference slices.
- **Expected HWND reduction:** ~50% of slices are below fold. ~50 HWNDs saved.
- **Key risk:** StTextSlice may be taller than the viewport → always visible → always HWND'd. Savings come from the other slices only.
- **Confidence level:** 9/10

### Lists Edit — Low Impact, Low Risk

- **Typical slice count:** 5-20
- **Slice mix:** Almost all StringSlice and MultiStringSlice
- **Expected HWND reduction:** Minimal absolute numbers. Often all slices fit in the viewport → no win.
- **Confidence level:** 9/10

### Interlinear Info Pane — Low Impact, Special Case

- **Typical slice count:** 10-30
- **Special behavior:** InfoPane dynamically creates/destroys RecordEditView per word selection. DataTree lifetime is already short.
- **Expected HWND reduction:** Small.
- **Confidence level:** 8/10

---

## Part 3: Structural Risks & Airtightness

### The .NET Framework High-Water Mark Bug (LT-7307)

**Problem:** Setting `Visible=true` on a child control changes its position in the parent's `Controls` collection. All slices before a visible slice must also be `Visible=true`.

**Impact on grow-only:** This constraint is **bypassed entirely** by not adding non-visible slices to `Controls`. Slices not in `Controls` don't participate in the visibility-based reordering. The `Slices` logical list maintains stable indexing independent of `Controls`.

**Airtightness:** 9/10

### Focus Chain Integrity

**Problem:** `CurrentSlice` setter calls `Validate()` on the old slice's `Control`. If the old slice never had an HWND, `Validate()` would throw or NRE.

**Mitigation:** Guard with `if (m_currentSlice.IsHandleCreated)`. In grow-only, this only triggers for the edge case where `CurrentSlice` is set to a never-viewed slice (possible via programmatic navigation, but these paths already call `MakeSliceVisible`).

**Airtightness:** 9/10

### Accessibility Tree Dynamism

**Problem:** Screen readers (NVDA, JAWS) enumerate the HWND tree. If HWNDs appear dynamically as the user scrolls, the screen reader may lose its position.

**Assessment:** Current behavior already has dynamic visibility — slices start `Visible=false` and are set to `Visible=true` on scroll. Screen readers already handle this. In grow-only, slices not in `Controls` are simply absent from the HWND tree, which is equivalent to `Visible=false` from the screen reader's perspective.

**Airtightness:** 8/10 (screen reader behavior with dynamic HWND creation is hard to test exhaustively)

### Scroll Position Accuracy

**Problem:** If height estimates for non-HWND slices are wrong, the scrollbar thumb position will jump when slices scroll into view and get their actual height.

**Assessment:** Default height for single-line slices is ~20px. For multi-line slices (StTextSlice), the actual height can be much larger. A StTextSlice estimated at 20px but actually 200px would cause a 10× jump.

**Mitigation:** Use XML layout hints (e.g., `<slice editor="sttext">` → use 60px default). Accept that scrollbar accuracy degrades for entries with many non-visible tall slices. This is within acceptable UX bounds — the scrollbar stabilizes as the user scrolls.

**Airtightness:** 6/10 (this is the weakest point; requires empirical tuning)

---

## Part 4: Summary Assessment

### Savings Confidence Matrix

| Scenario | HWND Before | HWND After (grow-only) | Reduction | Confidence |
|----------|------------|----------------------|-----------|------------|
| Lexicon Edit, first screen only | ~1,500 | ~90 | **94%** | High |
| Lexicon Edit, full scroll-through | ~1,500 | ~1,500 | 0% | N/A (grow-only ceiling) |
| Lexicon Edit, partial scroll (top half) | ~1,500 | ~750 | **50%** | High |
| Grammar Edit, first screen | ~360 | ~90 | **75%** | High |
| Lists Edit, short entry | ~120 | ~120 | 0% (all fit) | N/A |
| Lists Edit, longer entry | ~120 | ~60 | **50%** | Medium |

### Risk vs. Reward

| Factor | Assessment |
|--------|-----------|
| **Common-case benefit** | Massive — 80-95% HWND reduction for typical editing |
| **Worst-case ceiling** | Same as current (all HWNDs created if user scrolls everything) |
| **Risk of data loss** | Zero — grow-only never destroys state |
| **Risk of visual regression** | Low — slices render identically once HWND'd |
| **Risk of functional regression** | Low-Medium — focus/Tab management is the primary concern |
| **Risk of scroll UX degradation** | Medium — height estimation inaccuracy causes scrollbar jumps |
| **Testing burden** | Medium — need to test all paths that assume HWNDs exist |

### The Honest Bottom Line

Grow-only virtualization is the right first move because:

1. **It's safe.** Never destroying HWNDs eliminates the entire class of state-loss bugs (selection, IME, undo, accessibility).
2. **It's impactful.** 80-95% reduction for the common case addresses the core performance complaint.
3. **It's incremental.** Each phase can be delivered and tested independently.
4. **It has a known ceiling.** If the user scrolls through everything, we're back to baseline — no worse than today.

The main weakness is scrollbar accuracy for non-visible slices. This is a UX annoyance, not a functional bug, and can be mitigated with reasonable height estimates. It's the kind of imperfection that's worth accepting to avoid the catastrophic risks of full HWND recycling.
