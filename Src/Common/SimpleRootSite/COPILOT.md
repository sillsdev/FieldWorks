---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# SimpleRootSite

## Purpose
Simplified root site implementation with streamlined API. 
Provides a more accessible interface to the Views system for common scenarios, handling 
standard view hosting patterns with pre-configured functionality. Reduces boilerplate code 
needed to embed Views in applications while maintaining full rendering capabilities.

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

## Interfaces and Data Models

- **IChangeRootObject** (interface)
  - Path: `IChangeRootObject.cs`
  - Public interface definition

- **IControl** (interface)
  - Path: `IControl.cs`
  - Public interface definition

- **IEditingCallbacks** (interface)
  - Path: `EditingHelper.cs`
  - Public interface definition

- **IPrintRootSite** (interface)
  - Path: `PrintRootSite.cs`
  - Public interface definition

- **IRawElementProviderFragment** (interface)
  - Path: `WpfInterfacesForMono.cs`
  - Public interface definition

- **IRawElementProviderFragmentRoot** (interface)
  - Path: `WpfInterfacesForMono.cs`
  - Public interface definition

- **IRawElementProviderSimple** (interface)
  - Path: `WpfInterfacesForMono.cs`
  - Public interface definition

- **IRefreshableRoot** (interface)
  - Path: `IRootSite.cs`
  - Public interface definition

- **IRootSite** (interface)
  - Path: `IRootSite.cs`
  - Public interface definition

- **ISelectionChangeNotifier** (interface)
  - Path: `ISelectionChangeNotifier.cs`
  - Public interface definition

- **ISuppressDefaultKeyboardOnKillFocus** (interface)
  - Path: `SimpleRootSite.cs`
  - Public interface definition

- **ITextProvider** (interface)
  - Path: `WpfInterfacesForMono.cs`
  - Public interface definition

- **IValueProvider** (interface)
  - Path: `WpfInterfacesForMono.cs`
  - Public interface definition

- **NavigateDirection** (interface)
  - Path: `WpfInterfacesForMono.cs`
  - Public interface definition

- **ActiveViewHelper** (class)
  - Path: `ActiveViewHelper.cs`
  - Public class implementation

- **DataUpdateMonitor** (class)
  - Path: `DataUpdateMonitor.cs`
  - Public class implementation

- **FwRightMouseClickEventArgs** (class)
  - Path: `FwRightMouseClickEventArgs.cs`
  - Public class implementation

- **IbusRootSiteEventHandler** (class)
  - Path: `IbusRootSiteEventHandler.cs`
  - Public class implementation

- **LocalLinkArgs** (class)
  - Path: `LocalLinkArgs.cs`
  - Public class implementation

- **OrientationManager** (class)
  - Path: `OrientationManager.cs`
  - Public class implementation

- **PrintRootSite** (class)
  - Path: `PrintRootSite.cs`
  - Public class implementation

- **RenderEngineFactory** (class)
  - Path: `RenderEngineFactory.cs`
  - Public class implementation

- **SelInfo** (class)
  - Path: `SelectionHelper.cs`
  - Public class implementation

- **SelPositionInfo** (class)
  - Path: `SelPositionInfo.cs`
  - Public class implementation

- **SelectionHelper** (class)
  - Path: `SelectionHelper.cs`
  - Public class implementation

- **SelectionRestorer** (class)
  - Path: `SelectionRestorer.cs`
  - Public class implementation

- **SimpleRootSite** (class)
  - Path: `SimpleRootSite.cs`
  - Public class implementation

- **SuspendDrawing** (class)
  - Path: `SimpleRootSite.cs`
  - Public class implementation

- **TextSelInfo** (class)
  - Path: `TextSelInfo.cs`
  - Public class implementation

- **UpdateSemaphore** (class)
  - Path: `DataUpdateMonitor.cs`
  - Public class implementation

- **VerticalOrientationManager** (class)
  - Path: `OrientationManager.cs`
  - Public class implementation

- **ViewInputManager** (class)
  - Path: `ViewInputManager.cs`
  - Public class implementation

- **VwBaseVc** (class)
  - Path: `VwBaseVc.cs`
  - Public class implementation

- **VwSelectionArgs** (class)
  - Path: `VwSelectionArgs.cs`
  - Public class implementation

- **CkBehavior** (enum)
  - Path: `EditingHelper.cs`

- **ObjType** (enum)
  - Path: `SimpleRootSiteTests/UndoableRealDataCache.cs`

- **PasteStatus** (enum)
  - Path: `EditingHelper.cs`

- **ProviderOptions** (enum)
  - Path: `WpfInterfacesForMono.cs`

- **SelLimitType** (enum)
  - Path: `SelectionHelper.cs`

## References

- **Project files**: SimpleRootSite.csproj, SimpleRootSiteTests.csproj
- **Target frameworks**: net462
- **Key C# files**: ActiveViewHelper.cs, AssemblyInfo.cs, IChangeRootObject.cs, ISelectionChangeNotifier.cs, PrintRootSite.cs, RenderEngineFactory.cs, SelPositionInfo.cs, SelectionRestorer.cs, VwSelectionArgs.cs, WpfInterfacesForMono.cs
- **XML data/config**: SimpleRootSiteDataProviderCacheModel.xml, TextCacheModel.xml
- **Source file count**: 43 files
- **Data file count**: 6 files
