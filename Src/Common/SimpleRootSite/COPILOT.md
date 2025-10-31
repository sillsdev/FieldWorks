---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# SimpleRootSite COPILOT summary

## Purpose
Simplified root site implementation with streamlined API for hosting FieldWorks views. Provides SimpleRootSite base class, ActiveViewHelper for view activation, DataUpdateMonitor for change tracking, and view lifecycle management. Easier-to-use alternative to RootSite for standard view hosting scenarios.

## Architecture
C# class library (.NET Framework 4.6.2) with simplified root site implementation.

## Key Components
- **SimpleRootSite**: Base class for simple view hosting
- **ActiveViewHelper**: View activation management
- **DataUpdateMonitor**: Change notification and updates
- **SelectionHelper**: Selection management
- View lifecycle classes

## Technology Stack
- C# .NET Framework 4.6.2
- Views rendering engine integration

## Dependencies
### Upstream
- Common/ViewsInterfaces
- views (native rendering)

### Downstream
- Many FieldWorks components (standard view hosting)

## Build Information
- **Project**: SimpleRootSite.csproj (net462, Library)
- **Test project**: SimpleRootSiteTests
- **Build**: Via FW.sln

## References
- **Namespace**: SIL.FieldWorks.Common.RootSites
