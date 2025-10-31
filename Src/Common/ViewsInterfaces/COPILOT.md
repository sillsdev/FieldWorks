---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# ViewsInterfaces COPILOT summary

## Purpose
Managed interface definitions for the native Views rendering engine, providing the critical bridge between managed C# code and native C++ Views text rendering system. Declares .NET interfaces corresponding to native COM interfaces (IVwGraphics, IVwSelection, IVwRootBox, IVwEnv, ITsString, ITsTextProps, IVwCacheDa, ISilDataAccess, etc.) enabling managed code to interact with sophisticated text rendering engine. Includes COM wrapper utilities (ComWrapper, ComUtils), managed property store (VwPropertyStoreManaged), display property override factory (DispPropOverrideFactory), COM interface definitions (IPicture, IServiceProvider), and data structures (Rect, ClipFormat enum). Essential infrastructure for all text display and editing in FieldWorks.

## Architecture
C# interface library (.NET Framework 4.6.2) with COM interop interface definitions. Pure interface declarations matching native Views COM interfaces, plus helper classes for COM marshaling and object lifetime management. No implementations - actual implementations reside in native Views DLL accessed via COM interop.

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
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- COM interop (Runtime.InteropServices)
- Interfaces for native Views C++ engine

## Dependencies

### Upstream (consumes)
- **views**: Native C++ Views rendering engine (COM server)
- **System.Runtime.InteropServices**: COM marshaling
- Native Views type libraries for COM interface definitions

### Downstream (consumed by)
- **Common/SimpleRootSite**: Implements IVwRootSite, uses Views interfaces
- **Common/RootSite**: Advanced root site using Views interfaces
- **All text display components**: Use Views via these interfaces
- Any managed code interfacing with Views rendering

## Interop & Contracts
- **COM interop**: All interfaces designed for COM marshaling to native Views
- **IUnknown**: COM interface lifetime management
- **ComWrapper**: Ensures proper COM reference counting
- **Marshaling attributes**: Control data marshaling between managed and native
- Critical bridge enabling managed FieldWorks to use native Views engine

## Threading & Performance
- **COM threading**: Views interfaces follow COM threading model
- **STA threads**: Views typically requires STA (Single-Threaded Apartment) threads
- **Reference counting**: ComWrapper ensures proper COM object lifetime
- **Performance**: Interface layer; performance determined by native Views implementation

## Config & Feature Flags
No configuration. Interface definitions only.

## Build Information
- **Project file**: ViewsInterfaces.csproj (net462, OutputType=Library)
- **Test project**: ViewsInterfacesTests/ViewsInterfacesTests.csproj
- **Output**: ViewsInterfaces.dll
- **Build**: Via top-level FW.sln or: `msbuild ViewsInterfaces.csproj /p:Configuration=Debug`
- **Run tests**: `dotnet test ViewsInterfacesTests/ViewsInterfacesTests.csproj`

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
Referenced by all FieldWorks components using Views rendering. Interface library - no executable entry point.

## Test Index
- **Test project**: ViewsInterfacesTests
- **Run tests**: `dotnet test ViewsInterfacesTests/ViewsInterfacesTests.csproj`
- **Coverage**: COM wrapper behavior, property stores, utilities

## Usage Hints
- Use ComWrapper for COM object lifetime management - always Dispose()
- Views interfaces accessed via COM interop to native Views.dll
- VwPropertyStoreManaged for managed-side property storage
- Critical infrastructure - changes affect all text rendering
- STA thread required for Views COM calls
- Reference counting via ComWrapper prevents leaks

## Related Folders
- **views/**: Native C++ Views rendering engine (COM server)
- **Common/SimpleRootSite**: Implements IVwRootSite using these interfaces
- **Common/RootSite**: Advanced root site using Views interfaces
- All FieldWorks text display components depend on ViewsInterfaces

## References
- **Project files**: ViewsInterfaces.csproj (net462), ViewsInterfacesTests/ViewsInterfacesTests.csproj
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: ComWrapper.cs, ComUtils.cs, VwPropertyStoreManaged.cs, DispPropOverrideFactory.cs, IPicture.cs, Rect.cs, AssemblyInfo.cs
- **Total lines of code**: 863
- **Output**: Output/Debug/ViewsInterfaces.dll
- **Namespace**: SIL.FieldWorks.Common.ViewsInterfaces
