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

## Architecture
C# library with 61 source files. Contains 2 subprojects: MorphologyEditorDll, MGA.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build MorphologyEditorDll.csproj`
- Morphology editing and rule management

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

## Entry Points
- Morphology editor interface
- Rule and feature editors
- Allomorph condition management

## Test Index
Test projects: MGATests, MorphologyEditorDllTests. 2 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **LexText/ParserCore/** - Parsing engine using morphology rules
- **LexText/ParserUI/** - Parser UI for morphology
- **LexText/Interlinear/** - Uses morphology for text analysis
- **LexText/Lexicon/** - Lexicon data used in morphology

## References

- **Project files**: MGA.csproj, MGATests.csproj, MorphologyEditorDll.csproj, MorphologyEditorDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AdhocCoProhibAtomicLauncher.cs, AssemblyInfo.cs, AssignFeaturesToPhonemes.Designer.cs, MEStrings.Designer.cs, OneAnalysisSandbox.cs, ParserAnnotationRemover.cs, RegRuleFormulaSlice.cs, RuleFormulaSlice.cs, RuleFormulaVcBase.cs, WordformGoDlg.cs
- **XSLT transforms**: CreateFeatureCatalog.xsl, MasterToEticGlossList.xsl, schematron-report.xsl, skeleton1-5.xsl, verbid.xsl
- **XML data/config**: EticGlossList.xml, FeatureCatalog.xml, MGAMaster.xml, MasterGlossListValidityConstraints.xml, MasterGlossListWithValidtyFailures.xml
- **Source file count**: 61 files
- **Data file count**: 27 files

## References (auto-generated hints)
- Project files:
  - LexText/Morphology/MGA/MGA.csproj
  - LexText/Morphology/MGA/MGATests/MGATests.csproj
  - LexText/Morphology/MorphologyEditorDll.csproj
  - LexText/Morphology/MorphologyEditorDllTests/MorphologyEditorDllTests.csproj
- Key C# files:
  - LexText/Morphology/AdhocCoProhibAtomicLauncher.cs
  - LexText/Morphology/AdhocCoProhibAtomicReferenceSlice.cs
  - LexText/Morphology/AdhocCoProhibVectorLauncher.cs
  - LexText/Morphology/AdhocCoProhibVectorReferenceSlice.cs
  - LexText/Morphology/AffixRuleFormulaControl.cs
  - LexText/Morphology/AffixRuleFormulaSlice.cs
  - LexText/Morphology/AffixRuleFormulaVc.cs
  - LexText/Morphology/AnalysisInterlinearRS.cs
  - LexText/Morphology/AssemblyInfo.cs
  - LexText/Morphology/AssignFeaturesToPhonemes.Designer.cs
  - LexText/Morphology/AssignFeaturesToPhonemes.cs
  - LexText/Morphology/BasicIPASymbolSlice.cs
  - LexText/Morphology/ConcordanceDlg.cs
  - LexText/Morphology/ImageHolder.cs
  - LexText/Morphology/InflAffixTemplateControl.cs
  - LexText/Morphology/InflAffixTemplateEventArgs.cs
  - LexText/Morphology/InflAffixTemplateMenuHandler.cs
  - LexText/Morphology/InflAffixTemplateSlice.cs
  - LexText/Morphology/InterlinearSlice.cs
  - LexText/Morphology/MEImages.cs
  - LexText/Morphology/MEStrings.Designer.cs
  - LexText/Morphology/MGA/AssemblyInfo.cs
  - LexText/Morphology/MGA/GlossListBox.cs
  - LexText/Morphology/MGA/GlossListBoxItem.cs
  - LexText/Morphology/MGA/GlossListEventArgs.cs
- Data contracts/transforms:
  - LexText/Morphology/AdhocCoProhibAtomicLauncher.resx
  - LexText/Morphology/AdhocCoProhibVectorLauncher.resx
  - LexText/Morphology/AnalysisInterlinearRS.resx
  - LexText/Morphology/ConcordanceDlg.resx
  - LexText/Morphology/ImageHolder.resx
  - LexText/Morphology/MEImages.resx
  - LexText/Morphology/MEStrings.resx
  - LexText/Morphology/MGA/GlossListBox.resx
  - LexText/Morphology/MGA/GlossLists/CreateFeatureCatalog.xsl
  - LexText/Morphology/MGA/GlossLists/EticGlossList.xml
  - LexText/Morphology/MGA/GlossLists/FeatureCatalog.dtd
  - LexText/Morphology/MGA/GlossLists/FeatureCatalog.xml
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/MGAMaster.xml
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/MasterGlossList.xml
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/MasterGlossListValidityConstraints.xml
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/MasterGlossListWithValidtyFailures.xml
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/MasterToEticGlossList.xsl
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/masterGlossList.dtd
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/schematron-report.xsl
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/schematron1-5.xsd
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/skeleton1-5.xsl
  - LexText/Morphology/MGA/GlossLists/OriginalWayToProduceEticGlossList/verbid.xsl
  - LexText/Morphology/MGA/GlossLists/eticGlossList.dtd
  - LexText/Morphology/MGA/MGADialog.resx
  - LexText/Morphology/MGA/MGAStrings.resx
## Code Evidence
*Analysis based on scanning 55 source files*

- **Classes found**: 20 public classes
- **Namespaces**: SIL.FieldWorks.LexText.Controls.MGA, SIL.FieldWorks.XWorks.MorphologyEditor
