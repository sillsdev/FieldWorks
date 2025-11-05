---
last-reviewed: 2025-10-31
last-reviewed-tree: 36dbed2fa5cc3fe62df4442a9cf6dcf87af17afc85572ad00a4df20d41937349
status: draft
---

# Morphology COPILOT summary

## Purpose
Morphological analysis UI library for FieldWorks Language Explorer (FLEx). Provides specialized controls, slices, and dialogs for morphology features: inflectional affix templates (InflAffixTemplateControl, InflAffixTemplateSlice), affix rule formulas (AffixRuleFormulaControl, AffixRuleFormulaVc), phoneme/feature editing (PhonemeWithAllophonesSlice, BasicIPASymbolSlice), phonological environments (PhEnvReferenceSlice, SegmentSequenceSlice), morpheme analysis (AnalysisInterlinearRS, MorphemeContextCtrl), concordance (ConcordanceDlg), and morphology-grammar area (MGA/ subfolder with rule strata, templates). Master list listeners (MasterCatDlgListener, MasterDlgListener, MasterInflFeatDlgListener, MasterPhonFeatDlgListener) handle list editing coordination. Moderate-sized library (16.9K lines) supporting FLEx morphology/grammar features. Project name: Morphology.csproj.

## Architecture
C# library (net462, OutputType=Library) with morphology UI components. Slice/control pattern for data entry fields. View constructors (InflAffixTemplateVc, AffixRuleFormulaVc, PhoneEnvReferenceVc) for custom rendering. Master list listeners as XCore colleagues. MGA/ subfolder for morphology-grammar area components (rule strata, templates, environment choosers). Resource files for localization (MEStrings.resx) and images (ImageHolder.resx, MEImages.resx). Integrates with LCModel (IMoAffixAllomorph, IMoInflAffixTemplate, IPhEnvironment, IPhoneme), Views rendering, XCore framework.

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
  - Rule strata, templates, environment choosers (separate COPILOT.md)
- **MEStrings** (MEStrings.Designer.cs, MEStrings.resx, 1.2K lines): Localized strings
  - Designer-generated resource accessor
  - Localized UI strings for morphology/grammar
- **ImageHolder, MEImages** (ImageHolder.cs, MEImages.cs, 250 lines): Icon resources
  - Embedded icons/images for morphology UI

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Windows Forms (slices, controls, dialogs)
- LCModel (data model)
- Views (rendering)
- XCore (framework)

## Dependencies

### Upstream (consumes)
- **LCModel**: Data model (IMoAffixAllomorph, IMoInflAffixTemplate, IPhEnvironment, IPhoneme, IPhFeatureConstraint)
- **Views**: Rendering engine (view constructors)
- **XCore**: Application framework (Mediator, IxCoreColleague)
- **LexTextControls/**: Shared lexicon controls
- **Common/FwUtils**: Utilities
- **Interlinear/**: Interlinear text support

### Downstream (consumed by)
- **xWorks**: Main application shell (Grammar area, morphology tools)
- **LexTextExe/**: FLEx application

## Interop & Contracts
- **IMoAffixAllomorph**: Affix allomorph object
- **IMoInflAffixTemplate**: Inflectional affix template
- **IPhEnvironment**: Phonological environment
- **IPhoneme**: Phoneme object
- **IPhFeatureConstraint**: Phonological feature constraint
- **IxCoreColleague**: XCore colleague pattern (master list listeners)

## Threading & Performance
- **UI thread**: All operations on UI thread
- **Concordance**: May be slow on large corpora

## Config & Feature Flags
No specific feature flags. Configuration via LCModel morphology settings.

## Build Information
- **Project file**: Morphology.csproj (net462, OutputType=Library)
- **Test project**: MorphologyTests/
- **Output**: SIL.FieldWorks.XWorks.Morphology.dll
- **Build**: Via top-level FieldWorks.sln or: `msbuild Morphology.csproj`
- **Run tests**: `dotnet test MorphologyTests/`

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
Loaded by xWorks main application shell. Slices/controls instantiated by data entry framework for Grammar area.

## Test Index
- **Test project**: MorphologyTests/
- **Run tests**: `dotnet test MorphologyTests/`
- **Coverage**: Affix templates, rule formulas, phoneme editing

## Usage Hints
- **Affix templates**: Grammar → Inflectional Affix Templates (InflAffixTemplateControl)
- **Rule formulas**: Edit affix processes (AffixRuleFormulaControl)
- **Phonemes**: Edit phoneme inventory with IPA symbols
- **Environments**: Define phonological environments
- **Concordance**: Search morpheme occurrences (ConcordanceDlg)
- **MGA subfolder**: Additional morphology-grammar area components
- **Master lists**: Category, feature list editing coordinated by listeners

## Related Folders
- **MGA/**: Morphology-Grammar Area components (COPILOT.md)
- **LexTextControls/**: Shared lexicon controls
- **Interlinear/**: Interlinear text integration
- **xWorks/**: Main application shell

## References
- **Project file**: Morphology.csproj (net462, OutputType=Library)
- **Key C# files**: InflAffixTemplateControl.cs (1.3K), MEStrings.Designer.cs (1.2K), ConcordanceDlg.cs (816), AffixRuleFormulaControl.cs (824), AffixRuleFormulaVc.cs (566), InflAffixTemplateMenuHandler.cs (460), AnalysisInterlinearRS.cs (434), and 55+ more files
- **MGA/ subfolder**: Additional components (see MGA/COPILOT.md)
- **Resources**: MEStrings.resx (19.2KB), ImageHolder.resx (20.2KB), MEImages.resx (14.4KB)
- **Test project**: MorphologyTests/
- **Total lines of code**: 16917
- **Output**: SIL.FieldWorks.XWorks.Morphology.dll
- **Namespace**: Various (SIL.FieldWorks.XWorks.Morphology, SIL.FieldWorks.XWorks.MGA, etc.)