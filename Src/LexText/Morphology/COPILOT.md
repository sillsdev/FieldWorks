---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Morphology

## Purpose
Morphological analysis and morphology editor. Provides tools for defining morphological rules, allomorph conditions, and phonological features.

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
