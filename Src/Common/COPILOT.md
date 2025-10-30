---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
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

## Architecture
C# library with 576 source files. Contains 14 subprojects: SimpleRootSite, XMLViews, FwControls....

## Interop & Contracts
Uses Marshaling, COM, P/Invoke for cross-boundary calls.

## Threading & Performance
Threading model: UI thread marshaling, synchronization.

## Config & Feature Flags
Config files: app.config, App.config.

## Test Index
Test projects: SimpleRootSiteTests, XMLViewsTests, FwControlsTests, WidgetsTests, DetailControlsTests, FiltersTests, ViewsInterfacesTests, ScriptureUtilsTests, RootSiteTests, FwUtilsTests, FieldWorksTests, FrameworkTests. 103 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## References (auto-generated hints)
- Project files:
  - Src\Common\Controls\Design\Design.csproj
  - Src\Common\Controls\DetailControls\DetailControls.csproj
  - Src\Common\Controls\DetailControls\DetailControlsTests\DetailControlsTests.csproj
  - Src\Common\Controls\FwControls\FwControls.csproj
  - Src\Common\Controls\FwControls\FwControlsTests\FwControlsTests.csproj
  - Src\Common\Controls\Widgets\Widgets.csproj
  - Src\Common\Controls\Widgets\WidgetsTests\WidgetsTests.csproj
  - Src\Common\Controls\XMLViews\XMLViews.csproj
  - Src\Common\Controls\XMLViews\XMLViewsTests\XMLViewsTests.csproj
  - Src\Common\FieldWorks\BuildInclude.targets
  - Src\Common\FieldWorks\FieldWorks.csproj
  - Src\Common\FieldWorks\FieldWorksTests\FieldWorksTests.csproj
  - Src\Common\Filters\Filters.csproj
  - Src\Common\Filters\FiltersTests\FiltersTests.csproj
  - Src\Common\Framework\Framework.csproj
  - Src\Common\Framework\FrameworkTests\FrameworkTests.csproj
  - Src\Common\FwUtils\BuildInclude.targets
  - Src\Common\FwUtils\FwUtils.csproj
  - Src\Common\FwUtils\FwUtilsTests\FwUtilsTests.csproj
  - Src\Common\RootSite\RootSite.csproj
  - Src\Common\RootSite\RootSiteTests\RootSiteTests.csproj
  - Src\Common\ScriptureUtils\ScriptureUtils.csproj
  - Src\Common\ScriptureUtils\ScriptureUtilsTests\ScriptureUtilsTests.csproj
  - Src\Common\SimpleRootSite\SimpleRootSite.csproj
  - Src\Common\SimpleRootSite\SimpleRootSiteTests\SimpleRootSiteTests.csproj
- Key C# files:
  - Src\Common\Controls\Design\AssemblyInfo.cs
  - Src\Common\Controls\Design\EnhancedCollectionEditor.cs
  - Src\Common\Controls\Design\FwButtonDesigner.cs
  - Src\Common\Controls\Design\FwHelpButtonDesigner.cs
  - Src\Common\Controls\Design\InformationBarButtonDesigner.cs
  - Src\Common\Controls\Design\PersistenceDesigner.cs
  - Src\Common\Controls\Design\ProgressLineDesigner.cs
  - Src\Common\Controls\Design\SideBarButtonDesigner.cs
  - Src\Common\Controls\Design\SideBarDesigner.cs
  - Src\Common\Controls\Design\SideBarTabDesigner.cs
  - Src\Common\Controls\DetailControls\AssemblyInfo.cs
  - Src\Common\Controls\DetailControls\AtomicRefTypeAheadSlice.cs
  - Src\Common\Controls\DetailControls\AtomicReferenceLauncher.cs
  - Src\Common\Controls\DetailControls\AtomicReferencePOSSlice.cs
  - Src\Common\Controls\DetailControls\AtomicReferenceSlice.cs
  - Src\Common\Controls\DetailControls\AtomicReferenceView.cs
  - Src\Common\Controls\DetailControls\AudioVisualSlice.cs
  - Src\Common\Controls\DetailControls\AutoMenuHandler.cs
  - Src\Common\Controls\DetailControls\BasicTypeSlices.cs
  - Src\Common\Controls\DetailControls\ButtonLauncher.cs
  - Src\Common\Controls\DetailControls\ChooserCommand.cs
  - Src\Common\Controls\DetailControls\CommandSlice.cs
  - Src\Common\Controls\DetailControls\ConfigureWritingSystemsDlg.cs
  - Src\Common\Controls\DetailControls\DataTree.cs
  - Src\Common\Controls\DetailControls\DataTreeImages.cs
- Data contracts/transforms:
  - Src\Common\Controls\Design\EnhancedCollectionEditor.resx
  - Src\Common\Controls\DetailControls\AtomicReferenceLauncher.resx
  - Src\Common\Controls\DetailControls\AtomicReferenceView.resx
  - Src\Common\Controls\DetailControls\ButtonLauncher.resx
  - Src\Common\Controls\DetailControls\ConfigureWritingSystemsDlg.resx
  - Src\Common\Controls\DetailControls\DataTree.resx
  - Src\Common\Controls\DetailControls\DataTreeImages.resx
  - Src\Common\Controls\DetailControls\DetailControlsTests\TestParts.xml
  - Src\Common\Controls\DetailControls\GenDateChooserDlg.resx
  - Src\Common\Controls\DetailControls\GenDateLauncher.resx
  - Src\Common\Controls\DetailControls\MorphTypeAtomicLauncher.resx
  - Src\Common\Controls\DetailControls\MorphTypeChooser.resx
  - Src\Common\Controls\DetailControls\MultiLevelConc.resx
  - Src\Common\Controls\DetailControls\PartGenerator\LayoutGenerate.xslt
  - Src\Common\Controls\DetailControls\PartGenerator\PartGenerate.xslt
  - Src\Common\Controls\DetailControls\PhoneEnvReferenceLauncher.resx
  - Src\Common\Controls\DetailControls\PhoneEnvReferenceView.resx
  - Src\Common\Controls\DetailControls\ReferenceLauncher.resx
  - Src\Common\Controls\DetailControls\Resources\DetailControlsStrings.resx
  - Src\Common\Controls\DetailControls\SemanticDomainsChooser.resx
  - Src\Common\Controls\DetailControls\SimpleListChooser.resx
  - Src\Common\Controls\DetailControls\SliceTreeNode.resx
  - Src\Common\Controls\DetailControls\SummaryCommandControl.resx
  - Src\Common\Controls\DetailControls\VectorReferenceLauncher.resx
  - Src\Common\Controls\DetailControls\VectorReferenceView.resx
