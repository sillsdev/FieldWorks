/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: resource.h
Responsibility: Alistair Imrie
Last reviewed: never

Description:
	Defined IDs for Notebook resources.
-------------------------------------------------------------------------------*//*:End Ignore*/

/*
	IDs in actual applications start at 1024 (==WM_USER) and work UP.
	To find out why we start at WM_USER, see ms-help://MS.VSCC/MS.MSDNVS/winui/messques_4soi.htm
*/

// File menu.
#define kcidFileExpt                    1044
#define kcidFileProps                   1045

// File...Properties submenu.
#define kcidFilePropsDN                 1046
#define kcidFilePropsLP                 1047

// File...Import submenu
#define kcidFileImpt                    1048
#define kcidFileImptXML                 1049

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
#define kcidInsEntryEvent               1060
#define kcidInsEntryAnal                1061

// Insert...Sub-Entry submenu.
#define kcidInsSubentEvent              1062
#define kcidInsSubentAnal               1063

// Format menu.

// Tools menu.
#define kcidToolsOpts                   1064

// Tools...Reports submenu.

// Help...FieldWorks submenu.
#define kcidHelpConts                   1068
#define kcidHelpIndex                   1069
#define kcidHelpFind                    1070
// Help menu.
#define kcidHelpApp                     1071
#define kcidHelpHowDoI                  1072
#define kcidHelpCtxBal                  1074
#define kcidHelpStudentManual           1075
#define kcidHelpExercises               1076
#define kcidHelpInstructorGuide         1077
#define kcidHelpTraining                1078
#define kcidFindInDictionary            1079


#define kridNoteBkIcon                  1089 // Icon

// Splash window
#define kridSplashStartMessage          4300
#define kridSplashSqlMessage            4301

#define kstidSubevent                   1096
#define kstidSubanalysis                1097
#define kstidRoledParticipants          1098
#define kstidEvent                      1099
#define kstidAnalysis                   1100

#define kstidRnGenericRec_Title             1101
#define kstidRnEvent_Type                   1102
#define kstidRnEvent_Description            1103
#define kstidRnEvent_SubRecords             1104
#define kstidRnGenericRec_AnthroCodes       1105
#define kstidRnEvent_Sources                1106
#define kstidRnEvent_Participants           1107
#define kstidRnEvent_Locations              1108
#define kstidRnGenericRec_Confidence        1109
#define kstidRnGenericRec_Restrictions      1110
#define kstidRnEvent_Weather                1111
#define kstidRnGenericRec_ExternalMaterials 1112
#define kstidRnGenericRec_Researchers       1113
#define kstidRnGenericRec_VersionHistory    1114

#define kstidEventEntry                     1115
#define kstidAnalEntry                      1116
#define kstidEventSubentry                  1117
#define kstidAnalSubentry                   1118
#define kstidSpHyphenSp                     1119

#define kstidViews                          1120
#define kstidFilters                        1121
#define kstidSortMethods                    1122
#define kstidOverlays                       1123

#define kstidTlsOptCstAllEnt                1124

#define kstidResearchNotebook               1125
#define kstidRnGenericRec_DateCreated       1126
#define kstidRnGenericRec_DateModified      1127
#define kstidRnEvent_DateOfEvent            1128
#define kstidRnEvent_TimeOfEvent            1129
#define kstidRnEvent_PersonalNotes          1130
#define kstidRnGenericRec_SeeAlso           1131

#define kstidConfigureVBar                  1132
#define kstidEnumGender                     1133
#define kstidEnumNoYes                      1134
#define kstidEnumBool                       1135

#define kcidViewViewsDoc                    1136
#define kcidViewViewsBrowse                 1137
#define kstidSelectView                     1138
#define kstidSelectFilter                   1139
#define kstidSelectSortMethod               1140
#define kstidSelectOverlay                  1141
#define kstidSubentries                     1142
#define kstidEntries                        1143
#define kstidSubentry                       1144
#define kstidEntry                          1145
#define kstidFindInDictionary				1146

// NOT USED: 1147-1966

#define kstidTlsOptESeeAlso             1967
#define kstidTlsOptASeeAlso             1968
// NOT USED: 1969
#define kstidTlsOptEType                1970
#define kstidTlsOptEDescription         1971
// NOT USED: 1972
#define kstidTlsOptAnthroCodes          1973
#define kstidTlsOptESources             1974
// NOT USED: 1975
#define kstidTlsOptELocations           1976
#define kstidTlsOptEConfidence          1977
#define kstidTlsOptERestrictions        1978
#define kstidTlsOptEWeather             1979
#define kstidTlsOptEExternalMaterials   1980
#define kstidTlsOptEResearchers         1981
#define kstidTlsOptEVersionHistory      1982
// NOT USED: 1983
#define kstidTlsOptATitle               1984
#define kstidTlsOptAHypothesis          1985
#define kstidTlsOptAResearchPlan        1986
#define kstidTlsOptADiscussion          1987
#define kstidTlsOptAConclusions         1988
#define kstidTlsOptASupportingEvidence  1989
#define kstidTlsOptACounterEvidence     1990
#define kstidTlsOptASupersededBy        1991
// NOT USED: 1992
#define kstidTlsOptAStatus              1993
#define kstidTlsOptAConfidence          1994
#define kstidTlsOptARestrictions        1995
#define kstidTlsOptAResearchers         1996
#define kstidTlsOptAVersionHistory      1997
#define kstidTlsOptASubentries          1998
#define kstidLabelEntryType             1999

// NOT USED: 2011-2014
// NOT USED: 2016-2027

#define kstidTlsOptETimeOfEvent         2030
#define kstidTlsOptEDateOfEvent         2029
#define kstidTlsOptEPersonalNotes       2031
#define kstidTlsOptEDateModified        2028
#define kstidTlsOptAFurtherQuestions    2015
// NOT USED: 2032-2120
#define kstidTlsOptDocDateCreated       2121
#define kstidTlsOptDocDateModified      2122
#define kstidTlsOptParticipants         2123
#define kstidTlsOptRole                 2124
// NOT USED: 2125-2129
#define kstidRnAnalysis_Hypothesis              2130
#define kstidRnAnalysis_ResearchPlan            2131
#define kstidRnAnalysis_Discussion              2132
#define kstidRnAnalysis_Conclusions             2133
#define kstidRnGenericRec_FurtherQuestions      2134
#define kstidRnAnalysis_SupportingEvidence      2135
#define kstidRnAnalysis_CounterEvidence         2136
#define kstidRnAnalysis_SupersededBy            2137
#define kstidRnAnalysis_Status                  2138
#define kstidRnAnalysis_SubRecords              2139
#define kstidRnRoledPartic_Participants   2140
#define kstidRnRoledPartic_Role           2141
#define kstidRnRoledPartic_HelpA          2142
#define kstidRnRoledPartic_HelpB          2143

#define kstidPossibilityName                2150
#define kstidPossibilityAbbreviation        2151
#define kstidPersonAlias                    2152
#define kstidPossibilityConfidence          2153
#define kstidPersonDateOfBirth              2154
#define kstidPersonDateOfDeath              2155
#define kstidPersonDiscussion               2156
#define kstidPersonEducation                2157
#define kstidPersonGender                   2158
#define kstidPersonIsResearcher             2159
#define kstidPersonPlaceOfBirth             2160
#define kstidPersonPlacesOfResidence        2161
#define kstidPersonPositions                2162
#define kstidGenericRecord                  2163
#define kstidGenericRecordPhraseTags        2164
#define kstidGenericRecordReminders         2165
#define kstidContextEventSub                2166
#define kstidContextAnalSub                 2167

#define kridRnEntryType                 2175

#define kstidEntryTypeIconWhatsThisHelp 2183

#define kctidGeneralPropTabDnName       2185

#define kcidEditRoles                   2186
#define kstidEditRoles                  2187

#define kridBaseAppLim              2200


#define kridRnImportMin             2600
#define kridRnImportLim             2900

#define kridEmptyNotebookMin        4660
#define kridEmptyNotebookLim        4700

// Add new expandable menu items here. Make sure they are between the range given by
// kcidMenuItemExpMin and kcidMenuItemExpLim in AfCoreRes.h (28900-29100).
#define kcidExpFilters                  28901
#define kcidExpSortMethods              28902
#define kcidExpOverlays                 28903
#define kcidExpParticipants             28904
//WARNING kcidMenuItemExpLim            29100
