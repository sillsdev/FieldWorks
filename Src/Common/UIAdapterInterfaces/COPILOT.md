# Common/UIAdapterInterfaces

## Purpose
UI adapter pattern interfaces for FieldWorks. Defines contracts for adapting different UI technologies and providing abstraction layers between UI implementations and business logic.

## Key Components
- **UIAdapterInterfaces.csproj** - Interface definitions library
- **SIBInterface.cs** - Sidebar interface definitions
- **TMInterface.cs** - Toolbar/menu interface definitions
- **HelperClasses.cs** - Support classes for adapters

## Technology Stack
- C# .NET
- Interface-based design
- Adapter pattern implementation

## Dependencies
- Depends on: Minimal (interface definitions)
- Used by: XCore, UI adapter implementations

## Build Information
- C# interface library project
- Build via: `dotnet build UIAdapterInterfaces.csproj`
- Pure interface definitions

## Entry Points
- Interface contracts for UI adapters
- Abstraction layer for UI technologies

## Related Folders
- **XCore/** - Uses these interfaces extensively
- **XCore/FlexUIAdapter/** - Implements these interfaces
- **Common/Controls/** - Controls that work with adapters
