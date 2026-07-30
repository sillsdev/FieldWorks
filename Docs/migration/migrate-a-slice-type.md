# Playbook: Migrate a Slice Type

The developer's workflow for converting one legacy slice type -- usually one
showing today as a labeled "Unsupported" worklist row in the detail view --
driven through the `/convert-slice` skill. You decide and confirm at every
gate; the skill analyzes, drafts, and builds. Design background is the
overview's "Detail pipeline" section; read it once before your first slice.

Prerequisite: an up-to-date main that builds locally. Legacy stays untouched
throughout -- the detail surface's UIMode gate covers the new work.

Keep FieldWorks open and explore while the agent works: its analysis phase
is read-only, so the two run in parallel. The agent will not touch your
running app, and it asks before any build or test run. Slice screenshots are
yours to capture while you are in there.

## Workflow

The skill talks about the work in named stages: **Understanding the slice**,
**Agreeing on how it works**, **Deciding what to prove**, **Planning the
replacement**, **Building it**, **Proving it works**. Your steps below run
alongside them.

1. Start `/convert-slice` and give it the slice class name (or the layout
   `editor=`/`class=` identity).
2. While it analyzes: open an affected entry in Lexicon Edit (Legacy mode)
   and explore the slice's interactions; look at the WinForms source for
   behavior the UI does not show.
3. Decide: port, or does this slice really need a redesign? **A redesign
   verdict parks the slice** -- this workflow ports.
4. When the analysis is ready, review it as with dialogs: correct and
   clarify until it accurately describes the current slice.
5. Work with the agent on the integration-test plan (compose, edit/undo,
   validation, refresh, cluster/bidi safety where text is involved).
6. Confirm the **route decision** -- existing `DetailFieldKind`
   classification, new kind + control, or `ISlicePlugin` -- then review the
   mapping/design report and settle the design document.
7. Choose: **generate scaffold** (the row goes from Unsupported to
   empty-but-present at its real position, composing green) or **implement
   design**. Hand-implementing inside the scaffold is fine.
8. Verify with `create-integration-test`, then manually test in live
   FieldWorks: edit interactions, single Ctrl+Z per save, cross-surface
   refresh, clean tool-switch mid-edit.
9. Approve the exemplar-map row for anything new.

## Where things live

- Working documents: `Docs/migration/working/<SliceClass>/` (gitignored).
- Process detail: the `convert-slice` skill. Test generation: the
  `create-integration-test` skill. Idiom rules:
  `.claude/skills/fieldworks-avalonia-ui/references/style-system.md`.
