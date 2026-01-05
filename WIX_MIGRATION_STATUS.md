# WiX v6 Migration Status

## Overview
The migration of the FieldWorks installer to WiX v6 has been verified and updated.

## Verified Components

### Project Files
- **FieldWorks.Installer.wixproj**: Converted to SDK-style, targets WiX v6.
- **FieldWorks.Bundle.wixproj**: Converted to SDK-style, targets WiX v6.
- **CustomActions.csproj**: Converted to SDK-style, uses `WixToolset.Dtf` packages.

### Source Files
- **Framework.wxs**: Updated to WiX v4 namespace (`http://wixtoolset.org/schemas/v4/wxs`).
- **Bundle.wxs**: Updated to WiX v4 namespace.
- **Dialogs**: All dialog files (`WixUI_DialogFlow.wxs`, `GIInstallDirDlg.wxs`, `GIWelcomeDlg.wxs`, `GIProgressDlg.wxs`, `GICustomizeDlg.wxs`, `GISetupTypeDlg.wxs`) are updated to WiX v4 namespace.
- **Localization**: `WixUI_en-us.wxl` and `BundleTheme.wxl` are updated to WiX v4 localization namespace (`http://wixtoolset.org/schemas/v4/wxl`).
- **Theme**: `BundleTheme.xml` updated to WiX v4 theme namespace (`http://wixtoolset.org/schemas/v4/thmutil`).

### Custom Actions
- **CustomAction.cs**: Updated to use `WixToolset.Dtf.WindowsInstaller` namespace.

## Updates Made
- Updated `FLExInstaller/Shared/Base/BundleTheme.xml` to use the correct WiX v4 theme namespace `http://wixtoolset.org/schemas/v4/thmutil`.

## Next Steps
- Run a full build to verify the installer generation.
- Test the generated MSI and Bundle.
