# Visual snapshot testing (PNG artifacts + layout tripwire)

How to *see* what a headless Avalonia surface actually renders, and how to gate it. This is the
subjective check that complements the deterministic spacing/border standard
(`DialogTheme.axaml` tokens) and the `DialogLayoutAssert.AssertNoCrowding` tripwire documented in
`SKILL.md` and `dialog-conversion.md`. Use it for **every** surface you build or change — dialogs,
region (detail) views, and the browse table — not just dialogs.

## Why both checks

- **`DialogLayoutAssert.AssertNoCrowding(view)`** — deterministic hard-fail. Catches zero-area text,
  sibling overlap, host-border frames missing, content butting padded edges, dialog-root padding.
  A regression becomes a red test. This is the gate.
- **PNG snapshot (`DialogSnapshot.Capture`)** — a real Skia-rendered frame written to disk. Catches
  what geometry asserts cannot judge: stray strikethrough/`TextDecorations`, lost selection highlight,
  wrong font/weight, color/contrast, baseline misalignment, "looks crowded but technically legal."
  The agent (via the Read tool) and the user open the PNG and judge whether it looks *nice*.

Run **both**. They catch different defect classes.

## The harness

`DialogSnapshot` (in `Src/Common/FwAvalonia/FwAvaloniaTests/Visual/DialogSnapshot.cs`, namespace
`FwAvaloniaTests.VisualChecks`) renders a surface with the Skia-backed headless backend
(`TestAppBuilder` sets `UseHeadlessDrawing=false`) and saves a PNG:

```csharp
using FwAvaloniaTests.VisualChecks;
using FwAvaloniaDialogsTests;            // DialogLayoutAssert (shared tripwire; link the .cs into your test csproj)

[AvaloniaTest]
public void MyDialog_DrivesItsStages()
{
    var view = /* build + show the real surface (dialog body, region view, browse view) */;

    // 1. Capture the INITIAL stage first, so the artifact exists for review even when the assert below fails.
    DialogSnapshot.Capture(view, "MyDialog-01-initial");
    DialogLayoutAssert.AssertNoCrowding(view);

    // 2. Drive the dialog to its next state and capture THAT stage too (one PNG per meaningful state).
    vm.SomeText = "casa";                // → populated
    Dispatcher.UIThread.RunJobs(); view.UpdateLayout(); Dispatcher.UIThread.RunJobs();
    DialogSnapshot.Capture(view, "MyDialog-02-populated");

    vm.UseRegularExpressions = true;     // → validation-error / OK-disabled
    Dispatcher.UIThread.RunJobs(); view.UpdateLayout(); Dispatcher.UIThread.RunJobs();
    DialogSnapshot.Capture(view, "MyDialog-03-invalid");
}
```

**Capture at EACH stage** (hard rule — part of the per-dialog definition of done). A dialog test that drives
distinct UI states emits one PNG per state with a consistent `"<Surface>-<NN>-<stage>"` name (e.g.
`InsertEntry-01-initial`, `InsertEntry-02-populated`, `InsertEntry-03-invalid`, `InsertEntry-05-morphtype-chosen`)
so the artifacts sort in interaction order. The harness writes each capture into ONE FLAT folder with a
surface-prefixed file name — `Output/Snapshots/InsertEntry-01-initial.png`,
`Output/Snapshots/Region-02-editable.png`, `Output/Snapshots/Browse-01-initial.png` — so a surface's stages
sort together by name for easy review (pass `surfaceOverride:` to add the prefix for a name without a `-`). Every dialog ends
up with PNGs covering at least its empty, populated, and (where applicable) error/disabled states; the region
view captures its read-only AND editable stages; the browse table its grid stage. A single-static-assertion
test captures at least its one realized state. Capture is **additive** — never weaken or remove the behavior
assertions to add it.

> **If the view is already hosted in a `Window`** (the common dialog-test shape: `new Window { Content = view }`,
> `window.Show()`), snapshot **that window** — `DialogSnapshot.Capture(window, name)` — not the `view`. Re-hosting
> an already-parented control in a second window throws "already has a visual parent". From a later stage,
> `DialogSnapshot.Capture((Window)view.GetVisualRoot(), name)` resolves it.

- Output: `Output/Snapshots/<Surface>-<NN>-<stage>.png` — ONE flat folder under the repo's **gitignored**
  `Output/` folder (ephemeral; never committed). The file name is the snapshot name verbatim (its leading
  segment, before the first `-`, is the surface prefix). `DialogSnapshot.Folder` returns the `Output/Snapshots` root.
- `Capture(surface, name, width, height, surfaceOverride)` hosts a non-`Window` surface in a window of the
  given size (default 420×320) before capturing; a `Window` is captured at its own size. `surfaceOverride`
  prepends the surface prefix when the name does not already carry one.
- **Order matters:** capture BEFORE asserting. The PNG is the evidence you inspect when the assert
  fails (it tells you whether the assert is right or the layout is right — see the splitter note).

## The agent's review step (mandatory — do not skip it)

After running the dialog tests, the agent **MUST Read every per-stage PNG** the run wrote to
`Output/Snapshots/` (the flat folder; the Read tool renders images) and explicitly answer these **subjective-quality
questions per image** — write the answer down, don't just glance:

1. **Alignment** — are the words, columns, icons, labels, and fields aligned to a consistent grid? Do column
   headers line up with their cells, and does each label sit on the same baseline/left edge as its field?
2. **Spacing** — is there appropriate, consistent space between words, fields, rows, and graphics — nothing
   crammed together or drifting too far apart, and dense like WinForms (not airy Fluent default)?
3. **Borders / containment** — do text inputs and editable areas have a clear, consistent border (box in
   dialogs, flat separators in the detail view, grid lines in browse)? Are related items grouped and unrelated
   items separated?
4. **Clipping / overflow** — is any text, label, column header, or icon truncated, clipped, wrapping
   unexpectedly, or running off the edge / under another element?
5. **Overlap** — does anything overlap that shouldn't (text over text, control over control, stray
   `TextDecorations`/strikethrough through text)?
6. **Legibility / consistency** — is the font size/weight consistent and legible, do icons/graphics render at a
   sensible size and align with their text, and are selection/row highlights and colors/contrast correct?

The loop is concrete and unambiguous: **capture → run the tests → Read each PNG → judge → fix the view (or a
`DialogTheme.axaml` token) → re-capture**, until every stage looks right. Treat a just-looks-off PNG as a real
finding **even when `AssertNoCrowding` passes** — the geometry tripwire and the eyeball catch different defect
classes, so passing tests alone do not satisfy the review step.

## Region / browse coverage and the splitter exception

The same gate applies to non-dialog owned surfaces (`LexicalEditRegionView`).
Example: `Src/Common/FwAvalonia/FwAvaloniaTests/Visual/VisualSnapshotTests.cs`.

`DialogLayoutAssert` was authored against dialog layouts; on grid-based region/browse surfaces a
`GridSplitter` (region label/value column) and the browse column-splitter `Border` are drag handles that
by design sit on a column boundary and overlap their neighbors — that is splitter behavior by design,
not the content-overlap defect.

> **Folded in (durable fix done):** the splitter exclusion now lives *inside* the shared sibling-overlap
> check — `DialogLayoutAssert.AssertSiblingsDoNotOverlap` skips any pair where either side is a `GridSplitter`
> or a control whose `Name`/`AutomationId` contains `Splitter` (see `IsSplitterChrome` in
> `DialogLayoutAssert.cs`). The region/browse tests therefore just call `AssertNoCrowding(view)` directly;
> the old in-test `AssertContentLaysOutCleanly` / `IsSplitterChrome` `IsVisible=false` workaround in
> `VisualSnapshotTests.cs` is gone.

## Requirements / gotchas

- The test project must reference `Avalonia.Skia` and configure `UseHeadless(UseHeadlessDrawing=false)`
  (already true for `FwAvaloniaTests` / `FwAvaloniaDialogsTests`). Without Skia, `CaptureRenderedFrame`
  returns null and `DialogSnapshot.Capture` throws with that diagnostic.
- Realize before capturing: `Capture` does `Show()` + `Dispatcher.UIThread.RunJobs()` for you when it hosts a
  non-`Window` surface. If you pass a surface that is **already** hosted/shown in a `Window`, pass that
  `Window` (see the already-hosted note above) so `Capture` does not try to re-parent the live control.
- To use `DialogSnapshot` / `DialogLayoutAssert` from a test project that doesn't own them, add a
  `<Compile Include="..\…\X.cs" Link="Visual\X.cs"/>` link (read-only reuse — one shared standard,
  no second copy of the logic). `FwAvaloniaTests` (which owns `DialogSnapshot`) links `DialogLayoutAssert.cs`;
  `FwAvaloniaDialogsTests` (which owns `DialogLayoutAssert`) links `DialogSnapshot.cs` — symmetric, so both
  test projects get both the PNG harness and the geometry tripwire from a single copy of each.
- Snapshots are ephemeral. Don't assert on pixels/bytes beyond "non-empty"; the PNG is for human/agent
  eyes, the geometry tripwire is the deterministic gate.
