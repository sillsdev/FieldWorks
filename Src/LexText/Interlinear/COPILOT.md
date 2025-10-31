---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Interlinear

## Purpose
Interlinear text analysis and morpheme-by-morpheme glossing functionality.
Implements the interlinear text editor (InterlinDocChart, InterlinVc), morpheme analysis tools,
glossing interfaces, concordance views (ConcordanceControl), and text import capabilities.
Central to linguistic text analysis workflows in FLEx, enabling detailed morphosyntactic
annotation of natural language texts.

## Architecture
C# library with 124 source files. Contains 1 subprojects: ITextDll.

## Key Components
### Key Classes
- **InterlinPrintChild**
- **InterlinPrintVc**
- **ConcordanceControl**
- **OccurrencesOfSelectedUnit**
- **MatchingConcordanceItems**
- **InterlinearTextsRecordClerk**
- **InterlinearExportDialog**
- **InterlinDocChart**
- **ParseIsCurrentFixer**
- **ComplexConcLeafNode**

### Key Interfaces
- **IParaDataLoader**
- **ISelectOccurrence**
- **ISetupLineChoices**
- **IInterlinearTabControl**
- **IStyleSheet**
- **IHandleBookmark**
- **IStTextBookmark**
- **IInterlinConfigurable**

## Technology Stack
- C# .NET WinForms
- Complex text layout and rendering
- Linguistic analysis algorithms

## Dependencies
- Depends on: Cellar (data model), Common (UI and views), LexText/ParserCore
- Used by: LexText application for text analysis

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Threading model: synchronization.

## Config & Feature Flags
Config files: toolConfigurationForITextDllTests.xml.

## Build Information
- C# class library project
- Build via: `dotnet build ITextDll.csproj` (Note: project named ITextDll)
- Core component of text analysis features

## Interfaces and Data Models

- **IInterlinearTabControl** (interface)
  - Path: `InterlinDocRootSiteBase.cs`
  - Public interface definition

- **IParaDataLoader** (interface)
  - Path: `InterlinVc.cs`
  - Public interface definition

- **ISelectOccurrence** (interface)
  - Path: `InterlinDocRootSiteBase.cs`
  - Public interface definition

- **ISetupLineChoices** (interface)
  - Path: `InterlinDocRootSiteBase.cs`
  - Public interface definition

- **IStyleSheet** (interface)
  - Path: `InterlinDocRootSiteBase.cs`
  - Public interface definition

- **ComplexConcLeafNode** (class)
  - Path: `ComplexConcLeafNode.cs`
  - Public class implementation

- **ComplexConcPatternModel** (class)
  - Path: `ComplexConcPatternModel.cs`
  - Public class implementation

- **ComplexConcPatternVc** (class)
  - Path: `ComplexConcPatternVc.cs`
  - Public class implementation

- **DuplicateAnalysisFixer** (class)
  - Path: `DuplicateAnalysisFixer.cs`
  - Public class implementation

- **DuplicateWordformFixer** (class)
  - Path: `DuplicateWordformFixer.cs`
  - Public class implementation

- **EditMorphBreaksDlg** (class)
  - Path: `EditMorphBreaksDlg.cs`
  - Public class implementation

- **InfoPane** (class)
  - Path: `InfoPane.cs`
  - Public class implementation

- **InterlinComboHandler** (class)
  - Path: `SandboxBase.ComboHandlers.cs`
  - Public class implementation

- **InterlinDocChart** (class)
  - Path: `InterlinDocChart.cs`
  - Public class implementation

- **InterlinDocForAnalysisVc** (class)
  - Path: `InterlinDocForAnalysis.cs`
  - Public class implementation

- **InterlinMasterNoTitleContent** (class)
  - Path: `InterlinMasterNoTitleContent.cs`
  - Public class implementation

- **InterlinPrintVc** (class)
  - Path: `InterlinPrintView.cs`
  - Public class implementation

- **InterlinearExportDialog** (class)
  - Path: `InterlinearExportDialog.cs`
  - Public class implementation

- **InterlinearExporter** (class)
  - Path: `InterlinearExporter.cs`
  - Public class implementation

- **InterlinearExporterForElan** (class)
  - Path: `InterlinearExporter.cs`
  - Public class implementation

- **InterlinearTextsRecordClerk** (class)
  - Path: `InterlinearTextsRecordClerk.cs`
  - Public class implementation

- **LinguaLinksImportDlg** (class)
  - Path: `LinguaLinksImportDlg.cs`
  - Public class implementation

- **MatchingConcordanceItems** (class)
  - Path: `ConcordanceControl.cs`
  - Public class implementation

- **OccurrencesOfSelectedUnit** (class)
  - Path: `ConcordanceControl.cs`
  - Public class implementation

- **ParseIsCurrentFixer** (class)
  - Path: `ParseIsCurrentFixer.cs`
  - Public class implementation

- **InterlinMode** (enum)
  - Path: `InterlinLineChoices.cs`

## Entry Points
- Interlinear text editor
- Morpheme analysis and glossing
- Text import from various formats

## Test Index
Test projects: ITextDllTests. 23 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **LexText/Morphology/** - Morphological parsing for interlinear
- **LexText/ParserCore/** - Parsing engine for analysis
- **LexText/Discourse/** - Discourse analysis on interlinear texts
- **Common/SimpleRootSite/** - View hosting for interlinear display
- **views/** - Native rendering for complex interlinear layout

## References

- **Project files**: ITextDll.csproj, ITextDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ComplexConcLeafNode.cs, ConcordanceControl.cs, ConcordanceResultsExporter.cs, ConfigureInterlinDialog.Designer.cs, InterlinDocChart.cs, InterlinPrintView.cs, InterlinearExportDialog.cs, InterlinearTextsRecordClerk.cs, ParseIsCurrentFixer.cs
- **XML data/config**: HalbiST1Old.xml, InterlinearExporterTests.xml, ParagraphParserTestTexts.xml, Phase1-Jibiyal3Text.xml, toolConfigurationForITextDllTests.xml
- **Source file count**: 124 files
- **Data file count**: 62 files

## References (auto-generated hints)
- Project files:
  - LexText/Interlinear/ITextDll.csproj
  - LexText/Interlinear/ITextDllTests/ITextDllTests.csproj
- Key C# files:
  - LexText/Interlinear/AssemblyInfo.cs
  - LexText/Interlinear/BIRDInterlinearImporter.cs
  - LexText/Interlinear/ChooseAnalysisHandler.cs
  - LexText/Interlinear/ChooseTextWritingSystemDlg.Designer.cs
  - LexText/Interlinear/ChooseTextWritingSystemDlg.cs
  - LexText/Interlinear/ClosedFeatureNode.cs
  - LexText/Interlinear/ClosedFeatureValue.cs
  - LexText/Interlinear/ComplexConcControl.Designer.cs
  - LexText/Interlinear/ComplexConcControl.cs
  - LexText/Interlinear/ComplexConcGroupNode.cs
  - LexText/Interlinear/ComplexConcLeafNode.cs
  - LexText/Interlinear/ComplexConcMorphDlg.cs
  - LexText/Interlinear/ComplexConcMorphNode.cs
  - LexText/Interlinear/ComplexConcOrNode.cs
  - LexText/Interlinear/ComplexConcParagraphData.cs
  - LexText/Interlinear/ComplexConcPatternModel.cs
  - LexText/Interlinear/ComplexConcPatternNode.cs
  - LexText/Interlinear/ComplexConcPatternSda.cs
  - LexText/Interlinear/ComplexConcPatternVc.cs
  - LexText/Interlinear/ComplexConcTagDlg.cs
  - LexText/Interlinear/ComplexConcTagNode.cs
  - LexText/Interlinear/ComplexConcWordBdryNode.cs
  - LexText/Interlinear/ComplexConcWordDlg.cs
  - LexText/Interlinear/ComplexConcWordNode.cs
  - LexText/Interlinear/ComplexFeatureNode.cs
- Data contracts/transforms:
  - LexText/Interlinear/ChooseTextWritingSystemDlg.resx
  - LexText/Interlinear/ComplexConcControl.resx
  - LexText/Interlinear/ComplexConcMorphDlg.resx
  - LexText/Interlinear/ComplexConcTagDlg.resx
  - LexText/Interlinear/ComplexConcWordDlg.resx
  - LexText/Interlinear/ConcordanceControl.resx
  - LexText/Interlinear/ConfigureInterlinDialog.resx
  - LexText/Interlinear/CreateAllomorphTypeMismatchDlg.resx
  - LexText/Interlinear/EditMorphBreaksDlg.resx
  - LexText/Interlinear/FilterAllTextsDialog.resx
  - LexText/Interlinear/FilterTextsDialog.resx
  - LexText/Interlinear/FocusBoxController.resx
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTest.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTestPunctuation.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTestPunctuationWordAlignedXLingPap.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTestWordAlignedXLingPap.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-OrizabaLesson2.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-OrizabaLesson2WordAlignedXLingPap.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-SETepehuanCorn.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/SETepehuanCornSingleListExample.xml
  - LexText/Interlinear/ITextDllTests/ExportTestFiles/SETepehuanCornWordsAppendix.xml
  - LexText/Interlinear/ITextDllTests/InterlinearExporterTests.xml
  - LexText/Interlinear/ITextDllTests/ParagraphParserTestTexts.xml
  - LexText/Interlinear/ITextDllTests/XLingPaperTransformerTestsDataFiles/BruceCoxEmptyOld.xml
  - LexText/Interlinear/ITextDllTests/XLingPaperTransformerTestsDataFiles/Gilaki01Old.xml
## Code Evidence
*Analysis based on scanning 108 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 8 public interfaces
- **Namespaces**: SIL.FieldWorks.IText, SIL.FieldWorks.IText.FlexInterlinModel, has, needs, via
