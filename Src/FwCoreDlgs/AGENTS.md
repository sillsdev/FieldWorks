---
last-reviewed: 2025-10-31
last-reviewed-tree: 686f899291d7c6b63b4532a7d7d32a41b409d3198444a91f4ba68020df7a99ac
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# FwCoreDlgs COPILOT summary

## Purpose
Common dialogs and UI components shared across FieldWorks applications. Comprehensive collection of standardized dialog boxes including backup/restore (BackupProjectSettings, RestoreProjectPresenter), project management (ChooseLangProjectDialog, AddNewUserDlg), writing system configuration (WritingSystemPropertiesDialog, AdvancedScriptRegionVariantView), converter management (AddCnvtrDlg, EncConverters), find/replace (BasicFindDialog, FindReplaceDialog), character context display (CharContextCtrl), archiving (ArchiveWithRamp), and numerous other common UI patterns. Ensures consistent user experience across xWorks, LexText, and other FieldWorks applications. Over 35K lines of dialog and UI code.

## Architecture
C# class library (.NET Framework 4.8.x) with Windows Forms dialogs and controls. Extensive collection of ~90 C# files providing reusable UI components. Many dialogs follow MVP (Model-View-Presenter) pattern (e.g., BackupProjectPresenter, RestoreProjectPresenter). Localized strings via resource files (*Strings.Designer.cs, *Resources.Designer.cs). Test project FwCoreDlgsTests validates dialog behavior.

## Key Components
- **BackupProjectSettings** (BackupProjectSettings.cs): Backup configuration
  - Properties: Comment, ConfigurationSettings, MediaFiles, Fonts, Keyboards, DestinationFolder
  - Serializable settings for backup operations
- **RestoreProjectPresenter**: Restore project dialog presenter (MVP pattern)
- **ChooseLangProjectDialog**: Project selection dialog
- **AddNewUserDlg**: Add user to project
- **WritingSystemPropertiesDialog**: Writing system configuration
- **AdvancedScriptRegionVariantView**: Advanced WS script/region/variant editor
- **AddCnvtrDlg**: Add encoding converter dialog
- **BasicFindDialog, FindReplaceDialog**: Search functionality
- **CharContextCtrl**: Character context display control
- **ArchiveWithRamp**: RAMP archiving support
- **LanguageChooser**: Language selection UI
- **MergeObjectDlg**: Object merging dialog
- **ProgressDialogWithTask**: Long operation progress
- **ValidCharactersDlg**: Valid characters configuration
- **CheckBoxColumnHeaderHandler**: Checkbox header for grid columns
- **Numerous specialized dialogs**: AddNewVernLangWarningDlg, AdvancedEncProps, and many more

## Technology Stack
- C# .NET Framework 4.8.x (net8)

## Dependencies
- Upstream: Data model (LcmCache, ICmObject)
- Downstream: Uses FwCoreDlgs for common UI

## Interop & Contracts
- Many dialogs implement standard Windows Forms patterns (ShowDialog, DialogResult)

## Threading & Performance
- **UI thread required**: All dialog operations

## Config & Feature Flags
- **BackupProjectSettings**: Configurable backup options (media, fonts, keyboards, config)

## Build Information
- **Project file**: FwCoreDlgs.csproj (net48, OutputType=Library)

## Interfaces and Data Models
BackupProjectSettings, ChooseLangProjectDialog, WritingSystemPropertiesDialog, ProgressDialogWithTask, MergeObjectDlg.

## Entry Points
Referenced as library by FieldWorks applications. Dialogs instantiated and shown via ShowDialog() pattern.

## Test Index
- **Test project**: FwCoreDlgsTests/

## Usage Hints
- Use standard Windows Forms pattern: instantiate dialog, call ShowDialog(), check DialogResult

## Related Folders
- **Common/Framework**: Framework using these dialogs

## References
See `.cache/copilot/diff-plan.json` for file details.
