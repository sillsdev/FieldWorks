---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# xWorks

## Purpose
Primary FieldWorks application shell and module hosting infrastructure.
Implements the main application framework (xWorks) that hosts LexText and other work areas,
provides dictionary configuration (DictionaryConfigurationDlg, DictionaryNodeOptions), area switching,
data navigation, and shared application services. Serves as the container that brings together
different FieldWorks tools into an integrated application environment.

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

## Build Information
- C# application with extensive test suite
- Build with MSBuild or Visual Studio
- Primary executable for FieldWorks

## Entry Points
- Main application executable
- Application shell hosting various modules (LexText, etc.)
- Dictionary and data tree interfaces

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

## Code Evidence
*Analysis based on scanning 159 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.FieldWorks.XWorks, SIL.FieldWorks.XWorks.Archiving, SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators, SIL.FieldWorks.XWorks.DictionaryDetailsView, SIL.FieldWorks.XWorks.LexText

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

## References

- **Project files**: xWorks.csproj, xWorksTests.csproj
- **Target frameworks**: net462
- **Key C# files**: DTMenuHandler.cs, DictionaryConfigurationDlg.Designer.cs, DictionaryConfigurationManagerDlg.Designer.cs, DictionaryNodeOptions.cs, DictionaryPublicationDecorator.cs, HeadWordNumbersDlg.cs, IDictionaryListOptionsView.cs, InterestingTextList.cs, LiftExportMessageDlg.Designer.cs, XmlDocConfigureDlg.Designer.cs
- **XML data/config**: strings-en.xml, strings-es-MX.xml, strings-fr.xml
- **Source file count**: 181 files
- **Data file count**: 38 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\xWorks\xWorks.csproj
  - Src\xWorks\xWorksTests\xWorksTests.csproj
- Key C# files:
  - Src\xWorks\AddCustomFieldDlg.cs
  - Src\xWorks\Archiving\ArchivingExtensions.cs
  - Src\xWorks\Archiving\ReapRamp.cs
  - Src\xWorks\AssemblyInfo.cs
  - Src\xWorks\ConcDecorator.cs
  - Src\xWorks\ConfigurableDictionaryNode.cs
  - Src\xWorks\ConfiguredLcmGenerator.cs
  - Src\xWorks\CssGenerator.cs
  - Src\xWorks\CustomListDlg.Designer.cs
  - Src\xWorks\CustomListDlg.cs
  - Src\xWorks\DTMenuHandler.cs
  - Src\xWorks\DataTreeImages.cs
  - Src\xWorks\DeleteCustomList.cs
  - Src\xWorks\DictConfigModelExt.cs
  - Src\xWorks\DictionaryConfigManager.cs
  - Src\xWorks\DictionaryConfigMgrDlg.Designer.cs
  - Src\xWorks\DictionaryConfigMgrDlg.cs
  - Src\xWorks\DictionaryConfigurationController.cs
  - Src\xWorks\DictionaryConfigurationDlg.Designer.cs
  - Src\xWorks\DictionaryConfigurationDlg.cs
  - Src\xWorks\DictionaryConfigurationImportController.cs
  - Src\xWorks\DictionaryConfigurationImportDlg.Designer.cs
  - Src\xWorks\DictionaryConfigurationImportDlg.cs
  - Src\xWorks\DictionaryConfigurationListener.cs
  - Src\xWorks\DictionaryConfigurationManagerController.cs
- Data contracts/transforms:
  - Src\xWorks\AddCustomFieldDlg.resx
  - Src\xWorks\CustomListDlg.resx
  - Src\xWorks\DataTreeImages.resx
  - Src\xWorks\DictionaryConfigMgrDlg.resx
  - Src\xWorks\DictionaryConfigurationDlg.resx
  - Src\xWorks\DictionaryConfigurationImportDlg.resx
  - Src\xWorks\DictionaryConfigurationManagerDlg.resx
  - Src\xWorks\DictionaryConfigurationNodeRenameDlg.resx
  - Src\xWorks\DictionaryConfigurationTreeControl.resx
  - Src\xWorks\DictionaryDetailsView\ButtonOverPanel.resx
  - Src\xWorks\DictionaryDetailsView\DetailsView.resx
  - Src\xWorks\DictionaryDetailsView\GroupingOptionsView.resx
  - Src\xWorks\DictionaryDetailsView\LabelOverPanel.resx
  - Src\xWorks\DictionaryDetailsView\ListOptionsView.resx
  - Src\xWorks\DictionaryDetailsView\PictureOptionsView.resx
  - Src\xWorks\DictionaryDetailsView\SenseOptionsView.resx
  - Src\xWorks\ExportDialog.resx
  - Src\xWorks\ExportSemanticDomainsDlg.resx
  - Src\xWorks\ExportTranslatedListsDlg.resx
  - Src\xWorks\FwXWindow.resx
  - Src\xWorks\GeneratedHtmlViewer.resx
  - Src\xWorks\HeadWordNumbersDlg.resx
  - Src\xWorks\ImageHolder.resx
  - Src\xWorks\LiftExportMessageDlg.resx
  - Src\xWorks\RecordBrowseView.resx
