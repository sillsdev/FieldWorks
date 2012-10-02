

/* this ALWAYS GENERATED file contains the proxy stub code */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:01:57 2006
 */
/* Compiler settings for C:\fw\Output\Common\FwCellarPs.idl:
	Oicf, W1, Zp8, env=Win32 (32b run)
	protocol : dce , ms_ext, c_ext, robust
	error checks: allocation ref bounds_check enum stub_data
	VC __declspec() decoration level:
		 __declspec(uuid()), __declspec(selectany), __declspec(novtable)
		 DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#if !defined(_M_IA64) && !defined(_M_AMD64)


#pragma warning( disable: 4049 )  /* more than 64k source lines */
#if _MSC_VER >= 1200
#pragma warning(push)
#endif
#pragma warning( disable: 4100 ) /* unreferenced arguments in x86 call */
#pragma warning( disable: 4211 )  /* redefine extent to static */
#pragma warning( disable: 4232 )  /* dllimport identity*/
#define USE_STUBLESS_PROXY


/* verify that the <rpcproxy.h> version is high enough to compile this file*/
#ifndef __REDQ_RPCPROXY_H_VERSION__
#define __REQUIRED_RPCPROXY_H_VERSION__ 475
#endif


#include "rpcproxy.h"
#ifndef __RPCPROXY_H_VERSION__
#error this stub requires an updated version of <rpcproxy.h>
#endif // __RPCPROXY_H_VERSION__


#include "FwCellarPs.h"

#define TYPE_FORMAT_STRING_SIZE   75
#define PROC_FORMAT_STRING_SIZE   217
#define TRANSMIT_AS_TABLE_SIZE    0
#define WIRE_MARSHAL_TABLE_SIZE   1

typedef struct _MIDL_TYPE_FORMAT_STRING
	{
	short          Pad;
	unsigned char  Format[ TYPE_FORMAT_STRING_SIZE ];
	} MIDL_TYPE_FORMAT_STRING;

typedef struct _MIDL_PROC_FORMAT_STRING
	{
	short          Pad;
	unsigned char  Format[ PROC_FORMAT_STRING_SIZE ];
	} MIDL_PROC_FORMAT_STRING;


static RPC_SYNTAX_IDENTIFIER  _RpcTransferSyntax =
{{0x8A885D04,0x1CEB,0x11C9,{0x9F,0xE8,0x08,0x00,0x2B,0x10,0x48,0x60}},{2,0}};


extern const MIDL_TYPE_FORMAT_STRING __MIDL_TypeFormatString;
extern const MIDL_PROC_FORMAT_STRING __MIDL_ProcFormatString;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IFwXmlData_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IFwXmlData_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IFwXmlData2_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IFwXmlData2_ProxyInfo;


extern const USER_MARSHAL_ROUTINE_QUADRUPLE UserMarshalRoutines[ WIRE_MARSHAL_TABLE_SIZE ];

#if !defined(__RPC_WIN32__)
#error  Invalid build platform for this stub.
#endif

#if !(TARGET_IS_NT50_OR_LATER)
#error You need a Windows 2000 or later to run this stub because it uses these features:
#error   /robust command line switch.
#error However, your C/C++ compilation flags indicate you intend to run this app on earlier systems.
#error This app will die there with the RPC_X_WRONG_STUB_VERSION error.
#endif


static const MIDL_PROC_FORMAT_STRING __MIDL_ProcFormatString =
	{
		0,
		{

	/* Procedure Open */

			0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x3 ),	/* 3 */
/*  8 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 16 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 18 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20 */	NdrFcShort( 0x2 ),	/* 2 */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrServer */

/* 24 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 26 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 28 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrDatabase */

/* 30 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 32 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 34 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 36 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 38 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 40 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Close */

/* 42 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 44 */	NdrFcLong( 0x0 ),	/* 0 */
/* 48 */	NdrFcShort( 0x4 ),	/* 4 */
/* 50 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 52 */	NdrFcShort( 0x0 ),	/* 0 */
/* 54 */	NdrFcShort( 0x8 ),	/* 8 */
/* 56 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 58 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 60 */	NdrFcShort( 0x0 ),	/* 0 */
/* 62 */	NdrFcShort( 0x0 ),	/* 0 */
/* 64 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 66 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 68 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 70 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LoadXml */

/* 72 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 74 */	NdrFcLong( 0x0 ),	/* 0 */
/* 78 */	NdrFcShort( 0x5 ),	/* 5 */
/* 80 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 82 */	NdrFcShort( 0x0 ),	/* 0 */
/* 84 */	NdrFcShort( 0x8 ),	/* 8 */
/* 86 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 88 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 90 */	NdrFcShort( 0x0 ),	/* 0 */
/* 92 */	NdrFcShort( 0x1 ),	/* 1 */
/* 94 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrFile */

/* 96 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 98 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 100 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter padvi */

/* 102 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 104 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 106 */	NdrFcShort( 0x26 ),	/* Type Offset=38 */

	/* Return value */

/* 108 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 110 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SaveXml */

/* 114 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 116 */	NdrFcLong( 0x0 ),	/* 0 */
/* 120 */	NdrFcShort( 0x6 ),	/* 6 */
/* 122 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 124 */	NdrFcShort( 0x0 ),	/* 0 */
/* 126 */	NdrFcShort( 0x8 ),	/* 8 */
/* 128 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 130 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 132 */	NdrFcShort( 0x0 ),	/* 0 */
/* 134 */	NdrFcShort( 0x1 ),	/* 1 */
/* 136 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrFile */

/* 138 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 140 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 142 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pwsf */

/* 144 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 146 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 148 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Parameter padvi */

/* 150 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 152 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 154 */	NdrFcShort( 0x26 ),	/* Type Offset=38 */

	/* Return value */

/* 156 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 158 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 160 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ImportXmlObject */

/* 162 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 164 */	NdrFcLong( 0x0 ),	/* 0 */
/* 168 */	NdrFcShort( 0x7 ),	/* 7 */
/* 170 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 172 */	NdrFcShort( 0x10 ),	/* 16 */
/* 174 */	NdrFcShort( 0x8 ),	/* 8 */
/* 176 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 178 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 180 */	NdrFcShort( 0x0 ),	/* 0 */
/* 182 */	NdrFcShort( 0x1 ),	/* 1 */
/* 184 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrFile */

/* 186 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 188 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 190 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter hvoOwner */

/* 192 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 194 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter flid */

/* 198 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 200 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 202 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter padvi */

/* 204 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 206 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 208 */	NdrFcShort( 0x26 ),	/* Type Offset=38 */

	/* Return value */

/* 210 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 212 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 214 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

			0x0
		}
	};

static const MIDL_TYPE_FORMAT_STRING __MIDL_TypeFormatString =
	{
		0,
		{
			NdrFcShort( 0x0 ),	/* 0 */
/*  2 */
			0x12, 0x0,	/* FC_UP */
/*  4 */	NdrFcShort( 0xe ),	/* Offset= 14 (18) */
/*  6 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/*  8 */	NdrFcShort( 0x2 ),	/* 2 */
/* 10 */	0x9,		/* Corr desc: FC_ULONG */
			0x0,		/*  */
/* 12 */	NdrFcShort( 0xfffc ),	/* -4 */
/* 14 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 16 */	0x6,		/* FC_SHORT */
			0x5b,		/* FC_END */
/* 18 */
			0x17,		/* FC_CSTRUCT */
			0x3,		/* 3 */
/* 20 */	NdrFcShort( 0x8 ),	/* 8 */
/* 22 */	NdrFcShort( 0xfff0 ),	/* Offset= -16 (6) */
/* 24 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 26 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 28 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 30 */	NdrFcShort( 0x0 ),	/* 0 */
/* 32 */	NdrFcShort( 0x4 ),	/* 4 */
/* 34 */	NdrFcShort( 0x0 ),	/* 0 */
/* 36 */	NdrFcShort( 0xffde ),	/* Offset= -34 (2) */
/* 38 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 40 */	NdrFcLong( 0x5f74ab40 ),	/* 1601481536 */
/* 44 */	NdrFcShort( 0xefe8 ),	/* -4120 */
/* 46 */	NdrFcShort( 0x4a0d ),	/* 18957 */
/* 48 */	0xb9,		/* 185 */
			0xae,		/* 174 */
/* 50 */	0x30,		/* 48 */
			0xf4,		/* 244 */
/* 52 */	0x93,		/* 147 */
			0xfe,		/* 254 */
/* 54 */	0x6e,		/* 110 */
			0x21,		/* 33 */
/* 56 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 58 */	NdrFcLong( 0x2c4636e3 ),	/* 742799075 */
/* 62 */	NdrFcShort( 0x4f49 ),	/* 20297 */
/* 64 */	NdrFcShort( 0x4966 ),	/* 18790 */
/* 66 */	0x96,		/* 150 */
			0x6f,		/* 111 */
/* 68 */	0x9,		/* 9 */
			0x53,		/* 83 */
/* 70 */	0xf9,		/* 249 */
			0x7f,		/* 127 */
/* 72 */	0x51,		/* 81 */
			0xc8,		/* 200 */

			0x0
		}
	};

static const USER_MARSHAL_ROUTINE_QUADRUPLE UserMarshalRoutines[ WIRE_MARSHAL_TABLE_SIZE ] =
		{

			{
			BSTR_UserSize
			,BSTR_UserMarshal
			,BSTR_UserUnmarshal
			,BSTR_UserFree
			}

		};



/* Standard interface: __MIDL_itf_FwCellarPs_0000, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IUnknown, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}} */


/* Object interface: IFwXmlData, ver. 0.0,
   GUID={0x65BAE1A5,0x1B75,0x4127,{0x84,0x1E,0x02,0x28,0xF9,0x08,0x72,0x7D}} */

#pragma code_seg(".orpc")
static const unsigned short IFwXmlData_FormatStringOffsetTable[] =
	{
	0,
	42,
	72,
	114
	};

static const MIDL_STUBLESS_PROXY_INFO IFwXmlData_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IFwXmlData_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IFwXmlData_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IFwXmlData_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IFwXmlDataProxyVtbl =
{
	&IFwXmlData_ProxyInfo,
	&IID_IFwXmlData,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IFwXmlData::Open */ ,
	(void *) (INT_PTR) -1 /* IFwXmlData::Close */ ,
	(void *) (INT_PTR) -1 /* IFwXmlData::LoadXml */ ,
	(void *) (INT_PTR) -1 /* IFwXmlData::SaveXml */
};

const CInterfaceStubVtbl _IFwXmlDataStubVtbl =
{
	&IID_IFwXmlData,
	&IFwXmlData_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwCellarPs_0327, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IFwXmlData2, ver. 0.0,
   GUID={0xDE12C0CD,0x5836,0x4A4A,{0x9E,0x80,0xD4,0x65,0xB6,0x9C,0x70,0x3E}} */

#pragma code_seg(".orpc")
static const unsigned short IFwXmlData2_FormatStringOffsetTable[] =
	{
	0,
	42,
	72,
	114,
	162
	};

static const MIDL_STUBLESS_PROXY_INFO IFwXmlData2_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IFwXmlData2_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IFwXmlData2_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IFwXmlData2_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(8) _IFwXmlData2ProxyVtbl =
{
	&IFwXmlData2_ProxyInfo,
	&IID_IFwXmlData2,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IFwXmlData::Open */ ,
	(void *) (INT_PTR) -1 /* IFwXmlData::Close */ ,
	(void *) (INT_PTR) -1 /* IFwXmlData::LoadXml */ ,
	(void *) (INT_PTR) -1 /* IFwXmlData::SaveXml */ ,
	(void *) (INT_PTR) -1 /* IFwXmlData2::ImportXmlObject */
};

const CInterfaceStubVtbl _IFwXmlData2StubVtbl =
{
	&IID_IFwXmlData2,
	&IFwXmlData2_ServerInfo,
	8,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};

static const MIDL_STUB_DESC Object_StubDesc =
	{
	0,
	NdrOleAllocate,
	NdrOleFree,
	0,
	0,
	0,
	0,
	0,
	__MIDL_TypeFormatString.Format,
	1, /* -error bounds_check flag */
	0x50002, /* Ndr library version */
	0,
	0x6000169, /* MIDL Version 6.0.361 */
	0,
	UserMarshalRoutines,
	0,  /* notify & notify_flag routine table */
	0x1, /* MIDL flag */
	0, /* cs routines */
	0,   /* proxy/server info */
	0   /* Reserved5 */
	};

const CInterfaceProxyVtbl * _FwCellarPs_ProxyVtblList[] =
{
	( CInterfaceProxyVtbl *) &_IFwXmlDataProxyVtbl,
	( CInterfaceProxyVtbl *) &_IFwXmlData2ProxyVtbl,
	0
};

const CInterfaceStubVtbl * _FwCellarPs_StubVtblList[] =
{
	( CInterfaceStubVtbl *) &_IFwXmlDataStubVtbl,
	( CInterfaceStubVtbl *) &_IFwXmlData2StubVtbl,
	0
};

PCInterfaceName const _FwCellarPs_InterfaceNamesList[] =
{
	"IFwXmlData",
	"IFwXmlData2",
	0
};


#define _FwCellarPs_CHECK_IID(n)	IID_GENERIC_CHECK_IID( _FwCellarPs, pIID, n)

int __stdcall _FwCellarPs_IID_Lookup( const IID * pIID, int * pIndex )
{
	IID_BS_LOOKUP_SETUP

	IID_BS_LOOKUP_INITIAL_TEST( _FwCellarPs, 2, 1 )
	IID_BS_LOOKUP_RETURN_RESULT( _FwCellarPs, 2, *pIndex )

}

const ExtendedProxyFileInfo FwCellarPs_ProxyFileInfo =
{
	(PCInterfaceProxyVtblList *) & _FwCellarPs_ProxyVtblList,
	(PCInterfaceStubVtblList *) & _FwCellarPs_StubVtblList,
	(const PCInterfaceName * ) & _FwCellarPs_InterfaceNamesList,
	0, // no delegation
	& _FwCellarPs_IID_Lookup,
	2,
	2,
	0, /* table of [async_uuid] interfaces */
	0, /* Filler1 */
	0, /* Filler2 */
	0  /* Filler3 */
};
#if _MSC_VER >= 1200
#pragma warning(pop)
#endif


#endif /* !defined(_M_IA64) && !defined(_M_AMD64)*/
