# WinForms Speedup Plan (Plan A)

**Feature**: Advanced Entry Avalonia View
**Related**: `FAST_FORM_PLAN.md`, `presentation-ir-research.md`
**Date**: January 2026

This document details specific WinForms optimization techniques that can be applied to FieldWorks **without third-party packages**, focusing on recoding and architectural improvements.

---

## Executive Summary

These 10 optimizations target the key performance bottlenecks identified in the Views architecture research. Combined, they can achieve **2-5× improvement** in perceived performance while maintaining UI compatibility.

| # | Optimization | Expected Impact | Effort | Risk |
|---|-------------|-----------------|--------|------|
| 1 | SuspendLayout/ResumeLayout Batching | 20-40% faster form load | Low | Low |
| 2 | Double Buffering | Eliminates flicker | Low | Low |
| 3 | Control Virtualization (Manual) | 5-10× for large lists | Medium | Medium |
| 4 | Lazy Control Creation | 50%+ faster initial load | Medium | Low |
| 5 | DisplayCommand Caching | 30-50% faster redraw | Medium | Medium |
| 6 | Background Data Loading | Non-blocking UI | Medium | Medium |
| 7 | Smart Invalidation | 2-3× faster updates | Medium | Medium |
| 8 | Control Count Reduction | 20-30% faster layout | Medium | Low |
| 9 | Custom Field Collapse-by-Default | 80%+ faster for heavy entries | Low | Low |
| 10 | XML/Layout Resolution Caching | 30-40% faster view construction | Medium | Low |

---

## Optimization 1: SuspendLayout/ResumeLayout Batching

### Problem
Every time a control property changes (Location, Size, Visible, etc.), WinForms triggers a layout pass. When adding/modifying multiple controls, this causes N layout passes instead of 1.

### Current Behavior (Slow)
```csharp
// Each AddControl triggers layout
panel.Controls.Add(label1);      // Layout pass 1
panel.Controls.Add(textBox1);    // Layout pass 2
panel.Controls.Add(label2);      // Layout pass 3
panel.Controls.Add(textBox2);    // Layout pass 4
// ... 50 more controls = 50 more layout passes
```

### Optimized Approach
```csharp
panel.SuspendLayout();
try
{
    panel.Controls.Add(label1);
    panel.Controls.Add(textBox1);
    panel.Controls.Add(label2);
    panel.Controls.Add(textBox2);
    // ... 50 more controls - NO intermediate layout passes
}
finally
{
    panel.ResumeLayout(performLayout: true); // Single layout pass
}
```

### Where to Apply in FieldWorks

**High-Impact Locations**:
- `Src/LexText/LexTextControls/` - Entry editor panels
- `Src/Common/Controls/DetailControls/` - Detail view construction
- Any code that builds UI from Parts/Layout XML

**Search Pattern** to find candidates:
```csharp
// Look for loops that add controls
foreach (var field in fields)
{
    panel.Controls.Add(CreateFieldControl(field)); // CANDIDATE
}
```

### Implementation Steps

1. **Audit control-adding code**:
   ```powershell
   # Find loops adding controls
   grep -r "Controls.Add" Src/ --include="*.cs" | grep -B5 "foreach\|for\|while"
   ```

2. **Wrap in SuspendLayout/ResumeLayout**:
   ```csharp
   // Before
   public void BuildFieldPanel(IEnumerable<FieldSpec> fields)
   {
       foreach (var field in fields)
           _panel.Controls.Add(CreateFieldEditor(field));
   }
   
   // After
   public void BuildFieldPanel(IEnumerable<FieldSpec> fields)
   {
       _panel.SuspendLayout();
       try
       {
           foreach (var field in fields)
               _panel.Controls.Add(CreateFieldEditor(field));
       }
       finally
       {
           _panel.ResumeLayout(true);
       }
   }
   ```

3. **Nested containers**: Apply to parent containers first, then children.

### Complexity
- **Big-O Impact**: O(N) layout passes → O(1) layout pass
- **Memory**: No change
- **Thread Safety**: Must be on UI thread

---

## Optimization 2: Double Buffering

### Problem
Default WinForms painting draws directly to screen, causing visible flicker during updates. Each control paints separately, showing intermediate states.

### Current Behavior (Flickery)
```
Frame 1: Background painted
Frame 2: Control 1 painted
Frame 3: Control 2 painted  ← User sees incomplete state
Frame 4: Control 3 painted
...
```

### Optimized Approach
```csharp
public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        // Enable all double-buffering flags
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.UserPaint, true);
        UpdateStyles();
    }
}
```

### Alternative: Form-Level Double Buffering
```csharp
public partial class EntryEditorForm : Form
{
    public EntryEditorForm()
    {
        InitializeComponent();
        
        // Enable double buffering for entire form
        DoubleBuffered = true;
        
        // Or via reflection for inherited forms
        SetDoubleBuffered(this, true);
    }
    
    private static void SetDoubleBuffered(Control control, bool enabled)
    {
        var prop = typeof(Control).GetProperty(
            "DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);
        prop?.SetValue(control, enabled);
        
        // Recursively apply to children
        foreach (Control child in control.Controls)
            SetDoubleBuffered(child, enabled);
    }
}
```

### Where to Apply in FieldWorks

**Priority Controls**:
- Main entry editor panels
- Browse view list containers
- Any panel with many child controls
- Custom-drawn controls (graphs, trees)

**Search for existing usage**:
```powershell
grep -r "DoubleBuffered\|OptimizedDoubleBuffer" Src/ --include="*.cs"
```

### Implementation Steps

1. **Create base class for panels**:
   ```csharp
   // Src/Common/Controls/FwDoubleBufferedPanel.cs
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

2. **Replace Panel with FwDoubleBufferedPanel** in key locations.

3. **Test for visual regressions** - some controls may paint incorrectly with double buffering.

### Complexity
- **Big-O Impact**: No algorithmic change, but eliminates visual stutter
- **Memory**: 2× memory for buffered bitmap (temporary)
- **Trade-off**: Slightly higher memory, much smoother visuals

---

## Optimization 3: Manual Control Virtualization

### Problem
Creating 500+ controls for a long list (senses, examples, custom fields) is expensive. Each control allocates memory, registers event handlers, and participates in layout.

### Current Behavior (Slow)
```csharp
// Creates ALL controls upfront
foreach (var sense in entry.Senses) // 50 senses
{
    foreach (var field in senseFields) // 30 fields each
    {
        panel.Controls.Add(CreateFieldEditor(sense, field));
        // Total: 1,500 controls created immediately
    }
}
```

### Optimized Approach: Owner-Draw with Virtual Data
```csharp
public class VirtualFieldList : Control
{
    private readonly List<FieldData> _allFields = new();
    private readonly Dictionary<int, Control> _visibleControls = new();
    private int _scrollOffset;
    private const int RowHeight = 24;
    
    protected override void OnPaint(PaintEventArgs e)
    {
        int firstVisible = _scrollOffset / RowHeight;
        int lastVisible = ((_scrollOffset + Height) / RowHeight) + 1;
        
        // Only create/show controls for visible range
        RecycleInvisibleControls(firstVisible, lastVisible);
        EnsureVisibleControls(firstVisible, lastVisible);
        
        base.OnPaint(e);
    }
    
    private void RecycleInvisibleControls(int first, int last)
    {
        var toRemove = _visibleControls.Keys
            .Where(i => i < first || i > last)
            .ToList();
            
        foreach (var idx in toRemove)
        {
            var ctrl = _visibleControls[idx];
            ctrl.Visible = false;
            _controlPool.Return(ctrl); // Reuse later
            _visibleControls.Remove(idx);
        }
    }
    
    private void EnsureVisibleControls(int first, int last)
    {
        for (int i = first; i <= last && i < _allFields.Count; i++)
        {
            if (!_visibleControls.ContainsKey(i))
            {
                var ctrl = _controlPool.Rent();
                ctrl.Top = (i * RowHeight) - _scrollOffset;
                BindDataToControl(ctrl, _allFields[i]);
                ctrl.Visible = true;
                _visibleControls[i] = ctrl;
            }
        }
    }
}
```

### Simpler Approach: ScrollableControl with Lazy Child Creation
```csharp
public class LazyFieldPanel : ScrollableControl
{
    private readonly List<Func<Control>> _controlFactories = new();
    private readonly HashSet<int> _createdIndices = new();
    
    public void AddFieldFactory(Func<Control> factory)
    {
        _controlFactories.Add(factory);
    }
    
    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        EnsureVisibleControlsCreated();
    }
    
    private void EnsureVisibleControlsCreated()
    {
        var visibleRect = new Rectangle(
            HorizontalScroll.Value,
            VerticalScroll.Value,
            ClientSize.Width,
            ClientSize.Height);
        
        int rowHeight = 30; // Approximate
        int firstRow = visibleRect.Top / rowHeight;
        int lastRow = visibleRect.Bottom / rowHeight;
        
        for (int i = firstRow; i <= lastRow && i < _controlFactories.Count; i++)
        {
            if (!_createdIndices.Contains(i))
            {
                var ctrl = _controlFactories[i]();
                ctrl.Top = i * rowHeight;
                Controls.Add(ctrl);
                _createdIndices.Add(i);
            }
        }
    }
}
```

### Where to Apply in FieldWorks

**Best Candidates**:
- Sense list in entry editor
- Example list within senses
- Custom fields panel (when expanded)
- Browse view rows

### Implementation Steps

1. **Identify scroll containers** with many children
2. **Replace eager creation** with factory pattern
3. **Hook scroll events** to trigger lazy creation
4. **Implement control pooling** for recycling

### Complexity
- **Big-O Impact**: O(N) control creation → O(V) where V = visible count
- **Memory**: O(N) → O(V) for control instances
- **Trade-off**: More complex code, significant memory/speed gains

---

## Optimization 4: Lazy Control Creation

### Problem
Forms create all controls during `InitializeComponent()`, even for tabs/panels that may never be viewed.

### Current Behavior (Slow)
```csharp
public EntryEditorForm()
{
    InitializeComponent(); // Creates ALL controls for ALL tabs
    
    // User may only look at "General" tab
    // But "Etymology", "History", "Custom Fields" tabs are fully built
}
```

### Optimized Approach: Create on First Access
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

// Usage
public class EntryEditorForm : Form
{
    private TabControl _tabControl;
    
    public EntryEditorForm()
    {
        _tabControl = new TabControl();
        _tabControl.Dock = DockStyle.Fill;
        
        // General tab - always needed
        _tabControl.TabPages.Add(CreateGeneralTab());
        
        // Other tabs - lazy
        _tabControl.TabPages.Add(new LazyTabPage("Etymology", CreateEtymologyPanel));
        _tabControl.TabPages.Add(new LazyTabPage("History", CreateHistoryPanel));
        _tabControl.TabPages.Add(new LazyTabPage("Custom Fields", CreateCustomFieldsPanel));
        
        // Create content when tab is selected
        _tabControl.SelectedIndexChanged += (s, e) =>
        {
            if (_tabControl.SelectedTab is LazyTabPage lazy)
                lazy.EnsureContentCreated();
        };
        
        Controls.Add(_tabControl);
    }
}
```

### Property-Level Lazy Loading
```csharp
public class LazySenseView : UserControl
{
    private Panel _examplesPanel;
    private bool _examplesLoaded;
    
    // Examples are expensive - load only when expanded
    public Panel ExamplesPanel
    {
        get
        {
            if (!_examplesLoaded)
            {
                _examplesPanel = BuildExamplesPanel();
                _examplesLoaded = true;
            }
            return _examplesPanel;
        }
    }
    
    private void OnExpandExamplesClicked(object sender, EventArgs e)
    {
        _examplesContainer.Controls.Add(ExamplesPanel);
    }
}
```

### Where to Apply in FieldWorks

**Priority Areas**:
- Tab pages that aren't initially visible
- Expandable sections (Etymology, Custom Fields)
- Detail panes in browse views
- Dialog boxes with multiple pages

### Implementation Steps

1. **Audit tab controls** for eager content creation
2. **Identify "rarely used" sections** via usage telemetry or heuristics
3. **Wrap content creation** in factory functions
4. **Hook visibility/selection events** for lazy initialization

### Complexity
- **Big-O Impact**: O(total) → O(initially visible)
- **Memory**: Deferred until needed
- **Trade-off**: Slight delay when first accessing lazy content

---

## Optimization 5: DisplayCommand Caching

### Problem
`XmlVc.ProcessFrag` walks the Parts/Layout XML tree for every object displayed. Even with `DisplayCommand` identity caching, there's overhead in resolution and tree walking.

### Current Caching
```csharp
// XmlVc already caches DisplayCommand by identity
internal Dictionary<DisplayCommand, int> m_displayCommandToId;
internal Dictionary<int, DisplayCommand> m_idToDisplayCommand;
```

### Enhanced Caching: Pre-compiled Command Sequences
```csharp
public class EnhancedLayoutCache
{
    // Cache by (classId, layoutName, configVersion)
    private readonly Dictionary<LayoutKey, CompiledLayout> _compiledLayouts = new();
    
    public record LayoutKey(int ClassId, string LayoutName, int ConfigVersion);
    
    public class CompiledLayout
    {
        // Pre-resolved sequence of operations
        public List<DisplayOperation> Operations { get; }
        
        // Field metadata for data binding
        public List<FieldBinding> Bindings { get; }
        
        // Estimated height for virtualization
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
    
    private CompiledLayout CompileLayout(int classId, string layoutName)
    {
        var operations = new List<DisplayOperation>();
        var bindings = new List<FieldBinding>();
        
        // Walk layout XML ONCE, record operations
        var layoutNode = _layoutCache.GetNodeForPart(classId, layoutName);
        CompileNode(layoutNode, operations, bindings);
        
        return new CompiledLayout
        {
            Operations = operations,
            Bindings = bindings,
            EstimatedHeight = EstimateHeight(operations)
        };
    }
}
```

### Application: Skip ProcessFrag for Cached Layouts
```csharp
// In XmlVc or a wrapper
public void DisplayWithCache(IVwEnv vwenv, int hvo, int classId, string layoutName)
{
    var compiled = _enhancedCache.GetOrCompile(classId, layoutName);
    
    // Execute pre-compiled operations instead of walking XML
    foreach (var op in compiled.Operations)
    {
        ExecuteOperation(vwenv, hvo, op);
    }
}

private void ExecuteOperation(IVwEnv vwenv, int hvo, DisplayOperation op)
{
    switch (op)
    {
        case OpenParagraphOp p:
            vwenv.OpenParagraph();
            break;
        case AddStringPropOp s:
            vwenv.AddStringProp(s.Flid, GetVc(s.VcType));
            break;
        case AddObjOp o:
            vwenv.AddObj(GetRelatedHvo(hvo, o.Flid), GetVc(o.VcType), o.Frag);
            break;
        // ... etc
    }
}
```

### Where to Apply in FieldWorks

**High-Value Targets**:
- `LexEntry` display (most common)
- `LexSense` display (nested, repeated)
- Browse view cell rendering

### Implementation Steps

1. **Profile** to confirm layout compilation is significant (vs. data access)
2. **Add compilation layer** on top of existing `LayoutCache`
3. **Invalidate cache** when config files change
4. **Measure improvement** for typical entry displays

### Complexity
- **Big-O Impact**: O(D × N) per display → O(operations) first time, O(1) thereafter
- **Memory**: Cache size proportional to unique (class, layout) pairs
- **Trade-off**: Complexity, but high payoff for repeated displays

---

## Optimization 6: Background Data Loading

### Problem
UI thread blocks while loading data from LCModel, causing freezes during navigation.

### Current Behavior (Blocking)
```csharp
private void OnEntrySelected(int hvo)
{
    // ALL of this runs on UI thread
    var entry = _cache.ServiceLocator.GetInstance<ILexEntryRepository>()
        .GetObject(hvo);
    
    // Load all senses (may trigger database queries)
    foreach (var sense in entry.SensesOS)
    {
        LoadSenseData(sense); // More queries
    }
    
    // Finally update UI
    DisplayEntry(entry);
}
```

### Optimized Approach: Async Data Loading
```csharp
private async void OnEntrySelected(int hvo)
{
    // Show loading indicator immediately
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
    // This runs on ThreadPool thread
    var entry = _cache.ServiceLocator.GetInstance<ILexEntryRepository>()
        .GetObject(hvo);
    
    // Pre-load all needed data into a DTO
    return new EntryDataPackage
    {
        Hvo = hvo,
        Headword = entry.HeadWord?.Text,
        Senses = entry.SensesOS.Select(s => new SenseData
        {
            Hvo = s.Hvo,
            Gloss = s.Gloss?.BestAnalysisAlternative?.Text,
            Definition = s.Definition?.BestAnalysisAlternative?.Text,
            // ... pre-load all display data
        }).ToList(),
        // ...
    };
}
```

### Progressive Loading Pattern
```csharp
private async void OnEntrySelected(int hvo)
{
    // Phase 1: Critical data (headword, POS) - fast
    var criticalData = await Task.Run(() => LoadCriticalDataAsync(hvo));
    DisplayCriticalSection(criticalData);
    
    // Phase 2: Primary content (senses) - medium
    var sensesData = await Task.Run(() => LoadSensesDataAsync(hvo));
    DisplaySensesSection(sensesData);
    
    // Phase 3: Secondary content (examples, etymology) - slow
    var secondaryData = await Task.Run(() => LoadSecondaryDataAsync(hvo));
    DisplaySecondarySection(secondaryData);
    
    // Phase 4: Custom fields - defer until expanded
    // Don't load until user expands the section
}
```

### Where to Apply in FieldWorks

**Priority Areas**:
- Entry selection in browse view
- Navigation between entries
- Initial view load
- Search result display

### Implementation Steps

1. **Identify blocking data loads** via profiling or code review
2. **Create DTO classes** for pre-loaded data
3. **Move loading to Task.Run**
4. **Update UI incrementally** with Invoke/BeginInvoke

### Complexity
- **Big-O Impact**: No change to data load time, but **perceived** performance improves dramatically
- **Memory**: DTOs add temporary memory usage
- **Trade-off**: Complexity, threading concerns, but much more responsive UI

---

## Optimization 7: Smart Invalidation (Dirty Rectangles)

### Problem
When data changes, entire panels are invalidated and repainted, even if only one field changed.

### Current Behavior (Full Repaint)
```csharp
private void OnDataChanged(object sender, EventArgs e)
{
    // Invalidates ENTIRE panel
    _entryPanel.Invalidate();
    _entryPanel.Refresh(); // Forces immediate repaint of everything
}
```

### Optimized Approach: Targeted Invalidation
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

// Usage
private void OnGlossChanged(int senseHvo, string newGloss)
{
    var fieldKey = $"sense_{senseHvo}_gloss";
    
    // Update the specific control
    if (_fieldControls.TryGetValue(fieldKey, out var textBox))
    {
        textBox.Text = newGloss;
    }
    
    // Only invalidate that region
    _smartPanel.InvalidateField(fieldKey);
}
```

### Control-Level Change Tracking
```csharp
public class ChangeTrackingTextBox : TextBox
{
    private string _originalValue;
    private bool _isDirty;
    
    public void SetValueWithoutDirty(string value)
    {
        _originalValue = value;
        Text = value;
        _isDirty = false;
    }
    
    protected override void OnTextChanged(EventArgs e)
    {
        _isDirty = (Text != _originalValue);
        base.OnTextChanged(e);
        
        // Only notify parent if actually changed
        if (_isDirty)
            OnValueChanged();
    }
    
    private void OnValueChanged()
    {
        // Only invalidate THIS control
        Invalidate();
    }
}
```

### Where to Apply in FieldWorks

**Priority Areas**:
- Property changes that update single fields
- Typing in text boxes (should not repaint whole form)
- Selection highlighting changes

### Implementation Steps

1. **Audit Invalidate() calls** that are too broad
2. **Track control bounds** by field/property key
3. **Replace broad invalidation** with targeted invalidation
4. **Verify no visual artifacts** from partial repaints

### Complexity
- **Big-O Impact**: O(total pixels) → O(changed region pixels)
- **Memory**: Bounds dictionary adds small overhead
- **Trade-off**: More bookkeeping, but much faster updates

---

## Optimization 8: Control Count Reduction

### Problem
Each WinForms control has overhead: window handle, message pump participation, layout calculations. Hundreds of controls compound these costs.

### Current Approach (Many Controls)
```csharp
// Each label and textbox is a separate control
foreach (var field in fields)
{
    var label = new Label { Text = field.Label };
    var textBox = new TextBox { Text = field.Value };
    panel.Controls.Add(label);
    panel.Controls.Add(textBox);
    // 100 fields = 200 controls
}
```

### Optimized Approach: Composite Controls
```csharp
public class FieldStrip : Control
{
    private readonly List<FieldData> _fields = new();
    
    public FieldStrip()
    {
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
    }
    
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        int y = 0;
        
        foreach (var field in _fields)
        {
            // Draw label (no Label control needed)
            g.DrawString(field.Label, Font, Brushes.Gray, 0, y);
            
            // Draw value (no TextBox control needed for read-only)
            g.DrawString(field.Value, Font, Brushes.Black, 100, y);
            
            y += 24;
        }
    }
    
    // Handle clicks to enter edit mode for specific field
    protected override void OnMouseClick(MouseEventArgs e)
    {
        int clickedIndex = e.Y / 24;
        if (clickedIndex < _fields.Count)
        {
            EnterEditMode(clickedIndex);
        }
    }
    
    private void EnterEditMode(int fieldIndex)
    {
        // Create a SINGLE TextBox overlay for editing
        var editor = new TextBox
        {
            Location = new Point(100, fieldIndex * 24),
            Text = _fields[fieldIndex].Value
        };
        Controls.Add(editor);
        editor.Focus();
        editor.LostFocus += (s, e) => ExitEditMode(editor, fieldIndex);
    }
}
```

### Alternative: Owner-Draw ListView
```csharp
public class OwnerDrawFieldList : ListView
{
    public OwnerDrawFieldList()
    {
        View = View.Details;
        OwnerDraw = true;
        VirtualMode = true; // Combine with virtualization!
        
        Columns.Add("Label", 100);
        Columns.Add("Value", 200);
    }
    
    protected override void OnDrawItem(DrawListViewItemEventArgs e)
    {
        // Custom painting - no child controls
        e.DrawBackground();
        
        var field = GetFieldAt(e.ItemIndex);
        e.Graphics.DrawString(field.Label, Font, Brushes.Gray, e.Bounds.X, e.Bounds.Y);
        e.Graphics.DrawString(field.Value, Font, Brushes.Black, e.Bounds.X + 100, e.Bounds.Y);
        
        e.DrawFocusRectangle();
    }
    
    protected override void OnRetrieveVirtualItem(RetrieveVirtualItemEventArgs e)
    {
        // Virtual mode - items created on demand
        var field = GetFieldAt(e.ItemIndex);
        e.Item = new ListViewItem(new[] { field.Label, field.Value });
    }
}
```

### Where to Apply in FieldWorks

**Best Candidates**:
- Read-only field displays (most fields most of the time)
- Simple list views
- Status/summary panels

**Not Suitable For**:
- Complex editors with validation
- Rich text editing
- Accessibility-critical areas (controls provide better a11y)

### Implementation Steps

1. **Identify read-heavy panels** where editing is rare
2. **Create composite control** that owner-draws fields
3. **Add edit-mode overlay** when user clicks to edit
4. **Test accessibility** - may need extra work for screen readers

### Complexity
- **Big-O Impact**: O(N) controls → O(1) control (plus paint cost)
- **Memory**: Significant reduction in handle count
- **Trade-off**: More custom code, but major performance gain for read-heavy UIs

---

## Optimization 9: Custom Field Collapse-by-Default

### Problem
Custom fields are the #1 contributor to UI complexity. An entry with 50 custom fields renders 50+ controls immediately, even though users rarely view all of them.

### Current Behavior
```
Entry Editor
├── Basic Fields (5 controls)
├── Senses (10 senses × 20 fields = 200 controls)
├── Custom Fields (50 fields = 50 controls)  ← ALWAYS RENDERED
└── Total: 255+ controls on load
```

### Optimized Approach: Collapsed by Default
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
    
    private void OnExpandClick(object sender, EventArgs e)
    {
        if (_isExpanded)
        {
            Collapse();
        }
        else
        {
            Expand();
        }
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
        ((Button)Controls[0].Controls[0]).Text = "▼";
        ResumeLayout(true);
    }
    
    private void Collapse()
    {
        SuspendLayout();
        Controls.Remove(_content);
        _isExpanded = false;
        ((Button)Controls[0].Controls[0]).Text = "▶";
        ResumeLayout(true);
    }
}

// Usage
var customFieldsSection = new CollapsibleSection(
    "Custom Fields",
    customFields.Count,
    () => BuildCustomFieldsPanel(customFields)
);
```

### Where to Apply in FieldWorks

**Priority Sections**:
- Custom Fields (highest impact)
- Etymology section
- History/Change Log
- References section
- Any section with 10+ fields

### Implementation Steps

1. **Identify sections** that can be collapsed
2. **Wrap in CollapsibleSection** with lazy content creation
3. **Persist expand/collapse state** per user/entry type
4. **Default collapsed** for sections with many items

### Complexity
- **Big-O Impact**: O(all fields) → O(visible sections)
- **Memory**: Deferred until expanded
- **Trade-off**: Extra click to see content, but dramatic speedup

### Expected Impact
For an entry with 50 custom fields:
- **Before**: 50 controls created on load
- **After**: 1 header control; 50 controls only if user expands
- **Savings**: ~98% reduction in custom field rendering cost

---

## Optimization 10: XML/Layout Resolution Caching

### Problem
`LayoutCache.GetNodeForPart` walks class hierarchy and applies overrides each time. For identical lookups, this is redundant.

### Current Caching
```csharp
// LayoutCache already has m_map
Dictionary<Tuple<int, string, bool>, XmlNode> m_map;
```

### Enhanced Caching: Include Override State
```csharp
public class EnhancedLayoutCache
{
    // Include configuration version in cache key
    private readonly Dictionary<LayoutCacheKey, XmlNode> _cache = new();
    
    private record LayoutCacheKey(
        int ClassId,
        string LayoutName,
        bool IncludeLayouts,
        int ConfigVersion,
        string UserOverrideHash  // Include user override state
    );
    
    public XmlNode GetNodeForPart(int classId, string layoutName, bool includeLayouts)
    {
        var key = new LayoutCacheKey(
            classId,
            layoutName,
            includeLayouts,
            GetConfigVersion(),
            GetUserOverrideHash()
        );
        
        if (!_cache.TryGetValue(key, out var node))
        {
            node = ComputeNodeForPart(classId, layoutName, includeLayouts);
            _cache[key] = node;
        }
        
        return node;
    }
    
    // Invalidate on config changes
    public void OnConfigurationChanged()
    {
        _cache.Clear();
    }
}
```

### Pre-Warming Cache at Startup
```csharp
public class LayoutCacheWarmer
{
    public async Task WarmCacheAsync()
    {
        // Pre-compute common layouts in background
        var commonLayouts = new[]
        {
            (typeof(ILexEntry), "publishRoot"),
            (typeof(ILexSense), "Normal"),
            (typeof(ILexExampleSentence), "Normal"),
            // ... other common layouts
        };
        
        await Task.Run(() =>
        {
            foreach (var (type, layout) in commonLayouts)
            {
                var classId = GetClassId(type);
                _layoutCache.GetNodeForPart(classId, layout, true);
            }
        });
    }
}

// Call during application startup
protected override async void OnLoad(EventArgs e)
{
    base.OnLoad(e);
    
    // Warm cache while showing splash screen
    await _cacheWarmer.WarmCacheAsync();
}
```

### Where to Apply in FieldWorks

**Priority**:
- Application startup (pre-warm common layouts)
- Project open (pre-warm project-specific layouts)
- Configuration change (invalidate and rebuild)

### Implementation Steps

1. **Profile cache hit rate** in current implementation
2. **Extend cache key** if needed for missed optimizations
3. **Add pre-warming** for common layouts
4. **Monitor cache size** to prevent memory bloat

### Complexity
- **Big-O Impact**: O(H) hierarchy walk → O(1) for cached lookups
- **Memory**: Proportional to unique layouts (typically small)
- **Trade-off**: Memory for speed; need invalidation strategy

---

## Implementation Priority Matrix

| Optimization | Impact | Effort | Dependencies | Recommended Order |
|-------------|--------|--------|--------------|-------------------|
| 9. Collapse-by-Default | High | Low | None | **1st** |
| 1. SuspendLayout Batching | Medium | Low | None | **2nd** |
| 2. Double Buffering | Medium | Low | None | **3rd** |
| 4. Lazy Control Creation | High | Medium | None | **4th** |
| 6. Background Data Loading | High | Medium | Async patterns | **5th** |
| 10. Layout Resolution Caching | Medium | Medium | LayoutCache access | **6th** |
| 7. Smart Invalidation | Medium | Medium | Control tracking | **7th** |
| 5. DisplayCommand Caching | Medium | Medium | XmlVc internals | **8th** |
| 3. Manual Virtualization | High | High | Scroll containers | **9th** |
| 8. Control Count Reduction | High | High | Custom painting | **10th** |

---

## Measurement Plan

### Before/After Metrics
```csharp
public class PerformanceMetrics
{
    public static void MeasureEntryLoad(Action loadAction)
    {
        var sw = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);
        var initialHandles = Process.GetCurrentProcess().HandleCount;
        
        loadAction();
        
        sw.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var finalHandles = Process.GetCurrentProcess().HandleCount;
        
        Console.WriteLine($"Load time: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Memory delta: {(finalMemory - initialMemory) / 1024}KB");
        Console.WriteLine($"Handle delta: {finalHandles - initialHandles}");
    }
}
```

### Target Metrics
| Metric | Current (est.) | Target | Success Criteria |
|--------|----------------|--------|------------------|
| Simple entry load | 500ms | 200ms | 60% reduction |
| Complex entry load | 3000ms | 800ms | 73% reduction |
| Custom fields expand | 2000ms | 300ms | 85% reduction |
| Memory per entry | 50MB | 20MB | 60% reduction |
| Handle count | 500+ | 100 | 80% reduction |

---

## Risk Mitigation

### Regression Risks
1. **Visual changes**: Double buffering or owner-draw may render differently
   - *Mitigation*: Visual regression tests, careful comparison

2. **Async bugs**: Background loading can cause race conditions
   - *Mitigation*: Proper synchronization, cancellation tokens

3. **Accessibility**: Owner-draw controls may lose screen reader support
   - *Mitigation*: Test with screen readers, add proper accessibility attributes

### Testing Strategy
1. **Unit tests**: For caching logic, data loading
2. **Integration tests**: For form load scenarios
3. **Visual tests**: Screenshot comparison before/after
4. **Performance tests**: Automated timing measurements
5. **Accessibility tests**: Screen reader verification

---

## Appendix: Quick Reference Code Snippets

### A. SuspendLayout Helper
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

### B. Double Buffer Extension
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

### C. Async Load Pattern
```csharp
public static class AsyncUIHelper
{
    public static async Task LoadAsync<T>(
        Control uiControl,
        Func<T> loader,
        Action<T> uiUpdater,
        Action showLoading = null,
        Action hideLoading = null)
    {
        showLoading?.Invoke();
        try
        {
            var data = await Task.Run(loader);
            
            if (uiControl.IsHandleCreated)
            {
                uiControl.BeginInvoke(new Action(() => uiUpdater(data)));
            }
        }
        finally
        {
            hideLoading?.Invoke();
        }
    }
}
```
