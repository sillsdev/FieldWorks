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

## Phase-1 Landing Strategy (canonical-per-primitive, document-then-back-out)

The program runs in two phases. **Phase 1** = high-value feature/bugfix-grade
migrations behind the `UIMode` flag (default `"Legacy"` —
`Src/Common/FwUtils/Properties/Settings.Designer.cs`; every Avalonia surface gates on
`UIMode=New` via `LexicalEditSurfaceRegistry` + `LexicalEditSurfaceResolver`, so default
users see no change). **Phase 2** (`avalonia-end-game`) = net10 / multiplatform / shell
conversion, gated until Phase-1 + tester burn-down complete.

A Phase-1 derisk branch tends to accrete far more than one PR should carry (the first such
branch reached ~864 files / +140k). Land it with this discipline:

1. **One canonical screen per UI primitive.** Keep exactly one fully-wired, green,
   parity-evidenced *consumer* per primitive as the reference teammates copy — distinct from
   the reusable *control*, which you always keep. Current canonical map (the screens to copy):
   - virtualized editable **TABLE** → Lexicon Browse/Edit pane (`LexicalBrowseView`)
   - composed **detail editor** (DataTree replacement) → Lexicon Edit entry pane
     (`FullEntryRegionComposer`); the same composer also drives `notebookEdit`/`posEdit`
   - **tree + multi-selector** → `ChooserDialog` (one screen covers both)
   - **tabs** → `OptionsDialog`; **owned-control composite form** → `InsertEntryDialog`;
     **search+list** → `EntryGoDialog`
2. **Document every deferred screen, then back it out.** For each WinForms screen not kept,
   write `Docs/migration/<screen>.md` (use `Docs/migration/_TEMPLATE.md`) with a legacy PNG
   captured from live FLEx (apply `fieldworks-winapp` / winforms-mcp), the primitive, the
   parity checklist, and gotchas; file a JIRA ticket; then remove the Avalonia view/VM/tests
   **and unwire its call site back to the legacy path**. Because the flag defaults off, this
   is safe to do aggressively — the goal is a reviewable PR and a clean starting point per
   ticket, not runtime safety.
3. **Split XL surfaces into their own follow-up PRs** rather than backing them out, when they
   already live in isolated openspec changes/worktrees (e.g. `avalonia-rule-formula-editor`,
   `avalonia-interlinear-editor`). Keep shared composer infra in the spine PR.
4. **Verify wiring from call sites, never from a summary.** Whether a dialog is wired (and
   thus needs its call site reverted) is determined by reading the product call site
   (`RecordBrowseView`, the `Lcm*Launcher`s), not by class names, comments, or an Explore
   agent's claim — those have produced false "unwired" negatives. Quote the `file:line`.
5. **The PR body is a manifest:** name each canonical screen and why; list each backed-out
   screen with its doc path + JIRA id; name the split-out follow-up PRs.

Re-implementers picking up a JIRA ticket: start from the named canonical screen for that
primitive, read its doc's parity checklist + gotchas, recover the backed-out stub from git
history as a starting point, then run the normal per-region Workflow above.

### Inert follow-up surfaces — how to find them and turn them on

A Phase-1 surface can ship **inert**: its view code is present and compiled but the tool is
deliberately *not registered*, so the resolver returns "not supported" and the surface falls
back to legacy WinForms even under `UIMode=New`. This is the safe way to land a large surface's
code in one PR and *activate* it in a small follow-up PR (the "flip"). Two distinct gates exist
and an active surface needs BOTH open:

- **Plugin registration** (does the slice *compose* on Avalonia): `RegionEditorPlugins.RegisterBuiltins`
  must `registry.Register(new <Surface>Plugin())`. The `LexemeEditorBurnDownTests` census
  asserts the registered set *exactly*, so it fails until the class name is added/removed in step.
- **Surface/tool gate** (does the tool's surface *resolve* to Avalonia): the tool name must live in
  `LexicalEditSurfaceRegistry.DefaultSupportedTools` (detail editor) or
  `LexicalEditSurfaceResolver.SupportedAvaloniaBrowseToolNames` (browse table) — NOT in the parallel
  `Phase1FollowUpSurfaceTools` / `Phase1FollowUpBrowseTools` arrays, which are exactly the **inert
  list**. To find every dormant surface, read those two `Phase1FollowUp*` arrays.

**Activation recipe** (turning an inert surface on, e.g. picking up its follow-up ticket):
1. If the view files were carved to a follow-up branch, restore them from the canonical branch
   (`git checkout <canonical> -- <owned files>`); if they shipped dormant in base, they are already present.
2. Restore the plugin registration line(s) in `RegionEditorPlugins.RegisterBuiltins`.
3. Restore the surface's class name(s) in the `LexemeEditorBurnDownTests` census array (+ any resolve assertion).
4. **Flip**: move the tool name(s) from `Phase1FollowUpSurfaceTools` → `DefaultSupportedTools`
   (edit) or `Phase1FollowUpBrowseTools` → `SupportedAvaloniaBrowseToolNames` (browse).
5. Add/flip the corresponding `TestCase` rows: `RecordEditViewSwitchTests.RegisteredRecordEditTools_*`
   (edit) and the `ResolveBrowse_*_YieldsAvalonia` resolver tests (browse).
6. Build + run the census, resolver, switch, and the surface's own suites green.

The **ground truth** of which tools were active before a split is the pre-split pinned commit's
version of `LexicalEditSurfaceRegistry.cs` + `LexicalEditSurfaceResolver.cs` (e.g. a `phase1-pin`
tag): `git show <pin>:<file>` shows the canonical active arrays to restore.

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
