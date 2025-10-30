---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Common

## Purpose
Cross-cutting utilities and shared managed/native code used throughout FieldWorks. Contains fundamental UI controls, framework components, and utility libraries that multiple applications depend on.

## Key Components
### Key Classes
- **VwSelectionArgs**
- **SelPositionInfo**
- **PrintRootSite**
- **SelectionRestorer**
- **ActiveViewHelper**
- **RenderEngineFactory**
- **UpdateSemaphore**
- **DataUpdateMonitor**
- **FwRightMouseClickEventArgs**
- **SimpleRootSite**

### Key Interfaces
- **IPrintRootSite**
- **IChangeRootObject**
- **ISelectionChangeNotifier**
- **IRawElementProviderFragment**
- **IRawElementProviderFragmentRoot**
- **ITextProvider**
- **IValueProvider**
- **NavigateDirection**

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

## Entry Points
- Provides shared infrastructure, not directly executable
- Key interfaces and base classes used throughout FieldWorks

## Related Folders
- **XCore/** - Framework components that work with Common utilities
- **xWorks/** - Major consumer of Common UI controls and utilities
- **LexText/** - Uses Common controls for lexicon UI
- **FwCoreDlgs/** - Dialog components built on Common infrastructure
- **views/** - Native view layer that Common components interface with

## Code Evidence
*Analysis based on scanning 537 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: ControlExtenders, SIL.FieldWorks, SIL.FieldWorks.Common.Controls, SIL.FieldWorks.Common.Controls.Design, SIL.FieldWorks.Common.Controls.FileDialog
- **Project references**: ..\..\LexText\LexTextControls\LexTextControls
