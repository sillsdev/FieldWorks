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
