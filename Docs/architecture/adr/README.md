# Architecture decision records

Numbered, dated records of decisions that constrain future work and would
otherwise be re-argued from scratch. One decision per file, written when the
decision is made, and left alone afterwards: a record that turns out to be
wrong is superseded by a new one that says so, not edited into agreement with
what happened later.

This sits under `Docs/architecture/` rather than at the top of `Docs/` so the
repository does not grow a fourth place to look for design material. The
neighbouring files in `Docs/architecture/` are topic guides that get updated as
the code changes; these are point-in-time decisions that do not. The
distinction is worth the subdirectory, not a separate tree.

Not to be confused with:

- `Docs/architecture/*.md` — living guides to how a subsystem works now.
- `Docs/lessons/` — retrospective lessons from completed, rejected, or retired
  work, indexed for reading before planning in a covered area.
- `openspec/` — proposed changes with their specs and task lists, which are
  archived once implemented.

An ADR records *why a constraint exists*. If what you are writing will need
editing the next time the code changes, it belongs in one of the other three.

| ADR | Decision |
| --- | --- |
| [0001](0001-alias-semi-semantic-tokens.md) | Alias Semi's semantic tokens instead of inventing a FieldWorks palette |
| [0002](0002-whole-tree-token-hygiene-check.md) | token-hygiene.ps1 enforces full conformance across the Avalonia surface, with no grandfathering |
| [0003](0003-geometric-assertions-not-pixel-diff.md) | Assert geometry, not pixel diffs, for layout parity |
