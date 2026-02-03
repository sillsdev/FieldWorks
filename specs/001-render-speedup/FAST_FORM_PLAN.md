# Fast Form Rendering Plan

**Feature**: Advanced Entry Avalonia View
**Related**: `specs/010-advanced-entry-view/spec.md`, `presentation-ir-research.md`
**Date**: January 2026

This document analyzes performance bottlenecks in FieldWorks' current rendering architecture and proposes paths forward for achieving fast, responsive UI with many custom fields.

---

## Section 1: Core Architecture of C++ Views and Why It's Slow

### 1.1 Architecture Overview

The FieldWorks Views system is a **retained-mode rendering engine** with these core components:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Managed Layer (C#)                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  XmlVc (View Constructor)                                                   │
│    ├── Interprets Parts/Layout XML                                          │
│    ├── Resolves layouts via LayoutCache + Inventory                         │
│    ├── Caches DisplayCommands (compiled fragments)                          │
│    └── Calls IVwEnv methods to build box tree                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                         COM Interface Boundary                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                         Native Layer (C++)                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  VwEnv (Box Builder)                                                        │
│    ├── Receives IVwEnv calls (OpenParagraph, AddStringProp, etc.)           │
│    └── Constructs VwBox hierarchy                                           │
│                                                                             │
│  VwRootBox (5,230 lines)                                                    │
│    ├── Root of box tree                                                     │
│    ├── Coordinates layout and painting                                      │
│    ├── Manages invalidation and redraw                                      │
│    └── Handles selection and editing                                        │
│                                                                             │
│  VwBox Hierarchy                                                            │
│    ├── VwDivBox (flow containers)                                           │
│    ├── VwParagraphBox (text paragraphs)                                     │
│    ├── VwStringBox (text runs)                                              │
│    ├── VwLazyBox (virtualization placeholder)                               │
│    └── VwTableBox, VwInnerPileBox, etc.                                     │
│                                                                             │
│  VwNotifier (Change Propagation)                                            │
│    └── Maps data changes to box regeneration                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Core Algorithms and Their Complexity

#### 1.2.1 Layout Resolution: `XmlVc.GetNodeForPart` + `LayoutCache`

**Algorithm**: Walk class hierarchy until a matching layout/part is found, apply overrides.

```
GetNodeForPart(classId, layoutName)
    for each class in hierarchy(classId):
        node = layoutInventory.GetElement("layout", class, layoutName)
        if node exists: apply overrides, return unified node
        node = partInventory.GetElement("part", class, layoutName)
        if node exists: return node
    return null
```

**Complexity**:
- **Uncached**: O(H × L) where H = class hierarchy depth, L = lookup cost
- **Cached**: O(1) via `m_map` dictionary after first access

**Problem**: Caching is per (classId, layoutName) pair. With custom fields, unique keys multiply.

---

#### 1.2.2 XML Interpretation: `XmlVc.ProcessFrag` (Recursive Tree Walk)

**Algorithm**: Recursively interpret XML nodes, emitting `IVwEnv` calls.

```
ProcessFrag(xmlNode, env, hvo)
    switch xmlNode.Name:
        case "para": env.OpenParagraph(); ProcessChildren(); env.CloseParagraph()
        case "string": env.AddStringProp(flid, wsRule)
        case "obj": env.AddObj(hvo, vc, frag)
        case "seq": ProcessSequence(node, env, hvo)  // iterates vector
        case "part": resolvedNode = GetNodeForPart(...); ProcessFrag(resolvedNode, ...)
        case "if": if EvaluateCondition(node, hvo): ProcessChildren()
        ...
    ProcessChildren(xmlNode, env, hvo):
        foreach child in xmlNode.ChildNodes:
            ProcessFrag(child, env, hvo)
```

**Complexity**: O(D × N) per object displayed
- D = maximum nesting depth of layouts/parts
- N = total XML nodes in the resolved tree

**Problem**: This is **executed on every object** in a list. For an entry with:
- 10 senses
- 5 examples per sense
- 20 custom fields per sense

The tree walk happens: 10 × (5 + 1) × 20 = **1,200 times per entry display**.

---

#### 1.2.3 Custom Field Expansion: `PartGenerator.Generate`

**Algorithm**: Clone XML template for each custom field, perform attribute substitutions.

```
Generate()
    result = new XmlNode[customFieldCount]
    for each fieldId in customFields:
        output = sourceNode.Clone()  // FULL SUBTREE CLONE
        ReplaceParamsInAttributes(output, fieldName, label, wsName)
        GeneratePartsFromLayouts(classId, fieldName, fieldId, ref output)
        result.Add(output)
    return result
```

**Complexity**: O(F × S × A)
- F = number of custom fields
- S = size of source XML subtree (nodes)
- A = attribute replacement passes

**Problem**: XML cloning is expensive. With 50 custom fields, this creates 50 deep-copied XML trees **per view construction**.

---

#### 1.2.4 Sequence/Vector Display: `VectorReferenceVc.Display`

**Algorithm**: Iterate all items in a vector property, optionally filter/sort.

```
Display(vwenv, hvo, frag)
    items = GetVectorProperty(hvo, flid)
    if hasFilter: items = items.Where(filterPredicate)
    if hasSort: items = items.OrderBy(sortKey)
    
    foreach item in items:
        AddEmbellishments(numbering, separator)
        vwenv.AddObj(item, vc, childFrag)  // RECURSIVE DISPLAY
```

**Complexity**: O(V × T)
- V = vector size (items in the sequence)
- T = per-item tree cost (from ProcessFrag)

**Problem**: No virtualization at this level—all items are processed even if off-screen.

---

#### 1.2.5 Native Box Layout: `VwBox.DoLayout` / `VwDivBox.DoLayout`

**Algorithm**: Top-down width propagation, bottom-up height computation.

```
// Simplified from VwDivBox.DoLayout
DoLayout(graphics, availWidth)
    foreach child in children:
        child.DoLayout(graphics, availWidth)
    
    height = 0
    foreach child in children:
        child.SetTop(height)
        height += child.Height
    
    this.Height = height
```

**Complexity**: O(B) where B = total boxes in subtree

**Problem**: Layout is recomputed on **any change**—no incremental/dirty-region optimization at the managed layer.

---

#### 1.2.6 Native Box Painting: `VwRootBox.DrawRoot`

**Algorithm**: Expand lazy boxes in clip rect, then paint visible boxes.

```
DrawRoot(graphics, clipRect)
    PrepareToDraw()  // May expand VwLazyBoxes
    
    foreach box in boxTree:
        if box.Intersects(clipRect):
            box.Draw(graphics, clipRect)
```

**Complexity**: O(V) where V = visible boxes (after lazy expansion)

**Note**: This is actually *reasonably efficient* thanks to `VwLazyBox`, but lazy expansion can trigger cascading layout work.

---

### 1.3 Existing Virtualization: VwLazyBox

FieldWorks **does** have a virtualization mechanism: `VwLazyBox`.

**How it works**:
1. Instead of creating real boxes for all vector items, a single `VwLazyBox` holds HVOs and estimated heights.
2. When the user scrolls, `ExpandForDisplay` is called to create real boxes for visible items.
3. When items scroll out of view, `LazifyRange` converts boxes back to lazy placeholders.

**Key methods** (`Src/views/VwLazyBox.cpp`):
- `ExpandItems(ihvoMin, ihvoLim)`: Create real boxes for range
- `ExpandItemsNoLayout(...)`: Create boxes without immediate layout
- `LayoutExpandedItems(...)`: Layout newly created boxes
- `GetItemsToExpand(clipRect)`: Determine which items need expansion

**Limitation**: VwLazyBox operates at the **vector property level**, not at the individual field level. Within each expanded item, **all fields are rendered immediately**.

---

### 1.4 Summary of Big-O Notation

| Operation | Complexity | Variables |
|-----------|------------|-----------|
| Layout lookup (cached) | O(1) | - |
| Layout lookup (uncached) | O(H) | H = class hierarchy depth |
| ProcessFrag tree walk | O(D × N) | D = depth, N = nodes |
| Custom field generation | O(F × S) | F = fields, S = source size |
| Sequence display | O(V × T) | V = vector size, T = per-item cost |
| Box layout | O(B) | B = boxes in subtree |
| Box painting | O(V) | V = visible boxes |

**Worst-case full display** of an entry with M senses, N subsenses each, F custom fields:
```
Total = O(M × N × D × F × S) + O(boxes for layout) + O(visible boxes for paint)
```

For a "heavy" entry (20 senses, 5 subsenses, 50 custom fields, depth 10):
- **Interpretation**: 20 × 5 × 10 × 50 = 500,000 node visits
- **XML clones**: 50 × (average 20 nodes) = 1,000 node clones per field expansion

---

### 1.5 Why It Feels Slow

1. **No field-level virtualization**: VwLazyBox helps with list items, but within each item all custom fields are eagerly rendered.

2. **Repeated XML interpretation**: The same layout tree is re-traversed for every object, even when the structure is identical.

3. **Synchronous COM boundary crossing**: Every `IVwEnv` call crosses the managed/native boundary.

4. **Custom field explosion**: Each custom field triggers XML cloning and additional layout resolution.

5. **Layout recomputation on any change**: No incremental layout; a property change triggers full subtree re-layout.

6. **Selection operations are expensive**: Comments in `VwSelection.cpp` explicitly note "This is way too slow" for certain operations.

---

## Section 2: How Modern Frameworks Solve This Problem

### 2.1 Strategy 1: UI Virtualization (Render Only What's Visible)

**Concept**: Create UI elements only for items currently in the viewport, plus a small buffer (overscan).

**How it works**:
```
┌─────────────────────────────────────────┐
│ Viewport (screen area)                  │
│ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐        │
│ │Item3│ │Item4│ │Item5│ │Item6│        │
│ └─────┘ └─────┘ └─────┘ └─────┘        │
└─────────────────────────────────────────┘
  Items 0-2: Not rendered (virtualized)
  Items 7+: Not rendered (virtualized)
```

**Implementations**:
| Framework | Control | Key Feature |
|-----------|---------|-------------|
| WPF | `VirtualizingStackPanel` | Pixel-scroll virtualization, recycling mode |
| Avalonia | `ItemsRepeater` | Layout-driven virtualization, smooth scroll |
| Avalonia | `TreeDataGrid` | Hierarchical virtualization for trees/tables |
| WinUI | `ItemsRepeater` | Headless virtualization building block |
| React | react-virtuoso, TanStack Virtual | Headless position calculation |

**Complexity improvement**:
- Non-virtualized: O(N) render for N items
- Virtualized: O(V) render where V = viewport items << N

**Example (Avalonia ItemsRepeater)**:
```xml
<ScrollViewer>
  <ItemsRepeater ItemsSource="{Binding Items}">
    <ItemsRepeater.Layout>
      <StackLayout Orientation="Vertical" />
    </ItemsRepeater.Layout>
    <ItemsRepeater.ItemTemplate>
      <DataTemplate>
        <TextBlock Text="{Binding Name}" />
      </DataTemplate>
    </ItemsRepeater.ItemTemplate>
  </ItemsRepeater>
</ScrollViewer>
```

**Applicability to FieldWorks**: 
- ✅ List of senses/examples: Ideal for `ItemsRepeater` or `TreeDataGrid`
- ⚠️ Custom fields within an item: Need additional strategy (see Section 2.3)

---

### 2.2 Strategy 2: Container Recycling (Reuse Instead of Recreate)

**Concept**: When an item scrolls out of view, reuse its UI container for a newly visible item instead of destroying and recreating.

**How it works**:
```
Scroll down:
  [Item A] leaves viewport → Container A sent to recycle pool
  [Item F] enters viewport → Container A rebound to Item F data
```

**Benefits**:
- Eliminates GC pressure from control creation/destruction
- Reduces layout cost (container structure already exists)
- Amortizes template expansion cost

**Implementation (WPF)**:
```csharp
// Enable recycling mode
<ListView VirtualizingPanel.VirtualizationMode="Recycling">
  ...
</ListView>
```

**Implementation (Avalonia/WinUI)**:
```csharp
// ItemsRepeater with element lifecycle events
repeater.ElementPrepared += (s, e) => {
    // Bind new data to recycled element
    var item = e.Element as MyItemControl;
    item.DataContext = e.Item;
};

repeater.ElementClearing += (s, e) => {
    // Clean up before recycling
    var item = e.Element as MyItemControl;
    item.ClearState();
};
```

**Applicability to FieldWorks**:
- ✅ Senses/examples in lists: Perfect fit
- ⚠️ Custom fields: Template complexity may limit recycling benefit

---

### 2.3 Strategy 3: Incremental/Lazy Rendering (Progressive Disclosure)

**Concept**: Don't render everything at once; render progressively based on priority or visibility.

**Variations**:

#### 3.3.1 Deferred Rendering
```
Initial render: Show skeleton/placeholder
Idle callback: Fill in actual content
User interaction: Prioritize visible/focused areas
```

**Example (React Concurrent)**:
```jsx
function EntryView({ entry }) {
  return (
    <div>
      <HeaderSection entry={entry} />
      <Suspense fallback={<SenseListSkeleton />}>
        <SenseList senses={entry.senses} />
      </Suspense>
    </div>
  );
}
```

#### 3.3.2 Field-Level Virtualization
**Concept**: Within a complex form, only render fields that are expanded or in view.

```
Entry Form:
  ├── Basic Fields (always visible)      → Rendered immediately
  ├── Senses Section (collapsed)         → Render header only
  │     └── Sense 1 (expanded)           → Render full content
  │     └── Sense 2 (collapsed)          → Render summary only
  └── Custom Fields Section (collapsed)  → Render count badge only
```

#### 3.3.3 Priority-Based Rendering
```csharp
// Pseudocode for priority rendering
async Task RenderEntryAsync(Entry entry)
{
    // Phase 1: Critical path (headword, POS) - synchronous
    await RenderCriticalFieldsSync();
    
    // Phase 2: Primary content (definitions) - high priority
    await Dispatcher.InvokeAsync(() => RenderDefinitions(), 
        DispatcherPriority.Render);
    
    // Phase 3: Secondary content (examples, etymology) - low priority
    await Dispatcher.InvokeAsync(() => RenderSecondaryFields(), 
        DispatcherPriority.Background);
    
    // Phase 4: Custom fields - idle time
    await Dispatcher.InvokeAsync(() => RenderCustomFields(), 
        DispatcherPriority.ApplicationIdle);
}
```

**Applicability to FieldWorks**:
- ✅ Sections that can collapse: Etymology, History, Custom Fields
- ✅ Async rendering possible in Avalonia with proper dispatcher use
- ⚠️ Requires careful state management for partially-rendered views

---

### 2.4 Comparison Matrix

| Strategy | Complexity Reduction | Memory Reduction | Implementation Effort | Best For |
|----------|---------------------|------------------|----------------------|----------|
| UI Virtualization | O(N) → O(V) | High | Medium | Long lists (senses, examples) |
| Container Recycling | Amortized O(1) per scroll | Medium | Low | Any virtualized list |
| Incremental Rendering | Perceived perf improvement | Low | Medium-High | Complex forms, custom fields |
| Layout Caching | O(N) → O(1) repeated | Low | Medium | Repeated layouts |

---

## Section 3: Available NuGet Packages and Solutions for Modern .NET

### 3.1 Avalonia Ecosystem (Primary Target)

| Package | Purpose | Stars/Quality | Notes |
|---------|---------|---------------|-------|
| **Avalonia.Controls.TreeDataGrid** | Virtualized hierarchical grid | Built-in, High | Ideal for entry/sense hierarchies |
| **Avalonia.Controls.ItemsRepeater** | Building-block virtualization | Built-in, High | For custom virtualized layouts |
| **ReactiveUI.Avalonia** | MVVM + Reactive | 8k+ stars | Excellent for incremental updates |
| **DynamicData** | Observable collection manipulation | 3k+ stars | Efficient filtering/sorting |
| **Avalonia.Controls.DataGrid** | Standard data grid | Built-in | Less flexible than TreeDataGrid |

### 3.2 WinForms Optimization Packages

| Package | Purpose | Notes |
|---------|---------|-------|
| **ObjectListView** | Virtual mode ListView | Excellent for large lists, mature |
| **FastColoredTextBox** | Optimized text editing | If syntax highlighting needed |
| **DevExpress.WinForms** | Commercial control suite | Extensive virtualization support |
| **Syncfusion.WinForms** | Commercial control suite | TreeView with load-on-demand |

### 3.3 Cross-Platform / General .NET

| Package | Purpose | Notes |
|---------|---------|-------|
| **System.Reactive** | Reactive extensions | Foundation for incremental updates |
| **Microsoft.Extensions.ObjectPool** | Object pooling | Container recycling foundation |
| **BenchmarkDotNet** | Performance measurement | For validating improvements |
| **JetBrains.Profiler.Api** | Profiling integration | Identify remaining bottlenecks |

### 3.4 Recommended Stack for Avalonia Migration

```xml
<!-- Core Avalonia with virtualization support -->
<PackageReference Include="Avalonia" Version="11.*" />
<PackageReference Include="Avalonia.Controls.TreeDataGrid" Version="11.*" />

<!-- MVVM and reactivity -->
<PackageReference Include="ReactiveUI.Avalonia" Version="20.*" />
<PackageReference Include="DynamicData" Version="8.*" />

<!-- Profiling (Debug only) -->
<PackageReference Include="BenchmarkDotNet" Version="0.13.*" 
                  Condition="'$(Configuration)' == 'Debug'" />
```

### 3.5 Specific Solutions for Custom Fields

**Challenge**: Custom fields are the primary performance problem—50+ fields per sense is common.

**Solution Options**:

1. **Lazy Field Rendering with Expanders**
   ```xml
   <!-- Only render fields when section is expanded -->
   <Expander Header="Custom Fields ({Binding CustomFieldCount})">
     <ItemsRepeater ItemsSource="{Binding CustomFields}" />
   </Expander>
   ```

2. **Virtualized Property Grid**
   - Use `Avalonia.PropertyGrid` (community package)
   - Supports virtualization for large property counts

3. **On-Demand Field Loading**
   ```csharp
   // Load field values only when visible
   public class LazyFieldViewModel : ReactiveObject
   {
       private readonly Lazy<string> _value;
       public string Value => _value.Value;
       
       public LazyFieldViewModel(Func<string> loader)
       {
           _value = new Lazy<string>(loader);
       }
   }
   ```

---

## Section 4: Proposed Implementation Paths

### 4.1 Path A: Optimize Current WinForms + Views (Incremental)

**Approach**: Keep existing architecture, add performance optimizations.

**Key Changes**:

1. **Add Field-Level Virtualization to XmlVc**
   - Introduce "collapse by default" for custom field sections
   - Only expand/render when user expands section
   - Cache rendered state

2. **Implement DisplayCommand Caching**
   - Cache compiled DisplayCommand graphs by (classId, layoutHash)
   - Skip ProcessFrag tree walk for identical layouts

3. **Add Incremental Layout in Views**
   - Track dirty regions at the field level
   - Only re-layout changed subtrees

4. **Optimize Custom Field Generation**
   - Cache generated XML per field definition
   - Implement structural sharing for cloned nodes

**Estimated Complexity**:
| Change | Effort | Risk | Impact |
|--------|--------|------|--------|
| Collapse-by-default | Low | Low | Medium (perceived) |
| DisplayCommand caching | Medium | Medium | High |
| Incremental layout | High | High | High |
| XML caching | Medium | Low | Medium |

**Pros**:
- Minimal UI changes (users see same screens)
- Can be done incrementally
- Doesn't require full Avalonia migration
- Preserves all existing functionality

**Cons**:
- Still bound to COM boundary overhead
- Doesn't address fundamental C++ Views complexity
- Limited ceiling for improvement
- Technical debt accumulation

**Recommended For**: Quick wins while planning Avalonia migration

---

### 4.2 Path B: Hybrid Rendering (WinForms Host + Avalonia Controls)

**Approach**: Replace specific slow controls with Avalonia equivalents, hosted in WinForms.

**Key Changes**:

1. **Create Avalonia-based Entry Editor Control**
   ```csharp
   // Host Avalonia control in WinForms
   using Avalonia.Controls.Embedding;
   
   public class AvaloniaEntryEditor : AvaloniaControlHost
   {
       public AvaloniaEntryEditor()
       {
           Content = new EntryEditorView();
       }
   }
   ```

2. **Implement Virtualized Sense List in Avalonia**
   - Use `TreeDataGrid` for sense/subsense hierarchy
   - Only fetch data for visible items

3. **Keep Non-Performance-Critical UI in WinForms**
   - Menu bar, toolbar, status bar
   - Dialog boxes

**Architecture**:
```
┌─────────────────────────────────────────────────────────────────┐
│ WinForms Shell (existing)                                       │
│  ├── Menu, Toolbar, Status                                      │
│  └── Main Content Area                                          │
│       └── AvaloniaControlHost                                   │
│            └── EntryEditorView (Avalonia)                       │
│                 ├── HeaderSection                               │
│                 ├── TreeDataGrid (Senses - virtualized)         │
│                 └── CustomFieldsPanel (virtualized)             │
└─────────────────────────────────────────────────────────────────┘
```

**Pros**:
- Targeted performance improvement where needed
- Can migrate incrementally
- Maintains familiar UX
- Reduces risk vs full migration

**Cons**:
- Two UI frameworks to maintain
- Potential styling inconsistencies
- Complex hosting/interop
- May need to solve problems twice

**Recommended For**: When full migration timeline is uncertain

---

### 4.3 Path C: Full Avalonia Migration with Presentation IR

**Approach**: Complete migration to Avalonia with a clean Presentation IR layer.

**Key Changes**:

1. **Implement Presentation IR Layer** (per `presentation-ir-research.md`)
   - Compile Parts/Layout XML → typed IR once
   - Cache IR by configuration + layout version
   - Render IR to Avalonia controls

2. **Build Virtualized Entry Editor**
   ```csharp
   public class EntryEditorViewModel : ReactiveObject
   {
       // IR-driven field list
       public IObservable<IChangeSet<FieldIrNode>> FieldNodes { get; }
       
       // Virtualization-friendly flat list
       public ReadOnlyObservableCollection<FieldViewModel> VisibleFields { get; }
   }
   ```

3. **Implement Field-Level Virtualization**
   ```xml
   <TreeDataGrid Source="{Binding EntrySource}">
     <TreeDataGrid.Columns>
       <HierarchicalExpanderColumn>
         <HierarchicalExpanderColumn.CellTemplate>
           <DataTemplate DataType="vm:SectionViewModel">
             <StackPanel Orientation="Horizontal">
               <TextBlock Text="{Binding Label}" />
               <Badge Content="{Binding FieldCount}" />
             </StackPanel>
           </DataTemplate>
         </HierarchicalExpanderColumn.CellTemplate>
       </HierarchicalExpanderColumn>
       <TemplateColumn Header="Value">
         <TemplateColumn.CellTemplate>
           <DataTemplate DataType="vm:FieldViewModel">
             <ContentControl Content="{Binding Editor}" />
           </DataTemplate>
         </TemplateColumn.CellTemplate>
       </TemplateColumn>
     </TreeDataGrid.Columns>
   </TreeDataGrid>
   ```

4. **Async IR Compilation**
   ```csharp
   public async Task<EntryIr> CompileEntryViewAsync(int classId, string layoutName)
   {
       var cacheKey = (classId, layoutName, configVersion);
       
       if (_irCache.TryGetValue(cacheKey, out var cached))
           return cached;
       
       // Compile off UI thread
       var ir = await Task.Run(() => 
           _irCompiler.Compile(classId, layoutName));
       
       _irCache[cacheKey] = ir;
       return ir;
   }
   ```

**Architecture**:
```
┌─────────────────────────────────────────────────────────────────┐
│ Presentation IR Compiler (one-time, cached)                     │
│  ├── LayoutCache + Inventory (reused)                           │
│  ├── Parts/Layout XML → IR transformation                       │
│  └── Custom field expansion (cached per definition)             │
├─────────────────────────────────────────────────────────────────┤
│ Presentation IR (stable, typed)                                 │
│  ├── SectionNode, FieldNode, SequenceNode, GroupNode            │
│  ├── Stable IDs for virtualization                              │
│  └── Data binding specs (flid, wsRule, editorType)              │
├─────────────────────────────────────────────────────────────────┤
│ Avalonia Renderer                                               │
│  ├── IR → ViewModel transformation                              │
│  ├── Virtualized TreeDataGrid for senses/sections               │
│  └── Lazy field rendering (expand on demand)                    │
└─────────────────────────────────────────────────────────────────┘
```

**Pros**:
- Best long-term architecture
- Full virtualization at all levels
- No COM boundary overhead
- Modern, maintainable codebase
- Cross-platform potential

**Cons**:
- Highest initial effort
- Risk of regression during migration
- Requires significant testing
- Team needs Avalonia expertise

**Recommended For**: Long-term strategic direction

---

### 4.4 Comparison: Paths A, B, C vs Full Avalonia Rewrite

| Dimension | Path A (Optimize) | Path B (Hybrid) | Path C (IR + Avalonia) | Full Rewrite |
|-----------|-------------------|-----------------|------------------------|--------------|
| **Effort** | Low-Medium | Medium | Medium-High | Very High |
| **Risk** | Low | Medium | Medium | High |
| **Performance Gain** | 2-3× | 5-10× | 10-50× | 10-50× |
| **Maintainability** | Worse | Mixed | Better | Best |
| **Time to First Value** | 1-2 months | 3-4 months | 4-6 months | 12+ months |
| **Technical Debt** | Increases | Neutral | Decreases | Minimizes |
| **Cross-Platform** | No | Limited | Yes | Yes |

---

### 4.5 Recommendation: Staged Approach

**Phase 1 (Months 1-2): Quick Wins in Current System**
- Implement collapse-by-default for custom field sections
- Add DisplayCommand caching
- Measure impact with BenchmarkDotNet

**Phase 2 (Months 2-4): Presentation IR Foundation**
- Build IR compiler using Path 3 from `presentation-ir-research.md`
- Create IR → ViewModel mapping
- Validate with sample data in FwAvaloniaPreviewHost

**Phase 3 (Months 4-6): Avalonia Entry Editor**
- Implement virtualized `TreeDataGrid`-based entry editor
- Wire up to real LCModel data via IR
- A/B test against current editor

**Phase 4 (Months 6+): Full Migration**
- Replace remaining Views-based editors
- Deprecate C++ Views code
- Consider cross-platform deployment

---

## Appendix A: Key Files Reference

### Current Views Implementation
- `Src/views/VwRootBox.cpp` (5,230 lines) - Root box coordination
- `Src/views/VwEnv.cpp` (2,600 lines) - Box builder
- `Src/views/VwLazyBox.cpp` - Existing virtualization
- `Src/views/VwSelection.cpp` - Selection (marked "too slow")
- `Src/views/VwNotifier.h` - Change propagation

### XMLViews (Managed)
- `Src/Common/Controls/XMLViews/XmlVc.cs` (5,998 lines) - View constructor
- `Src/Common/Controls/XMLViews/LayoutCache.cs` - Layout resolution
- `Src/Common/Controls/XMLViews/PartGenerator.cs` - Custom field expansion
- `Src/Common/Controls/XMLViews/VectorReferenceVc.cs` - Sequence display

### Configuration
- `DistFiles/Language Explorer/Configuration/Parts/LexEntryParts.xml` - Entry layout

### Preview Host (for testing)
- `Src/Common/FwAvaloniaPreviewHost/` - Avalonia preview environment
- `scripts/Agent/Run-AvaloniaPreview.ps1` - Launch script

---

## Appendix B: Performance Measurement Plan

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class EntryRenderingBenchmarks
{
    private LexEntry _simpleEntry;    // 3 senses, 0 custom fields
    private LexEntry _complexEntry;   // 20 senses, 50 custom fields
    
    [Benchmark(Baseline = true)]
    public void RenderSimpleEntry_Current()
    {
        // Current XmlVc path
    }
    
    [Benchmark]
    public void RenderComplexEntry_Current()
    {
        // Current XmlVc path - expected slow
    }
    
    [Benchmark]
    public void RenderSimpleEntry_IR()
    {
        // IR-based rendering
    }
    
    [Benchmark]
    public void RenderComplexEntry_IR()
    {
        // IR-based rendering - should be much faster
    }
}
```

**Target Metrics**:
- Simple entry render: < 16ms (60fps)
- Complex entry render: < 50ms (acceptable)
- Scroll latency: < 8ms (120fps feel)
- Memory per entry: < 100KB

---

## Appendix C: Glossary

| Term | Definition |
|------|------------|
| **Box** | Native rendering unit in Views (VwParagraphBox, VwStringBox, etc.) |
| **DisplayCommand** | Compiled representation of a layout fragment in XmlVc |
| **Frag** | Fragment ID identifying a layout/part in the XML config |
| **IR** | Intermediate Representation - typed tree describing what to render |
| **Lazy Box** | Placeholder box that expands on demand (VwLazyBox) |
| **Parts/Layout** | XML configuration files defining how to display LCModel objects |
| **Presentation IR** | Proposed abstraction layer between config and Avalonia |
| **Virtualization** | Rendering only visible items instead of all items |
| **View Constructor** | Class that interprets Parts/Layout to build UI (XmlVc) |
