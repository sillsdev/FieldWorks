---
name: fieldworks-winforms-to-avalonia-migration
description: "End-to-end playbook for migrating any FieldWorks WinForms surface (DataTree slices, XMLViews browse/table, dialogs, choosers, launchers, shell panes) to Avalonia using the established region/seam architecture. Use whenever planning, implementing, or reviewing WinForms-to-Avalonia work — including seam extraction, region composition, owned controls, plugin editors, parity evidence, or retiring legacy surfaces — even if the request only says port, modernize, replace WinForms, or new Avalonia view. Also use after finishing a migration to run the retrospective step that folds new lessons back into these skills."
---

# FieldWorks WinForms To Avalonia Migration

This is the hub skill for the migration program. It tells you what
architecture already exists (do not reinvent it), what order to work in,
which companion skill to apply at each step, and how to keep this skill
set current as more surfaces are migrated.

## Core Rule

Migrate by proving behavior first, extracting seams second, and introducing
Avalonia controls only after legacy behavior has executable parity evidence.
A region is not "migrated" until it passes the symbol audit, parity gates,
and has zero runtime dependency on native Views/DataTree infrastructure —
otherwise you have only wrapped the old system.

## Established Architecture — Reuse, Don't Reinvent

Past migrations already decided the paradigms below. Before writing any new
abstraction, read `references/architecture-patterns.md` (table of contents at
top) for the decision, the why, and the gotchas. Quick map:

| Pattern | Canonical code | Details |
| --- | --- | --- |
| Typed view-definition IR compiled from XML layouts | `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionModel.cs`, `XmlLayoutImporter.cs`, `ViewDefinitionCompiler.cs` | architecture-patterns.md §1 |
| Region model + composer (boundary sits *above* DataTree) | `Src/xWorks/FullEntryRegionComposer.cs`, `Src/Common/FwAvalonia/Region/LexicalEditRegionModel.cs`, `LexicalEditRegionMapper.cs` | §2 |
| Explicit surface selection per host (`HostUiBehavior`) | `Src/Common/FwAvalonia/LexicalEditSurfaceSelectionService.cs` | §3 |
| Owned dense controls, not stock property grids | `Src/Common/FwAvalonia/Region/FwFieldControls.cs`, `FwOptionPicker.cs`, `RegionMenuFlyout.cs` | §4 |
| Plugin registry for custom/legacy slice classes | `Src/xWorks/RegionEditorPlugins.cs`, `Src/xWorks/ChorusNotesPlugin.cs` | §5 |
| Seam contracts (edit session, undo, validation, scheduler, lifetime, refresh) | `Src/Common/FwAvalonia/Seams/ISeams.cs` | `references/seam-catalog.md` |
| Writing-system-aware text fields (font, RTL, keyboard per WS) | `Src/Common/FwAvalonia/Region/FwFieldControls.cs` (`FwMultiWsTextField`) | architecture-patterns.md §6 |
| Dialog ownership across the WinForms/Avalonia boundary | `openspec/changes/lexical-edit-avalonia-migration/dialog-ownership.md` | §7 |
| Headless integration-test harness (scenario/workflow drivers + real-clerk layer) | `Src/Common/FwAvalonia/FwAvaloniaTests/Workflows/HeadlessWorkflowHarness.cs`, `Src/xWorks/xWorksTests/ClerkRoutedFilterTests.cs` | architecture-patterns.md §13 |

## Workflow

Work through the phases in order. Copy
`references/migration-checklist.md` into your task notes and check items
off — it is the per-region definition of done.

1. **Inventory and scope.** Identify the legacy surface, its entry points,
   layouts/parts, custom slice classes, dialogs, and command wiring.
   Produce a coverage map (surface × behavior × test status). Apply
   `fieldworks-migration-scope-review` when sizing the PR/branch.
2. **Characterize before refactor.** Lock current behavior in executable
   tests (semantic baselines, timing baselines, UIA smoke) *before*
   extracting anything. Gates: every behavior is tested, consciously
   deferred with an owner, or blocked by a named seam. Examples:
   `Src/xWorks/xWorksTests/WinFormsUiaSmokeTests.cs`,
   `Src/Common/Controls/DetailControls/DetailControlsTests/`.
3. **Extract seams.** Reuse the existing contracts in
   `Src/Common/FwAvalonia/Seams/ISeams.cs`; only add a new seam when
   `references/seam-catalog.md` has no fit, and record why there.
4. **Select controls.** Default to the owned-control decisions in
   architecture-patterns.md §4. Re-evaluate only when a pivot trigger in
   seam-catalog.md §"Pivot triggers" has fired.
5. **Compose the region.** Walk the compiled IR in a composer, project into
   a region model, route custom classes through the plugin registry, and
   render unclaimed classes as explicit "unsupported" rows — never silent
   fallback. Apply `fieldworks-avalonia-ui` for the control work.
6. **Wire the host.** Explicit per-host contract: supported Avalonia,
   explicit legacy fallback, or blocked. Apply `fieldworks-ui-wiring-review`.
7. **Prove parity.** Build the evidence bundle defined in
   `references/parity-evidence.md` (semantic + visual + workflow lanes).
   Apply `fieldworks-semantic-render-parity` and
   `fieldworks-uia2-parity-testing`. **Front-and-center: write headless
   integration tests that walk the surface's real scenarios/workflows**
   (filter → clear, select → detail follows, edit → refresh, navigate) via the
   harness (architecture-patterns.md §13) — at the surface layer
   (`FwAvaloniaTests`) and, for domain claims like real list narrowing/undo, the
   real-clerk layer (`xWorksTests`). These replace deferred "live verification."
8. **Localize.** Apply `fieldworks-localization-review`; field labels stay
  on the StringTable lane, while Avalonia chrome joins the existing
  LocalizationManager/L10NSharp XLIFF catalog (prefer existing
  Palaso/Chorus ids when semantics and markup match; otherwise add
  unique Avalonia-prefixed ids in that catalog).
9. **Retire and gate.** Run the symbol audit
   (`Src/Common/FwAvalonia/FwAvaloniaTests/EngineIsolationAuditTests.cs`),
   active-host contract tests
   (`Src/xWorks/xWorksTests/RecordEditViewActiveHostContractTests.cs`),
   and the normal repo gates (`./build.ps1`, `./test.ps1`).
10. **Retrospective.** Update these skills — see "Keep this skill set
    current" below. This step is part of the migration, not optional polish.

## Hard Rules

- Active Avalonia hosts must not instantiate or drive hidden legacy
  `DataTree`, `Slice`, `RootSite`, menu, or renderer infrastructure except
  through approved baseline adapters
  (`Src/Common/FwAvalonia/Seams/ActiveHostContract.cs`).
- Migrated-region production code must stay free of the forbidden symbols
  listed in parity-evidence.md §"Forbidden symbols" (enforced by
  `EngineIsolationAuditTests.cs`).
- Evidence comes from the normal repo path: `./build.ps1` and `./test.ps1`.
  Branch-only lanes or ad hoc commands are not integration evidence.
- One global undo/redo stack (LCModel action handler). Never a parallel
  Avalonia-only history for committed state.
- Avalonia modal windows are not supported during coexistence; anything
  modal uses a WinForms dialog with the host form as owner (see
  architecture-patterns.md §7).
- Performance budgets are measured against legacy baselines, not estimated
  (parity-evidence.md §"Performance budgets").

## Review Red Flags

- Tests manually invoke `OnPropertyChanged`, `ShowRecord`, or similar
  handlers to simulate runtime wiring instead of driving the real path.
- Active Avalonia routing depends on a lossy DTO mapper or preview-only
  code without an explicit product contract.
- Task checkboxes claim parity while evidence says substitute, placeholder,
  skipped, or future work (see parity-evidence.md §"Evidence language").
- A custom slice class silently renders wrong instead of producing an
  explicit unsupported row.
- A PR mixes plans, tests, infrastructure, product wiring, and unrelated
  changes — apply `fieldworks-migration-scope-review`.

## Handoff

State what is legacy baseline, what is extracted seam, what is Avalonia
product surface, what each affected host does under the global switch, what
remains outside parity, and what you changed in this skill set during the
retrospective.

## Keep This Skill Set Current

These skills are the institutional memory of the migration. Every completed
migration teaches something; if it stays in your head or in a PR thread it
is lost. The retrospective step (workflow step 10) is how the skills stay
ahead of the codebase instead of trailing it:

1. Read `references/lessons-learned.md` and follow its update protocol —
   it maps each kind of discovery (new pattern, new gotcha, fired pivot
   trigger, new canonical example, stale pointer) to the exact file and
   section to update.
2. Make the skill edits in the same PR as the migration, so reviewers see
   the lesson next to the evidence that produced it.
3. If a file pointer in any of these skills is stale (file moved, openspec
   change archived), fix the pointer immediately — do not work around it
   silently.
