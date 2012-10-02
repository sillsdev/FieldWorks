' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This script is to build a compound converter for the Devpooja <> UNICODE Encoding Conversion
' from FindPhone>SIL IPA93 + SIL IPA93<>UNICODE
mapNameStep1 = "FindPhone>SIL IPA93"
mapNameStep2 = "SIL IPA93<>UNICODE"
mapName = "FindPhone>UNICODE"

' vbscript doesn't allow import of tlb info, so redefine them here for documentation purposes
NormalizeFlags_None = 0

' get the repository object and use it to add this converter
Dim aECs
Set aECs = CreateObject("SilEncConverters31.EncConverters")

' first remove any traces from the repository (or these'll be steps n and n+1)
aECs.Remove mapName
aECs.AddCompoundConverterStep mapName, mapNameStep1, True, NormalizeFlags_None
aECs.AddCompoundConverterStep mapName, mapNameStep2, True, NormalizeFlags_None

Set aECs = Nothing
