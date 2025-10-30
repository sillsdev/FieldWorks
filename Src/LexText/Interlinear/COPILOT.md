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

## Build Information
- C# class library project
- Build via: `dotnet build ITextDll.csproj` (Note: project named ITextDll)
- Core component of text analysis features

## Entry Points
- Interlinear text editor
- Morpheme analysis and glossing
- Text import from various formats

## Related Folders
- **LexText/Morphology/** - Morphological parsing for interlinear
- **LexText/ParserCore/** - Parsing engine for analysis
- **LexText/Discourse/** - Discourse analysis on interlinear texts
- **Common/SimpleRootSite/** - View hosting for interlinear display
- **views/** - Native rendering for complex interlinear layout

## Code Evidence
*Analysis based on scanning 108 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 8 public interfaces
- **Namespaces**: SIL.FieldWorks.IText, SIL.FieldWorks.IText.FlexInterlinModel, has, needs, via

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

## References

- **Project files**: ITextDll.csproj, ITextDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ComplexConcLeafNode.cs, ConcordanceControl.cs, ConcordanceResultsExporter.cs, ConfigureInterlinDialog.Designer.cs, InterlinDocChart.cs, InterlinPrintView.cs, InterlinearExportDialog.cs, InterlinearTextsRecordClerk.cs, ParseIsCurrentFixer.cs
- **XML data/config**: HalbiST1Old.xml, InterlinearExporterTests.xml, ParagraphParserTestTexts.xml, Phase1-Jibiyal3Text.xml, toolConfigurationForITextDllTests.xml
- **Source file count**: 124 files
- **Data file count**: 62 files
