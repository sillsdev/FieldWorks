

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:13:40 2006
 */
/* Compiler settings for C:\fw\Output\Common\CmnFwDlgsPs.idl:
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

#ifndef __CmnFwDlgsPs_h__
#define __CmnFwDlgsPs_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __IOpenFWProjectDlg_FWD_DEFINED__
#define __IOpenFWProjectDlg_FWD_DEFINED__
typedef interface IOpenFWProjectDlg IOpenFWProjectDlg;
#endif 	/* __IOpenFWProjectDlg_FWD_DEFINED__ */


#ifndef __IFwExportDlg_FWD_DEFINED__
#define __IFwExportDlg_FWD_DEFINED__
typedef interface IFwExportDlg IFwExportDlg;
#endif 	/* __IFwExportDlg_FWD_DEFINED__ */


#ifndef __IFwStylesDlg_FWD_DEFINED__
#define __IFwStylesDlg_FWD_DEFINED__
typedef interface IFwStylesDlg IFwStylesDlg;
#endif 	/* __IFwStylesDlg_FWD_DEFINED__ */


#ifndef __IFwDbMergeStyles_FWD_DEFINED__
#define __IFwDbMergeStyles_FWD_DEFINED__
typedef interface IFwDbMergeStyles IFwDbMergeStyles;
#endif 	/* __IFwDbMergeStyles_FWD_DEFINED__ */


#ifndef __IFwDbMergeWrtSys_FWD_DEFINED__
#define __IFwDbMergeWrtSys_FWD_DEFINED__
typedef interface IFwDbMergeWrtSys IFwDbMergeWrtSys;
#endif 	/* __IFwDbMergeWrtSys_FWD_DEFINED__ */


#ifndef __IFwCheckAnthroList_FWD_DEFINED__
#define __IFwCheckAnthroList_FWD_DEFINED__
typedef interface IFwCheckAnthroList IFwCheckAnthroList;
#endif 	/* __IFwCheckAnthroList_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "FwKernelPs.h"
#include "ViewsPs.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_CmnFwDlgsPs_0000 */
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
IOpenFWProjectDlg
,
8cb6f2f9-3b0a-4030-8992-c50fb78e77f3
);


extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0000_v0_0_s_ifspec;

#ifndef __IOpenFWProjectDlg_INTERFACE_DEFINED__
#define __IOpenFWProjectDlg_INTERFACE_DEFINED__

/* interface IOpenFWProjectDlg */
/* [unique][object][uuid] */


#define IID_IOpenFWProjectDlg __uuidof(IOpenFWProjectDlg)

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
			/* [in] */ ILgWritingSystemFactory *pwsf) = 0;

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
			/* [in] */ ILgWritingSystemFactory *pwsf);

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
	/* [in] */ ILgWritingSystemFactory *pwsf);


void __RPC_STUB IOpenFWProjectDlg_putref_WritingSystemFactory_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IOpenFWProjectDlg_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_CmnFwDlgsPs_0359 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwExportDlg
,
67A68372-5727-4bd4-94A7-C2D703A75C36
);


extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0359_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0359_v0_0_s_ifspec;

#ifndef __IFwExportDlg_INTERFACE_DEFINED__
#define __IFwExportDlg_INTERFACE_DEFINED__

/* interface IFwExportDlg */
/* [unique][object][uuid] */


#define IID_IFwExportDlg __uuidof(IFwExportDlg)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("67A68372-5727-4bd4-94A7-C2D703A75C36")
	IFwExportDlg : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ DWORD hwndParent,
			/* [in] */ IVwStylesheet *pvss,
			/* [in] */ IFwCustomExport *pfcex,
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
			/* [in] */ IFwCustomExport *pfcex,
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
	/* [in] */ IFwCustomExport *pfcex,
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


/* interface __MIDL_itf_CmnFwDlgsPs_0360 */
/* [local] */

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


extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0360_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0360_v0_0_s_ifspec;

#ifndef __IFwStylesDlg_INTERFACE_DEFINED__
#define __IFwStylesDlg_INTERFACE_DEFINED__

/* interface IFwStylesDlg */
/* [unique][object][uuid] */


#define IID_IFwStylesDlg __uuidof(IFwStylesDlg)

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
			/* [in] */ ILgWritingSystemFactory *pwsf) = 0;

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
			/* [size_is][in] */ ITsTextProps **rgpttpPara,
			/* [in] */ int cttpPara,
			/* [size_is][in] */ ITsTextProps **rgpttpChar,
			/* [in] */ int cttpChar) = 0;

		virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_RootObjectId(
			/* [in] */ int hvoRootObj) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetWritingSystemsOfInterest(
			/* [size_is][in] */ int *rgws,
			/* [in] */ int cws) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_LogFile(
			/* [in] */ IStream *pstrmLog) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_HelpTopicProvider(
			/* [in] */ IHelpTopicProvider *phtprov) = 0;

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
			/* [in] */ ILgWritingSystemFactory *pwsf);

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
			/* [size_is][in] */ ITsTextProps **rgpttpPara,
			/* [in] */ int cttpPara,
			/* [size_is][in] */ ITsTextProps **rgpttpChar,
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
			/* [in] */ IHelpTopicProvider *phtprov);

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
	/* [in] */ ILgWritingSystemFactory *pwsf);


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
	/* [size_is][in] */ ITsTextProps **rgpttpPara,
	/* [in] */ int cttpPara,
	/* [size_is][in] */ ITsTextProps **rgpttpChar,
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
	/* [in] */ IHelpTopicProvider *phtprov);


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


/* interface __MIDL_itf_CmnFwDlgsPs_0361 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwDbMergeStyles
,
A7CD703C-6199-4097-A5C0-AB78DD23120E
);


extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0361_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0361_v0_0_s_ifspec;

#ifndef __IFwDbMergeStyles_INTERFACE_DEFINED__
#define __IFwDbMergeStyles_INTERFACE_DEFINED__

/* interface IFwDbMergeStyles */
/* [unique][object][uuid] */


#define IID_IFwDbMergeStyles __uuidof(IFwDbMergeStyles)

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


/* interface __MIDL_itf_CmnFwDlgsPs_0362 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwDbMergeWrtSys
,
DE96B989-91A5-4104-9764-69ABE0BF0B9A
);


extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0362_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0362_v0_0_s_ifspec;

#ifndef __IFwDbMergeWrtSys_INTERFACE_DEFINED__
#define __IFwDbMergeWrtSys_INTERFACE_DEFINED__

/* interface IFwDbMergeWrtSys */
/* [unique][object][uuid] */


#define IID_IFwDbMergeWrtSys __uuidof(IFwDbMergeWrtSys)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("DE96B989-91A5-4104-9764-69ABE0BF0B9A")
	IFwDbMergeWrtSys : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Initialize(
			/* [in] */ IFwTool *pfwt,
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
			/* [in] */ IFwTool *pfwt,
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
	/* [in] */ IFwTool *pfwt,
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


/* interface __MIDL_itf_CmnFwDlgsPs_0363 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwCheckAnthroList
,
8AC06CED-7B73-4E34-81A3-852A43E28BD8
);


extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0363_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_CmnFwDlgsPs_0363_v0_0_s_ifspec;

#ifndef __IFwCheckAnthroList_INTERFACE_DEFINED__
#define __IFwCheckAnthroList_INTERFACE_DEFINED__

/* interface IFwCheckAnthroList */
/* [unique][object][uuid] */


#define IID_IFwCheckAnthroList __uuidof(IFwCheckAnthroList)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("8AC06CED-7B73-4E34-81A3-852A43E28BD8")
	IFwCheckAnthroList : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE CheckAnthroList(
			/* [in] */ IOleDbEncap *pode,
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
			/* [in] */ IOleDbEncap *pode,
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
	/* [in] */ IOleDbEncap *pode,
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
