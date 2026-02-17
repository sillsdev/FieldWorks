---
last-reviewed: 2025-10-31
last-reviewed-tree: 977cb1ce93764b5209ed7283c33b95492c2d9129a7d7e8665fcf91d75e4646ac
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/XCore. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# XCore

## Purpose
Cross-cutting application framework (~9.8K lines in main folder + 4 subfolders) providing plugin architecture, command routing (Mediator), XML-driven UI composition (Inventory, XWindow), and extensibility infrastructure for FieldWorks applications. Implements colleague pattern (IxCoreColleague), UI adapters (IUIAdapter), property propagation (PropertyTable), and choice management. See subfolder COPILOT.md files for xCoreInterfaces/, FlexUIAdapter/, SilSidePane/, xCoreTests/ details.

## Architecture
Plugin-based application framework (~9.8K lines main + 4 subfolders) with XML-driven UI composition. Three-tier design: 1) Core framework (Mediator, PropertyTable, Inventory XML processor), 2) UI components (XWindow, CollapsingSplitContainer, MultiPane, RecordBar), 3) Plugin interfaces (IxCoreColleague, IUIAdapter). Implements colleague pattern for extensible command routing and view coordination across all FieldWorks applications.

## Key Components

### Core Framework (main folder)
- **Inventory** (Inventory.cs) - XML configuration aggregation with base/derived unification
  - `GetElement(string xpath)` - Retrieves unified config elements (layouts, parts)
  - `LoadElements(string path, string xpath)` - Loads XML from files with key attribute merging
  - Handles derivation: elements with `base` attribute unified with base elements
- **XWindow** (xWindow.cs) - Main application window implementing IxCoreColleague, IxWindow
  - Manages: m_mainSplitContainer (CollapsingSplitContainer), m_sidebar, m_recordBar, m_mainContentControl
  - Properties: ShowSidebar, ShowRecordList, persistent splitter distances
  - `Init(Mediator mediator, PropertyTable propertyTable, XmlNode config)` - XML-driven window initialization
- **Mediator** - Central command routing and colleague coordination (referenced throughout)
- **PropertyTable** - Centralized property storage and change notification

### UI Components
- **CollapsingSplitContainer** (CollapsingSplitContainer.cs) - Enhanced SplitContainer with panel collapse
- **RecordBar** (RecordBar.cs) - Navigation bar for record lists
- **MultiPane** (MultiPane.cs) - Tab control equivalent for area switching
- **PaneBarContainer** (PaneBarContainer.cs) - Container for pane bars and content
- **AdapterMenuItem** (AdapterMenuItem.cs) - Menu item with command routing

### Supporting Infrastructure
- **HtmlViewer**, **HtmlControl** (HtmlViewer.cs, HtmlControl.cs) - Embedded HTML display (Gecko/WebBrowser wrappers)
- **ImageCollection**, **ImageContent** (ImageCollection.cs, ImageContent.cs) - Image resource management
- **IconHolder** (IconHolder.cs) - Icon wrapper for UI elements
- **NotifyWindow** (NotifyWindow.cs) - Toast/notification popup
- **Ticker** (Ticker.cs) - Timer-based UI updates
- **AreaManager** (AreaManager.cs) - Area switching and configuration with DlgListenerBase
- **IncludeXml** (IncludeXml.cs) - XML inclusion helper
- **XMessageBoxExManager** (XMessageBoxExManager.cs) - MessageBoxEx adapter
- **xCoreUserControl** (xCoreUserControl.cs) - Base class for XCore-aware user controls implementing IXCoreUserControl

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **UI framework**: System.Windows.Forms (WinForms)
- **Key libraries**: WeifenLuo.WinFormsUI.Docking (SilSidePane), SIL.Utils, Common/FwUtils
- **Configuration**: XML-based Inventory system for UI composition
- **Pattern**: Mediator (command routing), Colleague (plugin integration)

## Dependencies
- **Upstream**: Common/FwUtils (utilities), Common/Framework (FwApp integration), FwResources (images), LCModel.Utils, SIL.Utils, WeifenLuo.WinFormsUI.Docking (SilSidePane)
- **Downstream consumers**: xWorks/, LexText applications (all major FLEx apps built on XCore), Common/Framework (FwApp uses XCore)

## Interop & Contracts
- **IxCoreColleague**: Plugin interface for command handling and property access
- **Mediator**: Central message broker (BroadcastMessage, SendMessage)
- **PropertyTable**: Shared property storage with change notification
- **Inventory**: XML configuration aggregation with base/derived unification
- **IUIAdapter**: UI adapter contracts for menu/toolbar integration
- **XML configuration**: Layouts, commands, choices defined in XML files

## Threading & Performance
- **UI thread**: All XCore operations on main UI thread (WinForms single-threaded model)
- **Idle processing**: IdleQueue for background work during idle time
- **Message sequencing**: MessageSequencer filters/sequences commands for performance
- **Lazy loading**: VwLazyBox supports deferred content creation
- **Property caching**: PropertyTable caches values for fast access

## Config & Feature Flags
- **Inventory XML**: Configuration files define UI structure (layouts, commands, choices)
- **Base/derived**: XML elements support `base` attribute for inheritance/override
- **PropertyTable**: Persistent properties (window size, splitter positions, user preferences)
- **Mediator configuration**: Command routing rules in XML
- **PersistenceProvider**: Settings persistence to registry or config files

## Build Information
- **Project type**: C# class library (net48)
- **Build**: `msbuild XCore.csproj` or via FieldWorks.sln
- **Output**: XCore.dll (main), xCoreInterfaces.dll, FlexUIAdapter.dll, SilSidePane.dll
- **Dependencies**: xCoreInterfaces (interfaces), Common/FwUtils, SIL.Utils, WeifenLuo docking
- **Test projects**: xCoreTests, xCoreInterfacesTests, SilSidePaneTests (11 test files)

## Interfaces and Data Models
- **IxCoreColleague**: Plugin interface (HandleMessage, PropertyValue methods)
- **IxWindow**: Main window contract (ShowSidebar, ShowRecordList properties)
- **IUIAdapter**: UI adapter interface (menu/toolbar binding)
- **PropertyTable**: Key-value property storage with change events
- **Mediator**: Central command broker
- **ChoiceGroup/Choice**: Menu/toolbar definitions from XML
- **Command**: Command pattern with undo/redo support

## Entry Points
- Provides framework base classes for applications
- Main application shell infrastructure

## Test Index
Test projects: xCoreTests, xCoreInterfacesTests, SilSidePaneTests. 11 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **xWorks/** - Primary application built on XCore framework
- **LexText/** - Lexicon application using XCore architecture
- **Common/** - Provides lower-level UI components used by XCore
- **FwCoreDlgs/** - Dialogs integrated into XCore applications
- **FwResources/** - Resources used by XCore framework

## References

- **Project files**: FlexUIAdapter.csproj, SilSidePane.csproj, SilSidePaneTests.csproj, xCore.csproj, xCoreInterfaces.csproj, xCoreInterfacesTests.csproj, xCoreTests.csproj
- **Target frameworks**: net48
- **Key C# files**: AssemblyInfo.cs, CollapsingSplitContainer.cs, IconHolder.cs, ImageContent.cs, IncludeXml.cs, Inventory.cs, MockupDialogLauncher.cs, NotifyWindow.cs, PaneBarContainer.Designer.cs, Ticker.cs
- **XML data/config**: CreateOverrideTestData.xml, IncludeXmlTestSource.xml, IncludeXmlTestSourceB.xml, basicTest.xml, includeTest.xml
- **Source file count**: 91 files
- **Data file count**: 35 files

## References (auto-generated hints)
- Project files:
  - Src/XCore/FlexUIAdapter/FlexUIAdapter.csproj
  - Src/XCore/SilSidePane/SilSidePane.csproj
  - Src/XCore/SilSidePane/SilSidePaneTests/BuildInclude.targets
  - Src/XCore/SilSidePane/SilSidePaneTests/SilSidePaneTests.csproj
  - Src/XCore/xCore.csproj
  - Src/XCore/xCoreInterfaces/xCoreInterfaces.csproj
  - Src/XCore/xCoreInterfaces/xCoreInterfacesTests/xCoreInterfacesTests.csproj
  - Src/XCore/xCoreTests/BuildInclude.targets
  - Src/XCore/xCoreTests/xCoreTests.csproj
- Key C# files:
  - Src/XCore/AdapterMenuItem.cs
  - Src/XCore/AreaManager.cs
  - Src/XCore/AssemblyInfo.cs
  - Src/XCore/CollapsingSplitContainer.Designer.cs
  - Src/XCore/CollapsingSplitContainer.cs
  - Src/XCore/FlexUIAdapter/AdapterBase.cs
  - Src/XCore/FlexUIAdapter/AdapterStrings.Designer.cs
  - Src/XCore/FlexUIAdapter/AssemblyInfo.cs
  - Src/XCore/FlexUIAdapter/BarAdapterBase.cs
  - Src/XCore/FlexUIAdapter/ContextHelper.cs
  - Src/XCore/FlexUIAdapter/MenuAdapter.cs
  - Src/XCore/FlexUIAdapter/NavBarAdapter.cs
  - Src/XCore/FlexUIAdapter/PaneBar.cs
  - Src/XCore/FlexUIAdapter/PanelButton.cs
  - Src/XCore/FlexUIAdapter/PanelMenu.cs
  - Src/XCore/FlexUIAdapter/SidebarAdapter.cs
  - Src/XCore/FlexUIAdapter/ToolbarAdapter.cs
  - Src/XCore/HtmlControl.cs
  - Src/XCore/HtmlViewer.cs
  - Src/XCore/IconHolder.cs
  - Src/XCore/ImageCollection.cs
  - Src/XCore/ImageContent.cs
  - Src/XCore/ImageDialog.cs
  - Src/XCore/IncludeXml.cs
  - Src/XCore/Inventory.cs
- Data contracts/transforms:
  - Src/XCore/AdapterMenuItem.resx
  - Src/XCore/CollapsingSplitContainer.resx
  - Src/XCore/FlexUIAdapter/AdapterStrings.resx
  - Src/XCore/FlexUIAdapter/PaneBar.resx
  - Src/XCore/HtmlControl.resx
  - Src/XCore/HtmlViewer.resx
  - Src/XCore/IconHolder.resx
  - Src/XCore/ImageContent.resx
  - Src/XCore/ImageDialog.resx
  - Src/XCore/MultiPane.resx
  - Src/XCore/NotifyWindow.resx
  - Src/XCore/PaneBarContainer.resx
  - Src/XCore/RecordBar.resx
  - Src/XCore/SilSidePane/NavPaneOptionsDlg.resx
  - Src/XCore/SilSidePane/Properties/Resources.resx
  - Src/XCore/SilSidePane/SilSidePane.resx
  - Src/XCore/Ticker.resx
  - Src/XCore/xCoreInterfaces/xCoreInterfaces.resx
  - Src/XCore/xCoreInterfaces/xCoreInterfacesTests/Properties/Resources.resx
  - Src/XCore/xCoreInterfaces/xCoreInterfacesTests/settingsBackup/Settings.xml
  - Src/XCore/xCoreInterfaces/xCoreInterfacesTests/settingsBackup/db_TestLocal_Settings.xml
  - Src/XCore/xCoreStrings.resx
  - Src/XCore/xCoreTests/CreateOverrideTestData.xml
  - Src/XCore/xCoreTests/IncludeXmlTestSource.xml
  - Src/XCore/xCoreTests/IncludeXmlTestSourceB.xml
## Subfolders (detailed docs in individual COPILOT.md files)
- **xCoreInterfaces/** - Core interfaces: IxCoreColleague, IUIAdapter, IxCoreContentControl, etc.
- **FlexUIAdapter/** - FLEx-specific UI adapter implementations
- **SilSidePane/** - WeifenLuo.WinFormsUI.Docking sidebar integration
- **xCoreTests/** - Comprehensive test suite for XCore framework

## Code Evidence
*Analysis based on scanning 78 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.SilSidePane, XCore, XCoreUnused
