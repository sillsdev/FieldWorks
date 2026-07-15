# shared-editable-virtualized-table — superseded

This change's plan (`proposal.md`, `design.md`, `tasks.md`, `rendering-cutover-design.md`, `specs/`)
has been removed. It used "Stage N" vocabulary and cited roadmap `epics/` files that never existed,
was never referenced by `avalonia-migration-roadmap`, and its own "Close-out" claims (tasks.md §8)
were false. The Browse/table-virtualization surface it targeted (`LexicalBrowseView`,
`ClerkBrowseRowSource`, `RecordBrowseView`, bulk-edit, row context menu, CSV export) was actually
built under `lexical-edit-avalonia-migration` (see its `tasks.md` §19f/§19i and
`browse-remainders-test-research.md`) — treat that as the authoritative record of what shipped.

The four files remaining here are kept because they are still cited by
`.claude/skills/fieldworks-winforms-to-avalonia-migration/references/parity-evidence.md` and
`architecture-patterns.md`, and were independently confirmed accurate:

- `architecture-review.md` — honest gap analysis (e.g. lexicon-Browse editing is dormant under
  shipping configs); still a useful scope correction.
- `bulk-edit-census.md` — inventory of BulkEditBar behaviors, including what's deferred.
- `headless-integration-harness.md` — the headless test-harness scaling doc, actively referenced.
- `pivot-trigger-resolutions.md` — decision log for pivot triggers raised during this change.
