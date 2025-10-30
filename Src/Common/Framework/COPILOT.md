---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Common/Framework

## Purpose
Application framework components for FieldWorks. Provides core application infrastructure including editing helpers, settings management, and export functionality.

## Key Components
- **Framework.csproj** - Main framework library
- **FwApp.cs** - Application base class
- **FwEditingHelper.cs** - Editing infrastructure support
- **FwRegistrySettings.cs** - Registry-based settings
- **ExportStyleInfo.cs** - Export styling configuration
- **ExternalSettingsAccessorBase.cs** - Settings abstraction
- **FrameworkTests/** - Framework test suite


## Key Classes/Interfaces
- **FwEditingHelper**
- **ExportStyleInfo**
- **IPublicationView**
- **IPageSetupDialog**
- **IMainWindowDelegatedFunctions**
- **IMainWindowDelegateCallbacks**
- **FwRegistrySettings**
- **IFieldWorksManager**

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

## Testing
- Run tests: `dotnet test Framework/FrameworkTests/`
- Tests cover framework components and settings

## Entry Points
- Base application classes (FwApp)
- Editing helper infrastructure
- Settings and configuration APIs

## Related Folders
- **Common/FwUtils/** - Utilities used by framework
- **Common/FieldWorks/** - FieldWorks-specific infrastructure
- **XCore/** - Application framework that uses Common/Framework
- **xWorks/** - Applications built on this framework


## References
- **Project Files**: Framework.csproj
- **Key C# Files**: ExportStyleInfo.cs, ExternalSettingsAccessorBase.cs, FwApp.cs, FwEditingHelper.cs, FwRegistrySettings.cs, FwRootSite.cs, IFieldWorksManager.cs, IFwMainWnd.cs
