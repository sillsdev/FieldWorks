# FieldWorks UI Repo Map

## Bundle UI

- `FLExInstaller/wix6/Shared/Base/Bundle.wxs`: online bundle. Uses WixStdBA, custom theme payloads, `SuppressOptionsUI="yes"`, and `bal:DisplayInternalUICondition="WixBundleUILevel >= 4"` on `AppMsiPackage`.
- `FLExInstaller/wix6/Shared/Base/OfflineBundle.wxs`: offline bundle equivalent. Keep UI behavior in sync unless intentionally different.
- `FLExInstaller/wix6/Shared/Base/BundleTheme.xml`: WixStdBA theme layout.
- `FLExInstaller/wix6/Shared/Base/BundleTheme.wxl`: WixStdBA theme strings.
- `FLExInstaller/wix6/FieldWorks.Bundle.wixproj`: stages `BundleTheme.xml`, `.wxl`, `fw-logo.png`, background assets, and `License.htm` into the culture output folder by flat filename.

## MSI UI

- `FLExInstaller/wix6/Shared/Base/Framework.wxs`: package root, properties, custom actions, `WIXUI_INSTALLDIR`, `WIXUI_PROJECTSDIR`, UI refs, app/data folder registry searches.
- `FLExInstaller/wix6/Shared/Base/WixUI_DialogFlow.wxs`: custom dialog navigation.
- `FLExInstaller/wix6/Shared/Base/GIWelcomeDlg.wxs`: welcome/update entry.
- `FLExInstaller/wix6/Shared/Base/GISetupTypeDlg.wxs`: typical/custom/complete route.
- `FLExInstaller/wix6/Shared/Base/GIInstallDirDlg.wxs`: app folder and project data folder controls.
- `FLExInstaller/wix6/Shared/Base/GICustomizeDlg.wxs`: feature selection tree.
- `FLExInstaller/wix6/Shared/Base/GIProgressDlg.wxs`: progress and patch/update text.
- `FLExInstaller/wix6/Shared/Base/WixUI_en-us.wxl`: MSI UI strings.

## Feature Tree Inputs

- `FLExInstaller/wix6/Shared/Common/CustomFeatures.wxi`: features and feature levels.
- `FLExInstaller/wix6/Shared/Common/CustomComponents.wxi`: component groups, shortcuts, environment variables, URL protocol registration.
- Harvested component groups from the WiX 6 build must match feature refs.

## High-Value Checks

- `Bundle.wxs` should hide the MSI in ARP with `Visible="no"` unless intentionally changing ARP behavior.
- `LogPathVariable="WixBundleLog_AppMsiPackage"` should stay available for MSI log discovery.
- `WixStdBASuppressOptionsUI`/`SuppressOptionsUI` keeps bundle Options UI from competing with the MSI directory UI.
- `GIInstallDirDlg` uses indirect PathEdit controls. Make sure the properties point to directory IDs and are initialized before the dialog opens.
- The data folder should lock on upgrade only when the registry/search state proves an existing data folder.
