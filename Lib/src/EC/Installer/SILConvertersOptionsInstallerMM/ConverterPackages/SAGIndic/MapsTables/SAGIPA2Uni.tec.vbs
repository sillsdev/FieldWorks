' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SAG IPA <> UNICODE Encoding Conversion as defined in SAGIPA2Uni.tec
mapName = "SAG IPA<>UNICODE"
fileName = "MapsTables\SAGIPA2Uni.tec"
encodingNameLhs = "SIL-SAG-IPA"
encodingNameRhs = "UNICODE"

fontNameSAGIPASILDoulos = "SAG-IPA SILDoulos"
fontNameSAGIPASILDoulosItalic = "SAG-IPA SILDoulos Italic"
fontNameSAGIPASILDoulosBold = "SAG-IPA SILDoulos Bold"
fontNameSAGIPASILDoulosBoldItalic = "SAG-IPA SILDoulos Bold Italic "

fontNameSAGIPASILManuscript = "SAG-IPA SILManuscript"
fontNameSAGIPASILManuscriptItalic = "SAG-IPA SILManuscript Italic"
fontNameSAGIPASILManuscriptBold = "SAG-IPA SILManuscript Bold"
fontNameSAGIPASILManuscriptBoldItalic = "SAG-IPA SILManuscript Bold Italic "

fontNameSAGIPASILSophia = "SAG-IPA SILSophia"
fontNameSAGIPASILSophiaItalic = "SAG-IPA SILSophia Italic"
fontNameSAGIPASILSophiaBold = "SAG-IPA SILSophia Bold"
fontNameSAGIPASILSophiaBoldItalic = "SAG-IPA SILSophia Bold Italic"

fontNameDoulosSIL = "Doulos SIL"

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
aECs.AddFont fontNameSAGIPASILDoulos, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILDoulosItalic, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILDoulosBold, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILDoulosBoldItalic, 42, encodingNameLhs

aECs.AddFont fontNameSAGIPASILManuscript, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILManuscriptItalic, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILManuscriptBold, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILManuscriptBoldItalic, 42, encodingNameLhs

aECs.AddFont fontNameSAGIPASILSophia, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILSophiaItalic, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILSophiaBold, 42, encodingNameLhs
aECs.AddFont fontNameSAGIPASILSophiaBoldItalic, 42, encodingNameLhs

aECs.AddFontMapping mapName, fontNameSAGIPASILDoulos, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSAGIPASILDoulosItalic, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSAGIPASILDoulosBold, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSAGIPASILDoulosBoldItalic, fontNameDoulosSIL

Set aECs = Nothing
