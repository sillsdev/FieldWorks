

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:11:57 2006
 */
/* Compiler settings for C:\fw\Output\Common\MigrateDataTlb.idl:
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


#ifndef __MigrateDataTlb_h__
#define __MigrateDataTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

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

/* interface __MIDL_itf_MigrateDataTlb_0000 */
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
727A3C6C-E2C3-473b-81FF-E8B34C88BC6E
,
DataUpgrader
);


extern RPC_IF_HANDLE __MIDL_itf_MigrateDataTlb_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_MigrateDataTlb_0000_v0_0_s_ifspec;


#ifndef __DataUpgrader_LIBRARY_DEFINED__
#define __DataUpgrader_LIBRARY_DEFINED__

/* library DataUpgrader */
/* [helpstring][version][uuid] */

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

EXTERN_C const IID LIBID_DataUpgrader;

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
#endif /* __DataUpgrader_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
