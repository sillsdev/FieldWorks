' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SIL PNG <> Unicode Encoding Conversion as defined in SIL-PNG.tec
mapName = "ECG<>UNICODE"
fileName = "MapsTables\ECG-Unicode.tec"
encodingNameLhs = "ECG"
encodingNameRhs = "Unicode IPA"

fontNameECGC = "ECG-C"
fontNameECGD = "ECG-D"
fontNameECGM = "ECG-M"
fontNameECGS = "ECG-S"

fontNameDoulosSIL = "Doulos SIL"

' vbscript doesn't allow import of tlb info, so redefine them here for documentation purposes
Legacy_to_from_Unicode = 1
UnicodeEncodingConversion = &H1

' get the repository object and use it to add this converter
Dim aECs
Set aECs = CreateObject("SilEncConverters31.EncConverters")

' WScript.Arguments(0) is the TARGETDIR on installation
aECs.Add mapName, WScript.Arguments(0) + fileName, Legacy_to_from_Unicode, _
	encodingNameLhs, encodingNameRhs, UnicodeEncodingConversion

' for the corresponding fonts, we also want to add an association between that font
' and the encoding name
aECs.AddFont fontNameECGC, 1252, encodingNameLhs
aECs.AddFont fontNameECGD, 1252, encodingNameLhs
aECs.AddFont fontNameECGM, 1252, encodingNameLhs
aECs.AddFont fontNameECGS, 1252, encodingNameLhs

' default conversions of SILDoulos to Doulos SIL for this mapping
aECs.AddFontMapping mapName, fontNameECGC, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameECGD, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameECGM, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameECGS, fontNameDoulosSIL

Set aECs = Nothing
