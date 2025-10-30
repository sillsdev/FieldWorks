---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# views

## Purpose
C++ view-layer components and UI view infrastructure. Provides the native rendering engine for FieldWorks' sophisticated text display, including complex writing systems, interlinear text, and formatted linguistic data.

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
