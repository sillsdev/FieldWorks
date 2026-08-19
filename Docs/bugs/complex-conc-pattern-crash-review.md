# Review: ComplexConcPatternVc direct-edit crash (architecture self-review)

**Revision note:** this review was corrected after adversarial review found two errors in
the first draft: (1) the fragment-targeting test helper was inert for 9 of 11 "fragment
angle" tests (it always selected the same whole-object range regardless of the tag
argument), so the claimed breadth was inflated; (2) a genuinely unmarked gap existed for
the OR/`#` literals' zero-width-space boundary run. Both are fixed; the corrected ablation
below also reverses part of the original conclusion -- `ktptEditable`, once complete, turns
out to be independently sufficient to stop the crash too, not merely "defense in depth" as
first claimed, and it prevents a real, demonstrated display-corruption artifact that
`UpdateProp` alone does not.

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

### Corrected ablation

The first draft of this review claimed `UpdateProp` alone was "necessary and sufficient"
and that `ktptEditable`/`ReadOnlyView` were pure defense-in-depth. That was measured with a
test helper that turned out to be inert for most fragments (see the Testing section
below) and is **wrong on the "defense in depth" characterization**. Redone with a fixed
helper that provably targets the requested fragment (`AssertSelectionTargets`, checked via
`IVwSelection.TextSelInfo`), plus the previously-missing `ktptEditable` marking on the
zero-width-space boundary run, the real ablation is:

| Configuration | Result (18-test suite) |
|---|---|
| Shipped state (`UpdateProp` + complete `ktptEditable` + `ReadOnlyView`) | 18/18 pass |
| `UpdateProp` removed, `ktptEditable` complete | 18/18 pass |
| `UpdateProp` present, `ktptEditable` neutralized everywhere | 16/18 pass -- the 2 failures are exactly the two tests that assert `IsEditable` directly; every crash/content test still passes |

So, once `ktptEditable` is *actually complete* (see the persistence bug below), **either
layer independently stops the crash** for every fragment enumerated. This is a different
finding from the sibling bug, where `ktptEditable` was the *only* load-bearing layer
(`AddStringAltMember`-bound fragments write straight to the real property without ever
calling `UpdateProp`). Here neither fragment binds a real field, so `UpdateProp`'s no-op is
a true no-op for the *model* regardless of `ktptEditable`.

**But `UpdateProp` alone is not equivalent to `ktptEditable`, because of what it does to
the *display*.** With `ktptEditable` neutralized and `UpdateProp` intact, a direct probe
(temporarily reverting `ktptEditable`, performing an edit, then re-selecting the same
fragment without any `Reconstruct`) showed the model correctly unchanged (`Form: 'original'`)
but the **redisplayed text read `'HACKEDForm: original'`** -- the discarded edit's text
was prepended into the cached display run and did not self-correct. `UpdateProp` returning
`tssVal` unmodified only means "no crash and no model write"; it does not mean the view's
own cached rendering of that fragment is refreshed to match. `ktptEditable` prevents this
because the edit never gets that far -- confirmed by `SelectionOverFormLine_IsNotEditable`
and `SelectionOverZeroWidthBoundaryRun_IsNotEditable`, both of which fail (red) if their
respective marking is removed and pass (green) with it present (verified by mutation, not
assumed).

**Conclusion:** `UpdateProp` is required (it is the only thing standing between a crash and
survival if `ktptEditable` is ever incomplete, including for a fragment nobody has thought
to mark yet). `ktptEditable` is also required, not for defense-in-depth against the crash,
but because it is the only layer that also prevents the visible, corrupted-looking display
artifact just described.

### A production bug found while fixing the test helper

While redoing the ablation, `SelectionOverFormLine_IsNotEditable` failed unexpectedly
(`IsEditable == true`) even though `ComplexConcPatternVc.DisplayFeatures` calls
`SetNotEditable(vwenv)` once before its *first* `AddProp` call (`ktagType`). A diagnostic
probe confirmed: the first `AddProp` after `SetNotEditable` is correctly marked, but the
*second* `AddProp` sharing that same earlier call (`ktagForm`, `ktagGloss`, etc.) is not --
`ktptEditable` set via `vwenv.set_IntProperty` does not persist across multiple `AddProp`
calls the way `vwenv.Props = someBuilder` does; it must be re-asserted immediately before
*every* `AddProp` call, which is exactly the pattern `PatternVcBase.AddExtraLines` and
`RuleFormulaVcBase` already use (and which the first draft of this fix did not follow
consistently). Fixed by adding a `SetNotEditable(vwenv)` call before each individual
`AddProp` in `DisplayFeatures`, `DisplayInflFeatureLines`, `DisplayInflFeatures`, and the
four multi-line bracket/paren pile-hook sequences (UpHook/Ext-loop/LowHook), none of which
had previously been re-asserted per call. This means most of the `ktptEditable` markings
claimed in the original fix were only accidentally correct for single-`AddProp` call sites
(OR, `#`, min/max, brackets/parens themselves) and were **not actually taking effect** for
the feature lines (Form/Entry/Category/Gloss/Infl) or the multi-line pile glyphs until this
correction.

### Should the invariant live in `PatternVcBase`/`PatternView` itself?

Yes, partially, and I did not do the full version here. Two independent things could move:

1. **`UpdateProp` could have a base-class default that returns `tssVal` instead of
   throwing.** `PatternVcBase` doesn't override `VwBaseVc.UpdateProp` at all today, so the
   *unimplemented* default is inherited from `VwBaseVc`. Given `ComplexConcPatternVc.UpdateProp`
   and `RuleFormulaVcBase.UpdateProp` are now textually identical one-liners, hoisting it
   is almost pure duplication removal with no behavior change for either existing
   subclass -- a safe follow-up, but it touches `RuleFormulaVcBase.cs` on the sibling's own
   unmerged branch, so I left it as a documented recommendation rather than doing it here
   to avoid a cross-branch collision on a file I don't own in this task.
2. **The "not free text" fragment-marking discipline cannot be hoisted as cheaply**, and
   the persistence bug above is exactly the argument for trying: marking editability is
   inherently per-fragment and per-call (each subclass alone knows which of its own
   `AddProp` calls binds to a mutable channel, and the property must be re-asserted before
   each one), so there's no single base-class change that forces a third subclass to mark
   its fragments correctly or to remember the per-call re-assertion rule. The closest
   structural guard I can identify without redesigning the VC pattern: give `PatternVcBase`
   a protected `SetNotEditable`/`MarkNotEditable` helper (already present here, and already
   the pattern `AddExtraLines` uses) so at least the *mechanism and its per-call-site
   convention* are shared and discoverable. I did not hoist it in this task, since it is a
   one-line wrapper and duplicating it costs less than adding cross-subclass coupling for a
   helper this small -- I would reconsider if a third subclass appears, and would make the
   per-call convention an explicit doc comment on the shared helper if I did.

`ReadOnlyView`/`AllowDisplaySelection` are already control-level and already shared
(`PatternView`), so nothing further to hoist there -- the risk is a future `PatternView`
consumer forgetting to set `ReadOnlyView = true` on its own control, which is a Designer.cs
wiring mistake no base-class change can prevent.

### `ReadOnlyView` -- corrected framing

**`ReadOnlyView = true` does not prevent the crash.** This was directly demonstrated by
mutation testing: in every ablation run above, `ReadOnlyView` was `true` throughout, and
whether a given configuration crashed was governed entirely by `UpdateProp`/`ktptEditable`,
never by `ReadOnlyView`. `ReadOnlyView` does not gate `IVwSelection.ReplaceWithTsString` at
all -- it is unrelated to the crash mechanism.

Its proven value is narrower and different: it unregisters the keyboard/IME controller hook
(`SimpleRootSite.cs` -- `UnsubscribeFromRootSiteEventHandlerEvents`, called from the
`ReadOnlyView` setter), which is the categorical fix for the IME-composition/drag-and-drop
path the bug report actually named as the likely real-world trigger. That specific claim
(that it closes the IME channel) has **not** been verified against a live IME or drag
operation in this task -- see section 4. It is kept because closing that channel is still
worthwhile even though it does not touch the crash mechanism, and because the sibling
branch already establishes the same pattern for the rule-formula editor.

### An unremarked behaviour change: `AcceptsReturn`/`AcceptsTab`

`SimpleRootSite.ReadOnlyView`'s setter also forces `AcceptsReturn = AcceptsTab = false`
when set to `true`. Checked directly against `ComplexConcControl.Designer.cs`'s generated
code (`ComplexConcControl_AcceptsTabUnchanged_AcceptsReturnNowFalse`):

- **`AcceptsTab` is unchanged.** The Designer already sets `AcceptsTab = false`
  unconditionally, independent of `ReadOnlyView`, before this fix. Tab already moved focus
  out of the pane; this fix does not change that.
- **`AcceptsReturn` changes from `true` to `false`.** The Designer explicitly set
  `AcceptsReturn = true`; the `ReadOnlyView = true` assignment that follows it in
  `InitializeComponent`'s generated ordering overrides that back to `false`. This is a
  real behaviour change this fix introduces, not the Tab regression a first guess might
  expect.

Since `PatternView.OnKeyPress` already swallows Return unconditionally (it is not
Backspace/Delete), the practical difference is only *where* the keystroke is disposed of:
previously `IsInputKey(Return)` returned `true`, so the key reached the control and was
silently swallowed there; now it returns `false`, so the key is never delivered to the
control and is processed as an ordinary dialog/navigation key by whatever contains the
pane instead. `ComplexConcControl` is hosted as a Words-area tool pane
(`DistFiles/Language Explorer/Configuration/Words/Concordance/toolConfiguration.xml`), not
inside a modal dialog with an `AcceptButton` -- a repo-wide grep for `AcceptButton` finds it
only on this feature's own *editing* dialogs (`ComplexConcMorphDlg`, `ComplexConcWordDlg`,
`ComplexConcTagDlg`), none of which host this control -- so no default-button activation is
expected in the pane's real hosting context. That is reasoning from the wiring, not a live
verification; see section 4.

## 2. What can be removed or simplified?

Nothing was removed. The specific candidate, per the task brief, was
`PatternView.PatternEditingHelper.CanCut()`/`CanPaste()`
(`Src/LexText/LexTextControls/PatternView.cs`), which look redundant now that
`ComplexConcControl` also runs with `ReadOnlyView = true` (matching `RuleFormulaControl`'s
state *on the sibling's own branch, PR sillsdev/FieldWorks#1082*). They were **not**
removed here, for a branch-topology reason rather than a functional one: on *this* branch
(and on current `origin/main`), `RuleFormulaControl.Designer.cs` still sets
`ReadOnlyView = false` -- the sibling's flip to `true` lives only on the unmerged PR #1082,
which cites "`ComplexConcControl` still runs with `ReadOnlyView = false`" as its own
explicit justification for keeping `CanCut`/`CanPaste`. This branch's change removes that
premise. Traced directly (not just by analogy): `SimpleRootSite.ReadOnlyView`'s setter is
literally `EditingHelper.Editable = !value`, and the base `EditingHelper.CanCut()`/
`CanPaste()` both open with `if (... && m_fEditable) ...` and otherwise return `false` --
so once **both** consumers set `ReadOnlyView = true`, the base class already returns
`false` unconditionally for both, with no dependency on the override; `CanCut`/`CanPaste`
become provably dead code once both branches land. **Not edited here** -- `PatternView.cs`
belongs partly to PR #1082's own review position, and the decision of when to remove them
belongs to whoever reconciles the two branches, not to this task.

`CanCopy()` stays regardless: the base implementation doesn't consult `Editable`, so it was
never redundant.

## 3. What was not fixed, and why

- **`PatternVcBase.UpdateProp` base-class hoist** -- see section 1. Left as a
  recommendation, not implemented, to avoid touching `RuleFormulaVcBase.cs` on a branch I
  don't own.
- **`ConstChartVc.cs:297`** -- out of scope for this bug. Status has moved since the first
  draft of this review: it is no longer "suspected safe" but a concrete suspicion of a
  Bug-1-class *corruption* defect (`ApplyFormatting`'s `vwenv.Props = ttp` appears to be a
  full property-bag replace that discards the cell-level `ktptNotEditable` `MakeCellsMethod`
  sets, immediately before a real, shared `ICmPossibility` is bound via
  `AddStringAltMember`). Unconfirmed -- a probe attempt hit an `ArgumentException`
  constructing the selection. This is a separate, already-flagged investigation; explicitly
  out of scope for this task.
- **Live IME/drag-and-drop reproduction, and the `AcceptsReturn` consequence** -- inferred
  from wiring, not reproduced with a real IME, a real OS-level drag operation, or a live
  check of what (if anything) receives an un-delivered Return keystroke. See section 4.

## 4. What needs manual verification in a running FLEx

- Open Texts & Words -> Complex Concordance, build a pattern with at least one Word and one
  Morph node with Form/Gloss/Category/Entry/Infl Features all populated, and confirm the
  pattern builder still renders identically to before this change (no visual regression
  from the `ktptEditable`/`ReadOnlyView` changes -- the persistence-bug fix touched every
  multi-line pile and feature-line call site).
- With a vernacular IME active, place focus in the pattern builder and attempt to compose
  and commit text into a feature line. Confirm composition does not commit into the pane at
  all -- this is `ReadOnlyView`'s specific, as-yet-unverified claim.
- Confirm the selection highlight is still visible when a chooser-inserted item is selected
  (this is what `PatternView.AllowDisplaySelection` restores), and that the Insert/Search
  controls still operate against the right selection.
- Confirm Delete still removes the selected item in the live UI, matching
  `DeleteKey_StillRaisesRemoveItemsRequested_WhenRootsiteIsReadOnly`.
- Press Enter/Return while focused in the pattern-builder pane and confirm nothing
  unexpected happens (no default button activates, focus does not jump unexpectedly) --
  this is the `AcceptsReturn` change's live consequence, reasoned about but not observed.
- Try dragging text onto the pattern-builder pane; confirm it is rejected/does nothing.
