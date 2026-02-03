# FieldWorks Views Architecture Research

## Executive Summary

The FieldWorks Views system is a sophisticated C++ rendering engine (~66.7K lines in `Src/views/`) that implements a box-based layout system for complex multilingual text. It provides the bridge between managed C# view constructors (like `XmlVc`) and native rendering through COM interfaces.

---

## Key Files Found

### Native C++ Views (`Src/views/`)

| File | Lines | Description |
|------|-------|-------------|
| [VwRootBox.cpp](../../Src/views/VwRootBox.cpp) | 5,230 | Root display box, coordinates layout/paint, manages selections |
| [VwRootBox.h](../../Src/views/VwRootBox.h) | 690 | Header for VwRootBox with IVwRootBox COM interface |
| [VwEnv.cpp](../../Src/views/VwEnv.cpp) | 2,600 | Display environment for view construction (implements IVwEnv) |
| [VwEnv.h](../../Src/views/VwEnv.h) | 353 | Header for VwEnv, tracks NotifierRec for change notification |
| [VwSimpleBoxes.cpp](../../Src/views/VwSimpleBoxes.cpp) | 3,729 | Base box classes: VwBox, VwGroupBox, VwPileBox, VwDivBox |
| [VwSimpleBoxes.h](../../Src/views/VwSimpleBoxes.h) | 1,504 | Box hierarchy definitions with layout/draw methods |
| [VwTextBoxes.cpp](../../Src/views/VwTextBoxes.cpp) | ~9,000 | VwParagraphBox, VwStringBox - text rendering |
| [VwLazyBox.cpp](../../Src/views/VwLazyBox.cpp) | ~500 | Lazy-loading boxes for virtualization |
| [VwLazyBox.h](../../Src/views/VwLazyBox.h) | 200 | VwLazyBox header, VwLazyInfo for deferred expansion |
| [VwNotifier.h](../../Src/views/VwNotifier.h) | 684 | Change notification system, VwAbstractNotifier |
| [VwSelection.cpp](../../Src/views/VwSelection.cpp) | ~11,300 | Text selection and editing |
| [VwPropertyStore.cpp](../../Src/views/VwPropertyStore.cpp) | ~1,800 | Text formatting properties |
| [Views.idh](../../Src/views/Views.idh) | 5,039 | IDL definitions for all Views COM interfaces |

### Managed View Constructors (`Src/Common/Controls/XMLViews/`)

| File | Lines | Description |
|------|-------|-------------|
| [XmlVc.cs](../../Src/Common/Controls/XMLViews/XmlVc.cs) | 5,998 | Primary managed view constructor, interprets XML layouts |
| DisplayCommand classes (in XmlVc.cs) | ~300 | Command pattern for display operations |

### Managed Interfaces (`Src/Common/ViewsInterfaces/`)

| File | Description |
|------|-------------|
| [Views.cs](../../Src/Common/ViewsInterfaces/Views.cs) | 16,561 lines of generated C# interfaces from IDL |
| [AGENTS.md](../../Src/Common/ViewsInterfaces/AGENTS.md) | Documentation of interfaces |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MANAGED LAYER (C#)                                │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐    │
│  │   XmlVc          │     │  DisplayCommand  │     │   LayoutCache    │    │
│  │  (View Const.)   │────▶│  (frag dispatch) │────▶│  (XML layouts)   │    │
│  └────────┬─────────┘     └──────────────────┘     └──────────────────┘    │
│           │                                                                 │
│           │ IVwViewConstructor.Display(IVwEnv, hvo, frag)                   │
│           ▼                                                                 │
│  ┌──────────────────┐                                                      │
│  │ IVwEnv           │ ◀── COM Interface (marshalled to native)              │
│  │ AddObjProp()     │                                                      │
│  │ AddStringProp()  │                                                      │
│  │ OpenParagraph()  │                                                      │
│  │ CloseParagraph() │                                                      │
│  └────────┬─────────┘                                                      │
├───────────┼─────────────────────────────────────────────────────────────────┤
│           │            NATIVE LAYER (C++)                                   │
├───────────┼─────────────────────────────────────────────────────────────────┤
│           ▼                                                                 │
│  ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐    │
│  │   VwEnv          │────▶│  Box Hierarchy   │────▶│   VwNotifier     │    │
│  │  (IVwEnv impl)   │     │  (builds boxes)  │     │  (change track)  │    │
│  └──────────────────┘     └────────┬─────────┘     └──────────────────┘    │
│                                    │                                        │
│  ┌─────────────────────────────────┼────────────────────────────────────┐  │
│  │                    BOX HIERARCHY                                      │  │
│  │  ┌──────────────┐              │              ┌──────────────────┐   │  │
│  │  │ VwRootBox    │──────────────┼─────────────▶│ VwPropertyStore  │   │  │
│  │  │ (root coord) │              │              │ (formatting)     │   │  │
│  │  └──────┬───────┘              │              └──────────────────┘   │  │
│  │         │                      │                                      │  │
│  │         ▼                      ▼                                      │  │
│  │  ┌──────────────┐       ┌──────────────┐       ┌──────────────┐      │  │
│  │  │ VwDivBox     │──────▶│ VwPileBox    │──────▶│ VwLazyBox    │      │  │
│  │  │ (division)   │       │ (vertical)   │       │ (deferred)   │      │  │
│  │  └──────────────┘       └──────────────┘       └──────────────┘      │  │
│  │         │                      │                                      │  │
│  │         ▼                      ▼                                      │  │
│  │  ┌──────────────┐       ┌──────────────┐       ┌──────────────┐      │  │
│  │  │VwParagraphBox│──────▶│ VwStringBox  │──────▶│ VwTableBox   │      │  │
│  │  │ (paragraph)  │       │ (text run)   │       │ (table)      │      │  │
│  │  └──────────────┘       └──────────────┘       └──────────────┘      │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                    │                                        │
│                                    ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                      RENDERING PIPELINE                               │  │
│  │  Layout(IVwGraphics, width) ──▶ DoLayout() ──▶ DrawRoot() ──▶ Draw() │  │
│  │           │                          │              │           │     │  │
│  │           ▼                          ▼              ▼           ▼     │  │
│  │  [Measure/Position]         [Compute Heights]  [DrawBorder]  [Paint] │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Rendering Pipeline

### 1. Construction Phase (`VwRootBox::Construct`)

```cpp
// VwRootBox.cpp:4176
void VwRootBox::Construct(IVwGraphics * pvg, int dxAvailWidth)
{
    VwEnvPtr qvwenv;
    qvwenv.Attach(MakeEnv());
    qvwenv->Initialize(pvg, this, m_vqvwvc[0]);

    for (int i = 0; i < m_chvoRoot; i++)
    {
        qvwenv->OpenObject(m_vhvo[i]);
        // Calls managed IVwViewConstructor.Display() via COM
        CheckHr(m_vqvwvc[i]->Display(qvwenv, m_vhvo[i], m_vfrag[i]));
        qvwenv->CloseObject();
    }
    qvwenv->Cleanup();
    m_fConstructed = true;
}
```

**Key points:**
- Creates `VwEnv` instance to receive box-building calls
- View constructor (e.g., `XmlVc`) calls back into `IVwEnv` methods
- `VwEnv` builds box tree and notifier structure

### 2. Layout Phase (`VwRootBox::Layout`)

```cpp
// VwRootBox.cpp:2834
STDMETHODIMP VwRootBox::Layout(IVwGraphics * pvg, int dxAvailWidth)
{
    if (!m_fConstructed)
        Construct(pvg, dxAvailWidth);
    VwDivBox::DoLayout(pvg, dxAvailWidth, -1, true);
}
```

**Layout propagates down:**
- `VwDivBox::DoLayout()` → children's `DoLayout()`
- `VwParagraphBox::DoLayout()` → line breaking, segment creation
- Each box computes `m_dxsWidth`, `m_dysHeight`, positions children

### 3. Drawing Phase (`VwRootBox::DrawRoot`)

```cpp
// VwRootBox.cpp:2744
STDMETHODIMP VwRootBox::DrawRoot(IVwGraphics * pvg, RECT rcSrcRoot1, RECT rcDstRoot1, ComBool fDrawSel)
{
    Draw(pvg, rcSrcRoot, rcDstRoot);  // Inherited from VwBox
    if (m_qvwsel && fDrawSel)
        m_qvwsel->DrawIfShowing(pvg, rcSrcRoot, rcDstRoot, -1, INT_MAX);
    MaximizeLaziness();  // Convert real boxes back to lazy if scrolled away
}
```

**Drawing propagates down:**
```cpp
// VwSimpleBoxes.cpp:642
void VwBox::Draw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
    DrawBorder(pvg, rcSrc, rcDst);      // Background + borders
    DrawForeground(pvg, rcSrc, rcDst);  // Content (overridden by subclasses)
}
```

### 4. Invalidation/Update (`VwRootBox::PropChanged`)

```cpp
// VwRootBox.cpp - PropChanged is called when data changes
STDMETHODIMP VwRootBox::PropChanged(HVO hvo, PropTag tag, int ivMin, int cvIns, int cvDel)
{
    // Finds notifiers tracking the changed property
    // Triggers regeneration of affected boxes
    // Re-layout and repaint
}
```

---

## XmlVc and View Constructor Relationship

### How XmlVc Drives Rendering

```csharp
// XmlVc.cs - Display method receives IVwEnv and emits box-building calls
public override void Display(IVwEnv vwenv, int hvo, int frag)
{
    DisplayCommand cmd;
    if (m_idToDisplayCommand.TryGetValue(frag, out cmd))
    {
        cmd.PerformDisplay(this, frag, hvo, vwenv);
    }
}
```

### DisplayCommand Pattern

```csharp
// XmlVc.cs:4955
public abstract class DisplayCommand
{
    abstract internal void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv);
}

public class NodeDisplayCommand : DisplayCommand
{
    internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
    {
        vc.ProcessFrag(m_node, vwenv, hvo, true, null);
    }
}
```

### Key IVwEnv Methods Called by XmlVc

| Method | Purpose |
|--------|---------|
| `OpenParagraph()` | Start a paragraph box |
| `CloseParagraph()` | End paragraph |
| `AddStringProp(tag, vc)` | Add string property content |
| `AddObjVecItems(tag, vc, frag)` | Iterate over vector property |
| `AddLazyVecItems(tag, vc, frag)` | Lazy iteration (virtualized) |
| `NoteDependency(hvos, tags, count)` | Register for change notifications |
| `put_IntProperty(prop, var, val)` | Set formatting properties |

---

## Custom Fields Expansion

Custom fields are handled through:

1. **LayoutCache** - Caches XML layout nodes
2. **PartGenerator** - Generates XML parts for custom fields dynamically
3. **XmlVc.GetNodeForPart()** - Looks up layout by class ID and layout name

```csharp
// XmlVc.cs
protected internal XmlNode GetNodeForPart(int hvo, string layoutName, bool fIncludeLayouts)
{
    int clsid = m_sda.get_IntProp(hvo, CmObjectTags.kflidClass);
    return m_layouts.GetNode(clsid, layoutName, fIncludeLayouts);
}
```

**Performance concern**: Custom field expansion can clone XML nodes repeatedly, which is expensive.

---

## Performance Bottlenecks (from code comments)

### 1. Layout Stream (VwLayoutStream.cpp)

```cpp
// VwLayoutStream.cpp:647
// Optimize: since the box's size didn't change...
// VwLayoutStream.cpp:1433
// Optimize JohnT: GetALineTopAndBottom already iterates over boxes in line
```

### 2. Selection Operations (VwSelection.cpp)

```cpp
// VwSelection.cpp:4624
// OPTIMIZE JohnT: look for ways to optimize this.

// VwSelection.cpp:7769
// OPTIMIZE JohnT: create a way to delete several in one go

// VwSelection.cpp:10149
// Review JohnT(SteveMc): This is way too slow, and VwSelection may be too low
```

### 3. Property Changes

```cpp
// Views.idh:637
// For properties that are expensive to compute and change less often...

// Views.idh:720
// performance problems result.
```

### 4. Pattern Matching (VwPattern.cpp)

```cpp
// VwPattern.cpp:718
// OPTIMIZE JohnT: to better support laziness, pass false here

// VwPattern.cpp:1117
// Optimize: (a) We could get just the current search range if not matching whole words.
```

---

## Core Algorithms

### Box Layout Algorithm

1. **Top-down width propagation**: Parent tells children available width
2. **Bottom-up height computation**: Children report computed height
3. **Position assignment**: Parent positions children based on accumulated heights

```cpp
// VwSimpleBoxes.cpp - Relayout process
// The process begins by constructing a FixupMap, which contains the changed box
// and all boxes that need their layout updated as a result.
```

### Lazy Box Expansion

```cpp
// VwLazyBox.h
class VwLazyBox : public VwBox
{
    VwLazyInfo m_vwlziItems;  // HVOs and estimated heights
    int m_ihvoMin;            // First index in original list
    IVwViewConstructorPtr m_qvc;  // View constructor for expansion
    int m_frag;               // Fragment ID for Display calls

    void ExpandItems(int ihvoMin, int ihvoLim, bool * pfForcedScroll = NULL);
    void ExpandForDisplay(IVwGraphics * pvg, Rect rcSrc, Rect rcDst, int dxsAvailWidth);
};
```

### Change Notification (Notifier System)

```cpp
// VwNotifier.h
// One notifier keeps track of everything displayed as part of one occurrence
// of one view of one object.
class VwAbstractNotifier : public IVwNotifyChange
{
    HVO m_hvo;           // Object being displayed
    VwBox* m_pboxKey;    // First box containing this content
    VwNotifier* m_qnoteParent;  // Parent notifier

    STDMETHOD(PropChanged)(HVO obj, int tag, int ivMin, int cvIns, int cvDel);
};
```

---

## Managed/Native Bridge

### COM Interface Flow

```
Managed (C#)              COM Boundary              Native (C++)
────────────────────────────────────────────────────────────────
XmlVc.Display()    ──────────────────────▶    VwEnv receives calls
  │                                              │
  ├─ vwenv.OpenParagraph()  ──────────▶    VwEnv::OpenParagraph()
  │                                              │ creates VwParagraphBox
  │                                              ▼
  ├─ vwenv.AddStringProp()  ──────────▶    VwEnv::AddStringProp()
  │                                              │ creates VwStringBox
  │                                              ▼
  └─ vwenv.CloseParagraph() ──────────▶    VwEnv::CloseParagraph()
                                                 │ finalizes paragraph
```

### Key Interfaces

| Interface | Side | Purpose |
|-----------|------|---------|
| `IVwViewConstructor` | Managed | View constructor callbacks |
| `IVwEnv` | Native | Box building environment |
| `IVwRootBox` | Native | Root box control |
| `ISilDataAccess` | Both | Data access layer |
| `IVwSelection` | Native | Selection management |

---

---

## Deep Dive: Native C++ Views Implementation

### VwRootBox - Layout and Drawing Coordination

**File**: [VwRootBox.cpp](../../Src/views/VwRootBox.cpp) (5,230 lines)

The `VwRootBox` is the top-level coordinator for the entire view:

```cpp
// Initialization (VwRootBox.cpp:51-68)
void VwRootBox::Init()
{
    m_cref = 1;
    m_fDirty = false;
    m_fNewSelection = false;
    m_fInDrag = false;
    m_hrSegmentError = S_OK;
    m_cMaxParasToScan = 4;  // Limits lazy expansion during operations
    m_ptDpiSrc.x = 96;
    m_ptDpiSrc.y = 96;
}
```

**Key methods:**

| Method | Line | Purpose |
|--------|------|---------|
| `Layout()` | 2834 | Entry point for layout pass |
| `DrawRoot()` | 2744 | Entry point for painting |
| `PrepareToDraw()` | 2723 | Expands lazy boxes before draw |
| `PropChanged()` | 189 | Handles data change notifications |
| `Reconstruct()` | - | Rebuilds entire display |
| `SetRootObjects()` | 285 | Configures root HVOs and view constructors |

**Drawing pipeline sequence:**
```cpp
// VwRootBox.cpp:2744-2802
STDMETHODIMP VwRootBox::DrawRoot(IVwGraphics * pvg, RECT rcSrcRoot1, RECT rcDstRoot1,
    ComBool fDrawSel)
{
    // 1. Draw the box hierarchy
    Draw(pvg, rcSrcRoot, rcDstRoot);
    
    // 2. Draw selection overlay (if any)
    if (m_qvwsel && fDrawSel)
        m_qvwsel->DrawIfShowing(pvg, rcSrcRoot, rcDstRoot, -1, INT_MAX);
    
    // 3. Reclaim memory by converting visible boxes to lazy (virtualization)
    if (m_ydTopLastDraw != rcDstRoot.TopLeft().y)
    {
        m_ydTopLastDraw = rcDstRoot.TopLeft().y;
        MaximizeLaziness();  // Key virtualization step!
    }
}
```

---

### VwEnv - Box Building from View Constructors

**File**: [VwEnv.cpp](../../Src/views/VwEnv.cpp) (2,600 lines)

`VwEnv` implements `IVwEnv` and receives calls from managed view constructors (like `XmlVc`):

```cpp
// VwEnv.cpp:71-99 - Initialization
void VwEnv::Initialize(IVwGraphics * pvg, VwRootBox * pzrootb, IVwViewConstructor * pvc)
{
    m_qzvps.Attach(MakePropertyStore()); // Default formatting
    m_qrootbox = pzrootb;
    m_qsda = pzrootb->GetDataAccess();
    m_qvg = pvg;
    m_pgboxCurr = pzrootb;  // Start building boxes in root
    m_vpgboxOpen.Push(m_pgboxCurr);
}
```

**Key box-building methods:**

| Method | Purpose |
|--------|---------|
| `OpenParagraph()` | Creates and opens a `VwParagraphBox` |
| `CloseParagraph()` | Closes current paragraph |
| `AddString(ITsString*)` | Adds string to current paragraph |
| `AddStringProp(tag, vc)` | Fetches string from data and displays |
| `AddObjVecItems(tag, vc, frag)` | Iterates vector property immediately |
| `AddLazyVecItems(tag, vc, frag)` | Creates `VwLazyBox` for deferred expansion |
| `OpenProp()` / `CloseProp()` | Tracks property boundaries for notifiers |

**AddLazyVecItems creates virtualized content:**
```cpp
// VwEnv.cpp:453-492
STDMETHODIMP VwEnv::AddLazyVecItems(int tag, IVwViewConstructor * pvvc, int frag)
{
    // Verify all containers are VwDivBox (required for lazy expansion)
    for (VwGroupBox * pgbox = m_pgboxCurr; pgbox; pgbox = pgbox->Container())
        if (!dynamic_cast<VwDivBox *>(pgbox))
            ThrowHr(WarnHr(E_UNEXPECTED));

    int cobj;
    CheckHr(m_qsda->get_VecSize(m_hvoCurr, tag, &cobj));
    if (cobj)
    {
        // Create lazy box with list of HVOs (doesn't display yet!)
        plzb = NewObj VwLazyBox(m_qzvps, NULL, 0, 0, pvvc, frag, m_hvoCurr);
        for (int i = 0; i < cobj; i++)
        {
            HVO hvoItem;
            CheckHr(m_qsda->get_VecItem(m_hvoCurr, tag, i, &hvoItem));
            plzb->m_vwlziItems.Push(hvoItem);
        }
        AddBox(plzb);
    }
}
```

---

### VwLazyBox - Virtualization/Lazy Loading

**File**: [VwLazyBox.cpp](../../Src/views/VwLazyBox.cpp) (1,501 lines)

The `VwLazyBox` implements UI virtualization by deferring box creation:

```cpp
// VwLazyBox.cpp:127-158 - Constructor
VwLazyBox::VwLazyBox(VwPropertyStore * pzvps, HVO * prghvoItems, int chvoItems, 
    int ihvoMin, IVwViewConstructor * pvc, int frag, HVO hvoContext)
    :VwBox(pzvps)
{
    m_vwlziItems.Replace(0, 0, prghvoItems, chvoItems);  // Store HVO list
    m_ihvoMin = ihvoMin;
    m_qvc = pvc;            // Remember view constructor
    m_frag = frag;          // Fragment ID for Display calls
    m_hvoContext = hvoContext;
    m_fInLayout = false;    // Guard against reentrant layout
}
```

**Key algorithms:**

#### 1. Item Expansion (on-demand)
```cpp
// VwLazyBox.cpp:225-270 - ExpandItems
void VwLazyBox::ExpandItems(int ihvoMin, int ihvoLim, bool * pfForcedScroll)
{
    VwNotifier * pnoteBest = FindMyNotifier(ipropBest, tag);
    VwRootBox * prootb = Root();

    HoldGraphics hg(prootb);
    Rect rcThisOld = GetBoundsRect(hg.m_qvg, hg.m_rcSrcRoot, hg.m_rcDstRoot);
    
    VwDivBox * pdboxContainer = dynamic_cast<VwDivBox *>(Container());
    ExpandItemsNoLayout(ihvoMin, ihvoLim, pnoteBest, ipropBest, tag, 
        &pboxFirstLayout, &pboxLimLayout);
    
    // WARNING: *this may have been deleted at this point!
    prootb->AdjustBoxPositions(rcRootOld, pboxFirstLayout, pboxLimLayout, 
        rcThisOld, pdboxContainer, pfForcedScroll, NULL, true);
}
```

#### 2. Height Estimation (for scroll bar accuracy)
```cpp
// VwLazyBox.cpp:772-810 - DoLayout for lazy boxes
void VwLazyBox::DoLayout(IVwGraphics* pvg, int dxsAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
    m_fInLayout = true;  // Guard flag
    if (m_vwlziItems.Size())
    {
        // Cache uniform heights for optimization
        if (m_dxsWidth == dxsAvailWidth && m_dysUniformHeightEstimate != 0)
        {
            m_dysHeight = m_dysUniformHeightEstimate * m_vwlziItems.Size();
        }
        else
        {
            // Ask view constructor for height estimates
            for (int i = 0; i < m_vwlziItems.Size(); i++)
            {
                CheckHr(m_qvc->EstimateHeight(m_vwlziItems.GetHvo(i), m_frag, 
                    dxsAvailWidth, &itemHeight));
                itemHeight = MulDiv(itemHeight > 0 ? itemHeight : 1, dypInch, 72);
                m_vwlziItems.SetEstimatedHeight(i, itemHeight);
                m_dysHeight += itemHeight;
            }
        }
    }
    m_fInLayout = false;
    m_dxsWidth = dxsAvailWidth;
}
```

#### 3. Lazy-to-Real Conversion (LazinessIncreaser)
```cpp
// VwLazyBox.cpp:1050-1108 - Reverse virtualization
void LazinessIncreaser::ConvertAsMuchAsPossible()
{
    while (FindSomethingToConvert())
        ConvertIt();  // Convert real boxes back to lazy when scrolled away
}
```

**Key virtualization data structure:**
```cpp
// VwLazyBox.h - VwLazyInfo stores HVOs and estimated heights
class VwLazyInfo
{
    Vector<HVO> m_vhvo;              // Object IDs
    Vector<long> m_vloEstimatedHeight;  // Estimated pixel heights
};
```

---

### VwSimpleBoxes - Layout Algorithm

**File**: [VwSimpleBoxes.cpp](../../Src/views/VwSimpleBoxes.cpp) (3,729 lines)

#### Box Layout Algorithm (VwPileBox)
```cpp
// VwSimpleBoxes.cpp:2253-2300 - DoLayout for piles (vertical stacking)
void VwPileBox::DoLayout(IVwGraphics* pvg, int dxpAvailWidth, int dxpAvailOnLine, bool fSyncTops)
{
    int dxpInch;
    CheckHr(pvg->get_XUnitsPerInch(&dxpInch));
    int dxpSurroundWidth = SurroundWidth(dxpInch);
    int dxpInnerAvailWidth = dxpAvailWidth - dxpSurroundWidth;

    // Layout each child with available width
    PileLayoutBinder plb(pvg, dxpInnerAvailWidth, m_qzvps->MaxLines(), fSyncTops);
    this->ForEachChild(plb);

    // Position children vertically
    AdjustInnerBoxes(pvg, fSyncTops ? Root()->GetSynchronizer() : NULL);
}
```

#### Drawing Propagation
```cpp
// VwSimpleBoxes.cpp:1626-1640 - GroupBox draws children
void VwGroupBox::DrawForeground(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
    rcSrc.Offset(-m_xsLeft, -m_ysTop);  // Transform to child coords
    GroupDrawBinder gdb(pvg, rcSrc, rcDst);
    this->ForEachChild(gdb);  // Draw each child
}

// VwSimpleBoxes.cpp:830-837 - Base box Draw method
void VwBox::Draw(IVwGraphics * pvg, Rect rcSrc, Rect rcDst)
{
    DrawBorder(pvg, rcSrc, rcDst);      // 1. Background + borders
    DrawForeground(pvg, rcSrc, rcDst);  // 2. Content
}
```

#### Invalidation and Relayout
```cpp
// VwSimpleBoxes.cpp:132-172 - Relayout optimization
bool VwBox::Relayout(IVwGraphics * pvg, int dxpAvailWidth, VwRootBox * prootb,
    FixupMap * pfixmap, int dxpAvailOnLine, BoxIntMultiMap * pmmbi)
{
    Rect vrect;
    VwBox * pboxThis = this;
    bool fGotOldRect = pfixmap->Retrieve(pboxThis, &vrect);
    
    if (fGotOldRect)
        Root()->InvalidateRect(&vrect); // Invalidate old position

    // Only re-layout if in fix map or never laid out
    if (fGotOldRect || m_dysHeight == 0)
    {
        this->DoLayout(pvg, dxpAvailWidth, dxpAvailOnLine);
        return true;  // Caller should invalidate new position
    }
    return false;
}
```

---

### Painting Pipeline Summary

```
┌─────────────────────────────────────────────────────────────────────┐
│                        PAINTING PIPELINE                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. PrepareToDraw(pvg, rcSrc, rcDst)                                │
│     └─► VwDivBox::PrepareToDraw() - expands lazy boxes in clip rect │
│         └─► VwLazyBox::ExpandItems() - creates real boxes           │
│                                                                      │
│  2. DrawRoot(pvg, rcSrc, rcDst, fDrawSel)                           │
│     └─► VwBox::Draw() - for each box:                               │
│         ├─► DrawBorder() - background color + borders               │
│         └─► DrawForeground() - content (recursive for groups)       │
│             └─► VwGroupBox: calls Draw() on each child              │
│             └─► VwStringBox: renders text segment                   │
│             └─► VwLazyBox: DEBUG warning (should not draw!)         │
│                                                                      │
│  3. m_qvwsel->DrawIfShowing() - selection overlay                   │
│                                                                      │
│  4. MaximizeLaziness() - convert off-screen boxes back to lazy      │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

### Selection and Invalidation

**How selection affects redraw:**
```cpp
// VwRootBox.cpp:2744-2802
// Selection is drawn AFTER main content to ensure proper overlay
if (m_qvwsel && fDrawSel)
    m_qvwsel->DrawIfShowing(pvg, rcSrcRoot, rcDstRoot, -1, INT_MAX);
```

**Dirty rectangle handling:**
```cpp
// VwSimpleBoxes.cpp:887-905 - GetInvalidateRect
Rect VwBox::GetInvalidateRect()
{
    // Rectangle relative to root, with 2px margin for IP overhang
    Rect rcRet(Left(), Top(), Right(), VisibleBottom());
    for (VwGroupBox * pgbox = Container(); pgbox; pgbox = pgbox->Container())
        rcRet.Offset(pgbox->Left(), pgbox->Top());
    rcRet.Inflate(2, 2);  // Margin for insertion point
    return rcRet;
}
```

---

### Performance Issues Found in Comments

| File | Line | Issue |
|------|------|-------|
| VwLazyBox.cpp | 339 | Reentrant expansion guard (`m_fInLayout`) |
| VwSimpleBoxes.cpp | 1553 | "OPTIMIZE JohnT: there may be other containers smart enough" |
| VwRootBox.cpp | 854 | 1/10 second timeout for MakeSimpleSel |
| VwSelection.cpp | Various | Multiple OPTIMIZE comments for selection operations |

---

### Caching Mechanisms Found

1. **Uniform height estimate cache** in `VwLazyBox`:
   ```cpp
   int m_dysUniformHeightEstimate;  // Cached if all items same height
   ```

2. **Notifier map** in `VwRootBox`:
   ```cpp
   ObjNoteMap m_mmhvoqnote;  // HVO → Notifier multimap
   ```

3. **Property store caching** through `VwPropertyStore::InitialStyle()`:
   ```cpp
   // Returns reset style for inherited properties
   VwPropertyStore * pzvps->InitialStyle()
   ```

---

## Implications for Avalonia Migration

1. **IVwEnv abstraction is key**: The managed code already talks to an abstract `IVwEnv` interface. A managed-only `IVwEnv` implementation can intercept calls and produce an IR instead of native boxes.

2. **DisplayCommand layer is stable**: The command pattern in `XmlVc` provides a typed intermediate representation that can be reused.

3. **Lazy loading exists**: `VwLazyBox` demonstrates that virtualization is already supported; this pattern can inform Avalonia virtualization using `ItemsRepeater` or similar.

4. **Performance issues are documented**: The guards and comments point to known bottlenecks:
   - Reentrant layout (guarded by `m_fInLayout`)
   - Selection operations need optimization
   - Height estimation could be cached more aggressively

5. **Notifier system for updates**: The change notification system (`VwNotifier`) provides a model for reactive updates in Avalonia.

6. **MaximizeLaziness pattern**: The reverse-virtualization during scroll shows the importance of reclaiming resources for large lists.

---

## References

- [Src/views/AGENTS.md](../../Src/views/AGENTS.md) - Views module documentation
- [Src/Common/ViewsInterfaces/AGENTS.md](../../Src/Common/ViewsInterfaces/AGENTS.md) - Interface documentation
- [presentation-ir-research.md](presentation-ir-research.md) - Presentation IR research (existing)

