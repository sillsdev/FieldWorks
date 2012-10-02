/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: BackupRes.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Resource defines for backup feature
-------------------------------------------------------------------------------*//*:End Ignore*/

// Abstract message
#define kstidUseDefault					24000      //Method called must supply the default kstid

// Messages for the backup system
#define kstidBkpSystem					24500
#define kstidBkpBrowseInfo				24501
#define kstidBkpSchedUser				24502
#define kstidBkpProgressProj			24503
#define kstidBkpCreateDirectory			24504
#define kstidBkpInsertDiskNum			24505
#define kstidBkpDiskInvalid				24506
#define kstidBkpAbort					24507
#define kstidBkpQueryAbort	 			24508
#define kstidBkpAborting				24509
#define kstidBkpComplete				24510
#define kstidBkpRemind					24511
#define kstidBkpSchedWarnTime			24512
#define kstidRstNoFiles					24513
#define kstidRstInsertLastDisk			24514
#define kstidRstDiskInvalid				24515
#define kstidRstLastDiskInvalid			24516
#define	kstidRstProgressProj			24517
#define kstidRstAbort					24518
#define kstidRstQueryAbort				24519
#define kstidRstDbExists				24520
#define kstidRstDbExists2				24521
#define kstidRstDbOld					24522
#define kstidRstRenameExists			24523
#define kstidRstWrongPasswd				24524
#define kstidRstComplete				24525
#define kstidRstFileExists				24526
#define kstidRstBrowseInfo				24527
#define kstidNoPasswordInUse			24528
#define kstidPasswordInUse				24529


// Error messages for the backup system
#define kstidBkpPswdLenError			24552
#define kstidBkpPswdPuncError			24553
#define kstidBkpPswdMatchError			24554
#define kstidBkpTimeConvertError		24555
#define kstidBkpFailure					24556
#define kstidBkpPossibleFailure			24557
#define kstidBkpNonFatalFailure			24558
#define kstidBkpMutexError				24559
#define kstidBkpCreateDirError			24560
#define kstidBkpCreateDirError2			24561
#define kstidBkpMasterDbError			24562
#define kstidBkpDbSaveError				24563
#define kstidBkpDbXmlError				24564
#define kstidBkpNoZipError				24570
#define kstidBkpZipError				24571
#define kstidBkpSystemError				24572
#define kstidBkpDiskWriteError			24573
#define kstidRstFailure					24574
#define kstidRstPossibleFailure			24575
#define kstidRstNonFatalFailure			24576
#define kstidRstMutexError				24577
#define kstidRstUnzipError				24578
#define kstidRstFilesMissingError		24579
#define kstidRstBakMissingError			24580
#define kstidRstXmlMissingError			24581
#define kstidRstPreserveDbFileError		24582
#define kstidRstZipHeaderError			24584
#define kstidRstRenameEmptyError		24585
#define kstidRstPurgeConnectError		24586
#define kstidRstNoBlankMdfError			24587
#define kstidRstNewMdfExistsError		24588
#define kstidRstNoBlankLdfError			24589
#define kstidRstNewLdfExistsError		24590
#define kstidRstXmlDbCreateError		24591
#define kstidRstXmlError				24592
#define kstidRstDbRestoreError			24593
#define kstidRstBakFileListError		24594
#define kstidRstDbRecvrAttachError		24595
#define kstidRstDbRecvrError			24596
#define kstidRstRegistryError			24597
#define kstidRstFileNameError			24598
#define kstidRstZipError				24599
#define kstidRstNoDbName				24600
#define kstidRstNoProjectName			24601
#define kstidRstMsdeFailure				24602
#define kstidRstIncompatibleVersn		24603
#define kstidNoPrivilege				24604
#define kstidMustBeAdmin				24605
#define kstidCantDetachDb				24606
#define kstidCantWriteToRestore			24607
#define kstidCantLaunchScheduler		24608

// Backup Dialog (and tabs)
#define kridDlgBackupRestore			24700
#define kctidBackupTabs 				24701
#define kridBackupTab					24702
#define kridRestoreTab					24703
#define kctidBackupStartBackup			24704
#define kctidBackupStartRestore			24705
#define kctidBackupClose				kctidOk
#define kctidBackupHelp					kctidHelp

// Backup Tab:
#define kctidBackupProjects 			24720
#define kctidBackupDestination			24721
#define kctidBackupBrowseDestination	24722
#define kctidBackupIncludeXml			24723
#define kctidBackupReminders			24724
#define kctidBackupSchedule 			24725
#define kctidBackupPassword 			24726
#define kctidBackupPasswordWarning		24727

// Retore Tab:
#define kctidRestoreFrom				24740
#define kctidRestoreBrowseFrom			24741
#define kctidRestoreProject 			24742
#define kctidRestoreVersion				24743
#define kctidRestoreXml					24744

// Backup Reminder Dialog
#define kridBackupReminder				24780
#define kctidBkpRmndDays				24781
#define kctidBkpRmndDaysSpin			24782
#define kctidBkpRmndOn	 				24783
#define kctidBkpRmndWarn				24784

// Backup Password Dialog
#define kridBackupPassword				24800
#define kctidBkpPswdLock				24801
#define kctidBkpPswdPassword			24802
#define kctidBkpPswdConfirm 			24803
#define kctidBkpPswdMemJog				24804
#define kctidBkpPswdWarn				24805

// Backup/Restore in progress Dialog
#define kridBackupInProgress			24820
#define kridRestoreInProgress			24821
#define kctidBkpProgAction				24822
#define kridBkpProgIcon1				24823
#define kridBkpProgActivity1			24824
#define kridBkpProgIcon2				24825
#define kridBkpProgActivity2			24826
#define kridBkpProgIcon3				24827
#define kridBkpProgActivity3			24828
#define kctidBkpProgProgress			24829
#define kctidBkpProgAbort				24830
#define kctidBkpProgClose				24831
#define kridBkpProgIcon4				24832
#define kridBkpProgActivity4			24833
#define kridBkpProgIcon5				24834
#define kridBkpProgActivity5			24835

// Scheduled Backup Warning Dialog
#define kridScheduledBackupWarning		24840
#define kctidSchdBkpTime				24841
#define kctidSchdNow					24842
#define kctidSchdOptions				24843
#define kctidSchdCancel					24844
#define kctidSchdPasswordWarning		24845

// Reminder to backup dialog
#define kridBackupNag					24860
#define kctidBkpNagIcon					24861
#define kctidBkpNagText					24862
#define kctidBkpNagYes					24863
#define kctidBkpNagNo					24864
#define kctidBkpNagConfigure			24865
#define kctidBkpNagPasswordWarning		24866

// Database Connections Dialog
#define kridConnectionsDlg				24880
#define kctidDisconnectWarnIcon			24881
#define kctidDisconnectWarnText			24882
#define kctidConnectionList				24883
#define kctidDisconnectExplnText		24884
#define kctidDisconnectNotify			24885
#define kctidDisconnectForceText1		24886
#define kctidTimeLeft					24887
#define kctidDisconnectForceText2		24888
#define kctidForceNow					24889
#define kstidReasonDisconnectRestore	24890
#define kstidRemoteComputer				24891
#define kstidRemoteStatus				24892
#define kstidRemoteNotYetNotified		24893
#define kstidRemoteNotified				24894
#define kstidRemoteUnableNotify			24895
#define kstidRemoteReasonRestore		24896
#define kstidCancelDisconnectRestore	24897
#define kstidDisconnectYou				24898
#define kstidDisconnectWait				24899
#define kstidOwnConnections				24900
#define kstidPurgeConnections			24901
#define kstidPurgeConnsFail				24902

// Restore Password Dialog
#define kridRestorePasswordDlg			24920
#define kctidDatabase					24921
#define kctidBackupVersion				24922
#define kctidMemoryJog					24923
#define kctidPassword					24924
#define kctidPasswordNoJog				24925

// Dialog to warn user about overwriting an exisitng project, with option to rename
#define kridRestoreDbExists				24940
#define kridRestoreDbExistsIcon			24941
#define kctidRestoreDbExistsText		24942
#define kctidRestoreOverwriteRenameBtn	24943
#define kctidRestoreOverwriteName		24944
#define kctidRestoreOverwriteCheck		24945
#define kctidRestoreOverwriteReplaceBtn	24946

// Image Lists
#define kridBackupImagesLarge 			25000
#define kridBackupImagesSmall 			25001
#define	kridProjImagesSmall				25002
#define kridBackupIconCheck				25003
#define kridBackupIconArrow				25004

// Remote Warning Countdown dialog:
#define kridRmtWnCountdownDlg			25010
#define kridRmtWnIcon					25011
#define kridRmtWnMessage				25012
#define kctidRmtTimeLeft				25013
#define kstidRmtWnWarning				25014

// General messages:
#define kstidFwTitle					25020

#define kctidOk             IDOK
#define kctidCancel         IDCANCEL

// Image indices:
#define kridBkpStartBackup				   0
#define kridBkpStartRestore				   1
#define kridBkpClose					   2
