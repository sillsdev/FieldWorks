---
last-reviewed: 2025-10-31
last-reviewed-tree: f3bbf7799d98247c33f5af21f8b6949cc55d7899ef7fe6dd752d40785cb9c4e3
status: draft
---

# FwCoreDlgs COPILOT summary

## Purpose
Common dialogs and UI components shared across FieldWorks applications. Comprehensive collection of standardized dialog boxes including backup/restore (BackupProjectSettings, RestoreProjectPresenter), project management (ChooseLangProjectDialog, AddNewUserDlg), writing system configuration (WritingSystemPropertiesDialog, AdvancedScriptRegionVariantView), converter management (AddCnvtrDlg, EncConverters), find/replace (BasicFindDialog, FindReplaceDialog), character context display (CharContextCtrl), archiving (ArchiveWithRamp), and numerous other common UI patterns. Ensures consistent user experience across xWorks, LexText, and other FieldWorks applications. Over 35K lines of dialog and UI code.

## Architecture
C# class library (.NET Framework 4.6.2) with Windows Forms dialogs and controls. Extensive collection of ~90 C# files providing reusable UI components. Many dialogs follow MVP (Model-View-Presenter) pattern (e.g., BackupProjectPresenter, RestoreProjectPresenter). Localized strings via resource files (*Strings.Designer.cs, *Resources.Designer.cs). Test project FwCoreDlgsTests validates dialog behavior.

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
- OutputType: Library
- Windows Forms (extensive use of Form, Control, UserControl)
- MVP pattern for complex dialogs
- Resource files for localization

## Dependencies

### Upstream (consumes)
- **SIL.LCModel**: Data model (LcmCache, ICmObject)
- **Common/Framework**: Application framework
- **Common/Controls**: Common controls
- **Common/FwUtils**: Utilities (DirectoryFinder, etc.)
- **Windows Forms**: UI framework
- **XCore**: Command routing for some dialogs

### Downstream (consumed by)
- **xWorks**: Uses FwCoreDlgs for common UI
- **LexText**: Lexicon editing dialogs
- **All FieldWorks applications**: Standardized dialog experience

## Interop & Contracts
- Many dialogs implement standard Windows Forms patterns (ShowDialog, DialogResult)
- MVP pattern for testability (presenters separate from views)
- Resource-based localization

## Threading & Performance
- **UI thread required**: All dialog operations
- **Progress dialogs**: ProgressDialogWithTask for responsive long operations
- **Performance**: Standard dialog performance; some with caching

## Config & Feature Flags
- **BackupProjectSettings**: Configurable backup options (media, fonts, keyboards, config)
- Dialogs configured via properties and initialization methods
- Many dialogs accept configuration objects

## Build Information
- **Project file**: FwCoreDlgs.csproj (net462, OutputType=Library)
- **Test project**: FwCoreDlgsTests/
- **Output**: FwCoreDlgs.dll
- **Build**: Via top-level FieldWorks.sln
- **Run tests**: `dotnet test FwCoreDlgsTests/`

## Interfaces and Data Models

- **BackupProjectSettings** (BackupProjectSettings.cs)
  - Purpose: Backup configuration data
  - Properties: Comment, ConfigurationSettings, MediaFiles, Fonts, Keyboards, DestinationFolder
  - Notes: XML serializable; default DestinationFolder from DirectoryFinder

- **ChooseLangProjectDialog**
  - Purpose: User selects language project to open
  - Inputs: Available projects
  - Outputs: Selected project (DialogResult.OK) or cancellation
  - Notes: Standard project selection UI

- **WritingSystemPropertiesDialog**
  - Purpose: Configure writing system properties
  - Inputs: Writing system definition
  - Outputs: Modified WS configuration
  - Notes: Comprehensive WS editing including script, region, variant

- **BasicFindDialog, FindReplaceDialog**
  - Purpose: Find and replace text operations
  - Inputs: Search parameters, scope
  - Outputs: Find/replace operations
  - Notes: Standard search UI pattern

- **ProgressDialogWithTask**
  - Purpose: Show progress during long-running operations
  - Inputs: Task delegate, cancellation token
  - Outputs: Task completion or cancellation
  - Notes: Keeps UI responsive with progress feedback

- **MergeObjectDlg**
  - Purpose: Merge duplicate data objects
  - Inputs: Source and target objects
  - Outputs: Merged object
  - Notes: Conflict resolution UI

## Entry Points
Referenced as library by FieldWorks applications. Dialogs instantiated and shown via ShowDialog() pattern.

## Test Index
- **Test project**: FwCoreDlgsTests/
- **Run tests**: `dotnet test FwCoreDlgsTests/`
- **Coverage**: Dialog initialization, presenter logic, MVP patterns

## Usage Hints
- Use standard Windows Forms pattern: instantiate dialog, call ShowDialog(), check DialogResult
- MVP pattern dialogs: create presenter, initialize, call Run()
- BackupProjectSettings for configurable backups
- ProgressDialogWithTask for long operations with cancellation
- Many dialogs are application-modal; use carefully
- Localized strings via resource files

## Related Folders
- **Common/Framework**: Framework using these dialogs
- **Common/Controls**: Complementary controls
- **xWorks, LexText**: Major consumers

## References
- **Project files**: FwCoreDlgs.csproj (net462), FwCoreDlgsTests/
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: ~90 dialog and control files including BackupProjectSettings.cs, ChooseLangProjectDialog.cs, WritingSystemPropertiesDialog.cs, and many more
- **Total lines of code**: 35502
- **Output**: FwCoreDlgs.dll
- **Namespace**: SIL.FieldWorks.FwCoreDlgs