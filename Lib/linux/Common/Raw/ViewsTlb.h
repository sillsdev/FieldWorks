

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:02:27 2006
 */
/* Compiler settings for C:\fw\Output\Common\ViewsTlb.idl:
	Oicf, W1, Zp8, env=Win32 (32b run)
	protocol : dce , ms_ext, c_ext, robust
	error checks: allocation ref bounds_check enum stub_data
	VC __declspec() decoration level:
		 __declspec(uuid()), __declspec(selectany), __declspec(novtable)
		 DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __ViewsTlb_h__
#define __ViewsTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __IVwNotifyChange_FWD_DEFINED__
#define __IVwNotifyChange_FWD_DEFINED__
typedef interface IVwNotifyChange IVwNotifyChange;
#endif 	/* __IVwNotifyChange_FWD_DEFINED__ */


#ifndef __IVwSelection_FWD_DEFINED__
#define __IVwSelection_FWD_DEFINED__
typedef interface IVwSelection IVwSelection;
#endif 	/* __IVwSelection_FWD_DEFINED__ */


#ifndef __IVwEmbeddedWindow_FWD_DEFINED__
#define __IVwEmbeddedWindow_FWD_DEFINED__
typedef interface IVwEmbeddedWindow IVwEmbeddedWindow;
#endif 	/* __IVwEmbeddedWindow_FWD_DEFINED__ */


#ifndef __IVwStylesheet_FWD_DEFINED__
#define __IVwStylesheet_FWD_DEFINED__
typedef interface IVwStylesheet IVwStylesheet;
#endif 	/* __IVwStylesheet_FWD_DEFINED__ */


#ifndef __IVwEnv_FWD_DEFINED__
#define __IVwEnv_FWD_DEFINED__
typedef interface IVwEnv IVwEnv;
#endif 	/* __IVwEnv_FWD_DEFINED__ */


#ifndef __IVwViewConstructor_FWD_DEFINED__
#define __IVwViewConstructor_FWD_DEFINED__
typedef interface IVwViewConstructor IVwViewConstructor;
#endif 	/* __IVwViewConstructor_FWD_DEFINED__ */


#ifndef __IVwRootSite_FWD_DEFINED__
#define __IVwRootSite_FWD_DEFINED__
typedef interface IVwRootSite IVwRootSite;
#endif 	/* __IVwRootSite_FWD_DEFINED__ */


#ifndef __IDbColSpec_FWD_DEFINED__
#define __IDbColSpec_FWD_DEFINED__
typedef interface IDbColSpec IDbColSpec;
#endif 	/* __IDbColSpec_FWD_DEFINED__ */


#ifndef __ISilDataAccess_FWD_DEFINED__
#define __ISilDataAccess_FWD_DEFINED__
typedef interface ISilDataAccess ISilDataAccess;
#endif 	/* __ISilDataAccess_FWD_DEFINED__ */


#ifndef __IVwCacheDa_FWD_DEFINED__
#define __IVwCacheDa_FWD_DEFINED__
typedef interface IVwCacheDa IVwCacheDa;
#endif 	/* __IVwCacheDa_FWD_DEFINED__ */


#ifndef __IVwOleDbDa_FWD_DEFINED__
#define __IVwOleDbDa_FWD_DEFINED__
typedef interface IVwOleDbDa IVwOleDbDa;
#endif 	/* __IVwOleDbDa_FWD_DEFINED__ */


#ifndef __ISetupVwOleDbDa_FWD_DEFINED__
#define __ISetupVwOleDbDa_FWD_DEFINED__
typedef interface ISetupVwOleDbDa ISetupVwOleDbDa;
#endif 	/* __ISetupVwOleDbDa_FWD_DEFINED__ */


#ifndef __IVwRootBox_FWD_DEFINED__
#define __IVwRootBox_FWD_DEFINED__
typedef interface IVwRootBox IVwRootBox;
#endif 	/* __IVwRootBox_FWD_DEFINED__ */


#ifndef __IVwPropertyStore_FWD_DEFINED__
#define __IVwPropertyStore_FWD_DEFINED__
typedef interface IVwPropertyStore IVwPropertyStore;
#endif 	/* __IVwPropertyStore_FWD_DEFINED__ */


#ifndef __IVwOverlay_FWD_DEFINED__
#define __IVwOverlay_FWD_DEFINED__
typedef interface IVwOverlay IVwOverlay;
#endif 	/* __IVwOverlay_FWD_DEFINED__ */


#ifndef __IEventListener_FWD_DEFINED__
#define __IEventListener_FWD_DEFINED__
typedef interface IEventListener IEventListener;
#endif 	/* __IEventListener_FWD_DEFINED__ */


#ifndef __IVwPrintContext_FWD_DEFINED__
#define __IVwPrintContext_FWD_DEFINED__
typedef interface IVwPrintContext IVwPrintContext;
#endif 	/* __IVwPrintContext_FWD_DEFINED__ */


#ifndef __ISqlUndoAction_FWD_DEFINED__
#define __ISqlUndoAction_FWD_DEFINED__
typedef interface ISqlUndoAction ISqlUndoAction;
#endif 	/* __ISqlUndoAction_FWD_DEFINED__ */


#ifndef __IVwSearchKiller_FWD_DEFINED__
#define __IVwSearchKiller_FWD_DEFINED__
typedef interface IVwSearchKiller IVwSearchKiller;
#endif 	/* __IVwSearchKiller_FWD_DEFINED__ */


#ifndef __IVwSynchronizer_FWD_DEFINED__
#define __IVwSynchronizer_FWD_DEFINED__
typedef interface IVwSynchronizer IVwSynchronizer;
#endif 	/* __IVwSynchronizer_FWD_DEFINED__ */


#ifndef __IVwDataSpec_FWD_DEFINED__
#define __IVwDataSpec_FWD_DEFINED__
typedef interface IVwDataSpec IVwDataSpec;
#endif 	/* __IVwDataSpec_FWD_DEFINED__ */


#ifndef __IVwNotifyObjCharDeletion_FWD_DEFINED__
#define __IVwNotifyObjCharDeletion_FWD_DEFINED__
typedef interface IVwNotifyObjCharDeletion IVwNotifyObjCharDeletion;
#endif 	/* __IVwNotifyObjCharDeletion_FWD_DEFINED__ */


#ifndef __IVwVirtualHandler_FWD_DEFINED__
#define __IVwVirtualHandler_FWD_DEFINED__
typedef interface IVwVirtualHandler IVwVirtualHandler;
#endif 	/* __IVwVirtualHandler_FWD_DEFINED__ */


#ifndef __IVwLayoutStream_FWD_DEFINED__
#define __IVwLayoutStream_FWD_DEFINED__
typedef interface IVwLayoutStream IVwLayoutStream;
#endif 	/* __IVwLayoutStream_FWD_DEFINED__ */


#ifndef __IVwLayoutManager_FWD_DEFINED__
#define __IVwLayoutManager_FWD_DEFINED__
typedef interface IVwLayoutManager IVwLayoutManager;
#endif 	/* __IVwLayoutManager_FWD_DEFINED__ */


#ifndef __DbColSpec_FWD_DEFINED__
#define __DbColSpec_FWD_DEFINED__

#ifdef __cplusplus
typedef class DbColSpec DbColSpec;
#else
typedef struct DbColSpec DbColSpec;
#endif /* __cplusplus */

#endif 	/* __DbColSpec_FWD_DEFINED__ */


#ifndef __VwCacheDa_FWD_DEFINED__
#define __VwCacheDa_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwCacheDa VwCacheDa;
#else
typedef struct VwCacheDa VwCacheDa;
#endif /* __cplusplus */

#endif 	/* __VwCacheDa_FWD_DEFINED__ */


#ifndef __VwUndoDa_FWD_DEFINED__
#define __VwUndoDa_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwUndoDa VwUndoDa;
#else
typedef struct VwUndoDa VwUndoDa;
#endif /* __cplusplus */

#endif 	/* __VwUndoDa_FWD_DEFINED__ */


#ifndef __VwOleDbDa_FWD_DEFINED__
#define __VwOleDbDa_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwOleDbDa VwOleDbDa;
#else
typedef struct VwOleDbDa VwOleDbDa;
#endif /* __cplusplus */

#endif 	/* __VwOleDbDa_FWD_DEFINED__ */


#ifndef __VwRootBox_FWD_DEFINED__
#define __VwRootBox_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwRootBox VwRootBox;
#else
typedef struct VwRootBox VwRootBox;
#endif /* __cplusplus */

#endif 	/* __VwRootBox_FWD_DEFINED__ */


#ifndef __IVwObjDelNotification_FWD_DEFINED__
#define __IVwObjDelNotification_FWD_DEFINED__
typedef interface IVwObjDelNotification IVwObjDelNotification;
#endif 	/* __IVwObjDelNotification_FWD_DEFINED__ */


#ifndef __VwStylesheet_FWD_DEFINED__
#define __VwStylesheet_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwStylesheet VwStylesheet;
#else
typedef struct VwStylesheet VwStylesheet;
#endif /* __cplusplus */

#endif 	/* __VwStylesheet_FWD_DEFINED__ */


#ifndef __VwPropertyStore_FWD_DEFINED__
#define __VwPropertyStore_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwPropertyStore VwPropertyStore;
#else
typedef struct VwPropertyStore VwPropertyStore;
#endif /* __cplusplus */

#endif 	/* __VwPropertyStore_FWD_DEFINED__ */


#ifndef __VwOverlay_FWD_DEFINED__
#define __VwOverlay_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwOverlay VwOverlay;
#else
typedef struct VwOverlay VwOverlay;
#endif /* __cplusplus */

#endif 	/* __VwOverlay_FWD_DEFINED__ */


#ifndef __VwPrintContextWin32_FWD_DEFINED__
#define __VwPrintContextWin32_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwPrintContextWin32 VwPrintContextWin32;
#else
typedef struct VwPrintContextWin32 VwPrintContextWin32;
#endif /* __cplusplus */

#endif 	/* __VwPrintContextWin32_FWD_DEFINED__ */


#ifndef __SqlUndoAction_FWD_DEFINED__
#define __SqlUndoAction_FWD_DEFINED__

#ifdef __cplusplus
typedef class SqlUndoAction SqlUndoAction;
#else
typedef struct SqlUndoAction SqlUndoAction;
#endif /* __cplusplus */

#endif 	/* __SqlUndoAction_FWD_DEFINED__ */


#ifndef __IVwPattern_FWD_DEFINED__
#define __IVwPattern_FWD_DEFINED__
typedef interface IVwPattern IVwPattern;
#endif 	/* __IVwPattern_FWD_DEFINED__ */


#ifndef __VwPattern_FWD_DEFINED__
#define __VwPattern_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwPattern VwPattern;
#else
typedef struct VwPattern VwPattern;
#endif /* __cplusplus */

#endif 	/* __VwPattern_FWD_DEFINED__ */


#ifndef __VwSearchKiller_FWD_DEFINED__
#define __VwSearchKiller_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwSearchKiller VwSearchKiller;
#else
typedef struct VwSearchKiller VwSearchKiller;
#endif /* __cplusplus */

#endif 	/* __VwSearchKiller_FWD_DEFINED__ */


#ifndef __IVwDrawRootBuffered_FWD_DEFINED__
#define __IVwDrawRootBuffered_FWD_DEFINED__
typedef interface IVwDrawRootBuffered IVwDrawRootBuffered;
#endif 	/* __IVwDrawRootBuffered_FWD_DEFINED__ */


#ifndef __VwDrawRootBuffered_FWD_DEFINED__
#define __VwDrawRootBuffered_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwDrawRootBuffered VwDrawRootBuffered;
#else
typedef struct VwDrawRootBuffered VwDrawRootBuffered;
#endif /* __cplusplus */

#endif 	/* __VwDrawRootBuffered_FWD_DEFINED__ */


#ifndef __VwSynchronizer_FWD_DEFINED__
#define __VwSynchronizer_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwSynchronizer VwSynchronizer;
#else
typedef struct VwSynchronizer VwSynchronizer;
#endif /* __cplusplus */

#endif 	/* __VwSynchronizer_FWD_DEFINED__ */


#ifndef __VwDataSpec_FWD_DEFINED__
#define __VwDataSpec_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwDataSpec VwDataSpec;
#else
typedef struct VwDataSpec VwDataSpec;
#endif /* __cplusplus */

#endif 	/* __VwDataSpec_FWD_DEFINED__ */


#ifndef __VwLayoutStream_FWD_DEFINED__
#define __VwLayoutStream_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwLayoutStream VwLayoutStream;
#else
typedef struct VwLayoutStream VwLayoutStream;
#endif /* __cplusplus */

#endif 	/* __VwLayoutStream_FWD_DEFINED__ */


#ifndef __IOpenFWProjectDlg_FWD_DEFINED__
#define __IOpenFWProjectDlg_FWD_DEFINED__
typedef interface IOpenFWProjectDlg IOpenFWProjectDlg;
#endif 	/* __IOpenFWProjectDlg_FWD_DEFINED__ */


#ifndef __OpenFWProjectDlg_FWD_DEFINED__
#define __OpenFWProjectDlg_FWD_DEFINED__

#ifdef __cplusplus
typedef class OpenFWProjectDlg OpenFWProjectDlg;
#else
typedef struct OpenFWProjectDlg OpenFWProjectDlg;
#endif /* __cplusplus */

#endif 	/* __OpenFWProjectDlg_FWD_DEFINED__ */


#ifndef __IFwExportDlg_FWD_DEFINED__
#define __IFwExportDlg_FWD_DEFINED__
typedef interface IFwExportDlg IFwExportDlg;
#endif 	/* __IFwExportDlg_FWD_DEFINED__ */


#ifndef __FwExportDlg_FWD_DEFINED__
#define __FwExportDlg_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwExportDlg FwExportDlg;
#else
typedef struct FwExportDlg FwExportDlg;
#endif /* __cplusplus */

#endif 	/* __FwExportDlg_FWD_DEFINED__ */


#ifndef __IFwStylesDlg_FWD_DEFINED__
#define __IFwStylesDlg_FWD_DEFINED__
typedef interface IFwStylesDlg IFwStylesDlg;
#endif 	/* __IFwStylesDlg_FWD_DEFINED__ */


#ifndef __FwStylesDlg_FWD_DEFINED__
#define __FwStylesDlg_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwStylesDlg FwStylesDlg;
#else
typedef struct FwStylesDlg FwStylesDlg;
#endif /* __cplusplus */

#endif 	/* __FwStylesDlg_FWD_DEFINED__ */


#ifndef __IFwDbMergeStyles_FWD_DEFINED__
#define __IFwDbMergeStyles_FWD_DEFINED__
typedef interface IFwDbMergeStyles IFwDbMergeStyles;
#endif 	/* __IFwDbMergeStyles_FWD_DEFINED__ */


#ifndef __FwDbMergeStyles_FWD_DEFINED__
#define __FwDbMergeStyles_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwDbMergeStyles FwDbMergeStyles;
#else
typedef struct FwDbMergeStyles FwDbMergeStyles;
#endif /* __cplusplus */

#endif 	/* __FwDbMergeStyles_FWD_DEFINED__ */


#ifndef __IFwDbMergeWrtSys_FWD_DEFINED__
#define __IFwDbMergeWrtSys_FWD_DEFINED__
typedef interface IFwDbMergeWrtSys IFwDbMergeWrtSys;
#endif 	/* __IFwDbMergeWrtSys_FWD_DEFINED__ */


#ifndef __FwDbMergeWrtSys_FWD_DEFINED__
#define __FwDbMergeWrtSys_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwDbMergeWrtSys FwDbMergeWrtSys;
#else
typedef struct FwDbMergeWrtSys FwDbMergeWrtSys;
#endif /* __cplusplus */

#endif 	/* __FwDbMergeWrtSys_FWD_DEFINED__ */


#ifndef __IFwCheckAnthroList_FWD_DEFINED__
#define __IFwCheckAnthroList_FWD_DEFINED__
typedef interface IFwCheckAnthroList IFwCheckAnthroList;
#endif 	/* __IFwCheckAnthroList_FWD_DEFINED__ */


#ifndef __FwCheckAnthroList_FWD_DEFINED__
#define __FwCheckAnthroList_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwCheckAnthroList FwCheckAnthroList;
#else
typedef struct FwCheckAnthroList FwCheckAnthroList;
#endif /* __cplusplus */

#endif 	/* __FwCheckAnthroList_FWD_DEFINED__ */


#ifndef __IMigrateData_FWD_DEFINED__
#define __IMigrateData_FWD_DEFINED__
typedef interface IMigrateData IMigrateData;
#endif 	/* __IMigrateData_FWD_DEFINED__ */


#ifndef __MigrateData_FWD_DEFINED__
#define __MigrateData_FWD_DEFINED__

#ifdef __cplusplus
typedef class MigrateData MigrateData;
#else
typedef struct MigrateData MigrateData;
#endif /* __cplusplus */

#endif 	/* __MigrateData_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_ViewsTlb_0000 */
/* [local] */


#undef ATTACH_GUID_TO_CLASS
#if defined(__cplusplus)
#define ATTACH_GUID_TO_CLASS(type, guid, cls) \
	type __declspec(uuid(#guid)) cls;
#else // !defined(__cplusplus)
#define ATTACH_GUID_TO_CLASS(type, guid, cls)
#endif // !defined(__cplusplus)

#ifndef DEFINE_COM_PTR
#define DEFINE_COM_PTR(cls)
#endif

#undef GENERIC_DECLARE_SMART_INTERFACE_PTR
#define GENERIC_DECLARE_SMART_INTERFACE_PTR(cls, iid) \
	ATTACH_GUID_TO_CLASS(interface, iid, cls); \
	DEFINE_COM_PTR(cls);


#ifndef CUSTOM_COM_BOOL
typedef VARIANT_BOOL ComBool;

#endif

#if 0
// This is so there is an equivalent VB type.
typedef CY SilTime;

#elif defined(SILTIME_IS_STRUCT)
// This is for code that compiles UtilTime.*.
struct SilTime;
#else
// This is for code that uses a 64-bit integer for SilTime.
typedef __int64 SilTime;
#endif

ATTACH_GUID_TO_CLASS(class,
f6d10640-c00c-11d2-8078-0000c0fb81b5
,
Views
);


extern RPC_IF_HANDLE __MIDL_itf_ViewsTlb_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_ViewsTlb_0000_v0_0_s_ifspec;


#ifndef __Views_LIBRARY_DEFINED__
#define __Views_LIBRARY_DEFINED__

/* library Views */
/* [helpstring][version][uuid] */





























GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwNotifyChange
,
6C456541-C2B6-11d3-8078-0000C0FB81B5
);
typedef /* [v1_enum] */
enum VwSpecialChars
	{	kscBackspace	= 8,
	kscDelForward	= 0x7f
	} 	VwSpecialChars;

typedef /* [v1_enum] */
enum VwSelType
	{	kstText	= 1,
	kstPicture	= 2
	} 	VwSelType;

typedef /* [v1_enum] */
enum VwSelChangeType
	{	ksctSamePara	= 1,
	ksctDiffPara	= 2,
	ksctUnknown	= 3,
	ksctDeleted	= 4
	} 	VwSelChangeType;

typedef /* [v1_enum] */
enum PropChangeType
	{	kpctNotifyMeThenAll	= 0,
	kpctNotifyAll	= kpctNotifyMeThenAll + 1,
	kpctNotifyAllButMe	= kpctNotifyAll + 1
	} 	PropChangeType;

typedef /* [v1_enum] */
enum VwDelProbType
	{	kdptNone	= 0,
	kdptComplexRange	= kdptNone + 1,
	kdptBsAtStartPara	= kdptComplexRange + 1,
	kdptDelAtEndPara	= kdptBsAtStartPara + 1,
	kdptBsReadOnly	= kdptDelAtEndPara + 1,
	kdptDelReadOnly	= kdptBsReadOnly + 1,
	kdptReadOnly	= kdptDelReadOnly + 1
	} 	VwDelProbType;

typedef /* [v1_enum] */
enum VwDelProbResponse
	{	kdprAbort	= 0,
	kdprFail	= kdprAbort + 1,
	kdprDone	= kdprFail + 1,
	kdprRetry	= kdprDone + 1
	} 	VwDelProbResponse;

typedef /* [v1_enum] */
enum VwInsertDiffParaResponse
	{	kidprDefault	= 0,
	kidprFail	= kidprDefault + 1,
	kidprDone	= kidprFail + 1
	} 	VwInsertDiffParaResponse;

typedef /* [v1_enum] */
enum DbColType
	{	koctGuid	= 0,
	koctInt	= 1,
	koctString	= 2,
	koctFmt	= 3,
	koctMlaAlt	= 4,
	koctMlsAlt	= 5,
	koctMltAlt	= 6,
	koctObj	= 7,
	koctObjVec	= 8,
	koctBaseId	= 9,
	koctTtp	= 10,
	koctUnicode	= 11,
	koctInt64	= 12,
	koctTime	= 13,
	koctEnc	= 14,
	koctFlid	= 15,
	koctTimeStamp	= 16,
	koctObjOwn	= 17,
	koctObjVecOwn	= 18,
	koctBinary	= 19,
	koctLim	= 20,
	koctObjVecExtra	= 21
	} 	DbColType;

typedef /* [v1_enum] */
enum FldType
	{	kftString	= 0,
	kftMsa	= kftString + 1,
	kftMta	= kftMsa + 1,
	kftRefAtomic	= kftMta + 1,
	kftRefCombo	= kftRefAtomic + 1,
	kftRefSeq	= kftRefCombo + 1,
	kftEnum	= kftRefSeq + 1,
	kftUnicode	= kftEnum + 1,
	kftTtp	= kftUnicode + 1,
	kftStText	= kftTtp + 1,
	kftDummy	= kftStText + 1,
	kftLimEmbedLabel	= kftDummy + 1,
	kftGroup	= kftLimEmbedLabel + 1,
	kftGroupOnePerLine	= kftGroup + 1,
	kftTitleGroup	= kftGroupOnePerLine + 1,
	kftDateRO	= kftTitleGroup + 1,
	kftDate	= kftDateRO + 1,
	kftGenDate	= kftDate + 1,
	kftSubItems	= kftGenDate + 1,
	kftObjRefAtomic	= kftSubItems + 1,
	kftObjRefSeq	= kftObjRefAtomic + 1,
	kftInteger	= kftObjRefSeq + 1,
	kftBackRefAtomic	= kftInteger + 1,
	kftExpandable	= kftBackRefAtomic + 1,
	kftObjOwnSeq	= kftExpandable + 1,
	kftObjOwnCol	= kftObjOwnSeq + 1,
	kftGuid	= kftObjOwnCol + 1,
	kftStTextParas	= kftGuid + 1,
	kftLim	= kftStTextParas + 1
	} 	FldType;

typedef /* [v1_enum] */
enum VwBoxType
	{	kvbtUnknown	= 0,
	kvbtGroup	= kvbtUnknown + 1,
	kvbtParagraph	= kvbtGroup + 1,
	kvbtConcPara	= kvbtParagraph + 1,
	kvbtPile	= kvbtConcPara + 1,
	kvbtInnerPile	= kvbtPile + 1,
	kvbtMoveablePile	= kvbtInnerPile + 1,
	kvbtDiv	= kvbtMoveablePile + 1,
	kvbtRoot	= kvbtDiv + 1,
	kvbtTable	= kvbtRoot + 1,
	kvbtTableRow	= kvbtTable + 1,
	kvbtTableCell	= kvbtTableRow + 1,
	kvbtLeaf	= kvbtTableCell + 1,
	kvbtString	= kvbtLeaf + 1,
	kvbtDropCapString	= kvbtString + 1,
	kvbtAnchor	= kvbtDropCapString + 1,
	kvbtSeparator	= kvbtAnchor + 1,
	kvbtBar	= kvbtSeparator + 1,
	kvbtPicture	= kvbtBar + 1,
	kvbtIndepPicture	= kvbtPicture + 1,
	kvbtIntegerPicture	= kvbtIndepPicture + 1,
	kvbtLazy	= kvbtIntegerPicture + 1
	} 	VwBoxType;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDbColSpec
,
A25318C8-EB1F-4f38-8E8D-80BF2849001B
);
ATTACH_GUID_TO_CLASS(class,
26F0F36D-C905-4d1e-B1A9-AB3EA8C4D340
,
DbColSpec
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ISilDataAccess
,
88C81964-DB97-4cdc-A942-730CF1DF73A4
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwCacheDa
,
146AA200-7061-4f79-A8D8-7CBBA1B5CADA
);
ATTACH_GUID_TO_CLASS(class,
FFF54604-C92B-4745-B74A-703CFBB81BB0
,
VwCacheDa
);
ATTACH_GUID_TO_CLASS(class,
2ABC0E1E-DCDB-4312-8B7E-7F644240E37C
,
VwUndoDa
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwOleDbDa
,
AAAA731D-E34E-4742-948F-C88BBD0AE136
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ISetupVwOleDbDa
,
8645fA4F-EE90-11D2-A9B8-0080C87B6086
);
ATTACH_GUID_TO_CLASS(class,
8645fa50-ee90-11d2-a9b8-0080c87b6086
,
VwOleDbDa
);
typedef /* [v1_enum] */
enum VwShiftStatus
	{	kfssNone	= 0,
	kfssShift	= 1,
	kfssControl	= 2,
	kgrfssShiftControl	= kfssShift | kfssControl
	} 	VwShiftStatus;

typedef struct VwSelLevInfo
	{
	int tag;
	int cpropPrevious;
	int ihvo;
	int hvo;
	int ws;
	int ich;
	} 	VwSelLevInfo;

typedef struct VwChangeInfo
	{
	long hvo;
	int tag;
	int ivIns;
	int cvIns;
	int cvDel;
	} 	VwChangeInfo;

typedef /* [v1_enum] */
enum VwUnit
	{	kunPoint1000	= 0,
	kunPercent100	= 1,
	kunRelative	= 2
	} 	VwUnit;

typedef /* [public][public][public][public][public] */ struct __MIDL___MIDL_itf_ViewsTlb_0263_0001
	{
	int nVal;
	VwUnit unit;
	} 	VwLength;

typedef /* [v1_enum] */
enum VwAlignment
	{	kvaLeft	= 0,
	kvaCenter	= kvaLeft + 1,
	kvaRight	= kvaCenter + 1,
	kvaJustified	= kvaRight + 1
	} 	VwAlignment;

typedef /* [v1_enum] */
enum VwFramePosition
	{	kvfpVoid	= 0,
	kvfpAbove	= 0x1,
	kvfpBelow	= 0x2,
	kvfpLhs	= 0x4,
	kvfpRhs	= 0x8,
	kvfpHsides	= kvfpAbove | kvfpBelow,
	kvfpVsides	= kvfpLhs | kvfpRhs,
	kvfpBox	= kvfpHsides | kvfpVsides
	} 	VwFramePosition;

typedef /* [v1_enum] */
enum VwRule
	{	kvrlNone	= 0,
	kvrlGroups	= 0x1,
	kvrlRowNoGroups	= 0x2,
	kvrlRows	= kvrlGroups | kvrlRowNoGroups,
	kvrlColsNoGroups	= 0x4,
	kvrlCols	= kvrlGroups | kvrlColsNoGroups,
	kvrlAll	= kvrlRows | kvrlCols
	} 	VwRule;

typedef /* [v1_enum] */
enum VwBulNum
	{	kvbnNone	= 0,
	kvbnNumberBase	= 10,
	kvbnArabic	= kvbnNumberBase,
	kvbnRomanUpper	= kvbnArabic + 1,
	kvbnRomanLower	= kvbnRomanUpper + 1,
	kvbnLetterUpper	= kvbnRomanLower + 1,
	kvbnLetterLower	= kvbnLetterUpper + 1,
	kvbnArabic01	= kvbnLetterLower + 1,
	kvbnNumberMax	= kvbnArabic01 + 1,
	kvbnBulletBase	= 100,
	kvbnBullet	= kvbnBulletBase,
	kvbnBulletMax	= kvbnBulletBase + 100
	} 	VwBulNum;

typedef /* [v1_enum] */
enum VwStyleProperty
	{	kspNamedStyle	= 133,
	kspMarginLeading	= 19,
	kspMarginTrailing	= 20,
	kspMarginTop	= 21,
	kspMarginBottom	= 22,
	kspMaxLines	= 151,
	kspWsStyle	= 156,
	kspRelLineHeight	= 160
	} 	VwStyleProperty;

typedef /* [v1_enum] */
enum VwFontAbsoluteSize
	{	kvfsXXSmall	= 0,
	kvfsXSmall	= kvfsXXSmall + 1,
	kvfsSmall	= kvfsXSmall + 1,
	kvfsNormal	= kvfsSmall + 1,
	kvfsLarge	= kvfsNormal + 1,
	kvfsXLarge	= kvfsLarge + 1,
	kvfsXXLarge	= kvfsXLarge + 1,
	kvfsSmaller	= kvfsXXLarge + 1,
	kvfsLarger	= kvfsSmaller + 1
	} 	VwFontAbsoluteSize;

typedef /* [v1_enum] */
enum VwFontWeight
	{	kvfw100	= 100,
	kvfw200	= 200,
	kvfw300	= 300,
	kvfw400	= 400,
	kvfw500	= 500,
	kvfw600	= 600,
	kvfw700	= 700,
	kvfw800	= 800,
	kvfw900	= 900,
	kvfwNormal	= 400,
	kvfwBold	= 700,
	kvfwBolder	= -1,
	kvfwLighter	= -2
	} 	VwFontWeight;

typedef /* [v1_enum] */
enum VwSpecialAttrTags
	{	ktagNotAnAttr	= -1,
	ktagGapInAttrs	= -2
	} 	VwSpecialAttrTags;

typedef /* [v1_enum] */
enum VwSelectionState
	{	vssDisabled	= 0,
	vssOutOfFocus	= vssDisabled + 1,
	vssEnabled	= vssOutOfFocus + 1,
	vssLim	= vssEnabled + 1
	} 	VwSelectionState;

typedef /* [v1_enum] */
enum VwPrepDrawResult
	{	kxpdrNormal	= 0,
	kxpdrAdjust	= kxpdrNormal + 1,
	kxpdrInvalidate	= kxpdrAdjust + 1,
	kxpdrLim	= kxpdrInvalidate + 1
	} 	VwPrepDrawResult;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwRootBox
,
24717CB1-0C4D-485e-BA7F-7B28DE861A3F
);
ATTACH_GUID_TO_CLASS(class,
D1074356-4F41-4e3e-A1ED-9C044FD0C096
,
VwRootBox
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwViewConstructor
,
EE103481-48BB-11d3-8078-0000C0FB81B5
);
typedef /* [v1_enum] */
enum VwScrollSelOpts
	{	kssoDefault	= 1,
	kssoNearTop	= 2
	} 	VwScrollSelOpts;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwRootSite
,
C999413C-28C8-481c-9543-B06C92B812D1
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwObjDelNotification
,
913B1BED-6199-4b6e-A63F-57B225B44997
);
typedef /* [v1_enum] */
enum VwConcParaOpts
	{	kcpoBold	= 1,
	kcpoAlign	= 2,
	kcpoDefault	= 3
	} 	VwConcParaOpts;

typedef struct DispPropOverride
	{
	/* external definition not present */ LgCharRenderProps chrp;
	int ichMin;
	int ichLim;
	} 	DispPropOverride;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwEnv
,
B5A11CC3-B1D4-4ae4-A1E4-02A6A8198CEB
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwEmbeddedWindow
,
f6d10646-c00c-11d2-8078-0000c0fb81b5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwSelection
,
4F8B678D-C5BA-4a2f-B9B3-2780956E3616
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IEventListener
,
F696B01E-974B-4065-B464-BDF459154054
);
typedef /* [v1_enum] */
enum StyleType
	{	kstParagraph	= 0,
	kstCharacter	= kstParagraph + 1,
	kstLim	= kstCharacter + 1
	} 	StyleType;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwStylesheet
,
D77C0DBC-C7BC-441d-9587-1E3664E1BCD3
);
ATTACH_GUID_TO_CLASS(class,
CCE2A7ED-464C-4ec7-A0B0-E3C1F6B94C5A
,
VwStylesheet
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwPropertyStore
,
3D4847FE-EA2D-4255-A496-770059A134CC
);
ATTACH_GUID_TO_CLASS(class,
CB59916A-C532-4a57-8CB4-6E1508B4DEC1
,
VwPropertyStore
);
typedef /* [v1_enum] */
enum VwOverlayFlags
	{	kfofTagsUseAttribs	= 1,
	kfofLeadBracket	= 2,
	kfofLeadTag	= 4,
	kfofTrailBracket	= 8,
	kfofTrailTag	= 16,
	kgrfofTagAbove	= 6,
	kgrfofTagBelow	= 24,
	kgrfofTagAnywhere	= 30,
	kgrfofBracketAnywhere	= 10,
	kgrfofDefault	= 31
	} 	VwOverlayFlags;

typedef /* [v1_enum] */
enum VwConst1
	{	kcchGuidRepLength	= 8
	} 	VwConst1;

typedef /* [v1_enum] */
enum FwOverlaySetMask
	{	kosmAbbr	= 0x1,
	kosmName	= 0x2,
	kosmClrFore	= 0x4,
	kosmClrBack	= 0x8,
	kosmClrUnder	= 0x10,
	kosmUnderType	= 0x20,
	kosmHidden	= 0x40,
	kosmAll	= 0x7f
	} 	FwOverlaySetMask;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwOverlay
,
7D9089C1-3BB9-11d4-8078-0000C0FB81B5
);
ATTACH_GUID_TO_CLASS(class,
73F5DB01-3D2A-11d4-8078-0000C0FB81B5
,
VwOverlay
);
typedef /* [v1_enum] */
enum VwHeaderPositions
	{	kvhpLeft	= 1,
	kvhpRight	= 2,
	kvhpOutside	= 4,
	kvhpInside	= 8,
	kvhpCenter	= 16,
	kvhpOdd	= 32,
	kvhpEven	= 64,
	kvhpTop	= 128,
	kvhpBottom	= 256,
	kvhpFirst	= 512,
	kgrfvhpNormal	= 915
	} 	VwHeaderPositions;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwPrintContext
,
FF2E1DC2-95A8-41c6-85F4-FFCA3A64216A
);
ATTACH_GUID_TO_CLASS(class,
5E9FB977-66AE-4c16-A036-1D40E7713573
,
VwPrintContextWin32
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ISqlUndoAction
,
2225FCC7-51AE-4461-930C-A42A8DC5A81A
);
ATTACH_GUID_TO_CLASS(class,
77272239-3228-4b02-9B6A-1DC5539F8153
,
SqlUndoAction
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwPattern
,
FACD01D9-BAF4-4ef0-BED6-A8966160C94D
);
ATTACH_GUID_TO_CLASS(class,
6C659C76-3991-48dd-93F7-DA65847D4863
,
VwPattern
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwSearchKiller
,
FF1B39DE-20D3-4cdd-A134-DCBE3BE23F3E
);
ATTACH_GUID_TO_CLASS(class,
4ADA9157-67F8-499b-88CE-D63DF918DF83
,
VwSearchKiller
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwDrawRootBuffered
,
09752C4C-CC1E-4268-891E-526BBBAC0DE8
);
ATTACH_GUID_TO_CLASS(class,
97199458-10C7-49da-B3AE-EA922EA64859
,
VwDrawRootBuffered
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwSynchronizer
,
C5C1E9DC-5880-4ee3-B3CD-EBDD132A6294
);
ATTACH_GUID_TO_CLASS(class,
5E149A49-CAEE-4823-97F7-BB9DED2A62BC
,
VwSynchronizer
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwDataSpec
,
DC9A7C08-138E-41C0-8532-5FD64B5E72BF
);
ATTACH_GUID_TO_CLASS(class,
6DE189F0-6F15-4242-943D-054AAEA92ACB
,
VwDataSpec
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwNotifyObjCharDeletion
,
CF1E5D07-B479-4195-B64C-02931F86014D
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwLayoutStream
,
963E6A91-513F-4490-A282-0E99B542B4CC
);
ATTACH_GUID_TO_CLASS(class,
1CD09E06-6978-4969-A1FC-462723587C32
,
VwLayoutStream
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwLayoutManager
,
13F3A421-4915-455b-B57F-AFD4073CFFA0
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwVirtualHandler
,
F8851137-6562-4120-A34E-1A51EE598EA7
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IOpenFWProjectDlg
,
8cb6f2f9-3b0a-4030-8992-c50fb78e77f3
);
ATTACH_GUID_TO_CLASS(class,
D7C505D0-F132-4e40-BFE7-A2E66A46991A
,
OpenFWProjectDlg
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwExportDlg
,
67A68372-5727-4bd4-94A7-C2D703A75C36
);
ATTACH_GUID_TO_CLASS(class,
86DD56A8-CDD0-49d2-BD57-C78F8367D6C4
,
FwExportDlg
);
typedef /* [v1_enum] */
enum StylesDlgType
	{	ksdtStandard	= 0,
	ksdtTransEditor	= ksdtStandard + 1
	} 	StylesDlgType;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwStylesDlg
,
0D598D88-C17D-4E46-AC89-51FFC5DA0799
);
ATTACH_GUID_TO_CLASS(class,
158F638D-D344-47FC-AB39-4C1A742FD06B
,
FwStylesDlg
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwDbMergeStyles
,
A7CD703C-6199-4097-A5C0-AB78DD23120E
);
ATTACH_GUID_TO_CLASS(class,
217874B4-90FE-469d-BF80-3D2306F3BB06
,
FwDbMergeStyles
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwDbMergeWrtSys
,
DE96B989-91A5-4104-9764-69ABE0BF0B9A
);
ATTACH_GUID_TO_CLASS(class,
40E4B757-4B7F-4B7C-A498-3EB942E7C6D6
,
FwDbMergeWrtSys
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwCheckAnthroList
,
8AC06CED-7B73-4E34-81A3-852A43E28BD8
);
ATTACH_GUID_TO_CLASS(class,
4D84B554-D3C8-4E0F-9416-4B26A4F0324B
,
FwCheckAnthroList
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IMigrateData
,
7BF2BC32-A603-4ba1-AC1F-B59D9F5FED8B
);
ATTACH_GUID_TO_CLASS(class,
461989B4-CA92-4eab-8CAD-ADB28C3B4D10
,
MigrateData
);

EXTERN_C const IID LIBID_Views;

#ifndef __IVwNotifyChange_INTERFACE_DEFINED__
#define __IVwNotifyChange_INTERFACE_DEFINED__

/* interface IVwNotifyChange */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwNotifyChange;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("6C456541-C2B6-11d3-8078-0000C0FB81B5")
	IVwNotifyChange : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE PropChanged(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ivMin,
			/* [in] */ int cvIns,
			/* [in] */ int cvDel) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwNotifyChangeVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwNotifyChange * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwNotifyChange * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwNotifyChange * This);

		HRESULT ( STDMETHODCALLTYPE *PropChanged )(
			IVwNotifyChange * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ivMin,
			/* [in] */ int cvIns,
			/* [in] */ int cvDel);

		END_INTERFACE
	} IVwNotifyChangeVtbl;

	interface IVwNotifyChange
	{
		CONST_VTBL struct IVwNotifyChangeVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwNotifyChange_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwNotifyChange_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwNotifyChange_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwNotifyChange_PropChanged(This,hvo,tag,ivMin,cvIns,cvDel)	\
	(This)->lpVtbl -> PropChanged(This,hvo,tag,ivMin,cvIns,cvDel)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwNotifyChange_PropChanged_Proxy(
	IVwNotifyChange * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int ivMin,
	/* [in] */ int cvIns,
	/* [in] */ int cvDel);


void __RPC_STUB IVwNotifyChange_PropChanged_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwNotifyChange_INTERFACE_DEFINED__ */


#ifndef __IVwSelection_INTERFACE_DEFINED__
#define __IVwSelection_INTERFACE_DEFINED__

/* interface IVwSelection */
/* [object][unique][oleautomation][dual][uuid] */


EXTERN_C const IID IID_IVwSelection;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("4F8B678D-C5BA-4a2f-B9B3-2780956E3616")
	IVwSelection : public IDispatch
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsRange(
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetSelectionProps(
			/* [in] */ int cttpMax,
			/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttp,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvps,
			/* [out] */ int *pcttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetHardAndSoftCharProps(
			/* [in] */ int cttpMax,
			/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpSel,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
			/* [out] */ int *pcttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetParaProps(
			/* [in] */ int cttpMax,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvps,
			/* [out] */ int *pcttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetHardAndSoftParaProps(
			/* [in] */ int cttpMax,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpPara,
			/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpHard,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
			/* [out] */ int *pcttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetSelectionProps(
			/* [in] */ int cttp,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE TextSelInfo(
			/* [in] */ ComBool fEndPoint,
			/* [out] */ /* external definition not present */ ITsString **pptss,
			/* [out] */ int *pich,
			/* [out] */ ComBool *pfAssocPrev,
			/* [out] */ long *phvoObj,
			/* [out] */ int *ptag,
			/* [out] */ int *pws) = 0;

		virtual HRESULT STDMETHODCALLTYPE CLevels(
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ int *pclev) = 0;

		virtual HRESULT STDMETHODCALLTYPE PropInfo(
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int ilev,
			/* [out] */ long *phvoObj,
			/* [out] */ int *ptag,
			/* [out] */ int *pihvo,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ IVwPropertyStore **ppvps) = 0;

		virtual HRESULT STDMETHODCALLTYPE AllTextSelInfo(
			/* [out] */ int *pihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][out] */ VwSelLevInfo *prgvsli,
			/* [out] */ int *ptagTextProp,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ int *pichAnchor,
			/* [out] */ int *pichEnd,
			/* [out] */ int *pws,
			/* [out] */ ComBool *pfAssocPrev,
			/* [out] */ int *pihvoEnd,
			/* [out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE AllSelEndInfo(
			/* [in] */ ComBool fEndPoint,
			/* [out] */ int *pihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][out] */ VwSelLevInfo *prgvsli,
			/* [out] */ int *ptagTextProp,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ int *pich,
			/* [out] */ int *pws,
			/* [out] */ ComBool *pfAssocPrev,
			/* [out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE Commit(
			/* [retval][out] */ ComBool *pfOk) = 0;

		virtual HRESULT STDMETHODCALLTYPE CompleteEdits(
			/* [out] */ VwChangeInfo *pci,
			/* [retval][out] */ ComBool *pfOk) = 0;

		virtual HRESULT STDMETHODCALLTYPE ExtendToStringBoundaries( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EndBeforeAnchor(
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual HRESULT STDMETHODCALLTYPE Location(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [out] */ RECT *prdPrimary,
			/* [out] */ RECT *prdSecondary,
			/* [out] */ ComBool *pfSplit,
			/* [out] */ ComBool *pfEndBeforeAnchor) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetParaLocation(
			/* [out] */ RECT *prdLoc) = 0;

		virtual HRESULT STDMETHODCALLTYPE ReplaceWithTsString(
			/* [in] */ /* external definition not present */ ITsString *ptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetSelectionString(
			/* [out] */ /* external definition not present */ ITsString **pptss,
			/* [in] */ BSTR bstrSep) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFirstParaString(
			/* [out] */ /* external definition not present */ ITsString **pptss,
			/* [in] */ BSTR bstrSep,
			/* [out] */ ComBool *pfGotItAll) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetIPLocation(
			/* [in] */ ComBool fTopLine,
			/* [in] */ int xdPos) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CanFormatPara(
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CanFormatChar(
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CanFormatOverlay(
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual HRESULT STDMETHODCALLTYPE Install( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Follows(
			/* [in] */ IVwSelection *psel,
			/* [retval][out] */ ComBool *pfFollows) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsValid(
			/* [retval][out] */ ComBool *pfValid) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ParagraphOffset(
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ int *pich) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SelType(
			/* [retval][out] */ VwSelType *piType) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RootBox(
			/* [retval][out] */ IVwRootBox **pprootb) = 0;

		virtual HRESULT STDMETHODCALLTYPE GrowToWord(
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE EndPoint(
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetIpTypingProps(
			/* [in] */ /* external definition not present */ ITsTextProps *pttp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxDepth(
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ int *pcDepth) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxIndex(
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [retval][out] */ int *piAtLevel) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxCount(
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [retval][out] */ int *pcAtLevel) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxType(
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [retval][out] */ VwBoxType *pvbt) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwSelectionVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwSelection * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwSelection * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwSelection * This);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )(
			IVwSelection * This,
			/* [out] */ UINT *pctinfo);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )(
			IVwSelection * This,
			/* [in] */ UINT iTInfo,
			/* [in] */ LCID lcid,
			/* [out] */ ITypeInfo **ppTInfo);

		HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )(
			IVwSelection * This,
			/* [in] */ REFIID riid,
			/* [size_is][in] */ LPOLESTR *rgszNames,
			/* [in] */ UINT cNames,
			/* [in] */ LCID lcid,
			/* [size_is][out] */ DISPID *rgDispId);

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )(
			IVwSelection * This,
			/* [in] */ DISPID dispIdMember,
			/* [in] */ REFIID riid,
			/* [in] */ LCID lcid,
			/* [in] */ WORD wFlags,
			/* [out][in] */ DISPPARAMS *pDispParams,
			/* [out] */ VARIANT *pVarResult,
			/* [out] */ EXCEPINFO *pExcepInfo,
			/* [out] */ UINT *puArgErr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsRange )(
			IVwSelection * This,
			/* [retval][out] */ ComBool *pfRet);

		HRESULT ( STDMETHODCALLTYPE *GetSelectionProps )(
			IVwSelection * This,
			/* [in] */ int cttpMax,
			/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttp,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvps,
			/* [out] */ int *pcttp);

		HRESULT ( STDMETHODCALLTYPE *GetHardAndSoftCharProps )(
			IVwSelection * This,
			/* [in] */ int cttpMax,
			/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpSel,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
			/* [out] */ int *pcttp);

		HRESULT ( STDMETHODCALLTYPE *GetParaProps )(
			IVwSelection * This,
			/* [in] */ int cttpMax,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvps,
			/* [out] */ int *pcttp);

		HRESULT ( STDMETHODCALLTYPE *GetHardAndSoftParaProps )(
			IVwSelection * This,
			/* [in] */ int cttpMax,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpPara,
			/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpHard,
			/* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
			/* [out] */ int *pcttp);

		HRESULT ( STDMETHODCALLTYPE *SetSelectionProps )(
			IVwSelection * This,
			/* [in] */ int cttp,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttp);

		HRESULT ( STDMETHODCALLTYPE *TextSelInfo )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [out] */ /* external definition not present */ ITsString **pptss,
			/* [out] */ int *pich,
			/* [out] */ ComBool *pfAssocPrev,
			/* [out] */ long *phvoObj,
			/* [out] */ int *ptag,
			/* [out] */ int *pws);

		HRESULT ( STDMETHODCALLTYPE *CLevels )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ int *pclev);

		HRESULT ( STDMETHODCALLTYPE *PropInfo )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int ilev,
			/* [out] */ long *phvoObj,
			/* [out] */ int *ptag,
			/* [out] */ int *pihvo,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ IVwPropertyStore **ppvps);

		HRESULT ( STDMETHODCALLTYPE *AllTextSelInfo )(
			IVwSelection * This,
			/* [out] */ int *pihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][out] */ VwSelLevInfo *prgvsli,
			/* [out] */ int *ptagTextProp,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ int *pichAnchor,
			/* [out] */ int *pichEnd,
			/* [out] */ int *pws,
			/* [out] */ ComBool *pfAssocPrev,
			/* [out] */ int *pihvoEnd,
			/* [out] */ /* external definition not present */ ITsTextProps **ppttp);

		HRESULT ( STDMETHODCALLTYPE *AllSelEndInfo )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [out] */ int *pihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][out] */ VwSelLevInfo *prgvsli,
			/* [out] */ int *ptagTextProp,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ int *pich,
			/* [out] */ int *pws,
			/* [out] */ ComBool *pfAssocPrev,
			/* [out] */ /* external definition not present */ ITsTextProps **ppttp);

		HRESULT ( STDMETHODCALLTYPE *Commit )(
			IVwSelection * This,
			/* [retval][out] */ ComBool *pfOk);

		HRESULT ( STDMETHODCALLTYPE *CompleteEdits )(
			IVwSelection * This,
			/* [out] */ VwChangeInfo *pci,
			/* [retval][out] */ ComBool *pfOk);

		HRESULT ( STDMETHODCALLTYPE *ExtendToStringBoundaries )(
			IVwSelection * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EndBeforeAnchor )(
			IVwSelection * This,
			/* [retval][out] */ ComBool *pfRet);

		HRESULT ( STDMETHODCALLTYPE *Location )(
			IVwSelection * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [out] */ RECT *prdPrimary,
			/* [out] */ RECT *prdSecondary,
			/* [out] */ ComBool *pfSplit,
			/* [out] */ ComBool *pfEndBeforeAnchor);

		HRESULT ( STDMETHODCALLTYPE *GetParaLocation )(
			IVwSelection * This,
			/* [out] */ RECT *prdLoc);

		HRESULT ( STDMETHODCALLTYPE *ReplaceWithTsString )(
			IVwSelection * This,
			/* [in] */ /* external definition not present */ ITsString *ptss);

		HRESULT ( STDMETHODCALLTYPE *GetSelectionString )(
			IVwSelection * This,
			/* [out] */ /* external definition not present */ ITsString **pptss,
			/* [in] */ BSTR bstrSep);

		HRESULT ( STDMETHODCALLTYPE *GetFirstParaString )(
			IVwSelection * This,
			/* [out] */ /* external definition not present */ ITsString **pptss,
			/* [in] */ BSTR bstrSep,
			/* [out] */ ComBool *pfGotItAll);

		HRESULT ( STDMETHODCALLTYPE *SetIPLocation )(
			IVwSelection * This,
			/* [in] */ ComBool fTopLine,
			/* [in] */ int xdPos);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CanFormatPara )(
			IVwSelection * This,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CanFormatChar )(
			IVwSelection * This,
			/* [retval][out] */ ComBool *pfRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CanFormatOverlay )(
			IVwSelection * This,
			/* [retval][out] */ ComBool *pfRet);

		HRESULT ( STDMETHODCALLTYPE *Install )(
			IVwSelection * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Follows )(
			IVwSelection * This,
			/* [in] */ IVwSelection *psel,
			/* [retval][out] */ ComBool *pfFollows);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsValid )(
			IVwSelection * This,
			/* [retval][out] */ ComBool *pfValid);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ParagraphOffset )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ int *pich);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SelType )(
			IVwSelection * This,
			/* [retval][out] */ VwSelType *piType);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RootBox )(
			IVwSelection * This,
			/* [retval][out] */ IVwRootBox **pprootb);

		HRESULT ( STDMETHODCALLTYPE *GrowToWord )(
			IVwSelection * This,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *EndPoint )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *SetIpTypingProps )(
			IVwSelection * This,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxDepth )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [retval][out] */ int *pcDepth);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxIndex )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [retval][out] */ int *piAtLevel);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxCount )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [retval][out] */ int *pcAtLevel);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxType )(
			IVwSelection * This,
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [retval][out] */ VwBoxType *pvbt);

		END_INTERFACE
	} IVwSelectionVtbl;

	interface IVwSelection
	{
		CONST_VTBL struct IVwSelectionVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwSelection_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwSelection_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwSelection_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwSelection_GetTypeInfoCount(This,pctinfo)	\
	(This)->lpVtbl -> GetTypeInfoCount(This,pctinfo)

#define IVwSelection_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
	(This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo)

#define IVwSelection_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
	(This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)

#define IVwSelection_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
	(This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)


#define IVwSelection_get_IsRange(This,pfRet)	\
	(This)->lpVtbl -> get_IsRange(This,pfRet)

#define IVwSelection_GetSelectionProps(This,cttpMax,prgpttp,prgpvps,pcttp)	\
	(This)->lpVtbl -> GetSelectionProps(This,cttpMax,prgpttp,prgpvps,pcttp)

#define IVwSelection_GetHardAndSoftCharProps(This,cttpMax,prgpttpSel,prgpvpsSoft,pcttp)	\
	(This)->lpVtbl -> GetHardAndSoftCharProps(This,cttpMax,prgpttpSel,prgpvpsSoft,pcttp)

#define IVwSelection_GetParaProps(This,cttpMax,prgpvps,pcttp)	\
	(This)->lpVtbl -> GetParaProps(This,cttpMax,prgpvps,pcttp)

#define IVwSelection_GetHardAndSoftParaProps(This,cttpMax,prgpttpPara,prgpttpHard,prgpvpsSoft,pcttp)	\
	(This)->lpVtbl -> GetHardAndSoftParaProps(This,cttpMax,prgpttpPara,prgpttpHard,prgpvpsSoft,pcttp)

#define IVwSelection_SetSelectionProps(This,cttp,prgpttp)	\
	(This)->lpVtbl -> SetSelectionProps(This,cttp,prgpttp)

#define IVwSelection_TextSelInfo(This,fEndPoint,pptss,pich,pfAssocPrev,phvoObj,ptag,pws)	\
	(This)->lpVtbl -> TextSelInfo(This,fEndPoint,pptss,pich,pfAssocPrev,phvoObj,ptag,pws)

#define IVwSelection_CLevels(This,fEndPoint,pclev)	\
	(This)->lpVtbl -> CLevels(This,fEndPoint,pclev)

#define IVwSelection_PropInfo(This,fEndPoint,ilev,phvoObj,ptag,pihvo,pcpropPrevious,ppvps)	\
	(This)->lpVtbl -> PropInfo(This,fEndPoint,ilev,phvoObj,ptag,pihvo,pcpropPrevious,ppvps)

#define IVwSelection_AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws,pfAssocPrev,pihvoEnd,ppttp)	\
	(This)->lpVtbl -> AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws,pfAssocPrev,pihvoEnd,ppttp)

#define IVwSelection_AllSelEndInfo(This,fEndPoint,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pich,pws,pfAssocPrev,ppttp)	\
	(This)->lpVtbl -> AllSelEndInfo(This,fEndPoint,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pich,pws,pfAssocPrev,ppttp)

#define IVwSelection_Commit(This,pfOk)	\
	(This)->lpVtbl -> Commit(This,pfOk)

#define IVwSelection_CompleteEdits(This,pci,pfOk)	\
	(This)->lpVtbl -> CompleteEdits(This,pci,pfOk)

#define IVwSelection_ExtendToStringBoundaries(This)	\
	(This)->lpVtbl -> ExtendToStringBoundaries(This)

#define IVwSelection_get_EndBeforeAnchor(This,pfRet)	\
	(This)->lpVtbl -> get_EndBeforeAnchor(This,pfRet)

#define IVwSelection_Location(This,pvg,rcSrc,rcDst,prdPrimary,prdSecondary,pfSplit,pfEndBeforeAnchor)	\
	(This)->lpVtbl -> Location(This,pvg,rcSrc,rcDst,prdPrimary,prdSecondary,pfSplit,pfEndBeforeAnchor)

#define IVwSelection_GetParaLocation(This,prdLoc)	\
	(This)->lpVtbl -> GetParaLocation(This,prdLoc)

#define IVwSelection_ReplaceWithTsString(This,ptss)	\
	(This)->lpVtbl -> ReplaceWithTsString(This,ptss)

#define IVwSelection_GetSelectionString(This,pptss,bstrSep)	\
	(This)->lpVtbl -> GetSelectionString(This,pptss,bstrSep)

#define IVwSelection_GetFirstParaString(This,pptss,bstrSep,pfGotItAll)	\
	(This)->lpVtbl -> GetFirstParaString(This,pptss,bstrSep,pfGotItAll)

#define IVwSelection_SetIPLocation(This,fTopLine,xdPos)	\
	(This)->lpVtbl -> SetIPLocation(This,fTopLine,xdPos)

#define IVwSelection_get_CanFormatPara(This,pfRet)	\
	(This)->lpVtbl -> get_CanFormatPara(This,pfRet)

#define IVwSelection_get_CanFormatChar(This,pfRet)	\
	(This)->lpVtbl -> get_CanFormatChar(This,pfRet)

#define IVwSelection_get_CanFormatOverlay(This,pfRet)	\
	(This)->lpVtbl -> get_CanFormatOverlay(This,pfRet)

#define IVwSelection_Install(This)	\
	(This)->lpVtbl -> Install(This)

#define IVwSelection_get_Follows(This,psel,pfFollows)	\
	(This)->lpVtbl -> get_Follows(This,psel,pfFollows)

#define IVwSelection_get_IsValid(This,pfValid)	\
	(This)->lpVtbl -> get_IsValid(This,pfValid)

#define IVwSelection_get_ParagraphOffset(This,fEndPoint,pich)	\
	(This)->lpVtbl -> get_ParagraphOffset(This,fEndPoint,pich)

#define IVwSelection_get_SelType(This,piType)	\
	(This)->lpVtbl -> get_SelType(This,piType)

#define IVwSelection_get_RootBox(This,pprootb)	\
	(This)->lpVtbl -> get_RootBox(This,pprootb)

#define IVwSelection_GrowToWord(This,ppsel)	\
	(This)->lpVtbl -> GrowToWord(This,ppsel)

#define IVwSelection_EndPoint(This,fEndPoint,ppsel)	\
	(This)->lpVtbl -> EndPoint(This,fEndPoint,ppsel)

#define IVwSelection_SetIpTypingProps(This,pttp)	\
	(This)->lpVtbl -> SetIpTypingProps(This,pttp)

#define IVwSelection_get_BoxDepth(This,fEndPoint,pcDepth)	\
	(This)->lpVtbl -> get_BoxDepth(This,fEndPoint,pcDepth)

#define IVwSelection_get_BoxIndex(This,fEndPoint,iLevel,piAtLevel)	\
	(This)->lpVtbl -> get_BoxIndex(This,fEndPoint,iLevel,piAtLevel)

#define IVwSelection_get_BoxCount(This,fEndPoint,iLevel,pcAtLevel)	\
	(This)->lpVtbl -> get_BoxCount(This,fEndPoint,iLevel,pcAtLevel)

#define IVwSelection_get_BoxType(This,fEndPoint,iLevel,pvbt)	\
	(This)->lpVtbl -> get_BoxType(This,fEndPoint,iLevel,pvbt)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_IsRange_Proxy(
	IVwSelection * This,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB IVwSelection_get_IsRange_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GetSelectionProps_Proxy(
	IVwSelection * This,
	/* [in] */ int cttpMax,
	/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttp,
	/* [size_is][out][in] */ IVwPropertyStore **prgpvps,
	/* [out] */ int *pcttp);


void __RPC_STUB IVwSelection_GetSelectionProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GetHardAndSoftCharProps_Proxy(
	IVwSelection * This,
	/* [in] */ int cttpMax,
	/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpSel,
	/* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
	/* [out] */ int *pcttp);


void __RPC_STUB IVwSelection_GetHardAndSoftCharProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GetParaProps_Proxy(
	IVwSelection * This,
	/* [in] */ int cttpMax,
	/* [size_is][out][in] */ IVwPropertyStore **prgpvps,
	/* [out] */ int *pcttp);


void __RPC_STUB IVwSelection_GetParaProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GetHardAndSoftParaProps_Proxy(
	IVwSelection * This,
	/* [in] */ int cttpMax,
	/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpPara,
	/* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpHard,
	/* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
	/* [out] */ int *pcttp);


void __RPC_STUB IVwSelection_GetHardAndSoftParaProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_SetSelectionProps_Proxy(
	IVwSelection * This,
	/* [in] */ int cttp,
	/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttp);


void __RPC_STUB IVwSelection_SetSelectionProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_TextSelInfo_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [out] */ /* external definition not present */ ITsString **pptss,
	/* [out] */ int *pich,
	/* [out] */ ComBool *pfAssocPrev,
	/* [out] */ long *phvoObj,
	/* [out] */ int *ptag,
	/* [out] */ int *pws);


void __RPC_STUB IVwSelection_TextSelInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_CLevels_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [retval][out] */ int *pclev);


void __RPC_STUB IVwSelection_CLevels_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_PropInfo_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [in] */ int ilev,
	/* [out] */ long *phvoObj,
	/* [out] */ int *ptag,
	/* [out] */ int *pihvo,
	/* [out] */ int *pcpropPrevious,
	/* [out] */ IVwPropertyStore **ppvps);


void __RPC_STUB IVwSelection_PropInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_AllTextSelInfo_Proxy(
	IVwSelection * This,
	/* [out] */ int *pihvoRoot,
	/* [in] */ int cvlsi,
	/* [size_is][out] */ VwSelLevInfo *prgvsli,
	/* [out] */ int *ptagTextProp,
	/* [out] */ int *pcpropPrevious,
	/* [out] */ int *pichAnchor,
	/* [out] */ int *pichEnd,
	/* [out] */ int *pws,
	/* [out] */ ComBool *pfAssocPrev,
	/* [out] */ int *pihvoEnd,
	/* [out] */ /* external definition not present */ ITsTextProps **ppttp);


void __RPC_STUB IVwSelection_AllTextSelInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_AllSelEndInfo_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [out] */ int *pihvoRoot,
	/* [in] */ int cvlsi,
	/* [size_is][out] */ VwSelLevInfo *prgvsli,
	/* [out] */ int *ptagTextProp,
	/* [out] */ int *pcpropPrevious,
	/* [out] */ int *pich,
	/* [out] */ int *pws,
	/* [out] */ ComBool *pfAssocPrev,
	/* [out] */ /* external definition not present */ ITsTextProps **ppttp);


void __RPC_STUB IVwSelection_AllSelEndInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_Commit_Proxy(
	IVwSelection * This,
	/* [retval][out] */ ComBool *pfOk);


void __RPC_STUB IVwSelection_Commit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_CompleteEdits_Proxy(
	IVwSelection * This,
	/* [out] */ VwChangeInfo *pci,
	/* [retval][out] */ ComBool *pfOk);


void __RPC_STUB IVwSelection_CompleteEdits_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_ExtendToStringBoundaries_Proxy(
	IVwSelection * This);


void __RPC_STUB IVwSelection_ExtendToStringBoundaries_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_EndBeforeAnchor_Proxy(
	IVwSelection * This,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB IVwSelection_get_EndBeforeAnchor_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_Location_Proxy(
	IVwSelection * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [out] */ RECT *prdPrimary,
	/* [out] */ RECT *prdSecondary,
	/* [out] */ ComBool *pfSplit,
	/* [out] */ ComBool *pfEndBeforeAnchor);


void __RPC_STUB IVwSelection_Location_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GetParaLocation_Proxy(
	IVwSelection * This,
	/* [out] */ RECT *prdLoc);


void __RPC_STUB IVwSelection_GetParaLocation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_ReplaceWithTsString_Proxy(
	IVwSelection * This,
	/* [in] */ /* external definition not present */ ITsString *ptss);


void __RPC_STUB IVwSelection_ReplaceWithTsString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GetSelectionString_Proxy(
	IVwSelection * This,
	/* [out] */ /* external definition not present */ ITsString **pptss,
	/* [in] */ BSTR bstrSep);


void __RPC_STUB IVwSelection_GetSelectionString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GetFirstParaString_Proxy(
	IVwSelection * This,
	/* [out] */ /* external definition not present */ ITsString **pptss,
	/* [in] */ BSTR bstrSep,
	/* [out] */ ComBool *pfGotItAll);


void __RPC_STUB IVwSelection_GetFirstParaString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_SetIPLocation_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fTopLine,
	/* [in] */ int xdPos);


void __RPC_STUB IVwSelection_SetIPLocation_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_CanFormatPara_Proxy(
	IVwSelection * This,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB IVwSelection_get_CanFormatPara_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_CanFormatChar_Proxy(
	IVwSelection * This,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB IVwSelection_get_CanFormatChar_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_CanFormatOverlay_Proxy(
	IVwSelection * This,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB IVwSelection_get_CanFormatOverlay_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_Install_Proxy(
	IVwSelection * This);


void __RPC_STUB IVwSelection_Install_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_Follows_Proxy(
	IVwSelection * This,
	/* [in] */ IVwSelection *psel,
	/* [retval][out] */ ComBool *pfFollows);


void __RPC_STUB IVwSelection_get_Follows_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_IsValid_Proxy(
	IVwSelection * This,
	/* [retval][out] */ ComBool *pfValid);


void __RPC_STUB IVwSelection_get_IsValid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_ParagraphOffset_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [retval][out] */ int *pich);


void __RPC_STUB IVwSelection_get_ParagraphOffset_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_SelType_Proxy(
	IVwSelection * This,
	/* [retval][out] */ VwSelType *piType);


void __RPC_STUB IVwSelection_get_SelType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_RootBox_Proxy(
	IVwSelection * This,
	/* [retval][out] */ IVwRootBox **pprootb);


void __RPC_STUB IVwSelection_get_RootBox_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_GrowToWord_Proxy(
	IVwSelection * This,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwSelection_GrowToWord_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_EndPoint_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwSelection_EndPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSelection_SetIpTypingProps_Proxy(
	IVwSelection * This,
	/* [in] */ /* external definition not present */ ITsTextProps *pttp);


void __RPC_STUB IVwSelection_SetIpTypingProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_BoxDepth_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [retval][out] */ int *pcDepth);


void __RPC_STUB IVwSelection_get_BoxDepth_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_BoxIndex_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [in] */ int iLevel,
	/* [retval][out] */ int *piAtLevel);


void __RPC_STUB IVwSelection_get_BoxIndex_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_BoxCount_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [in] */ int iLevel,
	/* [retval][out] */ int *pcAtLevel);


void __RPC_STUB IVwSelection_get_BoxCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSelection_get_BoxType_Proxy(
	IVwSelection * This,
	/* [in] */ ComBool fEndPoint,
	/* [in] */ int iLevel,
	/* [retval][out] */ VwBoxType *pvbt);


void __RPC_STUB IVwSelection_get_BoxType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwSelection_INTERFACE_DEFINED__ */


#ifndef __IVwEmbeddedWindow_INTERFACE_DEFINED__
#define __IVwEmbeddedWindow_INTERFACE_DEFINED__

/* interface IVwEmbeddedWindow */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwEmbeddedWindow;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("f6d10646-c00c-11d2-8078-0000c0fb81b5")
	IVwEmbeddedWindow : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE MoveWindow(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int xdLeft,
			/* [in] */ int ydTop,
			/* [in] */ int dxdWidth,
			/* [in] */ int dydHeight) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsWindowVisible(
			/* [retval][out] */ ComBool *pfRet) = 0;

		virtual HRESULT STDMETHODCALLTYPE ShowWindow( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE DrawWindow(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Width(
			/* [retval][out] */ int *pnTwips) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Height(
			/* [retval][out] */ int *pnTwips) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwEmbeddedWindowVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwEmbeddedWindow * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwEmbeddedWindow * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwEmbeddedWindow * This);

		HRESULT ( STDMETHODCALLTYPE *MoveWindow )(
			IVwEmbeddedWindow * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int xdLeft,
			/* [in] */ int ydTop,
			/* [in] */ int dxdWidth,
			/* [in] */ int dydHeight);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsWindowVisible )(
			IVwEmbeddedWindow * This,
			/* [retval][out] */ ComBool *pfRet);

		HRESULT ( STDMETHODCALLTYPE *ShowWindow )(
			IVwEmbeddedWindow * This);

		HRESULT ( STDMETHODCALLTYPE *DrawWindow )(
			IVwEmbeddedWindow * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Width )(
			IVwEmbeddedWindow * This,
			/* [retval][out] */ int *pnTwips);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Height )(
			IVwEmbeddedWindow * This,
			/* [retval][out] */ int *pnTwips);

		END_INTERFACE
	} IVwEmbeddedWindowVtbl;

	interface IVwEmbeddedWindow
	{
		CONST_VTBL struct IVwEmbeddedWindowVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwEmbeddedWindow_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwEmbeddedWindow_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwEmbeddedWindow_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwEmbeddedWindow_MoveWindow(This,pvg,xdLeft,ydTop,dxdWidth,dydHeight)	\
	(This)->lpVtbl -> MoveWindow(This,pvg,xdLeft,ydTop,dxdWidth,dydHeight)

#define IVwEmbeddedWindow_get_IsWindowVisible(This,pfRet)	\
	(This)->lpVtbl -> get_IsWindowVisible(This,pfRet)

#define IVwEmbeddedWindow_ShowWindow(This)	\
	(This)->lpVtbl -> ShowWindow(This)

#define IVwEmbeddedWindow_DrawWindow(This,pvg)	\
	(This)->lpVtbl -> DrawWindow(This,pvg)

#define IVwEmbeddedWindow_get_Width(This,pnTwips)	\
	(This)->lpVtbl -> get_Width(This,pnTwips)

#define IVwEmbeddedWindow_get_Height(This,pnTwips)	\
	(This)->lpVtbl -> get_Height(This,pnTwips)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwEmbeddedWindow_MoveWindow_Proxy(
	IVwEmbeddedWindow * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ int xdLeft,
	/* [in] */ int ydTop,
	/* [in] */ int dxdWidth,
	/* [in] */ int dydHeight);


void __RPC_STUB IVwEmbeddedWindow_MoveWindow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwEmbeddedWindow_get_IsWindowVisible_Proxy(
	IVwEmbeddedWindow * This,
	/* [retval][out] */ ComBool *pfRet);


void __RPC_STUB IVwEmbeddedWindow_get_IsWindowVisible_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEmbeddedWindow_ShowWindow_Proxy(
	IVwEmbeddedWindow * This);


void __RPC_STUB IVwEmbeddedWindow_ShowWindow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEmbeddedWindow_DrawWindow_Proxy(
	IVwEmbeddedWindow * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg);


void __RPC_STUB IVwEmbeddedWindow_DrawWindow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwEmbeddedWindow_get_Width_Proxy(
	IVwEmbeddedWindow * This,
	/* [retval][out] */ int *pnTwips);


void __RPC_STUB IVwEmbeddedWindow_get_Width_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwEmbeddedWindow_get_Height_Proxy(
	IVwEmbeddedWindow * This,
	/* [retval][out] */ int *pnTwips);


void __RPC_STUB IVwEmbeddedWindow_get_Height_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwEmbeddedWindow_INTERFACE_DEFINED__ */


#ifndef __IVwStylesheet_INTERFACE_DEFINED__
#define __IVwStylesheet_INTERFACE_DEFINED__

/* interface IVwStylesheet */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwStylesheet;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("D77C0DBC-C7BC-441d-9587-1E3664E1BCD3")
	IVwStylesheet : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE GetDefaultBasedOnStyleName(
			/* [retval][out] */ BSTR *pbstrNormal) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDefaultStyleForContext(
			/* [in] */ int nContext,
			/* [retval][out] */ BSTR *pbstrStyleName) = 0;

		virtual HRESULT STDMETHODCALLTYPE PutStyle(
			/* [in] */ BSTR bstrName,
			/* [in] */ BSTR bstrUsage,
			/* [in] */ long hvoStyle,
			/* [in] */ long hvoBasedOn,
			/* [in] */ long hvoNext,
			/* [in] */ int nType,
			/* [in] */ ComBool fBuiltIn,
			/* [in] */ ComBool fModified,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetStyleRgch(
			/* [in] */ int cch,
			/* [size_is][in] */ OLECHAR *prgchName,
			/* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetNextStyle(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ BSTR *pbstrNext) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetBasedOn(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ BSTR *pbstrBasedOn) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetType(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ int *pnType) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetContext(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ int *pnContext) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsBuiltIn(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfBuiltIn) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsModified(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfModified) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DataAccess(
			/* [retval][out] */ ISilDataAccess **ppsda) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeNewStyle(
			/* [retval][out] */ long *phvoNewStyle) = 0;

		virtual HRESULT STDMETHODCALLTYPE Delete(
			/* [in] */ long hvoStyle) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CStyles(
			/* [retval][out] */ int *pcttp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NthStyle(
			/* [in] */ int ihvo,
			/* [retval][out] */ long *phvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NthStyleName(
			/* [in] */ int ihvo,
			/* [retval][out] */ BSTR *pbstrStyleName) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NormalFontStyle(
			/* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsStyleProtected(
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfProtected) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheProps(
			/* [in] */ int cch,
			/* [size_is][in] */ OLECHAR *prgchName,
			/* [in] */ long hvoStyle,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwStylesheetVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwStylesheet * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwStylesheet * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwStylesheet * This);

		HRESULT ( STDMETHODCALLTYPE *GetDefaultBasedOnStyleName )(
			IVwStylesheet * This,
			/* [retval][out] */ BSTR *pbstrNormal);

		HRESULT ( STDMETHODCALLTYPE *GetDefaultStyleForContext )(
			IVwStylesheet * This,
			/* [in] */ int nContext,
			/* [retval][out] */ BSTR *pbstrStyleName);

		HRESULT ( STDMETHODCALLTYPE *PutStyle )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [in] */ BSTR bstrUsage,
			/* [in] */ long hvoStyle,
			/* [in] */ long hvoBasedOn,
			/* [in] */ long hvoNext,
			/* [in] */ int nType,
			/* [in] */ ComBool fBuiltIn,
			/* [in] */ ComBool fModified,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp);

		HRESULT ( STDMETHODCALLTYPE *GetStyleRgch )(
			IVwStylesheet * This,
			/* [in] */ int cch,
			/* [size_is][in] */ OLECHAR *prgchName,
			/* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp);

		HRESULT ( STDMETHODCALLTYPE *GetNextStyle )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ BSTR *pbstrNext);

		HRESULT ( STDMETHODCALLTYPE *GetBasedOn )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ BSTR *pbstrBasedOn);

		HRESULT ( STDMETHODCALLTYPE *GetType )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ int *pnType);

		HRESULT ( STDMETHODCALLTYPE *GetContext )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ int *pnContext);

		HRESULT ( STDMETHODCALLTYPE *IsBuiltIn )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfBuiltIn);

		HRESULT ( STDMETHODCALLTYPE *IsModified )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfModified);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DataAccess )(
			IVwStylesheet * This,
			/* [retval][out] */ ISilDataAccess **ppsda);

		HRESULT ( STDMETHODCALLTYPE *MakeNewStyle )(
			IVwStylesheet * This,
			/* [retval][out] */ long *phvoNewStyle);

		HRESULT ( STDMETHODCALLTYPE *Delete )(
			IVwStylesheet * This,
			/* [in] */ long hvoStyle);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CStyles )(
			IVwStylesheet * This,
			/* [retval][out] */ int *pcttp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NthStyle )(
			IVwStylesheet * This,
			/* [in] */ int ihvo,
			/* [retval][out] */ long *phvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NthStyleName )(
			IVwStylesheet * This,
			/* [in] */ int ihvo,
			/* [retval][out] */ BSTR *pbstrStyleName);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NormalFontStyle )(
			IVwStylesheet * This,
			/* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsStyleProtected )(
			IVwStylesheet * This,
			/* [in] */ BSTR bstrName,
			/* [retval][out] */ ComBool *pfProtected);

		HRESULT ( STDMETHODCALLTYPE *CacheProps )(
			IVwStylesheet * This,
			/* [in] */ int cch,
			/* [size_is][in] */ OLECHAR *prgchName,
			/* [in] */ long hvoStyle,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp);

		END_INTERFACE
	} IVwStylesheetVtbl;

	interface IVwStylesheet
	{
		CONST_VTBL struct IVwStylesheetVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwStylesheet_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwStylesheet_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwStylesheet_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwStylesheet_GetDefaultBasedOnStyleName(This,pbstrNormal)	\
	(This)->lpVtbl -> GetDefaultBasedOnStyleName(This,pbstrNormal)

#define IVwStylesheet_GetDefaultStyleForContext(This,nContext,pbstrStyleName)	\
	(This)->lpVtbl -> GetDefaultStyleForContext(This,nContext,pbstrStyleName)

#define IVwStylesheet_PutStyle(This,bstrName,bstrUsage,hvoStyle,hvoBasedOn,hvoNext,nType,fBuiltIn,fModified,pttp)	\
	(This)->lpVtbl -> PutStyle(This,bstrName,bstrUsage,hvoStyle,hvoBasedOn,hvoNext,nType,fBuiltIn,fModified,pttp)

#define IVwStylesheet_GetStyleRgch(This,cch,prgchName,ppttp)	\
	(This)->lpVtbl -> GetStyleRgch(This,cch,prgchName,ppttp)

#define IVwStylesheet_GetNextStyle(This,bstrName,pbstrNext)	\
	(This)->lpVtbl -> GetNextStyle(This,bstrName,pbstrNext)

#define IVwStylesheet_GetBasedOn(This,bstrName,pbstrBasedOn)	\
	(This)->lpVtbl -> GetBasedOn(This,bstrName,pbstrBasedOn)

#define IVwStylesheet_GetType(This,bstrName,pnType)	\
	(This)->lpVtbl -> GetType(This,bstrName,pnType)

#define IVwStylesheet_GetContext(This,bstrName,pnContext)	\
	(This)->lpVtbl -> GetContext(This,bstrName,pnContext)

#define IVwStylesheet_IsBuiltIn(This,bstrName,pfBuiltIn)	\
	(This)->lpVtbl -> IsBuiltIn(This,bstrName,pfBuiltIn)

#define IVwStylesheet_IsModified(This,bstrName,pfModified)	\
	(This)->lpVtbl -> IsModified(This,bstrName,pfModified)

#define IVwStylesheet_get_DataAccess(This,ppsda)	\
	(This)->lpVtbl -> get_DataAccess(This,ppsda)

#define IVwStylesheet_MakeNewStyle(This,phvoNewStyle)	\
	(This)->lpVtbl -> MakeNewStyle(This,phvoNewStyle)

#define IVwStylesheet_Delete(This,hvoStyle)	\
	(This)->lpVtbl -> Delete(This,hvoStyle)

#define IVwStylesheet_get_CStyles(This,pcttp)	\
	(This)->lpVtbl -> get_CStyles(This,pcttp)

#define IVwStylesheet_get_NthStyle(This,ihvo,phvo)	\
	(This)->lpVtbl -> get_NthStyle(This,ihvo,phvo)

#define IVwStylesheet_get_NthStyleName(This,ihvo,pbstrStyleName)	\
	(This)->lpVtbl -> get_NthStyleName(This,ihvo,pbstrStyleName)

#define IVwStylesheet_get_NormalFontStyle(This,ppttp)	\
	(This)->lpVtbl -> get_NormalFontStyle(This,ppttp)

#define IVwStylesheet_get_IsStyleProtected(This,bstrName,pfProtected)	\
	(This)->lpVtbl -> get_IsStyleProtected(This,bstrName,pfProtected)

#define IVwStylesheet_CacheProps(This,cch,prgchName,hvoStyle,pttp)	\
	(This)->lpVtbl -> CacheProps(This,cch,prgchName,hvoStyle,pttp)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwStylesheet_GetDefaultBasedOnStyleName_Proxy(
	IVwStylesheet * This,
	/* [retval][out] */ BSTR *pbstrNormal);


void __RPC_STUB IVwStylesheet_GetDefaultBasedOnStyleName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_GetDefaultStyleForContext_Proxy(
	IVwStylesheet * This,
	/* [in] */ int nContext,
	/* [retval][out] */ BSTR *pbstrStyleName);


void __RPC_STUB IVwStylesheet_GetDefaultStyleForContext_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_PutStyle_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [in] */ BSTR bstrUsage,
	/* [in] */ long hvoStyle,
	/* [in] */ long hvoBasedOn,
	/* [in] */ long hvoNext,
	/* [in] */ int nType,
	/* [in] */ ComBool fBuiltIn,
	/* [in] */ ComBool fModified,
	/* [in] */ /* external definition not present */ ITsTextProps *pttp);


void __RPC_STUB IVwStylesheet_PutStyle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_GetStyleRgch_Proxy(
	IVwStylesheet * This,
	/* [in] */ int cch,
	/* [size_is][in] */ OLECHAR *prgchName,
	/* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp);


void __RPC_STUB IVwStylesheet_GetStyleRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_GetNextStyle_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ BSTR *pbstrNext);


void __RPC_STUB IVwStylesheet_GetNextStyle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_GetBasedOn_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ BSTR *pbstrBasedOn);


void __RPC_STUB IVwStylesheet_GetBasedOn_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_GetType_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ int *pnType);


void __RPC_STUB IVwStylesheet_GetType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_GetContext_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ int *pnContext);


void __RPC_STUB IVwStylesheet_GetContext_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_IsBuiltIn_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ ComBool *pfBuiltIn);


void __RPC_STUB IVwStylesheet_IsBuiltIn_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_IsModified_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ ComBool *pfModified);


void __RPC_STUB IVwStylesheet_IsModified_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwStylesheet_get_DataAccess_Proxy(
	IVwStylesheet * This,
	/* [retval][out] */ ISilDataAccess **ppsda);


void __RPC_STUB IVwStylesheet_get_DataAccess_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_MakeNewStyle_Proxy(
	IVwStylesheet * This,
	/* [retval][out] */ long *phvoNewStyle);


void __RPC_STUB IVwStylesheet_MakeNewStyle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_Delete_Proxy(
	IVwStylesheet * This,
	/* [in] */ long hvoStyle);


void __RPC_STUB IVwStylesheet_Delete_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwStylesheet_get_CStyles_Proxy(
	IVwStylesheet * This,
	/* [retval][out] */ int *pcttp);


void __RPC_STUB IVwStylesheet_get_CStyles_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwStylesheet_get_NthStyle_Proxy(
	IVwStylesheet * This,
	/* [in] */ int ihvo,
	/* [retval][out] */ long *phvo);


void __RPC_STUB IVwStylesheet_get_NthStyle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwStylesheet_get_NthStyleName_Proxy(
	IVwStylesheet * This,
	/* [in] */ int ihvo,
	/* [retval][out] */ BSTR *pbstrStyleName);


void __RPC_STUB IVwStylesheet_get_NthStyleName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwStylesheet_get_NormalFontStyle_Proxy(
	IVwStylesheet * This,
	/* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp);


void __RPC_STUB IVwStylesheet_get_NormalFontStyle_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwStylesheet_get_IsStyleProtected_Proxy(
	IVwStylesheet * This,
	/* [in] */ BSTR bstrName,
	/* [retval][out] */ ComBool *pfProtected);


void __RPC_STUB IVwStylesheet_get_IsStyleProtected_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwStylesheet_CacheProps_Proxy(
	IVwStylesheet * This,
	/* [in] */ int cch,
	/* [size_is][in] */ OLECHAR *prgchName,
	/* [in] */ long hvoStyle,
	/* [in] */ /* external definition not present */ ITsTextProps *pttp);


void __RPC_STUB IVwStylesheet_CacheProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwStylesheet_INTERFACE_DEFINED__ */


#ifndef __IVwEnv_INTERFACE_DEFINED__
#define __IVwEnv_INTERFACE_DEFINED__

/* interface IVwEnv */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwEnv;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("B5A11CC3-B1D4-4ae4-A1E4-02A6A8198CEB")
	IVwEnv : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE AddObjProp(
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddObjVec(
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddObjVecItems(
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddObj(
			/* [in] */ long hvo,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddLazyVecItems(
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddLazyItems(
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddProp(
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddDerivedProp(
			/* [size_is][in] */ int *prgtag,
			/* [in] */ int ctag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE NoteDependency(
			/* [size_is][in] */ long *prghvo,
			/* [size_is][in] */ int *prgtag,
			/* [in] */ int chvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddStringProp(
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddUnicodeProp(
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwViewConstructor *pvwvc) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddIntProp(
			/* [in] */ int tag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddIntPropPic(
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvc,
			/* [in] */ int frag,
			/* [in] */ int nMin,
			/* [in] */ int nMax) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddStringAltMember(
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwViewConstructor *pvwvc) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddStringAlt(
			/* [in] */ int tag) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddStringAltSeq(
			/* [in] */ int tag,
			/* [size_is][in] */ int *prgenc,
			/* [in] */ int cws) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddString(
			/* [in] */ /* external definition not present */ ITsString *pss) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddTimeProp(
			/* [in] */ int tag,
			/* [in] */ DWORD flags) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddGenDateProp(
			/* [in] */ int tag) = 0;

		virtual HRESULT STDMETHODCALLTYPE CurrentObject(
			/* [retval][out] */ long *phvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_OpenObject(
			/* [retval][out] */ long *phvoRet) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EmbeddingLevel(
			/* [retval][out] */ int *pchvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetOuterObject(
			/* [in] */ int ichvoLevel,
			/* [out] */ long *phvo,
			/* [out] */ int *ptag,
			/* [out] */ int *pihvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DataAccess(
			/* [retval][out] */ ISilDataAccess **ppsda) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddWindow(
			/* [in] */ IVwEmbeddedWindow *pew,
			/* [in] */ int dmpAscent,
			/* [in] */ ComBool fJustifyRight,
			/* [in] */ ComBool fAutoShow) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddSeparatorBar( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddSimpleRect(
			/* [in] */ COLORREF rgb,
			/* [in] */ int dmpWidth,
			/* [in] */ int dmpHeight,
			/* [in] */ int dmpBaselineOffset) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenDiv( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseDiv( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenParagraph( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTaggedPara( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenMappedPara( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenMappedTaggedPara( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenConcPara(
			/* [in] */ int ichMinItem,
			/* [in] */ int ichLimItem,
			/* [in] */ VwConcParaOpts cpoFlags,
			/* [in] */ int dmpAlign) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenOverridePara(
			/* [in] */ int cOverrideProperties,
			/* [size_is][in] */ DispPropOverride *prgOverrideProperties) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseParagraph( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenInnerPile( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseInnerPile( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenSpan( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseSpan( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTable(
			/* [in] */ int cCols,
			/* [out][in] */ VwLength *pvlWidth,
			/* [in] */ int mpBorder,
			/* [in] */ VwAlignment vwalign,
			/* [in] */ VwFramePosition frmpos,
			/* [in] */ VwRule vwrule,
			/* [in] */ int mpSpacing,
			/* [in] */ int mpPadding) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseTable( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTableRow( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseTableRow( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTableCell(
			/* [in] */ int nRowSpan,
			/* [in] */ int nColSpan) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseTableCell( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTableHeaderCell(
			/* [in] */ int nRowSpan,
			/* [in] */ int nColSpan) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseTableHeaderCell( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeColumns(
			/* [in] */ int nColSpan,
			/* [in] */ VwLength vlWidth) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeColumnGroup(
			/* [in] */ int nColSpan,
			/* [in] */ VwLength vlWidth) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTableHeader( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseTableHeader( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTableFooter( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseTableFooter( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenTableBody( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseTableBody( void) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IntProperty(
			/* [in] */ int tpt,
			/* [in] */ int tpv,
			/* [in] */ int nValue) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_StringProperty(
			/* [in] */ int sp,
			/* [in] */ BSTR bstrValue) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Props(
			/* [in] */ /* external definition not present */ ITsTextProps *pttp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringWidth(
			/* [in] */ /* external definition not present */ ITsString *ptss,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp,
			/* [out] */ int *dmpx,
			/* [out] */ int *dmpy) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddPicture(
			/* [in] */ IPicture *ppict,
			/* [in] */ int tag) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwEnvVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwEnv * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwEnv * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *AddObjProp )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *AddObjVec )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *AddObjVecItems )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *AddObj )(
			IVwEnv * This,
			/* [in] */ long hvo,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *AddLazyVecItems )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *AddLazyItems )(
			IVwEnv * This,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *AddProp )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *AddDerivedProp )(
			IVwEnv * This,
			/* [size_is][in] */ int *prgtag,
			/* [in] */ int ctag,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *NoteDependency )(
			IVwEnv * This,
			/* [size_is][in] */ long *prghvo,
			/* [size_is][in] */ int *prgtag,
			/* [in] */ int chvo);

		HRESULT ( STDMETHODCALLTYPE *AddStringProp )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvwvc);

		HRESULT ( STDMETHODCALLTYPE *AddUnicodeProp )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwViewConstructor *pvwvc);

		HRESULT ( STDMETHODCALLTYPE *AddIntProp )(
			IVwEnv * This,
			/* [in] */ int tag);

		HRESULT ( STDMETHODCALLTYPE *AddIntPropPic )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ IVwViewConstructor *pvc,
			/* [in] */ int frag,
			/* [in] */ int nMin,
			/* [in] */ int nMax);

		HRESULT ( STDMETHODCALLTYPE *AddStringAltMember )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwViewConstructor *pvwvc);

		HRESULT ( STDMETHODCALLTYPE *AddStringAlt )(
			IVwEnv * This,
			/* [in] */ int tag);

		HRESULT ( STDMETHODCALLTYPE *AddStringAltSeq )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [size_is][in] */ int *prgenc,
			/* [in] */ int cws);

		HRESULT ( STDMETHODCALLTYPE *AddString )(
			IVwEnv * This,
			/* [in] */ /* external definition not present */ ITsString *pss);

		HRESULT ( STDMETHODCALLTYPE *AddTimeProp )(
			IVwEnv * This,
			/* [in] */ int tag,
			/* [in] */ DWORD flags);

		HRESULT ( STDMETHODCALLTYPE *AddGenDateProp )(
			IVwEnv * This,
			/* [in] */ int tag);

		HRESULT ( STDMETHODCALLTYPE *CurrentObject )(
			IVwEnv * This,
			/* [retval][out] */ long *phvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_OpenObject )(
			IVwEnv * This,
			/* [retval][out] */ long *phvoRet);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EmbeddingLevel )(
			IVwEnv * This,
			/* [retval][out] */ int *pchvo);

		HRESULT ( STDMETHODCALLTYPE *GetOuterObject )(
			IVwEnv * This,
			/* [in] */ int ichvoLevel,
			/* [out] */ long *phvo,
			/* [out] */ int *ptag,
			/* [out] */ int *pihvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DataAccess )(
			IVwEnv * This,
			/* [retval][out] */ ISilDataAccess **ppsda);

		HRESULT ( STDMETHODCALLTYPE *AddWindow )(
			IVwEnv * This,
			/* [in] */ IVwEmbeddedWindow *pew,
			/* [in] */ int dmpAscent,
			/* [in] */ ComBool fJustifyRight,
			/* [in] */ ComBool fAutoShow);

		HRESULT ( STDMETHODCALLTYPE *AddSeparatorBar )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *AddSimpleRect )(
			IVwEnv * This,
			/* [in] */ COLORREF rgb,
			/* [in] */ int dmpWidth,
			/* [in] */ int dmpHeight,
			/* [in] */ int dmpBaselineOffset);

		HRESULT ( STDMETHODCALLTYPE *OpenDiv )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *CloseDiv )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenParagraph )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenTaggedPara )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenMappedPara )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenMappedTaggedPara )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenConcPara )(
			IVwEnv * This,
			/* [in] */ int ichMinItem,
			/* [in] */ int ichLimItem,
			/* [in] */ VwConcParaOpts cpoFlags,
			/* [in] */ int dmpAlign);

		HRESULT ( STDMETHODCALLTYPE *OpenOverridePara )(
			IVwEnv * This,
			/* [in] */ int cOverrideProperties,
			/* [size_is][in] */ DispPropOverride *prgOverrideProperties);

		HRESULT ( STDMETHODCALLTYPE *CloseParagraph )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenInnerPile )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *CloseInnerPile )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenSpan )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *CloseSpan )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenTable )(
			IVwEnv * This,
			/* [in] */ int cCols,
			/* [out][in] */ VwLength *pvlWidth,
			/* [in] */ int mpBorder,
			/* [in] */ VwAlignment vwalign,
			/* [in] */ VwFramePosition frmpos,
			/* [in] */ VwRule vwrule,
			/* [in] */ int mpSpacing,
			/* [in] */ int mpPadding);

		HRESULT ( STDMETHODCALLTYPE *CloseTable )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenTableRow )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *CloseTableRow )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenTableCell )(
			IVwEnv * This,
			/* [in] */ int nRowSpan,
			/* [in] */ int nColSpan);

		HRESULT ( STDMETHODCALLTYPE *CloseTableCell )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenTableHeaderCell )(
			IVwEnv * This,
			/* [in] */ int nRowSpan,
			/* [in] */ int nColSpan);

		HRESULT ( STDMETHODCALLTYPE *CloseTableHeaderCell )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *MakeColumns )(
			IVwEnv * This,
			/* [in] */ int nColSpan,
			/* [in] */ VwLength vlWidth);

		HRESULT ( STDMETHODCALLTYPE *MakeColumnGroup )(
			IVwEnv * This,
			/* [in] */ int nColSpan,
			/* [in] */ VwLength vlWidth);

		HRESULT ( STDMETHODCALLTYPE *OpenTableHeader )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *CloseTableHeader )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenTableFooter )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *CloseTableFooter )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *OpenTableBody )(
			IVwEnv * This);

		HRESULT ( STDMETHODCALLTYPE *CloseTableBody )(
			IVwEnv * This);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IntProperty )(
			IVwEnv * This,
			/* [in] */ int tpt,
			/* [in] */ int tpv,
			/* [in] */ int nValue);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_StringProperty )(
			IVwEnv * This,
			/* [in] */ int sp,
			/* [in] */ BSTR bstrValue);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Props )(
			IVwEnv * This,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringWidth )(
			IVwEnv * This,
			/* [in] */ /* external definition not present */ ITsString *ptss,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp,
			/* [out] */ int *dmpx,
			/* [out] */ int *dmpy);

		HRESULT ( STDMETHODCALLTYPE *AddPicture )(
			IVwEnv * This,
			/* [in] */ IPicture *ppict,
			/* [in] */ int tag);

		END_INTERFACE
	} IVwEnvVtbl;

	interface IVwEnv
	{
		CONST_VTBL struct IVwEnvVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwEnv_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwEnv_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwEnv_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwEnv_AddObjProp(This,tag,pvwvc,frag)	\
	(This)->lpVtbl -> AddObjProp(This,tag,pvwvc,frag)

#define IVwEnv_AddObjVec(This,tag,pvwvc,frag)	\
	(This)->lpVtbl -> AddObjVec(This,tag,pvwvc,frag)

#define IVwEnv_AddObjVecItems(This,tag,pvwvc,frag)	\
	(This)->lpVtbl -> AddObjVecItems(This,tag,pvwvc,frag)

#define IVwEnv_AddObj(This,hvo,pvwvc,frag)	\
	(This)->lpVtbl -> AddObj(This,hvo,pvwvc,frag)

#define IVwEnv_AddLazyVecItems(This,tag,pvwvc,frag)	\
	(This)->lpVtbl -> AddLazyVecItems(This,tag,pvwvc,frag)

#define IVwEnv_AddLazyItems(This,prghvo,chvo,pvwvc,frag)	\
	(This)->lpVtbl -> AddLazyItems(This,prghvo,chvo,pvwvc,frag)

#define IVwEnv_AddProp(This,tag,pvwvc,frag)	\
	(This)->lpVtbl -> AddProp(This,tag,pvwvc,frag)

#define IVwEnv_AddDerivedProp(This,prgtag,ctag,pvwvc,frag)	\
	(This)->lpVtbl -> AddDerivedProp(This,prgtag,ctag,pvwvc,frag)

#define IVwEnv_NoteDependency(This,prghvo,prgtag,chvo)	\
	(This)->lpVtbl -> NoteDependency(This,prghvo,prgtag,chvo)

#define IVwEnv_AddStringProp(This,tag,pvwvc)	\
	(This)->lpVtbl -> AddStringProp(This,tag,pvwvc)

#define IVwEnv_AddUnicodeProp(This,tag,ws,pvwvc)	\
	(This)->lpVtbl -> AddUnicodeProp(This,tag,ws,pvwvc)

#define IVwEnv_AddIntProp(This,tag)	\
	(This)->lpVtbl -> AddIntProp(This,tag)

#define IVwEnv_AddIntPropPic(This,tag,pvc,frag,nMin,nMax)	\
	(This)->lpVtbl -> AddIntPropPic(This,tag,pvc,frag,nMin,nMax)

#define IVwEnv_AddStringAltMember(This,tag,ws,pvwvc)	\
	(This)->lpVtbl -> AddStringAltMember(This,tag,ws,pvwvc)

#define IVwEnv_AddStringAlt(This,tag)	\
	(This)->lpVtbl -> AddStringAlt(This,tag)

#define IVwEnv_AddStringAltSeq(This,tag,prgenc,cws)	\
	(This)->lpVtbl -> AddStringAltSeq(This,tag,prgenc,cws)

#define IVwEnv_AddString(This,pss)	\
	(This)->lpVtbl -> AddString(This,pss)

#define IVwEnv_AddTimeProp(This,tag,flags)	\
	(This)->lpVtbl -> AddTimeProp(This,tag,flags)

#define IVwEnv_AddGenDateProp(This,tag)	\
	(This)->lpVtbl -> AddGenDateProp(This,tag)

#define IVwEnv_CurrentObject(This,phvo)	\
	(This)->lpVtbl -> CurrentObject(This,phvo)

#define IVwEnv_get_OpenObject(This,phvoRet)	\
	(This)->lpVtbl -> get_OpenObject(This,phvoRet)

#define IVwEnv_get_EmbeddingLevel(This,pchvo)	\
	(This)->lpVtbl -> get_EmbeddingLevel(This,pchvo)

#define IVwEnv_GetOuterObject(This,ichvoLevel,phvo,ptag,pihvo)	\
	(This)->lpVtbl -> GetOuterObject(This,ichvoLevel,phvo,ptag,pihvo)

#define IVwEnv_get_DataAccess(This,ppsda)	\
	(This)->lpVtbl -> get_DataAccess(This,ppsda)

#define IVwEnv_AddWindow(This,pew,dmpAscent,fJustifyRight,fAutoShow)	\
	(This)->lpVtbl -> AddWindow(This,pew,dmpAscent,fJustifyRight,fAutoShow)

#define IVwEnv_AddSeparatorBar(This)	\
	(This)->lpVtbl -> AddSeparatorBar(This)

#define IVwEnv_AddSimpleRect(This,rgb,dmpWidth,dmpHeight,dmpBaselineOffset)	\
	(This)->lpVtbl -> AddSimpleRect(This,rgb,dmpWidth,dmpHeight,dmpBaselineOffset)

#define IVwEnv_OpenDiv(This)	\
	(This)->lpVtbl -> OpenDiv(This)

#define IVwEnv_CloseDiv(This)	\
	(This)->lpVtbl -> CloseDiv(This)

#define IVwEnv_OpenParagraph(This)	\
	(This)->lpVtbl -> OpenParagraph(This)

#define IVwEnv_OpenTaggedPara(This)	\
	(This)->lpVtbl -> OpenTaggedPara(This)

#define IVwEnv_OpenMappedPara(This)	\
	(This)->lpVtbl -> OpenMappedPara(This)

#define IVwEnv_OpenMappedTaggedPara(This)	\
	(This)->lpVtbl -> OpenMappedTaggedPara(This)

#define IVwEnv_OpenConcPara(This,ichMinItem,ichLimItem,cpoFlags,dmpAlign)	\
	(This)->lpVtbl -> OpenConcPara(This,ichMinItem,ichLimItem,cpoFlags,dmpAlign)

#define IVwEnv_OpenOverridePara(This,cOverrideProperties,prgOverrideProperties)	\
	(This)->lpVtbl -> OpenOverridePara(This,cOverrideProperties,prgOverrideProperties)

#define IVwEnv_CloseParagraph(This)	\
	(This)->lpVtbl -> CloseParagraph(This)

#define IVwEnv_OpenInnerPile(This)	\
	(This)->lpVtbl -> OpenInnerPile(This)

#define IVwEnv_CloseInnerPile(This)	\
	(This)->lpVtbl -> CloseInnerPile(This)

#define IVwEnv_OpenSpan(This)	\
	(This)->lpVtbl -> OpenSpan(This)

#define IVwEnv_CloseSpan(This)	\
	(This)->lpVtbl -> CloseSpan(This)

#define IVwEnv_OpenTable(This,cCols,pvlWidth,mpBorder,vwalign,frmpos,vwrule,mpSpacing,mpPadding)	\
	(This)->lpVtbl -> OpenTable(This,cCols,pvlWidth,mpBorder,vwalign,frmpos,vwrule,mpSpacing,mpPadding)

#define IVwEnv_CloseTable(This)	\
	(This)->lpVtbl -> CloseTable(This)

#define IVwEnv_OpenTableRow(This)	\
	(This)->lpVtbl -> OpenTableRow(This)

#define IVwEnv_CloseTableRow(This)	\
	(This)->lpVtbl -> CloseTableRow(This)

#define IVwEnv_OpenTableCell(This,nRowSpan,nColSpan)	\
	(This)->lpVtbl -> OpenTableCell(This,nRowSpan,nColSpan)

#define IVwEnv_CloseTableCell(This)	\
	(This)->lpVtbl -> CloseTableCell(This)

#define IVwEnv_OpenTableHeaderCell(This,nRowSpan,nColSpan)	\
	(This)->lpVtbl -> OpenTableHeaderCell(This,nRowSpan,nColSpan)

#define IVwEnv_CloseTableHeaderCell(This)	\
	(This)->lpVtbl -> CloseTableHeaderCell(This)

#define IVwEnv_MakeColumns(This,nColSpan,vlWidth)	\
	(This)->lpVtbl -> MakeColumns(This,nColSpan,vlWidth)

#define IVwEnv_MakeColumnGroup(This,nColSpan,vlWidth)	\
	(This)->lpVtbl -> MakeColumnGroup(This,nColSpan,vlWidth)

#define IVwEnv_OpenTableHeader(This)	\
	(This)->lpVtbl -> OpenTableHeader(This)

#define IVwEnv_CloseTableHeader(This)	\
	(This)->lpVtbl -> CloseTableHeader(This)

#define IVwEnv_OpenTableFooter(This)	\
	(This)->lpVtbl -> OpenTableFooter(This)

#define IVwEnv_CloseTableFooter(This)	\
	(This)->lpVtbl -> CloseTableFooter(This)

#define IVwEnv_OpenTableBody(This)	\
	(This)->lpVtbl -> OpenTableBody(This)

#define IVwEnv_CloseTableBody(This)	\
	(This)->lpVtbl -> CloseTableBody(This)

#define IVwEnv_put_IntProperty(This,tpt,tpv,nValue)	\
	(This)->lpVtbl -> put_IntProperty(This,tpt,tpv,nValue)

#define IVwEnv_put_StringProperty(This,sp,bstrValue)	\
	(This)->lpVtbl -> put_StringProperty(This,sp,bstrValue)

#define IVwEnv_put_Props(This,pttp)	\
	(This)->lpVtbl -> put_Props(This,pttp)

#define IVwEnv_get_StringWidth(This,ptss,pttp,dmpx,dmpy)	\
	(This)->lpVtbl -> get_StringWidth(This,ptss,pttp,dmpx,dmpy)

#define IVwEnv_AddPicture(This,ppict,tag)	\
	(This)->lpVtbl -> AddPicture(This,ppict,tag)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwEnv_AddObjProp_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddObjProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddObjVec_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddObjVec_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddObjVecItems_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddObjVecItems_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddObj_Proxy(
	IVwEnv * This,
	/* [in] */ long hvo,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddObj_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddLazyVecItems_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddLazyVecItems_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddLazyItems_Proxy(
	IVwEnv * This,
	/* [size_is][in] */ long *prghvo,
	/* [in] */ int chvo,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddLazyItems_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddProp_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddDerivedProp_Proxy(
	IVwEnv * This,
	/* [size_is][in] */ int *prgtag,
	/* [in] */ int ctag,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwEnv_AddDerivedProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_NoteDependency_Proxy(
	IVwEnv * This,
	/* [size_is][in] */ long *prghvo,
	/* [size_is][in] */ int *prgtag,
	/* [in] */ int chvo);


void __RPC_STUB IVwEnv_NoteDependency_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddStringProp_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ IVwViewConstructor *pvwvc);


void __RPC_STUB IVwEnv_AddStringProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddUnicodeProp_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [in] */ IVwViewConstructor *pvwvc);


void __RPC_STUB IVwEnv_AddUnicodeProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddIntProp_Proxy(
	IVwEnv * This,
	/* [in] */ int tag);


void __RPC_STUB IVwEnv_AddIntProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddIntPropPic_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ IVwViewConstructor *pvc,
	/* [in] */ int frag,
	/* [in] */ int nMin,
	/* [in] */ int nMax);


void __RPC_STUB IVwEnv_AddIntPropPic_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddStringAltMember_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [in] */ IVwViewConstructor *pvwvc);


void __RPC_STUB IVwEnv_AddStringAltMember_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddStringAlt_Proxy(
	IVwEnv * This,
	/* [in] */ int tag);


void __RPC_STUB IVwEnv_AddStringAlt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddStringAltSeq_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [size_is][in] */ int *prgenc,
	/* [in] */ int cws);


void __RPC_STUB IVwEnv_AddStringAltSeq_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddString_Proxy(
	IVwEnv * This,
	/* [in] */ /* external definition not present */ ITsString *pss);


void __RPC_STUB IVwEnv_AddString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddTimeProp_Proxy(
	IVwEnv * This,
	/* [in] */ int tag,
	/* [in] */ DWORD flags);


void __RPC_STUB IVwEnv_AddTimeProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddGenDateProp_Proxy(
	IVwEnv * This,
	/* [in] */ int tag);


void __RPC_STUB IVwEnv_AddGenDateProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CurrentObject_Proxy(
	IVwEnv * This,
	/* [retval][out] */ long *phvo);


void __RPC_STUB IVwEnv_CurrentObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwEnv_get_OpenObject_Proxy(
	IVwEnv * This,
	/* [retval][out] */ long *phvoRet);


void __RPC_STUB IVwEnv_get_OpenObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwEnv_get_EmbeddingLevel_Proxy(
	IVwEnv * This,
	/* [retval][out] */ int *pchvo);


void __RPC_STUB IVwEnv_get_EmbeddingLevel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_GetOuterObject_Proxy(
	IVwEnv * This,
	/* [in] */ int ichvoLevel,
	/* [out] */ long *phvo,
	/* [out] */ int *ptag,
	/* [out] */ int *pihvo);


void __RPC_STUB IVwEnv_GetOuterObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwEnv_get_DataAccess_Proxy(
	IVwEnv * This,
	/* [retval][out] */ ISilDataAccess **ppsda);


void __RPC_STUB IVwEnv_get_DataAccess_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddWindow_Proxy(
	IVwEnv * This,
	/* [in] */ IVwEmbeddedWindow *pew,
	/* [in] */ int dmpAscent,
	/* [in] */ ComBool fJustifyRight,
	/* [in] */ ComBool fAutoShow);


void __RPC_STUB IVwEnv_AddWindow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddSeparatorBar_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_AddSeparatorBar_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddSimpleRect_Proxy(
	IVwEnv * This,
	/* [in] */ COLORREF rgb,
	/* [in] */ int dmpWidth,
	/* [in] */ int dmpHeight,
	/* [in] */ int dmpBaselineOffset);


void __RPC_STUB IVwEnv_AddSimpleRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenDiv_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenDiv_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseDiv_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseDiv_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenParagraph_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenParagraph_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTaggedPara_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenTaggedPara_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenMappedPara_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenMappedPara_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenMappedTaggedPara_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenMappedTaggedPara_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenConcPara_Proxy(
	IVwEnv * This,
	/* [in] */ int ichMinItem,
	/* [in] */ int ichLimItem,
	/* [in] */ VwConcParaOpts cpoFlags,
	/* [in] */ int dmpAlign);


void __RPC_STUB IVwEnv_OpenConcPara_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenOverridePara_Proxy(
	IVwEnv * This,
	/* [in] */ int cOverrideProperties,
	/* [size_is][in] */ DispPropOverride *prgOverrideProperties);


void __RPC_STUB IVwEnv_OpenOverridePara_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseParagraph_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseParagraph_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenInnerPile_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenInnerPile_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseInnerPile_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseInnerPile_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenSpan_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenSpan_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseSpan_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseSpan_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTable_Proxy(
	IVwEnv * This,
	/* [in] */ int cCols,
	/* [out][in] */ VwLength *pvlWidth,
	/* [in] */ int mpBorder,
	/* [in] */ VwAlignment vwalign,
	/* [in] */ VwFramePosition frmpos,
	/* [in] */ VwRule vwrule,
	/* [in] */ int mpSpacing,
	/* [in] */ int mpPadding);


void __RPC_STUB IVwEnv_OpenTable_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseTable_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseTable_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTableRow_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenTableRow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseTableRow_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseTableRow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTableCell_Proxy(
	IVwEnv * This,
	/* [in] */ int nRowSpan,
	/* [in] */ int nColSpan);


void __RPC_STUB IVwEnv_OpenTableCell_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseTableCell_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseTableCell_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTableHeaderCell_Proxy(
	IVwEnv * This,
	/* [in] */ int nRowSpan,
	/* [in] */ int nColSpan);


void __RPC_STUB IVwEnv_OpenTableHeaderCell_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseTableHeaderCell_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseTableHeaderCell_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_MakeColumns_Proxy(
	IVwEnv * This,
	/* [in] */ int nColSpan,
	/* [in] */ VwLength vlWidth);


void __RPC_STUB IVwEnv_MakeColumns_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_MakeColumnGroup_Proxy(
	IVwEnv * This,
	/* [in] */ int nColSpan,
	/* [in] */ VwLength vlWidth);


void __RPC_STUB IVwEnv_MakeColumnGroup_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTableHeader_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenTableHeader_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseTableHeader_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseTableHeader_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTableFooter_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenTableFooter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseTableFooter_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseTableFooter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_OpenTableBody_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_OpenTableBody_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_CloseTableBody_Proxy(
	IVwEnv * This);


void __RPC_STUB IVwEnv_CloseTableBody_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwEnv_put_IntProperty_Proxy(
	IVwEnv * This,
	/* [in] */ int tpt,
	/* [in] */ int tpv,
	/* [in] */ int nValue);


void __RPC_STUB IVwEnv_put_IntProperty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwEnv_put_StringProperty_Proxy(
	IVwEnv * This,
	/* [in] */ int sp,
	/* [in] */ BSTR bstrValue);


void __RPC_STUB IVwEnv_put_StringProperty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwEnv_put_Props_Proxy(
	IVwEnv * This,
	/* [in] */ /* external definition not present */ ITsTextProps *pttp);


void __RPC_STUB IVwEnv_put_Props_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwEnv_get_StringWidth_Proxy(
	IVwEnv * This,
	/* [in] */ /* external definition not present */ ITsString *ptss,
	/* [in] */ /* external definition not present */ ITsTextProps *pttp,
	/* [out] */ int *dmpx,
	/* [out] */ int *dmpy);


void __RPC_STUB IVwEnv_get_StringWidth_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwEnv_AddPicture_Proxy(
	IVwEnv * This,
	/* [in] */ IPicture *ppict,
	/* [in] */ int tag);


void __RPC_STUB IVwEnv_AddPicture_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwEnv_INTERFACE_DEFINED__ */


#ifndef __IVwViewConstructor_INTERFACE_DEFINED__
#define __IVwViewConstructor_INTERFACE_DEFINED__

/* interface IVwViewConstructor */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwViewConstructor;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("EE103481-48BB-11d3-8078-0000C0FB81B5")
	IVwViewConstructor : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Display(
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ long hvo,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE DisplayVec(
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE DisplayVariant(
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ int tag,
			/* [in] */ VARIANT v,
			/* [in] */ int frag,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE DisplayPicture(
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ int hvo,
			/* [in] */ int tag,
			/* [in] */ int val,
			/* [in] */ int frag,
			/* [retval][out] */ IPicture **ppPict) = 0;

		virtual HRESULT STDMETHODCALLTYPE UpdateProp(
			/* [in] */ IVwSelection *pvwsel,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int frag,
			/* [in] */ /* external definition not present */ ITsString *ptssVal,
			/* [retval][out] */ /* external definition not present */ ITsString **pptssRepVal) = 0;

		virtual HRESULT STDMETHODCALLTYPE EstimateHeight(
			/* [in] */ long hvo,
			/* [in] */ int frag,
			/* [in] */ int dxAvailWidth,
			/* [retval][out] */ int *pdyHeight) = 0;

		virtual HRESULT STDMETHODCALLTYPE LoadDataFor(
			/* [in] */ IVwEnv *pvwenv,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo,
			/* [in] */ long hvoParent,
			/* [in] */ int tag,
			/* [in] */ int frag,
			/* [in] */ int ihvoMin) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetStrForGuid(
			/* [in] */ BSTR bstrGuid,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE DoHotLinkAction(
			/* [in] */ BSTR bstrData,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ /* external definition not present */ ITsString *ptss,
			/* [in] */ int ichObj) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetIdFromGuid(
			/* [in] */ ISilDataAccess *psda,
			/* [in] */ GUID *puid,
			/* [retval][out] */ long *phvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE DisplayEmbeddedObject(
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ long hvo) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwViewConstructorVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwViewConstructor * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwViewConstructor * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwViewConstructor * This);

		HRESULT ( STDMETHODCALLTYPE *Display )(
			IVwViewConstructor * This,
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ long hvo,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *DisplayVec )(
			IVwViewConstructor * This,
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *DisplayVariant )(
			IVwViewConstructor * This,
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ int tag,
			/* [in] */ VARIANT v,
			/* [in] */ int frag,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *DisplayPicture )(
			IVwViewConstructor * This,
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ int hvo,
			/* [in] */ int tag,
			/* [in] */ int val,
			/* [in] */ int frag,
			/* [retval][out] */ IPicture **ppPict);

		HRESULT ( STDMETHODCALLTYPE *UpdateProp )(
			IVwViewConstructor * This,
			/* [in] */ IVwSelection *pvwsel,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int frag,
			/* [in] */ /* external definition not present */ ITsString *ptssVal,
			/* [retval][out] */ /* external definition not present */ ITsString **pptssRepVal);

		HRESULT ( STDMETHODCALLTYPE *EstimateHeight )(
			IVwViewConstructor * This,
			/* [in] */ long hvo,
			/* [in] */ int frag,
			/* [in] */ int dxAvailWidth,
			/* [retval][out] */ int *pdyHeight);

		HRESULT ( STDMETHODCALLTYPE *LoadDataFor )(
			IVwViewConstructor * This,
			/* [in] */ IVwEnv *pvwenv,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo,
			/* [in] */ long hvoParent,
			/* [in] */ int tag,
			/* [in] */ int frag,
			/* [in] */ int ihvoMin);

		HRESULT ( STDMETHODCALLTYPE *GetStrForGuid )(
			IVwViewConstructor * This,
			/* [in] */ BSTR bstrGuid,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *DoHotLinkAction )(
			IVwViewConstructor * This,
			/* [in] */ BSTR bstrData,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ /* external definition not present */ ITsString *ptss,
			/* [in] */ int ichObj);

		HRESULT ( STDMETHODCALLTYPE *GetIdFromGuid )(
			IVwViewConstructor * This,
			/* [in] */ ISilDataAccess *psda,
			/* [in] */ GUID *puid,
			/* [retval][out] */ long *phvo);

		HRESULT ( STDMETHODCALLTYPE *DisplayEmbeddedObject )(
			IVwViewConstructor * This,
			/* [in] */ IVwEnv *pvwenv,
			/* [in] */ long hvo);

		END_INTERFACE
	} IVwViewConstructorVtbl;

	interface IVwViewConstructor
	{
		CONST_VTBL struct IVwViewConstructorVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwViewConstructor_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwViewConstructor_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwViewConstructor_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwViewConstructor_Display(This,pvwenv,hvo,frag)	\
	(This)->lpVtbl -> Display(This,pvwenv,hvo,frag)

#define IVwViewConstructor_DisplayVec(This,pvwenv,hvo,tag,frag)	\
	(This)->lpVtbl -> DisplayVec(This,pvwenv,hvo,tag,frag)

#define IVwViewConstructor_DisplayVariant(This,pvwenv,tag,v,frag,pptss)	\
	(This)->lpVtbl -> DisplayVariant(This,pvwenv,tag,v,frag,pptss)

#define IVwViewConstructor_DisplayPicture(This,pvwenv,hvo,tag,val,frag,ppPict)	\
	(This)->lpVtbl -> DisplayPicture(This,pvwenv,hvo,tag,val,frag,ppPict)

#define IVwViewConstructor_UpdateProp(This,pvwsel,hvo,tag,frag,ptssVal,pptssRepVal)	\
	(This)->lpVtbl -> UpdateProp(This,pvwsel,hvo,tag,frag,ptssVal,pptssRepVal)

#define IVwViewConstructor_EstimateHeight(This,hvo,frag,dxAvailWidth,pdyHeight)	\
	(This)->lpVtbl -> EstimateHeight(This,hvo,frag,dxAvailWidth,pdyHeight)

#define IVwViewConstructor_LoadDataFor(This,pvwenv,prghvo,chvo,hvoParent,tag,frag,ihvoMin)	\
	(This)->lpVtbl -> LoadDataFor(This,pvwenv,prghvo,chvo,hvoParent,tag,frag,ihvoMin)

#define IVwViewConstructor_GetStrForGuid(This,bstrGuid,pptss)	\
	(This)->lpVtbl -> GetStrForGuid(This,bstrGuid,pptss)

#define IVwViewConstructor_DoHotLinkAction(This,bstrData,hvoOwner,tag,ptss,ichObj)	\
	(This)->lpVtbl -> DoHotLinkAction(This,bstrData,hvoOwner,tag,ptss,ichObj)

#define IVwViewConstructor_GetIdFromGuid(This,psda,puid,phvo)	\
	(This)->lpVtbl -> GetIdFromGuid(This,psda,puid,phvo)

#define IVwViewConstructor_DisplayEmbeddedObject(This,pvwenv,hvo)	\
	(This)->lpVtbl -> DisplayEmbeddedObject(This,pvwenv,hvo)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwViewConstructor_Display_Proxy(
	IVwViewConstructor * This,
	/* [in] */ IVwEnv *pvwenv,
	/* [in] */ long hvo,
	/* [in] */ int frag);


void __RPC_STUB IVwViewConstructor_Display_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_DisplayVec_Proxy(
	IVwViewConstructor * This,
	/* [in] */ IVwEnv *pvwenv,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int frag);


void __RPC_STUB IVwViewConstructor_DisplayVec_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_DisplayVariant_Proxy(
	IVwViewConstructor * This,
	/* [in] */ IVwEnv *pvwenv,
	/* [in] */ int tag,
	/* [in] */ VARIANT v,
	/* [in] */ int frag,
	/* [retval][out] */ /* external definition not present */ ITsString **pptss);


void __RPC_STUB IVwViewConstructor_DisplayVariant_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_DisplayPicture_Proxy(
	IVwViewConstructor * This,
	/* [in] */ IVwEnv *pvwenv,
	/* [in] */ int hvo,
	/* [in] */ int tag,
	/* [in] */ int val,
	/* [in] */ int frag,
	/* [retval][out] */ IPicture **ppPict);


void __RPC_STUB IVwViewConstructor_DisplayPicture_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_UpdateProp_Proxy(
	IVwViewConstructor * This,
	/* [in] */ IVwSelection *pvwsel,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int frag,
	/* [in] */ /* external definition not present */ ITsString *ptssVal,
	/* [retval][out] */ /* external definition not present */ ITsString **pptssRepVal);


void __RPC_STUB IVwViewConstructor_UpdateProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_EstimateHeight_Proxy(
	IVwViewConstructor * This,
	/* [in] */ long hvo,
	/* [in] */ int frag,
	/* [in] */ int dxAvailWidth,
	/* [retval][out] */ int *pdyHeight);


void __RPC_STUB IVwViewConstructor_EstimateHeight_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_LoadDataFor_Proxy(
	IVwViewConstructor * This,
	/* [in] */ IVwEnv *pvwenv,
	/* [size_is][in] */ long *prghvo,
	/* [in] */ int chvo,
	/* [in] */ long hvoParent,
	/* [in] */ int tag,
	/* [in] */ int frag,
	/* [in] */ int ihvoMin);


void __RPC_STUB IVwViewConstructor_LoadDataFor_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_GetStrForGuid_Proxy(
	IVwViewConstructor * This,
	/* [in] */ BSTR bstrGuid,
	/* [retval][out] */ /* external definition not present */ ITsString **pptss);


void __RPC_STUB IVwViewConstructor_GetStrForGuid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_DoHotLinkAction_Proxy(
	IVwViewConstructor * This,
	/* [in] */ BSTR bstrData,
	/* [in] */ long hvoOwner,
	/* [in] */ int tag,
	/* [in] */ /* external definition not present */ ITsString *ptss,
	/* [in] */ int ichObj);


void __RPC_STUB IVwViewConstructor_DoHotLinkAction_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_GetIdFromGuid_Proxy(
	IVwViewConstructor * This,
	/* [in] */ ISilDataAccess *psda,
	/* [in] */ GUID *puid,
	/* [retval][out] */ long *phvo);


void __RPC_STUB IVwViewConstructor_GetIdFromGuid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwViewConstructor_DisplayEmbeddedObject_Proxy(
	IVwViewConstructor * This,
	/* [in] */ IVwEnv *pvwenv,
	/* [in] */ long hvo);


void __RPC_STUB IVwViewConstructor_DisplayEmbeddedObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwViewConstructor_INTERFACE_DEFINED__ */


#ifndef __IVwRootSite_INTERFACE_DEFINED__
#define __IVwRootSite_INTERFACE_DEFINED__

/* interface IVwRootSite */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwRootSite;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("C999413C-28C8-481c-9543-B06C92B812D1")
	IVwRootSite : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE InvalidateRect(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ int xsLeft,
			/* [in] */ int ysTop,
			/* [in] */ int dxsWidth,
			/* [in] */ int dysHeight) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetGraphics(
			/* [in] */ IVwRootBox *pRoot,
			/* [out] */ /* external definition not present */ IVwGraphics **ppvg,
			/* [out] */ RECT *prcSrcRoot,
			/* [out] */ RECT *prcDstRoot) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LayoutGraphics(
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ScreenGraphics(
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetTransformAtDst(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ POINT pt,
			/* [out] */ RECT *prcSrcRoot,
			/* [out] */ RECT *prcDstRoot) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetTransformAtSrc(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ POINT pt,
			/* [out] */ RECT *prcSrcRoot,
			/* [out] */ RECT *prcDstRoot) = 0;

		virtual HRESULT STDMETHODCALLTYPE ReleaseGraphics(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetAvailWidth(
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ int *ptwWidth) = 0;

		virtual HRESULT STDMETHODCALLTYPE DoUpdates(
			/* [in] */ IVwRootBox *pRoot) = 0;

		virtual HRESULT STDMETHODCALLTYPE SizeChanged(
			/* [in] */ IVwRootBox *pRoot) = 0;

		virtual HRESULT STDMETHODCALLTYPE AdjustScrollRange(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ int dxdSize,
			/* [in] */ int dxdPosition,
			/* [in] */ int dydSize,
			/* [in] */ int dydPosition,
			/* [retval][out] */ ComBool *pfForcedScroll) = 0;

		virtual HRESULT STDMETHODCALLTYPE SelectionChanged(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ IVwSelection *pvwselNew) = 0;

		virtual HRESULT STDMETHODCALLTYPE OverlayChanged(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ IVwOverlay *pvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SemiTagging(
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ ComBool *pf) = 0;

		virtual HRESULT STDMETHODCALLTYPE ScreenToClient(
			/* [in] */ IVwRootBox *pRoot,
			/* [out][in] */ POINT *ppnt) = 0;

		virtual HRESULT STDMETHODCALLTYPE ClientToScreen(
			/* [in] */ IVwRootBox *pRoot,
			/* [out][in] */ POINT *ppnt) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetAndClearPendingWs(
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ int *pws) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsOkToMakeLazy(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ int ydTop,
			/* [in] */ int ydBottom,
			/* [retval][out] */ ComBool *pfOK) = 0;

		virtual HRESULT STDMETHODCALLTYPE OnProblemDeletion(
			/* [in] */ IVwSelection *psel,
			/* [in] */ VwDelProbType dpt,
			/* [retval][out] */ VwDelProbResponse *pdpr) = 0;

		virtual HRESULT STDMETHODCALLTYPE OnInsertDiffParas(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ /* external definition not present */ ITsTextProps *pttpDest,
			/* [in] */ int cPara,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpSrc,
			/* [size_is][in] */ /* external definition not present */ ITsString **prgptssSrc,
			/* [in] */ /* external definition not present */ ITsString *ptssTrailing,
			/* [retval][out] */ VwInsertDiffParaResponse *pidpr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TextRepOfObj(
			/* [in] */ GUID *pguid,
			/* [retval][out] */ BSTR *pbstrRep) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MakeObjFromText(
			/* [in] */ BSTR bstrText,
			/* [in] */ IVwSelection *pselDst,
			/* [out] */ int *podt,
			/* [retval][out] */ GUID *pGuid) = 0;

		virtual HRESULT STDMETHODCALLTYPE ScrollSelectionIntoView(
			/* [in] */ IVwSelection *psel,
			/* [in] */ VwScrollSelOpts ssoFlag) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RootBox(
			/* [retval][out] */ IVwRootBox **prootb) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Hwnd(
			/* [retval][out] */ DWORD *phwnd) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwRootSiteVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwRootSite * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwRootSite * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwRootSite * This);

		HRESULT ( STDMETHODCALLTYPE *InvalidateRect )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ int xsLeft,
			/* [in] */ int ysTop,
			/* [in] */ int dxsWidth,
			/* [in] */ int dysHeight);

		HRESULT ( STDMETHODCALLTYPE *GetGraphics )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [out] */ /* external definition not present */ IVwGraphics **ppvg,
			/* [out] */ RECT *prcSrcRoot,
			/* [out] */ RECT *prcDstRoot);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LayoutGraphics )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ScreenGraphics )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg);

		HRESULT ( STDMETHODCALLTYPE *GetTransformAtDst )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ POINT pt,
			/* [out] */ RECT *prcSrcRoot,
			/* [out] */ RECT *prcDstRoot);

		HRESULT ( STDMETHODCALLTYPE *GetTransformAtSrc )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ POINT pt,
			/* [out] */ RECT *prcSrcRoot,
			/* [out] */ RECT *prcDstRoot);

		HRESULT ( STDMETHODCALLTYPE *ReleaseGraphics )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg);

		HRESULT ( STDMETHODCALLTYPE *GetAvailWidth )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ int *ptwWidth);

		HRESULT ( STDMETHODCALLTYPE *DoUpdates )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot);

		HRESULT ( STDMETHODCALLTYPE *SizeChanged )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot);

		HRESULT ( STDMETHODCALLTYPE *AdjustScrollRange )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ int dxdSize,
			/* [in] */ int dxdPosition,
			/* [in] */ int dydSize,
			/* [in] */ int dydPosition,
			/* [retval][out] */ ComBool *pfForcedScroll);

		HRESULT ( STDMETHODCALLTYPE *SelectionChanged )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ IVwSelection *pvwselNew);

		HRESULT ( STDMETHODCALLTYPE *OverlayChanged )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ IVwOverlay *pvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SemiTagging )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ ComBool *pf);

		HRESULT ( STDMETHODCALLTYPE *ScreenToClient )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [out][in] */ POINT *ppnt);

		HRESULT ( STDMETHODCALLTYPE *ClientToScreen )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [out][in] */ POINT *ppnt);

		HRESULT ( STDMETHODCALLTYPE *GetAndClearPendingWs )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [retval][out] */ int *pws);

		HRESULT ( STDMETHODCALLTYPE *IsOkToMakeLazy )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ int ydTop,
			/* [in] */ int ydBottom,
			/* [retval][out] */ ComBool *pfOK);

		HRESULT ( STDMETHODCALLTYPE *OnProblemDeletion )(
			IVwRootSite * This,
			/* [in] */ IVwSelection *psel,
			/* [in] */ VwDelProbType dpt,
			/* [retval][out] */ VwDelProbResponse *pdpr);

		HRESULT ( STDMETHODCALLTYPE *OnInsertDiffParas )(
			IVwRootSite * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ /* external definition not present */ ITsTextProps *pttpDest,
			/* [in] */ int cPara,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpSrc,
			/* [size_is][in] */ /* external definition not present */ ITsString **prgptssSrc,
			/* [in] */ /* external definition not present */ ITsString *ptssTrailing,
			/* [retval][out] */ VwInsertDiffParaResponse *pidpr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TextRepOfObj )(
			IVwRootSite * This,
			/* [in] */ GUID *pguid,
			/* [retval][out] */ BSTR *pbstrRep);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MakeObjFromText )(
			IVwRootSite * This,
			/* [in] */ BSTR bstrText,
			/* [in] */ IVwSelection *pselDst,
			/* [out] */ int *podt,
			/* [retval][out] */ GUID *pGuid);

		HRESULT ( STDMETHODCALLTYPE *ScrollSelectionIntoView )(
			IVwRootSite * This,
			/* [in] */ IVwSelection *psel,
			/* [in] */ VwScrollSelOpts ssoFlag);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RootBox )(
			IVwRootSite * This,
			/* [retval][out] */ IVwRootBox **prootb);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Hwnd )(
			IVwRootSite * This,
			/* [retval][out] */ DWORD *phwnd);

		END_INTERFACE
	} IVwRootSiteVtbl;

	interface IVwRootSite
	{
		CONST_VTBL struct IVwRootSiteVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwRootSite_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwRootSite_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwRootSite_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwRootSite_InvalidateRect(This,pRoot,xsLeft,ysTop,dxsWidth,dysHeight)	\
	(This)->lpVtbl -> InvalidateRect(This,pRoot,xsLeft,ysTop,dxsWidth,dysHeight)

#define IVwRootSite_GetGraphics(This,pRoot,ppvg,prcSrcRoot,prcDstRoot)	\
	(This)->lpVtbl -> GetGraphics(This,pRoot,ppvg,prcSrcRoot,prcDstRoot)

#define IVwRootSite_get_LayoutGraphics(This,pRoot,ppvg)	\
	(This)->lpVtbl -> get_LayoutGraphics(This,pRoot,ppvg)

#define IVwRootSite_get_ScreenGraphics(This,pRoot,ppvg)	\
	(This)->lpVtbl -> get_ScreenGraphics(This,pRoot,ppvg)

#define IVwRootSite_GetTransformAtDst(This,pRoot,pt,prcSrcRoot,prcDstRoot)	\
	(This)->lpVtbl -> GetTransformAtDst(This,pRoot,pt,prcSrcRoot,prcDstRoot)

#define IVwRootSite_GetTransformAtSrc(This,pRoot,pt,prcSrcRoot,prcDstRoot)	\
	(This)->lpVtbl -> GetTransformAtSrc(This,pRoot,pt,prcSrcRoot,prcDstRoot)

#define IVwRootSite_ReleaseGraphics(This,pRoot,pvg)	\
	(This)->lpVtbl -> ReleaseGraphics(This,pRoot,pvg)

#define IVwRootSite_GetAvailWidth(This,pRoot,ptwWidth)	\
	(This)->lpVtbl -> GetAvailWidth(This,pRoot,ptwWidth)

#define IVwRootSite_DoUpdates(This,pRoot)	\
	(This)->lpVtbl -> DoUpdates(This,pRoot)

#define IVwRootSite_SizeChanged(This,pRoot)	\
	(This)->lpVtbl -> SizeChanged(This,pRoot)

#define IVwRootSite_AdjustScrollRange(This,pRoot,dxdSize,dxdPosition,dydSize,dydPosition,pfForcedScroll)	\
	(This)->lpVtbl -> AdjustScrollRange(This,pRoot,dxdSize,dxdPosition,dydSize,dydPosition,pfForcedScroll)

#define IVwRootSite_SelectionChanged(This,pRoot,pvwselNew)	\
	(This)->lpVtbl -> SelectionChanged(This,pRoot,pvwselNew)

#define IVwRootSite_OverlayChanged(This,pRoot,pvo)	\
	(This)->lpVtbl -> OverlayChanged(This,pRoot,pvo)

#define IVwRootSite_get_SemiTagging(This,pRoot,pf)	\
	(This)->lpVtbl -> get_SemiTagging(This,pRoot,pf)

#define IVwRootSite_ScreenToClient(This,pRoot,ppnt)	\
	(This)->lpVtbl -> ScreenToClient(This,pRoot,ppnt)

#define IVwRootSite_ClientToScreen(This,pRoot,ppnt)	\
	(This)->lpVtbl -> ClientToScreen(This,pRoot,ppnt)

#define IVwRootSite_GetAndClearPendingWs(This,pRoot,pws)	\
	(This)->lpVtbl -> GetAndClearPendingWs(This,pRoot,pws)

#define IVwRootSite_IsOkToMakeLazy(This,pRoot,ydTop,ydBottom,pfOK)	\
	(This)->lpVtbl -> IsOkToMakeLazy(This,pRoot,ydTop,ydBottom,pfOK)

#define IVwRootSite_OnProblemDeletion(This,psel,dpt,pdpr)	\
	(This)->lpVtbl -> OnProblemDeletion(This,psel,dpt,pdpr)

#define IVwRootSite_OnInsertDiffParas(This,pRoot,pttpDest,cPara,prgpttpSrc,prgptssSrc,ptssTrailing,pidpr)	\
	(This)->lpVtbl -> OnInsertDiffParas(This,pRoot,pttpDest,cPara,prgpttpSrc,prgptssSrc,ptssTrailing,pidpr)

#define IVwRootSite_get_TextRepOfObj(This,pguid,pbstrRep)	\
	(This)->lpVtbl -> get_TextRepOfObj(This,pguid,pbstrRep)

#define IVwRootSite_get_MakeObjFromText(This,bstrText,pselDst,podt,pGuid)	\
	(This)->lpVtbl -> get_MakeObjFromText(This,bstrText,pselDst,podt,pGuid)

#define IVwRootSite_ScrollSelectionIntoView(This,psel,ssoFlag)	\
	(This)->lpVtbl -> ScrollSelectionIntoView(This,psel,ssoFlag)

#define IVwRootSite_get_RootBox(This,prootb)	\
	(This)->lpVtbl -> get_RootBox(This,prootb)

#define IVwRootSite_get_Hwnd(This,phwnd)	\
	(This)->lpVtbl -> get_Hwnd(This,phwnd)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwRootSite_InvalidateRect_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ int xsLeft,
	/* [in] */ int ysTop,
	/* [in] */ int dxsWidth,
	/* [in] */ int dysHeight);


void __RPC_STUB IVwRootSite_InvalidateRect_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_GetGraphics_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [out] */ /* external definition not present */ IVwGraphics **ppvg,
	/* [out] */ RECT *prcSrcRoot,
	/* [out] */ RECT *prcDstRoot);


void __RPC_STUB IVwRootSite_GetGraphics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootSite_get_LayoutGraphics_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg);


void __RPC_STUB IVwRootSite_get_LayoutGraphics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootSite_get_ScreenGraphics_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg);


void __RPC_STUB IVwRootSite_get_ScreenGraphics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_GetTransformAtDst_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ POINT pt,
	/* [out] */ RECT *prcSrcRoot,
	/* [out] */ RECT *prcDstRoot);


void __RPC_STUB IVwRootSite_GetTransformAtDst_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_GetTransformAtSrc_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ POINT pt,
	/* [out] */ RECT *prcSrcRoot,
	/* [out] */ RECT *prcDstRoot);


void __RPC_STUB IVwRootSite_GetTransformAtSrc_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_ReleaseGraphics_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg);


void __RPC_STUB IVwRootSite_ReleaseGraphics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_GetAvailWidth_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [retval][out] */ int *ptwWidth);


void __RPC_STUB IVwRootSite_GetAvailWidth_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_DoUpdates_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot);


void __RPC_STUB IVwRootSite_DoUpdates_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_SizeChanged_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot);


void __RPC_STUB IVwRootSite_SizeChanged_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_AdjustScrollRange_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ int dxdSize,
	/* [in] */ int dxdPosition,
	/* [in] */ int dydSize,
	/* [in] */ int dydPosition,
	/* [retval][out] */ ComBool *pfForcedScroll);


void __RPC_STUB IVwRootSite_AdjustScrollRange_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_SelectionChanged_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ IVwSelection *pvwselNew);


void __RPC_STUB IVwRootSite_SelectionChanged_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_OverlayChanged_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ IVwOverlay *pvo);


void __RPC_STUB IVwRootSite_OverlayChanged_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootSite_get_SemiTagging_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [retval][out] */ ComBool *pf);


void __RPC_STUB IVwRootSite_get_SemiTagging_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_ScreenToClient_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [out][in] */ POINT *ppnt);


void __RPC_STUB IVwRootSite_ScreenToClient_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_ClientToScreen_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [out][in] */ POINT *ppnt);


void __RPC_STUB IVwRootSite_ClientToScreen_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_GetAndClearPendingWs_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [retval][out] */ int *pws);


void __RPC_STUB IVwRootSite_GetAndClearPendingWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_IsOkToMakeLazy_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ int ydTop,
	/* [in] */ int ydBottom,
	/* [retval][out] */ ComBool *pfOK);


void __RPC_STUB IVwRootSite_IsOkToMakeLazy_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_OnProblemDeletion_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwSelection *psel,
	/* [in] */ VwDelProbType dpt,
	/* [retval][out] */ VwDelProbResponse *pdpr);


void __RPC_STUB IVwRootSite_OnProblemDeletion_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_OnInsertDiffParas_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ /* external definition not present */ ITsTextProps *pttpDest,
	/* [in] */ int cPara,
	/* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpSrc,
	/* [size_is][in] */ /* external definition not present */ ITsString **prgptssSrc,
	/* [in] */ /* external definition not present */ ITsString *ptssTrailing,
	/* [retval][out] */ VwInsertDiffParaResponse *pidpr);


void __RPC_STUB IVwRootSite_OnInsertDiffParas_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootSite_get_TextRepOfObj_Proxy(
	IVwRootSite * This,
	/* [in] */ GUID *pguid,
	/* [retval][out] */ BSTR *pbstrRep);


void __RPC_STUB IVwRootSite_get_TextRepOfObj_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootSite_get_MakeObjFromText_Proxy(
	IVwRootSite * This,
	/* [in] */ BSTR bstrText,
	/* [in] */ IVwSelection *pselDst,
	/* [out] */ int *podt,
	/* [retval][out] */ GUID *pGuid);


void __RPC_STUB IVwRootSite_get_MakeObjFromText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootSite_ScrollSelectionIntoView_Proxy(
	IVwRootSite * This,
	/* [in] */ IVwSelection *psel,
	/* [in] */ VwScrollSelOpts ssoFlag);


void __RPC_STUB IVwRootSite_ScrollSelectionIntoView_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootSite_get_RootBox_Proxy(
	IVwRootSite * This,
	/* [retval][out] */ IVwRootBox **prootb);


void __RPC_STUB IVwRootSite_get_RootBox_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootSite_get_Hwnd_Proxy(
	IVwRootSite * This,
	/* [retval][out] */ DWORD *phwnd);


void __RPC_STUB IVwRootSite_get_Hwnd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwRootSite_INTERFACE_DEFINED__ */


#ifndef __IDbColSpec_INTERFACE_DEFINED__
#define __IDbColSpec_INTERFACE_DEFINED__

/* interface IDbColSpec */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IDbColSpec;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("A25318C8-EB1F-4f38-8E8D-80BF2849001B")
	IDbColSpec : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Clear( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Push(
			/* [in] */ int oct,
			/* [in] */ int icolBase,
			/* [in] */ int tag,
			/* [in] */ int ws) = 0;

		virtual HRESULT STDMETHODCALLTYPE Size(
			/* [out] */ int *pc) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetColInfo(
			/* [in] */ int iIndex,
			/* [out] */ int *poct,
			/* [out] */ int *picolBase,
			/* [out] */ int *ptag,
			/* [out] */ int *pws) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDbColType(
			/* [in] */ int iIndex,
			/* [out] */ int *poct) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetBaseCol(
			/* [in] */ int iIndex,
			/* [out] */ int *piBaseCol) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetTag(
			/* [in] */ int iIndex,
			/* [out] */ int *ptag) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetWs(
			/* [in] */ int iIndex,
			/* [out] */ int *pws) = 0;

	};

#else 	/* C style interface */

	typedef struct IDbColSpecVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IDbColSpec * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IDbColSpec * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IDbColSpec * This);

		HRESULT ( STDMETHODCALLTYPE *Clear )(
			IDbColSpec * This);

		HRESULT ( STDMETHODCALLTYPE *Push )(
			IDbColSpec * This,
			/* [in] */ int oct,
			/* [in] */ int icolBase,
			/* [in] */ int tag,
			/* [in] */ int ws);

		HRESULT ( STDMETHODCALLTYPE *Size )(
			IDbColSpec * This,
			/* [out] */ int *pc);

		HRESULT ( STDMETHODCALLTYPE *GetColInfo )(
			IDbColSpec * This,
			/* [in] */ int iIndex,
			/* [out] */ int *poct,
			/* [out] */ int *picolBase,
			/* [out] */ int *ptag,
			/* [out] */ int *pws);

		HRESULT ( STDMETHODCALLTYPE *GetDbColType )(
			IDbColSpec * This,
			/* [in] */ int iIndex,
			/* [out] */ int *poct);

		HRESULT ( STDMETHODCALLTYPE *GetBaseCol )(
			IDbColSpec * This,
			/* [in] */ int iIndex,
			/* [out] */ int *piBaseCol);

		HRESULT ( STDMETHODCALLTYPE *GetTag )(
			IDbColSpec * This,
			/* [in] */ int iIndex,
			/* [out] */ int *ptag);

		HRESULT ( STDMETHODCALLTYPE *GetWs )(
			IDbColSpec * This,
			/* [in] */ int iIndex,
			/* [out] */ int *pws);

		END_INTERFACE
	} IDbColSpecVtbl;

	interface IDbColSpec
	{
		CONST_VTBL struct IDbColSpecVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IDbColSpec_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDbColSpec_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IDbColSpec_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IDbColSpec_Clear(This)	\
	(This)->lpVtbl -> Clear(This)

#define IDbColSpec_Push(This,oct,icolBase,tag,ws)	\
	(This)->lpVtbl -> Push(This,oct,icolBase,tag,ws)

#define IDbColSpec_Size(This,pc)	\
	(This)->lpVtbl -> Size(This,pc)

#define IDbColSpec_GetColInfo(This,iIndex,poct,picolBase,ptag,pws)	\
	(This)->lpVtbl -> GetColInfo(This,iIndex,poct,picolBase,ptag,pws)

#define IDbColSpec_GetDbColType(This,iIndex,poct)	\
	(This)->lpVtbl -> GetDbColType(This,iIndex,poct)

#define IDbColSpec_GetBaseCol(This,iIndex,piBaseCol)	\
	(This)->lpVtbl -> GetBaseCol(This,iIndex,piBaseCol)

#define IDbColSpec_GetTag(This,iIndex,ptag)	\
	(This)->lpVtbl -> GetTag(This,iIndex,ptag)

#define IDbColSpec_GetWs(This,iIndex,pws)	\
	(This)->lpVtbl -> GetWs(This,iIndex,pws)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDbColSpec_Clear_Proxy(
	IDbColSpec * This);


void __RPC_STUB IDbColSpec_Clear_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbColSpec_Push_Proxy(
	IDbColSpec * This,
	/* [in] */ int oct,
	/* [in] */ int icolBase,
	/* [in] */ int tag,
	/* [in] */ int ws);


void __RPC_STUB IDbColSpec_Push_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbColSpec_Size_Proxy(
	IDbColSpec * This,
	/* [out] */ int *pc);


void __RPC_STUB IDbColSpec_Size_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbColSpec_GetColInfo_Proxy(
	IDbColSpec * This,
	/* [in] */ int iIndex,
	/* [out] */ int *poct,
	/* [out] */ int *picolBase,
	/* [out] */ int *ptag,
	/* [out] */ int *pws);


void __RPC_STUB IDbColSpec_GetColInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbColSpec_GetDbColType_Proxy(
	IDbColSpec * This,
	/* [in] */ int iIndex,
	/* [out] */ int *poct);


void __RPC_STUB IDbColSpec_GetDbColType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbColSpec_GetBaseCol_Proxy(
	IDbColSpec * This,
	/* [in] */ int iIndex,
	/* [out] */ int *piBaseCol);


void __RPC_STUB IDbColSpec_GetBaseCol_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbColSpec_GetTag_Proxy(
	IDbColSpec * This,
	/* [in] */ int iIndex,
	/* [out] */ int *ptag);


void __RPC_STUB IDbColSpec_GetTag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbColSpec_GetWs_Proxy(
	IDbColSpec * This,
	/* [in] */ int iIndex,
	/* [out] */ int *pws);


void __RPC_STUB IDbColSpec_GetWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IDbColSpec_INTERFACE_DEFINED__ */


#ifndef __ISilDataAccess_INTERFACE_DEFINED__
#define __ISilDataAccess_INTERFACE_DEFINED__

/* interface ISilDataAccess */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ISilDataAccess;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("88C81964-DB97-4cdc-A942-730CF1DF73A4")
	ISilDataAccess : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ObjectProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ long *phvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VecItem(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int index,
			/* [retval][out] */ long *phvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VecSize(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ int *pchvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VecSizeAssumeCached(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ int *pchvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE VecProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int chvoMax,
			/* [out] */ int *pchvo,
			/* [length_is][size_is][out] */ long *prghvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE BinaryPropRgb(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [length_is][size_is][out] */ byte *prgb,
			/* [in] */ int cbMax,
			/* [out] */ int *pcb) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_GuidProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ GUID *puid) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ int *pn) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Int64Prop(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ __int64 *plln) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MultiStringAlt(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MultiStringProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ /* external definition not present */ ITsMultiString **pptms) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Prop(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ VARIANT *pvar) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TimeProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ __int64 *ptim) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UnicodeProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_UnicodeProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ BSTR bstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE UnicodePropRgch(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [length_is][size_is][out] */ OLECHAR *prgch,
			/* [in] */ int cchMax,
			/* [out] */ int *pcch) = 0;

		virtual /* [propget][local] */ HRESULT STDMETHODCALLTYPE get_UnknownProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ REFIID iid,
			/* [retval][out] */ void **ppunk) = 0;

		virtual HRESULT STDMETHODCALLTYPE BeginUndoTask(
			/* [in] */ BSTR bstrUndo,
			/* [in] */ BSTR bstrRedo) = 0;

		virtual HRESULT STDMETHODCALLTYPE EndUndoTask( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE ContinueUndoTask( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE EndOuterUndoTask( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE BreakUndoTask(
			/* [in] */ BSTR bstrUndo,
			/* [in] */ BSTR bstrRedo) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetActionHandler(
			/* [retval][out] */ /* external definition not present */ IActionHandler **ppacth) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetActionHandler(
			/* [in] */ /* external definition not present */ IActionHandler *pacth) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeleteObj(
			/* [in] */ long hvoObj) = 0;

		virtual HRESULT STDMETHODCALLTYPE DeleteObjOwner(
			/* [in] */ long hvoOwner,
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE InsertNew(
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ int chvo,
			/* [in] */ IVwStylesheet *pss) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeNewObject(
			/* [in] */ int clid,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ int ord,
			/* [retval][out] */ long *phvoNew) = 0;

		virtual HRESULT STDMETHODCALLTYPE MoveOwnSeq(
			/* [in] */ long hvoSrcOwner,
			/* [in] */ int tagSrc,
			/* [in] */ int ihvoStart,
			/* [in] */ int ihvoEnd,
			/* [in] */ long hvoDstOwner,
			/* [in] */ int tagDst,
			/* [in] */ int ihvoDstStart) = 0;

		virtual HRESULT STDMETHODCALLTYPE Replace(
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvoMin,
			/* [in] */ int ihvoLim,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetObjProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ long hvoObj) = 0;

		virtual HRESULT STDMETHODCALLTYPE RemoveObjRefs(
			/* [in] */ long hvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetBinary(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [size_is][in] */ byte *prgb,
			/* [in] */ int cb) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetGuid(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ GUID uid) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetInt(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int n) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetInt64(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ __int64 lln) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetMultiStringAlt(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ /* external definition not present */ ITsString *ptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetString(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ /* external definition not present */ ITsString *ptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetTime(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ __int64 lln) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetUnicode(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetUnknown(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ IUnknown *punk) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddNotification(
			/* [in] */ IVwNotifyChange *pnchng) = 0;

		virtual HRESULT STDMETHODCALLTYPE PropChanged(
			/* [in] */ IVwNotifyChange *pnchng,
			/* [in] */ int pct,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ivMin,
			/* [in] */ int cvIns,
			/* [in] */ int cvDel) = 0;

		virtual HRESULT STDMETHODCALLTYPE RemoveNotification(
			/* [in] */ IVwNotifyChange *pnchng) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemFactory(
			/* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemsOfInterest(
			/* [in] */ int cwsMax,
			/* [size_is][out] */ int *pws,
			/* [retval][out] */ int *pcws) = 0;

		virtual HRESULT STDMETHODCALLTYPE InsertRelExtra(
			/* [in] */ long hvoSrc,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ long hvoDst,
			/* [in] */ BSTR bstrExtra) = 0;

		virtual HRESULT STDMETHODCALLTYPE UpdateRelExtra(
			/* [in] */ long hvoSrc,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ BSTR bstrExtra) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetRelExtra(
			/* [in] */ long hvoSrc,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [retval][out] */ BSTR *pbstrExtra) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsPropInCache(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int cpt,
			/* [in] */ int ws,
			/* [retval][out] */ ComBool *pfCached) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsDirty(
			/* [retval][out] */ ComBool *pf) = 0;

		virtual HRESULT STDMETHODCALLTYPE ClearDirty( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MetaDataCache(
			/* [retval][out] */ /* external definition not present */ IFwMetaDataCache **ppmdc) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_MetaDataCache(
			/* [in] */ /* external definition not present */ IFwMetaDataCache *pmdc) = 0;

	};

#else 	/* C style interface */

	typedef struct ISilDataAccessVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ISilDataAccess * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ISilDataAccess * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ISilDataAccess * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ObjectProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ long *phvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VecItem )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int index,
			/* [retval][out] */ long *phvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VecSize )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ int *pchvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VecSizeAssumeCached )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ int *pchvo);

		HRESULT ( STDMETHODCALLTYPE *VecProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int chvoMax,
			/* [out] */ int *pchvo,
			/* [length_is][size_is][out] */ long *prghvo);

		HRESULT ( STDMETHODCALLTYPE *BinaryPropRgb )(
			ISilDataAccess * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [length_is][size_is][out] */ byte *prgb,
			/* [in] */ int cbMax,
			/* [out] */ int *pcb);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_GuidProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ GUID *puid);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ int *pn);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Int64Prop )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ __int64 *plln);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MultiStringAlt )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MultiStringProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ /* external definition not present */ ITsMultiString **pptms);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Prop )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ VARIANT *pvar);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TimeProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [retval][out] */ __int64 *ptim);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UnicodeProp )(
			ISilDataAccess * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_UnicodeProp )(
			ISilDataAccess * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ BSTR bstr);

		HRESULT ( STDMETHODCALLTYPE *UnicodePropRgch )(
			ISilDataAccess * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [length_is][size_is][out] */ OLECHAR *prgch,
			/* [in] */ int cchMax,
			/* [out] */ int *pcch);

		/* [propget][local] */ HRESULT ( STDMETHODCALLTYPE *get_UnknownProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ REFIID iid,
			/* [retval][out] */ void **ppunk);

		HRESULT ( STDMETHODCALLTYPE *BeginUndoTask )(
			ISilDataAccess * This,
			/* [in] */ BSTR bstrUndo,
			/* [in] */ BSTR bstrRedo);

		HRESULT ( STDMETHODCALLTYPE *EndUndoTask )(
			ISilDataAccess * This);

		HRESULT ( STDMETHODCALLTYPE *ContinueUndoTask )(
			ISilDataAccess * This);

		HRESULT ( STDMETHODCALLTYPE *EndOuterUndoTask )(
			ISilDataAccess * This);

		HRESULT ( STDMETHODCALLTYPE *BreakUndoTask )(
			ISilDataAccess * This,
			/* [in] */ BSTR bstrUndo,
			/* [in] */ BSTR bstrRedo);

		HRESULT ( STDMETHODCALLTYPE *GetActionHandler )(
			ISilDataAccess * This,
			/* [retval][out] */ /* external definition not present */ IActionHandler **ppacth);

		HRESULT ( STDMETHODCALLTYPE *SetActionHandler )(
			ISilDataAccess * This,
			/* [in] */ /* external definition not present */ IActionHandler *pacth);

		HRESULT ( STDMETHODCALLTYPE *DeleteObj )(
			ISilDataAccess * This,
			/* [in] */ long hvoObj);

		HRESULT ( STDMETHODCALLTYPE *DeleteObjOwner )(
			ISilDataAccess * This,
			/* [in] */ long hvoOwner,
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvo);

		HRESULT ( STDMETHODCALLTYPE *InsertNew )(
			ISilDataAccess * This,
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ int chvo,
			/* [in] */ IVwStylesheet *pss);

		HRESULT ( STDMETHODCALLTYPE *MakeNewObject )(
			ISilDataAccess * This,
			/* [in] */ int clid,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ int ord,
			/* [retval][out] */ long *phvoNew);

		HRESULT ( STDMETHODCALLTYPE *MoveOwnSeq )(
			ISilDataAccess * This,
			/* [in] */ long hvoSrcOwner,
			/* [in] */ int tagSrc,
			/* [in] */ int ihvoStart,
			/* [in] */ int ihvoEnd,
			/* [in] */ long hvoDstOwner,
			/* [in] */ int tagDst,
			/* [in] */ int ihvoDstStart);

		HRESULT ( STDMETHODCALLTYPE *Replace )(
			ISilDataAccess * This,
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvoMin,
			/* [in] */ int ihvoLim,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo);

		HRESULT ( STDMETHODCALLTYPE *SetObjProp )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ long hvoObj);

		HRESULT ( STDMETHODCALLTYPE *RemoveObjRefs )(
			ISilDataAccess * This,
			/* [in] */ long hvo);

		HRESULT ( STDMETHODCALLTYPE *SetBinary )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [size_is][in] */ byte *prgb,
			/* [in] */ int cb);

		HRESULT ( STDMETHODCALLTYPE *SetGuid )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ GUID uid);

		HRESULT ( STDMETHODCALLTYPE *SetInt )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int n);

		HRESULT ( STDMETHODCALLTYPE *SetInt64 )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ __int64 lln);

		HRESULT ( STDMETHODCALLTYPE *SetMultiStringAlt )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ /* external definition not present */ ITsString *ptss);

		HRESULT ( STDMETHODCALLTYPE *SetString )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ /* external definition not present */ ITsString *ptss);

		HRESULT ( STDMETHODCALLTYPE *SetTime )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ __int64 lln);

		HRESULT ( STDMETHODCALLTYPE *SetUnicode )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch);

		HRESULT ( STDMETHODCALLTYPE *SetUnknown )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ IUnknown *punk);

		HRESULT ( STDMETHODCALLTYPE *AddNotification )(
			ISilDataAccess * This,
			/* [in] */ IVwNotifyChange *pnchng);

		HRESULT ( STDMETHODCALLTYPE *PropChanged )(
			ISilDataAccess * This,
			/* [in] */ IVwNotifyChange *pnchng,
			/* [in] */ int pct,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ivMin,
			/* [in] */ int cvIns,
			/* [in] */ int cvDel);

		HRESULT ( STDMETHODCALLTYPE *RemoveNotification )(
			ISilDataAccess * This,
			/* [in] */ IVwNotifyChange *pnchng);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemFactory )(
			ISilDataAccess * This,
			/* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			ISilDataAccess * This,
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemsOfInterest )(
			ISilDataAccess * This,
			/* [in] */ int cwsMax,
			/* [size_is][out] */ int *pws,
			/* [retval][out] */ int *pcws);

		HRESULT ( STDMETHODCALLTYPE *InsertRelExtra )(
			ISilDataAccess * This,
			/* [in] */ long hvoSrc,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ long hvoDst,
			/* [in] */ BSTR bstrExtra);

		HRESULT ( STDMETHODCALLTYPE *UpdateRelExtra )(
			ISilDataAccess * This,
			/* [in] */ long hvoSrc,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ BSTR bstrExtra);

		HRESULT ( STDMETHODCALLTYPE *GetRelExtra )(
			ISilDataAccess * This,
			/* [in] */ long hvoSrc,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [retval][out] */ BSTR *pbstrExtra);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsPropInCache )(
			ISilDataAccess * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int cpt,
			/* [in] */ int ws,
			/* [retval][out] */ ComBool *pfCached);

		HRESULT ( STDMETHODCALLTYPE *IsDirty )(
			ISilDataAccess * This,
			/* [retval][out] */ ComBool *pf);

		HRESULT ( STDMETHODCALLTYPE *ClearDirty )(
			ISilDataAccess * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MetaDataCache )(
			ISilDataAccess * This,
			/* [retval][out] */ /* external definition not present */ IFwMetaDataCache **ppmdc);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_MetaDataCache )(
			ISilDataAccess * This,
			/* [in] */ /* external definition not present */ IFwMetaDataCache *pmdc);

		END_INTERFACE
	} ISilDataAccessVtbl;

	interface ISilDataAccess
	{
		CONST_VTBL struct ISilDataAccessVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ISilDataAccess_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISilDataAccess_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ISilDataAccess_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ISilDataAccess_get_ObjectProp(This,hvo,tag,phvo)	\
	(This)->lpVtbl -> get_ObjectProp(This,hvo,tag,phvo)

#define ISilDataAccess_get_VecItem(This,hvo,tag,index,phvo)	\
	(This)->lpVtbl -> get_VecItem(This,hvo,tag,index,phvo)

#define ISilDataAccess_get_VecSize(This,hvo,tag,pchvo)	\
	(This)->lpVtbl -> get_VecSize(This,hvo,tag,pchvo)

#define ISilDataAccess_get_VecSizeAssumeCached(This,hvo,tag,pchvo)	\
	(This)->lpVtbl -> get_VecSizeAssumeCached(This,hvo,tag,pchvo)

#define ISilDataAccess_VecProp(This,hvo,tag,chvoMax,pchvo,prghvo)	\
	(This)->lpVtbl -> VecProp(This,hvo,tag,chvoMax,pchvo,prghvo)

#define ISilDataAccess_BinaryPropRgb(This,obj,tag,prgb,cbMax,pcb)	\
	(This)->lpVtbl -> BinaryPropRgb(This,obj,tag,prgb,cbMax,pcb)

#define ISilDataAccess_get_GuidProp(This,hvo,tag,puid)	\
	(This)->lpVtbl -> get_GuidProp(This,hvo,tag,puid)

#define ISilDataAccess_get_IntProp(This,hvo,tag,pn)	\
	(This)->lpVtbl -> get_IntProp(This,hvo,tag,pn)

#define ISilDataAccess_get_Int64Prop(This,hvo,tag,plln)	\
	(This)->lpVtbl -> get_Int64Prop(This,hvo,tag,plln)

#define ISilDataAccess_get_MultiStringAlt(This,hvo,tag,ws,pptss)	\
	(This)->lpVtbl -> get_MultiStringAlt(This,hvo,tag,ws,pptss)

#define ISilDataAccess_get_MultiStringProp(This,hvo,tag,pptms)	\
	(This)->lpVtbl -> get_MultiStringProp(This,hvo,tag,pptms)

#define ISilDataAccess_get_Prop(This,hvo,tag,pvar)	\
	(This)->lpVtbl -> get_Prop(This,hvo,tag,pvar)

#define ISilDataAccess_get_StringProp(This,hvo,tag,pptss)	\
	(This)->lpVtbl -> get_StringProp(This,hvo,tag,pptss)

#define ISilDataAccess_get_TimeProp(This,hvo,tag,ptim)	\
	(This)->lpVtbl -> get_TimeProp(This,hvo,tag,ptim)

#define ISilDataAccess_get_UnicodeProp(This,obj,tag,pbstr)	\
	(This)->lpVtbl -> get_UnicodeProp(This,obj,tag,pbstr)

#define ISilDataAccess_put_UnicodeProp(This,obj,tag,bstr)	\
	(This)->lpVtbl -> put_UnicodeProp(This,obj,tag,bstr)

#define ISilDataAccess_UnicodePropRgch(This,obj,tag,prgch,cchMax,pcch)	\
	(This)->lpVtbl -> UnicodePropRgch(This,obj,tag,prgch,cchMax,pcch)

#define ISilDataAccess_get_UnknownProp(This,hvo,tag,iid,ppunk)	\
	(This)->lpVtbl -> get_UnknownProp(This,hvo,tag,iid,ppunk)

#define ISilDataAccess_BeginUndoTask(This,bstrUndo,bstrRedo)	\
	(This)->lpVtbl -> BeginUndoTask(This,bstrUndo,bstrRedo)

#define ISilDataAccess_EndUndoTask(This)	\
	(This)->lpVtbl -> EndUndoTask(This)

#define ISilDataAccess_ContinueUndoTask(This)	\
	(This)->lpVtbl -> ContinueUndoTask(This)

#define ISilDataAccess_EndOuterUndoTask(This)	\
	(This)->lpVtbl -> EndOuterUndoTask(This)

#define ISilDataAccess_BreakUndoTask(This,bstrUndo,bstrRedo)	\
	(This)->lpVtbl -> BreakUndoTask(This,bstrUndo,bstrRedo)

#define ISilDataAccess_GetActionHandler(This,ppacth)	\
	(This)->lpVtbl -> GetActionHandler(This,ppacth)

#define ISilDataAccess_SetActionHandler(This,pacth)	\
	(This)->lpVtbl -> SetActionHandler(This,pacth)

#define ISilDataAccess_DeleteObj(This,hvoObj)	\
	(This)->lpVtbl -> DeleteObj(This,hvoObj)

#define ISilDataAccess_DeleteObjOwner(This,hvoOwner,hvoObj,tag,ihvo)	\
	(This)->lpVtbl -> DeleteObjOwner(This,hvoOwner,hvoObj,tag,ihvo)

#define ISilDataAccess_InsertNew(This,hvoObj,tag,ihvo,chvo,pss)	\
	(This)->lpVtbl -> InsertNew(This,hvoObj,tag,ihvo,chvo,pss)

#define ISilDataAccess_MakeNewObject(This,clid,hvoOwner,tag,ord,phvoNew)	\
	(This)->lpVtbl -> MakeNewObject(This,clid,hvoOwner,tag,ord,phvoNew)

#define ISilDataAccess_MoveOwnSeq(This,hvoSrcOwner,tagSrc,ihvoStart,ihvoEnd,hvoDstOwner,tagDst,ihvoDstStart)	\
	(This)->lpVtbl -> MoveOwnSeq(This,hvoSrcOwner,tagSrc,ihvoStart,ihvoEnd,hvoDstOwner,tagDst,ihvoDstStart)

#define ISilDataAccess_Replace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo)	\
	(This)->lpVtbl -> Replace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo)

#define ISilDataAccess_SetObjProp(This,hvo,tag,hvoObj)	\
	(This)->lpVtbl -> SetObjProp(This,hvo,tag,hvoObj)

#define ISilDataAccess_RemoveObjRefs(This,hvo)	\
	(This)->lpVtbl -> RemoveObjRefs(This,hvo)

#define ISilDataAccess_SetBinary(This,hvo,tag,prgb,cb)	\
	(This)->lpVtbl -> SetBinary(This,hvo,tag,prgb,cb)

#define ISilDataAccess_SetGuid(This,hvo,tag,uid)	\
	(This)->lpVtbl -> SetGuid(This,hvo,tag,uid)

#define ISilDataAccess_SetInt(This,hvo,tag,n)	\
	(This)->lpVtbl -> SetInt(This,hvo,tag,n)

#define ISilDataAccess_SetInt64(This,hvo,tag,lln)	\
	(This)->lpVtbl -> SetInt64(This,hvo,tag,lln)

#define ISilDataAccess_SetMultiStringAlt(This,hvo,tag,ws,ptss)	\
	(This)->lpVtbl -> SetMultiStringAlt(This,hvo,tag,ws,ptss)

#define ISilDataAccess_SetString(This,hvo,tag,ptss)	\
	(This)->lpVtbl -> SetString(This,hvo,tag,ptss)

#define ISilDataAccess_SetTime(This,hvo,tag,lln)	\
	(This)->lpVtbl -> SetTime(This,hvo,tag,lln)

#define ISilDataAccess_SetUnicode(This,hvo,tag,prgch,cch)	\
	(This)->lpVtbl -> SetUnicode(This,hvo,tag,prgch,cch)

#define ISilDataAccess_SetUnknown(This,hvo,tag,punk)	\
	(This)->lpVtbl -> SetUnknown(This,hvo,tag,punk)

#define ISilDataAccess_AddNotification(This,pnchng)	\
	(This)->lpVtbl -> AddNotification(This,pnchng)

#define ISilDataAccess_PropChanged(This,pnchng,pct,hvo,tag,ivMin,cvIns,cvDel)	\
	(This)->lpVtbl -> PropChanged(This,pnchng,pct,hvo,tag,ivMin,cvIns,cvDel)

#define ISilDataAccess_RemoveNotification(This,pnchng)	\
	(This)->lpVtbl -> RemoveNotification(This,pnchng)

#define ISilDataAccess_get_WritingSystemFactory(This,ppwsf)	\
	(This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf)

#define ISilDataAccess_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#define ISilDataAccess_get_WritingSystemsOfInterest(This,cwsMax,pws,pcws)	\
	(This)->lpVtbl -> get_WritingSystemsOfInterest(This,cwsMax,pws,pcws)

#define ISilDataAccess_InsertRelExtra(This,hvoSrc,tag,ihvo,hvoDst,bstrExtra)	\
	(This)->lpVtbl -> InsertRelExtra(This,hvoSrc,tag,ihvo,hvoDst,bstrExtra)

#define ISilDataAccess_UpdateRelExtra(This,hvoSrc,tag,ihvo,bstrExtra)	\
	(This)->lpVtbl -> UpdateRelExtra(This,hvoSrc,tag,ihvo,bstrExtra)

#define ISilDataAccess_GetRelExtra(This,hvoSrc,tag,ihvo,pbstrExtra)	\
	(This)->lpVtbl -> GetRelExtra(This,hvoSrc,tag,ihvo,pbstrExtra)

#define ISilDataAccess_get_IsPropInCache(This,hvo,tag,cpt,ws,pfCached)	\
	(This)->lpVtbl -> get_IsPropInCache(This,hvo,tag,cpt,ws,pfCached)

#define ISilDataAccess_IsDirty(This,pf)	\
	(This)->lpVtbl -> IsDirty(This,pf)

#define ISilDataAccess_ClearDirty(This)	\
	(This)->lpVtbl -> ClearDirty(This)

#define ISilDataAccess_get_MetaDataCache(This,ppmdc)	\
	(This)->lpVtbl -> get_MetaDataCache(This,ppmdc)

#define ISilDataAccess_putref_MetaDataCache(This,pmdc)	\
	(This)->lpVtbl -> putref_MetaDataCache(This,pmdc)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_ObjectProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ long *phvo);


void __RPC_STUB ISilDataAccess_get_ObjectProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_VecItem_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int index,
	/* [retval][out] */ long *phvo);


void __RPC_STUB ISilDataAccess_get_VecItem_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_VecSize_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ int *pchvo);


void __RPC_STUB ISilDataAccess_get_VecSize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_VecSizeAssumeCached_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ int *pchvo);


void __RPC_STUB ISilDataAccess_get_VecSizeAssumeCached_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_VecProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int chvoMax,
	/* [out] */ int *pchvo,
	/* [length_is][size_is][out] */ long *prghvo);


void __RPC_STUB ISilDataAccess_VecProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_BinaryPropRgb_Proxy(
	ISilDataAccess * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [length_is][size_is][out] */ byte *prgb,
	/* [in] */ int cbMax,
	/* [out] */ int *pcb);


void __RPC_STUB ISilDataAccess_BinaryPropRgb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_GuidProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ GUID *puid);


void __RPC_STUB ISilDataAccess_get_GuidProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_IntProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ int *pn);


void __RPC_STUB ISilDataAccess_get_IntProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_Int64Prop_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ __int64 *plln);


void __RPC_STUB ISilDataAccess_get_Int64Prop_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_MultiStringAlt_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [retval][out] */ /* external definition not present */ ITsString **pptss);


void __RPC_STUB ISilDataAccess_get_MultiStringAlt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_MultiStringProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ /* external definition not present */ ITsMultiString **pptms);


void __RPC_STUB ISilDataAccess_get_MultiStringProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_Prop_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ VARIANT *pvar);


void __RPC_STUB ISilDataAccess_get_Prop_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_StringProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ /* external definition not present */ ITsString **pptss);


void __RPC_STUB ISilDataAccess_get_StringProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_TimeProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [retval][out] */ __int64 *ptim);


void __RPC_STUB ISilDataAccess_get_TimeProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_UnicodeProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB ISilDataAccess_get_UnicodeProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_put_UnicodeProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ BSTR bstr);


void __RPC_STUB ISilDataAccess_put_UnicodeProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_UnicodePropRgch_Proxy(
	ISilDataAccess * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [length_is][size_is][out] */ OLECHAR *prgch,
	/* [in] */ int cchMax,
	/* [out] */ int *pcch);


void __RPC_STUB ISilDataAccess_UnicodePropRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget][local] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_UnknownProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ REFIID iid,
	/* [retval][out] */ void **ppunk);


void __RPC_STUB ISilDataAccess_get_UnknownProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_BeginUndoTask_Proxy(
	ISilDataAccess * This,
	/* [in] */ BSTR bstrUndo,
	/* [in] */ BSTR bstrRedo);


void __RPC_STUB ISilDataAccess_BeginUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_EndUndoTask_Proxy(
	ISilDataAccess * This);


void __RPC_STUB ISilDataAccess_EndUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_ContinueUndoTask_Proxy(
	ISilDataAccess * This);


void __RPC_STUB ISilDataAccess_ContinueUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_EndOuterUndoTask_Proxy(
	ISilDataAccess * This);


void __RPC_STUB ISilDataAccess_EndOuterUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_BreakUndoTask_Proxy(
	ISilDataAccess * This,
	/* [in] */ BSTR bstrUndo,
	/* [in] */ BSTR bstrRedo);


void __RPC_STUB ISilDataAccess_BreakUndoTask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_GetActionHandler_Proxy(
	ISilDataAccess * This,
	/* [retval][out] */ /* external definition not present */ IActionHandler **ppacth);


void __RPC_STUB ISilDataAccess_GetActionHandler_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetActionHandler_Proxy(
	ISilDataAccess * This,
	/* [in] */ /* external definition not present */ IActionHandler *pacth);


void __RPC_STUB ISilDataAccess_SetActionHandler_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_DeleteObj_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoObj);


void __RPC_STUB ISilDataAccess_DeleteObj_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_DeleteObjOwner_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoOwner,
	/* [in] */ long hvoObj,
	/* [in] */ int tag,
	/* [in] */ int ihvo);


void __RPC_STUB ISilDataAccess_DeleteObjOwner_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_InsertNew_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoObj,
	/* [in] */ int tag,
	/* [in] */ int ihvo,
	/* [in] */ int chvo,
	/* [in] */ IVwStylesheet *pss);


void __RPC_STUB ISilDataAccess_InsertNew_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_MakeNewObject_Proxy(
	ISilDataAccess * This,
	/* [in] */ int clid,
	/* [in] */ long hvoOwner,
	/* [in] */ int tag,
	/* [in] */ int ord,
	/* [retval][out] */ long *phvoNew);


void __RPC_STUB ISilDataAccess_MakeNewObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_MoveOwnSeq_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoSrcOwner,
	/* [in] */ int tagSrc,
	/* [in] */ int ihvoStart,
	/* [in] */ int ihvoEnd,
	/* [in] */ long hvoDstOwner,
	/* [in] */ int tagDst,
	/* [in] */ int ihvoDstStart);


void __RPC_STUB ISilDataAccess_MoveOwnSeq_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_Replace_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoObj,
	/* [in] */ int tag,
	/* [in] */ int ihvoMin,
	/* [in] */ int ihvoLim,
	/* [size_is][in] */ long *prghvo,
	/* [in] */ int chvo);


void __RPC_STUB ISilDataAccess_Replace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetObjProp_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ long hvoObj);


void __RPC_STUB ISilDataAccess_SetObjProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_RemoveObjRefs_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo);


void __RPC_STUB ISilDataAccess_RemoveObjRefs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetBinary_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [size_is][in] */ byte *prgb,
	/* [in] */ int cb);


void __RPC_STUB ISilDataAccess_SetBinary_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetGuid_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ GUID uid);


void __RPC_STUB ISilDataAccess_SetGuid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetInt_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int n);


void __RPC_STUB ISilDataAccess_SetInt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetInt64_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ __int64 lln);


void __RPC_STUB ISilDataAccess_SetInt64_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetMultiStringAlt_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [in] */ /* external definition not present */ ITsString *ptss);


void __RPC_STUB ISilDataAccess_SetMultiStringAlt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetString_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ /* external definition not present */ ITsString *ptss);


void __RPC_STUB ISilDataAccess_SetString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetTime_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ __int64 lln);


void __RPC_STUB ISilDataAccess_SetTime_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetUnicode_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [size_is][in] */ OLECHAR *prgch,
	/* [in] */ int cch);


void __RPC_STUB ISilDataAccess_SetUnicode_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_SetUnknown_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ IUnknown *punk);


void __RPC_STUB ISilDataAccess_SetUnknown_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_AddNotification_Proxy(
	ISilDataAccess * This,
	/* [in] */ IVwNotifyChange *pnchng);


void __RPC_STUB ISilDataAccess_AddNotification_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_PropChanged_Proxy(
	ISilDataAccess * This,
	/* [in] */ IVwNotifyChange *pnchng,
	/* [in] */ int pct,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int ivMin,
	/* [in] */ int cvIns,
	/* [in] */ int cvDel);


void __RPC_STUB ISilDataAccess_PropChanged_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_RemoveNotification_Proxy(
	ISilDataAccess * This,
	/* [in] */ IVwNotifyChange *pnchng);


void __RPC_STUB ISilDataAccess_RemoveNotification_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_WritingSystemFactory_Proxy(
	ISilDataAccess * This,
	/* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf);


void __RPC_STUB ISilDataAccess_get_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_putref_WritingSystemFactory_Proxy(
	ISilDataAccess * This,
	/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB ISilDataAccess_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_WritingSystemsOfInterest_Proxy(
	ISilDataAccess * This,
	/* [in] */ int cwsMax,
	/* [size_is][out] */ int *pws,
	/* [retval][out] */ int *pcws);


void __RPC_STUB ISilDataAccess_get_WritingSystemsOfInterest_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_InsertRelExtra_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoSrc,
	/* [in] */ int tag,
	/* [in] */ int ihvo,
	/* [in] */ long hvoDst,
	/* [in] */ BSTR bstrExtra);


void __RPC_STUB ISilDataAccess_InsertRelExtra_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_UpdateRelExtra_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoSrc,
	/* [in] */ int tag,
	/* [in] */ int ihvo,
	/* [in] */ BSTR bstrExtra);


void __RPC_STUB ISilDataAccess_UpdateRelExtra_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_GetRelExtra_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvoSrc,
	/* [in] */ int tag,
	/* [in] */ int ihvo,
	/* [retval][out] */ BSTR *pbstrExtra);


void __RPC_STUB ISilDataAccess_GetRelExtra_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_IsPropInCache_Proxy(
	ISilDataAccess * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int cpt,
	/* [in] */ int ws,
	/* [retval][out] */ ComBool *pfCached);


void __RPC_STUB ISilDataAccess_get_IsPropInCache_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_IsDirty_Proxy(
	ISilDataAccess * This,
	/* [retval][out] */ ComBool *pf);


void __RPC_STUB ISilDataAccess_IsDirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISilDataAccess_ClearDirty_Proxy(
	ISilDataAccess * This);


void __RPC_STUB ISilDataAccess_ClearDirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_get_MetaDataCache_Proxy(
	ISilDataAccess * This,
	/* [retval][out] */ /* external definition not present */ IFwMetaDataCache **ppmdc);


void __RPC_STUB ISilDataAccess_get_MetaDataCache_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE ISilDataAccess_putref_MetaDataCache_Proxy(
	ISilDataAccess * This,
	/* [in] */ /* external definition not present */ IFwMetaDataCache *pmdc);


void __RPC_STUB ISilDataAccess_putref_MetaDataCache_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ISilDataAccess_INTERFACE_DEFINED__ */


#ifndef __IVwCacheDa_INTERFACE_DEFINED__
#define __IVwCacheDa_INTERFACE_DEFINED__

/* interface IVwCacheDa */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwCacheDa;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("146AA200-7061-4f79-A8D8-7CBBA1B5CADA")
	IVwCacheDa : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE CacheObjProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ long val) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheVecProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ long rghvo[  ],
			/* [in] */ const int chvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheReplace(
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvoMin,
			/* [in] */ int ihvoLim,
			/* [size_is][in] */ long prghvo[  ],
			/* [in] */ int chvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheBinaryProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ byte *prgb,
			/* [in] */ int cb) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheGuidProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ GUID uid) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheInt64Prop(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ __int64 val) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheIntProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ int val) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheStringAlt(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ /* external definition not present */ ITsString *ptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheStringFields(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ const OLECHAR *prgchTxt,
			/* [in] */ int cchTxt,
			/* [size_is][in] */ const byte *prgbFmt,
			/* [in] */ int cbFmt) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheStringProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ /* external definition not present */ ITsString *ptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheTimeProp(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ SilTime val) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheUnicodeProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheUnknown(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ IUnknown *punk) = 0;

		virtual HRESULT STDMETHODCALLTYPE NewObject(
			/* [in] */ int clid,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ int ord,
			/* [retval][out] */ long *phvoNew) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetObjIndex(
			/* [in] */ long hvoOwn,
			/* [in] */ int flid,
			/* [in] */ long hvo,
			/* [retval][out] */ int *ihvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetOutlineNumber(
			/* [in] */ long hvo,
			/* [in] */ int flid,
			/* [in] */ ComBool fFinPer,
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE ClearInfoAbout(
			/* [in] */ long hvo,
			/* [in] */ ComBool fIncludeOwnedObjects) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CachedIntProp(
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [out] */ ComBool *pf,
			/* [retval][out] */ int *pn) = 0;

		virtual HRESULT STDMETHODCALLTYPE ClearAllData( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE InstallVirtual(
			/* [in] */ IVwVirtualHandler *pvh) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetVirtualHandlerId(
			/* [in] */ int tag,
			/* [retval][out] */ IVwVirtualHandler **ppvh) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetVirtualHandlerName(
			/* [in] */ BSTR bstrClass,
			/* [in] */ BSTR bstrField,
			/* [retval][out] */ IVwVirtualHandler **ppvh) = 0;

		virtual HRESULT STDMETHODCALLTYPE ClearVirtualProperties( void) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwCacheDaVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwCacheDa * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwCacheDa * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwCacheDa * This);

		HRESULT ( STDMETHODCALLTYPE *CacheObjProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ long val);

		HRESULT ( STDMETHODCALLTYPE *CacheVecProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ long rghvo[  ],
			/* [in] */ const int chvo);

		HRESULT ( STDMETHODCALLTYPE *CacheReplace )(
			IVwCacheDa * This,
			/* [in] */ long hvoObj,
			/* [in] */ int tag,
			/* [in] */ int ihvoMin,
			/* [in] */ int ihvoLim,
			/* [size_is][in] */ long prghvo[  ],
			/* [in] */ int chvo);

		HRESULT ( STDMETHODCALLTYPE *CacheBinaryProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ byte *prgb,
			/* [in] */ int cb);

		HRESULT ( STDMETHODCALLTYPE *CacheGuidProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ GUID uid);

		HRESULT ( STDMETHODCALLTYPE *CacheInt64Prop )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ __int64 val);

		HRESULT ( STDMETHODCALLTYPE *CacheIntProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ int val);

		HRESULT ( STDMETHODCALLTYPE *CacheStringAlt )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ /* external definition not present */ ITsString *ptss);

		HRESULT ( STDMETHODCALLTYPE *CacheStringFields )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ const OLECHAR *prgchTxt,
			/* [in] */ int cchTxt,
			/* [size_is][in] */ const byte *prgbFmt,
			/* [in] */ int cbFmt);

		HRESULT ( STDMETHODCALLTYPE *CacheStringProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ /* external definition not present */ ITsString *ptss);

		HRESULT ( STDMETHODCALLTYPE *CacheTimeProp )(
			IVwCacheDa * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ SilTime val);

		HRESULT ( STDMETHODCALLTYPE *CacheUnicodeProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [size_is][in] */ OLECHAR *prgch,
			/* [in] */ int cch);

		HRESULT ( STDMETHODCALLTYPE *CacheUnknown )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [in] */ IUnknown *punk);

		HRESULT ( STDMETHODCALLTYPE *NewObject )(
			IVwCacheDa * This,
			/* [in] */ int clid,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ int ord,
			/* [retval][out] */ long *phvoNew);

		HRESULT ( STDMETHODCALLTYPE *GetObjIndex )(
			IVwCacheDa * This,
			/* [in] */ long hvoOwn,
			/* [in] */ int flid,
			/* [in] */ long hvo,
			/* [retval][out] */ int *ihvo);

		HRESULT ( STDMETHODCALLTYPE *GetOutlineNumber )(
			IVwCacheDa * This,
			/* [in] */ long hvo,
			/* [in] */ int flid,
			/* [in] */ ComBool fFinPer,
			/* [retval][out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *ClearInfoAbout )(
			IVwCacheDa * This,
			/* [in] */ long hvo,
			/* [in] */ ComBool fIncludeOwnedObjects);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CachedIntProp )(
			IVwCacheDa * This,
			/* [in] */ long obj,
			/* [in] */ int tag,
			/* [out] */ ComBool *pf,
			/* [retval][out] */ int *pn);

		HRESULT ( STDMETHODCALLTYPE *ClearAllData )(
			IVwCacheDa * This);

		HRESULT ( STDMETHODCALLTYPE *InstallVirtual )(
			IVwCacheDa * This,
			/* [in] */ IVwVirtualHandler *pvh);

		HRESULT ( STDMETHODCALLTYPE *GetVirtualHandlerId )(
			IVwCacheDa * This,
			/* [in] */ int tag,
			/* [retval][out] */ IVwVirtualHandler **ppvh);

		HRESULT ( STDMETHODCALLTYPE *GetVirtualHandlerName )(
			IVwCacheDa * This,
			/* [in] */ BSTR bstrClass,
			/* [in] */ BSTR bstrField,
			/* [retval][out] */ IVwVirtualHandler **ppvh);

		HRESULT ( STDMETHODCALLTYPE *ClearVirtualProperties )(
			IVwCacheDa * This);

		END_INTERFACE
	} IVwCacheDaVtbl;

	interface IVwCacheDa
	{
		CONST_VTBL struct IVwCacheDaVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwCacheDa_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwCacheDa_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwCacheDa_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwCacheDa_CacheObjProp(This,obj,tag,val)	\
	(This)->lpVtbl -> CacheObjProp(This,obj,tag,val)

#define IVwCacheDa_CacheVecProp(This,obj,tag,rghvo,chvo)	\
	(This)->lpVtbl -> CacheVecProp(This,obj,tag,rghvo,chvo)

#define IVwCacheDa_CacheReplace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo)	\
	(This)->lpVtbl -> CacheReplace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo)

#define IVwCacheDa_CacheBinaryProp(This,obj,tag,prgb,cb)	\
	(This)->lpVtbl -> CacheBinaryProp(This,obj,tag,prgb,cb)

#define IVwCacheDa_CacheGuidProp(This,obj,tag,uid)	\
	(This)->lpVtbl -> CacheGuidProp(This,obj,tag,uid)

#define IVwCacheDa_CacheInt64Prop(This,obj,tag,val)	\
	(This)->lpVtbl -> CacheInt64Prop(This,obj,tag,val)

#define IVwCacheDa_CacheIntProp(This,obj,tag,val)	\
	(This)->lpVtbl -> CacheIntProp(This,obj,tag,val)

#define IVwCacheDa_CacheStringAlt(This,obj,tag,ws,ptss)	\
	(This)->lpVtbl -> CacheStringAlt(This,obj,tag,ws,ptss)

#define IVwCacheDa_CacheStringFields(This,obj,tag,prgchTxt,cchTxt,prgbFmt,cbFmt)	\
	(This)->lpVtbl -> CacheStringFields(This,obj,tag,prgchTxt,cchTxt,prgbFmt,cbFmt)

#define IVwCacheDa_CacheStringProp(This,obj,tag,ptss)	\
	(This)->lpVtbl -> CacheStringProp(This,obj,tag,ptss)

#define IVwCacheDa_CacheTimeProp(This,hvo,tag,val)	\
	(This)->lpVtbl -> CacheTimeProp(This,hvo,tag,val)

#define IVwCacheDa_CacheUnicodeProp(This,obj,tag,prgch,cch)	\
	(This)->lpVtbl -> CacheUnicodeProp(This,obj,tag,prgch,cch)

#define IVwCacheDa_CacheUnknown(This,obj,tag,punk)	\
	(This)->lpVtbl -> CacheUnknown(This,obj,tag,punk)

#define IVwCacheDa_NewObject(This,clid,hvoOwner,tag,ord,phvoNew)	\
	(This)->lpVtbl -> NewObject(This,clid,hvoOwner,tag,ord,phvoNew)

#define IVwCacheDa_GetObjIndex(This,hvoOwn,flid,hvo,ihvo)	\
	(This)->lpVtbl -> GetObjIndex(This,hvoOwn,flid,hvo,ihvo)

#define IVwCacheDa_GetOutlineNumber(This,hvo,flid,fFinPer,pbstr)	\
	(This)->lpVtbl -> GetOutlineNumber(This,hvo,flid,fFinPer,pbstr)

#define IVwCacheDa_ClearInfoAbout(This,hvo,fIncludeOwnedObjects)	\
	(This)->lpVtbl -> ClearInfoAbout(This,hvo,fIncludeOwnedObjects)

#define IVwCacheDa_get_CachedIntProp(This,obj,tag,pf,pn)	\
	(This)->lpVtbl -> get_CachedIntProp(This,obj,tag,pf,pn)

#define IVwCacheDa_ClearAllData(This)	\
	(This)->lpVtbl -> ClearAllData(This)

#define IVwCacheDa_InstallVirtual(This,pvh)	\
	(This)->lpVtbl -> InstallVirtual(This,pvh)

#define IVwCacheDa_GetVirtualHandlerId(This,tag,ppvh)	\
	(This)->lpVtbl -> GetVirtualHandlerId(This,tag,ppvh)

#define IVwCacheDa_GetVirtualHandlerName(This,bstrClass,bstrField,ppvh)	\
	(This)->lpVtbl -> GetVirtualHandlerName(This,bstrClass,bstrField,ppvh)

#define IVwCacheDa_ClearVirtualProperties(This)	\
	(This)->lpVtbl -> ClearVirtualProperties(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheObjProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ long val);


void __RPC_STUB IVwCacheDa_CacheObjProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheVecProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [size_is][in] */ long rghvo[  ],
	/* [in] */ const int chvo);


void __RPC_STUB IVwCacheDa_CacheVecProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheReplace_Proxy(
	IVwCacheDa * This,
	/* [in] */ long hvoObj,
	/* [in] */ int tag,
	/* [in] */ int ihvoMin,
	/* [in] */ int ihvoLim,
	/* [size_is][in] */ long prghvo[  ],
	/* [in] */ int chvo);


void __RPC_STUB IVwCacheDa_CacheReplace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheBinaryProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [size_is][in] */ byte *prgb,
	/* [in] */ int cb);


void __RPC_STUB IVwCacheDa_CacheBinaryProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheGuidProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ GUID uid);


void __RPC_STUB IVwCacheDa_CacheGuidProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheInt64Prop_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ __int64 val);


void __RPC_STUB IVwCacheDa_CacheInt64Prop_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheIntProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ int val);


void __RPC_STUB IVwCacheDa_CacheIntProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheStringAlt_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [in] */ /* external definition not present */ ITsString *ptss);


void __RPC_STUB IVwCacheDa_CacheStringAlt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheStringFields_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [size_is][in] */ const OLECHAR *prgchTxt,
	/* [in] */ int cchTxt,
	/* [size_is][in] */ const byte *prgbFmt,
	/* [in] */ int cbFmt);


void __RPC_STUB IVwCacheDa_CacheStringFields_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheStringProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ /* external definition not present */ ITsString *ptss);


void __RPC_STUB IVwCacheDa_CacheStringProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheTimeProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ SilTime val);


void __RPC_STUB IVwCacheDa_CacheTimeProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheUnicodeProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [size_is][in] */ OLECHAR *prgch,
	/* [in] */ int cch);


void __RPC_STUB IVwCacheDa_CacheUnicodeProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_CacheUnknown_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [in] */ IUnknown *punk);


void __RPC_STUB IVwCacheDa_CacheUnknown_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_NewObject_Proxy(
	IVwCacheDa * This,
	/* [in] */ int clid,
	/* [in] */ long hvoOwner,
	/* [in] */ int tag,
	/* [in] */ int ord,
	/* [retval][out] */ long *phvoNew);


void __RPC_STUB IVwCacheDa_NewObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_GetObjIndex_Proxy(
	IVwCacheDa * This,
	/* [in] */ long hvoOwn,
	/* [in] */ int flid,
	/* [in] */ long hvo,
	/* [retval][out] */ int *ihvo);


void __RPC_STUB IVwCacheDa_GetObjIndex_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_GetOutlineNumber_Proxy(
	IVwCacheDa * This,
	/* [in] */ long hvo,
	/* [in] */ int flid,
	/* [in] */ ComBool fFinPer,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IVwCacheDa_GetOutlineNumber_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_ClearInfoAbout_Proxy(
	IVwCacheDa * This,
	/* [in] */ long hvo,
	/* [in] */ ComBool fIncludeOwnedObjects);


void __RPC_STUB IVwCacheDa_ClearInfoAbout_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwCacheDa_get_CachedIntProp_Proxy(
	IVwCacheDa * This,
	/* [in] */ long obj,
	/* [in] */ int tag,
	/* [out] */ ComBool *pf,
	/* [retval][out] */ int *pn);


void __RPC_STUB IVwCacheDa_get_CachedIntProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_ClearAllData_Proxy(
	IVwCacheDa * This);


void __RPC_STUB IVwCacheDa_ClearAllData_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_InstallVirtual_Proxy(
	IVwCacheDa * This,
	/* [in] */ IVwVirtualHandler *pvh);


void __RPC_STUB IVwCacheDa_InstallVirtual_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_GetVirtualHandlerId_Proxy(
	IVwCacheDa * This,
	/* [in] */ int tag,
	/* [retval][out] */ IVwVirtualHandler **ppvh);


void __RPC_STUB IVwCacheDa_GetVirtualHandlerId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_GetVirtualHandlerName_Proxy(
	IVwCacheDa * This,
	/* [in] */ BSTR bstrClass,
	/* [in] */ BSTR bstrField,
	/* [retval][out] */ IVwVirtualHandler **ppvh);


void __RPC_STUB IVwCacheDa_GetVirtualHandlerName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwCacheDa_ClearVirtualProperties_Proxy(
	IVwCacheDa * This);


void __RPC_STUB IVwCacheDa_ClearVirtualProperties_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwCacheDa_INTERFACE_DEFINED__ */


#ifndef __IVwOleDbDa_INTERFACE_DEFINED__
#define __IVwOleDbDa_INTERFACE_DEFINED__

/* interface IVwOleDbDa */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwOleDbDa;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("AAAA731D-E34E-4742-948F-C88BBD0AE136")
	IVwOleDbDa : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE CreateDummyID(
			/* [out] */ long *phvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE Load(
			/* [in] */ BSTR bstrSqlStmt,
			/* [in] */ IDbColSpec *pdcs,
			/* [in] */ long hvoBase,
			/* [in] */ int nrowMax,
			/* [in] */ /* external definition not present */ IAdvInd *padvi,
			/* [in] */ ComBool fNotifyChange) = 0;

		virtual HRESULT STDMETHODCALLTYPE Save( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Clear( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CheckTimeStamp(
			/* [in] */ long hvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetTimeStamp(
			/* [in] */ long hvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheCurrTimeStamp(
			/* [in] */ long hvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE CacheCurrTimeStampAndOwner(
			/* [in] */ long hvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ObjOwner(
			/* [in] */ long hvo,
			/* [retval][out] */ long *phvoOwn) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ObjClid(
			/* [in] */ long hvo,
			/* [retval][out] */ int *pclid) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ObjOwnFlid(
			/* [in] */ long hvo,
			/* [retval][out] */ int *pflidOwn) = 0;

		virtual HRESULT STDMETHODCALLTYPE LoadData(
			/* [size_is][in] */ long *prghvo,
			/* [size_is][in] */ int *prgclsid,
			/* [in] */ int chvo,
			/* [in] */ IVwDataSpec *pdts,
			/* [in] */ /* external definition not present */ IAdvInd *padvi,
			/* [in] */ ComBool fIncludeOwnedObjects) = 0;

		virtual HRESULT STDMETHODCALLTYPE UpdatePropIfCached(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int cpt,
			/* [in] */ int ws) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetIdFromGuid(
			/* [in] */ GUID *puid,
			/* [retval][out] */ long *phvo) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwOleDbDaVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwOleDbDa * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwOleDbDa * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwOleDbDa * This);

		HRESULT ( STDMETHODCALLTYPE *CreateDummyID )(
			IVwOleDbDa * This,
			/* [out] */ long *phvo);

		HRESULT ( STDMETHODCALLTYPE *Load )(
			IVwOleDbDa * This,
			/* [in] */ BSTR bstrSqlStmt,
			/* [in] */ IDbColSpec *pdcs,
			/* [in] */ long hvoBase,
			/* [in] */ int nrowMax,
			/* [in] */ /* external definition not present */ IAdvInd *padvi,
			/* [in] */ ComBool fNotifyChange);

		HRESULT ( STDMETHODCALLTYPE *Save )(
			IVwOleDbDa * This);

		HRESULT ( STDMETHODCALLTYPE *Clear )(
			IVwOleDbDa * This);

		HRESULT ( STDMETHODCALLTYPE *CheckTimeStamp )(
			IVwOleDbDa * This,
			/* [in] */ long hvo);

		HRESULT ( STDMETHODCALLTYPE *SetTimeStamp )(
			IVwOleDbDa * This,
			/* [in] */ long hvo);

		HRESULT ( STDMETHODCALLTYPE *CacheCurrTimeStamp )(
			IVwOleDbDa * This,
			/* [in] */ long hvo);

		HRESULT ( STDMETHODCALLTYPE *CacheCurrTimeStampAndOwner )(
			IVwOleDbDa * This,
			/* [in] */ long hvo);

		HRESULT ( STDMETHODCALLTYPE *Close )(
			IVwOleDbDa * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ObjOwner )(
			IVwOleDbDa * This,
			/* [in] */ long hvo,
			/* [retval][out] */ long *phvoOwn);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ObjClid )(
			IVwOleDbDa * This,
			/* [in] */ long hvo,
			/* [retval][out] */ int *pclid);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ObjOwnFlid )(
			IVwOleDbDa * This,
			/* [in] */ long hvo,
			/* [retval][out] */ int *pflidOwn);

		HRESULT ( STDMETHODCALLTYPE *LoadData )(
			IVwOleDbDa * This,
			/* [size_is][in] */ long *prghvo,
			/* [size_is][in] */ int *prgclsid,
			/* [in] */ int chvo,
			/* [in] */ IVwDataSpec *pdts,
			/* [in] */ /* external definition not present */ IAdvInd *padvi,
			/* [in] */ ComBool fIncludeOwnedObjects);

		HRESULT ( STDMETHODCALLTYPE *UpdatePropIfCached )(
			IVwOleDbDa * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int cpt,
			/* [in] */ int ws);

		HRESULT ( STDMETHODCALLTYPE *GetIdFromGuid )(
			IVwOleDbDa * This,
			/* [in] */ GUID *puid,
			/* [retval][out] */ long *phvo);

		END_INTERFACE
	} IVwOleDbDaVtbl;

	interface IVwOleDbDa
	{
		CONST_VTBL struct IVwOleDbDaVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwOleDbDa_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwOleDbDa_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwOleDbDa_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwOleDbDa_CreateDummyID(This,phvo)	\
	(This)->lpVtbl -> CreateDummyID(This,phvo)

#define IVwOleDbDa_Load(This,bstrSqlStmt,pdcs,hvoBase,nrowMax,padvi,fNotifyChange)	\
	(This)->lpVtbl -> Load(This,bstrSqlStmt,pdcs,hvoBase,nrowMax,padvi,fNotifyChange)

#define IVwOleDbDa_Save(This)	\
	(This)->lpVtbl -> Save(This)

#define IVwOleDbDa_Clear(This)	\
	(This)->lpVtbl -> Clear(This)

#define IVwOleDbDa_CheckTimeStamp(This,hvo)	\
	(This)->lpVtbl -> CheckTimeStamp(This,hvo)

#define IVwOleDbDa_SetTimeStamp(This,hvo)	\
	(This)->lpVtbl -> SetTimeStamp(This,hvo)

#define IVwOleDbDa_CacheCurrTimeStamp(This,hvo)	\
	(This)->lpVtbl -> CacheCurrTimeStamp(This,hvo)

#define IVwOleDbDa_CacheCurrTimeStampAndOwner(This,hvo)	\
	(This)->lpVtbl -> CacheCurrTimeStampAndOwner(This,hvo)

#define IVwOleDbDa_Close(This)	\
	(This)->lpVtbl -> Close(This)

#define IVwOleDbDa_get_ObjOwner(This,hvo,phvoOwn)	\
	(This)->lpVtbl -> get_ObjOwner(This,hvo,phvoOwn)

#define IVwOleDbDa_get_ObjClid(This,hvo,pclid)	\
	(This)->lpVtbl -> get_ObjClid(This,hvo,pclid)

#define IVwOleDbDa_get_ObjOwnFlid(This,hvo,pflidOwn)	\
	(This)->lpVtbl -> get_ObjOwnFlid(This,hvo,pflidOwn)

#define IVwOleDbDa_LoadData(This,prghvo,prgclsid,chvo,pdts,padvi,fIncludeOwnedObjects)	\
	(This)->lpVtbl -> LoadData(This,prghvo,prgclsid,chvo,pdts,padvi,fIncludeOwnedObjects)

#define IVwOleDbDa_UpdatePropIfCached(This,hvo,tag,cpt,ws)	\
	(This)->lpVtbl -> UpdatePropIfCached(This,hvo,tag,cpt,ws)

#define IVwOleDbDa_GetIdFromGuid(This,puid,phvo)	\
	(This)->lpVtbl -> GetIdFromGuid(This,puid,phvo)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwOleDbDa_CreateDummyID_Proxy(
	IVwOleDbDa * This,
	/* [out] */ long *phvo);


void __RPC_STUB IVwOleDbDa_CreateDummyID_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_Load_Proxy(
	IVwOleDbDa * This,
	/* [in] */ BSTR bstrSqlStmt,
	/* [in] */ IDbColSpec *pdcs,
	/* [in] */ long hvoBase,
	/* [in] */ int nrowMax,
	/* [in] */ /* external definition not present */ IAdvInd *padvi,
	/* [in] */ ComBool fNotifyChange);


void __RPC_STUB IVwOleDbDa_Load_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_Save_Proxy(
	IVwOleDbDa * This);


void __RPC_STUB IVwOleDbDa_Save_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_Clear_Proxy(
	IVwOleDbDa * This);


void __RPC_STUB IVwOleDbDa_Clear_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_CheckTimeStamp_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo);


void __RPC_STUB IVwOleDbDa_CheckTimeStamp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_SetTimeStamp_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo);


void __RPC_STUB IVwOleDbDa_SetTimeStamp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_CacheCurrTimeStamp_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo);


void __RPC_STUB IVwOleDbDa_CacheCurrTimeStamp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_CacheCurrTimeStampAndOwner_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo);


void __RPC_STUB IVwOleDbDa_CacheCurrTimeStampAndOwner_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_Close_Proxy(
	IVwOleDbDa * This);


void __RPC_STUB IVwOleDbDa_Close_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOleDbDa_get_ObjOwner_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo,
	/* [retval][out] */ long *phvoOwn);


void __RPC_STUB IVwOleDbDa_get_ObjOwner_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOleDbDa_get_ObjClid_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo,
	/* [retval][out] */ int *pclid);


void __RPC_STUB IVwOleDbDa_get_ObjClid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOleDbDa_get_ObjOwnFlid_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo,
	/* [retval][out] */ int *pflidOwn);


void __RPC_STUB IVwOleDbDa_get_ObjOwnFlid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_LoadData_Proxy(
	IVwOleDbDa * This,
	/* [size_is][in] */ long *prghvo,
	/* [size_is][in] */ int *prgclsid,
	/* [in] */ int chvo,
	/* [in] */ IVwDataSpec *pdts,
	/* [in] */ /* external definition not present */ IAdvInd *padvi,
	/* [in] */ ComBool fIncludeOwnedObjects);


void __RPC_STUB IVwOleDbDa_LoadData_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_UpdatePropIfCached_Proxy(
	IVwOleDbDa * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int cpt,
	/* [in] */ int ws);


void __RPC_STUB IVwOleDbDa_UpdatePropIfCached_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOleDbDa_GetIdFromGuid_Proxy(
	IVwOleDbDa * This,
	/* [in] */ GUID *puid,
	/* [retval][out] */ long *phvo);


void __RPC_STUB IVwOleDbDa_GetIdFromGuid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwOleDbDa_INTERFACE_DEFINED__ */


#ifndef __ISetupVwOleDbDa_INTERFACE_DEFINED__
#define __ISetupVwOleDbDa_INTERFACE_DEFINED__

/* interface ISetupVwOleDbDa */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ISetupVwOleDbDa;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8645fA4F-EE90-11D2-A9B8-0080C87B6086")
	ISetupVwOleDbDa : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *pmdc,
			/* [in] */ IUnknown *pwsf,
			/* [in] */ /* external definition not present */ IActionHandler *pacth) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetOleDbEncap(
			/* [retval][out] */ IUnknown **ppode) = 0;

	};

#else 	/* C style interface */

	typedef struct ISetupVwOleDbDaVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ISetupVwOleDbDa * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ISetupVwOleDbDa * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ISetupVwOleDbDa * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			ISetupVwOleDbDa * This,
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *pmdc,
			/* [in] */ IUnknown *pwsf,
			/* [in] */ /* external definition not present */ IActionHandler *pacth);

		HRESULT ( STDMETHODCALLTYPE *GetOleDbEncap )(
			ISetupVwOleDbDa * This,
			/* [retval][out] */ IUnknown **ppode);

		END_INTERFACE
	} ISetupVwOleDbDaVtbl;

	interface ISetupVwOleDbDa
	{
		CONST_VTBL struct ISetupVwOleDbDaVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ISetupVwOleDbDa_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISetupVwOleDbDa_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ISetupVwOleDbDa_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ISetupVwOleDbDa_Init(This,pode,pmdc,pwsf,pacth)	\
	(This)->lpVtbl -> Init(This,pode,pmdc,pwsf,pacth)

#define ISetupVwOleDbDa_GetOleDbEncap(This,ppode)	\
	(This)->lpVtbl -> GetOleDbEncap(This,ppode)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISetupVwOleDbDa_Init_Proxy(
	ISetupVwOleDbDa * This,
	/* [in] */ IUnknown *pode,
	/* [in] */ IUnknown *pmdc,
	/* [in] */ IUnknown *pwsf,
	/* [in] */ /* external definition not present */ IActionHandler *pacth);


void __RPC_STUB ISetupVwOleDbDa_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISetupVwOleDbDa_GetOleDbEncap_Proxy(
	ISetupVwOleDbDa * This,
	/* [retval][out] */ IUnknown **ppode);


void __RPC_STUB ISetupVwOleDbDa_GetOleDbEncap_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ISetupVwOleDbDa_INTERFACE_DEFINED__ */


#ifndef __IVwRootBox_INTERFACE_DEFINED__
#define __IVwRootBox_INTERFACE_DEFINED__

/* interface IVwRootBox */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwRootBox;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("24717CB1-0C4D-485e-BA7F-7B28DE861A3F")
	IVwRootBox : public IVwNotifyChange
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE SetSite(
			/* [in] */ IVwRootSite *pvrs) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_DataAccess(
			/* [in] */ ISilDataAccess *psda) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DataAccess(
			/* [retval][out] */ ISilDataAccess **ppsda) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetRootObjects(
			/* [size_is][in] */ long *prghvo,
			/* [size_is][in] */ IVwViewConstructor **prgpvwvc,
			/* [size_is][in] */ int *prgfrag,
			/* [in] */ IVwStylesheet *pss,
			/* [in] */ int chvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetRootObject(
			/* [in] */ long hvo,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag,
			/* [in] */ IVwStylesheet *pss) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetRootVariant(
			/* [in] */ VARIANT v,
			/* [in] */ IVwStylesheet *pss,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetRootString(
			/* [in] */ /* external definition not present */ ITsString *ptss,
			/* [in] */ IVwStylesheet *pss,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Overlay(
			/* [in] */ IVwOverlay *pvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Overlay(
			/* [retval][out] */ IVwOverlay **ppvo) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetRootVariant(
			/* [retval][out] */ VARIANT *pv) = 0;

		virtual HRESULT STDMETHODCALLTYPE Serialize(
			/* [in] */ IStream *pstrm) = 0;

		virtual HRESULT STDMETHODCALLTYPE Deserialize(
			/* [in] */ IStream *pstrm) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteWpx(
			/* [in] */ IStream *pstrm) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Selection(
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE DestroySelection( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeTextSelection(
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int tagTextProp,
			/* [in] */ int cpropPrevious,
			/* [in] */ int ichAnchor,
			/* [in] */ int ichEnd,
			/* [in] */ int ws,
			/* [in] */ ComBool fAssocPrev,
			/* [in] */ int ihvoEnd,
			/* [in] */ /* external definition not present */ ITsTextProps *pttpIns,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeRangeSelection(
			/* [in] */ IVwSelection *pselAnchor,
			/* [in] */ IVwSelection *pselEnd,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeSimpleSel(
			/* [in] */ ComBool fInitial,
			/* [in] */ ComBool fEdit,
			/* [in] */ ComBool fRange,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeTextSelInObj(
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvsli,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int cvsliEnd,
			/* [size_is][in] */ VwSelLevInfo *prgvsliEnd,
			/* [in] */ ComBool fInitial,
			/* [in] */ ComBool fEdit,
			/* [in] */ ComBool fRange,
			/* [in] */ ComBool fWholeObj,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeSelInObj(
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvsli,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int tag,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeSelAt(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE MakeSelInBox(
			/* [in] */ IVwSelection *pselInit,
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [in] */ int iBox,
			/* [in] */ ComBool fInitial,
			/* [in] */ ComBool fRange,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsClickInText(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [retval][out] */ ComBool *pfInText) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsClickInObject(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [out] */ int *podt,
			/* [retval][out] */ ComBool *pfInObject) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsClickInOverlayTag(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc1,
			/* [in] */ RECT rcDst1,
			/* [out] */ int *piGuid,
			/* [out] */ BSTR *pbstrGuids,
			/* [out] */ RECT *prcTag,
			/* [out] */ RECT *prcAllTags,
			/* [out] */ ComBool *pfOpeningTag,
			/* [retval][out] */ ComBool *pfInOverlayTag) = 0;

		virtual HRESULT STDMETHODCALLTYPE OnTyping(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ BSTR bstrInput,
			/* [in] */ int cchBackspace,
			/* [in] */ int cchDelForward,
			/* [in] */ OLECHAR chFirst,
			/* [out][in] */ int *pwsPending) = 0;

		virtual HRESULT STDMETHODCALLTYPE OnChar(
			/* [in] */ int chw) = 0;

		virtual HRESULT STDMETHODCALLTYPE OnSysChar(
			/* [in] */ int chw) = 0;

		virtual /* [custom] */ HRESULT STDMETHODCALLTYPE OnExtendedKey(
			/* [in] */ int chw,
			/* [in] */ VwShiftStatus ss,
			/* [in] */ int nFlags) = 0;

		virtual HRESULT STDMETHODCALLTYPE FlashInsertionPoint( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE MouseDown(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst) = 0;

		virtual HRESULT STDMETHODCALLTYPE MouseDblClk(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst) = 0;

		virtual HRESULT STDMETHODCALLTYPE MouseMoveDrag(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst) = 0;

		virtual HRESULT STDMETHODCALLTYPE MouseDownExtended(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst) = 0;

		virtual HRESULT STDMETHODCALLTYPE MouseUp(
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst) = 0;

		virtual HRESULT STDMETHODCALLTYPE Activate(
			/* [in] */ VwSelectionState vss) = 0;

		virtual HRESULT STDMETHODCALLTYPE PrepareToDraw(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [retval][out] */ VwPrepDrawResult *pxpdr) = 0;

		virtual HRESULT STDMETHODCALLTYPE DrawRoot(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ ComBool fDrawSel) = 0;

		virtual HRESULT STDMETHODCALLTYPE Layout(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int dxsAvailWidth) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Height(
			/* [retval][out] */ int *pdysHeight) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Width(
			/* [retval][out] */ int *pdxsWidth) = 0;

		virtual HRESULT STDMETHODCALLTYPE InitializePrinting(
			/* [in] */ IVwPrintContext *pvpc) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetTotalPrintPages(
			/* [in] */ IVwPrintContext *pvpc,
			/* [in] */ /* external definition not present */ IAdvInd3 *padvi3,
			/* [retval][out] */ int *pcPageTotal) = 0;

		virtual HRESULT STDMETHODCALLTYPE PrintSinglePage(
			/* [in] */ IVwPrintContext *pvpc,
			/* [in] */ int nPageNo) = 0;

		virtual HRESULT STDMETHODCALLTYPE Print(
			/* [in] */ IVwPrintContext *pvpc,
			/* [in] */ /* external definition not present */ IAdvInd3 *padvi3) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Site(
			/* [retval][out] */ IVwRootSite **ppvrs) = 0;

		virtual HRESULT STDMETHODCALLTYPE LoseFocus(
			/* [retval][out] */ ComBool *pfOk) = 0;

		virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddSelChngListener(
			/* [in] */ IEventListener *pel) = 0;

		virtual HRESULT STDMETHODCALLTYPE DelSelChngListener(
			/* [in] */ IEventListener *pel) = 0;

		virtual HRESULT STDMETHODCALLTYPE Reconstruct( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OnStylesheetChange( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE DrawingErrors( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Stylesheet(
			/* [retval][out] */ IVwStylesheet **ppvss) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetTableColWidths(
			/* [size_is][in] */ VwLength *prgvlen,
			/* [in] */ int cvlen) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsDirty(
			/* [retval][out] */ ComBool *pfDirty) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_XdPos(
			/* [retval][out] */ int *pxdPos) = 0;

		virtual HRESULT STDMETHODCALLTYPE RequestObjCharDeleteNotification(
			IVwNotifyObjCharDeletion *pnocd) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetRootObject(
			/* [out] */ long *phvo,
			/* [out] */ IVwViewConstructor **ppvwvc,
			/* [out] */ int *pfrag,
			/* [out] */ IVwStylesheet **ppss) = 0;

		virtual HRESULT STDMETHODCALLTYPE DrawRoot2(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ ComBool fDrawSel,
			/* [in] */ int ysTop,
			/* [in] */ int dysHeight) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetKeyboardForWs(
			/* [in] */ /* external definition not present */ ILgWritingSystem *pws,
			/* [out][in] */ BSTR *pbstrActiveKeymanKbd,
			/* [out][in] */ int *pnActiveLangId,
			/* [out][in] */ int *phklActive,
			/* [out][in] */ ComBool *pfSelectLangPending) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwRootBoxVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwRootBox * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwRootBox * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwRootBox * This);

		HRESULT ( STDMETHODCALLTYPE *PropChanged )(
			IVwRootBox * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ivMin,
			/* [in] */ int cvIns,
			/* [in] */ int cvDel);

		HRESULT ( STDMETHODCALLTYPE *SetSite )(
			IVwRootBox * This,
			/* [in] */ IVwRootSite *pvrs);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_DataAccess )(
			IVwRootBox * This,
			/* [in] */ ISilDataAccess *psda);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DataAccess )(
			IVwRootBox * This,
			/* [retval][out] */ ISilDataAccess **ppsda);

		HRESULT ( STDMETHODCALLTYPE *SetRootObjects )(
			IVwRootBox * This,
			/* [size_is][in] */ long *prghvo,
			/* [size_is][in] */ IVwViewConstructor **prgpvwvc,
			/* [size_is][in] */ int *prgfrag,
			/* [in] */ IVwStylesheet *pss,
			/* [in] */ int chvo);

		HRESULT ( STDMETHODCALLTYPE *SetRootObject )(
			IVwRootBox * This,
			/* [in] */ long hvo,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag,
			/* [in] */ IVwStylesheet *pss);

		HRESULT ( STDMETHODCALLTYPE *SetRootVariant )(
			IVwRootBox * This,
			/* [in] */ VARIANT v,
			/* [in] */ IVwStylesheet *pss,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		HRESULT ( STDMETHODCALLTYPE *SetRootString )(
			IVwRootBox * This,
			/* [in] */ /* external definition not present */ ITsString *ptss,
			/* [in] */ IVwStylesheet *pss,
			/* [in] */ IVwViewConstructor *pvwvc,
			/* [in] */ int frag);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Overlay )(
			IVwRootBox * This,
			/* [in] */ IVwOverlay *pvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Overlay )(
			IVwRootBox * This,
			/* [retval][out] */ IVwOverlay **ppvo);

		HRESULT ( STDMETHODCALLTYPE *GetRootVariant )(
			IVwRootBox * This,
			/* [retval][out] */ VARIANT *pv);

		HRESULT ( STDMETHODCALLTYPE *Serialize )(
			IVwRootBox * This,
			/* [in] */ IStream *pstrm);

		HRESULT ( STDMETHODCALLTYPE *Deserialize )(
			IVwRootBox * This,
			/* [in] */ IStream *pstrm);

		HRESULT ( STDMETHODCALLTYPE *WriteWpx )(
			IVwRootBox * This,
			/* [in] */ IStream *pstrm);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Selection )(
			IVwRootBox * This,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *DestroySelection )(
			IVwRootBox * This);

		HRESULT ( STDMETHODCALLTYPE *MakeTextSelection )(
			IVwRootBox * This,
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int tagTextProp,
			/* [in] */ int cpropPrevious,
			/* [in] */ int ichAnchor,
			/* [in] */ int ichEnd,
			/* [in] */ int ws,
			/* [in] */ ComBool fAssocPrev,
			/* [in] */ int ihvoEnd,
			/* [in] */ /* external definition not present */ ITsTextProps *pttpIns,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *MakeRangeSelection )(
			IVwRootBox * This,
			/* [in] */ IVwSelection *pselAnchor,
			/* [in] */ IVwSelection *pselEnd,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *MakeSimpleSel )(
			IVwRootBox * This,
			/* [in] */ ComBool fInitial,
			/* [in] */ ComBool fEdit,
			/* [in] */ ComBool fRange,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *MakeTextSelInObj )(
			IVwRootBox * This,
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvsli,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int cvsliEnd,
			/* [size_is][in] */ VwSelLevInfo *prgvsliEnd,
			/* [in] */ ComBool fInitial,
			/* [in] */ ComBool fEdit,
			/* [in] */ ComBool fRange,
			/* [in] */ ComBool fWholeObj,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *MakeSelInObj )(
			IVwRootBox * This,
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvsli,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int tag,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *MakeSelAt )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *MakeSelInBox )(
			IVwRootBox * This,
			/* [in] */ IVwSelection *pselInit,
			/* [in] */ ComBool fEndPoint,
			/* [in] */ int iLevel,
			/* [in] */ int iBox,
			/* [in] */ ComBool fInitial,
			/* [in] */ ComBool fRange,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClickInText )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [retval][out] */ ComBool *pfInText);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClickInObject )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [out] */ int *podt,
			/* [retval][out] */ ComBool *pfInObject);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClickInOverlayTag )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc1,
			/* [in] */ RECT rcDst1,
			/* [out] */ int *piGuid,
			/* [out] */ BSTR *pbstrGuids,
			/* [out] */ RECT *prcTag,
			/* [out] */ RECT *prcAllTags,
			/* [out] */ ComBool *pfOpeningTag,
			/* [retval][out] */ ComBool *pfInOverlayTag);

		HRESULT ( STDMETHODCALLTYPE *OnTyping )(
			IVwRootBox * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ BSTR bstrInput,
			/* [in] */ int cchBackspace,
			/* [in] */ int cchDelForward,
			/* [in] */ OLECHAR chFirst,
			/* [out][in] */ int *pwsPending);

		HRESULT ( STDMETHODCALLTYPE *OnChar )(
			IVwRootBox * This,
			/* [in] */ int chw);

		HRESULT ( STDMETHODCALLTYPE *OnSysChar )(
			IVwRootBox * This,
			/* [in] */ int chw);

		/* [custom] */ HRESULT ( STDMETHODCALLTYPE *OnExtendedKey )(
			IVwRootBox * This,
			/* [in] */ int chw,
			/* [in] */ VwShiftStatus ss,
			/* [in] */ int nFlags);

		HRESULT ( STDMETHODCALLTYPE *FlashInsertionPoint )(
			IVwRootBox * This);

		HRESULT ( STDMETHODCALLTYPE *MouseDown )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst);

		HRESULT ( STDMETHODCALLTYPE *MouseDblClk )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst);

		HRESULT ( STDMETHODCALLTYPE *MouseMoveDrag )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst);

		HRESULT ( STDMETHODCALLTYPE *MouseDownExtended )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst);

		HRESULT ( STDMETHODCALLTYPE *MouseUp )(
			IVwRootBox * This,
			/* [in] */ int xd,
			/* [in] */ int yd,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst);

		HRESULT ( STDMETHODCALLTYPE *Activate )(
			IVwRootBox * This,
			/* [in] */ VwSelectionState vss);

		HRESULT ( STDMETHODCALLTYPE *PrepareToDraw )(
			IVwRootBox * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [retval][out] */ VwPrepDrawResult *pxpdr);

		HRESULT ( STDMETHODCALLTYPE *DrawRoot )(
			IVwRootBox * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ ComBool fDrawSel);

		HRESULT ( STDMETHODCALLTYPE *Layout )(
			IVwRootBox * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int dxsAvailWidth);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Height )(
			IVwRootBox * This,
			/* [retval][out] */ int *pdysHeight);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Width )(
			IVwRootBox * This,
			/* [retval][out] */ int *pdxsWidth);

		HRESULT ( STDMETHODCALLTYPE *InitializePrinting )(
			IVwRootBox * This,
			/* [in] */ IVwPrintContext *pvpc);

		HRESULT ( STDMETHODCALLTYPE *GetTotalPrintPages )(
			IVwRootBox * This,
			/* [in] */ IVwPrintContext *pvpc,
			/* [in] */ /* external definition not present */ IAdvInd3 *padvi3,
			/* [retval][out] */ int *pcPageTotal);

		HRESULT ( STDMETHODCALLTYPE *PrintSinglePage )(
			IVwRootBox * This,
			/* [in] */ IVwPrintContext *pvpc,
			/* [in] */ int nPageNo);

		HRESULT ( STDMETHODCALLTYPE *Print )(
			IVwRootBox * This,
			/* [in] */ IVwPrintContext *pvpc,
			/* [in] */ /* external definition not present */ IAdvInd3 *padvi3);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Site )(
			IVwRootBox * This,
			/* [retval][out] */ IVwRootSite **ppvrs);

		HRESULT ( STDMETHODCALLTYPE *LoseFocus )(
			IVwRootBox * This,
			/* [retval][out] */ ComBool *pfOk);

		HRESULT ( STDMETHODCALLTYPE *Close )(
			IVwRootBox * This);

		HRESULT ( STDMETHODCALLTYPE *AddSelChngListener )(
			IVwRootBox * This,
			/* [in] */ IEventListener *pel);

		HRESULT ( STDMETHODCALLTYPE *DelSelChngListener )(
			IVwRootBox * This,
			/* [in] */ IEventListener *pel);

		HRESULT ( STDMETHODCALLTYPE *Reconstruct )(
			IVwRootBox * This);

		HRESULT ( STDMETHODCALLTYPE *OnStylesheetChange )(
			IVwRootBox * This);

		HRESULT ( STDMETHODCALLTYPE *DrawingErrors )(
			IVwRootBox * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Stylesheet )(
			IVwRootBox * This,
			/* [retval][out] */ IVwStylesheet **ppvss);

		HRESULT ( STDMETHODCALLTYPE *SetTableColWidths )(
			IVwRootBox * This,
			/* [size_is][in] */ VwLength *prgvlen,
			/* [in] */ int cvlen);

		HRESULT ( STDMETHODCALLTYPE *IsDirty )(
			IVwRootBox * This,
			/* [retval][out] */ ComBool *pfDirty);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_XdPos )(
			IVwRootBox * This,
			/* [retval][out] */ int *pxdPos);

		HRESULT ( STDMETHODCALLTYPE *RequestObjCharDeleteNotification )(
			IVwRootBox * This,
			IVwNotifyObjCharDeletion *pnocd);

		HRESULT ( STDMETHODCALLTYPE *GetRootObject )(
			IVwRootBox * This,
			/* [out] */ long *phvo,
			/* [out] */ IVwViewConstructor **ppvwvc,
			/* [out] */ int *pfrag,
			/* [out] */ IVwStylesheet **ppss);

		HRESULT ( STDMETHODCALLTYPE *DrawRoot2 )(
			IVwRootBox * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ ComBool fDrawSel,
			/* [in] */ int ysTop,
			/* [in] */ int dysHeight);

		HRESULT ( STDMETHODCALLTYPE *SetKeyboardForWs )(
			IVwRootBox * This,
			/* [in] */ /* external definition not present */ ILgWritingSystem *pws,
			/* [out][in] */ BSTR *pbstrActiveKeymanKbd,
			/* [out][in] */ int *pnActiveLangId,
			/* [out][in] */ int *phklActive,
			/* [out][in] */ ComBool *pfSelectLangPending);

		END_INTERFACE
	} IVwRootBoxVtbl;

	interface IVwRootBox
	{
		CONST_VTBL struct IVwRootBoxVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwRootBox_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwRootBox_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwRootBox_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwRootBox_PropChanged(This,hvo,tag,ivMin,cvIns,cvDel)	\
	(This)->lpVtbl -> PropChanged(This,hvo,tag,ivMin,cvIns,cvDel)


#define IVwRootBox_SetSite(This,pvrs)	\
	(This)->lpVtbl -> SetSite(This,pvrs)

#define IVwRootBox_putref_DataAccess(This,psda)	\
	(This)->lpVtbl -> putref_DataAccess(This,psda)

#define IVwRootBox_get_DataAccess(This,ppsda)	\
	(This)->lpVtbl -> get_DataAccess(This,ppsda)

#define IVwRootBox_SetRootObjects(This,prghvo,prgpvwvc,prgfrag,pss,chvo)	\
	(This)->lpVtbl -> SetRootObjects(This,prghvo,prgpvwvc,prgfrag,pss,chvo)

#define IVwRootBox_SetRootObject(This,hvo,pvwvc,frag,pss)	\
	(This)->lpVtbl -> SetRootObject(This,hvo,pvwvc,frag,pss)

#define IVwRootBox_SetRootVariant(This,v,pss,pvwvc,frag)	\
	(This)->lpVtbl -> SetRootVariant(This,v,pss,pvwvc,frag)

#define IVwRootBox_SetRootString(This,ptss,pss,pvwvc,frag)	\
	(This)->lpVtbl -> SetRootString(This,ptss,pss,pvwvc,frag)

#define IVwRootBox_putref_Overlay(This,pvo)	\
	(This)->lpVtbl -> putref_Overlay(This,pvo)

#define IVwRootBox_get_Overlay(This,ppvo)	\
	(This)->lpVtbl -> get_Overlay(This,ppvo)

#define IVwRootBox_GetRootVariant(This,pv)	\
	(This)->lpVtbl -> GetRootVariant(This,pv)

#define IVwRootBox_Serialize(This,pstrm)	\
	(This)->lpVtbl -> Serialize(This,pstrm)

#define IVwRootBox_Deserialize(This,pstrm)	\
	(This)->lpVtbl -> Deserialize(This,pstrm)

#define IVwRootBox_WriteWpx(This,pstrm)	\
	(This)->lpVtbl -> WriteWpx(This,pstrm)

#define IVwRootBox_get_Selection(This,ppsel)	\
	(This)->lpVtbl -> get_Selection(This,ppsel)

#define IVwRootBox_DestroySelection(This)	\
	(This)->lpVtbl -> DestroySelection(This)

#define IVwRootBox_MakeTextSelection(This,ihvoRoot,cvlsi,prgvsli,tagTextProp,cpropPrevious,ichAnchor,ichEnd,ws,fAssocPrev,ihvoEnd,pttpIns,fInstall,ppsel)	\
	(This)->lpVtbl -> MakeTextSelection(This,ihvoRoot,cvlsi,prgvsli,tagTextProp,cpropPrevious,ichAnchor,ichEnd,ws,fAssocPrev,ihvoEnd,pttpIns,fInstall,ppsel)

#define IVwRootBox_MakeRangeSelection(This,pselAnchor,pselEnd,fInstall,ppsel)	\
	(This)->lpVtbl -> MakeRangeSelection(This,pselAnchor,pselEnd,fInstall,ppsel)

#define IVwRootBox_MakeSimpleSel(This,fInitial,fEdit,fRange,fInstall,ppsel)	\
	(This)->lpVtbl -> MakeSimpleSel(This,fInitial,fEdit,fRange,fInstall,ppsel)

#define IVwRootBox_MakeTextSelInObj(This,ihvoRoot,cvsli,prgvsli,cvsliEnd,prgvsliEnd,fInitial,fEdit,fRange,fWholeObj,fInstall,ppsel)	\
	(This)->lpVtbl -> MakeTextSelInObj(This,ihvoRoot,cvsli,prgvsli,cvsliEnd,prgvsliEnd,fInitial,fEdit,fRange,fWholeObj,fInstall,ppsel)

#define IVwRootBox_MakeSelInObj(This,ihvoRoot,cvsli,prgvsli,tag,fInstall,ppsel)	\
	(This)->lpVtbl -> MakeSelInObj(This,ihvoRoot,cvsli,prgvsli,tag,fInstall,ppsel)

#define IVwRootBox_MakeSelAt(This,xd,yd,rcSrc,rcDst,fInstall,ppsel)	\
	(This)->lpVtbl -> MakeSelAt(This,xd,yd,rcSrc,rcDst,fInstall,ppsel)

#define IVwRootBox_MakeSelInBox(This,pselInit,fEndPoint,iLevel,iBox,fInitial,fRange,fInstall,ppsel)	\
	(This)->lpVtbl -> MakeSelInBox(This,pselInit,fEndPoint,iLevel,iBox,fInitial,fRange,fInstall,ppsel)

#define IVwRootBox_get_IsClickInText(This,xd,yd,rcSrc,rcDst,pfInText)	\
	(This)->lpVtbl -> get_IsClickInText(This,xd,yd,rcSrc,rcDst,pfInText)

#define IVwRootBox_get_IsClickInObject(This,xd,yd,rcSrc,rcDst,podt,pfInObject)	\
	(This)->lpVtbl -> get_IsClickInObject(This,xd,yd,rcSrc,rcDst,podt,pfInObject)

#define IVwRootBox_get_IsClickInOverlayTag(This,xd,yd,rcSrc1,rcDst1,piGuid,pbstrGuids,prcTag,prcAllTags,pfOpeningTag,pfInOverlayTag)	\
	(This)->lpVtbl -> get_IsClickInOverlayTag(This,xd,yd,rcSrc1,rcDst1,piGuid,pbstrGuids,prcTag,prcAllTags,pfOpeningTag,pfInOverlayTag)

#define IVwRootBox_OnTyping(This,pvg,bstrInput,cchBackspace,cchDelForward,chFirst,pwsPending)	\
	(This)->lpVtbl -> OnTyping(This,pvg,bstrInput,cchBackspace,cchDelForward,chFirst,pwsPending)

#define IVwRootBox_OnChar(This,chw)	\
	(This)->lpVtbl -> OnChar(This,chw)

#define IVwRootBox_OnSysChar(This,chw)	\
	(This)->lpVtbl -> OnSysChar(This,chw)

#define IVwRootBox_OnExtendedKey(This,chw,ss,nFlags)	\
	(This)->lpVtbl -> OnExtendedKey(This,chw,ss,nFlags)

#define IVwRootBox_FlashInsertionPoint(This)	\
	(This)->lpVtbl -> FlashInsertionPoint(This)

#define IVwRootBox_MouseDown(This,xd,yd,rcSrc,rcDst)	\
	(This)->lpVtbl -> MouseDown(This,xd,yd,rcSrc,rcDst)

#define IVwRootBox_MouseDblClk(This,xd,yd,rcSrc,rcDst)	\
	(This)->lpVtbl -> MouseDblClk(This,xd,yd,rcSrc,rcDst)

#define IVwRootBox_MouseMoveDrag(This,xd,yd,rcSrc,rcDst)	\
	(This)->lpVtbl -> MouseMoveDrag(This,xd,yd,rcSrc,rcDst)

#define IVwRootBox_MouseDownExtended(This,xd,yd,rcSrc,rcDst)	\
	(This)->lpVtbl -> MouseDownExtended(This,xd,yd,rcSrc,rcDst)

#define IVwRootBox_MouseUp(This,xd,yd,rcSrc,rcDst)	\
	(This)->lpVtbl -> MouseUp(This,xd,yd,rcSrc,rcDst)

#define IVwRootBox_Activate(This,vss)	\
	(This)->lpVtbl -> Activate(This,vss)

#define IVwRootBox_PrepareToDraw(This,pvg,rcSrc,rcDst,pxpdr)	\
	(This)->lpVtbl -> PrepareToDraw(This,pvg,rcSrc,rcDst,pxpdr)

#define IVwRootBox_DrawRoot(This,pvg,rcSrc,rcDst,fDrawSel)	\
	(This)->lpVtbl -> DrawRoot(This,pvg,rcSrc,rcDst,fDrawSel)

#define IVwRootBox_Layout(This,pvg,dxsAvailWidth)	\
	(This)->lpVtbl -> Layout(This,pvg,dxsAvailWidth)

#define IVwRootBox_get_Height(This,pdysHeight)	\
	(This)->lpVtbl -> get_Height(This,pdysHeight)

#define IVwRootBox_get_Width(This,pdxsWidth)	\
	(This)->lpVtbl -> get_Width(This,pdxsWidth)

#define IVwRootBox_InitializePrinting(This,pvpc)	\
	(This)->lpVtbl -> InitializePrinting(This,pvpc)

#define IVwRootBox_GetTotalPrintPages(This,pvpc,padvi3,pcPageTotal)	\
	(This)->lpVtbl -> GetTotalPrintPages(This,pvpc,padvi3,pcPageTotal)

#define IVwRootBox_PrintSinglePage(This,pvpc,nPageNo)	\
	(This)->lpVtbl -> PrintSinglePage(This,pvpc,nPageNo)

#define IVwRootBox_Print(This,pvpc,padvi3)	\
	(This)->lpVtbl -> Print(This,pvpc,padvi3)

#define IVwRootBox_get_Site(This,ppvrs)	\
	(This)->lpVtbl -> get_Site(This,ppvrs)

#define IVwRootBox_LoseFocus(This,pfOk)	\
	(This)->lpVtbl -> LoseFocus(This,pfOk)

#define IVwRootBox_Close(This)	\
	(This)->lpVtbl -> Close(This)

#define IVwRootBox_AddSelChngListener(This,pel)	\
	(This)->lpVtbl -> AddSelChngListener(This,pel)

#define IVwRootBox_DelSelChngListener(This,pel)	\
	(This)->lpVtbl -> DelSelChngListener(This,pel)

#define IVwRootBox_Reconstruct(This)	\
	(This)->lpVtbl -> Reconstruct(This)

#define IVwRootBox_OnStylesheetChange(This)	\
	(This)->lpVtbl -> OnStylesheetChange(This)

#define IVwRootBox_DrawingErrors(This)	\
	(This)->lpVtbl -> DrawingErrors(This)

#define IVwRootBox_get_Stylesheet(This,ppvss)	\
	(This)->lpVtbl -> get_Stylesheet(This,ppvss)

#define IVwRootBox_SetTableColWidths(This,prgvlen,cvlen)	\
	(This)->lpVtbl -> SetTableColWidths(This,prgvlen,cvlen)

#define IVwRootBox_IsDirty(This,pfDirty)	\
	(This)->lpVtbl -> IsDirty(This,pfDirty)

#define IVwRootBox_get_XdPos(This,pxdPos)	\
	(This)->lpVtbl -> get_XdPos(This,pxdPos)

#define IVwRootBox_RequestObjCharDeleteNotification(This,pnocd)	\
	(This)->lpVtbl -> RequestObjCharDeleteNotification(This,pnocd)

#define IVwRootBox_GetRootObject(This,phvo,ppvwvc,pfrag,ppss)	\
	(This)->lpVtbl -> GetRootObject(This,phvo,ppvwvc,pfrag,ppss)

#define IVwRootBox_DrawRoot2(This,pvg,rcSrc,rcDst,fDrawSel,ysTop,dysHeight)	\
	(This)->lpVtbl -> DrawRoot2(This,pvg,rcSrc,rcDst,fDrawSel,ysTop,dysHeight)

#define IVwRootBox_SetKeyboardForWs(This,pws,pbstrActiveKeymanKbd,pnActiveLangId,phklActive,pfSelectLangPending)	\
	(This)->lpVtbl -> SetKeyboardForWs(This,pws,pbstrActiveKeymanKbd,pnActiveLangId,phklActive,pfSelectLangPending)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwRootBox_SetSite_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwRootSite *pvrs);


void __RPC_STUB IVwRootBox_SetSite_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwRootBox_putref_DataAccess_Proxy(
	IVwRootBox * This,
	/* [in] */ ISilDataAccess *psda);


void __RPC_STUB IVwRootBox_putref_DataAccess_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_DataAccess_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ ISilDataAccess **ppsda);


void __RPC_STUB IVwRootBox_get_DataAccess_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_SetRootObjects_Proxy(
	IVwRootBox * This,
	/* [size_is][in] */ long *prghvo,
	/* [size_is][in] */ IVwViewConstructor **prgpvwvc,
	/* [size_is][in] */ int *prgfrag,
	/* [in] */ IVwStylesheet *pss,
	/* [in] */ int chvo);


void __RPC_STUB IVwRootBox_SetRootObjects_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_SetRootObject_Proxy(
	IVwRootBox * This,
	/* [in] */ long hvo,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag,
	/* [in] */ IVwStylesheet *pss);


void __RPC_STUB IVwRootBox_SetRootObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_SetRootVariant_Proxy(
	IVwRootBox * This,
	/* [in] */ VARIANT v,
	/* [in] */ IVwStylesheet *pss,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwRootBox_SetRootVariant_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_SetRootString_Proxy(
	IVwRootBox * This,
	/* [in] */ /* external definition not present */ ITsString *ptss,
	/* [in] */ IVwStylesheet *pss,
	/* [in] */ IVwViewConstructor *pvwvc,
	/* [in] */ int frag);


void __RPC_STUB IVwRootBox_SetRootString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwRootBox_putref_Overlay_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwOverlay *pvo);


void __RPC_STUB IVwRootBox_putref_Overlay_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_Overlay_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ IVwOverlay **ppvo);


void __RPC_STUB IVwRootBox_get_Overlay_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_GetRootVariant_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ VARIANT *pv);


void __RPC_STUB IVwRootBox_GetRootVariant_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_Serialize_Proxy(
	IVwRootBox * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB IVwRootBox_Serialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_Deserialize_Proxy(
	IVwRootBox * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB IVwRootBox_Deserialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_WriteWpx_Proxy(
	IVwRootBox * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB IVwRootBox_WriteWpx_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_Selection_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_get_Selection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_DestroySelection_Proxy(
	IVwRootBox * This);


void __RPC_STUB IVwRootBox_DestroySelection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MakeTextSelection_Proxy(
	IVwRootBox * This,
	/* [in] */ int ihvoRoot,
	/* [in] */ int cvlsi,
	/* [size_is][in] */ VwSelLevInfo *prgvsli,
	/* [in] */ int tagTextProp,
	/* [in] */ int cpropPrevious,
	/* [in] */ int ichAnchor,
	/* [in] */ int ichEnd,
	/* [in] */ int ws,
	/* [in] */ ComBool fAssocPrev,
	/* [in] */ int ihvoEnd,
	/* [in] */ /* external definition not present */ ITsTextProps *pttpIns,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_MakeTextSelection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MakeRangeSelection_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwSelection *pselAnchor,
	/* [in] */ IVwSelection *pselEnd,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_MakeRangeSelection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MakeSimpleSel_Proxy(
	IVwRootBox * This,
	/* [in] */ ComBool fInitial,
	/* [in] */ ComBool fEdit,
	/* [in] */ ComBool fRange,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_MakeSimpleSel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MakeTextSelInObj_Proxy(
	IVwRootBox * This,
	/* [in] */ int ihvoRoot,
	/* [in] */ int cvsli,
	/* [size_is][in] */ VwSelLevInfo *prgvsli,
	/* [in] */ int cvsliEnd,
	/* [size_is][in] */ VwSelLevInfo *prgvsliEnd,
	/* [in] */ ComBool fInitial,
	/* [in] */ ComBool fEdit,
	/* [in] */ ComBool fRange,
	/* [in] */ ComBool fWholeObj,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_MakeTextSelInObj_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MakeSelInObj_Proxy(
	IVwRootBox * This,
	/* [in] */ int ihvoRoot,
	/* [in] */ int cvsli,
	/* [size_is][in] */ VwSelLevInfo *prgvsli,
	/* [in] */ int tag,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_MakeSelInObj_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MakeSelAt_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_MakeSelAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MakeSelInBox_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwSelection *pselInit,
	/* [in] */ ComBool fEndPoint,
	/* [in] */ int iLevel,
	/* [in] */ int iBox,
	/* [in] */ ComBool fInitial,
	/* [in] */ ComBool fRange,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwRootBox_MakeSelInBox_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_IsClickInText_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [retval][out] */ ComBool *pfInText);


void __RPC_STUB IVwRootBox_get_IsClickInText_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_IsClickInObject_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [out] */ int *podt,
	/* [retval][out] */ ComBool *pfInObject);


void __RPC_STUB IVwRootBox_get_IsClickInObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_IsClickInOverlayTag_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc1,
	/* [in] */ RECT rcDst1,
	/* [out] */ int *piGuid,
	/* [out] */ BSTR *pbstrGuids,
	/* [out] */ RECT *prcTag,
	/* [out] */ RECT *prcAllTags,
	/* [out] */ ComBool *pfOpeningTag,
	/* [retval][out] */ ComBool *pfInOverlayTag);


void __RPC_STUB IVwRootBox_get_IsClickInOverlayTag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_OnTyping_Proxy(
	IVwRootBox * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ BSTR bstrInput,
	/* [in] */ int cchBackspace,
	/* [in] */ int cchDelForward,
	/* [in] */ OLECHAR chFirst,
	/* [out][in] */ int *pwsPending);


void __RPC_STUB IVwRootBox_OnTyping_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_OnChar_Proxy(
	IVwRootBox * This,
	/* [in] */ int chw);


void __RPC_STUB IVwRootBox_OnChar_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_OnSysChar_Proxy(
	IVwRootBox * This,
	/* [in] */ int chw);


void __RPC_STUB IVwRootBox_OnSysChar_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [custom] */ HRESULT STDMETHODCALLTYPE IVwRootBox_OnExtendedKey_Proxy(
	IVwRootBox * This,
	/* [in] */ int chw,
	/* [in] */ VwShiftStatus ss,
	/* [in] */ int nFlags);


void __RPC_STUB IVwRootBox_OnExtendedKey_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_FlashInsertionPoint_Proxy(
	IVwRootBox * This);


void __RPC_STUB IVwRootBox_FlashInsertionPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MouseDown_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst);


void __RPC_STUB IVwRootBox_MouseDown_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MouseDblClk_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst);


void __RPC_STUB IVwRootBox_MouseDblClk_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MouseMoveDrag_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst);


void __RPC_STUB IVwRootBox_MouseMoveDrag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MouseDownExtended_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst);


void __RPC_STUB IVwRootBox_MouseDownExtended_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_MouseUp_Proxy(
	IVwRootBox * This,
	/* [in] */ int xd,
	/* [in] */ int yd,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst);


void __RPC_STUB IVwRootBox_MouseUp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_Activate_Proxy(
	IVwRootBox * This,
	/* [in] */ VwSelectionState vss);


void __RPC_STUB IVwRootBox_Activate_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_PrepareToDraw_Proxy(
	IVwRootBox * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [retval][out] */ VwPrepDrawResult *pxpdr);


void __RPC_STUB IVwRootBox_PrepareToDraw_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_DrawRoot_Proxy(
	IVwRootBox * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ ComBool fDrawSel);


void __RPC_STUB IVwRootBox_DrawRoot_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_Layout_Proxy(
	IVwRootBox * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ int dxsAvailWidth);


void __RPC_STUB IVwRootBox_Layout_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_Height_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ int *pdysHeight);


void __RPC_STUB IVwRootBox_get_Height_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_Width_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ int *pdxsWidth);


void __RPC_STUB IVwRootBox_get_Width_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_InitializePrinting_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwPrintContext *pvpc);


void __RPC_STUB IVwRootBox_InitializePrinting_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_GetTotalPrintPages_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwPrintContext *pvpc,
	/* [in] */ /* external definition not present */ IAdvInd3 *padvi3,
	/* [retval][out] */ int *pcPageTotal);


void __RPC_STUB IVwRootBox_GetTotalPrintPages_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_PrintSinglePage_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwPrintContext *pvpc,
	/* [in] */ int nPageNo);


void __RPC_STUB IVwRootBox_PrintSinglePage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_Print_Proxy(
	IVwRootBox * This,
	/* [in] */ IVwPrintContext *pvpc,
	/* [in] */ /* external definition not present */ IAdvInd3 *padvi3);


void __RPC_STUB IVwRootBox_Print_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_Site_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ IVwRootSite **ppvrs);


void __RPC_STUB IVwRootBox_get_Site_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_LoseFocus_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ ComBool *pfOk);


void __RPC_STUB IVwRootBox_LoseFocus_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_Close_Proxy(
	IVwRootBox * This);


void __RPC_STUB IVwRootBox_Close_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_AddSelChngListener_Proxy(
	IVwRootBox * This,
	/* [in] */ IEventListener *pel);


void __RPC_STUB IVwRootBox_AddSelChngListener_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_DelSelChngListener_Proxy(
	IVwRootBox * This,
	/* [in] */ IEventListener *pel);


void __RPC_STUB IVwRootBox_DelSelChngListener_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_Reconstruct_Proxy(
	IVwRootBox * This);


void __RPC_STUB IVwRootBox_Reconstruct_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_OnStylesheetChange_Proxy(
	IVwRootBox * This);


void __RPC_STUB IVwRootBox_OnStylesheetChange_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_DrawingErrors_Proxy(
	IVwRootBox * This);


void __RPC_STUB IVwRootBox_DrawingErrors_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_Stylesheet_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ IVwStylesheet **ppvss);


void __RPC_STUB IVwRootBox_get_Stylesheet_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_SetTableColWidths_Proxy(
	IVwRootBox * This,
	/* [size_is][in] */ VwLength *prgvlen,
	/* [in] */ int cvlen);


void __RPC_STUB IVwRootBox_SetTableColWidths_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_IsDirty_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ ComBool *pfDirty);


void __RPC_STUB IVwRootBox_IsDirty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwRootBox_get_XdPos_Proxy(
	IVwRootBox * This,
	/* [retval][out] */ int *pxdPos);


void __RPC_STUB IVwRootBox_get_XdPos_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_RequestObjCharDeleteNotification_Proxy(
	IVwRootBox * This,
	IVwNotifyObjCharDeletion *pnocd);


void __RPC_STUB IVwRootBox_RequestObjCharDeleteNotification_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_GetRootObject_Proxy(
	IVwRootBox * This,
	/* [out] */ long *phvo,
	/* [out] */ IVwViewConstructor **ppvwvc,
	/* [out] */ int *pfrag,
	/* [out] */ IVwStylesheet **ppss);


void __RPC_STUB IVwRootBox_GetRootObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_DrawRoot2_Proxy(
	IVwRootBox * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ ComBool fDrawSel,
	/* [in] */ int ysTop,
	/* [in] */ int dysHeight);


void __RPC_STUB IVwRootBox_DrawRoot2_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwRootBox_SetKeyboardForWs_Proxy(
	IVwRootBox * This,
	/* [in] */ /* external definition not present */ ILgWritingSystem *pws,
	/* [out][in] */ BSTR *pbstrActiveKeymanKbd,
	/* [out][in] */ int *pnActiveLangId,
	/* [out][in] */ int *phklActive,
	/* [out][in] */ ComBool *pfSelectLangPending);


void __RPC_STUB IVwRootBox_SetKeyboardForWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwRootBox_INTERFACE_DEFINED__ */


#ifndef __IVwPropertyStore_INTERFACE_DEFINED__
#define __IVwPropertyStore_INTERFACE_DEFINED__

/* interface IVwPropertyStore */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwPropertyStore;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("3D4847FE-EA2D-4255-A496-770059A134CC")
	IVwPropertyStore : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntProperty(
			/* [in] */ int nID,
			/* [retval][out] */ int *pnValue) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringProperty(
			/* [in] */ int sp,
			/* [retval][out] */ BSTR *bstrValue) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ChrpFor(
			/* [in] */ /* external definition not present */ ITsTextProps *pttp,
			/* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Stylesheet(
			/* [in] */ IVwStylesheet *pvps) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ParentStore(
			/* [retval][out] */ IVwPropertyStore **ppvps) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TextProps(
			/* [out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DerivedPropertiesForTtp(
			/* [in] */ /* external definition not present */ ITsTextProps *pttp,
			/* [retval][out] */ IVwPropertyStore **ppvps) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwPropertyStoreVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwPropertyStore * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwPropertyStore * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwPropertyStore * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntProperty )(
			IVwPropertyStore * This,
			/* [in] */ int nID,
			/* [retval][out] */ int *pnValue);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringProperty )(
			IVwPropertyStore * This,
			/* [in] */ int sp,
			/* [retval][out] */ BSTR *bstrValue);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ChrpFor )(
			IVwPropertyStore * This,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp,
			/* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Stylesheet )(
			IVwPropertyStore * This,
			/* [in] */ IVwStylesheet *pvps);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			IVwPropertyStore * This,
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ParentStore )(
			IVwPropertyStore * This,
			/* [retval][out] */ IVwPropertyStore **ppvps);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TextProps )(
			IVwPropertyStore * This,
			/* [out] */ /* external definition not present */ ITsTextProps **ppttp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DerivedPropertiesForTtp )(
			IVwPropertyStore * This,
			/* [in] */ /* external definition not present */ ITsTextProps *pttp,
			/* [retval][out] */ IVwPropertyStore **ppvps);

		END_INTERFACE
	} IVwPropertyStoreVtbl;

	interface IVwPropertyStore
	{
		CONST_VTBL struct IVwPropertyStoreVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwPropertyStore_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwPropertyStore_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwPropertyStore_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwPropertyStore_get_IntProperty(This,nID,pnValue)	\
	(This)->lpVtbl -> get_IntProperty(This,nID,pnValue)

#define IVwPropertyStore_get_StringProperty(This,sp,bstrValue)	\
	(This)->lpVtbl -> get_StringProperty(This,sp,bstrValue)

#define IVwPropertyStore_get_ChrpFor(This,pttp,pchrp)	\
	(This)->lpVtbl -> get_ChrpFor(This,pttp,pchrp)

#define IVwPropertyStore_putref_Stylesheet(This,pvps)	\
	(This)->lpVtbl -> putref_Stylesheet(This,pvps)

#define IVwPropertyStore_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#define IVwPropertyStore_get_ParentStore(This,ppvps)	\
	(This)->lpVtbl -> get_ParentStore(This,ppvps)

#define IVwPropertyStore_get_TextProps(This,ppttp)	\
	(This)->lpVtbl -> get_TextProps(This,ppttp)

#define IVwPropertyStore_get_DerivedPropertiesForTtp(This,pttp,ppvps)	\
	(This)->lpVtbl -> get_DerivedPropertiesForTtp(This,pttp,ppvps)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_get_IntProperty_Proxy(
	IVwPropertyStore * This,
	/* [in] */ int nID,
	/* [retval][out] */ int *pnValue);


void __RPC_STUB IVwPropertyStore_get_IntProperty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_get_StringProperty_Proxy(
	IVwPropertyStore * This,
	/* [in] */ int sp,
	/* [retval][out] */ BSTR *bstrValue);


void __RPC_STUB IVwPropertyStore_get_StringProperty_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_get_ChrpFor_Proxy(
	IVwPropertyStore * This,
	/* [in] */ /* external definition not present */ ITsTextProps *pttp,
	/* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp);


void __RPC_STUB IVwPropertyStore_get_ChrpFor_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_putref_Stylesheet_Proxy(
	IVwPropertyStore * This,
	/* [in] */ IVwStylesheet *pvps);


void __RPC_STUB IVwPropertyStore_putref_Stylesheet_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_putref_WritingSystemFactory_Proxy(
	IVwPropertyStore * This,
	/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB IVwPropertyStore_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_get_ParentStore_Proxy(
	IVwPropertyStore * This,
	/* [retval][out] */ IVwPropertyStore **ppvps);


void __RPC_STUB IVwPropertyStore_get_ParentStore_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_get_TextProps_Proxy(
	IVwPropertyStore * This,
	/* [out] */ /* external definition not present */ ITsTextProps **ppttp);


void __RPC_STUB IVwPropertyStore_get_TextProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPropertyStore_get_DerivedPropertiesForTtp_Proxy(
	IVwPropertyStore * This,
	/* [in] */ /* external definition not present */ ITsTextProps *pttp,
	/* [retval][out] */ IVwPropertyStore **ppvps);


void __RPC_STUB IVwPropertyStore_get_DerivedPropertiesForTtp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwPropertyStore_INTERFACE_DEFINED__ */


#ifndef __IVwOverlay_INTERFACE_DEFINED__
#define __IVwOverlay_INTERFACE_DEFINED__

/* interface IVwOverlay */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwOverlay;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("7D9089C1-3BB9-11d4-8078-0000C0FB81B5")
	IVwOverlay : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Name(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Guid(
			/* [retval][size_is][out] */ OLECHAR *prgchGuid) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Guid(
			/* [size_is][in] */ OLECHAR *prgchGuid) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_PossListId(
			/* [retval][out] */ long *ppsslId) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_PossListId(
			/* [in] */ long psslId) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags(
			/* [retval][out] */ VwOverlayFlags *pvof) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Flags(
			/* [in] */ VwOverlayFlags vof) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FontName(
			/* [in] */ BSTR bstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE FontNameRgch(
			/* [size_is][out] */ OLECHAR *prgch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontSize(
			/* [retval][out] */ int *pmp) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FontSize(
			/* [in] */ int mp) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MaxShowTags(
			/* [retval][out] */ int *pctag) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MaxShowTags(
			/* [in] */ int ctag) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CTags(
			/* [retval][out] */ int *pctag) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDbTagInfo(
			/* [in] */ int itag,
			/* [out] */ long *phvo,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [out] */ ComBool *pfHidden,
			/* [size_is][out] */ OLECHAR *prgchGuid) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetTagInfo(
			/* [size_is][in] */ OLECHAR *prgchGuid,
			/* [in] */ long hvo,
			/* [in] */ int grfosm,
			/* [in] */ BSTR bstrAbbr,
			/* [in] */ BSTR bstrName,
			/* [in] */ COLORREF clrFore,
			/* [in] */ COLORREF clrBack,
			/* [in] */ COLORREF clrUnder,
			/* [in] */ int unt,
			/* [in] */ ComBool fHidden) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetTagInfo(
			/* [size_is][in] */ OLECHAR *prgchGuid,
			/* [out] */ long *phvo,
			/* [out] */ BSTR *pbstrAbbr,
			/* [out] */ BSTR *pbstrName,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [out] */ ComBool *pfHidden) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDlgTagInfo(
			/* [in] */ int itag,
			/* [out] */ ComBool *pfHidden,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [out] */ BSTR *pbstrAbbr,
			/* [out] */ BSTR *pbstrName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDispTagInfo(
			/* [size_is][in] */ OLECHAR *prgchGuid,
			/* [out] */ ComBool *pfHidden,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [size_is][out] */ OLECHAR *prgchAbbr,
			/* [in] */ int cchMaxAbbr,
			/* [out] */ int *pcchAbbr,
			/* [size_is][out] */ OLECHAR *prgchName,
			/* [in] */ int cchMaxName,
			/* [out] */ int *pcchName) = 0;

		virtual HRESULT STDMETHODCALLTYPE RemoveTag(
			/* [size_is][in] */ OLECHAR *prgchGuid) = 0;

		virtual HRESULT STDMETHODCALLTYPE Sort(
			/* [in] */ ComBool fByAbbr) = 0;

		virtual HRESULT STDMETHODCALLTYPE Merge(
			/* [in] */ IVwOverlay *pvo,
			/* [retval][out] */ IVwOverlay **ppvoMerged) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwOverlayVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwOverlay * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwOverlay * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwOverlay * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )(
			IVwOverlay * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Name )(
			IVwOverlay * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Guid )(
			IVwOverlay * This,
			/* [retval][size_is][out] */ OLECHAR *prgchGuid);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Guid )(
			IVwOverlay * This,
			/* [size_is][in] */ OLECHAR *prgchGuid);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_PossListId )(
			IVwOverlay * This,
			/* [retval][out] */ long *ppsslId);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_PossListId )(
			IVwOverlay * This,
			/* [in] */ long psslId);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )(
			IVwOverlay * This,
			/* [retval][out] */ VwOverlayFlags *pvof);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Flags )(
			IVwOverlay * This,
			/* [in] */ VwOverlayFlags vof);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontName )(
			IVwOverlay * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FontName )(
			IVwOverlay * This,
			/* [in] */ BSTR bstr);

		HRESULT ( STDMETHODCALLTYPE *FontNameRgch )(
			IVwOverlay * This,
			/* [size_is][out] */ OLECHAR *prgch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontSize )(
			IVwOverlay * This,
			/* [retval][out] */ int *pmp);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FontSize )(
			IVwOverlay * This,
			/* [in] */ int mp);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MaxShowTags )(
			IVwOverlay * This,
			/* [retval][out] */ int *pctag);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MaxShowTags )(
			IVwOverlay * This,
			/* [in] */ int ctag);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CTags )(
			IVwOverlay * This,
			/* [retval][out] */ int *pctag);

		HRESULT ( STDMETHODCALLTYPE *GetDbTagInfo )(
			IVwOverlay * This,
			/* [in] */ int itag,
			/* [out] */ long *phvo,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [out] */ ComBool *pfHidden,
			/* [size_is][out] */ OLECHAR *prgchGuid);

		HRESULT ( STDMETHODCALLTYPE *SetTagInfo )(
			IVwOverlay * This,
			/* [size_is][in] */ OLECHAR *prgchGuid,
			/* [in] */ long hvo,
			/* [in] */ int grfosm,
			/* [in] */ BSTR bstrAbbr,
			/* [in] */ BSTR bstrName,
			/* [in] */ COLORREF clrFore,
			/* [in] */ COLORREF clrBack,
			/* [in] */ COLORREF clrUnder,
			/* [in] */ int unt,
			/* [in] */ ComBool fHidden);

		HRESULT ( STDMETHODCALLTYPE *GetTagInfo )(
			IVwOverlay * This,
			/* [size_is][in] */ OLECHAR *prgchGuid,
			/* [out] */ long *phvo,
			/* [out] */ BSTR *pbstrAbbr,
			/* [out] */ BSTR *pbstrName,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [out] */ ComBool *pfHidden);

		HRESULT ( STDMETHODCALLTYPE *GetDlgTagInfo )(
			IVwOverlay * This,
			/* [in] */ int itag,
			/* [out] */ ComBool *pfHidden,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [out] */ BSTR *pbstrAbbr,
			/* [out] */ BSTR *pbstrName);

		HRESULT ( STDMETHODCALLTYPE *GetDispTagInfo )(
			IVwOverlay * This,
			/* [size_is][in] */ OLECHAR *prgchGuid,
			/* [out] */ ComBool *pfHidden,
			/* [out] */ COLORREF *pclrFore,
			/* [out] */ COLORREF *pclrBack,
			/* [out] */ COLORREF *pclrUnder,
			/* [out] */ int *punt,
			/* [size_is][out] */ OLECHAR *prgchAbbr,
			/* [in] */ int cchMaxAbbr,
			/* [out] */ int *pcchAbbr,
			/* [size_is][out] */ OLECHAR *prgchName,
			/* [in] */ int cchMaxName,
			/* [out] */ int *pcchName);

		HRESULT ( STDMETHODCALLTYPE *RemoveTag )(
			IVwOverlay * This,
			/* [size_is][in] */ OLECHAR *prgchGuid);

		HRESULT ( STDMETHODCALLTYPE *Sort )(
			IVwOverlay * This,
			/* [in] */ ComBool fByAbbr);

		HRESULT ( STDMETHODCALLTYPE *Merge )(
			IVwOverlay * This,
			/* [in] */ IVwOverlay *pvo,
			/* [retval][out] */ IVwOverlay **ppvoMerged);

		END_INTERFACE
	} IVwOverlayVtbl;

	interface IVwOverlay
	{
		CONST_VTBL struct IVwOverlayVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwOverlay_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwOverlay_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwOverlay_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwOverlay_get_Name(This,pbstr)	\
	(This)->lpVtbl -> get_Name(This,pbstr)

#define IVwOverlay_put_Name(This,bstr)	\
	(This)->lpVtbl -> put_Name(This,bstr)

#define IVwOverlay_get_Guid(This,prgchGuid)	\
	(This)->lpVtbl -> get_Guid(This,prgchGuid)

#define IVwOverlay_put_Guid(This,prgchGuid)	\
	(This)->lpVtbl -> put_Guid(This,prgchGuid)

#define IVwOverlay_get_PossListId(This,ppsslId)	\
	(This)->lpVtbl -> get_PossListId(This,ppsslId)

#define IVwOverlay_put_PossListId(This,psslId)	\
	(This)->lpVtbl -> put_PossListId(This,psslId)

#define IVwOverlay_get_Flags(This,pvof)	\
	(This)->lpVtbl -> get_Flags(This,pvof)

#define IVwOverlay_put_Flags(This,vof)	\
	(This)->lpVtbl -> put_Flags(This,vof)

#define IVwOverlay_get_FontName(This,pbstr)	\
	(This)->lpVtbl -> get_FontName(This,pbstr)

#define IVwOverlay_put_FontName(This,bstr)	\
	(This)->lpVtbl -> put_FontName(This,bstr)

#define IVwOverlay_FontNameRgch(This,prgch)	\
	(This)->lpVtbl -> FontNameRgch(This,prgch)

#define IVwOverlay_get_FontSize(This,pmp)	\
	(This)->lpVtbl -> get_FontSize(This,pmp)

#define IVwOverlay_put_FontSize(This,mp)	\
	(This)->lpVtbl -> put_FontSize(This,mp)

#define IVwOverlay_get_MaxShowTags(This,pctag)	\
	(This)->lpVtbl -> get_MaxShowTags(This,pctag)

#define IVwOverlay_put_MaxShowTags(This,ctag)	\
	(This)->lpVtbl -> put_MaxShowTags(This,ctag)

#define IVwOverlay_get_CTags(This,pctag)	\
	(This)->lpVtbl -> get_CTags(This,pctag)

#define IVwOverlay_GetDbTagInfo(This,itag,phvo,pclrFore,pclrBack,pclrUnder,punt,pfHidden,prgchGuid)	\
	(This)->lpVtbl -> GetDbTagInfo(This,itag,phvo,pclrFore,pclrBack,pclrUnder,punt,pfHidden,prgchGuid)

#define IVwOverlay_SetTagInfo(This,prgchGuid,hvo,grfosm,bstrAbbr,bstrName,clrFore,clrBack,clrUnder,unt,fHidden)	\
	(This)->lpVtbl -> SetTagInfo(This,prgchGuid,hvo,grfosm,bstrAbbr,bstrName,clrFore,clrBack,clrUnder,unt,fHidden)

#define IVwOverlay_GetTagInfo(This,prgchGuid,phvo,pbstrAbbr,pbstrName,pclrFore,pclrBack,pclrUnder,punt,pfHidden)	\
	(This)->lpVtbl -> GetTagInfo(This,prgchGuid,phvo,pbstrAbbr,pbstrName,pclrFore,pclrBack,pclrUnder,punt,pfHidden)

#define IVwOverlay_GetDlgTagInfo(This,itag,pfHidden,pclrFore,pclrBack,pclrUnder,punt,pbstrAbbr,pbstrName)	\
	(This)->lpVtbl -> GetDlgTagInfo(This,itag,pfHidden,pclrFore,pclrBack,pclrUnder,punt,pbstrAbbr,pbstrName)

#define IVwOverlay_GetDispTagInfo(This,prgchGuid,pfHidden,pclrFore,pclrBack,pclrUnder,punt,prgchAbbr,cchMaxAbbr,pcchAbbr,prgchName,cchMaxName,pcchName)	\
	(This)->lpVtbl -> GetDispTagInfo(This,prgchGuid,pfHidden,pclrFore,pclrBack,pclrUnder,punt,prgchAbbr,cchMaxAbbr,pcchAbbr,prgchName,cchMaxName,pcchName)

#define IVwOverlay_RemoveTag(This,prgchGuid)	\
	(This)->lpVtbl -> RemoveTag(This,prgchGuid)

#define IVwOverlay_Sort(This,fByAbbr)	\
	(This)->lpVtbl -> Sort(This,fByAbbr)

#define IVwOverlay_Merge(This,pvo,ppvoMerged)	\
	(This)->lpVtbl -> Merge(This,pvo,ppvoMerged)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_Name_Proxy(
	IVwOverlay * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IVwOverlay_get_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwOverlay_put_Name_Proxy(
	IVwOverlay * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB IVwOverlay_put_Name_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_Guid_Proxy(
	IVwOverlay * This,
	/* [retval][size_is][out] */ OLECHAR *prgchGuid);


void __RPC_STUB IVwOverlay_get_Guid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwOverlay_put_Guid_Proxy(
	IVwOverlay * This,
	/* [size_is][in] */ OLECHAR *prgchGuid);


void __RPC_STUB IVwOverlay_put_Guid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_PossListId_Proxy(
	IVwOverlay * This,
	/* [retval][out] */ long *ppsslId);


void __RPC_STUB IVwOverlay_get_PossListId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwOverlay_put_PossListId_Proxy(
	IVwOverlay * This,
	/* [in] */ long psslId);


void __RPC_STUB IVwOverlay_put_PossListId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_Flags_Proxy(
	IVwOverlay * This,
	/* [retval][out] */ VwOverlayFlags *pvof);


void __RPC_STUB IVwOverlay_get_Flags_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwOverlay_put_Flags_Proxy(
	IVwOverlay * This,
	/* [in] */ VwOverlayFlags vof);


void __RPC_STUB IVwOverlay_put_Flags_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_FontName_Proxy(
	IVwOverlay * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IVwOverlay_get_FontName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwOverlay_put_FontName_Proxy(
	IVwOverlay * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB IVwOverlay_put_FontName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_FontNameRgch_Proxy(
	IVwOverlay * This,
	/* [size_is][out] */ OLECHAR *prgch);


void __RPC_STUB IVwOverlay_FontNameRgch_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_FontSize_Proxy(
	IVwOverlay * This,
	/* [retval][out] */ int *pmp);


void __RPC_STUB IVwOverlay_get_FontSize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwOverlay_put_FontSize_Proxy(
	IVwOverlay * This,
	/* [in] */ int mp);


void __RPC_STUB IVwOverlay_put_FontSize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_MaxShowTags_Proxy(
	IVwOverlay * This,
	/* [retval][out] */ int *pctag);


void __RPC_STUB IVwOverlay_get_MaxShowTags_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwOverlay_put_MaxShowTags_Proxy(
	IVwOverlay * This,
	/* [in] */ int ctag);


void __RPC_STUB IVwOverlay_put_MaxShowTags_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwOverlay_get_CTags_Proxy(
	IVwOverlay * This,
	/* [retval][out] */ int *pctag);


void __RPC_STUB IVwOverlay_get_CTags_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_GetDbTagInfo_Proxy(
	IVwOverlay * This,
	/* [in] */ int itag,
	/* [out] */ long *phvo,
	/* [out] */ COLORREF *pclrFore,
	/* [out] */ COLORREF *pclrBack,
	/* [out] */ COLORREF *pclrUnder,
	/* [out] */ int *punt,
	/* [out] */ ComBool *pfHidden,
	/* [size_is][out] */ OLECHAR *prgchGuid);


void __RPC_STUB IVwOverlay_GetDbTagInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_SetTagInfo_Proxy(
	IVwOverlay * This,
	/* [size_is][in] */ OLECHAR *prgchGuid,
	/* [in] */ long hvo,
	/* [in] */ int grfosm,
	/* [in] */ BSTR bstrAbbr,
	/* [in] */ BSTR bstrName,
	/* [in] */ COLORREF clrFore,
	/* [in] */ COLORREF clrBack,
	/* [in] */ COLORREF clrUnder,
	/* [in] */ int unt,
	/* [in] */ ComBool fHidden);


void __RPC_STUB IVwOverlay_SetTagInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_GetTagInfo_Proxy(
	IVwOverlay * This,
	/* [size_is][in] */ OLECHAR *prgchGuid,
	/* [out] */ long *phvo,
	/* [out] */ BSTR *pbstrAbbr,
	/* [out] */ BSTR *pbstrName,
	/* [out] */ COLORREF *pclrFore,
	/* [out] */ COLORREF *pclrBack,
	/* [out] */ COLORREF *pclrUnder,
	/* [out] */ int *punt,
	/* [out] */ ComBool *pfHidden);


void __RPC_STUB IVwOverlay_GetTagInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_GetDlgTagInfo_Proxy(
	IVwOverlay * This,
	/* [in] */ int itag,
	/* [out] */ ComBool *pfHidden,
	/* [out] */ COLORREF *pclrFore,
	/* [out] */ COLORREF *pclrBack,
	/* [out] */ COLORREF *pclrUnder,
	/* [out] */ int *punt,
	/* [out] */ BSTR *pbstrAbbr,
	/* [out] */ BSTR *pbstrName);


void __RPC_STUB IVwOverlay_GetDlgTagInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_GetDispTagInfo_Proxy(
	IVwOverlay * This,
	/* [size_is][in] */ OLECHAR *prgchGuid,
	/* [out] */ ComBool *pfHidden,
	/* [out] */ COLORREF *pclrFore,
	/* [out] */ COLORREF *pclrBack,
	/* [out] */ COLORREF *pclrUnder,
	/* [out] */ int *punt,
	/* [size_is][out] */ OLECHAR *prgchAbbr,
	/* [in] */ int cchMaxAbbr,
	/* [out] */ int *pcchAbbr,
	/* [size_is][out] */ OLECHAR *prgchName,
	/* [in] */ int cchMaxName,
	/* [out] */ int *pcchName);


void __RPC_STUB IVwOverlay_GetDispTagInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_RemoveTag_Proxy(
	IVwOverlay * This,
	/* [size_is][in] */ OLECHAR *prgchGuid);


void __RPC_STUB IVwOverlay_RemoveTag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_Sort_Proxy(
	IVwOverlay * This,
	/* [in] */ ComBool fByAbbr);


void __RPC_STUB IVwOverlay_Sort_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwOverlay_Merge_Proxy(
	IVwOverlay * This,
	/* [in] */ IVwOverlay *pvo,
	/* [retval][out] */ IVwOverlay **ppvoMerged);


void __RPC_STUB IVwOverlay_Merge_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwOverlay_INTERFACE_DEFINED__ */


#ifndef __IEventListener_INTERFACE_DEFINED__
#define __IEventListener_INTERFACE_DEFINED__

/* interface IEventListener */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IEventListener;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("F696B01E-974B-4065-B464-BDF459154054")
	IEventListener : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Notify(
			/* [in] */ int nArg1,
			/* [in] */ int nArg2) = 0;

	};

#else 	/* C style interface */

	typedef struct IEventListenerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IEventListener * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IEventListener * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IEventListener * This);

		HRESULT ( STDMETHODCALLTYPE *Notify )(
			IEventListener * This,
			/* [in] */ int nArg1,
			/* [in] */ int nArg2);

		END_INTERFACE
	} IEventListenerVtbl;

	interface IEventListener
	{
		CONST_VTBL struct IEventListenerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IEventListener_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IEventListener_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IEventListener_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IEventListener_Notify(This,nArg1,nArg2)	\
	(This)->lpVtbl -> Notify(This,nArg1,nArg2)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IEventListener_Notify_Proxy(
	IEventListener * This,
	/* [in] */ int nArg1,
	/* [in] */ int nArg2);


void __RPC_STUB IEventListener_Notify_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IEventListener_INTERFACE_DEFINED__ */


#ifndef __IVwPrintContext_INTERFACE_DEFINED__
#define __IVwPrintContext_INTERFACE_DEFINED__

/* interface IVwPrintContext */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwPrintContext;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("FF2E1DC2-95A8-41c6-85F4-FFCA3A64216A")
	IVwPrintContext : public IUnknown
	{
	public:
		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Graphics(
			/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FirstPageNumber(
			/* [retval][out] */ int *pn) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsPageWanted(
			/* [in] */ int nPageNo,
			/* [retval][out] */ ComBool *pfWanted) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AreMorePagesWanted(
			/* [in] */ int nPageNo,
			/* [retval][out] */ ComBool *pfWanted) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Aborted(
			/* [retval][out] */ ComBool *pfAborted) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Copies(
			/* [retval][out] */ int *pnCopies) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Collate(
			/* [retval][out] */ ComBool *pfCollate) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HeaderString(
			/* [in] */ VwHeaderPositions grfvhp,
			/* [in] */ int pn,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetMargins(
			/* [out] */ int *pdxpLeft,
			/* [out] */ int *pdxpRight,
			/* [out] */ int *pdypHeader,
			/* [out] */ int *pdypTop,
			/* [out] */ int *pdypBottom,
			/* [out] */ int *pdypFooter) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenPage( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE ClosePage( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE OpenDoc( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseDoc( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LastPageNo(
			/* [retval][out] */ int *pnPageNo) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_HeaderMask(
			/* [in] */ VwHeaderPositions grfvhp) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetHeaderString(
			/* [in] */ VwHeaderPositions grfvhp,
			/* [in] */ /* external definition not present */ ITsString *ptss) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetMargins(
			/* [in] */ int dxpLeft,
			/* [in] */ int dxpRight,
			/* [in] */ int dypHeader,
			/* [in] */ int dypTop,
			/* [in] */ int dypBottom,
			/* [in] */ int dypFooter) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetPagePrintInfo(
			/* [in] */ int nFirstPageNo,
			/* [in] */ int nFirstPrintPage,
			/* [in] */ int nLastPrintPage,
			/* [in] */ int nCopies,
			/* [in] */ ComBool fCollate) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetGraphics(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg) = 0;

		virtual HRESULT STDMETHODCALLTYPE RequestAbort( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE AbortDoc( void) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwPrintContextVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwPrintContext * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwPrintContext * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwPrintContext * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Graphics )(
			IVwPrintContext * This,
			/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FirstPageNumber )(
			IVwPrintContext * This,
			/* [retval][out] */ int *pn);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsPageWanted )(
			IVwPrintContext * This,
			/* [in] */ int nPageNo,
			/* [retval][out] */ ComBool *pfWanted);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AreMorePagesWanted )(
			IVwPrintContext * This,
			/* [in] */ int nPageNo,
			/* [retval][out] */ ComBool *pfWanted);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Aborted )(
			IVwPrintContext * This,
			/* [retval][out] */ ComBool *pfAborted);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Copies )(
			IVwPrintContext * This,
			/* [retval][out] */ int *pnCopies);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Collate )(
			IVwPrintContext * This,
			/* [retval][out] */ ComBool *pfCollate);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HeaderString )(
			IVwPrintContext * This,
			/* [in] */ VwHeaderPositions grfvhp,
			/* [in] */ int pn,
			/* [retval][out] */ /* external definition not present */ ITsString **pptss);

		HRESULT ( STDMETHODCALLTYPE *GetMargins )(
			IVwPrintContext * This,
			/* [out] */ int *pdxpLeft,
			/* [out] */ int *pdxpRight,
			/* [out] */ int *pdypHeader,
			/* [out] */ int *pdypTop,
			/* [out] */ int *pdypBottom,
			/* [out] */ int *pdypFooter);

		HRESULT ( STDMETHODCALLTYPE *OpenPage )(
			IVwPrintContext * This);

		HRESULT ( STDMETHODCALLTYPE *ClosePage )(
			IVwPrintContext * This);

		HRESULT ( STDMETHODCALLTYPE *OpenDoc )(
			IVwPrintContext * This);

		HRESULT ( STDMETHODCALLTYPE *CloseDoc )(
			IVwPrintContext * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LastPageNo )(
			IVwPrintContext * This,
			/* [retval][out] */ int *pnPageNo);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_HeaderMask )(
			IVwPrintContext * This,
			/* [in] */ VwHeaderPositions grfvhp);

		HRESULT ( STDMETHODCALLTYPE *SetHeaderString )(
			IVwPrintContext * This,
			/* [in] */ VwHeaderPositions grfvhp,
			/* [in] */ /* external definition not present */ ITsString *ptss);

		HRESULT ( STDMETHODCALLTYPE *SetMargins )(
			IVwPrintContext * This,
			/* [in] */ int dxpLeft,
			/* [in] */ int dxpRight,
			/* [in] */ int dypHeader,
			/* [in] */ int dypTop,
			/* [in] */ int dypBottom,
			/* [in] */ int dypFooter);

		HRESULT ( STDMETHODCALLTYPE *SetPagePrintInfo )(
			IVwPrintContext * This,
			/* [in] */ int nFirstPageNo,
			/* [in] */ int nFirstPrintPage,
			/* [in] */ int nLastPrintPage,
			/* [in] */ int nCopies,
			/* [in] */ ComBool fCollate);

		HRESULT ( STDMETHODCALLTYPE *SetGraphics )(
			IVwPrintContext * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg);

		HRESULT ( STDMETHODCALLTYPE *RequestAbort )(
			IVwPrintContext * This);

		HRESULT ( STDMETHODCALLTYPE *AbortDoc )(
			IVwPrintContext * This);

		END_INTERFACE
	} IVwPrintContextVtbl;

	interface IVwPrintContext
	{
		CONST_VTBL struct IVwPrintContextVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwPrintContext_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwPrintContext_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwPrintContext_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwPrintContext_get_Graphics(This,ppvg)	\
	(This)->lpVtbl -> get_Graphics(This,ppvg)

#define IVwPrintContext_get_FirstPageNumber(This,pn)	\
	(This)->lpVtbl -> get_FirstPageNumber(This,pn)

#define IVwPrintContext_get_IsPageWanted(This,nPageNo,pfWanted)	\
	(This)->lpVtbl -> get_IsPageWanted(This,nPageNo,pfWanted)

#define IVwPrintContext_get_AreMorePagesWanted(This,nPageNo,pfWanted)	\
	(This)->lpVtbl -> get_AreMorePagesWanted(This,nPageNo,pfWanted)

#define IVwPrintContext_get_Aborted(This,pfAborted)	\
	(This)->lpVtbl -> get_Aborted(This,pfAborted)

#define IVwPrintContext_get_Copies(This,pnCopies)	\
	(This)->lpVtbl -> get_Copies(This,pnCopies)

#define IVwPrintContext_get_Collate(This,pfCollate)	\
	(This)->lpVtbl -> get_Collate(This,pfCollate)

#define IVwPrintContext_get_HeaderString(This,grfvhp,pn,pptss)	\
	(This)->lpVtbl -> get_HeaderString(This,grfvhp,pn,pptss)

#define IVwPrintContext_GetMargins(This,pdxpLeft,pdxpRight,pdypHeader,pdypTop,pdypBottom,pdypFooter)	\
	(This)->lpVtbl -> GetMargins(This,pdxpLeft,pdxpRight,pdypHeader,pdypTop,pdypBottom,pdypFooter)

#define IVwPrintContext_OpenPage(This)	\
	(This)->lpVtbl -> OpenPage(This)

#define IVwPrintContext_ClosePage(This)	\
	(This)->lpVtbl -> ClosePage(This)

#define IVwPrintContext_OpenDoc(This)	\
	(This)->lpVtbl -> OpenDoc(This)

#define IVwPrintContext_CloseDoc(This)	\
	(This)->lpVtbl -> CloseDoc(This)

#define IVwPrintContext_get_LastPageNo(This,pnPageNo)	\
	(This)->lpVtbl -> get_LastPageNo(This,pnPageNo)

#define IVwPrintContext_put_HeaderMask(This,grfvhp)	\
	(This)->lpVtbl -> put_HeaderMask(This,grfvhp)

#define IVwPrintContext_SetHeaderString(This,grfvhp,ptss)	\
	(This)->lpVtbl -> SetHeaderString(This,grfvhp,ptss)

#define IVwPrintContext_SetMargins(This,dxpLeft,dxpRight,dypHeader,dypTop,dypBottom,dypFooter)	\
	(This)->lpVtbl -> SetMargins(This,dxpLeft,dxpRight,dypHeader,dypTop,dypBottom,dypFooter)

#define IVwPrintContext_SetPagePrintInfo(This,nFirstPageNo,nFirstPrintPage,nLastPrintPage,nCopies,fCollate)	\
	(This)->lpVtbl -> SetPagePrintInfo(This,nFirstPageNo,nFirstPrintPage,nLastPrintPage,nCopies,fCollate)

#define IVwPrintContext_SetGraphics(This,pvg)	\
	(This)->lpVtbl -> SetGraphics(This,pvg)

#define IVwPrintContext_RequestAbort(This)	\
	(This)->lpVtbl -> RequestAbort(This)

#define IVwPrintContext_AbortDoc(This)	\
	(This)->lpVtbl -> AbortDoc(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_Graphics_Proxy(
	IVwPrintContext * This,
	/* [retval][out] */ /* external definition not present */ IVwGraphics **ppvg);


void __RPC_STUB IVwPrintContext_get_Graphics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_FirstPageNumber_Proxy(
	IVwPrintContext * This,
	/* [retval][out] */ int *pn);


void __RPC_STUB IVwPrintContext_get_FirstPageNumber_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_IsPageWanted_Proxy(
	IVwPrintContext * This,
	/* [in] */ int nPageNo,
	/* [retval][out] */ ComBool *pfWanted);


void __RPC_STUB IVwPrintContext_get_IsPageWanted_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_AreMorePagesWanted_Proxy(
	IVwPrintContext * This,
	/* [in] */ int nPageNo,
	/* [retval][out] */ ComBool *pfWanted);


void __RPC_STUB IVwPrintContext_get_AreMorePagesWanted_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_Aborted_Proxy(
	IVwPrintContext * This,
	/* [retval][out] */ ComBool *pfAborted);


void __RPC_STUB IVwPrintContext_get_Aborted_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_Copies_Proxy(
	IVwPrintContext * This,
	/* [retval][out] */ int *pnCopies);


void __RPC_STUB IVwPrintContext_get_Copies_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_Collate_Proxy(
	IVwPrintContext * This,
	/* [retval][out] */ ComBool *pfCollate);


void __RPC_STUB IVwPrintContext_get_Collate_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_HeaderString_Proxy(
	IVwPrintContext * This,
	/* [in] */ VwHeaderPositions grfvhp,
	/* [in] */ int pn,
	/* [retval][out] */ /* external definition not present */ ITsString **pptss);


void __RPC_STUB IVwPrintContext_get_HeaderString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_GetMargins_Proxy(
	IVwPrintContext * This,
	/* [out] */ int *pdxpLeft,
	/* [out] */ int *pdxpRight,
	/* [out] */ int *pdypHeader,
	/* [out] */ int *pdypTop,
	/* [out] */ int *pdypBottom,
	/* [out] */ int *pdypFooter);


void __RPC_STUB IVwPrintContext_GetMargins_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_OpenPage_Proxy(
	IVwPrintContext * This);


void __RPC_STUB IVwPrintContext_OpenPage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_ClosePage_Proxy(
	IVwPrintContext * This);


void __RPC_STUB IVwPrintContext_ClosePage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_OpenDoc_Proxy(
	IVwPrintContext * This);


void __RPC_STUB IVwPrintContext_OpenDoc_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_CloseDoc_Proxy(
	IVwPrintContext * This);


void __RPC_STUB IVwPrintContext_CloseDoc_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_get_LastPageNo_Proxy(
	IVwPrintContext * This,
	/* [retval][out] */ int *pnPageNo);


void __RPC_STUB IVwPrintContext_get_LastPageNo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPrintContext_put_HeaderMask_Proxy(
	IVwPrintContext * This,
	/* [in] */ VwHeaderPositions grfvhp);


void __RPC_STUB IVwPrintContext_put_HeaderMask_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_SetHeaderString_Proxy(
	IVwPrintContext * This,
	/* [in] */ VwHeaderPositions grfvhp,
	/* [in] */ /* external definition not present */ ITsString *ptss);


void __RPC_STUB IVwPrintContext_SetHeaderString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_SetMargins_Proxy(
	IVwPrintContext * This,
	/* [in] */ int dxpLeft,
	/* [in] */ int dxpRight,
	/* [in] */ int dypHeader,
	/* [in] */ int dypTop,
	/* [in] */ int dypBottom,
	/* [in] */ int dypFooter);


void __RPC_STUB IVwPrintContext_SetMargins_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_SetPagePrintInfo_Proxy(
	IVwPrintContext * This,
	/* [in] */ int nFirstPageNo,
	/* [in] */ int nFirstPrintPage,
	/* [in] */ int nLastPrintPage,
	/* [in] */ int nCopies,
	/* [in] */ ComBool fCollate);


void __RPC_STUB IVwPrintContext_SetPagePrintInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_SetGraphics_Proxy(
	IVwPrintContext * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg);


void __RPC_STUB IVwPrintContext_SetGraphics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_RequestAbort_Proxy(
	IVwPrintContext * This);


void __RPC_STUB IVwPrintContext_RequestAbort_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPrintContext_AbortDoc_Proxy(
	IVwPrintContext * This);


void __RPC_STUB IVwPrintContext_AbortDoc_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwPrintContext_INTERFACE_DEFINED__ */


#ifndef __ISqlUndoAction_INTERFACE_DEFINED__
#define __ISqlUndoAction_INTERFACE_DEFINED__

/* interface ISqlUndoAction */
/* [unique][object][uuid] */


EXTERN_C const IID IID_ISqlUndoAction;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("2225FCC7-51AE-4461-930C-A42A8DC5A81A")
	ISqlUndoAction : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE AddRedoCommand(
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddUndoCommand(
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql) = 0;

		virtual HRESULT STDMETHODCALLTYPE VerifyUndoable(
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql) = 0;

		virtual HRESULT STDMETHODCALLTYPE VerifyRedoable(
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddRedoReloadInfo(
			/* [in] */ IVwOleDbDa *podda,
			/* [in] */ BSTR bstrSqlReloadData,
			/* [in] */ IDbColSpec *pdcs,
			/* [in] */ long hvoBase,
			/* [in] */ int nrowMax,
			/* [in] */ /* external definition not present */ IAdvInd *padvi) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddUndoReloadInfo(
			/* [in] */ IVwOleDbDa *podda,
			/* [in] */ BSTR bstrSqlReloadData,
			/* [in] */ IDbColSpec *pdcs,
			/* [in] */ long hvoBase,
			/* [in] */ int nrowMax,
			/* [in] */ /* external definition not present */ IAdvInd *padvi) = 0;

	};

#else 	/* C style interface */

	typedef struct ISqlUndoActionVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ISqlUndoAction * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ISqlUndoAction * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ISqlUndoAction * This);

		HRESULT ( STDMETHODCALLTYPE *AddRedoCommand )(
			ISqlUndoAction * This,
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql);

		HRESULT ( STDMETHODCALLTYPE *AddUndoCommand )(
			ISqlUndoAction * This,
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql);

		HRESULT ( STDMETHODCALLTYPE *VerifyUndoable )(
			ISqlUndoAction * This,
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql);

		HRESULT ( STDMETHODCALLTYPE *VerifyRedoable )(
			ISqlUndoAction * This,
			/* [in] */ IUnknown *pode,
			/* [in] */ IUnknown *podc,
			/* [in] */ BSTR bstrSql);

		HRESULT ( STDMETHODCALLTYPE *AddRedoReloadInfo )(
			ISqlUndoAction * This,
			/* [in] */ IVwOleDbDa *podda,
			/* [in] */ BSTR bstrSqlReloadData,
			/* [in] */ IDbColSpec *pdcs,
			/* [in] */ long hvoBase,
			/* [in] */ int nrowMax,
			/* [in] */ /* external definition not present */ IAdvInd *padvi);

		HRESULT ( STDMETHODCALLTYPE *AddUndoReloadInfo )(
			ISqlUndoAction * This,
			/* [in] */ IVwOleDbDa *podda,
			/* [in] */ BSTR bstrSqlReloadData,
			/* [in] */ IDbColSpec *pdcs,
			/* [in] */ long hvoBase,
			/* [in] */ int nrowMax,
			/* [in] */ /* external definition not present */ IAdvInd *padvi);

		END_INTERFACE
	} ISqlUndoActionVtbl;

	interface ISqlUndoAction
	{
		CONST_VTBL struct ISqlUndoActionVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ISqlUndoAction_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define ISqlUndoAction_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define ISqlUndoAction_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define ISqlUndoAction_AddRedoCommand(This,pode,podc,bstrSql)	\
	(This)->lpVtbl -> AddRedoCommand(This,pode,podc,bstrSql)

#define ISqlUndoAction_AddUndoCommand(This,pode,podc,bstrSql)	\
	(This)->lpVtbl -> AddUndoCommand(This,pode,podc,bstrSql)

#define ISqlUndoAction_VerifyUndoable(This,pode,podc,bstrSql)	\
	(This)->lpVtbl -> VerifyUndoable(This,pode,podc,bstrSql)

#define ISqlUndoAction_VerifyRedoable(This,pode,podc,bstrSql)	\
	(This)->lpVtbl -> VerifyRedoable(This,pode,podc,bstrSql)

#define ISqlUndoAction_AddRedoReloadInfo(This,podda,bstrSqlReloadData,pdcs,hvoBase,nrowMax,padvi)	\
	(This)->lpVtbl -> AddRedoReloadInfo(This,podda,bstrSqlReloadData,pdcs,hvoBase,nrowMax,padvi)

#define ISqlUndoAction_AddUndoReloadInfo(This,podda,bstrSqlReloadData,pdcs,hvoBase,nrowMax,padvi)	\
	(This)->lpVtbl -> AddUndoReloadInfo(This,podda,bstrSqlReloadData,pdcs,hvoBase,nrowMax,padvi)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE ISqlUndoAction_AddRedoCommand_Proxy(
	ISqlUndoAction * This,
	/* [in] */ IUnknown *pode,
	/* [in] */ IUnknown *podc,
	/* [in] */ BSTR bstrSql);


void __RPC_STUB ISqlUndoAction_AddRedoCommand_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISqlUndoAction_AddUndoCommand_Proxy(
	ISqlUndoAction * This,
	/* [in] */ IUnknown *pode,
	/* [in] */ IUnknown *podc,
	/* [in] */ BSTR bstrSql);


void __RPC_STUB ISqlUndoAction_AddUndoCommand_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISqlUndoAction_VerifyUndoable_Proxy(
	ISqlUndoAction * This,
	/* [in] */ IUnknown *pode,
	/* [in] */ IUnknown *podc,
	/* [in] */ BSTR bstrSql);


void __RPC_STUB ISqlUndoAction_VerifyUndoable_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISqlUndoAction_VerifyRedoable_Proxy(
	ISqlUndoAction * This,
	/* [in] */ IUnknown *pode,
	/* [in] */ IUnknown *podc,
	/* [in] */ BSTR bstrSql);


void __RPC_STUB ISqlUndoAction_VerifyRedoable_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISqlUndoAction_AddRedoReloadInfo_Proxy(
	ISqlUndoAction * This,
	/* [in] */ IVwOleDbDa *podda,
	/* [in] */ BSTR bstrSqlReloadData,
	/* [in] */ IDbColSpec *pdcs,
	/* [in] */ long hvoBase,
	/* [in] */ int nrowMax,
	/* [in] */ /* external definition not present */ IAdvInd *padvi);


void __RPC_STUB ISqlUndoAction_AddRedoReloadInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE ISqlUndoAction_AddUndoReloadInfo_Proxy(
	ISqlUndoAction * This,
	/* [in] */ IVwOleDbDa *podda,
	/* [in] */ BSTR bstrSqlReloadData,
	/* [in] */ IDbColSpec *pdcs,
	/* [in] */ long hvoBase,
	/* [in] */ int nrowMax,
	/* [in] */ /* external definition not present */ IAdvInd *padvi);


void __RPC_STUB ISqlUndoAction_AddUndoReloadInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __ISqlUndoAction_INTERFACE_DEFINED__ */


#ifndef __IVwSearchKiller_INTERFACE_DEFINED__
#define __IVwSearchKiller_INTERFACE_DEFINED__

/* interface IVwSearchKiller */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwSearchKiller;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("FF1B39DE-20D3-4cdd-A134-DCBE3BE23F3E")
	IVwSearchKiller : public IUnknown
	{
	public:
		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Window(
			/* [in] */ int hwnd) = 0;

		virtual HRESULT STDMETHODCALLTYPE FlushMessages( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AbortRequest(
			/* [retval][out] */ ComBool *pfAbort) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_AbortRequest(
			/* [in] */ ComBool fAbort) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwSearchKillerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwSearchKiller * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwSearchKiller * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwSearchKiller * This);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Window )(
			IVwSearchKiller * This,
			/* [in] */ int hwnd);

		HRESULT ( STDMETHODCALLTYPE *FlushMessages )(
			IVwSearchKiller * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AbortRequest )(
			IVwSearchKiller * This,
			/* [retval][out] */ ComBool *pfAbort);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_AbortRequest )(
			IVwSearchKiller * This,
			/* [in] */ ComBool fAbort);

		END_INTERFACE
	} IVwSearchKillerVtbl;

	interface IVwSearchKiller
	{
		CONST_VTBL struct IVwSearchKillerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwSearchKiller_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwSearchKiller_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwSearchKiller_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwSearchKiller_put_Window(This,hwnd)	\
	(This)->lpVtbl -> put_Window(This,hwnd)

#define IVwSearchKiller_FlushMessages(This)	\
	(This)->lpVtbl -> FlushMessages(This)

#define IVwSearchKiller_get_AbortRequest(This,pfAbort)	\
	(This)->lpVtbl -> get_AbortRequest(This,pfAbort)

#define IVwSearchKiller_put_AbortRequest(This,fAbort)	\
	(This)->lpVtbl -> put_AbortRequest(This,fAbort)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propput] */ HRESULT STDMETHODCALLTYPE IVwSearchKiller_put_Window_Proxy(
	IVwSearchKiller * This,
	/* [in] */ int hwnd);


void __RPC_STUB IVwSearchKiller_put_Window_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwSearchKiller_FlushMessages_Proxy(
	IVwSearchKiller * This);


void __RPC_STUB IVwSearchKiller_FlushMessages_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwSearchKiller_get_AbortRequest_Proxy(
	IVwSearchKiller * This,
	/* [retval][out] */ ComBool *pfAbort);


void __RPC_STUB IVwSearchKiller_get_AbortRequest_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwSearchKiller_put_AbortRequest_Proxy(
	IVwSearchKiller * This,
	/* [in] */ ComBool fAbort);


void __RPC_STUB IVwSearchKiller_put_AbortRequest_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwSearchKiller_INTERFACE_DEFINED__ */


#ifndef __IVwSynchronizer_INTERFACE_DEFINED__
#define __IVwSynchronizer_INTERFACE_DEFINED__

/* interface IVwSynchronizer */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwSynchronizer;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("C5C1E9DC-5880-4ee3-B3CD-EBDD132A6294")
	IVwSynchronizer : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE AddRoot(
			/* [in] */ IVwRootBox *prootb) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwSynchronizerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwSynchronizer * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwSynchronizer * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwSynchronizer * This);

		HRESULT ( STDMETHODCALLTYPE *AddRoot )(
			IVwSynchronizer * This,
			/* [in] */ IVwRootBox *prootb);

		END_INTERFACE
	} IVwSynchronizerVtbl;

	interface IVwSynchronizer
	{
		CONST_VTBL struct IVwSynchronizerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwSynchronizer_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwSynchronizer_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwSynchronizer_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwSynchronizer_AddRoot(This,prootb)	\
	(This)->lpVtbl -> AddRoot(This,prootb)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwSynchronizer_AddRoot_Proxy(
	IVwSynchronizer * This,
	/* [in] */ IVwRootBox *prootb);


void __RPC_STUB IVwSynchronizer_AddRoot_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwSynchronizer_INTERFACE_DEFINED__ */


#ifndef __IVwDataSpec_INTERFACE_DEFINED__
#define __IVwDataSpec_INTERFACE_DEFINED__

/* interface IVwDataSpec */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwDataSpec;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("DC9A7C08-138E-41C0-8532-5FD64B5E72BF")
	IVwDataSpec : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE AddField(
			/* [in] */ int clsid,
			/* [in] */ int tag,
			/* [in] */ FldType ft,
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int ws) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwDataSpecVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwDataSpec * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwDataSpec * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwDataSpec * This);

		HRESULT ( STDMETHODCALLTYPE *AddField )(
			IVwDataSpec * This,
			/* [in] */ int clsid,
			/* [in] */ int tag,
			/* [in] */ FldType ft,
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf,
			/* [in] */ int ws);

		END_INTERFACE
	} IVwDataSpecVtbl;

	interface IVwDataSpec
	{
		CONST_VTBL struct IVwDataSpecVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwDataSpec_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwDataSpec_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwDataSpec_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwDataSpec_AddField(This,clsid,tag,ft,pwsf,ws)	\
	(This)->lpVtbl -> AddField(This,clsid,tag,ft,pwsf,ws)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwDataSpec_AddField_Proxy(
	IVwDataSpec * This,
	/* [in] */ int clsid,
	/* [in] */ int tag,
	/* [in] */ FldType ft,
	/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf,
	/* [in] */ int ws);


void __RPC_STUB IVwDataSpec_AddField_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwDataSpec_INTERFACE_DEFINED__ */


#ifndef __IVwNotifyObjCharDeletion_INTERFACE_DEFINED__
#define __IVwNotifyObjCharDeletion_INTERFACE_DEFINED__

/* interface IVwNotifyObjCharDeletion */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwNotifyObjCharDeletion;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("CF1E5D07-B479-4195-B64C-02931F86014D")
	IVwNotifyObjCharDeletion : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE ObjDeleted(
			/* [in] */ GUID *pguid) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwNotifyObjCharDeletionVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwNotifyObjCharDeletion * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwNotifyObjCharDeletion * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwNotifyObjCharDeletion * This);

		HRESULT ( STDMETHODCALLTYPE *ObjDeleted )(
			IVwNotifyObjCharDeletion * This,
			/* [in] */ GUID *pguid);

		END_INTERFACE
	} IVwNotifyObjCharDeletionVtbl;

	interface IVwNotifyObjCharDeletion
	{
		CONST_VTBL struct IVwNotifyObjCharDeletionVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwNotifyObjCharDeletion_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwNotifyObjCharDeletion_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwNotifyObjCharDeletion_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwNotifyObjCharDeletion_ObjDeleted(This,pguid)	\
	(This)->lpVtbl -> ObjDeleted(This,pguid)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwNotifyObjCharDeletion_ObjDeleted_Proxy(
	IVwNotifyObjCharDeletion * This,
	/* [in] */ GUID *pguid);


void __RPC_STUB IVwNotifyObjCharDeletion_ObjDeleted_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwNotifyObjCharDeletion_INTERFACE_DEFINED__ */


#ifndef __IVwVirtualHandler_INTERFACE_DEFINED__
#define __IVwVirtualHandler_INTERFACE_DEFINED__

/* interface IVwVirtualHandler */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwVirtualHandler;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("F8851137-6562-4120-A34E-1A51EE598EA7")
	IVwVirtualHandler : public IUnknown
	{
	public:
		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ClassName(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FieldName(
			/* [in] */ BSTR bstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FieldName(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Tag(
			/* [in] */ int tag) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Tag(
			/* [retval][out] */ int *ptag) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Type(
			/* [in] */ int cpt) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Type(
			/* [retval][out] */ int *pcpt) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Writeable(
			/* [in] */ ComBool f) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Writeable(
			/* [retval][out] */ ComBool *pf) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ComputeEveryTime(
			/* [in] */ ComBool f) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ComputeEveryTime(
			/* [retval][out] */ ComBool *pf) = 0;

		virtual HRESULT STDMETHODCALLTYPE Load(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwCacheDa *pcda) = 0;

		virtual HRESULT STDMETHODCALLTYPE Replace(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ihvoMin,
			/* [in] */ int ihvoLim,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo,
			/* [in] */ ISilDataAccess *psda) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteObj(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IUnknown *punk,
			/* [in] */ ISilDataAccess *psda) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteInt64(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ __int64 val,
			/* [in] */ ISilDataAccess *psda) = 0;

		virtual HRESULT STDMETHODCALLTYPE WriteUnicode(
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ BSTR bstr,
			/* [in] */ ISilDataAccess *psda) = 0;

		virtual HRESULT STDMETHODCALLTYPE PreLoad(
			/* [in] */ int chvo,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwCacheDa *pcda) = 0;

		virtual HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ BSTR bstrData) = 0;

		virtual HRESULT STDMETHODCALLTYPE DoesResultDependOnProp(
			/* [in] */ long hvoObj,
			/* [in] */ long hvoChange,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [retval][out] */ ComBool *pfDepends) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwVirtualHandlerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwVirtualHandler * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwVirtualHandler * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwVirtualHandler * This);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ClassName )(
			IVwVirtualHandler * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassName )(
			IVwVirtualHandler * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FieldName )(
			IVwVirtualHandler * This,
			/* [in] */ BSTR bstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FieldName )(
			IVwVirtualHandler * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Tag )(
			IVwVirtualHandler * This,
			/* [in] */ int tag);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Tag )(
			IVwVirtualHandler * This,
			/* [retval][out] */ int *ptag);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Type )(
			IVwVirtualHandler * This,
			/* [in] */ int cpt);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Type )(
			IVwVirtualHandler * This,
			/* [retval][out] */ int *pcpt);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Writeable )(
			IVwVirtualHandler * This,
			/* [in] */ ComBool f);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Writeable )(
			IVwVirtualHandler * This,
			/* [retval][out] */ ComBool *pf);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ComputeEveryTime )(
			IVwVirtualHandler * This,
			/* [in] */ ComBool f);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ComputeEveryTime )(
			IVwVirtualHandler * This,
			/* [retval][out] */ ComBool *pf);

		HRESULT ( STDMETHODCALLTYPE *Load )(
			IVwVirtualHandler * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwCacheDa *pcda);

		HRESULT ( STDMETHODCALLTYPE *Replace )(
			IVwVirtualHandler * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ihvoMin,
			/* [in] */ int ihvoLim,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int chvo,
			/* [in] */ ISilDataAccess *psda);

		HRESULT ( STDMETHODCALLTYPE *WriteObj )(
			IVwVirtualHandler * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IUnknown *punk,
			/* [in] */ ISilDataAccess *psda);

		HRESULT ( STDMETHODCALLTYPE *WriteInt64 )(
			IVwVirtualHandler * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ __int64 val,
			/* [in] */ ISilDataAccess *psda);

		HRESULT ( STDMETHODCALLTYPE *WriteUnicode )(
			IVwVirtualHandler * This,
			/* [in] */ long hvo,
			/* [in] */ int tag,
			/* [in] */ BSTR bstr,
			/* [in] */ ISilDataAccess *psda);

		HRESULT ( STDMETHODCALLTYPE *PreLoad )(
			IVwVirtualHandler * This,
			/* [in] */ int chvo,
			/* [size_is][in] */ long *prghvo,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [in] */ IVwCacheDa *pcda);

		HRESULT ( STDMETHODCALLTYPE *Initialize )(
			IVwVirtualHandler * This,
			/* [in] */ BSTR bstrData);

		HRESULT ( STDMETHODCALLTYPE *DoesResultDependOnProp )(
			IVwVirtualHandler * This,
			/* [in] */ long hvoObj,
			/* [in] */ long hvoChange,
			/* [in] */ int tag,
			/* [in] */ int ws,
			/* [retval][out] */ ComBool *pfDepends);

		END_INTERFACE
	} IVwVirtualHandlerVtbl;

	interface IVwVirtualHandler
	{
		CONST_VTBL struct IVwVirtualHandlerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwVirtualHandler_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwVirtualHandler_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwVirtualHandler_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwVirtualHandler_put_ClassName(This,bstr)	\
	(This)->lpVtbl -> put_ClassName(This,bstr)

#define IVwVirtualHandler_get_ClassName(This,pbstr)	\
	(This)->lpVtbl -> get_ClassName(This,pbstr)

#define IVwVirtualHandler_put_FieldName(This,bstr)	\
	(This)->lpVtbl -> put_FieldName(This,bstr)

#define IVwVirtualHandler_get_FieldName(This,pbstr)	\
	(This)->lpVtbl -> get_FieldName(This,pbstr)

#define IVwVirtualHandler_put_Tag(This,tag)	\
	(This)->lpVtbl -> put_Tag(This,tag)

#define IVwVirtualHandler_get_Tag(This,ptag)	\
	(This)->lpVtbl -> get_Tag(This,ptag)

#define IVwVirtualHandler_put_Type(This,cpt)	\
	(This)->lpVtbl -> put_Type(This,cpt)

#define IVwVirtualHandler_get_Type(This,pcpt)	\
	(This)->lpVtbl -> get_Type(This,pcpt)

#define IVwVirtualHandler_put_Writeable(This,f)	\
	(This)->lpVtbl -> put_Writeable(This,f)

#define IVwVirtualHandler_get_Writeable(This,pf)	\
	(This)->lpVtbl -> get_Writeable(This,pf)

#define IVwVirtualHandler_put_ComputeEveryTime(This,f)	\
	(This)->lpVtbl -> put_ComputeEveryTime(This,f)

#define IVwVirtualHandler_get_ComputeEveryTime(This,pf)	\
	(This)->lpVtbl -> get_ComputeEveryTime(This,pf)

#define IVwVirtualHandler_Load(This,hvo,tag,ws,pcda)	\
	(This)->lpVtbl -> Load(This,hvo,tag,ws,pcda)

#define IVwVirtualHandler_Replace(This,hvo,tag,ihvoMin,ihvoLim,prghvo,chvo,psda)	\
	(This)->lpVtbl -> Replace(This,hvo,tag,ihvoMin,ihvoLim,prghvo,chvo,psda)

#define IVwVirtualHandler_WriteObj(This,hvo,tag,ws,punk,psda)	\
	(This)->lpVtbl -> WriteObj(This,hvo,tag,ws,punk,psda)

#define IVwVirtualHandler_WriteInt64(This,hvo,tag,val,psda)	\
	(This)->lpVtbl -> WriteInt64(This,hvo,tag,val,psda)

#define IVwVirtualHandler_WriteUnicode(This,hvo,tag,bstr,psda)	\
	(This)->lpVtbl -> WriteUnicode(This,hvo,tag,bstr,psda)

#define IVwVirtualHandler_PreLoad(This,chvo,prghvo,tag,ws,pcda)	\
	(This)->lpVtbl -> PreLoad(This,chvo,prghvo,tag,ws,pcda)

#define IVwVirtualHandler_Initialize(This,bstrData)	\
	(This)->lpVtbl -> Initialize(This,bstrData)

#define IVwVirtualHandler_DoesResultDependOnProp(This,hvoObj,hvoChange,tag,ws,pfDepends)	\
	(This)->lpVtbl -> DoesResultDependOnProp(This,hvoObj,hvoChange,tag,ws,pfDepends)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propput] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_put_ClassName_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB IVwVirtualHandler_put_ClassName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_get_ClassName_Proxy(
	IVwVirtualHandler * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IVwVirtualHandler_get_ClassName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_put_FieldName_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ BSTR bstr);


void __RPC_STUB IVwVirtualHandler_put_FieldName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_get_FieldName_Proxy(
	IVwVirtualHandler * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IVwVirtualHandler_get_FieldName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_put_Tag_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ int tag);


void __RPC_STUB IVwVirtualHandler_put_Tag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_get_Tag_Proxy(
	IVwVirtualHandler * This,
	/* [retval][out] */ int *ptag);


void __RPC_STUB IVwVirtualHandler_get_Tag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_put_Type_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ int cpt);


void __RPC_STUB IVwVirtualHandler_put_Type_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_get_Type_Proxy(
	IVwVirtualHandler * This,
	/* [retval][out] */ int *pcpt);


void __RPC_STUB IVwVirtualHandler_get_Type_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_put_Writeable_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ ComBool f);


void __RPC_STUB IVwVirtualHandler_put_Writeable_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_get_Writeable_Proxy(
	IVwVirtualHandler * This,
	/* [retval][out] */ ComBool *pf);


void __RPC_STUB IVwVirtualHandler_get_Writeable_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_put_ComputeEveryTime_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ ComBool f);


void __RPC_STUB IVwVirtualHandler_put_ComputeEveryTime_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwVirtualHandler_get_ComputeEveryTime_Proxy(
	IVwVirtualHandler * This,
	/* [retval][out] */ ComBool *pf);


void __RPC_STUB IVwVirtualHandler_get_ComputeEveryTime_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_Load_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [in] */ IVwCacheDa *pcda);


void __RPC_STUB IVwVirtualHandler_Load_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_Replace_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int ihvoMin,
	/* [in] */ int ihvoLim,
	/* [size_is][in] */ long *prghvo,
	/* [in] */ int chvo,
	/* [in] */ ISilDataAccess *psda);


void __RPC_STUB IVwVirtualHandler_Replace_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_WriteObj_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [in] */ IUnknown *punk,
	/* [in] */ ISilDataAccess *psda);


void __RPC_STUB IVwVirtualHandler_WriteObj_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_WriteInt64_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ __int64 val,
	/* [in] */ ISilDataAccess *psda);


void __RPC_STUB IVwVirtualHandler_WriteInt64_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_WriteUnicode_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ long hvo,
	/* [in] */ int tag,
	/* [in] */ BSTR bstr,
	/* [in] */ ISilDataAccess *psda);


void __RPC_STUB IVwVirtualHandler_WriteUnicode_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_PreLoad_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ int chvo,
	/* [size_is][in] */ long *prghvo,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [in] */ IVwCacheDa *pcda);


void __RPC_STUB IVwVirtualHandler_PreLoad_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_Initialize_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ BSTR bstrData);


void __RPC_STUB IVwVirtualHandler_Initialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwVirtualHandler_DoesResultDependOnProp_Proxy(
	IVwVirtualHandler * This,
	/* [in] */ long hvoObj,
	/* [in] */ long hvoChange,
	/* [in] */ int tag,
	/* [in] */ int ws,
	/* [retval][out] */ ComBool *pfDepends);


void __RPC_STUB IVwVirtualHandler_DoesResultDependOnProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwVirtualHandler_INTERFACE_DEFINED__ */


#ifndef __IVwLayoutStream_INTERFACE_DEFINED__
#define __IVwLayoutStream_INTERFACE_DEFINED__

/* interface IVwLayoutStream */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwLayoutStream;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("963E6A91-513F-4490-A282-0E99B542B4CC")
	IVwLayoutStream : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE SetManager(
			/* [in] */ IVwLayoutManager *plm) = 0;

		virtual HRESULT STDMETHODCALLTYPE LayoutObj(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int dxsAvailWidth,
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvsli,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int hPage) = 0;

		virtual HRESULT STDMETHODCALLTYPE LayoutPage(
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int dxsAvailWidth,
			/* [in] */ int dysAvailHeight,
			/* [out][in] */ int *pysStartThisPage,
			/* [in] */ int hPage,
			/* [out] */ int *pdysUsedHeight,
			/* [out] */ int *pysStartNextPage) = 0;

		virtual HRESULT STDMETHODCALLTYPE DiscardPage(
			/* [in] */ int hPage) = 0;

		virtual HRESULT STDMETHODCALLTYPE PageBoundary(
			/* [in] */ int hPage,
			/* [in] */ ComBool fEnd,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE PageHeight(
			/* [in] */ int hPage,
			/* [retval][out] */ int *pdysHeight) = 0;

		virtual HRESULT STDMETHODCALLTYPE PagePostion(
			/* [in] */ int hPage,
			/* [retval][out] */ int *pysPosition) = 0;

		virtual HRESULT STDMETHODCALLTYPE RollbackLayoutObjects(
			/* [in] */ int hPage) = 0;

		virtual HRESULT STDMETHODCALLTYPE CommitLayoutObjects(
			/* [in] */ int hPage) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwLayoutStreamVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwLayoutStream * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwLayoutStream * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwLayoutStream * This);

		HRESULT ( STDMETHODCALLTYPE *SetManager )(
			IVwLayoutStream * This,
			/* [in] */ IVwLayoutManager *plm);

		HRESULT ( STDMETHODCALLTYPE *LayoutObj )(
			IVwLayoutStream * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int dxsAvailWidth,
			/* [in] */ int ihvoRoot,
			/* [in] */ int cvsli,
			/* [size_is][in] */ VwSelLevInfo *prgvsli,
			/* [in] */ int hPage);

		HRESULT ( STDMETHODCALLTYPE *LayoutPage )(
			IVwLayoutStream * This,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int dxsAvailWidth,
			/* [in] */ int dysAvailHeight,
			/* [out][in] */ int *pysStartThisPage,
			/* [in] */ int hPage,
			/* [out] */ int *pdysUsedHeight,
			/* [out] */ int *pysStartNextPage);

		HRESULT ( STDMETHODCALLTYPE *DiscardPage )(
			IVwLayoutStream * This,
			/* [in] */ int hPage);

		HRESULT ( STDMETHODCALLTYPE *PageBoundary )(
			IVwLayoutStream * This,
			/* [in] */ int hPage,
			/* [in] */ ComBool fEnd,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *PageHeight )(
			IVwLayoutStream * This,
			/* [in] */ int hPage,
			/* [retval][out] */ int *pdysHeight);

		HRESULT ( STDMETHODCALLTYPE *PagePostion )(
			IVwLayoutStream * This,
			/* [in] */ int hPage,
			/* [retval][out] */ int *pysPosition);

		HRESULT ( STDMETHODCALLTYPE *RollbackLayoutObjects )(
			IVwLayoutStream * This,
			/* [in] */ int hPage);

		HRESULT ( STDMETHODCALLTYPE *CommitLayoutObjects )(
			IVwLayoutStream * This,
			/* [in] */ int hPage);

		END_INTERFACE
	} IVwLayoutStreamVtbl;

	interface IVwLayoutStream
	{
		CONST_VTBL struct IVwLayoutStreamVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwLayoutStream_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwLayoutStream_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwLayoutStream_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwLayoutStream_SetManager(This,plm)	\
	(This)->lpVtbl -> SetManager(This,plm)

#define IVwLayoutStream_LayoutObj(This,pvg,dxsAvailWidth,ihvoRoot,cvsli,prgvsli,hPage)	\
	(This)->lpVtbl -> LayoutObj(This,pvg,dxsAvailWidth,ihvoRoot,cvsli,prgvsli,hPage)

#define IVwLayoutStream_LayoutPage(This,pvg,dxsAvailWidth,dysAvailHeight,pysStartThisPage,hPage,pdysUsedHeight,pysStartNextPage)	\
	(This)->lpVtbl -> LayoutPage(This,pvg,dxsAvailWidth,dysAvailHeight,pysStartThisPage,hPage,pdysUsedHeight,pysStartNextPage)

#define IVwLayoutStream_DiscardPage(This,hPage)	\
	(This)->lpVtbl -> DiscardPage(This,hPage)

#define IVwLayoutStream_PageBoundary(This,hPage,fEnd,ppsel)	\
	(This)->lpVtbl -> PageBoundary(This,hPage,fEnd,ppsel)

#define IVwLayoutStream_PageHeight(This,hPage,pdysHeight)	\
	(This)->lpVtbl -> PageHeight(This,hPage,pdysHeight)

#define IVwLayoutStream_PagePostion(This,hPage,pysPosition)	\
	(This)->lpVtbl -> PagePostion(This,hPage,pysPosition)

#define IVwLayoutStream_RollbackLayoutObjects(This,hPage)	\
	(This)->lpVtbl -> RollbackLayoutObjects(This,hPage)

#define IVwLayoutStream_CommitLayoutObjects(This,hPage)	\
	(This)->lpVtbl -> CommitLayoutObjects(This,hPage)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwLayoutStream_SetManager_Proxy(
	IVwLayoutStream * This,
	/* [in] */ IVwLayoutManager *plm);


void __RPC_STUB IVwLayoutStream_SetManager_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_LayoutObj_Proxy(
	IVwLayoutStream * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ int dxsAvailWidth,
	/* [in] */ int ihvoRoot,
	/* [in] */ int cvsli,
	/* [size_is][in] */ VwSelLevInfo *prgvsli,
	/* [in] */ int hPage);


void __RPC_STUB IVwLayoutStream_LayoutObj_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_LayoutPage_Proxy(
	IVwLayoutStream * This,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ int dxsAvailWidth,
	/* [in] */ int dysAvailHeight,
	/* [out][in] */ int *pysStartThisPage,
	/* [in] */ int hPage,
	/* [out] */ int *pdysUsedHeight,
	/* [out] */ int *pysStartNextPage);


void __RPC_STUB IVwLayoutStream_LayoutPage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_DiscardPage_Proxy(
	IVwLayoutStream * This,
	/* [in] */ int hPage);


void __RPC_STUB IVwLayoutStream_DiscardPage_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_PageBoundary_Proxy(
	IVwLayoutStream * This,
	/* [in] */ int hPage,
	/* [in] */ ComBool fEnd,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwLayoutStream_PageBoundary_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_PageHeight_Proxy(
	IVwLayoutStream * This,
	/* [in] */ int hPage,
	/* [retval][out] */ int *pdysHeight);


void __RPC_STUB IVwLayoutStream_PageHeight_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_PagePostion_Proxy(
	IVwLayoutStream * This,
	/* [in] */ int hPage,
	/* [retval][out] */ int *pysPosition);


void __RPC_STUB IVwLayoutStream_PagePostion_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_RollbackLayoutObjects_Proxy(
	IVwLayoutStream * This,
	/* [in] */ int hPage);


void __RPC_STUB IVwLayoutStream_RollbackLayoutObjects_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutStream_CommitLayoutObjects_Proxy(
	IVwLayoutStream * This,
	/* [in] */ int hPage);


void __RPC_STUB IVwLayoutStream_CommitLayoutObjects_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwLayoutStream_INTERFACE_DEFINED__ */


#ifndef __IVwLayoutManager_INTERFACE_DEFINED__
#define __IVwLayoutManager_INTERFACE_DEFINED__

/* interface IVwLayoutManager */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwLayoutManager;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("13F3A421-4915-455b-B57F-AFD4073CFFA0")
	IVwLayoutManager : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE AddDependentObjects(
			/* [in] */ IVwLayoutStream *play,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int hPage,
			/* [in] */ int cguid,
			/* [size_is][in] */ GUID *prgguidObj,
			/* [in] */ ComBool fAllowFail,
			/* [out] */ ComBool *pfFailed,
			/* [out][in] */ int *pdysAvailHeight) = 0;

		virtual HRESULT STDMETHODCALLTYPE PageBroken(
			/* [in] */ IVwLayoutStream *play,
			/* [in] */ int hPage) = 0;

		virtual HRESULT STDMETHODCALLTYPE PageBoundaryMoved(
			/* [in] */ IVwLayoutStream *play,
			/* [in] */ int hPage,
			/* [in] */ int ichOld) = 0;

		virtual HRESULT STDMETHODCALLTYPE EstimateHeight(
			/* [in] */ int dxpWidth,
			/* [retval][out] */ int *pdxpHeight) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwLayoutManagerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwLayoutManager * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwLayoutManager * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwLayoutManager * This);

		HRESULT ( STDMETHODCALLTYPE *AddDependentObjects )(
			IVwLayoutManager * This,
			/* [in] */ IVwLayoutStream *play,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ int hPage,
			/* [in] */ int cguid,
			/* [size_is][in] */ GUID *prgguidObj,
			/* [in] */ ComBool fAllowFail,
			/* [out] */ ComBool *pfFailed,
			/* [out][in] */ int *pdysAvailHeight);

		HRESULT ( STDMETHODCALLTYPE *PageBroken )(
			IVwLayoutManager * This,
			/* [in] */ IVwLayoutStream *play,
			/* [in] */ int hPage);

		HRESULT ( STDMETHODCALLTYPE *PageBoundaryMoved )(
			IVwLayoutManager * This,
			/* [in] */ IVwLayoutStream *play,
			/* [in] */ int hPage,
			/* [in] */ int ichOld);

		HRESULT ( STDMETHODCALLTYPE *EstimateHeight )(
			IVwLayoutManager * This,
			/* [in] */ int dxpWidth,
			/* [retval][out] */ int *pdxpHeight);

		END_INTERFACE
	} IVwLayoutManagerVtbl;

	interface IVwLayoutManager
	{
		CONST_VTBL struct IVwLayoutManagerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwLayoutManager_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwLayoutManager_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwLayoutManager_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwLayoutManager_AddDependentObjects(This,play,pvg,hPage,cguid,prgguidObj,fAllowFail,pfFailed,pdysAvailHeight)	\
	(This)->lpVtbl -> AddDependentObjects(This,play,pvg,hPage,cguid,prgguidObj,fAllowFail,pfFailed,pdysAvailHeight)

#define IVwLayoutManager_PageBroken(This,play,hPage)	\
	(This)->lpVtbl -> PageBroken(This,play,hPage)

#define IVwLayoutManager_PageBoundaryMoved(This,play,hPage,ichOld)	\
	(This)->lpVtbl -> PageBoundaryMoved(This,play,hPage,ichOld)

#define IVwLayoutManager_EstimateHeight(This,dxpWidth,pdxpHeight)	\
	(This)->lpVtbl -> EstimateHeight(This,dxpWidth,pdxpHeight)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwLayoutManager_AddDependentObjects_Proxy(
	IVwLayoutManager * This,
	/* [in] */ IVwLayoutStream *play,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ int hPage,
	/* [in] */ int cguid,
	/* [size_is][in] */ GUID *prgguidObj,
	/* [in] */ ComBool fAllowFail,
	/* [out] */ ComBool *pfFailed,
	/* [out][in] */ int *pdysAvailHeight);


void __RPC_STUB IVwLayoutManager_AddDependentObjects_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutManager_PageBroken_Proxy(
	IVwLayoutManager * This,
	/* [in] */ IVwLayoutStream *play,
	/* [in] */ int hPage);


void __RPC_STUB IVwLayoutManager_PageBroken_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutManager_PageBoundaryMoved_Proxy(
	IVwLayoutManager * This,
	/* [in] */ IVwLayoutStream *play,
	/* [in] */ int hPage,
	/* [in] */ int ichOld);


void __RPC_STUB IVwLayoutManager_PageBoundaryMoved_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwLayoutManager_EstimateHeight_Proxy(
	IVwLayoutManager * This,
	/* [in] */ int dxpWidth,
	/* [retval][out] */ int *pdxpHeight);


void __RPC_STUB IVwLayoutManager_EstimateHeight_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwLayoutManager_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_DbColSpec;

#ifdef __cplusplus

class DECLSPEC_UUID("26F0F36D-C905-4d1e-B1A9-AB3EA8C4D340")
DbColSpec;
#endif

EXTERN_C const CLSID CLSID_VwCacheDa;

#ifdef __cplusplus

class DECLSPEC_UUID("FFF54604-C92B-4745-B74A-703CFBB81BB0")
VwCacheDa;
#endif

EXTERN_C const CLSID CLSID_VwUndoDa;

#ifdef __cplusplus

class DECLSPEC_UUID("2ABC0E1E-DCDB-4312-8B7E-7F644240E37C")
VwUndoDa;
#endif

EXTERN_C const CLSID CLSID_VwOleDbDa;

#ifdef __cplusplus

class DECLSPEC_UUID("8645fa50-ee90-11d2-a9b8-0080c87b6086")
VwOleDbDa;
#endif

EXTERN_C const CLSID CLSID_VwRootBox;

#ifdef __cplusplus

class DECLSPEC_UUID("D1074356-4F41-4e3e-A1ED-9C044FD0C096")
VwRootBox;
#endif

#ifndef __IVwObjDelNotification_INTERFACE_DEFINED__
#define __IVwObjDelNotification_INTERFACE_DEFINED__

/* interface IVwObjDelNotification */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwObjDelNotification;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("913B1BED-6199-4b6e-A63F-57B225B44997")
	IVwObjDelNotification : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE AboutToDelete(
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ long hvoObject,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ ComBool fMergeNext) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwObjDelNotificationVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwObjDelNotification * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwObjDelNotification * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwObjDelNotification * This);

		HRESULT ( STDMETHODCALLTYPE *AboutToDelete )(
			IVwObjDelNotification * This,
			/* [in] */ IVwRootBox *pRoot,
			/* [in] */ long hvoObject,
			/* [in] */ long hvoOwner,
			/* [in] */ int tag,
			/* [in] */ int ihvo,
			/* [in] */ ComBool fMergeNext);

		END_INTERFACE
	} IVwObjDelNotificationVtbl;

	interface IVwObjDelNotification
	{
		CONST_VTBL struct IVwObjDelNotificationVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwObjDelNotification_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwObjDelNotification_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwObjDelNotification_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwObjDelNotification_AboutToDelete(This,pRoot,hvoObject,hvoOwner,tag,ihvo,fMergeNext)	\
	(This)->lpVtbl -> AboutToDelete(This,pRoot,hvoObject,hvoOwner,tag,ihvo,fMergeNext)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwObjDelNotification_AboutToDelete_Proxy(
	IVwObjDelNotification * This,
	/* [in] */ IVwRootBox *pRoot,
	/* [in] */ long hvoObject,
	/* [in] */ long hvoOwner,
	/* [in] */ int tag,
	/* [in] */ int ihvo,
	/* [in] */ ComBool fMergeNext);


void __RPC_STUB IVwObjDelNotification_AboutToDelete_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwObjDelNotification_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_VwStylesheet;

#ifdef __cplusplus

class DECLSPEC_UUID("CCE2A7ED-464C-4ec7-A0B0-E3C1F6B94C5A")
VwStylesheet;
#endif

EXTERN_C const CLSID CLSID_VwPropertyStore;

#ifdef __cplusplus

class DECLSPEC_UUID("CB59916A-C532-4a57-8CB4-6E1508B4DEC1")
VwPropertyStore;
#endif

EXTERN_C const CLSID CLSID_VwOverlay;

#ifdef __cplusplus

class DECLSPEC_UUID("73F5DB01-3D2A-11d4-8078-0000C0FB81B5")
VwOverlay;
#endif

EXTERN_C const CLSID CLSID_VwPrintContextWin32;

#ifdef __cplusplus

class DECLSPEC_UUID("5E9FB977-66AE-4c16-A036-1D40E7713573")
VwPrintContextWin32;
#endif

EXTERN_C const CLSID CLSID_SqlUndoAction;

#ifdef __cplusplus

class DECLSPEC_UUID("77272239-3228-4b02-9B6A-1DC5539F8153")
SqlUndoAction;
#endif

#ifndef __IVwPattern_INTERFACE_DEFINED__
#define __IVwPattern_INTERFACE_DEFINED__

/* interface IVwPattern */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwPattern;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("FACD01D9-BAF4-4ef0-BED6-A8966160C94D")
	IVwPattern : public IUnknown
	{
	public:
		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Pattern(
			/* [in] */ /* external definition not present */ ITsString *ptssPattern) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Pattern(
			/* [retval][out] */ /* external definition not present */ ITsString **pptssPattern) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Overlay(
			/* [in] */ IVwOverlay *pvo) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Overlay(
			/* [retval][out] */ IVwOverlay **ppvo) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchCase(
			/* [in] */ ComBool fMatch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchCase(
			/* [retval][out] */ ComBool *pfMatch) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchDiacritics(
			/* [in] */ ComBool fMatch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchDiacritics(
			/* [retval][out] */ ComBool *pfMatch) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchWholeWord(
			/* [in] */ ComBool fMatch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchWholeWord(
			/* [retval][out] */ ComBool *pfMatch) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchOldWritingSystem(
			/* [in] */ ComBool fMatch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchOldWritingSystem(
			/* [retval][out] */ ComBool *pfMatch) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchExactly(
			/* [in] */ ComBool fMatch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchExactly(
			/* [retval][out] */ ComBool *pfMatch) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchCompatibility(
			/* [in] */ ComBool fMatch) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchCompatibility(
			/* [retval][out] */ ComBool *pfMatch) = 0;

		virtual HRESULT STDMETHODCALLTYPE Find(
			/* [in] */ IVwRootBox *prootb,
			/* [in] */ ComBool fForward,
			/* [in] */ IVwSearchKiller *pxserkl) = 0;

		virtual HRESULT STDMETHODCALLTYPE FindFrom(
			/* [in] */ IVwSelection *psel,
			/* [in] */ ComBool fForward,
			/* [in] */ IVwSearchKiller *pxserkl) = 0;

		virtual HRESULT STDMETHODCALLTYPE FindNext(
			/* [in] */ ComBool fForward,
			/* [in] */ IVwSearchKiller *pxserkl) = 0;

		virtual HRESULT STDMETHODCALLTYPE FindIn(
			/* [in] */ /* external definition not present */ IVwTextSource *pts,
			/* [in] */ int ichStart,
			/* [in] */ int ichEnd,
			/* [in] */ ComBool fForward,
			/* [out] */ int *pichMinFound,
			/* [out] */ int *pichLimFound,
			/* [in] */ IVwSearchKiller *pxserkl) = 0;

		virtual HRESULT STDMETHODCALLTYPE Install( void) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Found(
			/* [retval][out] */ ComBool *pfFound) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetSelection(
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual HRESULT STDMETHODCALLTYPE CLevels(
			/* [out] */ int *pclev) = 0;

		virtual HRESULT STDMETHODCALLTYPE AllTextSelInfo(
			/* [out] */ int *pihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][out] */ VwSelLevInfo *prgvsli,
			/* [out] */ int *ptagTextProp,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ int *pichAnchor,
			/* [out] */ int *pichEnd,
			/* [out] */ int *pws) = 0;

		virtual HRESULT STDMETHODCALLTYPE MatchWhole(
			/* [in] */ IVwSelection *psel,
			/* [retval][out] */ ComBool *pfMatch) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Limit(
			/* [in] */ IVwSelection *psel) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Limit(
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_StartingPoint(
			/* [in] */ IVwSelection *psel) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StartingPoint(
			/* [retval][out] */ IVwSelection **ppsel) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SearchWindow(
			/* [in] */ DWORD hwnd) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SearchWindow(
			/* [retval][out] */ DWORD *phwnd) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StoppedAtLimit(
			/* [retval][out] */ ComBool *pfAtLimit) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_StoppedAtLimit(
			/* [in] */ ComBool fAtLimit) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LastDirection(
			/* [retval][out] */ ComBool *pfForward) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_ReplaceWith(
			/* [in] */ /* external definition not present */ ITsString *ptssPattern) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ReplaceWith(
			/* [retval][out] */ /* external definition not present */ ITsString **pptssPattern) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ShowMore(
			/* [in] */ ComBool fMore) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ShowMore(
			/* [retval][out] */ ComBool *pfMore) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuLocale(
			/* [retval][out] */ BSTR *pbstrLocale) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuLocale(
			/* [in] */ BSTR bstrLocale) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuCollatingRules(
			/* [retval][out] */ BSTR *pbstrRules) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuCollatingRules(
			/* [in] */ BSTR bstrRules) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwPatternVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwPattern * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwPattern * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwPattern * This);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Pattern )(
			IVwPattern * This,
			/* [in] */ /* external definition not present */ ITsString *ptssPattern);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Pattern )(
			IVwPattern * This,
			/* [retval][out] */ /* external definition not present */ ITsString **pptssPattern);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Overlay )(
			IVwPattern * This,
			/* [in] */ IVwOverlay *pvo);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Overlay )(
			IVwPattern * This,
			/* [retval][out] */ IVwOverlay **ppvo);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchCase )(
			IVwPattern * This,
			/* [in] */ ComBool fMatch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchCase )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfMatch);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchDiacritics )(
			IVwPattern * This,
			/* [in] */ ComBool fMatch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchDiacritics )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfMatch);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchWholeWord )(
			IVwPattern * This,
			/* [in] */ ComBool fMatch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchWholeWord )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfMatch);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchOldWritingSystem )(
			IVwPattern * This,
			/* [in] */ ComBool fMatch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchOldWritingSystem )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfMatch);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchExactly )(
			IVwPattern * This,
			/* [in] */ ComBool fMatch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchExactly )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfMatch);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchCompatibility )(
			IVwPattern * This,
			/* [in] */ ComBool fMatch);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchCompatibility )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfMatch);

		HRESULT ( STDMETHODCALLTYPE *Find )(
			IVwPattern * This,
			/* [in] */ IVwRootBox *prootb,
			/* [in] */ ComBool fForward,
			/* [in] */ IVwSearchKiller *pxserkl);

		HRESULT ( STDMETHODCALLTYPE *FindFrom )(
			IVwPattern * This,
			/* [in] */ IVwSelection *psel,
			/* [in] */ ComBool fForward,
			/* [in] */ IVwSearchKiller *pxserkl);

		HRESULT ( STDMETHODCALLTYPE *FindNext )(
			IVwPattern * This,
			/* [in] */ ComBool fForward,
			/* [in] */ IVwSearchKiller *pxserkl);

		HRESULT ( STDMETHODCALLTYPE *FindIn )(
			IVwPattern * This,
			/* [in] */ /* external definition not present */ IVwTextSource *pts,
			/* [in] */ int ichStart,
			/* [in] */ int ichEnd,
			/* [in] */ ComBool fForward,
			/* [out] */ int *pichMinFound,
			/* [out] */ int *pichLimFound,
			/* [in] */ IVwSearchKiller *pxserkl);

		HRESULT ( STDMETHODCALLTYPE *Install )(
			IVwPattern * This);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Found )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfFound);

		HRESULT ( STDMETHODCALLTYPE *GetSelection )(
			IVwPattern * This,
			/* [in] */ ComBool fInstall,
			/* [retval][out] */ IVwSelection **ppsel);

		HRESULT ( STDMETHODCALLTYPE *CLevels )(
			IVwPattern * This,
			/* [out] */ int *pclev);

		HRESULT ( STDMETHODCALLTYPE *AllTextSelInfo )(
			IVwPattern * This,
			/* [out] */ int *pihvoRoot,
			/* [in] */ int cvlsi,
			/* [size_is][out] */ VwSelLevInfo *prgvsli,
			/* [out] */ int *ptagTextProp,
			/* [out] */ int *pcpropPrevious,
			/* [out] */ int *pichAnchor,
			/* [out] */ int *pichEnd,
			/* [out] */ int *pws);

		HRESULT ( STDMETHODCALLTYPE *MatchWhole )(
			IVwPattern * This,
			/* [in] */ IVwSelection *psel,
			/* [retval][out] */ ComBool *pfMatch);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Limit )(
			IVwPattern * This,
			/* [in] */ IVwSelection *psel);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Limit )(
			IVwPattern * This,
			/* [retval][out] */ IVwSelection **ppsel);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_StartingPoint )(
			IVwPattern * This,
			/* [in] */ IVwSelection *psel);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StartingPoint )(
			IVwPattern * This,
			/* [retval][out] */ IVwSelection **ppsel);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SearchWindow )(
			IVwPattern * This,
			/* [in] */ DWORD hwnd);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SearchWindow )(
			IVwPattern * This,
			/* [retval][out] */ DWORD *phwnd);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StoppedAtLimit )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfAtLimit);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_StoppedAtLimit )(
			IVwPattern * This,
			/* [in] */ ComBool fAtLimit);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LastDirection )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfForward);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_ReplaceWith )(
			IVwPattern * This,
			/* [in] */ /* external definition not present */ ITsString *ptssPattern);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ReplaceWith )(
			IVwPattern * This,
			/* [retval][out] */ /* external definition not present */ ITsString **pptssPattern);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ShowMore )(
			IVwPattern * This,
			/* [in] */ ComBool fMore);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ShowMore )(
			IVwPattern * This,
			/* [retval][out] */ ComBool *pfMore);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuLocale )(
			IVwPattern * This,
			/* [retval][out] */ BSTR *pbstrLocale);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuLocale )(
			IVwPattern * This,
			/* [in] */ BSTR bstrLocale);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuCollatingRules )(
			IVwPattern * This,
			/* [retval][out] */ BSTR *pbstrRules);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuCollatingRules )(
			IVwPattern * This,
			/* [in] */ BSTR bstrRules);

		END_INTERFACE
	} IVwPatternVtbl;

	interface IVwPattern
	{
		CONST_VTBL struct IVwPatternVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwPattern_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwPattern_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwPattern_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwPattern_putref_Pattern(This,ptssPattern)	\
	(This)->lpVtbl -> putref_Pattern(This,ptssPattern)

#define IVwPattern_get_Pattern(This,pptssPattern)	\
	(This)->lpVtbl -> get_Pattern(This,pptssPattern)

#define IVwPattern_putref_Overlay(This,pvo)	\
	(This)->lpVtbl -> putref_Overlay(This,pvo)

#define IVwPattern_get_Overlay(This,ppvo)	\
	(This)->lpVtbl -> get_Overlay(This,ppvo)

#define IVwPattern_put_MatchCase(This,fMatch)	\
	(This)->lpVtbl -> put_MatchCase(This,fMatch)

#define IVwPattern_get_MatchCase(This,pfMatch)	\
	(This)->lpVtbl -> get_MatchCase(This,pfMatch)

#define IVwPattern_put_MatchDiacritics(This,fMatch)	\
	(This)->lpVtbl -> put_MatchDiacritics(This,fMatch)

#define IVwPattern_get_MatchDiacritics(This,pfMatch)	\
	(This)->lpVtbl -> get_MatchDiacritics(This,pfMatch)

#define IVwPattern_put_MatchWholeWord(This,fMatch)	\
	(This)->lpVtbl -> put_MatchWholeWord(This,fMatch)

#define IVwPattern_get_MatchWholeWord(This,pfMatch)	\
	(This)->lpVtbl -> get_MatchWholeWord(This,pfMatch)

#define IVwPattern_put_MatchOldWritingSystem(This,fMatch)	\
	(This)->lpVtbl -> put_MatchOldWritingSystem(This,fMatch)

#define IVwPattern_get_MatchOldWritingSystem(This,pfMatch)	\
	(This)->lpVtbl -> get_MatchOldWritingSystem(This,pfMatch)

#define IVwPattern_put_MatchExactly(This,fMatch)	\
	(This)->lpVtbl -> put_MatchExactly(This,fMatch)

#define IVwPattern_get_MatchExactly(This,pfMatch)	\
	(This)->lpVtbl -> get_MatchExactly(This,pfMatch)

#define IVwPattern_put_MatchCompatibility(This,fMatch)	\
	(This)->lpVtbl -> put_MatchCompatibility(This,fMatch)

#define IVwPattern_get_MatchCompatibility(This,pfMatch)	\
	(This)->lpVtbl -> get_MatchCompatibility(This,pfMatch)

#define IVwPattern_Find(This,prootb,fForward,pxserkl)	\
	(This)->lpVtbl -> Find(This,prootb,fForward,pxserkl)

#define IVwPattern_FindFrom(This,psel,fForward,pxserkl)	\
	(This)->lpVtbl -> FindFrom(This,psel,fForward,pxserkl)

#define IVwPattern_FindNext(This,fForward,pxserkl)	\
	(This)->lpVtbl -> FindNext(This,fForward,pxserkl)

#define IVwPattern_FindIn(This,pts,ichStart,ichEnd,fForward,pichMinFound,pichLimFound,pxserkl)	\
	(This)->lpVtbl -> FindIn(This,pts,ichStart,ichEnd,fForward,pichMinFound,pichLimFound,pxserkl)

#define IVwPattern_Install(This)	\
	(This)->lpVtbl -> Install(This)

#define IVwPattern_get_Found(This,pfFound)	\
	(This)->lpVtbl -> get_Found(This,pfFound)

#define IVwPattern_GetSelection(This,fInstall,ppsel)	\
	(This)->lpVtbl -> GetSelection(This,fInstall,ppsel)

#define IVwPattern_CLevels(This,pclev)	\
	(This)->lpVtbl -> CLevels(This,pclev)

#define IVwPattern_AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws)	\
	(This)->lpVtbl -> AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws)

#define IVwPattern_MatchWhole(This,psel,pfMatch)	\
	(This)->lpVtbl -> MatchWhole(This,psel,pfMatch)

#define IVwPattern_putref_Limit(This,psel)	\
	(This)->lpVtbl -> putref_Limit(This,psel)

#define IVwPattern_get_Limit(This,ppsel)	\
	(This)->lpVtbl -> get_Limit(This,ppsel)

#define IVwPattern_putref_StartingPoint(This,psel)	\
	(This)->lpVtbl -> putref_StartingPoint(This,psel)

#define IVwPattern_get_StartingPoint(This,ppsel)	\
	(This)->lpVtbl -> get_StartingPoint(This,ppsel)

#define IVwPattern_put_SearchWindow(This,hwnd)	\
	(This)->lpVtbl -> put_SearchWindow(This,hwnd)

#define IVwPattern_get_SearchWindow(This,phwnd)	\
	(This)->lpVtbl -> get_SearchWindow(This,phwnd)

#define IVwPattern_get_StoppedAtLimit(This,pfAtLimit)	\
	(This)->lpVtbl -> get_StoppedAtLimit(This,pfAtLimit)

#define IVwPattern_put_StoppedAtLimit(This,fAtLimit)	\
	(This)->lpVtbl -> put_StoppedAtLimit(This,fAtLimit)

#define IVwPattern_get_LastDirection(This,pfForward)	\
	(This)->lpVtbl -> get_LastDirection(This,pfForward)

#define IVwPattern_putref_ReplaceWith(This,ptssPattern)	\
	(This)->lpVtbl -> putref_ReplaceWith(This,ptssPattern)

#define IVwPattern_get_ReplaceWith(This,pptssPattern)	\
	(This)->lpVtbl -> get_ReplaceWith(This,pptssPattern)

#define IVwPattern_put_ShowMore(This,fMore)	\
	(This)->lpVtbl -> put_ShowMore(This,fMore)

#define IVwPattern_get_ShowMore(This,pfMore)	\
	(This)->lpVtbl -> get_ShowMore(This,pfMore)

#define IVwPattern_get_IcuLocale(This,pbstrLocale)	\
	(This)->lpVtbl -> get_IcuLocale(This,pbstrLocale)

#define IVwPattern_put_IcuLocale(This,bstrLocale)	\
	(This)->lpVtbl -> put_IcuLocale(This,bstrLocale)

#define IVwPattern_get_IcuCollatingRules(This,pbstrRules)	\
	(This)->lpVtbl -> get_IcuCollatingRules(This,pbstrRules)

#define IVwPattern_put_IcuCollatingRules(This,bstrRules)	\
	(This)->lpVtbl -> put_IcuCollatingRules(This,bstrRules)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwPattern_putref_Pattern_Proxy(
	IVwPattern * This,
	/* [in] */ /* external definition not present */ ITsString *ptssPattern);


void __RPC_STUB IVwPattern_putref_Pattern_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_Pattern_Proxy(
	IVwPattern * This,
	/* [retval][out] */ /* external definition not present */ ITsString **pptssPattern);


void __RPC_STUB IVwPattern_get_Pattern_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwPattern_putref_Overlay_Proxy(
	IVwPattern * This,
	/* [in] */ IVwOverlay *pvo);


void __RPC_STUB IVwPattern_putref_Overlay_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_Overlay_Proxy(
	IVwPattern * This,
	/* [retval][out] */ IVwOverlay **ppvo);


void __RPC_STUB IVwPattern_get_Overlay_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_MatchCase_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fMatch);


void __RPC_STUB IVwPattern_put_MatchCase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_MatchCase_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfMatch);


void __RPC_STUB IVwPattern_get_MatchCase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_MatchDiacritics_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fMatch);


void __RPC_STUB IVwPattern_put_MatchDiacritics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_MatchDiacritics_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfMatch);


void __RPC_STUB IVwPattern_get_MatchDiacritics_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_MatchWholeWord_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fMatch);


void __RPC_STUB IVwPattern_put_MatchWholeWord_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_MatchWholeWord_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfMatch);


void __RPC_STUB IVwPattern_get_MatchWholeWord_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_MatchOldWritingSystem_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fMatch);


void __RPC_STUB IVwPattern_put_MatchOldWritingSystem_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_MatchOldWritingSystem_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfMatch);


void __RPC_STUB IVwPattern_get_MatchOldWritingSystem_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_MatchExactly_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fMatch);


void __RPC_STUB IVwPattern_put_MatchExactly_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_MatchExactly_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfMatch);


void __RPC_STUB IVwPattern_get_MatchExactly_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_MatchCompatibility_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fMatch);


void __RPC_STUB IVwPattern_put_MatchCompatibility_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_MatchCompatibility_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfMatch);


void __RPC_STUB IVwPattern_get_MatchCompatibility_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_Find_Proxy(
	IVwPattern * This,
	/* [in] */ IVwRootBox *prootb,
	/* [in] */ ComBool fForward,
	/* [in] */ IVwSearchKiller *pxserkl);


void __RPC_STUB IVwPattern_Find_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_FindFrom_Proxy(
	IVwPattern * This,
	/* [in] */ IVwSelection *psel,
	/* [in] */ ComBool fForward,
	/* [in] */ IVwSearchKiller *pxserkl);


void __RPC_STUB IVwPattern_FindFrom_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_FindNext_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fForward,
	/* [in] */ IVwSearchKiller *pxserkl);


void __RPC_STUB IVwPattern_FindNext_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_FindIn_Proxy(
	IVwPattern * This,
	/* [in] */ /* external definition not present */ IVwTextSource *pts,
	/* [in] */ int ichStart,
	/* [in] */ int ichEnd,
	/* [in] */ ComBool fForward,
	/* [out] */ int *pichMinFound,
	/* [out] */ int *pichLimFound,
	/* [in] */ IVwSearchKiller *pxserkl);


void __RPC_STUB IVwPattern_FindIn_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_Install_Proxy(
	IVwPattern * This);


void __RPC_STUB IVwPattern_Install_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_Found_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfFound);


void __RPC_STUB IVwPattern_get_Found_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_GetSelection_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fInstall,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwPattern_GetSelection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_CLevels_Proxy(
	IVwPattern * This,
	/* [out] */ int *pclev);


void __RPC_STUB IVwPattern_CLevels_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_AllTextSelInfo_Proxy(
	IVwPattern * This,
	/* [out] */ int *pihvoRoot,
	/* [in] */ int cvlsi,
	/* [size_is][out] */ VwSelLevInfo *prgvsli,
	/* [out] */ int *ptagTextProp,
	/* [out] */ int *pcpropPrevious,
	/* [out] */ int *pichAnchor,
	/* [out] */ int *pichEnd,
	/* [out] */ int *pws);


void __RPC_STUB IVwPattern_AllTextSelInfo_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwPattern_MatchWhole_Proxy(
	IVwPattern * This,
	/* [in] */ IVwSelection *psel,
	/* [retval][out] */ ComBool *pfMatch);


void __RPC_STUB IVwPattern_MatchWhole_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwPattern_putref_Limit_Proxy(
	IVwPattern * This,
	/* [in] */ IVwSelection *psel);


void __RPC_STUB IVwPattern_putref_Limit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_Limit_Proxy(
	IVwPattern * This,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwPattern_get_Limit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwPattern_putref_StartingPoint_Proxy(
	IVwPattern * This,
	/* [in] */ IVwSelection *psel);


void __RPC_STUB IVwPattern_putref_StartingPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_StartingPoint_Proxy(
	IVwPattern * This,
	/* [retval][out] */ IVwSelection **ppsel);


void __RPC_STUB IVwPattern_get_StartingPoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_SearchWindow_Proxy(
	IVwPattern * This,
	/* [in] */ DWORD hwnd);


void __RPC_STUB IVwPattern_put_SearchWindow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_SearchWindow_Proxy(
	IVwPattern * This,
	/* [retval][out] */ DWORD *phwnd);


void __RPC_STUB IVwPattern_get_SearchWindow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_StoppedAtLimit_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfAtLimit);


void __RPC_STUB IVwPattern_get_StoppedAtLimit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_StoppedAtLimit_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fAtLimit);


void __RPC_STUB IVwPattern_put_StoppedAtLimit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_LastDirection_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfForward);


void __RPC_STUB IVwPattern_get_LastDirection_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IVwPattern_putref_ReplaceWith_Proxy(
	IVwPattern * This,
	/* [in] */ /* external definition not present */ ITsString *ptssPattern);


void __RPC_STUB IVwPattern_putref_ReplaceWith_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_ReplaceWith_Proxy(
	IVwPattern * This,
	/* [retval][out] */ /* external definition not present */ ITsString **pptssPattern);


void __RPC_STUB IVwPattern_get_ReplaceWith_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_ShowMore_Proxy(
	IVwPattern * This,
	/* [in] */ ComBool fMore);


void __RPC_STUB IVwPattern_put_ShowMore_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_ShowMore_Proxy(
	IVwPattern * This,
	/* [retval][out] */ ComBool *pfMore);


void __RPC_STUB IVwPattern_get_ShowMore_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_IcuLocale_Proxy(
	IVwPattern * This,
	/* [retval][out] */ BSTR *pbstrLocale);


void __RPC_STUB IVwPattern_get_IcuLocale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_IcuLocale_Proxy(
	IVwPattern * This,
	/* [in] */ BSTR bstrLocale);


void __RPC_STUB IVwPattern_put_IcuLocale_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IVwPattern_get_IcuCollatingRules_Proxy(
	IVwPattern * This,
	/* [retval][out] */ BSTR *pbstrRules);


void __RPC_STUB IVwPattern_get_IcuCollatingRules_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IVwPattern_put_IcuCollatingRules_Proxy(
	IVwPattern * This,
	/* [in] */ BSTR bstrRules);


void __RPC_STUB IVwPattern_put_IcuCollatingRules_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwPattern_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_VwPattern;

#ifdef __cplusplus

class DECLSPEC_UUID("6C659C76-3991-48dd-93F7-DA65847D4863")
VwPattern;
#endif

EXTERN_C const CLSID CLSID_VwSearchKiller;

#ifdef __cplusplus

class DECLSPEC_UUID("4ADA9157-67F8-499b-88CE-D63DF918DF83")
VwSearchKiller;
#endif

#ifndef __IVwDrawRootBuffered_INTERFACE_DEFINED__
#define __IVwDrawRootBuffered_INTERFACE_DEFINED__

/* interface IVwDrawRootBuffered */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IVwDrawRootBuffered;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("09752C4C-CC1E-4268-891E-526BBBAC0DE8")
	IVwDrawRootBuffered : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE DrawTheRoot(
			/* [in] */ IVwRootBox *prootb,
			/* [in] */ HDC hdc,
			/* [in] */ RECT rcpDraw,
			/* [in] */ COLORREF bkclr,
			/* [in] */ ComBool fDrawSel,
			/* [in] */ IVwRootSite *pvrs) = 0;

		virtual HRESULT STDMETHODCALLTYPE DrawTheRootAt(
			/* [in] */ IVwRootBox *prootb,
			/* [in] */ HDC hdc,
			/* [in] */ RECT rcpDraw,
			/* [in] */ COLORREF bkclr,
			/* [in] */ ComBool fDrawSel,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ int ysTop,
			/* [in] */ int dysHeight) = 0;

	};

#else 	/* C style interface */

	typedef struct IVwDrawRootBufferedVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IVwDrawRootBuffered * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IVwDrawRootBuffered * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IVwDrawRootBuffered * This);

		HRESULT ( STDMETHODCALLTYPE *DrawTheRoot )(
			IVwDrawRootBuffered * This,
			/* [in] */ IVwRootBox *prootb,
			/* [in] */ HDC hdc,
			/* [in] */ RECT rcpDraw,
			/* [in] */ COLORREF bkclr,
			/* [in] */ ComBool fDrawSel,
			/* [in] */ IVwRootSite *pvrs);

		HRESULT ( STDMETHODCALLTYPE *DrawTheRootAt )(
			IVwDrawRootBuffered * This,
			/* [in] */ IVwRootBox *prootb,
			/* [in] */ HDC hdc,
			/* [in] */ RECT rcpDraw,
			/* [in] */ COLORREF bkclr,
			/* [in] */ ComBool fDrawSel,
			/* [in] */ /* external definition not present */ IVwGraphics *pvg,
			/* [in] */ RECT rcSrc,
			/* [in] */ RECT rcDst,
			/* [in] */ int ysTop,
			/* [in] */ int dysHeight);

		END_INTERFACE
	} IVwDrawRootBufferedVtbl;

	interface IVwDrawRootBuffered
	{
		CONST_VTBL struct IVwDrawRootBufferedVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IVwDrawRootBuffered_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IVwDrawRootBuffered_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IVwDrawRootBuffered_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IVwDrawRootBuffered_DrawTheRoot(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvrs)	\
	(This)->lpVtbl -> DrawTheRoot(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvrs)

#define IVwDrawRootBuffered_DrawTheRootAt(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvg,rcSrc,rcDst,ysTop,dysHeight)	\
	(This)->lpVtbl -> DrawTheRootAt(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvg,rcSrc,rcDst,ysTop,dysHeight)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IVwDrawRootBuffered_DrawTheRoot_Proxy(
	IVwDrawRootBuffered * This,
	/* [in] */ IVwRootBox *prootb,
	/* [in] */ HDC hdc,
	/* [in] */ RECT rcpDraw,
	/* [in] */ COLORREF bkclr,
	/* [in] */ ComBool fDrawSel,
	/* [in] */ IVwRootSite *pvrs);


void __RPC_STUB IVwDrawRootBuffered_DrawTheRoot_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IVwDrawRootBuffered_DrawTheRootAt_Proxy(
	IVwDrawRootBuffered * This,
	/* [in] */ IVwRootBox *prootb,
	/* [in] */ HDC hdc,
	/* [in] */ RECT rcpDraw,
	/* [in] */ COLORREF bkclr,
	/* [in] */ ComBool fDrawSel,
	/* [in] */ /* external definition not present */ IVwGraphics *pvg,
	/* [in] */ RECT rcSrc,
	/* [in] */ RECT rcDst,
	/* [in] */ int ysTop,
	/* [in] */ int dysHeight);


void __RPC_STUB IVwDrawRootBuffered_DrawTheRootAt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IVwDrawRootBuffered_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_VwDrawRootBuffered;

#ifdef __cplusplus

class DECLSPEC_UUID("97199458-10C7-49da-B3AE-EA922EA64859")
VwDrawRootBuffered;
#endif

EXTERN_C const CLSID CLSID_VwSynchronizer;

#ifdef __cplusplus

class DECLSPEC_UUID("5E149A49-CAEE-4823-97F7-BB9DED2A62BC")
VwSynchronizer;
#endif

EXTERN_C const CLSID CLSID_VwDataSpec;

#ifdef __cplusplus

class DECLSPEC_UUID("6DE189F0-6F15-4242-943D-054AAEA92ACB")
VwDataSpec;
#endif

EXTERN_C const CLSID CLSID_VwLayoutStream;

#ifdef __cplusplus

class DECLSPEC_UUID("1CD09E06-6978-4969-A1FC-462723587C32")
VwLayoutStream;
#endif

#ifndef __IOpenFWProjectDlg_INTERFACE_DEFINED__
#define __IOpenFWProjectDlg_INTERFACE_DEFINED__

/* interface IOpenFWProjectDlg */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IOpenFWProjectDlg;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8cb6f2f9-3b0a-4030-8992-c50fb78e77f3")
	IOpenFWProjectDlg : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Show(
			/* [in] */ IStream *fist,
			/* [in] */ BSTR bstrCurrentServer,
			/* [in] */ BSTR bstrLocalServer,
			/* [in] */ BSTR bstrUserWs,
			/* [in] */ DWORD hwndParent,
			/* [in] */ ComBool fAllowMenu,
			/* [in] */ int clidSubitem,
			/* [in] */ BSTR bstrHelpFullUrl) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetResults(
			/* [out] */ ComBool *fHaveProject,
			/* [out] */ int *hvoProj,
			/* [out] */ BSTR *bstrProject,
			/* [out] */ BSTR *bstrDatabase,
			/* [out] */ BSTR *bstrMachine,
			/* [out] */ GUID *guid,
			/* [out] */ ComBool *fHaveSubitem,
			/* [out] */ int *hvoSubitem,
			/* [out] */ BSTR *bstrName) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;

	};

#else 	/* C style interface */

	typedef struct IOpenFWProjectDlgVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IOpenFWProjectDlg * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IOpenFWProjectDlg * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IOpenFWProjectDlg * This);

		HRESULT ( STDMETHODCALLTYPE *Show )(
			IOpenFWProjectDlg * This,
			/* [in] */ IStream *fist,
			/* [in] */ BSTR bstrCurrentServer,
			/* [in] */ BSTR bstrLocalServer,
			/* [in] */ BSTR bstrUserWs,
			/* [in] */ DWORD hwndParent,
			/* [in] */ ComBool fAllowMenu,
			/* [in] */ int clidSubitem,
			/* [in] */ BSTR bstrHelpFullUrl);

		HRESULT ( STDMETHODCALLTYPE *GetResults )(
			IOpenFWProjectDlg * This,
			/* [out] */ ComBool *fHaveProject,
			/* [out] */ int *hvoProj,
			/* [out] */ BSTR *bstrProject,
			/* [out] */ BSTR *bstrDatabase,
			/* [out] */ BSTR *bstrMachine,
			/* [out] */ GUID *guid,
			/* [out] */ ComBool *fHaveSubitem,
			/* [out] */ int *hvoSubitem,
			/* [out] */ BSTR *bstrName);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			IOpenFWProjectDlg * This,
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);

		END_INTERFACE
	} IOpenFWProjectDlgVtbl;

	interface IOpenFWProjectDlg
	{
		CONST_VTBL struct IOpenFWProjectDlgVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IOpenFWProjectDlg_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IOpenFWProjectDlg_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IOpenFWProjectDlg_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IOpenFWProjectDlg_Show(This,fist,bstrCurrentServer,bstrLocalServer,bstrUserWs,hwndParent,fAllowMenu,clidSubitem,bstrHelpFullUrl)	\
	(This)->lpVtbl -> Show(This,fist,bstrCurrentServer,bstrLocalServer,bstrUserWs,hwndParent,fAllowMenu,clidSubitem,bstrHelpFullUrl)

#define IOpenFWProjectDlg_GetResults(This,fHaveProject,hvoProj,bstrProject,bstrDatabase,bstrMachine,guid,fHaveSubitem,hvoSubitem,bstrName)	\
	(This)->lpVtbl -> GetResults(This,fHaveProject,hvoProj,bstrProject,bstrDatabase,bstrMachine,guid,fHaveSubitem,hvoSubitem,bstrName)

#define IOpenFWProjectDlg_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IOpenFWProjectDlg_Show_Proxy(
	IOpenFWProjectDlg * This,
	/* [in] */ IStream *fist,
	/* [in] */ BSTR bstrCurrentServer,
	/* [in] */ BSTR bstrLocalServer,
	/* [in] */ BSTR bstrUserWs,
	/* [in] */ DWORD hwndParent,
	/* [in] */ ComBool fAllowMenu,
	/* [in] */ int clidSubitem,
	/* [in] */ BSTR bstrHelpFullUrl);


void __RPC_STUB IOpenFWProjectDlg_Show_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOpenFWProjectDlg_GetResults_Proxy(
	IOpenFWProjectDlg * This,
	/* [out] */ ComBool *fHaveProject,
	/* [out] */ int *hvoProj,
	/* [out] */ BSTR *bstrProject,
	/* [out] */ BSTR *bstrDatabase,
	/* [out] */ BSTR *bstrMachine,
	/* [out] */ GUID *guid,
	/* [out] */ ComBool *fHaveSubitem,
	/* [out] */ int *hvoSubitem,
	/* [out] */ BSTR *bstrName);


void __RPC_STUB IOpenFWProjectDlg_GetResults_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IOpenFWProjectDlg_putref_WritingSystemFactory_Proxy(
	IOpenFWProjectDlg * This,
	/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB IOpenFWProjectDlg_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IOpenFWProjectDlg_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_OpenFWProjectDlg;

#ifdef __cplusplus

class DECLSPEC_UUID("D7C505D0-F132-4e40-BFE7-A2E66A46991A")
OpenFWProjectDlg;
#endif

#ifndef __IFwExportDlg_INTERFACE_DEFINED__
#define __IFwExportDlg_INTERFACE_DEFINED__

/* interface IFwExportDlg */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwExportDlg;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("67A68372-5727-4bd4-94A7-C2D703A75C36")
	IFwExportDlg : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ DWORD hwndParent,
			/* [in] */ IVwStylesheet *pvss,
			/* [in] */ /* external definition not present */ IFwCustomExport *pfcex,
			/* [in] */ GUID *pclsidApp,
			/* [in] */ BSTR bstrRegProgName,
			/* [in] */ BSTR bstrProgHelpFile,
			/* [in] */ BSTR bstrHelpTopic,
			/* [in] */ int hvoLp,
			/* [in] */ int hvoObj,
			/* [in] */ int flidSubitems) = 0;

		virtual HRESULT STDMETHODCALLTYPE DoDialog(
			/* [in] */ int vwt,
			/* [in] */ int crec,
			/* [size_is][in] */ int *rghvoRec,
			/* [size_is][in] */ int *rgclidRec,
			/* [retval][out] */ int *pnRet) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwExportDlgVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwExportDlg * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwExportDlg * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwExportDlg * This);

		HRESULT ( STDMETHODCALLTYPE *Initialize )(
			IFwExportDlg * This,
			/* [in] */ DWORD hwndParent,
			/* [in] */ IVwStylesheet *pvss,
			/* [in] */ /* external definition not present */ IFwCustomExport *pfcex,
			/* [in] */ GUID *pclsidApp,
			/* [in] */ BSTR bstrRegProgName,
			/* [in] */ BSTR bstrProgHelpFile,
			/* [in] */ BSTR bstrHelpTopic,
			/* [in] */ int hvoLp,
			/* [in] */ int hvoObj,
			/* [in] */ int flidSubitems);

		HRESULT ( STDMETHODCALLTYPE *DoDialog )(
			IFwExportDlg * This,
			/* [in] */ int vwt,
			/* [in] */ int crec,
			/* [size_is][in] */ int *rghvoRec,
			/* [size_is][in] */ int *rgclidRec,
			/* [retval][out] */ int *pnRet);

		END_INTERFACE
	} IFwExportDlgVtbl;

	interface IFwExportDlg
	{
		CONST_VTBL struct IFwExportDlgVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwExportDlg_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwExportDlg_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwExportDlg_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwExportDlg_Initialize(This,hwndParent,pvss,pfcex,pclsidApp,bstrRegProgName,bstrProgHelpFile,bstrHelpTopic,hvoLp,hvoObj,flidSubitems)	\
	(This)->lpVtbl -> Initialize(This,hwndParent,pvss,pfcex,pclsidApp,bstrRegProgName,bstrProgHelpFile,bstrHelpTopic,hvoLp,hvoObj,flidSubitems)

#define IFwExportDlg_DoDialog(This,vwt,crec,rghvoRec,rgclidRec,pnRet)	\
	(This)->lpVtbl -> DoDialog(This,vwt,crec,rghvoRec,rgclidRec,pnRet)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwExportDlg_Initialize_Proxy(
	IFwExportDlg * This,
	/* [in] */ DWORD hwndParent,
	/* [in] */ IVwStylesheet *pvss,
	/* [in] */ /* external definition not present */ IFwCustomExport *pfcex,
	/* [in] */ GUID *pclsidApp,
	/* [in] */ BSTR bstrRegProgName,
	/* [in] */ BSTR bstrProgHelpFile,
	/* [in] */ BSTR bstrHelpTopic,
	/* [in] */ int hvoLp,
	/* [in] */ int hvoObj,
	/* [in] */ int flidSubitems);


void __RPC_STUB IFwExportDlg_Initialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwExportDlg_DoDialog_Proxy(
	IFwExportDlg * This,
	/* [in] */ int vwt,
	/* [in] */ int crec,
	/* [size_is][in] */ int *rghvoRec,
	/* [size_is][in] */ int *rgclidRec,
	/* [retval][out] */ int *pnRet);


void __RPC_STUB IFwExportDlg_DoDialog_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwExportDlg_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_FwExportDlg;

#ifdef __cplusplus

class DECLSPEC_UUID("86DD56A8-CDD0-49d2-BD57-C78F8367D6C4")
FwExportDlg;
#endif

#ifndef __IFwStylesDlg_INTERFACE_DEFINED__
#define __IFwStylesDlg_INTERFACE_DEFINED__

/* interface IFwStylesDlg */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwStylesDlg;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0D598D88-C17D-4E46-AC89-51FFC5DA0799")
	IFwStylesDlg : public IUnknown
	{
	public:
		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_DlgType(
			/* [in] */ StylesDlgType sdt) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ShowAll(
			/* [in] */ ComBool fShowAll) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SysMsrUnit(
			/* [in] */ int nMsrSys) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_UserWs(
			/* [in] */ int wsUser) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_HelpFile(
			/* [in] */ BSTR bstrHelpFile) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_TabHelpFileUrl(
			/* [in] */ int tabNum,
			/* [in] */ BSTR bstrHelpFileUrl) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory(
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ParentHwnd(
			/* [in] */ DWORD hwndParent) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_CanDoRtl(
			/* [in] */ ComBool fCanDoRtl) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_OuterRtl(
			/* [in] */ ComBool fOuterRtl) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FontFeatures(
			/* [in] */ ComBool fFontFeatures) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Stylesheet(
			/* [in] */ IVwStylesheet *pasts) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetApplicableStyleContexts(
			/* [size_is][in] */ int *rgnContexts,
			/* [in] */ int cpnContexts) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_CanFormatChar(
			/* [in] */ ComBool fCanFormatChar) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_OnlyCharStyles(
			/* [in] */ ComBool fOnlyCharStyles) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_StyleName(
			/* [in] */ BSTR bstrStyleName) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_CustomStyleLevel(
			/* [in] */ int level) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetTextProps(
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **rgpttpPara,
			/* [in] */ int cttpPara,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **rgpttpChar,
			/* [in] */ int cttpChar) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_RootObjectId(
			/* [in] */ int hvoRootObj) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetWritingSystemsOfInterest(
			/* [size_is][in] */ int *rgws,
			/* [in] */ int cws) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_LogFile(
			/* [in] */ IStream *pstrmLog) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_HelpTopicProvider(
			/* [in] */ /* external definition not present */ IHelpTopicProvider *phtprov) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_AppClsid(
			/* [in] */ GUID *pclsidApp) = 0;

		virtual HRESULT STDMETHODCALLTYPE ShowModal(
			/* [retval][out] */ int *pnResult) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetResults(
			/* [out] */ BSTR *pbstrStyleName,
			/* [out] */ ComBool *pfStylesChanged,
			/* [out] */ ComBool *pfApply,
			/* [out] */ ComBool *pfReloadDb,
			/* [retval][out] */ ComBool *pfResult) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwStylesDlgVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwStylesDlg * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwStylesDlg * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwStylesDlg * This);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_DlgType )(
			IFwStylesDlg * This,
			/* [in] */ StylesDlgType sdt);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ShowAll )(
			IFwStylesDlg * This,
			/* [in] */ ComBool fShowAll);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SysMsrUnit )(
			IFwStylesDlg * This,
			/* [in] */ int nMsrSys);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_UserWs )(
			IFwStylesDlg * This,
			/* [in] */ int wsUser);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_HelpFile )(
			IFwStylesDlg * This,
			/* [in] */ BSTR bstrHelpFile);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_TabHelpFileUrl )(
			IFwStylesDlg * This,
			/* [in] */ int tabNum,
			/* [in] */ BSTR bstrHelpFileUrl);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )(
			IFwStylesDlg * This,
			/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ParentHwnd )(
			IFwStylesDlg * This,
			/* [in] */ DWORD hwndParent);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_CanDoRtl )(
			IFwStylesDlg * This,
			/* [in] */ ComBool fCanDoRtl);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_OuterRtl )(
			IFwStylesDlg * This,
			/* [in] */ ComBool fOuterRtl);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FontFeatures )(
			IFwStylesDlg * This,
			/* [in] */ ComBool fFontFeatures);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Stylesheet )(
			IFwStylesDlg * This,
			/* [in] */ IVwStylesheet *pasts);

		HRESULT ( STDMETHODCALLTYPE *SetApplicableStyleContexts )(
			IFwStylesDlg * This,
			/* [size_is][in] */ int *rgnContexts,
			/* [in] */ int cpnContexts);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_CanFormatChar )(
			IFwStylesDlg * This,
			/* [in] */ ComBool fCanFormatChar);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_OnlyCharStyles )(
			IFwStylesDlg * This,
			/* [in] */ ComBool fOnlyCharStyles);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_StyleName )(
			IFwStylesDlg * This,
			/* [in] */ BSTR bstrStyleName);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_CustomStyleLevel )(
			IFwStylesDlg * This,
			/* [in] */ int level);

		HRESULT ( STDMETHODCALLTYPE *SetTextProps )(
			IFwStylesDlg * This,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **rgpttpPara,
			/* [in] */ int cttpPara,
			/* [size_is][in] */ /* external definition not present */ ITsTextProps **rgpttpChar,
			/* [in] */ int cttpChar);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_RootObjectId )(
			IFwStylesDlg * This,
			/* [in] */ int hvoRootObj);

		HRESULT ( STDMETHODCALLTYPE *SetWritingSystemsOfInterest )(
			IFwStylesDlg * This,
			/* [size_is][in] */ int *rgws,
			/* [in] */ int cws);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_LogFile )(
			IFwStylesDlg * This,
			/* [in] */ IStream *pstrmLog);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_HelpTopicProvider )(
			IFwStylesDlg * This,
			/* [in] */ /* external definition not present */ IHelpTopicProvider *phtprov);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_AppClsid )(
			IFwStylesDlg * This,
			/* [in] */ GUID *pclsidApp);

		HRESULT ( STDMETHODCALLTYPE *ShowModal )(
			IFwStylesDlg * This,
			/* [retval][out] */ int *pnResult);

		HRESULT ( STDMETHODCALLTYPE *GetResults )(
			IFwStylesDlg * This,
			/* [out] */ BSTR *pbstrStyleName,
			/* [out] */ ComBool *pfStylesChanged,
			/* [out] */ ComBool *pfApply,
			/* [out] */ ComBool *pfReloadDb,
			/* [retval][out] */ ComBool *pfResult);

		END_INTERFACE
	} IFwStylesDlgVtbl;

	interface IFwStylesDlg
	{
		CONST_VTBL struct IFwStylesDlgVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwStylesDlg_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwStylesDlg_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwStylesDlg_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwStylesDlg_put_DlgType(This,sdt)	\
	(This)->lpVtbl -> put_DlgType(This,sdt)

#define IFwStylesDlg_put_ShowAll(This,fShowAll)	\
	(This)->lpVtbl -> put_ShowAll(This,fShowAll)

#define IFwStylesDlg_put_SysMsrUnit(This,nMsrSys)	\
	(This)->lpVtbl -> put_SysMsrUnit(This,nMsrSys)

#define IFwStylesDlg_put_UserWs(This,wsUser)	\
	(This)->lpVtbl -> put_UserWs(This,wsUser)

#define IFwStylesDlg_put_HelpFile(This,bstrHelpFile)	\
	(This)->lpVtbl -> put_HelpFile(This,bstrHelpFile)

#define IFwStylesDlg_put_TabHelpFileUrl(This,tabNum,bstrHelpFileUrl)	\
	(This)->lpVtbl -> put_TabHelpFileUrl(This,tabNum,bstrHelpFileUrl)

#define IFwStylesDlg_putref_WritingSystemFactory(This,pwsf)	\
	(This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf)

#define IFwStylesDlg_put_ParentHwnd(This,hwndParent)	\
	(This)->lpVtbl -> put_ParentHwnd(This,hwndParent)

#define IFwStylesDlg_put_CanDoRtl(This,fCanDoRtl)	\
	(This)->lpVtbl -> put_CanDoRtl(This,fCanDoRtl)

#define IFwStylesDlg_put_OuterRtl(This,fOuterRtl)	\
	(This)->lpVtbl -> put_OuterRtl(This,fOuterRtl)

#define IFwStylesDlg_put_FontFeatures(This,fFontFeatures)	\
	(This)->lpVtbl -> put_FontFeatures(This,fFontFeatures)

#define IFwStylesDlg_putref_Stylesheet(This,pasts)	\
	(This)->lpVtbl -> putref_Stylesheet(This,pasts)

#define IFwStylesDlg_SetApplicableStyleContexts(This,rgnContexts,cpnContexts)	\
	(This)->lpVtbl -> SetApplicableStyleContexts(This,rgnContexts,cpnContexts)

#define IFwStylesDlg_put_CanFormatChar(This,fCanFormatChar)	\
	(This)->lpVtbl -> put_CanFormatChar(This,fCanFormatChar)

#define IFwStylesDlg_put_OnlyCharStyles(This,fOnlyCharStyles)	\
	(This)->lpVtbl -> put_OnlyCharStyles(This,fOnlyCharStyles)

#define IFwStylesDlg_put_StyleName(This,bstrStyleName)	\
	(This)->lpVtbl -> put_StyleName(This,bstrStyleName)

#define IFwStylesDlg_put_CustomStyleLevel(This,level)	\
	(This)->lpVtbl -> put_CustomStyleLevel(This,level)

#define IFwStylesDlg_SetTextProps(This,rgpttpPara,cttpPara,rgpttpChar,cttpChar)	\
	(This)->lpVtbl -> SetTextProps(This,rgpttpPara,cttpPara,rgpttpChar,cttpChar)

#define IFwStylesDlg_put_RootObjectId(This,hvoRootObj)	\
	(This)->lpVtbl -> put_RootObjectId(This,hvoRootObj)

#define IFwStylesDlg_SetWritingSystemsOfInterest(This,rgws,cws)	\
	(This)->lpVtbl -> SetWritingSystemsOfInterest(This,rgws,cws)

#define IFwStylesDlg_putref_LogFile(This,pstrmLog)	\
	(This)->lpVtbl -> putref_LogFile(This,pstrmLog)

#define IFwStylesDlg_putref_HelpTopicProvider(This,phtprov)	\
	(This)->lpVtbl -> putref_HelpTopicProvider(This,phtprov)

#define IFwStylesDlg_put_AppClsid(This,pclsidApp)	\
	(This)->lpVtbl -> put_AppClsid(This,pclsidApp)

#define IFwStylesDlg_ShowModal(This,pnResult)	\
	(This)->lpVtbl -> ShowModal(This,pnResult)

#define IFwStylesDlg_GetResults(This,pbstrStyleName,pfStylesChanged,pfApply,pfReloadDb,pfResult)	\
	(This)->lpVtbl -> GetResults(This,pbstrStyleName,pfStylesChanged,pfApply,pfReloadDb,pfResult)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_DlgType_Proxy(
	IFwStylesDlg * This,
	/* [in] */ StylesDlgType sdt);


void __RPC_STUB IFwStylesDlg_put_DlgType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_ShowAll_Proxy(
	IFwStylesDlg * This,
	/* [in] */ ComBool fShowAll);


void __RPC_STUB IFwStylesDlg_put_ShowAll_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_SysMsrUnit_Proxy(
	IFwStylesDlg * This,
	/* [in] */ int nMsrSys);


void __RPC_STUB IFwStylesDlg_put_SysMsrUnit_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_UserWs_Proxy(
	IFwStylesDlg * This,
	/* [in] */ int wsUser);


void __RPC_STUB IFwStylesDlg_put_UserWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_HelpFile_Proxy(
	IFwStylesDlg * This,
	/* [in] */ BSTR bstrHelpFile);


void __RPC_STUB IFwStylesDlg_put_HelpFile_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_TabHelpFileUrl_Proxy(
	IFwStylesDlg * This,
	/* [in] */ int tabNum,
	/* [in] */ BSTR bstrHelpFileUrl);


void __RPC_STUB IFwStylesDlg_put_TabHelpFileUrl_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_putref_WritingSystemFactory_Proxy(
	IFwStylesDlg * This,
	/* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB IFwStylesDlg_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_ParentHwnd_Proxy(
	IFwStylesDlg * This,
	/* [in] */ DWORD hwndParent);


void __RPC_STUB IFwStylesDlg_put_ParentHwnd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_CanDoRtl_Proxy(
	IFwStylesDlg * This,
	/* [in] */ ComBool fCanDoRtl);


void __RPC_STUB IFwStylesDlg_put_CanDoRtl_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_OuterRtl_Proxy(
	IFwStylesDlg * This,
	/* [in] */ ComBool fOuterRtl);


void __RPC_STUB IFwStylesDlg_put_OuterRtl_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_FontFeatures_Proxy(
	IFwStylesDlg * This,
	/* [in] */ ComBool fFontFeatures);


void __RPC_STUB IFwStylesDlg_put_FontFeatures_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_putref_Stylesheet_Proxy(
	IFwStylesDlg * This,
	/* [in] */ IVwStylesheet *pasts);


void __RPC_STUB IFwStylesDlg_putref_Stylesheet_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwStylesDlg_SetApplicableStyleContexts_Proxy(
	IFwStylesDlg * This,
	/* [size_is][in] */ int *rgnContexts,
	/* [in] */ int cpnContexts);


void __RPC_STUB IFwStylesDlg_SetApplicableStyleContexts_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_CanFormatChar_Proxy(
	IFwStylesDlg * This,
	/* [in] */ ComBool fCanFormatChar);


void __RPC_STUB IFwStylesDlg_put_CanFormatChar_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_OnlyCharStyles_Proxy(
	IFwStylesDlg * This,
	/* [in] */ ComBool fOnlyCharStyles);


void __RPC_STUB IFwStylesDlg_put_OnlyCharStyles_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_StyleName_Proxy(
	IFwStylesDlg * This,
	/* [in] */ BSTR bstrStyleName);


void __RPC_STUB IFwStylesDlg_put_StyleName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_CustomStyleLevel_Proxy(
	IFwStylesDlg * This,
	/* [in] */ int level);


void __RPC_STUB IFwStylesDlg_put_CustomStyleLevel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwStylesDlg_SetTextProps_Proxy(
	IFwStylesDlg * This,
	/* [size_is][in] */ /* external definition not present */ ITsTextProps **rgpttpPara,
	/* [in] */ int cttpPara,
	/* [size_is][in] */ /* external definition not present */ ITsTextProps **rgpttpChar,
	/* [in] */ int cttpChar);


void __RPC_STUB IFwStylesDlg_SetTextProps_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_RootObjectId_Proxy(
	IFwStylesDlg * This,
	/* [in] */ int hvoRootObj);


void __RPC_STUB IFwStylesDlg_put_RootObjectId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwStylesDlg_SetWritingSystemsOfInterest_Proxy(
	IFwStylesDlg * This,
	/* [size_is][in] */ int *rgws,
	/* [in] */ int cws);


void __RPC_STUB IFwStylesDlg_SetWritingSystemsOfInterest_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_putref_LogFile_Proxy(
	IFwStylesDlg * This,
	/* [in] */ IStream *pstrmLog);


void __RPC_STUB IFwStylesDlg_putref_LogFile_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_putref_HelpTopicProvider_Proxy(
	IFwStylesDlg * This,
	/* [in] */ /* external definition not present */ IHelpTopicProvider *phtprov);


void __RPC_STUB IFwStylesDlg_putref_HelpTopicProvider_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwStylesDlg_put_AppClsid_Proxy(
	IFwStylesDlg * This,
	/* [in] */ GUID *pclsidApp);


void __RPC_STUB IFwStylesDlg_put_AppClsid_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwStylesDlg_ShowModal_Proxy(
	IFwStylesDlg * This,
	/* [retval][out] */ int *pnResult);


void __RPC_STUB IFwStylesDlg_ShowModal_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwStylesDlg_GetResults_Proxy(
	IFwStylesDlg * This,
	/* [out] */ BSTR *pbstrStyleName,
	/* [out] */ ComBool *pfStylesChanged,
	/* [out] */ ComBool *pfApply,
	/* [out] */ ComBool *pfReloadDb,
	/* [retval][out] */ ComBool *pfResult);


void __RPC_STUB IFwStylesDlg_GetResults_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwStylesDlg_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_FwStylesDlg;

#ifdef __cplusplus

class DECLSPEC_UUID("158F638D-D344-47FC-AB39-4C1A742FD06B")
FwStylesDlg;
#endif

#ifndef __IFwDbMergeStyles_INTERFACE_DEFINED__
#define __IFwDbMergeStyles_INTERFACE_DEFINED__

/* interface IFwDbMergeStyles */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwDbMergeStyles;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("A7CD703C-6199-4097-A5C0-AB78DD23120E")
	IFwDbMergeStyles : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pstrmLog,
			/* [in] */ int hvoRootObj,
			/* [in] */ const GUID *pclsidApp) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddStyleReplacement(
			/* [in] */ BSTR bstrOldStyleName,
			/* [in] */ BSTR bstrNewStyleName) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddStyleDeletion(
			/* [in] */ BSTR bstrDeleteStyleName) = 0;

		virtual HRESULT STDMETHODCALLTYPE Process( void) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwDbMergeStylesVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwDbMergeStyles * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwDbMergeStyles * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwDbMergeStyles * This);

		HRESULT ( STDMETHODCALLTYPE *Initialize )(
			IFwDbMergeStyles * This,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pstrmLog,
			/* [in] */ int hvoRootObj,
			/* [in] */ const GUID *pclsidApp);

		HRESULT ( STDMETHODCALLTYPE *AddStyleReplacement )(
			IFwDbMergeStyles * This,
			/* [in] */ BSTR bstrOldStyleName,
			/* [in] */ BSTR bstrNewStyleName);

		HRESULT ( STDMETHODCALLTYPE *AddStyleDeletion )(
			IFwDbMergeStyles * This,
			/* [in] */ BSTR bstrDeleteStyleName);

		HRESULT ( STDMETHODCALLTYPE *Process )(
			IFwDbMergeStyles * This);

		END_INTERFACE
	} IFwDbMergeStylesVtbl;

	interface IFwDbMergeStyles
	{
		CONST_VTBL struct IFwDbMergeStylesVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwDbMergeStyles_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwDbMergeStyles_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwDbMergeStyles_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwDbMergeStyles_Initialize(This,bstrServer,bstrDatabase,pstrmLog,hvoRootObj,pclsidApp)	\
	(This)->lpVtbl -> Initialize(This,bstrServer,bstrDatabase,pstrmLog,hvoRootObj,pclsidApp)

#define IFwDbMergeStyles_AddStyleReplacement(This,bstrOldStyleName,bstrNewStyleName)	\
	(This)->lpVtbl -> AddStyleReplacement(This,bstrOldStyleName,bstrNewStyleName)

#define IFwDbMergeStyles_AddStyleDeletion(This,bstrDeleteStyleName)	\
	(This)->lpVtbl -> AddStyleDeletion(This,bstrDeleteStyleName)

#define IFwDbMergeStyles_Process(This)	\
	(This)->lpVtbl -> Process(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwDbMergeStyles_Initialize_Proxy(
	IFwDbMergeStyles * This,
	/* [in] */ BSTR bstrServer,
	/* [in] */ BSTR bstrDatabase,
	/* [in] */ IStream *pstrmLog,
	/* [in] */ int hvoRootObj,
	/* [in] */ const GUID *pclsidApp);


void __RPC_STUB IFwDbMergeStyles_Initialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwDbMergeStyles_AddStyleReplacement_Proxy(
	IFwDbMergeStyles * This,
	/* [in] */ BSTR bstrOldStyleName,
	/* [in] */ BSTR bstrNewStyleName);


void __RPC_STUB IFwDbMergeStyles_AddStyleReplacement_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwDbMergeStyles_AddStyleDeletion_Proxy(
	IFwDbMergeStyles * This,
	/* [in] */ BSTR bstrDeleteStyleName);


void __RPC_STUB IFwDbMergeStyles_AddStyleDeletion_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwDbMergeStyles_Process_Proxy(
	IFwDbMergeStyles * This);


void __RPC_STUB IFwDbMergeStyles_Process_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwDbMergeStyles_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_FwDbMergeStyles;

#ifdef __cplusplus

class DECLSPEC_UUID("217874B4-90FE-469d-BF80-3D2306F3BB06")
FwDbMergeStyles;
#endif

#ifndef __IFwDbMergeWrtSys_INTERFACE_DEFINED__
#define __IFwDbMergeWrtSys_INTERFACE_DEFINED__

/* interface IFwDbMergeWrtSys */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwDbMergeWrtSys;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("DE96B989-91A5-4104-9764-69ABE0BF0B9A")
	IFwDbMergeWrtSys : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ /* external definition not present */ IFwTool *pfwt,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pstrmLog,
			/* [in] */ int hvoProj,
			/* [in] */ int hvoRootObj,
			/* [in] */ int wsUser) = 0;

		virtual HRESULT STDMETHODCALLTYPE Process(
			/* [in] */ int wsOld,
			/* [in] */ BSTR bstrOldName,
			/* [in] */ int wsNew,
			/* [in] */ BSTR bstrNewName) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwDbMergeWrtSysVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwDbMergeWrtSys * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwDbMergeWrtSys * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwDbMergeWrtSys * This);

		HRESULT ( STDMETHODCALLTYPE *Initialize )(
			IFwDbMergeWrtSys * This,
			/* [in] */ /* external definition not present */ IFwTool *pfwt,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pstrmLog,
			/* [in] */ int hvoProj,
			/* [in] */ int hvoRootObj,
			/* [in] */ int wsUser);

		HRESULT ( STDMETHODCALLTYPE *Process )(
			IFwDbMergeWrtSys * This,
			/* [in] */ int wsOld,
			/* [in] */ BSTR bstrOldName,
			/* [in] */ int wsNew,
			/* [in] */ BSTR bstrNewName);

		END_INTERFACE
	} IFwDbMergeWrtSysVtbl;

	interface IFwDbMergeWrtSys
	{
		CONST_VTBL struct IFwDbMergeWrtSysVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwDbMergeWrtSys_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwDbMergeWrtSys_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwDbMergeWrtSys_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwDbMergeWrtSys_Initialize(This,pfwt,bstrServer,bstrDatabase,pstrmLog,hvoProj,hvoRootObj,wsUser)	\
	(This)->lpVtbl -> Initialize(This,pfwt,bstrServer,bstrDatabase,pstrmLog,hvoProj,hvoRootObj,wsUser)

#define IFwDbMergeWrtSys_Process(This,wsOld,bstrOldName,wsNew,bstrNewName)	\
	(This)->lpVtbl -> Process(This,wsOld,bstrOldName,wsNew,bstrNewName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwDbMergeWrtSys_Initialize_Proxy(
	IFwDbMergeWrtSys * This,
	/* [in] */ /* external definition not present */ IFwTool *pfwt,
	/* [in] */ BSTR bstrServer,
	/* [in] */ BSTR bstrDatabase,
	/* [in] */ IStream *pstrmLog,
	/* [in] */ int hvoProj,
	/* [in] */ int hvoRootObj,
	/* [in] */ int wsUser);


void __RPC_STUB IFwDbMergeWrtSys_Initialize_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwDbMergeWrtSys_Process_Proxy(
	IFwDbMergeWrtSys * This,
	/* [in] */ int wsOld,
	/* [in] */ BSTR bstrOldName,
	/* [in] */ int wsNew,
	/* [in] */ BSTR bstrNewName);


void __RPC_STUB IFwDbMergeWrtSys_Process_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwDbMergeWrtSys_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_FwDbMergeWrtSys;

#ifdef __cplusplus

class DECLSPEC_UUID("40E4B757-4B7F-4B7C-A498-3EB942E7C6D6")
FwDbMergeWrtSys;
#endif

#ifndef __IFwCheckAnthroList_INTERFACE_DEFINED__
#define __IFwCheckAnthroList_INTERFACE_DEFINED__

/* interface IFwCheckAnthroList */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwCheckAnthroList;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8AC06CED-7B73-4E34-81A3-852A43E28BD8")
	IFwCheckAnthroList : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE CheckAnthroList(
			/* [in] */ /* external definition not present */ IOleDbEncap *pode,
			/* [in] */ DWORD hwndParent,
			/* [in] */ BSTR bstrProjName,
			/* [in] */ int wsDefault) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Description(
			/* [in] */ BSTR bstrDescription) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwCheckAnthroListVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwCheckAnthroList * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwCheckAnthroList * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwCheckAnthroList * This);

		HRESULT ( STDMETHODCALLTYPE *CheckAnthroList )(
			IFwCheckAnthroList * This,
			/* [in] */ /* external definition not present */ IOleDbEncap *pode,
			/* [in] */ DWORD hwndParent,
			/* [in] */ BSTR bstrProjName,
			/* [in] */ int wsDefault);

		/* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Description )(
			IFwCheckAnthroList * This,
			/* [in] */ BSTR bstrDescription);

		END_INTERFACE
	} IFwCheckAnthroListVtbl;

	interface IFwCheckAnthroList
	{
		CONST_VTBL struct IFwCheckAnthroListVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwCheckAnthroList_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwCheckAnthroList_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwCheckAnthroList_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwCheckAnthroList_CheckAnthroList(This,pode,hwndParent,bstrProjName,wsDefault)	\
	(This)->lpVtbl -> CheckAnthroList(This,pode,hwndParent,bstrProjName,wsDefault)

#define IFwCheckAnthroList_put_Description(This,bstrDescription)	\
	(This)->lpVtbl -> put_Description(This,bstrDescription)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwCheckAnthroList_CheckAnthroList_Proxy(
	IFwCheckAnthroList * This,
	/* [in] */ /* external definition not present */ IOleDbEncap *pode,
	/* [in] */ DWORD hwndParent,
	/* [in] */ BSTR bstrProjName,
	/* [in] */ int wsDefault);


void __RPC_STUB IFwCheckAnthroList_CheckAnthroList_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propput] */ HRESULT STDMETHODCALLTYPE IFwCheckAnthroList_put_Description_Proxy(
	IFwCheckAnthroList * This,
	/* [in] */ BSTR bstrDescription);


void __RPC_STUB IFwCheckAnthroList_put_Description_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwCheckAnthroList_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_FwCheckAnthroList;

#ifdef __cplusplus

class DECLSPEC_UUID("4D84B554-D3C8-4E0F-9416-4B26A4F0324B")
FwCheckAnthroList;
#endif

#ifndef __IMigrateData_INTERFACE_DEFINED__
#define __IMigrateData_INTERFACE_DEFINED__

/* interface IMigrateData */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IMigrateData;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("7BF2BC32-A603-4ba1-AC1F-B59D9F5FED8B")
	IMigrateData : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Migrate(
			/* [in] */ BSTR bstrDbName,
			/* [in] */ int nDestVersion,
			/* [in] */ IStream *pfist) = 0;

	};

#else 	/* C style interface */

	typedef struct IMigrateDataVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IMigrateData * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IMigrateData * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IMigrateData * This);

		HRESULT ( STDMETHODCALLTYPE *Migrate )(
			IMigrateData * This,
			/* [in] */ BSTR bstrDbName,
			/* [in] */ int nDestVersion,
			/* [in] */ IStream *pfist);

		END_INTERFACE
	} IMigrateDataVtbl;

	interface IMigrateData
	{
		CONST_VTBL struct IMigrateDataVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IMigrateData_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IMigrateData_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IMigrateData_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IMigrateData_Migrate(This,bstrDbName,nDestVersion,pfist)	\
	(This)->lpVtbl -> Migrate(This,bstrDbName,nDestVersion,pfist)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IMigrateData_Migrate_Proxy(
	IMigrateData * This,
	/* [in] */ BSTR bstrDbName,
	/* [in] */ int nDestVersion,
	/* [in] */ IStream *pfist);


void __RPC_STUB IMigrateData_Migrate_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IMigrateData_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_MigrateData;

#ifdef __cplusplus

class DECLSPEC_UUID("461989B4-CA92-4eab-8CAD-ADB28C3B4D10")
MigrateData;
#endif
#endif /* __Views_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
