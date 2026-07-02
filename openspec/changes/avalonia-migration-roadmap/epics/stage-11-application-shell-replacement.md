# Stage 11 — Application shell replacement (Epic draft)

> JIRA-ready epic + sub-epic draft for **Stage 11** of the FieldWorks → Avalonia complete
> migration program. Source of truth: `complete-migration-program.md` (§6 Stage 11 + Track IV
> callout; §7 Definition of Done; §10 JIRA structure/labels), the Stage 11 review
> (`reviews/stage-11-application-shell-replacement.md`), the cross-comparison synthesis
> (`reviews/00-cross-comparison-synthesis.md` §3/§6/§7), and the existing OpenSpec change whose
> body **is** this stage: `fieldworks-avalonia-shell-migration` (proposal/design/tasks/specs).
>
> **This epic does not contradict `fieldworks-avalonia-shell-migration`** — it is the JIRA
> projection of that change. Where this draft and the change appear to differ, the change wins;
> open questions are flagged in the final section.

---

## Epic

**Summary:** Replace the WinForms/XCore application shell with an Avalonia default shell —
application lifetime, windowing, navigation, menus/toolbars/status, typed shell composition from
`Main.xml`, command/state bridging, an area-by-area main-screen registry, and the final
default-switch + WinForms/FlexUIAdapter decommission from the default path.

**Type:** Epic

**Labels:** `track-shell`, `lead-senior`, `parity-blocked-by:IXCoreCommandBridge`,
`parity-blocked-by:dialog-owner-port`, `av12-delta-localized`
*(label vocabulary per program §10: `track-foundation|track-surfaces|track-longpole|track-shell`,
`lead-junior|mid|senior`, `parallel-safe`, `parity-blocked-by:<seam>`)*

**Description:**

Stage 11 is the IV-Shell stage and the critical path for making FieldWorks a true Avalonia
application rather than an Avalonia island inside the legacy WinForms host. Its body is the
existing `fieldworks-avalonia-shell-migration` change (10 task groups, ~20 ADDED requirements).
That change is a second migration program in its own right, so this epic decomposes it into six
independently gateable sub-epics (**11a–11f**) per synthesis §6, matching the existing change's
natural task-group seams.

The shell coupling is real and deep, and the work is senior-only / multi-quarter:

- The process entry point is WinForms-bound: `Src/Common/FieldWorks/FieldWorks.cs` runs
  `[STAThread] static int Main` → `Application.Run()`, threading `Form.ActiveForm` /
  `Form dialogOwner` through ~15 project-lifecycle methods (`OpenExistingProject`,
  `ChooseLangProject`, `CreateNewProject`, `BackupProject`, `RestoreProject`,
  `ArchiveProjectWithRamp`, `HandleRestoreRequest`, …).
- `Src/XCore/xWindow.cs` is a **2,498-line `: Form`** holding `CollapsingSplitContainer`,
  `RecordBar`, sidebar, `StatusBar`, the `IUIAdapter` rebar/sidebar/menubar adapters, and a
  `Mediator`/`PropertyTable`. `FwXWindow` (`Src/xWorks/FwXWindow.cs`, 2,588 lines) derives from it.
  Replacement cannot be incremental within one window instance — a **parallel Avalonia main window
  + area-by-area screen migration** is the only viable path (Strangler Fig at the area/tool
  boundary).
- Because `XWindow : Form` and `Application.Run()` pin the whole process to **net48 / Avalonia
  11.x** through all of Stage 11 (one CLR per process), every line of Stage 11 Avalonia code must
  be written **Av12-delta-localized** (confine unavoidable Avalonia-11-only APIs — clipboard,
  binding, focus, theming — to named seams) so the 11 → 12 runtime jump ports surviving code, not
  rework.

**Stage 2 / Stage 11 split (state explicitly):** Stage 2 = *contract / port design + seam
stand-up* (the lifetime, main-window, active-window-registry, dialog-owner, dispatcher, shutdown,
modal-state ports, plus `IXCoreCommandBridge`); Stage 11 = *implementation + shell-scoping +
default switch*. Stage 11 **consumes and shell-scopes** those ports — it does **not** redefine
`IUiScheduler` / `IRegionLifetime` / the lifetime/command contracts (already in
`Src/Common/FwAvalonia/Seams/ISeams.cs`).

**Stage 5 → 11 is a *finishing* edge, not a block.** On the coexistence path (Avalonia 11.x + .NET
FW 4.8) Avalonia modal windows are **not** supported; the only supported modal pattern is a
WinForms `Form` owning embedded Avalonia content. Stage 5 therefore ships Avalonia dialog *content*
inside WinForms-owned modal forms and is **not** blocked by Stage 11. True Avalonia
`Window.ShowDialog` modality only becomes available once the Avalonia desktop lifetime owns the
main window — i.e. at the Stage 11 default switch — so flipping Stage-5 content from WinForms-owned
to `Window`-owned modality is a **distinct 11a task**, not free.

**Acceptance criteria (epic-level):**

- All six sub-epics (11a–11f) closed with their own gates green.
- All `fieldworks-avalonia-shell-migration` task groups (1–10) and ADDED requirements satisfied;
  spec assertions hold: *Windowing uses framework-neutral lifetime services*; *Avalonia owns final
  desktop lifetime*; *Hidden WinForms startup is disallowed*; *Shutdown is deterministic*; *Shell
  XML imports into a typed shell definition*; *Unsupported constructs are diagnostic, not silently
  omitted*; *Typed definition is the runtime target*; *Commands are typed descriptors*; *XCore
  mediator bridges*; *Command parity validated*; *Screen registry + per-screen manifest*; *legacy
  content explicit and temporary*; *Full-app smoke gates protect the default switch*.
- **Command-target / shortcut parity tests** pass (not merely a launch-and-click smoke test —
  guard against "hallucinated parity at shell scale", §7 DoD + `parity-evidence.md`).
- Avalonia shell is the **default** only after hard gates pass; WinForms shell default path,
  FlexUIAdapter default dependency (~3.2k lines), WinForms dynamic content host, retired dialogs,
  and obsolete shell-XML runtime pieces removed from the default path.
- Per-surface §7 Definition of Done satisfied for each migrated screen (semantic + visual +
  workflow + performance bundle; AutomationIds; localization lanes; `EngineIsolationAuditTests`
  green; `./build.ps1` + `./test.ps1` green); evidence-language enforcement honored.
- All new shell code is Av12-delta-localized; an exit gate confirms Avalonia-11-only APIs are
  confined to named seams.

**Dependencies:** Stage 2 (ownership ports + `IXCoreCommandBridge` seam — single source of truth);
"enough of 5–8" Avalonia (areas enumerated in 11e); reuses the `Src/Common/FwAvalonia/ViewDefinition/`
compiler pipeline (Stage 0/4); blocks **Stage 12** (runtime jump cannot start until the WinForms
host is gone). Stage 5 → 11 is a finishing edge. Texts & Words / Interlinear (Stage 7,
Views-engine-heavy, depends on Stage 9) is the most likely **legacy island** at first switch.

**Rough size:** **XL** (a second migration program; multi-quarter, senior-led).

---

## Sub-epics / stories

> Internal dependency shape (synthesis §6/§5, review §5.1): **11a (lifetime) + 11b (compiler) +
> 11c (command bridge) are foundational and can run in parallel**; **11d (composition) depends on
> 11b/11c**; **11e (screens) depends on 11d** + per-area Track-II completion; **11f (default switch
> + FlexUIAdapter removal) is last**. So: `11a ∥ 11b ∥ 11c → 11d → 11e → 11f`.

### 11a — App lifetime & windowing  *(critical path)*

**Summary:** Stand up the Avalonia desktop lifetime, main-window ownership, active-window registry,
multi-window behavior, modal owner, and deterministic shutdown/disposal; re-home the ~15 `Form
dialogOwner` lifecycle seams behind the framework-neutral ports; flip already-migrated Stage-5
dialog content from WinForms-owned to `Window`-owned modality at the switch.

**Type:** Sub-epic / Story

**Description:** This is the **long pole inside Stage 11**. Every `Form`-typed lifecycle seam in
`FieldWorks.cs` (`OpenExistingProject`, `ChooseLangProject`, `CreateNewProject`, `BackupProject`,
`RestoreProject`, `ArchiveProjectWithRamp`, `HandleRestoreRequest`, …) must move behind the Stage-2
dialog-owner / lifetime ports before an Avalonia `Main` can exist. Implement classic Avalonia
desktop lifetime + explicit `Window` ownership over those ports; preserve `FieldWorks.cs`
`s_activeMainWnd` / `Form.ActiveForm as IFwMainWnd` multi-project / multi-window semantics in an
Avalonia active-window registry. Add the explicit **dialog-modality re-host** task: migrate the
Stage-5 WinForms-owned Avalonia dialog *content* to Avalonia `Window`-owned modality — the single
point where true Avalonia modal windows appear — tied to the full-app smoke gate.

**Acceptance criteria:**
- Framework-neutral lifetime services own windowing; no `Form`/`Control` requirement remains in the
  default shell path; *hidden WinForms startup is disallowed*; *Avalonia owns final desktop
  lifetime*.
- Active-window registry preserves multi-project / multi-window semantics; shutdown/disposal of
  windows, caches, dialogs, background services, and retained native services is deterministic.
- Stage-5 dialog content re-hosted to `Window`-owned modality and passing owner/modal,
  cancellation, focus-return, accessibility, and localization tests behind the full-app smoke gate.
- Avalonia.Headless tests for dialog ownership / focus traversal; contract tests for startup,
  active-window tracking, dialog ownership, shutdown, UI dispatch.

**Dependencies:** Stage 2 ownership ports (consumes, does not redefine). Parallelizable with
11b/11c. The dialog-modality re-host depends on Stage 5 content existing (finishing edge 5 → 11).

**Cross-reference (`fieldworks-avalonia-shell-migration` tasks):** **2** (Shell Contracts — 2.1–2.4,
the port consumption side), **5.2/5.4** (main window, app lifetime, dialog-ownership headless),
**10.1–10.2** (Avalonia app startup path; shutdown/disposal tests). Spec: lifetime/windowing/shutdown/
hidden-startup requirements + dialog re-host (review §1, §4 action 3).

**Labels:** `track-shell`, `lead-senior`, `parity-blocked-by:dialog-owner-port`, `av12-delta-localized`

**Rough size:** **L–XL** (review confidence Medium on sizing — warrants its own spike).

---

### 11b — Main.xml typed-shell compiler

**Summary:** Build the typed shell-definition importer/compiler for `Main.xml` + area/tool XML
includes, with diagnostics for unsupported constructs and deterministic snapshot tests; reuse the
proven `ViewDefinition/` pipeline.

**Type:** Sub-epic / Story

**Description:** The *tractable* part of Stage 11. `DistFiles/Language Explorer/Configuration/Main.xml`
is ~989 lines with 118 `<include>`s pulling 51 config XMLs (~15.7k lines). The command/choice model
already lives in typed C# (`xCoreInterfaces/Command.cs`, `ChoiceGroup.cs`, `Inventory.cs`), so the
compiler imports XML into types that **already have a runtime peer**. Reuse — do not reinvent — the
cache-keyed, off-thread, deterministic-snapshot machinery in `Src/Common/FwAvalonia/ViewDefinition/`
(same bet proven by the Lexical-Edit view-definition compiler). Represent commands, lists, areas/
tools, menus, context menus, toolbars, status panes, shortcuts, icons, localization metadata, and
screen registrations. Keep XML as **import/audit input only** — the typed definition is the runtime
target. Diagnostics, not silent omission, for unsupported commands/listeners/dynamic-loaders/
toolbar-widgets/status-panels/extension constructs.

**Acceptance criteria:**
- `Main.xml` + includes import into a typed shell definition; unsupported constructs raise
  diagnostics (never silently dropped); typed definition is the runtime target; localization
  survives the import.
- Deterministic shell-definition snapshot tests pass; compiler shares the `ViewDefinition/`
  cache-key / off-thread / snapshot pipeline.

**Dependencies:** Reuses Stage 0/4 `ViewDefinition/` pipeline. Parallelizable with 11a/11c; **blocks
11d**.

**Cross-reference (`fieldworks-avalonia-shell-migration` tasks):** **3** (Typed Shell Composition —
3.1 importer, 3.2 representation, 3.3 diagnostics, 3.4 snapshot tests). Spec: *Shell XML imports into
typed shell definition*; *Unsupported constructs are diagnostic*; *Typed definition is the runtime
target*; *localization survives*.

**Labels:** `track-shell`, `lead-senior`, `parallel-safe`, `av12-delta-localized`

**Rough size:** **M–L** (bounded, high transfer confidence; the "easy" sub-epic).

---

### 11c — Command / state bridge

**Summary:** Define typed command descriptors and bridge XCore mediator/property-table behavior into
Avalonia commands + active-target routing, with command-parity validation. Promotion of an existing
seam, not green-field.

**Type:** Sub-epic / Story

**Description:** `IXCoreCommandBridge` and `IRecordNavigationContext` already exist in
`Src/Common/FwAvalonia/Seams/ISeams.cs` and are consumed at the region edge
(`Src/xWorks/RecordEditView.cs`, `RecordClerkNavigationContext.cs`); `seam-catalog.md` §1 reserves
*"shell-scope wiring happens in the shell phase, not per region."* This sub-epic is the documented
**promotion** of that seam to shell-global scope. Define typed command descriptors with stable IDs,
labels, gestures, icons, visibility, enabled state, target resolution, and diagnostics; bridge XCore
mediator handlers + property-table state into Avalonia commands and active-target routing; preserve
command IDs/shortcuts as the stable contract so menus/toolbars/context-menus can be re-skinned
(Fluent restyle, program decision §11.4) while behavior parity is asserted by **command-target
tests, not pixels**.

**Acceptance criteria:**
- Typed command descriptors expose stable IDs/labels/gestures/icons/visibility/enabled/target
  resolution; XCore mediator/property-table bridged; one-at-a-time ("one-shot") commands honored.
- **Command parity validated** by command-enable/visible/shortcut/target-selection/mediator-bridge
  tests (the shell-scale anti-hallucination gate).
- Menu / context-menu automation metadata + localization checks pass.

**Dependencies:** Stage 2 `IXCoreCommandBridge` seam (promotes, does not redefine). Parallelizable
with 11a/11b; **feeds 11d**.

**Cross-reference (`fieldworks-avalonia-shell-migration` tasks):** **4** (Command Routing and State —
4.1 descriptors, 4.2 bridge, 4.3 parity tests, 4.4 automation/localization). Spec: *Commands are
typed descriptors*; *XCore mediator bridges*; *Command parity validated*.

**Labels:** `track-shell`, `lead-senior`, `parallel-safe`, `parity-blocked-by:IXCoreCommandBridge`,
`av12-delta-localized`

**Rough size:** **M** (seams already seeded).

---

### 11d — Shell composition / navigation & panes

**Summary:** Build owned Avalonia controls replacing `SilSidePane`/OutlookBar,
`CollapsingSplitContainer`/`MultiPane`, `PaneBarContainer`, `RecordBar`; render menus/context-menus/
toolbars/status panels; implement split/side panes, record-list region, collapse/restore, and layout
persistence.

**Type:** Sub-epic / Story

**Description:** Owned-control work comparable in weight to the Stage-3 grid build — none of these
WinForms controls (`SilSidePane/` OutlookBar + ~20 files, `MultiPane.cs` 674 lines,
`CollapsingSplitContainer.cs` 667 lines, `PaneBarContainer`, `RecordBar`) have stock Avalonia
peers. Implement the Avalonia shell skeleton: main-window navigation regions, content host,
status/progress region, diagnostics hooks, theme resources, accessibility root metadata. Render menu
and context-menu structures (labels, shortcuts, icons, separators, extension items, visibility,
enablement); standard/format/insert/view toolbars (including writing-system + style selectors);
status panels (message, progress, area, sort, filter, parsing, record number); split/side panes +
record-list region with collapse/restore + layout persistence. **Spike the build-vs-docking-library
decision alongside Stage 3** and record it with a pivot trigger (a docking library only if owned
controls cannot meet documented FieldWorks workflows — risk of under-scoping). Run the shell in
preview/sample mode before LCModel project startup.

**Acceptance criteria:**
- Owned controls replace SilSidePane/MultiPane/CollapsingSplitContainer at parity; menus/toolbars/
  status/layout render with full label/shortcut/icon/visibility/enablement fidelity.
- Layout persistence + collapse/restore behavior preserved; theme resources + accessibility root
  metadata present; AutomationIds on every control (mandatory, day one).
- Avalonia.Headless tests for shell creation, navigation host swapping, command dispatch, status
  updates, focus traversal, pane state.
- Build-vs-library decision recorded with a fired pivot trigger if a docking library is adopted.

**Dependencies:** **Depends on 11b (typed definition) + 11c (command bridge).** Spike alongside
Stage 3 owned-control work. Feeds 11e.

**Cross-reference (`fieldworks-avalonia-shell-migration` tasks):** **5** (Avalonia Shell Skeleton —
5.1–5.4), **6** (Navigation — partial, 6.1–6.2 navigation host), **7** (Menus, Toolbars, Status,
Layout — 7.1–7.5, incl. the docking-library evaluation in 7.5).

**Labels:** `track-shell`, `lead-senior`, `av12-delta-localized`

**Rough size:** **L–XL** (custom-control build; review confidence Medium on sizing — spike).

---

### 11e — Screen registry & area-by-area migration

**Summary:** Map area/tool IDs from the typed shell definition to an Avalonia screen registry;
implement area/tool navigation + persisted `areaChoice`/`currentContentControl` compatibility; add a
per-screen manifest; migrate main screens area by area with explicit, temporary legacy islands for
the rest.

**Type:** Sub-epic / Story

**Description:** Strangle the shell at the area/tool boundary, running two main windows side by side.
Each migrated screen gets a manifest (entry points, shell commands, state, native-boundary status,
accessibility, performance, rollback, default-switch gates); non-migrated screens render as
**explicit, temporary legacy islands** (never silent fallback). **Enumerate the area gate
concretely:** which areas must be Avalonia before the default switch (Lexicon first, then Words/
Grammar/Notebook/Lists) and which may remain explicit legacy islands. Texts & Words / Interlinear
(Stage 7, Views-engine-heavy, depends on Stage 9) is the most likely first-switch legacy island —
call it out in the manifest. Add memory-project + sample-project navigation tests. Confirm no
migrated Avalonia screen transitively pulls a `BarAdapterBase`/`MenuAdapter` during coexistence
(active-host contract should catch this).

**Acceptance criteria:**
- Area/tool IDs map to an Avalonia screen registry; `areaChoice`/`currentContentControl`
  persistence compatible; each migrated screen has a manifest with all required gate rows.
- Legacy content for non-migrated areas is explicit and temporary (manifested), never a silent
  fallback; the first-switch legacy-island list (incl. Interlinear) is enumerated.
- Memory-project + sample-project navigation tests pass; active-host contract confirms no hidden
  DataTree/Views/FlexUIAdapter pull on migrated screens.

**Dependencies:** **Depends on 11d** + per-area Track-II completion ("enough of 5–8"). Feeds 11f.

**Cross-reference (`fieldworks-avalonia-shell-migration` tasks):** **6** (Navigation and Screen
Registry — 6.1–6.4), **9** (Main Screen Migration — 9.1 Lexicon, 9.2 Words/Interlinear, 9.3 Grammar/
Morphology, 9.4 Notebook, 9.5 Lists, 9.6 preview/print/browser-PDF isolation). Spec: *screen
registry*; *each migrated screen has a manifest*; *legacy content explicit and temporary*.

**Labels:** `track-shell`, `lead-senior`, `av12-delta-localized`

**Rough size:** **L** (per-area; absorbs Track-II head-count).

---

### 11f — Startup / installer / default-switch + FlexUIAdapter removal

**Summary:** Land the Avalonia startup path, installer/runtime packaging + dependency harvest, the
feature-flag/default selector, full-app smoke gates; flip the default to Avalonia only after hard
gates pass; remove the WinForms shell default path, FlexUIAdapter default dependency (~3.2k lines),
WinForms dynamic content host, retired dialogs, and obsolete shell XML from the default path.

**Type:** Sub-epic / Story

**Description:** The last sub-epic — the irreversible-feeling default flip and the decommission of
the legacy default path. Add the Avalonia app startup path (project selection, cache creation,
splash/safe-mode, remote-request listener, no-UI/app-server modes, update checks accounted for);
update installer/runtime packaging + dependency harvest for Avalonia shell assets; add the feature-
flag / default selector for the Avalonia shell; run full local build/test + app smoke gates;
**make Avalonia default only after hard gates pass.** Then remove the WinForms shell default path,
FlexUIAdapter default dependency (`Src/XCore/FlexUIAdapter/`, ~3.2k lines across Menu/Bar/Sidebar/
Toolbar/PaneBar adapters), WinForms dynamic content host, retired dialogs, and obsolete shell-XML
runtime pieces. Deferring FlexUIAdapter removal until **after** the default switch is correct; before
removal, confirm no migrated Avalonia screen transitively pulls `IUIAdapter` during coexistence.
Revisit heavier reactive/region-framework alternatives only if pivot triggers in
`lexical-edit-avalonia-migration/seam-recommendations.md` are met.

**Acceptance criteria:**
- Avalonia startup path covers project selection / cache / splash / safe-mode / remote listener /
  no-UI / app-server / update checks; installer + dependency harvest updated for Avalonia assets;
  feature-flag/default selector present.
- Full local build/test + **full-app smoke gates pass before** the default switch; command-parity
  evidence (not just launch-and-click) is the gate.
- Avalonia is default; WinForms shell default path, FlexUIAdapter default dependency, WinForms
  dynamic content host, retired dialogs, and obsolete shell-XML runtime pieces removed from the
  default path (WinForms may remain only as explicit, manifested legacy islands per 11e until their
  areas migrate).

**Dependencies:** **Last — depends on 11a–11e** and "enough of 5–8". Blocks **Stage 12** (the
WinForms host must be gone before the .NET 10 + Avalonia 12 jump).

**Cross-reference (`fieldworks-avalonia-shell-migration` tasks):** **10** (Startup, Shutdown,
Installer, and Default Switch — 10.3 packaging, 10.4 feature flag, 10.5 smoke gates, 10.6 default
switch, 10.7 WinForms/FlexUIAdapter removal, 10.8 pivot-trigger revisit). Spec: *Full-app smoke gates
protect the default switch*.

**Labels:** `track-shell`, `lead-senior`, `av12-delta-localized`

**Rough size:** **L** (default switch + ~3.2k-line decommission; gated, last).

---

## Notes / open questions

**Relationship to `fieldworks-avalonia-shell-migration`.** This epic is the JIRA projection of that
OpenSpec change; the change's `proposal.md` / `design.md` / `tasks.md` / `specs/` remain the
authoritative body. The 11a–11f decomposition maps onto the change's 10 task groups (11a→2/5/10.1–2,
11b→3, 11c→4, 11d→5/6/7, 11e→6/9, 11f→10). Per program §10, existing OpenSpec changes map onto
epics — `fieldworks-avalonia-shell-migration → Stage 11`. Do not let this draft drift from the change;
if the change updates, re-sync this file.

**Stage 2 / Stage 11 split (restated for backlog hygiene).** Stage 2 = ports/contracts (lifetime,
main-window, active-window registry, dialog owner, dispatcher, shutdown, modal state +
`IXCoreCommandBridge`) and seam stand-up. Stage 11 = implement + shell-scope + default switch. Stage
11 must not be read as re-creating the lifetime/command contracts. Reuse `IUiScheduler` /
`IRegionLifetime` from `ISeams.cs`.

**Av12-delta-localized constraint (program §1a / §5; synthesis §2; Stage 12 review).** "Av12-ready
during coexistence" is **not literally achievable** — code running on Avalonia 11.x / net48 cannot
avoid all 11-only APIs. The achievable posture, and the one this epic adopts, is **confining
Avalonia-11-only APIs (clipboard `IDataObject`→`IAsyncDataTransfer`, binding, focus, theming) to
named seams**, enforced by a Stage 2/11 exit gate. `XWindow : Form` + `Application.Run()` pin the
process to net48/Av11 through all of Stage 11; Stage 12 (the .NET 10 + Avalonia 12 jump) cannot start
until 11f removes the WinForms host. Every sub-epic carries the `av12-delta-localized` label.

**Dialog ordering (5 → 11) — verified, not inverted.** Stage 5 dialogs ship as WinForms-owned
Avalonia *content* first; Avalonia-native modality is a Stage 11 *output* (the 11a re-host task), used
after the shell exists. The dependency arrow is **5 → 11** (a finishing edge), not 11 → 5.

**Open questions (each gates a sub-epic — assign resolution owners):**
- **OQ1 runtime target** → 11a / Stage 12 (Av12-delta-localized seams; net48 pin until 11f).
- **OQ-B docking library vs owned controls** → 11d (spike alongside Stage 3; record build-vs-library
  decision + pivot trigger; risk of under-scoping 11d).
- **OQ-C partner / third-party `Main.xml` extension hooks** → 11b (diagnostics-not-silent-omission is
  the safety net, but a partner extension with no Avalonia equivalent is a hard blocker for *their*
  default switch — needs a policy).
- **OQ-D multi-window / active-window registry parity** → 11a (preserve `s_activeMainWnd` /
  `Form.ActiveForm as IFwMainWnd` multi-project semantics; under-tested in current tasks — only 6.4
  nav tests; review confidence Medium-low).
- **OQ5 first-switch blocking screens** → 11e gate (enumerate Avalonia-required areas vs legacy
  islands; Interlinear/Stage 7 likely the first island).

**Risks (program §9 + review §6):** shell pulled too early (mitigation: hard, enumerated "enough of
5–8" gate in 11e); "hallucinated parity" at shell scale (mitigation: command-target/shortcut parity
tests as the gate, not launch-and-click); FlexUIAdapter half-life during coexistence (mitigation:
active-host contract catches transitive `IUIAdapter` pulls).
