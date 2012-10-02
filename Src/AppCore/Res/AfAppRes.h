/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: AfAppRes.h
Responsibility:
Last reviewed: never

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/

#include "..\..\Generic\GenericResource.h"

#define kstidErrorOutOfMemOrResource	22800
#define kstidErrorFileNotFound			22801
#define kstidErrorPathNotFound			22802
#define kstidErrorBadFormatExe			22803
#define kstidErrorAccessDenied			22804
#define kstidErrorAssocIncomplete		22805
#define kstidErrorDDEBusy				22806
#define kstidErrorDDEFail				22807
#define kstidErrorDDETimeOut			22808
#define kstidErrorDLLNotFound			22809
#define kstidErrorFNF					22810
#define kstidErrorNoAssoc				22811
#define kstidErrorOutOfMemory			22812
#define kstidErrorPNF					22813
#define kstidErrorShare					22814
#define kstidErrorSqlSvrOutOfMemTtl		22815
#define kstidErrorSqlSvrOutOfMemLcl		22816
#define kstidErrorSqlSvrOutOfMemNtw		22817
#define kstidErrorUnrecognized			22818

#define kstidReasonDisconnectStrCrawl	22820
#define kstidRemoteReasonStrCrawl		22821
#define kstidReasonDisconnectImport	22822
#define kstidRemoteReasonImport			22823

#define kstidWSVernWs					22824
#define kstidWSAnalWs					22825
#define kstidWSVernWss					22826
#define kstidWSAnalWss					22827
#define kstidWSAnalVernWss				22828
#define kstidWSVernAnalWss				22829
#define kstidWSAnals					22830
#define kstidWSVerns					22831
#define kstidWSAnalVerns				22832
#define kstidWSVernAnals				22833

#define kstidWizProjMsgCaption          22840

#define kridWndSplitMin					23990
#define kcidWndSplitOff					23997
#define kcidWndSplitOn					23998
#define kcidWndSplit					23999
#define kridWndSplitLim					24000


#define kridDbCrawlerProgress			24000
#define kctidDbCrawlProgProgress		24001
#define kctidDbCrawlProgAction			24002

#define kstidMergeItemPhaseOne			24005
#define kstidMergeItemPhaseTwo			24006
#define kstidDeleteItemPhaseOne			24007
#define kstidDeleteItemPhaseTwo			24008
#define kstidMergePos					24009
#define kstidConfirmUndoableActionMsg	24010
#define kstidConfirmUndoableActionCpt	24011

// Miscellaneous error and message strings:
#define	kstidInitAppError				24300
#define	kstidUnknExcnError				24301
#define	kstidCghtExcnError				24302
#define	kstidMiscError					24303
#define	kstidErrorEmail					24304
#define	kstidUnknErrorEmail				24305
#define	kstidNoCompError				24306
#define	kstidNoDataError				24307
#define	kstidNoProjError				24308
#define	kstidMissObjError				24309
#define	kstidNoObjError					24310
#define	kstidMissDataError				24311
#define	kstidSqlError					24312
#define	kstidUnknError					24313
#define	kstidInitRetry					24314
#define	kstidNoHelpError				24315
#define	kstidHelpAbout					24316
#define	kstidDiskSpace					24317
#define	kstidFreeSpace					24318
#define	kstidAboutVersion				24319
#define	kstidAppVersion					24320
#define	kstidKeyCtrl					24321
#define	kstidKeyAlt						24322
#define	kstidKeyShift					24323
#define	kstidKeyDelete					24324
#define	kstidLangUnknown				24325
#define kstidOverlayNone				24326
#define kstidChoicesFail				24327
#define kstidTagTextDemo				24328
#define kstidDrawError					24329
#define kstidInvalKybd					24330
#define kstidInvalKybdCaption			24331

#define kstidSvrCnctError				24332
#define kstidFntHdrFtrWarn				24333
#define kstidUnspec						24334
#define kstidOk							24335
#define kstidCls						24336
#define kstidTrainErrorTitle			24337
#define kstidTrainErrorMsg				24338
#define kstidHLErrorTitle				24339
#define kstidHLErrorMsg					24340
#define kstidHLErrorMsg2				24341
#define kstidInvalidDatabaseName		24342
#define kstidExtLinkOpen                24343
#define kcidExtLinkOpen                 24344
#define kstidExtLinkOpenWith            24345
#define kcidExtLinkOpenWith             24346
#define kstidExtLink                    24347
#define kcidExtLink                     24348
#define kstidExtLinkRemove              24349
#define kcidExtLinkRemove               24350
#define kstidExtLinkTitle               24351
#define kstidExtLinkRemovePrompt        24352
#define kstidHLInvalidTitle             24353
#define kstidHLInvalidMsg               24354
#define kstidExtLinkFileAssociation     24355
#define kstidHLErrorMsg3                24356
#define kstidMaxIntMsg                  24357
#define kstidMaxIntTitle                24358
#define kstidPromoteWarning				24359
#define kstidPromoteCaption				24360
#define kstidFatalError					24361
#define kstidNoExtLink					24362
#define kridOpenProjImagesSmall         24363	// Various images.

#define kstidMissingStyleTitle			24364	// used in AfStylesheet
#define kstidMissingStyleMsg			24365

#define kcidExtLinkUrl					24366
#define	kstidFwVersion					24367
#define	kstidFwVersionWithRev			24368
#define kstidCannotMoveFile				24369
#define kstidCannotCopyFile				24370

#define kstidCaptionBarViews			25300
#define kstidCaptionBarFilters			25301
#define kstidCaptionBarSort				25302
#define kstidCaptionBarOverlay			25303
#define kstidCaptionBarTree				25304

#define kstidLeftMarginError			25305
#define kstidRightMarginError			25306
#define kstidTopMarginError				25307
#define kstidBottomMarginError			25308
#define kstidHeaderMarginError			25309
#define kstidFooterMarginError			25310

#define kstidUnprintableText			25311
#define kstidUnprintableHeader			25312
#define kstidUnprintableFooter			25313


// Undo string ids.
#define kstidUndoUnknown				25400
#define kstidRedoUnknown				25401
#define kstidUndoTyping					25402
#define kstidUndoDelete					25403
#define kstidUndoFormatting				25404
#define kstidUndoFontFormatting			25405
#define kstidUndoParaFormatting			25406
#define kstidUndoBulAndNum				25407
#define kstidUndoBorder					25408
#define kstidUndoStyleChanges			25409
#define kstidUndoApplyStyle				25410
#define kstidUndoWritingSys				25411
#define kstidUndoFontSize				25412
#define kstidUndoFont					25413
#define kstidUndoBold					25414
#define kstidUndoItalic					25415
#define kstidUndoBackColor				25416
#define kstidUndoForeColor				25417
#define kstidUndoParaAlign				25418
#define kstidUndoNumber					25419
#define kstidUndoBullet					25420
#define kstidUndoDecIndent				25421
#define kstidUndoIncIndent				25422
#define kstidUndoCut					25423
#define kstidUndoPaste					25424
#define kstidUndoChangesTo				25425
#define kstidUndoRefDropTo				25426
#define kstidUndoRefDel					25427
// Used by print progress
#define kctidPrintProgress				25428
#define kstidLayingOutPage				25429
#define kstidCalculatingPages			25430
#define kstidPrintingPage				25431
#define kstidUndoMoveX					25432
#define kstidUndoPromoteX				25433
#define kstidUndoDeleteX				25434
#define kstidUndoInsertX				25435
#define kstidUndoExtLink				25436
#define kstidUndoFieldDisabled			25437
#define kstidRedoFieldDisabled			25438
#define kstidListsProperties			25439
#define kstidUndoChangeField			25440
#define kstidRedoChangeField			25441

// Defines for data entry
#define kstidContextCut					27300
#define kstidContextCopy				27301
#define kstidContextPaste				27302
#define kstidContextWritingSystem		27303
#define kstidContextInsert				27304
#define kstidContextHelp				27305
#define kstidContextShow				27306
#define kstidContextDelete				27307
#define kcidContextWritingSystem		27308
#define kcidContextInsert				27309
#define kcidContextHelp					27310
#define kcidContextShow					27311
#define kstidShowBody					27312
#define kstidContextPromote				27313
#define kcidContextPromote				27314

#define kstridRecLockDeleted			27315
#define kstridRecLockDeletedTitle		27316
#define kstridRecLockModified			27317
#define kstridRecLockModifiedTitle		27318

#define kstidRequiredMsg				27319
#define kstidEncouragedMsg				27320
#define kstidRequiredTitle				27321
#define kstidEncouragedTitle			27322
#define kstidOverflowText				27323	// Used in Views\Lib\VwOleDbDa.cpp.
#define kstidRequiredFldMsg				27324

#define kstidRequiredHiddenTitle		27325
#define kstidRequiredHiddenMsg			27326
#define kstidEncouragedHiddenTitle		27327
#define kstidEncouragedHiddenMsg		27328

// Viewbar and listbar
#define kstidListBarSelect				27350
#define kstidViewBarShellChooseList		27351
#define kstidOverlaysGenWhatsThisHelp	27352
#define kstidOverlaysItemWhatsThisHelp	27353
#define kstidOverlaysNoneWhatsThisHelp	27354
#define kstidListGenWhatsThisHelp		27355
#define kstidListItemWhatsThisHelp		27356

#define kstidEllipsisButtonWhatsThisHelp	27357
#define kstidEllipsisBtnDateWhatsThisHelp	27358
#define kstidEllipsisCrossRefWhatsThisHelp	27359
#define kstidDownarrowButtonWhatsThisHelp	27360

#define kcidViewViewsConfig             27361
#define kcidViewFltrsConfig             27362
#define kcidViewSortsConfig             27363
#define kstidViewsGenWhatsThisHelp      27364
#define kstidFiltersGenWhatsThisHelp    27365
#define kstidSortGenWhatsThisHelp       27366
#define kstidViewsItemWhatsThisHelp     27367
#define kstidFiltersNoneWhatsThisHelp   27368
#define kstidFiltersItemWhatsThisHelp	27369
#define kstidSortItemWhatsThisHelp		27370

#define kridDatePickDlg                 28000
#define kctidExpiration                 28001
#define kctidDpkPrecision               28002
#define kctidDpkMonth                   28003
#define kctidDpkDay                     28004
#define kctidDpkCalendar                28005
#define kctidDpkYearSpin                28006
#define kctidDpkYear                    28007
#define kridFilterDatePickDlg           28008
#define kcidViewFullWindow              28009
#define kctidDpkGroup					28010
#define kctidDpkNoCal					28011
#define kstidNoLogging                  28012
#define kridAccelBasic					28013
#define kstridBadDate					28014
#define kstridInvalidDate				28015

#define kstidSupportEmail               28050
#define kstidProgError					28051
#define kstidMailInstructions			28052


// Add new expandable menu items here. Make sure they are between the range given by
// kcidMenuItemExpMin and kcidMenuItemExpLim in AfCoreRes.h (28900-29100).
#define kcidMenuItemExpMin              28900
#define kcidExpShow                     28910
#define kcidExpViews                    28900


#define kcidFltrSpcExpand               29000
#define kcidAddExpViews                 29001
#define kcidExpToolbars                 29002
#define kcidExpFindFmtWs				29003
#define kcidExpFindFmtStyle				29004
#define kcidRestoreFocus				29005	// not used in resource, trick in AfFindDlg.

#define kstidCannotGetMasterDb			29006	// duplicated in MigrateData/resource.h
#define kcidMenuItemExpLim              29100


#define kcidMenuItemDynMin              29100
#define kcidMenuItemDynLim              31100

#define kstidNoFilter                   31244

#define kridHelpAboutDlg                31600
#define kridHelpAbout                   31601
#define kctidHelp                       31602
#define kctidVersion                    31603
#define kctidName                       31604
#define kctidMemory                     31605
#define kctidDiskSpace                  31606
#define kridTBarMove                    31607
#define kridTBarCopy                    31608
#define kridTBarNoDrop                  31609
#define kcidViewVbar                    31610
#define kcidVBarLargeIcons              31611
#define kcidVBarSmallIcons              31612
#define kcidHideVBar                    31613
#define kstidMore                       31614
#define kstidLess                       31615
#define kstidAppName                    31616
#define kstidBack                       31617
#define kstidForward                    31618
#define kstidReference                  31619
#define kstidInchesTxt                  31620
#define kstidInches                     31621
#define kstidMm                         31622
#define kstidCm                         31623
#define kstidPt                         31624
#define kstidMinimize                   31625
#define kstidMaximize                   31626
#define kstidClose                      31627
#define kstidVScroll                    31628
#define kstidHScroll                    31629
#define kstidSizeBorder                 31630
#define kstidTitleBar                   31631
#define kstidHelp                       31632
#define kstidIdle                       31633
#define kridStdBtns                     31634
#define kcidViewSbar                    31635
#define kstidInherit                    31636
#define kstidNoLabel                    31637
#define kstidLocationToolTip            31638
#define kstidNoStyle                    31639  // <no style> in Find/Replace dlg
#define kstidDefParaCharacters          31640
#define kridPrintCancelDlg              31641
#define kstidIn                         31642
#define kctidRegistrationNumber         31643
#define kstidUserWs                     31644
#define kstidNewItem                    31645
#define kstidNew                        31646
#define kstidShowHide                   31647
#define kstidStBarFiltered              31648
#define kstidStBarSorted                31649
#define kstidStBarHelpMsg               31650
#define kstidStBarDateFmt               31651
#define kstidStBarRecordFmt             31652
#define kstidStBarDefaultSort           31653
#define kstidDropDead                   31654
#define kstidCannotPrint                31655
#define kstidMsgCannotTile              31656
#define kstidStBar_LoadingProject       31657
#define kstidDbError                    31658
#define kstiddbeNoAppCompTbl            31659
#define kstiddbeNoCompAppVer            31660
#define kstiddbeNoDbVer                 31661
#define kstiddbeNoVerTbl                31662
#define kstidStBar_WhatsThisHelp        31663

#define kstidEditSrchQuick              31664
#define kcidViewStatBar                 31665
#define kstidCannotLaunchListEditor     31666
#define kstidCannotInsertListItem       31667
#define kstidCannotDeleteListItem       31668
#define kctidSuiteName                  31669

// These numbers must be in sequence.
// The first is the resource ID of the bitmap for the buttons.
// Following are the ids of the corresponding help strings.
// (At present the string IDs are not actually used, except in the resource file.)
#define kridFmtBorderBtns				31670
#define kstidFmtBdrAll					31671
#define kstidFmtBdrTop					31672
#define kstidFmtBdrBottom				31673
#define kstidFmtBdrLeft					31674
#define kstidFmtBdrRight				31675
#define kstidFmtBdrNone					31676

// Used primarily (only?) by WorldPad.
#define kstidUserWsXml                  31677

// Reserved for future, not yet used. For table versions of format
// border combo
#define kridFmtBdrTblBtns				31678
#define kstidFmtBdrTblOutside			31679
#define kstidFmtBdrTblTop				31680
#define kstidFmtBdrTblBottom			31681
#define kstidFmtBdrTblLeft				31682
#define kstidFmtBdrTblRight				31683
#define kstidFmtBdrTblAll				31684
#define kstidFmtBdrTblInside			31685
#define kstidFmtBdrTblRows				31686
#define kstidFmtBdrTblCols				31687
#define kstidFmtBdrTblNone				31688

#define kstidVwNormalWindow				31689
#define kstidVwFullWindow				31690

#define kstidLaunchRefChooser			31691
#define kstidLaunchRefHeader			31692
#define kstidDupItemName                31693
#define kstidDupItemAbbr                31694
#define kstidDupItemTitle               31695
#define kstidCmdLineUsage               31696
#define kstidCmdLineName                31697	// Provided by application subclasses.
#define kstidCmdLineOptions             31698	// Provided by application subclasses.
#define kstidDate						31699

// Splash screen image and common text ids.
#define kridSplashImage                 31900	// Same for all apps.
#define kridSplashLoadMessage           31901	// Same for all apps. (uses %s).
#define kstidSuiteName                  31902	// Same for all apps.
#define kstidCopyright                  31903	// Same for all apps.

#define kstidInvalidMergeMsg			31906
#define kstidInvalidMergeT				31907
#define kstidFixedStr					31908
#define kstidFixedStrTitle				31909
//#define kstiddbeOldDb					31910  // Not in use.
//#define kstiddbeOldDbTtl				31911  // Not in use.
#define kstiddbeOldApp					31912
#define kstiddbeOldAppTtl				31913
#define kstidStBar_LoadingData			31915
#define kstidTlsOptEDateCreated         31916
#define kstidTlsOptETitle               31917
#define kstidUnknownClass               31918

#define kstidCantDeleteItem				31920
#define kstidCantMergeItem				31921

#define kstidMmTxt						31930 // Text values for measurement combobox
#define kstidCmTxt						31931
#define kstidPtTxt						31932
