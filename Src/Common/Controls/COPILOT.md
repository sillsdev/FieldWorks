---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Controls

## Purpose
Shared UI controls library providing reusable widgets and XML-based view components used throughout FieldWorks applications.

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
