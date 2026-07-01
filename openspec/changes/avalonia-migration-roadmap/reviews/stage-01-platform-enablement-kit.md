# Stage 1 Review — Migration platform & developer-enablement kit

> Reviewer pass over Stage 1 of `complete-migration-program.md` (§6 Track I, §4 table row 1).
> Grounded in the as-built `Src/Common/FwAvalonia/` tree and the frozen skill references.
> Status: planning review only; no code/behavior change proposed here.

## Scope assessment

The five deliverables are the right ones and are well-chosen: (1) reusable region
scaffolding/generator, (2) Path-3 harness promoted to a shared test base, (3) frozen+documented
seam catalog & plugin-registry onboarding, (4) a "migrate-a-surface" runbook wired to the skill,
(5) locked conventions (AutomationId-from-StableId, density tokens, ControlTheme baseline,
localization lanes, RootNamespace). The validation gate (junior+Claude migrates a trivial surface
end-to-end using only the kit) is exactly the right exit criterion — it tests the *kit*, not the
author.

Three scope gaps and one over-scope concern:

- **Gap — the trivial validation surface is a *dialog*, but dialogs were ruled OUT of the
  region/IR pattern** (Decision 3, 2026-06-15: dialogs/wizards use CommunityToolkit.Mvvm +
  compiled bindings, *not* the composer/IR). The §6 Stage-1 text says "e.g. a single simple
  dialog." That means the kit's headline deliverable (region scaffolding/generator) is NOT what
  the validation surface exercises. **Stage 1 actually has two kits**: (a) the region/IR
  scaffolding for XML-driven surfaces, and (b) an MVVM-dialog scaffolding + runbook for the
  Stage-5 reservoir. The plan currently funds (a) explicitly and leaves (b) implicit, yet the
  validation gate and the largest hand-off reservoir (Stage 5, ~200 dialogs) both ride on (b).
  This is the single most important correction.

- **Gap — naming/de-`LexicalEdit` generalization is unscoped work.** Nearly every region-layer
  type is `LexicalEdit`-prefixed and single-scenario hardcoded (see Feasibility). "Extract a
  reusable region scaffolding" implicitly requires renaming/parameterizing these, which is real
  senior effort, not a template-copy. Call it out as an explicit sub-task with a compatibility
  plan (Stage 4 still consumes these symbols).

- **Gap — a generator needs a *test harness for the generator itself*** (golden output, "does the
  generated skeleton compile + its stub tests run red-then-green"). Otherwise the generator rots.

- **Over-scope risk — "freeze the seam catalog."** The catalog is demonstrably *not* frozen:
  `IXCoreCommandBridge` is explicitly shell-phase/deferred, document-engine seams (Stage 9) do not
  exist, and the grid/tree control (Stage 3) will likely add virtualization seams. Stage 1 should
  **document and version** the catalog and define the *amendment protocol* (already half-specified
  in seam-catalog.md §"add the new seam here in the same PR"), not declare it frozen. Freezing a
  catalog that Stages 3/9 will provably extend sets up a false gate.

## Feasibility (repo-grounded)

Feasible and well-advanced, but "turn the one-off into a platform" is more generalization than the
plan's verb "extract" implies. What I found:

**Already reusable, near as-is:**
- Owned field controls — `Src/Common/FwAvalonia/Region/FwFieldControls.cs`
  (`FwMultiWsTextField`, `FwChooserField`, `FwReferenceVectorField`), `FwOptionPicker.cs`,
  `RegionMenuFlyout.cs`, `HoverReveal.cs`, `RegionFocusMemory.cs`. These take an `automationId` +
  model and are surface-agnostic. The AutomationId-from-StableId convention is *implemented*
  (`AutomationProperties.SetAutomationId(this, automationId)` then `+ "." + wsKey`,
  `+ ".Settings"`, `+ ".Add"`, `+ ".Item." + item.Key` — FwFieldControls.cs:44,138,361,537,576)
  but **not documented** as a contract — Stage 1 must lift it into a written convention + a peer
  test, because juniors will otherwise invent their own ids.
- Typed IR pipeline — `Src/Common/FwAvalonia/ViewDefinition/` (importer, compiler, JSON
  serializer, coverage). Genuinely surface-generic already.
- Seams — `Src/Common/FwAvalonia/Seams/ISeams.cs`, `SeamImplementations.cs`,
  `ActiveHostContract.cs`. Reusable; well-documented in seam-catalog.md.
- Forbidden-symbol audit — `FwAvaloniaTests/EngineIsolationAuditTests.cs`. Reusable as-is.
- `<RootNamespace>` is **already present** in `Src/Common/FwAvalonia/FwAvalonia.csproj` and
  `Src/xWorks/xWorks.csproj` — so this convention is locked for existing projects; Stage 1's job is
  to make it a *checklist item / project template default* for the many new csprojs Stage 5 spawns.

**Needs real generalization (senior work, not copy-paste):**
- The region layer is single-scenario and `LexicalEdit`-named:
  `LexicalEditRegionModel.cs`, `LexicalEditRegionMapper.cs`, `LexicalEditRegionView.cs`,
  `LexicalEditSurfaceSelectionService.cs` (`SurfaceDecision` returns a `LexicalEditSurface` enum),
  `LexicalEditFirstSlice.cs`. The composer lives outside FwAvalonia in
  `Src/xWorks/FullEntryRegionComposer.cs`. `RegionViewingServices.cs` is a static, hardcoded
  lexical map. A "new-surface generator" must abstract a `RegionId`/surface key out of these and
  rename the switch service to an app-wide registry — which **overlaps Stage 2** (the plan
  already assigns "generalize `LexicalEditSurfaceSelectionService` → app-wide surface registry"
  to Stage 2). Decide the boundary: I recommend the *generic switch contract/registry shape* lands
  in Stage 1 (the kit needs it to scaffold), with Stage 2 doing the host-bridge wiring.
- The Path-3 harness — `FwAvaloniaTests/Path3BundleTests.cs` — is a single hardcoded
  `scenarioId = "first-slice"` test that points at one committed WinForms baseline PNG. Promoting
  it to a "shared test base any surface test derives from" means extracting a parameterized base
  class (scenarioId, IR factory, region view factory, baseline path, lane manifest writer) — a
  worthwhile but non-trivial refactor. The lane-manifest pattern (proven/pending per lane, never
  silently omitted) is the valuable reusable kernel and is already correct.

**Validation-gate feasibility caveat:** the gate says junior+Claude migrates a surface "with a
green parity bundle." For an MVVM dialog there is no IR semantic snapshot, so the Path-3 bundle
shape (semantic.json anchor) doesn't map cleanly. Stage 1 must define a **dialog-flavored evidence
bundle** (visual + workflow/UIA + localization + AutomationId audit; no IR semantic lane) or the
gate is unsatisfiable as written.

## Best practices (enablement kits / scaffolding)

- **Generator output must be red-green-test-ready.** The scaffold should emit stub tests that fail
  meaningfully until parity is captured — aligns with the program's anti-"hallucinated-parity"
  stance (§2 principle 4). A generator that emits green-by-default stubs is an anti-pattern here.
- **"Golden path" + guardrails over freedom.** Junior+Claude productivity comes from one blessed
  path with build-time enforcement (compiled bindings catch binding errors — Decision 3; the
  symbol audit catches engine leakage). Lock these as defaults in the template, not as docs.
- **Executable conventions beat prose.** Each locked convention should have a *test* (an
  AutomationId-shape test, a RootNamespace-present test, a localization-lane lint), not just a
  runbook paragraph. The repo already does this for symbols/host-contract; extend the pattern.
- **Runbook should be a checklist the skill drives, not a tutorial.** The 10-step workflow in
  `SKILL.md` and `migration-checklist.md` already exist; Stage 1's runbook should *map each step
  to a concrete repo action/command/generator invocation* and live next to (or inside) the skill,
  avoiding a third parallel copy of the checklist that will drift.
- **Version the kit.** Stamp generated surfaces with the kit/template version so a later kit change
  can find surfaces built on the old template.

## Interactions & dependencies

- **Depends on Stage 0 only** (per the table) — correct. It does **not** depend on Stage 2/3, and
  it must not: making it wait for the grid (Stage 3) would stall all junior hand-off. The kit
  should scaffold a *placeholder/seam* for the shared grid so Stage-3 delivery slots in without
  re-scaffolding.
- **Blocks all junior work** and is the explicit prerequisite for Stage 5 (§8 hand-off model).
  Concretely Stage 5 needs from Stage 1: the **MVVM-dialog scaffolding + dialog evidence bundle +
  dialog-ownership runbook** (modality stays WinForms-owned per dialog-ownership.md / Decision 3),
  not the region generator. **This is currently under-served by the Stage-1 deliverable list.**
- **Stage 6 (mid, detail surfaces)** is the true consumer of the region/composer generator + plugin
  onboarding ("how to add a custom slice"). Stage 1 must deliver the plugin-registry runbook
  (`Src/xWorks/RegionEditorPlugins.cs`, burn-down tracking) aimed at Stage 6.
- **Stage 8** consumes both kits (dialogs + detail). No new Stage-1 obligation beyond 5/6.
- **Overlap with Stage 2** on the surface-registry generalization (noted above) — needs an explicit
  seam-line so the two stages don't both rewrite `LexicalEditSurfaceSelectionService`.
- **Tension with Stage 4:** the plan sequences hand-off "begins after Stage 1, accelerates after
  Stage 4 (worked exemplar)." But the manifest (`region-manifest.md` §6) shows the exemplar still
  has **Partial** gates (layout-semantic, validation, accessibility, performance). Stage 1's
  *region* generator is therefore being extracted from an exemplar that is not yet fully green. The
  conventions/seams/harness are stable enough to extract now; the *region composer template* should
  be marked provisional until Stage 4 closes, or Stage 1 risks freezing patterns Stage 4 changes.

## Recommended plan changes (concrete)

1. **Split the kit into two explicit tracks** in the §6 Stage-1 text: (a) region/IR scaffolding for
   XML-driven surfaces (feeds Stage 4/6); (b) **MVVM-dialog scaffolding (CommunityToolkit.Mvvm +
   compiled bindings) + dialog-ownership runbook + dialog evidence bundle** (feeds Stage 5 and the
   validation gate). Make (b) a named deliverable, not implied.
2. **Redefine the validation gate** to use a dialog (track b) AND add a second mini-validation: a
   trivial *detail/region* surface through track (a), so both kits are proven before hand-off.
   Define the **dialog evidence bundle** shape (visual + UIA/workflow + localization + AutomationId
   audit; no IR semantic lane) since the Path-3 semantic anchor doesn't apply to dialogs.
3. **Reword "freeze the seam catalog" → "document, version, and define the amendment protocol."**
   The catalog is provably still growing (Stage 3 grid seams, Stage 9 doc-engine seams,
   `IXCoreCommandBridge` shell-phase). Freezing it is a false gate.
4. **Add an explicit "de-`LexicalEdit` / parameterize the region layer" sub-task** with a named
   compatibility plan for Stage-4 consumers, and **draw the Stage-1/Stage-2 boundary** on the
   surface-registry generalization (generic switch *shape* in 1; host-bridge wiring in 2).
5. **Make conventions executable:** ship a test per locked convention (AutomationId shape from
   StableId, `<RootNamespace>` present, localization-lane placement, density-token usage). The
   AutomationId derivation is already in code (FwFieldControls.cs) — lift it to a documented +
   tested contract.
6. **Promote Path-3 to a parameterized base class** (`Path3BundleTests` → abstract base taking
   scenarioId/IR-factory/view-factory/baseline-path/lane-writer) and ship one derived sample.
7. **Require the generator to emit red stub tests** (fail until parity captured) and add a
   generator-golden-output test so the generator itself is covered.
8. **Mark the region composer template "provisional until Stage 4 green"**; ship seams/harness/
   conventions as stable now.

## Open questions / risks

- Is the validation surface a dialog (track b) or a region (track a)? The plan says dialog, but the
  headline deliverable is the region generator — they must be reconciled (Rec. 1–2).
- Where does the surface-registry generalization live — Stage 1 or Stage 2? Currently double-booked.
- Risk: extracting the region template from a not-yet-green exemplar (Stage 4 Partial gates) bakes
  in patterns that Stage 4 later changes; mitigate by marking the region template provisional.
- Risk: a generator/runbook that drifts from the skill set. Mitigate by wiring the runbook *into*
  the skill (single source) and versioning the template.
- Risk: dialog evidence bundle undefined → Stage-5 juniors produce inconsistent/weak evidence at
  ~200-dialog scale (the highest-volume, lowest-supervision reservoir). Define it in Stage 1.

## Confidence

**High** on the feasibility read (the as-built code is present and inspected; conventions and seams
are stable enough to extract now). **Medium** on the scope correction — the dialog-vs-region split
hinges on Decision 3's interaction with the validation gate, which the plan text has not yet
reconciled; if the program intends the validation surface to be a region after all, Recs. 1–2 change
shape.
