---
name: fieldworks-wix6-ui
description: Use this skill for FieldWorks WiX 6 installer GUI work: bundle window does not display, MSI internal UI handoff, WixStdBA theme/layout/assets, dual-directory App + Project Data selection, feature-tree dialogs, full/passive/quiet UI behavior, screenshots, and bundle/MSI UI parity with the old WiX 3 installer. Trigger strongly for any FieldWorks installer UI, GUI, dialog, theme, UX, or DisplayInternalUICondition task.
---

# FieldWorks WiX 6 UI Skill

This skill keeps FieldWorks installer UI work grounded in the actual Burn + MSI architecture. The goal is not a pretty standalone screen; it is a correct installer flow with evidence.

## Load References When Needed

- Read `references/ui-repo-map.md` for the FieldWorks UI file map and checks.
- Read `references/official-ui-notes.md` for WiX Toolset UI/Burn facts.

## First Moves

1. Read `specs/001-wix-v6-migration/BUNDLE_UI.md` and `verification-matrix.md`.
2. Inspect `FLExInstaller/wix6/Shared/Base/Bundle.wxs`, `OfflineBundle.wxs`, `BundleTheme.xml`, `BundleTheme.wxl`, and `FieldWorks.Bundle.wixproj` for bundle UI behavior.
3. Inspect `Framework.wxs`, `WixUI_DialogFlow.wxs`, `GIInstallDirDlg.wxs`, and `GICustomizeDlg.wxs` for MSI UI behavior.
4. If custom actions affect UI, inspect `Shared/CustomActions/CustomActions/CustomAction.cs` and the custom action IDs in `Framework.wxs`.
5. Gather screenshots/logs before changing visual or sequencing behavior. If WinApp MCP is available, use it to drive the visible installer UI and record the controls/states observed.

## Mental Model

- FieldWorks uses Burn/WixStdBA as a prerequisite/bootstrapper shell and the MSI UI for the real FieldWorks choices.
- The expected interactive flow is usually: bundle welcome/license -> prerequisite progress -> MSI internal UI opens for directories/features -> bundle success/failure.
- Seeing two windows during the MSI handoff can be correct: WixStdBA window behind, Windows Installer UI in front.
- Bundle Options UI is intentionally suppressed so MSI owns app/data directories and feature selection.
- Full UI should show MSI internal UI. `/quiet` must not show UI; `/passive` should remain non-interactive/minimal.

## FieldWorks UI Anchors

- `Bundle.wxs`: `bal:WixStandardBootstrapperApplication`, theme payloads, `SuppressOptionsUI`, `MsiPackage`, `bal:DisplayInternalUICondition`, `LogPathVariable`, `Visible`.
- `FieldWorks.Bundle.wixproj`: `StageBundlePayloads` and flat filename staging for theme assets.
- `Framework.wxs`: `MajorUpgrade`, `WIXUI_INSTALLDIR=APPFOLDER`, `WIXUI_PROJECTSDIR=DATAFOLDER`, data/app folder registry searches, custom actions, UIRefs.
- `WixUI_DialogFlow.wxs`: dialog transitions, browse events, path validation, maintenance/update path.
- `GIInstallDirDlg.wxs`: dual directory controls and data-folder lock explanation.
- `GICustomizeDlg.wxs`: feature tree selection UI.
- `CustomFeatures.wxi` and `CustomComponents.wxi`: feature/component definitions that must match the UI tree.

## Common UI Failure Modes

- Bundle appears blank or exits: missing theme payload, BA load failure, condition failure, wrong architecture, or no visible full UI.
- MSI UI never appears from bundle: `bal:DisplayInternalUICondition` not authored, condition false, wrong namespace/extension, or non-full UI mode.
- MSI UI appears in `/quiet` or `/passive`: condition too broad.
- Directory fields do not behave: `WIXUI_INSTALLDIR`, `WIXUI_PROJECTSDIR`, `APPFOLDER`, `DATAFOLDER`, or registry search values are not initialized when the dialog appears.
- Feature tree is wrong: feature IDs, levels, component refs, or feature-group wiring drifted from `CustomFeatures.wxi`/`CustomComponents.wxi`.
- ARP or uninstall UI path is odd: bundle/MSI visibility and maintenance behavior are interacting; use the upgrade/patching skill too.

## UI Change Workflow

1. Identify whether the issue is bundle UI, MSI UI, or handoff between them.
2. Prove the current behavior with screenshots and a bundle log. For MSI UI issues, capture an MSI verbose log too.
3. Make one class of change at a time: theme asset/staging, bundle BA metadata, MSI dialog wiring, custom action/property initialization, or feature authoring.
4. Preserve localization: put translatable strings in existing `.wxl`/`.resx` patterns, not hardcoded source where localization is expected.
5. Validate full, passive, and quiet modes when changing `DisplayInternalUICondition` or dialog sequencing.

## WinApp MCP Click-Through Workflow

Use WinApp MCP for bundle/MSI UI smoke tests whenever the server is available.

1. Launch the bundle with an explicit log path, for example `FieldWorksBundle.exe /log Output\InstallerEvidence\<RunId>\bundle.log`.
2. Wait for input idle, then request a UI snapshot. If the app-root snapshot has no accessible elements, list desktop windows and use the focused element/keyboard navigation.
3. Safe bundle smoke test: focus Cancel, Tab/Shift+Tab through `license terms`, `I agree to the license terms.`, `Install`, and `Cancel`; press Space on the license checkbox; verify focus can reach `Install`; then Cancel and confirm exit.
4. For install-intent tests, click `Install` only when the machine/VM is prepared for an install run and evidence collection is in place.
5. If the MSI internal UI opens, attach to or inspect the visible `msiexec` window and capture the Destination Folders and feature-selection screens.

Known behavior: the Burn launcher PID may not own the visible WixStdBA window after startup. If `get_snapshot` returns no accessible elements, use desktop window discovery, `get_focused_element`, keyboard commands, or coordinate clicks rather than concluding the UI is untestable.

Elevation boundary: a non-elevated VS Code/WinApp MCP host can usually drive the bundle UI before elevation, but may only observe the elevated MSI internal UI. If the MSI welcome dialog shows focus on `&Next` but UIA click, keyboard, coordinate click, and direct button messages do not advance it, rerun with an elevated automation host or switch to quiet install/uninstall evidence for completion.

## Evidence Checklist

- Bundle command and UI level used.
- WinApp MCP app/window IDs or visible window title/PID used for automation.
- Focus/click path used to reach key controls when UI Automation does not expose a full element tree.
- Screenshot of bundle welcome/license screen.
- Screenshot of MSI destination folders dialog.
- Screenshot of feature selection dialog if touched.
- Bundle log path and `WixBundleLog_AppMsiPackage` MSI log path.
- Confirmation that `/quiet` and `/passive` do not show unexpected MSI UI.
