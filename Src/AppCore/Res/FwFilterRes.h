/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwFilterRes.h
Responsibility:
Last reviewed: never

Description:
	Resource definitions for FwFilterDlg and related dialogs.  These must be in the range
	given by kridFltrDlgsMin and kridFltrDlgsLim in AfCoreRes.h (31200-31500).
-------------------------------------------------------------------------------*//*:End Ignore*/

#undef VERSION2FILTER /* Marks items which are not implemented in Version 1 of Data Notebook. */

#define kridFilterSimpleDlg             31200
#define kridFilterFullDlg               31201
#define kridFilterSimpleShellDlg        31202
#define kridFilterBuilderShellDlg       31203
#define kridFilterNoMatchDlg            31204
#define kridFilterPromptDlg             31205
#define kridTlsOptDlgFltr               31206
#define kridFilterFullShellDlg          31207
#define kridFilterTips                  31208

#define kctidFilterList                 31210
#define kctidAddFilter                  31211
#define kctidCopyFilter                 31212
#define kctidDeleteFilter               31213
#define kctidPrompt                     31214
#define kctidCondition                  31215
#define kctidShowTips                   31216
#define kctidExpand                     31217
#define kctidShowBuilder                31218
#define kctidField                      31219
#define kctidCriteria                   31220
#define kctidAnd                        31221
#define kctidOr                         31222

#define kstidTlsOptFltr                 31224
#define kridFilterButtonImages          31225
#define kctidFilterSpecial              31226
#define kctidFilterFormat               31227
#define kctidFilterMatchCase            31228
#define kctidFilterMatchDiac            31229
#define kctidFilterText                 31230
#define kctidTextLabel                  31231
#define kctidRefLabel                   31232
#define kctidScopeLabel                 31233
#define kctidDateLabel                  31234
#define kctidFilterRef                  31235
#define kctidFilterDate                 31236
#define kctidFilterScope                31237
#define kctidFilterChooseItem           31238
#define kctidFilterChooseDate           31239
#define kctidFilterSubitems             31240
#define kctidFilterCriteria             31241
#define kcidAddSimpleFilter             31242
#define kcidAddFullFilter               31243

#define kstidFltrEmpty                  31245
#define kstidFltrNotEmpty               31246
#define kstidFltrContains               31247
#define kstidFltrDoesNotContain         31248
#define kstidFltrMatches                31249
#define kstidFltrDoesNotMatch           31250
#define kstidFltrEqualTo                31251
#define kstidFltrNotEqualTo             31252
#define kstidFltrGreaterThan            31253
#define kstidFltrLessThan               31254
#define kstidFltrGreaterThanEqual       31255
#define kstidFltrLessThanEqual          31256
#define kstidFltrOn                     31257
#define kstidFltrNotOn                  31258
#define kstidFltrAfter                  31259
#define kstidFltrBefore                 31260
#define kstidFltrOnAfter                31261
#define kstidFltrOnBefore               31262
#define kridFilterErrorMsgDlg           31263
#define kcidFltrErrorMsg                31264

#define kridFltrPopups                  31300
#define kcidFltrNoEntries               31301

#define kcidFltrFmtFont                 31312
#define kcidFltrFmtStyle                31313
#define kcidFltrFmtNone                 31314
#define kstidFltrExactDate              31315
#ifdef VERSION2FILTER
#define kstidFltrMonth                  31316
#endif /*VERSION2FILTER*/
#define kstidFltrMonthYear              31317
#define kstidFltrYear                   31318
#ifdef VERSION2FILTER
#define kstidFltrDay                    31319
#endif /*VERSION2FILTER*/
#define kstidFltrToday                  31320
#define kstidFltrLastWeek               31321
#define kstidFltrLastMonth              31322
#define kstidFltrLastYear               31323
#ifdef VERSION2FILTER
#define kstidFltrLast7Days              31324
#define kstidFltrLast30Days             31325
#define kstidFltrLast365Days            31326
#endif /*VERSION2FILTER*/
#define kctidFilterField                31327
#define kcidFullFilterPopupMenu         31328
#define kstidFltrAnd                    31329
#define kstidFltrOr                     31330
#define kstidFltrChooseField            31331
#define kstidFltrChooseFieldNHK         31332
#define kcidFullFilterDelCol            31333
#define kstidFltrRemoveField            31334
#define kstidFltrDelFieldMsg            31335
#define kstidFltrFullBldrTitle          31336
#define kridFltrTurnOff                 31337
#define kridFltrModifyFilter            31338
#define kridFltrSelectNew               31339
#define kctidInsert                     31340
#define kcidSimpleFilterPopupMenu       31341
#define kctidConditionLabel             31342
#define kstidFltrDefaultPrompt          31343
#define kstidFilterDate                 31344
#define kstidFilterText                 31345
#define kstidFilterReference            31346
#define kstidFilterTag                  31347
#define kctidConditionRef               31348
#define kstidFltrNewSimple              31349
#define kstidFltrNewFull                31350
#define kstidFltrDelFilterMsg           31351
#define kstidFltrRenFilterMsg           31352
#define kstidFltrRenEmptyMsg            31353
#define kstidNoFilterHotKey             31354
#define kctidFltrFieldLabel             31355
#define kctidPromptLabel                31356
#define kstidFilterEnum                 31357
#define kctidEnumLabel                  31358
#define kctidFilterEnum                 31359
#define kridFilterTurnOffDlg            31360
#define kctidFltrInfoIcon               31361
#define kctidFltrNoShow                 31362
#define kstidFltrYes                    31363
#define kstidFltrNo                     31364
#define kstidFltrError                  31365
#define kstidFltrImproper               31366
#define kstidFltrDateProb               31367
#define kstidFltrBadBigStringCompare    31368
//#define kstidFltrInvalidSQL             31369
//#define kstidFltrBug                    31370
#define kstidFltrOrSortError            31369
#define kstidFltrOrSortImproper         31370
#define kstidFltrInvalidWildPattern     31371
#define kstidFltrAndSubitems            31372
#define kstidFltrNoEntries              31373
#define kstidFltrDateProbCap            31374
#define kstidFilterDateTtl              31375
#define kstidFltrPersonMenuLabel        31376
#define kstidFltrAnyRoleMenuLabel       31377
#define kstidFltrNoRoleMenuLabel        31378
#define kstidFilterNumber               31379
#define kstidFilterBoolean              31380
#define kctidNumberLabel                31381
#define kctidFilterNumber               31382
#define kstidFltrDefaultBooleanPrompt   31383
#define kstidDeleteFilter               31384
#define kstidFltrCannotCreate           31385
#define kstidFltrCannotEmptyList        31386
#define kcidFltrTurnOffInfo             31387
#define kcidFltrTurnOffQuestion         31388
#define kstidFltrTurnOffInfo            31389
#define kstidFltrTurnOffQuestion        31390
#define kstidFltrUndefinedFilter        31391
#define kstidFltrUndefinedFilters       31392
#define kstidFltrImproperAdv            31393
#define kstidFltrOrSortImproperAdv      31394
#define kstidFltrNumberCap              31395
#define kstidFltrNumberError            31396
#define kstidFltrMaxIntMsg              31397
#define kstidStBar_NoFilterMatch        31398
#define kstidStBar_ApplyingFilter       31399
#define kstidStBar_RemovingFilter       31400

#define kwidFltrTableHeader             31498
#define kwidFltrTable                   31499
