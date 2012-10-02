' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the Annapurna <> UNICODE Encoding Conversion as defined in Annapurna.tec
mapName = "Winscript/iLeap Devanagari<>UNICODE"
fileName = "MapsTables\WinScrDev.tec"
encodingNameLhs = "CDAC-ISFOC_DEVANAGARI"
encodingNameRhs = "Unicode Devanagari"

fontNameDevPanini = "DEV Panini"
fontNameDVTTYogesh = "DV-TTYogesh"

fontNameArialUnicodeMS = "Arial Unicode MS"

' vbscript doesn't allow import of tlb info, so redefine them here for documentation purposes
Legacy_to_from_Unicode = 1
UnicodeEncodingConversion = &H1

' get the repository object and use it to add this converter
Dim aECs
Set aECs = CreateObject("SilEncConverters31.EncConverters")

' WScript.Arguments(0) is the TARGETDIR on installation
aECs.Add mapName, WScript.Arguments(0) + fileName, Legacy_to_from_Unicode, _
	encodingNameLhs, encodingNameRhs, UnicodeEncodingConversion

' for the 'Annapurna' font, we also want to add an association between that font
' and the encoding name
aECs.AddFont fontNameDevPanini, 42, encodingNameLhs
aECs.AddFont fontNameDVTTYogesh, 1252, encodingNameLhs

aECs.AddFontMapping mapName, fontNameDevPanini, fontNameArialUnicodeMS
aECs.AddFontMapping mapName, fontNameDVTTYogesh, fontNameArialUnicodeMS

Set aECs = Nothing
