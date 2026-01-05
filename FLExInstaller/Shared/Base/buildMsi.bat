@echo off
call setVars.bat %*

(
	@echo on
	@REM CustomActions are built by WiX magic; ProcRunner must be built normally.
	@REM Although there are no NuGet packages to restore, the Restore target is required to generate project.assets.json
	%msbuild% ../ProcRunner/ProcRunner.sln /p:Configuration=Release /t:Restore;Rebuild

) && (
	@REM Harvest (heat) the application and data files.
	heat.exe dir %MASTERBUILDDIR% -cg HarvestedAppFiles -gg -scom -sreg -sfrag -srd -sw5150 -sw5151 -dr APPFOLDER -var var.MASTERBUILDDIR -t KeyPathFix.xsl -out AppHarvest.wxs
	heat.exe dir %MASTERDATADIR% -cg HarvestedDataFiles -gg -scom -sreg -sfrag -srd -sw5150 -sw5151 -dr HARVESTDATAFOLDER -var var.MASTERDATADIR -t KeyPathFix.xsl -out DataHarvest.wxs
) && (
	@REM Compile (candle) and Link (light) the MSI file.
	candle.exe -arch %Arch% -dApplicationName=%AppName% -dSafeApplicationName=%SafeAppName% -dManufacturer=%Manufacturer% -dSafeManufacturer=%SafeManufacturer% -dVersionNumber=%Version% -dMajorVersion=%Major% -dMinorVersion=%Minor% -dMASTERBUILDDIR=%MASTERBUILDDIR% -dMASTERDATADIR=%MASTERDATADIR% -dUpgradeCode=%UPGRADECODEGUID% -dProductCode=%PRODUCTIDGUID% Framework.wxs AppHarvest.wxs DataHarvest.wxs WixUI_DialogFlow.wxs GIInstallDirDlg.wxs GIProgressDlg.wxs GIWelcomeDlg.wxs GICustomizeDlg.wxs GISetupTypeDlg.wxs -ext WixFirewallExtension -ext WixUtilExtension
) && (
	light.exe Framework.wixobj AppHarvest.wixobj DataHarvest.wixobj WixUI_DialogFlow.wixobj GIInstallDirDlg.wixobj GIProgressDlg.wixobj GIWelcomeDlg.wixobj GICustomizeDlg.wixobj GISetupTypeDlg.wixobj -ext WixFirewallExtension -ext WixUIExtension -ext WixUtilExtension -cultures:en-us -loc WixUI_en-us.wxl %SuppressICE% -sw1076 -out %SafeAppName%_%Version%.msi
) && (
	call signingProxy %CD%\%SafeAppName%_%Version%.msi
)