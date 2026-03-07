# DataTree State Management & Virtualization Constraints

This document records the critical state-management patterns in DataTree, Slice, and ViewSlice that constrain how HWND virtualization can be implemented. Every finding here was extracted from the actual codebase on the `001-render-speedup` branch.

---

## 1. DataTree Usage Across FLEx

### Single Production Host

`RecordEditView` is the **sole production host** for DataTree. The flow is:

```
RecordEditView() → new DataTree()
    → Init() → SetupDataContext()
        → m_dataEntryForm.Initialize(cache, true, layouts, parts)
        → Controls.Add(m_dataEntryForm)
    → ShowRecord() → ShowRecordOnIdle()
        → m_dataEntryForm.ShowObject(obj, layoutName, ...)
```

Instantiation sites:
- `Src/xWorks/RecordEditView.cs` — production
- `Src/Common/RenderVerification/DataTreeRenderHarness.cs` — benchmark
- `Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeTests.cs` — unit tests
- `Src/Common/Controls/DetailControls/DetailControlsTests/SliceTests.cs` — unit tests

### FLEx Areas Using DataTree

Every area with a "detail pane" uses `RecordEditView` → `DataTree`:

| Area | Config Location | Typical Slice Count |
|------|----------------|---------------------|
| **Lexicon Edit** | `DistFiles/Language Explorer/Configuration/Lexicon/Edit/toolConfiguration.xml` | 40-253 |
| **Lexicon RDE** | `.../Lexicon/RDE/toolConfiguration.xml` | 20-40 |
| **Reversal Indices** | `.../Lexicon/ReversalIndices/toolConfiguration.xml` | 15-40 |
| **Grammar (all tools)** | `.../Grammar/Edit/toolConfiguration.xml` | 20-60 |
| **Notebook** | `.../Notebook/Edit/toolConfiguration.xml` | 15-30 |
| **Lists (all)** | `.../Lists/Edit/toolConfiguration.xml` | 5-20 |
| **Words/Analyses** | `.../Words/Analyses/toolConfiguration.xml` | 10-30 |
| **Interlinear Info Pane** | `Src/LexText/Interlinear/InfoPane.cs` | 10-30 |

---

## 2. Focus Management

### Slice.OnEnter → CurrentSlice

```csharp
// Slice.cs line 557
protected override void OnEnter(EventArgs e)
{
    base.OnEnter(e);
    if (ContainingDataTree == null || ContainingDataTree.ConstructingSlices)
        return;
    ContainingDataTree.CurrentSlice = this;
    TakeFocus(false);
}
```

### Slice.TakeFocus — Forces Visibility

```csharp
// Slice.cs line 647
public bool TakeFocus(bool fOkToFocusTreeNode)
{
    Control ctrl = Control;
    if (!Visible)
    {
        if ((ctrl != null && ctrl.TabStop) || fOkToFocusTreeNode)
            ContainingDataTree.MakeSliceVisible(this); // forces HWND creation
    }
    if (ctrl != null && ctrl.CanFocus && ctrl.TabStop)
        ctrl.Focus();
    // ...
}
```

**Constraint:** Any code path that tries to focus a slice must first ensure the HWND exists. `TakeFocus` already calls `MakeSliceVisible`, which is the natural integration point for `EnsureHwndCreated()`.

### Slice.OnGotFocus — Scroll Into View

```csharp
// Slice.cs line 683
protected override void OnGotFocus(EventArgs e)
{
    if (Disposing) return;
    ContainingDataTree.MakeSliceVisible(this);
    base.OnGotFocus(e);
    if (Control != null && Control.CanFocus)
        Control.Focus();
}
```

### DataTree.CurrentSlice Setter — The Central State Machine

```csharp
// DataTree.cs line 694
set
{
    if (m_currentSlice == value || m_fSuspendSettingCurrentSlice) return;
    // Validate old slice (commits pending edits)
    if (m_currentSlice != null)
    {
        m_currentSlice.Validate();
        if (m_currentSlice.Control is ContainerControl)
            ((ContainerControl)m_currentSlice.Control).Validate();
        m_currentSlice.SetCurrentState(false);
    }
    m_currentSlice = value;
    m_currentSlice.SetCurrentState(true);
    ScrollControlIntoView(m_currentSlice);
    // Ensure tab neighbors are real (for Tab/Shift-Tab navigation)
    for (int i = index + 1; ...) MakeSliceRealAt(i);
    for (int i = index - 1; ...) MakeSliceRealAt(i);
    m_descendant = DescendantForSlice(m_currentSlice);
    CurrentSliceChanged?.Invoke(this, new EventArgs());
}
```

**Constraint:** The `Validate()` call on the old `CurrentSlice` requires its `Control` to exist. In grow-only mode this is safe (we never destroy), but must be guarded with a null check if the slice was never scrolled into view and someone sets `CurrentSlice` to a different slice. This can happen via `Tab` key to a non-visible slice → `TakeFocus` → `MakeSliceVisible` which creates the HWND.

### ViewSlice_Enter — Redirects Focus to RootSite

```csharp
// ViewSlice.cs line 296
private void ViewSlice_Enter(object sender, EventArgs e)
{
    RootSite.Focus();
    ContainingDataTree.ActiveControl = RootSite;
}
```

---

## 3. Selection State

### No Save/Restore Pattern Exists

The codebase has **no explicit save/restore** of `VwSelLevInfo[]` (RootBox text selection). Selection state management:

- `DataTree.CurrentSlice` tracks **which** slice is selected (persisted via `m_currentSlicePartName` and `m_currentSliceObjGuid` to `PropertyTable`).
- On `RefreshList`, the part name + object GUID are used to re-find the matching slice.
- `SetDefaultCurrentSlice` places the cursor at position 99999 (end of line).
- The selection (insertion point / text range) within a `RootSite` is **lost when the RootBox is destroyed**.

**Implication for grow-only:** Since we never destroy HWNDs, selection state is preserved naturally. This constraint only matters if we ever move to Phase 4 (full recycle), which is out of scope.

---

## 4. IME (Input Method Editor) State

No direct IME handling in Slice or DataTree. IME is managed at the `SimpleRootSite` level:

```csharp
// SimpleRootSite.cs line 6200
// WM_IME_CHAR (0x286) intercepted in WndProc to prevent duplicate WM_CHAR messages
```

Keyboard/input method switching is handled by `GetWSForInputMethod` which maps keyboards to writing systems.

**Implication for grow-only:** Since we never destroy HWNDs, no IME composition can be lost. This is a non-issue for this approach.

---

## 5. Accessibility

Accessibility names are set at multiple points:

| Location | What |
|----------|------|
| `Slice.cs` line 524 | `SplitContainer.AccessibleName = "Slice.SplitContainer"` |
| `Slice.cs` lines 806-809 | `Panel1.AccessibleName = "Panel1"`, `Panel2.AccessibleName = "Panel2"` |
| `Slice.cs` line 871 | `mainControl.AccessibleName = Label` |
| `DataTree.MakeSliceVisible` line 3575 | `tci.Control.AccessibilityObject.Name = tci.Label` (set **only when visible**) |
| `ViewSlice.Install` line 186 | `rs.SetAccessibleName(this.Label)` |
| `SimpleRootSite WndProc` line 6190 | Responds to `WM_GETOBJECT` with `IAccessible` via `LresultFromObject` |

**Implication for grow-only:** Accessibility names are already set lazily in `MakeSliceVisible`. Screen readers will only see HWNDs that have been created (visible slices). This matches current behavior — slices with `Visible=false` are already not in the accessibility tree.

---

## 6. Event Subscriptions

### DataTree → LCM Data Notifications

```csharp
m_sda.AddNotification(this);        // in InitializeComponentBasic
m_sda.RemoveNotification(this);     // in Dispose
// PropChanged monitors specific (hvo, flid) pairs:
m_monitoredProps.Add(Tuple.Create(obj.Hvo, flid));
```

**These are on DataTree itself, not on individual slices.** Safe for virtualization.

### DataTree → Mediator

```csharp
Subscriber.Subscribe(EventConstants.PostponePropChanged, PostponePropChanged);
m_mediator.IdleQueue.Add(IdleQueuePriority.High, OnReadyToSetCurrentSlice, ...);
```

### Slice Event Subscriptions

```csharp
// Per-slice, installed in InstallSlice:
SplitCont.SplitterMoved += slice_SplitterMoved;
// Removed in RemoveSlice:
gonner.SplitCont.SplitterMoved -= ...;
```

### ViewSlice Event Subscriptions

```csharp
// ViewSlice.cs Install (line 67-88):
rs.LayoutSizeChanged += HandleLayoutSizeChanged;
rs.Enter += ViewSlice_Enter;
rs.SizeChanged += rs_SizeChanged;
// Dispose:
rs.LayoutSizeChanged -= HandleLayoutSizeChanged;
rs.Enter -= ViewSlice_Enter;
rs.SizeChanged -= rs_SizeChanged;
```

**Implication for grow-only:** Event subscriptions are set during `Install()` and removed during `Dispose()`. If we defer `Install()` or split it into logical-install + HWND-install, we need to ensure events are subscribed at the right time. Specifically: `SplitterMoved` should only be subscribed when the SplitContainer exists.

---

## 7. The High-Water Mark Constraint

### The Problem (LT-7307)

```csharp
// DataTree.cs MakeSliceVisible, line 3553
internal void MakeSliceVisible(Slice tci, int knownIndex = -1)
{
    if (!tci.Visible)
    {
        int index = knownIndex >= 0 ? knownIndex : Slices.IndexOf(tci);
        // ALL prior slices must also be Visible (WinForms Controls collection
        // reorders when Visible changes, breaking indices)
        int start = Math.Max(0, m_lastVisibleHighWaterMark + 1);
        for (int i = start; i < index; ++i)
        {
            Control ctrl = Slices[i];
            if (ctrl != null && !ctrl.Visible)
                ctrl.Visible = true;
        }
        tci.Visible = true;
        if (index > m_lastVisibleHighWaterMark)
            m_lastVisibleHighWaterMark = index;
    }
    tci.ShowSubControls();
}
```

**The fundamental constraint:** Due to a .NET Framework bug, setting `Visible=true` on a child control changes its position in the parent's `Controls` collection. If slice index 20 is made visible while slices 0-19 are invisible, the indices shift and all slice lookups break. The workaround is the high-water mark: all slices from 0 to the target must be set `Visible=true`.

**Implication for grow-only:** If we keep slices OUT of `DataTree.Controls` until they need to be visible, this constraint doesn't apply — slices not in `Controls` don't participate in the index-shifting bug. The `Slices` list (logical ordering) is separate from `Controls` (HWND parent-child). This is the key architectural insight that makes grow-only viable.

---

## 8. HandleLayout1 — The Visibility Decision

```csharp
// DataTree.cs line 3398
// Full layout (fFull=true, from OnLayout): iterates ALL slices
// Paint check (fFull=false, from OnPaint): binary search to visible range

bool fSliceIsVisible = !fFull && yTop + defHeight > clipRect.Top && yTop <= clipRect.Bottom;

// When visible:
FieldAt(i);                              // ensures slice is real (not DummyObjectSlice)
var dummy = tci.Handle;                  // forces HWND creation
dummy = tci.Control.Handle;             // forces content HWND creation
tci.SetWidthForDataTreeLayout(width);   // triggers RootSite layout if ViewSlice
MakeSliceVisible(tci, i);               // sets Visible=true (+ high-water mark)
```

**This is the natural integration point.** The `fSliceIsVisible` check already exists. In the virtualized model:
- Non-visible slices skip the Handle-forcing and MakeSliceVisible calls (they already do in the `fFull=true` path)
- The `yTop` positioning for non-visible slices would use the virtual layout arrays instead of `tci.Top`

---

## 9. FieldAt — Lazy Realization

```csharp
// DataTree.cs line 3171
public Slice FieldAt(int i)
{
    Slice slice = FieldOrDummyAt(i);
    while (!slice.IsRealSlice)
    {
        if (slice.BecomeRealInPlace())
        {
            SetTabIndex(Slices.IndexOf(slice));
            return slice;
        }
        slice.BecomeReal(i);    // DummyObjectSlice → real slices
        RemoveSliceAt(i);       // remove the dummy
        slice = Slices[i];      // pick up replacement
    }
    return slice;
}
```

Two paths:
- `BecomeRealInPlace() == true` → ViewSlice: same object stays, RootBox created, `AllowLayout=true`
- `BecomeReal(i)` → DummyObjectSlice: replaces itself with real slices via `CreateSlicesFor`

**Implication:** `FieldAt` is only called for visible slices (from `HandleLayout1` when `fSliceIsVisible`). Non-visible slices remain as DummyObjectSlice placeholders. This is already the correct behavior for grow-only.

---

## 10. Slice Disposal

```csharp
// DataTree.RemoveSlice (line 415):
gonner.AboutToDiscard();               // notify
gonner.SplitCont.SplitterMoved -= ...; // unsubscribe
Controls.Remove(gonner);               // remove from WinForms Controls
Slices.RemoveAt(index);               // remove from logical list
gonner.Dispose();                      // full disposal

// ViewSlice.Dispose (line 67):
rs.LayoutSizeChanged -= HandleLayoutSizeChanged;
rs.Enter -= ViewSlice_Enter;
rs.SizeChanged -= rs_SizeChanged;
```

**Implication for grow-only:** Disposal patterns remain unchanged. When a slice is removed (due to navigation to a different entry), it follows the same dispose path regardless of whether its HWND was ever created.

---

## Key Takeaways for Implementation

1. **The `Controls` collection and the `Slices` list can be decoupled.** Slices exist in the logical list from creation; they enter `Controls` only when `MakeSliceVisible` / `EnsureHwndCreated` is called. This sidesteps the high-water mark bug.

2. **`MakeSliceVisible` is the natural enforcement point.** All paths that need an HWND (focus, Tab navigation, CurrentSlice) already call `MakeSliceVisible`. Adding `EnsureHwndCreated()` here covers all cases.

3. **The `Validate()` call on CurrentSlice is the main risk.** If a slice was never visible (never had HWND), and is being replaced as CurrentSlice, `Validate()` would fail. Guard: only call `Validate()` if `m_currentSlice.IsHandleCreated`.

4. **Event subscriptions need to be split.** Logical subscriptions (data notifications) should happen at logical-install time. HWND subscriptions (SplitterMoved, LayoutSizeChanged) should happen at HWND-creation time.

5. **Scroll position and `AutoScrollMinSize` need virtual heights.** The DataTree scroll region must be computed from virtual heights (estimated for non-HWND slices, actual for HWND'd slices) to maintain scrollbar accuracy.
