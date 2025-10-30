---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Morphology

## Purpose
Morphological analysis and morphology editor infrastructure.
Provides tools for defining morphological rules, allomorph conditions, phonological features,
natural classes, and morpheme environments. Enables linguistic definition of word formation
patterns and supports the morphological parser configuration.

## Key Components
### Key Classes
- **AdhocCoProhibAtomicLauncher**
- **OneAnalysisSandbox**
- **UpdateRealAnalysisMethod**
- **RuleFormulaVcBase**
- **ParserAnnotationRemover**
- **RuleFormulaSlice**
- **WordformGoDlg**
- **RegRuleFormulaSlice**
- **RespellerDlgListener**
- **RespellerTemporaryRecordClerk**

## Technology Stack
- C# .NET WinForms
- Linguistic rule system
- Morphological analysis engine integration

## Dependencies
- Depends on: Cellar (data model), LexText/ParserCore, Common (UI)
- Used by: LexText/LexTextDll, LexText/Interlinear (for parsing)

## Build Information
- C# class library project
- Build via: `dotnet build MorphologyEditorDll.csproj`
- Morphology editing and rule management

## Entry Points
- Morphology editor interface
- Rule and feature editors
- Allomorph condition management

## Related Folders
- **LexText/ParserCore/** - Parsing engine using morphology rules
- **LexText/ParserUI/** - Parser UI for morphology
- **LexText/Interlinear/** - Uses morphology for text analysis
- **LexText/Lexicon/** - Lexicon data used in morphology

## Code Evidence
*Analysis based on scanning 55 source files*

- **Classes found**: 20 public classes
- **Namespaces**: SIL.FieldWorks.LexText.Controls.MGA, SIL.FieldWorks.XWorks.MorphologyEditor

## Interfaces and Data Models

- **AdhocCoProhibAtomicLauncher** (class)
  - Path: `AdhocCoProhibAtomicLauncher.cs`
  - Public class implementation

- **InflAffixTemplateSlice** (class)
  - Path: `InflAffixTemplateSlice.cs`
  - Public class implementation

- **MEImages** (class)
  - Path: `MEImages.cs`
  - Public class implementation

- **MetaRuleFormulaControl** (class)
  - Path: `MetaRuleFormulaControl.cs`
  - Public class implementation

- **OccurrenceComparer** (class)
  - Path: `RespellerDlgListener.cs`
  - Public class implementation

- **OccurrenceSorter** (class)
  - Path: `RespellerDlgListener.cs`
  - Public class implementation

- **ParserAnnotationRemover** (class)
  - Path: `ParserAnnotationRemover.cs`
  - Public class implementation

- **PhEnvStrRepresentationSlice** (class)
  - Path: `PhEnvStrRepresentationSlice.cs`
  - Public class implementation

- **RegRuleFormulaSlice** (class)
  - Path: `RegRuleFormulaSlice.cs`
  - Public class implementation

- **RespellerDlgListener** (class)
  - Path: `RespellerDlgListener.cs`
  - Public class implementation

- **RespellerRecordList** (class)
  - Path: `RespellerDlgListener.cs`
  - Public class implementation

- **RespellerTemporaryRecordClerk** (class)
  - Path: `RespellerDlgListener.cs`
  - Public class implementation

- **RuleFormulaControl** (class)
  - Path: `RuleFormulaControl.cs`
  - Public class implementation

- **RuleFormulaSlice** (class)
  - Path: `RuleFormulaSlice.cs`
  - Public class implementation

- **RuleFormulaVcBase** (class)
  - Path: `RuleFormulaVcBase.cs`
  - Public class implementation

- **StringRepSliceVc** (class)
  - Path: `PhEnvStrRepresentationSlice.cs`
  - Public class implementation

- **UpdateRealAnalysisMethod** (class)
  - Path: `OneAnalysisSandbox.cs`
  - Public class implementation

- **UserAnalysisRemover** (class)
  - Path: `UserAnalysisRemover.cs`
  - Public class implementation

- **WordformApplicationServices** (class)
  - Path: `RespellerDlgListener.cs`
  - Public class implementation

- **WordformGoDlg** (class)
  - Path: `WordformGoDlg.cs`
  - Public class implementation

## References

- **Project files**: MGA.csproj, MGATests.csproj, MorphologyEditorDll.csproj, MorphologyEditorDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AdhocCoProhibAtomicLauncher.cs, AssemblyInfo.cs, AssignFeaturesToPhonemes.Designer.cs, MEStrings.Designer.cs, OneAnalysisSandbox.cs, ParserAnnotationRemover.cs, RegRuleFormulaSlice.cs, RuleFormulaSlice.cs, RuleFormulaVcBase.cs, WordformGoDlg.cs
- **XSLT transforms**: CreateFeatureCatalog.xsl, MasterToEticGlossList.xsl, schematron-report.xsl, skeleton1-5.xsl, verbid.xsl
- **XML data/config**: EticGlossList.xml, FeatureCatalog.xml, MGAMaster.xml, MasterGlossListValidityConstraints.xml, MasterGlossListWithValidtyFailures.xml
- **Source file count**: 61 files
- **Data file count**: 27 files
