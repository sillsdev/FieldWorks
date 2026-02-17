---
applyTo: "Src/xWorks/**"
name: "xworks.instructions"
description: "Auto-generated concise instructions from COPILOT.md for xWorks"
---

# xWorks (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **FwXApp** (FwXApp.cs) - Abstract base class extending FwApp
- `OnMasterRefresh(object sender)` - Master refresh coordination
- `DefaultConfigurationPathname` property - XML config file path
- Subclassed by LexTextApp, etc.
- **FwXWindow** (FwXWindow.cs) - Main area-based window extending XWindow
- Hosts: RecordView, RecordClerk, area switching UI

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: e3d23340d2c25cc047a44f5a66afbeddb81369a04741c212090ccece2fd83a28
status: reviewed
---

# xWorks

## Purpose
Main application shell and area-based UI framework (~66.9K lines in main folder + subfolders) built on XCore. Provides FwXApp (application base class), FwXWindow (area-based window), RecordClerk (record management), RecordView hierarchy (browse/edit/doc views), dictionary configuration subsystem (ConfigurableDictionaryNode, DictionaryConfigurationModel), and XHTML export (LcmXhtmlGenerator, DictionaryExportService, Webonary upload). Implements area-switching UI, record browsing/editing, configurable dictionary publishing, and interlinear text display for all FieldWorks applications.

## Key Components

### Application Framework
- **FwXApp** (FwXApp.cs) - Abstract base class extending FwApp
  - `OnMasterRefresh(object sender)` - Master refresh coordination
  - `DefaultConfigurationPathname` property - XML config file path
  - Subclassed by LexTextApp, etc.
- **FwXWindow** (FwXWindow.cs) - Main area-based window extending XWindow
  - Hosts: RecordView, RecordClerk, area switching UI
  - XML-driven configuration via Inventory system

### Record Management (RecordClerk.cs, SubitemRecordClerk.cs)
- **RecordClerk** - Master record list manager
  - `CurrentObject` property - Active LCModel object
  - `OnRecordNavigation` - Record navigation handling
  - Filters: m_filters, m_filterProvider
  - Sorters: m_sorter, m_sortName
- **SubitemRecordClerk** - Subitem/sub-entry management
- **RecordList** (RecordList.cs) - Manages lists of records with filtering/sorting
- **InterestingTextList** (InterestingTextList.cs) - Text corpus management

### View Hierarchy
- **RecordView** (RecordView.cs) - Abstract base for record display views
- **RecordBrowseView** (RecordBrowseView.cs) - Browse view (list/grid)
- **RecordEditView** (RecordEditView.cs) - Edit view (form-based)
- **RecordDocView** (RecordDocView.cs) - Document view (re
