' See the "EC Installation Readme.txt" file for details of these methods, and parameters
' This map is for the SIL PNG <> Unicode Encoding Conversion as defined in Cameroon2Unicode2007.tec
mapName = "Cameroon<>UNICODE"
fileName = "MapsTables\Cameroon2Unicode2007.tec"
encodingNameLhs = "Cameroon"
encodingNameRhs = "Unicode IPA"

fontNameCamCamSILDoulosL = "Cam Cam SILDoulosL"
fontNameCamCamSILSophiaL = "Cam Cam SILSophiaL"
fontNameCamCamSILManuscriptL = "Cam Cam SILManuscriptL"

fontNameCam2Cam2SILDoulos = "Cam2 Cam2 SILDoulos"
fontNameCam2Cam2SILSophia = "Cam2 Cam2 SILSophia"
fontNameCam2Cam2SILManuscript = "Cam2 Cam2 SILManuscript"

fontNameCamParatextSILDoulos = "Cam Paratext SILDoulos"
fontNameCamParatextSILSophia = "Cam Paratext SILSophia"
fontNameCamParatextSILManuscript = "Cam Paratext SILManuscript"

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

' for the corresponding fonts, we also want to add an association between that font
' and the encoding name
aECs.AddFont fontNameCamCamSILDoulosL, 1252, encodingNameLhs
aECs.AddFont fontNameCamCamSILSophiaL, 1252, encodingNameLhs
aECs.AddFont fontNameCamCamSILManuscriptL, 1252, encodingNameLhs
aECs.AddFont fontNameCam2Cam2SILDoulos, 1252, encodingNameLhs
aECs.AddFont fontNameCam2Cam2SILSophia, 1252, encodingNameLhs
aECs.AddFont fontNameCam2Cam2SILManuscript, 1252, encodingNameLhs
aECs.AddFont fontNameCamParatextSILDoulos, 1252, encodingNameLhs
aECs.AddFont fontNameCamParatextSILSophia, 1252, encodingNameLhs
aECs.AddFont fontNameCamParatextSILManuscript, 1252, encodingNameLhs

' default conversions of SILDoulos to Doulos SIL for this mapping
aECs.AddFontMapping mapName, fontNameCamCamSILDoulosL, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameCam2Cam2SILDoulos, fontNameDoulosSIL
aECs.AddFontMapping mapName, fontNameCamParatextSILDoulos, fontNameDoulosSIL

Set aECs = Nothing
