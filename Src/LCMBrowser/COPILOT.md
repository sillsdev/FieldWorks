---
last-reviewed: 2025-10-31
last-verified-commit: 063b43d
status: draft
---

# LCMBrowser COPILOT summary

## Purpose
Standalone developer/QA tool for browsing and inspecting FieldWorks LCModel database objects. Provides raw data browser interface for exploring LCM cache, object properties, relationships, and custom fields. Displays object trees (LCMClassList), property inspectors (LCMInspectorList), and data model viewer (ModelWnd). Supports GUID search, property selection, object editing (with AllowEdit flag), and FDO file save. Built on SIL.ObjectBrowser base framework with WeifenLuo DockPanel UI. Critical for debugging, QA validation, and understanding LCM data structures. Windows Forms desktop application (5.7K lines).

## Architecture
C# Windows Forms application (WinExe, net462) extending SIL.ObjectBrowser base class. LCMBrowserForm main window with docking panels (WeifenLuo.WinFormsUI.Docking). Three primary panels: ModelWnd (model browser), LangProjectWnd (language project inspector), RepositoryWnd (repository inspector). LCMClassList custom control for object tree navigation. LCMInspectorList custom control for property display. ClassPropertySelector dialog for choosing displayed properties. BrowserProjectId for project selection. Integrates with LCModel cache for data access.

## Key Components
- **LCMBrowser** (LCMBrowser.cs, 31 lines): Application entry point
  - Main() entry: Initializes ICU, SLDR, FwRegistry, runs LCMBrowserForm
  - STAThread for Windows Forms threading model
- **LCMBrowserForm** (LCMBrowserForm.cs, 2.8K lines): Main application window
  - Extends SIL.ObjectBrowser.ObjectBrowser base class
  - LcmCache integration: m_cache, m_lp (ILangProject), m_repoCmObject
  - Three docking panels: ModelWnd, LangProjectWnd (inspector), RepositoryWnd (inspector)
  - GUID search: m_tstxtGuidSrch text box, OnGuidSearchActivated() handler
  - Property selection: ClassPropertySelector dialog, OnSelectProperties() handler
  - Object editing: AllowEdit menu item (disabled by default for safety)
  - FDO file operations: Save menu for persisting changes
  - Custom menus/toolbars: SetupCustomMenusAndToolbarItems()
- **LCMClassList** (LCMClassList.cs, 537 lines): Object tree navigation control
  - Displays LCM object hierarchy by class
  - ShowCmObjectProperties static flag: Show/hide base CmObject properties
  - Tree view with class/object nodes
  - Context menu: Add, Delete, Move Up/Down objects
- **LCMInspectorList** (LCMInspectorList.cs, 1.4K lines): Property inspector control
  - Displays object properties in list/grid format
  - Property value editing when AllowEdit enabled
  - Multi-value property support (collections, sequences)
  - Virtual property display toggle (m_mnuDisplayVirtual)
- **ModelWnd** (ModelWnd.cs, 449 lines): Data model browser window
  - DockContent for model exploration
  - Shows LCM classes, fields, relationships
- **ClassPropertySelector** (ClassPropertySelector.cs, 200 lines): Property chooser dialog
  - Select which properties to display per class
  - CheckedListBox interface
  - Persists selections in settings
- **BrowserProjectId** (BrowserProjectId.cs, 151 lines): Project selection
  - Implements FwAppArgs for project identification
  - Project chooser on startup
- **CustomFields** (CustomFields.cs, 21 lines): Custom field metadata
  - Stores custom field definitions for display
  - Static list in LCMBrowserForm.CFields

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- Windows Forms (WinExe)
- WeifenLuo.WinFormsUI.Docking (docking panel framework)
- SIL.ObjectBrowser base framework
- LCModel for data access
- ICU and SLDR (writing system support)

## Dependencies

### Upstream (consumes)
- **LCModel**: Core data access (LcmCache, ICmObjectRepository, ILangProject)
- **SIL.ObjectBrowser**: Base browser framework (ObjectBrowser, InspectorWnd)
- **Common/FwUtils**: Utilities (FwRegistryHelper, FwUtils)
- **Common/Framework/DetailControls**: Detail control support
- **FdoUi**: UI helpers (CmObjectUi integration)
- **WeifenLuo.WinFormsUI.Docking**: Docking panel UI
- **SIL.WritingSystems**: Writing system support (SLDR)

### Downstream (consumed by)
- **Developers**: Browse/debug LCM data during development
- **QA**: Validate data integrity, inspect object relationships
- **Support**: Troubleshoot data issues in field

## Interop & Contracts
- **LcmCache**: Read/write access to LCModel database
- **ICmObjectRepository**: Object retrieval by GUID/class
- **ILangProject**: Root project object access
- **FwAppArgs**: Project selection via BrowserProjectId

## Threading & Performance
- **STAThread**: Required for Windows Forms
- **UI thread**: All LCM access on UI thread (not multi-threaded)
- **Lazy loading**: Tree/list views load data on demand

## Config & Feature Flags
- **AllowEdit** (m_mnuToolsAllowEdit): Enable/disable object editing (default: disabled for safety)
- **ShowCmObjectProperties** (Settings.Default.ShowCmObjectProperties): Show base CmObject properties
- **DisplayVirtual** (m_mnuDisplayVirtual): Show virtual properties

## Build Information
- **Project file**: LCMBrowser.csproj (net462, OutputType=WinExe)
- **Output**: LCMBrowser.exe
- **Build**: Via top-level FW.sln or: `msbuild LCMBrowser.csproj`
- **Run**: `LCMBrowser.exe` (prompts for project selection)
- **Dependencies**: WeifenLuo.WinFormsUI.Docking.dll (included)

## Interfaces and Data Models

- **LCMBrowserForm** (LCMBrowserForm.cs)
  - Purpose: Main browser window with docking panels
  - Key methods: OpenProjectWithDialog(), OnGuidSearchActivated(), OnSelectProperties(), OnAllowEdit()
  - Panels: ModelWnd, LangProjectWnd, RepositoryWnd
  - Notes: Extends SIL.ObjectBrowser.ObjectBrowser

- **LCMClassList** (LCMClassList.cs)
  - Purpose: Tree view for navigating LCM objects by class
  - Inputs: LcmCache, selected class IDs
  - Outputs: Selected object for inspection
  - Notes: Context menu for object manipulation

- **LCMInspectorList** (LCMInspectorList.cs)
  - Purpose: Display and edit object properties
  - Inputs: ICmObject instance
  - Outputs: Property values (read/write if AllowEdit)
  - Notes: Supports multi-value properties, virtual properties

- **ClassPropertySelector dialog** (ClassPropertySelector.cs)
  - Purpose: Choose which properties to display for a class
  - Inputs: LcmCache, class ID (clid)
  - Outputs: Selected property flids (field IDs)
  - Notes: Persists selections in user settings

- **GUID search**:
  - Inputs: GUID string in m_tstxtGuidSrch text box
  - Outputs: Navigates to matching object in tree/inspector
  - Notes: OnGuidSearchActivated() handler

## Entry Points
- **LCMBrowser.exe**: Main executable
- **Main()**: Entry point with ICU/SLDR initialization
- **LCMBrowserForm**: Main window

## Test Index
No dedicated test project (developer/QA tool).

## Usage Hints
- **Launch**: Run LCMBrowser.exe, select project from dialog
- **Navigate**: Use ModelWnd to explore classes, LCMClassList to browse objects
- **Inspect**: Select object in tree to view properties in LCMInspectorList
- **GUID search**: Enter GUID in toolbar search box to jump to object
- **Property selection**: Use "Select Properties" menu to customize displayed properties per class
- **Edit mode**: Enable "Allow Edit" menu (use with caution; can corrupt data)
- **Save**: Use "Save File" menu to persist changes
- **Virtual properties**: Toggle "Display Virtual" to show computed properties
- **CmObject properties**: Toggle toolbar button to show/hide base CmObject fields
- **Docking**: Drag panels to rearrange workspace
- **Developer tool**: Not for end users; for development/QA/debugging

## Related Folders
- **LCModel**: Data model being browsed
- **SIL.ObjectBrowser**: Base framework
- **FdoUi**: UI integration
- **Common/FwUtils**: Utilities

## References
- **Project file**: LCMBrowser.csproj (net462, WinExe)
- **Key C# files**: LCMBrowserForm.cs (2817 lines), LCMInspectorList.cs (1373 lines), LCMClassList.cs (537 lines), ModelWnd.cs (449 lines), ClassPropertySelector.cs (200 lines), BrowserProjectId.cs (151 lines)
- **Total lines of code**: 5658
- **Output**: LCMBrowser.exe (Output/Debug or Output/Release)
- **Framework**: .NET Framework 4.6.2
- **UI framework**: Windows Forms + WeifenLuo Docking
- **Namespace**: LCMBrowser
