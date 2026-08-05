# Avalonia picture editing

Status: retired implementation; picture editing deferred to a native editor
Sources: PR #964; commits `413434fe3` and `90b698101`; removed
`PicturePropertiesDialog*`, `DetailPictureMetadata`, `DetailPictureDialogResult`,
`FwAvaloniaStrings.PictureInsert`/`PictureDelete`,
`AvaloniaHostControlBase.HostedContent`
Human review: PR #964

## Question tested

Could the Avalonia detail view carry picture editing -- a picture row plus a
properties dialog for caption, description, licence, and creator -- as part of
the first slice set?

## Observations

- A picture-properties view, view-model, and two LCModel-free exchange types
  were built and committed, with tests. No launcher was ever written, so
  nothing outside those tests constructed either the view or the view-model.
- Because the view-model compiled, carried a coherent OK gate, and had passing
  tests, it read as a finished feature to a later reader. It was unfinished.
- Host support existed only for the picture path: an accessor handing the
  Avalonia storage provider to a media seam's file picker, whose sole caller
  went away when that seam was removed.
- Localization accessors and their neutral resx rows had to be removed as a
  pair. The localization test reflects over every accessor and asserts it
  resolves from the neutral resx, so splitting the pair across commits fails.
- The legacy WinForms picture path (`FwCoreDlgs/PicturePropertiesDialog`,
  `DetailControls/PictureSlice`) was never touched and still ships.

## What failed or was retired

Picture editing was cut from the Avalonia detail view, which left the dialog
with no consumer on the new path. The dialog, its test, its exchange types, its
localization rows, and the picture-only host affordances were removed. A picture
field in the detail view now composes a labelled Unsupported worklist row until
a native picture editor exists.

## Durable lessons

1. A view plus view-model with no launcher is not a feature -- it is dormant
   code that reads as finished. Judge completeness by whether a product route
   reaches the view, never by whether it compiles and has tests.
2. Remove a localization accessor and its neutral resx row in the same commit;
   the localization test reflects over accessors and fails on a split.
3. LCModel-free exchange DTOs have no independent value. When their only
   consumer goes, they go with it.
4. Host affordances added to serve one seam become orphans when that seam is
   removed. Grep the accessor, not the seam.
5. Prove the legacy path is untouched as part of the removal, not afterwards.

## Evidence needed next time

- A product route -- launcher or slice -- reaching the view before any
  further capability is added to it.
- What a native picture editor must cover: file choice, crop, licence and
  creator metadata, and the LinkedFiles move/copy/leave decision.
- Behavior for a picture already inside the LinkedFiles folder versus one
  outside it, including the overwrite case.
- Whether editing an image inside LinkedFiles modifies the shared file or
  copies it, and how orphans are then cleaned up.

## Decision boundary

This record constrains how completeness is judged and the order in which a
picture view earns capability. A human decides whether picture editing
returns to the Avalonia detail view, which capabilities the first version
carries, and what a native editor must do.

## Do not infer

- Do not restore the removed dialog, its exchange types, or the host accessor
  from history.
- Do not assume the WinForms picture path changed; it did not.
- Do not treat the Unsupported worklist row as a permanent product decision.
- Do not infer that a compiled, tested view-model indicates a shipped feature.
- Do not infer that the removal authorizes a replacement design; no picture
  editor has been specified.
