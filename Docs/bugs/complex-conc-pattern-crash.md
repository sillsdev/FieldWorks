# Bug 4 — Complex Concordance pattern builder crashes on any direct edit

**Area:** Texts & Words → Complex Concordance → pattern builder pane (`ComplexConcControl`)
**Type:** Crash (unhandled exception), not data corruption
**Found by:** adversarial review of Bug 1 (`phon-rule-direct-editing.md`), which shares the same base classes

## What the user sees

Texts & Words area, **Complex Concordance** tool. The top-left pane is a pattern builder: you insert
Morph / Word / Tag / OR / Word Boundary pieces from a row of options, and each appears as a bracketed
column with labelled rows (Form, Gloss, Cat, Entry, Type, Infl) filled in through choosers. Like the
phonological rule formula, it is meant to be built by inserting and deleting, never by typing.

If input reaches the view without passing through `PatternView.OnKeyPress` — IME composition when
typing a vernacular script, or dragging text into the pane — FLEx dies with an unhandled
`NotImplementedException`. No warning, no "field not editable" feedback. Because the trigger is an
IME path, it would preferentially hit vernacular-script users.

## Why it happens

`ComplexConcControl` and the rule formula editor are built from the same two classes: `PatternView`
(`Src/LexText/LexTextControls/PatternView.cs`) and `PatternVcBase`
(`Src/LexText/LexTextControls/PatternVcBase.cs`). `PatternVcBase` has exactly two subclasses and
`PatternView` exactly two consumers — the rule formula editor and this one — so the audit surface is
closed.

Two differences from the rule formula editor make this a crash rather than a rename:

1. **No `UpdateProp` override.** `RuleFormulaVcBase` overrides `UpdateProp`, so an edit reaching the
   view is intercepted and absorbed. `ComplexConcPatternVc` (`Src/LexText/Interlinear/ComplexConcPatternVc.cs`)
   has no such override, so the engine falls through to `VwBaseVc.UpdateProp`, which throws
   `NotImplementedException`. Nothing catches it.
2. **No wall.** `ComplexConcControl.Designer.cs:57` still sets `ReadOnlyView = false`, and none of
   `ComplexConcPatternVc`'s fragments are marked `ktptNotEditable`. Bug 1 closed both of these for the
   rule formula editor; this control was left as-is.

## Why it is NOT the Bug 1 corruption

`ComplexConcPatternVc` binds no real domain fields — a grep for `AddStringAltMember` in that file
returns zero. Its content is synthetic: `ComplexConcPatternNode.Hvo` values are negative sentinels and
`Form`/`Gloss`/etc. are plain in-memory properties served by `ComplexConcPatternSda`, not LCM objects.
So a stray edit cannot rename a shared phoneme or natural class the way Bug 1's could. It just throws.

Crash is louder but arguably less dangerous than Bug 1's silent project-wide rename. Both are real.

## Evidence

`Docs/bugs/ComplexConcPatternVcDirectEditProbeTests.cs` (seeded in this worktree) is a working probe
written during Bug 1's adversarial review. It builds a real `ComplexConcGroupNode` /
`ComplexConcWordNode` / `ComplexConcPatternSda` / `PatternView` exactly as `ComplexConcControl.Init`
does, then calls `IVwSelection.ReplaceWithTsString` on the word node's Type line
(`ComplexConcPatternVc.ktagType`). Result: unhandled `NotImplementedException` from
`VwBaseVc.UpdateProp` propagating out of `ReplaceWithTsString`.

Treat the probe as a starting point to verify independently, not as a finished test.

## Not yet established

Whether the crash is reachable in a running FLEx via a real IME or drag-and-drop, as opposed to via a
direct `ReplaceWithTsString` call in a test. This is the same open question Bug 1 has, and it is why
the probe proves the *mechanism* rather than the *user path*.

## Related

- `phon-rule-direct-editing.md` — Bug 1, the sibling defect in the other `PatternVcBase` subclass.
- `ConstChartVc.cs:297` has the same defect *shape* as Bug 1 (binds a shared `CmPossibility` field)
  but appears guarded at the cell level by `MakeCellsMethod.cs:495`. Marked SUSPECTED-safe by code
  reading only; never verified by execution.
