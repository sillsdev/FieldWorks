---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ViewsInterfaces

## Purpose
Managed interface definitions for the native Views rendering engine.
Declares .NET interfaces corresponding to native COM interfaces in the Views system, enabling
managed code to interact with the powerful native text rendering capabilities. Provides type-safe,
managed access to complex text layout, multilingual display, and sophisticated formatting features.

## Architecture
C# library with 10 source files. Contains 1 subprojects: ViewsInterfaces.

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

## Interop & Contracts
Uses Marshaling, COM, P/Invoke for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# interface library with COM interop
- Build via: `dotnet build ViewsInterfaces.csproj`
- Includes test suite

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

## Entry Points
- Interface definitions for view layer
- COM wrappers for native views
- Property and rendering abstractions

## Test Index
Test projects: ViewsInterfacesTests. 2 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **views/** - Native view layer implementing these interfaces
- **Common/RootSite/** - Uses ViewsInterfaces extensively
- **Common/SimpleRootSite/** - Built on ViewsInterfaces
- **ManagedVwWindow/** - Window management using these interfaces

## References

- **Project files**: ViewsInterfaces.csproj, ViewsInterfacesTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, AssemblyInfo.cs, ComUtils.cs, ComWrapper.cs, DispPropOverrideFactory.cs, ExtraComInterfacesTests.cs, IPicture.cs, Rect.cs, VwGraphicsTests.cs, VwPropertyStoreManaged.cs
- **Source file count**: 10 files
- **Data file count**: 0 files

## References (auto-generated hints)
- Project files:
  - Common/ViewsInterfaces/BuildInclude.targets
  - Common/ViewsInterfaces/ViewsInterfaces.csproj
  - Common/ViewsInterfaces/ViewsInterfacesTests/ViewsInterfacesTests.csproj
- Key C# files:
  - Common/ViewsInterfaces/AssemblyInfo.cs
  - Common/ViewsInterfaces/ComUtils.cs
  - Common/ViewsInterfaces/ComWrapper.cs
  - Common/ViewsInterfaces/DispPropOverrideFactory.cs
  - Common/ViewsInterfaces/IPicture.cs
  - Common/ViewsInterfaces/Rect.cs
  - Common/ViewsInterfaces/Views.cs
  - Common/ViewsInterfaces/ViewsInterfacesTests/ExtraComInterfacesTests.cs
  - Common/ViewsInterfaces/ViewsInterfacesTests/Properties/AssemblyInfo.cs
  - Common/ViewsInterfaces/ViewsInterfacesTests/VwGraphicsTests.cs
  - Common/ViewsInterfaces/VwPropertyStoreManaged.cs
## Code Evidence
*Analysis based on scanning 9 source files*

- **Classes found**: 9 public classes
- **Interfaces found**: 3 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.ViewsInterfaces
