' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the AkrutiOriSarala99 <> UNICODE Encoding Conversion as defined in Ori2Uni_AkrutiOriSarala99.tec
mapName = "AkrutiOriSarala99<>UNICODE"
fileName = "MapsTables\Ori2Uni_AkrutiOriSarala99.tec"
encodingNameLhs = "Oriya-AkrutiOriSarala-99"
encodingNameRhs = "Unicode Oriya"

fontNameAkrutiOriSarala99 = "AkrutiOriSarala-99"
fontNameKalinga = "Kalinga"

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
aECs.AddFont fontNameAkrutiOriSarala99, 1252, encodingNameLhs

aECs.AddFontMapping mapName, fontNameAkrutiOriSarala99, fontNameKalinga

Set aECs = Nothing
