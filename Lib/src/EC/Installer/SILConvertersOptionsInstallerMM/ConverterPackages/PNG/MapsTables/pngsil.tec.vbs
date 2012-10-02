' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SIL PNG <> Unicode Encoding Conversion as defined in SIL-PNG.tec
mapName = "SIL PNG<>UNICODE"
fileName = "MapsTables\SIL-PNG.tec"
encodingNameLhs = "SIL-PNG_Fonts-1998"
encodingNameRhs = "Unicode IPA"

fontNamePNGSILCharis = "PNG SILCharis"
fontNamePNGSILDoulos = "PNG SILDoulos"
fontNamePNGSILManuscript = "PNG SILManuscript"
fontNamePNGSILSophiaLit = "PNG SILSophia Lit"
fontNamePNGSILCharisLit = "PNG SILCharis Lit"
fontNamePNGSILSophiaCQLit = "PNG SILSophia CQLit"

fontNameCharisSIL = "Charis SIL"
fontNameDoulosSIL = "Doulos SIL"
fontNameCharisSILLit = "Charis SIL Lit"
fontNameAndika = "Andika"

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
aECs.AddFont fontNamePNGSILCharis, 1252, encodingNameLhs
aECs.AddFont fontNamePNGSILDoulos, 1252, encodingNameLhs
aECs.AddFont fontNamePNGSILManuscript, 1252, encodingNameLhs
aECs.AddFont fontNamePNGSILSophiaLit, 1252, encodingNameLhs
aECs.AddFont fontNamePNGSILCharisLit, 1252, encodingNameLhs
aECs.AddFont fontNamePNGSILSophiaCQLit, 1252, encodingNameLhs

' default conversions of SILDoulos to Doulos SIL for this mapping
aECs.AddFontMapping mapName, fontNamePNGSILCharis, fontNameCharisSIL
aECs.AddFontMapping mapName, fontNamePNGSILDoulos, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNamePNGSILCharisLit, fontNameCharisSILLit
aECs.AddFontMapping mapName, fontNamePNGSILSophiaCQLit, fontNameAndika

Set aECs = Nothing
