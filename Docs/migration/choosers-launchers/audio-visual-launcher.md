# Audio/Visual Launcher (`AudioVisualLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.AudioVisualLauncher` (`Src/Common/Controls/DetailControls/AudioVisualSlice.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwOptionPicker (atomic owned control) |
| **JIRA** | LT-XXXXX |

## What it is
The button + filename view for a media (audio/visual) field; subclasses `ButtonLauncher` and launches the media player along with the embedded filename display. Defined inside `AudioVisualSlice.cs`.

## Notes / gotchas
- Involves a linked media file path and an external player — porting must handle the LinkedFiles path resolution and the launch of the player, not just a chooser.
- Owned control inside `AudioVisualSlice`; not a standalone dialog.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
