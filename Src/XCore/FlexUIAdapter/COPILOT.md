---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# XCore/FlexUIAdapter

## Purpose
FLEx UI adapter implementation. Provides the concrete implementation of UI adapter interfaces for FieldWorks applications, translating between XCore framework commands and actual UI controls.

## Key Components
- **FlexUIAdapter.csproj** - UI adapter library
- **AdapterBase.cs** - Base adapter class
- **BarAdapterBase.cs** - Toolbar/menubar adapter base
- **ContextHelper.cs** - Context management for adapters
- Accessibility mode registry settings
- Adapter localization resources

## Technology Stack
- C# .NET WinForms
- Adapter pattern implementation
- XCore framework integration

## Dependencies
- Depends on: XCore/xCoreInterfaces, Common/UIAdapterInterfaces
- Used by: xWorks, LexText (for UI integration)

## Build Information
- C# class library project
- Build via: `dotnet build FlexUIAdapter.csproj`
- UI adapter implementation

## Entry Points
- Adapter base classes for UI components
- Context helpers for command routing
- Accessibility support

## Related Folders
- **XCore/xCoreInterfaces/** - Interfaces implemented by adapters
- **Common/UIAdapterInterfaces/** - Additional adapter interfaces
- **XCore/** - Framework using these adapters
- **xWorks/** - Application using UI adapters


## References
- **Project Files**: FlexUIAdapter.csproj
- **Key C# Files**: AdapterBase.cs, BarAdapterBase.cs, ContextHelper.cs, MenuAdapter.cs, NavBarAdapter.cs, PaneBar.cs, PanelButton.cs, PanelMenu.cs
