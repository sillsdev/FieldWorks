# Phase-1 Avalonia (New UI) — Tester Burn-Down Guide

**Audience:** FieldWorks testers exercising the switchable "New UI" Lexicon surface.
**Goal of this round (task §19h.4):** drive the Phase-1 defect list to zero so the derisk PR
can land and Phase 2 (`avalonia-end-game`, the full WinForms cut-over) can begin.

This is a **derisk** build: the new Avalonia surface runs *behind the existing WinForms shell*.
Nothing is deleted; you can switch back to the classic UI at any time. If something is broken
in New UI, the classic UI is always one toggle away.

---

## 1. How to turn New UI on / off

1. **Tools → Options → UI Mode**.
2. Set the **UI Mode** chooser to **New** (the default is **Legacy**).
3. Click **OK**. The current window re-shows on the new surface — no tool reload.
4. To go back, set UI Mode → **Legacy** the same way.

The setting persists per user. Switching is non-destructive and can be done mid-session.

---

## 2. What is covered vs. what falls back

| Area | New UI in this build? |
|---|---|
| **Lexicon — entry edit (detail view)** | ✅ Yes |
| **Lexicon — browse/list view + bulk edit** | ✅ Yes |
| Grammar, Notebook, Lists, Words tools | ⟲ Auto-fallback to Legacy (by design — not in Phase-1 scope) |

If you open a non-Lexicon tool while UI Mode = New, it quietly uses the classic surface.
That is expected, not a bug.

---

## 3. What to exercise (by category)

Please hit each area, and especially the **integration journeys** at the bottom — the cross-feature
flows are where subtle composition/undo bugs hide.

**19a — Multi-paragraph text (StText):** Definition, Discussion, Comment, example fields.
Add/delete paragraphs (Enter adds, Backspace at start deletes), apply a paragraph style, edit text,
save, reopen. Confirm the *only* paragraph can't be deleted.

**19b — Grammatical info / feature structures:** On an entry's grammatical info (MSA), open the
feature-structure editor; expand nodes, pick values, nest. Inflection class on stem/derivational MSAs.
Create-new-POS flow.

**19c — Rich text depth:** Apply a **named character/paragraph style** to a selection; **retag a
run's writing system**; verify per-run fonts render. Insert/edit/delete a **hyperlink**.

**19d — Media & pictures:** Insert / replace / delete a **picture** with caption/license/creator
(managed file picker). **Audio** rows: play, record (Windows), clear.

**19e — Field types:** Enum closed-combos (reject free text), integer fields (reject non-numeric),
**GenDate** (circa/precision/era + exact-date calendar), semantic-domain tree vectors, literal labels,
"show hidden fields" toggle, per-field writing-system visibility.

**19f — Browse remainders:** Right-click **row context menu**; **Rapid Data Entry** new row;
copy/paste cells (Ctrl+C / Ctrl+V); **drag-reorder columns**; **Find/Replace** (incl. diacritic-insensitive);
bulk-edit tabs (List Choice / Bulk Copy / Clear / Click Copy / Process-Transduce / Delete);
**export visible rows to CSV**.

**19g — Dialogs:** Delete-confirmation (with orphan warnings), Writing-System properties,
LexReferenceDetails, special-character / character-map insert.

### High-value integration journeys (please run at least these)
1. **Create → detail → re-browse:** RDE new row → open in detail → set MorphType (enum) +
   inflection class (from POS) + Definition (multi-paragraph) + a paragraph style → save → filter
   browse → confirm the new entry shows with the right gloss.
2. **Undo granularity:** In one entry, edit the lexeme + a multi-paragraph Definition + add a
   semantic domain. Ctrl+Z should undo **one gesture per press**, in order — not all at once, not
   one-step-per-character.
3. **Browse filter → bulk delete → undo:** Filter, multi-select rows, bulk-delete (confirm the
   dialog, try Cancel then confirm), then Ctrl+Z restores the rows + selection.
4. **RTL / complex-script:** Enter and edit Arabic (RTL) and Khmer (subscript consonants) content
   in a Definition and a browse cell. Confirm no reordering, truncation, or character corruption on
   save/reopen. *(Headless round-trip is already proven; we want real on-screen confirmation.)*

---

## 4. Intentionally deferred — please do **NOT** file these as bugs

These are documented Phase-1 boundaries (deferred to a later stage or to Phase 2 `avalonia-end-game`):

- **Graphite shaping** — the only intentional rendering drop (Skia/HarfBuzz shaping instead).
- **Styles *management* dialog** (create/edit style definitions). Applying existing styles works;
  the FwStylesDlg editor stays in classic UI for now.
- **Cross-platform audio *recording*** on non-Windows. Playback works everywhere; record is Windows-only
  in this build (a tooltip says so where unavailable).
- **Footnote full editing** inside text (scripture-coupled). Footnotes render and can be deleted.
- **Reversal-entry** GO/move dialog and **Occurrence** min/max picker (non-Lexicon-tool surfaces).
- **Import/Export dialogs** (LIFT/SFM/CSV wizards) — except the browse "Export visible rows (CSV)".
- **OS print** and the full legacy XHTML/XML configured-export.
- **Deep/arbitrary nested embedded layouts (jtview)** beyond a bounded depth (falls back gracefully).
- Non-Lexicon tools (Grammar/Notebook/Lists/Words) running on the new surface.

If you hit one of these and the classic UI is reachable, that's working as designed.

---

## 5. How to report a bug

For each issue, please capture:
- **UI Mode** (New) and the **tool** (Lexicon edit vs browse).
- **Exact steps** + the entry/field involved; whether the **classic UI** does it correctly.
- **Writing system / script** if relevant (RTL, Khmer, other complex scripts are high-priority).
- A **screenshot**, and the **undo behavior** if the bug involves an edit.
- Whether toggling back to Legacy and forward to New reproduces it.

File against the Phase-1 derisk list. We are driving this list to **zero** before the PR.
