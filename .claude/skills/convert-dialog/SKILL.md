---
name: convert-dialog
description: Drive the conversion of one WinForms dialog to Avalonia through analysis, developer alignment, integration-test planning, exemplar mapping, design, and scaffold/implement. Use when the user invokes /convert-dialog with a dialog class name, or asks to start converting a specific WinForms dialog.
---

# Convert Dialog

Converts one WinForms dialog to Avalonia with the developer in the loop at
every gate. The developer decides and confirms; this skill analyzes, drafts,
builds, and never advances past a gate without their explicit go-ahead.

## Two rules that override everything else

**1. The developer's FieldWorks is untouchable.** They are exploring the
dialog while you work. NEVER close, kill, restart, or drive their running
FieldWorks, and never change the UI-mode registry setting under them. The
live-app capture route (fieldworks-winapp) owns the app lifecycle
(relaunch-per-tool, `CloseMainWindow`) -- it is FORBIDDEN while their
instance is open. Do not run `build.ps1`/`test.ps1` without asking either: a
build fails on binaries their running app holds locked, and killing their
process to unblock a build is never acceptable. If a build or test fails on
locked files, STOP and ask.

**2. Every gate is a real stop.** A gate is an interactive question to the
developer (use the question tool, which returns only when they answer) --
never a sentence in a message you then continue past. At a gate: end your
turn on the question and take no further action. Do NOT summarize or restate
a report's contents in chat -- the document is the artifact, and a chat
summary invites skipping the review the gate exists for. Point at the file
and stop.

## How to talk about where you are

The stages below have names. USE THEM with the developer, in plain language
about the work: "I've finished understanding the dialog", "we've agreed on
how it works, so next is deciding what to prove", "the replacement plan is
ready for your review". NEVER say "Phase 2", "phase 4 gate passed", or any
numbered-stage shorthand -- it means nothing to someone who has not read
this file. The stages, in order: **Understanding the dialog** -> **Agreeing
on how it works** -> **Deciding what to prove** -> **Planning the
replacement** -> **Building it** -> **Proving it works**.

Input: the WinForms dialog class name (e.g. `MergeEntryDlg`).
Scope: PORT with low-cost improvements. If the developer's verdict is
"redesign, not port", stop after Understanding the dialog -- redesign is out of
scope; the dialog waits.

## Working documents

All working artifacts live in `Docs/migration/working/<DialogClass>/`
(gitignored; create on first use):

- `<DialogClass>-analysis.md` -- from Understanding the dialog
- `<DialogClass>-integration-test-plan.md` -- from Deciding what to prove
- `<DialogClass>-design.md` -- from Planning the replacement
- `<DialogClass>-state.md` -- one-page state: current phase, plus any OPEN
  GATE and what it waits on (e.g. `waiting-on: MSAGroupBox (control,
  Docs/migration/working/MSAGroupBox/)`); kept current at every gate and
  phase transition

**Resume rule:** on invocation, read `<DialogClass>-state.md` first
(falling back to artifact-presence inference when it is absent: analysis
present -> resume at Agreeing on how it works; test plan -> Planning the replacement; design ->
scaffold/implement choice). Then RE-EVALUATE every open gate against
reality, not the note: if the exemplar map now carries the awaited
control's row, or the awaited child dialog now has an Avalonia route, the
gate self-clears and the dialog resumes past it. If a gate is still
blocked, drop into the blocker's own sub-cycle where IT left off (its
working dir carries its sub-phase by the same rules -- resumable
recursively). State what was found and CONFIRM with the developer before
proceeding. Never silently redo a completed phase.

The working documents remain in place after completion; what to keep,
delete, or attach anywhere is the developer's call.

## Understanding the dialog (read-only; safe to run while they explore)

The steps below are pure reading -- source, git history, Jira, layout XML. No
build, no test run, no app automation. This is what makes the phase safe to
run concurrently with the developer's own exploration of the live dialog.
The "before" evidence (the last step here) is NOT part of that concurrent work.

1. Read the dialog source and every related file (designer, resx, helpers).
2. Pull the file history and investigate related Jira issues (read-only
   Atlassian tooling); document them chronologically as framing context for
   why the dialog's features exist.
3. Find and document ALL calling sites and the conditions under which each
   opens the dialog. Check every launch-site kind the conversion documents
   call out: launcher edges (`Lcm*DialogLauncher`), xCore command handlers
   and dialog listeners (the `EntryDlgListener`/`RecordDlgListener`
   pattern), menu/toolbar command XML, DetailControls slice launchers (the
   `ReferenceLauncher` "..." family), popup-tree manager items ("More..."/
   "Create..." -- `MSAPopupTreeManager` and kin), other dialogs that chain
   into this one, context menus, and direct static `RunDlg`-style helpers.
4. Analyze the dialog's logic and its interaction with data, internal and
   external: static calls, LCM objects, and any LCM Units of Work --
   including the undo/redo task labels it uses.
5. Build the interaction picture: every supported user flow through cancel /
   OK / apply (and next / back / finish for wizards); whether the dialog is
   modal or MODELESS; the help topic; every CHILD dialog or message box the
   dialog itself opens (each is a conversion dependency -- catalog them
   explicitly).
6. Catalog the enabled/disabled state of every control and everything that
   affects it.
6b. Inventory the dialog's localizable strings (resx usage) and note the
   project-data prerequisites needed to exercise each flow (these feed the
   test plan and the manual checkpoint).
7. "Before" evidence -- NEVER while their FieldWorks is open. Ask which the
   developer prefers, and wait for the answer:
   - **They capture it** (default while they are exploring): they are
     already looking at the dialog, so ask them to grab the screenshots and
     say where they put them. Zero risk to their session.
   - **The harness captures it** (unattended, repeatable): add a
     `Cap`/`CapLoop` case to `ScreenshotHarnessTests` and run
     `.\test.ps1 -SkipNative -TestProject LexTextControlsTests -TestFilter
     "FullyQualifiedName~ScreenshotHarness"` -- but ONLY once they confirm
     FieldWorks is closed (it is an in-process `DrawToBitmap` render that
     never touches the desktop, yet the build it needs fails on binaries
     their running app locks).
   The live-app automation route is a last resort for behavior the harness
   cannot fake, and only with their explicit go-ahead that the app is yours
   to drive.

Write `<DialogClass>-analysis.md` with exactly these sections:

1. **Dialog purpose**
2. **Data interaction**
   - A. All model data displayed and modified, including WHEN modification
     occurs (on OK/apply, on focus change, etc.)
   - B. Any properties or settings displayed or modified, including when
3. **Control interactions** -- interactions between controls, specific about
   enabled/disabled state and what drives it
4. **Layout strategy** -- current control layout, control sizes and minimum
   sizes, resizing behavior, and any persisted bounds/splitter state

## Agreeing on how it works (gate)

When the analysis document is written, STOP. Ask -- via the question tool,
then end your turn -- "I've finished my analysis. Are you ready to align our
understanding?" Do not describe what you found. Do not summarize the
document. Wait.

When they say yes, point them at the file (path only) and ask whether they
see errors, gaps, or have questions -- then stop again and wait. When they
paste a section back with a correction, fix the document and ask again.
Re-ask with varied phrasings ("anything else that looks off?", "any
interaction I've missed?") until you get a clear negative. Each round is its
own stop: one question, end of turn.

The gate is the developer declaring the document an accurate description of
the current dialog.

## Deciding what to prove (gate)

Ask the developer for guidance on integration-test creation -- one question,
end of turn, wait -- then design the test set for the dialog's capabilities
WITH them using the grill-me skill (one question at a time, each its own
stop, recommendations offered) until the set is defined.
Every test item must be expressible as: drive the scenario, assert the
behavioral outcome, capture a labeled snapshot.

Save the result as `<DialogClass>-integration-test-plan.md`. The
create-integration-test skill consumes this file verbatim.

## Planning the replacement (gate)

0. FIRST check kit membership: if the dialog belongs to a family an
   existing kit already covers (the EntryGo/BaseGoDlg family, the
   ChooserDialog family), the conversion is a new kit consumer + launcher,
   not a new dialog -- present that finding and shrink the remaining phases
   accordingly.
0b. DEPENDENCY GATE -- children convert first: every child dialog the
   target opens must already have an Avalonia route (converted, covered by
   a kit, or a plain message box via `FwMessageBox`). If any child lacks
   one, present the dependency tree with a recommended conversion order,
   record the open gate and its waiting-on list in
   `<DialogClass>-state.md`, and ASK the developer whether to start
   converting the deepest unconverted child NOW, in this session. Yes:
   launch convert-dialog for that child class (from context) and carry it
   forward -- the parent resumes when the child completes. No: pause this
   conversion; resume re-evaluates the gate.
1. Analyze every WinForms control and design pattern in the dialog.
2. Map each against `.claude/skills/fieldworks-winforms-to-avalonia-migration/references/control-exemplar-map.md` (the exemplar map
   in the migration skill).
3. From the analysis document's layout section, propose an Avalonia layout
   that AT MINIMUM replicates the WinForms capability; exceed it where the
   effort is low.
4. For items with no exemplar: catalog the item's purpose and capabilities,
   then look for an adequate replacement in Avalonia packages -- both those
   already imported and others not yet used (an un-imported package is a
   finding to surface, not a decision to make alone).

**Custom-control sub-cycle.** A CUSTOM WinForms control with no exemplar
gets a capability-first treatment -- what it does, never how it is painted:

1. From the dialog analysis and the control's own source, list every
   capability the control provides.
2. Produce a capability-comparison table: capability -> the stock Avalonia /
   composed / package answer -> classified **covered**,
   **possible-but-costly**, or **not-possible**.
3. The DEVELOPER makes the convert-vs-replace verdict on that table.
   Default bias is stock/composed (unnecessary owned controls are the
   snowflakes the idiomatic audit removed). Not-possible earns an owned
   conversion; possible-but-costly is the developer's judgment.
4. If the verdict is CONVERT: the control becomes its own conversion, and
   **the control converts first**. Inform the developer and ASK whether to
   start converting it NOW, in this session. Yes: open
   `Docs/migration/working/<ControlClass>/` with its own analysis / design
   / test-plan / state documents, record the gate in
   `<DialogClass>-state.md`, and run the control's cycle (align -> grilled
   test plan -> design -> implement -> create-integration-test ->
   exemplar-map row), then resume the dialog with the control as an
   available exemplar. No: record the gate the same way and pause this
   conversion; resume re-evaluates the gate.
5. Placement rule for a new owned control (the two-assembly layering IS the
   legacy FwCoreDlgs/FwCoreDlgControls separation -- FwAvalonia is the
   controls layer, FwAvaloniaDialogs the dialogs layer):
   - used by dialogs only -> `Src/Common/FwAvaloniaDialogs/Controls/` (the
     dialog-composite subfolder; dialogs themselves stay at the project
     root);
   - shared with the detail view or other non-dialog surfaces ->
     `Src/Common/FwAvalonia/` (in `Detail/` when detail-specific, the root
     when general);
   - tests mirror the SUT's folder either way.

Produce the design report with these sections:

1. Each WinForms control type -> its Avalonia exemplar -> the proposed
   replacement
2. Each WinForms code pattern identified -> the proposed Avalonia
   replacement
3. Each control/pattern with NO exemplar -> a proposed replacement, or an
   explicit "none found"
4. The proposed Avalonia layout, with every deliberate difference from the
   WinForms version called out

Point the developer at the report and ask them to review it for errors,
gaps, and suggestions -- then stop and wait, without summarizing its
contents. Capture the agreed result as `<DialogClass>-design.md`. If a missing conversion or
capability surfaced, run grill-me with the developer to produce the fill
plan (the exemplar-promotion path: the first implementation is built on the
existing idiom rules and, on the developer's approval, a
control-exemplar-map row lands in the same PR naming it THE exemplar --
draft the row and show it before committing).

## Building it (developer's choice)

Offer the choice explicitly -- as a question, then stop and wait:
**generate scaffold** or **implement design**.

**Merge policy: scaffold is branch-state only.** The scaffold/implement
choice is about how much the AI builds first, not what ships -- nothing
merges until Proving it works passes (implemented and verified), so preview users
never meet an empty dialog.

**Scaffold** means: create the Avalonia files following
`.claude/skills/fieldworks-avalonia-ui/references/dialog-conversion.md`; the result
compiles and launches (empty); and EVERY launch site from the analysis
document's calling-sites section is adapted to open the Avalonia or WinForms
dialog based on the UIMode setting (fail-closed: Legacy stays the default).

**Implement** means: scaffold first, then build out the dialog from
`<DialogClass>-design.md` and `<DialogClass>-integration-test-plan.md`.

Build conventions the result must satisfy (confirm each in the diff):

- Naming: legacy stem + role suffix (`FooDlgInput` / `FooDlgView.axaml` /
  `FooDlgViewModel` / `FooDlgPayload`) in `Src/Common/FwAvaloniaDialogs/`;
  kits keep general names.
- Boundaries: the Input carries everything LCModel-free the dialog needs;
  the ViewModel never touches LCModel; the launcher owns all LCModel work inside
  one undo task with the legacy undo text.
- Presentation, modal: `AvaloniaDialogHost.ShowModal` with the WinForms
  owner -- never an Avalonia-owned window during coexistence; owner icon or
  none; focus returns to the invoking control.
- Presentation, modeless: no exemplar exists yet -- the FIRST modeless
  conversion designs the hosting pattern through the exemplar-gap path
  (grill-me), with the constraint that WinForms owns the window during
  coexistence (an Avalonia surface hosted in a modeless WinForms Form via
  the host control), and promotes it as the modeless exemplar.
- Strings in `.resx` (accessors appended in order); no L10NSharp; no
  hardcoded UI text. Concrete brushes only. Validation through
  `DialogViewModelBase.GetValidationErrors` with the inline-error exemplar;
  help through `HelpRequested` -> launcher.
- Watch-outs: `Flyout` over free `Popup`; arrow/Enter keys claimed at the
  host; WS-sensitive inputs are never plain TextBoxes (Go-family exemplar);
  match the legacy commit semantics (OK-gated vs commit-on-select) and say
  which in the ViewModel summary.
- Accessibility and keyboard parity: stable automation ids per the owned
  control convention (pinned by `OwnedControlAutomationConventionTests`);
  tab order and mnemonics match the legacy dialog.
- The repository comment standard applies to everything written.

## Proving it works

1. Run create-integration-test against the plan (TDD before implementation,
   or verification after; if the developer hand-implemented inside the
   scaffold, first evaluate their implementation against
   `<DialogClass>-design.md` and report deviations).
2. The Avalonia visual test emits the paired `<name>-after.png` (same data
   flavor as the `-before`).
3. Ask the developer to manually test -- then stop and wait for their
   findings: walk the analysis document's the Data interaction and Control interactions sections line by line against
   the live dialog in New UI mode, and compare against the `-before`
   captures. They own the app for this; do not drive it or change its UI
   mode for them.
4. Legacy-mode smoke: with the toggle OFF, every launch site from the
   analysis document still opens the legacy dialog unchanged.
5. Add any new exemplars created during this conversion to the exemplar map
   (the promotion row from Planning the replacement, if not already landed).
6. Land: comment audit against the repository standard, preflight the
   branch, and PR per the repo's conventions.
