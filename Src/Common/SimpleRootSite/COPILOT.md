---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# SimpleRootSite

## Purpose
Simplified root site implementation for view hosting. Provides a streamlined API for hosting FieldWorks views with common functionality pre-configured.

## Key Components
### Key Classes
- **VwSelectionArgs**
- **SelPositionInfo**
- **PrintRootSite**
- **SelectionRestorer**
- **ActiveViewHelper**
- **RenderEngineFactory**
- **UpdateSemaphore**
- **DataUpdateMonitor**
- **FwRightMouseClickEventArgs**
- **SimpleRootSite**

### Key Interfaces
- **IPrintRootSite**
- **IChangeRootObject**
- **ISelectionChangeNotifier**
- **IRawElementProviderFragment**
- **IRawElementProviderFragmentRoot**
- **ITextProvider**
- **IValueProvider**
- **NavigateDirection**

## Technology Stack
- C# .NET
- View hosting infrastructure
- Event-driven architecture

## Dependencies
- Depends on: Common/RootSite, views (native views), Common/ViewsInterfaces
- Used by: Most FieldWorks view-based components

## Build Information
- C# class library project
- Build via: `dotnet build SimpleRootSite.csproj`
- Higher-level abstraction over RootSite

## Entry Points
- SimpleRootSite class for easy view hosting
- Helper classes for common view operations
- Selection and editing support

## Related Folders
- **Common/RootSite/** - Base infrastructure used by SimpleRootSite
- **Common/ViewsInterfaces/** - Interfaces implemented
- **ManagedVwWindow/** - Window components using SimpleRootSite
- **xWorks/** - Uses SimpleRootSite for data views
- **LexText/** - Uses SimpleRootSite for text editing

## Code Evidence
*Analysis based on scanning 40 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.RootSites, SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
