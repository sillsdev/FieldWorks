# Playbook: Migrate a WinForms Dialog

The developer's workflow for converting one WinForms dialog to Avalonia,
driven through the `/convert-dialog` skill. You decide and confirm at every
gate; the skill analyzes, drafts, and builds. Design background is the
overview document; the skill carries the process detail.

Prerequisite: an up-to-date main that builds locally. The legacy dialog
keeps working throughout -- launch sites gate on the UIMode setting and the
legacy path stays untouched.

Keep FieldWorks open and explore while the agent works: its analysis phase
is read-only, so the two run in parallel. The agent will not touch your
running app, and it asks before any build or test run (a build fails on
binaries your app holds locked). Screenshot capture is yours while you are
in there; hand it to the agent only when you have closed the app.

## Workflow

The skill talks about the work in named stages: **Understanding the dialog**,
**Agreeing on how it works**, **Deciding what to prove**, **Planning the
replacement**, **Building it**, **Proving it works**. Your steps below run
alongside them.

1. Start `/convert-dialog` and give it the dialog class name.
2. While it analyzes: open the dialog yourself (this may require certain
   project data to exist) and explore the controls and interactions.
3. Look at the WinForms source for hidden controls or logic that is not
   obvious from the UI.
4. Decide: is this dialog good enough to just port, or does it really need
   a redesign? **A redesign verdict parks the dialog** -- this workflow
   ports (with low-cost improvements); redesigns are a separate effort.
5. The skill stops and asks when its analysis is ready. Read the report
   looking for gaps and inaccuracies -- it will not summarize it for you, by
   design. Copy any section that needs changing or clarifying into your
   reply. Keep working until the document reflects an accurate understanding
   of the current dialog.
6. Work with the agent to identify the integration tests that prove the
   dialog's capabilities (it will grill you until the set is defined and
   saved as the test plan).
7. Review the control/pattern mapping report: which WinForms controls have
   existing exemplar ports, where the gaps are, and the proposed Avalonia
   layout. Verify and adjust; the agreed result becomes the design
   document. If the analysis surfaced unconverted CHILD dialogs, children
   convert first: the skill shows the recommended order and asks whether to
   start the blocker now in this session or pause. If it surfaced a CUSTOM
   WinForms control with no exemplar, you get a capability table for the
   convert-vs-replace verdict; an owned conversion gets the same choice --
   convert the control now, or pause. Paused conversions resume exactly
   where they left off, even mid-control: restart /convert-dialog with the
   dialog name and the skill re-checks its gates against reality.
8. Choose: **generate scaffold** (compiling, launching-empty dialog with
   every launch site gated) or **implement design** (scaffold + full build
   from the design and test plan). Hand-implementing inside the scaffold is
   fine -- the skill will evaluate your implementation against the design.
   Either way, nothing merges until step 9 passes: the scaffold is
   branch-state, never a shipped state. To tune the generated layout by hand,
   work from [Adjust a Converted Layout by Hand](adjust-the-layout.md): it
   covers which file holds which visual property, how to see your change, and
   the gotchas that waste an afternoon.
9. Verify with `create-integration-test` (TDD or post-implementation), then
   manually test the live dialog in New UI mode against the analysis
   document and the `-before` captures -- and smoke Legacy mode (toggle
   OFF: every launch site still opens the legacy dialog).
10. Approve the exemplar-map row for anything new this conversion created.
11. Land it: comment audit, preflight, PR per the repo's conventions.

## Where things live

- Working documents: `Docs/migration/working/<DialogClass>/` (gitignored).
- Process detail: the `convert-dialog` skill. Test generation: the
  `create-integration-test` skill. Build mechanics:
  `.claude/skills/fieldworks-avalonia-ui/references/dialog-conversion.md`.
- Hand-tuning the visual result: [Adjust a Converted Layout by Hand](adjust-the-layout.md).
