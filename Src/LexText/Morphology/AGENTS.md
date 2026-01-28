---
last-reviewed: 2025-10-31
last-reviewed-tree: bc58db0bdef56c69ed19c8cd2613479a8dd45cfc84dfe07c67e02fe96a7fab2b
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Morphology COPILOT summary

## Purpose
Morphological analysis UI library for FieldWorks Language Explorer (FLEx). Provides specialized controls, slices, and dialogs for morphology features: inflectional affix templates (InflAffixTemplateControl, InflAffixTemplateSlice), affix rule formulas (AffixRuleFormulaControl, AffixRuleFormulaVc), phoneme/feature editing (PhonemeWithAllophonesSlice, BasicIPASymbolSlice), phonological environments (PhEnvReferenceSlice, SegmentSequenceSlice), morpheme analysis (AnalysisInterlinearRS, MorphemeContextCtrl), concordance (ConcordanceDlg), and morphology-grammar area (MGA/ subfolder with rule strata, templates). Master list listeners (MasterCatDlgListener, MasterDlgListener, MasterInflFeatDlgListener, MasterPhonFeatDlgListener) handle list editing coordination. Moderate-sized library (16.9K lines) supporting FLEx morphology/grammar features. Project name: Morphology.csproj.

## Architecture
C# library (net48, OutputType=Library) with morphology UI components. Slice/control pattern for data entry fields. View constructors (InflAffixTemplateVc, AffixRuleFormulaVc, PhoneEnvReferenceVc) for custom rendering. Master list listeners as XCore colleagues. MGA/ subfolder for morphology-grammar area components (rule strata, templates, environment choosers). Resource files for localization (MEStrings.resx) and images (ImageHolder.resx, MEImages.resx). Integrates with LCModel (IMoAffixAllomorph, IMoInflAffixTemplate, IPhEnvironment, IPhoneme), Views rendering, XCore framework.

## Key Components
- **InflAffixTemplateControl** (InflAffixTemplateControl.cs, 1.3K lines): Inflectional affix template editor
  - Visual editor for affix template slots
  - Drag-and-drop slot ordering
  - InflAffixTemplateSlice: Data entry slice
  - InflAffixTemplateMenuHandler: Context menu operations
  - InflAffixTemplateEventArgs: Event arguments
- **AffixRuleFormulaControl** (AffixRuleFormulaControl.cs, 824 lines): Affix rule formula editor
  - Edit morphological rules (affix processes)
  - AffixRuleFormulaSlice: Data entry slice
  - AffixRuleFormulaVc: View constructor for rendering
- **AnalysisInterlinearRS** (AnalysisInterlinearRS.cs, 434 lines): Analysis interlinear root site
  - Display morpheme analysis in interlinear format
  - Integrates with interlinear text view
- **ConcordanceDlg** (ConcordanceDlg.cs, 816 lines): Morpheme concordance dialog
  - Search morpheme occurrences in corpus
  - Display concordance results with context
- **PhonemeWithAllophonesSlice** (PhonemeWithAllophonesSlice*.cs, likely 300+ lines): Phoneme editing
  - Edit phonemes with allophone representations
- **BasicIPASymbolSlice** (BasicIPASymbolSlice.cs, 170 lines): IPA symbol slice
  - Edit International Phonetic Alphabet symbols
- **PhEnvReferenceSlice** (PhEnvReferenceSlice*.cs, likely 200+ lines): Phonological environment reference
  - Edit phonological environment references
- **SegmentSequenceSlice** (SegmentSequenceSlice*.cs, likely 150+ lines): Segment sequence slice
  - Edit phonological segment sequences
- **MorphemeContextCtrl** (MorphemeContextCtrl*.cs, likely 400+ lines): Morpheme context control
  - Display/edit morpheme grammatical context
- **Master list listeners** (600 lines combined):
  - MasterCatDlgListener (94 lines): Category list coordination
  - MasterDlgListener (172 lines): Generic master list coordination
  - MasterInflFeatDlgListener (107 lines): Inflectional feature list coordination
  - MasterPhonFeatDlgListener (129 lines): Phonological feature list coordination
  - XCore colleagues for list editing coordination
- **AdhocCoProhib slices** (200 lines combined): Ad-hoc co-occurrence constraints
  - AdhocCoProhibAtomicLauncher, AdhocCoProhibAtomicReferenceSlice
  - AdhocCoProhibVectorLauncher, AdhocCoProhibVectorReferenceSlice
  - Edit morpheme co-occurrence restrictions
- **InterlinearSlice** (InterlinearSlice.cs, 88 lines): Interlinear slice base
  - Base class for interlinear-style slices
- **AssignFeaturesToPhonemes** (AssignFeaturesToPhonemes.cs, 73 lines): Feature assignment utility
  - Assign phonological features to phonemes
- **MGA/ subfolder**: Morphology-Grammar Area components
  - Rule strata, templates, environment choosers (separate AGENTS.md)
- **MEStrings** (MEStrings.Designer.cs, MEStrings.resx, 1.2K lines): Localized strings
  - Designer-generated resource accessor
  - Localized UI strings for morphology/grammar
- **ImageHolder, MEImages** (ImageHolder.cs, MEImages.cs, 250 lines): Icon resources
  - Embedded icons/images for morphology UI

## Technology Stack
C# .NET Framework 4.8.x, Windows Forms, LCModel, Views (rendering), XCore.

## Dependencies
Consumes: LCModel (IMoAffixAllomorph, IMoInflAffixTemplate, IPhEnvironment, IPhoneme), Views, XCore, LexTextControls, Interlinear. Used by: xWorks (Grammar area), FieldWorks.exe.

## Interop & Contracts
IMoAffixAllomorph, IMoInflAffixTemplate, IPhEnvironment, IPhoneme, IPhFeatureConstraint, IxCoreColleague (master list listeners).

## Threading & Performance
UI thread. Concordance may be slow on large corpora.

## Config & Feature Flags
Configuration via LCModel morphology settings.

## Build Information
Morphology.csproj (net48), output: SIL.FieldWorks.XWorks.Morphology.dll. Tests: `dotnet test MorphologyTests/`.

## Interfaces and Data Models

- **InflAffixTemplateControl** (InflAffixTemplateControl.cs)
  - Purpose: Visual editor for inflectional affix templates
  - Inputs: IMoInflAffixTemplate
  - Outputs: Modified template with slot ordering
  - Notes: 1.3K lines, drag-and-drop interface

- **AffixRuleFormulaControl** (AffixRuleFormulaControl.cs)
  - Purpose: Edit affix rule formulas
  - Inputs: Affix rule data
  - Outputs: Modified rule formula
  - Notes: 824 lines, AffixRuleFormulaVc for rendering

- **ConcordanceDlg** (ConcordanceDlg.cs)
  - Purpose: Search morpheme occurrences in corpus
  - Inputs: Morpheme search criteria
  - Outputs: Concordance results with context
  - Notes: 816 lines

- **AnalysisInterlinearRS** (AnalysisInterlinearRS.cs)
  - Purpose: Display morpheme analysis in interlinear format
  - Notes: 434 lines, integrates with interlinear views

- **Master list listeners**:
  - Purpose: Coordinate list editing operations
  - Interface: IxCoreColleague
  - Notes: MasterCatDlgListener (categories), MasterInflFeatDlgListener (inflectional features), MasterPhonFeatDlgListener (phonological features)

## Entry Points
Loaded by xWorks. Slices/controls instantiated by data entry framework for Grammar area.

## Test Index
MorphologyTests project. Run: `dotnet test MorphologyTests/`.

## Usage Hints
Grammar → Inflectional Affix Templates (InflAffixTemplateControl), rule formulas (AffixRuleFormulaControl), phoneme editing, concordance (ConcordanceDlg). MGA/ subfolder contains additional components.

## Related Folders
MGA (Morphology-Grammar Area, see MGA/AGENTS.md), LexTextControls, Interlinear, xWorks.

## References
Morphology.csproj (net48), 16.9K lines. Key files: InflAffixTemplateControl.cs (1.3K), MEStrings.Designer.cs (1.2K), ConcordanceDlg.cs (816). See `.cache/copilot/diff-plan.json` for file inventory.

