/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: resource.h
Responsibility: Sharon Correll
Last reviewed: never

Description:
	Defined IDs for WorldPad resources.
-------------------------------------------------------------------------------*//*:End Ignore*/

/*
	IDs in actual applications start at 1024 (==WM_USER) and work UP.
	To find out why we start at WM_USER, see ms-help://MS.VSCC/MS.MSDNVS/winui/messques_4soi.htm
*/

// File menu.
#define kcidFileImpt                    1045
#define kcidFileExpt                    1046
#define kcidFileProps                   1047

// Edit menu.

// View menu.
// View...Synchronize submenu.
#define kcidViewSyncSend                1050
#define kcidViewSyncRecv                1051
#define kcidViewSyncConfig              1052

// A few things from the data menu--WP will put them in Edit.
#define kcidDataSrch                    1053
#define kcidDataNextMatch               1054
#define kcidDataPrevMatch               1055
#define kcidDataRepl                    1056

// Insert menu.
#define kcidInsEntryEvent				1060
#define kcidInsEntryHyp                 1061

#define kcidInsComment					1062
#define kcidInsTable                    1063
#define kcidInsSound                    1064
#define kcidInsVideo                    1065
#define kcidInsObj                      1066

// Format menu.
#define kcidFmtTable                    1072
#define kcidFmtDoc						1073

#define kcidToolsExpl                   1099
#define kcidToolsShowSelUsing           1100
#define kcidToolsOpts                   1101
#define kcidToolsCust                   1102
#define kcidToolsOldWritingSystems			1400

// Help...FieldWorks submenu.
#define kcidHelpConts                   1107
#define kcidHelpIndex                   1108
#define kcidHelpFind                    1109
// Help menu.
#define kcidHelpApp                     1110
#define kcidHelpHowDoI                  1111
#define kcidHelpStudentManual           1113
#define kcidHelpCtxBal                  1114

#define kridVBarSmall                   1125 // Bitmap
#define kridVBarLarge                   1126 // Bitmap
#define kridWorldPadIcon                1128 // Icon

//#define kstidNinch                      1129

// Writing Systems dialog
#define kridOldWritingSystemsDlg           1130
#define kctidWritingSystems                  1131
#define kctidDeleteWs                  1132
#define kctidAddWs                     1133
#define kctidKeymanKeyboard            1003
//#define kctidRendererType               1134
#define kctidFont                       1135
#define kctidKeyboardType               1136
#define kctidFeatures					1137

#define kctidRightToLeft                1138
#define kctidWsName					    1139
#define kctidWsCode                     1140
#define kctidWsDescrip                  1141
#define kctidLangId                     1142
#define kctidFocusRenderer              1143
#define kctidFocusFonts                 1144
#define kctidFocusKeyboard              1145
#define kctidFocusLangId                1146

#define kctidFeaturesPopup				1147

// New Writing system dialog
#define kridNewWs                      1148
#define kctidWs                        1149

// Delete Writing system dialog
#define kridDeleteWs                   1150
#define kctidWsToDel                   1151

// Save Plain Text dialog
#define kridSavePlainTextDlg			1152
#define kctidTextWs					    1153

// Options dialog
#define kridOptionsDlg					1154
#define kctidArrLog                     1155
#define kctidArrVis                     1156
//#define kctidShArrLog                   1019  not currently used
//#define kctidShArrVis                   1020
//#define kctidHomeLog                    1021
//#define kctidHomeVis                    1022
#define kctidGraphiteLog                1157

// Document dialog
#define kridDocDlg						1158
#define kctidDocLtr						1159
#define kctidDocRtl						1160

//#define kcidApplyNormalStyle            1161	// defined in AfDef.h

// Messages and labels
#define kstidCouldntCommit				1200
#define kstidOverwriteFile				1201
#define kstidSaveChanges				1202
#define kstidSaveChangesNoName			1203
#define kstidWindowLabel				1206
#define kstidKeyboardMismatch			1207
#define kstidUtf16						1208
#define kstidUtf8						1209
#define kstidAnsi						1210
#define kstidSavePlainText				1211
#define kstidOpenPlainText				1212
#define kstidCantSaveFile				1213
#define kstidCantFormatAnsi				1214
#define kstidXsltFailed					1215
#define kstidSavingWsTitle				1216
#define kstidSavingWsMsg				1217

// Message and labels for Writing System Properties dialog
#define kstidStandard					1221
#define kstidKeyman						1222
#define kstidOther						1223
#define kstidGraphite					1224
#define kstidCantDeleteDefWs			1225
#define kstidInvalidRenderer			1226
#define kstidPlsEnterFont				1227
#define kstidInvalidFont				1228
#define kstidInvalidGrFont				1229
#define kstidInvalidKeybType			1230
#define kstidInvalidLangId				1231
#define kstidWsInUse					1232
#define kstidWsInUseMultDoc				1233
#define kstidInvalidWs					1234
#define kstidDupWs						1235
#define kstidUniscribe					1236
#define kstidCorrectedWs				1237


// XML import/export
#define kstidWpXmlErrMsg001				1251
#define kstidWpXmlErrMsg002				1252
#define kstidWpXmlErrMsg003				1253
#define kstidWpXmlErrMsg004				1254
#define kstidWpXmlErrMsg005				1255
#define kstidWpXmlErrMsg006				1256
#define kstidWpXmlErrMsg007				1257
#define kstidWpXmlErrMsg008				1258
#define kstidWpXmlErrMsg009				1259
#define kstidWpXmlErrMsg010				1260
#define kstidWpXmlErrMsg011				1261
#define kstidWpXmlErrMsg012				1262
#define kstidWpXmlErrMsg013				1263
#define kstidWpXmlErrMsg014				1264
#define kstidWpXmlErrMsg015				1265
#define kstidWpXmlErrMsg016				1266
#define kstidWpXmlErrMsg017				1267
#define kstidWpXmlErrMsg018				1268
#define kstidWpXmlErrMsg019				1269
#define kstidWpXmlErrMsg020				1270
#define kstidWpXmlErrMsg021				1271
#define kstidWpXmlErrMsg022				1272
#define kstidWpXmlErrMsg023				1273
#define kstidWpXmlErrMsg024				1274
#define kstidWpXmlErrMsg025				1275
#define kstidWpXmlErrMsg026				1276
#define kstidWpXmlErrMsg027				1277
#define kstidWpXmlErrMsg028				1278
#define kstidWpXmlErrMsg029				1279
#define kstidWpXmlErrMsg030				1280
#define kstidWpXmlErrMsg031				1281
#define kstidWpXmlErrMsg032				1282
#define kstidWpXmlErrMsg033				1283
#define kstidWpXmlErrMsg034				1284
#define kstidWpXmlErrMsg035				1285
#define kstidWpXmlErrMsg036				1286
#define kstidWpXmlErrMsg037				1287
