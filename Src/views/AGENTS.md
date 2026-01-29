---
last-reviewed: 2025-10-31
last-reviewed-tree: ad7aed3bda62551a83bc3f56b57f0314b51383b65607f0ee3a031e4e26c6d8e4
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# views

## Purpose
Native C++ rendering engine (~66.7K lines) implementing sophisticated box-based layout and display for complex writing systems. Provides VwRootBox (root display), VwParagraphBox (paragraph layout), VwStringBox (text runs), VwTableBox (table layout), VwSelection (text selection), VwEnv (display environment), and VwPropertyStore (formatting properties). Core component enabling accurate multi-script text layout, bidirectional text, complex rendering, and accessible UI across all FieldWorks applications.

## Architecture
Sophisticated C++ rendering engine (~66.7K lines) implementing box-based layout system. Three-layer hierarchy: 1) Box system (VwBox, VwGroupBox, VwParagraphBox, VwStringBox, VwTableBox), 2) Root/Environment (VwRootBox coordinates layout/paint, VwEnv constructs display), 3) Selection/Interaction (VwSelection, VwTextSelection for editing). Provides accurate multi-script text layout, bidirectional text, complex rendering for all FieldWorks text display.

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

## Technology Stack
Native C++ with COM interfaces. Uses nmake build system with Visual Studio toolchain.

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
TBD - populate from code. See auto-generated hints below.

## Interfaces and Data Models
See Key Components section above.

## Entry Points
- Provides view classes and rendering engine

## Test Index
Test project: `Src/views/Test/` produces `TestViews.exe` using Unit++ framework.

### Building Tests
Requires VS Developer Command Prompt:
```cmd
cd Src\views\Test
nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=%CD%\..\..\..\  BUILD_ARCH=x64 /f testViews.mak
```

### Running Tests
```cmd
cd Output\Debug
TestViews.exe
```

### Test Files (29 test files)
- `testViews.cpp` - Main test entry point
- `testViews.h` - Test suite header
- `TestVwRootBox.h` - VwRootBox tests
- `TestVwParagraph.h` - Paragraph box tests
- `TestVwSelection.h` - Selection tests
- `TestVwEnv.h` - Display environment tests
- `TestVwGraphics.h` - Graphics tests
- `TestTsString.h` - TsString tests
- `TestTsTextProps.h` - Text properties tests
- And many more...

### Dependencies
- Generic.lib, Views.dll, FwKernel.dll
- unit++.lib (test framework)
- ICU 70 DLLs

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- ManagedVwWindow/ - Managed wrappers for native views

## References
See `.cache/copilot/diff-plan.json` for file details.

## COM Interfaces (IDL files)
- Views.idh, ViewsTlb.idl, ViewsPs.idl - COM interface definitions

## Test Infrastructure
- Test/ subfolder (excluded from main line count)

## Code Evidence
*Analysis based on scanning 129 source files*
