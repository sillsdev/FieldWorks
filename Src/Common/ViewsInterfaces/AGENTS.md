---
last-reviewed: 2025-10-31
last-reviewed-tree: 0757bbbaaff5bc9955aa7b4ae78c8dab29ad614626296c6de00f72aade14ff77
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - referenced-by
  - key-components
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# ViewsInterfaces COPILOT summary

## Purpose
Managed interface definitions for the native Views rendering engine, providing the critical bridge between managed C# code and native C++ Views text rendering system. Declares .NET interfaces corresponding to native COM interfaces (IVwGraphics, IVwSelection, IVwRootBox, IVwEnv, ITsString, ITsTextProps, IVwCacheDa, ISilDataAccess, etc.) enabling managed code to interact with sophisticated text rendering engine. Includes COM wrapper utilities (ComWrapper, ComUtils), managed property store (VwPropertyStoreManaged), display property override factory (DispPropOverrideFactory), COM interface definitions (IPicture, IServiceProvider), and data structures (Rect, ClipFormat enum). Essential infrastructure for all text display and editing in FieldWorks.

## Architecture
C# interface library (.NET Framework 4.8.x) with COM interop interface definitions. Pure interface declarations matching native Views COM interfaces, plus helper classes for COM marshaling and object lifetime management. No implementations - actual implementations reside in native Views DLL accessed via COM interop.

### Referenced By

- [Views Rendering](../../../openspec/specs/architecture/ui-framework/views-rendering.md#rendering-pipeline) — Managed Views interfaces

## Key Components
- **ComWrapper** (ComWrapper.cs): COM object lifetime management
  - Wraps COM interface pointers for proper reference counting
  - Ensures IUnknown::Release() called on disposal
  - Base class for COM wrapper objects
- **ComUtils** (ComUtils.cs): COM utility functions
  - Helper methods for COM interop
  - Marshal, conversion utilities
- **VwPropertyStoreManaged** (VwPropertyStoreManaged.cs): Managed property store
  - C# implementation of property store for Views
  - Holds display properties (text props, writing system, etc.)
- **DispPropOverrideFactory** (DispPropOverrideFactory.cs): Display property override factory
  - Creates property overrides for text formatting
  - Manages ITsTextProps overrides for Views
- **IPicture** (IPicture.cs): COM IPicture interface
  - Standard COM interface for images
  - Used for picture display in Views
- **Rect** (Rect.cs): Rectangle data structure
  - Geometric rectangle for Views rendering
  - Left, Top, Right, Bottom coordinates
- **ClipFormat** enum (ComWrapper.cs): Clipboard format enumeration
  - Standard clipboard formats (Text, Bitmap, UnicodeText, etc.)
  - Used for clipboard operations
- **COM Interface declarations**: Numerous interfaces for Views engine
  - IVwGraphics, IVwSelection, IVwRootBox, IVwEnv (declared in Views headers, referenced here)
  - ITsString, ITsTextProps (text string interfaces)
  - IVwCacheDa, ISilDataAccess (data access interfaces)
  - Note: Full interface declarations in C++ headers; C# side uses COM interop attributes

## Technology Stack
C# .NET Framework 4.8.x, COM interop (Runtime.InteropServices) for native Views C++ engine.

## Dependencies
Consumes: views (native C++ COM server), System.Runtime.InteropServices. Used by: Common/SimpleRootSite, Common/RootSite, all text display components.

## Interop & Contracts
COM interop for native Views. IUnknown lifetime, ComWrapper reference counting, marshaling attributes. Critical bridge for managed-to-native Views.

### Referenced By

- [Native Boundary](../../../openspec/specs/architecture/interop/native-boundary.md#marshaling-patterns) — COM interop boundary

## Threading & Performance
COM threading model, STA threads required. ComWrapper ensures proper COM object lifetime.

## Config & Feature Flags
Interface definitions only; no configuration.

## Build Information
ViewsInterfaces.csproj (net48), output: ViewsInterfaces.dll. Tests: `dotnet test ViewsInterfacesTests/`.

## Interfaces and Data Models

- **ComWrapper** (ComWrapper.cs)
  - Purpose: Base class for COM object wrappers ensuring proper lifetime management
  - Inputs: COM interface pointer
  - Outputs: Managed wrapper with IDisposable for cleanup
  - Notes: Critical for preventing COM memory leaks; call Dispose() to release COM object

- **VwPropertyStoreManaged** (VwPropertyStoreManaged.cs)
  - Purpose: Managed implementation of Views property store
  - Inputs: Display properties (text props, writing system)
  - Outputs: Property storage for Views rendering
  - Notes: C# side property store complementing native property stores

- **DispPropOverrideFactory** (DispPropOverrideFactory.cs)
  - Purpose: Creates display property overrides for text formatting
  - Inputs: Base properties, override specifications
  - Outputs: ITsTextProps overrides
  - Notes: Enables formatted text rendering with property variations

- **IPicture** (IPicture.cs)
  - Purpose: Standard COM IPicture interface for image handling
  - Inputs: Image data
  - Outputs: Picture object for rendering
  - Notes: Used for embedded pictures in Views

- **Rect** (Rect.cs)
  - Purpose: Rectangle data structure for Views geometry
  - Inputs: Left, Top, Right, Bottom coordinates
  - Outputs: Rectangle bounds
  - Notes: Standard rectangle used throughout Views API

- **ClipFormat** enum (ComWrapper.cs)
  - Purpose: Clipboard format enumeration
  - Values: Text, Bitmap, UnicodeText, MetaFilePict, etc.
  - Notes: Standard Windows clipboard formats for data transfer

- **Views COM Interfaces** (referenced from native Views)
  - IVwGraphics: Graphics context for rendering
  - IVwSelection: Text selection representation
  - IVwRootBox: Root display box
  - IVwEnv: Environment for view construction
  - ITsString: Formatted text string
  - ITsTextProps: Text properties
  - IVwCacheDa: Data access for Views
  - ISilDataAccess: SIL data access interface
  - Many others defined in native Views headers

## Entry Points
Interface library - no executable entry point. Referenced by all Views-using components.

## Test Index
ViewsInterfacesTests project. Run: `dotnet test ViewsInterfacesTests/`.

## Usage Hints
Use ComWrapper for COM lifetime - always Dispose(). STA thread required. VwPropertyStoreManaged for managed property storage. Critical infrastructure affecting all text rendering.

## Related Folders
views (native C++ COM server), Common/SimpleRootSite, Common/RootSite, all text display components.

## References
ViewsInterfaces.csproj (net48), 863 lines. Key files: ComWrapper.cs, ComUtils.cs, VwPropertyStoreManaged.cs. See `.cache/copilot/diff-plan.json` for file inventory.
