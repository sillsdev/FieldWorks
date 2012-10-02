' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the IPA90<>Unicode Encoding Conversion as defined in SIL-IPA-90.map
mapName = "SIL-IPA-1990<>UNICODE"
fileName = "MapsTables\SIL-IPA-1990.tec"
encodingNameLhs = "SIL-IPA-1990"
encodingNameRhs = "Unicode IPA"
fontNameSILDoulos = "SILDoulosIPA"
fontNameSILManuscript = "SILManuscriptIPA"
fontNameSILSophia = "SILSophiaIPA"
fontNameDoulosSIL = "Doulos SIL"
fontNameCourierNew = "Courier New"

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
aECs.AddFont fontNameSILDoulos, 42, encodingNameLhs
aECs.AddFont fontNameSILManuscript, 42, encodingNameLhs
aECs.AddFont fontNameSILSophia, 42, encodingNameLhs
aECs.AddUnicodeFontEncoding fontNameDoulosSIL, encodingNameRhs

' default conversions of SILDoulos to Doulos SIL for this mapping
aECs.AddFontMapping mapName, fontNameSILDoulos, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSILManuscript, fontNameCourierNew

Set aECs = Nothing
