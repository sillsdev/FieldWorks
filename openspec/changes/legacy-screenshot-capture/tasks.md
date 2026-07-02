# Tasks — legacy-screenshot-capture

Tooling-only change. Scope = reachable Phase-1 set, Sena 3 data, UIMode=Legacy. Validate with
`openspec validate legacy-screenshot-capture --strict`.

## Block 1 — Capture manifest + shared infra
- [x] 1.1 Generate `manifests/tools.csv` (`toolId, folder, imagePath`) from `INVENTORY.md` + tool docs.
      (dialogs registry lives in the harness fixture, not a CSV — see Block 3.)
- [x] 1.2 UIMode=Legacy is the default; the worktree's `Output/Debug/FieldWorks.exe` is launched.
- [x] 1.3 Window-capture via PrintWindow (PW_RENDERFULLCONTENT) in the script; blank-detection via file size.

## Block 2 — Option 2: launch-per-tool capture script
- [x] 2.1 `scripts/migration-capture/Capture-LegacyTools.ps1`: guid-less silfw link per tool, launch,
      wait for load, PrintWindow capture → imagePath, graceful close (releases project lock).
- [x] 2.2 Proven on lexiconBrowse / concordance / semanticDomainEdit (correct tool, non-blank).
- [x] 2.3 Ran all 67 targets: **67/67 captured**; log `manifests/tools-capture.log`; wired into docs.

## Block 3 — Option 3: dialog screenshot harness  (PIVOT: piggyback test fixture, not standalone exe)
- [x] 3.1 `LexTextControlsTests/ScreenshotHarnessTests.cs` — `[Explicit]` `[Category("ScreenshotHarness")]`
      fixture on the in-memory-cache base (reg-free COM + LcmCache already bootstrapped). See design.md decision.
- [x] 3.2 Generic `Capture(Form, file, docRelDir)` helper (off-screen Show + DrawToBitmap + non-blank assert).
- [x] 3.4 Proven on `FeatureSystemInflectionFeatureListDlg` → `feature-chooser-01.png` (green, visually verified).
- [ ] 3.5 Wire the remaining constructable dialogs (one `[Test]` each; bespoke seed + `SetDlgInfo`).
      Dialogs outside LexTextControls reuse the same fixture pattern in their own test project. INCREMENTAL —
      best done as each dialog's JIRA is worked; unmapped/Views-coupled → recorded `on-pickup` in the ledger.

## Block 4 — Wire-in, ledger, gates, retrospective
- [x] 4.1 Tool/list docs (66) + feature-chooser doc carry their `## What it looks like` image ref.
- [x] 4.2 `capture-ledger.md` reconciles targets → captured / on-pickup; `INVENTORY.md` capture state.
- [x] 4.3 `./test.ps1 -TestProject LexTextControlsTests` build green with the fixture added.
- [x] 4.4 Retrospective: capture recipes folded into `fieldworks-winapp`; ledger entry in the hub skill.
