---
name: fieldworks-wix6-upgrade-patching
description: Use this skill for FieldWorks installer upgrade and patch work: installing WiX 6 over prior WiX 3 builds, single-instance guarantees, MajorUpgrade behavior, ARP duplicate entries, uninstall/repair after upgrade, BuildPatch, base build artifacts, MSP creation, PatchBaseline, .wixpdb baselines, component GUID/file ID stability, and replacing PatchableInstaller patch infrastructure. Trigger whenever the user mentions upgrade, patch, MSP, base build, ARP, repair, uninstall compatibility, ProductCode, UpgradeCode, or Burn provider keys.
---

# FieldWorks WiX 6 Upgrade And Patching

This skill handles the part of the migration most likely to hurt later: compatibility with existing installs and the discipline needed for future patches.

## Load References When Needed

- Read `references/upgrade-patch-checklist.md` for FieldWorks-specific gates.
- Read `references/official-external-upgrade-notes.md` for WiX/Windows Installer guidance and external lessons.

## First Moves

1. Read `specs/001-wix-v6-migration/REMAINING_WIX6_ISSUES.md`, `verification-matrix.md`, `golden-install-checklist.md`, and `parity-check.md`.
2. Inspect `Framework.wxs` for `MajorUpgrade`, `UpgradeCode`, path registry searches, `WIX_UPGRADE_DETECTED`, and `AllowSameVersionUpgrades`.
3. Inspect `Bundle.wxs`/`OfflineBundle.wxs` for bundle identity, `RelatedBundle`, `MsiPackage`, `Visible`, `ProviderKey`, and any `Provides` children.
4. Inspect `Build/InstallerBuild.proj`, `Build/Installer.targets`, and `Build/Installer.Wix3.targets` before assuming patch targets exist for WiX 6.
5. For actual failures, use the diagnostics skill to collect bundle/MSI logs and ARP/registry snapshots first.

## Hard Requirements

- FieldWorks must be single-instance. WiX 6 must not allow side-by-side FieldWorks installs from any previous WiX 3 or WiX 6 generation.
- Upgrade must preserve user data and settings while replacing the old install.
- WiX 3 remains the current default build during transition, but the migration target is WiX 6-first. Do not break the fallback while building WiX 6 upgrade behavior, and do not treat the current default as the desired final state.
- Major upgrades are the safer default for broad installer changes. MSP patches require stricter component-rule discipline.

## WiX 3 To WiX 6 Upgrade Matrix

Validate these scenarios before declaring upgrade support complete:

- Clean WiX 6 bundle install.
- Prior WiX 3 MSI -> WiX 6 MSI.
- Prior WiX 3 bundle -> WiX 6 bundle.
- WiX 6 repair after WiX 3-to-WiX 6 upgrade.
- WiX 6 uninstall after WiX 3-to-WiX 6 upgrade.
- Downgrade attempt from newer install.
- Interrupted upgrade rollback.
- Same-version dev build replacement if `AllowSameVersionUpgrades` is in play.

Capture bundle logs, MSI logs, ARP snapshots, path registry exports, and before/after install/data folder listings.

## Burn Provider Compatibility

Public WiX issue history shows v3 and v4+ bundles can disagree about package dependency/provider keys. For upgrades from a WiX 3 bundle to WiX 6:

- Review whether FieldWorks needs explicit provider compatibility.
- Consider whether a package-level `<Provides Key="..." />` matching the old MSI provider/product identity is needed.
- Test repair/uninstall after adding any provider workaround; it can fix upgrade but expose repair behavior.
- Do not guess ProductCode/ProviderKey values. Extract them from the baseline artifact or registry evidence.

## ARP Duplication Workflow

When Programs and Features shows duplicate or strange FieldWorks entries:

1. Inventory bundle entries and MSI entries separately.
2. Check `Bundle` identity/name/version/upgrade provider behavior.
3. Check `MsiPackage Visible`, MSI `ARPSYSTEMCOMPONENT`, `ARPNOMODIFY`, `ARPNOREMOVE`, and bundle `DisableModify`/`DisableRemove`.
4. Use Windows Installer product inventory and registry snapshots to identify which product/package registered each ARP row.
5. Verify uninstall from ARP does not hang and removes the expected entry only.

## WinApp MCP For Upgrade/Uninstall Runs

Use WinApp MCP to observe and drive UI during upgrade, repair, and uninstall evidence runs:

- Attach to the visible bundle/MSI/ARP window and capture the title, PID, focused element, and any prompt text before clicking.
- For ARP uninstall hangs, use WinApp to identify whether focus is on a hidden prompt, MSI dialog, bundle confirmation, or UAC-adjacent window before changing authoring.
- During WiX 3 to WiX 6 upgrade tests, use WinApp screenshots/focus traces for the bundle welcome/license page, MSI destination folders page, feature tree, progress, completion, and any maintenance prompt.
- Do not use WinApp to bypass prompts silently. The point is to document and reproduce the user-visible path while bundle/MSI logs and snapshots capture the underlying state.
- Run the WinApp automation host elevated for full manual upgrade or uninstall UI once Burn hands off to MSI internal UI. A non-elevated host can observe the elevated MSI dialog but may not be able to click `Next`, `Repair`, `Uninstall`, or ARP prompts.

## MSP Patch Gate

Before creating a WiX 6 MSP path, classify the change:

- Safe-ish for patch: changed file contents with stable component GUIDs/file IDs and unchanged component ownership.
- Risky for patch: removed files, renamed files, moved directories, changed component GUIDs, changed key paths, feature tree reshaping, or harvested fragment identity churn.
- Prefer a major upgrade when component rules are uncertain.

Patch infrastructure must preserve:

- Target and update `.msi` files.
- Target and update `.wixpdb` files.
- Source payload directories or enough `.msi` information for extraction.
- Bind paths for target/update payloads.
- Generated fragments and ID maps.
- Build logs and installer version metadata.

## Output Expectations

For upgrade/patch work, report:

- Baseline artifact and update artifact identities.
- UpgradeCode, ProductCode, package identity, and first-three-field version relationship.
- Whether the result is a major upgrade, repair, uninstall, or MSP patch path.
- Evidence captured and any matrix rows proven.
- Residual risk around Burn provider compatibility or component-rule stability.
