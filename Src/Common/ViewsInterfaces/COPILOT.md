---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# ViewsInterfaces COPILOT summary

## Purpose
Managed interface definitions for the native Views rendering engine. Declares .NET interfaces corresponding to native COM interfaces in the Views system, enabling managed code to interact with the native text rendering engine. Critical bridge between managed C# code and native C++ views.

## Architecture
C# interface library (.NET Framework 4.6.2) with COM interop interfaces for Views engine.

## Key Components
- COM wrapper classes (ComWrapper, ComUtils)
- View interfaces (IVwGraphics, IVwSelection, IVwRootBox, etc.)
- Text string interfaces (ITsString, ITsTextProps)
- Cache interfaces (IVwCacheDa, ISilDataAccess)
- Interface definitions for native Views

## Technology Stack
- C# .NET Framework 4.6.2
- COM interop
- Native Views engine integration

## Dependencies
### Upstream
- views (native rendering engine)
- COM type libraries

### Downstream
- All components using Views rendering
- Common/SimpleRootSite, Common/RootSite
- All text display components

## Build Information
- **Project**: ViewsInterfaces.csproj (net462, Library)
- **Test project**: ViewsInterfacesTests
- **Build**: Via FW.sln

## References
- **Namespace**: SIL.FieldWorks.Common.ViewsInterfaces
