# §19a — Editable StText (multi-paragraph structured text) on the Avalonia lexical-edit surface

## Problem

StText-typed fields (Sense.Definition / Discussion, Entry.Comment, example
sentence/translation, extended-note discussion, …) render **read-only** on the
Avalonia surface today: `FullEntryRegionComposer.WalkOtherField`'s
`OwningAtomic → IStText` branch flattens the paragraphs to a single joined
read-only string. That is a functional regression versus the WinForms
`StTextSlice` (a RootSite rich editor that edits paragraph text, adds/deletes
paragraphs, and sets per-paragraph named styles, one undo step per edit). It is
also the explicitly-recorded deferral in
`RegionViewingServices.Deferred` ("StText multi-paragraph editing").

## Goal & boundary

Bring StText fields to editable parity with `StTextSlice`: edit paragraph text,
add/delete paragraphs, set per-paragraph named style, one undo step per
edit/gesture. Phase 1 / derisk: WinForms stays switchable, nothing deleted, the
view stays **LCModel-free** (LCModel work lives in the xWorks composed
edit-context), rendering is managed Avalonia. ORC embedded objects inside StText
are **§19c.3 — out of scope**: ORC-bearing paragraphs stay read-only/preserved
(reusing the existing `CanEditRichText` lossless-preserve path), marked
`// PARITY §19c.3`.

## Design — consistent with the existing region model + edit-context patterns

This deliberately mirrors the established run-aware text path
(`FwMultiWsTextField` / `RegionRichTextEditAlgorithms` / `RegionRichTextAdapter`)
and the reference-vector structural-gesture path
(`FwReferenceVectorField` + `gestureCompleted` immediate commit + host re-show).
It invents **no parallel system**.

### 1. Region model (FwAvalonia, LCModel-free)

* New `RegionFieldKind.StructuredText`.
* New `RegionParagraph` (LCModel-free): an ordered paragraph of an StText —
  * `RegionRichTextValue Text` — the run model the run-aware editor already edits
    (reuses the lossless `RichXml` round-trip + `CanEditRichText`/`LossyProperties`
    read-only safety; an ORC/lossy paragraph is held read-only, §19c.3).
  * `string ParagraphStyle` — the paragraph's named style id (`StStyle`/
    `StPara.StyleName`), or null for the default.
  * `bool CanEditText` — false for an ORC/lossy paragraph (mirrors
    `RegionRichTextValue.CanEditRichText`).
* `LexicalEditRegionField` gains:
  * `IReadOnlyList<RegionParagraph> Paragraphs`
  * `IReadOnlyList<string> AvailableParagraphStyles` (host-supplied, like
    `AvailableNamedStyles`): the project's **paragraph**-type style names for the
    per-paragraph style picker. Empty ⇒ the picker is suppressed.

### 2. Edit-context seam (LCModel-free contract, xWorks-implemented)

`IRegionEditContext` gains paragraph CRUD, keyed by `field.StableId` + paragraph
index (the same StableId keying every other setter uses). All default to `false`
on the base context (only the composed full-entry context implements them):

* `bool TrySetParagraphText(field, int paragraphIndex, RegionRichTextValue value)`
* `bool TrySetParagraphStyle(field, int paragraphIndex, string styleName)`
* `bool TryInsertParagraph(field, int afterParagraphIndex)`
* `bool TryDeleteParagraph(field, int paragraphIndex)`

Each routes through the shared `RegionEditContextBase.Stage(setter, label)` so it
opens the one fenced `LcmRegionEditSession` lazily and commits/cancels as **one
undoable step** — identical undo-granularity rule to the rest of the region.

### 3. Managed editor (FwAvalonia) — `FwStructuredTextField`

A vertical `StackPanel`, one bordered/dense row per paragraph:

* **Text:** a run-aware `TextBox` reusing the exact staging logic
  `FwMultiWsTextField` uses (TextChanged → `RegionRichTextEditAlgorithms.
  ApplyPlainTextEdit` → `editContext.TrySetParagraphText`; Ctrl+B/I/U span
  formatting; grapheme-cluster bidi navigation). `AcceptsReturn = false`.
* **Add paragraph:** Enter at paragraph end, **and** a per-row "+" affordance →
  `TryInsertParagraph(after)`.
* **Delete paragraph:** Backspace in an empty paragraph (when >1 paragraph),
  **and** a per-row delete affordance → `TryDeleteParagraph(index)`.
* **Paragraph-style picker:** a per-row "Paragraph Style" button opening the
  shared `FwOptionPicker` seeded from `AvailableParagraphStyles` (+ a "Default"
  clear entry) → `TrySetParagraphStyle`.
* ORC/lossy paragraph (`!CanEditText`): read-only `TextBox` with the existing
  `EmbeddedObjectReadOnly` tooltip (`// PARITY §19c.3`).
* Stable AutomationIds: `{automationId}.Para.{i}`, `.Para.{i}.Style`,
  `.Para.{i}.Add`, `.Para.{i}.Delete`. `IDisposable` teardown like the other
  owned editors.

**Commit timing (mirrors the reference-vector rule):** per-paragraph **text**
edits stage and ride the region view's focus-loss autosave (one step per field
edit). **Structural** gestures (add/delete/style) commit **immediately** via the
`gestureCompleted`/`Save` callback, because `Paragraphs` is a compose-time
snapshot — without an immediate commit + host re-show the inserted/deleted
paragraph would not appear (LCModel broadcasts PropChanged only at EndUndoTask).
Wired in `RegionFieldControlFactory.Build` next to `ReferenceVector`.

### 4. Adapter (xWorks, LCModel-aware) — `FullEntryRegionComposer`

Replace the read-only `OwningAtomic → IStText` flatten with a
`RegionFieldKind.StructuredText` row:

* Build `RegionParagraph`s from `stText.ParagraphsOS.OfType<IStTxtPara>()`:
  `Text = RegionRichTextAdapter.FromTsString(par.Contents, wsFactory)`,
  `ParagraphStyle = par.StyleName`, `CanEditText = Text.CanEditRichText`.
* `AvailableParagraphStyles = ParagraphStyleNames()` (memoized,
  `Cache.LangProject.StylesOC` filtered to `StyleType.kstParagraph`).
* Register paragraph CRUD setters (keyed by StableId) that mutate the LCModel
  StText inside the open fenced UOW, mirroring `StTextSlice`/the legacy view:
  * text → `par.Contents = RegionRichTextAdapter.ToTsString(value, …)`.
  * style → `par.StyleName = styleName`. LCModel forbids a null/empty
    `StyleName`, so "clear" reverts to the default paragraph style
    (`StyleServices.NormalStyleName`), matching the picker's "Default" entry.
  * insert → `IStTxtParaFactory`/`stText.AddNewTextPara` (or
    `MakeNewObject(StTxtParaTags…, StTextTags.kflidParagraphs, ord)`), empty
    contents in the field's default WS — same as `StTextSlice.OnEnter`'s create.
  * delete → `stText.ParagraphsOS.RemoveAt(index)` (guard: never delete the last
    paragraph to nothing → keep one empty paragraph, like the legacy editor).
* A null StText is materialized on first edit exactly as `StTextSlice.OnEnter`
  does (MakeNewObject StText + one empty StTxtPara), inside the fenced session.

The composed `ComposedRegionEditContext` overrides the four new methods, routing
through `Stage`, falling back to `false` when no setter registered (read-only /
ORC paragraph).

## Undo-granularity rule

One `LcmRegionEditSession` (one global undo step) per edit/gesture, opened lazily
by `Stage` on the first staged write, labeled with the field
("Undo change to Definition"). Text edits commit on focus loss; add/delete/style
commit immediately. Cancel rolls back every staged write including a
just-created paragraph/StText. This is the **same** rule the rest of the region
already follows.

## Deferred (named, not silent)

* **ORC embedded objects inside StText (§19c.3):** ORC/lossy paragraphs stay
  read-only, lossless-preserved (existing `CanEditRichText` path), tooltip
  shown. `// PARITY §19c.3` at each gate.
* `RegionViewingServices.Deferred` "StText multi-paragraph editing" entry is
  updated: text + add/delete + paragraph-style are now editable; only ORC
  paragraphs remain read-only (pointing at §19c.3).
