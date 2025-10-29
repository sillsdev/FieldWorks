# Common

## Purpose
Cross-cutting utilities and shared managed/native code used throughout FieldWorks. Contains fundamental UI controls, framework components, and utility libraries that multiple applications depend on.

## Key Components

### Subprojects
Each subfolder has its own COPILOT.md file with detailed documentation:

- **Controls/** - Shared UI controls library (see Controls/COPILOT.md)
- **FieldWorks/** - Core FieldWorks-specific utilities (see FieldWorks/COPILOT.md)
- **Filters/** - Data filtering functionality (see Filters/COPILOT.md)
- **Framework/** - Application framework components (see Framework/COPILOT.md)
- **FwUtils/** - General FieldWorks utilities (see FwUtils/COPILOT.md)
- **RootSite/** - Root-level site management for views (see RootSite/COPILOT.md)
- **ScriptureUtils/** - Scripture-specific utilities (see ScriptureUtils/COPILOT.md)
- **SimpleRootSite/** - Simplified root site implementation (see SimpleRootSite/COPILOT.md)
- **UIAdapterInterfaces/** - UI adapter pattern interfaces (see UIAdapterInterfaces/COPILOT.md)
- **ViewsInterfaces/** - View layer interfaces (see ViewsInterfaces/COPILOT.md)

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
