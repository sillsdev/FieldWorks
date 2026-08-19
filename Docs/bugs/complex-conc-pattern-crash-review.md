# Review: ComplexConcPatternVc direct-edit crash (architecture self-review)

## 1. Is this the right architecture?

The invariant is the same one identified by the sibling bug's review: "a pattern view is
not free text; it changes only via chooser-insert and delete." That invariant has the
same two independent parts here:

- **"This fragment is a computed display, not a bound field, and must not accept an
  edit."** `ComplexConcPatternVc` binds no real domain object field at all -- a grep for
  `AddStringAltMember` in `ComplexConcPatternVc.cs` returns zero matches, confirmed by
  inspection and by `ReplaceWithTsString_OnMorphNodeCategoryLine_DoesNotThrow_AndRealPartOfSpeechUnrenamed`
  and `..._OnTagNodeTagLine_..._AndRealTagUnrenamed`, which specifically check that the
  real, shared `IPartOfSpeech`/`ICmPossibility` referenced by a node is untouched after an
  edit attempt. Every fragment is `AddProp` + `DisplayVariant` over the synthetic
  `ComplexConcPatternNode` tree, so there is nothing to corrupt project-wide -- but there
  was also nothing absorbing the edit, so it fell through to `VwBaseVc.UpdateProp`
  (`NotImplementedException`, unhandled).
- **"This widget only accepts chooser-insert and delete, never typed input."** A
  control-level fact, fixed the same way as the sibling: `ComplexConcControl` now sets
  `m_view.ReadOnlyView = true` instead of `false`.

**This bug is not the same failure mode as the sibling's, and the ablation evidence shows
it plainly.** For the sibling (`RuleFormulaVcBase`), `ktptEditable` was the *only*
load-bearing layer, because `AddStringAltMember`-bound fragments write straight to the
real property without ever calling `UpdateProp` -- the sibling's own review documents this
(removing `ktptEditable` alone reproduced the corruption). Here, re-running the entire
failing test suite with *only* the `UpdateProp` override added (no `ktptEditable`
markings, no `ReadOnlyView` change) turned all 13 tests green. `UpdateProp` alone is
necessary and sufficient to stop the crash, because there is no real property for an edit
to land on -- `UpdateProp` returning `tssVal` unchanged is a true no-op relative to model
state, not a mitigation racing a live write.

Given that, the `ktptNotEditable` markings and `ReadOnlyView = true` added here are
**not required to stop the crash** -- they are added anyway, for reasons independent of
the crash:

- `ktptNotEditable` on every fragment (feature lines, `Infl Features` header, `OR`/`#`,
  every bracket/paren glyph including the multi-line pile hooks, and both quantifier
  lines) rejects an edit at the selection layer instead of letting it reach `UpdateProp`
  at all. This matters because `UpdateProp`'s no-op is only a no-op *for the model*; it is
  not obviously a no-op for the view's own display cache -- returning `tssVal` from
  `UpdateProp` is how a VC normally *approves* a display update, and nothing in this
  codebase demonstrates that the specific cache entry backing a fake, negative flid
  (`ktagType` et al., never registered with `ISilDataAccess`) is invalidated correctly
  without a full `Reconstruct`. Marking the fragments non-editable removes the need to
  trust that path at all. `SelectionOverFormLine_IsNotEditable` confirms the marking
  actually takes hold (`sel.IsEditable == false`).
- `ReadOnlyView = true` is the categorical fix for the trigger the bug report actually
  named: IME composition and drag-and-drop, which do not go through
  `PatternView.OnKeyPress`. `ReadOnlyView` unregisters the keyboard/IME controller hook
  (`SimpleRootSite`); `ktptEditable` does not gate that registration, only whether a
  `ReplaceWithTsString`-style edit is accepted once something reaches the view.
  `ComplexConcControl_WiresViewAsReadOnly` checks the real Designer-generated wiring, not
  just a synthetic test harness.
- `PatternView.AllowDisplaySelection` had to be added (it did not previously exist on
  this branch) because `ReadOnlyView = true` suppresses `Activate()` by default
  (`SimpleRootSite.AllowDisplaySelection` defaults to `IsEditable`), which would hide the
  selection the chooser insert/delete buttons need the user to see.

**Should the invariant live in `PatternVcBase`/`PatternView` itself, so a third subclass
can't reintroduce this?** Yes, partially, and I did not do the full version here. Two
independent things could move:

1. **`UpdateProp` could have a base-class default that returns `tssVal` instead of
   throwing.** `PatternVcBase` doesn't override `VwBaseVc.UpdateProp` at all today, so
   the *unimplemented* default is inherited from `VwBaseVc`. A `PatternVcBase.UpdateProp`
   override doing exactly what both subclasses currently do by hand (`RuleFormulaVcBase`
   and now `ComplexConcPatternVc`) would mean a third subclass gets the safe behavior
   automatically instead of needing to remember to add it. **I did not make this change.**
   It only affects the crash-avoidance half of the story (which, per the ablation above,
   is the *entire* story for this bug but was *not* sufficient for the sibling's, where
   `ktptEditable` was load-bearing) -- moving it to the base class doesn't, by itself,
   protect a future subclass that binds a real field via `AddStringAltMember`, which is
   the actually dangerous case. Given `ComplexConcPatternVc.UpdateProp` and
   `RuleFormulaVcBase.UpdateProp` are now textually identical one-liners, hoisting it is
   almost pure duplication removal with no behavior change for either existing subclass --
   a safe follow-up, but it touches `RuleFormulaVcBase.cs` on the sibling's own unmerged
   branch, so I left it as a documented recommendation rather than doing it here to avoid
   a cross-branch collision on a file I don't own in this task.
2. **The "not free text" fragment-marking discipline cannot be hoisted as cheaply.**
   Marking editability is inherently per-fragment (each subclass alone knows which of its
   own `AddProp`/`AddStringAltMember` calls binds to a mutable channel), so there's no
   single base-class change that forces a third subclass to mark its fragments correctly.
   The closest structural guard I can identify without redesigning the VC pattern: give
   `PatternVcBase` a protected helper (`MarkNotEditable(vwenv)`, effectively what
   `SetNotEditable` is here) so at least the *mechanism* is shared and discoverable, and
   have `PatternVcBase.AddExtraLines` (which already does this for filler lines) serve as
   the precedent a new subclass's author is likely to copy. I did not hoist `SetNotEditable`
   itself, since it is a one-line wrapper and duplicating it costs less than adding
   cross-subclass coupling for a helper this small; I would reconsider if a third
   subclass appears.

`ReadOnlyView`/`AllowDisplaySelection` are already control-level and already shared
(`PatternView`), so nothing further to hoist there -- the risk is a future `PatternView`
consumer forgetting to set `ReadOnlyView = true` on its own control, which is a Designer.cs
wiring mistake no base-class change can prevent.

## 2. What can be removed or simplified?

Nothing was removed. The specific candidate, per the task brief, was
`PatternView.PatternEditingHelper.CanCut()`/`CanPaste()`
(`Src/LexText/LexTextControls/PatternView.cs`), which look redundant now that
`ComplexConcControl` also runs with `ReadOnlyView = true` (matching `RuleFormulaControl`'s
state *on the sibling's own branch*). They were **not** removed here, for a
branch-topology reason rather than a functional one: on *this* branch,
`RuleFormulaControl.Designer.cs` still sets `ReadOnlyView = false` (the sibling's flip to
`true` lives only on the unmerged `fix/phon-rule-formula-readonly` branch). From this
branch's point of view, the override is still load-bearing for the rule-formula editor,
so removing it now would reduce coverage for a consumer this task did not touch. This is
the flip side of the sibling's own review note ("kept because `ComplexConcControl` still
depends on it") -- once both branches are integrated and *both* consumers set
`ReadOnlyView = true`, `CanCut`/`CanPaste` become fully redundant with
`EditingHelper.CanCut`/`CanPaste`'s own `Editable`-gated base behavior, and should be
removed then. **Noting this for whoever integrates both branches, per the task brief,
rather than editing `fix/phon-rule-formula-readonly`.** `CanCopy()` stays regardless: the
base implementation doesn't consult `Editable`, so it was never redundant.

## 3. What was not fixed, and why

- **Zero-width-space boundary markers** (`PatternVcBase.OpenSingleLinePile`/
  `CloseSingleLinePile`, `kfragZeroWidthSpace` on `ktagLeftBoundary`/`ktagRightBoundary`)
  are not individually marked `ktptNotEditable`, in either `PatternVcBase` subclass. This
  is a pre-existing, shared gap the sibling's own review flagged and left open for the
  same reason: these are invisible cursor-parking glyphs used for boundary navigation, not
  literal or bound content, and `UpdateProp` (now overridden in both subclasses) absorbs
  any edit attempt there harmlessly. Confirmed by reasoning, not by a dedicated test --
  I did not add one, since it would exercise the identical `UpdateProp` no-op path already
  covered by the eleven fragment tests, not a new mechanism.
- **`PatternVcBase.UpdateProp` base-class hoist** -- see section 1. Left as a
  recommendation, not implemented, to avoid touching `RuleFormulaVcBase.cs` on a branch I
  don't own.
- **`ConstChartVc.cs:297`** -- out of scope for this bug; already tracked as
  SUSPECTED-safe-by-reading-only in `phon-rule-direct-editing.md`, unchanged by this work.
- **Live IME/drag-and-drop reproduction** -- as with the sibling bug, this was inferred
  (the bug report itself says "if input reaches the view without passing through
  PatternView.OnKeyPress"), not reproduced with a real IME or a real OS-level drag
  operation. `ReadOnlyView = true` closing the keyboard-controller registration is the
  categorical fix for that path; see section 4 for what still needs a live check.

## 4. What needs manual verification in a running FLEx

- Open Texts & Words -> Complex Concordance, build a pattern with at least one Word and
  one Morph node with Form/Gloss/Category/Entry/Infl Features all populated, and confirm
  the pattern builder still renders identically to before this change (no visual
  regression from the `ktptEditable`/`ReadOnlyView` changes).
- With a vernacular IME active, place focus in the pattern builder and attempt to compose
  and commit text into a feature line. Confirm composition does not commit into the pane
  at all (rather than committing and then silently reverting) -- this is what
  `ReadOnlyView = true` should prevent categorically.
- Confirm the selection highlight is still visible when a chooser-inserted item is
  selected (this is what `PatternView.AllowDisplaySelection` restores), and that the
  Insert/Search controls still operate against the right selection.
- Confirm Delete still removes the selected item in the live UI, matching
  `DeleteKey_StillRaisesRemoveItemsRequested_WhenRootsiteIsReadOnly`.
- Try dragging text onto the pattern-builder pane; confirm it is rejected/does nothing,
  rather than being accepted and then not visibly changing anything (the two are
  distinguishable to a user who's watching the drop target, even though neither corrupts
  data).
