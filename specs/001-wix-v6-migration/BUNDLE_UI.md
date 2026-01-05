# Bundle UI (WiX v6) — FieldWorks

This document describes the desired UI flow for `FieldWorksBundle.exe` and provides a checklist of changes needed to implement that flow starting from the current bundle UI state in this worktree.

## Scope

- Applies to the **online** bundle authored in [FLExInstaller/Shared/Base/Bundle.wxs](../../FLExInstaller/Shared/Base/Bundle.wxs).
- Uses **WixStdBA (WixStandardBootstrapperApplication)** with a custom theme authored in [FLExInstaller/Shared/Base/BundleTheme.xml](../../FLExInstaller/Shared/Base/BundleTheme.xml) + [FLExInstaller/Shared/Base/BundleTheme.wxl](../../FLExInstaller/Shared/Base/BundleTheme.wxl).
- Goal: **mirror the MSI UI** as closely as practical while still letting Burn handle prerequisites, detection, logging, and restart.

## Reference screenshots (expected UX)

These screenshots reflect the desired end-user experience and should be used as the visual reference when implementing/adjusting the theme and bundle metadata.

### Bundle (first screen) — Welcome

- Window title: “FieldWorks Language Explorer <version>”
- Main text: “To install FieldWorks Language Explorer <version>, agree to the license terms, and then click Install.”
- Includes:
  - **license terms** hyperlink
  - **I agree to the license terms** checkbox (gates Install)
- Buttons:
  - **Install** (shows elevation shield when required)
  - **Close**

### MSI (Destination Folders)

The MSI UI shows two destination folders. The expected defaults are:

- Program folder: `C:\Program Files\SIL\FieldWorks 9\`
- Projects folder: `C:\ProgramData\SIL\FieldWorks\Projects\`

### Bundle (last screen) — Completed

- “Operation Completed”
- **Close** button

## Current state (baseline)

### What the bundle shows today

- The bundle uses WixStdBA `Theme="hyperlinkLicense"` with a custom `ThemeFile` + `LocalizationFile`.
- The theme includes the standard WixStdBA pages:
  - `Loading`, `Install` (with EULA hyperlink + checkbox), `Options` (install folder), `Progress`, `Modify`, plus `Success`/`Failure` pages (later in the file).
- The window/background is built as a template `BundleThemeTemplate.wxi`:
  - `logo.png`.

### What the chain does today

- The chain includes prerequisites and the MSI:
  - `.NET 4.8 web` package group
  - `vcredists` package group
  - `AppMsiPackage` (FieldWorks MSI)
- In [FLExInstaller/Shared/Base/Bundle.wxs](../../FLExInstaller/Shared/Base/Bundle.wxs), the `AppMsiPackage` currently has:
  - `Visible="no"`
  - MSI internal UI is enabled for **full UI** runs (see note below)
  - MSI logging captured via `LogPathVariable="WixBundleLog_AppMsiPackage"`.

- Bundle-level Options UI is suppressed via `WixStdBASuppressOptionsUI=1` so the MSI is the single source of truth for directory/feature selection.

**Note:** Attempting to author `DisplayInternalUI="yes"` on `MsiPackage` fails with WiX v6 in this repo (error `WIX0004`: unexpected attribute). This remains true even after upgrading the installer projects to WiX v6.0.2.

The WiX v6 mechanism for showing MSI internal UI from a bundle is `bal:DisplayInternalUICondition` on `MsiPackage`.

- Current bundle authoring uses: `bal:DisplayInternalUICondition="WixBundleUILevel >= 4"`
  - This turns on MSI internal UI only for **full UI** runs.
  - It avoids MSI dialogs in `/passive` and `/quiet` runs (runtime verification still required).

Reference docs:
- https://docs.firegiant.com/wix/schema/wxs/msipackage/ (see `DisplayInternalUICondition`)
- https://docs.firegiant.com/wix/tools/burn/builtin-variables/ (see `WixBundleUILevel`)

Attempting to force `REBOOT=ReallySuppress` via `MsiProperty` also fails (error `WIX0365`: `REBOOT` is bootstrapper-controlled). Enabling true MSI dialog UI from the bundle therefore requires either:

- upgrading the WiX toolset packages to a version where `DisplayInternalUI` is supported in this project, or
- switching to the “bundle-only UI styled to match MSI” approach (Option B).

### Payload staging constraint (important)

WixStdBA binds theme resources by *filename* at runtime. This worktree stages flat-named copies of the theme and its assets into the culture output folder via [FLExInstaller/FieldWorks.Bundle.wixproj](../../FLExInstaller/FieldWorks.Bundle.wixproj) target `StageBundlePayloads`.

That staging approach is required for stability of any custom theme/asset work.

## Target UI approach

There are two viable approaches. The recommended one is **Option A**.

### Option A (recommended): Burn handles prereqs + MSI shows internal UI

In this approach:

- Burn/WixStdBA shows a **minimal shell UI** (welcome/prep, prereq progress, completion).
- When it is time to install/repair/uninstall FieldWorks, Burn launches the MSI with **internal UI enabled**, so the user sees the real MSI dialogs (directory selection, features, etc.).

This provides the closest parity to the MSI UI with the least custom theming work.

#### Important behavior note

With WixStdBA, MSI internal UI is shown in the **Windows Installer UI window** (msiexec) rather than being embedded in the bootstrapper window. Expect “two windows” during the handoff.

### Option B: Keep bundle-only UI and only style it to look like MSI

This is the current direction of the custom theme: MSI-like background, sizes, and copy. This can be kept even if Option A is implemented, because Option A still needs a small bootstrapper shell.

## Recommended interactive UI flow (Option A)

This section is the concrete “optimal flow” for **Full UI** runs (the normal interactive case).

### 1) Startup / detection

- Bundle starts and detects:
  - whether prerequisites are already present
  - whether FieldWorks is installed (to decide between `Install` vs `Modify` pages)

UI:
- Show `Loading` briefly (or a quiet welcome page if you want to avoid a “blank” feel).

### 2) Install (first-time install)

UI:
- Show the Welcome bundle screen (per the reference screenshot):
  - prerequisites may be installed first
  - then the FieldWorks MSI installer UI will open

License:
- Keep the **license terms** hyperlink and the **I agree to the license terms** checkbox.
- The Install action should be gated until the checkbox is checked.

Buttons:
- `Install`
- `Close`

### 3) Prerequisites phase

UI:
- Show the bundle `Progress` page while Burn installs:
  - .NET 4.8 (if needed)
  - VC++ redists (if needed)

Behavior:
- If prerequisites require a reboot, the bundle may need to resume (depending on package behaviors).

### 4) FieldWorks install phase (MSI internal UI)

At the point Burn reaches `AppMsiPackage`:

- MSI internal UI opens (MSI welcome + directory dialogs + feature selection + progress).
- **Bundle window behavior:** keep the bundle UI open and visible directly behind the MSI UI window while MSI internal UI is running.
  - The bundle should not minimize itself or jump in front of the MSI UI.

MSI destination folders (defaults shown in the MSI UI):

- Program folder: `C:\Program Files\SIL\FieldWorks 9\`
- Projects folder: `C:\ProgramData\SIL\FieldWorks\Projects\`

### 5) Completion

UI:
- On success, show `Success` page.
- On failure, show `Failure` page with a link to the bundle log and/or the MSI log.

Restart messaging:
- Only show restart text/buttons when `WixStdBARestartRequired` is true.
- Do not gate on “RebootPending” alone.

### 6) Maintenance runs (Repair / Uninstall)

If FieldWorks is already installed:

UI:
- Show the bundle `Modify` page with:
  - `Repair` (optional)
  - `Uninstall`

Behavior:
- Clicking `Repair` or `Uninstall` launches MSI internal UI in maintenance mode.

## Non-interactive modes

These must continue to work:

- `/quiet`: no UI
- `/passive`: minimal UI

Key requirement:
- MSI internal UI must not appear in `/quiet` and should not appear in `/passive`.

(Implementation note: the bundle currently gates `bal:DisplayInternalUICondition` on `WixBundleUILevel >= 4` (intended to mean full UI only). This still needs explicit verification.)

## Implementation checklist (from current state)

### Branding and identity requirements

- [ ] **Elevation prompt branding:** when Windows prompts for elevation (UAC), the prompt should display program name **“FieldWorks Installer”** and show the cube logo.
- [ ] **ARP display name:** after install, Add/Remove Programs should list **“FieldWorks Language Explorer <version number>”**.
- [ ] **Window title:** the Welcome bundle window title should display **“FieldWorks Language Explorer <version>”**.

### A) Bundle authoring changes (Bundle.wxs)

- [x] Enable MSI internal UI for the FieldWorks MSI package (WiX v6 approach).
  - Implemented using `bal:DisplayInternalUICondition` on `MsiPackage`.
  - Currently set to `WixBundleUILevel >= 4` (intended to be full UI only).
- [ ] Decide whether to suppress MSI-initiated reboots.
  - `REBOOT` cannot be authored via `MsiProperty` (`WIX0365`).
  - If MSI internal UI becomes possible via toolset upgrade, verify reboot prompting behavior via `/norestart` handling.
- [x] Keep MSI logging.
  - Retain `LogPathVariable="WixBundleLog_AppMsiPackage"`.
  - (Still verify) the failure page hyperlink points at a useful log (bundle log + MSI log, if available).

- [x] Hide bundle-level Options UI to avoid conflicting UX.
  - Set `WixStdBASuppressOptionsUI=1`.

### B) Theme changes (BundleTheme.xml / BundleTheme.wxl)

- [ ] Align the Welcome bundle screen with the reference screenshot:
  - Keep the license hyperlink and acceptance checkbox.
  - Welcome copy should explain prerequisites → then MSI UI.
- [x] Make the Install page button label/copy reflect the handoff.
  - Suggested: keep button text “Install”, but change the page text to mention the MSI installer will open.
- [x] Keep restart UI gated on `WixStdBARestartRequired`.
  - Restart-related controls in `Success`/`Failure` use `VisibleCondition="WixStdBARestartRequired"`.
- [x] Options page decision: hide it.
  - MSI internal UI owns install paths and feature selection; bundle Options is suppressed.

### C) Build/payload staging validation

- [x] Confirm [FLExInstaller/FieldWorks.Bundle.wixproj](../../FLExInstaller/FieldWorks.Bundle.wixproj) `StageBundlePayloads` continues to stage all theme assets needed by the theme:
  - `BundleTheme.xml`, `BundleTheme.wxl`, `logo.png`, `License.htm`, and any bitmap assets referenced by the theme.
- [x] Confirm installer build uses the intended WiX v6.0.2 toolset and extensions.
  - Recent `build.ps1 -BuildInstaller` output shows `wix.exe` and all `-ext` paths coming from `packages\\wixtoolset.*\\6.0.2\\...`.
- [ ] Ensure any newly referenced images in the theme are staged by filename into the culture output directory.

### D) Test checklist

**Important:** per project practice, remove older FieldWorks installs before each test run.

- [ ] Fresh machine test (no FieldWorks installed):
  - Run bundle normally.
  - Verify prereqs install (or are detected).
  - Verify MSI UI opens and completes install.
  - Verify MSI destination defaults:
    - `C:\Program Files\SIL\FieldWorks 9\`
    - `C:\ProgramData\SIL\FieldWorks\Projects\`
  - Verify the bundle window stays open behind the MSI UI.
  - Verify Add/Remove Programs entry is “FieldWorks Language Explorer <version number>”.
  - Verify bundle completion page shows and exit code is success.
- [ ] Upgrade test (older FieldWorks installed):
  - Verify the expected upgrade/uninstall behavior.
  - Ensure UI copy remains accurate (no misleading “clean install” messaging).
- [ ] Repair/uninstall test:
  - Run bundle again and use `Repair` and `Uninstall`.
  - Verify MSI UI opens in maintenance mode.
- [ ] Restart behavior test:
  - Use a scenario that triggers a reboot requirement.
  - Verify restart UI appears only when `WixStdBARestartRequired` is set.
- [ ] Quiet/passive tests:
  - Run `FieldWorksBundle.exe /quiet` and confirm no UI (including no MSI UI) and correct exit codes.
  - Run `FieldWorksBundle.exe /passive` and confirm no MSI internal UI appears.

## Notes / known gaps

- The offline bundle in this repo ([FLExInstaller/Shared/Base/OfflineBundle.wxs](../../FLExInstaller/Shared/Base/OfflineBundle.wxs)) is not currently wired into the WiX v6 build outputs.
- `DisplayInternalUI` (legacy attribute) is not supported by WiX v6 schema here; use `bal:DisplayInternalUICondition` instead.
- Runtime behavior still needs verification for:
  - normal interactive install
  - `/quiet` (no UI)
  - `/passive` (minimal UI, no MSI dialogs)
- This document describes the desired UI flow; it does not decide branding/art layout beyond what is already in the current theme.
