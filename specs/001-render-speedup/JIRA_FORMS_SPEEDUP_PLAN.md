# JIRA Forms Speedup Plan

**Feature**: WinForms Rendering Performance (Plan A)
**Related**: `FAST_FORM_PLAN.md`, `FORMS_SPEEDUP_PLAN.md`
**Date**: January 2026

This document provides JIRA-ready epic and story definitions for implementing WinForms performance optimizations.

---

## Implementation Principles

### 1. Baseline Measurement MUST Come First (Story 0)
You can't track improvement without a baseline. This should be a **blocker** for all other work.

### 2. Dependency Ordering
49 x 8
I've created FORMS_SPEEDUP_PLAN.md with detailed implementation guidance for 10 WinForms optimizations. Here's the summary:

Top 10 WinForms Speedup Techniques (No Third-Party Packages)

Some optimizations enable or conflict with others:
```
Story 0: Baseline → blocks everything
Story 9 (Collapse-by-Default) → no dependencies, highest ROI
Story 1 (SuspendLayout) → no dependencies
Story 4 (Lazy Creation) → depends on having collapsible sections
Story 3 (Virtualization) → depends on baseline patterns from 4
```

### 3. Definition of Done Per Story
Each story needs:
- Performance target (e.g., "Complex entry load < 800ms")
- No visual regression
- No accessibility regression
- Tests passing

### 4. Spike Stories for Risky Items
Optimizations 3 (Virtualization) and 8 (Control Count Reduction) are high-risk. Consider spike stories first.

### 5. Feature Flags / Gradual Rollout
Ship optimizations behind flags so you can disable them if issues arise in production.

### 6. Validation/Bake Period
Add a final "Integration Validation" story to test all optimizations together.

---

## Epic Definition

### Title
`PERF: WinForms Rendering Speedup (Plan A)`

### Description
```markdown
## Goal
Improve rendering performance for entries with many custom fields by 60-80%
without changing the UI framework.

## Success Criteria
- Simple entry load: 500ms → 200ms (60% reduction)
- Complex entry load: 3000ms → 800ms (73% reduction)
- Custom fields expand: 2000ms → 300ms (85% reduction)
- No visual regressions
- No accessibility regressions

## Reference
- specs/010-advanced-entry-view/FAST_FORM_PLAN.md
- specs/010-advanced-entry-view/FORMS_SPEEDUP_PLAN.md

## Test Entries
- Simple: [define or create test entry with 3 senses, 0 custom fields]
- Medium: [10 senses, 10 custom fields]
- Complex: [20 senses, 50 custom fields, nested subsenses]
```

### Labels
- `performance`
- `winforms`
- `plan-a`

---

## Story Definitions

---

### Story 0: Performance Baseline & Test Harness

| Field | Value |
|-------|-------|
| **Title** | PERF-0: Performance Baseline & Test Harness |
| **Type** | Story |
| **Priority** | Highest |
| **Points** | 5-8 |
| **Blocks** | All other stories in this epic |
| **Labels** | `performance`, `testing`, `infrastructure` |

#### Description
```markdown
## Summary
Create automated performance tests that measure rendering times for
representative entry scenarios. These will be run before/after each
optimization to validate improvement.

## Acceptance Criteria
- [ ] Test harness can run from CLI (for CI integration)
- [ ] Measures: load time, memory delta, handle count, paint time
- [ ] Test scenarios defined (see below)
- [ ] Baseline numbers recorded in wiki/confluence
- [ ] Tests are repeatable (±5% variance)

## Test Scenarios
1. **Simple Entry Load**: Entry with 3 senses, 0 custom fields
2. **Medium Entry Load**: Entry with 10 senses, 10 custom fields
3. **Complex Entry Load**: Entry with 20 senses, 50 custom fields
4. **Custom Fields Expand**: Expand custom fields section on complex entry
5. **Scroll Through Senses**: Scroll through 50 senses rapidly
6. **Navigate Between Entries**: Switch between 10 entries rapidly
7. **Type in Field**: Keystroke latency in a text field
8. **Browse View Scroll**: Scroll through 1000 entries in browse view

## Technical Notes
- Use Stopwatch for timing
- Use Process.HandleCount for handle tracking
- Use GC.GetTotalMemory for memory tracking
- Run 3 iterations, take median to reduce variance
- Store results in standardized JSON format for comparison

## Sample Test Code
```csharp
public class PerformanceMetrics
{
    public static PerformanceResult MeasureEntryLoad(Action loadAction)
    {
        // Warm up
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var sw = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var initialHandles = Process.GetCurrentProcess().HandleCount;

        loadAction();

        sw.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var finalHandles = Process.GetCurrentProcess().HandleCount;

        return new PerformanceResult
        {
            ElapsedMs = sw.ElapsedMilliseconds,
            MemoryDeltaKB = (finalMemory - initialMemory) / 1024,
            HandleDelta = finalHandles - initialHandles
        };
    }
}
```

## Deliverables
- [ ] PerformanceTestHarness project/class
- [ ] Test data entries (or script to create them)
- [ ] Baseline results document
- [ ] CI integration (optional, can be follow-up)
```

---

### Story 1: SuspendLayout/ResumeLayout Batching

| Field | Value |
|-------|-------|
| **Title** | PERF-1: SuspendLayout/ResumeLayout Batching |
| **Type** | Story |
| **Priority** | High |
| **Points** | 3 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `winforms`, `low-risk` |

#### Description
```markdown
## Summary
Wrap control-adding loops in SuspendLayout/ResumeLayout to reduce
layout passes from O(N) to O(1).

## Problem
Every time a control property changes (Location, Size, Visible, etc.),
WinForms triggers a layout pass. When adding/modifying multiple controls,
this causes N layout passes instead of 1.

## Solution
```csharp
// Before (slow)
foreach (var field in fields)
    panel.Controls.Add(CreateFieldEditor(field));

// After (fast)
panel.SuspendLayout();
try
{
    foreach (var field in fields)
        panel.Controls.Add(CreateFieldEditor(field));
}
finally
{
    panel.ResumeLayout(true);
}
```

## Acceptance Criteria
- [ ] Audit finds all control-adding loops
- [ ] Loops wrapped in SuspendLayout/ResumeLayout
- [ ] No visual regressions
- [ ] Performance improvement measured and documented
- [ ] Helper method created for reuse

## Target Files (to audit)
- Src/LexText/LexTextControls/*
- Src/Common/Controls/DetailControls/*
- Src/xWorks/*

## Expected Impact
20-40% faster form load times

## Helper Code
```csharp
public static class LayoutHelper
{
    public static void BatchUpdate(Control control, Action updateAction)
    {
        control.SuspendLayout();
        try
        {
            updateAction();
        }
        finally
        {
            control.ResumeLayout(true);
        }
    }
}
```

## Rollback
- Remove SuspendLayout/ResumeLayout calls
- Risk: Low (this is standard WinForms practice)
```

---

### Story 2: Double Buffering

| Field | Value |
|-------|-------|
| **Title** | PERF-2: Enable Double Buffering |
| **Type** | Story |
| **Priority** | High |
| **Points** | 2 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `winforms`, `low-risk`, `visual` |

#### Description
```markdown
## Summary
Enable double buffering on key panels to eliminate visual flicker during updates.

## Problem
Default WinForms painting draws directly to screen, causing visible flicker
during updates. Each control paints separately, showing intermediate states.

## Solution
Create a base class with double buffering enabled:

```csharp
public class FwDoubleBufferedPanel : Panel
{
    public FwDoubleBufferedPanel()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true);
        UpdateStyles();
    }
}
```

## Acceptance Criteria
- [ ] FwDoubleBufferedPanel base class created
- [ ] Applied to entry editor panels
- [ ] No visual regressions (some controls render differently with double buffering)
- [ ] Flicker eliminated in entry switching
- [ ] Tested on high-DPI displays

## Extension Method for Existing Controls
```csharp
public static class ControlExtensions
{
    public static void EnableDoubleBuffering(this Control control)
    {
        typeof(Control)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(control, true);
    }
}
```

## Expected Impact
- Eliminates visual flicker
- No performance improvement per se, but much smoother perceived performance

## Rollback
- Remove double buffering flags
- Risk: Low
```

---

### Story 3: Manual Control Virtualization

| Field | Value |
|-------|-------|
| **Title** | PERF-3: Manual Control Virtualization |
| **Type** | Story |
| **Priority** | Medium |
| **Points** | 8-13 |
| **Depends on** | Story 0, Story 4 |
| **Labels** | `performance`, `winforms`, `high-risk`, `needs-spike` |

#### Description
```markdown
## Summary
Implement manual virtualization for long lists (senses, examples)
to render only visible items instead of all items.

## Problem
Creating 500+ controls for a long list is expensive. Each control allocates
memory, registers event handlers, and participates in layout.

## Spike Questions (DO SPIKE FIRST - 3 points)
- [ ] Which scroll containers have 20+ items typically?
- [ ] Can we use ListView VirtualMode or need custom implementation?
- [ ] What's the control recycling strategy?
- [ ] How does this interact with existing selection logic?
- [ ] What's the estimated height strategy for scroll bar sizing?

## Solution Approach
```csharp
public class VirtualFieldList : ScrollableControl
{
    private readonly List<FieldData> _allFields = new();
    private readonly Dictionary<int, Control> _visibleControls = new();
    private readonly Queue<Control> _controlPool = new();

    protected override void OnScroll(ScrollEventArgs e)
    {
        base.OnScroll(e);
        UpdateVisibleControls();
    }

    private void UpdateVisibleControls()
    {
        int firstVisible = VerticalScroll.Value / RowHeight;
        int lastVisible = (VerticalScroll.Value + Height) / RowHeight;

        RecycleInvisibleControls(firstVisible, lastVisible);
        EnsureVisibleControls(firstVisible, lastVisible);
    }
}
```

## Acceptance Criteria
- [ ] Spike completed and approach documented
- [ ] Sense list virtualized (only visible items rendered)
- [ ] Scroll performance smooth (< 16ms per frame)
- [ ] Selection/navigation still works correctly
- [ ] Keyboard navigation works
- [ ] No memory leaks from control pooling
- [ ] Accessibility still works

## Expected Impact
5-10× improvement for entries with many senses

## Rollback
- Feature flag to disable virtualization
- Risk: High (complex implementation, affects selection/navigation)
```

---

### Story 4: Lazy Control Creation (Tabs/Sections)

| Field | Value |
|-------|-------|
| **Title** | PERF-4: Lazy Control Creation for Tabs/Sections |
| **Type** | Story |
| **Priority** | High |
| **Points** | 5 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `winforms`, `medium-risk` |

#### Description
```markdown
## Summary
Create LazyTabPage and defer content creation until first access.

## Problem
Forms create all controls during InitializeComponent(), even for tabs/panels
that may never be viewed. User may only look at "General" tab, but
"Etymology", "History", "Custom Fields" tabs are fully built.

## Solution
```csharp
public class LazyTabPage : TabPage
{
    private readonly Func<Control> _contentFactory;
    private bool _isContentCreated;

    public LazyTabPage(string text, Func<Control> contentFactory)
    {
        Text = text;
        _contentFactory = contentFactory;
    }

    public void EnsureContentCreated()
    {
        if (!_isContentCreated)
        {
            SuspendLayout();
            try
            {
                var content = _contentFactory();
                content.Dock = DockStyle.Fill;
                Controls.Add(content);
                _isContentCreated = true;
            }
            finally
            {
                ResumeLayout(true);
            }
        }
    }
}
```

## Acceptance Criteria
- [ ] LazyTabPage class implemented
- [ ] Applied to Etymology, History, References tabs
- [ ] First-access latency acceptable (< 200ms)
- [ ] Content state preserved when switching tabs
- [ ] Tab switching hooks in place

## Target Areas
- Tab controls in entry editor
- Expandable panels (Etymology, Custom Fields)
- Dialog pages with multiple tabs

## Expected Impact
50%+ faster initial load for complex editors

## Rollback
- Replace LazyTabPage with regular TabPage
- Risk: Low-Medium
```

---

### Story 5: DisplayCommand Caching Enhancement

| Field | Value |
|-------|-------|
| **Title** | PERF-5: DisplayCommand Caching Enhancement |
| **Type** | Story |
| **Priority** | Medium |
| **Points** | 5 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `xmlviews`, `medium-risk` |

#### Description
```markdown
## Summary
Extend XmlVc DisplayCommand caching to pre-compile layout operations
for faster repeated displays.

## Problem
XmlVc.ProcessFrag walks the Parts/Layout XML tree for every object displayed.
Even with existing DisplayCommand identity caching, there's overhead in
resolution and tree walking.

## Current Caching
```csharp
// XmlVc already caches DisplayCommand by identity
internal Dictionary<DisplayCommand, int> m_displayCommandToId;
internal Dictionary<int, DisplayCommand> m_idToDisplayCommand;
```

## Enhanced Solution
```csharp
public class EnhancedLayoutCache
{
    private readonly Dictionary<LayoutKey, CompiledLayout> _compiledLayouts = new();

    public record LayoutKey(int ClassId, string LayoutName, int ConfigVersion);

    public class CompiledLayout
    {
        public List<DisplayOperation> Operations { get; }
        public List<FieldBinding> Bindings { get; }
        public int EstimatedHeight { get; }
    }

    public CompiledLayout GetOrCompile(int classId, string layoutName)
    {
        var key = new LayoutKey(classId, layoutName, GetConfigVersion());

        if (!_compiledLayouts.TryGetValue(key, out var compiled))
        {
            compiled = CompileLayout(classId, layoutName);
            _compiledLayouts[key] = compiled;
        }

        return compiled;
    }
}
```

## Acceptance Criteria
- [ ] Enhanced cache key includes config version
- [ ] Cache hit rate > 95% for repeated layouts
- [ ] Cache invalidation on config change works
- [ ] Performance improvement measured and documented
- [ ] No regression in layout correctness

## Expected Impact
30-50% faster redraw for repeated layouts

## Rollback
- Feature flag to use original caching
- Risk: Medium (touches XmlVc internals)
```

---

### Story 6: Background Data Loading

| Field | Value |
|-------|-------|
| **Title** | PERF-6: Background Data Loading |
| **Type** | Story |
| **Priority** | High |
| **Points** | 5 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `async`, `medium-risk` |

#### Description
```markdown
## Summary
Move data loading off UI thread using async/await pattern for
non-blocking entry display.

## Problem
UI thread blocks while loading data from LCModel, causing freezes
during navigation.

## Solution
```csharp
private async void OnEntrySelected(int hvo)
{
    ShowLoadingState();

    try
    {
        // Load data on background thread
        var entryData = await Task.Run(() => LoadEntryDataAsync(hvo));

        // Update UI on UI thread
        DisplayEntry(entryData);
    }
    finally
    {
        HideLoadingState();
    }
}

private EntryDataPackage LoadEntryDataAsync(int hvo)
{
    var entry = _cache.ServiceLocator
        .GetInstance<ILexEntryRepository>()
        .GetObject(hvo);

    return new EntryDataPackage
    {
        Hvo = hvo,
        Headword = entry.HeadWord?.Text,
        Senses = entry.SensesOS.Select(s => new SenseData
        {
            Hvo = s.Hvo,
            Gloss = s.Gloss?.BestAnalysisAlternative?.Text,
            Definition = s.Definition?.BestAnalysisAlternative?.Text,
        }).ToList(),
    };
}
```

## Progressive Loading Pattern
```csharp
private async void OnEntrySelected(int hvo)
{
    // Phase 1: Critical data (headword, POS) - fast
    var criticalData = await Task.Run(() => LoadCriticalDataAsync(hvo));
    DisplayCriticalSection(criticalData);

    // Phase 2: Primary content (senses) - medium
    var sensesData = await Task.Run(() => LoadSensesDataAsync(hvo));
    DisplaySensesSection(sensesData);

    // Phase 3: Secondary content - defer until expanded
}
```

## Acceptance Criteria
- [ ] Entry loading shows skeleton/loading state immediately
- [ ] Data loads on background thread
- [ ] UI updates progressively (critical → primary → secondary)
- [ ] Cancellation works when switching entries quickly
- [ ] No race conditions or cross-thread exceptions
- [ ] LCModel thread safety verified

## Expected Impact
Perceived performance improves dramatically (UI never freezes)

## Rollback
- Feature flag to use synchronous loading
- Risk: Medium (threading concerns)
```

---

### Story 7: Smart Invalidation (Dirty Rectangles)

| Field | Value |
|-------|-------|
| **Title** | PERF-7: Smart Invalidation |
| **Type** | Story |
| **Priority** | Medium |
| **Points** | 5 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `winforms`, `medium-risk` |

#### Description
```markdown
## Summary
Replace broad Invalidate() calls with targeted field-level invalidation.

## Problem
When data changes, entire panels are invalidated and repainted, even if
only one field changed.

## Solution
```csharp
public class SmartInvalidationPanel : Panel
{
    private readonly Dictionary<string, Rectangle> _fieldBounds = new();

    public void RegisterField(string fieldKey, Control control)
    {
        _fieldBounds[fieldKey] = control.Bounds;
    }

    public void InvalidateField(string fieldKey)
    {
        if (_fieldBounds.TryGetValue(fieldKey, out var bounds))
        {
            // Only invalidate the specific field region
            Invalidate(bounds);
        }
    }
}
```

## Acceptance Criteria
- [ ] Field bounds tracked by key
- [ ] InvalidateField(key) only repaints affected region
- [ ] No visual artifacts from partial repaint
- [ ] Typing latency improved (measured)
- [ ] Property change triggers targeted invalidation

## Expected Impact
2-3× faster updates for single-field changes

## Rollback
- Call full Invalidate() instead of targeted
- Risk: Medium (potential visual artifacts)
```

---

### Story 8: Control Count Reduction (Owner-Draw)

| Field | Value |
|-------|-------|
| **Title** | PERF-8: Control Count Reduction |
| **Type** | Story |
| **Priority** | Low |
| **Points** | 13+ |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `winforms`, `high-risk`, `needs-spike` |

#### Description
```markdown
## Summary
Replace multiple controls with owner-draw composite controls for
read-heavy panels.

## Problem
Each WinForms control has overhead: window handle, message pump
participation, layout calculations. Hundreds of controls compound these costs.

## Spike Questions (DO SPIKE FIRST - 5 points)
- [ ] Which panels are read-heavy vs edit-heavy?
- [ ] What's the accessibility impact of owner-draw?
- [ ] Can we use ListView owner-draw for field lists?
- [ ] What's the edit-mode overlay strategy?
- [ ] How do we handle focus/tab navigation?

## Solution Approach
```csharp
public class FieldStrip : Control
{
    private readonly List<FieldData> _fields = new();

    protected override void OnPaint(PaintEventArgs e)
    {
        int y = 0;
        foreach (var field in _fields)
        {
            // Draw label (no Label control needed)
            e.Graphics.DrawString(field.Label, Font, Brushes.Gray, 0, y);

            // Draw value (no TextBox control needed for read-only)
            e.Graphics.DrawString(field.Value, Font, Brushes.Black, 100, y);

            y += 24;
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        int clickedIndex = e.Y / 24;
        if (clickedIndex < _fields.Count)
            EnterEditMode(clickedIndex); // Create single TextBox overlay
    }
}
```

## Acceptance Criteria
- [ ] Spike completed and approach documented
- [ ] Read-heavy panel converted to owner-draw
- [ ] Edit mode works via overlay control
- [ ] Accessibility verified with screen reader
- [ ] Handle count reduced by 50%+
- [ ] Visual appearance matches original

## Expected Impact
20-30% faster layout, major handle reduction

## Rollback
- Feature flag to use traditional controls
- Risk: High (complex, accessibility concerns)
```

---

### Story 9: Custom Fields Collapse-by-Default 🎯

| Field | Value |
|-------|-------|
| **Title** | PERF-9: Custom Fields Collapse-by-Default |
| **Type** | Story |
| **Priority** | **HIGHEST** |
| **Points** | 3 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `winforms`, `low-risk`, `high-impact` |

#### Description
```markdown
## Summary
Collapse custom fields section by default, create content only on expand.
THIS IS THE HIGHEST ROI OPTIMIZATION.

## Problem
Custom fields are the #1 contributor to UI complexity. An entry with
50 custom fields renders 50+ controls immediately, even though users
rarely view all of them.

## Solution
```csharp
public class CollapsibleSection : UserControl
{
    private readonly Func<Control> _contentFactory;
    private bool _isExpanded;
    private Control _content;

    public CollapsibleSection(string header, int itemCount, Func<Control> contentFactory)
    {
        _contentFactory = contentFactory;

        var headerPanel = new Panel { Height = 24, Dock = DockStyle.Top };
        var expandButton = new Button { Text = "▶", Width = 24 };
        var label = new Label { Text = $"{header} ({itemCount})", Left = 30 };

        headerPanel.Controls.Add(expandButton);
        headerPanel.Controls.Add(label);
        Controls.Add(headerPanel);

        expandButton.Click += OnExpandClick;
    }

    private void Expand()
    {
        if (_content == null)
        {
            // LAZY: Create content only on first expand
            _content = _contentFactory();
            _content.Dock = DockStyle.Fill;
        }

        SuspendLayout();
        Controls.Add(_content);
        _isExpanded = true;
        ResumeLayout(true);
    }
}
```

## Acceptance Criteria
- [ ] CollapsibleSection component created
- [ ] Custom fields collapsed by default
- [ ] Lazy content creation on first expand
- [ ] Expand/collapse state persisted per user (optional)
- [ ] Badge shows field count when collapsed
- [ ] Expand animation smooth (optional)

## Why Highest Priority
| Factor | Rating |
|--------|--------|
| ROI | 80%+ improvement for heavy entries |
| Risk | Low (UI change is minimal) |
| Effort | Low (3 points) |
| Dependencies | Only baseline needed |

## Expected Impact
For an entry with 50 custom fields:
- **Before**: 50 controls created on load
- **After**: 1 header control; 50 controls only if user expands
- **Savings**: ~98% reduction in custom field rendering cost

## Rollback
- Set default state to expanded
- Risk: Very Low
```

---

### Story 10: XML/Layout Resolution Caching

| Field | Value |
|-------|-------|
| **Title** | PERF-10: XML/Layout Resolution Caching |
| **Type** | Story |
| **Priority** | Medium |
| **Points** | 3 |
| **Depends on** | Story 0 |
| **Labels** | `performance`, `xmlviews`, `low-risk` |

#### Description
```markdown
## Summary
Enhance LayoutCache with pre-warming and better cache keys.

## Problem
LayoutCache.GetNodeForPart walks class hierarchy and applies overrides
each time. For identical lookups, this is redundant.

## Solution
```csharp
public class EnhancedLayoutCache
{
    private readonly Dictionary<LayoutCacheKey, XmlNode> _cache = new();

    private record LayoutCacheKey(
        int ClassId,
        string LayoutName,
        bool IncludeLayouts,
        int ConfigVersion,
        string UserOverrideHash
    );

    public XmlNode GetNodeForPart(int classId, string layoutName, bool includeLayouts)
    {
        var key = new LayoutCacheKey(
            classId, layoutName, includeLayouts,
            GetConfigVersion(), GetUserOverrideHash());

        if (!_cache.TryGetValue(key, out var node))
        {
            node = ComputeNodeForPart(classId, layoutName, includeLayouts);
            _cache[key] = node;
        }

        return node;
    }
}
```

## Pre-Warming at Startup
```csharp
public async Task WarmCacheAsync()
{
    var commonLayouts = new[]
    {
        (typeof(ILexEntry), "publishRoot"),
        (typeof(ILexSense), "Normal"),
        (typeof(ILexExampleSentence), "Normal"),
    };

    await Task.Run(() =>
    {
        foreach (var (type, layout) in commonLayouts)
            _layoutCache.GetNodeForPart(GetClassId(type), layout, true);
    });
}
```

## Acceptance Criteria
- [ ] Cache pre-warmed at startup for common layouts
- [ ] Cache key includes user override hash
- [ ] Cache invalidation on config change
- [ ] Hit rate logged for monitoring
- [ ] No regression in layout correctness

## Expected Impact
30-40% faster view construction for repeated layouts

## Rollback
- Disable pre-warming, use original cache
- Risk: Low
```

---

### Story 11: Integration Validation & Documentation

| Field | Value |
|-------|-------|
| **Title** | PERF-11: Integration Validation & Documentation |
| **Type** | Story |
| **Priority** | High |
| **Points** | 5 |
| **Depends on** | All other stories |
| **Labels** | `performance`, `documentation`, `testing` |

#### Description
```markdown
## Summary
Validate all optimizations work together, update documentation,
and prepare for release.

## Acceptance Criteria
- [ ] All optimizations enabled together
- [ ] Full test harness passes with all optimizations
- [ ] Performance targets met (see epic success criteria)
- [ ] No unexpected interactions between optimizations
- [ ] AGENTS.md updated for changed modules
- [ ] Architecture docs updated
- [ ] Release notes drafted
- [ ] Feature flags documented
- [ ] Rollback procedures documented

## Validation Checklist
- [ ] Simple entry load < 200ms
- [ ] Complex entry load < 800ms
- [ ] Custom fields expand < 300ms
- [ ] No visual regressions (screenshot comparison)
- [ ] No accessibility regressions (screen reader test)
- [ ] Memory usage acceptable
- [ ] Handle count reduced

## Documentation Updates
- [ ] FORMS_SPEEDUP_PLAN.md - mark completed items
- [ ] FAST_FORM_PLAN.md - update with results
- [ ] Src/Common/Controls/AGENTS.md
- [ ] Src/LexText/LexTextControls/AGENTS.md
- [ ] Release notes

## Rollback
- N/A (documentation story)
```

---

## Sprint Plan

### Sprint 1: Foundation (Weeks 1-2)
| Story | Points | Notes |
|-------|--------|-------|
| Story 0: Baseline & Test Harness | 5-8 | **BLOCKER** |
| Story 9: Collapse-by-Default | 3 | Highest ROI, start after baseline |
| Story 1: SuspendLayout Batching | 3 | Quick win |
| **Sprint Total** | 11-14 | |

### Sprint 2: Quick Wins (Weeks 3-4)
| Story | Points | Notes |
|-------|--------|-------|
| Story 2: Double Buffering | 2 | Visual improvement |
| Story 4: Lazy Control Creation | 5 | Tab/section deferral |
| Story 10: Layout Caching | 3 | Cache enhancement |
| **Sprint Total** | 10 | |

### Sprint 3: Async & Invalidation (Weeks 5-6)
| Story | Points | Notes |
|-------|--------|-------|
| Story 6: Background Data Loading | 5 | Non-blocking UI |
| Story 7: Smart Invalidation | 5 | Targeted repaints |
| **Sprint Total** | 10 | |

### Sprint 4: Complex Work (Weeks 7-8)
| Story | Points | Notes |
|-------|--------|-------|
| Story 3: Virtualization (spike) | 3 | Spike first |
| Story 3: Virtualization (impl) | 8-10 | If spike successful |
| Story 5: DisplayCommand Caching | 5 | XmlVc enhancement |
| **Sprint Total** | 16-18 | |

### Sprint 5: Polish (Weeks 9-10)
| Story | Points | Notes |
|-------|--------|-------|
| Story 8: Control Count Reduction | 5-13 | If time/value |
| Story 11: Integration Validation | 5 | Final validation |
| **Sprint Total** | 10-18 | |

---

## Feature Flags

Implement feature flags for gradual rollout and easy rollback:

```csharp
public static class PerfFlags
{
    // Check environment variables or config file
    public static bool UseCollapseByDefault =>
        GetFlag("FW_PERF_COLLAPSE_DEFAULT", defaultValue: true);

    public static bool UseBackgroundLoading =>
        GetFlag("FW_PERF_ASYNC_LOAD", defaultValue: true);

    public static bool UseVirtualization =>
        GetFlag("FW_PERF_VIRTUALIZATION", defaultValue: false); // Off until stable

    public static bool UseSmartInvalidation =>
        GetFlag("FW_PERF_SMART_INVALIDATE", defaultValue: true);

    public static bool UseEnhancedLayoutCache =>
        GetFlag("FW_PERF_LAYOUT_CACHE", defaultValue: true);

    private static bool GetFlag(string name, bool defaultValue)
    {
        var env = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(env))
            return defaultValue;
        return env != "0" && env.ToLower() != "false";
    }
}
```

---

## Monitoring & Telemetry (Optional Follow-Up)

Track real-world performance after deployment:

```csharp
public static class PerfTelemetry
{
    public static void LogEntryLoad(int hvo, long elapsedMs, int senseCount, int customFieldCount)
    {
        // Log to file or telemetry service
        var entry = new
        {
            Timestamp = DateTime.UtcNow,
            Event = "EntryLoad",
            ElapsedMs = elapsedMs,
            SenseCount = senseCount,
            CustomFieldCount = customFieldCount,
            // Anonymized - no PII
        };

        Logger.LogPerformance(entry);
    }
}
```

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Virtualization breaks selection | Medium | High | Spike first, feature flag |
| Async loading causes race conditions | Medium | High | Cancellation tokens, proper sync |
| Owner-draw breaks accessibility | High | High | Spike first, screen reader testing |
| Double buffering visual artifacts | Low | Medium | Test on multiple DPI settings |
| Cache invalidation bugs | Medium | Medium | Clear cache on config change |

---

## Success Metrics

### Performance Targets
| Metric | Current (est.) | Target | Measurement Method |
|--------|----------------|--------|-------------------|
| Simple entry load | 500ms | 200ms | Test harness |
| Complex entry load | 3000ms | 800ms | Test harness |
| Custom fields expand | 2000ms | 300ms | Test harness |
| Memory per entry | 50MB | 20MB | Process memory |
| Handle count | 500+ | 100 | Process handles |
| Scroll FPS | 15 | 60 | Frame timing |

### Quality Gates
- No P1/P2 bugs introduced
- Accessibility audit passes
- Visual regression tests pass
- All existing tests pass

---

## Appendix: JIRA Import Format

If your JIRA supports CSV import, here's a simplified format:

```csv
Summary,Issue Type,Priority,Story Points,Labels,Description
"PERF-0: Performance Baseline & Test Harness",Story,Highest,8,"performance,testing,infrastructure","Create automated performance tests..."
"PERF-1: SuspendLayout/ResumeLayout Batching",Story,High,3,"performance,winforms,low-risk","Wrap control-adding loops..."
"PERF-2: Enable Double Buffering",Story,High,2,"performance,winforms,low-risk,visual","Enable double buffering on key panels..."
"PERF-3: Manual Control Virtualization",Story,Medium,13,"performance,winforms,high-risk,needs-spike","Implement manual virtualization..."
"PERF-4: Lazy Control Creation",Story,High,5,"performance,winforms,medium-risk","Create LazyTabPage and defer content..."
"PERF-5: DisplayCommand Caching Enhancement",Story,Medium,5,"performance,xmlviews,medium-risk","Extend XmlVc DisplayCommand caching..."
"PERF-6: Background Data Loading",Story,High,5,"performance,async,medium-risk","Move data loading off UI thread..."
"PERF-7: Smart Invalidation",Story,Medium,5,"performance,winforms,medium-risk","Replace broad Invalidate() calls..."
"PERF-8: Control Count Reduction",Story,Low,13,"performance,winforms,high-risk,needs-spike","Replace multiple controls with owner-draw..."
"PERF-9: Custom Fields Collapse-by-Default",Story,Highest,3,"performance,winforms,low-risk,high-impact","Collapse custom fields section by default..."
"PERF-10: XML/Layout Resolution Caching",Story,Medium,3,"performance,xmlviews,low-risk","Enhance LayoutCache with pre-warming..."
"PERF-11: Integration Validation & Documentation",Story,High,5,"performance,documentation,testing","Validate all optimizations work together..."
```

