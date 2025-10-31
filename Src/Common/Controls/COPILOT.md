---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Controls

## Purpose
Shared UI controls library providing reusable widgets and XML-based view components.
Contains sophisticated controls like DetailControls for property editing, FwControls for specialized
FieldWorks UI elements, Widgets for general-purpose controls, and XMLViews for XML-driven view
composition. These components enable consistent UI patterns across all FieldWorks applications and
support complex data-driven interfaces through declarative XML specifications.

## Architecture
C# library with 276 source files. Contains 5 subprojects: XMLViews, FwControls, Widgets....

## Key Components
### Key Classes
- **NonEmptyTargetControl**
- **SimpleIntegerMatchDlg**
- **SearchCompletedEventArgs**
- **XmlViewsUtils**
- **PartGenerator**
- **LayoutFinder**
- **SortMethodFinder**
- **IntCompareFinder**
- **XmlBrowseRDEView**
- **GhostParentHelper**

### Key Interfaces
- **IGetReplacedObjects**
- **IGhostable**
- **IBulkEditSpecControl**
- **ITextChangedNotification**
- **IClearValues**
- **ISortItemProvider**
- **IMultiListSortItemProvider**
- **ISettings**

## Technology Stack
- C# .NET WinForms
- Custom control development
- XML-driven UI configuration

## Dependencies
- Depends on: Common/Framework, Common/ViewsInterfaces
- Used by: xWorks, LexText, FwCoreDlgs (UI-heavy applications)

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Threading model: async/await, UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- Build using the top-level FW.sln (Visual Studio/MSBuild) or run: `bash ./agent-build-fw.sh`
- Avoid building this project in isolation; solution builds ensure repo props/targets and interop settings are applied.
- Contains multiple control libraries under this folder; build as part of the full solution.
-

## Interfaces and Data Models

- **IBulkEditSpecControl** (interface)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public interface definition

- **IClearValues** (interface)
  - Path: `XMLViews/XmlBrowseViewBase.cs`
  - Public interface definition

- **IGetReplacedObjects** (interface)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public interface definition

- **IGhostable** (interface)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public interface definition

- **ITextChangedNotification** (interface)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public interface definition

- **BulkCopyTabPageSettings** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **BulkEditBar** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **BulkEditTabPageSettings** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **BulkReplaceTabPageSettings** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **ClickCopyTabPageSettings** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **DeleteTabPageSettings** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **GhostParentHelper** (class)
  - Path: `XMLViews/GhostParentHelper.cs`
  - Public class implementation

- **IntCompareFinder** (class)
  - Path: `XMLViews/LayoutFinder.cs`
  - Public class implementation

- **LayoutFinder** (class)
  - Path: `XMLViews/LayoutFinder.cs`
  - Public class implementation

- **ListChoiceTabPageSettings** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **NonEmptyTargetControl** (class)
  - Path: `XMLViews/NonEmptyTargetControl.cs`
  - Public class implementation

- **ObjectListPublisher** (class)
  - Path: `XMLViews/ObjectListPublisher.cs`
  - Public class implementation

- **PartGenerator** (class)
  - Path: `XMLViews/PartGenerator.cs`
  - Public class implementation

- **ProcessTabPageSettings** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **SearchCompletedEventArgs** (class)
  - Path: `XMLViews/SearchCompletedEventArgs.cs`
  - Public class implementation

- **SimpleIntegerMatchDlg** (class)
  - Path: `XMLViews/SimpleIntegerMatchDlg.cs`
  - Public class implementation

- **SortMethodFinder** (class)
  - Path: `XMLViews/LayoutFinder.cs`
  - Public class implementation

- **TargetColumnChangedEventArgs** (class)
  - Path: `XMLViews/BulkEditBar.cs`
  - Public class implementation

- **XmlBrowseRDEView** (class)
  - Path: `XMLViews/XmlBrowseRDEView.cs`
  - Public class implementation

- **XmlViewsUtils** (class)
  - Path: `XMLViews/XmlViewsUtils.cs`
  - Public class implementation

- **SearchField** (struct)
  - Path: `XMLViews/SearchEngine.cs`

- **NonEmptyTargetOptions** (enum)
  - Path: `XMLViews/NonEmptyTargetControl.cs`

- **SelectionHighlighting** (enum)
  - Path: `XMLViews/XmlBrowseViewBase.cs`

## Entry Points
- Provides reusable controls for application UIs
- XML view system for declarative UI definition

## Test Index
Test projects: XMLViewsTests, FwControlsTests, WidgetsTests, DetailControlsTests. 36 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Common/Framework/** - Application framework using these controls
- **Common/ViewsInterfaces/** - Interfaces implemented by controls
- **xWorks/** - Major consumer of Common controls
- **FwCoreDlgs/** - Dialog system using Common controls

## References

- **Project files**: Design.csproj, DetailControls.csproj, DetailControlsTests.csproj, FwControls.csproj, FwControlsTests.csproj, Widgets.csproj, WidgetsTests.csproj, XMLViews.csproj, XMLViewsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, GhostParentHelper.cs, LayoutFinder.cs, NonEmptyTargetControl.cs, ObjectListPublisher.cs, PartGenerator.cs, SearchCompletedEventArgs.cs, SimpleIntegerMatchDlg.cs, XmlBrowseRDEView.cs, XmlViewsUtils.cs
- **XSLT transforms**: LayoutGenerate.xslt, PartGenerate.xslt
- **XML data/config**: My_Stem-based_LexEntry_Layouts.xml, SampleCm.xml, SampleData.xml, TestColumns.xml, TestParts.xml
- **Source file count**: 276 files
- **Data file count**: 95 files

## References (auto-generated hints)
- Project files:
  - Common/Controls/Design/Design.csproj
  - Common/Controls/DetailControls/DetailControls.csproj
  - Common/Controls/DetailControls/DetailControlsTests/DetailControlsTests.csproj
  - Common/Controls/FwControls/FwControls.csproj
  - Common/Controls/FwControls/FwControlsTests/FwControlsTests.csproj
  - Common/Controls/Widgets/Widgets.csproj
  - Common/Controls/Widgets/WidgetsTests/WidgetsTests.csproj
  - Common/Controls/XMLViews/XMLViews.csproj
  - Common/Controls/XMLViews/XMLViewsTests/XMLViewsTests.csproj
- Key C# files:
  - Common/Controls/Design/AssemblyInfo.cs
  - Common/Controls/Design/EnhancedCollectionEditor.cs
  - Common/Controls/Design/FwButtonDesigner.cs
  - Common/Controls/Design/FwHelpButtonDesigner.cs
  - Common/Controls/Design/InformationBarButtonDesigner.cs
  - Common/Controls/Design/PersistenceDesigner.cs
  - Common/Controls/Design/ProgressLineDesigner.cs
  - Common/Controls/Design/SideBarButtonDesigner.cs
  - Common/Controls/Design/SideBarDesigner.cs
  - Common/Controls/Design/SideBarTabDesigner.cs
  - Common/Controls/DetailControls/AssemblyInfo.cs
  - Common/Controls/DetailControls/AtomicRefTypeAheadSlice.cs
  - Common/Controls/DetailControls/AtomicReferenceLauncher.cs
  - Common/Controls/DetailControls/AtomicReferencePOSSlice.cs
  - Common/Controls/DetailControls/AtomicReferenceSlice.cs
  - Common/Controls/DetailControls/AtomicReferenceView.cs
  - Common/Controls/DetailControls/AudioVisualSlice.cs
  - Common/Controls/DetailControls/AutoMenuHandler.cs
  - Common/Controls/DetailControls/BasicTypeSlices.cs
  - Common/Controls/DetailControls/ButtonLauncher.cs
  - Common/Controls/DetailControls/ChooserCommand.cs
  - Common/Controls/DetailControls/CommandSlice.cs
  - Common/Controls/DetailControls/ConfigureWritingSystemsDlg.cs
  - Common/Controls/DetailControls/DataTree.cs
  - Common/Controls/DetailControls/DataTreeImages.cs
- Data contracts/transforms:
  - Common/Controls/Design/EnhancedCollectionEditor.resx
  - Common/Controls/DetailControls/AtomicReferenceLauncher.resx
  - Common/Controls/DetailControls/AtomicReferenceView.resx
  - Common/Controls/DetailControls/ButtonLauncher.resx
  - Common/Controls/DetailControls/ConfigureWritingSystemsDlg.resx
  - Common/Controls/DetailControls/DataTree.resx
  - Common/Controls/DetailControls/DataTreeImages.resx
  - Common/Controls/DetailControls/DetailControlsTests/TestParts.xml
  - Common/Controls/DetailControls/GenDateChooserDlg.resx
  - Common/Controls/DetailControls/GenDateLauncher.resx
  - Common/Controls/DetailControls/MorphTypeAtomicLauncher.resx
  - Common/Controls/DetailControls/MorphTypeChooser.resx
  - Common/Controls/DetailControls/MultiLevelConc.resx
  - Common/Controls/DetailControls/PartGenerator/LayoutGenerate.xslt
  - Common/Controls/DetailControls/PartGenerator/PartGenerate.xslt
  - Common/Controls/DetailControls/PhoneEnvReferenceLauncher.resx
  - Common/Controls/DetailControls/PhoneEnvReferenceView.resx
  - Common/Controls/DetailControls/ReferenceLauncher.resx
  - Common/Controls/DetailControls/Resources/DetailControlsStrings.resx
  - Common/Controls/DetailControls/SemanticDomainsChooser.resx
  - Common/Controls/DetailControls/SimpleListChooser.resx
  - Common/Controls/DetailControls/SliceTreeNode.resx
  - Common/Controls/DetailControls/SummaryCommandControl.resx
  - Common/Controls/DetailControls/VectorReferenceLauncher.resx
  - Common/Controls/DetailControls/VectorReferenceView.resx
## Code Evidence
*Analysis based on scanning 257 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: ControlExtenders, SIL.FieldWorks.Common.Controls, SIL.FieldWorks.Common.Controls.Design, SIL.FieldWorks.Common.Controls.FileDialog, SIL.FieldWorks.Common.Controls.FileDialog.Windows
