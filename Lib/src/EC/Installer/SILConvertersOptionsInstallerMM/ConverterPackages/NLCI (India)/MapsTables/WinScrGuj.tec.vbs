' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SAG IPA Super <> UNICODE Encoding Conversion as defined in SAGIPS2Uni.tec
mapName = "Winscript/iLeap Gujarati<>UNICODE"
fileName = "MapsTables\WinScrGuj.tec"
encodingNameLhs = "CDAC-ISFOC_GUJARATI"
encodingNameRhs = "UNICODE"

fontNameGUJGir = "GUJ Gir"

' vbscript doesn't allow import of tlb info, so redefine them here for documentation purposes
Legacy_to_from_Unicode = 1
UnicodeEncodingConversion = &H1

' get the repository object and use it to add this converter
Dim aECs
Set aECs = CreateObject("SilEncConverters40.EncConverters")

' WScript.Arguments(0) is the TARGETDIR on installation
aECs.Add mapName, WScript.Arguments(0) + fileName, Legacy_to_from_Unicode, _
	encodingNameLhs, encodingNameRhs, UnicodeEncodingConversion

' for the 'Annapurna' font, we also want to add an association between that font
' and the encoding name
aECs.AddFont fontNameGUJGir, 42, encodingNameLhs

Set aECs = Nothing
