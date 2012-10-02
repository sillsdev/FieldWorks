/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: resource.h
Responsibility: Alistair Imrie
Last reviewed: never

Description:
	Defined IDs for Topics List Editor resources.
-------------------------------------------------------------------------------*//*:End Ignore*/

/*
	IDs in actual applications start at 1024 (==WM_USER) and work UP.
	To find out why we start at WM_USER, see ms-help://MS.VSCC/MS.MSDNVS/winui/messques_4soi.htm
*/

// File menu.
#define kcidFileNewTL                   1044
#define kcidFileImpt                    1045
#define kcidFileExpt                    1046

// File...Properties submenu.
#define kcidFilePropsTL                 1047
#define kcidFilePropsLP                 1048

#define kcidFileDelete                  1043

// View menu.
// View...Views submenu.
// View...Filters submenu.
#define kcidViewFltrsNone               1051
// View...Sort Methods submenu.
#define kcidViewSortsNone               1053
// View...Overlays submenu.
#define kcidViewOlaysNone               1055
#define kcidViewOlaysConfig             1056

// Data menu.

// Insert menu.
#define kcidInsItem                     1060
#define kcidInsItemBef                  1061
#define kcidInsItemAft                  1062
#define kcidInsSubItem                  1063
#define kcidInsEntryEvent               1066
#define kcidInsEntryAnal                1067

// Insert...Sub-Entry submenu.
#define kcidInsSubentEvent              1068
#define kcidInsSubentAnal               1069

// Format menu.

// Tools menu.
#define kcidToolsOpts                   1070

// Tools...Reports submenu.

// Help...FieldWorks submenu.
#define kcidHelpConts                   1080
#define kcidHelpIndex                   1081
#define kcidHelpFind                    1082
// Help menu.
#define kcidHelpApp                     1083
#define kcidHelpHowDoI                  1084
#define kcidHelpStudentManual           1086
#define kcidHelpCtxBal                  1087

#ifdef DEBUG
#define kcidPossChsr                    1090
#define kcidTagOverlayTool              1091
#endif

#define kridCleIcon                     1102 // Icon

// Splash window
#define kridSplashStartMessage          4300
#define kridSplashSqlMessage            4301

#define kstidViews                          1120
#define kstidFilters                        1121
#define kstidSortMethods                    1122
#define kstidSortIncLabel                   1123

#define kstidList                           1124

#define kstidChoicesListEditor              1127
#define kstidConfigureVBar                  1130

#define kstidSelectView                     1138
#define kstidSelectFilter                   1139
#define kstidSelectSortMethod               1140

// CLE extras
#define kcidViewTreeAbbrevs                 1150
#define kcidViewTreeNames                   1151
#define kcidViewTreeBoth                    1152
#define kcidInsListItemBef                  1153
#define kcidInsListItemAft                  1154
#define kcidInsListSubItem                  1155
#define kstidInsListItem                    1156
#define kstidInsListItemBef                 1157
#define kcidInsListItem                     1158

// Above are used by RN and may not all be needed for CLE
#define kstidCmPossibility_Name             1160
#define kstidCmPossibility_Abbreviation     1161
#define kstidCmPossibility_Description      1163
#define kstidCmPossibility_Restrictions     1164
#define kstidCmPossibility_Confidence       1165
#define kstidCmPossibility_DateCreated      1166
#define kstidCmPossibility_DateModified     1167
#define kstidCmPossibility_Researchers      1168
#define kstidCmPossibility_Discussion       1172

#define kstidCmPossibility                  1174

#define kcidTrBarMerge                      1176
#define kstidCmPossibility_SortSpec         1177

#define kstidEnumGender                     1178
#define kstidEnumNoYes                      1179
#define kstidEnumBool                       1180

#define kcidTrBarDelete                     1181
#define kcidTrBarInsert                     1182
#define kcidTrBarInsertBef                  1183
#define kcidTrBarInsertAft                  1184
#define kcidTrBarInsertSub                  1185

// View...Overlays submenu.
//#define kcidViewTreesNone                 1012
#define kcidViewTreesConfig                 1186


#define kstidCmPossibility_HelpId           1187
#define kstidCmPerson_Alias                 1188
#define kstidCmPerson_Gender                1189
#define kstidCmPerson_DateOfBirth           1190
#define kstidCmPerson_PlaceOfBirth          1191
#define kstidCmPerson_IsResearcher          1192
#define kstidCmPerson_PlacesOfResidence     1193
#define kstidCmPerson_Education             1194
#define kstidCmPerson_DateOfDeath           1195
#define kstidCmPerson_Positions             1196
#define kstidCmLocation_Alias               1197

#define kstidTlsOptClName                   1198
#define kstidTlsOptClPossibilities          1199
#define kstidTlsOptClDiscussion             1200
#define kstidTlsOptClAbbreviation           1201
#define kstidTlsOptClDescription            1202
#define kstidTlsOptClSortSpec               1203
#define kstidTlsOptERestrictions            1204
#define kstidTlsOptEConfidence              1205
#define kstidTlsOptEDateModified            1207
#define kstidTlsOptEResearchers             1208
#define kstidTlsOptHelpId                   1209
#define kstidListEntry                      1211

#define kstidPossibilityFmt                 1237

#define kstidTlsOptPerAlias                 1257
#define kstidTlsOptPerGender                1258
#define kstidTlsOptPerDateOfBirth           1259
#define kstidTlsOptPerPlaceOfBirth          1260
#define kstidTlsOptPerIsResearcher          1261
#define kstidTlsOptPerPlacesOfResidence     1262
#define kstidTlsOptPerEducation             1263
#define kstidTlsOptPerDateOfDeath           1264
#define kstidTlsOptPerPositions             1265
#define kstidTlsOptPerDateModified          1266
#define kstidTlsOptPerResearchers           1267

#define kstidTlsOptLocAlias                 1268
#define kstidDefaultListName                1271

#define kstidCmPerson_Name              1279
#define kstidCmPerson_Discussion        1280
#define kstidCmLocation_Name            1281
#define kstidTlsOptPerName              1282
#define kstidTlsOptPerDiscussion        1283
#define kstidTlsOptLocName              1284
#define kstidAnthroList                 1285    // Used for empty list in new project.

// File...Open dialog popup menu.
#define kcidCleCopyList                 1291

// TODO: remove this stuff:
#define kstidAnalysis                   1300
#define kstidEvent                      1301
#define kstidSubevent                   1302
#define kstidSubanalysis                1303
#define kstidSpHyphenSp                 1304

#define kstidTlsOptPossibility          1317
#define kstidTlsOptPerson               1318
#define kstidTlsOptLocation             1319
#define kstidTlsOptAnthroItem           1320
#define kstidTlsOptCustomItem           1321
#define kstidTlsOptLexEntryType			1322
#define kstidTlsOptMoMorphType          1323
#define kstidTlsOptPartOfSpeech         1324
#define kstidTlsOptCstAllPss            1325

#ifdef ADD_LEXTEXT_LISTS
#define kstidTlsOptAnnAllowsComment     1336
#define kstidTlsOptAnnAllowsFeatStruct  1337
#define kstidTlsOptAnnAllowsInstOf      1338
#define kstidTlsOptAnnInstOfSig         1339
#define kstidTlsOptAnnUserCanCreate     1340
#define kstidTlsOptAnnCanCreateOrphan   1341
#define kstidTlsOptAnnPromptUser        1342
#define kstidTlsOptAnnCopyCutPastable   1343
#define kstidTlsOptAnnZeroWidth         1344
#define kstidTlsOptAnnMulti             1345
#define kstidTlsOptAnnSeverity          1346
#define kstidTlsOptTypType			    1347
#define kstidTlsOptTypReverseAbbr		1366
#define kstidTlsOptMorPostFix           1348
#define kstidTlsOptMorPreFix            1349
#define kstidTlsOptMorSecondaryOrder    1350



#define kstidCmAnnotationDefn_AllowsComment             1351
#define kstidCmAnnotationDefn_AllowsFeatureStructure    1352
#define kstidCmAnnotationDefn_AllowsInstanceOf          1353
#define kstidCmAnnotationDefn_InstanceOfSignature       1354
#define kstidCmAnnotationDefn_UserCanCreate             1355
#define kstidCmAnnotationDefn_CanCreateOrphan           1356
#define kstidCmAnnotationDefn_PromptUser                1357
#define kstidCmAnnotationDefn_CopyCutPastable           1358
#define kstidCmAnnotationDefn_ZeroWidth                 1359
#define kstidCmAnnotationDefn_Multi                     1360
#define kstidCmAnnotationDefn_Severity                  1361
#define kstidLexEntryType_ReverseAbbr					1367
#define kstidMoMorphType_PostFix                        1363
#define kstidMoMorphType_PreFix                         1364
#define kstidMoMorphType_SecondaryOrder                 1365
//#define kstidTlsOptTypReverseAbbr						1366
//#define kstidLexEntryType_ReverseAbbr					1367
#endif

// Add new expandable menu items here. Make sure they are between the range given by
// kcidMenuItemExpMin and kcidMenuItemExpLim in AfCoreRes.h (28900-29100).
#define kcidExpFilters                  28901
#define kcidExpSortMethods              28902
