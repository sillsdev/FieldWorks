/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: resource.h
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	Resource defines for Data Migration feature
-------------------------------------------------------------------------------*//*:End Ignore*/

// String messages:
#define kstidMgdError					1001
#define kstidMgdUsage					1002
#define kstidMgdExtError				1003
#define kstidMgdExtError2				1004
#define kstidMgdInitError				1005
#define kstidMgdParamHighError			1006
#define kstidMgdQodcError				1007
#define kstidMgdVersionError			1008
#define kstidMgdBackupError				1009
#define kstidMgdTooNewError				1010
#define kstidMgdFileError				1011
#define kstidMgdFileOpenError			1012
#define kstidMgdSqlError				1013
#define kstidMgdRestoreError			1014
#define kstidMgdTagError				1015
#define kstidFixCmOverlayTagPhaseOne	1016
#define kstidFixCmOverlayTagPhaseTwo	1017
//#define kstidCannotGetMasterDb			1018	// duplicated in AfAppRes.h

#define kstidMigratingTitle             1019
#define kstidMigrateBackup              1020
#define kstidMigrateM5toV1              1021
#define kstidMigrateV1toV15             1022
#define kstidMigrateV15toV2             1023
#define kstidMigrateV2toV200006			1024
#define kstidMigrateInstLangs           1025
#define kstidMigrateIncremental			1026
#define kstidMgdIncrementalError		1027
#define kstidMgdMissingIncrementalFile	1028
#define kstidMgdErr201To204Gap			1029

#define kstidMgdLoadingLexDb			1030
#define kstidMgdLoadingPartsOfSpeech	1031
#define kstidMgdLoadingTranslationTags	1032
#define kstidMgdLoadingPhonologicalData	1033
#define kstidMgdLoadingAnnotationDefns	1034
#define kstidMgdLoadingSemanticDomains	1035
#define kstidMigrationSucceededFmt		1036
#define kstidMigrationFailedFmt			1037
#define kstidMgdFailed                  1038

#define kstidMigrateIncrementalXml		1039
