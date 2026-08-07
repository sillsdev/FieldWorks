# Migration-doc capture — shared language

Scoped context for the legacy-screenshot capture work that feeds `Docs/migration/`.
Extends the root [`CONTEXT.md`](../../CONTEXT.md); only adds terms specific to this bounded context.

## Terms
- **Legacy truth PNG**: a screenshot of a *legacy WinForms* FLEx surface (UIMode=Legacy),
  captured as the visual parity baseline for its migration doc. Stored in
  `Docs/migration/**/images/<screen>-NN.png`. NOT an Avalonia screenshot.
- **Capture target**: a screen that needs a legacy truth PNG — a **tool** (Area/Tool screen),
  a **list editor**, or a **dialog**. Tracked in `INVENTORY.md`.
- **Launch-per-tool capture** (option 2): drive FLEx to a **tool** by launching
  `FieldWorks.exe -db "<language project>" "<silfw link>"` where the link
  (`silfw://localhost/link?app=flex&database=<db>&tool=<toolId>&guid=<guid>`, see
  `FwLinkArgs.kFwUrlPrefix`) makes the shell open that tool at startup
  (`FieldWorks.cs` → `FwAppArgs` → `HasLinkInformation`). Then screenshot the main window.
- **Dialog screenshot harness** (option 3): a standalone WinForms host that opens a
  **language project** `LcmCache` (the `LCMBrowser` pattern), constructs a legacy dialog
  (`new XDlg()` + `SetDlgInfo(cache, …)` — FLEx's two-phase dialog init), shows it with
  seeded data, and renders it to a PNG. Reaches dialogs that navigation can't (conditional
  delete/restore/warning dialogs) and doubles as the future legacy↔Avalonia parity rig.
- **Reachable set**: capture targets achievable by option 2 or 3. EXCLUDES Views/Gecko-coupled
  dialogs (need a live `IVwRootBox`/`IVwSelection`, e.g. `RelatedWords`) and the 12 non-visual
  Phase-2 internals — those stay **on-pickup**.
- **Capture manifest**: machine-readable list (toolId / dialog factory → target doc + image
  path) the script and harness iterate over; derived from `INVENTORY.md`.

## Invariants
- Capture in **UIMode=Legacy** only (this is the legacy baseline). The Avalonia surface has a
  separate headless path.
- Data source for representative states: the **Sena 3** language project.
- A capture writes to the image path its doc already references; capturing never edits the
  language project (open read-only / discard; close with Cancel/Escape).

## Open questions
- Headless rendering: `Control.DrawToBitmap` works for ordinary dialogs but not for embedded
  native **Views** content — confirm per-dialog during the harness build; fall back to a
  visible/VDD desktop + window screenshot where DrawToBitmap is blank.
