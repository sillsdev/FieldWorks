

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:01:36 2006
 */
/* Compiler settings for C:\fw\Output\Common\DbAccessTlb.idl:
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


#ifndef __DbAccessTlb_h__
#define __DbAccessTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __IOleDbCommand_FWD_DEFINED__
#define __IOleDbCommand_FWD_DEFINED__
typedef interface IOleDbCommand IOleDbCommand;
#endif 	/* __IOleDbCommand_FWD_DEFINED__ */


#ifndef __IOleDbEncap_FWD_DEFINED__
#define __IOleDbEncap_FWD_DEFINED__
typedef interface IOleDbEncap IOleDbEncap;
#endif 	/* __IOleDbEncap_FWD_DEFINED__ */


#ifndef __IFwMetaDataCache_FWD_DEFINED__
#define __IFwMetaDataCache_FWD_DEFINED__
typedef interface IFwMetaDataCache IFwMetaDataCache;
#endif 	/* __IFwMetaDataCache_FWD_DEFINED__ */


#ifndef __IDbAdmin_FWD_DEFINED__
#define __IDbAdmin_FWD_DEFINED__
typedef interface IDbAdmin IDbAdmin;
#endif 	/* __IDbAdmin_FWD_DEFINED__ */


#ifndef __OleDbEncap_FWD_DEFINED__
#define __OleDbEncap_FWD_DEFINED__

#ifdef __cplusplus
typedef class OleDbEncap OleDbEncap;
#else
typedef struct OleDbEncap OleDbEncap;
#endif /* __cplusplus */

#endif 	/* __OleDbEncap_FWD_DEFINED__ */


#ifndef __FwMetaDataCache_FWD_DEFINED__
#define __FwMetaDataCache_FWD_DEFINED__

#ifdef __cplusplus
typedef class FwMetaDataCache FwMetaDataCache;
#else
typedef struct FwMetaDataCache FwMetaDataCache;
#endif /* __cplusplus */

#endif 	/* __FwMetaDataCache_FWD_DEFINED__ */


#ifndef __DbAdmin_FWD_DEFINED__
#define __DbAdmin_FWD_DEFINED__

#ifdef __cplusplus
typedef class DbAdmin DbAdmin;
#else
typedef struct DbAdmin DbAdmin;
#endif /* __cplusplus */

#endif 	/* __DbAdmin_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_DbAccessTlb_0000 */
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
AAB4A4A1-3C83-11d4-A1BB-00C04F0C9593
,
DbAccess
);


extern RPC_IF_HANDLE __MIDL_itf_DbAccessTlb_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DbAccessTlb_0000_v0_0_s_ifspec;


#ifndef __DbAccess_LIBRARY_DEFINED__
#define __DbAccess_LIBRARY_DEFINED__

/* library DbAccess */
/* [helpstring][version][uuid] */

typedef /* [v1_enum] */
enum SqlStmtType
	{	knSqlStmtNoResults	= 0,
	knSqlStmtSelectWithOneRowset	= 1,
	knSqlStmtStoredProcedure	= 2
	} 	SqlStmtType;

typedef /* [v1_enum] */
enum OdeLockTimeoutMode
	{	koltNone	= 0,
	koltMsgBox	= koltNone + 1,
	koltReturnError	= koltMsgBox + 1
	} 	OdeLockTimeoutMode;

typedef /* [v1_enum] */
enum OdeLockTimeoutValue
	{	koltvForever	= -1,
	koltvNoWait	= 0,
	koltvFwDefault	= 1000
	} 	OdeLockTimeoutValue;




GENERIC_DECLARE_SMART_INTERFACE_PTR(
IOleDbCommand
,
21993161-3E24-11d4-A1BD-00C04F0C9593
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IOleDbEncap
,
CB7BEA0F-960A-4b23-80D3-DE06C0530E04
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwMetaDataCache
,
6AA9042E-0A4D-4f33-881B-3FBE48861D14
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDbAdmin
,
2A861F95-63D0-480d-B5AF-4FAF0D22125D
);
ATTACH_GUID_TO_CLASS(class,
AAB4A4A3-3C83-11d4-A1BB-00C04F0C9593
,
OleDbEncap
);
ATTACH_GUID_TO_CLASS(class,
3A1B1AC6-24C5-4ffe-85D5-675DB4B9FCBB
,
FwMetaDataCache
);
ATTACH_GUID_TO_CLASS(class,
D584A725-8CF4-4699-941F-D1337AC7DB5C
,
DbAdmin
);

EXTERN_C const IID LIBID_DbAccess;

#ifndef __IOleDbCommand_INTERFACE_DEFINED__
#define __IOleDbCommand_INTERFACE_DEFINED__

/* interface IOleDbCommand */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IOleDbCommand;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("21993161-3E24-11d4-A1BD-00C04F0C9593")
	IOleDbCommand : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE ColValWasNull(
			/* [out] */ int *pfIsNull) = 0;

		virtual HRESULT STDMETHODCALLTYPE ExecCommand(
			/* [in] */ BSTR bstrSqlStatement,
			/* [in] */ int nStatementType) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetColValue(
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cbBufferLength,
			/* [out] */ ULONG *pcbAmtBuffUsed,
			/* [out] */ ComBool *pfIsNull,
			/* [in] */ int cbPad) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetInt(
			/* [in] */ int iColIndex,
			/* [out] */ int *pnValue) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetParameter(
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength,
			/* [out] */ ComBool *pfIsNull) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetRowset(
			/* [in] */ int nRowsBuffered) = 0;

		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ IUnknown *punkSession,
			/* [in] */ IStream *pfistLog) = 0;

		virtual HRESULT STDMETHODCALLTYPE NextRow(
			/* [out] */ ComBool *pfMoreRows) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetParameter(
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [in] */ WORD nDataType,
			/* [size_is][in] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetByteBuffParameter(
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ BYTE *prgbDataBuffer,
			/* [in] */ ULONG cluBufferLength) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetStringParameter(
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ OLECHAR *prgchDataBuffer,
			/* [in] */ ULONG cluBufferLength) = 0;

	};

#else 	/* C style interface */

	typedef struct IOleDbCommandVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IOleDbCommand * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IOleDbCommand * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IOleDbCommand * This);

		HRESULT ( STDMETHODCALLTYPE *ColValWasNull )(
			IOleDbCommand * This,
			/* [out] */ int *pfIsNull);

		HRESULT ( STDMETHODCALLTYPE *ExecCommand )(
			IOleDbCommand * This,
			/* [in] */ BSTR bstrSqlStatement,
			/* [in] */ int nStatementType);

		HRESULT ( STDMETHODCALLTYPE *GetColValue )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cbBufferLength,
			/* [out] */ ULONG *pcbAmtBuffUsed,
			/* [out] */ ComBool *pfIsNull,
			/* [in] */ int cbPad);

		HRESULT ( STDMETHODCALLTYPE *GetInt )(
			IOleDbCommand * This,
			/* [in] */ int iColIndex,
			/* [out] */ int *pnValue);

		HRESULT ( STDMETHODCALLTYPE *GetParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluColIndex,
			/* [size_is][out] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength,
			/* [out] */ ComBool *pfIsNull);

		HRESULT ( STDMETHODCALLTYPE *GetRowset )(
			IOleDbCommand * This,
			/* [in] */ int nRowsBuffered);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IOleDbCommand * This,
			/* [in] */ IUnknown *punkSession,
			/* [in] */ IStream *pfistLog);

		HRESULT ( STDMETHODCALLTYPE *NextRow )(
			IOleDbCommand * This,
			/* [out] */ ComBool *pfMoreRows);

		HRESULT ( STDMETHODCALLTYPE *SetParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [in] */ WORD nDataType,
			/* [size_is][in] */ ULONG *prgluDataBuffer,
			/* [in] */ ULONG cluBufferLength);

		HRESULT ( STDMETHODCALLTYPE *SetByteBuffParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ BYTE *prgbDataBuffer,
			/* [in] */ ULONG cluBufferLength);

		HRESULT ( STDMETHODCALLTYPE *SetStringParameter )(
			IOleDbCommand * This,
			/* [in] */ ULONG iluParamIndex,
			/* [in] */ DWORD dwFlags,
			/* [in] */ BSTR bstrParamName,
			/* [size_is][in] */ OLECHAR *prgchDataBuffer,
			/* [in] */ ULONG cluBufferLength);

		END_INTERFACE
	} IOleDbCommandVtbl;

	interface IOleDbCommand
	{
		CONST_VTBL struct IOleDbCommandVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IOleDbCommand_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IOleDbCommand_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IOleDbCommand_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IOleDbCommand_ColValWasNull(This,pfIsNull)	\
	(This)->lpVtbl -> ColValWasNull(This,pfIsNull)

#define IOleDbCommand_ExecCommand(This,bstrSqlStatement,nStatementType)	\
	(This)->lpVtbl -> ExecCommand(This,bstrSqlStatement,nStatementType)

#define IOleDbCommand_GetColValue(This,iluColIndex,prgluDataBuffer,cbBufferLength,pcbAmtBuffUsed,pfIsNull,cbPad)	\
	(This)->lpVtbl -> GetColValue(This,iluColIndex,prgluDataBuffer,cbBufferLength,pcbAmtBuffUsed,pfIsNull,cbPad)

#define IOleDbCommand_GetInt(This,iColIndex,pnValue)	\
	(This)->lpVtbl -> GetInt(This,iColIndex,pnValue)

#define IOleDbCommand_GetParameter(This,iluColIndex,prgluDataBuffer,cluBufferLength,pfIsNull)	\
	(This)->lpVtbl -> GetParameter(This,iluColIndex,prgluDataBuffer,cluBufferLength,pfIsNull)

#define IOleDbCommand_GetRowset(This,nRowsBuffered)	\
	(This)->lpVtbl -> GetRowset(This,nRowsBuffered)

#define IOleDbCommand_Init(This,punkSession,pfistLog)	\
	(This)->lpVtbl -> Init(This,punkSession,pfistLog)

#define IOleDbCommand_NextRow(This,pfMoreRows)	\
	(This)->lpVtbl -> NextRow(This,pfMoreRows)

#define IOleDbCommand_SetParameter(This,iluParamIndex,dwFlags,bstrParamName,nDataType,prgluDataBuffer,cluBufferLength)	\
	(This)->lpVtbl -> SetParameter(This,iluParamIndex,dwFlags,bstrParamName,nDataType,prgluDataBuffer,cluBufferLength)

#define IOleDbCommand_SetByteBuffParameter(This,iluParamIndex,dwFlags,bstrParamName,prgbDataBuffer,cluBufferLength)	\
	(This)->lpVtbl -> SetByteBuffParameter(This,iluParamIndex,dwFlags,bstrParamName,prgbDataBuffer,cluBufferLength)

#define IOleDbCommand_SetStringParameter(This,iluParamIndex,dwFlags,bstrParamName,prgchDataBuffer,cluBufferLength)	\
	(This)->lpVtbl -> SetStringParameter(This,iluParamIndex,dwFlags,bstrParamName,prgchDataBuffer,cluBufferLength)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IOleDbCommand_ColValWasNull_Proxy(
	IOleDbCommand * This,
	/* [out] */ int *pfIsNull);


void __RPC_STUB IOleDbCommand_ColValWasNull_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_ExecCommand_Proxy(
	IOleDbCommand * This,
	/* [in] */ BSTR bstrSqlStatement,
	/* [in] */ int nStatementType);


void __RPC_STUB IOleDbCommand_ExecCommand_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetColValue_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluColIndex,
	/* [size_is][out] */ ULONG *prgluDataBuffer,
	/* [in] */ ULONG cbBufferLength,
	/* [out] */ ULONG *pcbAmtBuffUsed,
	/* [out] */ ComBool *pfIsNull,
	/* [in] */ int cbPad);


void __RPC_STUB IOleDbCommand_GetColValue_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetInt_Proxy(
	IOleDbCommand * This,
	/* [in] */ int iColIndex,
	/* [out] */ int *pnValue);


void __RPC_STUB IOleDbCommand_GetInt_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluColIndex,
	/* [size_is][out] */ ULONG *prgluDataBuffer,
	/* [in] */ ULONG cluBufferLength,
	/* [out] */ ComBool *pfIsNull);


void __RPC_STUB IOleDbCommand_GetParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_GetRowset_Proxy(
	IOleDbCommand * This,
	/* [in] */ int nRowsBuffered);


void __RPC_STUB IOleDbCommand_GetRowset_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_Init_Proxy(
	IOleDbCommand * This,
	/* [in] */ IUnknown *punkSession,
	/* [in] */ IStream *pfistLog);


void __RPC_STUB IOleDbCommand_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_NextRow_Proxy(
	IOleDbCommand * This,
	/* [out] */ ComBool *pfMoreRows);


void __RPC_STUB IOleDbCommand_NextRow_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_SetParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluParamIndex,
	/* [in] */ DWORD dwFlags,
	/* [in] */ BSTR bstrParamName,
	/* [in] */ WORD nDataType,
	/* [size_is][in] */ ULONG *prgluDataBuffer,
	/* [in] */ ULONG cluBufferLength);


void __RPC_STUB IOleDbCommand_SetParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_SetByteBuffParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluParamIndex,
	/* [in] */ DWORD dwFlags,
	/* [in] */ BSTR bstrParamName,
	/* [size_is][in] */ BYTE *prgbDataBuffer,
	/* [in] */ ULONG cluBufferLength);


void __RPC_STUB IOleDbCommand_SetByteBuffParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbCommand_SetStringParameter_Proxy(
	IOleDbCommand * This,
	/* [in] */ ULONG iluParamIndex,
	/* [in] */ DWORD dwFlags,
	/* [in] */ BSTR bstrParamName,
	/* [size_is][in] */ OLECHAR *prgchDataBuffer,
	/* [in] */ ULONG cluBufferLength);


void __RPC_STUB IOleDbCommand_SetStringParameter_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IOleDbCommand_INTERFACE_DEFINED__ */


#ifndef __IOleDbEncap_INTERFACE_DEFINED__
#define __IOleDbEncap_INTERFACE_DEFINED__

/* interface IOleDbEncap */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IOleDbEncap;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("CB7BEA0F-960A-4b23-80D3-DE06C0530E04")
	IOleDbEncap : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE BeginTrans( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CommitTrans( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE CreateCommand(
			/* [out] */ IOleDbCommand **ppodc) = 0;

		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pfistLog,
			/* [in] */ OdeLockTimeoutMode olt,
			/* [in] */ int nmsTimeout) = 0;

		virtual HRESULT STDMETHODCALLTYPE IsTransactionOpen(
			/* [retval][out] */ ComBool *pfTransactionOpen) = 0;

		virtual HRESULT STDMETHODCALLTYPE RollbackTrans( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE RollbackSavePoint(
			/* [in] */ BSTR bstrSavePoint) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetSavePoint(
			/* [out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE SetSavePointOrBeginTrans(
			/* [out] */ BSTR *pbstr) = 0;

		virtual HRESULT STDMETHODCALLTYPE InitMSDE(
			/* [in] */ IStream *pfistLog,
			/* [in] */ ComBool fForce) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Server(
			/* [retval][out] */ BSTR *pbstrSvr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Database(
			/* [retval][out] */ BSTR *pbstrDb) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFreeLogKb(
			/* [in] */ int nReservespace,
			/* [out] */ int *pnKbFree) = 0;

	};

#else 	/* C style interface */

	typedef struct IOleDbEncapVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IOleDbEncap * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IOleDbEncap * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *BeginTrans )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *CommitTrans )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *CreateCommand )(
			IOleDbEncap * This,
			/* [out] */ IOleDbCommand **ppodc);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IOleDbEncap * This,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase,
			/* [in] */ IStream *pfistLog,
			/* [in] */ OdeLockTimeoutMode olt,
			/* [in] */ int nmsTimeout);

		HRESULT ( STDMETHODCALLTYPE *IsTransactionOpen )(
			IOleDbEncap * This,
			/* [retval][out] */ ComBool *pfTransactionOpen);

		HRESULT ( STDMETHODCALLTYPE *RollbackTrans )(
			IOleDbEncap * This);

		HRESULT ( STDMETHODCALLTYPE *RollbackSavePoint )(
			IOleDbEncap * This,
			/* [in] */ BSTR bstrSavePoint);

		HRESULT ( STDMETHODCALLTYPE *SetSavePoint )(
			IOleDbEncap * This,
			/* [out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *SetSavePointOrBeginTrans )(
			IOleDbEncap * This,
			/* [out] */ BSTR *pbstr);

		HRESULT ( STDMETHODCALLTYPE *InitMSDE )(
			IOleDbEncap * This,
			/* [in] */ IStream *pfistLog,
			/* [in] */ ComBool fForce);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Server )(
			IOleDbEncap * This,
			/* [retval][out] */ BSTR *pbstrSvr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Database )(
			IOleDbEncap * This,
			/* [retval][out] */ BSTR *pbstrDb);

		HRESULT ( STDMETHODCALLTYPE *GetFreeLogKb )(
			IOleDbEncap * This,
			/* [in] */ int nReservespace,
			/* [out] */ int *pnKbFree);

		END_INTERFACE
	} IOleDbEncapVtbl;

	interface IOleDbEncap
	{
		CONST_VTBL struct IOleDbEncapVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IOleDbEncap_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IOleDbEncap_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IOleDbEncap_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IOleDbEncap_BeginTrans(This)	\
	(This)->lpVtbl -> BeginTrans(This)

#define IOleDbEncap_CommitTrans(This)	\
	(This)->lpVtbl -> CommitTrans(This)

#define IOleDbEncap_CreateCommand(This,ppodc)	\
	(This)->lpVtbl -> CreateCommand(This,ppodc)

#define IOleDbEncap_Init(This,bstrServer,bstrDatabase,pfistLog,olt,nmsTimeout)	\
	(This)->lpVtbl -> Init(This,bstrServer,bstrDatabase,pfistLog,olt,nmsTimeout)

#define IOleDbEncap_IsTransactionOpen(This,pfTransactionOpen)	\
	(This)->lpVtbl -> IsTransactionOpen(This,pfTransactionOpen)

#define IOleDbEncap_RollbackTrans(This)	\
	(This)->lpVtbl -> RollbackTrans(This)

#define IOleDbEncap_RollbackSavePoint(This,bstrSavePoint)	\
	(This)->lpVtbl -> RollbackSavePoint(This,bstrSavePoint)

#define IOleDbEncap_SetSavePoint(This,pbstr)	\
	(This)->lpVtbl -> SetSavePoint(This,pbstr)

#define IOleDbEncap_SetSavePointOrBeginTrans(This,pbstr)	\
	(This)->lpVtbl -> SetSavePointOrBeginTrans(This,pbstr)

#define IOleDbEncap_InitMSDE(This,pfistLog,fForce)	\
	(This)->lpVtbl -> InitMSDE(This,pfistLog,fForce)

#define IOleDbEncap_get_Server(This,pbstrSvr)	\
	(This)->lpVtbl -> get_Server(This,pbstrSvr)

#define IOleDbEncap_get_Database(This,pbstrDb)	\
	(This)->lpVtbl -> get_Database(This,pbstrDb)

#define IOleDbEncap_GetFreeLogKb(This,nReservespace,pnKbFree)	\
	(This)->lpVtbl -> GetFreeLogKb(This,nReservespace,pnKbFree)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IOleDbEncap_BeginTrans_Proxy(
	IOleDbEncap * This);


void __RPC_STUB IOleDbEncap_BeginTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_CommitTrans_Proxy(
	IOleDbEncap * This);


void __RPC_STUB IOleDbEncap_CommitTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_CreateCommand_Proxy(
	IOleDbEncap * This,
	/* [out] */ IOleDbCommand **ppodc);


void __RPC_STUB IOleDbEncap_CreateCommand_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_Init_Proxy(
	IOleDbEncap * This,
	/* [in] */ BSTR bstrServer,
	/* [in] */ BSTR bstrDatabase,
	/* [in] */ IStream *pfistLog,
	/* [in] */ OdeLockTimeoutMode olt,
	/* [in] */ int nmsTimeout);


void __RPC_STUB IOleDbEncap_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_IsTransactionOpen_Proxy(
	IOleDbEncap * This,
	/* [retval][out] */ ComBool *pfTransactionOpen);


void __RPC_STUB IOleDbEncap_IsTransactionOpen_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_RollbackTrans_Proxy(
	IOleDbEncap * This);


void __RPC_STUB IOleDbEncap_RollbackTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_RollbackSavePoint_Proxy(
	IOleDbEncap * This,
	/* [in] */ BSTR bstrSavePoint);


void __RPC_STUB IOleDbEncap_RollbackSavePoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_SetSavePoint_Proxy(
	IOleDbEncap * This,
	/* [out] */ BSTR *pbstr);


void __RPC_STUB IOleDbEncap_SetSavePoint_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_SetSavePointOrBeginTrans_Proxy(
	IOleDbEncap * This,
	/* [out] */ BSTR *pbstr);


void __RPC_STUB IOleDbEncap_SetSavePointOrBeginTrans_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_InitMSDE_Proxy(
	IOleDbEncap * This,
	/* [in] */ IStream *pfistLog,
	/* [in] */ ComBool fForce);


void __RPC_STUB IOleDbEncap_InitMSDE_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IOleDbEncap_get_Server_Proxy(
	IOleDbEncap * This,
	/* [retval][out] */ BSTR *pbstrSvr);


void __RPC_STUB IOleDbEncap_get_Server_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IOleDbEncap_get_Database_Proxy(
	IOleDbEncap * This,
	/* [retval][out] */ BSTR *pbstrDb);


void __RPC_STUB IOleDbEncap_get_Database_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IOleDbEncap_GetFreeLogKb_Proxy(
	IOleDbEncap * This,
	/* [in] */ int nReservespace,
	/* [out] */ int *pnKbFree);


void __RPC_STUB IOleDbEncap_GetFreeLogKb_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IOleDbEncap_INTERFACE_DEFINED__ */


#ifndef __IFwMetaDataCache_INTERFACE_DEFINED__
#define __IFwMetaDataCache_INTERFACE_DEFINED__

/* interface IFwMetaDataCache */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IFwMetaDataCache;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("6AA9042E-0A4D-4f33-881B-3FBE48861D14")
	IFwMetaDataCache : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Init(
			/* [in] */ IOleDbEncap *pode) = 0;

		virtual HRESULT STDMETHODCALLTYPE Reload(
			/* [in] */ IOleDbEncap *pode,
			/* [in] */ ComBool fKeepVirtuals) = 0;

		virtual HRESULT STDMETHODCALLTYPE InitXml(
			/* [in] */ BSTR bstrPathname,
			/* [in] */ ComBool fClearPrevCache) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FieldCount(
			/* [retval][out] */ int *pcflid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldIds(
			/* [in] */ int cflid,
			/* [size_is][out] */ ULONG *rgflid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetOwnClsName(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrOwnClsName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDstClsName(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrDstClsName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetOwnClsId(
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluOwnClsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDstClsId(
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluDstClsid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldName(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldLabel(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldLabel) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldHelp(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldHelp) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldXml(
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldXml) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldListRoot(
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piListRoot) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldWs(
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piWs) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldType(
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piType) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsValidClass(
			/* [in] */ ULONG luFlid,
			/* [in] */ ULONG luClid,
			/* [retval][out] */ ComBool *pfValid) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassCount(
			/* [retval][out] */ int *pcclid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetClassIds(
			/* [in] */ int cclid,
			/* [size_is][out] */ ULONG *rgclid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetClassName(
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrClassName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetAbstract(
			/* [in] */ ULONG luClid,
			/* [out] */ ComBool *pfAbstract) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetBaseClsId(
			/* [in] */ ULONG luClid,
			/* [out] */ ULONG *pluClid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetBaseClsName(
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrBaseClsName) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFields(
			/* [in] */ ULONG luClid,
			/* [in] */ ComBool fIncludeSuperclasses,
			/* [in] */ int grfcpt,
			/* [in] */ int cflidMax,
			/* [size_is][out] */ ULONG *prgflid,
			/* [out] */ int *pcflid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetClassId(
			/* [in] */ BSTR bstrClassName,
			/* [retval][out] */ ULONG *pluClid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldId(
			/* [in] */ BSTR bstrClassName,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetFieldId2(
			/* [in] */ ULONG luClid,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetDirectSubclasses(
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses) = 0;

		virtual HRESULT STDMETHODCALLTYPE GetAllSubclasses(
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddVirtualProp(
			/* [in] */ BSTR bstrClass,
			/* [in] */ BSTR bstrField,
			/* [in] */ ULONG luFlid,
			/* [in] */ int type) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwMetaDataCacheVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwMetaDataCache * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwMetaDataCache * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwMetaDataCache * This);

		HRESULT ( STDMETHODCALLTYPE *Init )(
			IFwMetaDataCache * This,
			/* [in] */ IOleDbEncap *pode);

		HRESULT ( STDMETHODCALLTYPE *Reload )(
			IFwMetaDataCache * This,
			/* [in] */ IOleDbEncap *pode,
			/* [in] */ ComBool fKeepVirtuals);

		HRESULT ( STDMETHODCALLTYPE *InitXml )(
			IFwMetaDataCache * This,
			/* [in] */ BSTR bstrPathname,
			/* [in] */ ComBool fClearPrevCache);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FieldCount )(
			IFwMetaDataCache * This,
			/* [retval][out] */ int *pcflid);

		HRESULT ( STDMETHODCALLTYPE *GetFieldIds )(
			IFwMetaDataCache * This,
			/* [in] */ int cflid,
			/* [size_is][out] */ ULONG *rgflid);

		HRESULT ( STDMETHODCALLTYPE *GetOwnClsName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrOwnClsName);

		HRESULT ( STDMETHODCALLTYPE *GetDstClsName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrDstClsName);

		HRESULT ( STDMETHODCALLTYPE *GetOwnClsId )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluOwnClsid);

		HRESULT ( STDMETHODCALLTYPE *GetDstClsId )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ ULONG *pluDstClsid);

		HRESULT ( STDMETHODCALLTYPE *GetFieldName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldName);

		HRESULT ( STDMETHODCALLTYPE *GetFieldLabel )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldLabel);

		HRESULT ( STDMETHODCALLTYPE *GetFieldHelp )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldHelp);

		HRESULT ( STDMETHODCALLTYPE *GetFieldXml )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ BSTR *pbstrFieldXml);

		HRESULT ( STDMETHODCALLTYPE *GetFieldListRoot )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piListRoot);

		HRESULT ( STDMETHODCALLTYPE *GetFieldWs )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piWs);

		HRESULT ( STDMETHODCALLTYPE *GetFieldType )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [out] */ int *piType);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsValidClass )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luFlid,
			/* [in] */ ULONG luClid,
			/* [retval][out] */ ComBool *pfValid);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassCount )(
			IFwMetaDataCache * This,
			/* [retval][out] */ int *pcclid);

		HRESULT ( STDMETHODCALLTYPE *GetClassIds )(
			IFwMetaDataCache * This,
			/* [in] */ int cclid,
			/* [size_is][out] */ ULONG *rgclid);

		HRESULT ( STDMETHODCALLTYPE *GetClassName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrClassName);

		HRESULT ( STDMETHODCALLTYPE *GetAbstract )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ ComBool *pfAbstract);

		HRESULT ( STDMETHODCALLTYPE *GetBaseClsId )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ ULONG *pluClid);

		HRESULT ( STDMETHODCALLTYPE *GetBaseClsName )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [out] */ BSTR *pbstrBaseClsName);

		HRESULT ( STDMETHODCALLTYPE *GetFields )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ ComBool fIncludeSuperclasses,
			/* [in] */ int grfcpt,
			/* [in] */ int cflidMax,
			/* [size_is][out] */ ULONG *prgflid,
			/* [out] */ int *pcflid);

		HRESULT ( STDMETHODCALLTYPE *GetClassId )(
			IFwMetaDataCache * This,
			/* [in] */ BSTR bstrClassName,
			/* [retval][out] */ ULONG *pluClid);

		HRESULT ( STDMETHODCALLTYPE *GetFieldId )(
			IFwMetaDataCache * This,
			/* [in] */ BSTR bstrClassName,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid);

		HRESULT ( STDMETHODCALLTYPE *GetFieldId2 )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ BSTR bstrFieldName,
			/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
			/* [retval][out] */ ULONG *pluFlid);

		HRESULT ( STDMETHODCALLTYPE *GetDirectSubclasses )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses);

		HRESULT ( STDMETHODCALLTYPE *GetAllSubclasses )(
			IFwMetaDataCache * This,
			/* [in] */ ULONG luClid,
			/* [in] */ int cluMax,
			/* [out] */ int *pcluOut,
			/* [length_is][size_is][out] */ ULONG *prgluSubclasses);

		HRESULT ( STDMETHODCALLTYPE *AddVirtualProp )(
			IFwMetaDataCache * This,
			/* [in] */ BSTR bstrClass,
			/* [in] */ BSTR bstrField,
			/* [in] */ ULONG luFlid,
			/* [in] */ int type);

		END_INTERFACE
	} IFwMetaDataCacheVtbl;

	interface IFwMetaDataCache
	{
		CONST_VTBL struct IFwMetaDataCacheVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwMetaDataCache_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwMetaDataCache_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwMetaDataCache_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwMetaDataCache_Init(This,pode)	\
	(This)->lpVtbl -> Init(This,pode)

#define IFwMetaDataCache_Reload(This,pode,fKeepVirtuals)	\
	(This)->lpVtbl -> Reload(This,pode,fKeepVirtuals)

#define IFwMetaDataCache_InitXml(This,bstrPathname,fClearPrevCache)	\
	(This)->lpVtbl -> InitXml(This,bstrPathname,fClearPrevCache)

#define IFwMetaDataCache_get_FieldCount(This,pcflid)	\
	(This)->lpVtbl -> get_FieldCount(This,pcflid)

#define IFwMetaDataCache_GetFieldIds(This,cflid,rgflid)	\
	(This)->lpVtbl -> GetFieldIds(This,cflid,rgflid)

#define IFwMetaDataCache_GetOwnClsName(This,luFlid,pbstrOwnClsName)	\
	(This)->lpVtbl -> GetOwnClsName(This,luFlid,pbstrOwnClsName)

#define IFwMetaDataCache_GetDstClsName(This,luFlid,pbstrDstClsName)	\
	(This)->lpVtbl -> GetDstClsName(This,luFlid,pbstrDstClsName)

#define IFwMetaDataCache_GetOwnClsId(This,luFlid,pluOwnClsid)	\
	(This)->lpVtbl -> GetOwnClsId(This,luFlid,pluOwnClsid)

#define IFwMetaDataCache_GetDstClsId(This,luFlid,pluDstClsid)	\
	(This)->lpVtbl -> GetDstClsId(This,luFlid,pluDstClsid)

#define IFwMetaDataCache_GetFieldName(This,luFlid,pbstrFieldName)	\
	(This)->lpVtbl -> GetFieldName(This,luFlid,pbstrFieldName)

#define IFwMetaDataCache_GetFieldLabel(This,luFlid,pbstrFieldLabel)	\
	(This)->lpVtbl -> GetFieldLabel(This,luFlid,pbstrFieldLabel)

#define IFwMetaDataCache_GetFieldHelp(This,luFlid,pbstrFieldHelp)	\
	(This)->lpVtbl -> GetFieldHelp(This,luFlid,pbstrFieldHelp)

#define IFwMetaDataCache_GetFieldXml(This,luFlid,pbstrFieldXml)	\
	(This)->lpVtbl -> GetFieldXml(This,luFlid,pbstrFieldXml)

#define IFwMetaDataCache_GetFieldListRoot(This,luFlid,piListRoot)	\
	(This)->lpVtbl -> GetFieldListRoot(This,luFlid,piListRoot)

#define IFwMetaDataCache_GetFieldWs(This,luFlid,piWs)	\
	(This)->lpVtbl -> GetFieldWs(This,luFlid,piWs)

#define IFwMetaDataCache_GetFieldType(This,luFlid,piType)	\
	(This)->lpVtbl -> GetFieldType(This,luFlid,piType)

#define IFwMetaDataCache_get_IsValidClass(This,luFlid,luClid,pfValid)	\
	(This)->lpVtbl -> get_IsValidClass(This,luFlid,luClid,pfValid)

#define IFwMetaDataCache_get_ClassCount(This,pcclid)	\
	(This)->lpVtbl -> get_ClassCount(This,pcclid)

#define IFwMetaDataCache_GetClassIds(This,cclid,rgclid)	\
	(This)->lpVtbl -> GetClassIds(This,cclid,rgclid)

#define IFwMetaDataCache_GetClassName(This,luClid,pbstrClassName)	\
	(This)->lpVtbl -> GetClassName(This,luClid,pbstrClassName)

#define IFwMetaDataCache_GetAbstract(This,luClid,pfAbstract)	\
	(This)->lpVtbl -> GetAbstract(This,luClid,pfAbstract)

#define IFwMetaDataCache_GetBaseClsId(This,luClid,pluClid)	\
	(This)->lpVtbl -> GetBaseClsId(This,luClid,pluClid)

#define IFwMetaDataCache_GetBaseClsName(This,luClid,pbstrBaseClsName)	\
	(This)->lpVtbl -> GetBaseClsName(This,luClid,pbstrBaseClsName)

#define IFwMetaDataCache_GetFields(This,luClid,fIncludeSuperclasses,grfcpt,cflidMax,prgflid,pcflid)	\
	(This)->lpVtbl -> GetFields(This,luClid,fIncludeSuperclasses,grfcpt,cflidMax,prgflid,pcflid)

#define IFwMetaDataCache_GetClassId(This,bstrClassName,pluClid)	\
	(This)->lpVtbl -> GetClassId(This,bstrClassName,pluClid)

#define IFwMetaDataCache_GetFieldId(This,bstrClassName,bstrFieldName,fIncludeBaseClasses,pluFlid)	\
	(This)->lpVtbl -> GetFieldId(This,bstrClassName,bstrFieldName,fIncludeBaseClasses,pluFlid)

#define IFwMetaDataCache_GetFieldId2(This,luClid,bstrFieldName,fIncludeBaseClasses,pluFlid)	\
	(This)->lpVtbl -> GetFieldId2(This,luClid,bstrFieldName,fIncludeBaseClasses,pluFlid)

#define IFwMetaDataCache_GetDirectSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)	\
	(This)->lpVtbl -> GetDirectSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)

#define IFwMetaDataCache_GetAllSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)	\
	(This)->lpVtbl -> GetAllSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)

#define IFwMetaDataCache_AddVirtualProp(This,bstrClass,bstrField,luFlid,type)	\
	(This)->lpVtbl -> AddVirtualProp(This,bstrClass,bstrField,luFlid,type)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwMetaDataCache_Init_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ IOleDbEncap *pode);


void __RPC_STUB IFwMetaDataCache_Init_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_Reload_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ IOleDbEncap *pode,
	/* [in] */ ComBool fKeepVirtuals);


void __RPC_STUB IFwMetaDataCache_Reload_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_InitXml_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrPathname,
	/* [in] */ ComBool fClearPrevCache);


void __RPC_STUB IFwMetaDataCache_InitXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwMetaDataCache_get_FieldCount_Proxy(
	IFwMetaDataCache * This,
	/* [retval][out] */ int *pcflid);


void __RPC_STUB IFwMetaDataCache_get_FieldCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldIds_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ int cflid,
	/* [size_is][out] */ ULONG *rgflid);


void __RPC_STUB IFwMetaDataCache_GetFieldIds_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetOwnClsName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrOwnClsName);


void __RPC_STUB IFwMetaDataCache_GetOwnClsName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetDstClsName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrDstClsName);


void __RPC_STUB IFwMetaDataCache_GetDstClsName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetOwnClsId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ ULONG *pluOwnClsid);


void __RPC_STUB IFwMetaDataCache_GetOwnClsId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetDstClsId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ ULONG *pluDstClsid);


void __RPC_STUB IFwMetaDataCache_GetDstClsId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldName);


void __RPC_STUB IFwMetaDataCache_GetFieldName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldLabel_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldLabel);


void __RPC_STUB IFwMetaDataCache_GetFieldLabel_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldHelp_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldHelp);


void __RPC_STUB IFwMetaDataCache_GetFieldHelp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldXml_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ BSTR *pbstrFieldXml);


void __RPC_STUB IFwMetaDataCache_GetFieldXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldListRoot_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ int *piListRoot);


void __RPC_STUB IFwMetaDataCache_GetFieldListRoot_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldWs_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ int *piWs);


void __RPC_STUB IFwMetaDataCache_GetFieldWs_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldType_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [out] */ int *piType);


void __RPC_STUB IFwMetaDataCache_GetFieldType_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwMetaDataCache_get_IsValidClass_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luFlid,
	/* [in] */ ULONG luClid,
	/* [retval][out] */ ComBool *pfValid);


void __RPC_STUB IFwMetaDataCache_get_IsValidClass_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IFwMetaDataCache_get_ClassCount_Proxy(
	IFwMetaDataCache * This,
	/* [retval][out] */ int *pcclid);


void __RPC_STUB IFwMetaDataCache_get_ClassCount_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetClassIds_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ int cclid,
	/* [size_is][out] */ ULONG *rgclid);


void __RPC_STUB IFwMetaDataCache_GetClassIds_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetClassName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ BSTR *pbstrClassName);


void __RPC_STUB IFwMetaDataCache_GetClassName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetAbstract_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ ComBool *pfAbstract);


void __RPC_STUB IFwMetaDataCache_GetAbstract_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetBaseClsId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ ULONG *pluClid);


void __RPC_STUB IFwMetaDataCache_GetBaseClsId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetBaseClsName_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [out] */ BSTR *pbstrBaseClsName);


void __RPC_STUB IFwMetaDataCache_GetBaseClsName_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFields_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ ComBool fIncludeSuperclasses,
	/* [in] */ int grfcpt,
	/* [in] */ int cflidMax,
	/* [size_is][out] */ ULONG *prgflid,
	/* [out] */ int *pcflid);


void __RPC_STUB IFwMetaDataCache_GetFields_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetClassId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrClassName,
	/* [retval][out] */ ULONG *pluClid);


void __RPC_STUB IFwMetaDataCache_GetClassId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldId_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrClassName,
	/* [in] */ BSTR bstrFieldName,
	/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
	/* [retval][out] */ ULONG *pluFlid);


void __RPC_STUB IFwMetaDataCache_GetFieldId_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetFieldId2_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ BSTR bstrFieldName,
	/* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
	/* [retval][out] */ ULONG *pluFlid);


void __RPC_STUB IFwMetaDataCache_GetFieldId2_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetDirectSubclasses_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ int cluMax,
	/* [out] */ int *pcluOut,
	/* [length_is][size_is][out] */ ULONG *prgluSubclasses);


void __RPC_STUB IFwMetaDataCache_GetDirectSubclasses_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_GetAllSubclasses_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ ULONG luClid,
	/* [in] */ int cluMax,
	/* [out] */ int *pcluOut,
	/* [length_is][size_is][out] */ ULONG *prgluSubclasses);


void __RPC_STUB IFwMetaDataCache_GetAllSubclasses_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwMetaDataCache_AddVirtualProp_Proxy(
	IFwMetaDataCache * This,
	/* [in] */ BSTR bstrClass,
	/* [in] */ BSTR bstrField,
	/* [in] */ ULONG luFlid,
	/* [in] */ int type);


void __RPC_STUB IFwMetaDataCache_AddVirtualProp_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwMetaDataCache_INTERFACE_DEFINED__ */


#ifndef __IDbAdmin_INTERFACE_DEFINED__
#define __IDbAdmin_INTERFACE_DEFINED__

/* interface IDbAdmin */
/* [unique][object][uuid] */


EXTERN_C const IID IID_IDbAdmin;

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("2A861F95-63D0-480d-B5AF-4FAF0D22125D")
	IDbAdmin : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE CopyDatabase(
			/* [in] */ BSTR bstrSrcPathName,
			/* [in] */ BSTR bstrDstPathName) = 0;

		virtual HRESULT STDMETHODCALLTYPE AttachDatabase(
			/* [in] */ BSTR bstrDatabaseName,
			/* [in] */ BSTR bstrPathName) = 0;

		virtual HRESULT STDMETHODCALLTYPE DetachDatabase(
			/* [in] */ BSTR bstrDatabaseName) = 0;

		virtual HRESULT STDMETHODCALLTYPE RenameDatabase(
			/* [in] */ BSTR bstrDirName,
			/* [in] */ BSTR bstrOldName,
			/* [in] */ BSTR bstrNewName,
			/* [in] */ ComBool fDetachBefore,
			/* [in] */ ComBool fAttachAfter) = 0;

		virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_LogStream(
			/* [in] */ IStream *pstrm) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwRootDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwMigrationScriptDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwDatabaseDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

		virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FwTemplateDir(
			/* [retval][out] */ BSTR *pbstr) = 0;

	};

#else 	/* C style interface */

	typedef struct IDbAdminVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IDbAdmin * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IDbAdmin * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IDbAdmin * This);

		HRESULT ( STDMETHODCALLTYPE *CopyDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrSrcPathName,
			/* [in] */ BSTR bstrDstPathName);

		HRESULT ( STDMETHODCALLTYPE *AttachDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrDatabaseName,
			/* [in] */ BSTR bstrPathName);

		HRESULT ( STDMETHODCALLTYPE *DetachDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrDatabaseName);

		HRESULT ( STDMETHODCALLTYPE *RenameDatabase )(
			IDbAdmin * This,
			/* [in] */ BSTR bstrDirName,
			/* [in] */ BSTR bstrOldName,
			/* [in] */ BSTR bstrNewName,
			/* [in] */ ComBool fDetachBefore,
			/* [in] */ ComBool fAttachAfter);

		/* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_LogStream )(
			IDbAdmin * This,
			/* [in] */ IStream *pstrm);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwRootDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwMigrationScriptDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwDatabaseDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		/* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FwTemplateDir )(
			IDbAdmin * This,
			/* [retval][out] */ BSTR *pbstr);

		END_INTERFACE
	} IDbAdminVtbl;

	interface IDbAdmin
	{
		CONST_VTBL struct IDbAdminVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IDbAdmin_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IDbAdmin_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IDbAdmin_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IDbAdmin_CopyDatabase(This,bstrSrcPathName,bstrDstPathName)	\
	(This)->lpVtbl -> CopyDatabase(This,bstrSrcPathName,bstrDstPathName)

#define IDbAdmin_AttachDatabase(This,bstrDatabaseName,bstrPathName)	\
	(This)->lpVtbl -> AttachDatabase(This,bstrDatabaseName,bstrPathName)

#define IDbAdmin_DetachDatabase(This,bstrDatabaseName)	\
	(This)->lpVtbl -> DetachDatabase(This,bstrDatabaseName)

#define IDbAdmin_RenameDatabase(This,bstrDirName,bstrOldName,bstrNewName,fDetachBefore,fAttachAfter)	\
	(This)->lpVtbl -> RenameDatabase(This,bstrDirName,bstrOldName,bstrNewName,fDetachBefore,fAttachAfter)

#define IDbAdmin_putref_LogStream(This,pstrm)	\
	(This)->lpVtbl -> putref_LogStream(This,pstrm)

#define IDbAdmin_get_FwRootDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwRootDir(This,pbstr)

#define IDbAdmin_get_FwMigrationScriptDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwMigrationScriptDir(This,pbstr)

#define IDbAdmin_get_FwDatabaseDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwDatabaseDir(This,pbstr)

#define IDbAdmin_get_FwTemplateDir(This,pbstr)	\
	(This)->lpVtbl -> get_FwTemplateDir(This,pbstr)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IDbAdmin_CopyDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrSrcPathName,
	/* [in] */ BSTR bstrDstPathName);


void __RPC_STUB IDbAdmin_CopyDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbAdmin_AttachDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrDatabaseName,
	/* [in] */ BSTR bstrPathName);


void __RPC_STUB IDbAdmin_AttachDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbAdmin_DetachDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrDatabaseName);


void __RPC_STUB IDbAdmin_DetachDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IDbAdmin_RenameDatabase_Proxy(
	IDbAdmin * This,
	/* [in] */ BSTR bstrDirName,
	/* [in] */ BSTR bstrOldName,
	/* [in] */ BSTR bstrNewName,
	/* [in] */ ComBool fDetachBefore,
	/* [in] */ ComBool fAttachAfter);


void __RPC_STUB IDbAdmin_RenameDatabase_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propputref] */ HRESULT STDMETHODCALLTYPE IDbAdmin_putref_LogStream_Proxy(
	IDbAdmin * This,
	/* [in] */ IStream *pstrm);


void __RPC_STUB IDbAdmin_putref_LogStream_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwRootDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwRootDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwMigrationScriptDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwMigrationScriptDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwDatabaseDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwDatabaseDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


/* [propget] */ HRESULT STDMETHODCALLTYPE IDbAdmin_get_FwTemplateDir_Proxy(
	IDbAdmin * This,
	/* [retval][out] */ BSTR *pbstr);


void __RPC_STUB IDbAdmin_get_FwTemplateDir_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IDbAdmin_INTERFACE_DEFINED__ */


EXTERN_C const CLSID CLSID_OleDbEncap;

#ifdef __cplusplus

class DECLSPEC_UUID("AAB4A4A3-3C83-11d4-A1BB-00C04F0C9593")
OleDbEncap;
#endif

EXTERN_C const CLSID CLSID_FwMetaDataCache;

#ifdef __cplusplus

class DECLSPEC_UUID("3A1B1AC6-24C5-4ffe-85D5-675DB4B9FCBB")
FwMetaDataCache;
#endif

EXTERN_C const CLSID CLSID_DbAdmin;

#ifdef __cplusplus

class DECLSPEC_UUID("D584A725-8CF4-4699-941F-D1337AC7DB5C")
DbAdmin;
#endif
#endif /* __DbAccess_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
