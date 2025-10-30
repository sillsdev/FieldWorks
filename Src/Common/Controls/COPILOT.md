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

## Build Information
- Build using the top-level FW.sln (Visual Studio/MSBuild) or run: `bash ./agent-build-fw.sh`
- Avoid building this project in isolation; solution builds ensure repo props/targets and interop settings are applied.
- Contains multiple control libraries under this folder; build as part of the full solution.
-

## Entry Points
- Provides reusable controls for application UIs
- XML view system for declarative UI definition

## Related Folders
- **Common/Framework/** - Application framework using these controls
- **Common/ViewsInterfaces/** - Interfaces implemented by controls
- **xWorks/** - Major consumer of Common controls
- **FwCoreDlgs/** - Dialog system using Common controls

## Code Evidence
*Analysis based on scanning 257 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: ControlExtenders, SIL.FieldWorks.Common.Controls, SIL.FieldWorks.Common.Controls.Design, SIL.FieldWorks.Common.Controls.FileDialog, SIL.FieldWorks.Common.Controls.FileDialog.Windows

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

## References

- **Project files**: Design.csproj, DetailControls.csproj, DetailControlsTests.csproj, FwControls.csproj, FwControlsTests.csproj, Widgets.csproj, WidgetsTests.csproj, XMLViews.csproj, XMLViewsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, GhostParentHelper.cs, LayoutFinder.cs, NonEmptyTargetControl.cs, ObjectListPublisher.cs, PartGenerator.cs, SearchCompletedEventArgs.cs, SimpleIntegerMatchDlg.cs, XmlBrowseRDEView.cs, XmlViewsUtils.cs
- **XSLT transforms**: LayoutGenerate.xslt, PartGenerate.xslt
- **XML data/config**: My_Stem-based_LexEntry_Layouts.xml, SampleCm.xml, SampleData.xml, TestColumns.xml, TestParts.xml
- **Source file count**: 276 files
- **Data file count**: 95 files
