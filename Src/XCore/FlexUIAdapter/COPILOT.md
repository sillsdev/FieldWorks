---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FlexUIAdapter

## Purpose
FLEx UI adapter implementation. Provides the concrete implementation of UI adapter interfaces for FieldWorks applications, translating between XCore framework commands and actual UI controls.

## Key Components
### Key Classes
- **ToolStripManager**
- **ReBarAdapter**
- **ContextHelper**
- **SidebarAdapter**
- **MenuAdapter**
- **BarAdapterBase**
- **PaneBar**
- **AdapterBase**
- **PanelCollection**

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

## Code Evidence
*Analysis based on scanning 11 source files*

- **Classes found**: 9 public classes
- **Namespaces**: XCore, XCoreUnused
