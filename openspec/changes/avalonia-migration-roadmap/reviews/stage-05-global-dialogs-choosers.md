# Stage 5 Review — Global dialogs & choosers (junior-friendly reservoir)

**Reviewer:** Claude (Opus 4.8) · **Date:** 2026-06-15 · **Stage lead level:** Junior · **Concurrency:** 4–6 streams
**Scope under review:** `complete-migration-program.md` §4 row 5, §6 Stage 5, §11 decision 3; cross-checked
against `architecture-patterns.md` §7, `dialog-ownership.md`, `migration-checklist.md`, `parity-evidence.md`,
and the as-built repo state.

---

## 1. Scope assessment

The stage targets the largest single block of work in the program (~200 surfaces) and is the designated
junior-with-Claude reservoir. The "~200 dialogs" figure is repo-validated: counting `: Form` subclasses
(non-designer, non-test) across the five major UI dirs gives **~123 Forms** plus **~78 UserControls**
(FwCoreDlgs 33, LexText 45, xWorks 17, Common 20, FdoUi 8 Forms). FwCoreDlgs alone has **46 `*.Designer.cs`**
and **121 non-designer `.cs`**. So the headcount premise is real.

But "junior-friendly + mechanical" is **only partly true**, and the stage as written conflates three very
different populations:

- **Genuinely mechanical (junior-safe):** small modal/warning dialogs and simple choosers —
  `FwChooserDlg.cs` (396 LOC), `FwFontDialog.cs` (461), `FwStylesModifiedDlg.cs` (58),
  `AddNewVernLangWarningDlg`, `DeleteWritingSystemWarningDialog`, `MissingOldFieldWorksDlg`,
  `MoveOrCopyFilesDlg`, the BackupRestore set (already MVP, separate Presenters + `IBackupProjectView`).
- **Mid-complexity, structured but large:** `FwProjPropertiesDlg.cs` (885), `ValidCharactersDlg.cs` (1,965),
  `FwWritingSystemSetupDlg.cs` (738, has `FwWritingSystemSetupModel.cs`), `FwNewLangProject.cs` (359 +
  `FwNewLangProjectModel.cs`, a multi-step wizard). These are **not** junior-mechanical.
- **Views-engine-coupled (NOT Stage-5 work at all):** `FwFindReplaceDlg.cs` (3,290 LOC, **14**
  `IVwRootSite`/`SimpleRootSite` references — search ops manipulate live rootbox selection) and
  `FwStylesDlg.cs` (1,335 LOC, hosts `IVwRootSite m_rootSite` for live style preview). These embed the
  native Views engine that Stage 9 replaces. `PicturePropertiesDialog.cs` (620) has a light
  `SimpleRootSite.ImageNotFoundX` reference (cosmetic, not blocking).

**Verdict:** the scope is realistic in *headcount* but mis-labelled in *difficulty*. Find/Replace and the
Styles dialog are explicitly listed as "FwCoreDlgs first" junior targets in §6, yet they are the two most
Views-coupled dialogs in the project and cannot be migrated until Stage 9. They must be re-tiered.

---

## 2. Feasibility (repo-grounded)

### 2a. The modal-during-coexistence tension — RESOLVED, but the plan's wording invites error

This is the headline question, and the architecture already answers it. Per `architecture-patterns.md` §7
and `dialog-ownership.md` rules 1 and 4:

- **No Avalonia modal windows exist during coexistence.** `Avalonia Window.ShowDialog` against a WinForms
  owner is "not a supported combination on 11.x in this app" (`dialog-ownership.md` rule 4).
- Anything modal stays a **WinForms `Form`** owned by `Control.FindForm()` of the host
  (`LexicalEditHostControl`), blocking the shared message loop (rule 1).

So how can a dialog be "migrated to Avalonia" before the shell (Stage 11)? The resolution — which §6 states
tersely and which must be made unmissable — is the **content/chrome split**:

> A Stage-5 "migration" replaces the dialog's **content** (the panel of fields/controls) with an Avalonia
> view, **hosted via `WinFormsAvaloniaControlHost` inside a thin WinForms `Form` shell** that still owns
> modality, `DialogResult`, and `ShowDialog`. The window is WinForms; the body is Avalonia.

This is exactly the `LexicalEditHostControl` spine (decision 2 / principle 2) applied at dialog scope. It is
feasible **today** on the as-built 11.x stack and is **not blocked on Stage 11**. Stage 11 only removes the
WinForms `Form` wrapper and promotes the body to a real Avalonia modal window once the shell can own one.

The risk is purely *documentation*: §6 says "new Avalonia dialog *content* is fine inside the host" in one
clause, but the surrounding prose ("dialogs migrated to Avalonia") and the §8 staffing model read as if whole
Avalonia dialogs ship. A junior will reasonably try `new Window().ShowDialog(owner)` and hit the unsupported
path. **Make the host-wrapped-content contract a hard, first-line rule of the Stage-5 epic, with a code
template.**

### 2b. CommunityToolkit.Mvvm + compiled bindings — feasible but currently **counter to the as-built grain**

Decision 3 mandates CommunityToolkit.Mvvm + compiled bindings (`x:CompileBindings`) for dialogs/wizards. The
repo today contradicts the prerequisites:

- **CommunityToolkit.Mvvm is not referenced anywhere** (not in any `.csproj`, no `packages.config`).
- **There are zero `.axaml` files in `Src/`.** Every as-built Avalonia surface — `FwOptionPicker.cs`,
  `FwFieldControls.cs`, `RegionMenuFlyout.cs`, `LexicalEditRegionView.cs` — is **hand-written C# code-behind**,
  net48, no XAML. Compiled bindings (`x:CompileBindings`) and `AvaloniaUseCompiledBindingsByDefault` are a
  **XAML feature**; they do not apply to code-behind UI.

This is not a blocker but it is an unacknowledged pivot: Stage 5 is the program's **first XAML + MVVM-toolkit
adoption**. The decision is sound (statically-checked bindings catch AI binding hallucinations at build time;
source-generated `[ObservableProperty]`/`[RelayCommand]` lower the junior boilerplate burden), but it means
Stage 5 introduces a *new authoring style* the codebase has never used, on the back of juniors. That tooling
must be stood up and proven **in Stage 1**, not discovered in Stage 5.

The reuse story is good: `FwMultiWsTextField` (in `FwFieldControls.cs`) and `FwOptionPicker` (`FwOptionPicker.cs`)
are self-contained Avalonia controls that drop into a dialog body wherever WS-aware text or a chooser field is
needed — exactly per decision 3. They are code-behind controls, so they compose into a XAML dialog as plain
elements; no friction there.

### 2c. The "compiled bindings" gate needs a code-behind exception

Because the proven owned controls are code-behind, the Stage-5 convention should be: **XAML + compiled
bindings for the dialog layout/view-model wiring; code-behind owned controls embedded as child elements.** A
blanket "all dialogs are XAML with compiled bindings" rule would either force a rewrite of the proven controls
or be quietly violated. State the hybrid explicitly.

---

## 3. Best practices for high-volume, parallel, junior+AI dialog migration

1. **Tier the backlog before hand-off** (see §5). Juniors get Tier-A (simple, Views-free, <~600 LOC). Mid
   devs get Tier-B (structured, has/needs a model). Tier-C (Views-coupled: Find/Replace, Styles) leaves
   Stage 5 entirely and is re-parented to Stage 9.
2. **Ship a dialog scaffolding generator in Stage 1** (the §6 Stage-1 "new-surface generator" must cover the
   *dialog* shape, not just regions): WinForms `Form` host shell + `WinFormsAvaloniaControlHost` + `.axaml`
   view + `CommunityToolkit.Mvvm` view-model + AutomationId scheme + parity-bundle test stub +
   localization-lane stubs. Decision 3 says dialogs do **not** use the region/composer generator, so a
   *separate* dialog template is required and is currently unscoped in Stage 1.
3. **One dialog per PR, whole files owned** (principle 10 / §9 merge-contention mitigation). New `.axaml` +
   `.cs` view-model + host `Form` are net-new files → near-zero merge contention across 4–6 streams. Defer
   the call-site swap (replacing `new FooDlg()` with the hosted variant) to the per-milestone integration
   step (principle 11).
4. **Compiled bindings are the anti-hallucination gate.** Enforce `x:CompileBindings="True"` so every binding
   is build-checked — this is the cheapest defense against the program's #1 risk (AI "hallucinated parity",
   §9). A binding typo fails the build instead of silently rendering blank.
5. **Parity bundle is mandatory per dialog** (`migration-checklist.md` Phase 7, `parity-evidence.md`):
   semantic + visual + workflow + perf, captured before refactor. The visual lane permits the Fluent restyle
   (decision 4) — assert field semantics/density, not pixels. **Enforce the evidence vocabulary**: a checked
   box whose note says *substitute/placeholder/skipped/future/partial* is a review blocker. With juniors this
   is the load-bearing control; require an integration-owner spot-check.
6. **AutomationId day one, every control** (principle 8, checklist Phase 5/8): nonlocalized IDs, localized
   Names. This is the prerequisite for the WinAppDriver/Appium workflow lane.
7. **Localization lanes**: field labels via the StringTable lane; product strings via `FwAvaloniaStrings.resx`
   (already the live pattern — see modified `FwAvaloniaStrings.resx` on this branch). Run
   `fieldworks-localization-review` per dialog. Do **not** hardcode strings (AGENTS.md hard rule).
8. **Focus-return contract is not free** (`dialog-ownership.md` rule 2): when a dialog is launched *from* an
   Avalonia surface, record the focused Avalonia control, restore after close. Bake this into the host
   template so juniors inherit it rather than reimplement it.
9. **Wizards are not "dialogs."** `FwNewLangProject` is a multi-step state machine (`FwNewLangProjectModel`
   with `Load*SetupDelegate` callbacks). Treat wizards as a distinct, mid-level sub-track with their own
   navigation/step view-model pattern.

---

## 4. Interactions & dependencies

- **Stage 1 (platform kit) — hard prerequisite, currently under-scoped for dialogs.** Stage 5 cannot start
  until Stage 1 delivers (a) the *dialog* scaffolding generator and host-`Form` template, and (b) the
  CommunityToolkit.Mvvm + compiled-bindings + first `.axaml` tooling — none of which exists in the repo today
  and none of which the region generator covers. Recommend adding an explicit Stage-1 sub-task: "Stand up and
  prove the MVVM-dialog authoring stack (CommunityToolkit.Mvvm package ref, compiled-bindings build setting,
  one trivial XAML dialog hosted in a WinForms `Form` shell, green parity bundle)." This *is* the Stage-1
  validation gate ("a junior migrates one trivial dialog"), so it is mostly already implied — but the
  XAML/MVVM-toolkit novelty must be called out, because the as-built code is 100% code-behind.
- **Stage 2 (host spine) — correct dependency.** §4 lists Stage 5 depending on Stage 2, which generalizes
  `LexicalEditHostControl` → reusable host and pulls forward the dialog-ownership contract layer. The
  dialog-body hosting reuses exactly this. Correct as drawn.
- **Stage 11 (shell/modality) — NOT a blocker for Stage 5; it is a *follow-on*.** This is the crux. Stage 5
  ships WinForms-`Form`-wrapped Avalonia bodies during coexistence; Stage 11 later removes the wrapper and
  promotes them to native Avalonia modal windows once the Avalonia shell can own modality. So Stage 5 → Stage
  11 is a *forward* edge (Stage 11 finishes what Stage 5 starts), not a backward block. The §5 graph already
  draws `S5 → S11` this way; keep it, but annotate the edge as "shell promotes hosted dialog bodies to native
  Avalonia windows" so it is not misread as a blocking dependency.
- **Stage 9 (Views replacement) — newly-surfaced dependency for two named dialogs.** `FwFindReplaceDlg` and
  `FwStylesDlg` embed `IVwRootSite`/`SimpleRootSite` and cannot be migrated until the managed document engine
  exists. They are currently listed under Stage 5 ("Find/Replace", "Styles") — that is a **cross-stage
  conflict** and must be corrected.
- **Stage 6/8 — downstream.** §4 has Stage 8 depending on Stage 5 (shared chooser/launcher infra). Correct:
  the shared `FwOptionPicker`/flyout chooser pattern hardened in Stage 5 is reused by detail surfaces.

---

## 5. Recommended plan changes

1. **Re-tier the Stage-5 backlog and remove Views-coupled dialogs.**
   - Move `FwFindReplaceDlg` and `FwStylesDlg` out of Stage 5; re-parent to **Stage 9** (or a Stage-9-gated
     sub-epic). Update §6 Stage 5's "FwCoreDlgs first" list to drop "Find/Replace" and "Styles" and add a note
     that those two are Views-engine-coupled and Stage-9-gated.
   - Split the remaining list into **Tier A (junior)**, **Tier B (mid: wizard + large structured dialogs:
     New-Project wizard, WS setup, Project properties, Valid-Characters)**. The §8 staffing model already
     allows mid devs; reflect the Tier-B mid assignment in Stage 5 rather than calling the whole stage junior.

2. **Make the host-wrapped-content contract the first rule of the Stage-5 epic**, with a one-paragraph code
   template (WinForms `Form` host + `WinFormsAvaloniaControlHost` + Avalonia body view-model). Cite
   `dialog-ownership.md` rule 4 inline so the "no Avalonia modal window" constraint is unmissable for juniors.

3. **Add the dialog-authoring stack to Stage 1's deliverables and gate.** Explicitly: CommunityToolkit.Mvvm
   package reference, compiled-bindings build setting, the first `.axaml` in the repo, and the dialog
   scaffolding generator (distinct from the region generator). Note in §11 decision 3 that this is the
   program's first XAML/MVVM-toolkit adoption (the as-built Avalonia work is all code-behind).

4. **State the code-behind-control exception to "compiled bindings everywhere"**: dialog layout is XAML +
   compiled bindings; proven owned controls (`FwMultiWsTextField`, `FwOptionPicker`) embed as code-behind
   child elements. Otherwise the rule is either violated or forces rewriting proven controls.

5. **Annotate the §5 `S5 → S11` edge** as "promote hosted dialog bodies to native Avalonia modal windows"
   (forward/finishing edge, not a block), to kill the "is Stage 5 blocked by Stage 11?" misreading.

6. **Reuse the BackupRestore MVP set as the worked example for the runbook.** It already has
   Presenter/`IBackupProjectView` separation (`BackupProjectPresenter.cs`, `RestoreProjectPresenter.cs`) —
   the cleanest existing target for a first junior conversion to MVVM, and a good "before" for the parity
   bundle.

---

## 6. Open questions & risks

- **Q (tooling):** Does compiled-bindings + CommunityToolkit.Mvvm + a `.axaml` dialog build cleanly on **net48**?
  The as-built FwAvalonia project is `net48` and Avalonia 11.3.17. Compiled bindings work on 11.x, but this
  combination is unproven in this repo and must be de-risked in Stage 1 before any junior touches it.
- **Q (host shell ownership):** When a Stage-5 dialog is itself launched from *another* WinForms dialog (not
  from the Avalonia surface), what owns the host `Form`? `dialog-ownership.md` assumes the launcher is the
  Avalonia surface. The common WinForms→WinForms dialog chain needs an owner rule too.
- **Risk (hallucinated parity at volume):** 4–6 junior streams × ~150 Tier-A/B dialogs is the program's
  largest exposure to the §9 #1 risk. Compiled bindings + mandatory parity bundle + evidence-vocabulary
  enforcement + a dedicated integration owner are the mitigations; do not relax the evidence gate for "simple"
  dialogs.
- **Risk (shared-control churn):** `FwOptionPicker`/`FwMultiWsTextField` will be exercised hard and may need
  changes mid-stage; a control change touches many dialogs at once (merge contention + re-verification). Freeze
  the owned-control API early or funnel changes through one owner.
- **Risk (mis-tiering recurs as new domain dialogs arrive):** §6 says "domain dialogs across xWorks/LexText/
  FdoUi as their owning areas migrate" — those areas (esp. LexText, 45 Forms) include more Views-coupled
  dialogs. Each incoming domain dialog needs the same Views-coupling triage before junior assignment.

## 7. Confidence

**High** on the central resolution (modal-during-coexistence is solved by host-wrapped WinForms-`Form` +
Avalonia body; Stage 5 is **not** blocked by Stage 11), the repo facts (counts, LOC, Views coupling, absence
of XAML/MVVM-toolkit), and the Find/Replace + Styles mis-tiering.
**Medium** on exact effort tiering of the mid band and on the net48 compiled-bindings build (flagged as the
key Stage-1 de-risk). All claims are grounded in cited repo paths and the frozen architecture docs.
