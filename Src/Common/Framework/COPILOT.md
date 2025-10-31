---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Framework

## Purpose
Application framework components providing core infrastructure services.
Includes editing helpers (FwEditingHelper), publication interfaces (IPublicationView, IPageSetupDialog),
settings management (FwRegistrySettings), style export (ExportStyleInfo), and main window coordination
(MainWindowDelegate). Establishes architectural patterns and shared functionality for building
FieldWorks applications.

## Architecture
C# library with 22 source files. Contains 1 subprojects: Framework.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Threading model: explicit threading, UI thread marshaling, synchronization.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build Framework.csproj`
- Includes comprehensive test suite

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

## Entry Points
- Base application classes (FwApp)
- Editing helper infrastructure
- Settings and configuration APIs

## Test Index
Test projects: FrameworkTests. 5 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Common/FwUtils/** - Utilities used by framework
- **Common/FieldWorks/** - FieldWorks-specific infrastructure
- **XCore/** - Application framework that uses Common/Framework
- **xWorks/** - Applications built on this framework

## References

- **Project files**: Framework.csproj, FrameworkTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ExportStyleInfo.cs, ExternalSettingsAccessorBase.cs, FrameworkStrings.Designer.cs, FwEditingHelper.cs, FwRegistrySettings.cs, IFieldWorksManager.cs, IFwMainWnd.cs, MainWindowDelegate.cs, PublicationInterfaces.cs
- **Source file count**: 22 files
- **Data file count**: 3 files

## References (auto-generated hints)
- Project files:
  - Common/Framework/Framework.csproj
  - Common/Framework/FrameworkTests/FrameworkTests.csproj
- Key C# files:
  - Common/Framework/AssemblyInfo.cs
  - Common/Framework/ExportStyleInfo.cs
  - Common/Framework/ExternalSettingsAccessorBase.cs
  - Common/Framework/FrameworkStrings.Designer.cs
  - Common/Framework/FrameworkTests/FwEditingHelperTests.cs
  - Common/Framework/FrameworkTests/MainWindowDelegateTests.cs
  - Common/Framework/FrameworkTests/SelInfoTests.cs
  - Common/Framework/FrameworkTests/StylesXmlAccessorTests.cs
  - Common/Framework/FrameworkTests/XhtmlHelperTests.cs
  - Common/Framework/FwApp.cs
  - Common/Framework/FwEditingHelper.cs
  - Common/Framework/FwRegistrySettings.cs
  - Common/Framework/FwRootSite.cs
  - Common/Framework/IFieldWorksManager.cs
  - Common/Framework/IFwMainWnd.cs
  - Common/Framework/MainWindowDelegate.cs
  - Common/Framework/PublicationInterfaces.cs
  - Common/Framework/SettingsXmlAccessorBase.cs
  - Common/Framework/StatusBarProgressHandler.cs
  - Common/Framework/StylesXmlAccessor.cs
  - Common/Framework/UndoRedoDropDown.cs
  - Common/Framework/XhtmlHelper.cs
- Data contracts/transforms:
  - Common/Framework/FrameworkStrings.resx
  - Common/Framework/FwRootSite.resx
  - Common/Framework/UndoRedoDropDown.resx
## Code Evidence
*Analysis based on scanning 21 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 9 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.Framework, SIL.FieldWorks.Common.Framework.SelInfo, for, info.
