' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SAG IPA Super <> UNICODE Encoding Conversion as defined in SAGIPS2Uni.tec
mapName = "SAG IPA Super<>UNICODE"
fileName = "MapsTables\SAGIPS2Uni.tec"
encodingNameLhs = "SIL-SAG-IPA_Super"
encodingNameRhs = "UNICODE"

fontNameSAGIPSSILCharis = "SAG-IPA Super SILCharis"
fontNameSAGIPSSILCharisBold = "SAG-IPA Super SILCharis Bold"
fontNameSAGIPSSILCharisBoldItalic = "SAG-IPA Super SILCharis Bold Italic"
fontNameSAGIPSSILCharisItalic = "SAG-IPA Super SILCharis Italic"

fontNameSAGIPSSILDoulos = "SAG-IPA Super SILDoulos"
fontNameSAGIPSSILDoulosItalic = "SAG-IPA Super SILDoulos Italic"
fontNameSAGIPSSILDoulosBold = "SAG-IPA Super SILDoulos Bold"
fontNameSAGIPSSILDoulosBoldItalic = "SAG-IPA Super SILDoulos Bold Italic "

fontNameSAGIPSSILManuscript = "SAG-IPA Super SILManuscript"
fontNameSAGIPSSILManuscriptItalic = "SAG-IPA Super SILManuscript Italic"
fontNameSAGIPSSILManuscriptBold = "SAG-IPA Super SILManuscript Bold"
fontNameSAGIPSSILManuscriptBoldItalic = "SAG-IPA Super SILManuscript Bold Italic "

fontNameSAGIPSSILSophia = "SAG-IPA Super SILSophia"
fontNameSAGIPSSILSophiaItalic = "SAG-IPA Super SILSophia Italic"
fontNameSAGIPSSILSophiaBold = "SAG-IPA Super SILSophia Bold"
fontNameSAGIPSSILSophiaBoldItalic = "SAG-IPA Super SILSophia Bold Italic"

fontNameDoulosSIL = "Doulos SIL"
fontNameCharisSIL = "Charis SIL"

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
aECs.AddFont fontNameSAGIPSSILCharis, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILCharisItalic, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILCharisBold, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILCharisBoldItalic, 42, encodingNameLhs

aECs.AddFont fontNameSAGIPSSILDoulos, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILDoulosItalic, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILDoulosBold, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILDoulosBoldItalic, 42, encodingNameLhs

aECs.AddFont fontNameSAGIPSSILManuscript, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILManuscriptItalic, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILManuscriptBold, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILManuscriptBoldItalic, 42, encodingNameLhs

aECs.AddFont fontNameSAGIPSSILSophia, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILSophiaItalic, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILSophiaBold, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPSSILSophiaBoldItalic, 42, encodingNameLhs

aECs.AddFontMapping mapName, fontNameSAGIPSSILDoulos, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSAGIPSSILDoulosItalic, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSAGIPSSILDoulosBold, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSAGIPSSILDoulosBoldItalic, fontNameDoulosSIL

aECs.AddFontMapping mapName, fontNameSAGIPSSILCharis, fontNameCharisSIL
aECs.AddFontMapping mapName, fontNameSAGIPSSILCharisItalic, fontNameCharisSIL
aECs.AddFontMapping mapName, fontNameSAGIPSSILCharisBold, fontNameCharisSIL
aECs.AddFontMapping mapName, fontNameSAGIPSSILCharisBoldItalic, fontNameCharisSIL

Set aECs = Nothing
