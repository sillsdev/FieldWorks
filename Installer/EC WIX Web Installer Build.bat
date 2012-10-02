WScript.exe EcProcessMergeModules.js web
candle -nologo EcWebProcessedMergeModules.wxs >EcWebProcessedMergeModules.wxs.log
light -nologo Ec.wixobj EcActions.wixobj EcFeatures.wixobj EcUI.wixobj EcWebProcessedMergeModules.wixobj EcFiles.wixobj -out WebSetupEC.msi >EcWixWebLink.log