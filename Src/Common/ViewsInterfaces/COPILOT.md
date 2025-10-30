---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Common/ViewsInterfaces

## Purpose
View layer interfaces for FieldWorks. Defines managed interfaces for interacting with the native view rendering engine, providing the contract between managed and native view code.

## Key Components
- **ViewsInterfaces.csproj** - View interfaces library
- **ComUtils.cs** - COM interop utilities
- **ComWrapper.cs** - COM object wrappers
- **DispPropOverrideFactory.cs** - Display property overrides
- **IPicture.cs** - Picture interface
- **Rect.cs** - Rectangle utilities
- **VwPropertyStoreManaged.cs** - Property store management
- **ViewsInterfacesTests/** - Interface tests


## Key Classes/Interfaces
- **Rect**
- **IPicture**
- **IPictureDisp**
- **ClipFormat**
- **IOleServiceProvider**
- **ComPictureWrapper**
- **SelLevInfo**
- **InnerPileHelper**

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

## Testing
- Run tests: `dotnet test ViewsInterfaces/ViewsInterfacesTests/`
- Tests cover interface contracts and COM interop

## Entry Points
- Interface definitions for view layer
- COM wrappers for native views
- Property and rendering abstractions

## Related Folders
- **views/** - Native view layer implementing these interfaces
- **Common/RootSite/** - Uses ViewsInterfaces extensively
- **Common/SimpleRootSite/** - Built on ViewsInterfaces
- **ManagedVwWindow/** - Window management using these interfaces


## References
- **Project Files**: ViewsInterfaces.csproj
- **Key C# Files**: ComUtils.cs, ComWrapper.cs, DispPropOverrideFactory.cs, IPicture.cs, Rect.cs, VwPropertyStoreManaged.cs
