---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# UIAdapterInterfaces

## Purpose
UI adapter pattern interfaces for FieldWorks. Defines contracts for adapting different UI technologies and providing abstraction layers between UI implementations and business logic.

## Key Components
### Key Classes
- **TMItemProperties**
- **TMBarProperties**
- **WindowListInfo**
- **SBTabProperties**
- **SBTabItemProperties**

### Key Interfaces
- **ISIBInterface**
- **ITMAdapter**

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

## Code Evidence
*Analysis based on scanning 4 source files*

- **Classes found**: 5 public classes
- **Interfaces found**: 2 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.UIAdapters
