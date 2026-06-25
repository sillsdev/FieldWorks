# Stage 5 — Global dialogs & choosers (Epic draft)

> **Dialog-authoring decision (2026-06-15): MVVM is chosen.** Dialogs use Avalonia XAML +
> CommunityToolkit.Mvvm + compiled bindings, authored in a dedicated XAML-enabled project
> (`FwAvaloniaDialogs`); the `FwAvalonia` foundation stays pure-C#. Stage 5 is gated only on the
> one-time net48 XAML-compiler/MSBuild integration spike (Stage 1.2 first task) — no longer on an
> open architectural decision.
>
> JIRA-ready draft for **Stage 5** of the FieldWorks → Avalonia migration program.
> Grounded in `complete-migration-program.md` (§4 row 5, §6 Stage 5 + Post-review, §7 Definition
> of Done, §10 JIRA structure/labels, §11.3 decision), `reviews/stage-05-global-dialogs-choosers.md`,
> and `reviews/00-cross-comparison-synthesis.md` (§3 conflicts, §6 sub-epic map, §7 edges).
>
> **Authoritative structure:** synthesis §6 — `5A junior` / `5B mid` / `5C → Stage 9`.
> **Hard contract (first rule):** host-wrapped Avalonia *body* inside a WinForms-owned `Form`; no
> `Window.ShowDialog` during coexistence (`dialog-ownership.md` rule 4).

---

## Epic

- **Summary (JIRA title):** Stage 5 — Migrate global dialogs & choosers to host-wrapped Avalonia (MVVM)
- **Type:** Epic
- **Labels:** `track-surfaces`, `lead-junior` (epic skews junior; carries a mid sub-track in 5B),
  `parallel-safe`, `parity-blocked-by:dialog-ownership`, `dependency:stage-1-dialog-mvvm-kit`,
  `dependency:stage-2-host`
- **Description:** Migrate FieldWorks' ~200 global dialogs and choosers (FwCoreDlgs first, then shared
  chooser/launcher infrastructure, then domain dialogs across xWorks/LexText/FdoUi) from WinForms to
  Avalonia. Every "migration" replaces the dialog **body** with an Avalonia view authored in
  CommunityToolkit.Mvvm + compiled bindings (§11.3), hosted via `WinFormsAvaloniaControlHost` inside a
  thin WinForms `Form` shell that still owns `ShowDialog`/`DialogResult`/modality — there are **no
  Avalonia modal windows during coexistence**. This is the program's largest single block of work and
  its primary junior+Claude reservoir, fanned out across 4–6 parallel file-owning streams. The backlog
  is **re-tiered** so juniors only receive small, Views-free dialogs; large/wizard dialogs go to mid
  devs; and the two Views-engine-coupled dialogs leave Stage 5 entirely.
- **Acceptance criteria:**
  - Host-wrapped-body contract is honored for every migrated dialog: WinForms `Form` owns modality;
    Avalonia body hosted via `WinFormsAvaloniaControlHost`; **no** `new Window().ShowDialog(owner)`
    (`dialog-ownership.md` rule 4). Focus-return contract (rule 2) inherited from the host template.
  - Each migrated dialog satisfies the §7 per-surface Definition of Done (parity bundle captured before
    refactor + matched after; AutomationIds nonlocalized / Names localized; seams reused;
    `EngineIsolationAuditTests` + `./build.ps1` + `./test.ps1` green; retrospective in the same PR).
  - Evidence-language gate enforced: a checked box noted *substitute/placeholder/skipped/future/partial*
    is a review blocker; the integration owner spot-checks junior parity bundles.
  - Localization lanes correct: field labels via StringTable; product strings via `FwAvaloniaStrings.resx`;
    no hardcoded UI strings (AGENTS.md hard rule). `fieldworks-localization-review` run per dialog.
  - `FwFindReplaceDlg` and `FwStylesDlg` are **out of scope** here (re-parented to Stage 9 — see 5C).
  - One dialog per PR; net-new `.axaml` + view-model + host `Form` files; call-site swaps deferred to the
    per-milestone integration step (principles 10/11).
- **Dependencies (epic links):**
  - **Blocked by Stage 1** — specifically `1-dialog-mvvm-kit` (CommunityToolkit.Mvvm package ref,
    compiled-bindings build setting, first `.axaml`, dialog scaffolding generator distinct from the region
    generator, dialog-flavored evidence bundle). The repo today has **zero `.axaml` and no
    CommunityToolkit.Mvvm**; this stack must exist before any junior starts.
  - **Blocked by Stage 2** — generalized `WinFormsAvaloniaControlHost`-based host + the dialog-ownership
    contract layer (`Form` owner, dispatcher, modal-state ports). Reuse, don't redefine.
  - **Downstream:** Stage 8 (shared chooser/launcher infra hardened here), Stage 6 (`S5 → S6`: every
    MSA/feature launcher opens a Stage-5 dialog body).
  - **Forward/finishing edge (NOT a block):** `S5 → S11` — Stage 11 later strips the WinForms wrapper and
    promotes hosted bodies to native Avalonia modal windows once the shell owns modality.
- **Rough size:** **XL** (~200 surfaces; largest single block; ~150 Tier-A/B after 5C removal).

---

## Sub-epics / stories

### 5A — Junior dialog stream (small, Views-free dialogs)  *(junior)*

- **Summary:** Stage 5A — Migrate small Views-free dialogs to host-wrapped Avalonia/MVVM (junior streams)
- **Type:** Story (epic-feature; spawns one issue per dialog)
- **Description:** The genuinely mechanical, junior-safe reservoir: small modal/warning dialogs and simple
  choosers under ~600 LOC with **no `IVwRootSite`/`SimpleRootSite` coupling**. Each dialog's body becomes
  an Avalonia MVVM view (CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` + compiled bindings)
  hosted in a thin WinForms `Form`. Owned controls (`FwMultiWsTextField`, `FwOptionPicker`) embed as
  code-behind child elements — the code-behind exception to "compiled bindings everywhere" (review §2c).
  Run as 4–6 parallel file-owning streams; one dialog per PR. The **BackupRestore MVP set** (already has
  `BackupProjectPresenter`/`RestoreProjectPresenter` + `IBackupProjectView`) is the worked runbook example
  and the first junior conversion.
- **Acceptance criteria:**
  - Per dialog: full §7 Definition of Done (semantic + visual + workflow + perf parity bundle captured
    before refactor and matched; perf ≤ legacy × 1.2 at 100% **and** 150% DPI; AutomationIds nonlocalized,
    Names localized; seams reused; audit + build + test green; retrospective same PR).
  - Host-wrapped-body contract honored; **no `Window.ShowDialog`**; focus-return inherited from template.
  - Evidence-vocabulary gate enforced with mandatory integration-owner spot-check (juniors at volume are
    the program's #1 exposure to "hallucinated parity").
- **Example concrete dialog issues (Tier A):**
  1. `FwChooserDlg.cs` (396 LOC) — simple chooser; exercises the `FwOptionPicker` reuse path end-to-end.
  2. `FwStylesModifiedDlg.cs` (58 LOC) — trivial modal; ideal first/onboarding issue.
  3. `MoveOrCopyFilesDlg` / `AddNewVernLangWarningDlg` / `DeleteWritingSystemWarningDialog` /
     `MissingOldFieldWorksDlg` — small warning/confirmation modals (any one is a self-contained issue).
- **Dependencies:** Stage 1 (`1-dialog-mvvm-kit`), Stage 2 (host + ownership contract). Within 5A,
  BackupRestore conversion lands first as the runbook exemplar.
- **Labels:** `track-surfaces`, `lead-junior`, `parallel-safe`, `parity-blocked-by:dialog-ownership`
- **Rough size:** **L** (high count, individually small; the bulk of the parallel head-count).

### 5B — Mid dialog/wizard stream (large structured dialogs + wizard)  *(mid)*

- **Summary:** Stage 5B — Migrate large structured dialogs & the New-Project wizard to Avalonia/MVVM (mid)
- **Type:** Story (epic-feature; one issue per dialog/wizard)
- **Description:** Structured, large, but **not** Views-coupled dialogs that are explicitly **not
  junior-mechanical** (review §1) — they have or need a model and substantial bespoke logic. Same
  host-wrapped-body + MVVM + compiled-bindings contract as 5A, assigned to mid devs (§8 staffing allows
  it). **Wizards are a distinct sub-track**: `FwNewLangProject` is a multi-step state machine
  (`FwNewLangProjectModel` + `Load*SetupDelegate` callbacks) and needs its own navigation/step view-model
  pattern, not a flat dialog view-model (review §3.9).
- **Acceptance criteria:**
  - Per surface: full §7 Definition of Done (parity bundle before/after, 100% + 150% DPI perf, AutomationId
    + localization lanes, seam reuse, audit/build/test green, retrospective same PR).
  - Existing models reused/extended where present (`FwWritingSystemSetupModel`, `FwNewLangProjectModel`) —
    not re-derived.
  - Wizard step navigation has an explicit step/navigation view-model; back/next/finish and per-step
    validation match legacy workflow parity.
  - Host-wrapped-body + focus-return contract honored; no Avalonia modal window.
- **Surfaces (Tier B):**
  - `FwNewLangProjectModel` / `FwNewLangProject.cs` (359 LOC) — multi-step **wizard** sub-track.
  - `FwWritingSystemSetupDlg.cs` (738 LOC, has `FwWritingSystemSetupModel.cs`).
  - `FwProjPropertiesDlg.cs` (885 LOC).
  - `ValidCharactersDlg.cs` (1,965 LOC) — the largest Tier-B item; consider its own issue + split tasks.
- **Dependencies:** Stage 1 (`1-dialog-mvvm-kit`), Stage 2 (host + ownership). No Views/Stage-9 dependency.
- **Labels:** `track-surfaces`, `lead-mid`, `parallel-safe`, `parity-blocked-by:dialog-ownership`
- **Rough size:** **L** (few surfaces, each large; wizard + 1,965-LOC ValidChars carry most of the weight).

### 5C — Views-coupled dialogs → re-parented to Stage 9  *(NOT junior; NOT Stage-5 work)*

- **Summary:** Stage 5C — Find/Replace + Styles dialogs (Views-coupled; tracked under Stage 9)
- **Type:** Story (tracking/cross-stage; **no migration work executes inside Stage 5**)
- **Description:** `FwFindReplaceDlg.cs` (3,290 LOC, **14** `IVwRootSite`/`SimpleRootSite` references —
  search ops manipulate the live rootbox selection) and `FwStylesDlg.cs` (1,335 LOC, hosts
  `IVwRootSite m_rootSite` for live style preview) embed the native Views engine that Stage 9 replaces.
  They were mislabeled "FwCoreDlgs first" junior targets; per synthesis §3 and review §5 they are
  **re-tiered to Stage 9-gated (Tier C)** and removed from Stage 5. This sub-epic exists only as a
  tracking/redirect record so the cross-stage conflict stays resolved.
- **Acceptance criteria:**
  - These two dialogs are explicitly excluded from 5A/5B and from any junior assignment.
  - Their migration is tracked under **Stage 9** (or a Stage-9-gated sub-epic) and follows the Stage-9
    Definition of Done for Views-coupled document/selection surfaces once the managed selection/caret
    model exists (9.2+).
  - Note: `PicturePropertiesDialog.cs` (620 LOC) has only a cosmetic `SimpleRootSite.ImageNotFoundX`
    reference — it is **not** blocking and may stay in 5A/5B after triage.
- **Dependencies:** **Stage 9** (managed document/text engine — `IVwSelection`/`VwRootBox` replacement).
  Not started until Stage 9 surfaces the needed selection/preview constructs.
- **Labels:** `track-surfaces`, `lead-senior`, `parity-blocked-by:stage-9-views-engine`,
  `cross-stage:moved-to-stage-9`
- **Rough size:** **M** (two dialogs, but Views-coupled and deferred — sized/owned under Stage 9, not here).

---

## Notes / open questions

- **First rule, repeated for juniors:** the dialog body is Avalonia; the *window* is a WinForms `Form` that
  owns `ShowDialog`/`DialogResult`/modality. `Avalonia Window.ShowDialog` against a WinForms owner is "not a
  supported combination on 11.x in this app" (`dialog-ownership.md` rule 4). Bake host shell + focus-return
  into the Stage-1 template so this is inherited, not re-implemented per dialog.
- **Compiled-bindings hybrid (review §2c):** dialog layout/view-model wiring is XAML + `x:CompileBindings`
  (the anti-hallucination build gate); proven owned controls (`FwMultiWsTextField`, `FwOptionPicker`) are
  code-behind and embed as plain child elements. State this exception so the rule is neither violated nor
  forces a rewrite of proven controls.
- **Open Q — net48 build (review §6):** does CommunityToolkit.Mvvm + compiled bindings + a `.axaml` dialog
  build cleanly on **net48 / Avalonia 11.3.17**? Unproven in-repo; must be de-risked in **Stage 1**, before
  any junior touches it. This is a hard precondition on the epic.
- **Open Q — host shell ownership (review §6):** when a Stage-5 dialog is launched from *another WinForms
  dialog* (not from the Avalonia surface), what owns the host `Form`? `dialog-ownership.md` assumes the
  launcher is the Avalonia surface; the WinForms→WinForms chain needs an explicit owner rule. Resolve in
  Stage 2's ownership-port work.
- **Risk — shared-control churn (review §6):** `FwOptionPicker`/`FwMultiWsTextField` get exercised hard;
  a mid-stage API change touches many dialogs at once (merge contention + re-verification). Freeze the
  owned-control API early or funnel changes through one owner.
- **Risk — mis-tiering recurs (review §6):** incoming domain dialogs (esp. LexText, ~45 Forms) include more
  Views-coupled dialogs. Each new domain dialog needs the same Views-coupling triage **before** junior
  assignment — do not assume "domain dialog" == Tier A.
- **Tier triage rule (recorded for the integration owner):** Tier A = junior, simple, Views-free, <~600 LOC;
  Tier B = mid, structured/has-a-model/wizard; Tier C = any `IVwRootSite`/`SimpleRootSite`-coupled → Stage 9.
