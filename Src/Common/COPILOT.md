# Common

## Purpose
Cross-cutting utilities and shared managed/native code used throughout FieldWorks. Contains fundamental UI controls, framework components, and utility libraries that multiple applications depend on.

## Key Components

### Subprojects
- **Controls/** - Shared UI controls used across applications
- **FieldWorks/** - Core FieldWorks-specific utilities and helpers
- **Filters/** (Filters.csproj) - Data filtering functionality
- **Framework/** - Application framework components
- **FwUtils/** - General FieldWorks utilities
- **RootSite/** (RootSite.csproj) - Root-level site management for views
- **ScriptureUtils/** (ScriptureUtils.csproj) - Scripture-specific utilities
- **SimpleRootSite/** (SimpleRootSite.csproj) - Simplified root site implementation
- **UIAdapterInterfaces/** - UI adapter pattern interfaces
- **ViewsInterfaces/** (ViewsInterfaces.csproj) - View layer interfaces

## Technology Stack
- Mix of C# and C++/CLI
- UI framework components
- Cross-platform utility patterns

## Dependencies
- Depends on: Kernel, Generic (for low-level utilities)
- Used by: Almost all FieldWorks applications and libraries

## Build Information
- Multiple C# projects within subfolders
- Mix of library and interface projects
- Build all subprojects as part of solution build

## Testing
- Some subprojects may have associated test projects
- Tests typically located in corresponding Test folders

## Entry Points
- Provides shared infrastructure, not directly executable
- Key interfaces and base classes used throughout FieldWorks

## Related Folders
- **XCore/** - Framework components that work with Common utilities
- **xWorks/** - Major consumer of Common UI controls and utilities
- **LexText/** - Uses Common controls for lexicon UI
- **FwCoreDlgs/** - Dialog components built on Common infrastructure
- **views/** - Native view layer that Common components interface with
