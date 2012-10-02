del WixLink.log
del WixLinkICU040.log
del WixLinkICUECHelp.log

light -nologo "MS KB908002 Fix.mmp.wixobj" -out "MS KB908002 Fix.msm" >"WixLinkMS KB908002 Fix.log"
light -nologo wixca.wixlib ICU040.mmp.wixobj -out ICU040.msm >WixLinkICU040.log
light -nologo wixca.wixlib ICUECHelp.mmp.wixobj -out ICUECHelp.msm >WixLinkICUECHelp.log
light -nologo SetPath.mmp.wixobj -out SetPath.msm >WixLinkSetPath.log
light -nologo wixca.wixlib EC_GAC_30.mmp.wixobj -out EC_GAC_30.msm >WixLinkEC_GAC_30.log
light -nologo PerlEC.mmp.wixobj -out PerlEC.msm >WixLinkPerlEC.log
light -nologo PythonEC.mmp.wixobj -out PythonEC.msm >WixLinkPythonEC.log
light -nologo "Old C++ and MFC DLLs.mmp.wixobj" -out "Old C++ and MFC DLLs.msm" >"WixLinkOld C++ and MFC DLLs.log"
light -nologo "Managed Install Fix.mmp.wixobj" -out "Managed Install Fix.msm" >"WixLinkManaged Install Fix.log"
light -nologo wixca.wixlib EcFolderACLs.mm.wixobj -out EcFolderACLs.msm >WixLinkEcFolderACLs.log

light -nologo wixca.wixlib DebugExtras.wixobj Actions.wixobj CopyFiles.wixobj Environment.wixobj Features.wixobj FwDebug.wixobj FwUI.wixobj ProcessedMergeModules.wixobj ProcessedFiles.wixobj ProcessedProperties.wixobj Registry.wixobj Shortcuts.wixobj -out SetupFwDebug.msi >WixLink.log