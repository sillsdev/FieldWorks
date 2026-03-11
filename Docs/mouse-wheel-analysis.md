# Mouse Wheel Forwarding Analysis: ButtonLauncher → DataTree

## Problem Statement

When the user hovers the mouse cursor over a `ButtonLauncher` control inside the DataTree
(Lexicon Editor detail pane) and scrolls the mouse wheel, the DataTree does not scroll.
Scrolling works normally when the cursor is over other areas of the DataTree.

---

## 1. Control Hierarchy

```
Form (ContainerControl)
└── SplitContainer / Panels
    └── RecordEditView
        └── DataTree : UserControl → ContainerControl → ScrollableControl
            ├── Slice (various types)
            │   └── ButtonLauncher : UserControl → ContainerControl → ScrollableControl
            │       ├── m_panel : Panel (HWND)
            │       │   └── m_btnLauncher : Button (HWND)
            │       └── m_mainControl : RootSiteControl (HWND) ← most common
            │           └── IVwRootBox (COM, renders into RootSiteControl's HWND)
            ├── Slice (non-ButtonLauncher types)
            │   └── various controls
            └── (scrollbar managed by ScrollableControl base)
```

### Inheritance Chains

| Class | Chain |
|-------|-------|
| **DataTree** | `UserControl → ContainerControl → ScrollableControl → Control` |
| **ButtonLauncher** | `UserControl → ContainerControl → ScrollableControl → Control` |
| **RootSiteControl** | `RootSite → SimpleRootSite → UserControl → ContainerControl → ScrollableControl → Control` |

### ButtonLauncher Subclasses and Their MainControl Types

| Subclass | MainControl | Type |
|----------|------------|------|
| AtomicReferenceLauncher | AtomicReferenceView | RootSiteControl (COM-based) |
| VectorReferenceLauncher | VectorReferenceView | RootSiteControl (COM-based) |
| PossibilityAtomicReferenceLauncher | AtomicReferenceView | RootSiteControl (COM-based) |
| MSADlgLauncher | MSADlglauncherView | RootSiteControl (COM-based) |
| PhonologicalFeatureListDlgLauncher | PhonologicalFeatureListDlgLauncherView | RootSiteControl (COM-based) |
| MsaInflectionFeatureListDlgLauncher | MsaInflectionFeatureListDlgLauncherView | RootSiteControl (COM-based) |
| RuleFormulaControl | PatternView | RootSiteControl (COM-based) |
| AudioVisualLauncher | AudioVisualView | RootSiteControl (COM-based) |
| GenDateLauncher | TextBox | Standard WinForms |
| GhostReferenceVectorLauncher | (none) | No MainControl |
| GhostLexRefLauncher | (none) | No MainControl |

**8 out of 11 subclasses** use RootSiteControl-based views with COM rendering.

---

## 2. Win32 WM_MOUSEWHEEL Message Routing

### Standard Windows Behavior

1. **WM_MOUSEWHEEL (0x020A)** is sent to the **focused window** (HWND with keyboard focus),
   NOT the window under the mouse cursor.
2. **Windows 10+ "Scroll inactive windows"** setting routes WM_MOUSEWHEEL to the
   top-level window under the cursor, but WinForms still routes internally via focus.
3. **DefWindowProc** bubbles unhandled WM_MOUSEWHEEL to the parent HWND.

### WinForms ContainerControl Focus Routing

`ContainerControl.WmMouseWheel` (called from `ContainerControl.WndProc`) adds another
routing layer:

1. When a ContainerControl receives WM_MOUSEWHEEL, `WmMouseWheel` is called
2. `WmMouseWheel` finds the **ActiveControl** (focused child)
3. If the ActiveControl is another **ContainerControl**, it calls its `WmMouseWheel` recursively
4. Otherwise, it sends WM_MOUSEWHEEL to the ActiveControl via `SendMessage`
5. If nobody handles it, calls `DefWndProc` which bubbles to the parent

**Key consequence**: The Form → DataTree → ButtonLauncher → MainControl chain always routes
WM_MOUSEWHEEL to the innermost focused control, never to a sibling or unrelated control.

---

## 3. Message Flow Chain (When Cursor Is Over RootSiteControl MainControl)

```
WM_MOUSEWHEEL arrives at: Form HWND
  ↓ Form.WndProc → ContainerControl.WmMouseWheel
  ↓ Routes to ActiveControl (ultimately the focused control in DataTree)
  ↓
If ActiveControl chain leads to a ButtonLauncher child:
  ↓
RootSiteControl (MainControl) HWND receives WM_MOUSEWHEEL
  ↓ SimpleRootSite.WndProc → MessageSequencer → OriginalWndProc
  ↓ OriginalWndProc: no WM_MOUSEWHEEL handler → falls through to base.WndProc
  ↓ UserControl.WndProc → ContainerControl.WndProc
  ↓ ContainerControl.WmMouseWheel: no focused child → base.WndProc
  ↓ ScrollableControl.WndProc → calls Control.WmMouseWheel
  ↓ Control.WmMouseWheel → OnMouseWheel(HandledMouseEventArgs)
  ↓
RootSite.OnMouseWheel:
  ↓ if m_group != null && this != scrollingController → redirect
  ↓ else → base.OnMouseWheel(e)
  ↓
ScrollableControl.OnMouseWheel(e):
  ↓ if (VScroll) → scroll → set Handled = true       ← **GATE #1**
  ↓ base.OnMouseWheel(e) → Control.OnMouseWheel(e)
  ↓ Raises MouseWheel event
  ↓
ButtonLauncher.HandleForwardMouseWheel fires (registered on m_mainControl)
  ↓ FindMouseWheelTarget() → DataTree
  ↓ InvokeOnMouseWheel(dataTree, e)
  ↓
[reflection] s_onMouseWheelMethod.Invoke(dataTree, {e})
  → ScrollableControl.OnMouseWheel(e) on DataTree (virtual dispatch)
  ↓ if (VScroll) → scroll → set Handled = true         ← **GATE #2**
  ↓ base.OnMouseWheel(e) → raises MouseWheel event
```

---

## 4. All Possible Failure Points

### FP-1: WM_MOUSEWHEEL Never Reaches ButtonLauncher or Its Children

**Scenario**: Focus is on a control OUTSIDE the ButtonLauncher (e.g., a non-ButtonLauncher
slice in the DataTree). WM_MOUSEWHEEL goes to that other control, never reaching our code.

**Likelihood**: **LOW** in the specific test scenario (user clicks IN a ButtonLauncher field,
then scrolls). But in general usage, focus can be anywhere.

**Impact**: No scroll at all — message never enters our forwarding code.

### FP-2: ContainerControl.WmMouseWheel Re-routes Message

**Scenario**: An intermediate ContainerControl (Form, SplitContainer, DataTree itself)
intercepts WM_MOUSEWHEEL and routes it to a focused child that is NOT inside a ButtonLauncher.

**Likelihood**: **MEDIUM** — depends on focus state. If focus was last set to a control
inside a ButtonLauncher, the routing should deliver the message there.

### FP-3: SimpleRootSite MessageSequencer Delays or Drops Message

**Scenario**: SimpleRootSite.WndProc passes WM_MOUSEWHEEL through `m_messageSequencer.SequenceWndProc`.
The sequencer might delay, reorder, or drop the message.

**Likelihood**: **LOW** — the message sequencer is designed the prevent re-entrant WndProc
calls, not to drop messages.

### FP-4: RootSite.OnMouseWheel Redirects to ScrollingController

**Scenario**: `RootSite.OnMouseWheel` checks `m_group` and if set, redirects the mouse wheel
to the scrolling controller. If the scrolling controller consumes the event without raising
the MouseWheel event to our handler, the forwarding fails.

**Likelihood**: **LOW** for ButtonLauncher children, which are standalone field views not
typically in a scroll group. But worth verifying at runtime.

### FP-5: ScrollableControl.OnMouseWheel VScroll Gate (MainControl) ★

**Scenario**: `ScrollableControl.OnMouseWheel` on the MainControl (RootSiteControl) checks
`if (VScroll)`. The MainControl is a small field control that does NOT have a vertical scrollbar.
`VScroll` is `false`. Therefore, `Handled` is NOT set to `true`.

**BUT**: This is NOT a fatal failure. ScrollableControl.OnMouseWheel still calls
`base.OnMouseWheel(e)` even when VScroll is false, which raises the MouseWheel event.
Our HandleForwardMouseWheel handler fires regardless.

**Impact**: None on forwarding — the event still reaches our handler.

### FP-6: ScrollableControl.OnMouseWheel VScroll Gate (DataTree) ★★★

**Scenario**: When our forwarding code calls `ScrollableControl.OnMouseWheel` on the DataTree
(via reflection), it checks `if (VScroll)`. If DataTree's `VScroll` is `false`, no scrolling occurs.

**Analysis**:
- DataTree sets `AutoScrollMinSize = new Size(0, yTop)` in `OnLayout` (line 3283)
- In .NET Framework, setting `AutoScrollMinSize` to a non-zero value automatically sets
  `AutoScroll = true` (the setter includes `AutoScroll = true`)
- When `AutoScroll = true` and content exceeds viewport, `AdjustFormScrollbars` sets
  `VScroll = true`
- The vertical scrollbar IS visible on the DataTree

**Likelihood**: **LOW** if layout has completed. VScroll should be true since the scrollbar
is visible.

### FP-7: Reflection MethodInfo.Invoke Dispatch ★★

**Scenario**: `typeof(Control).GetMethod("OnMouseWheel")` gets the `MethodInfo` for
`Control.OnMouseWheel`. When `Invoke` is called on a DataTree instance, virtual dispatch
should resolve to `ScrollableControl.OnMouseWheel` (the most-derived override).

**Analysis**: `MethodInfo.Invoke` DOES perform virtual dispatch for virtual methods.
This is standard .NET behavior.

**Likelihood**: **VERY LOW** — this is well-documented behavior.

### FP-8: DataTree OnLayout Resets Scroll Position ★★

**Scenario**: After `SetDisplayRectLocation` changes the scroll position, WinForms triggers
a layout cycle. DataTree.OnLayout (line 3254) saves/restores `AutoScrollPosition`:

```csharp
Point aspOld = AutoScrollPosition;
base.OnLayout(levent);
if (AutoScrollPosition != aspOld)
    AutoScrollPosition = new Point(-aspOld.X, -aspOld.Y);
```

The layout code saves the position AFTER the scroll change, calls base.OnLayout, and
restores it if base.OnLayout changed it. This should preserve our scroll position change.

**Likelihood**: **LOW** — the save/restore pattern preserves the current position.

### FP-9: HandleForwardMouseWheel Event Not Wired ★★

**Scenario**: `RegisterWheelForwarding(m_mainControl)` subscribes to `m_mainControl.MouseWheel`.
But if the MainControl is set and replaced, or if the control handle is recreated, the event
subscription might be lost.

**Analysis**: `RegisterWheelForwarding` is called in the `MainControl` setter:
```csharp
set {
    Debug.Assert(m_mainControl == null); // only set once
    m_mainControl = value;
    m_mainControl.TabIndex = 0;
    RegisterWheelForwarding(m_mainControl);
}
```

The `Debug.Assert(m_mainControl == null)` confirms it's only set once. So the subscription
should persist.

**Likelihood**: **LOW** — single-assignment pattern ensures subscription is stable.

### FP-10: ButtonLauncher.WndProc Never Receives WM_MOUSEWHEEL ★★★

**Scenario**: WM_MOUSEWHEEL is sent to the focused control, which is a CHILD of
ButtonLauncher (e.g., the RootSiteControl/MainControl). The message goes directly to the
child's HWND, never passing through ButtonLauncher's WndProc.

In this case, only `HandleForwardMouseWheel` (the MouseWheel event handler on the child)
can catch it. ButtonLauncher.WndProc is bypassed entirely.

**Impact**: Our WndProc-based interception ONLY works when ButtonLauncher itself has focus.
For child-focused scenarios, we rely entirely on the MouseWheel event subscription.

**Likelihood**: **HIGH** — in practice, focus is almost always on a child control (the
RootSiteControl/TextBox), not on the ButtonLauncher UserControl itself.

---

## 5. Root Cause Analysis

The most likely root cause is a combination of failure points, depending on the specific
scenario:

### Scenario A: Focus is on a child control inside ButtonLauncher

1. WM_MOUSEWHEEL → child control (RootSiteControl)
2. RootSiteControl processes the message through its WndProc chain
3. Eventually OnMouseWheel is called → raises MouseWheel event
4. HandleForwardMouseWheel fires → calls InvokeOnMouseWheel on DataTree
5. ScrollableControl.OnMouseWheel on DataTree checks `VScroll`
6. **If VScroll is true**: scrolling should work
7. **If VScroll is false**: no scrolling (but this is unlikely given visible scrollbar)

### Scenario B: Focus is NOT inside any ButtonLauncher

1. WM_MOUSEWHEEL → focused control elsewhere in DataTree
2. ButtonLauncher's code never runs
3. No forwarding occurs

### Most Probable Root Cause: **OnMouseWheel via reflection works correctly,
but the actual scrolling mechanism in ScrollableControl may behave differently
than expected in .NET Framework 4.8**

The .NET Framework 4.8 `ScrollableControl.OnMouseWheel` may have subtle differences
from the .NET Core source we analyzed. Additionally, the reflection-based approach
calls `OnMouseWheel` outside the normal WndProc processing context, which means:

- No Win32 message context (MSG structure, message pump state)
- `SetDisplayRectLocation` may trigger layout/paint events synchronously
- The scroll change may be undone by subsequent processing

### Recommended Fix: **IMessageFilter + Direct AutoScrollPosition manipulation**

The fix uses two layers:

**Layer 1 (primary): `IMessageFilter`** — Intercepts WM_MOUSEWHEEL at the application
message pump level, before any ContainerControl routing or focus-based dispatch.
Checks if the cursor is over any registered ButtonLauncher using screen coordinates.
If so, directly scrolls the parent DataTree via `AutoScrollPosition`.

```csharp
private sealed class WheelRedirector : IMessageFilter
{
    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != WM_MOUSEWHEEL) return false;
        Point cursor = Cursor.Position;
        foreach (var launcher in m_launchers)
        {
            Rectangle bounds = launcher.RectangleToScreen(launcher.ClientRectangle);
            if (bounds.Contains(cursor))
            {
                var target = launcher.FindMouseWheelTarget(); // DataTree
                int delta = (short)((long)m.WParam >> 16);
                launcher.ScrollTarget(target, delta);
                return true; // consumed
            }
        }
        return false;
    }
}
```

**Layer 2 (fallback): WndProc + MouseWheel event handlers** — Kept for robustness
and test support. The WndProc override catches any WM_MOUSEWHEEL sent directly to
the ButtonLauncher HWND. The MouseWheel event handlers catch events propagated
through the child control's OnMouseWheel chain.

**AutoScrollPosition manipulation** (both layers):
```csharp
int currentY = -dataTree.AutoScrollPosition.Y;
int maxScroll = Math.Max(0, dataTree.AutoScrollMinSize.Height - dataTree.ClientRectangle.Height);
int newY = Math.Max(0, Math.Min(currentY - delta, maxScroll));
dataTree.AutoScrollPosition = new Point(0, newY);
```

---

## 6. .NET Framework ScrollableControl.OnMouseWheel Source (Reference)

From the dotnet/winforms open-source (.NET Core, should be similar to .NET FW 4.8):

```csharp
protected override void OnMouseWheel(MouseEventArgs e)
{
    if (VScroll)
    {
        Rectangle client = ClientRectangle;
        int pos = -_displayRect.Y;
        int maxPos = -(client.Height - _displayRect.Height);
        pos = Math.Max(pos - e.Delta, 0);
        pos = Math.Min(pos, maxPos);
        SetDisplayRectLocation(_displayRect.X, -pos);
        SyncScrollbars(AutoScroll);
        if (e is HandledMouseEventArgs args)
            args.Handled = true;
    }
    else if (HScroll)
    {
        // ... similar horizontal scroll logic
    }

    // Always call base, which raises MouseWheel event
    base.OnMouseWheel(e);
}
```

Key observations:
- **`VScroll` is a protected property** set by `AdjustFormScrollbars`/`SetVisibleScrollbars`
  during layout. It reflects whether the vertical scrollbar is currently visible.
- **`SetDisplayRectLocation`** physically moves child controls and scrolls the window.
- **`SyncScrollbars`** updates scrollbar thumb position and range.
- **`e.Delta`** is used directly as pixel offset (no `SystemInformation.MouseWheelScrollLines`
  multiplication in this implementation).

---

## 7. DataTree Scroll Management

DataTree does NOT set `AutoScroll = true` explicitly. Instead, it manages scrolling through:

1. **`AutoScrollMinSize`** — Set in `OnLayout` (line 3283) to the total height of all slices.
   In .NET Framework, setting `AutoScrollMinSize` automatically sets `AutoScroll = true`.

2. **`AutoScrollPosition`** — Read throughout layout methods. Manually saved and restored
   in `OnLayout` to prevent `base.OnLayout` from resetting it.

3. **`OnLayout` override** (lines 3254-3310) — Runs up to 3 iterations:
   - Saves `AutoScrollPosition`
   - Calls `base.OnLayout` (which may change scroll position)
   - Restores position if changed
   - Calls `HandleLayout1` to position slices
   - Sets `AutoScrollMinSize` based on total slice height

4. **`WndProc` override** (line 4695) — Minimal: saves Y, delegates to base, logs if changed.

5. **No `OnMouseWheel` override** — Relies entirely on `ScrollableControl.OnMouseWheel`.
