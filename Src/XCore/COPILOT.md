---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# XCore

## Purpose
Cross-cutting framework base used by multiple FieldWorks applications. Provides the application framework, plugin architecture, command handling, and UI composition infrastructure that all major FieldWorks applications are built upon.

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

## Build Information
- Multiple C# projects comprising the framework
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Provides framework base classes for applications
- Main application shell infrastructure

## Related Folders
- **xWorks/** - Primary application built on XCore framework
- **LexText/** - Lexicon application using XCore architecture
- **Common/** - Provides lower-level UI components used by XCore
- **FwCoreDlgs/** - Dialogs integrated into XCore applications
- **FwResources/** - Resources used by XCore framework

## Code Evidence
*Analysis based on scanning 78 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.SilSidePane, XCore, XCoreUnused

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

## References

- **Project files**: FlexUIAdapter.csproj, SilSidePane.csproj, SilSidePaneTests.csproj, xCore.csproj, xCoreInterfaces.csproj, xCoreInterfacesTests.csproj, xCoreTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, CollapsingSplitContainer.cs, IconHolder.cs, ImageContent.cs, IncludeXml.cs, Inventory.cs, MockupDialogLauncher.cs, NotifyWindow.cs, PaneBarContainer.Designer.cs, Ticker.cs
- **XML data/config**: CreateOverrideTestData.xml, IncludeXmlTestSource.xml, IncludeXmlTestSourceB.xml, basicTest.xml, includeTest.xml
- **Source file count**: 91 files
- **Data file count**: 35 files
