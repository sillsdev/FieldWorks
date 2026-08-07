# Stage 11 Review — Application shell replacement (senior)

> Reviewer pass over Stage 11 of `complete-migration-program.md` (the IV-Shell stage whose
> body is the existing `fieldworks-avalonia-shell-migration` change). Grounded in the existing
> change (proposal/design/tasks/spec) and the live repo. File paths are absolute-from-repo-root.

## 1. Scope assessment

**Stage 11 is too large to be a single epic and must decompose.** The roadmap row collapses
the entire `fieldworks-avalonia-shell-migration` change — which itself carries **10 task groups
and ~20 ADDED requirements** — into one line. That change is a full second migration program, not
a stage peer to (say) Stage 5. The natural seams already exist in the existing change's task
groups and should become **sub-epics**:

- **11a — App lifetime & windowing.** Avalonia desktop lifetime, main-window ownership,
  active-window registry, multi-window, modal owner, shutdown/disposal. (shell tasks 2, 10.1–10.2;
  spec "Windowing uses framework-neutral lifetime services", "Avalonia owns final desktop lifetime",
  "Shutdown deterministic", "Hidden WinForms startup disallowed".)
- **11b — Main.xml typed-shell compiler.** Importer + typed model + diagnostics + snapshot tests.
  (shell tasks 3; spec "Shell XML imports into typed shell definition", "Unsupported constructs are
  diagnostic", "Typed definition is the runtime target", "localization survives".)
- **11c — Command/state bridge.** Typed command descriptors + xCore mediator/PropertyTable bridge +
  parity validation. (shell tasks 4; spec "Commands are typed descriptors", "XCore mediator bridges",
  "Command parity validated".)
- **11d — Shell composition / navigation & panes.** Replace `SilSidePane`/OutlookBar,
  `CollapsingSplitContainer`/`MultiPane`; menus/toolbars/status/layout. (shell tasks 5, 6, 7.)
- **11e — Screen registry & area-by-area migration.** (shell tasks 6, 9; spec "screen registry",
  "each migrated screen has a manifest", "legacy content explicit and temporary".)
- **11f — Startup/installer/default-switch + FlexUIAdapter retirement.** (shell tasks 10.)

This is exactly the decomposition the existing change already implies; the roadmap just needs to
say so. Each sub-epic is independently gateable and 11b/11c are largely non-UI and unblock the rest.

**One scope contradiction to fix:** Stage 11's row pulls in "compile Main.xml" and "route mediator
through the typed command bridge," but Stage 2's plan text (§6 Stage 2) already says *"Stand up the
XCore mediator/PropertyTable bridge seam (`IXCoreCommandBridge`)"* and *"Pull forward the contract
layer of `fieldworks-avalonia-shell-migration` (window/dialog ownership contracts)."* The
Stage 2 review confirms the **ports/contracts** are designed in Stage 2 and **implemented** in
Stage 11. The roadmap should state this split explicitly so Stage 11 is not read as re-defining the
lifetime/command contracts — it *implements and shell-scopes* them. (See cross-stage conflicts below.)

## 2. Feasibility (repo-grounded)

The shell coupling is real and deep — feasible but senior-only and multi-quarter:

- **Entry point is WinForms-bound.** `Src/Common/FieldWorks/FieldWorks.cs` is `[STAThread] static int Main`
  → `Application.Run()` (line ~383), with `Form.ActiveForm`/`Form dialogOwner` threaded through ~15
  project lifecycle methods (`OpenExistingProject`, `ChooseLangProject`, `CreateNewProject`,
  `BackupProject`, `RestoreProject`, `ArchiveProjectWithRamp`, `HandleRestoreRequest`, …). The spec's
  "Hidden WinForms startup is disallowed" / "Avalonia owns final desktop lifetime" requirements are
  the hardest part: every one of these `Form`-typed seams must move behind the framework-neutral
  dialog-owner/lifetime ports (Stage 2) before the Avalonia `Main` can exist. **11a is the critical-path
  long pole inside Stage 11.**
- **`XWindow` is `: Form`.** `Src/XCore/xWindow.cs` (2,498 lines) *is* a WinForms Form holding
  `CollapsingSplitContainer m_mainSplitContainer`, `RecordBar`, `m_sidebar`, `StatusBar`,
  `IUIAdapter` rebar/sidebar/menubar adapters, and a `Mediator`/`PropertyTable`. `FwXWindow`
  (`Src/xWorks/FwXWindow.cs`, 2,588 lines) derives from it (`IFwMainWnd, ISettings, IRecordListOwner`).
  Replacing the shell means re-homing all of this; it cannot be incremental within one window
  instance, which is why area-by-area screen migration (11e) + a parallel Avalonia main window is the
  only viable path.
- **Main.xml compiler is tractable and bounded.** `DistFiles/Language Explorer/Configuration/Main.xml`
  is **989 lines with 118 `<include>`s** pulling **51 config XML files totaling ~15.7k lines**. The
  command/choice model already lives in typed C# (`xCoreInterfaces/Command.cs` 768 lines,
  `ChoiceGroup.cs` 808 lines, `Inventory.cs` 1,709 lines) — so 11b compiles XML into types that
  *already have a runtime peer*. This is the same deterministic-import + snapshot pattern proven by
  the Lexical-Edit view-definition compiler (`Src/Common/FwAvalonia/ViewDefinition/`); high
  confidence it transfers. The diagnostics-not-silent-omission requirement is the right discipline.
- **Command bridge already seeded.** `IXCoreCommandBridge` and `IRecordNavigationContext` exist in
  `Src/Common/FwAvalonia/Seams/ISeams.cs` and are consumed at the region edge
  (`Src/xWorks/RecordEditView.cs`, `RecordClerkNavigationContext.cs`). `seam-catalog.md` §1
  explicitly reserves *"shell-scope wiring happens in the shell phase, not per region"* — so 11c is a
  documented promotion of an existing seam, not a green-field design. Feasible.
- **Navigation/pane replacements are owned-control work.** `SilSidePane/` (OutlookBar + ~20 files),
  `MultiPane.cs` (674), `CollapsingSplitContainer.cs` (667), `PaneBarContainer`, `RecordBar`. None
  have stock Avalonia equivalents; 11d is custom-control build comparable in weight to Stage 3's grid.
- **FlexUIAdapter retirement is ~3.2k lines** across `Src/XCore/FlexUIAdapter/` (Menu/Bar/Sidebar/
  Toolbar/PaneBar adapters). Its *default* removal is the spec's `10.7`; deferring removal until after
  the default switch is correct.

## 3. Best practices (shell strangling)

- **Strangle the shell at the area/tool boundary, run two main windows side by side.** The existing
  change's "screen registry + per-screen manifest + explicit legacy host for non-migrated screens"
  (Decision 5, spec "screen registry"/"legacy content explicit and temporary") is textbook Strangler
  Fig and matches §2 principle 1. Keep it.
- **Compile config to types, keep XML as import/audit only** (Decision 3, spec "typed definition is the
  runtime target"). This is the same bet that worked for view-definitions; reuse the cache-keyed,
  off-thread, deterministic-snapshot machinery rather than reinventing it.
- **Bridge commands before replacing them** (Decision 4). Preserve command IDs/shortcuts as the stable
  contract so menus/toolbars/context-menus can be re-skinned (Fluent restyle, program decision 4)
  while behavior parity is asserted by command-target tests, not pixels.
- **One UI thread / one message loop until the switch.** Modality stays WinForms-owned through the
  whole transition (architecture-patterns §7, `dialog-ownership.md`). Avalonia desktop lifetime only
  becomes real at the *default switch* (11f / shell task 10.6), gated by full-app smoke
  (spec "Full-app smoke gates protect default switch").
- **Headless-first.** shell task 5.4 (Avalonia.Headless for shell creation, nav host swap, command
  dispatch, dialog ownership, focus traversal, pane state) keeps deep behavior testable without UI
  automation — consistent with the program's harness discipline.

## 4. Interactions & dependencies

**Stage 5 ↔ Stage 11 modal-dialog ordering verdict: the ordering is CORRECT — dialogs (Stage 5)
must precede the shell (Stage 11), not the reverse.** The prompt's framing ("this unblocks Avalonia
modal windows (Stage 5 dialogs!)") inverts the actual constraint and would be a real bug if taken
literally. The repo evidence:

- On the **coexistence path (Avalonia 11.x + .NET FW 4.8)** Avalonia modal windows are **not
  supported**; the only supported modal pattern is a **WinForms `Form` owning embedded Avalonia
  content** (architecture-patterns §7; `dialog-ownership.md`; corroborated in the Stage 2 review's
  sources). Stage 5 therefore deliberately ships **Avalonia dialog *content* inside WinForms-owned
  modal forms** (roadmap §6 Stage 5 "Coexistence rule"; existing change does not require Avalonia
  modality for dialogs). Stage 5 does **not** need a real Avalonia modal window and is **not blocked**
  by Stage 11.
- Conversely, Stage 11's `Window.ShowDialog` (true Avalonia modality) only becomes available once the
  **Avalonia desktop lifetime owns the main window** — i.e. at the Stage 11 default switch. So
  Avalonia-native modal windows are a *Stage 11 deliverable*, used *after* the shell exists; they are
  not a prerequisite that Stage 5 waits on.
- The dependency arrow is therefore **5 → 11** (the roadmap graph already draws `S5 … → S11`), and
  Stage 11's gate ("enough of 5–8") is right: high-frequency dialogs migrated as WinForms-owned
  content first, then the shell flips them to Avalonia-owned modality. **Action:** make this two-phase
  dialog lifecycle explicit in Stage 11 — re-host the already-migrated Stage 5 dialog *content* from
  WinForms-owned to `Window`-owned modality is a distinct 11a task, not free.

**Stage 12 (runtime jump) ordering vs Stage 11: correct and important.** §4/§5 sequence
`11 → 12 → 13`: replace the WinForms shell first so the .NET 10 + Avalonia 12 jump ports *surviving*
managed code, not WinForms about to be deleted (principle 14). One caveat to record in Stage 11:
because `XWindow : Form` and `FieldWorks.cs` use `Application.Run()`, the **co-running WinForms shell
keeps the whole process on net48/Avalonia 11.x through all of Stage 11** (one CLR per process). New
Stage 11 Avalonia shell code must be written **Avalonia-12-ready** (avoid APIs removed in 12) exactly
as §5 demands, or 11→12 incurs avoidable rework.

**Stage 2 dependency.** Stage 11 *consumes* the lifetime/dialog-owner/dispatcher/command ports that
Stage 2 designs (Stage 2 review §4–5; shell task 2.1 "following `avalonia-ui-scheduler` and
`avalonia-lifetime`"). Stage 11 should not redefine `IUiScheduler`/`IRegionLifetime` (already in
`ISeams.cs`).

## 5. Recommended plan changes

1. **Decompose Stage 11 into the six sub-epics (11a–11f)** above and add an internal dependency note:
   `11a (lifetime) + 11b (compiler) + 11c (command bridge)` are foundational and can run in parallel;
   `11d (composition)` depends on 11b/11c; `11e (screens)` depends on 11d + per-area Track-II
   completion; `11f (default switch + FlexUIAdapter removal)` is last.
2. **State the Stage 2/Stage 11 split explicitly in the roadmap row:** Stage 2 = *contract/port design
   + seam stand-up*; Stage 11 = *implementation + shell-scoping + default switch*. Remove any
   implication that Stage 11 re-creates the lifetime/command contracts.
3. **Add an explicit "dialog modality re-host" task to 11a:** migrate Stage 5's WinForms-owned Avalonia
   dialog *content* to Avalonia `Window`-owned modality at the switch; this is the only point where
   true Avalonia modal windows appear. Tie it to the full-app smoke gate.
4. **Make the area-by-area gate concrete:** Stage 11's exit gate "enough of 5–8" should enumerate which
   areas must be Avalonia (Lexicon, then Words/Grammar/Notebook/Lists) before the default switch, and
   which may remain explicit legacy islands with manifests (spec "legacy content explicit and
   temporary"). Texts&Words/Interlinear (Stage 7, Views-engine-heavy, depends on Stage 9) is the most
   likely legacy island at first switch — call that out.
5. **Reuse, don't rebuild, the compiler infra:** point 11b at
   `Src/Common/FwAvalonia/ViewDefinition/` (cache key, off-thread, deterministic snapshot) so the
   Main.xml compiler shares the proven pipeline.
6. **Add Open Question resolution owners.** The existing change's 5 open questions (runtime target;
   partner XML extension points; docking library vs owned; browser/PDF engine; first-switch blocking
   screens) are still unanswered and each gates a sub-epic. Map them: OQ1→11a/Stage 12, OQ3→11d,
   OQ4→Stage 10, OQ5→11e gate.

## 6. Open questions / risks

- **OQ-A (FlexUIAdapter half-life):** removing it from the *default* path (11f) is clean, but ~3.2k
  lines and `IUIAdapter` are referenced by `XWindow`; confirm no migrated Avalonia screen transitively
  pulls a `BarAdapterBase`/`MenuAdapter` during coexistence (active-host contract should catch this).
- **OQ-B (docking):** `MultiPane`/`CollapsingSplitContainer`/`SilSidePane` have no stock Avalonia
  peer. Existing change task 7.5 says "evaluate a docking library only if owned controls cannot meet
  workflows." Risk: under-scoping 11d. Spike 11d alongside Stage 3's owned-control work; record the
  build-vs-library decision with a pivot trigger.
- **OQ-C (partner extension XML):** Decision/Open-Q2 — third-party `Main.xml` includes/extension hooks.
  The typed compiler's "diagnostics not silent omission" is the right safety net, but a partner whose
  extension has no Avalonia equivalent is a hard blocker for *their* default switch; needs a policy.
- **OQ-D (multi-window/active-window registry):** `FieldWorks.cs` tracks `s_activeMainWnd` and
  `Form.ActiveForm as IFwMainWnd`. The Avalonia active-window registry must preserve multi-project /
  multi-window semantics; this is subtle and under-tested in the existing tasks (only 6.4 nav tests).
- **Risk — "hallucinated parity" at shell scale.** A shell smoke test that *launches and clicks
  around* is not command-parity evidence. Enforce command-target/shortcut parity tests (shell task 4.3,
  spec "command parity validated") as the gate, per §7 DoD and `parity-evidence.md` language rules.
- **Risk — pulling the shell too early** (roadmap §9 names this, likelihood Low/impact High). The
  "enough of 5–8" gate is the mitigation; keep it hard and enumerated (change #4).

## 7. Confidence

**High** that Stage 11 must decompose, that the existing change already supplies the seams, and that
the **Stage 5→11 dialog ordering is correct** (dialogs ship as WinForms-owned Avalonia content first;
Avalonia-native modality is a Stage 11 output) — grounded in `dialog-ownership.md`,
architecture-patterns §7, the Stage 2 review, and the `FieldWorks.cs`/`xWindow.cs` WinForms coupling.

**High** on the Stage 11→12 ordering rationale (port surviving code, not soon-deleted WinForms).

**Medium** on sizing of 11d (navigation/pane owned controls) and 11a (lifetime re-homing of the ~15
`Form dialogOwner` seams in `FieldWorks.cs`) — both are larger than the single-line roadmap row
suggests and warrant their own spikes.

**Medium-low** on partner/extension XML and multi-window active-window registry parity — under-specified
in the existing tasks and dependent on unresolved open questions.
