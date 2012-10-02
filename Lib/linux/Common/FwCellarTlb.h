

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0500 */
/* at Fri Sep 09 18:38:15 2011
 */
/* Compiler settings for D:\jenkins\jobs\FieldWorks-Calgary-WW-build-tlb\workspace\Output\Common\FwCellarTlb.idl:
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


#ifndef __FwCellarTlb_h__
#define __FwCellarTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif


/* interface __MIDL_itf_FwCellarTlb_0000_0000 */
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
2F0FCCC0-C160-11d3-8DA2-005004DEFEC4
,
FwCellarLib
);


extern RPC_IF_HANDLE __MIDL_itf_FwCellarTlb_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_FwCellarTlb_0000_0000_v0_0_s_ifspec;


#ifndef __FwCellarLib_LIBRARY_DEFINED__
#define __FwCellarLib_LIBRARY_DEFINED__

/* library FwCellarLib */
/* [helpstring][version][uuid] */

typedef /* [v1_enum] */
enum CellarModuleDefns
	{	kcptNil	= 0,
	kcptMin	= 1,
	kcptBoolean	= 1,
	kcptInteger	= 2,
	kcptNumeric	= 3,
	kcptFloat	= 4,
	kcptTime	= 5,
	kcptGuid	= 6,
	kcptImage	= 7,
	kcptGenDate	= 8,
	kcptBinary	= 9,
	kcptString	= 13,
	kcptMultiString	= 14,
	kcptUnicode	= 15,
	kcptMultiUnicode	= 16,
	kcptBigString	= 17,
	kcptMultiBigString	= 18,
	kcptBigUnicode	= 19,
	kcptMultiBigUnicode	= 20,
	kcptMinObj	= 23,
	kcptOwningAtom	= 23,
	kcptReferenceAtom	= 24,
	kcptOwningCollection	= 25,
	kcptReferenceCollection	= 26,
	kcptOwningSequence	= 27,
	kcptReferenceSequence	= 28,
	kcptLim	= 29,
	kcptVirtual	= 32,
	kfcptOwningAtom	= 8388608,
	kfcptReferenceAtom	= 16777216,
	kfcptOwningCollection	= 33554432,
	kfcptReferenceCollection	= 67108864,
	kfcptOwningSequence	= 134217728,
	kfcptReferenceSequence	= 268435456,
	kgrfcptOwning	= 176160768,
	kgrfcptReference	= 352321536,
	kgrfcptAll	= 528482304,
	kwsAnal	= 0xffffffff,
	kwsVern	= 0xfffffffe,
	kwsAnals	= 0xfffffffd,
	kwsVerns	= 0xfffffffc,
	kwsAnalVerns	= 0xfffffffb,
	kwsVernAnals	= 0xfffffffa,
	kwsLim	= 0xfffffff9,
	kflidStartDummyFlids	= 1000000000
	} 	CellarModuleDefns;

typedef
enum CmObjectFields
	{	kflidCmObject_Id	= 100,
	kflidCmObject_Guid	= ( kflidCmObject_Id + 1 ) ,
	kflidCmObject_Class	= ( kflidCmObject_Guid + 1 ) ,
	kflidCmObject_Owner	= ( kflidCmObject_Class + 1 ) ,
	kflidCmObject_OwnFlid	= ( kflidCmObject_Owner + 1 ) ,
	kflidCmObject_OwnOrd	= ( kflidCmObject_OwnFlid + 1 )
	} 	CmObjectFields;


#define LIBID_FwCellarLib __uuidof(FwCellarLib)
#endif /* __FwCellarLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
