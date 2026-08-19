# token-hygiene.ps1 enforces full conformance, whole-tree, no grandfathering — scoped to the Avalonia surface only

Most lint/hygiene gates in this repo (`comment-hygiene.ps1`) are deliberately diff-scoped:
they check only the lines a branch adds, so pre-existing violations are grandfathered
rather than blocking unrelated work. `token-hygiene.ps1` (hardcoded color/spacing literal
detection for the Avalonia design-token system) is the opposite on purpose: every run
scans the entire scoped tree and fails on any violation found anywhere in it, with no
diff-scoping and no per-file suppression mechanism beyond a small, hand-audited allowlist
of the token system's own plumbing files.

**Why**: the scoped tree (`Src/Common/FwAvalonia*`, `Src/LexText/LexTextControls/Avalonia`,
`Src/xWorks/Avalonia`) is new code with nothing to grandfather — every file in it was
written after the token system existed. Current design-token literature confirms this is
the correct call specifically for greenfield surfaces (a genuinely new codebase should
turn strict rules on fully rather than phase them in); the same literature is equally
clear that whole-tree zero-tolerance is the wrong call for retrofitting legacy code, which
is why this gate's scope explicitly excludes the ~218-dialog WinForms surface FieldWorks
hasn't converted yet, rather than trying to enforce it there too.

**Consequence, accepted deliberately**: since humans aren't required to run
`-TokenHygiene` locally (only agents are, per `AGENTS.md`), a single violation that lands
on `main` will fail every subsequent unrelated PR touching the Avalonia tree until someone
notices and fixes it — there's no ratcheting/baseline mechanism to absorb it quietly. This
is treated as a feature (the gate double-checked as clean rather than silently degrading)
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
this file's current hard whole-tree gate.
