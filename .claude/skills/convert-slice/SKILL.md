---
name: convert-slice
description: Drive the conversion of one legacy slice type to the Avalonia detail view through analysis, developer alignment, integration-test planning, route/exemplar mapping, design, and scaffold/implement. Use when the user invokes /convert-slice with a slice class or layout identity, or asks to convert a slice type or an Unsupported detail row.
---

# Convert Slice

The slice-type counterpart of convert-dialog: the same stages, gates, and
working-document conventions -- with the deltas a slice demands. The
developer decides and confirms; this skill analyzes, drafts, and builds.

## Two rules that override everything else

**1. The developer's FieldWorks is untouchable.** They are exploring the
slice while you work. NEVER close, kill, restart, or drive their running
FieldWorks, and never change the UI-mode setting under them. The live-app
capture route owns the app lifecycle (relaunch-per-tool, `CloseMainWindow`)
-- FORBIDDEN while their instance is open. Ask before any
`build.ps1`/`test.ps1` run: a build fails on binaries their app holds
locked, and killing their process to unblock it is never acceptable. On a
locked-file failure, STOP and ask.

**2. Every gate is a real stop.** A gate is an interactive question (use the
question tool, which returns only when they answer), then END the turn. Do
not summarize or restate a report in chat -- point at the file and stop; a
chat summary invites skipping the review the gate exists for.

## How to talk about where you are

Use the stage names with the developer, in plain language about the work --
never "Phase 2" or any numbered-stage shorthand. The stages, in order:
**Understanding the slice** -> **Agreeing on how it works** -> **Deciding
what to prove** -> **Planning the replacement** -> **Building it** ->
**Proving it works**.

Input: the legacy slice class name (e.g. `PhoneEnvReferenceSlice`) or the
layout identity (`editor="..."` / `editor="Custom" class="..."`).
Scope: PORT with low-cost improvements; a "redesign" verdict parks the
slice.

Working documents, resume rule, and ticket attachment are identical to
convert-dialog: `Docs/migration/working/<SliceClass>/` holding
`<SliceClass>-analysis.md`, `<SliceClass>-integration-test-plan.md`,
`<SliceClass>-design.md`; detect-and-resume on re-invocation; the documents
remain after completion (retention is the developer's call).

## Understanding the slice (read-only; safe while they explore)

Source, history, Jira, and layout-XML reading only -- no build, no test run,
no app automation -- so it runs concurrently with the developer's own
exploration. The "before" evidence is NOT part of that concurrent work.

Everything convert-dialog does when understanding a dialog (source + related
files, file history + Jira chronology, logic and data-member analysis incl.
LCM objects and Units of Work, enabled/disabled cataloging), plus
slice-specific work:

- Resolve the layout identity: the `editor=`/`class=` attributes and every
  layout node that produces this slice.
- Inventory where it appears: which fields, which tools, roughly how many
  instances (compose an affected record in the New UI -- the Unsupported
  worklist row names the unclaimed class).
- Interaction picture is row-shaped: mouse and keyboard edit interactions,
  commit timing (focus-loss autosave vs immediate-commit actions), context
  menus, and how the slice behaves inside DataTree (indent, label,
  expansion).
- "Before" evidence: the dialog screenshot harness does NOT apply (a slice
  renders inside the DataTree), so the capture needs the live app -- which
  means it is the DEVELOPER's to do while they explore (ask them to grab the
  affected entry's rows and say where they put them). Only drive the app
  yourself with their explicit go-ahead that it is closed and yours.

The analysis document keeps the four-section shape; section 4 (layout)
describes the row: label/value split, sizing, wrapping, and any multi-row
structure.

## Agreeing on how it works (gate)

Identical to convert-dialog: announce and STOP ("I've finished my analysis.
Are you ready to align our understanding?"), then point at the file and
correct round by round -- each round its own question and end of turn --
until the developer gives a clear negative to "any errors, gaps, or
questions?". Never summarize the document in chat.

## Deciding what to prove (gate)

Identical process (developer guidance -> `grill-with-docs` -> saved plan). Slice
plans must cover: compose (the field renders with correct values, not
Unsupported), edit -> ONE undo step, validation blocking, re-show after
external PropChanged, and cluster/bidi safety when the slice carries text.

## Planning the replacement (gate)

The route decision comes FIRST and is the developer's (present the tree
with a recommendation):

1. An existing `DetailFieldKind` fits -> the work is classification in
   `EditorKindMap`; composer and `SliceFactory` already handle the rest.
2. A genuinely new interaction shape -> new `DetailFieldKind` + a new owned
   `Fw*Field` control + a `SliceFactory` case.
3. A custom slice (`editor="Custom"`) -> an `ISlicePlugin` keyed by the
   exact legacy `class=` string, registered in `SlicePluginRegistry`; no layout
   edits.

Then map controls/patterns against the exemplar map exactly as convert-dialog
does when planning a replacement (including the no-exemplar catalog, package
search, `grill-with-docs` fill plan, and human-gated promotion row), and produce the
same four-section design report, with section 4 describing the proposed row
layout and every deliberate difference from the legacy slice.

The children-convert-first dependency gate applies here too: any dialog the
slice opens (choosers, create dialogs) must already have an Avalonia route
(converted, kit-covered, or FwMessageBox) before this slice proceeds. So
does the custom-control discipline: designing a new `Fw*Field` follows the
capability-comparison table with the stock/composed bias (convert-dialog's
custom-control sub-cycle), and the state-manifest + gate re-evaluation
resume rules are shared (`<SliceClass>-state.md`). At any gate, the same
choice applies: inform the developer and ask whether to convert the blocker
now in-session (launching the right skill with the class from context) or
pause.

## Building it (developer's choice)

**Scaffold** for a slice means: the editor string is classified (or the
plugin registered), a placeholder control composes and renders at the
slice's REAL position in the New UI detail view, tests compose green, and
legacy is untouched -- the row goes from "Unsupported" to "empty but
present". There is no per-slice launcher gate; the detail surface's UIMode
gate already covers it. Merge policy is the same as dialogs: scaffold is
branch-state only; nothing merges until Proving it works passes (an empty row
is worse than an honest Unsupported row for preview users).

**Implement** builds the control out from the design + test plan:

- Values project LCModel-free through `DetailValueFactory` /
  `IDetailValueProvider` -- never a second projection of an existing recipe.
- Composition wires through `DetailComposer` (the field's
  `FieldEditHandler` carries its edit operations).
- Edits stage through the edit context; the fenced session commits ONE undo
  step; new edit operations go on a sub-capability interface (the
  `IStructuredTextEditing` precedent) -- never widen `IDetailEditContext`.
- Idiom rules per `.claude/skills/fieldworks-avalonia-ui/references/style-system.md`;
  WS typography via the multi-WS text exemplar when text is involved; a
  null edit context yields read-only display.
- Compose-time snapshot discipline: rows do not live-update; the re-show
  does. No ad-hoc refresh plumbing; refresh coordination stays with
  `AvaloniaDetailRefreshController`.
- Plugin factories degrade: missing/null/throwing renders the labeled
  Unsupported row, never a crash or a blank row.
- Keep `FwAvalonia` LCModel-free (projection and write-back live in
  xWorks). The repository comment standard applies throughout.

## Proving it works

1. create-integration-test against the plan (tests land per the mirroring
   rule: control tests in `FwAvaloniaTests/Detail/`, composer tests in
   `xWorksTests/Avalonia/Composer/`).
2. Developer manual test in live FieldWorks, New UI on: the field at its
   real position, edit interactions per the analysis document, focus-loss
   autosave, single Ctrl+Z per save, cross-surface PropChanged refresh both
   directions, tool-switch mid-edit settles cleanly.
3. Land the exemplar-promotion row for anything new (human-gated, same PR).
