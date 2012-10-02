

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon Jun 07 16:44:12 2004
 */
/* Compiler settings for wrtXML.idl:
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

#ifndef __wrtXML_h__
#define __wrtXML_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __IWriteXML_FWD_DEFINED__
#define __IWriteXML_FWD_DEFINED__
typedef interface IWriteXML IWriteXML;
#endif 	/* __IWriteXML_FWD_DEFINED__ */


#ifndef __WriteXML_FWD_DEFINED__
#define __WriteXML_FWD_DEFINED__

#ifdef __cplusplus
typedef class WriteXML WriteXML;
#else
typedef struct WriteXML WriteXML;
#endif /* __cplusplus */

#endif 	/* __WriteXML_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

#ifndef __IWriteXML_INTERFACE_DEFINED__
#define __IWriteXML_INTERFACE_DEFINED__

/* interface IWriteXML */
/* [unique][helpstring][dual][uuid][object] */


EXTERN_C const IID IID_IWriteXML;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("F4BC024E-B050-11D2-9273-00400541F9E9")
	IWriteXML : public IDispatch
	{
	public:
		virtual /* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE get_FileName(
			/* [retval][out] */ BSTR *pbstrOut) = 0;

		virtual /* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE get_Encoding(
			/* [retval][out] */ BSTR *pbstrOut) = 0;

		virtual /* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE get_IndentLevel(
			/* [retval][out] */ long *plnOut) = 0;

		virtual /* [helpstring][id][propput] */ HRESULT STDMETHODCALLTYPE put_IndentLevel(
			/* [in] */ long lnIndentLevel) = 0;

		virtual /* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE get_UseSpaces(
			/* [retval][out] */ BOOL *pfOut) = 0;

		virtual /* [helpstring][id][propput] */ HRESULT STDMETHODCALLTYPE put_UseSpaces(
			/* [in] */ BOOL fUseSpaces) = 0;

		virtual /* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE get_TabSpaces(
			/* [retval][out] */ long *plnOut) = 0;

		virtual /* [helpstring][id][propput] */ HRESULT STDMETHODCALLTYPE put_TabSpaces(
			/* [in] */ long lnTabSpaces) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE Comment(
			/* [in] */ BSTR Comment,
			/* [optional][in] */ VARIANT NewlineFirst) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE Prolog(
			/* [in] */ BSTR Version,
			/* [in] */ BOOL Standalone) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE Tag(
			/* [in] */ BSTR TagName,
			/* [optional][in] */ VARIANT NewlineFirst) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE WriteAttrib(
			/* [in] */ BSTR AttribName,
			/* [in] */ BSTR AttribValue,
			/* [optional][in] */ VARIANT NewlineFirst) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE String(
			/* [in] */ BSTR String,
			/* [optional][in] */ VARIANT NewlineFirst) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE RawString(
			/* [in] */ BSTR String,
			/* [optional][in] */ VARIANT NewlineFirst) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE EndTag(
			/* [optional][in] */ VARIANT NewlineFirst) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE CloseAttribList( void) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE CloseOutputFile( void) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE AddAttribute(
			/* [in] */ BSTR AttribName,
			/* [in] */ BSTR AttribValue) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE ModifyAttribute(
			/* [in] */ BSTR AttribName,
			/* [in] */ BSTR NewAttribValue) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE RemoveAttribute(
			/* [in] */ BSTR AttribName) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE ClearAttributes( void) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE GetAttribute(
			/* [in] */ BSTR AttribName,
			/* [out] */ BSTR *AttribValuel) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE SingleLineEntity(
			/* [in] */ BSTR TagName,
			/* [in] */ BSTR Data,
			/* [optional][in] */ VARIANT NewlineFirst) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE MultiLineEntity(
			/* [in] */ BSTR TagName,
			/* [in] */ BSTR Data) = 0;

		virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE OpenOutputFile(
			/* [in] */ BSTR Filename,
			/* [in] */ BSTR Encoding) = 0;

	};

#else 	/* C style interface */

	typedef struct IWriteXMLVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IWriteXML * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IWriteXML * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IWriteXML * This);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )(
			IWriteXML * This,
			/* [out] */ UINT *pctinfo);

		HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )(
			IWriteXML * This,
			/* [in] */ UINT iTInfo,
			/* [in] */ LCID lcid,
			/* [out] */ ITypeInfo **ppTInfo);

		HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )(
			IWriteXML * This,
			/* [in] */ REFIID riid,
			/* [size_is][in] */ LPOLESTR *rgszNames,
			/* [in] */ UINT cNames,
			/* [in] */ LCID lcid,
			/* [size_is][out] */ DISPID *rgDispId);

		/* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )(
			IWriteXML * This,
			/* [in] */ DISPID dispIdMember,
			/* [in] */ REFIID riid,
			/* [in] */ LCID lcid,
			/* [in] */ WORD wFlags,
			/* [out][in] */ DISPPARAMS *pDispParams,
			/* [out] */ VARIANT *pVarResult,
			/* [out] */ EXCEPINFO *pExcepInfo,
			/* [out] */ UINT *puArgErr);

		/* [helpstring][id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_FileName )(
			IWriteXML * This,
			/* [retval][out] */ BSTR *pbstrOut);

		/* [helpstring][id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_Encoding )(
			IWriteXML * This,
			/* [retval][out] */ BSTR *pbstrOut);

		/* [helpstring][id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_IndentLevel )(
			IWriteXML * This,
			/* [retval][out] */ long *plnOut);

		/* [helpstring][id][propput] */ HRESULT ( STDMETHODCALLTYPE *put_IndentLevel )(
			IWriteXML * This,
			/* [in] */ long lnIndentLevel);

		/* [helpstring][id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_UseSpaces )(
			IWriteXML * This,
			/* [retval][out] */ BOOL *pfOut);

		/* [helpstring][id][propput] */ HRESULT ( STDMETHODCALLTYPE *put_UseSpaces )(
			IWriteXML * This,
			/* [in] */ BOOL fUseSpaces);

		/* [helpstring][id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_TabSpaces )(
			IWriteXML * This,
			/* [retval][out] */ long *plnOut);

		/* [helpstring][id][propput] */ HRESULT ( STDMETHODCALLTYPE *put_TabSpaces )(
			IWriteXML * This,
			/* [in] */ long lnTabSpaces);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *Comment )(
			IWriteXML * This,
			/* [in] */ BSTR Comment,
			/* [optional][in] */ VARIANT NewlineFirst);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *Prolog )(
			IWriteXML * This,
			/* [in] */ BSTR Version,
			/* [in] */ BOOL Standalone);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *Tag )(
			IWriteXML * This,
			/* [in] */ BSTR TagName,
			/* [optional][in] */ VARIANT NewlineFirst);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *WriteAttrib )(
			IWriteXML * This,
			/* [in] */ BSTR AttribName,
			/* [in] */ BSTR AttribValue,
			/* [optional][in] */ VARIANT NewlineFirst);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *String )(
			IWriteXML * This,
			/* [in] */ BSTR String,
			/* [optional][in] */ VARIANT NewlineFirst);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *RawString )(
			IWriteXML * This,
			/* [in] */ BSTR String,
			/* [optional][in] */ VARIANT NewlineFirst);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *EndTag )(
			IWriteXML * This,
			/* [optional][in] */ VARIANT NewlineFirst);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *CloseAttribList )(
			IWriteXML * This);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *CloseOutputFile )(
			IWriteXML * This);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *AddAttribute )(
			IWriteXML * This,
			/* [in] */ BSTR AttribName,
			/* [in] */ BSTR AttribValue);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *ModifyAttribute )(
			IWriteXML * This,
			/* [in] */ BSTR AttribName,
			/* [in] */ BSTR NewAttribValue);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *RemoveAttribute )(
			IWriteXML * This,
			/* [in] */ BSTR AttribName);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *ClearAttributes )(
			IWriteXML * This);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *GetAttribute )(
			IWriteXML * This,
			/* [in] */ BSTR AttribName,
			/* [out] */ BSTR *AttribValuel);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *SingleLineEntity )(
			IWriteXML * This,
			/* [in] */ BSTR TagName,
			/* [in] */ BSTR Data,
			/* [optional][in] */ VARIANT NewlineFirst);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *MultiLineEntity )(
			IWriteXML * This,
			/* [in] */ BSTR TagName,
			/* [in] */ BSTR Data);

		/* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *OpenOutputFile )(
			IWriteXML * This,
			/* [in] */ BSTR Filename,
			/* [in] */ BSTR Encoding);

		END_INTERFACE
	} IWriteXMLVtbl;

	interface IWriteXML
	{
		CONST_VTBL struct IWriteXMLVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IWriteXML_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IWriteXML_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IWriteXML_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IWriteXML_GetTypeInfoCount(This,pctinfo)	\
	(This)->lpVtbl -> GetTypeInfoCount(This,pctinfo)

#define IWriteXML_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
	(This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo)

#define IWriteXML_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
	(This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)

#define IWriteXML_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
	(This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)


#define IWriteXML_get_FileName(This,pbstrOut)	\
	(This)->lpVtbl -> get_FileName(This,pbstrOut)

#define IWriteXML_get_Encoding(This,pbstrOut)	\
	(This)->lpVtbl -> get_Encoding(This,pbstrOut)

#define IWriteXML_get_IndentLevel(This,plnOut)	\
	(This)->lpVtbl -> get_IndentLevel(This,plnOut)

#define IWriteXML_put_IndentLevel(This,lnIndentLevel)	\
	(This)->lpVtbl -> put_IndentLevel(This,lnIndentLevel)

#define IWriteXML_get_UseSpaces(This,pfOut)	\
	(This)->lpVtbl -> get_UseSpaces(This,pfOut)

#define IWriteXML_put_UseSpaces(This,fUseSpaces)	\
	(This)->lpVtbl -> put_UseSpaces(This,fUseSpaces)

#define IWriteXML_get_TabSpaces(This,plnOut)	\
	(This)->lpVtbl -> get_TabSpaces(This,plnOut)

#define IWriteXML_put_TabSpaces(This,lnTabSpaces)	\
	(This)->lpVtbl -> put_TabSpaces(This,lnTabSpaces)

#define IWriteXML_Comment(This,Comment,NewlineFirst)	\
	(This)->lpVtbl -> Comment(This,Comment,NewlineFirst)

#define IWriteXML_Prolog(This,Version,Standalone)	\
	(This)->lpVtbl -> Prolog(This,Version,Standalone)

#define IWriteXML_Tag(This,TagName,NewlineFirst)	\
	(This)->lpVtbl -> Tag(This,TagName,NewlineFirst)

#define IWriteXML_WriteAttrib(This,AttribName,AttribValue,NewlineFirst)	\
	(This)->lpVtbl -> WriteAttrib(This,AttribName,AttribValue,NewlineFirst)

#define IWriteXML_String(This,String,NewlineFirst)	\
	(This)->lpVtbl -> String(This,String,NewlineFirst)

#define IWriteXML_RawString(This,String,NewlineFirst)	\
	(This)->lpVtbl -> RawString(This,String,NewlineFirst)

#define IWriteXML_EndTag(This,NewlineFirst)	\
	(This)->lpVtbl -> EndTag(This,NewlineFirst)

#define IWriteXML_CloseAttribList(This)	\
	(This)->lpVtbl -> CloseAttribList(This)

#define IWriteXML_CloseOutputFile(This)	\
	(This)->lpVtbl -> CloseOutputFile(This)

#define IWriteXML_AddAttribute(This,AttribName,AttribValue)	\
	(This)->lpVtbl -> AddAttribute(This,AttribName,AttribValue)

#define IWriteXML_ModifyAttribute(This,AttribName,NewAttribValue)	\
	(This)->lpVtbl -> ModifyAttribute(This,AttribName,NewAttribValue)

#define IWriteXML_RemoveAttribute(This,AttribName)	\
	(This)->lpVtbl -> RemoveAttribute(This,AttribName)

#define IWriteXML_ClearAttributes(This)	\
	(This)->lpVtbl -> ClearAttributes(This)

#define IWriteXML_GetAttribute(This,AttribName,AttribValuel)	\
	(This)->lpVtbl -> GetAttribute(This,AttribName,AttribValuel)

#define IWriteXML_SingleLineEntity(This,TagName,Data,NewlineFirst)	\
	(This)->lpVtbl -> SingleLineEntity(This,TagName,Data,NewlineFirst)

#define IWriteXML_MultiLineEntity(This,TagName,Data)	\
	(This)->lpVtbl -> MultiLineEntity(This,TagName,Data)

#define IWriteXML_OpenOutputFile(This,Filename,Encoding)	\
	(This)->lpVtbl -> OpenOutputFile(This,Filename,Encoding)

#endif /* COBJMACROS */


#endif 	/* C style interface */



/* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE IWriteXML_get_FileName_Proxy(
	IWriteXML * This,
	/* [retval][out] */ BSTR *pbstrOut);


void __RPC_STUB IWriteXML_get_FileName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE IWriteXML_get_Encoding_Proxy(
	IWriteXML * This,
	/* [retval][out] */ BSTR *pbstrOut);


void __RPC_STUB IWriteXML_get_Encoding_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE IWriteXML_get_IndentLevel_Proxy(
	IWriteXML * This,
	/* [retval][out] */ long *plnOut);


void __RPC_STUB IWriteXML_get_IndentLevel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id][propput] */ HRESULT STDMETHODCALLTYPE IWriteXML_put_IndentLevel_Proxy(
	IWriteXML * This,
	/* [in] */ long lnIndentLevel);


void __RPC_STUB IWriteXML_put_IndentLevel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE IWriteXML_get_UseSpaces_Proxy(
	IWriteXML * This,
	/* [retval][out] */ BOOL *pfOut);


void __RPC_STUB IWriteXML_get_UseSpaces_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id][propput] */ HRESULT STDMETHODCALLTYPE IWriteXML_put_UseSpaces_Proxy(
	IWriteXML * This,
	/* [in] */ BOOL fUseSpaces);


void __RPC_STUB IWriteXML_put_UseSpaces_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id][propget] */ HRESULT STDMETHODCALLTYPE IWriteXML_get_TabSpaces_Proxy(
	IWriteXML * This,
	/* [retval][out] */ long *plnOut);


void __RPC_STUB IWriteXML_get_TabSpaces_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id][propput] */ HRESULT STDMETHODCALLTYPE IWriteXML_put_TabSpaces_Proxy(
	IWriteXML * This,
	/* [in] */ long lnTabSpaces);


void __RPC_STUB IWriteXML_put_TabSpaces_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_Comment_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR Comment,
	/* [optional][in] */ VARIANT NewlineFirst);


void __RPC_STUB IWriteXML_Comment_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_Prolog_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR Version,
	/* [in] */ BOOL Standalone);


void __RPC_STUB IWriteXML_Prolog_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_Tag_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR TagName,
	/* [optional][in] */ VARIANT NewlineFirst);


void __RPC_STUB IWriteXML_Tag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_WriteAttrib_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR AttribName,
	/* [in] */ BSTR AttribValue,
	/* [optional][in] */ VARIANT NewlineFirst);


void __RPC_STUB IWriteXML_WriteAttrib_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_String_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR String,
	/* [optional][in] */ VARIANT NewlineFirst);


void __RPC_STUB IWriteXML_String_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_RawString_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR String,
	/* [optional][in] */ VARIANT NewlineFirst);


void __RPC_STUB IWriteXML_RawString_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_EndTag_Proxy(
	IWriteXML * This,
	/* [optional][in] */ VARIANT NewlineFirst);


void __RPC_STUB IWriteXML_EndTag_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_CloseAttribList_Proxy(
	IWriteXML * This);


void __RPC_STUB IWriteXML_CloseAttribList_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_CloseOutputFile_Proxy(
	IWriteXML * This);


void __RPC_STUB IWriteXML_CloseOutputFile_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_AddAttribute_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR AttribName,
	/* [in] */ BSTR AttribValue);


void __RPC_STUB IWriteXML_AddAttribute_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_ModifyAttribute_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR AttribName,
	/* [in] */ BSTR NewAttribValue);


void __RPC_STUB IWriteXML_ModifyAttribute_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_RemoveAttribute_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR AttribName);


void __RPC_STUB IWriteXML_RemoveAttribute_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_ClearAttributes_Proxy(
	IWriteXML * This);


void __RPC_STUB IWriteXML_ClearAttributes_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_GetAttribute_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR AttribName,
	/* [out] */ BSTR *AttribValuel);


void __RPC_STUB IWriteXML_GetAttribute_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_SingleLineEntity_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR TagName,
	/* [in] */ BSTR Data,
	/* [optional][in] */ VARIANT NewlineFirst);


void __RPC_STUB IWriteXML_SingleLineEntity_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_MultiLineEntity_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR TagName,
	/* [in] */ BSTR Data);


void __RPC_STUB IWriteXML_MultiLineEntity_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [helpstring][id] */ HRESULT STDMETHODCALLTYPE IWriteXML_OpenOutputFile_Proxy(
	IWriteXML * This,
	/* [in] */ BSTR Filename,
	/* [in] */ BSTR Encoding);


void __RPC_STUB IWriteXML_OpenOutputFile_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IWriteXML_INTERFACE_DEFINED__ */



#ifndef __WRTXMLLib_LIBRARY_DEFINED__
#define __WRTXMLLib_LIBRARY_DEFINED__

/* library WRTXMLLib */
/* [helpstring][version][uuid] */


EXTERN_C const IID LIBID_WRTXMLLib;

EXTERN_C const CLSID CLSID_WriteXML;

#ifdef __cplusplus

class DECLSPEC_UUID("F4BC024F-B050-11D2-9273-00400541F9E9")
WriteXML;
#endif
#endif /* __WRTXMLLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * );
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * );
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * );
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * );

unsigned long             __RPC_USER  VARIANT_UserSize(     unsigned long *, unsigned long            , VARIANT * );
unsigned char * __RPC_USER  VARIANT_UserMarshal(  unsigned long *, unsigned char *, VARIANT * );
unsigned char * __RPC_USER  VARIANT_UserUnmarshal(unsigned long *, unsigned char *, VARIANT * );
void                      __RPC_USER  VARIANT_UserFree(     unsigned long *, VARIANT * );

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
