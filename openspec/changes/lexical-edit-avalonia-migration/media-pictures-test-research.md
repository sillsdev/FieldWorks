# §19d — Audio + Pictures editable parity: test research (T0)

Research note for the §19.0 T0–T5 rubric. Maps the legacy WinForms behaviors
(`PictureSlice` + `InsertPictureDialog`/`PicturePropertiesDialog`, and
`AudioVisualSlice` / `LabeledMultiStringView` voice-WS audio) and the picture
dialogs to the tests that lock them on the Avalonia surface, and records the
audio cross-platform ship/defer decision.

## Sources read (legacy truth)

| Concern | Legacy file | Behavior |
| --- | --- | --- |
| Picture display + properties | `Src/Common/Controls/DetailControls/PictureSlice.cs` | thumbnail from `ICmPicture.PictureFileRA.AbsoluteInternalPath`; double-click → `PicturePropertiesDialog`; OK → `picture.UpdatePicture(file, null, DefaultPictureFolder, 0)` in a UOW |
| Picture insert | `Src/xWorks/DTMenuHandler.cs` `OnInsertPicture` (~216) | `PicturePropertiesDialog` (null initial); OK → `MakeNewObject(CmPictureTags.kClassId,…)` then `picture.UpdatePicture(dlg.CurrentFile, null, DefaultPictureFolder, 0)` in one UOW |
| Picture properties dialog | `Src/FwCoreDlgs/PicturePropertiesDialog.cs` | file pick + Palaso ImageToolbox metadata (caption/copyright/creator/license); copy/move/leave file via `MoveOrCopyFilesController` |
| Picture ORC (rich text) | `Src/Common/Framework/FwEditingHelper.cs` `InsertPictureOrc` (~455) | `pict.InsertORCAt(tss, ich)` → `SetString`/`SetMultiStringAlt`; ORC run uses `kodtGuidMoveableObjDisp` (tag 8) |
| Audio play (CmMedia slice) | `Src/Common/Controls/DetailControls/AudioVisualSlice.cs` | `System.Media.SoundPlayer` for WAV, else shell-launch the file |
| Audio play/record (voice WS) | `Src/Common/Controls/Widgets/LabeledMultiStringView.cs` (~323) | one libpalaso `ShortSoundFieldControl` per IsVoice WS; record writes a new filename into the WS multistring alt, delete clears it |
| Voice WS storage | (same) | the WS alternative's TsString **text is the relative audio filename**; absolute path = `LinkedFilesRootDir`/`media`/filename |

## Avalonia side (current state, read-only)

- Pictures: `RegionFieldControlFactory.BuildImage` (read-only `Image`); composer
  `FullEntryRegionComposer.WalkPictures` emits a non-editable
  `RegionFieldKind.Image` row (`isEditable:false`); model comment at
  `LexicalEditRegionModel.cs:112` defers insert/caption to §19d.
- Audio: composer flags an IsVoice alternative `isAudio:true` and substitutes the
  `AudioRecordingReadOnly` placeholder text; `FwMultiWsTextField` (~99) renders it
  read-only with the audio tooltip.
- Edit seam: `IRegionEditContext` had text/option/reference/paragraph setters but
  **no picture and no audio** methods; `ComposedRegionEditContext` routes by StableId.

## Design decisions (§19d)

1. **Picture metadata model**: `Caption` and `Description` are real
   `ICmPicture` multistring properties → round-trippable on an in-memory cache
   (no real image file needed) → T1/T4 testable directly. **License/Creator**
   live in the image file's Palaso metadata (need a real file); the dialog
   captures all four, the adapter writes Caption/Description to LCModel always
   and applies license/creator to the file metadata when a real file is present.
2. **Picture edit seam**: new `IRegionEditContext` methods
   `TryInsertPicture` / `TryReplacePictureFile` / `TryDeletePicture` /
   `TrySetPictureMetadata`, keyed by StableId like the paragraph setters, each
   one undoable step via the shared fenced `Stage(...)`.
3. **Picture ORC** (§19c→§19d close): `RegionRichTextEditAlgorithms.InsertPictureOrc`
   builds the `kodtGuidMoveableObjDisp` run (managed, LCModel-free) over a caret;
   the xWorks rich-text setter resolves the guid to a created `ICmPicture` via a
   new `IRegionEditContext.TryInsertPictureObject` that returns the ORC ObjectData.
4. **Audio play/record**: managed device layer in FwAvalonia (`net48`,
   `System.Windows.Forms`/`System.Drawing` already referenced; **NAudio 1.10.0**
   available). Play = NAudio `AudioFileReader`+`WaveOutEvent` (falls back to
   `System.Media.SoundPlayer` for WAV). Record (Windows) = NAudio `WaveInEvent`
   to a project media WAV file. The WS value (a filename string) writes through
   the **existing text path** — the read-only `IsAudio` gate is lifted so the
   composer makes the audio row editable-via-affordance, and clear writes empty.

## Audio cross-platform ship/defer

- **Ship now**: play (NAudio, cross-platform-capable output) + record on Windows
  (NAudio `WaveInEvent`) + clear/attach the WS value. The audio row is no longer a
  blanket read-only placeholder.
- **Defer to `avalonia-end-game`**: macOS/Linux microphone capture (NAudio WASAPI/
  WaveIn is Windows-only). The recorder device abstraction (`IFwAudioRecorder`)
  has a Windows implementation; non-Windows throws a "record on Windows for now"
  notice. Marked `// PARITY §19d (cross-platform record → avalonia-end-game)`.

## Behavior × edge × workflow → test map (T1–T5)

### T1 unit (real in-memory LCModel for the adapter; mocked device for audio)
- `TryInsertPicture` creates an `ICmPicture` in the field vector with caption.
- `TryReplacePictureFile` swaps the file on an existing picture.
- `TryDeletePicture` removes the picture object from the vector.
- `TrySetPictureMetadata` writes Caption + Description (LCModel round-trip).
- `InsertPictureOrc` builds a run whose ObjectData[0] == tag 8 (picture ORC).
- Audio: composer makes the IsVoice row editable (not the blanket placeholder);
  `TrySetText` on the voice WS writes the filename; empty clears it (model write
  asserted, **device mocked**).
- Picture dialog VM: caption/license/creator edit + OK gating + cancel.

### T2 integration (one realized surface, multi-item)
- Detail surface with a picture field + an audio (voice WS) field + a text field:
  insert a picture (with metadata) AND record/clear audio AND edit text on one
  realized `LexicalEditRegionView`; assert one undo step per gesture and the
  others' state intact.

### T3 edge
- Missing/invalid picture file; unsupported format; delete-then-undo; empty
  caption; very large image (display sanity, no crash); audio value cleared;
  dialog cancel-vs-commit.

### T4 workflow (real cache round-trip)
- Insert picture → set caption + creator → commit → re-compose → the `ICmPicture`
  + Caption/Description persisted.
- Attach audio to a voice WS → commit → re-compose → the filename persisted + the
  row exposes the play affordance.

### T5 visual (PNG, then AssertNoCrowding, read the PNGs)
- Picture field with thumbnail + insert/replace/delete/edit affordances.
- Picture-properties dialog.
- Audio field with play/record affordances.
</content>
