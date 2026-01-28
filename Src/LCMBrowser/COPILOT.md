---
last-reviewed: 2025-10-31
last-reviewed-tree: d039883a0aeb01f7efa9710c250a2d2d16f68a61b91d800d98d0709f4b452257
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# LCMBrowser COPILOT summary

## Purpose
Standalone developer/QA tool for browsing and inspecting FieldWorks LCModel database objects. Provides raw data browser interface for exploring LCM cache, object properties, relationships, and custom fields. Displays object trees (LCMClassList), property inspectors (LCMInspectorList), and data model viewer (ModelWnd). Supports GUID search, property selection, object editing (with AllowEdit flag), and FDO file save. Built on SIL.ObjectBrowser base framework with WeifenLuo DockPanel UI. Critical for debugging, QA validation, and understanding LCM data structures. Windows Forms desktop application (5.7K lines).

## Architecture
C# Windows Forms application (WinExe, net48) extending SIL.ObjectBrowser base class. LCMBrowserForm main window with docking panels (WeifenLuo.WinFormsUI.Docking). Three primary panels: ModelWnd (model browser), LangProjectWnd (language project inspector), RepositoryWnd (repository inspector). LCMClassList custom control for object tree navigation. LCMInspectorList custom control for property display. ClassPropertySelector dialog for choosing displayed properties. BrowserProjectId for project selection. Integrates with LCModel cache for data access.

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
C# .NET Framework 4.8.x, Windows Forms (WinExe), WeifenLuo.WinFormsUI.Docking, SIL.ObjectBrowser, LCModel, ICU/SLDR.

## Dependencies
Consumes: LCModel (LcmCache, ICmObjectRepository), SIL.ObjectBrowser, FwUtils, FdoUi, WeifenLuo.WinFormsUI.Docking. Used by: developers, QA, support (debugging/troubleshooting tool).

## Interop & Contracts
LcmCache (read/write), ICmObjectRepository (GUID/class retrieval), ILangProject, FwAppArgs (BrowserProjectId).

## Threading & Performance
STAThread, UI thread only. Lazy loading for tree/list views.

## Config & Feature Flags
AllowEdit (default: disabled for safety), ShowCmObjectProperties, DisplayVirtual.

## Build Information
LCMBrowser.csproj (net48, WinExe), output: LCMBrowser.exe. Run: `LCMBrowser.exe` (prompts for project).

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
LCMBrowser.exe (Main() with ICU/SLDR init), LCMBrowserForm main window.

## Test Index
No test project (developer/QA tool).

## Usage Hints
Launch LCMBrowser.exe, select project. Navigate via ModelWnd (classes) and LCMClassList (objects). GUID search in toolbar. "Select Properties" menu for customization. Enable "Allow Edit" with caution. Developer/QA/debugging tool only.

## Related Folders
LCModel (data model), SIL.ObjectBrowser (base framework), FdoUi, FwUtils.

## References
LCMBrowser.csproj (net48, WinExe), 5.7K lines. Key files: LCMBrowserForm.cs (2.8K), LCMInspectorList.cs (1.4K), LCMClassList.cs (537). See `.cache/copilot/diff-plan.json` for file inventory.
