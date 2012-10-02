' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SAG IPA Super <> UNICODE Encoding Conversion as defined in SAGIPS2Uni.tec
mapName = "TransRoman<>UNICODE"
fileName = "MapsTables\TRom2Uni.tec"
encodingNameLhs = "SIL-SAG_TransRoman21-2002"
encodingNameRhs = "UNICODE"

fontNameTransRoman2Charis = "TransRoman2 Charis"
fontNameTransRoman2CharisBold = "TransRoman2 Charis Bold"
fontNameTransRoman2CharisBoldItalic = "TransRoman2 Charis Bold Italic"
fontNameTransRoman2CharisItalic = "TransRoman2 Charis Italic"

fontNameTransRoman2Doulos  = "TransRoman2 Doulos"
fontNameTransRoman2DoulosBold = "TransRoman2 Doulos Bold"
fontNameTransRoman2DoulosBoldItalic = "TransRoman2 Doulos Bold Italic"
fontNameTransRoman2DoulosItalic = "TransRoman2 Doulos Italic"

fontNameTransRoman2Manuscript = "TransRoman2 Manuscript"
fontNameTransRoman2ManuscriptBold = "TransRoman2 Manuscript Bold"
fontNameTransRoman2ManuscriptBoldItalic = "TransRoman2 Manuscript Bold Italic"
fontNameTransRoman2ManuscriptItalic = "TransRoman2 Manuscript Italic"

fontNameTransRoman2Sophia = "TransRoman2 Sophia"
fontNameTransRoman2SophiaBold = "TransRoman2 Sophia Bold"
fontNameTransRoman2SophiaBoldItalic = "TransRoman2 Sophia Bold Italic"
fontNameTransRoman2SophiaItalic = "TransRoman2 Sophia Italic"

fontNameDoulosSIL = "Doulos SIL"
fontNameCharisSIL = "Charis SIL"

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
aECs.AddFont fontNameTransRoman2Charis, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2CharisItalic, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2CharisBold, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2CharisBoldItalic, 1252, encodingNameLhs

aECs.AddFont fontNameTransRoman2Doulos, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2DoulosItalic, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2DoulosBold, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2DoulosBoldItalic, 1252, encodingNameLhs

aECs.AddFont fontNameTransRoman2Manuscript, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2ManuscriptItalic, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2ManuscriptBold, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2ManuscriptBoldItalic, 1252, encodingNameLhs

aECs.AddFont fontNameTransRoman2Sophia, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2SophiaItalic, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2SophiaBold, 1252, encodingNameLhs
aECs.AddFont fontNameTransRoman2SophiaBoldItalic, 1252, encodingNameLhs

aECs.AddFontMapping mapName, fontNameTransRoman2Doulos, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameTransRoman2DoulosItalic, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameTransRoman2DoulosBold, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameTransRoman2DoulosBoldItalic, fontNameDoulosSIL

aECs.AddFontMapping mapName, fontNameTransRoman2Charis, fontNameCharisSIL
aECs.AddFontMapping mapName, fontNameTransRoman2CharisItalic, fontNameCharisSIL
aECs.AddFontMapping mapName, fontNameTransRoman2CharisBold, fontNameCharisSIL
aECs.AddFontMapping mapName, fontNameTransRoman2CharisBoldItalic, fontNameCharisSIL

Set aECs = Nothing
