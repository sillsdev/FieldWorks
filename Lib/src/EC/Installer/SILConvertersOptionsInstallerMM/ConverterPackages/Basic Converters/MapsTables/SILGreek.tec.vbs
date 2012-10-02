' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SIL Galatia <> Unicode Encoding Conversion as defined in SILGreek2004-04-27.tec
mapName = "SIL Galatia<>UNICODE"
fileName = "MapsTables\SILGreek2004-04-27.tec"
encodingNameLhs = "SIL-GREEK_GALATIA-2001"
encodingNameRhs = "Unicode Greek"
fontNameSILGalatia = "SIL Galatia"
fontNameGalatiaSIL = "Galatia SIL"

' vbscript doesn't allow import of tlb info, so redefine them here for documentation purposes
Legacy_to_from_Unicode = 1
UnicodeEncodingConversion = &H1

' get the repository object and use it to add this converter
Dim aECs
Set aECs = CreateObject("SilEncConverters40.EncConverters")

' WScript.Arguments(0) is the TARGETDIR on installation
aECs.Add mapName, WScript.Arguments(0) + fileName, Legacy_to_from_Unicode, _
	encodingNameLhs, encodingNameRhs, UnicodeEncodingConversion

' for the corresponding fonts, we also want to add an association between that font
' and the encoding name
aECs.AddFont fontNameSILGalatia, 42, encodingNameLhs
aECs.AddUnicodeFontEncoding fontNameGalatiaSIL, encodingNameRhs
aECs.AddFontMapping mapName, fontNameSILGalatia, fontNameGalatiaSIL

Set aECs = Nothing
