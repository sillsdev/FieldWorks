# Bug 1 — Rule formula cells are directly editable, and edits rename the underlying phoneme / natural class

**Area:** Grammar → Phonological Rules / Affix Processes (rule formula slices)
**Type:** Data corruption
**Related prior work:** LT-21888 (the keystroke filter this report argues is insufficient)

## Symptom

The rule formula view is specified to be modifiable only by (a) inserting an item through a chooser and (b) deleting an item. In practice users are sometimes able to modify the content of a cell directly. Because the view renders the *referenced object's own* text field, an edit that lands does not merely corrupt the rule — it renames the phoneme or natural class project-wide, affecting every other rule that references it.

## Root cause

Editability is never denied at the view level. It is only filtered at the control level, and the filter covers exactly one input path.

### The fragments are editable

`RuleFormulaVcBase.Display` renders the substantive parts of a rule with plain string-alternative calls against the referenced object's real multistring property:

- `RuleFormulaVcBase.cs:287-303` — `kfragNC` calls `AddStringAltMember` on the natural class's `Abbreviation` / `Name`.
- `RuleFormulaVcBase.cs:305-312` — `kfragTerminalUnit` calls `AddStringAltMember(PhTerminalUnitTags.kflidName, ...)`, i.e. the phoneme's or boundary marker's live `Name`.

None of `kfragNC`, `kfragTerminalUnit`, `kfragFeatureLine`, or `kfragFeats` sets `ktptEditable = TptEditable.ktptNotEditable`. The **only** place that property is set anywhere in the `RuleFormulaVcBase` / `PatternVcBase` chain is on the blank filler lines: `PatternVcBase.cs:209` (`AddExtraLines`).

### The rootsite is not read-only

`RuleFormulaControl.cs:1153` sets `m_view.ReadOnlyView = false`.

### The only guard is a WM_CHAR filter

- `PatternView.cs:135-149` — `OnKeyPress` swallows every character except Backspace and Delete. The comment cites LT-21888, i.e. this was itself added as a bug fix.
- `PatternView.cs:41-54` — a custom `PatternEditingHelper` hard-codes `CanCopy` / `CanCut` / `CanPaste` to `false`.

Anything that reaches the root box without going through `OnKeyPress` or the clipboard helper is unguarded. Candidates, in rough order of likelihood for FLEx users:

1. **IME composition.** Vernacular-script keyboards commit text through IME messages rather than plain WM_CHAR. This is the most probable real-world trigger and matches the "sometimes" in the report.
2. Drag-and-drop text onto the view.
3. Any other rootsite entry point that mutates the selection's string property directly.

**Status: CONFIRMED by code reading.** The `ReadOnlyView = false` setting, the absence of `ktptNotEditable` on substantive fragments, and the single-path keystroke filter are all directly verified. The specific IME mechanism is **inferred, not reproduced** — see Verification below.

## Proposed fix

Enforce the invariant where it belongs, in the view constructor, rather than patching input paths one at a time.

1. In `RuleFormulaVcBase`, wrap the substantive fragments in `ktptEditable = TptEditable.ktptNotEditable` before the `AddStringAltMember` calls at `RuleFormulaVcBase.cs:287-312`, and likewise for the feature-line and feature fragments.
2. Consider setting `m_view.ReadOnlyView = true` at `RuleFormulaControl.cs:1153`. This needs checking against the delete path — `PatternView.OnKeyDown` (`PatternView.cs:120-149`) intercepts Delete/Backspace and raises `RemoveItemsRequested` rather than editing text, so a read-only rootsite may still be compatible with deletion, but this must be verified, not assumed.
3. Keep the `OnKeyPress` filter as defence in depth. Do not remove it as part of this fix.

Option 1 alone is likely sufficient and is the lower-risk change.

## Verification required before closing

- Reproduce the original defect with an IME / vernacular keyboard against an unpatched build. Without a repro we are fixing an inferred mechanism.
- Confirm delete still works on all three rule kinds after the change: regular phonological rules, metathesis rules, affix processes.
- Confirm the fix covers metathesis rules. `MetaRuleFormulaControl` / `MetaRuleFormulaVc` were not read line by line; they share `RuleFormulaVcBase` and reuse `CmdCtxtSetFeatures`, so they are expected to share the defect, but this is unverified.

## Scope

Independent of Bug 2 (natural class vs. phonological features) and Bug 3 (affix process clone on sense split). No shared code paths beyond both Bug 1 and Bug 2 living in the rule formula UI.

## Key files

| Path:line | Role |
|---|---|
| `Src/LexText/Morphology/RuleFormulaVcBase.cs:287-312` | Renders NC abbreviation and phoneme name as editable strings |
| `Src/LexText/LexTextControls/PatternVcBase.cs:209` | The only `ktptNotEditable` in the chain (filler lines only) |
| `Src/LexText/LexTextControls/PatternView.cs:41-54` | Clipboard guard |
| `Src/LexText/LexTextControls/PatternView.cs:120-149` | Keystroke filter and delete interception |
| `Src/LexText/Morphology/RuleFormulaControl.cs:1153` | `ReadOnlyView = false` |
