' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the IPA93 <> Unicode Encoding Conversion as defined in SIL-IPA93.tec
mapName = "SIL IPA93<>UNICODE"
fileName = "MapsTables\silipa93.tec"
encodingNameLhs = "SIL-IPA93-2001"
encodingNameRhs = "Unicode IPA"
fontNameSILDoulos = "SILDoulos IPA93"
fontNameSILManuscript = "SILManuscript IPA93"
fontNameSILSophia = "SILSophia IPA93"
fontNameDoulosSIL = "Doulos SIL"
fontNameCourierNew = "Courier New"
keyboardName = "IPA93 1.0 (FF)"

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
aECs.AddFont fontNameSILDoulos, 42, encodingNameLhs
aECs.AddFont fontNameSILManuscript, 42, encodingNameLhs
aECs.AddFont fontNameSILSophia, 42, encodingNameLhs
aECs.AddUnicodeFontEncoding fontNameDoulosSIL, encodingNameRhs

' default conversions of SILDoulos to Doulos SIL for this mapping
aECs.AddFontMapping mapName, fontNameSILDoulos, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameSILManuscript, fontNameCourierNew

' indicate which Keyman keyboard goes with these font names
' but these may fail if it's already been added (by a previous run)
On Error Resume Next
PropertyType_FontName = 2
aECs.Attributes(fontNameSILDoulos, PropertyType_FontName).Add "Keyman Keyboard", keyboardName
aECs.Attributes(fontNameSILManuscript, PropertyType_FontName).Add "Keyman Keyboard", keyboardName
aECs.Attributes(fontNameSILSophia, PropertyType_FontName).Add "Keyman Keyboard", keyboardName

Set aECs = Nothing
