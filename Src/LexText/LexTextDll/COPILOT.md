---
last-reviewed: 2025-10-31
last-reviewed-tree: 2814f4356b54b9a12a970508d40ba3d5887bd059ef7ab9e0acb18f4af88eb223
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/LexText/LexTextDll. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# LexTextDll COPILOT summary

## Purpose
Core business logic library for FLEx lexicon and text features. Provides LexTextApp application class (extends FwXApp), AreaListener (XCore colleague managing list area configuration), FlexHelpTopicProvider (context-sensitive help), RestoreDefaultsDlg (restore default settings), and resource files (localized strings, images, help topic paths). Central application coordination layer between XCore framework and lexicon/text-specific functionality. Small focused library (2.8K lines) providing essential infrastructure without heavy UI or business logic (which lives in Lexicon/, Interlinear/, etc.).

## Architecture
C# library (net48, OutputType=Library) with application infrastructure classes. LexTextApp main application class (extends FwXApp, implements IApp, IxCoreColleague). AreaListener XCore colleague for managing area configuration. Resource files for localization (LexTextStrings.resx) and help topics (HelpTopicPaths.resx). ImageHolder icon resources. Integrates XCore framework, LCModel, and lexicon/text-specific features.

## Key Components
- **LexTextApp** (LexTextApp.cs, 955 lines): Main FLEx lexicon/text application class
  - Extends FwXApp (FieldWorks application base)
  - Implements IApp, IxCoreColleague (XCore integration)
  - DoApplicationInitialization(): Splash screen operations, message dialogs
  - InitializeMessageDialogs(): Setup message box manager
  - webBrowserProgramLinux: Linux web browser selection ("firefox")
  - Constructor: Takes IFieldWorksManager, IHelpTopicProvider, FwAppArgs
- **AreaListener** (AreaListener.cs, 1.1K lines): XCore colleague managing list area configuration
  - Implements IxCoreColleague, IDisposable
  - Tracks lists loaded in List area (m_ctotalLists, m_ccustomLists)
  - Mediator integration (m_mediator, m_propertyTable)
  - Configuration management for area customization
  - MediatorDispose attribute for proper cleanup
- **FlexHelpTopicProvider** (FlexHelpTopicProvider.cs, 29 lines): Context-sensitive help
  - Implements IHelpTopicProvider
  - Maps UI contexts to help topics
  - Uses HelpTopicPaths.resx resource
- **RestoreDefaultsDlg** (RestoreDefaultsDlg.cs, 26 lines): Restore defaults dialog
  - Confirm restoration of default settings
  - Simple dialog with Designer file
- **TransductionSample** (TransductionSample.cs, 114 lines): Transduction sample class
  - Sample/example for transduction operations
- **LexTextStrings** (LexTextStrings.Designer.cs, LexTextStrings.resx, 530 lines): Localized strings
  - Designer-generated resource accessor
  - Localized UI strings for lexicon/text features
- **HelpTopicPaths** (HelpTopicPaths.resx): Help topic mappings
  - Resource file mapping contexts to help topics
  - Large resource file (215KB)
- **ImageHolder** (ImageHolder.cs, ImageHolder.resx, 156 lines): Icon resources
  - Embedded icons/images for lexicon/text UI
  - Resource accessor class

## Technology Stack
- C# .NET Framework 4.8.x (net8)
- OutputType: Library
- XCore (application framework)
- LCModel (data model)
- Windows Forms (dialogs)
- Resource files (.resx) for localization and resources

## Dependencies

### Upstream (consumes)
- **Common/Framework**: FwXApp base class
- **XCore**: Mediator, IxCoreColleague, IApp
- **LCModel**: Data model
- **Common/FwUtils**: Utilities
- **Common/Controls**: UI controls
- **Common/RootSites**: Root site infrastructure
- **Interlinear/**: IText namespace
- **LexTextControls/**: Dialog controls

### Downstream (consumed by)
- **FieldWorks.exe**: FLEx application host (instantiates LexTextApp)
- **Lexicon/**: Lexicon editing UI
- **Interlinear/**: Text analysis UI
- **Morphology/**: Morphology UI
- **xWorks/**: Application shell

## Interop & Contracts
- **IApp**: Application interface (XCore)
- **IxCoreColleague**: XCore colleague pattern
- **FwXApp**: FieldWorks application base class
- **IFieldWorksManager**: FieldWorks manager interface
- **IHelpTopicProvider**: Help topic provider interface
- **Mediator**: XCore mediation pattern

## Threading & Performance
- **UI thread**: All operations on UI thread
- **Splash screen operations**: DoApplicationInitialization() runs during splash

## Config & Feature Flags
- **webBrowserProgramLinux**: Configurable Linux web browser (default: "firefox")
- **Area configuration**: AreaListener manages list area customization

## Build Information
- **Project file**: LexTextDll.csproj (net48, OutputType=Library)
- **Test project**: LexTextDllTests/
- **Output**: SIL.FieldWorks.XWorks.LexText.dll
- **Build**: Via top-level FieldWorks.sln or: `msbuild LexTextDll.csproj`
- **Run tests**: `dotnet test LexTextDllTests/`

## Interfaces and Data Models

- **LexTextApp** (LexTextApp.cs)
  - Purpose: Main application class for FLEx lexicon/text features
  - Base: FwXApp
  - Interfaces: IApp, IxCoreColleague
  - Key methods: DoApplicationInitialization(), InitializeMessageDialogs()
  - Notes: Coordinates XCore framework with lexicon/text functionality

- **AreaListener** (AreaListener.cs)
  - Purpose: Manage list area configuration
  - Interfaces: IxCoreColleague, IDisposable
  - Properties: m_ctotalLists (total list count), m_ccustomLists (custom list count)
  - Notes: XCore colleague for area customization

- **FlexHelpTopicProvider** (FlexHelpTopicProvider.cs)
  - Purpose: Context-sensitive help
  - Interface: IHelpTopicProvider
  - Notes: Maps UI contexts to help topics in HelpTopicPaths.resx

- **RestoreDefaultsDlg** (RestoreDefaultsDlg.cs)
  - Purpose: Confirm restoration of default settings
  - Notes: Simple confirmation dialog

## Entry Points
Loaded by the FieldWorks.exe host (LexTextExe stub removed). LexTextApp is instantiated as the FLEx application class.

## Test Index
- **Test project**: LexTextDllTests/
- **Run tests**: `dotnet test LexTextDllTests/`
- **Coverage**: Application initialization, area listener logic

## Usage Hints
- **LexTextApp**: Main application class instantiated by FieldWorks.exe
- **AreaListener**: Manages list area configuration (XCore colleague)
- **FlexHelpTopicProvider**: Provides context-sensitive help
- **Resources**: LexTextStrings for localized UI strings, ImageHolder for icons
- **Small library**: 2.8K lines, focused on application infrastructure
- **Business logic elsewhere**: Heavy UI and business logic in Lexicon/, Interlinear/, etc.

## Related Folders
- **Common/FieldWorks/**: FieldWorks.exe host
- **LexTextControls/**: Shared UI controls
- **Lexicon/**: Lexicon editing UI
- **Interlinear/**: Text analysis UI
- **Common/Framework**: FwXApp base class

## References
- **Project file**: LexTextDll.csproj (net48, OutputType=Library)
- **Key C# files**: AreaListener.cs (1113 lines), LexTextApp.cs (955 lines), LexTextStrings.Designer.cs (530 lines), ImageHolder.cs (156 lines), TransductionSample.cs (114 lines), FlexHelpTopicProvider.cs (29 lines), RestoreDefaultsDlg.cs (26 lines), AssemblyInfo.cs (14 lines)
- **Resources**: LexTextStrings.resx (13.6KB), HelpTopicPaths.resx (215KB), ImageHolder.resx (23.6KB)
- **Test project**: LexTextDllTests/
- **Total lines of code**: 2800
- **Output**: SIL.FieldWorks.XWorks.LexText.dll
- **Namespace**: SIL.FieldWorks.XWorks.LexText