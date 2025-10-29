---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
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
