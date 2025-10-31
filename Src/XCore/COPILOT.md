---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# XCore

## Purpose
Cross-cutting application framework and plugin architecture.
Provides command handling, choice management, property propagation, UI composition, and extensibility
infrastructure (xCoreInterfaces, FlexUIAdapter) used by multiple FieldWorks applications. Includes
navigation components (SilSidePane) and comprehensive tests (xCoreTests). Foundation for building
extensible, plugin-based applications with consistent command and UI patterns.

## Architecture
C# library with 91 source files. Contains 4 subprojects: xCore, xCoreInterfaces, FlexUIAdapter....

## Key Components
### Key Classes
- **Inventory**
- **XmlIncluder**
- **ImageContent**
- **NotifyWindow**
- **IconHolder**
- **MockupDialogLauncher**
- **CollapsingSplitContainer**
- **Ticker**
- **XWindow**
- **HtmlViewer**

### Key Interfaces
- **IOldVersionMerger**
- **IPostLayoutInit**
- **IFeedbackInfoProvider**
- **IContextHelper**
- **IUIAdapter**
- **IUIAdapterForceRegenerate**
- **IUIMenuAdapter**
- **ITestableUIAdapter**

## Technology Stack
- C# .NET WinForms
- Plugin/mediator architecture
- Command pattern implementation
- Event-driven UI framework

## Dependencies
- Depends on: Common (UI infrastructure), FwResources (resources)
- Used by: xWorks, LexText (all major applications built on XCore)

## Interop & Contracts
Uses COM, P/Invoke for cross-boundary calls.

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- Multiple C# projects comprising the framework
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Interfaces and Data Models

- **IContextHelper** (interface)
  - Path: `xCoreInterfaces/BaseContextHelper.cs`
  - Public interface definition

- **IFeedbackInfoProvider** (interface)
  - Path: `xCoreInterfaces/IFeedbackInfoProvider.cs`
  - Public interface definition

- **IImageCollection** (interface)
  - Path: `xCoreInterfaces/IImageCollection.cs`
  - Public interface definition

- **IOldVersionMerger** (interface)
  - Path: `Inventory.cs`
  - Public interface definition

- **IPostLayoutInit** (interface)
  - Path: `xWindow.cs`
  - Public interface definition

- **ITestableUIAdapter** (interface)
  - Path: `xCoreInterfaces/IUIAdapter.cs`
  - Public interface definition

- **IUIAdapter** (interface)
  - Path: `xCoreInterfaces/IUIAdapter.cs`
  - Public interface definition

- **IUIAdapterForceRegenerate** (interface)
  - Path: `xCoreInterfaces/IUIAdapter.cs`
  - Public interface definition

- **IUIMenuAdapter** (interface)
  - Path: `xCoreInterfaces/IUIAdapter.cs`
  - Public interface definition

- **IXCoreUserControl** (interface)
  - Path: `xCoreInterfaces/IxCoreColleague.cs`
  - Public interface definition

- **IxCoreColleague** (interface)
  - Path: `xCoreInterfaces/IxCoreColleague.cs`
  - Public interface definition

- **IxCoreContentControl** (interface)
  - Path: `xCoreInterfaces/IxCoreColleague.cs`
  - Public interface definition

- **AdapterMenuItem** (class)
  - Path: `AdapterMenuItem.cs`
  - Public class implementation

- **DlgListenerBase** (class)
  - Path: `AreaManager.cs`
  - Public class implementation

- **HtmlControl** (class)
  - Path: `HtmlControl.cs`
  - Public class implementation

- **HtmlControlEventArgs** (class)
  - Path: `HtmlControl.cs`
  - Public class implementation

- **HtmlViewer** (class)
  - Path: `HtmlViewer.cs`
  - Public class implementation

- **IconHolder** (class)
  - Path: `IconHolder.cs`
  - Public class implementation

- **ImageCollection** (class)
  - Path: `ImageCollection.cs`
  - Public class implementation

- **ImageContent** (class)
  - Path: `ImageContent.cs`
  - Public class implementation

- **Inventory** (class)
  - Path: `Inventory.cs`
  - Public class implementation

- **MockupDialogLauncher** (class)
  - Path: `MockupDialogLauncher.cs`
  - Public class implementation

- **MultiPane** (class)
  - Path: `MultiPane.cs`
  - Public class implementation

- **NotifyWindow** (class)
  - Path: `NotifyWindow.cs`
  - Public class implementation

- **RecordBar** (class)
  - Path: `RecordBar.cs`
  - Public class implementation

- **StringPair** (class)
  - Path: `XMessageBoxExManager.cs`
  - Public class implementation

- **Ticker** (class)
  - Path: `Ticker.cs`
  - Public class implementation

- **XCoreAccessibleObject** (class)
  - Path: `xCoreUserControl.cs`
  - Public class implementation

- **XCoreUserControl** (class)
  - Path: `xCoreUserControl.cs`
  - Public class implementation

- **XMessageBoxExManager** (class)
  - Path: `XMessageBoxExManager.cs`
  - Public class implementation

- **XWindow** (class)
  - Path: `xWindow.cs`
  - Public class implementation

- **XmlIncluder** (class)
  - Path: `IncludeXml.cs`
  - Public class implementation

- **BackgroundStyles** (enum)
  - Path: `NotifyWindow.cs`

- **ClockStates** (enum)
  - Path: `NotifyWindow.cs`

- **SettingsGroup** (enum)
  - Path: `xCoreInterfaces/PropertyTable.cs`

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
- **Target frameworks**: net462
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
## Code Evidence
*Analysis based on scanning 78 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.SilSidePane, XCore, XCoreUnused
