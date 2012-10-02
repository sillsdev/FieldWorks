rem Run this batch file to build WP only installer after normal FieldWorks installers have been built.

wscript StripOutAllButWP.js

candle -nologo Actions_WP.wxs >Actions_WP.wxs.log
candle -nologo CopyFiles_WP.wxs -sw1044 >CopyFiles_WP.wxs.log
candle -nologo Features_WP.wxs >Features_WP.wxs.log
candle -nologo FW_WP.wxs >FW_WP.wxs.log
candle -nologo FwUI_WP.wxs >FwUI_WP.wxs.log
candle -nologo ProcessedFiles_WP.wxs -sw1044 >ProcessedFiles_WP.wxs.log
candle -nologo ProcessedMergeModules_WP.wxs -sw1044 >ProcessedMergeModules_WP.wxs.log
candle -nologo Registry_WP.wxs >Registry_WP.wxs.log
candle -nologo Shortcuts_WP.wxs >Shortcuts_WP.wxs.log

del WixLink_WP.log
light -nologo wixca.wixlib Actions_WP.wixobj CopyFiles_WP.wixobj Environment.wixobj Features_WP.wixobj FW_WP.wixobj FwUI_WP.wixobj ProcessedMergeModules_WP.wixobj ProcessedFiles_WP.wixobj ProcessedProperties.wixobj Registry_WP.wixobj Shortcuts_WP.wixobj -out SetupWP.msi >WixLink_WP.log