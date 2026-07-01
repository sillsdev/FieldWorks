# §19c rich-text DEPTH — test research note (T0)

Phase-1 §19c adds rich-text DEPTH for the Avalonia lexical-edit editors over the
EXISTING run model (`RegionRichTextValue`/`RegionTextRun`,
`RegionRichTextEditAlgorithms`, `RegionRichTextAdapter`). This note maps the
RootSite formatting + Styles + ORC-by-kind behaviors × edges × workflows to the
tests that pin them (the §19.0 T0–T5 rubric). Design decisions are FINAL (see the
task brief); this note records the behavior contract and the ship/defer table.

## Scope (decided — build to these)

1. **StructuredText pickers** — add the SAME span-based character-style picker +
   WS-retag picker that `FwMultiWsTextField` already has to
   `FwStructuredTextField`'s per-paragraph rows (reuse the exact pattern:
   `FwOptionPicker` flyout → snapshot selection → `ApplySpanNamedStyle` /
   `RetagSpanWritingSystem` → `TrySetParagraphText`). The per-paragraph PARAGRAPH
   style picker stays; the new pickers act on the run-level character style / ws of
   the TextBox selection, unified onto the same row. Style/WS lists are
   host-supplied (`AvailableNamedStyles` / `AvailableWritingSystems` on the field —
   already populated by the composer; LCModel-free view).
2. **Per-run WS font rendering (both editors)** — inline-display-on-blur /
   editable-TextBox-on-focus swap. When a value/paragraph has >1 run with differing
   WS/style and is NOT focused, render a read-along `TextBlock` with one `Run`
   inline per text run, each `Run` carrying its own `FontFamily`/`FontStyle`/
   `FontWeight` from a host-supplied per-WS font map; swap to the editable `TextBox`
   on focus, back to the display on blur.
3. **ORC kind-aware** — lift the blanket `ObjectData ⇒ read-only` block:
   - External link / hyperlink (`kodtExternalPathName`): insert / edit / delete
     fully (managed `GetURL`-equivalent on the `ObjectData` string;
     `AddHyperlink`/`MarkTextInBldrAsHyperlink` on the xWorks adapter write path).
   - Generic ORC delete: ANY ORC run is deletable (remove the run) regardless of
     kind.
   - Picture ORC: render (read) + deletable; insert + caption-edit DEFERRED to
     §19d (precise `// PARITY §19c → §19d` note).
   - Footnote ORC: render (read) + deletable; full editing DEFERRED (scripture
     `StText`/`IScrFootnote`) (precise `// PARITY §19c` note).
   - The lossy-property guard (genuinely unsupported run props: colour, offset,
     superscript, …) STAYS read-only — that is data-safety, not ORC.

## ORC-by-kind ship / defer table

`FwObjDataTypes` first char of `RegionTextRun.ObjectData` selects the kind:

| Kind | ObjData first char | Read (render) | Delete run | Insert | Edit |
|------|--------------------|---------------|-----------|--------|------|
| External link / hyperlink | `kodtExternalPathName` (4) | yes | yes | **§19c (ship)** | **§19c (ship): edit URL** |
| Picture | `kodtGuidMoveableObjDisp` (8) | yes | yes | DEFER §19d | DEFER §19d (caption) |
| Footnote | `kodtOwnNameGuidHot` (5) / `kodtNameGuidHot` (3) | yes | yes | DEFER (scripture) | DEFER (scripture) |
| Unknown / other ORC | any other | yes | yes | — | — |
| (not ORC) lossy prop run | n/a | yes | n/a | — | read-only (data-safety, unchanged) |

Net hard requirement: ORC is **no longer a blanket read-only block** — a value/
paragraph carrying ONLY link/ORC runs (no genuinely-unsupported props) is editable
to the extent of: link insert/edit/delete + generic ORC delete. A run carrying a
genuinely-unsupported TsString property still forces read-only.

## Behavior contract → tests (T1–T5)

### RootSite formatting (char style + ws retag on StructuredText)
- A StructuredText paragraph row with available char styles exposes a char-style
  picker keyed `<id>.Para.<n>.CharStyle`; picking a style stages
  `TrySetParagraphText` with the covered runs restyled; "Default" clears. — **T1**
- A row with available writing systems exposes a ws-retag picker keyed
  `<id>.Para.<n>.WritingSystem`; picking retags the covered runs. — **T1**
- No available styles/ws ⇒ no affordance; lossy/read-only paragraph ⇒ no
  affordance. — **T1, T3**
- Selection snapshot on flyout open, cluster-snapped span, mixed-selection
  pre-select, collapsed-caret no-op. — **T3** (the algorithms are already pinned by
  the multi-WS tests; these re-pin the StructuredText wiring).

### Per-run font display swap (both editors)
- A multi-run value with differing WS/style, unfocused, renders a `TextBlock` with
  per-run `Run` inlines each carrying its own font from the host map; focus swaps
  to the editable `TextBox` (same text), blur swaps back. — **T1**
- A single-run / uniform value never builds the display layer (the plain TextBox is
  enough). — **T1**
- The swap preserves the staged edits (focus → type → blur shows the new runs). —
  **T3**

### ORC by kind
- Link run: value is editable; the link's URL is extractable; insert a link over a
  selection; edit a link's URL; delete the link run. — **T1** (view + algorithm),
  **T4** (real cache round-trip of `ObjData`).
- Generic ORC (picture/footnote/unknown): the value is NOT blanket read-only —
  the ORC run is deletable; picture/footnote insert/caption are NOT offered (the
  affordance is absent), with the precise PARITY note. — **T1**.
- Lossy non-ORC run stays read-only. — **T1** (unchanged, re-pinned).

### Edges (T3)
empty selection; style/ws on a multi-run boundary; retag across runs; RTL + Khmer
run; ORC at run boundaries (start / middle / end); link with no/invalid URL;
cancel-vs-commit; focus/blur swap mid-edit.

### Integration (T2, one realized editor)
type + apply char style to a span + retag a run's ws + insert a link + delete an
ORC + undo each → assert the run model composes, undo grouping per-gesture,
round-trips through the adapter to a TsString with correct run props/ObjData.

### Workflow (T4, real cache)
edit field → select word → apply named style → retag another run's ws → insert a
hyperlink → commit → reopen → verify run props + the link `ObjData` round-tripped.

### Visual (T5)
PNGs: styled span; multi-WS multi-run with visibly different fonts in DISPLAY mode;
link ORC; ORC selected-for-delete — before `AssertNoCrowding`; READ each PNG and
answer the six subjective-quality questions; full-project run.

## Files touched (ownership)
- `Src/Common/FwAvalonia/Region/FwStructuredTextField.cs` — add char-style + ws
  pickers + the font-display swap.
- `Src/Common/FwAvalonia/Region/FwFieldControls.cs` (`FwMultiWsTextField`) — add the
  font-display swap; link insert/edit/delete + generic ORC delete affordances.
- `Src/Common/FwAvalonia/Region/LexicalEditRegionModel.cs` — `RegionRichTextValue`
  kind-aware ORC editability; ORC-kind helpers; per-WS font map seam on the field;
  `RegionRichTextEditAlgorithms` link/ORC span helpers.
- `Src/xWorks/RegionValueFactory.cs` (`RegionRichTextAdapter`) — kind-aware
  `FromTsString` (ORC no longer blanket read-only); link write on `ToTsString`.
- `Src/xWorks/FullEntryRegionComposer.cs` — populate the per-WS font map; link
  setter on the StText + text paths; paragraph char-style/ws setters already exist.
- `Src/Common/FwAvalonia/FwAvaloniaStrings.{cs,resx}` — new link/insert strings.
- TEST files (see below).

## Test files (mapped)
- `FwAvaloniaTests/StructuredTextFieldTests.cs` / `StructuredTextEdgeCaseTests.cs`
  — StructuredText pickers + font swap + ORC-in-paragraph (T1/T3/T5).
- `FwAvaloniaTests/RegionEditingTests.cs` — link insert/edit/delete + generic ORC
  delete + font swap on `FwMultiWsTextField` (T1/T2/T3).
- `FwAvaloniaTests/RegionModelTests.cs` — `RegionRichTextEditAlgorithms` link/ORC
  span helpers + ORC-kind classification + editability (T1/T3).
- `xWorksTests/StructuredTextAdapterTests.cs` / `StructuredTextWorkflowTests.cs` /
  `LexicalEditRegionEditingTests.cs` — adapter ORC round-trip, link write, workflow
  (T2/T4).
