---
last-reviewed: 2025-10-31
last-reviewed-tree: 63ed79fcc6cda62d113b1fb1f808833b752c6d157dcd8c45b715310ebbd26da1
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# xWorks

## Purpose
Primary FieldWorks application shell and module hosting infrastructure.
Implements the main application framework (xWorks) that hosts LexText and other work areas,
provides dictionary configuration (DictionaryConfigurationDlg, DictionaryNodeOptions), area switching,
data navigation, and shared application services. Serves as the container that brings together
different FieldWorks tools into an integrated application environment.

## Architecture
C# library with 181 source files. Contains 1 subprojects: xWorks.

## Key Components
### Key Classes
- **DTMenuHandler**
- **HeadwordNumbersDlg**
- **DictionaryPublicationDecorator**
- **DictionaryNodeOptions**
- **DictionaryNodeSenseOptions**
- **DictionaryNodeListOptions**
- **DictionaryNodeOption**
- **DictionaryNodeListAndParaOptions**
- **DictionaryNodeWritingSystemAndParaOptions**
- **DictionaryNodeWritingSystemOptions**

### Key Interfaces
- **IDictionaryListOptionsView**
- **IParaOption**
- **IDictionaryGroupingOptionsView**
- **ILcmStylesGenerator**
- **IFragmentWriter**
- **IFragment**
- **IHeadwordNumbersView**
- **IDictionarySenseOptionsView**

## Technology Stack
- C# .NET WinForms/WPF
- Application shell architecture
- Dictionary and data visualization
- XCore-based plugin framework

## Dependencies
- Depends on: XCore (framework), Cellar (data model), Common (UI), FdoUi (data UI), FwCoreDlgs (dialogs), views (rendering)
- Used by: End users as the main FieldWorks application

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
configuration settings.

## Build Information
- C# application with extensive test suite
- Build with MSBuild or Visual Studio
- Primary executable for FieldWorks

## Interfaces and Data Models

- **IDictionaryGroupingOptionsView** (interface)
  - Path: `IDictionaryGroupingOptionsView.cs`
  - Public interface definition

- **IDictionaryListOptionsView** (interface)
  - Path: `IDictionaryListOptionsView.cs`
  - Public interface definition

- **IFragment** (interface)
  - Path: `ConfiguredLcmGenerator.cs`
  - Public interface definition

- **IFragmentWriter** (interface)
  - Path: `ConfiguredLcmGenerator.cs`
  - Public interface definition

- **ILcmStylesGenerator** (interface)
  - Path: `ConfiguredLcmGenerator.cs`
  - Public interface definition

- **IParaOption** (interface)
  - Path: `DictionaryNodeOptions.cs`
  - Public interface definition

- **DTMenuHandler** (class)
  - Path: `DTMenuHandler.cs`
  - Public class implementation

- **DictionaryNodeGroupingOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeListAndParaOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeListOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeOption** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodePictureOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeReferringSenseOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeSenseOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeWritingSystemAndParaOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryNodeWritingSystemOptions** (class)
  - Path: `DictionaryNodeOptions.cs`
  - Public class implementation

- **DictionaryPublicationDecorator** (class)
  - Path: `DictionaryPublicationDecorator.cs`
  - Public class implementation

- **InterestingTextList** (class)
  - Path: `InterestingTextList.cs`
  - Public class implementation

- **InterestingTextsChangedArgs** (class)
  - Path: `InterestingTextList.cs`
  - Public class implementation

- **RecordBrowseActiveView** (class)
  - Path: `RecordBrowseView.cs`
  - Public class implementation

- **RecordBrowseView** (class)
  - Path: `RecordBrowseView.cs`
  - Public class implementation

- **RecordView** (class)
  - Path: `RecordView.cs`
  - Public class implementation

- **TreeBarHandlerUtils** (class)
  - Path: `TreeBarHandlerUtils.cs`
  - Public class implementation

- **VisibleListItem** (class)
  - Path: `DictionaryConfigMgrDlg.cs`
  - Public class implementation

- **WordStylesGenerator** (class)
  - Path: `WordStylesGenerator.cs`
  - Public class implementation

- **AlignmentType** (enum)
  - Path: `DictionaryNodeOptions.cs`

- **ListIds** (enum)
  - Path: `DictionaryNodeOptions.cs`

- **TreebarAvailability** (enum)
  - Path: `XWorksViewBase.cs`

- **WritingSystemType** (enum)
  - Path: `DictionaryNodeOptions.cs`

## Entry Points
- Main application executable
- Application shell hosting various modules (LexText, etc.)
- Dictionary and data tree interfaces

## Test Index
Test projects: xWorksTests. 46 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **XCore/** - Application framework that xWorks is built on
- **LexText/** - Major module hosted by xWorks
- **Common/** - UI infrastructure used throughout xWorks
- **FdoUi/** - Data object UI components
- **FwCoreDlgs/** - Dialogs used in xWorks
- **views/** - Native rendering engine for data display
- **ManagedVwWindow/** - View window management
- **Cellar/** - Data model accessed by xWorks
- **FwResources/** - Resources used in xWorks UI

## References

- **Project files**: xWorks.csproj, xWorksTests.csproj
- **Target frameworks**: net48
- **Key C# files**: DTMenuHandler.cs, DictionaryConfigurationDlg.Designer.cs, DictionaryConfigurationManagerDlg.Designer.cs, DictionaryNodeOptions.cs, DictionaryPublicationDecorator.cs, HeadWordNumbersDlg.cs, IDictionaryListOptionsView.cs, InterestingTextList.cs, LiftExportMessageDlg.Designer.cs, XmlDocConfigureDlg.Designer.cs
- **XML data/config**: strings-en.xml, strings-es-MX.xml, strings-fr.xml
- **Source file count**: 181 files
- **Data file count**: 38 files

## Subfolders (detailed docs in individual COPILOT.md files)
- **xWorksTests/** - Comprehensive test suite
- **DictionaryConfigurationMigrators/** - Version-specific migration code
- **DictionaryDetailsView/** - Details view implementations
- **Archiving/** - RAMP/REAP archiving support
- **Resources/** - Images, XML configs, stylesheets

## Test Infrastructure
- **xWorksTests/** subfolder with comprehensive unit tests
- Tests for: Dictionary configuration, export generation, record management, view coordination

## Code Evidence
*Analysis based on scanning 159 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.FieldWorks.XWorks, SIL.FieldWorks.XWorks.Archiving, SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators, SIL.FieldWorks.XWorks.DictionaryDetailsView, SIL.FieldWorks.XWorks.LexText
