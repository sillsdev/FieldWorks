# Stage 1 — Migration platform & developer-enablement kit (Epic — **finished spec**)

> Status: **implementation-ready** (finished, not draft). Grounded in `reviews/stage-01-platform-enablement-kit.md`,
> the as-built code under `Src/Common/FwAvalonia/`, and the open items in
> `lexical-edit-avalonia-migration/tasks.md`. "Finished" = the spec is complete and code-mapped; the
> code work itself is teed up per sub-epic with explicit done-vs-remaining.

## Epic
- **Summary:** Turn the one-off lexical-edit migration into a reusable, documented platform two kits +
  evidence base + runbook so mid/junior devs (with Claude) can migrate a surface end-to-end.
- **Type:** Epic · **Labels:** `track-foundation`, `lead-senior` · **Size:** L
- **Description:** Generalize the proven lexical-edit assets into (1) a **region/IR scaffolding kit** for
  XML-view-definition-driven surfaces and (2) a **dialog MVVM kit** for hand-authored dialogs, plus a
  shared parity-evidence base, a migrate-a-surface runbook, and executable conventions. Much is already
  reusable; this epic is **generalization + documentation + the net-new dialog toolchain**, not green-field.
- **Acceptance criteria:**
  - A junior + Claude migrates **both** a trivial dialog (MVVM kit) **and** a trivial region mini-surface
    (region kit) end-to-end using only the kit + runbook, each with a green evidence bundle.
  - Conventions are enforced by tests, not prose (one test each).
  - Seam catalog is **versioned with an amendment protocol** (it still grows via Stages 3/9).
- **Dependencies:** consumes Stage 2's surface registry; **blocks** all junior work (esp. Stage 5) and
  feeds Stages 4/6/8. Region template marked **provisional until Stage 4 closes** (exemplar gates still Partial).
- **Cross-stage note:** resolves the Stage 1/2 double-booking — the **app-wide surface registry is owned by
  Stage 2**, not here.

## Sub-epics / stories

### 1.1 Region/IR scaffolding kit  · Story · L
- **Description:** A generator that emits a new region's composer skeleton, region model, view, manifest
  stub, and a **red** evidence-bundle test, extracted from the de-`LexicalEdit`-prefixed region layer.
- **Done:** owned field controls are already surface-agnostic (`FwFieldControls.cs`, `FwOptionPicker.cs`);
  `RegionViewingServices.cs` documents the as-built viewing-replacement contract to copy.
- **Remaining:** the region layer is `LexicalEdit`-prefixed throughout (`LexicalEditRegionModel/Mapper/View`,
  `LexicalEditSurfaceSelectionService`) — de-prefix into a reusable base; pairs with **task 18.11** (unify
  dual projector) and **8.9** (extract per-capability seam on 2nd-region adoption).
- **Acceptance:** generate → red tests → fill-in produces a compiling region; generator emits red stubs by default.

### 1.2 Dialog MVVM kit  · Story · M  *(net-new toolchain — DECIDED 2026-06-15: adopt MVVM)*
> **Decision:** adopt Avalonia XAML + CommunityToolkit.Mvvm + compiled bindings, in a **dedicated
> XAML-enabled project** (`FwAvaloniaDialogs`) so the foundation (`FwAvalonia`) stays pure-C# per its
> documented guarantee. The net48 XAML-compiler/MSBuild integration spike is this story's first task
> (and the Stage 5 gate); it is unblocked.
- **Description:** Establish the dialog-authoring stack the repo does not yet have: add the
  **CommunityToolkit.Mvvm** package, the **first `.axaml`**, enable **compiled bindings** (`x:CompileBindings`)
  on the net48 build, and a dialog scaffolding generator (view + view-model + host-wrapped-body Form wrapper).
- **Status (2026-06-16):** **de-risk DONE** — `FwAvaloniaDialogs` adds CommunityToolkit.Mvvm, the first
  `.axaml` (Tools→Options dialog), and compiled bindings on net48, building green through `build.ps1`
  (dialog suite 9/9). **Remaining:** the host-modal WinForms wrapper for coexistence, the localization
  lane, and the dialog scaffolding generator (view + view-model + host-wrapped Form wrapper).
- **Acceptance:** scaffold a dialog whose body is an Avalonia view hosted in a WinForms-owned modal Form
  (per `dialog-ownership.md`); code-behind exception documented so owned controls embed without rewrite.

### 1.3 Shared parity-evidence base  · Story · M
- **Description:** Parameterize `Path3BundleTests.cs` (today a single hardcoded `scenarioId="first-slice"`)
  into a reusable base any surface test derives from; define a **dialog-flavored evidence bundle** (no IR
  semantic anchor — dialogs have no compiled IR).
- **Acceptance:** two scenarios (one region, one dialog) run through the same base with distinct bundle shapes.

### 1.4 Migrate-a-surface runbook  · Story · S
- **Description:** Map the 10-step migration workflow to concrete repo actions; wire it to the
  `fieldworks-winforms-to-avalonia-migration` skill so AI assistants follow it.
- **Acceptance:** runbook drives the validation-gate migration without tribal knowledge.

### 1.5 Executable conventions + seam-catalog versioning  · Story · S
- **Description:** Document + **test-enforce** AutomationId-from-StableId (already implemented,
  `FwFieldControls.cs:44/138/361/537/576`), density tokens, the modernized-Fluent ControlTheme baseline,
  localization lanes, and `<RootNamespace>` (already present). Convert "freeze the seam catalog" into a
  **versioned catalog + amendment protocol**.
- **Acceptance:** one test per convention; a CI check fails if a new owned control omits an AutomationId.

## Notes / open questions
- The region template is extracted from a not-yet-green exemplar → keep it **provisional** until Stage 4
  flips the region-manifest §6 rows from Partial.
- Net48 compiled-bindings build is the single biggest unknown — spike it first (gates 1.2 and Stage 5).
