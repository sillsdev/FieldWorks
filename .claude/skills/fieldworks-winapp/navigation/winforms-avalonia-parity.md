# WinForms ↔ Avalonia parity capture

Use this route to establish **functional and visual parity** between a legacy WinForms surface and its
Avalonia replacement: drive the live WinForms app through winforms-mcp for the "before", and render the
Avalonia surface headless for the "after", into one folder for side-by-side review.

## When to use

A migration task asks "does the Avalonia X match the WinForms X?" — interlinear editor, a DataTree slice,
a dialog, a browse table. The WinForms side needs the live app (this skill); the Avalonia side is headless
(no app, no MCP).

## Prerequisites

- Setup + reconnect done — see `../references/mcp-setup.md`; `winforms_*` tools must be present.
- Run `../scripts/Preflight-WinFormsMcp.ps1` (build present, package resolves).
- A project with the relevant data (e.g. Words ▸ Analyses parity needs **parsed wordform analyses** — a
  bare project shows an empty interlinear). Restore per `project-loading.md`.
- Evidence folder: `Output/ManualEvidence/<change-id>/` (gitignored) or the change's OpenSpec evidence
  dir if it should join review history.

## The two captures

### Before — WinForms (live, via MCP)
1. `winforms_launch_app` → `path = <repo>\Output\Debug\FieldWorks.exe`; keep the `pid`.
2. Open the project (`project-loading.md`) and navigate to the surface (e.g. Words area → Analyses tool →
   select a wordform that HAS an analysis). `winforms_get_element_tree` first to confirm the target.
3. `winforms_take_screenshot` (pass the `pid` so the hidden-desktop window is captured) →
   `Output/ManualEvidence/<change-id>/winforms-<surface>-<stage>.png`.
4. For workflow parity, exercise the gesture (`winforms_click_element` / `winforms_set_value` /
   `winforms_select_item`) and capture each stage. Headless-safe ops only (see SKILL.md "Core Rules").

### After — Avalonia (headless, no app)
The Avalonia surfaces render through the Skia headless harness in tests, **not** through this MCP:
- Reuse / add a `[AvaloniaTest]` that builds the control and calls
  `DialogSnapshot.Capture(surface, "<Surface>-<NN>-<stage>")`
  (`Src/Common/FwAvalonia/FwAvaloniaTests/Visual/DialogSnapshot.cs`) → `Output/Snapshots/<name>.png`,
  paired with `DialogLayoutAssert.AssertNoCrowding(root)`.
- Run it scoped: `./test.ps1 -SkipNative -TestProject Src/Common/FwAvalonia/FwAvaloniaTests -TestFilter "FullyQualifiedName~<YourVisualTest>"`.
- Read both PNGs with the Read tool and compare structure, alignment, labels, colors, affordances.

## Judging parity (record both, separately)

- **Functional**: does the Avalonia surface support the same gestures and write the same data? This is the
  strong, code-verifiable claim (headless workflow tests + the WinForms walk above).
- **Visual**: does it *look* similar? Only the live WinForms capture is the reference — a code
  reconstruction is not evidence. Note matches and divergences (labels, line set, colors, RTL policy)
  honestly; many legacy colors are project-configurable, so treat exact tints as low-value to chase.

## Exit

Close evidence-only FieldWorks state with the app's normal close (do not save unless the task changed
data on purpose). Leave the captured PNGs in the evidence folder for the user/agent to review.
