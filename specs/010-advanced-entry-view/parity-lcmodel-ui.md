# LCModel ↔ UI Parity Checklist (Legacy Entry/Edit vs. Avalonia)

**Feature**: Advanced New Entry Avalonia View
**Spec**: `specs/010-advanced-entry-view/spec.md`
**Plan**: `specs/010-advanced-entry-view/plan.md`

## Purpose
This document is a working parity checklist to ensure the Avalonia-based editor can represent (and eventually edit) the *same conceptual LCModel surface area* as the existing LexText Entry/Edit experience, including nested structures.

It also evaluates three implementation paths against that checklist under these constraints:
- We’re starting small, but the long-term direction is: **move all UI to Avalonia**.
- We will **sunset all C++ UI/view code** over time; new work should not depend on legacy C++ view infrastructure.
- Migration will happen in **stages**, not all at once.
- We want an **easily maintainable** solution using **modern packages**.
- We need the ability to **customize any view**, regardless of the model.
- Views should **follow the model ≥ 90%** by default (customization should be the exception, not the rule).

## Non-negotiable constraint: retire legacy C++ view code
FieldWorks has legacy view infrastructure that includes custom C++ code (and C++-adjacent view runtime concerns). The direction here is:

- **Keep** the *logic and structure* of how LCModel is interpreted (field grouping, nested traversal, “ghost” behaviors, computed displays like headword rendering, chooser semantics).
- **Replace** the UI layer entirely with Avalonia.
- **Do not** build new functionality that requires the legacy C++ view runtime to function.

Practically, this means we expect to introduce one or more **new managed abstraction layers** (C#) that preserve those interpretation rules and feed an Avalonia renderer/editor layer.

## What’s happening today (why only “4 boxes”)
The current Avalonia preview uses a minimal detached DTO (`EntryModel`) with four string properties (Lexical Form, Citation Form, Morph Type, Part of Speech). The PropertyGrid is bound directly to that DTO, so it can only render those four fields. No nested collections (senses/examples/pronunciations/etc.) exist yet, and no LCModel-driven shaping is implemented.

## Sources of truth for “legacy UI surface area”
The legacy Entry/Edit UI is a mix of:
1) **Config-driven field layout** via XML part inventories (highly relevant for parity and customization):
   - `DistFiles/Language Explorer/Configuration/Parts/LexEntryParts.xml`
   - Related parts files for child types (e.g., LexSense parts, example parts, etc.)
2) **Code-driven rendering** for certain computed displays and special semantics:
   - `Src/FdoUi/LexEntryUi.cs` (`LexEntryVc`, headword/morph type/homograph rendering, variant type formatting)
   - Various LexTextControls and XWorks pieces for specialized pickers and dialogs

Important nuance for migration planning:
- Some legacy behaviors may currently be implemented “in the view stack” (including custom C++ code). For parity, we treat these as **behavioral requirements**, not as implementation dependencies. The Avalonia path must re-host those behaviors in **managed interpretation/services**.

This checklist leans on (1) for breadth and customization requirements, and on (2) for known non-trivial behaviors.

---

## Enhanced parity checklist
Legend:
- **P0**: must exist even in early stages (core creation flow)
- **P1**: needed for “credible Entry/Edit parity”
- **P2**: needed for “complete LCModel coverage” (including custom fields)

For each item we track:
- **Legacy surface**: where it appears today (typically XML part id; sometimes code).
- **LCModel shape**: object/field path and nesting.
- **Notes**: special editors/behaviors.

### A. Identity & headword (P0/P1)
- [ ] A1 Lexeme Form (multi-writing-system) (P0)
  - Legacy surface: `LexEntry-Detail-LexemeForm` in `LexEntryParts.xml`
  - LCModel shape: `LexEntry.LexemeForm (MoForm) → MoForm.Form (MultiString)`
  - Notes: includes morph-type prefix/postfix and homograph number behaviors in legacy headword display.
- [ ] A2 Citation Form (multi-writing-system) (P0)
  - Legacy surface: `LexEntry-Detail-CitationFormAllV`
  - LCModel shape: `LexEntry.CitationForm (MultiString)`
- [ ] A3 Homograph number (P1)
  - Legacy surface: computed headword behavior in `LexEntryVc` (`Src/FdoUi/LexEntryUi.cs`)
  - LCModel shape: `LexEntry.HomographNumber (int)`
- [ ] A4 Entry type / minor entry type (P1)
  - Legacy surface: `LexEntry-Jt-EntryTypeofEntry`
  - LCModel shape: `LexEntry.EntryType (LexEntryType)`

### B. Morphology & grammatical info (P0/P1)
- [ ] B1 Morph type (P0)
  - Legacy surface: `LexEntry-Jt-MorphTypeofEntry`, headword affix rendering in `LexEntryVc`
  - LCModel shape: `LexEntry.LexemeForm (MoForm) → MoForm.MorphType (MoMorphType)`
- [ ] B2 Part of speech / grammatical category (P0/P1)
  - Legacy surface: typically surfaced via Sense-level grammatical info (see Sense checklist), and various layouts
  - LCModel shape: usually Sense-level `LexSense.MorphoSyntaxAnalysis` / POS-related properties
  - Notes: needs FW-specific chooser semantics.
- [ ] B3 “Grammatical Functions” section grouping (P1)
  - Legacy surface: `LexEntry-Detail-GrammaticalFunctionsSection`
  - LCModel shape: grouping concept rather than a single field
  - Notes: tests that the Avalonia view can express sections/groups independent of the model.

### C. Pronunciations (P1)
- [ ] C1 Pronunciations collection (P1)
  - Legacy surface: `LexEntry-Detail-Pronunciations`
  - LCModel shape: `LexEntry.Pronunciations (sequence)`
  - Notes: nested item editor; publication-specific settings exist (see publication section).

### D. Alternate forms & allomorphs (P1/P2)
- [ ] D1 Alternate forms collection (P1)
  - Legacy surface: `LexEntry-Detail-AlternateForms`
  - LCModel shape: `LexEntry.AlternateForms (sequence)`
- [ ] D2 Allomorph custom fields (P2)
  - Legacy surface: `LexEntry-Jt-Custom*ForAllomorph_$fieldName`
  - LCModel shape: `LexEntry.AllAllomorphs → <custom fields of many types>`
  - Notes: implies the UI must handle *custom fields* and multiple field types (int, GenDate, StText, poss atom/vector, multi-string).

### E. Senses (nested) (P0/P1)
- [ ] E1 Senses collection (P0)
  - Legacy surface: `LexEntry-Jt-$fieldName` and many sense layouts
  - LCModel shape: `LexEntry.Senses (sequence)`
  - Notes: nested editing is mandatory (sense editor in the UI, not just display).
- [ ] E2 Sense gloss(es) (P0)
  - Legacy surface: `LexEntry-Jt-SenseList` and `GlossesForSense`-type layouts
  - LCModel shape: `LexSense.Gloss (MultiString)`
- [ ] E3 Sense definition(s) (P1)
  - Legacy surface: `DefinitionsForSense`, `DefinitionInPara`
  - LCModel shape: `LexSense.Definition (MultiString/StText depending on model usage)`
- [ ] E4 Sense grammatical info (P0/P1)
  - Legacy surface: `GrammaticalCategoryForSense`, `GrammaticalInfo*ForSense`
  - LCModel shape: sense-level POS/MSA-related fields
- [ ] E5 Sense semantic domains / anthropology codes (P1)
  - Legacy surface: `LexEntry-Jt-DomainsOfSenses`, `LexEntry-Jt-AnthroCodesOfSenses`
  - LCModel shape: `LexSense.SemanticDomains (vector)`, `LexSense.AnthroCodes (vector)`
- [ ] E6 Sense notes (P1/P2)
  - Legacy surface: `AnthropologyNoteForSense`, `DiscourseNoteForSense`, `EncyclopedicInfoForSense`, etc.
  - LCModel shape: sense-level note fields

### F. Examples (nested under senses) (P1)
- [ ] F1 Example sentence(s) (P1)
  - Legacy surface: `LexEntry-Jt-Example`
  - LCModel shape: `LexEntry.AllSenses → LexSense.Examples (sequence)`
- [ ] F2 Example translations (P1)
  - Legacy surface: `LexEntry-Jt-ExampleTranslation`
  - LCModel shape: example translation fields
- [ ] F3 Example references and metadata (P2)
  - Legacy surface: `LexEntry-Jt-ExampleReference`, `ExampleTranslationType`
  - LCModel shape: nested metadata objects/refs
- [ ] F4 Example custom fields (P2)
  - Legacy surface: `LexEntry-Jt-Custom*ForExample_$fieldName`
  - LCModel shape: `LexSense.Examples → <custom fields of many types>`

### G. Entry references: variants, complex forms, components (P1)
- [ ] G1 Variant-of relationships (P1)
  - Legacy surface: `LexEntry-Detail-EntryRefsGhostVariantOf`, plus variant type formatting in `LexEntryVc`
  - LCModel shape: `LexEntry.EntryRefs (sequence)`, `LexEntryRef.VariantEntryTypesRS` etc.
  - Notes: requires complex chooser and display logic.
- [ ] G2 Complex form components (P1)
  - Legacy surface: `LexEntry-Detail-EntryRefsGhostComponents`
  - LCModel shape: `LexEntry.EntryRefs / ComplexFormEntryRefs` linking entries/senses
- [ ] G3 General EntryRefs list (P2)
  - Legacy surface: `LexEntry-Detail-EntryRefs`
  - LCModel shape: `LexEntry.EntryRefs (sequence)`

### H. Publishing, restrictions, and metadata (P1/P2)
- [ ] H1 Publish-in (entry level) (P1)
  - Legacy surface: `LexEntry-Detail-PublishIn`
  - LCModel shape: `LexEntry.PublishIn (vector ref)` with visibility field `DoNotPublishIn`
- [ ] H2 Publish-in (pronunciation level) (P2)
  - Legacy surface: `LexEntry-Detail-PublishInForPronunciations`
  - LCModel shape: pronunciation child publication constraints
- [ ] H3 Restrictions (P2)
  - Legacy surface: `LexEntry-Detail-RestrictionsAllA`
  - LCModel shape: `LexEntry.Restrictions (MultiString)`
- [ ] H4 Bibliography (entry + sense) (P2)
  - Legacy surface: `LexEntry-Detail-BibliographyAllA`, `BibliographyForSense`
  - LCModel shape: bibliography fields

### I. Etymology (P2)
- [ ] I1 Etymologies collection (P2)
  - Legacy surface: `LexEntry-Detail-Etymologies`
  - LCModel shape: `LexEntry.Etymology (sequence)`

### J. Custom fields (model-following ≥ 90%) (P2)
- [ ] J1 Entry custom fields (all supported data types) (P2)
  - Legacy surface: pattern exists widely in parts system; not enumerated here
  - LCModel shape: `LexEntry.<Custom*>`
- [ ] J2 Sense custom fields (P2)
  - Legacy surface: `LexEntry-Jt-Custom*ForSense_$fieldName`
  - LCModel shape: `LexSense.<Custom*>`
- [ ] J3 Example custom fields (P2)
  - Legacy surface: `LexEntry-Jt-Custom*ForExample_$fieldName`
  - LCModel shape: `LexExampleSentence.<Custom*>`

### K. Operations & UX behaviors (cross-cutting) (P1)
- [ ] K1 “Ghost” items / add-first-item behavior (P1)
  - Legacy surface: many `<seq ... ghost=... ghostLabel=... ghostInitMethod=...>` patterns
  - Notes: critical for staged editing UX (add first item without forcing navigation).
- [ ] K2 Choosers for possibility lists, references, and hierarchical pickers (P1)
  - Legacy surface: various `PopupTreeManager` family and chooser definitions
  - Notes: requires custom editors in Avalonia.
- [ ] K3 Multi-writing-system correctness (P0/P1)
  - Notes: multi-WS editing appears throughout; needs WS selection per field.

---

## Mapping the checklist to staged delivery
A practical staged plan that matches our “start small” requirement:
- **Stage 0 (done)**: A1/A2/B1 (stubbed as plain strings) and a preview host.
- **Stage 1 (next)**: A1/A2/B1/B2 + E1/E2 + basic nested editor for senses.
- **Stage 2**: C1 + F1/F2 + G1/G2 + core choosers.
- **Stage 3**: custom fields (J*) + publish/restrictions + broader parity.

---

## Comparing the 3 implementation paths against the checklist
The three paths (from earlier discussion) are:

1) **DTO-first (staged DTO graph + custom editors)**
2) **Metadata-driven (generate UI from LCModel schema/metadata)**
3) **Reuse legacy layout definitions as the view contract** (e.g., XML Parts system as source-of-truth)

### Summary matrix (fitness vs constraints)
Ratings are qualitative: High / Medium / Low.

| Constraint / Goal | (1) DTO-first | (2) Metadata-driven | (3) Reuse view definitions |
|---|---:|---:|---:|
| Staged migration | High | Medium | High |
| Maintainability (long-term) | Medium | Medium-High | High |
| Modern packages (Avalonia/MVVM) | High | High | High |
| Sunset all C++ UI/view code | High | High | High |
| Customizable views regardless of model | Medium | High | High |
| “Follow the model ≥ 90%” | Medium | High | Medium-High |
| Parity with legacy Entry/Edit grouping & UX | Medium | Medium | High |
| Handles nested LCModel + custom fields | Medium | High (if done fully) | High |
| Testability without LCModel runtime | High | Low-Medium | Medium |

### How each path addresses the enhanced checklist

#### Path 1 — DTO-first (recommended for early stages)
**How it would satisfy parity**
- Implement the checklist by growing the DTO graph in the same shape as the Entry subtree (Entry → Senses → Examples, etc.).
- Add Avalonia editors for FW-specific types (WS strings, possibility pickers, references).

**Why it aligns with “sunset C++”**
- It can be implemented entirely in managed code and Avalonia without leaning on the legacy view runtime.

**Where it struggles**
- Customization “for any view” becomes a second system unless we *also* introduce a view-definition layer.
- Risk: a large hand-maintained DTO/schema layer that diverges from LCModel and from legacy view definitions.

**Checklist fit**
- Strong for Stage 1/2 parity (A/B/E/F/G) with good tests.
- Weak unless augmented for J* (custom fields) and broad LCModel breadth.

#### Path 2 — Metadata-driven (schema-first)
**How it would satisfy parity**
- Automatically covers most of J* (custom fields) and “follow the model” by default.
- Nested objects/collections become first-class by reflection/metadata traversal.

**Why it aligns with “sunset C++”**
- Like DTO-first, it can be fully managed and Avalonia-based. The main cost is quality/UX parity, not C++ dependency.

**Where it struggles**
- Parity with legacy UX (ghost items, special choosers, computed headword formatting) requires many overrides.
- Hard to get “LexText-quality” editors purely from type metadata.

**Checklist fit**
- Excellent for broad LCModel coverage and the “90% model-following” requirement.
- Requires a strong customization/override system to reach UX parity.

#### Path 3 — Reuse legacy view definitions (parts/layout as contract)
**How it would satisfy parity**
- Treat the existing configuration system (e.g., parts/layout definitions) as the primary view contract.
- Build an Avalonia renderer/editor framework that can interpret those definitions, but back it with staged DTOs for edit/transaction requirements.

**Critical clarification for “sunset C++”**
- This path is only acceptable if we build a **managed (C#) interpreter** for the parts/layout contract and a managed set of editor semantics.
- We must not “wrap” or “embed” the legacy C++ view runtime as the long-term solution.

**Where it struggles**
- You must define/maintain a mapping layer from the config DSL (parts/layout concepts) to Avalonia controls and editors.
- Some legacy definitions are display-oriented; editing semantics may need explicit enhancement.

**Checklist fit**
- Best for parity and customization. The config already encodes nested structure, grouping, ghost behaviors, and many custom-field patterns.
- Medium-High for “follow the model ≥ 90%”: legacy definitions are close to the model, but not purely model-derived.

---

## Recommendation (aligned with the current decision)
This feature has chosen **Path 3** as the implementation strategy (see `specs/010-advanced-entry-view/spec.md` and `specs/010-advanced-entry-view/plan.md`).

Implementation guidance for parity work:
- Treat Parts/Layout as the contract and compile it into a stable **Presentation IR**, as researched in `specs/010-advanced-entry-view/presentation-ir-research.md`.
- Enforce performance constraints from `specs/010-advanced-entry-view/plan.md`:
  - cache key + explicit invalidation
  - async compilation boundary (no heavy layout/custom-field work on the UI thread)
  - virtualization boundary for sequences
- Use DTO/staged-state techniques only as an *internal* editing representation (detached staging), not as a separate “DTO-first view-definition system”.

This keeps the parity checklist grounded in the same contract that drives the shipped UI while still satisfying the non-negotiable “sunset C++ view runtime” constraint.
