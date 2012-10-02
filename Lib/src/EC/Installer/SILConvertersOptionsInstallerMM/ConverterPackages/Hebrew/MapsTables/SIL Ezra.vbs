' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This script is to build a compound converter for the SIL Ezra <> UNICODE Encoding Conversion
' from ReverseString + SIL Ezra<>UNICODE
mapNameStep1 = "ReverseString"
fileNameStep1 = "MapsTables\r2l_2004.cct"

mapNameStep2 = "SIL Ezra<>UNICODE (without byte reversing)"
fileNameStep2 = "MapsTables\SILEzratoUni50.tec"
encodingNameStep2Lhs = "SIL-HEBREW_STANDARD-1997"
encodingNameStep2Rhs = "UNICODE"

fontNameSILEzra = "SIL Ezra"
fontNameEzraSIL = "Ezra SIL"

mapName = "SIL Ezra<>UNICODE"

' vbscript doesn't allow import of tlb info, so redefine them here for documentation purposes
NormalizeFlags_None = 0
DontCare = &H0
Legacy_to_from_Unicode = 1
Legacy_to_from_Legacy = 2
UnicodeEncodingConversion = &H1

' get the repository object and use it to add this converter
Dim aECs
Set aECs = CreateObject("SilEncConverters40.EncConverters")

' WScript.Arguments(0) is the TARGETDIR on installation
' add step 1 (ReverseString)
aECs.Add mapNameStep1, WScript.Arguments(0) + fileNameStep1, Legacy_to_from_Legacy, "", "", DontCare

' add step 2 (SIL Ezra<>UNICODE (without byte reversing))
aECs.Add mapNameStep2, WScript.Arguments(0) + fileNameStep2, Legacy_to_from_Unicode, _
	encodingNameStep2Lhs, encodingNameStep2Rhs, UnicodeEncodingConversion

' first remove any traces from the repository (or these'll be steps n and n+1)
aECs.Remove mapName
aECs.AddCompoundConverterStep mapName, mapNameStep1, True, NormalizeFlags_None
aECs.AddCompoundConverterStep mapName, mapNameStep2, True, NormalizeFlags_None

' for the 'SIL Ezra' font, we also want to add an association between that font
' and the encoding name
aECs.AddFont fontNameSILEzra, 42, encodingNameStep2Lhs
aECs.AddFontMapping mapName, fontNameSILEzra, fontNameEzraSIL

Set aECs = Nothing
