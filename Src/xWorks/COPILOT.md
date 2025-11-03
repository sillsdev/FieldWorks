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
- **RecordDocView** (RecordDocView.cs) - Document view (read-only display)
- **XmlDocView** (XmlDocView.cs) - XML-configured document view
- **XhtmlDocView** (XhtmlDocView.cs) - XHTML export/display view
- **XhtmlRecordDocView** (XhtmlRecordDocView.cs) - Per-record XHTML view
- **XWorksViewBase** (XWorksViewBase.cs) - Shared view base class
- **GeneratedHtmlViewer** (GeneratedHtmlViewer.cs) - HTML preview pane

### Dictionary Configuration System
- **DictionaryConfigurationModel** (DictionaryConfigurationModel.cs) - Configuration data model
- **ConfigurableDictionaryNode** (ConfigurableDictionaryNode.cs) - Tree node for configuration
- **DictionaryConfigurationController**, **DictionaryConfigurationManagerController** - MVC controllers
- **DictionaryConfigMgrDlg**, **DictionaryConfigurationDlg** - Configuration dialogs
- **DictionaryConfigurationTreeControl** - Tree editor for configuration
- **DictionaryNodeOptions** (DictionaryNodeOptions.cs) - Per-node display options
- **DictionaryConfigurationMigrator** (DictionaryConfigurationMigrator.cs) - Config version migration
- **DictionaryDetailsController** (DictionaryDetailsController.cs) - Details panel controller

### XHTML/HTML Generation
- **LcmXhtmlGenerator** (LcmXhtmlGenerator.cs) - Main XHTML generator for dictionary export
- **LcmJsonGenerator** (LcmJsonGenerator.cs) - JSON export for Webonary
- **LcmWordGenerator** (LcmWordGenerator.cs) - Word document generation
- **ConfiguredLcmGenerator** (ConfiguredLcmGenerator.cs) - Configurable export generator
- **CssGenerator** (CssGenerator.cs) - CSS stylesheet generation
- **WordStylesGenerator** (WordStylesGenerator.cs) - Word style definitions
- **FlexStylesXmlAccessor** (FlexStylesXmlAccessor.cs) - FLEx styles to CSS mapping
- **DictionaryExportService** (DictionaryExportService.cs) - Export coordination

### Webonary Integration
- **UploadToWebonaryController**, **UploadToWebonaryModel** (UploadToWebonaryController.cs, UploadToWebonaryModel.cs) - Webonary upload
- **UploadToWebonaryDlg** (UploadToWebonaryDlg.cs) - Upload dialog
- **WebonaryClient** (WebonaryClient.cs) - Webonary API client implementing IWebonaryClient
- **WebonaryLogViewer** (WebonaryLogViewer.cs) - Upload log display
- **WebonaryUploadLog** (WebonaryUploadLog.cs) - Upload log model

### UI Components and Handlers
- **RecordBarListHandler**, **RecordBarTreeHandler** (RecordBarListHandler.cs, RecordBarTreeHandler.cs) - Record bar UI handlers
- **TreeBarHandlerUtils** (TreeBarHandlerUtils.cs) - Tree bar utilities
- **DTMenuHandler** (DTMenuHandler.cs) - Dynamic menu handler
- **LinkListener**, **MacroListener**, **TextListeners** - Event listeners
- **ImageHolder** (ImageHolder.cs) - Image display control

### Supporting Services
- **GlobalSettingServices** (GlobalSettingServices.cs) - Global settings management
- **ReversalIndexServices** (ReversalIndexServices.cs) - Reversal index operations
- **ExportDialog** (ExportDialog.cs) - Generic export dialog
- **LiftExportMessageDlg** (LiftExportMessageDlg.cs) - LIFT export messages
- **UnicodeCharacterEditingHelper** (UnicodeCharacterEditingHelper.cs) - PUA character support
- **SilErrorReportingAdapter** (SilErrorReportingAdapter.cs) - Error reporting integration

## Subfolders (detailed docs in individual COPILOT.md files)
- **xWorksTests/** - Comprehensive test suite
- **DictionaryConfigurationMigrators/** - Version-specific migration code
- **DictionaryDetailsView/** - Details view implementations
- **Archiving/** - RAMP/REAP archiving support
- **Resources/** - Images, XML configs, stylesheets

## Dependencies
- **Upstream**: XCore (Mediator, Inventory, XWindow), Common/Framework (FwApp, FwXApp), Common/RootSite (view infrastructure), LCModel (data model), LCModel.DomainServices (export), FdoUi (object-specific UI), Common/FwUtils (utilities)
- **Downstream consumers**: LexText/LexTextDll (LexTextApp extends FwXApp), all area-based FLEx applications

## Test Infrastructure
- **xWorksTests/** subfolder with comprehensive unit tests
- Tests for: Dictionary configuration, export generation, record management, view coordination

## Related Folders
- **XCore/** - Application framework foundation
- **LexText/** - Dictionary/lexicon areas built on xWorks
- **Common/Framework/** - FwApp base class
- **FdoUi/** - Object-specific UI components
- **FXT/** - XML export templates used by dictionary export

## References
- **Project**: xWorks.csproj (.NET Framework 4.6.2 class library)
- **Test project**: xWorksTests/xWorksTests.csproj
- **~97 CS files** in main folder (~66.9K lines): FwXApp.cs, FwXWindow.cs, RecordClerk.cs, RecordView hierarchy, DictionaryConfigurationModel.cs, LcmXhtmlGenerator.cs, UploadToWebonaryController.cs, etc.
- **Resources**: xWorksStrings.resx, DataTreeImages.resx, RecordClerkImages.resx, many dialog .resx files

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
- **Target frameworks**: net462
- **Key C# files**: DTMenuHandler.cs, DictionaryConfigurationDlg.Designer.cs, DictionaryConfigurationManagerDlg.Designer.cs, DictionaryNodeOptions.cs, DictionaryPublicationDecorator.cs, HeadWordNumbersDlg.cs, IDictionaryListOptionsView.cs, InterestingTextList.cs, LiftExportMessageDlg.Designer.cs, XmlDocConfigureDlg.Designer.cs
- **XML data/config**: strings-en.xml, strings-es-MX.xml, strings-fr.xml
- **Source file count**: 181 files
- **Data file count**: 38 files

## References (auto-generated hints)
- Project files:
  - Src/xWorks/xWorks.csproj
  - Src/xWorks/xWorksTests/xWorksTests.csproj
- Key C# files:
  - Src/xWorks/AddCustomFieldDlg.cs
  - Src/xWorks/Archiving/ArchivingExtensions.cs
  - Src/xWorks/Archiving/ReapRamp.cs
  - Src/xWorks/AssemblyInfo.cs
  - Src/xWorks/ConcDecorator.cs
  - Src/xWorks/ConfigurableDictionaryNode.cs
  - Src/xWorks/ConfiguredLcmGenerator.cs
  - Src/xWorks/CssGenerator.cs
  - Src/xWorks/CustomListDlg.Designer.cs
  - Src/xWorks/CustomListDlg.cs
  - Src/xWorks/DTMenuHandler.cs
  - Src/xWorks/DataTreeImages.cs
  - Src/xWorks/DeleteCustomList.cs
  - Src/xWorks/DictConfigModelExt.cs
  - Src/xWorks/DictionaryConfigManager.cs
  - Src/xWorks/DictionaryConfigMgrDlg.Designer.cs
  - Src/xWorks/DictionaryConfigMgrDlg.cs
  - Src/xWorks/DictionaryConfigurationController.cs
  - Src/xWorks/DictionaryConfigurationDlg.Designer.cs
  - Src/xWorks/DictionaryConfigurationDlg.cs
  - Src/xWorks/DictionaryConfigurationImportController.cs
  - Src/xWorks/DictionaryConfigurationImportDlg.Designer.cs
  - Src/xWorks/DictionaryConfigurationImportDlg.cs
  - Src/xWorks/DictionaryConfigurationListener.cs
  - Src/xWorks/DictionaryConfigurationManagerController.cs
- Data contracts/transforms:
  - Src/xWorks/AddCustomFieldDlg.resx
  - Src/xWorks/CustomListDlg.resx
  - Src/xWorks/DataTreeImages.resx
  - Src/xWorks/DictionaryConfigMgrDlg.resx
  - Src/xWorks/DictionaryConfigurationDlg.resx
  - Src/xWorks/DictionaryConfigurationImportDlg.resx
  - Src/xWorks/DictionaryConfigurationManagerDlg.resx
  - Src/xWorks/DictionaryConfigurationNodeRenameDlg.resx
  - Src/xWorks/DictionaryConfigurationTreeControl.resx
  - Src/xWorks/DictionaryDetailsView/ButtonOverPanel.resx
  - Src/xWorks/DictionaryDetailsView/DetailsView.resx
  - Src/xWorks/DictionaryDetailsView/GroupingOptionsView.resx
  - Src/xWorks/DictionaryDetailsView/LabelOverPanel.resx
  - Src/xWorks/DictionaryDetailsView/ListOptionsView.resx
  - Src/xWorks/DictionaryDetailsView/PictureOptionsView.resx
  - Src/xWorks/DictionaryDetailsView/SenseOptionsView.resx
  - Src/xWorks/ExportDialog.resx
  - Src/xWorks/ExportSemanticDomainsDlg.resx
  - Src/xWorks/ExportTranslatedListsDlg.resx
  - Src/xWorks/FwXWindow.resx
  - Src/xWorks/GeneratedHtmlViewer.resx
  - Src/xWorks/HeadWordNumbersDlg.resx
  - Src/xWorks/ImageHolder.resx
  - Src/xWorks/LiftExportMessageDlg.resx
  - Src/xWorks/RecordBrowseView.resx
## Code Evidence
*Analysis based on scanning 159 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.FieldWorks.XWorks, SIL.FieldWorks.XWorks.Archiving, SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators, SIL.FieldWorks.XWorks.DictionaryDetailsView, SIL.FieldWorks.XWorks.LexText