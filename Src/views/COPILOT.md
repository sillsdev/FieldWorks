---
last-reviewed: 2025-10-31
last-verified-commit: 73fb49b
status: reviewed
---

# views

## Purpose
Native C++ rendering engine (~66.7K lines) implementing sophisticated box-based layout and display for complex writing systems. Provides VwRootBox (root display), VwParagraphBox (paragraph layout), VwStringBox (text runs), VwTableBox (table layout), VwSelection (text selection), VwEnv (display environment), and VwPropertyStore (formatting properties). Core component enabling accurate multi-script text layout, bidirectional text, complex rendering, and accessible UI across all FieldWorks applications.

## Key Components

### Box Hierarchy (VwSimpleBoxes.h, VwTextBoxes.h, VwTableBox.h)
- **VwBox** - Abstract base class for all display boxes
- **VwGroupBox** - Container box for nested boxes
- **VwParagraphBox** - Paragraph layout with line breaking, justification
- **VwConcParaBox** - Concatenated paragraph box for interlinear text
- **VwStringBox** - Text run display with font, color, style
- **VwDropCapStringBox** - Drop capital initial letter
- **VwTableBox**, **VwTableRowBox**, **VwTableCellBox** - Table layout (VwTableBox.cpp/h)
- **VwDivBox**, **VwAnchorBox** - Division and anchor boxes
- **VwLazyBox** (VwLazyBox.cpp/h) - Lazy-loading container for performance

### Root and Environment
- **VwRootBox** (VwRootBox.cpp/h) - Top-level display root, owns VwSelection, coordinates layout/paint
  - `Layout(IVwGraphics* pvg, int dxsAvailWidth)` - Performs layout pass
  - `DrawRoot(IVwGraphics* pvg, RECT* prcpDraw, ...)` - Renders to graphics context
  - Manages: m_qvss (selections), m_pvpbox (paragraph boxes), m_qdrs (data access)
- **VwEnv** (VwEnv.cpp/h) - Display environment for view construction
  - Implements IVwEnv COM interface for managed callers
  - `OpenParagraph()`, `CloseParagraph()`, `AddString(ITsString* ptss)` - Display element creation
  - **NotifierRec** - Tracks display notifications

### Selection and Interaction
- **VwSelection** (VwSelection.cpp/h) - Abstract base for text selections
- **VwTextSelection** - Text range selection with IP (insertion point) support
  - `Install()` - Activates selection for keyboard input
  - `EndKey(bool fSuppressClumping)` - Handles End key navigation

### Text Storage and Access (VwTextStore.cpp/h, VwTxtSrc.cpp/h)
- **VwTextStore** - Text storage interface for COM Text Services Framework (TSF)
- **VwTxtSrc** - Text source abstraction for string iteration

### Property Management (VwPropertyStore.cpp/h)
- **VwPropertyStore** - Stores text formatting properties (font, color, alignment, etc.)
  - Implements ITsTextProps for interop with TsString system

### Rendering Utilities
- **VwLayoutStream** (VwLayoutStream.cpp/h) - Stream-based layout coordination
- **VwPrintContext** (VwPrintContext.cpp/h) - Print layout context
- **VwOverlay** (VwOverlay.cpp/h) - Overlay graphics for selection highlighting
- **VwPattern** (VwPattern.cpp/h) - Pattern matching for search/replace display

### Synchronization and Notifications
- **VwSynchronizer** (VwSynchronizer.cpp/h) - Synchronizes display updates with data changes
- **VwNotifier**, **VwAbstractNotifier** (VwNotifier.cpp/h) - Change notification system
- **VwInvertedViews** (VwInvertedViews.cpp/h) - Manages inverted (reflected) view hierarchies

### Accessibility (Windows-specific)
- **VwAccessRoot** (VwAccessRoot.cpp/h) - IAccessible implementation for screen readers (WIN32/WIN64 only)

## COM Interfaces (IDL files)
- **Views.idh**, **ViewsTlb.idl**, **ViewsPs.idl** - COM interface definitions
- **Render.idh** - Rendering interfaces
- Exports: IVwRootBox, IVwEnv, IVwSelection, IVwPropertyStore, IVwGraphics, IVwLayoutStream, IVwOverlay

## Dependencies
- **Upstream**: Kernel (low-level utilities, COM infrastructure), Generic (ComSmartPtr, collections), AppCore (GDI wrappers, styled text), Cellar (XML parsing for FwXml)
- **Downstream consumers**: Common/ViewsInterfaces (COM wrappers), ManagedVwWindow (HWND wrapper), Common/RootSite (SimpleRootSite, CollectorEnv), Common/SimpleRootSite (EditingHelper), all UI displaying formatted text
- **External**: Windows GDI/GDI+, Text Services Framework (TSF) for advanced input

## Test Infrastructure
- **Test/** subfolder (excluded from main line count)
- Native C++ tests for box layout, selection, rendering

## Related Folders
- **Common/ViewsInterfaces/** - Managed COM wrappers for Views interfaces
- **ManagedVwWindow/** - Managed IVwWindow implementation
- **ManagedVwDrawRootBuffered/** - Managed double-buffered drawing
- **Common/RootSite/** - View coordinator (SimpleRootSite, CollectorEnv)
- **Kernel/** - Low-level utilities used by Views
- **Generic/** - COM smart pointers and collections
- **AppCore/** - GDI wrappers, ColorTable, FwStyledText

## References
- **Project**: views.vcxproj (native C++ DLL)
- **90 source/header files** (~66.7K lines): VwRootBox.cpp, VwEnv.cpp, VwSelection.cpp, VwParagraphBox in VwTextBoxes.cpp, VwStringBox in VwTextBoxes.cpp, VwTableBox.cpp, VwPropertyStore.cpp, VwLazyBox.cpp, VwNotifier.cpp, VwSynchronizer.cpp, VwAccessRoot.cpp (Windows), etc.
- **Main.h** - Central header with forward declarations
- **Views.def** - DLL export definitions
  - Public class implementation

- **VwInnerPileBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwLazyBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwLeafBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwMoveablePileBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwParagraphBox** (class)
  - Path: `VwTextBoxes.h`
  - Public class implementation

- **VwPictureSelection** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwPileBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwStringBox** (class)
  - Path: `VwTextBoxes.h`
  - Public class implementation

- **__declspec** (class)
  - Path: `VwEnv.h`
  - Public class implementation

## Entry Points
- Provides view classes and rendering engine
- Accessed from managed code via ManagedVwWindow and interop layers

## Test Index
Test projects: TestViews. 29 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **ManagedVwWindow/** - Managed wrappers for native views
- **ManagedVwDrawRootBuffered/** - Buffered rendering for views
- **Kernel/** - Low-level infrastructure used by views
- **AppCore/** - Application-level graphics utilities
- **Common/RootSite/** - Root site components using views
- **Common/SimpleRootSite/** - Simplified view hosting
- **LexText/** - Major consumer of view rendering for lexicon display
- **xWorks/** - Uses views for data visualization

## References

- **Project files**: TestViews.vcxproj, VwGraphicsReplayer.csproj, views.vcxproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, VwGraphicsReplayer.cs
- **Key C++ files**: ExplicitInstantiation.cpp, VwAccessRoot.cpp, VwLayoutStream.cpp, VwLazyBox.cpp, VwNotifier.cpp, VwPattern.cpp, VwSelection.cpp, VwTextBoxes.cpp, VwTextStore.cpp, VwTxtSrc.cpp
- **Key headers**: VwAccessRoot.h, VwEnv.h, VwNotifier.h, VwPattern.h, VwResources.h, VwSimpleBoxes.h, VwSynchronizer.h, VwTableBox.h, VwTextBoxes.h, VwTxtSrc.h
- **XML data/config**: VirtualsCm.xml
- **Source file count**: 130 files
- **Data file count**: 1 files

## References (auto-generated hints)
- Project files:
  - Src/views/Test/TestViews.vcxproj
  - Src/views/lib/VwGraphicsReplayer/VwGraphicsReplayer.csproj
  - Src/views/views.vcxproj
- Key C# files:
  - Src/views/lib/VwGraphicsReplayer/AssemblyInfo.cs
  - Src/views/lib/VwGraphicsReplayer/VwGraphicsReplayer.cs
- Key C++ files:
  - Src/views/ExplicitInstantiation.cpp
  - Src/views/Test/Collection.cpp
  - Src/views/Test/testViews.cpp
  - Src/views/ViewsExtra_GUIDs.cpp
  - Src/views/ViewsGlobals.cpp
  - Src/views/Views_GUIDs.cpp
  - Src/views/VwAccessRoot.cpp
  - Src/views/VwEnv.cpp
  - Src/views/VwInvertedViews.cpp
  - Src/views/VwLayoutStream.cpp
  - Src/views/VwLazyBox.cpp
  - Src/views/VwNotifier.cpp
  - Src/views/VwOverlay.cpp
  - Src/views/VwPattern.cpp
  - Src/views/VwPrintContext.cpp
  - Src/views/VwPropertyStore.cpp
  - Src/views/VwRootBox.cpp
  - Src/views/VwSelection.cpp
  - Src/views/VwSimpleBoxes.cpp
  - Src/views/VwSynchronizer.cpp
  - Src/views/VwTableBox.cpp
  - Src/views/VwTextBoxes.cpp
  - Src/views/VwTextStore.cpp
  - Src/views/VwTxtSrc.cpp
  - Src/views/dlldatax.c
- Key headers:
  - Src/views/Main.h
  - Src/views/Test/BasicVc.h
  - Src/views/Test/DummyBaseVc.h
  - Src/views/Test/DummyRootsite.h
  - Src/views/Test/MockLgWritingSystem.h
  - Src/views/Test/MockLgWritingSystemFactory.h
  - Src/views/Test/MockRenderEngineFactory.h
  - Src/views/Test/RenderEngineTestBase.h
  - Src/views/Test/TestGraphiteEngine.h
  - Src/views/Test/TestInsertDiffPara.h
  - Src/views/Test/TestLayoutPage.h
  - Src/views/Test/TestLazyBox.h
  - Src/views/Test/TestLgCollatingEngine.h
  - Src/views/Test/TestLgLineBreaker.h
  - Src/views/Test/TestNotifier.h
  - Src/views/Test/TestTsPropsBldr.h
  - Src/views/Test/TestTsStrBldr.h
  - Src/views/Test/TestTsString.h
  - Src/views/Test/TestTsTextProps.h
  - Src/views/Test/TestUndoStack.h
  - Src/views/Test/TestUniscribeEngine.h
  - Src/views/Test/TestVirtualHandlers.h
  - Src/views/Test/TestVwEnv.h
  - Src/views/Test/TestVwGraphics.h
  - Src/views/Test/TestVwOverlay.h
- Data contracts/transforms:
  - Src/views/Test/VirtualsCm.xml
## Code Evidence
*Analysis based on scanning 129 source files*

- **Classes found**: 20 public classes
- **Namespaces**: VwGraphicsReplayer
