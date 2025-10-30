---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ViewsInterfaces

## Purpose
View layer interfaces for FieldWorks. Defines managed interfaces for interacting with the native view rendering engine, providing the contract between managed and native view code.

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
