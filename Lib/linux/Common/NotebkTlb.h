

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:14:07 2006
 */
/* Compiler settings for C:\fw\Output\Common\NotebkTlb.idl:
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


#ifndef __NotebkTlb_h__
#define __NotebkTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __ResearchNotebook_FWD_DEFINED__
#define __ResearchNotebook_FWD_DEFINED__

#ifdef __cplusplus
typedef class ResearchNotebook ResearchNotebook;
#else
typedef struct ResearchNotebook ResearchNotebook;
#endif /* __cplusplus */

#endif 	/* __ResearchNotebook_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_NotebkTlb_0000 */
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
6B94B3E1-4DD4-11d4-8078-0000C0FB81B5
,
NoteBkLib
);


extern RPC_IF_HANDLE __MIDL_itf_NotebkTlb_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_NotebkTlb_0000_v0_0_s_ifspec;


#ifndef __NoteBkLib_LIBRARY_DEFINED__
#define __NoteBkLib_LIBRARY_DEFINED__

/* library NoteBkLib */
/* [helpstring][version][uuid] */

ATTACH_GUID_TO_CLASS(class,
39886581-4DD5-11d4-8078-0000C0FB81B5
,
ResearchNotebook
);

#define LIBID_NoteBkLib __uuidof(NoteBkLib)

#define CLSID_ResearchNotebook __uuidof(ResearchNotebook)

#ifdef __cplusplus

class DECLSPEC_UUID("39886581-4DD5-11d4-8078-0000C0FB81B5")
ResearchNotebook;
#endif
#endif /* __NoteBkLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
