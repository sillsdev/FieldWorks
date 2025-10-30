---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# LexText

## Purpose
Lexicon/Dictionary application suite and related components.
Encompasses the FieldWorks Language Explorer (FLEx) application including lexicon editing (Lexicon),
dictionary configuration (LexTextDll), interlinear text analysis (Interlinear), morphological
analysis (Morphology, ParserCore, ParserUI), discourse analysis (Discourse), specialized controls
(LexTextControls), and publishing integration (FlexPathwayPlugin). Complete solution for
dictionary development and linguistic text analysis.

## Key Components
### Key Classes
- **FlexPathwayPlugin**
- **DeExportDialog**
- **MyFolders**
- **MyFoldersTest**
- **FlexPathwayPluginTest**
- **InterlinearMapping**
- **Sfm2FlexTextWordsFrag**
- **Sfm2FlexTextMappingBase**
- **Sfm2FlexTextBase**
- **PhonologicalFeatureChooserDlg**

### Key Interfaces
- **IFwExtension**
- **ICmLiftObject**
- **IPatternControl**
- **ILexReferenceSlice**
- **IParser**
- **IHCLoadErrorLogger**
- **IXAmpleWrapper**
- **IInterlinRibbon**

## Technology Stack
- C# .NET WinForms/WPF
- Complex linguistic data processing
- Dictionary and lexicon management
- Interlinear glossing and text analysis

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), FdoUi (data object UI), XCore (framework), xWorks (shared app infrastructure)
- Used by: Linguists and language workers for lexicon and text work

## Build Information
- Multiple C# projects comprising the lexicon application
- Build entire suite via solution or individual projects
- Build with MSBuild or Visual Studio

## Entry Points
- **LexTextExe** - Main lexicon application executable
- **LexTextDll** - Core functionality exposed to other components

## Related Folders
- **xWorks/** - Shared application infrastructure for LexText
- **XCore/** - Framework components used by LexText
- **FdoUi/** - Data object UI used in lexicon editing
- **Cellar/** - Data model for lexicon data
- **FwParatextLexiconPlugin/** - Exposes LexText data to Paratext
- **ParatextImport/** - Imports Paratext data into LexText

## Code Evidence
*Analysis based on scanning 430 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: FlexDePluginTests, FlexPathwayPluginTests, LexEdDllTests, LexTextControlsTests, LexTextDllTests
- **Project references**: ..\..\Common\Controls\DetailControls\DetailControls, ..\..\Common\Controls\XMLViews\XMLViews

## Interfaces and Data Models

- **IFwExtension** (interface)
  - Path: `LexTextControls/IFwExtension.cs`
  - Public interface definition

- **AddAllomorphDlg** (class)
  - Path: `LexTextControls/AddAllomorphDlg.cs`
  - Public class implementation

- **ContentMapping** (class)
  - Path: `LexTextControls/LexImportWizardHelpers.cs`
  - Public class implementation

- **DeExportDialog** (class)
  - Path: `FlexPathwayPlugin/FlexPathwayPlugin.cs`
  - Public class implementation

- **FlexPathwayPlugin** (class)
  - Path: `FlexPathwayPlugin/FlexPathwayPlugin.cs`
  - Public class implementation

- **InterlinearMapping** (class)
  - Path: `LexTextControls/Sfm2FlexTextWords.cs`
  - Public class implementation

- **LexImport** (class)
  - Path: `LexTextControls/LexImport.cs`
  - Public class implementation

- **LexImportField** (class)
  - Path: `LexTextControls/LexImportWizardHelpers.cs`
  - Public class implementation

- **LexImportFields** (class)
  - Path: `LexTextControls/LexImportWizardHelpers.cs`
  - Public class implementation

- **LexImportWizard** (class)
  - Path: `LexTextControls/LexImportWizard.cs`
  - Public class implementation

- **LexReferenceDetailsDlg** (class)
  - Path: `LexTextControls/LexReferenceDetailsDlg.cs`
  - Public class implementation

- **ListViewItemComparer** (class)
  - Path: `LexTextControls/LexImportWizardHelpers.cs`
  - Public class implementation

- **MarkerPresenter** (class)
  - Path: `LexTextControls/LexImportWizardHelpers.cs`
  - Public class implementation

- **MasterCategoryListDlg** (class)
  - Path: `LexTextControls/MasterCategoryListDlg.cs`
  - Public class implementation

- **MyFolders** (class)
  - Path: `FlexPathwayPlugin/myFolders.cs`
  - Public class implementation

- **OccurrenceDlg** (class)
  - Path: `LexTextControls/OccurrenceDlg.cs`
  - Public class implementation

- **PhonologicalFeatureChooserDlg** (class)
  - Path: `LexTextControls/PhonologicalFeatureChooserDlg.cs`
  - Public class implementation

- **RecordGoDlg** (class)
  - Path: `LexTextControls/RecordGoDlg.cs`
  - Public class implementation

- **Sfm2FlexTextBase** (class)
  - Path: `LexTextControls/Sfm2FlexTextWords.cs`
  - Public class implementation

- **Sfm2FlexTextMappingBase** (class)
  - Path: `LexTextControls/Sfm2FlexTextWords.cs`
  - Public class implementation

- **Sfm2FlexTextWordsFrag** (class)
  - Path: `LexTextControls/Sfm2FlexTextWords.cs`
  - Public class implementation

- **CFChanges** (enum)
  - Path: `LexTextControls/LexImportWizardHelpers.cs`

- **ImageKind** (enum)
  - Path: `LexTextControls/FeatureStructureTreeView.cs`

- **InterlinDestination** (enum)
  - Path: `LexTextControls/Sfm2FlexTextWords.cs`

- **MorphTypeFilterType** (enum)
  - Path: `LexTextControls/InsertEntryDlg.cs`

- **NodeKind** (enum)
  - Path: `LexTextControls/FeatureStructureTreeView.cs`

## References

- **Project files**: Discourse.csproj, DiscourseTests.csproj, FlexPathwayPlugin.csproj, FlexPathwayPluginTests.csproj, ITextDll.csproj, ITextDllTests.csproj, LexEdDll.csproj, LexEdDllTests.csproj, LexTextControls.csproj, LexTextControlsTests.csproj, LexTextDll.csproj, LexTextDllTests.csproj, LexTextExe.csproj, MGA.csproj, MGATests.csproj, MorphologyEditorDll.csproj, MorphologyEditorDllTests.csproj, ParserCore.csproj, ParserCoreTests.csproj, ParserUI.csproj, ParserUITests.csproj, XAmpleCOMWrapper.vcxproj, XAmpleManagedWrapper.csproj, XAmpleManagedWrapperTests.csproj
- **Target frameworks**: net462
- **Key dependencies**: ..\..\Common\Controls\DetailControls\DetailControls, ..\..\Common\Controls\XMLViews\XMLViews
- **Key C# files**: AddAllomorphDlg.cs, AssemblyInfo.cs, AssemblyInfo.cs, FlexPathwayPlugin.cs, LexImportWizard.cs, LexImportWizardHelpers.cs, MasterCategoryListDlg.cs, PhonologicalFeatureChooserDlg.cs, Sfm2FlexTextWords.cs, myFolders.cs
- **Key C++ files**: XAmpleCOMWrapper.cpp, XAmpleWrapper.cpp, XAmpleWrapperCore.cpp, stdafx.cpp
- **Key headers**: Resource.h, XAmpleWrapperCore.h, stdafx.h, xamplewrapper.h
- **XAML files**: ParserReportDialog.xaml, ParserReportsDialog.xaml
- **XSLT transforms**: CreateFeatureCatalog.xsl, MasterToEticGlossList.xsl, schematron-report.xsl, skeleton1-5.xsl, verbid.xsl
- **XML data/config**: Failures.xml, FeatureSystem2.xml, FeatureSystem3.xml, IrregularlyInflectedFormsParserFxtResult.xml, emi-flexFxtResult.xml
- **Source file count**: 487 files
- **Data file count**: 450 files

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
  - Src\LexText\Discourse\Discourse.csproj
  - Src\LexText\Discourse\DiscourseTests\DiscourseTests.csproj
  - Src\LexText\FlexPathwayPlugin\FlexPathwayPlugin.csproj
  - Src\LexText\FlexPathwayPlugin\FlexPathwayPluginTests\FlexPathwayPluginTests.csproj
  - Src\LexText\Interlinear\ITextDll.csproj
  - Src\LexText\Interlinear\ITextDllTests\ITextDllTests.csproj
  - Src\LexText\LexTextControls\LexTextControls.csproj
  - Src\LexText\LexTextControls\LexTextControlsTests\LexTextControlsTests.csproj
  - Src\LexText\LexTextDll\LexTextDll.csproj
  - Src\LexText\LexTextDll\LexTextDllTests\LexTextDllTests.csproj
  - Src\LexText\LexTextExe\LexTextExe.csproj
  - Src\LexText\Lexicon\LexEdDll.csproj
  - Src\LexText\Lexicon\LexEdDllTests\LexEdDllTests.csproj
  - Src\LexText\Morphology\MGA\MGA.csproj
  - Src\LexText\Morphology\MGA\MGATests\MGATests.csproj
  - Src\LexText\Morphology\MorphologyEditorDll.csproj
  - Src\LexText\Morphology\MorphologyEditorDllTests\MorphologyEditorDllTests.csproj
  - Src\LexText\ParserCore\ParserCore.csproj
  - Src\LexText\ParserCore\ParserCoreTests\ParserCoreTests.csproj
  - Src\LexText\ParserCore\XAmpleCOMWrapper\XAmpleCOMWrapper.vcxproj
  - Src\LexText\ParserCore\XAmpleManagedWrapper\BuildInclude.targets
  - Src\LexText\ParserCore\XAmpleManagedWrapper\XAmpleManagedWrapper.csproj
  - Src\LexText\ParserCore\XAmpleManagedWrapper\XAmpleManagedWrapperTests\XAmpleManagedWrapperTests.csproj
  - Src\LexText\ParserUI\ParserUI.csproj
  - Src\LexText\ParserUI\ParserUITests\ParserUITests.csproj
- Key C# files:
  - Src\LexText\Discourse\AdvancedMTDialog.Designer.cs
  - Src\LexText\Discourse\AdvancedMTDialog.cs
  - Src\LexText\Discourse\ChartLocation.cs
  - Src\LexText\Discourse\ConstChartBody.cs
  - Src\LexText\Discourse\ConstChartRowDecorator.cs
  - Src\LexText\Discourse\ConstChartVc.cs
  - Src\LexText\Discourse\ConstituentChart.Designer.cs
  - Src\LexText\Discourse\ConstituentChart.cs
  - Src\LexText\Discourse\ConstituentChartLogic.cs
  - Src\LexText\Discourse\DiscourseExportDialog.cs
  - Src\LexText\Discourse\DiscourseExporter.cs
  - Src\LexText\Discourse\DiscourseStrings.Designer.cs
  - Src\LexText\Discourse\DiscourseTests\AdvancedMTDialogLogicTests.cs
  - Src\LexText\Discourse\DiscourseTests\ConstChartRowDecoratorTests.cs
  - Src\LexText\Discourse\DiscourseTests\ConstituentChartDatabaseTests.cs
  - Src\LexText\Discourse\DiscourseTests\ConstituentChartTests.cs
  - Src\LexText\Discourse\DiscourseTests\DiscourseExportTests.cs
  - Src\LexText\Discourse\DiscourseTests\DiscourseTestHelper.cs
  - Src\LexText\Discourse\DiscourseTests\InMemoryDiscourseTestBase.cs
  - Src\LexText\Discourse\DiscourseTests\InMemoryLogicTest.cs
  - Src\LexText\Discourse\DiscourseTests\InMemoryMoveEditTests.cs
  - Src\LexText\Discourse\DiscourseTests\InMemoryMovedTextTests.cs
  - Src\LexText\Discourse\DiscourseTests\InterlinRibbonTests.cs
  - Src\LexText\Discourse\DiscourseTests\LogicTest.cs
  - Src\LexText\Discourse\DiscourseTests\MultilevelHeaderModelTests.cs
- Key C++ files:
  - Src\LexText\ParserCore\XAmpleCOMWrapper\XAmpleCOMWrapper.cpp
  - Src\LexText\ParserCore\XAmpleCOMWrapper\XAmpleWrapper.cpp
  - Src\LexText\ParserCore\XAmpleCOMWrapper\XAmpleWrapperCore.cpp
  - Src\LexText\ParserCore\XAmpleCOMWrapper\stdafx.cpp
- Key headers:
  - Src\LexText\ParserCore\XAmpleCOMWrapper\Resource.h
  - Src\LexText\ParserCore\XAmpleCOMWrapper\XAmpleWrapperCore.h
  - Src\LexText\ParserCore\XAmpleCOMWrapper\stdafx.h
  - Src\LexText\ParserCore\XAmpleCOMWrapper\xamplewrapper.h
- Data contracts/transforms:
  - Src\LexText\Discourse\AdvancedMTDialog.resx
  - Src\LexText\Discourse\ConstChartBody.resx
  - Src\LexText\Discourse\ConstituentChart.resx
  - Src\LexText\Discourse\DiscourseStrings.resx
  - Src\LexText\Discourse\SelectClausesDialog.resx
  - Src\LexText\Interlinear\ChooseTextWritingSystemDlg.resx
  - Src\LexText\Interlinear\ComplexConcControl.resx
  - Src\LexText\Interlinear\ComplexConcMorphDlg.resx
  - Src\LexText\Interlinear\ComplexConcTagDlg.resx
  - Src\LexText\Interlinear\ComplexConcWordDlg.resx
  - Src\LexText\Interlinear\ConcordanceControl.resx
  - Src\LexText\Interlinear\ConfigureInterlinDialog.resx
  - Src\LexText\Interlinear\CreateAllomorphTypeMismatchDlg.resx
  - Src\LexText\Interlinear\EditMorphBreaksDlg.resx
  - Src\LexText\Interlinear\FilterAllTextsDialog.resx
  - Src\LexText\Interlinear\FilterTextsDialog.resx
  - Src\LexText\Interlinear\FocusBoxController.resx
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\Phase1-KalabaTest.xml
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\Phase1-KalabaTestPunctuation.xml
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\Phase1-KalabaTestPunctuationWordAlignedXLingPap.xml
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\Phase1-KalabaTestWordAlignedXLingPap.xml
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\Phase1-OrizabaLesson2.xml
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\Phase1-OrizabaLesson2WordAlignedXLingPap.xml
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\Phase1-SETepehuanCorn.xml
  - Src\LexText\Interlinear\ITextDllTests\ExportTestFiles\SETepehuanCornSingleListExample.xml
