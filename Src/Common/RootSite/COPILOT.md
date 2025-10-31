---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# RootSite COPILOT summary

## Purpose
Root-level site management infrastructure for hosting FieldWorks views with advanced features. Provides CollectorEnv for view collection, FwBaseVc for view construction, and root site coordination. More sophisticated than SimpleRootSite with additional view management capabilities.

## Architecture
C# class library (.NET Framework 4.6.2) with root site implementation classes.

## Key Components
- **CollectorEnv**: View collection environment
- **FwBaseVc**: Base view constructor
- **IApp**: Application interface
- Root site coordination classes

## Technology Stack
- C# .NET Framework 4.6.2
- Views rendering engine integration

## Dependencies
### Upstream
- Common/ViewsInterfaces
- Common/SimpleRootSite
- views (native rendering)

### Downstream
- xWorks, LexText (complex view scenarios)

## Build Information
- **Project**: RootSite.csproj (net462, Library)
- **Test project**: RootSiteTests
- **Build**: Via FW.sln

## References
- **Namespace**: SIL.FieldWorks.Common.RootSites
