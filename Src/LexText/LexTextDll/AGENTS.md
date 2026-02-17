---
last-reviewed: 2025-10-31
last-reviewed-tree: 2814f4356b54b9a12a970508d40ba3d5887bd059ef7ab9e0acb18f4af88eb223
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
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

## Dependencies
- Upstream: FwXApp base class
- Downstream: FLEx application host (instantiates LexTextApp)

## Interop & Contracts
- **IApp**: Application interface (XCore)

## Threading & Performance
- **UI thread**: All operations on UI thread

## Config & Feature Flags
- **webBrowserProgramLinux**: Configurable Linux web browser (default: "firefox")

## Build Information
- **Project file**: LexTextDll.csproj (net48, OutputType=Library)

## Interfaces and Data Models
LexTextApp, AreaListener, FlexHelpTopicProvider, RestoreDefaultsDlg.

## Entry Points
Loaded by the FieldWorks.exe host (LexTextExe stub removed). LexTextApp is instantiated as the FLEx application class.

## Test Index
- **Test project**: LexTextDllTests/

## Usage Hints
- **LexTextApp**: Main application class instantiated by FieldWorks.exe

## Related Folders
- **Common/FieldWorks/**: FieldWorks.exe host

## References
See `.cache/copilot/diff-plan.json` for file details.
