---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# views

## Purpose
C++ native rendering engine providing sophisticated text display capabilities for complex
writing systems. Implements the Views architecture that handles multi-script text layout,
bidirectional text, complex rendering requirements, and performance-critical display operations.
Core component enabling FieldWorks to properly display texts in hundreds of writing systems
with accurate formatting, alignment, and complex script rendering.

## Key Components
### Key Classes
- **ParaBuilder**
- **VwStringBox**
- **VwDropCapStringBox**
- **VwParagraphBox**
- **VwConcParaBox**
- **VwAccessRoot**
- **__declspec**
- **VwEnv**
- **NotifierRec**
- **VwLazyBox**

## Technology Stack
- C++ native code
- GDI/DirectX rendering
- Complex text layout algorithms
- Unicode and writing system support

## Dependencies
- Depends on: Kernel (low-level services), Generic (utilities), AppCore
- Used by: All UI components displaying formatted text (via ManagedVwWindow wrappers)

## Build Information
- Native C++ project (vcxproj)
- Performance-critical rendering code
- Includes test suite
- Build with Visual Studio or MSBuild

## Entry Points
- Provides view classes and rendering engine
- Accessed from managed code via ManagedVwWindow and interop layers

## Related Folders
- **ManagedVwWindow/** - Managed wrappers for native views
- **ManagedVwDrawRootBuffered/** - Buffered rendering for views
- **Kernel/** - Low-level infrastructure used by views
- **AppCore/** - Application-level graphics utilities
- **Common/RootSite/** - Root site components using views
- **Common/SimpleRootSite/** - Simplified view hosting
- **LexText/** - Major consumer of view rendering for lexicon display
- **xWorks/** - Uses views for data visualization

## Code Evidence
*Analysis based on scanning 129 source files*

- **Classes found**: 20 public classes
- **Namespaces**: VwGraphicsReplayer

## Interfaces and Data Models

- **IVwNotifier** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **NotifierRec** (class)
  - Path: `VwEnv.h`
  - Public class implementation

- **ParaBuilder** (class)
  - Path: `VwTextBoxes.h`
  - Public class implementation

- **VwAccessRoot** (class)
  - Path: `VwAccessRoot.h`
  - Public class implementation

- **VwAnchorBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwConcParaBox** (class)
  - Path: `VwTextBoxes.h`
  - Public class implementation

- **VwDivBox** (class)
  - Path: `VwSimpleBoxes.h`
  - Public class implementation

- **VwDropCapStringBox** (class)
  - Path: `VwTextBoxes.h`
  - Public class implementation

- **VwEnv** (class)
  - Path: `VwEnv.h`
  - Public class implementation

- **VwGroupBox** (class)
  - Path: `VwSimpleBoxes.h`
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

## References

- **Project files**: TestViews.vcxproj, VwGraphicsReplayer.csproj, views.vcxproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, VwGraphicsReplayer.cs
- **Key C++ files**: ExplicitInstantiation.cpp, VwAccessRoot.cpp, VwLayoutStream.cpp, VwLazyBox.cpp, VwNotifier.cpp, VwPattern.cpp, VwSelection.cpp, VwTextBoxes.cpp, VwTextStore.cpp, VwTxtSrc.cpp
- **Key headers**: VwAccessRoot.h, VwEnv.h, VwNotifier.h, VwPattern.h, VwResources.h, VwSimpleBoxes.h, VwSynchronizer.h, VwTableBox.h, VwTextBoxes.h, VwTxtSrc.h
- **XML data/config**: VirtualsCm.xml
- **Source file count**: 130 files
- **Data file count**: 1 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\views\Test\TestViews.vcxproj
  - Src\views\lib\VwGraphicsReplayer\VwGraphicsReplayer.csproj
  - Src\views\views.vcxproj
- Key C# files:
  - Src\views\lib\VwGraphicsReplayer\AssemblyInfo.cs
  - Src\views\lib\VwGraphicsReplayer\VwGraphicsReplayer.cs
- Key C++ files:
  - Src\views\ExplicitInstantiation.cpp
  - Src\views\Test\Collection.cpp
  - Src\views\Test\testViews.cpp
  - Src\views\ViewsExtra_GUIDs.cpp
  - Src\views\ViewsGlobals.cpp
  - Src\views\Views_GUIDs.cpp
  - Src\views\VwAccessRoot.cpp
  - Src\views\VwEnv.cpp
  - Src\views\VwInvertedViews.cpp
  - Src\views\VwLayoutStream.cpp
  - Src\views\VwLazyBox.cpp
  - Src\views\VwNotifier.cpp
  - Src\views\VwOverlay.cpp
  - Src\views\VwPattern.cpp
  - Src\views\VwPrintContext.cpp
  - Src\views\VwPropertyStore.cpp
  - Src\views\VwRootBox.cpp
  - Src\views\VwSelection.cpp
  - Src\views\VwSimpleBoxes.cpp
  - Src\views\VwSynchronizer.cpp
  - Src\views\VwTableBox.cpp
  - Src\views\VwTextBoxes.cpp
  - Src\views\VwTextStore.cpp
  - Src\views\VwTxtSrc.cpp
  - Src\views\dlldatax.c
- Key headers:
  - Src\views\Main.h
  - Src\views\Test\BasicVc.h
  - Src\views\Test\DummyBaseVc.h
  - Src\views\Test\DummyRootsite.h
  - Src\views\Test\MockLgWritingSystem.h
  - Src\views\Test\MockLgWritingSystemFactory.h
  - Src\views\Test\MockRenderEngineFactory.h
  - Src\views\Test\RenderEngineTestBase.h
  - Src\views\Test\TestGraphiteEngine.h
  - Src\views\Test\TestInsertDiffPara.h
  - Src\views\Test\TestLayoutPage.h
  - Src\views\Test\TestLazyBox.h
  - Src\views\Test\TestLgCollatingEngine.h
  - Src\views\Test\TestLgLineBreaker.h
  - Src\views\Test\TestNotifier.h
  - Src\views\Test\TestTsPropsBldr.h
  - Src\views\Test\TestTsStrBldr.h
  - Src\views\Test\TestTsString.h
  - Src\views\Test\TestTsTextProps.h
  - Src\views\Test\TestUndoStack.h
  - Src\views\Test\TestUniscribeEngine.h
  - Src\views\Test\TestVirtualHandlers.h
  - Src\views\Test\TestVwEnv.h
  - Src\views\Test\TestVwGraphics.h
  - Src\views\Test\TestVwOverlay.h
- Data contracts/transforms:
  - Src\views\Test\VirtualsCm.xml
