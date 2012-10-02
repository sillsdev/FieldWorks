del EC*.wxs.log
del EC*.wixobj

candle -nologo EC.wxs >EC.wxs.log
candle -nologo EcActions.wxs >EcActions.wxs.log
candle -nologo EcFeatures.wxs >EcFeatures.wxs.log
candle -nologo EcUI.wxs >EcUI.wxs.log
candle -nologo EcFiles.wxs -sw1044 >EcFiles.wxs.log
candle -nologo EcProcessedMergeModules.wxs >EcProcessedMergeModules.wxs.log

candle -nologo EC_GAC_30.mmp.wxs -sw1044 >EC_GAC_30.mmp.wxs.log
candle -nologo PythonEC.mmp.wxs -sw1044 >PythonEC.mmp.wxs.log
candle -nologo PerlEC.mmp.wxs -sw1044 >PerlEC.mmp.wxs.log
candle -nologo ICU040.mmp.wxs -sw1044 >ICU040.mmp.wxs.log
candle -nologo ICUECHelp.mmp.wxs -sw1044 >ICUECHelp.mmp.wxs.log
candle -nologo SetPath.mmp.wxs >SetPath.mmp.wxs.log
candle -nologo "Managed Install Fix.mmp.wxs" -sw1044 >"Managed Install Fix.mmp.wxs.log"
candle -nologo "MS KB908002 Fix.mmp.wxs" -sw1044 >"MS KB908002 Fix.mmp.wxs.log"
candle -nologo EcFolderACLs.mmp.wxs -sw1044 >EcFolderACLs.mmp.wxs.log

"EC WIX Installer Link.bat"