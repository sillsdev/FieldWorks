---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Common

## Purpose
Cross-cutting utilities and shared infrastructure used throughout FieldWorks.
Contains fundamental UI controls (Controls), framework components (Framework), shared managed
code (FwUtils), native code bridges (SimpleRootSite, RootSite), view interfaces (ViewsInterfaces),
filtering (Filters), scripture utilities (ScriptureUtils), and application services (FieldWorks).
Most comprehensive collection of shared code, providing building blocks for all FieldWorks applications.

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
- Mix of C# and C++/CLI
- UI framework components
- Cross-platform utility patterns

## Dependencies
- Depends on: Kernel, Generic (for low-level utilities)
- Used by: Almost all FieldWorks applications and libraries

## Build Information
- Multiple C# projects within subfolders
- Mix of library and interface projects
- Build all subprojects as part of solution build

## Entry Points
- Provides shared infrastructure, not directly executable
- Key interfaces and base classes used throughout FieldWorks

## Related Folders
- **XCore/** - Framework components that work with Common utilities
- **xWorks/** - Major consumer of Common UI controls and utilities
- **LexText/** - Uses Common controls for lexicon UI
- **FwCoreDlgs/** - Dialog components built on Common infrastructure
- **views/** - Native view layer that Common components interface with

## Code Evidence
*Analysis based on scanning 537 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: ControlExtenders, SIL.FieldWorks, SIL.FieldWorks.Common.Controls, SIL.FieldWorks.Common.Controls.Design, SIL.FieldWorks.Common.Controls.FileDialog
- **Project references**: ..\..\LexText\LexTextControls\LexTextControls

## Interfaces and Data Models

- **IChangeRootObject** (interface)
  - Path: `SimpleRootSite/IChangeRootObject.cs`
  - Public interface definition

- **IControl** (interface)
  - Path: `SimpleRootSite/IControl.cs`
  - Public interface definition

- **IEditingCallbacks** (interface)
  - Path: `SimpleRootSite/EditingHelper.cs`
  - Public interface definition

- **IPrintRootSite** (interface)
  - Path: `SimpleRootSite/PrintRootSite.cs`
  - Public interface definition

- **IRawElementProviderFragment** (interface)
  - Path: `SimpleRootSite/WpfInterfacesForMono.cs`
  - Public interface definition

- **IRawElementProviderFragmentRoot** (interface)
  - Path: `SimpleRootSite/WpfInterfacesForMono.cs`
  - Public interface definition

- **IRawElementProviderSimple** (interface)
  - Path: `SimpleRootSite/WpfInterfacesForMono.cs`
  - Public interface definition

- **IRefreshableRoot** (interface)
  - Path: `SimpleRootSite/IRootSite.cs`
  - Public interface definition

- **IRootSite** (interface)
  - Path: `SimpleRootSite/IRootSite.cs`
  - Public interface definition

- **ISelectionChangeNotifier** (interface)
  - Path: `SimpleRootSite/ISelectionChangeNotifier.cs`
  - Public interface definition

- **ISuppressDefaultKeyboardOnKillFocus** (interface)
  - Path: `SimpleRootSite/SimpleRootSite.cs`
  - Public interface definition

- **ITextProvider** (interface)
  - Path: `SimpleRootSite/WpfInterfacesForMono.cs`
  - Public interface definition

- **IValueProvider** (interface)
  - Path: `SimpleRootSite/WpfInterfacesForMono.cs`
  - Public interface definition

- **NavigateDirection** (interface)
  - Path: `SimpleRootSite/WpfInterfacesForMono.cs`
  - Public interface definition

- **ActiveViewHelper** (class)
  - Path: `SimpleRootSite/ActiveViewHelper.cs`
  - Public class implementation

- **DataUpdateMonitor** (class)
  - Path: `SimpleRootSite/DataUpdateMonitor.cs`
  - Public class implementation

- **FwRightMouseClickEventArgs** (class)
  - Path: `SimpleRootSite/FwRightMouseClickEventArgs.cs`
  - Public class implementation

- **IbusRootSiteEventHandler** (class)
  - Path: `SimpleRootSite/IbusRootSiteEventHandler.cs`
  - Public class implementation

- **LocalLinkArgs** (class)
  - Path: `SimpleRootSite/LocalLinkArgs.cs`
  - Public class implementation

- **OrientationManager** (class)
  - Path: `SimpleRootSite/OrientationManager.cs`
  - Public class implementation

- **PrintRootSite** (class)
  - Path: `SimpleRootSite/PrintRootSite.cs`
  - Public class implementation

- **RenderEngineFactory** (class)
  - Path: `SimpleRootSite/RenderEngineFactory.cs`
  - Public class implementation

- **SelInfo** (class)
  - Path: `SimpleRootSite/SelectionHelper.cs`
  - Public class implementation

- **SelPositionInfo** (class)
  - Path: `SimpleRootSite/SelPositionInfo.cs`
  - Public class implementation

- **SelectionHelper** (class)
  - Path: `SimpleRootSite/SelectionHelper.cs`
  - Public class implementation

- **SelectionRestorer** (class)
  - Path: `SimpleRootSite/SelectionRestorer.cs`
  - Public class implementation

- **SimpleRootSite** (class)
  - Path: `SimpleRootSite/SimpleRootSite.cs`
  - Public class implementation

- **SuspendDrawing** (class)
  - Path: `SimpleRootSite/SimpleRootSite.cs`
  - Public class implementation

- **TextSelInfo** (class)
  - Path: `SimpleRootSite/TextSelInfo.cs`
  - Public class implementation

- **UpdateSemaphore** (class)
  - Path: `SimpleRootSite/DataUpdateMonitor.cs`
  - Public class implementation

- **VerticalOrientationManager** (class)
  - Path: `SimpleRootSite/OrientationManager.cs`
  - Public class implementation

- **ViewInputManager** (class)
  - Path: `SimpleRootSite/ViewInputManager.cs`
  - Public class implementation

- **VwBaseVc** (class)
  - Path: `SimpleRootSite/VwBaseVc.cs`
  - Public class implementation

- **VwSelectionArgs** (class)
  - Path: `SimpleRootSite/VwSelectionArgs.cs`
  - Public class implementation

- **CkBehavior** (enum)
  - Path: `SimpleRootSite/EditingHelper.cs`

- **PasteStatus** (enum)
  - Path: `SimpleRootSite/EditingHelper.cs`

- **ProviderOptions** (enum)
  - Path: `SimpleRootSite/WpfInterfacesForMono.cs`

- **SelLimitType** (enum)
  - Path: `SimpleRootSite/SelectionHelper.cs`

## References

- **Project files**: Design.csproj, DetailControls.csproj, DetailControlsTests.csproj, FieldWorks.csproj, FieldWorksTests.csproj, Filters.csproj, FiltersTests.csproj, Framework.csproj, FrameworkTests.csproj, FwControls.csproj, FwControlsTests.csproj, FwUtils.csproj, FwUtilsTests.csproj, RootSite.csproj, RootSiteTests.csproj, ScriptureUtils.csproj, ScriptureUtilsTests.csproj, SimpleRootSite.csproj, SimpleRootSiteTests.csproj, UIAdapterInterfaces.csproj, ViewsInterfaces.csproj, ViewsInterfacesTests.csproj, Widgets.csproj, WidgetsTests.csproj, XMLViews.csproj, XMLViewsTests.csproj
- **Target frameworks**: net462
- **Key dependencies**: ..\..\LexText\LexTextControls\LexTextControls
- **Key C# files**: ActiveViewHelper.cs, AssemblyInfo.cs, IChangeRootObject.cs, ISelectionChangeNotifier.cs, PrintRootSite.cs, RenderEngineFactory.cs, SelPositionInfo.cs, SelectionRestorer.cs, VwSelectionArgs.cs, WpfInterfacesForMono.cs
- **XSLT transforms**: LayoutGenerate.xslt, PartGenerate.xslt
- **XML data/config**: SampleCm.xml, SampleData.xml, SimpleRootSiteDataProviderCacheModel.xml, TestParts.xml, TextCacheModel.xml
- **Source file count**: 576 files
- **Data file count**: 124 files
