---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ManagedVwDrawRootBuffered

## Purpose
Managed view rendering primitives for buffered drawing. Provides double-buffered rendering infrastructure for FieldWorks views to eliminate flicker and improve visual performance.

## Key Components
- **ManagedVwDrawRootBuffered.csproj** - Buffered rendering library


## Key Classes/Interfaces
- **VwDrawRootBuffered**

## Technology Stack
- C# .NET with GDI+ or DirectX
- Double-buffered rendering
- View drawing optimization

## Dependencies
- Depends on: views (native view layer), Common (UI infrastructure)
- Used by: All view-based UI components requiring smooth rendering

## Build Information
- C# class library
- Build with MSBuild or Visual Studio
- Performance-critical rendering code

## Entry Points
- Provides buffered drawing surfaces for views
- Used by view infrastructure for rendering

## Related Folders
- **views/** - Native view layer that works with buffered rendering
- **ManagedVwWindow/** - Window management that uses buffered drawing
- **Common/RootSite/** - Root site components using buffered rendering
- **Common/SimpleRootSite/** - Simplified sites using this infrastructure
