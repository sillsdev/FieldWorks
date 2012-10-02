wscript StripOutTE.js

candle -nologo CopyFiles_No_TE.wxs -sw1044 >CopyFiles_No_TE.wxs.log
candle -nologo Features_No_TE.wxs >Features_No_TE.wxs.log
candle -nologo FwUI_No_TE.wxs >FwUI_No_TE.wxs.log
candle -nologo ProcessedFiles_No_TE.wxs -sw1044 >ProcessedFiles_No_TE.wxs.log
candle -nologo Registry_No_TE.wxs >Registry_No_TE.wxs.log
candle -nologo Shortcuts_No_TE.wxs >Shortcuts_No_TE.wxs.log

del WixLink_No_TE.log
light -nologo wixca.wixlib Actions.wixobj CopyFiles_No_TE.wixobj Environment.wixobj Features_No_TE.wixobj FW.wixobj FwUI_No_TE.wixobj ProcessedMergeModules.wixobj ProcessedFiles_No_TE.wixobj ProcessedProperties.wixobj Registry_No_TE.wixobj Shortcuts_No_TE.wixobj -out SetupFW_No_TE.msi >WixLink_No_TE.log