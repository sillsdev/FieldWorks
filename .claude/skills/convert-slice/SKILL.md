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

**Every working document opens with a LINKED ticket reference whenever a ticket
is involved.** Write the Jira key as a markdown link to
`https://jira.sil.org/browse/<KEY>`, never as bare text, and link the parent or
epic the same way when there is one:

```markdown
Ticket: [LT-22672](https://jira.sil.org/browse/LT-22672) "[Avalonia] Convert
Allomorphs slice", parent [LT-22622](https://jira.sil.org/browse/LT-22622).
```

These documents are gitignored and outlive the session that wrote them, so a
reader arriving later has no other route back to the ticket. It applies to the
analysis, test plan, design report, and state manifest alike.

This does NOT license ticket keys as pointers in code comments. Comment hygiene
governs there: a bare finding-code such as `P1:` referring to one of these
documents is a banned doc-pointer, because the document it points into is not
in the repository.

### Citing other documents and sections

Every reference -- to another document OR to a section of the document you
are writing -- is a markdown link whose VISIBLE TEXT is the target's name.
Never write a bare section number: a number from another document collides
with the local numbering and resolves to nothing.

- Cross-document citations also carry the target's stable id when it has
  one, e.g. `[per-control F1 help (GAP-F1-HELP)](../../../../.claude/skills/fieldworks-winforms-to-avalonia-migration/references/control-exemplar-map.md#gap-f1-help)`.
- Verify the anchor exists by READING the target's heading before writing
  the link -- do not guess. GitHub's slug rule: lowercase, spaces to
  hyphens, punctuation dropped.
- Compute the relative path from the document being written. Working
  documents sit at `Docs/migration/working/<SliceClass>/`, four levels below
  the repo root.
- Keep the headings you generate short and ASCII so their auto-anchors stay
  predictable.
- Numbers may stay in headings for structure; they never appear as a
  citation.

## Understanding the slice (read-only; safe while they explore)

Source, history, Jira, and layout-XML reading only -- no build, no test run,
no app automation -- so it runs concurrently with the developer's own
exploration. The "before" evidence is NOT part of that concurrent work.

Everything convert-dialog does when understanding a dialog (source + related
files, file history + Jira chronology, logic and data-member analysis incl.
LCM objects and Units of Work, enabled/disabled cataloging), plus
slice-specific work:

- Read `Docs/lessons/avalonia-migration/README.md` and any card matching the
  slice's capabilities. Record applicable constraints and rejected assumptions
  in the analysis, then verify them against current code and legacy behavior;
  never use a card as an implementation recipe.

- Resolve the layout identity: the `editor=`/`class=` attributes and every
  layout node that produces this slice.
- Inventory where it appears: which fields, which tools, roughly how many
  instances (compose an affected record in the New UI -- the Unsupported row
  names the unclaimed class).
- Interaction picture is row-shaped: mouse and keyboard edit interactions,
  commit timing (focus-loss autosave vs immediate-commit actions), context
  menus, and how the slice behaves inside DataTree (indent, label,
  expansion).
- "Before" evidence: the dialog screenshot harness does NOT apply (a slice
  renders inside the DataTree), so the capture needs the live app -- which
  means it is the DEVELOPER's to do while they explore (ask them to grab the
  affected entry's rows and say where they put them). Only drive the app
  yourself with their explicit go-ahead that it is closed and yours.

The analysis document keeps the four-section shape; its layout section
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
same four-section design report, with its layout section describing the
proposed row layout and every deliberate difference from the legacy slice.
Every reference the report makes follows
[Citing other documents and sections](#citing-other-documents-and-sections).

The children-convert-first dependency gate applies here too: any dialog the
slice opens (choosers, create dialogs) must already have an Avalonia route
(converted, covered by a shared dialog, or FwMessageBox) before this slice proceeds. So
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
present". There is no per-slice launcher gate; the detail view's UIMode
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
  xWorks).
- The repository comment standard, `.claude/skills/fieldworks-code-commenting/SKILL.md`, applies
  throughout.

## Proving it works

1. create-integration-test against the plan (tests land per the mirroring
   rule: control tests in `FwAvaloniaTests/Detail/`, composer tests in
   `xWorksTests/Avalonia/Composer/`).
2. Ask the developer to manually test -- then STOP and wait for their
   findings. They own the app; do not drive it or change its UI mode for
   them. In live FieldWorks, New UI on: the field at its real position, edit
   interactions per the analysis document, focus-loss autosave, single Ctrl+Z
   per save, cross-framework PropChanged refresh both directions, tool-switch
   mid-edit settles cleanly.
3. Legacy-mode smoke: with the UI-mode toggle OFF, every field the change
   touched still behaves unchanged. A composer or shared-control change
   reaches far beyond the slice being converted, so name what else it reaches
   and have that checked too.
4. Work each manual finding through
   [The manual-finding loop](#the-manual-finding-loop).
5. Land the exemplar-promotion row for anything new (human-gated, same PR).

### The manual-finding loop

A green suite that missed a defect the developer found by looking is evidence
the SUITE has a hole, not just the code. Close both.

1. **Triage the layer before writing anything.** Model (composer), dispatch
   (factory), or render (control)? Existing green tests narrow it in one step:
   if the composer tests pass and the row looks wrong, the model is right and
   the defect is below it.
2. **Write the missing test at the layer the defect lives in**, before the
   fix. A model-only plan cannot see a control that draws nothing; a
   render-layer defect needs a render-layer test.
3. **Verify the new test actually catches the defect: revert the fix (or
   write the test first) and watch it FAIL.** This is not ceremony. A test
   asserting a control "renders" can pass against a control drawing nothing --
   an untemplated Avalonia control still measures to its padding and still has
   visual children. A test that has never failed proves nothing, and a
   false-green test is worse than no test because it silences the next person.
4. **Fix, then re-run the owning suite AND every suite the change reaches.**
   Shared code (the composer, `SliceFactory`, a shared control style) means
   the blast radius is not the slice.
5. **Record it in `<SliceClass>-state.md`**: the cause, the fix, and -- when
   the first test attempt was a false green -- which assertion was too weak
   and which one actually bites. That lesson is the reusable part.

If the finding turns out to be a scope decision rather than a defect (a row
nobody planned, a behavior the plan never covered), do not absorb it silently:
put it to the developer as a decision, and if they take it in, add plan items
for it rather than implementing untested.
