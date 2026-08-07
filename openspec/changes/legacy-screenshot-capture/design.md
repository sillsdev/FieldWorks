# Design — legacy-screenshot-capture

Shared language: `Docs/migration/CONTEXT.md`. This change is dev tooling; no product code path
changes, no native (C++) changes.

## Goal & non-goals
- **Goal:** produce legacy truth PNGs for the reachable Phase-1 capture targets in `INVENTORY.md`,
  stored at the image paths the docs already reference, from the Sena 3 language project, in
  UIMode=Legacy.
- **Non-goals:** capturing Views/Gecko-coupled dialogs (need live rootbox/selection), the 12
  non-visual Phase-2 internals, or any Avalonia surface (separate headless path). No edits to the
  language project. Not a parity *diff* tool (the harness merely makes parity capture possible later).

## Capture manifest
A single source both tools read, generated from `INVENTORY.md` (+ each doc's front matter table):
- `tools.csv` — `toolId, area, surface, docPath, imagePath` for option 2.
- `dialogs.csv` — `dialogName, factoryKey, docPath, imagePath, status(reachable|views-coupled|on-pickup)`
  for option 3.
Generation is a small script step so the manifest stays in sync as docs/inventory evolve.

## Option 2 — launch-per-tool capture (PowerShell)
`scripts/Capture-LegacyTools.ps1` (worktree-aware; resolves `Output/Debug/FieldWorks.exe` in the
current worktree):
1. For each `tools.csv` row, build `silfw://localhost/link?app=flex&database=Sena 3&tool=<toolId>&guid=<root-or-empty>`.
2. Launch `FieldWorks.exe -db "Sena 3" "<link>"`; wait for the main window title to settle.
3. Screenshot the main window to `imagePath` (`<screen>-01.png`).
4. Close the process; proceed to the next tool.

Decisions:
- **Relaunch per tool** (not one long-lived instance): robust and stateless — no fragile in-app
  navigation — at the cost of ~one project-load per tool (acceptable, unattended). A future
  optimization could reuse one instance + a follow-link, but relaunch is the safe baseline.
- **Screenshot mechanism:** the same window-capture proven to render (PrintWindow via winforms-mcp,
  or `Capture-Window.ps1` PrintWindow helper). The screenshot is of the **main window** in the
  target tool; Views-rendered content inside is fine as a bitmap (we only need the picture).
- **`guid`:** empty (open the tool's default record) unless a doc needs a specific object.

## Option 3 — dialog screenshot harness (net48 WinForms)
New MSBuild project `Src/Utilities/ScreenshotHarness/ScreenshotHarness.csproj` (`net48`, `x64`,
`WinExe`), modeled on `LCMBrowser` (the precedent for a standalone tool that opens a project cache).
- **Cache open:** open the Sena 3 project read-only via the `ProjectId` / `LcmCache` open path
  `LCMBrowser` uses; build the minimal `Mediator`/`PropertyTable`/`IHelpTopicProvider`/stylesheet
  context the dialogs need.
- **Dialog registry:** `Dictionary<string, Func<HarnessContext, Form>>` mapping a dialog key to a
  factory. Most factories are `new XDlg()` then `SetDlgInfo(cache, …)` (FLEx's two-phase init);
  light ones (`ConfirmDeleteObjectDlg(IHelpTopicProvider)`, `MergeObjectDlg(IHelpTopicProvider)`)
  need only the help provider + seeded args. Each factory seeds representative data so the shot is
  meaningful (e.g. a real entry for delete-confirmation).
- **Render:** show the form off-screen and capture via `Control.DrawToBitmap` into `imagePath`; if a
  dialog embeds native Views and `DrawToBitmap` is blank, fall back to a visible-desktop window
  screenshot and mark it; if it can't construct headless, mark `views-coupled`/`on-pickup`.
- **Invocation:** `ScreenshotHarness.exe --project "Sena 3" --manifest dialogs.csv --out <dir>`;
  one dialog per `--only <key>` for iteration. Runs unattended over the manifest.

Build/run posture: built by the normal traversal (added to the solution) but **not shipped/installed**
— it is a dev utility, like `LCMBrowser`. It references existing legacy assemblies (FwCoreDlgs,
xWorks, LexText.*, FdoUi, Common.Controls); per the repo's dependency-justification invariant, this
is a one-way dev-tool→product reference (no product code references the harness), so it introduces
no new product coupling.

## Decision (2026-06-23): piggyback test fixture, not a standalone exe
The standalone `ScreenshotHarness.exe` proved heavier than expected: dialogs need not just a cache
but a `Mediator` + `PropertyTable` + a `CmObjectUi` per object, and a fresh exe also needs its own
registration-free COM manifest + directory/registry bootstrap. Instead, **piggyback on an existing
test project** (`LexTextControlsTests`) whose `MemoryOnlyBackendProviderRestoredForEachTestTestBase`
already bootstraps reg-free COM + an `LcmCache`. The "harness" is an `[Explicit]` NUnit fixture
(`ScreenshotHarnessTests`) — each capture is a `[Test]` that seeds objects, constructs the dialog
(ctor + `SetDlgInfo`), and renders via `DrawToBitmap`. Lower risk, runs under `./test.ps1`, no new
project/COM manifest. Trade-off: in-memory seeded data instead of full Sena 3 (fine for layout
fidelity), and dialogs split across assemblies use the same fixture pattern in their own test project.
Proven on `FeatureSystemInflectionFeatureListDlg` (feature-chooser).

## Risks & mitigations
- **Headless render blank for native Views dialogs** → detect (blank/near-uniform bitmap), fall back
  to visible-desktop capture, else mark on-pickup. Bounded: most dialogs are plain WinForms.
- **Heavy/variable dialog constructors** → registry is incremental; unmapped/failing dialogs are
  logged and left on-pickup, never block the batch.
- **Link navigation lands on the wrong tool** → verify on 2–3 tools first (title + a known control)
  before the full run; log mismatches.
- **Build state** → the harness needs the managed build; reuse the existing `Output/Debug` assemblies.

## Verification
- Option 2 proven on ≥3 tools (correct tool in shot) before the full run.
- Option 3 proven on `ConfirmDeleteObjectDlg` + `MergeObjectDlg` (non-blank, correct dialog) before
  the full run.
- `capture-ledger.md` reconciles every `INVENTORY.md` reachable target to captured / skipped+reason.
- `./build.ps1` stays green with the harness project added; no product test changes.
