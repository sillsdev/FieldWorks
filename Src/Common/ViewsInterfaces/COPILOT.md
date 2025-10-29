---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ViewsInterfaces

## Purpose
Managed interface definitions for the native Views rendering engine.
Declares .NET interfaces corresponding to native COM interfaces in the Views system, enabling
managed code to interact with the powerful native text rendering capabilities. Provides type-safe,
managed access to complex text layout, multilingual display, and sophisticated formatting features.

## Key Components
### Key Classes
- **VwPropertyStoreManaged**
- **DispPropOverrideFactory**
- **ComPictureWrapper**
- **VwConstructorServices**
- **InnerPileHelper**
- **ParagraphBoxHelper**
- **MockIStream**
- **ReleaseComObjectTests**
- **VwGraphicsTests**

### Key Interfaces
- **IPicture**
- **IPictureDisp**
- **IOleServiceProvider**

## Technology Stack
- C# .NET with COM interop
- Interface definitions for native views
- Managed wrappers for native code

## Dependencies
- Depends on: views (native view layer)
- Used by: Common/RootSite, Common/SimpleRootSite, view-based components

## Build Information
- C# interface library with COM interop
- Build via: `dotnet build ViewsInterfaces.csproj`
- Includes test suite

## Entry Points
- Interface definitions for view layer
- COM wrappers for native views
- Property and rendering abstractions

## Related Folders
- **views/** - Native view layer implementing these interfaces
- **Common/RootSite/** - Uses ViewsInterfaces extensively
- **Common/SimpleRootSite/** - Built on ViewsInterfaces
- **ManagedVwWindow/** - Window management using these interfaces

## Code Evidence
*Analysis based on scanning 9 source files*

- **Classes found**: 9 public classes
- **Interfaces found**: 3 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.ViewsInterfaces

## Interfaces and Data Models

- **IOleServiceProvider** (interface)
  - Path: `ComWrapper.cs`
  - Public interface definition

- **IPicture** (interface)
  - Path: `IPicture.cs`
  - Public interface definition

- **IPictureDisp** (interface)
  - Path: `IPicture.cs`
  - Public interface definition

- **ComPictureWrapper** (class)
  - Path: `ComWrapper.cs`
  - Public class implementation

- **DispPropOverrideFactory** (class)
  - Path: `DispPropOverrideFactory.cs`
  - Public class implementation

- **InnerPileHelper** (class)
  - Path: `ComUtils.cs`
  - Public class implementation

- **MockIStream** (class)
  - Path: `ViewsInterfacesTests/ExtraComInterfacesTests.cs`
  - Public class implementation

- **ParagraphBoxHelper** (class)
  - Path: `ComUtils.cs`
  - Public class implementation

- **VwConstructorServices** (class)
  - Path: `ComUtils.cs`
  - Public class implementation

- **VwPropertyStoreManaged** (class)
  - Path: `VwPropertyStoreManaged.cs`
  - Public class implementation

- **Rect** (struct)
  - Path: `Rect.cs`

- **SelLevInfo** (struct)
  - Path: `ComUtils.cs`

- **ClipFormat** (enum)
  - Path: `ComWrapper.cs`

## References

- **Project files**: ViewsInterfaces.csproj, ViewsInterfacesTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, AssemblyInfo.cs, ComUtils.cs, ComWrapper.cs, DispPropOverrideFactory.cs, ExtraComInterfacesTests.cs, IPicture.cs, Rect.cs, VwGraphicsTests.cs, VwPropertyStoreManaged.cs
- **Source file count**: 10 files
- **Data file count**: 0 files
