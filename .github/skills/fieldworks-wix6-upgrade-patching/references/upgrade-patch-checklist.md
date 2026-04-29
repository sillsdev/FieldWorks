# FieldWorks Upgrade And Patch Checklist

## FieldWorks Files To Inspect

- `specs/001-wix-v6-migration/REMAINING_WIX6_ISSUES.md`: active upgrade, ARP, and uninstall risks.
- `specs/001-wix-v6-migration/verification-matrix.md`: rows for upgrade, registry, uninstall, offline, custom actions, signing.
- `specs/001-wix-v6-migration/golden-install-checklist.md`: VM scenarios A-F.
- `specs/001-wix-v6-migration/parity-check.md`: existing WiX 3 vs WiX 6 evidence.
- `FLExInstaller/wix6/Shared/Base/Framework.wxs`: MSI major upgrade, properties, registry searches, custom action sequencing.
- `FLExInstaller/wix6/Shared/Base/Bundle.wxs`: bundle identity, related bundle upgrade, app MSI package, ARP visibility.
- `Build/Installer.targets`: WiX 6 build targets. Verify patch target presence before using `-BuildPatch -InstallerToolset Wix6`.
- `Build/Installer.legacy.targets`: old patch process and warnings about patch fragility.

## Evidence To Capture

- Bundle log for install/upgrade/uninstall.
- Package MSI log via `WixBundleLog_AppMsiPackage` or explicit `msiexec /l*v`.
- ARP screenshots before and after.
- Registry exports for FieldWorks keys and Windows uninstall keys.
- File listings for app folder and data folder.
- Product inventory showing ProductCode, UpgradeCode, version, and package source/cache.
- `.msi`, `.wixpdb`, and SHA256 hashes for baseline and update artifacts.

## Upgrade Pass Criteria

- Existing WiX 3 install is detected.
- WiX 6 install does not create side-by-side FieldWorks instances.
- Old install is removed/replaced.
- App folder defaults or preserved paths match expected behavior.
- Data folder remains in place and is not duplicated.
- User settings and projects survive.
- ARP shows one sensible FieldWorks entry.
- Repair and uninstall work after upgrade.

## Patch Pass Criteria

- Target and update baselines are archived with `.wixpdb` and payloads.
- Component GUIDs and file IDs are stable across target/update where required.
- Patch authoring uses `Patch`/`PatchBaseline` and produces a valid `.msp`.
- Patch install, repair, uninstall, rollback, and supersedence behavior are tested.
- Build logs prove which target/update artifacts were used.
