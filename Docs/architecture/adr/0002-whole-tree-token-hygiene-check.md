# token-hygiene.ps1 enforces full conformance across its whole scope, with no grandfathering

Most lint/hygiene checks in this repo (`comment-hygiene.ps1`) are deliberately diff-scoped:
they check only the lines a branch adds, so pre-existing violations are grandfathered
rather than blocking unrelated work. `token-hygiene.ps1` (hardcoded color/spacing literal
detection for the Avalonia design-token system) is the opposite on purpose: every run
scans the entire token-hygiene scope and fails on any violation found in it, with no
diff-scoping and no per-file suppression mechanism beyond a small, hand-audited allowlist
of the token system's own plumbing files.

**Scope, concretely**: 170 files, out of roughly 2,300 `.cs`/`.axaml`/`.xaml` files under
`Src`. "Whole-tree" means the whole of the scoped tree, not the whole repository:
`Src/Common/FwAvalonia*`, `Src/LexText/LexTextControls/Avalonia` and
`Src/xWorks/Avalonia`. Everything else is untouched by the check.

**Why**: that tree is new code with nothing to grandfather — every file in it was
written after the token system existed, so there is no backlog for a phase-in to work
through, and a violation can only arrive with a new edit. That is what makes
zero-tolerance cheap here and expensive elsewhere: applied to the ~218-dialog WinForms
surface, the same rule would fail every build until a large retrofit finished, so the
scope deliberately excludes it.

**Consequence, accepted deliberately**: since humans aren't required to run
`-TokenHygiene` locally (only agents are, per `AGENTS.md`), a single violation that lands
on `main` will fail every subsequent unrelated PR touching the Avalonia tree until someone
notices and fixes it — there's no ratcheting/baseline mechanism to absorb it quietly. This
is treated as a feature (the check double-checked as clean rather than silently degrading)
not a bug, but it does mean the escape valve for a genuine future exception is a hand-audited
file allowlist in `TokenHygiene.psm1`, not a lighter-weight per-line suppression — see the one real case that motivated this
(`CompactDialogStyles.cs`/`FwSurfaceStyles.cs` needing values Avalonia's compiled XAML
cannot hand them via `{StaticResource}`, since it rejects `x:Static` as a resource
declaration). That case is no longer a hand-duplicated literal at all: the
`GenerateTokenKeys` FwBuildTasks task (see ADR 0001) bakes those values from the XAML
token text at build time, so there is nothing left to drift — `DuplicateTokenPairConsistencyTests.cs`
stays as a backstop regardless, since generated code can still have bugs.

**If this needs to change**: revisit when the scope grows enough that a single slipped-in
violation blocking the whole PR queue becomes a real operational cost rather than a rare
event — that's the trigger condition for adopting a ratcheting/baseline tool instead of
this file's current hard whole-tree check.
