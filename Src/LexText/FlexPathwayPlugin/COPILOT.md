---
last-reviewed: 2025-10-31
last-verified-commit: 8b06ba1
status: draft
---

# FlexPathwayPlugin COPILOT summary

## Purpose
Integration plugin connecting FieldWorks FLEx with SIL Pathway publishing solution. Implements IUtility interface allowing FLEx users to export lexicon/dictionary data via Pathway for print/digital publication. Appears as "Pathway" option in FLEx Tools → Configure menu. Handles data export to Pathway-compatible formats, folder management, and Pathway process launching. Provides seamless publishing workflow from FLEx to final output (PDF, ePub, etc.). Small focused plugin (595 lines) bridging FLEx and external Pathway publishing system.

## Architecture
C# library (net462, OutputType=Library) implementing IUtility and IFeedbackInfoProvider interfaces. FlexPathwayPlugin main class handles export dialog integration, data preparation, and Pathway invocation. MyFolders static utility class for folder operations (copy, create, naming). Integrates with FwCoreDlgs UtilityDlg framework. Discovered/loaded by FLEx Tools menu via IUtility interface pattern.

## Key Components
- **FlexPathwayPlugin** (FlexPathwayPlugin.cs, 464 lines): Main plugin implementation
  - Implements IUtility: Label property, Dialog property, OnSelection()
  - Implements IFeedbackInfoProvider: Feedback info for support
  - UtilityDlg exportDialog: Access to dialog, mediator, LcmCache
  - Label property: Returns "Pathway" for Tools menu display
  - OnSelection(): Called when user selects Pathway utility
  - Process(): Main export logic - prepares data, launches Pathway
  - ExpCss constant: "main.css" default CSS
  - Registry integration: Reads Pathway installation path
  - Pathway invocation: Launches external Pathway.exe with exported data
- **MyFolders** (myFolders.cs, 119 lines): Folder utility class
  - Copy(): Recursive folder copy with filter support
  - GetNewName(): Generate unique folder names (appends counter if exists)
  - CreateDirectory(): Create directory with error handling, access rights check
  - Regex-based naming: Handles numbered folder suffixes (name1, name2, etc.)

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Windows Forms (MessageBox for errors)
- Registry API (Microsoft.Win32.Registry) for Pathway path lookup
- File I/O (System.IO)

## Dependencies

### Upstream (consumes)
- **FwCoreDlgs**: UtilityDlg framework
- **Common/FwUtils/Pathway**: Pathway integration utilities
- **LCModel**: Data access (LcmCache)
- **XCore**: Mediator pattern
- **Common/RootSites**: Root site support
- **FwResources**: Resources
- **SIL Pathway** (external): Publishing solution (invoked via Process.Start)

### Downstream (consumed by)
- **FLEx**: Tools → Configure → Pathway menu option
- **Users**: Dictionary publishing workflow

## Interop & Contracts
- **IUtility**: FLEx utility interface (Label, Dialog, OnSelection(), Process())
- **IFeedbackInfoProvider**: Support feedback interface
- **UtilityDlg**: Dialog integration (exposes Mediator, Cache, FeedbackInfoProvider)
- **Pathway.exe**: External process invocation (SIL Pathway publishing tool)
- **Registry**: Reads Pathway installation path from registry

## Threading & Performance
- **UI thread**: All operations on UI thread
- **Process invocation**: Launches Pathway.exe as separate process
- **I/O operations**: Folder copy, file operations (synchronous)

## Config & Feature Flags
- **Registry**: Pathway installation path in Windows registry
- **ExpCss**: Default CSS file name ("main.css")

## Build Information
- **Project file**: FlexPathwayPlugin.csproj (net462, OutputType=Library)
- **Test project**: FlexPathwayPluginTests/
- **Output**: SIL.FieldWorks.FlexPathwayPlugin.dll
- **Build**: Via top-level FW.sln or: `msbuild FlexPathwayPlugin.csproj`
- **Run tests**: `dotnet test FlexPathwayPluginTests/`
- **Discovery**: Loaded by FLEx via IUtility interface (reflection or explicit reference)

## Interfaces and Data Models

- **FlexPathwayPlugin** (FlexPathwayPlugin.cs)
  - Purpose: Pathway export utility implementation
  - Interface: IUtility (Label, Dialog, OnSelection(), Process())
  - Interface: IFeedbackInfoProvider (feedback for support)
  - Inputs: UtilityDlg (provides Mediator, LcmCache)
  - Outputs: Exports data, launches Pathway.exe
  - Notes: Appears as "Pathway" in FLEx Tools menu

- **IUtility interface**:
  - Label: Display name for Tools menu ("Pathway")
  - Dialog: UtilityDlg setter for accessing FLEx infrastructure
  - OnSelection(): Called when utility selected in dialog
  - Process(): Execute utility's main functionality

- **MyFolders** (myFolders.cs)
  - Purpose: Folder management utilities
  - Key methods: Copy(src, dst, dirFilter, appName), GetNewName(directory, name), CreateDirectory(outPath, appName)
  - Inputs: Source/destination paths, filter patterns
  - Outputs: Folder operations (copy, create), unique names
  - Notes: Static utility class, error handling with MessageBox

## Entry Points
Loaded by FLEx Tools → Configure menu. FlexPathwayPlugin class instantiated when user selects Pathway utility.

## Test Index
- **Test project**: FlexPathwayPluginTests/
- **Run tests**: `dotnet test FlexPathwayPluginTests/`
- **Coverage**: Pathway export logic, folder utilities

## Usage Hints
- **Access**: In FLEx, Tools → Configure → select "Pathway" utility
- **Requirement**: SIL Pathway must be installed separately
- **Workflow**: Select Pathway utility → configure export → Process() exports data → launches Pathway.exe
- **Registry**: Plugin reads Pathway installation path from Windows registry
- **Output**: Exported data prepared in configured folder, Pathway opens for publishing
- **Formats**: Pathway supports PDF, ePub, Word, InDesign, etc. (handled by Pathway, not plugin)
- **Folder management**: MyFolders utilities handle temp folder creation, unique naming
- **Error handling**: MessageBox for folder permission errors

## Related Folders
- **FwCoreDlgs**: UtilityDlg framework
- **Common/FwUtils/Pathway**: Pathway integration utilities
- **LexTextExe**: FLEx application (hosts plugin)
- **xWorks**: Application framework

## References
- **Project file**: FlexPathwayPlugin.csproj (net462, OutputType=Library)
- **Key C# files**: FlexPathwayPlugin.cs (464 lines), myFolders.cs (119 lines), AssemblyInfo.cs (12 lines)
- **Test project**: FlexPathwayPluginTests/
- **Total lines of code**: 595
- **Output**: SIL.FieldWorks.FlexPathwayPlugin.dll
- **Namespace**: SIL.PublishingSolution
- **External dependency**: SIL Pathway (separate installation, invoked via Process.Start)
- **Interface**: IUtility, IFeedbackInfoProvider
