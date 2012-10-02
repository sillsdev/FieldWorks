del *.wxs.log
del *.wixobj

candle -nologo DebugExtras.wxs -sw1044 >DebugExtras.wxs.log
candle -nologo Actions.wxs >Actions.wxs.log
candle -nologo CopyFiles.wxs -sw1044 >CopyFiles.wxs.log
candle -nologo Environment.wxs >Environment.wxs.log
candle -nologo Features.wxs >Features.wxs.log
candle -nologo FwDebug.wxs >FwDebug.wxs.log
candle -nologo FwUI.wxs >FwUI.wxs.log
candle -nologo ProcessedFiles.wxs -sw1044 >ProcessedFiles.wxs.log
candle -nologo ProcessedMergeModules.wxs >ProcessedMergeModules.wxs.log
candle -nologo ProcessedProperties.wxs -sw1044 >ProcessedProperties.wxs.log
candle -nologo Registry.wxs >Registry.wxs.log
candle -nologo Shortcuts.wxs >Shortcuts.wxs.log

candle -nologo ICU040.mmp.wxs -sw1044 >ICU040.mmp.wxs.log
candle -nologo ICUECHelp.mmp.wxs -sw1044 >ICUECHelp.mmp.wxs.log
candle -nologo SetPath.mmp.wxs >SetPath.mmp.wxs.log
candle -nologo PerlEC.mmp.wxs >PerlEC.mmp.wxs.log
candle -nologo EC_GAC_30.mmp.wxs >EC_GAC_30.mmp.wxs.log
candle -nologo PythonEC.mmp.wxs >PythonEC.mmp.wxs.log
candle -nologo "Old C++ and MFC DLLs.mmp.wxs" >"Old C++ and MFC DLLs.mmp.wxs.log"
candle -nologo "Managed Install Fix.mmp.wxs" -sw1044 >"Managed Install Fix.mmp.wxs.log"
candle -nologo "MS KB908002 Fix.mmp.wxs" -sw1044 >"MS KB908002 Fix.mmp.wxs.log"
candle -nologo EcFolderACLs.mm.wxs -sw1044 >EcFolderACLs.mm.wxs.log

"WIX Debug Installer Link.bat"