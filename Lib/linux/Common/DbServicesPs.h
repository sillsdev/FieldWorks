

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:12:38 2006
 */
/* Compiler settings for C:\fw\Output\Common\DbServicesPs.idl:
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

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __DbServicesPs_h__
#define __DbServicesPs_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __IBackupDelegates_FWD_DEFINED__
#define __IBackupDelegates_FWD_DEFINED__
typedef interface IBackupDelegates IBackupDelegates;
#endif 	/* __IBackupDelegates_FWD_DEFINED__ */


#ifndef __DIFwBackupDb_FWD_DEFINED__
#define __DIFwBackupDb_FWD_DEFINED__
typedef interface DIFwBackupDb DIFwBackupDb;
#endif 	/* __DIFwBackupDb_FWD_DEFINED__ */


#ifndef __IDisconnectDb_FWD_DEFINED__
#define __IDisconnectDb_FWD_DEFINED__
typedef interface IDisconnectDb IDisconnectDb;
#endif 	/* __IDisconnectDb_FWD_DEFINED__ */


#ifndef __IRemoteDbWarn_FWD_DEFINED__
#define __IRemoteDbWarn_FWD_DEFINED__
typedef interface IRemoteDbWarn IRemoteDbWarn;
#endif 	/* __IRemoteDbWarn_FWD_DEFINED__ */


#ifndef __IDbWarnSetup_FWD_DEFINED__
#define __IDbWarnSetup_FWD_DEFINED__
typedef interface IDbWarnSetup IDbWarnSetup;
#endif 	/* __IDbWarnSetup_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_DbServicesPs_0000 */
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

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IBackupDelegates
,
1C0FA5AF-00B4-4dc1-8F9E-168AF3F892B0
);


extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0000_v0_0_s_ifspec;

#ifndef __IBackupDelegates_INTERFACE_DEFINED__
#define __IBackupDelegates_INTERFACE_DEFINED__

/* interface IBackupDelegates */
/* [unique][object][uuid] */


#define IID_IBackupDelegates __uuidof(IBackupDelegates)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("1C0FA5AF-00B4-4dc1-8F9E-168AF3F892B0")
	IBackupDelegates : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE GetLocalServer_Bkupd(
			/* [retval][out] */ BSTR *pbstrSvrName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetLogPointer_Bkupd(
			/* [retval][out] */ IStream **ppfist) = 0;

		virtual HRESULT STDMETHODCALLTYPE SaveAllData_Bkupd(
			/* [string][in] */ const OLECHAR *pszServer,
			/* [string][in] */ const OLECHAR *pszDbName) = 0;

		virtual HRESULT STDMETHODCALLTYPE CloseDbAndWindows_Bkupd(
			/* [string][in] */ const OLECHAR *pszSvrName,
			/* [string][in] */ const OLECHAR *pszDbName,
			/* [in] */ ComBool fOkToClose,
			/* [retval][out] */ ComBool *pfWindowsClosed) = 0;

		virtual HRESULT STDMETHODCALLTYPE IncExportedObjects_Bkupd( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE DecExportedObjects_Bkupd( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CheckDbVerCompatibility_Bkupd(
			/* [string][in] */ const OLECHAR *pszSvrName,
			/* [string][in] */ const OLECHAR *pszDbName,
			/* [retval][out] */ ComBool *pfCompatible) = 0;

		virtual HRESULT STDMETHODCALLTYPE ReopenDbAndOneWindow_Bkupd(
			/* [string][in] */ const OLECHAR *pszSvrName,
			/* [string][in] */ const OLECHAR *pszDbName) = 0;

	};

#else 	/* C style interface */

	typedef struct IBackupDelegatesVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IBackupDelegates * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IBackupDelegates * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IBackupDelegates * This);

		HRESULT ( STDMETHODCALLTYPE *GetLocalServer_Bkupd )(
			IBackupDelegates * This,
			/* [retval][out] */ BSTR *pbstrSvrName);

		HRESULT ( STDMETHODCALLTYPE *GetLogPointer_Bkupd )(
			IBackupDelegates * This,
			/* [retval][out] */ IStream **ppfist);

		HRESULT ( STDMETHODCALLTYPE *SaveAllData_Bkupd )(
			IBackupDelegates * This,
			/* [string][in] */ const OLECHAR *pszServer,
			/* [string][in] */ const OLECHAR *pszDbName);

		HRESULT ( STDMETHODCALLTYPE *CloseDbAndWindows_Bkupd )(
			IBackupDelegates * This,
			/* [string][in] */ const OLECHAR *pszSvrName,
			/* [string][in] */ const OLECHAR *pszDbName,
			/* [in] */ ComBool fOkToClose,
			/* [retval][out] */ ComBool *pfWindowsClosed);

		HRESULT ( STDMETHODCALLTYPE *IncExportedObjects_Bkupd )(
			IBackupDelegates * This);

		HRESULT ( STDMETHODCALLTYPE *DecExportedObjects_Bkupd )(
			IBackupDelegates * This);

		HRESULT ( STDMETHODCALLTYPE *CheckDbVerCompatibility_Bkupd )(
			IBackupDelegates * This,
			/* [string][in] */ const OLECHAR *pszSvrName,
			/* [string][in] */ const OLECHAR *pszDbName,
			/* [retval][out] */ ComBool *pfCompatible);

		HRESULT ( STDMETHODCALLTYPE *ReopenDbAndOneWindow_Bkupd )(
			IBackupDelegates * This,
			/* [string][in] */ const OLECHAR *pszSvrName,
			/* [string][in] */ const OLECHAR *pszDbName);

		END_INTERFACE
	} IBackupDelegatesVtbl;

	interface IBackupDelegates
	{
		CONST_VTBL struct IBackupDelegatesVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IBackupDelegates_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IBackupDelegates_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IBackupDelegates_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IBackupDelegates_GetLocalServer_Bkupd(This,pbstrSvrName)	\
	(This)->lpVtbl -> GetLocalServer_Bkupd(This,pbstrSvrName)

#define IBackupDelegates_GetLogPointer_Bkupd(This,ppfist)	\
	(This)->lpVtbl -> GetLogPointer_Bkupd(This,ppfist)

#define IBackupDelegates_SaveAllData_Bkupd(This,pszServer,pszDbName)	\
	(This)->lpVtbl -> SaveAllData_Bkupd(This,pszServer,pszDbName)

#define IBackupDelegates_CloseDbAndWindows_Bkupd(This,pszSvrName,pszDbName,fOkToClose,pfWindowsClosed)	\
	(This)->lpVtbl -> CloseDbAndWindows_Bkupd(This,pszSvrName,pszDbName,fOkToClose,pfWindowsClosed)

#define IBackupDelegates_IncExportedObjects_Bkupd(This)	\
	(This)->lpVtbl -> IncExportedObjects_Bkupd(This)

#define IBackupDelegates_DecExportedObjects_Bkupd(This)	\
	(This)->lpVtbl -> DecExportedObjects_Bkupd(This)

#define IBackupDelegates_CheckDbVerCompatibility_Bkupd(This,pszSvrName,pszDbName,pfCompatible)	\
	(This)->lpVtbl -> CheckDbVerCompatibility_Bkupd(This,pszSvrName,pszDbName,pfCompatible)

#define IBackupDelegates_ReopenDbAndOneWindow_Bkupd(This,pszSvrName,pszDbName)	\
	(This)->lpVtbl -> ReopenDbAndOneWindow_Bkupd(This,pszSvrName,pszDbName)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IBackupDelegates_GetLocalServer_Bkupd_Proxy(
	IBackupDelegates * This,
	/* [retval][out] */ BSTR *pbstrSvrName);


void __RPC_STUB IBackupDelegates_GetLocalServer_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IBackupDelegates_GetLogPointer_Bkupd_Proxy(
	IBackupDelegates * This,
	/* [retval][out] */ IStream **ppfist);


void __RPC_STUB IBackupDelegates_GetLogPointer_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IBackupDelegates_SaveAllData_Bkupd_Proxy(
	IBackupDelegates * This,
	/* [string][in] */ const OLECHAR *pszServer,
	/* [string][in] */ const OLECHAR *pszDbName);


void __RPC_STUB IBackupDelegates_SaveAllData_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IBackupDelegates_CloseDbAndWindows_Bkupd_Proxy(
	IBackupDelegates * This,
	/* [string][in] */ const OLECHAR *pszSvrName,
	/* [string][in] */ const OLECHAR *pszDbName,
	/* [in] */ ComBool fOkToClose,
	/* [retval][out] */ ComBool *pfWindowsClosed);


void __RPC_STUB IBackupDelegates_CloseDbAndWindows_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IBackupDelegates_IncExportedObjects_Bkupd_Proxy(
	IBackupDelegates * This);


void __RPC_STUB IBackupDelegates_IncExportedObjects_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IBackupDelegates_DecExportedObjects_Bkupd_Proxy(
	IBackupDelegates * This);


void __RPC_STUB IBackupDelegates_DecExportedObjects_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IBackupDelegates_CheckDbVerCompatibility_Bkupd_Proxy(
	IBackupDelegates * This,
	/* [string][in] */ const OLECHAR *pszSvrName,
	/* [string][in] */ const OLECHAR *pszDbName,
	/* [retval][out] */ ComBool *pfCompatible);


void __RPC_STUB IBackupDelegates_CheckDbVerCompatibility_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IBackupDelegates_ReopenDbAndOneWindow_Bkupd_Proxy(
	IBackupDelegates * This,
	/* [string][in] */ const OLECHAR *pszSvrName,
	/* [string][in] */ const OLECHAR *pszDbName);


void __RPC_STUB IBackupDelegates_ReopenDbAndOneWindow_Bkupd_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IBackupDelegates_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_DbServicesPs_0257 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
DIFwBackupDb
,
00A94783-8F5F-42af-A993-49F2154A67E2
);


extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0257_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0257_v0_0_s_ifspec;

#ifndef __DIFwBackupDb_INTERFACE_DEFINED__
#define __DIFwBackupDb_INTERFACE_DEFINED__

/* interface DIFwBackupDb */
/* [object][unique][oleautomation][dual][uuid] */


#define IID_DIFwBackupDb __uuidof(DIFwBackupDb)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("00A94783-8F5F-42af-A993-49F2154A67E2")
	DIFwBackupDb : public IDispatch
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ IBackupDelegates *pbkupd,
			/* [in] */ int hwndParent) = 0;

		virtual HRESULT STDMETHODCALLTYPE CheckForMissedSchedules( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Backup( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE Remind( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE UserConfigure(
			/* [in] */ IUnknown *phtprovHelpUrls,
			/* [in] */ ComBool fShowRestore,
			/* [retval][out] */ int *pnUserAction) = 0;

	};

#else 	/* C style interface */

	typedef struct DIFwBackupDbVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			DIFwBackupDb * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			DIFwBackupDb * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			DIFwBackupDb * This);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )(
			DIFwBackupDb * This,
			/* [out] */ UINT *pctinfo);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )(
			DIFwBackupDb * This,
			/* [in] */ UINT iTInfo,
			/* [in] */ LCID lcid,
			/* [out] */ ITypeInfo **ppTInfo);

		HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )(
			DIFwBackupDb * This,
			/* [in] */ REFIID riid,
			/* [size_is][in] */ LPOLESTR *rgszNames,
			/* [in] */ UINT cNames,
			/* [in] */ LCID lcid,
			/* [size_is][out] */ DISPID *rgDispId);

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )(
			DIFwBackupDb * This,
			/* [in] */ DISPID dispIdMember,
			/* [in] */ REFIID riid,
			/* [in] */ LCID lcid,
			/* [in] */ WORD wFlags,
			/* [out][in] */ DISPPARAMS *pDispParams,
			/* [out] */ VARIANT *pVarResult,
			/* [out] */ EXCEPINFO *pExcepInfo,
			/* [out] */ UINT *puArgErr);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			DIFwBackupDb * This,
			/* [in] */ IBackupDelegates *pbkupd,
			/* [in] */ int hwndParent);

		HRESULT ( STDMETHODCALLTYPE *CheckForMissedSchedules )(
			DIFwBackupDb * This);

		HRESULT ( STDMETHODCALLTYPE *Backup )(
			DIFwBackupDb * This);

		HRESULT ( STDMETHODCALLTYPE *Remind )(
			DIFwBackupDb * This);

		HRESULT ( STDMETHODCALLTYPE *UserConfigure )(
			DIFwBackupDb * This,
			/* [in] */ IUnknown *phtprovHelpUrls,
			/* [in] */ ComBool fShowRestore,
			/* [retval][out] */ int *pnUserAction);

		END_INTERFACE
	} DIFwBackupDbVtbl;

	interface DIFwBackupDb
	{
		CONST_VTBL struct DIFwBackupDbVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define DIFwBackupDb_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define DIFwBackupDb_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define DIFwBackupDb_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define DIFwBackupDb_GetTypeInfoCount(This,pctinfo)	\
	(This)->lpVtbl -> GetTypeInfoCount(This,pctinfo)

#define DIFwBackupDb_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
	(This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo)

#define DIFwBackupDb_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
	(This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)

#define DIFwBackupDb_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
	(This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)


#define DIFwBackupDb_Init(This,pbkupd,hwndParent)	\
	(This)->lpVtbl -> Init(This,pbkupd,hwndParent)

#define DIFwBackupDb_CheckForMissedSchedules(This)	\
	(This)->lpVtbl -> CheckForMissedSchedules(This)

#define DIFwBackupDb_Backup(This)	\
	(This)->lpVtbl -> Backup(This)

#define DIFwBackupDb_Remind(This)	\
	(This)->lpVtbl -> Remind(This)

#define DIFwBackupDb_UserConfigure(This,phtprovHelpUrls,fShowRestore,pnUserAction)	\
	(This)->lpVtbl -> UserConfigure(This,phtprovHelpUrls,fShowRestore,pnUserAction)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE DIFwBackupDb_Init_Proxy(
	DIFwBackupDb * This,
	/* [in] */ IBackupDelegates *pbkupd,
	/* [in] */ int hwndParent);


void __RPC_STUB DIFwBackupDb_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE DIFwBackupDb_CheckForMissedSchedules_Proxy(
	DIFwBackupDb * This);


void __RPC_STUB DIFwBackupDb_CheckForMissedSchedules_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE DIFwBackupDb_Backup_Proxy(
	DIFwBackupDb * This);


void __RPC_STUB DIFwBackupDb_Backup_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE DIFwBackupDb_Remind_Proxy(
	DIFwBackupDb * This);


void __RPC_STUB DIFwBackupDb_Remind_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE DIFwBackupDb_UserConfigure_Proxy(
	DIFwBackupDb * This,
	/* [in] */ IUnknown *phtprovHelpUrls,
	/* [in] */ ComBool fShowRestore,
	/* [retval][out] */ int *pnUserAction);


void __RPC_STUB DIFwBackupDb_UserConfigure_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __DIFwBackupDb_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_DbServicesPs_0259 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDisconnectDb
,
0CC74E0C-3017-4c02-A507-3FB8CE621CDC
);


extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0259_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0259_v0_0_s_ifspec;

#ifndef __IDisconnectDb_INTERFACE_DEFINED__
#define __IDisconnectDb_INTERFACE_DEFINED__

/* interface IDisconnectDb */
/* [unique][object][uuid] */


#define IID_IDisconnectDb __uuidof(IDisconnectDb)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("0CC74E0C-3017-4c02-A507-3FB8CE621CDC")
	IDisconnectDb : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrReason,
			/* [in] */ BSTR bstrExternalReason,
			/* [in] */ ComBool fConfirmCancel,
			/* [in] */ BSTR bstrCancelQuestion,
			/* [in] */ int hwndParent) = 0;

		virtual HRESULT STDMETHODCALLTYPE CheckConnections(
			/* [retval][out] */ int *pnResponse) = 0;

		virtual HRESULT STDMETHODCALLTYPE DisconnectAll(
			/* [retval][out] */ ComBool *pfResult) = 0;

		virtual HRESULT STDMETHODCALLTYPE ForceDisconnectAll( void) = 0;

	};

#else 	/* C style interface */

	typedef struct IDisconnectDbVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IDisconnectDb * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IDisconnectDb * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IDisconnectDb * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IDisconnectDb * This,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrReason,
			/* [in] */ BSTR bstrExternalReason,
			/* [in] */ ComBool fConfirmCancel,
			/* [in] */ BSTR bstrCancelQuestion,
			/* [in] */ int hwndParent);

		HRESULT ( STDMETHODCALLTYPE *CheckConnections )(
			IDisconnectDb * This,
			/* [retval][out] */ int *pnResponse);

		HRESULT ( STDMETHODCALLTYPE *DisconnectAll )(
			IDisconnectDb * This,
			/* [retval][out] */ ComBool *pfResult);

		HRESULT ( STDMETHODCALLTYPE *ForceDisconnectAll )(
			IDisconnectDb * This);

		END_INTERFACE
	} IDisconnectDbVtbl;

	interface IDisconnectDb
	{
		CONST_VTBL struct IDisconnectDbVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IDisconnectDb_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDisconnectDb_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IDisconnectDb_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IDisconnectDb_Init(This,bstrDatabase,bstrServer,bstrReason,bstrExternalReason,fConfirmCancel,bstrCancelQuestion,hwndParent)	\
	(This)->lpVtbl -> Init(This,bstrDatabase,bstrServer,bstrReason,bstrExternalReason,fConfirmCancel,bstrCancelQuestion,hwndParent)

#define IDisconnectDb_CheckConnections(This,pnResponse)	\
	(This)->lpVtbl -> CheckConnections(This,pnResponse)

#define IDisconnectDb_DisconnectAll(This,pfResult)	\
	(This)->lpVtbl -> DisconnectAll(This,pfResult)

#define IDisconnectDb_ForceDisconnectAll(This)	\
	(This)->lpVtbl -> ForceDisconnectAll(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDisconnectDb_Init_Proxy(
	IDisconnectDb * This,
	/* [in] */ BSTR bstrDatabase,
	/* [in] */ BSTR bstrServer,
	/* [in] */ BSTR bstrReason,
	/* [in] */ BSTR bstrExternalReason,
	/* [in] */ ComBool fConfirmCancel,
	/* [in] */ BSTR bstrCancelQuestion,
	/* [in] */ int hwndParent);


void __RPC_STUB IDisconnectDb_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDisconnectDb_CheckConnections_Proxy(
	IDisconnectDb * This,
	/* [retval][out] */ int *pnResponse);


void __RPC_STUB IDisconnectDb_CheckConnections_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDisconnectDb_DisconnectAll_Proxy(
	IDisconnectDb * This,
	/* [retval][out] */ ComBool *pfResult);


void __RPC_STUB IDisconnectDb_DisconnectAll_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDisconnectDb_ForceDisconnectAll_Proxy(
	IDisconnectDb * This);


void __RPC_STUB IDisconnectDb_ForceDisconnectAll_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IDisconnectDb_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_DbServicesPs_0261 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IRemoteDbWarn
,
004C42AE-CB07-47b5-A936-D9CA4AC466D7
);


extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0261_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0261_v0_0_s_ifspec;

#ifndef __IRemoteDbWarn_INTERFACE_DEFINED__
#define __IRemoteDbWarn_INTERFACE_DEFINED__

/* interface IRemoteDbWarn */
/* [unique][object][uuid] */


#define IID_IRemoteDbWarn __uuidof(IRemoteDbWarn)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("004C42AE-CB07-47b5-A936-D9CA4AC466D7")
	IRemoteDbWarn : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE WarnSimple(
			/* [in] */ BSTR bstrMessage,
			/* [in] */ int nFlags,
			/* [retval][out] */ int *pnResponse) = 0;

		virtual HRESULT STDMETHODCALLTYPE WarnWithTimeout(
			/* [in] */ BSTR bstrMessage,
			/* [in] */ int nTimeLeft) = 0;

		virtual HRESULT STDMETHODCALLTYPE Cancel( void) = 0;

	};

#else 	/* C style interface */

	typedef struct IRemoteDbWarnVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IRemoteDbWarn * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IRemoteDbWarn * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IRemoteDbWarn * This);

		HRESULT ( STDMETHODCALLTYPE *WarnSimple )(
			IRemoteDbWarn * This,
			/* [in] */ BSTR bstrMessage,
			/* [in] */ int nFlags,
			/* [retval][out] */ int *pnResponse);

		HRESULT ( STDMETHODCALLTYPE *WarnWithTimeout )(
			IRemoteDbWarn * This,
			/* [in] */ BSTR bstrMessage,
			/* [in] */ int nTimeLeft);

		HRESULT ( STDMETHODCALLTYPE *Cancel )(
			IRemoteDbWarn * This);

		END_INTERFACE
	} IRemoteDbWarnVtbl;

	interface IRemoteDbWarn
	{
		CONST_VTBL struct IRemoteDbWarnVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IRemoteDbWarn_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IRemoteDbWarn_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IRemoteDbWarn_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IRemoteDbWarn_WarnSimple(This,bstrMessage,nFlags,pnResponse)	\
	(This)->lpVtbl -> WarnSimple(This,bstrMessage,nFlags,pnResponse)

#define IRemoteDbWarn_WarnWithTimeout(This,bstrMessage,nTimeLeft)	\
	(This)->lpVtbl -> WarnWithTimeout(This,bstrMessage,nTimeLeft)

#define IRemoteDbWarn_Cancel(This)	\
	(This)->lpVtbl -> Cancel(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IRemoteDbWarn_WarnSimple_Proxy(
	IRemoteDbWarn * This,
	/* [in] */ BSTR bstrMessage,
	/* [in] */ int nFlags,
	/* [retval][out] */ int *pnResponse);


void __RPC_STUB IRemoteDbWarn_WarnSimple_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRemoteDbWarn_WarnWithTimeout_Proxy(
	IRemoteDbWarn * This,
	/* [in] */ BSTR bstrMessage,
	/* [in] */ int nTimeLeft);


void __RPC_STUB IRemoteDbWarn_WarnWithTimeout_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IRemoteDbWarn_Cancel_Proxy(
	IRemoteDbWarn * This);


void __RPC_STUB IRemoteDbWarn_Cancel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IRemoteDbWarn_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_DbServicesPs_0263 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDbWarnSetup
,
06082023-C2BA-4425-90FD-2F76B74CCBE7
);


extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0263_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DbServicesPs_0263_v0_0_s_ifspec;

#ifndef __IDbWarnSetup_INTERFACE_DEFINED__
#define __IDbWarnSetup_INTERFACE_DEFINED__

/* interface IDbWarnSetup */
/* [unique][object][uuid] */


#define IID_IDbWarnSetup __uuidof(IDbWarnSetup)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("06082023-C2BA-4425-90FD-2F76B74CCBE7")
	IDbWarnSetup : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE PermitRemoteWarnings( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE RefuseRemoteWarnings( void) = 0;

	};

#else 	/* C style interface */

	typedef struct IDbWarnSetupVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IDbWarnSetup * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IDbWarnSetup * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IDbWarnSetup * This);

		HRESULT ( STDMETHODCALLTYPE *PermitRemoteWarnings )(
			IDbWarnSetup * This);

		HRESULT ( STDMETHODCALLTYPE *RefuseRemoteWarnings )(
			IDbWarnSetup * This);

		END_INTERFACE
	} IDbWarnSetupVtbl;

	interface IDbWarnSetup
	{
		CONST_VTBL struct IDbWarnSetupVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IDbWarnSetup_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDbWarnSetup_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IDbWarnSetup_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IDbWarnSetup_PermitRemoteWarnings(This)	\
	(This)->lpVtbl -> PermitRemoteWarnings(This)

#define IDbWarnSetup_RefuseRemoteWarnings(This)	\
	(This)->lpVtbl -> RefuseRemoteWarnings(This)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDbWarnSetup_PermitRemoteWarnings_Proxy(
	IDbWarnSetup * This);


void __RPC_STUB IDbWarnSetup_PermitRemoteWarnings_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbWarnSetup_RefuseRemoteWarnings_Proxy(
	IDbWarnSetup * This);


void __RPC_STUB IDbWarnSetup_RefuseRemoteWarnings_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IDbWarnSetup_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * );
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * );
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * );
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * );

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
