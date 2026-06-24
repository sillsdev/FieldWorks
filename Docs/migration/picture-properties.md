# Picture Properties (legacy `PicturePropertiesDialog`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.PicturePropertiesDialog` (`Src/FwCoreDlgs/PicturePropertiesDialog.cs`) |
| **Area / tool** | Lexicon › sense picture region › insert / edit picture |
| **Primitive(s)** | plain-form (metadata text fields + image file picker / media) |
| **Canonical reference** | InsertEntryDialog (closest kept canonical for a plain-form with text fields + a picker affordance) |
| **Backed-out Avalonia stub** | `Src/Common/FwAvaloniaDialogs/PicturePropertiesDialogView.cs` (code-only, no `.axaml`) + `PicturePropertiesDialogViewModel.cs` @ git `this branch (recover from history)` |
| **JIRA** | LT-XXXXX |

## What it is
Lets the user choose an image file and edit a picture's metadata (caption, description, license, creator).
Opens when inserting a new picture or editing an existing one in the picture region. Returns the chosen
file plus the edited metadata.

## What it looks like
<!-- CAPTURE: launch legacy FLEx (UIMode=Legacy), open an entry with a picture region and
     insert / edit a picture. See .claude/skills/fieldworks-winapp. -->
![Picture Properties – initial](./images/picture-properties-01.png) <!-- TODO: capture -->

## Behaviour to preserve (parity checklist)
- [ ] Metadata text fields: caption, description, license, creator.
- [ ] Image-file display (shows "(no file chosen)" when none).
- [ ] "Choose image…" button triggers an async file picker.
- [ ] OK caption is "Insert" for a new picture, "OK" when editing.
- [ ] OK gated for a new picture on a chosen file (`!_isNew || !string.IsNullOrEmpty(ImageFile)`); for an existing picture the file is optional (metadata-only edits allowed).

## Migration gotchas
- The Avalonia stub is **code-only** (`PicturePropertiesDialogView.cs`, no `.axaml`) — re-wiring should keep
  the programmatic view or convert it deliberately.
- Stub header (`§19d`): "view-model for the Avalonia picture-properties dialog — the parity replacement for
  the WinForms `PicturePropertiesDialog`. OK snapshots the edited metadata + file into `Result`; the
  launcher's Apply reads it. For a new picture OK is gated on a chosen file … for an existing one the file
  is optional."
- Media handling: the file picker is wired through a `ChooseImageRequested` event handled by the launcher
  (`LcmRegionMediaServices`). Preserve the linked-files / media-path conventions (see `sil-library-reuse`).

## Wiring
- Legacy call site(s): the Legacy picture insert/edit path constructs the WinForms `PicturePropertiesDialog`
  (`Src/FwCoreDlgs/PicturePropertiesDialog.cs`).
- The Avalonia path branched on `UIMode=New` here before back-out: `Src/xWorks/LcmRegionMediaServices.cs` —
  `ShowPictureProperties(...)` (method at line 79; instantiates `PicturePropertiesDialogViewModel` +
  `PicturePropertiesDialogView`, wires the picker through `ChooseImageRequested`, shows modal via
  `AvaloniaDialogHost.ShowModal`, lines ~82–99).
- Re-wiring target: `LcmRegionMediaServices.ShowPictureProperties` re-enters the Avalonia dialog behind
  `UIMode=New`; Legacy keeps `PicturePropertiesDialog`.
