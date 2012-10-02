del EcWixLink.log
del "WixLinkMS KB908002 Fix.log"
del WixLinkPythonEC.log
del WixLinkPerlEC.log
del WixLinkEC_GAC_30.log
del WixLinkICU040.log
del WixLinkICUECHelp.log
del WixLinkSetPath.log
del "WixLinkManaged Install Fix.log"
del WixLinkEcFolderACLs.log

light -nologo "MS KB908002 Fix.mmp.wixobj" -out "MS KB908002 Fix.msm" >"WixLinkMS KB908002 Fix.log"
light -nologo wixca.wixlib EC_GAC_30.mmp.wixobj -out EC_GAC_30.msm >WixLinkEC_GAC_30.log
light -nologo wixca.wixlib PerlEC.mmp.wixobj -out PerlEC.msm >WixLinkPerlEC.log
light -nologo wixca.wixlib PythonEC.mmp.wixobj -out PythonEC.msm >WixLinkPythonEC.log
light -nologo wixca.wixlib ICU040.mmp.wixobj -out ICU040.msm >WixLinkICU040.log
light -nologo wixca.wixlib ICUECHelp.mmp.wixobj -out ICUECHelp.msm >WixLinkICUECHelp.log
light -nologo SetPath.mmp.wixobj -out SetPath.msm >WixLinkSetPath.log
light -nologo "Managed Install Fix.mmp.wixobj" -out "Managed Install Fix.msm" >"WixLinkManaged Install Fix.log"
light -nologo wixca.wixlib EcFolderACLs.mmp.wixobj -out EcFolderACLs.msm >WixLinkEcFolderACLs.log

light -nologo Ec.wixobj EcActions.wixobj EcFeatures.wixobj EcUI.wixobj EcProcessedMergeModules.wixobj EcFiles.wixobj -out SetupEC.msi >EcWixLink.log