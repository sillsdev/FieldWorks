---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Framework

## Purpose
Application framework components for FieldWorks. Provides core application infrastructure including editing helpers, settings management, and export functionality.

## Key Components
### Key Classes
- **FwEditingHelper**
- **ExportStyleInfo**
- **MainWindowDelegate**
- **FwRegistrySettings**
- **ExternalSettingsAccessorBase**
- **UndoRedoDropDown**
- **SettingsXmlAccessorBase**
- **FwApp**
- **StatusBarProgressHandler**
- **StylesXmlAccessor**

### Key Interfaces
- **IPublicationView**
- **IPageSetupDialog**
- **IMainWindowDelegatedFunctions**
- **IMainWindowDelegateCallbacks**
- **IFieldWorksManager**
- **IFwMainWnd**
- **IRecordListUpdater**
- **IRecordListOwner**

## Technology Stack
- C# .NET
- Application framework patterns
- Settings and configuration management

## Dependencies
- Depends on: Common/FwUtils, Common/ViewsInterfaces
- Used by: All FieldWorks applications (xWorks, LexText)

## Build Information
- C# class library project
- Build via: `dotnet build Framework.csproj`
- Includes comprehensive test suite

## Entry Points
- Base application classes (FwApp)
- Editing helper infrastructure
- Settings and configuration APIs

## Related Folders
- **Common/FwUtils/** - Utilities used by framework
- **Common/FieldWorks/** - FieldWorks-specific infrastructure
- **XCore/** - Application framework that uses Common/Framework
- **xWorks/** - Applications built on this framework

## Code Evidence
*Analysis based on scanning 21 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 9 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.Framework, SIL.FieldWorks.Common.Framework.SelInfo, for, info.

## Interfaces and Data Models

- **IFieldWorksManager** (interface)
  - Path: `IFieldWorksManager.cs`
  - Public interface definition

- **IFwMainWnd** (interface)
  - Path: `IFwMainWnd.cs`
  - Public interface definition

- **IMainWindowDelegateCallbacks** (interface)
  - Path: `MainWindowDelegate.cs`
  - Public interface definition

- **IMainWindowDelegatedFunctions** (interface)
  - Path: `MainWindowDelegate.cs`
  - Public interface definition

- **IPageSetupDialog** (interface)
  - Path: `PublicationInterfaces.cs`
  - Public interface definition

- **IPublicationView** (interface)
  - Path: `PublicationInterfaces.cs`
  - Public interface definition

- **IRecordChangeHandler** (interface)
  - Path: `FwApp.cs`
  - Public interface definition

- **IRecordListOwner** (interface)
  - Path: `FwApp.cs`
  - Public interface definition

- **IRecordListUpdater** (interface)
  - Path: `FwApp.cs`
  - Public interface definition

- **DummyFwApp** (class)
  - Path: `FrameworkTests/MainWindowDelegateTests.cs`
  - Public class implementation

- **DummyMainWindowDelegateCallbacks** (class)
  - Path: `FrameworkTests/MainWindowDelegateTests.cs`
  - Public class implementation

- **ExportStyleInfo** (class)
  - Path: `ExportStyleInfo.cs`
  - Public class implementation

- **ExternalSettingsAccessorBase** (class)
  - Path: `ExternalSettingsAccessorBase.cs`
  - Public class implementation

- **FwApp** (class)
  - Path: `FwApp.cs`
  - Public class implementation

- **FwEditingHelper** (class)
  - Path: `FwEditingHelper.cs`
  - Public class implementation

- **FwRegistrySettings** (class)
  - Path: `FwRegistrySettings.cs`
  - Public class implementation

- **FwRootSite** (class)
  - Path: `FwRootSite.cs`
  - Public class implementation

- **MainWindowDelegate** (class)
  - Path: `MainWindowDelegate.cs`
  - Public class implementation

- **ReservedStyleInfo** (class)
  - Path: `StylesXmlAccessor.cs`
  - Public class implementation

- **SelInfo_Compare** (class)
  - Path: `FrameworkTests/SelInfoTests.cs`
  - Public class implementation

- **SettingsXmlAccessorBase** (class)
  - Path: `SettingsXmlAccessorBase.cs`
  - Public class implementation

- **StatusBarProgressHandler** (class)
  - Path: `StatusBarProgressHandler.cs`
  - Public class implementation

- **StylesXmlAccessor** (class)
  - Path: `StylesXmlAccessor.cs`
  - Public class implementation

- **UndoRedoDropDown** (class)
  - Path: `UndoRedoDropDown.cs`
  - Public class implementation

- **XhtmlHelper** (class)
  - Path: `XhtmlHelper.cs`
  - Public class implementation

- **CssType** (enum)
  - Path: `XhtmlHelper.cs`

- **ResourceStringType** (enum)
  - Path: `FwApp.cs`

- **WindowTiling** (enum)
  - Path: `FwApp.cs`

## References

- **Project files**: Framework.csproj, FrameworkTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ExportStyleInfo.cs, ExternalSettingsAccessorBase.cs, FrameworkStrings.Designer.cs, FwEditingHelper.cs, FwRegistrySettings.cs, IFieldWorksManager.cs, IFwMainWnd.cs, MainWindowDelegate.cs, PublicationInterfaces.cs
- **Source file count**: 22 files
- **Data file count**: 3 files
