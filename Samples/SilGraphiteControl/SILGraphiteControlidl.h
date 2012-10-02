
#pragma warning( disable: 4049 )  /* more than 64k source lines */

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0347 */
/* at Fri Jun 27 14:46:43 2003
 */
/* Compiler settings for SILGraphiteControl.idl:
	Os, W1, Zp8, env=Win32 (32b run)
	protocol : dce , ms_ext, c_ext
	error checks: allocation ref bounds_check enum stub_data
	VC __declspec() decoration level:
		 __declspec(uuid()), __declspec(selectany), __declspec(novtable)
		 DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 440
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __SILGraphiteControlidl_h__
#define __SILGraphiteControlidl_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef ___DSILGraphiteControl_FWD_DEFINED__
#define ___DSILGraphiteControl_FWD_DEFINED__
typedef interface _DSILGraphiteControl _DSILGraphiteControl;
#endif 	/* ___DSILGraphiteControl_FWD_DEFINED__ */


#ifndef ___DSILGraphiteControlEvents_FWD_DEFINED__
#define ___DSILGraphiteControlEvents_FWD_DEFINED__
typedef interface _DSILGraphiteControlEvents _DSILGraphiteControlEvents;
#endif 	/* ___DSILGraphiteControlEvents_FWD_DEFINED__ */


#ifndef __SILGraphiteControl_FWD_DEFINED__
#define __SILGraphiteControl_FWD_DEFINED__

#ifdef __cplusplus
typedef class SILGraphiteControl SILGraphiteControl;
#else
typedef struct SILGraphiteControl SILGraphiteControl;
#endif /* __cplusplus */

#endif 	/* __SILGraphiteControl_FWD_DEFINED__ */


#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );


#ifndef __SILGraphiteControlLib_LIBRARY_DEFINED__
#define __SILGraphiteControlLib_LIBRARY_DEFINED__

/* library SILGraphiteControlLib */
/* [control][helpstring][helpfile][version][uuid] */


EXTERN_C const IID LIBID_SILGraphiteControlLib;

#ifndef ___DSILGraphiteControl_DISPINTERFACE_DEFINED__
#define ___DSILGraphiteControl_DISPINTERFACE_DEFINED__

/* dispinterface _DSILGraphiteControl */
/* [helpstring][uuid] */


EXTERN_C const IID DIID__DSILGraphiteControl;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("CFFFE4B0-0E6E-4D86-82C5-10FBD2CA6F16")
	_DSILGraphiteControl : public IDispatch
	{
	};

#else 	/* C style interface */

	typedef struct _DSILGraphiteControlVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			_DSILGraphiteControl * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			_DSILGraphiteControl * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			_DSILGraphiteControl * This);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )(
			_DSILGraphiteControl * This,
			/* [out] */ UINT *pctinfo);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )(
			_DSILGraphiteControl * This,
			/* [in] */ UINT iTInfo,
			/* [in] */ LCID lcid,
			/* [out] */ ITypeInfo **ppTInfo);

		HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )(
			_DSILGraphiteControl * This,
			/* [in] */ REFIID riid,
			/* [size_is][in] */ LPOLESTR *rgszNames,
			/* [in] */ UINT cNames,
			/* [in] */ LCID lcid,
			/* [size_is][out] */ DISPID *rgDispId);

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )(
			_DSILGraphiteControl * This,
			/* [in] */ DISPID dispIdMember,
			/* [in] */ REFIID riid,
			/* [in] */ LCID lcid,
			/* [in] */ WORD wFlags,
			/* [out][in] */ DISPPARAMS *pDispParams,
			/* [out] */ VARIANT *pVarResult,
			/* [out] */ EXCEPINFO *pExcepInfo,
			/* [out] */ UINT *puArgErr);

		END_INTERFACE
	} _DSILGraphiteControlVtbl;

	interface _DSILGraphiteControl
	{
		CONST_VTBL struct _DSILGraphiteControlVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define _DSILGraphiteControl_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define _DSILGraphiteControl_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define _DSILGraphiteControl_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define _DSILGraphiteControl_GetTypeInfoCount(This,pctinfo)	\
	(This)->lpVtbl -> GetTypeInfoCount(This,pctinfo)

#define _DSILGraphiteControl_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
	(This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo)

#define _DSILGraphiteControl_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
	(This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)

#define _DSILGraphiteControl_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
	(This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)

#endif /* COBJMACROS */


#endif 	/* C style interface */


#endif 	/* ___DSILGraphiteControl_DISPINTERFACE_DEFINED__ */


#ifndef ___DSILGraphiteControlEvents_DISPINTERFACE_DEFINED__
#define ___DSILGraphiteControlEvents_DISPINTERFACE_DEFINED__

/* dispinterface _DSILGraphiteControlEvents */
/* [helpstring][uuid] */


EXTERN_C const IID DIID__DSILGraphiteControlEvents;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("F6C724C7-CC62-46DC-AB50-1E8802ABD0D4")
	_DSILGraphiteControlEvents : public IDispatch
	{
	};

#else 	/* C style interface */

	typedef struct _DSILGraphiteControlEventsVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			_DSILGraphiteControlEvents * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			_DSILGraphiteControlEvents * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			_DSILGraphiteControlEvents * This);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )(
			_DSILGraphiteControlEvents * This,
			/* [out] */ UINT *pctinfo);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )(
			_DSILGraphiteControlEvents * This,
			/* [in] */ UINT iTInfo,
			/* [in] */ LCID lcid,
			/* [out] */ ITypeInfo **ppTInfo);

		HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )(
			_DSILGraphiteControlEvents * This,
			/* [in] */ REFIID riid,
			/* [size_is][in] */ LPOLESTR *rgszNames,
			/* [in] */ UINT cNames,
			/* [in] */ LCID lcid,
			/* [size_is][out] */ DISPID *rgDispId);

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )(
			_DSILGraphiteControlEvents * This,
			/* [in] */ DISPID dispIdMember,
			/* [in] */ REFIID riid,
			/* [in] */ LCID lcid,
			/* [in] */ WORD wFlags,
			/* [out][in] */ DISPPARAMS *pDispParams,
			/* [out] */ VARIANT *pVarResult,
			/* [out] */ EXCEPINFO *pExcepInfo,
			/* [out] */ UINT *puArgErr);

		END_INTERFACE
	} _DSILGraphiteControlEventsVtbl;

	interface _DSILGraphiteControlEvents
	{
		CONST_VTBL struct _DSILGraphiteControlEventsVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define _DSILGraphiteControlEvents_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define _DSILGraphiteControlEvents_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define _DSILGraphiteControlEvents_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define _DSILGraphiteControlEvents_GetTypeInfoCount(This,pctinfo)	\
	(This)->lpVtbl -> GetTypeInfoCount(This,pctinfo)

#define _DSILGraphiteControlEvents_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
	(This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo)

#define _DSILGraphiteControlEvents_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
	(This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)

#define _DSILGraphiteControlEvents_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
	(This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)

#endif /* COBJMACROS */


#endif 	/* C style interface */


#endif 	/* ___DSILGraphiteControlEvents_DISPINTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_SILGraphiteControl;

#ifdef __cplusplus

class DECLSPEC_UUID("62631FFE-1185-44CC-8AD2-602A05D1D071")
SILGraphiteControl;
#endif
#endif /* __SILGraphiteControlLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
