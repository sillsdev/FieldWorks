## Why

The Phase-1 migration backlog now has a markdown doc for every in-scope WinForms component
(`Docs/migration/` — 227 stubs + `INVENTORY.md`), but the docs have **no legacy "truth"
screenshots**. Those PNGs are the reliable visual baseline that guides faithful Avalonia
recreation. The obvious capture route — UIA2 automation via winforms-mcp — **cannot reach most
surfaces**: FLEx's left Area/Tool navigator is a custom-drawn `SilSidePane`/`OutlookBar` not
exposed to UIA2 (no element, no coordinate-click), menu popups are flaky, and ~100 dialogs are
conditional (only appear on specific data/error states). winforms-mcp renders the main window and
open modal dialogs fine, but it cannot *navigate* to them.

Two non-UIA routes, validated against the code, get past this:
- The shell already supports **programmatic navigation**: `FieldWorks.exe` parses its args into
  `FwAppArgs` and, when the link `HasLinkInformation`, opens the named tool at startup
  (`Src/Common/FieldWorks/FieldWorks.cs`; link format `FwLinkArgs.kFwUrlPrefix` =
  `silfw://localhost/link?app=flex&database=<db>&tool=<toolId>&guid=<guid>`).
- A standalone tool can open a **language project** `LcmCache` and host FLEx controls
  (the `LCMBrowser` pattern), so legacy dialogs can be constructed and rendered directly —
  reaching even the conditional dialogs navigation can't.

## What Changes

- **Option 2 — launch-per-tool capture script.** A worktree-aware PowerShell script reads a
  **capture manifest** of tools/lists (derived from `INVENTORY.md`), and for each: launches
  `FieldWorks.exe -db "Sena 3" "<silfw tool link>"`, waits for the main window, screenshots it to
  the target doc's `images/` path, and closes. Covers the ~67 navigable Area/Tool and list-editor
  screens with real data.
- **Option 3 — dialog screenshot harness.** A standalone net48 WinForms host (`ScreenshotHarness`)
  opens the Sena 3 `LcmCache`, and for each entry in a **dialog registry** (dialog name → factory
  that does `new XDlg()` + `SetDlgInfo(cache, …)` with seeded data), shows the dialog and renders it
  to a PNG. Covers the constructable dialogs — including conditional delete/restore/warning dialogs
  that navigation cannot reach. The harness is also the reusable rig for future legacy↔Avalonia
  parity screenshots.
- **Wire-in + ledger.** Each captured PNG is referenced from its `Docs/migration/**` doc; a
  `capture-ledger.md` records captured / skipped (with reason) / on-pickup, and `INVENTORY.md`
  is annotated with capture state.

Scope = the **reachable Phase-1 set**, data from the **Sena 3** language project, **UIMode=Legacy**
only. Explicitly out of scope (stay on-pickup): Views/Gecko-coupled dialogs that need a live
`IVwRootBox`/`IVwSelection` (e.g. `RelatedWords`, `SummaryDialogForm`, dictionary HTML preview) and
the 12 non-visual Phase-2 internals.

This is **tooling only** — a dev script plus a standalone harness project. It adds no product
dependency, changes no shipping code path, and never writes to the language project.

## Capabilities

### New Capabilities
- `legacy-screenshot-capture`: A repeatable way to produce legacy-WinForms truth PNGs for the
  migration docs via (a) link-driven launch-per-tool capture and (b) a dialog-construction harness,
  with a manifest, an output convention, and an explicit reachable-vs-on-pickup boundary.
