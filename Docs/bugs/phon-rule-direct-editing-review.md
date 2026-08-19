# Review: rule formula cells are directly editable (architecture self-review)

## 1. Is this the right architecture for the invariant?

The invariant is "a rule cell is not free text; it only changes via chooser-insert and
delete." That invariant actually has two independent parts, and each belongs at a
different layer:

- **"This span of text is bound to a real domain object's field and must not accept an
  edit."** This is a per-fragment fact that only the view constructor knows, because only
  it knows which `AddStringAltMember`/`AddProp` calls bind to a real, shared field
  (`PhNaturalClass.Abbreviation`, `PhTerminalUnit.Name`) versus a fake tag or a filler
  line. `RuleFormulaVcBase.Display` is exactly the place this was missing, and it is where
  the fix went (five call sites: the natural-class abbreviation, the terminal-unit name,
  and the three computed feature/variable lines).
- **"This whole widget only accepts chooser-insert and delete, never typed input."** This
  is a fact about the control, not about any one fragment, and it belongs on the rootsite.
  `RuleFormulaControl` now sets `m_view.ReadOnlyView = true` instead of `false`.

Both were previously expressed only as a third thing: an input-path filter
(`PatternView.OnKeyPress` swallowing everything but Backspace/Delete). An input-path
filter is the wrong layer for either fact above -- it has to be re-derived and
re-applied for every new input path (keyboard, IME, drag-and-drop, programmatic paste),
and it says nothing about which content is actually safe to bind live. Fixing at the two
layers above is categorical: a new fragment added to any of the three rule-kind view
constructors is safe by default only if its author remembers to mark it, which is still
not perfect, but a new *input path* into an already-correctly-marked view is safe with no
further action, which is the property the old design did not have.

**An experiment confirms which half is load-bearing.** Temporarily removing the
`ktptEditable` line for `kfragTerminalUnit` while leaving `ReadOnlyView = true` in place
reproduces the corruption again (`ReplaceWithTsString` still renames the phoneme).
Removing it back and only relying on the view-constructor fix, with `ReadOnlyView` never
touched, was already proven sufficient in the first fix iteration. So: the
`ktptEditable` marking is the control that actually stops a direct `ReplaceWithTsString`
call; `ReadOnlyView` does not gate that low-level API at all. `ReadOnlyView = true` earns
its place for a different reason -- it unregisters the keyboard/IME controller hook
(`SimpleRootSite.UnsubscribeFromRootSiteEventHandlerEvents`), which is the categorical fix
for the IME-composition bypass the original report flagged as the most likely real-world
trigger, and it disables `EditingHelper.CanCut`/`CanPaste` so cut/paste menu commands
stop offering to mutate the view. Both layers are necessary; neither is sufficient alone.

## 2. What can be removed or simplified?

Nothing was safely removable. The natural candidate was
`PatternView.PatternEditingHelper.CanCut()`/`CanPaste()`, which look redundant now that
`EditingHelper.CanCut`/`CanPaste` already return `false` whenever `Editable` is `false`
(which it now always is for `RuleFormulaControl`'s view). They were **not** removed,
because `PatternView` is also instantiated directly by `ComplexConcControl` (the complex
concordance pattern builder), which leaves `ReadOnlyView = false` and depends on this same
override to keep cut/paste disabled while still being interactively editable in other
respects. Removing the override would silently enable clipboard paste into that unrelated
feature. `CanCopy()` also stays for a different reason: the base implementation does not
consult `Editable` at all, so it is not made redundant by `ReadOnlyView` -- it is a
deliberate, independent restriction that this fix does not touch.

`PatternView.OnKeyPress` also stays (see LT-21888 in the class's existing comment). It is
not dead: the audit below found several literal separator glyphs and fake-tag-bound
"index"/boundary strings across `MetaRuleFormulaVc`/`AffixRuleFormulaVc` that were never
audited for `ktptEditable` and are outside this bug's scope (see next section). Those
spans do not corrupt real data if edited -- they are not bound to a real field -- but
`OnKeyPress` is the only thing currently stopping a keystroke from reaching them. Removing
it would trade one narrow, already-fixed hole for a wider, unaudited one.

## 3. What was not fixed, and why

- **`ComplexConcPatternVc`** (the complex-concordance pattern builder) extends the same
  `PatternVcBase` and is built on the same "chooser insert/delete only" premise, but was
  not inspected fragment-by-fragment or fixed. It is a different feature with no test
  coverage in this session's reach, and changing its rootsite's editability was
  deliberately left alone (see section 2). It is worth a follow-up audit using the same
  method used here.
- **Literal/fake-tag spans elsewhere in the same VC family** -- bracket glyphs
  (`kfragLeftBracket`/`kfragRightBracket`, via `m_bracketProps`, which never sets
  `ktptEditable`), zero-width boundary markers (`ktagLeftBoundary`/`ktagRightBoundary`),
  and several `MetaRuleFormulaVc`/`AffixRuleFormulaVc` fields (`m_inputCtxtProps`-rendered
  content, `ktagIndex`, `ktagLeftEmpty`/`ktagRightEmpty`) were not individually marked.
  None of them bind to a real, shared domain field the way `kfragNC`/`kfragTerminalUnit`
  do, so an edit landing there cannot rename a phoneme or natural class -- the actual bug
  in scope -- but an edit attempt against a fake tag is unverified territory (it may throw,
  or silently no-op, depending on how the data access layer handles an unknown flid). This
  is exactly the gap `OnKeyPress` is still covering.
- **The IME repro itself.** The bug report's own "Verification required" section already
  flagged this: the IME mechanism was inferred, not reproduced, because it requires a live
  IME/vernacular keyboard. That remains true after this fix; see section 4.

## 4. What still needs manual verification in a running FLEx

- Open a phonological rule (or metathesis rule, or affix process) with an IME or
  vernacular keyboard active, place the insertion point in a rule cell, and confirm that
  IME composition can no longer commit text into the cell (it should behave as if the
  control has no keyboard focus for typing at all, since the keyboard controller no longer
  has this control registered).
- Confirm the selection highlight is still visible when clicking into a rule cell (this
  is what `PatternView.AllowDisplaySelection` restores), and that the chooser
  insert/delete buttons still operate against the right selection.
- Confirm Delete and Backspace still remove the selected item in a live UI for all three
  rule kinds, matching the headless test added here.
- Try drag-and-drop of text onto a rule cell; confirm it either does nothing or is
  rejected, rather than landing.
