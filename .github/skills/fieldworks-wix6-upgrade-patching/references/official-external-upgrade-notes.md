# Official And External Upgrade/Patch Notes

Use these for WiX Toolset and Windows Installer only.

## Official Sources

- Patches: https://docs.firegiant.com/wix/tools/patches/
- Patch schema: https://docs.firegiant.com/wix/schema/wxs/patch/
- PatchBaseline schema: https://docs.firegiant.com/wix/schema/wxs/patchbaseline/
- MajorUpgrade schema: https://docs.firegiant.com/wix/schema/wxs/majorupgrade/
- MsiPackage schema: https://docs.firegiant.com/wix/schema/wxs/msipackage/
- Bundle schema: https://docs.firegiant.com/wix/schema/wxs/bundle/
- Windows Installer patching: https://learn.microsoft.com/windows/win32/msi/patching

## Key Facts

- Major upgrades are simpler and safer than MSPs for broad product changes.
- MSI version comparison ignores the fourth field. Do not depend on fourth-field changes for upgrade detection.
- `AllowSameVersionUpgrades` can be useful for dev builds but can also mask versioning mistakes.
- WiX 4+ patch authoring can use `.wixpdb` or `.msi` baselines with `PatchBaseline`.
- `.wixpdb` baselines need correct bind paths to target/update payload files.
- `.msi` baselines can be easier to use but require extraction and can be slower.

## External Lessons Worth Testing

- WiX issue #7778: v3 and v4+ Burn package dependency keys can be incompatible. A package-level `<Provides Key="PRODUCTCODE_FOR_YOUR_MSI" />` can preserve compatibility in some scenarios, but repair/uninstall must be tested afterward.
- WiX 4+ bundles respect `-arch`; old WiX 3 x86-Burn assumptions can break x64 registry searches, BA payload loading, and dependency detection.
- Stable component GUIDs, stable file IDs, and stable generated fragment identity matter for MSPs.
- Custom action failures should be diagnosed from MSI logs, not from UI symptoms.

## Treat With Caution

- v4/v5 guidance usually applies conceptually to v6, but verify exact package names and schema support against the WiX version pinned in the repo.
- Stack Overflow/blog advice is useful for symptoms, not authoritative design. Convert it into a FieldWorks repro and validation checklist.
