

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:14:31 2006
 */
/* Compiler settings for C:\fw\Output\Common\CleTlb.idl:
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


#ifndef __CleTlb_h__
#define __CleTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __ChoicesListEditor_FWD_DEFINED__
#define __ChoicesListEditor_FWD_DEFINED__

#ifdef __cplusplus
typedef class ChoicesListEditor ChoicesListEditor;
#else
typedef struct ChoicesListEditor ChoicesListEditor;
#endif /* __cplusplus */

#endif 	/* __ChoicesListEditor_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_CleTlb_0000 */
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
6F4B4141-7A8C-11d4-8078-0000C0FB81B5
,
CleLib
);


extern RPC_IF_HANDLE __MIDL_itf_CleTlb_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CleTlb_0000_v0_0_s_ifspec;


#ifndef __CleLib_LIBRARY_DEFINED__
#define __CleLib_LIBRARY_DEFINED__

/* library CleLib */
/* [helpstring][version][uuid] */

ATTACH_GUID_TO_CLASS(class,
5EA62D01-7A78-11d4-8078-0000C0FB81B5
,
ChoicesListEditor
);

EXTERN_C const IID LIBID_CleLib;

EXTERN_C const CLSID CLSID_ChoicesListEditor;

#ifdef __cplusplus

class DECLSPEC_UUID("5EA62D01-7A78-11d4-8078-0000C0FB81B5")
ChoicesListEditor;
#endif
#endif /* __CleLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
