# HWND Cost Analysis: Why Window Handles Are Expensive

## Sources

- [Microsoft Docs: About Window Classes](https://learn.microsoft.com/en-us/windows/win32/winmsg/about-window-classes) — official API documentation
- [Microsoft Docs: System Quotas](https://learn.microsoft.com/en-us/windows/win32/sysinfo/kernel-objects) — USER object limits
- [Raymond Chen: "Windows are not cheap objects"](https://devblogs.microsoft.com/oldnewthing/20050315-00/?p=36183) — The Old New Thing (2005)
- [Raymond Chen: "Windowless controls are not magic"](https://devblogs.microsoft.com/oldnewthing/20050211-00/?p=36473) — The Old New Thing (2005)

---

## What Is an HWND?

An `HWND` is a handle to a Win32 **window object** — a kernel-managed USER object that represents any visible (or invisible) rectangle in the Windows desktop compositor. Every WinForms `Control` creates exactly one HWND (via `CreateWindowEx`) the first time its `Handle` property is accessed or when it's added to a parent's `Controls` collection.

## Per-HWND Kernel Cost

Each HWND allocates:

| Resource | Cost | Notes |
|----------|------|-------|
| **Kernel USER object slot** | Entry in per-process USER handle table | Subject to 10,000-handle default limit |
| **Window class memory** | Shared across instances of same class | Pre-amortized for WinForms controls |
| **Window instance data** | ~800+ bytes kernel desktop heap | Includes `WNDCLASS` pointer, styles, position rect, parent/child/sibling pointers, window procedure pointer |
| **Message queue attachment** | Thread affinity binding | All child windows share the parent's message queue thread |
| **Z-order list entry** | doubly-linked list node | Maintained by the window manager for all siblings |

## Why HWNDs Get Expensive at Scale

### 1. O(N) Message Broadcasts

When the parent window resizes, moves, or changes visibility, Windows sends `WM_SIZE`, `WM_MOVE`, `WM_SHOWWINDOW`, `WM_WINDOWPOSCHANGING`/`WM_WINDOWPOSCHANGED` to **every child HWND**. For a DataTree with 253 slices × 6 HWNDs = 1,518 child windows, a single `OnLayout` triggers 1,518+ `WM_WINDOWPOSCHANGED` messages plus internal repaints.

### 2. Handle Table Pressure

The default process-wide USER handle limit is 10,000 (configurable via `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows\USERProcessHandleQuota`). FieldWorks, as a large desktop application, also uses handles for menus, icons, cursors, and other windows beyond DataTree. With multiple views open, approaching 5,000-6,000 handles is feasible and begins degrading system performance.

### 3. Desktop Heap Exhaustion

Each Windows desktop session has a fixed-size kernel desktop heap (default 48MB for interactive desktops). Every HWND, menu, hook, and string consumes from this shared pool. Exhaustion produces `ERROR_NOT_ENOUGH_MEMORY` from `CreateWindowEx` — not per-process, but per-session. On terminal server / multi-user configurations (relevant for SIL field deployments), this limit bites harder.

### 4. Z-Order and Hit-Testing

The window manager maintains a Z-ordered doubly-linked list of sibling windows. Operations like `BringWindowToTop`, `SetWindowPos`, and mouse hit-testing walk this list. With 1,500 siblings, hit-testing and focus-change operations degrade noticeably.

### 5. Painting Coordination

`WM_PAINT` is only sent to windows that have an invalid region. But `InvalidateRect` on a parent propagates to all child windows whose rects intersect the invalid region. With 1,500 children, the clipping region management alone becomes a measurable cost.

## Raymond Chen's Analysis

From ["Windows are not cheap objects"](https://devblogs.microsoft.com/oldnewthing/20050315-00/?p=36183):

> "Each window costs you a window handle, a window class, ownership and parent/child tracking, message dispatching, and all the other overhead that comes with being a window... If you have a list with 30,000 items, you do not want to create 30,000 windows. You want one window that draws 30,000 items."

From ["Windowless controls are not magic"](https://devblogs.microsoft.com/oldnewthing/20050211-00/?p=36473):

> "Windowless controls are a performance optimization. Instead of having each sub-element be its own window, you have one window that manages all the sub-elements, tracking focus, hit-testing, and painting itself."

## Current FieldWorks HWND Tree Per Slice

```
Slice (UserControl)                    ← 1 HWND
└── SplitContainer                     ← 1 HWND
    ├── SplitterPanel (Panel1)         ← 1 HWND
    │   └── SliceTreeNode (UserControl) ← 1 HWND
    └── SplitterPanel (Panel2)         ← 1 HWND
        └── [Content Control]          ← 1+ HWNDs
            ├── RootSiteControl        ← 1 HWND (ViewSlice family)
            ├── ButtonLauncher         ← 2+ HWNDs (Reference slices: control + button)
            ├── TextBox / ComboBox     ← 1 HWND (FieldSlice family)
            └── Label / Button         ← 1 HWND (stateless slices)
```

**Minimum per slice: 6 HWNDs.** ButtonLauncher-based slices: 7-8. SummarySlice (Panel + RootSite + buttons): 9-10.

## Quantified Impact

| Scenario | Slices | HWNDs (est.) | Allocation Time |
|----------|--------|-------------|-----------------|
| Simple list item | 5-10 | 30-60 | Negligible |
| Typical lexical entry | 40-80 | 240-480 | ~50-100ms |
| Complex entry (many senses) | 100-150 | 600-900 | ~200-400ms |
| Pathological entry (253 slices) | 253 | 1,518-2,000 | ~500-800ms |

The pathological case is the target of this optimization. Users switching between entries pay the full HWND allocation cost on every navigation.
