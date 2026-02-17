---
last-reviewed: 2025-10-31
last-reviewed-tree: 0b46a07bacc1ebfb88a3f7245988715314fcbb60b0bad599b15fb69ae99807b8
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - referenced-by
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

# FlexPathwayPlugin COPILOT summary

## Purpose
Integration plugin connecting FieldWorks FLEx with SIL Pathway publishing solution. Implements IUtility interface allowing FLEx users to export lexicon/dictionary data via Pathway for print/digital publication. Appears as "Pathway" option in FLEx Tools → Configure menu. Handles data export to Pathway-compatible formats, folder management, and Pathway process launching. Provides seamless publishing workflow from FLEx to final output (PDF, ePub, etc.). Small focused plugin (595 lines) bridging FLEx and external Pathway publishing system.

### Referenced By

- [Pathway Export](../../../openspec/specs/lexicon/export/pathway.md#behavior) — Pathway publishing workflow

## Architecture
C# library (net48, OutputType=Library) implementing IUtility and IFeedbackInfoProvider interfaces. FlexPathwayPlugin main class handles export dialog integration, data preparation, and Pathway invocation. MyFolders static utility class for folder operations (copy, create, naming). Integrates with FwCoreDlgs UtilityDlg framework. Discovered/loaded by FLEx Tools menu via IUtility interface pattern.

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
- C# .NET Framework 4.8.x (net8)

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- IUtility: FLEx utility interface (Label, Dialog, OnSelection(), Process())

## Threading & Performance
- UI thread: All operations on UI thread

## Config & Feature Flags
- Registry: Pathway installation path in Windows registry

## Build Information
- Project file: FlexPathwayPlugin.csproj (net48, OutputType=Library)

## Interfaces and Data Models
FlexPathwayPlugin, MyFolders.

## Entry Points
Loaded by FLEx Tools → Configure menu. FlexPathwayPlugin class instantiated when user selects Pathway utility.

## Test Index
- Test project: FlexPathwayPluginTests/

## Usage Hints
- Access: In FLEx, Tools → Configure → select "Pathway" utility

## Related Folders
- FwCoreDlgs: UtilityDlg framework

## References
See `.cache/copilot/diff-plan.json` for file details.
