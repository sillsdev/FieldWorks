

/* this ALWAYS GENERATED file contains the proxy stub code */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:02:56 2006
 */
/* Compiler settings for C:\fw\Output\Common\ViewsPs.idl:
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


#include "ViewsPs.h"

#define TYPE_FORMAT_STRING_SIZE   3147
#define PROC_FORMAT_STRING_SIZE   19891
#define TRANSMIT_AS_TABLE_SIZE    0
#define WIRE_MARSHAL_TABLE_SIZE   3

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


extern const MIDL_SERVER_INFO IVwNotifyChange_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwNotifyChange_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IDbColSpec_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IDbColSpec_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ISilDataAccess_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ISilDataAccess_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwCacheDa_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwCacheDa_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwOleDbDa_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwOleDbDa_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ISetupVwOleDbDa_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ISetupVwOleDbDa_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwRootBox_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwRootBox_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwViewConstructor_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwViewConstructor_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwRootSite_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwRootSite_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwObjDelNotification_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwObjDelNotification_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwEnv_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwEnv_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwEmbeddedWindow_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwEmbeddedWindow_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwSelection_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwSelection_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IEventListener_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IEventListener_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwStylesheet_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwStylesheet_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwPropertyStore_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwPropertyStore_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwOverlay_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwOverlay_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwPrintContext_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwPrintContext_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ISqlUndoAction_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ISqlUndoAction_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwPattern_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwPattern_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwSearchKiller_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwSearchKiller_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwDrawRootBuffered_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwDrawRootBuffered_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwSynchronizer_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwSynchronizer_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwDataSpec_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwDataSpec_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwNotifyObjCharDeletion_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwNotifyObjCharDeletion_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwLayoutStream_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwLayoutStream_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwLayoutManager_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwLayoutManager_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwVirtualHandler_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwVirtualHandler_ProxyInfo;


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

	/* Procedure PropChanged */

			0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x3 ),	/* 3 */
/*  8 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 10 */	NdrFcShort( 0x28 ),	/* 40 */
/* 12 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 16 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 24 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 26 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 28 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 30 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 32 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 34 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ivMin */

/* 36 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 38 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 40 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvIns */

/* 42 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 44 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 46 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvDel */

/* 48 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 50 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 52 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 54 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 56 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 58 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Clear */

/* 60 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 62 */	NdrFcLong( 0x0 ),	/* 0 */
/* 66 */	NdrFcShort( 0x3 ),	/* 3 */
/* 68 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 70 */	NdrFcShort( 0x0 ),	/* 0 */
/* 72 */	NdrFcShort( 0x8 ),	/* 8 */
/* 74 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 76 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 78 */	NdrFcShort( 0x0 ),	/* 0 */
/* 80 */	NdrFcShort( 0x0 ),	/* 0 */
/* 82 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 84 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 86 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 88 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Push */

/* 90 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 92 */	NdrFcLong( 0x0 ),	/* 0 */
/* 96 */	NdrFcShort( 0x4 ),	/* 4 */
/* 98 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 100 */	NdrFcShort( 0x20 ),	/* 32 */
/* 102 */	NdrFcShort( 0x8 ),	/* 8 */
/* 104 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 106 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 108 */	NdrFcShort( 0x0 ),	/* 0 */
/* 110 */	NdrFcShort( 0x0 ),	/* 0 */
/* 112 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter oct */

/* 114 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 116 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 118 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter icolBase */

/* 120 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 122 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 126 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 128 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 130 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 132 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 134 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 136 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 138 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 140 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 142 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Size */

/* 144 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 146 */	NdrFcLong( 0x0 ),	/* 0 */
/* 150 */	NdrFcShort( 0x5 ),	/* 5 */
/* 152 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 154 */	NdrFcShort( 0x0 ),	/* 0 */
/* 156 */	NdrFcShort( 0x24 ),	/* 36 */
/* 158 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 160 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 162 */	NdrFcShort( 0x0 ),	/* 0 */
/* 164 */	NdrFcShort( 0x0 ),	/* 0 */
/* 166 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pc */

/* 168 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 170 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 174 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 176 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 178 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetColInfo */

/* 180 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 182 */	NdrFcLong( 0x0 ),	/* 0 */
/* 186 */	NdrFcShort( 0x6 ),	/* 6 */
/* 188 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 190 */	NdrFcShort( 0x8 ),	/* 8 */
/* 192 */	NdrFcShort( 0x78 ),	/* 120 */
/* 194 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 196 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 198 */	NdrFcShort( 0x0 ),	/* 0 */
/* 200 */	NdrFcShort( 0x0 ),	/* 0 */
/* 202 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iIndex */

/* 204 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 206 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 208 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter poct */

/* 210 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 212 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 214 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter picolBase */

/* 216 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 218 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 220 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptag */

/* 222 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 224 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 226 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 228 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 230 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 232 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 234 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 236 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDbColType */

/* 240 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 242 */	NdrFcLong( 0x0 ),	/* 0 */
/* 246 */	NdrFcShort( 0x7 ),	/* 7 */
/* 248 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 250 */	NdrFcShort( 0x8 ),	/* 8 */
/* 252 */	NdrFcShort( 0x24 ),	/* 36 */
/* 254 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 256 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 260 */	NdrFcShort( 0x0 ),	/* 0 */
/* 262 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iIndex */

/* 264 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 266 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 268 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter poct */

/* 270 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 272 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 274 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 276 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 278 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 280 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PageHeight */


	/* Procedure GetBaseCol */

/* 282 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 284 */	NdrFcLong( 0x0 ),	/* 0 */
/* 288 */	NdrFcShort( 0x8 ),	/* 8 */
/* 290 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 292 */	NdrFcShort( 0x8 ),	/* 8 */
/* 294 */	NdrFcShort( 0x24 ),	/* 36 */
/* 296 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 298 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 302 */	NdrFcShort( 0x0 ),	/* 0 */
/* 304 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hPage */


	/* Parameter iIndex */

/* 306 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 308 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 310 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdysHeight */


	/* Parameter piBaseCol */

/* 312 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 314 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 316 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 318 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 320 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 322 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PagePostion */


	/* Procedure GetTag */

/* 324 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 326 */	NdrFcLong( 0x0 ),	/* 0 */
/* 330 */	NdrFcShort( 0x9 ),	/* 9 */
/* 332 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 334 */	NdrFcShort( 0x8 ),	/* 8 */
/* 336 */	NdrFcShort( 0x24 ),	/* 36 */
/* 338 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 340 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 346 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hPage */


	/* Parameter iIndex */

/* 348 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 350 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pysPosition */


	/* Parameter ptag */

/* 354 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 356 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 358 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 360 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 362 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 364 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetWs */

/* 366 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 368 */	NdrFcLong( 0x0 ),	/* 0 */
/* 372 */	NdrFcShort( 0xa ),	/* 10 */
/* 374 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 376 */	NdrFcShort( 0x8 ),	/* 8 */
/* 378 */	NdrFcShort( 0x24 ),	/* 36 */
/* 380 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 382 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 384 */	NdrFcShort( 0x0 ),	/* 0 */
/* 386 */	NdrFcShort( 0x0 ),	/* 0 */
/* 388 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iIndex */

/* 390 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 392 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 394 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 396 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 398 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 400 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 402 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 404 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 406 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ObjectProp */

/* 408 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 410 */	NdrFcLong( 0x0 ),	/* 0 */
/* 414 */	NdrFcShort( 0x3 ),	/* 3 */
/* 416 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 418 */	NdrFcShort( 0x10 ),	/* 16 */
/* 420 */	NdrFcShort( 0x24 ),	/* 36 */
/* 422 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 424 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 426 */	NdrFcShort( 0x0 ),	/* 0 */
/* 428 */	NdrFcShort( 0x0 ),	/* 0 */
/* 430 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 432 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 434 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 436 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 438 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 440 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 442 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvo */

/* 444 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 446 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 448 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 450 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 452 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 454 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_VecItem */

/* 456 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 458 */	NdrFcLong( 0x0 ),	/* 0 */
/* 462 */	NdrFcShort( 0x4 ),	/* 4 */
/* 464 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 466 */	NdrFcShort( 0x18 ),	/* 24 */
/* 468 */	NdrFcShort( 0x24 ),	/* 36 */
/* 470 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 472 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 474 */	NdrFcShort( 0x0 ),	/* 0 */
/* 476 */	NdrFcShort( 0x0 ),	/* 0 */
/* 478 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 480 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 482 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 484 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 486 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 488 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 490 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter index */

/* 492 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 494 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvo */

/* 498 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 500 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 504 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 506 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_VecSize */

/* 510 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 512 */	NdrFcLong( 0x0 ),	/* 0 */
/* 516 */	NdrFcShort( 0x5 ),	/* 5 */
/* 518 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 520 */	NdrFcShort( 0x10 ),	/* 16 */
/* 522 */	NdrFcShort( 0x24 ),	/* 36 */
/* 524 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 526 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 528 */	NdrFcShort( 0x0 ),	/* 0 */
/* 530 */	NdrFcShort( 0x0 ),	/* 0 */
/* 532 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 534 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 536 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 540 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 542 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 544 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pchvo */

/* 546 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 548 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 550 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 552 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 554 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 556 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_VecSizeAssumeCached */

/* 558 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 560 */	NdrFcLong( 0x0 ),	/* 0 */
/* 564 */	NdrFcShort( 0x6 ),	/* 6 */
/* 566 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 568 */	NdrFcShort( 0x10 ),	/* 16 */
/* 570 */	NdrFcShort( 0x24 ),	/* 36 */
/* 572 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 574 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 576 */	NdrFcShort( 0x0 ),	/* 0 */
/* 578 */	NdrFcShort( 0x0 ),	/* 0 */
/* 580 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 582 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 584 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 586 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 588 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 590 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pchvo */

/* 594 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 596 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 600 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 602 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 604 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure VecProp */

/* 606 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 608 */	NdrFcLong( 0x0 ),	/* 0 */
/* 612 */	NdrFcShort( 0x7 ),	/* 7 */
/* 614 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 616 */	NdrFcShort( 0x18 ),	/* 24 */
/* 618 */	NdrFcShort( 0x24 ),	/* 36 */
/* 620 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 622 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 624 */	NdrFcShort( 0x1 ),	/* 1 */
/* 626 */	NdrFcShort( 0x0 ),	/* 0 */
/* 628 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 630 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 632 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 634 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 636 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 638 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 640 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter chvoMax */

/* 642 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 644 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 646 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pchvo */

/* 648 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 650 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 652 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prghvo */

/* 654 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 656 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 658 */	NdrFcShort( 0xa ),	/* Type Offset=10 */

	/* Return value */

/* 660 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 662 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BinaryPropRgb */

/* 666 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 668 */	NdrFcLong( 0x0 ),	/* 0 */
/* 672 */	NdrFcShort( 0x8 ),	/* 8 */
/* 674 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 676 */	NdrFcShort( 0x18 ),	/* 24 */
/* 678 */	NdrFcShort( 0x24 ),	/* 36 */
/* 680 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 682 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 684 */	NdrFcShort( 0x1 ),	/* 1 */
/* 686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 688 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 690 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 692 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 696 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 698 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgb */

/* 702 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 704 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 706 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter cbMax */

/* 708 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 710 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 712 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcb */

/* 714 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 716 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 718 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 720 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 722 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 724 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_GuidProp */

/* 726 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 728 */	NdrFcLong( 0x0 ),	/* 0 */
/* 732 */	NdrFcShort( 0x9 ),	/* 9 */
/* 734 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 736 */	NdrFcShort( 0x10 ),	/* 16 */
/* 738 */	NdrFcShort( 0x4c ),	/* 76 */
/* 740 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 742 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 744 */	NdrFcShort( 0x0 ),	/* 0 */
/* 746 */	NdrFcShort( 0x0 ),	/* 0 */
/* 748 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 750 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 752 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 754 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 756 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 758 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 760 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter puid */

/* 762 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 764 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 766 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 768 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 770 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 772 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IntProp */

/* 774 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 776 */	NdrFcLong( 0x0 ),	/* 0 */
/* 780 */	NdrFcShort( 0xa ),	/* 10 */
/* 782 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 784 */	NdrFcShort( 0x10 ),	/* 16 */
/* 786 */	NdrFcShort( 0x24 ),	/* 36 */
/* 788 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 790 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 792 */	NdrFcShort( 0x0 ),	/* 0 */
/* 794 */	NdrFcShort( 0x0 ),	/* 0 */
/* 796 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 798 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 800 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 802 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 804 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 806 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 808 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pn */

/* 810 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 812 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 814 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 816 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 818 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 820 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Int64Prop */

/* 822 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 824 */	NdrFcLong( 0x0 ),	/* 0 */
/* 828 */	NdrFcShort( 0xb ),	/* 11 */
/* 830 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 832 */	NdrFcShort( 0x10 ),	/* 16 */
/* 834 */	NdrFcShort( 0x2c ),	/* 44 */
/* 836 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 838 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 840 */	NdrFcShort( 0x0 ),	/* 0 */
/* 842 */	NdrFcShort( 0x0 ),	/* 0 */
/* 844 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 846 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 848 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 850 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 852 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 854 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 856 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter plln */

/* 858 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 860 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 862 */	0xb,		/* FC_HYPER */
			0x0,		/* 0 */

	/* Return value */

/* 864 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 866 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 868 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MultiStringAlt */

/* 870 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 872 */	NdrFcLong( 0x0 ),	/* 0 */
/* 876 */	NdrFcShort( 0xc ),	/* 12 */
/* 878 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 880 */	NdrFcShort( 0x18 ),	/* 24 */
/* 882 */	NdrFcShort( 0x8 ),	/* 8 */
/* 884 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 886 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 888 */	NdrFcShort( 0x0 ),	/* 0 */
/* 890 */	NdrFcShort( 0x0 ),	/* 0 */
/* 892 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 894 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 896 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 898 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 900 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 902 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 904 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 906 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 908 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 910 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 912 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 914 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 916 */	NdrFcShort( 0x4c ),	/* Type Offset=76 */

	/* Return value */

/* 918 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 920 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 922 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MultiStringProp */

/* 924 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 926 */	NdrFcLong( 0x0 ),	/* 0 */
/* 930 */	NdrFcShort( 0xd ),	/* 13 */
/* 932 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 934 */	NdrFcShort( 0x10 ),	/* 16 */
/* 936 */	NdrFcShort( 0x8 ),	/* 8 */
/* 938 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 940 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 942 */	NdrFcShort( 0x0 ),	/* 0 */
/* 944 */	NdrFcShort( 0x0 ),	/* 0 */
/* 946 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 948 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 950 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 952 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 954 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 956 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 958 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptms */

/* 960 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 962 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 964 */	NdrFcShort( 0x62 ),	/* Type Offset=98 */

	/* Return value */

/* 966 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 968 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Prop */

/* 972 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 974 */	NdrFcLong( 0x0 ),	/* 0 */
/* 978 */	NdrFcShort( 0xe ),	/* 14 */
/* 980 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 982 */	NdrFcShort( 0x10 ),	/* 16 */
/* 984 */	NdrFcShort( 0x8 ),	/* 8 */
/* 986 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 988 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 990 */	NdrFcShort( 0x20 ),	/* 32 */
/* 992 */	NdrFcShort( 0x0 ),	/* 0 */
/* 994 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 996 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 998 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1000 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1002 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1004 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1006 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvar */

/* 1008 */	NdrFcShort( 0x4113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=16 */
/* 1010 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1012 */	NdrFcShort( 0x468 ),	/* Type Offset=1128 */

	/* Return value */

/* 1014 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1016 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1018 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StringProp */

/* 1020 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1022 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1026 */	NdrFcShort( 0xf ),	/* 15 */
/* 1028 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1030 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1032 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1034 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 1036 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1038 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1040 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1042 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 1044 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1046 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1050 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1052 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1054 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 1056 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 1058 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1060 */	NdrFcShort( 0x4c ),	/* Type Offset=76 */

	/* Return value */

/* 1062 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1064 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1066 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_TimeProp */

/* 1068 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1070 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1074 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1076 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1078 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1080 */	NdrFcShort( 0x2c ),	/* 44 */
/* 1082 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 1084 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1086 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1088 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1090 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 1092 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1094 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1098 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1100 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1102 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptim */

/* 1104 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1106 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1108 */	0xb,		/* FC_HYPER */
			0x0,		/* 0 */

	/* Return value */

/* 1110 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1112 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1114 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_UnicodeProp */

/* 1116 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1118 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1122 */	NdrFcShort( 0x11 ),	/* 17 */
/* 1124 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1126 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1128 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1130 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 1132 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1134 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1136 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1138 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 1140 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1142 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1144 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1146 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1148 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1150 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 1152 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1154 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1156 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 1158 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1160 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1162 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_UnicodeProp */

/* 1164 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1166 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1170 */	NdrFcShort( 0x12 ),	/* 18 */
/* 1172 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1174 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1176 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1178 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 1180 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1182 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1184 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1186 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 1188 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1190 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1192 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1194 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1196 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1198 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */

/* 1200 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1202 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1204 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 1206 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1208 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1210 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure UnicodePropRgch */

/* 1212 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1214 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1218 */	NdrFcShort( 0x13 ),	/* 19 */
/* 1220 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 1222 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1224 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1226 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 1228 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1230 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1232 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1234 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 1236 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1238 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1240 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1242 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1244 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 1248 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 1250 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1252 */	NdrFcShort( 0x492 ),	/* Type Offset=1170 */

	/* Parameter cchMax */

/* 1254 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1256 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1258 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcch */

/* 1260 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1262 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1264 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1266 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1268 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1270 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BeginUndoTask */

/* 1272 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1274 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1278 */	NdrFcShort( 0x15 ),	/* 21 */
/* 1280 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1282 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1284 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1286 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 1288 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1290 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1292 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1294 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrUndo */

/* 1296 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1298 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1300 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter bstrRedo */

/* 1302 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1304 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1306 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 1308 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1310 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1312 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RequestAbort */


	/* Procedure EndUndoTask */

/* 1314 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1316 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1320 */	NdrFcShort( 0x16 ),	/* 22 */
/* 1322 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1324 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1326 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1328 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1330 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1332 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1336 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 1338 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1340 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1342 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Install */


	/* Procedure AbortDoc */


	/* Procedure ContinueUndoTask */

/* 1344 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1346 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1350 */	NdrFcShort( 0x17 ),	/* 23 */
/* 1352 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1354 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1356 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1358 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1360 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1362 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1364 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1366 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 1368 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1370 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1372 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure EndOuterUndoTask */

/* 1374 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1376 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1380 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1382 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1384 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1386 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1388 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1390 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1392 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1394 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1396 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 1398 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1400 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1402 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BreakUndoTask */

/* 1404 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1406 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1410 */	NdrFcShort( 0x19 ),	/* 25 */
/* 1412 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1416 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1418 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 1420 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1422 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1424 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1426 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrUndo */

/* 1428 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1430 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1432 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter bstrRedo */

/* 1434 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1436 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1438 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 1440 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1442 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1444 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetActionHandler */

/* 1446 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1448 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1452 */	NdrFcShort( 0x1a ),	/* 26 */
/* 1454 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1456 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1458 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1460 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 1462 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1464 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1466 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1468 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppacth */

/* 1470 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 1472 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1474 */	NdrFcShort( 0x4a4 ),	/* Type Offset=1188 */

	/* Return value */

/* 1476 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1478 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1480 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetActionHandler */

/* 1482 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1484 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1488 */	NdrFcShort( 0x1b ),	/* 27 */
/* 1490 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1492 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1494 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1496 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 1498 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1500 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1502 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1504 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pacth */

/* 1506 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 1508 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1510 */	NdrFcShort( 0x4a8 ),	/* Type Offset=1192 */

	/* Return value */

/* 1512 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1514 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1516 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeleteObj */

/* 1518 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1520 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1524 */	NdrFcShort( 0x1c ),	/* 28 */
/* 1526 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1528 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1530 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1532 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1534 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1536 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1538 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1540 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoObj */

/* 1542 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1544 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1546 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1548 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1550 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1552 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddSimpleRect */


	/* Procedure DeleteObjOwner */

/* 1554 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1556 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1560 */	NdrFcShort( 0x1d ),	/* 29 */
/* 1562 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1564 */	NdrFcShort( 0x20 ),	/* 32 */
/* 1566 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1568 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 1570 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1572 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1574 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1576 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter rgb */


	/* Parameter hvoOwner */

/* 1578 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1580 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1582 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dmpWidth */


	/* Parameter hvoObj */

/* 1584 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1586 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1588 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dmpHeight */


	/* Parameter tag */

/* 1590 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1592 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1594 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dmpBaselineOffset */


	/* Parameter ihvo */

/* 1596 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1598 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 1602 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1604 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1606 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InsertNew */

/* 1608 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1610 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1614 */	NdrFcShort( 0x1e ),	/* 30 */
/* 1616 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 1618 */	NdrFcShort( 0x20 ),	/* 32 */
/* 1620 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1622 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 1624 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1626 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1628 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1630 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoObj */

/* 1632 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1634 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1636 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1638 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1640 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1642 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvo */

/* 1644 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1646 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1648 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter chvo */

/* 1650 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1652 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1654 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pss */

/* 1656 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 1658 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1660 */	NdrFcShort( 0x4ba ),	/* Type Offset=1210 */

	/* Return value */

/* 1662 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1664 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1666 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeNewObject */

/* 1668 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1670 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1674 */	NdrFcShort( 0x1f ),	/* 31 */
/* 1676 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 1678 */	NdrFcShort( 0x20 ),	/* 32 */
/* 1680 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1682 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 1684 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1688 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1690 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter clid */

/* 1692 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1694 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1696 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoOwner */

/* 1698 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1700 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1702 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1704 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1706 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1708 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ord */

/* 1710 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1712 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvoNew */

/* 1716 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1718 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1722 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1724 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1726 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MoveOwnSeq */

/* 1728 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1730 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1734 */	NdrFcShort( 0x20 ),	/* 32 */
/* 1736 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 1738 */	NdrFcShort( 0x38 ),	/* 56 */
/* 1740 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1742 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x8,		/* 8 */
/* 1744 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1746 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1748 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1750 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoSrcOwner */

/* 1752 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1754 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1756 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tagSrc */

/* 1758 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1760 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1762 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoStart */

/* 1764 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1766 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1768 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoEnd */

/* 1770 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1772 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1774 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoDstOwner */

/* 1776 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1778 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tagDst */

/* 1782 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1784 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1786 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoDstStart */

/* 1788 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1790 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 1792 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1794 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1796 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 1798 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Replace */

/* 1800 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1802 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1806 */	NdrFcShort( 0x21 ),	/* 33 */
/* 1808 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 1810 */	NdrFcShort( 0x28 ),	/* 40 */
/* 1812 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1814 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 1816 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1818 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1820 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1822 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoObj */

/* 1824 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1826 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1828 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1830 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1832 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1834 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoMin */

/* 1836 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1838 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1840 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoLim */

/* 1842 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1844 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1846 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prghvo */

/* 1848 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 1850 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1852 */	NdrFcShort( 0x4d0 ),	/* Type Offset=1232 */

	/* Parameter chvo */

/* 1854 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1856 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1858 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1860 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1862 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 1864 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetObjProp */

/* 1866 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1868 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1872 */	NdrFcShort( 0x22 ),	/* 34 */
/* 1874 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1876 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1878 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1880 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 1882 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1884 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1886 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1888 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 1890 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1892 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1894 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1896 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1898 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1900 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoObj */

/* 1902 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1904 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1906 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1908 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1910 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1912 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RemoveObjRefs */

/* 1914 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1916 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1920 */	NdrFcShort( 0x23 ),	/* 35 */
/* 1922 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1924 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1926 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1928 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1930 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1932 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1934 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1936 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 1938 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1940 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1942 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1944 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1946 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1948 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetBinary */

/* 1950 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1952 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1956 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1958 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1960 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1962 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1964 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 1966 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1970 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1972 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 1974 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1976 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1978 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 1980 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1982 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1984 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgb */

/* 1986 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 1988 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1990 */	NdrFcShort( 0x4e0 ),	/* Type Offset=1248 */

	/* Parameter cb */

/* 1992 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1994 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1996 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1998 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2000 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2002 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetGuid */

/* 2004 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2006 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2010 */	NdrFcShort( 0x25 ),	/* 37 */
/* 2012 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 2014 */	NdrFcShort( 0x40 ),	/* 64 */
/* 2016 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2018 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 2020 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2022 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2024 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2026 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2028 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2030 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2032 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2034 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2036 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2038 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter uid */

/* 2040 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 2042 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2044 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 2046 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2048 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 2050 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetInt */

/* 2052 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2054 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2058 */	NdrFcShort( 0x26 ),	/* 38 */
/* 2060 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2062 */	NdrFcShort( 0x18 ),	/* 24 */
/* 2064 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2066 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 2068 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2070 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2072 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2074 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2076 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2078 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2080 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2082 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2084 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2086 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter n */

/* 2088 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2090 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2092 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2094 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2096 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2098 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetInt64 */

/* 2100 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2102 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2106 */	NdrFcShort( 0x27 ),	/* 39 */
/* 2108 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2110 */	NdrFcShort( 0x20 ),	/* 32 */
/* 2112 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2114 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 2116 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2120 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2122 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2124 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2126 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2128 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2130 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2132 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2134 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter lln */

/* 2136 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2138 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2140 */	0xb,		/* FC_HYPER */
			0x0,		/* 0 */

	/* Return value */

/* 2142 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2144 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2146 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetMultiStringAlt */

/* 2148 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2150 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2154 */	NdrFcShort( 0x28 ),	/* 40 */
/* 2156 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2158 */	NdrFcShort( 0x18 ),	/* 24 */
/* 2160 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2162 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 2164 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2166 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2168 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2170 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2172 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2174 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2176 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2178 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2180 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2182 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 2184 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2186 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2188 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 2190 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2192 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2194 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Return value */

/* 2196 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2198 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2200 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetString */

/* 2202 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2204 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2208 */	NdrFcShort( 0x29 ),	/* 41 */
/* 2210 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2212 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2214 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2216 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 2218 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2220 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2222 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2224 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2226 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2228 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2230 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2232 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2234 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2236 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 2238 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2240 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2242 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Return value */

/* 2244 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2246 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2248 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetTime */

/* 2250 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2252 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2256 */	NdrFcShort( 0x2a ),	/* 42 */
/* 2258 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2260 */	NdrFcShort( 0x20 ),	/* 32 */
/* 2262 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2264 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 2266 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2272 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2274 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2276 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2280 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2282 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2284 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter lln */

/* 2286 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2288 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2290 */	0xb,		/* FC_HYPER */
			0x0,		/* 0 */

	/* Return value */

/* 2292 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2294 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2296 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetUnicode */

/* 2298 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2300 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2304 */	NdrFcShort( 0x2b ),	/* 43 */
/* 2306 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2308 */	NdrFcShort( 0x18 ),	/* 24 */
/* 2310 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2312 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 2314 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2318 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2320 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2322 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2324 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2326 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2328 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2330 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2332 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 2334 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 2336 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2338 */	NdrFcShort( 0x4f0 ),	/* Type Offset=1264 */

	/* Parameter cch */

/* 2340 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2342 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2344 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2346 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2348 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2350 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetUnknown */

/* 2352 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2354 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2358 */	NdrFcShort( 0x2c ),	/* 44 */
/* 2360 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2362 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2364 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2366 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 2368 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2374 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2376 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2378 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2380 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2382 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2384 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2386 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter punk */

/* 2388 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2390 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2392 */	NdrFcShort( 0x1ca ),	/* Type Offset=458 */

	/* Return value */

/* 2394 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2396 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2398 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddNotification */

/* 2400 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2402 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2406 */	NdrFcShort( 0x2d ),	/* 45 */
/* 2408 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2410 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2412 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2414 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2416 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2418 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2420 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2422 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnchng */

/* 2424 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2426 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2428 */	NdrFcShort( 0x4fc ),	/* Type Offset=1276 */

	/* Return value */

/* 2430 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2432 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2434 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PropChanged */

/* 2436 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2438 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2442 */	NdrFcShort( 0x2e ),	/* 46 */
/* 2444 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 2446 */	NdrFcShort( 0x30 ),	/* 48 */
/* 2448 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2450 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 2452 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2454 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2456 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2458 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnchng */

/* 2460 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2462 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2464 */	NdrFcShort( 0x4fc ),	/* Type Offset=1276 */

	/* Parameter pct */

/* 2466 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2468 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvo */

/* 2472 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2474 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2476 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2478 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2480 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2482 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ivMin */

/* 2484 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2486 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2488 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvIns */

/* 2490 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2492 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2494 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvDel */

/* 2496 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2498 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 2500 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2502 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2504 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 2506 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RemoveNotification */

/* 2508 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2510 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2514 */	NdrFcShort( 0x2f ),	/* 47 */
/* 2516 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2520 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2522 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2524 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2526 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2528 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2530 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnchng */

/* 2532 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2534 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2536 */	NdrFcShort( 0x4fc ),	/* Type Offset=1276 */

	/* Return value */

/* 2538 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2540 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2542 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WritingSystemFactory */

/* 2544 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2546 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2550 */	NdrFcShort( 0x30 ),	/* 48 */
/* 2552 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2554 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2556 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2558 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2560 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2562 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2564 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2566 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppwsf */

/* 2568 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 2570 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2572 */	NdrFcShort( 0x50e ),	/* Type Offset=1294 */

	/* Return value */

/* 2574 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2576 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2578 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_WritingSystemFactory */

/* 2580 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2582 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2586 */	NdrFcShort( 0x31 ),	/* 49 */
/* 2588 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2590 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2592 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2594 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2596 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2598 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2600 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2602 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 2604 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2606 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2608 */	NdrFcShort( 0x512 ),	/* Type Offset=1298 */

	/* Return value */

/* 2610 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2612 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2614 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WritingSystemsOfInterest */

/* 2616 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2618 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2622 */	NdrFcShort( 0x32 ),	/* 50 */
/* 2624 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2626 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2628 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2630 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 2632 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2634 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2636 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2638 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cwsMax */

/* 2640 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2642 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2644 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 2646 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 2648 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2650 */	NdrFcShort( 0x528 ),	/* Type Offset=1320 */

	/* Parameter pcws */

/* 2652 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2654 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2656 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2658 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2660 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2662 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InsertRelExtra */

/* 2664 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2666 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2670 */	NdrFcShort( 0x33 ),	/* 51 */
/* 2672 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 2674 */	NdrFcShort( 0x20 ),	/* 32 */
/* 2676 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2678 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 2680 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2682 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2684 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2686 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoSrc */

/* 2688 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2690 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2692 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2694 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2696 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2698 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvo */

/* 2700 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2702 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2704 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoDst */

/* 2706 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2708 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2710 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrExtra */

/* 2712 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2714 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2716 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 2718 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2720 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2722 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure UpdateRelExtra */

/* 2724 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2726 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2730 */	NdrFcShort( 0x34 ),	/* 52 */
/* 2732 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2734 */	NdrFcShort( 0x18 ),	/* 24 */
/* 2736 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2738 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 2740 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2742 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2744 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2746 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoSrc */

/* 2748 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2750 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2752 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2754 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2756 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2758 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvo */

/* 2760 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2762 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2764 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrExtra */

/* 2766 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2768 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2770 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 2772 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2774 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2776 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetRelExtra */

/* 2778 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2780 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2784 */	NdrFcShort( 0x35 ),	/* 53 */
/* 2786 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2788 */	NdrFcShort( 0x18 ),	/* 24 */
/* 2790 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2792 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 2794 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2796 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2800 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoSrc */

/* 2802 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2804 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2806 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2808 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2810 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2812 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvo */

/* 2814 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2816 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2818 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrExtra */

/* 2820 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 2822 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2824 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 2826 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2828 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2830 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsPropInCache */

/* 2832 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2834 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2838 */	NdrFcShort( 0x36 ),	/* 54 */
/* 2840 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 2842 */	NdrFcShort( 0x20 ),	/* 32 */
/* 2844 */	NdrFcShort( 0x22 ),	/* 34 */
/* 2846 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 2848 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2850 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2852 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2854 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 2856 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2858 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2860 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 2862 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2864 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2866 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cpt */

/* 2868 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2870 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2872 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 2874 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2876 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2878 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfCached */

/* 2880 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2882 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2884 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 2886 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2888 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2890 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsDirty */

/* 2892 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2894 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2898 */	NdrFcShort( 0x37 ),	/* 55 */
/* 2900 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2904 */	NdrFcShort( 0x22 ),	/* 34 */
/* 2906 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2908 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2910 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2912 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2914 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pf */

/* 2916 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2918 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2920 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 2922 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2924 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2926 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseTableFooter */


	/* Procedure DrawingErrors */


	/* Procedure ClearDirty */

/* 2928 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2930 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2934 */	NdrFcShort( 0x38 ),	/* 56 */
/* 2936 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2938 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2940 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2942 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 2944 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2948 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2950 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 2952 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2954 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2956 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MetaDataCache */

/* 2958 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2960 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2964 */	NdrFcShort( 0x39 ),	/* 57 */
/* 2966 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2970 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2972 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2974 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2976 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2978 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2980 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppmdc */

/* 2982 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 2984 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2986 */	NdrFcShort( 0x538 ),	/* Type Offset=1336 */

	/* Return value */

/* 2988 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2990 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2992 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_MetaDataCache */

/* 2994 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2996 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3000 */	NdrFcShort( 0x3a ),	/* 58 */
/* 3002 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3004 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3006 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3008 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 3010 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3012 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3014 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3016 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pmdc */

/* 3018 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3020 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3022 */	NdrFcShort( 0x53c ),	/* Type Offset=1340 */

	/* Return value */

/* 3024 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3026 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3028 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheObjProp */

/* 3030 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3032 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3036 */	NdrFcShort( 0x3 ),	/* 3 */
/* 3038 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3040 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3042 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3044 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 3046 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3048 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3050 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3052 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3054 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3056 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3058 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3060 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3062 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3064 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter val */

/* 3066 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3068 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3070 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3072 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3074 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3076 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheVecProp */

/* 3078 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3080 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3084 */	NdrFcShort( 0x4 ),	/* 4 */
/* 3086 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3088 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3090 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3092 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 3094 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 3096 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3098 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3100 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3102 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3104 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3106 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3108 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3110 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rghvo */

/* 3114 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3116 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3118 */	NdrFcShort( 0x54e ),	/* Type Offset=1358 */

	/* Parameter chvo */

/* 3120 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3122 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3126 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3128 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3130 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheReplace */

/* 3132 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3134 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3138 */	NdrFcShort( 0x5 ),	/* 5 */
/* 3140 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 3142 */	NdrFcShort( 0x28 ),	/* 40 */
/* 3144 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3146 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 3148 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 3150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3152 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3154 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoObj */

/* 3156 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3158 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3160 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3162 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3164 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3166 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoMin */

/* 3168 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3170 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoLim */

/* 3174 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3176 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3178 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prghvo */

/* 3180 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3182 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3184 */	NdrFcShort( 0x4d0 ),	/* Type Offset=1232 */

	/* Parameter chvo */

/* 3186 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3188 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3192 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3194 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 3196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheBinaryProp */

/* 3198 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3200 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3204 */	NdrFcShort( 0x6 ),	/* 6 */
/* 3206 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3208 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3210 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3212 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 3214 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 3216 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3218 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3220 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3222 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3224 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3226 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3228 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3230 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3232 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgb */

/* 3234 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 3236 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3238 */	NdrFcShort( 0x4e0 ),	/* Type Offset=1248 */

	/* Parameter cb */

/* 3240 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3242 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3244 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3246 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3248 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3250 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheGuidProp */

/* 3252 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3254 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3258 */	NdrFcShort( 0x7 ),	/* 7 */
/* 3260 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 3262 */	NdrFcShort( 0x40 ),	/* 64 */
/* 3264 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3266 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 3268 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3272 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3274 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3276 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3278 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3280 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3282 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3284 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3286 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter uid */

/* 3288 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 3290 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3292 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 3294 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3296 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 3298 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheInt64Prop */

/* 3300 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3302 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3306 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3308 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3310 */	NdrFcShort( 0x20 ),	/* 32 */
/* 3312 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3314 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 3316 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3318 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3320 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3322 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3324 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3326 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3328 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3330 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3332 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3334 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter val */

/* 3336 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3338 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3340 */	0xb,		/* FC_HYPER */
			0x0,		/* 0 */

	/* Return value */

/* 3342 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3344 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3346 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheIntProp */

/* 3348 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3350 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3354 */	NdrFcShort( 0x9 ),	/* 9 */
/* 3356 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3358 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3360 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3362 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 3364 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3366 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3370 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3372 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3374 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3376 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3378 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3380 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3382 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter val */

/* 3384 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3386 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3388 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3390 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3392 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3394 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheStringAlt */

/* 3396 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3398 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3402 */	NdrFcShort( 0xa ),	/* 10 */
/* 3404 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3406 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3408 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3410 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 3412 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3416 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3418 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3420 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3422 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3424 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3426 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3428 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3430 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 3432 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3434 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3436 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 3438 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3440 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3442 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Return value */

/* 3444 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3446 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3448 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheStringFields */

/* 3450 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3452 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3456 */	NdrFcShort( 0xb ),	/* 11 */
/* 3458 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 3460 */	NdrFcShort( 0x20 ),	/* 32 */
/* 3462 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3464 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 3466 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 3468 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3470 */	NdrFcShort( 0x2 ),	/* 2 */
/* 3472 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3474 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3476 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3478 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3480 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3482 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3484 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchTxt */

/* 3486 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 3488 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3490 */	NdrFcShort( 0x4f0 ),	/* Type Offset=1264 */

	/* Parameter cchTxt */

/* 3492 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3494 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgbFmt */

/* 3498 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 3500 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3502 */	NdrFcShort( 0x55e ),	/* Type Offset=1374 */

	/* Parameter cbFmt */

/* 3504 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3506 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3510 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3512 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 3514 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheStringProp */

/* 3516 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3518 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3522 */	NdrFcShort( 0xc ),	/* 12 */
/* 3524 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3526 */	NdrFcShort( 0x10 ),	/* 16 */
/* 3528 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3530 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 3532 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3534 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3536 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3538 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3540 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3542 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3544 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3546 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3548 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3550 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 3552 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3554 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3556 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Return value */

/* 3558 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3560 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3562 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheTimeProp */

/* 3564 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3566 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3570 */	NdrFcShort( 0xd ),	/* 13 */
/* 3572 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3574 */	NdrFcShort( 0x28 ),	/* 40 */
/* 3576 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3578 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 3580 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3582 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3584 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3586 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 3588 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3590 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3594 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3596 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter val */

/* 3600 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 3602 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3604 */	NdrFcShort( 0x1aa ),	/* Type Offset=426 */

	/* Return value */

/* 3606 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3608 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3610 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheUnicodeProp */

/* 3612 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3614 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3618 */	NdrFcShort( 0xe ),	/* 14 */
/* 3620 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3622 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3624 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3626 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 3628 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 3630 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3632 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3634 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3636 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3638 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3640 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3642 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3644 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3646 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 3648 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 3650 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3652 */	NdrFcShort( 0x4f0 ),	/* Type Offset=1264 */

	/* Parameter cch */

/* 3654 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3656 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3658 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3660 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3662 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheUnknown */

/* 3666 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3668 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3672 */	NdrFcShort( 0xf ),	/* 15 */
/* 3674 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3676 */	NdrFcShort( 0x10 ),	/* 16 */
/* 3678 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3680 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 3682 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3684 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3688 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3690 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3692 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3696 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3698 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter punk */

/* 3702 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3704 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3706 */	NdrFcShort( 0x1ca ),	/* Type Offset=458 */

	/* Return value */

/* 3708 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3710 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3712 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NewObject */

/* 3714 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3716 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3720 */	NdrFcShort( 0x10 ),	/* 16 */
/* 3722 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 3724 */	NdrFcShort( 0x20 ),	/* 32 */
/* 3726 */	NdrFcShort( 0x24 ),	/* 36 */
/* 3728 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 3730 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3732 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3734 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3736 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter clid */

/* 3738 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3740 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3742 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoOwner */

/* 3744 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3746 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3748 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3750 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3752 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3754 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ord */

/* 3756 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3758 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3760 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvoNew */

/* 3762 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3764 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3766 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3768 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3770 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3772 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetObjIndex */

/* 3774 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3776 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3780 */	NdrFcShort( 0x11 ),	/* 17 */
/* 3782 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3784 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3786 */	NdrFcShort( 0x24 ),	/* 36 */
/* 3788 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 3790 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3792 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3794 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3796 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoOwn */

/* 3798 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3800 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3802 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter flid */

/* 3804 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3806 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3808 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvo */

/* 3810 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3812 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3814 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvo */

/* 3816 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3818 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3820 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3822 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3824 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetOutlineNumber */

/* 3828 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3830 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3834 */	NdrFcShort( 0x12 ),	/* 18 */
/* 3836 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3838 */	NdrFcShort( 0x16 ),	/* 22 */
/* 3840 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3842 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 3844 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 3846 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3848 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3850 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 3852 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3854 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3856 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter flid */

/* 3858 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3860 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3862 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fFinPer */

/* 3864 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3866 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3868 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 3870 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 3872 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3874 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 3876 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3878 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3880 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ClearInfoAbout */

/* 3882 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3884 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3888 */	NdrFcShort( 0x13 ),	/* 19 */
/* 3890 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3892 */	NdrFcShort( 0xe ),	/* 14 */
/* 3894 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3896 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 3898 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3900 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3904 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 3906 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3908 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3910 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fIncludeOwnedObjects */

/* 3912 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3914 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3916 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 3918 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3920 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3922 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CachedIntProp */

/* 3924 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3926 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3930 */	NdrFcShort( 0x14 ),	/* 20 */
/* 3932 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3934 */	NdrFcShort( 0x10 ),	/* 16 */
/* 3936 */	NdrFcShort( 0x3e ),	/* 62 */
/* 3938 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 3940 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3942 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3944 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3946 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter obj */

/* 3948 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3950 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3952 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 3954 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3956 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3958 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pf */

/* 3960 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3962 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3964 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pn */

/* 3966 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3968 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3972 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3974 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3976 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ClearAllData */

/* 3978 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3980 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3984 */	NdrFcShort( 0x15 ),	/* 21 */
/* 3986 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3988 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3990 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3992 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 3994 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3996 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3998 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4000 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 4002 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4004 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4006 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InstallVirtual */

/* 4008 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4010 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4014 */	NdrFcShort( 0x16 ),	/* 22 */
/* 4016 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4018 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4020 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4022 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 4024 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4028 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4030 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvh */

/* 4032 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4034 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4036 */	NdrFcShort( 0x56a ),	/* Type Offset=1386 */

	/* Return value */

/* 4038 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4040 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4042 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetVirtualHandlerId */

/* 4044 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4046 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4050 */	NdrFcShort( 0x17 ),	/* 23 */
/* 4052 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4054 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4056 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4058 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 4060 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4062 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4064 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4066 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 4068 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4070 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4072 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppvh */

/* 4074 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4076 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4078 */	NdrFcShort( 0x57c ),	/* Type Offset=1404 */

	/* Return value */

/* 4080 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4082 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4084 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetVirtualHandlerName */

/* 4086 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4088 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4092 */	NdrFcShort( 0x18 ),	/* 24 */
/* 4094 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4096 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4098 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4100 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 4102 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4104 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4106 */	NdrFcShort( 0x2 ),	/* 2 */
/* 4108 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrClass */

/* 4110 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 4112 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4114 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter bstrField */

/* 4116 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 4118 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4120 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter ppvh */

/* 4122 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4124 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4126 */	NdrFcShort( 0x57c ),	/* Type Offset=1404 */

	/* Return value */

/* 4128 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4130 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4132 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ClearVirtualProperties */

/* 4134 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4136 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4140 */	NdrFcShort( 0x19 ),	/* 25 */
/* 4142 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4144 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4146 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4148 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 4150 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4152 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4154 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4156 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 4158 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4160 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4162 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CreateDummyID */

/* 4164 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4166 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4170 */	NdrFcShort( 0x3 ),	/* 3 */
/* 4172 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4174 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4176 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4178 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 4180 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4182 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4184 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4186 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phvo */

/* 4188 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4190 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4192 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4194 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4196 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4198 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Load */

/* 4200 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4202 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4206 */	NdrFcShort( 0x4 ),	/* 4 */
/* 4208 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 4210 */	NdrFcShort( 0x16 ),	/* 22 */
/* 4212 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4214 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 4216 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4218 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4220 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4222 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSqlStmt */

/* 4224 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 4226 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4228 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pdcs */

/* 4230 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4232 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4234 */	NdrFcShort( 0x580 ),	/* Type Offset=1408 */

	/* Parameter hvoBase */

/* 4236 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4238 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4240 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nrowMax */

/* 4242 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4244 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter padvi */

/* 4248 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4250 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4252 */	NdrFcShort( 0x592 ),	/* Type Offset=1426 */

	/* Parameter fNotifyChange */

/* 4254 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4256 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4258 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 4260 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4262 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 4264 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ShowWindow */


	/* Procedure Save */

/* 4266 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4268 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4272 */	NdrFcShort( 0x5 ),	/* 5 */
/* 4274 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4276 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4278 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4280 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 4282 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4284 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4286 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4288 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 4290 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4292 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4294 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Clear */

/* 4296 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4298 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4302 */	NdrFcShort( 0x6 ),	/* 6 */
/* 4304 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4306 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4308 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4310 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 4312 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4314 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4318 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 4320 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4322 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4324 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Tag */


	/* Procedure CheckTimeStamp */

/* 4326 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4328 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4332 */	NdrFcShort( 0x7 ),	/* 7 */
/* 4334 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4336 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4338 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4340 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 4342 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4346 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4348 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */


	/* Parameter hvo */

/* 4350 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4352 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4354 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 4356 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4358 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4360 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_PossListId */


	/* Procedure SetTimeStamp */

/* 4362 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4364 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4368 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4370 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4372 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4374 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4376 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 4378 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4380 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4382 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4384 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psslId */


	/* Parameter hvo */

/* 4386 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4388 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4390 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 4392 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4394 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4396 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Type */


	/* Procedure CacheCurrTimeStamp */

/* 4398 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4400 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4404 */	NdrFcShort( 0x9 ),	/* 9 */
/* 4406 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4408 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4410 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4412 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 4414 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4416 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4418 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4420 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cpt */


	/* Parameter hvo */

/* 4422 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4424 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4426 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 4428 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4430 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4432 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RollbackLayoutObjects */


	/* Procedure CacheCurrTimeStampAndOwner */

/* 4434 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4436 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4440 */	NdrFcShort( 0xa ),	/* 10 */
/* 4442 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4444 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4446 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4448 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 4450 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4452 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4454 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4456 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hPage */


	/* Parameter hvo */

/* 4458 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4460 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4462 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 4464 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4466 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4468 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Close */

/* 4470 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4472 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4476 */	NdrFcShort( 0xb ),	/* 11 */
/* 4478 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4480 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4482 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4484 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 4486 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4488 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4490 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4492 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 4494 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4496 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4498 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ObjOwner */

/* 4500 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4502 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4506 */	NdrFcShort( 0xc ),	/* 12 */
/* 4508 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4510 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4512 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4514 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 4516 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4520 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4522 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 4524 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4526 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4528 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvoOwn */

/* 4530 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4532 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4534 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4536 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4538 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4540 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ObjClid */

/* 4542 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4544 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4548 */	NdrFcShort( 0xd ),	/* 13 */
/* 4550 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4552 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4554 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4556 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 4558 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4560 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4562 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4564 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 4566 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4568 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4570 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclid */

/* 4572 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4574 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4576 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4578 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4580 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4582 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ObjOwnFlid */

/* 4584 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4586 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4590 */	NdrFcShort( 0xe ),	/* 14 */
/* 4592 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4594 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4596 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4598 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 4600 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4602 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4604 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4606 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 4608 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4610 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4612 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pflidOwn */

/* 4614 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4616 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4618 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4620 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4622 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4624 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LoadData */

/* 4626 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4628 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4632 */	NdrFcShort( 0xf ),	/* 15 */
/* 4634 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 4636 */	NdrFcShort( 0xe ),	/* 14 */
/* 4638 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4640 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 4642 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4646 */	NdrFcShort( 0x2 ),	/* 2 */
/* 4648 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prghvo */

/* 4650 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4652 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4654 */	NdrFcShort( 0x5a8 ),	/* Type Offset=1448 */

	/* Parameter prgclsid */

/* 4656 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4658 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4660 */	NdrFcShort( 0x5a8 ),	/* Type Offset=1448 */

	/* Parameter chvo */

/* 4662 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4664 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4666 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdts */

/* 4668 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4670 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4672 */	NdrFcShort( 0x5b4 ),	/* Type Offset=1460 */

	/* Parameter padvi */

/* 4674 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4676 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4678 */	NdrFcShort( 0x592 ),	/* Type Offset=1426 */

	/* Parameter fIncludeOwnedObjects */

/* 4680 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4682 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4684 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 4686 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4688 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 4690 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure UpdatePropIfCached */

/* 4692 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4694 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4698 */	NdrFcShort( 0x10 ),	/* 16 */
/* 4700 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4702 */	NdrFcShort( 0x20 ),	/* 32 */
/* 4704 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4706 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 4708 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4710 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4712 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4714 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 4716 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4718 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 4722 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4724 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4726 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cpt */

/* 4728 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4730 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4732 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 4734 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4736 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4738 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4740 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4742 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4744 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIdFromGuid */

/* 4746 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4748 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4752 */	NdrFcShort( 0x11 ),	/* 17 */
/* 4754 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4756 */	NdrFcShort( 0x44 ),	/* 68 */
/* 4758 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4760 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 4762 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4768 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter puid */

/* 4770 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 4772 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4774 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Parameter phvo */

/* 4776 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4778 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4782 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4784 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4786 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 4788 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4790 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4794 */	NdrFcShort( 0x3 ),	/* 3 */
/* 4796 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4800 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4802 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 4804 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4806 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4808 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4810 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 4812 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4814 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4816 */	NdrFcShort( 0x5ca ),	/* Type Offset=1482 */

	/* Parameter pmdc */

/* 4818 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4820 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4822 */	NdrFcShort( 0x5ca ),	/* Type Offset=1482 */

	/* Parameter pwsf */

/* 4824 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4826 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4828 */	NdrFcShort( 0x5ca ),	/* Type Offset=1482 */

	/* Parameter pacth */

/* 4830 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4832 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4834 */	NdrFcShort( 0x5dc ),	/* Type Offset=1500 */

	/* Return value */

/* 4836 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4838 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4840 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetOleDbEncap */

/* 4842 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4844 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4848 */	NdrFcShort( 0x4 ),	/* 4 */
/* 4850 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4852 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4854 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4856 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 4858 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4860 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4862 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4864 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppode */

/* 4866 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4868 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4870 */	NdrFcShort( 0x5ee ),	/* Type Offset=1518 */

	/* Return value */

/* 4872 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4874 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4876 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetSite */

/* 4878 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4880 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4884 */	NdrFcShort( 0x4 ),	/* 4 */
/* 4886 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4888 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4890 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4892 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 4894 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4896 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4898 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4900 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvrs */

/* 4902 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4904 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4906 */	NdrFcShort( 0x5f2 ),	/* Type Offset=1522 */

	/* Return value */

/* 4908 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4910 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4912 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_DataAccess */

/* 4914 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4916 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4920 */	NdrFcShort( 0x5 ),	/* 5 */
/* 4922 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4926 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4928 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 4930 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4932 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4934 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4936 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psda */

/* 4938 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4940 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4942 */	NdrFcShort( 0x604 ),	/* Type Offset=1540 */

	/* Return value */

/* 4944 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4946 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4948 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DataAccess */

/* 4950 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4952 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4956 */	NdrFcShort( 0x6 ),	/* 6 */
/* 4958 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4960 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4962 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4964 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 4966 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4970 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4972 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsda */

/* 4974 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4976 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4978 */	NdrFcShort( 0x616 ),	/* Type Offset=1558 */

	/* Return value */

/* 4980 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4982 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4984 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetRootObjects */

/* 4986 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4988 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4992 */	NdrFcShort( 0x7 ),	/* 7 */
/* 4994 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 4996 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4998 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5000 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 5002 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5004 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5006 */	NdrFcShort( 0x3 ),	/* 3 */
/* 5008 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prghvo */

/* 5010 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5012 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5014 */	NdrFcShort( 0x61e ),	/* Type Offset=1566 */

	/* Parameter prgpvwvc */

/* 5016 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5018 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5020 */	NdrFcShort( 0x640 ),	/* Type Offset=1600 */

	/* Parameter prgfrag */

/* 5022 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5024 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5026 */	NdrFcShort( 0x61e ),	/* Type Offset=1566 */

	/* Parameter pss */

/* 5028 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5030 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5032 */	NdrFcShort( 0x656 ),	/* Type Offset=1622 */

	/* Parameter chvo */

/* 5034 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5036 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5038 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 5040 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5042 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5044 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetRootObject */

/* 5046 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5048 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5052 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5054 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5056 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5058 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5060 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 5062 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5064 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5066 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5068 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 5070 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5072 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5074 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 5076 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5078 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5080 */	NdrFcShort( 0x62e ),	/* Type Offset=1582 */

	/* Parameter frag */

/* 5082 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5084 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5086 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pss */

/* 5088 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5090 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5092 */	NdrFcShort( 0x656 ),	/* Type Offset=1622 */

	/* Return value */

/* 5094 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5096 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5098 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetRootVariant */

/* 5100 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5102 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5106 */	NdrFcShort( 0x9 ),	/* 9 */
/* 5108 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 5110 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5112 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5114 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 5116 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5120 */	NdrFcShort( 0x20 ),	/* 32 */
/* 5122 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter v */

/* 5124 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 5126 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5128 */	NdrFcShort( 0x66c ),	/* Type Offset=1644 */

	/* Parameter pss */

/* 5130 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5132 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5134 */	NdrFcShort( 0x656 ),	/* Type Offset=1622 */

	/* Parameter pvwvc */

/* 5136 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5138 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5140 */	NdrFcShort( 0x62e ),	/* Type Offset=1582 */

	/* Parameter frag */

/* 5142 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5144 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5146 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 5148 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5150 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 5152 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetRootString */

/* 5154 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5156 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5160 */	NdrFcShort( 0xa ),	/* 10 */
/* 5162 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5164 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5166 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5168 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 5170 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5172 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5174 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5176 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptss */

/* 5178 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5180 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5182 */	NdrFcShort( 0x676 ),	/* Type Offset=1654 */

	/* Parameter pss */

/* 5184 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5186 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5188 */	NdrFcShort( 0x656 ),	/* Type Offset=1622 */

	/* Parameter pvwvc */

/* 5190 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5192 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5194 */	NdrFcShort( 0x62e ),	/* Type Offset=1582 */

	/* Parameter frag */

/* 5196 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5198 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5200 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 5202 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5204 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5206 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_Overlay */

/* 5208 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5210 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5214 */	NdrFcShort( 0xb ),	/* 11 */
/* 5216 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5218 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5220 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5222 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 5224 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5226 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5228 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5230 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvo */

/* 5232 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5234 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5236 */	NdrFcShort( 0x688 ),	/* Type Offset=1672 */

	/* Return value */

/* 5238 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5240 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5242 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Overlay */

/* 5244 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5246 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5250 */	NdrFcShort( 0xc ),	/* 12 */
/* 5252 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5254 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5256 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5258 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 5260 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5262 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5264 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5266 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppvo */

/* 5268 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5270 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5272 */	NdrFcShort( 0x69a ),	/* Type Offset=1690 */

	/* Return value */

/* 5274 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5276 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetRootVariant */

/* 5280 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5282 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5286 */	NdrFcShort( 0xd ),	/* 13 */
/* 5288 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5290 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5292 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5294 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 5296 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 5298 */	NdrFcShort( 0x20 ),	/* 32 */
/* 5300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5302 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pv */

/* 5304 */	NdrFcShort( 0x4113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=16 */
/* 5306 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5308 */	NdrFcShort( 0x468 ),	/* Type Offset=1128 */

	/* Return value */

/* 5310 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5312 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5314 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Serialize */

/* 5316 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5318 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5322 */	NdrFcShort( 0xe ),	/* 14 */
/* 5324 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5326 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5328 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5330 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 5332 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5336 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5338 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 5340 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5342 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5344 */	NdrFcShort( 0x69e ),	/* Type Offset=1694 */

	/* Return value */

/* 5346 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5348 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5350 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Deserialize */

/* 5352 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5354 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5358 */	NdrFcShort( 0xf ),	/* 15 */
/* 5360 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5362 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5364 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5366 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 5368 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5374 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 5376 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5378 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5380 */	NdrFcShort( 0x69e ),	/* Type Offset=1694 */

	/* Return value */

/* 5382 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5384 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5386 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteWpx */

/* 5388 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5390 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5394 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5396 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5398 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5400 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5402 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 5404 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5406 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5408 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5410 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 5412 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5414 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5416 */	NdrFcShort( 0x69e ),	/* Type Offset=1694 */

	/* Return value */

/* 5418 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5420 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5422 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Selection */

/* 5424 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5426 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5430 */	NdrFcShort( 0x11 ),	/* 17 */
/* 5432 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5434 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5436 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5438 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 5440 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5442 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5444 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5446 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsel */

/* 5448 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5450 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5452 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 5454 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5456 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5458 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DestroySelection */

/* 5460 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5462 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5466 */	NdrFcShort( 0x12 ),	/* 18 */
/* 5468 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5470 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5472 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5474 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 5476 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5478 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5480 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5482 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 5484 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5486 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5488 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeTextSelection */

/* 5490 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5492 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5496 */	NdrFcShort( 0x13 ),	/* 19 */
/* 5498 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 5500 */	NdrFcShort( 0x4c ),	/* 76 */
/* 5502 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5504 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0xe,		/* 14 */
/* 5506 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5508 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5510 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5512 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ihvoRoot */

/* 5514 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5516 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5518 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvlsi */

/* 5520 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5522 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5524 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsli */

/* 5526 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5528 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5530 */	NdrFcShort( 0x6d6 ),	/* Type Offset=1750 */

	/* Parameter tagTextProp */

/* 5532 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5534 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5536 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cpropPrevious */

/* 5538 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5540 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5542 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichAnchor */

/* 5544 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5546 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5548 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichEnd */

/* 5550 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5552 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5554 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 5556 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5558 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 5560 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fAssocPrev */

/* 5562 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5564 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 5566 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ihvoEnd */

/* 5568 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5570 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 5572 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pttpIns */

/* 5574 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5576 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 5578 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Parameter fInstall */

/* 5580 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5582 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 5584 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 5586 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5588 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 5590 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 5592 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5594 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 5596 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeRangeSelection */

/* 5598 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5600 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5604 */	NdrFcShort( 0x14 ),	/* 20 */
/* 5606 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5608 */	NdrFcShort( 0x6 ),	/* 6 */
/* 5610 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5612 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 5614 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5616 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5618 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5620 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pselAnchor */

/* 5622 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5624 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5626 */	NdrFcShort( 0x6b4 ),	/* Type Offset=1716 */

	/* Parameter pselEnd */

/* 5628 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5630 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5632 */	NdrFcShort( 0x6b4 ),	/* Type Offset=1716 */

	/* Parameter fInstall */

/* 5634 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5636 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5638 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 5640 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5642 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5644 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 5646 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5648 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5650 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeSimpleSel */

/* 5652 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5654 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5658 */	NdrFcShort( 0x15 ),	/* 21 */
/* 5660 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5662 */	NdrFcShort( 0x18 ),	/* 24 */
/* 5664 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5666 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 5668 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5670 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5672 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5674 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fInitial */

/* 5676 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5678 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5680 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fEdit */

/* 5682 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5684 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5686 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fRange */

/* 5688 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5690 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5692 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fInstall */

/* 5694 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5696 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5698 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 5700 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5702 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5704 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 5706 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5708 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5710 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeTextSelInObj */

/* 5712 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5714 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5718 */	NdrFcShort( 0x16 ),	/* 22 */
/* 5720 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 5722 */	NdrFcShort( 0x36 ),	/* 54 */
/* 5724 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5726 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0xc,		/* 12 */
/* 5728 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5730 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5732 */	NdrFcShort( 0x2 ),	/* 2 */
/* 5734 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ihvoRoot */

/* 5736 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5738 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5740 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvsli */

/* 5742 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5744 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5746 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsli */

/* 5748 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5750 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5752 */	NdrFcShort( 0x6d6 ),	/* Type Offset=1750 */

	/* Parameter cvsliEnd */

/* 5754 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5756 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5758 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsliEnd */

/* 5760 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5762 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5764 */	NdrFcShort( 0x6fc ),	/* Type Offset=1788 */

	/* Parameter fInitial */

/* 5766 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5768 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5770 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fEdit */

/* 5772 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5774 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5776 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fRange */

/* 5778 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5780 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 5782 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fWholeObj */

/* 5784 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5786 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 5788 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fInstall */

/* 5790 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5792 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 5794 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 5796 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5798 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 5800 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 5802 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5804 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 5806 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeSelInObj */

/* 5808 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5810 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5814 */	NdrFcShort( 0x17 ),	/* 23 */
/* 5816 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 5818 */	NdrFcShort( 0x1e ),	/* 30 */
/* 5820 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5822 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 5824 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5826 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5828 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5830 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ihvoRoot */

/* 5832 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5834 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5836 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvsli */

/* 5838 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5840 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5842 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsli */

/* 5844 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5846 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5848 */	NdrFcShort( 0x6d6 ),	/* Type Offset=1750 */

	/* Parameter tag */

/* 5850 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5852 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5854 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fInstall */

/* 5856 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5858 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5860 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 5862 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5864 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5866 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 5868 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5870 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5872 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeSelAt */

/* 5874 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5876 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5880 */	NdrFcShort( 0x18 ),	/* 24 */
/* 5882 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 5884 */	NdrFcShort( 0x56 ),	/* 86 */
/* 5886 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5888 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x7,		/* 7 */
/* 5890 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5892 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5894 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5896 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 5898 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5900 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5902 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 5904 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5906 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5908 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 5910 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 5912 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5914 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 5916 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 5918 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5920 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter fInstall */

/* 5922 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5924 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 5926 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 5928 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5930 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 5932 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 5934 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5936 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 5938 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeSelInBox */

/* 5940 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5942 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5946 */	NdrFcShort( 0x19 ),	/* 25 */
/* 5948 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 5950 */	NdrFcShort( 0x28 ),	/* 40 */
/* 5952 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5954 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x9,		/* 9 */
/* 5956 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5958 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5960 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5962 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pselInit */

/* 5964 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5966 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5968 */	NdrFcShort( 0x6b4 ),	/* Type Offset=1716 */

	/* Parameter fEndPoint */

/* 5970 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5972 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5974 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter iLevel */

/* 5976 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5978 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5980 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter iBox */

/* 5982 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5984 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5986 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fInitial */

/* 5988 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5990 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5992 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fRange */

/* 5994 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5996 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5998 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fInstall */

/* 6000 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6002 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6004 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 6006 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6008 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 6010 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 6012 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6014 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 6016 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsClickInText */

/* 6018 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6020 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6024 */	NdrFcShort( 0x1a ),	/* 26 */
/* 6026 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 6028 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6030 */	NdrFcShort( 0x22 ),	/* 34 */
/* 6032 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 6034 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6036 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6038 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6040 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6042 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6044 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6046 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6048 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6050 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6052 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 6054 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6056 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6058 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6060 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6062 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6064 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter pfInText */

/* 6066 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6068 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6070 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 6072 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6074 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6076 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsClickInObject */

/* 6078 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6080 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6084 */	NdrFcShort( 0x1b ),	/* 27 */
/* 6086 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 6088 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6090 */	NdrFcShort( 0x3e ),	/* 62 */
/* 6092 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x7,		/* 7 */
/* 6094 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6096 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6098 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6100 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6102 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6104 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6106 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6108 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6110 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 6114 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6116 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6118 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6120 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6122 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6124 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter podt */

/* 6126 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6128 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6130 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfInObject */

/* 6132 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6134 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6136 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 6138 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6140 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 6142 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsClickInOverlayTag */

/* 6144 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6146 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6150 */	NdrFcShort( 0x1c ),	/* 28 */
/* 6152 */	NdrFcShort( 0x48 ),	/* x86 Stack size/offset = 72 */
/* 6154 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6156 */	NdrFcShort( 0xc0 ),	/* 192 */
/* 6158 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0xb,		/* 11 */
/* 6160 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 6162 */	NdrFcShort( 0x1 ),	/* 1 */
/* 6164 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6166 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6168 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6170 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6174 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6176 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6178 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc1 */

/* 6180 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6182 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6184 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst1 */

/* 6186 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6188 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6190 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter piGuid */

/* 6192 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6194 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrGuids */

/* 6198 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 6200 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6202 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Parameter prcTag */

/* 6204 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 6206 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 6208 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter prcAllTags */

/* 6210 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 6212 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 6214 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter pfOpeningTag */

/* 6216 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6218 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 6220 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfInOverlayTag */

/* 6222 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6224 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 6226 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 6228 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6230 */	NdrFcShort( 0x44 ),	/* x86 Stack size/offset = 68 */
/* 6232 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OnTyping */

/* 6234 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6236 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6240 */	NdrFcShort( 0x1d ),	/* 29 */
/* 6242 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 6244 */	NdrFcShort( 0x32 ),	/* 50 */
/* 6246 */	NdrFcShort( 0x24 ),	/* 36 */
/* 6248 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 6250 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 6252 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6254 */	NdrFcShort( 0x1 ),	/* 1 */
/* 6256 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 6258 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6260 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6262 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Parameter bstrInput */

/* 6264 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 6266 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6268 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter cchBackspace */

/* 6270 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6272 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6274 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cchDelForward */

/* 6276 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6278 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6280 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter chFirst */

/* 6282 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6284 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6286 */	0x5,		/* FC_WCHAR */
			0x0,		/* 0 */

	/* Parameter pwsPending */

/* 6288 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 6290 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 6292 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6294 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6296 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6298 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OnChar */

/* 6300 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6302 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6306 */	NdrFcShort( 0x1e ),	/* 30 */
/* 6308 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6310 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6312 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6314 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 6316 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6318 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6320 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6322 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter chw */

/* 6324 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6326 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6328 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6330 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6332 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6334 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OnSysChar */

/* 6336 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6338 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6342 */	NdrFcShort( 0x1f ),	/* 31 */
/* 6344 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6346 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6348 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6350 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 6352 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6354 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6356 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6358 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter chw */

/* 6360 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6362 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6364 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6366 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6368 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6370 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OnExtendedKey */

/* 6372 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6374 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6378 */	NdrFcShort( 0x20 ),	/* 32 */
/* 6380 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6382 */	NdrFcShort( 0x18 ),	/* 24 */
/* 6384 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6386 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 6388 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6390 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6392 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6394 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter chw */

/* 6396 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6398 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6400 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ss */

/* 6402 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6404 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6406 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter nFlags */

/* 6408 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6410 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6412 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6414 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6416 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTaggedPara */


	/* Procedure FlashInsertionPoint */

/* 6420 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6422 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6426 */	NdrFcShort( 0x21 ),	/* 33 */
/* 6428 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6430 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6432 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6434 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 6436 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6438 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6440 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6442 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 6444 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6446 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6448 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MouseDown */

/* 6450 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6452 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6456 */	NdrFcShort( 0x22 ),	/* 34 */
/* 6458 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6460 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6462 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6464 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 6466 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6468 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6470 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6472 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6474 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6476 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6478 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6480 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6482 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6484 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 6486 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6488 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6490 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6492 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6494 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6496 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 6498 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6500 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MouseDblClk */

/* 6504 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6506 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6510 */	NdrFcShort( 0x23 ),	/* 35 */
/* 6512 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6514 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6516 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6518 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 6520 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6522 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6524 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6526 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6528 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6530 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6532 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6534 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6536 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 6540 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6542 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6544 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6546 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6548 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6550 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 6552 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6554 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6556 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MouseMoveDrag */

/* 6558 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6560 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6564 */	NdrFcShort( 0x24 ),	/* 36 */
/* 6566 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6568 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6570 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6572 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 6574 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6576 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6578 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6580 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6582 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6584 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6586 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6588 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6590 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 6594 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6596 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6598 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6600 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6602 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6604 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 6606 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6608 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6610 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MouseDownExtended */

/* 6612 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6614 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6618 */	NdrFcShort( 0x25 ),	/* 37 */
/* 6620 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6622 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6624 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6626 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 6628 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6630 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6632 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6634 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6636 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6638 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6640 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6642 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6644 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6646 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 6648 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6650 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6652 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6654 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6656 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6658 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 6660 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6662 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MouseUp */

/* 6666 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6668 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6672 */	NdrFcShort( 0x26 ),	/* 38 */
/* 6674 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6676 */	NdrFcShort( 0x50 ),	/* 80 */
/* 6678 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6680 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 6682 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6684 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6688 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xd */

/* 6690 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6692 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yd */

/* 6696 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6698 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 6702 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6704 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6706 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6708 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6710 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6712 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 6714 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6716 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6718 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Activate */

/* 6720 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6722 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6726 */	NdrFcShort( 0x27 ),	/* 39 */
/* 6728 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6730 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6732 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6734 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 6736 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6738 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6740 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6742 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter vss */

/* 6744 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6746 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6748 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 6750 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6752 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6754 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PrepareToDraw */

/* 6756 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6758 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6762 */	NdrFcShort( 0x28 ),	/* 40 */
/* 6764 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6766 */	NdrFcShort( 0x40 ),	/* 64 */
/* 6768 */	NdrFcShort( 0x24 ),	/* 36 */
/* 6770 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 6772 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6776 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6778 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 6780 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6782 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6784 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Parameter rcSrc */

/* 6786 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6788 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6790 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6792 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6794 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 6796 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter pxpdr */

/* 6798 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6800 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 6802 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 6804 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6806 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6808 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawRoot */

/* 6810 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6812 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6816 */	NdrFcShort( 0x29 ),	/* 41 */
/* 6818 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 6820 */	NdrFcShort( 0x46 ),	/* 70 */
/* 6822 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6824 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 6826 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6828 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6830 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6832 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 6834 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6836 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6838 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Parameter rcSrc */

/* 6840 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6842 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6844 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 6846 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 6848 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 6850 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter fDrawSel */

/* 6852 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6854 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 6856 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 6858 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6860 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 6862 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Layout */

/* 6864 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6866 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6870 */	NdrFcShort( 0x2a ),	/* 42 */
/* 6872 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6874 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6876 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6878 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6880 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6882 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6884 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6886 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 6888 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6890 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6892 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Parameter dxsAvailWidth */

/* 6894 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6896 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6898 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6900 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6902 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6904 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Height */

/* 6906 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6908 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6912 */	NdrFcShort( 0x2b ),	/* 43 */
/* 6914 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6916 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6918 */	NdrFcShort( 0x24 ),	/* 36 */
/* 6920 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 6922 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6926 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6928 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pdysHeight */

/* 6930 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6932 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6936 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6938 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6940 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Width */

/* 6942 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6944 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6948 */	NdrFcShort( 0x2c ),	/* 44 */
/* 6950 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6952 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6954 */	NdrFcShort( 0x24 ),	/* 36 */
/* 6956 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 6958 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6960 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6962 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6964 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pdxsWidth */

/* 6966 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6968 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6972 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6974 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6976 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InitializePrinting */

/* 6978 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6980 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6984 */	NdrFcShort( 0x2d ),	/* 45 */
/* 6986 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6988 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6990 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6992 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 6994 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6996 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6998 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7000 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvpc */

/* 7002 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7004 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7006 */	NdrFcShort( 0x734 ),	/* Type Offset=1844 */

	/* Return value */

/* 7008 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7010 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7012 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetTotalPrintPages */

/* 7014 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7016 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7020 */	NdrFcShort( 0x2e ),	/* 46 */
/* 7022 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7024 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7026 */	NdrFcShort( 0x24 ),	/* 36 */
/* 7028 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 7030 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7032 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7034 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7036 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvpc */

/* 7038 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7040 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7042 */	NdrFcShort( 0x734 ),	/* Type Offset=1844 */

	/* Parameter padvi3 */

/* 7044 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7046 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7048 */	NdrFcShort( 0x746 ),	/* Type Offset=1862 */

	/* Parameter pcPageTotal */

/* 7050 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7052 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7054 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7056 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7058 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7060 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PrintSinglePage */

/* 7062 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7064 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7068 */	NdrFcShort( 0x2f ),	/* 47 */
/* 7070 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7072 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7074 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7076 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 7078 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7080 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7082 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7084 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvpc */

/* 7086 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7088 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7090 */	NdrFcShort( 0x734 ),	/* Type Offset=1844 */

	/* Parameter nPageNo */

/* 7092 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7094 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7098 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7100 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7102 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Print */

/* 7104 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7106 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7110 */	NdrFcShort( 0x30 ),	/* 48 */
/* 7112 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7114 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7116 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7118 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 7120 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7122 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7124 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7126 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvpc */

/* 7128 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7130 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7132 */	NdrFcShort( 0x734 ),	/* Type Offset=1844 */

	/* Parameter padvi3 */

/* 7134 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7136 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7138 */	NdrFcShort( 0x746 ),	/* Type Offset=1862 */

	/* Return value */

/* 7140 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7142 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7144 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Site */

/* 7146 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7148 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7152 */	NdrFcShort( 0x31 ),	/* 49 */
/* 7154 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7156 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7158 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7160 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 7162 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7164 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7166 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7168 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppvrs */

/* 7170 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 7172 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7174 */	NdrFcShort( 0x758 ),	/* Type Offset=1880 */

	/* Return value */

/* 7176 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7178 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7180 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LoseFocus */

/* 7182 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7184 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7188 */	NdrFcShort( 0x32 ),	/* 50 */
/* 7190 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7192 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7194 */	NdrFcShort( 0x22 ),	/* 34 */
/* 7196 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 7198 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7200 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7202 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7204 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfOk */

/* 7206 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7208 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7210 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7212 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7214 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7216 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Close */

/* 7218 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7220 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7224 */	NdrFcShort( 0x33 ),	/* 51 */
/* 7226 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7228 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7230 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7232 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 7234 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7236 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7238 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7240 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 7242 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7244 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddSelChngListener */

/* 7248 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7250 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7254 */	NdrFcShort( 0x34 ),	/* 52 */
/* 7256 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7260 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7262 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 7264 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7266 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7270 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pel */

/* 7272 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7274 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7276 */	NdrFcShort( 0x76e ),	/* Type Offset=1902 */

	/* Return value */

/* 7278 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7280 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7282 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DelSelChngListener */

/* 7284 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7286 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7290 */	NdrFcShort( 0x35 ),	/* 53 */
/* 7292 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7294 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7296 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7298 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 7300 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7302 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7304 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7306 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pel */

/* 7308 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7310 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7312 */	NdrFcShort( 0x76e ),	/* Type Offset=1902 */

	/* Return value */

/* 7314 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7316 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7318 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseTableHeader */


	/* Procedure Reconstruct */

/* 7320 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7322 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7326 */	NdrFcShort( 0x36 ),	/* 54 */
/* 7328 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7330 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7332 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7334 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 7336 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7338 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7340 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7342 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 7344 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7346 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7348 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTableFooter */


	/* Procedure OnStylesheetChange */

/* 7350 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7352 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7356 */	NdrFcShort( 0x37 ),	/* 55 */
/* 7358 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7360 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7362 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7364 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 7366 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7372 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 7374 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7376 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7378 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Stylesheet */

/* 7380 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7382 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7386 */	NdrFcShort( 0x39 ),	/* 57 */
/* 7388 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7390 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7392 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7394 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 7396 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7398 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7400 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7402 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppvss */

/* 7404 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 7406 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7408 */	NdrFcShort( 0x780 ),	/* Type Offset=1920 */

	/* Return value */

/* 7410 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7412 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7414 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetTableColWidths */

/* 7416 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7418 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7422 */	NdrFcShort( 0x3a ),	/* 58 */
/* 7424 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7426 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7428 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7430 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 7432 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7434 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7436 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7438 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgvlen */

/* 7440 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 7442 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7444 */	NdrFcShort( 0x7a2 ),	/* Type Offset=1954 */

	/* Parameter cvlen */

/* 7446 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7448 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7450 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7452 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7454 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7456 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsDirty */

/* 7458 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7460 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7464 */	NdrFcShort( 0x3b ),	/* 59 */
/* 7466 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7468 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7470 */	NdrFcShort( 0x22 ),	/* 34 */
/* 7472 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 7474 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7476 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7478 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7480 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfDirty */

/* 7482 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7484 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7486 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7488 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7490 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7492 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_XdPos */

/* 7494 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7496 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7500 */	NdrFcShort( 0x3c ),	/* 60 */
/* 7502 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7504 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7506 */	NdrFcShort( 0x24 ),	/* 36 */
/* 7508 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 7510 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7512 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7514 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7516 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pxdPos */

/* 7518 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7520 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7522 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7524 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7526 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7528 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RequestObjCharDeleteNotification */

/* 7530 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7532 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7536 */	NdrFcShort( 0x3d ),	/* 61 */
/* 7538 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7540 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7542 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7544 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 7546 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7548 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7550 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7552 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnocd */

/* 7554 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7556 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7558 */	NdrFcShort( 0x7b2 ),	/* Type Offset=1970 */

	/* Return value */

/* 7560 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7562 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7564 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetRootObject */

/* 7566 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7568 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7572 */	NdrFcShort( 0x3e ),	/* 62 */
/* 7574 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7576 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7578 */	NdrFcShort( 0x40 ),	/* 64 */
/* 7580 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 7582 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7584 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7588 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phvo */

/* 7590 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7592 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7594 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppvwvc */

/* 7596 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 7598 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7600 */	NdrFcShort( 0x7c4 ),	/* Type Offset=1988 */

	/* Parameter pfrag */

/* 7602 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7604 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7606 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppss */

/* 7608 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 7610 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7612 */	NdrFcShort( 0x780 ),	/* Type Offset=1920 */

	/* Return value */

/* 7614 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7616 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7618 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawRoot2 */

/* 7620 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7622 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7626 */	NdrFcShort( 0x3f ),	/* 63 */
/* 7628 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 7630 */	NdrFcShort( 0x56 ),	/* 86 */
/* 7632 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7634 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 7636 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7638 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7640 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7642 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 7644 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7646 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7648 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Parameter rcSrc */

/* 7650 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 7652 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7654 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 7656 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 7658 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7660 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter fDrawSel */

/* 7662 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7664 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 7666 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ysTop */

/* 7668 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7670 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 7672 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dysHeight */

/* 7674 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7676 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 7678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7680 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7682 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 7684 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetKeyboardForWs */

/* 7686 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7688 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7692 */	NdrFcShort( 0x40 ),	/* 64 */
/* 7694 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7696 */	NdrFcShort( 0x52 ),	/* 82 */
/* 7698 */	NdrFcShort( 0x5a ),	/* 90 */
/* 7700 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 7702 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 7704 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7706 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7708 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pws */

/* 7710 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7712 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7714 */	NdrFcShort( 0x7da ),	/* Type Offset=2010 */

	/* Parameter pbstrActiveKeymanKbd */

/* 7716 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 7718 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7720 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Parameter pnActiveLangId */

/* 7722 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 7724 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7726 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phklActive */

/* 7728 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 7730 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7732 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfSelectLangPending */

/* 7734 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 7736 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7738 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7740 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7742 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7744 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Display */

/* 7746 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7748 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7752 */	NdrFcShort( 0x3 ),	/* 3 */
/* 7754 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7756 */	NdrFcShort( 0x10 ),	/* 16 */
/* 7758 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7760 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 7762 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7768 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvwenv */

/* 7770 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7772 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7774 */	NdrFcShort( 0x7f4 ),	/* Type Offset=2036 */

	/* Parameter hvo */

/* 7776 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7778 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter frag */

/* 7782 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7784 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7786 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7788 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7790 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7792 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DisplayVec */

/* 7794 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7796 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7800 */	NdrFcShort( 0x4 ),	/* 4 */
/* 7802 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7804 */	NdrFcShort( 0x18 ),	/* 24 */
/* 7806 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7808 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 7810 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7812 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7814 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7816 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvwenv */

/* 7818 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7820 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7822 */	NdrFcShort( 0x7f4 ),	/* Type Offset=2036 */

	/* Parameter hvo */

/* 7824 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7826 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7828 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 7830 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7832 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7834 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter frag */

/* 7836 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7838 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7840 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7842 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7844 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7846 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DisplayVariant */

/* 7848 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7850 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7854 */	NdrFcShort( 0x5 ),	/* 5 */
/* 7856 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 7858 */	NdrFcShort( 0x10 ),	/* 16 */
/* 7860 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7862 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 7864 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7866 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7868 */	NdrFcShort( 0x20 ),	/* 32 */
/* 7870 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvwenv */

/* 7872 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7874 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7876 */	NdrFcShort( 0x7f4 ),	/* Type Offset=2036 */

	/* Parameter tag */

/* 7878 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7880 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7882 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter v */

/* 7884 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7886 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7888 */	NdrFcShort( 0x66c ),	/* Type Offset=1644 */

	/* Parameter frag */

/* 7890 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7892 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7894 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 7896 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 7898 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 7900 */	NdrFcShort( 0x806 ),	/* Type Offset=2054 */

	/* Return value */

/* 7902 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7904 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 7906 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DisplayPicture */

/* 7908 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7910 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7914 */	NdrFcShort( 0x6 ),	/* 6 */
/* 7916 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 7918 */	NdrFcShort( 0x20 ),	/* 32 */
/* 7920 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7922 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 7924 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7926 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7928 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7930 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvwenv */

/* 7932 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7934 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7936 */	NdrFcShort( 0x81c ),	/* Type Offset=2076 */

	/* Parameter hvo */

/* 7938 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7940 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7942 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 7944 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7946 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7948 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter val */

/* 7950 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7952 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7954 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter frag */

/* 7956 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7958 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7960 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppPict */

/* 7962 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 7964 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7966 */	NdrFcShort( 0x82e ),	/* Type Offset=2094 */

	/* Return value */

/* 7968 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7970 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7972 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure UpdateProp */

/* 7974 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7976 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7980 */	NdrFcShort( 0x7 ),	/* 7 */
/* 7982 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 7984 */	NdrFcShort( 0x18 ),	/* 24 */
/* 7986 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7988 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 7990 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7992 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7994 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7996 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvwsel */

/* 7998 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8000 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8002 */	NdrFcShort( 0x844 ),	/* Type Offset=2116 */

	/* Parameter hvo */

/* 8004 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8006 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8008 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 8010 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8012 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8014 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter frag */

/* 8016 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8018 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8020 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptssVal */

/* 8022 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8024 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8026 */	NdrFcShort( 0x856 ),	/* Type Offset=2134 */

	/* Parameter pptssRepVal */

/* 8028 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 8030 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8032 */	NdrFcShort( 0x868 ),	/* Type Offset=2152 */

	/* Return value */

/* 8034 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8036 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8038 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure EstimateHeight */

/* 8040 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8042 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8046 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8048 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8050 */	NdrFcShort( 0x18 ),	/* 24 */
/* 8052 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8054 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 8056 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8058 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8060 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8062 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 8064 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8066 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8068 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter frag */

/* 8070 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8072 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8074 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dxAvailWidth */

/* 8076 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8078 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8080 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdyHeight */

/* 8082 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8084 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8086 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8088 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8090 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8092 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LoadDataFor */

/* 8094 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8096 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8100 */	NdrFcShort( 0x9 ),	/* 9 */
/* 8102 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 8104 */	NdrFcShort( 0x28 ),	/* 40 */
/* 8106 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8108 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 8110 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 8112 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8114 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8116 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvwenv */

/* 8118 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8120 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8122 */	NdrFcShort( 0x86c ),	/* Type Offset=2156 */

	/* Parameter prghvo */

/* 8124 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 8126 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8128 */	NdrFcShort( 0x5a8 ),	/* Type Offset=1448 */

	/* Parameter chvo */

/* 8130 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8132 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8134 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoParent */

/* 8136 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8138 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8140 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 8142 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8144 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8146 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter frag */

/* 8148 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8150 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8152 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoMin */

/* 8154 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8156 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8158 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8160 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8162 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 8164 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetStrForGuid */

/* 8166 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8168 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8172 */	NdrFcShort( 0xa ),	/* 10 */
/* 8174 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8176 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8178 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8180 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8182 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 8184 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8186 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8188 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrGuid */

/* 8190 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 8192 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8194 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pptss */

/* 8196 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 8198 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8200 */	NdrFcShort( 0x868 ),	/* Type Offset=2152 */

	/* Return value */

/* 8202 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8204 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8206 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DoHotLinkAction */

/* 8208 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8210 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8214 */	NdrFcShort( 0xb ),	/* 11 */
/* 8216 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8218 */	NdrFcShort( 0x18 ),	/* 24 */
/* 8220 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8222 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 8224 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 8226 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8228 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8230 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrData */

/* 8232 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 8234 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8236 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter hvoOwner */

/* 8238 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8240 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8242 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 8244 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8246 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8248 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 8250 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8252 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8254 */	NdrFcShort( 0x856 ),	/* Type Offset=2134 */

	/* Parameter ichObj */

/* 8256 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8258 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8260 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8262 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8264 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8266 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIdFromGuid */

/* 8268 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8270 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8274 */	NdrFcShort( 0xc ),	/* 12 */
/* 8276 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8278 */	NdrFcShort( 0x44 ),	/* 68 */
/* 8280 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8282 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 8284 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8286 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8288 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8290 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psda */

/* 8292 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8294 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8296 */	NdrFcShort( 0x87e ),	/* Type Offset=2174 */

	/* Parameter puid */

/* 8298 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 8300 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8302 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Parameter phvo */

/* 8304 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8306 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8308 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8310 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8312 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8314 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DisplayEmbeddedObject */

/* 8316 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8318 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8322 */	NdrFcShort( 0xd ),	/* 13 */
/* 8324 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8326 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8328 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8330 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8332 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8336 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8338 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvwenv */

/* 8340 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8342 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8344 */	NdrFcShort( 0x81c ),	/* Type Offset=2076 */

	/* Parameter hvo */

/* 8346 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8348 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8350 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8352 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8354 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8356 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InvalidateRect */

/* 8358 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8360 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8364 */	NdrFcShort( 0x3 ),	/* 3 */
/* 8366 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8368 */	NdrFcShort( 0x20 ),	/* 32 */
/* 8370 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8372 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 8374 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8376 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8378 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8380 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8382 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8384 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8386 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter xsLeft */

/* 8388 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8390 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8392 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ysTop */

/* 8394 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8396 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8398 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dxsWidth */

/* 8400 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8402 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8404 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dysHeight */

/* 8406 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8408 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8410 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8412 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8414 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8416 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetGraphics */

/* 8418 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8420 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8424 */	NdrFcShort( 0x4 ),	/* 4 */
/* 8426 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8428 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8430 */	NdrFcShort( 0x70 ),	/* 112 */
/* 8432 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 8434 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8436 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8438 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8440 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8442 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8444 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8446 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter ppvg */

/* 8448 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 8450 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8452 */	NdrFcShort( 0x8a2 ),	/* Type Offset=2210 */

	/* Parameter prcSrcRoot */

/* 8454 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 8456 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8458 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter prcDstRoot */

/* 8460 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 8462 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8464 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 8466 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8468 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LayoutGraphics */

/* 8472 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8474 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8478 */	NdrFcShort( 0x5 ),	/* 5 */
/* 8480 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8484 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8486 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8488 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8490 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8492 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8494 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8496 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8498 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8500 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter ppvg */

/* 8502 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 8504 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8506 */	NdrFcShort( 0x8a2 ),	/* Type Offset=2210 */

	/* Return value */

/* 8508 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8510 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8512 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ScreenGraphics */

/* 8514 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8516 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8520 */	NdrFcShort( 0x6 ),	/* 6 */
/* 8522 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8524 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8526 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8528 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8530 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8532 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8534 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8536 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8538 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8540 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8542 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter ppvg */

/* 8544 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 8546 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8548 */	NdrFcShort( 0x8a2 ),	/* Type Offset=2210 */

	/* Return value */

/* 8550 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8552 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8554 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetTransformAtDst */

/* 8556 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8558 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8562 */	NdrFcShort( 0x7 ),	/* 7 */
/* 8564 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8566 */	NdrFcShort( 0x18 ),	/* 24 */
/* 8568 */	NdrFcShort( 0x70 ),	/* 112 */
/* 8570 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 8572 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8574 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8576 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8578 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8580 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8582 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8584 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pt */

/* 8586 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 8588 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8590 */	NdrFcShort( 0x3e0 ),	/* Type Offset=992 */

	/* Parameter prcSrcRoot */

/* 8592 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 8594 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8596 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter prcDstRoot */

/* 8598 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 8600 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8602 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 8604 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8606 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8608 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetTransformAtSrc */

/* 8610 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8612 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8616 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8618 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8620 */	NdrFcShort( 0x18 ),	/* 24 */
/* 8622 */	NdrFcShort( 0x70 ),	/* 112 */
/* 8624 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 8626 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8628 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8630 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8632 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8634 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8636 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8638 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pt */

/* 8640 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 8642 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8644 */	NdrFcShort( 0x3e0 ),	/* Type Offset=992 */

	/* Parameter prcSrcRoot */

/* 8646 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 8648 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8650 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter prcDstRoot */

/* 8652 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 8654 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8656 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 8658 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8660 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8662 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReleaseGraphics */

/* 8664 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8666 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8670 */	NdrFcShort( 0x9 ),	/* 9 */
/* 8672 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8674 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8676 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8678 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8680 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8682 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8684 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8686 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8688 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8690 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8692 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pvg */

/* 8694 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8696 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8698 */	NdrFcShort( 0x8a6 ),	/* Type Offset=2214 */

	/* Return value */

/* 8700 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8702 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8704 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetAvailWidth */

/* 8706 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8708 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8712 */	NdrFcShort( 0xa ),	/* 10 */
/* 8714 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8716 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8718 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8720 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8722 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8724 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8726 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8728 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8730 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8732 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8734 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter ptwWidth */

/* 8736 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8738 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8740 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8742 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8744 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8746 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DoUpdates */

/* 8748 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8750 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8754 */	NdrFcShort( 0xb ),	/* 11 */
/* 8756 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8758 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8760 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8762 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 8764 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8768 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8770 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8772 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8774 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8776 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Return value */

/* 8778 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8780 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8782 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SizeChanged */

/* 8784 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8786 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8790 */	NdrFcShort( 0xc ),	/* 12 */
/* 8792 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8794 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8796 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8798 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 8800 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8802 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8804 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8806 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8808 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8810 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8812 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Return value */

/* 8814 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8816 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8818 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AdjustScrollRange */

/* 8820 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8822 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8826 */	NdrFcShort( 0xd ),	/* 13 */
/* 8828 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 8830 */	NdrFcShort( 0x20 ),	/* 32 */
/* 8832 */	NdrFcShort( 0x22 ),	/* 34 */
/* 8834 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 8836 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8840 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8842 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8844 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8846 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8848 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter dxdSize */

/* 8850 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8852 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8854 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dxdPosition */

/* 8856 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8858 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8860 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dydSize */

/* 8862 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8864 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8866 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dydPosition */

/* 8868 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8870 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8872 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfForcedScroll */

/* 8874 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8876 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8878 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 8880 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8882 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8884 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SelectionChanged */

/* 8886 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8888 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8892 */	NdrFcShort( 0xe ),	/* 14 */
/* 8894 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8896 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8898 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8900 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8902 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8904 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8906 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8908 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8910 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8912 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8914 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pvwselNew */

/* 8916 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8918 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8920 */	NdrFcShort( 0x8b8 ),	/* Type Offset=2232 */

	/* Return value */

/* 8922 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8924 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8926 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OverlayChanged */

/* 8928 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8930 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8934 */	NdrFcShort( 0xf ),	/* 15 */
/* 8936 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8938 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8940 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8942 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8944 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8948 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8950 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8952 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8954 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8956 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pvo */

/* 8958 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8960 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8962 */	NdrFcShort( 0x8ca ),	/* Type Offset=2250 */

	/* Return value */

/* 8964 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8966 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8968 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SemiTagging */

/* 8970 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8972 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8976 */	NdrFcShort( 0x10 ),	/* 16 */
/* 8978 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8980 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8982 */	NdrFcShort( 0x22 ),	/* 34 */
/* 8984 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8986 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8988 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8990 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8992 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 8994 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8996 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8998 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pf */

/* 9000 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9002 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9004 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 9006 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9008 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9010 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ScreenToClient */

/* 9012 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9014 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9018 */	NdrFcShort( 0x11 ),	/* 17 */
/* 9020 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9022 */	NdrFcShort( 0x2c ),	/* 44 */
/* 9024 */	NdrFcShort( 0x34 ),	/* 52 */
/* 9026 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 9028 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9030 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9032 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9034 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 9036 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9038 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9040 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter ppnt */

/* 9042 */	NdrFcShort( 0x11a ),	/* Flags:  must free, in, out, simple ref, */
/* 9044 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9046 */	NdrFcShort( 0x3e0 ),	/* Type Offset=992 */

	/* Return value */

/* 9048 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9050 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9052 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ClientToScreen */

/* 9054 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9056 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9060 */	NdrFcShort( 0x12 ),	/* 18 */
/* 9062 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9064 */	NdrFcShort( 0x2c ),	/* 44 */
/* 9066 */	NdrFcShort( 0x34 ),	/* 52 */
/* 9068 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 9070 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9072 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9074 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9076 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 9078 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9080 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9082 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter ppnt */

/* 9084 */	NdrFcShort( 0x11a ),	/* Flags:  must free, in, out, simple ref, */
/* 9086 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9088 */	NdrFcShort( 0x3e0 ),	/* Type Offset=992 */

	/* Return value */

/* 9090 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9092 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9094 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetAndClearPendingWs */

/* 9096 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9098 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9102 */	NdrFcShort( 0x13 ),	/* 19 */
/* 9104 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9106 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9108 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9110 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 9112 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9114 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9118 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 9120 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9122 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9124 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pws */

/* 9126 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9128 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9130 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9132 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9134 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9136 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsOkToMakeLazy */

/* 9138 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9140 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9144 */	NdrFcShort( 0x14 ),	/* 20 */
/* 9146 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9148 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9150 */	NdrFcShort( 0x22 ),	/* 34 */
/* 9152 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 9154 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9156 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9158 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9160 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 9162 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9164 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9166 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter ydTop */

/* 9168 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9170 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ydBottom */

/* 9174 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9176 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9178 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfOK */

/* 9180 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9182 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9184 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 9186 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9188 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OnProblemDeletion */

/* 9192 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9194 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9198 */	NdrFcShort( 0x15 ),	/* 21 */
/* 9200 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9202 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9204 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9206 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 9208 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9210 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9212 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9214 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psel */

/* 9216 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9218 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9220 */	NdrFcShort( 0x8b8 ),	/* Type Offset=2232 */

	/* Parameter dpt */

/* 9222 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9224 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9226 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pdpr */

/* 9228 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9230 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9232 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 9234 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9236 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OnInsertDiffParas */

/* 9240 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9242 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9246 */	NdrFcShort( 0x16 ),	/* 22 */
/* 9248 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 9250 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9252 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9254 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 9256 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9260 */	NdrFcShort( 0x2 ),	/* 2 */
/* 9262 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 9264 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9266 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9268 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter pttpDest */

/* 9270 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9272 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9274 */	NdrFcShort( 0x8e0 ),	/* Type Offset=2272 */

	/* Parameter cPara */

/* 9276 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9278 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9280 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgpttpSrc */

/* 9282 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9284 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9286 */	NdrFcShort( 0x8f6 ),	/* Type Offset=2294 */

	/* Parameter prgptssSrc */

/* 9288 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9290 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9292 */	NdrFcShort( 0x910 ),	/* Type Offset=2320 */

	/* Parameter ptssTrailing */

/* 9294 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9296 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9298 */	NdrFcShort( 0x80a ),	/* Type Offset=2058 */

	/* Parameter pidpr */

/* 9300 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9302 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 9304 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 9306 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9308 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 9310 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_TextRepOfObj */

/* 9312 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9314 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9318 */	NdrFcShort( 0x17 ),	/* 23 */
/* 9320 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9322 */	NdrFcShort( 0x44 ),	/* 68 */
/* 9324 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9326 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 9328 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 9330 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9332 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9334 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pguid */

/* 9336 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 9338 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9340 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Parameter pbstrRep */

/* 9342 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 9344 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9346 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 9348 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9350 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MakeObjFromText */

/* 9354 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9356 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9360 */	NdrFcShort( 0x18 ),	/* 24 */
/* 9362 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9364 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9366 */	NdrFcShort( 0x68 ),	/* 104 */
/* 9368 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 9370 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9374 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9376 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrText */

/* 9378 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9380 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9382 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pselDst */

/* 9384 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9386 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9388 */	NdrFcShort( 0x8b8 ),	/* Type Offset=2232 */

	/* Parameter podt */

/* 9390 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9392 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9394 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pGuid */

/* 9396 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 9398 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9400 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 9402 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9404 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9406 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ScrollSelectionIntoView */

/* 9408 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9410 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9414 */	NdrFcShort( 0x19 ),	/* 25 */
/* 9416 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9418 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9420 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9422 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 9424 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9426 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9428 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9430 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psel */

/* 9432 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9434 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9436 */	NdrFcShort( 0x8b8 ),	/* Type Offset=2232 */

	/* Parameter ssoFlag */

/* 9438 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9440 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9442 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 9444 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9446 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9448 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RootBox */

/* 9450 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9452 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9456 */	NdrFcShort( 0x1a ),	/* 26 */
/* 9458 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9460 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9462 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9464 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 9466 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9468 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9470 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9472 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prootb */

/* 9474 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 9476 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9478 */	NdrFcShort( 0x926 ),	/* Type Offset=2342 */

	/* Return value */

/* 9480 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9482 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9484 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Hwnd */

/* 9486 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9488 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9492 */	NdrFcShort( 0x1b ),	/* 27 */
/* 9494 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9496 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9498 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9500 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 9502 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9504 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9506 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9508 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phwnd */

/* 9510 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9512 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9514 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9516 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9518 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9520 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AboutToDelete */

/* 9522 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9524 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9528 */	NdrFcShort( 0x3 ),	/* 3 */
/* 9530 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 9532 */	NdrFcShort( 0x26 ),	/* 38 */
/* 9534 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9536 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 9538 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9540 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9542 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9544 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pRoot */

/* 9546 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9548 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9550 */	NdrFcShort( 0x890 ),	/* Type Offset=2192 */

	/* Parameter hvoObject */

/* 9552 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9554 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9556 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoOwner */

/* 9558 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9560 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9562 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 9564 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9566 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9568 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvo */

/* 9570 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9572 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9574 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fMergeNext */

/* 9576 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9578 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9580 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 9582 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9584 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 9586 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddObjProp */

/* 9588 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9590 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9594 */	NdrFcShort( 0x3 ),	/* 3 */
/* 9596 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9598 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9600 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9602 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 9604 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9606 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9608 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9610 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 9612 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9614 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9616 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9618 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9620 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9622 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9624 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9626 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9628 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9630 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9632 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9634 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddObjVec */

/* 9636 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9638 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9642 */	NdrFcShort( 0x4 ),	/* 4 */
/* 9644 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9646 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9648 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9650 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 9652 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9654 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9656 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9658 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 9660 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9662 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9666 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9668 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9670 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9672 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9674 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9676 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9678 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9680 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9682 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddObjVecItems */

/* 9684 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9686 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9690 */	NdrFcShort( 0x5 ),	/* 5 */
/* 9692 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9694 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9696 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9698 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 9700 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9702 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9704 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9706 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 9708 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9710 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9712 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9714 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9716 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9718 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9720 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9722 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9724 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9726 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9728 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9730 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddObj */

/* 9732 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9734 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9738 */	NdrFcShort( 0x6 ),	/* 6 */
/* 9740 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9742 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9744 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9746 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 9748 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9750 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9754 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 9756 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9758 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9760 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9762 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9764 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9766 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9768 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9770 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9772 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9774 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9776 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9778 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddLazyVecItems */

/* 9780 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9782 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9786 */	NdrFcShort( 0x7 ),	/* 7 */
/* 9788 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9790 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9792 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9794 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 9796 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9800 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9802 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 9804 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9806 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9808 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9810 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9812 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9814 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9816 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9818 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9820 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9822 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9824 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddLazyItems */

/* 9828 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9830 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9834 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9836 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9838 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9840 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9842 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 9844 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9846 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9848 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9850 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prghvo */

/* 9852 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9854 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9856 */	NdrFcShort( 0x940 ),	/* Type Offset=2368 */

	/* Parameter chvo */

/* 9858 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9860 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9862 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9864 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9866 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9868 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9870 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9872 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9874 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9876 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9878 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9880 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddProp */

/* 9882 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9884 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9888 */	NdrFcShort( 0x9 ),	/* 9 */
/* 9890 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9892 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9894 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9896 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 9898 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9900 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9904 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 9906 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9908 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9910 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9912 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9914 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9916 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9918 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9920 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9922 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9924 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9926 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9928 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddDerivedProp */

/* 9930 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9932 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9936 */	NdrFcShort( 0xa ),	/* 10 */
/* 9938 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9940 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9942 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9944 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 9946 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9948 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9950 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9952 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgtag */

/* 9954 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9956 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9958 */	NdrFcShort( 0x940 ),	/* Type Offset=2368 */

	/* Parameter ctag */

/* 9960 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9962 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9964 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 9966 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9968 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9970 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 9972 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9974 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9976 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9978 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9980 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9982 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NoteDependency */

/* 9984 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9986 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9990 */	NdrFcShort( 0xb ),	/* 11 */
/* 9992 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9994 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9996 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9998 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 10000 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 10002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10004 */	NdrFcShort( 0x2 ),	/* 2 */
/* 10006 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prghvo */

/* 10008 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 10010 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10012 */	NdrFcShort( 0x5a8 ),	/* Type Offset=1448 */

	/* Parameter prgtag */

/* 10014 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 10016 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10018 */	NdrFcShort( 0x5a8 ),	/* Type Offset=1448 */

	/* Parameter chvo */

/* 10020 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10022 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10024 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10026 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10028 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10030 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddStringProp */

/* 10032 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10034 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10038 */	NdrFcShort( 0xc ),	/* 12 */
/* 10040 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10042 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10044 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10046 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 10048 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10050 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10052 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10054 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10056 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10058 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10060 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 10062 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10064 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10066 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Return value */

/* 10068 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10070 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10072 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddUnicodeProp */

/* 10074 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10076 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10080 */	NdrFcShort( 0xd ),	/* 13 */
/* 10082 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10084 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10086 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10088 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 10090 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10092 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10094 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10096 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10098 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10100 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10102 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 10104 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10106 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10108 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 10110 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10112 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10114 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Return value */

/* 10116 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10118 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10120 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddIntProp */

/* 10122 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10124 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10128 */	NdrFcShort( 0xe ),	/* 14 */
/* 10130 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10132 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10134 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10136 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10138 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10140 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10142 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10144 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10146 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10148 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10150 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10152 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10154 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10156 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddIntPropPic */

/* 10158 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10160 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10164 */	NdrFcShort( 0xf ),	/* 15 */
/* 10166 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 10168 */	NdrFcShort( 0x20 ),	/* 32 */
/* 10170 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10172 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 10174 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10176 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10178 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10180 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10182 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10184 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10186 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvc */

/* 10188 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10190 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10192 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Parameter frag */

/* 10194 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10196 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10198 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nMin */

/* 10200 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10202 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10204 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nMax */

/* 10206 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10208 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10210 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10212 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10214 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10216 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddStringAltMember */

/* 10218 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10220 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10224 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10226 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10228 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10230 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10232 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 10234 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10236 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10238 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10240 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10242 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10244 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 10248 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10250 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10252 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvwvc */

/* 10254 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10256 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10258 */	NdrFcShort( 0x92a ),	/* Type Offset=2346 */

	/* Return value */

/* 10260 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10262 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10264 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_MaxShowTags */


	/* Procedure AddStringAlt */

/* 10266 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10268 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10272 */	NdrFcShort( 0x11 ),	/* 17 */
/* 10274 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10276 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10278 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10280 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10282 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10284 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10286 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10288 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ctag */


	/* Parameter tag */

/* 10290 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10292 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10294 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 10296 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10298 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10300 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddStringAltSeq */

/* 10302 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10304 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10308 */	NdrFcShort( 0x12 ),	/* 18 */
/* 10310 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10312 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10314 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10316 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 10318 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 10320 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10322 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10324 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10326 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10328 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10330 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgenc */

/* 10332 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 10334 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10336 */	NdrFcShort( 0x5a8 ),	/* Type Offset=1448 */

	/* Parameter cws */

/* 10338 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10340 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10342 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10344 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10346 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10348 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddString */

/* 10350 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10352 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10356 */	NdrFcShort( 0x13 ),	/* 19 */
/* 10358 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10360 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10362 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10364 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 10366 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10372 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pss */

/* 10374 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10376 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10378 */	NdrFcShort( 0x80a ),	/* Type Offset=2058 */

	/* Return value */

/* 10380 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10382 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10384 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddTimeProp */

/* 10386 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10388 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10392 */	NdrFcShort( 0x14 ),	/* 20 */
/* 10394 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10396 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10398 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10400 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 10402 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10404 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10406 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10408 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10410 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10412 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10414 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter flags */

/* 10416 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10418 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10420 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10422 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10424 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10426 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddGenDateProp */

/* 10428 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10430 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10434 */	NdrFcShort( 0x15 ),	/* 21 */
/* 10436 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10438 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10440 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10442 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10444 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10446 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10448 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10450 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tag */

/* 10452 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10454 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10456 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10458 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10460 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10462 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CurrentObject */

/* 10464 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10466 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10470 */	NdrFcShort( 0x16 ),	/* 22 */
/* 10472 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10474 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10476 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10478 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10480 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10484 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10486 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phvo */

/* 10488 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10490 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10492 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10494 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10496 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10498 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_OpenObject */

/* 10500 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10502 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10506 */	NdrFcShort( 0x17 ),	/* 23 */
/* 10508 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10510 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10512 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10514 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10516 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10520 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10522 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phvoRet */

/* 10524 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10526 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10528 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10530 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10532 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10534 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_EmbeddingLevel */

/* 10536 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10538 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10542 */	NdrFcShort( 0x18 ),	/* 24 */
/* 10544 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10546 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10548 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10550 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10552 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10554 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10556 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10558 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pchvo */

/* 10560 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10562 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10564 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10566 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10568 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10570 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetOuterObject */

/* 10572 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10574 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10578 */	NdrFcShort( 0x19 ),	/* 25 */
/* 10580 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10582 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10584 */	NdrFcShort( 0x5c ),	/* 92 */
/* 10586 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 10588 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10590 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10592 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10594 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichvoLevel */

/* 10596 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10598 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvo */

/* 10602 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10604 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10606 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptag */

/* 10608 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10610 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10612 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pihvo */

/* 10614 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10616 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10618 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10620 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10622 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10624 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DataAccess */

/* 10626 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10628 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10632 */	NdrFcShort( 0x1a ),	/* 26 */
/* 10634 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10636 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10638 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10640 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 10642 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10646 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10648 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsda */

/* 10650 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 10652 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10654 */	NdrFcShort( 0x94c ),	/* Type Offset=2380 */

	/* Return value */

/* 10656 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10658 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10660 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddWindow */

/* 10662 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10664 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10668 */	NdrFcShort( 0x1b ),	/* 27 */
/* 10670 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10672 */	NdrFcShort( 0x14 ),	/* 20 */
/* 10674 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10676 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 10678 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10680 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10682 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10684 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pew */

/* 10686 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10688 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10690 */	NdrFcShort( 0x962 ),	/* Type Offset=2402 */

	/* Parameter dmpAscent */

/* 10692 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10694 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10696 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fJustifyRight */

/* 10698 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10700 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10702 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fAutoShow */

/* 10704 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10706 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10708 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 10710 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10712 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddSeparatorBar */

/* 10716 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10718 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10722 */	NdrFcShort( 0x1c ),	/* 28 */
/* 10724 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10726 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10728 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10730 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 10732 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10734 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10736 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10738 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 10740 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10742 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10744 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenDiv */

/* 10746 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10748 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10752 */	NdrFcShort( 0x1e ),	/* 30 */
/* 10754 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10756 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10758 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10760 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 10762 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10768 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 10770 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10772 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10774 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Install */


	/* Procedure CloseDiv */

/* 10776 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10778 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10782 */	NdrFcShort( 0x1f ),	/* 31 */
/* 10784 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10786 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10788 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10790 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 10792 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10794 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10796 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10798 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 10800 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10802 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10804 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenParagraph */

/* 10806 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10808 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10812 */	NdrFcShort( 0x20 ),	/* 32 */
/* 10814 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10818 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10820 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 10822 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10824 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10826 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10828 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 10830 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10832 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10834 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenMappedPara */

/* 10836 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10838 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10842 */	NdrFcShort( 0x22 ),	/* 34 */
/* 10844 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10846 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10848 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10850 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 10852 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10854 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10856 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10858 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 10860 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10862 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10864 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenMappedTaggedPara */

/* 10866 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10868 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10872 */	NdrFcShort( 0x23 ),	/* 35 */
/* 10874 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10876 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10878 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10880 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 10882 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10884 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10886 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10888 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 10890 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10892 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10894 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenConcPara */

/* 10896 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10898 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10902 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10904 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10906 */	NdrFcShort( 0x20 ),	/* 32 */
/* 10908 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10910 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 10912 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10914 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10916 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10918 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMinItem */

/* 10920 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10922 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10924 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLimItem */

/* 10926 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10928 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10930 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cpoFlags */

/* 10932 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10934 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10936 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter dmpAlign */

/* 10938 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10940 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10942 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10944 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10946 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10948 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenOverridePara */

/* 10950 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10952 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10956 */	NdrFcShort( 0x25 ),	/* 37 */
/* 10958 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10960 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10962 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10964 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 10966 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 10968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10970 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10972 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cOverrideProperties */

/* 10974 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10976 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10978 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgOverrideProperties */

/* 10980 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 10982 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10984 */	NdrFcShort( 0x9aa ),	/* Type Offset=2474 */

	/* Return value */

/* 10986 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10988 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10990 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseParagraph */

/* 10992 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10994 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10998 */	NdrFcShort( 0x26 ),	/* 38 */
/* 11000 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11004 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11006 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11008 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11010 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11012 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11014 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11016 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11018 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11020 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenInnerPile */

/* 11022 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11024 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11028 */	NdrFcShort( 0x27 ),	/* 39 */
/* 11030 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11032 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11034 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11036 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11038 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11040 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11042 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11044 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11046 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11048 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11050 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseInnerPile */

/* 11052 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11054 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11058 */	NdrFcShort( 0x28 ),	/* 40 */
/* 11060 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11062 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11064 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11066 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11068 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11070 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11072 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11074 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11076 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11078 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11080 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenSpan */

/* 11082 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11084 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11088 */	NdrFcShort( 0x29 ),	/* 41 */
/* 11090 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11092 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11094 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11096 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11098 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11100 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11102 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11104 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11106 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11108 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11110 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseSpan */

/* 11112 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11114 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11118 */	NdrFcShort( 0x2a ),	/* 42 */
/* 11120 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11122 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11124 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11126 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11128 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11130 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11132 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11134 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11136 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11138 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11140 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTable */

/* 11142 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11144 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11148 */	NdrFcShort( 0x2b ),	/* 43 */
/* 11150 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 11152 */	NdrFcShort( 0x64 ),	/* 100 */
/* 11154 */	NdrFcShort( 0x34 ),	/* 52 */
/* 11156 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x9,		/* 9 */
/* 11158 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11160 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11162 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11164 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cCols */

/* 11166 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11168 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11170 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvlWidth */

/* 11172 */	NdrFcShort( 0x11a ),	/* Flags:  must free, in, out, simple ref, */
/* 11174 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11176 */	NdrFcShort( 0x79a ),	/* Type Offset=1946 */

	/* Parameter mpBorder */

/* 11178 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11180 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11182 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter vwalign */

/* 11184 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11186 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11188 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter frmpos */

/* 11190 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11192 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11194 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter vwrule */

/* 11196 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11198 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 11200 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter mpSpacing */

/* 11202 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11204 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 11206 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter mpPadding */

/* 11208 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11210 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 11212 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11214 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11216 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 11218 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseTable */

/* 11220 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11222 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11226 */	NdrFcShort( 0x2c ),	/* 44 */
/* 11228 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11230 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11232 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11234 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11236 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11238 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11240 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11242 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11244 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11246 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11248 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTableRow */

/* 11250 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11252 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11256 */	NdrFcShort( 0x2d ),	/* 45 */
/* 11258 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11260 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11262 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11264 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11266 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11272 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11274 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11276 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseTableRow */

/* 11280 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11282 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11286 */	NdrFcShort( 0x2e ),	/* 46 */
/* 11288 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11290 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11292 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11294 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11296 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11298 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11302 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11304 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11306 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11308 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTableCell */

/* 11310 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11312 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11316 */	NdrFcShort( 0x2f ),	/* 47 */
/* 11318 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11320 */	NdrFcShort( 0x10 ),	/* 16 */
/* 11322 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11324 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11326 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11328 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11330 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11332 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nRowSpan */

/* 11334 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11336 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11338 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nColSpan */

/* 11340 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11342 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11344 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11346 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11348 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11350 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseTableCell */

/* 11352 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11354 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11358 */	NdrFcShort( 0x30 ),	/* 48 */
/* 11360 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11362 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11364 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11366 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11368 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11374 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11376 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11378 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11380 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTableHeaderCell */

/* 11382 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11384 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11388 */	NdrFcShort( 0x31 ),	/* 49 */
/* 11390 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11392 */	NdrFcShort( 0x10 ),	/* 16 */
/* 11394 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11396 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11398 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11400 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11402 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11404 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nRowSpan */

/* 11406 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11408 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11410 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nColSpan */

/* 11412 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11414 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11416 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11418 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11420 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11422 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseTableHeaderCell */

/* 11424 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11426 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11430 */	NdrFcShort( 0x32 ),	/* 50 */
/* 11432 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11434 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11436 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11438 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11440 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11442 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11444 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11446 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11448 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11450 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11452 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeColumns */

/* 11454 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11456 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11460 */	NdrFcShort( 0x33 ),	/* 51 */
/* 11462 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11464 */	NdrFcShort( 0x20 ),	/* 32 */
/* 11466 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11468 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11470 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11472 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11474 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11476 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nColSpan */

/* 11478 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11480 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11482 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter vlWidth */

/* 11484 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11486 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11488 */	NdrFcShort( 0x79a ),	/* Type Offset=1946 */

	/* Return value */

/* 11490 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11492 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11494 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeColumnGroup */

/* 11496 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11498 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11502 */	NdrFcShort( 0x34 ),	/* 52 */
/* 11504 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11506 */	NdrFcShort( 0x20 ),	/* 32 */
/* 11508 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11510 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11512 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11514 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11516 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11518 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nColSpan */

/* 11520 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11522 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11524 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter vlWidth */

/* 11526 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11528 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11530 */	NdrFcShort( 0x79a ),	/* Type Offset=1946 */

	/* Return value */

/* 11532 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11534 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11536 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTableHeader */

/* 11538 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11540 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11544 */	NdrFcShort( 0x35 ),	/* 53 */
/* 11546 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11548 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11550 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11552 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11554 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11556 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11558 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11560 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11562 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11564 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11566 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenTableBody */

/* 11568 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11570 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11574 */	NdrFcShort( 0x39 ),	/* 57 */
/* 11576 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11578 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11580 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11582 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11584 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11588 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11590 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11592 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11594 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11596 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseTableBody */

/* 11598 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11600 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11604 */	NdrFcShort( 0x3a ),	/* 58 */
/* 11606 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11608 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11610 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11612 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 11614 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11616 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11618 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11620 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 11622 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11624 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11626 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_IntProperty */

/* 11628 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11630 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11634 */	NdrFcShort( 0x3b ),	/* 59 */
/* 11636 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11638 */	NdrFcShort( 0x18 ),	/* 24 */
/* 11640 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11642 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 11644 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11646 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11648 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11650 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tpt */

/* 11652 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11654 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11656 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tpv */

/* 11658 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11660 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11662 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nValue */

/* 11664 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11666 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11668 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11670 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11672 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11674 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_StringProperty */

/* 11676 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11678 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11682 */	NdrFcShort( 0x3c ),	/* 60 */
/* 11684 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11686 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11688 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11690 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 11692 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 11694 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11696 */	NdrFcShort( 0x1 ),	/* 1 */
/* 11698 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter sp */

/* 11700 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11702 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11704 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrValue */

/* 11706 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 11708 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11710 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 11712 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11714 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11716 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Props */

/* 11718 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11720 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11724 */	NdrFcShort( 0x3d ),	/* 61 */
/* 11726 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11728 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11730 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11732 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 11734 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11736 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11738 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11740 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pttp */

/* 11742 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11744 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11746 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Return value */

/* 11748 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11750 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11752 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StringWidth */

/* 11754 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11756 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11760 */	NdrFcShort( 0x3e ),	/* 62 */
/* 11762 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 11764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11766 */	NdrFcShort( 0x40 ),	/* 64 */
/* 11768 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 11770 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11772 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11776 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptss */

/* 11778 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11780 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11782 */	NdrFcShort( 0x9be ),	/* Type Offset=2494 */

	/* Parameter pttp */

/* 11784 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11786 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11788 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Parameter dmpx */

/* 11790 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11792 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11794 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dmpy */

/* 11796 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11798 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11800 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11802 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11804 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11806 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddPicture */

/* 11808 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11810 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11814 */	NdrFcShort( 0x3f ),	/* 63 */
/* 11816 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11818 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11820 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11822 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 11824 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11826 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11828 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11830 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppict */

/* 11832 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11834 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11836 */	NdrFcShort( 0x9d0 ),	/* Type Offset=2512 */

	/* Parameter tag */

/* 11838 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11840 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11842 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11844 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11846 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11848 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MoveWindow */

/* 11850 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11852 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11856 */	NdrFcShort( 0x3 ),	/* 3 */
/* 11858 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 11860 */	NdrFcShort( 0x20 ),	/* 32 */
/* 11862 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11864 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 11866 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11868 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11870 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11872 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 11874 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11876 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11878 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Parameter xdLeft */

/* 11880 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11882 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11884 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ydTop */

/* 11886 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11888 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11890 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dxdWidth */

/* 11892 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11894 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11896 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dydHeight */

/* 11898 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11900 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11902 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11904 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11906 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 11908 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsWindowVisible */

/* 11910 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11912 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11916 */	NdrFcShort( 0x4 ),	/* 4 */
/* 11918 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11920 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11922 */	NdrFcShort( 0x22 ),	/* 34 */
/* 11924 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 11926 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11928 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11930 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11932 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfRet */

/* 11934 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11936 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11938 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 11940 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11942 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11944 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawWindow */

/* 11946 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11948 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11952 */	NdrFcShort( 0x6 ),	/* 6 */
/* 11954 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11956 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11958 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11960 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 11962 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11964 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11966 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11968 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 11970 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11972 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11974 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Return value */

/* 11976 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11978 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11980 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_PossListId */


	/* Procedure get_Width */

/* 11982 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11984 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11988 */	NdrFcShort( 0x7 ),	/* 7 */
/* 11990 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11992 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11994 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11996 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 11998 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12000 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12004 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsslId */


	/* Parameter pnTwips */

/* 12006 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12008 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12010 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 12012 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12014 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12016 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Tag */


	/* Procedure get_Copies */


	/* Procedure get_Height */

/* 12018 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12020 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12024 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12026 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12028 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12030 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12032 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 12034 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12036 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12038 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12040 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptag */


	/* Parameter pnCopies */


	/* Parameter pnTwips */

/* 12042 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12044 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12046 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 12048 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12050 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12052 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Aborted */


	/* Procedure get_IsRange */

/* 12054 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12056 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12060 */	NdrFcShort( 0x7 ),	/* 7 */
/* 12062 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12064 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12066 */	NdrFcShort( 0x22 ),	/* 34 */
/* 12068 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 12070 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12072 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12074 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12076 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfAborted */


	/* Parameter pfRet */

/* 12078 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12080 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12082 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 12084 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12086 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12088 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetSelectionProps */

/* 12090 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12092 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12096 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12098 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12100 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12102 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12104 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 12106 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 12108 */	NdrFcShort( 0x2 ),	/* 2 */
/* 12110 */	NdrFcShort( 0x2 ),	/* 2 */
/* 12112 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cttpMax */

/* 12114 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12116 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12118 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgpttp */

/* 12120 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 12122 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12124 */	NdrFcShort( 0x9e6 ),	/* Type Offset=2534 */

	/* Parameter prgpvps */

/* 12126 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 12128 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12130 */	NdrFcShort( 0xa12 ),	/* Type Offset=2578 */

	/* Parameter pcttp */

/* 12132 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12134 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12136 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12138 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12140 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12142 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetHardAndSoftCharProps */

/* 12144 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12146 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12150 */	NdrFcShort( 0x9 ),	/* 9 */
/* 12152 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12154 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12156 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12158 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 12160 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 12162 */	NdrFcShort( 0x2 ),	/* 2 */
/* 12164 */	NdrFcShort( 0x2 ),	/* 2 */
/* 12166 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cttpMax */

/* 12168 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12170 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgpttpSel */

/* 12174 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 12176 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12178 */	NdrFcShort( 0x9e6 ),	/* Type Offset=2534 */

	/* Parameter prgpvpsSoft */

/* 12180 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 12182 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12184 */	NdrFcShort( 0xa12 ),	/* Type Offset=2578 */

	/* Parameter pcttp */

/* 12186 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12188 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12192 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12194 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetParaProps */

/* 12198 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12200 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12204 */	NdrFcShort( 0xa ),	/* 10 */
/* 12206 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12208 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12210 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12212 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 12214 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 12216 */	NdrFcShort( 0x1 ),	/* 1 */
/* 12218 */	NdrFcShort( 0x1 ),	/* 1 */
/* 12220 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cttpMax */

/* 12222 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12224 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12226 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgpvps */

/* 12228 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 12230 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12232 */	NdrFcShort( 0xa12 ),	/* Type Offset=2578 */

	/* Parameter pcttp */

/* 12234 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12236 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12240 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12242 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12244 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetHardAndSoftParaProps */

/* 12246 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12248 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12252 */	NdrFcShort( 0xb ),	/* 11 */
/* 12254 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12256 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12258 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12260 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 12262 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 12264 */	NdrFcShort( 0x2 ),	/* 2 */
/* 12266 */	NdrFcShort( 0x3 ),	/* 3 */
/* 12268 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cttpMax */

/* 12270 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12272 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12274 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgpttpPara */

/* 12276 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 12278 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12280 */	NdrFcShort( 0x9e6 ),	/* Type Offset=2534 */

	/* Parameter prgpttpHard */

/* 12282 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 12284 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12286 */	NdrFcShort( 0x9e6 ),	/* Type Offset=2534 */

	/* Parameter prgpvpsSoft */

/* 12288 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 12290 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12292 */	NdrFcShort( 0xa12 ),	/* Type Offset=2578 */

	/* Parameter pcttp */

/* 12294 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12296 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12298 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12300 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12302 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12304 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetSelectionProps */

/* 12306 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12308 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12312 */	NdrFcShort( 0xc ),	/* 12 */
/* 12314 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12316 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12318 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12320 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 12322 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 12324 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12326 */	NdrFcShort( 0x1 ),	/* 1 */
/* 12328 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cttp */

/* 12330 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12332 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12334 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgpttp */

/* 12336 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 12338 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12340 */	NdrFcShort( 0x9e6 ),	/* Type Offset=2534 */

	/* Return value */

/* 12342 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12344 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12346 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure TextSelInfo */

/* 12348 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12350 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12354 */	NdrFcShort( 0xd ),	/* 13 */
/* 12356 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 12358 */	NdrFcShort( 0x6 ),	/* 6 */
/* 12360 */	NdrFcShort( 0x92 ),	/* 146 */
/* 12362 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x8,		/* 8 */
/* 12364 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12366 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12370 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 12372 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12374 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12376 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 12378 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 12380 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12382 */	NdrFcShort( 0xa28 ),	/* Type Offset=2600 */

	/* Parameter pich */

/* 12384 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12386 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12388 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfAssocPrev */

/* 12390 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12392 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12394 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter phvoObj */

/* 12396 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12398 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12400 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptag */

/* 12402 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12404 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12406 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 12408 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12410 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12412 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12414 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12416 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 12418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CLevels */

/* 12420 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12422 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12426 */	NdrFcShort( 0xe ),	/* 14 */
/* 12428 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12430 */	NdrFcShort( 0x6 ),	/* 6 */
/* 12432 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12434 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 12436 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12438 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12440 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12442 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 12444 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12446 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12448 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pclev */

/* 12450 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12452 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12454 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12456 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12458 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12460 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PropInfo */

/* 12462 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12464 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12468 */	NdrFcShort( 0xf ),	/* 15 */
/* 12470 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 12472 */	NdrFcShort( 0xe ),	/* 14 */
/* 12474 */	NdrFcShort( 0x78 ),	/* 120 */
/* 12476 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x8,		/* 8 */
/* 12478 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12480 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12484 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 12486 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12488 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12490 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ilev */

/* 12492 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12494 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvoObj */

/* 12498 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12500 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptag */

/* 12504 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12506 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pihvo */

/* 12510 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12512 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12514 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcpropPrevious */

/* 12516 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12518 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12520 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppvps */

/* 12522 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 12524 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12526 */	NdrFcShort( 0xa2c ),	/* Type Offset=2604 */

	/* Return value */

/* 12528 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12530 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 12532 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AllTextSelInfo */

/* 12534 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12536 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12540 */	NdrFcShort( 0x10 ),	/* 16 */
/* 12542 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12544 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12546 */	NdrFcShort( 0xe6 ),	/* 230 */
/* 12548 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0xc,		/* 12 */
/* 12550 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 12552 */	NdrFcShort( 0x1 ),	/* 1 */
/* 12554 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12556 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pihvoRoot */

/* 12558 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12560 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12562 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvlsi */

/* 12564 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12566 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12568 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsli */

/* 12570 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 12572 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12574 */	NdrFcShort( 0x6d6 ),	/* Type Offset=1750 */

	/* Parameter ptagTextProp */

/* 12576 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12578 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12580 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcpropPrevious */

/* 12582 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12584 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12586 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichAnchor */

/* 12588 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12590 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichEnd */

/* 12594 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12596 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 12600 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12602 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 12604 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfAssocPrev */

/* 12606 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12608 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 12610 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pihvoEnd */

/* 12612 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12614 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 12616 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppttp */

/* 12618 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 12620 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12622 */	NdrFcShort( 0xa30 ),	/* Type Offset=2608 */

	/* Return value */

/* 12624 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12626 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 12628 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AllSelEndInfo */

/* 12630 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12632 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12636 */	NdrFcShort( 0x11 ),	/* 17 */
/* 12638 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 12640 */	NdrFcShort( 0xe ),	/* 14 */
/* 12642 */	NdrFcShort( 0xae ),	/* 174 */
/* 12644 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0xb,		/* 11 */
/* 12646 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 12648 */	NdrFcShort( 0x1 ),	/* 1 */
/* 12650 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12652 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 12654 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12656 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12658 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pihvoRoot */

/* 12660 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12662 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvlsi */

/* 12666 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12668 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12670 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsli */

/* 12672 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 12674 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12676 */	NdrFcShort( 0xa38 ),	/* Type Offset=2616 */

	/* Parameter ptagTextProp */

/* 12678 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12680 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12682 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcpropPrevious */

/* 12684 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12686 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12688 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pich */

/* 12690 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12692 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 12696 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12698 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 12700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfAssocPrev */

/* 12702 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12704 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 12706 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppttp */

/* 12708 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 12710 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 12712 */	NdrFcShort( 0xa30 ),	/* Type Offset=2608 */

	/* Return value */

/* 12714 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12716 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12718 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MatchCompatibility */


	/* Procedure Commit */

/* 12720 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12722 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12726 */	NdrFcShort( 0x12 ),	/* 18 */
/* 12728 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12730 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12732 */	NdrFcShort( 0x22 ),	/* 34 */
/* 12734 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 12736 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12738 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12740 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12742 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfMatch */


	/* Parameter pfOk */

/* 12744 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12746 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12748 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 12750 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12752 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12754 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CompleteEdits */

/* 12756 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12758 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12762 */	NdrFcShort( 0x13 ),	/* 19 */
/* 12764 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12768 */	NdrFcShort( 0x5a ),	/* 90 */
/* 12770 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 12772 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12776 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12778 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pci */

/* 12780 */	NdrFcShort( 0x6112 ),	/* Flags:  must free, out, simple ref, srv alloc size=24 */
/* 12782 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12784 */	NdrFcShort( 0xa4c ),	/* Type Offset=2636 */

	/* Parameter pfOk */

/* 12786 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12788 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12790 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12792 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12794 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12796 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ExtendToStringBoundaries */

/* 12798 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12800 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12804 */	NdrFcShort( 0x14 ),	/* 20 */
/* 12806 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12808 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12810 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12812 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 12814 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12818 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12820 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 12822 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12824 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_EndBeforeAnchor */

/* 12828 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12830 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12834 */	NdrFcShort( 0x15 ),	/* 21 */
/* 12836 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12840 */	NdrFcShort( 0x22 ),	/* 34 */
/* 12842 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 12844 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12846 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12848 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12850 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfRet */

/* 12852 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12854 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12856 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12858 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12860 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12862 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Location */

/* 12864 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12866 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12870 */	NdrFcShort( 0x16 ),	/* 22 */
/* 12872 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 12874 */	NdrFcShort( 0x40 ),	/* 64 */
/* 12876 */	NdrFcShort( 0xa4 ),	/* 164 */
/* 12878 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 12880 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12882 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12884 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12886 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 12888 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12890 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12892 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Parameter rcSrc */

/* 12894 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12896 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12898 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 12900 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12902 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12904 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter prdPrimary */

/* 12906 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 12908 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 12910 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter prdSecondary */

/* 12912 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 12914 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12916 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter pfSplit */

/* 12918 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12920 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 12922 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfEndBeforeAnchor */

/* 12924 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12926 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12928 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12930 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12932 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 12934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetParaLocation */

/* 12936 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12938 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12942 */	NdrFcShort( 0x17 ),	/* 23 */
/* 12944 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12948 */	NdrFcShort( 0x3c ),	/* 60 */
/* 12950 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 12952 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12954 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12956 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12958 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prdLoc */

/* 12960 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 12962 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12964 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Return value */

/* 12966 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12968 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReplaceWithTsString */

/* 12972 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12974 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12978 */	NdrFcShort( 0x18 ),	/* 24 */
/* 12980 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12982 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12984 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12986 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 12988 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12990 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12992 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12994 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptss */

/* 12996 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12998 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13000 */	NdrFcShort( 0x9be ),	/* Type Offset=2494 */

	/* Return value */

/* 13002 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13004 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13006 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetSelectionString */

/* 13008 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13010 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13014 */	NdrFcShort( 0x19 ),	/* 25 */
/* 13016 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13018 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13020 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13022 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 13024 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 13026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13028 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13030 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptss */

/* 13032 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13034 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13036 */	NdrFcShort( 0xa28 ),	/* Type Offset=2600 */

	/* Parameter bstrSep */

/* 13038 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 13040 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13042 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 13044 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13046 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFirstParaString */

/* 13050 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13052 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13056 */	NdrFcShort( 0x1a ),	/* 26 */
/* 13058 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13060 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13062 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13064 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 13066 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 13068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13070 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13072 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptss */

/* 13074 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13076 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13078 */	NdrFcShort( 0xa28 ),	/* Type Offset=2600 */

	/* Parameter bstrSep */

/* 13080 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 13082 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13084 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pfGotItAll */

/* 13086 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13088 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13090 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13092 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13094 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetIPLocation */

/* 13098 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13100 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13104 */	NdrFcShort( 0x1b ),	/* 27 */
/* 13106 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13108 */	NdrFcShort( 0xe ),	/* 14 */
/* 13110 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13112 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 13114 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13120 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fTopLine */

/* 13122 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13124 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13126 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter xdPos */

/* 13128 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13130 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13132 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13134 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13136 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13138 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CanFormatPara */

/* 13140 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13142 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13146 */	NdrFcShort( 0x1c ),	/* 28 */
/* 13148 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13152 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13154 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13156 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13158 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13160 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13162 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfRet */

/* 13164 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13166 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13168 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13170 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13172 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13174 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CanFormatChar */

/* 13176 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13178 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13182 */	NdrFcShort( 0x1d ),	/* 29 */
/* 13184 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13186 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13188 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13190 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13192 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13194 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13196 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13198 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfRet */

/* 13200 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13202 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13204 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13206 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13208 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13210 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CanFormatOverlay */

/* 13212 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13214 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13218 */	NdrFcShort( 0x1e ),	/* 30 */
/* 13220 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13222 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13224 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13226 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13228 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13230 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13232 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13234 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfRet */

/* 13236 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13238 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13240 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13242 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13244 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Follows */

/* 13248 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13250 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13254 */	NdrFcShort( 0x20 ),	/* 32 */
/* 13256 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13260 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13262 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 13264 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13266 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13270 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psel */

/* 13272 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13274 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13276 */	NdrFcShort( 0x6b4 ),	/* Type Offset=1716 */

	/* Parameter pfFollows */

/* 13278 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13280 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13282 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13284 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13286 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13288 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsValid */

/* 13290 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13292 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13296 */	NdrFcShort( 0x21 ),	/* 33 */
/* 13298 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13302 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13304 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13306 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13308 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13310 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13312 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfValid */

/* 13314 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13316 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13318 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13320 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13322 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13324 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ParagraphOffset */

/* 13326 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13328 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13332 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13334 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13336 */	NdrFcShort( 0x6 ),	/* 6 */
/* 13338 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13340 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 13342 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13346 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13348 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 13350 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13352 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13354 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pich */

/* 13356 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13358 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13360 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13362 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13364 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13366 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SelType */

/* 13368 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13370 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13374 */	NdrFcShort( 0x23 ),	/* 35 */
/* 13376 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13378 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13380 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13382 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13384 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13386 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13390 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter piType */

/* 13392 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13394 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13396 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 13398 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13400 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13402 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RootBox */

/* 13404 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13406 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13410 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13412 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13416 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13418 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 13420 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13422 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13424 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13426 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pprootb */

/* 13428 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13430 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13432 */	NdrFcShort( 0xa56 ),	/* Type Offset=2646 */

	/* Return value */

/* 13434 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13436 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13438 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GrowToWord */

/* 13440 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13442 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13446 */	NdrFcShort( 0x25 ),	/* 37 */
/* 13448 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13450 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13452 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13454 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 13456 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13458 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13460 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13462 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsel */

/* 13464 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13466 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13468 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 13470 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13472 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13474 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure EndPoint */

/* 13476 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13478 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13482 */	NdrFcShort( 0x26 ),	/* 38 */
/* 13484 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13486 */	NdrFcShort( 0x6 ),	/* 6 */
/* 13488 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13490 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 13492 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13494 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13496 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13498 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 13500 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13502 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13504 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 13506 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13508 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13510 */	NdrFcShort( 0x6b0 ),	/* Type Offset=1712 */

	/* Return value */

/* 13512 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13514 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13516 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetIpTypingProps */

/* 13518 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13520 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13524 */	NdrFcShort( 0x27 ),	/* 39 */
/* 13526 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13528 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13530 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13532 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 13534 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13536 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13538 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13540 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pttp */

/* 13542 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13544 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13546 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Return value */

/* 13548 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13550 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13552 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_BoxDepth */

/* 13554 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13556 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13560 */	NdrFcShort( 0x28 ),	/* 40 */
/* 13562 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13564 */	NdrFcShort( 0x6 ),	/* 6 */
/* 13566 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13568 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 13570 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13572 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13574 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13576 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 13578 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13580 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13582 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pcDepth */

/* 13584 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13586 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13588 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13590 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13592 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13594 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_BoxIndex */

/* 13596 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13598 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13602 */	NdrFcShort( 0x29 ),	/* 41 */
/* 13604 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13606 */	NdrFcShort( 0xe ),	/* 14 */
/* 13608 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13610 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 13612 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13614 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13616 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13618 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 13620 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13622 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13624 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter iLevel */

/* 13626 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13628 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13630 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter piAtLevel */

/* 13632 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13634 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13636 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13638 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13640 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13642 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_BoxCount */

/* 13644 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13646 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13650 */	NdrFcShort( 0x2a ),	/* 42 */
/* 13652 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13654 */	NdrFcShort( 0xe ),	/* 14 */
/* 13656 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13658 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 13660 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13662 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13664 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13666 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 13668 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13670 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13672 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter iLevel */

/* 13674 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13676 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcAtLevel */

/* 13680 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13682 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13684 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13686 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13688 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13690 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_BoxType */

/* 13692 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13694 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13698 */	NdrFcShort( 0x2b ),	/* 43 */
/* 13700 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13702 */	NdrFcShort( 0xe ),	/* 14 */
/* 13704 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13706 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 13708 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13710 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13712 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13714 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fEndPoint */

/* 13716 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13718 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13720 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter iLevel */

/* 13722 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13724 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13726 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvbt */

/* 13728 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13730 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13732 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 13734 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13736 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13738 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Notify */

/* 13740 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13742 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13746 */	NdrFcShort( 0x3 ),	/* 3 */
/* 13748 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13750 */	NdrFcShort( 0x10 ),	/* 16 */
/* 13752 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13754 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 13756 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13758 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13760 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13762 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nArg1 */

/* 13764 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13766 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13768 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nArg2 */

/* 13770 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13772 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13774 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13776 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13778 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Name */


	/* Procedure GetDefaultBasedOnStyleName */

/* 13782 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13784 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13788 */	NdrFcShort( 0x3 ),	/* 3 */
/* 13790 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13792 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13794 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13796 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 13798 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13800 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13802 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13804 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstrNormal */

/* 13806 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 13808 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13810 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */


	/* Return value */

/* 13812 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13814 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13816 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StringProperty */


	/* Procedure GetDefaultStyleForContext */

/* 13818 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13820 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13824 */	NdrFcShort( 0x4 ),	/* 4 */
/* 13826 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13828 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13830 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13832 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 13834 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13836 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13840 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter sp */


	/* Parameter nContext */

/* 13842 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13844 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13846 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrValue */


	/* Parameter pbstrStyleName */

/* 13848 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 13850 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13852 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */


	/* Return value */

/* 13854 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13856 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13858 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PutStyle */

/* 13860 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13862 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13866 */	NdrFcShort( 0x5 ),	/* 5 */
/* 13868 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 13870 */	NdrFcShort( 0x2c ),	/* 44 */
/* 13872 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13874 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xa,		/* 10 */
/* 13876 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 13878 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13880 */	NdrFcShort( 0x2 ),	/* 2 */
/* 13882 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 13884 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 13886 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13888 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter bstrUsage */

/* 13890 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 13892 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13894 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter hvoStyle */

/* 13896 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13898 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13900 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoBasedOn */

/* 13902 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13904 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13906 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoNext */

/* 13908 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13910 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13912 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nType */

/* 13914 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13916 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 13918 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fBuiltIn */

/* 13920 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13922 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 13924 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fModified */

/* 13926 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13928 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 13930 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pttp */

/* 13932 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13934 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 13936 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Return value */

/* 13938 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13940 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 13942 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetStyleRgch */

/* 13944 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13946 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13950 */	NdrFcShort( 0x6 ),	/* 6 */
/* 13952 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13954 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13956 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13958 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 13960 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 13962 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13964 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13966 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cch */

/* 13968 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13970 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13972 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchName */

/* 13974 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 13976 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13978 */	NdrFcShort( 0xa70 ),	/* Type Offset=2672 */

	/* Parameter ppttp */

/* 13980 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13982 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13984 */	NdrFcShort( 0xa30 ),	/* Type Offset=2608 */

	/* Return value */

/* 13986 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13988 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13990 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetNextStyle */

/* 13992 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13994 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13998 */	NdrFcShort( 0x7 ),	/* 7 */
/* 14000 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14004 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14006 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14008 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 14010 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14012 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14014 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 14016 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14018 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14020 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pbstrNext */

/* 14022 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 14024 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14026 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 14028 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14030 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14032 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBasedOn */

/* 14034 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14036 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14040 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14042 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14044 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14046 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14048 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14050 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 14052 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14054 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14056 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 14058 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14060 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14062 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pbstrBasedOn */

/* 14064 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 14066 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14068 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 14070 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14072 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14074 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetType */

/* 14076 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14078 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14082 */	NdrFcShort( 0x9 ),	/* 9 */
/* 14084 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14086 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14088 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14090 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14092 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14094 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14096 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14098 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 14100 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14102 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14104 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pnType */

/* 14106 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14108 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14110 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 14112 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14114 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14116 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetContext */

/* 14118 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14120 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14124 */	NdrFcShort( 0xa ),	/* 10 */
/* 14126 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14128 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14130 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14132 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14134 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14136 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14138 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14140 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 14142 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14144 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14146 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pnContext */

/* 14148 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14150 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14152 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 14154 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14156 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14158 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsBuiltIn */

/* 14160 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14162 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14166 */	NdrFcShort( 0xb ),	/* 11 */
/* 14168 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14170 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14172 */	NdrFcShort( 0x22 ),	/* 34 */
/* 14174 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14176 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14178 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14180 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14182 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 14184 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14186 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14188 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pfBuiltIn */

/* 14190 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14192 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14194 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 14196 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14198 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14200 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsModified */

/* 14202 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14204 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14208 */	NdrFcShort( 0xc ),	/* 12 */
/* 14210 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14212 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14214 */	NdrFcShort( 0x22 ),	/* 34 */
/* 14216 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14218 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14220 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14222 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14224 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 14226 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14228 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14230 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pfModified */

/* 14232 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14234 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14236 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 14238 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14240 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14242 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DataAccess */

/* 14244 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14246 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14250 */	NdrFcShort( 0xd ),	/* 13 */
/* 14252 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14254 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14256 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14258 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14260 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14262 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14264 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14266 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsda */

/* 14268 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14270 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14272 */	NdrFcShort( 0x616 ),	/* Type Offset=1558 */

	/* Return value */

/* 14274 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14276 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FontSize */


	/* Procedure MakeNewStyle */

/* 14280 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14282 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14286 */	NdrFcShort( 0xe ),	/* 14 */
/* 14288 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14290 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14292 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14294 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14296 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14298 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14302 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pmp */


	/* Parameter phvoNewStyle */

/* 14304 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14306 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14308 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 14310 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14312 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14314 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_FontSize */


	/* Procedure Delete */

/* 14316 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14318 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14322 */	NdrFcShort( 0xf ),	/* 15 */
/* 14324 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14326 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14328 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14330 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14332 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14336 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14338 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter mp */


	/* Parameter hvoStyle */

/* 14340 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14342 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14344 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 14346 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14348 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14350 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LastPageNo */


	/* Procedure get_MaxShowTags */


	/* Procedure get_CStyles */

/* 14352 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14354 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14358 */	NdrFcShort( 0x10 ),	/* 16 */
/* 14360 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14362 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14364 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14366 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14368 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14374 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnPageNo */


	/* Parameter pctag */


	/* Parameter pcttp */

/* 14376 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14378 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14380 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 14382 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14384 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14386 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NthStyle */

/* 14388 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14390 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14394 */	NdrFcShort( 0x11 ),	/* 17 */
/* 14396 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14398 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14400 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14402 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 14404 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14406 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14408 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14410 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ihvo */

/* 14412 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14414 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14416 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvo */

/* 14418 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14420 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14422 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 14424 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14426 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14428 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NthStyleName */

/* 14430 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14432 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14436 */	NdrFcShort( 0x12 ),	/* 18 */
/* 14438 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14440 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14442 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14444 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 14446 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 14448 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14450 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14452 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ihvo */

/* 14454 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14456 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14458 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrStyleName */

/* 14460 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 14462 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14464 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 14466 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14468 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NormalFontStyle */

/* 14472 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14474 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14478 */	NdrFcShort( 0x13 ),	/* 19 */
/* 14480 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14484 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14486 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14488 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14490 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14492 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14494 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppttp */

/* 14496 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14498 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14500 */	NdrFcShort( 0xa30 ),	/* Type Offset=2608 */

	/* Return value */

/* 14502 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14504 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14506 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsStyleProtected */

/* 14508 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14510 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14514 */	NdrFcShort( 0x14 ),	/* 20 */
/* 14516 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14520 */	NdrFcShort( 0x22 ),	/* 34 */
/* 14522 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14524 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14526 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14528 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14530 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 14532 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14534 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14536 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pfProtected */

/* 14538 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14540 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14542 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 14544 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14546 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14548 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CacheProps */

/* 14550 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14552 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14556 */	NdrFcShort( 0x15 ),	/* 21 */
/* 14558 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 14560 */	NdrFcShort( 0x10 ),	/* 16 */
/* 14562 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14564 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 14566 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14568 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14570 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14572 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cch */

/* 14574 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14576 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14578 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchName */

/* 14580 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 14582 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14584 */	NdrFcShort( 0xa70 ),	/* Type Offset=2672 */

	/* Parameter hvoStyle */

/* 14586 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14588 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14590 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pttp */

/* 14592 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14594 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14596 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Return value */

/* 14598 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14600 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 14602 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IntProperty */

/* 14604 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14606 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14610 */	NdrFcShort( 0x3 ),	/* 3 */
/* 14612 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14614 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14616 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14618 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 14620 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14622 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14624 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14626 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nID */

/* 14628 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14630 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14632 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnValue */

/* 14634 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14636 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14638 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 14640 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14642 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14644 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ChrpFor */

/* 14646 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14648 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14652 */	NdrFcShort( 0x5 ),	/* 5 */
/* 14654 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14656 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14658 */	NdrFcShort( 0x13c ),	/* 316 */
/* 14660 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14662 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14664 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14666 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14668 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pttp */

/* 14670 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14672 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14674 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Parameter pchrp */

/* 14676 */	NdrFcShort( 0x112 ),	/* Flags:  must free, out, simple ref, */
/* 14678 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14680 */	NdrFcShort( 0x984 ),	/* Type Offset=2436 */

	/* Return value */

/* 14682 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14684 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14686 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_Stylesheet */

/* 14688 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14690 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14694 */	NdrFcShort( 0x6 ),	/* 6 */
/* 14696 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14698 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14700 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14702 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14704 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14706 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14708 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14710 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvps */

/* 14712 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14714 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14716 */	NdrFcShort( 0xa80 ),	/* Type Offset=2688 */

	/* Return value */

/* 14718 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14720 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14722 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_WritingSystemFactory */

/* 14724 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14726 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14730 */	NdrFcShort( 0x7 ),	/* 7 */
/* 14732 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14734 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14736 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14738 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14740 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14742 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14744 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14746 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 14748 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14750 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14752 */	NdrFcShort( 0xa92 ),	/* Type Offset=2706 */

	/* Return value */

/* 14754 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14756 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14758 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ParentStore */

/* 14760 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14762 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14766 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14768 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14770 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14772 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14774 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14776 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14778 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14780 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14782 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppvps */

/* 14784 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14786 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14788 */	NdrFcShort( 0xa2c ),	/* Type Offset=2604 */

	/* Return value */

/* 14790 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14792 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14794 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_TextProps */

/* 14796 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14798 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14802 */	NdrFcShort( 0x9 ),	/* 9 */
/* 14804 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14806 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14808 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14810 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14812 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14814 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14818 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppttp */

/* 14820 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14822 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14824 */	NdrFcShort( 0xa30 ),	/* Type Offset=2608 */

	/* Return value */

/* 14826 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14828 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14830 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DerivedPropertiesForTtp */

/* 14832 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14834 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14838 */	NdrFcShort( 0xa ),	/* 10 */
/* 14840 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14842 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14844 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14846 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14848 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14850 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14852 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14854 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pttp */

/* 14856 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14858 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14860 */	NdrFcShort( 0x6e6 ),	/* Type Offset=1766 */

	/* Parameter ppvps */

/* 14862 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14864 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14866 */	NdrFcShort( 0xa2c ),	/* Type Offset=2604 */

	/* Return value */

/* 14868 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14870 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14872 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Name */

/* 14874 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14876 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14880 */	NdrFcShort( 0x4 ),	/* 4 */
/* 14882 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14884 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14886 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14888 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14890 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14892 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14894 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14896 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 14898 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14900 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14902 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 14904 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14906 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14908 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Guid */

/* 14910 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14912 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14916 */	NdrFcShort( 0x5 ),	/* 5 */
/* 14918 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14920 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14922 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14924 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14926 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 14928 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14930 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14932 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchGuid */

/* 14934 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 14936 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14938 */	NdrFcShort( 0xaa8 ),	/* Type Offset=2728 */

	/* Return value */

/* 14940 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14942 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14944 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Guid */

/* 14946 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14948 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14952 */	NdrFcShort( 0x6 ),	/* 6 */
/* 14954 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14956 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14958 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14960 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14962 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14964 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14966 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14968 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchGuid */

/* 14970 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 14972 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14974 */	NdrFcShort( 0xaa8 ),	/* Type Offset=2728 */

	/* Return value */

/* 14976 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14978 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14980 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Flags */

/* 14982 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14984 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14988 */	NdrFcShort( 0x9 ),	/* 9 */
/* 14990 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14992 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14994 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14996 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14998 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15000 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15004 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvof */

/* 15006 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15008 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15010 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 15012 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15014 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15016 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Flags */

/* 15018 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15020 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15024 */	NdrFcShort( 0xa ),	/* 10 */
/* 15026 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15028 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15030 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15032 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15034 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15036 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15038 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15040 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter vof */

/* 15042 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15044 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15046 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 15048 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15050 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15052 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FontName */

/* 15054 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15056 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15060 */	NdrFcShort( 0xb ),	/* 11 */
/* 15062 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15064 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15066 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15068 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15070 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15072 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15074 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15076 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 15078 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15080 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15082 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 15084 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15086 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15088 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_FontName */

/* 15090 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15092 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15096 */	NdrFcShort( 0xc ),	/* 12 */
/* 15098 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15100 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15102 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15104 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 15106 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15108 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15110 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15112 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 15114 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15116 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15118 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 15120 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15122 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FontNameRgch */

/* 15126 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15128 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15132 */	NdrFcShort( 0xd ),	/* 13 */
/* 15134 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15136 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15138 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15140 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15142 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15144 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15146 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15148 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgch */

/* 15150 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 15152 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15154 */	NdrFcShort( 0xab8 ),	/* Type Offset=2744 */

	/* Return value */

/* 15156 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15158 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15160 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CTags */

/* 15162 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15164 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15168 */	NdrFcShort( 0x12 ),	/* 18 */
/* 15170 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15172 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15174 */	NdrFcShort( 0x24 ),	/* 36 */
/* 15176 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15178 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15180 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15182 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15184 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pctag */

/* 15186 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15188 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15192 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15194 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDbTagInfo */

/* 15198 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15200 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15204 */	NdrFcShort( 0x13 ),	/* 19 */
/* 15206 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 15208 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15210 */	NdrFcShort( 0xae ),	/* 174 */
/* 15212 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x9,		/* 9 */
/* 15214 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15216 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15218 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15220 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter itag */

/* 15222 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15224 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15226 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phvo */

/* 15228 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15230 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15232 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrFore */

/* 15234 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15236 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrBack */

/* 15240 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15242 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15244 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrUnder */

/* 15246 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15248 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 15250 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter punt */

/* 15252 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15254 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 15256 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfHidden */

/* 15258 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15260 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 15262 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter prgchGuid */

/* 15264 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 15266 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 15268 */	NdrFcShort( 0xaa8 ),	/* Type Offset=2728 */

	/* Return value */

/* 15270 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15272 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 15274 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetTagInfo */

/* 15276 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15278 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15282 */	NdrFcShort( 0x14 ),	/* 20 */
/* 15284 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 15286 */	NdrFcShort( 0x36 ),	/* 54 */
/* 15288 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15290 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xb,		/* 11 */
/* 15292 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15294 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15296 */	NdrFcShort( 0x3 ),	/* 3 */
/* 15298 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchGuid */

/* 15300 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 15302 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15304 */	NdrFcShort( 0xaa8 ),	/* Type Offset=2728 */

	/* Parameter hvo */

/* 15306 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15308 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15310 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter grfosm */

/* 15312 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15314 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15316 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrAbbr */

/* 15318 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15320 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15322 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter bstrName */

/* 15324 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15326 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 15328 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter clrFore */

/* 15330 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15332 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 15334 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter clrBack */

/* 15336 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15338 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 15340 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter clrUnder */

/* 15342 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15344 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 15346 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter unt */

/* 15348 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15350 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 15352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fHidden */

/* 15354 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15356 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 15358 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15360 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15362 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 15364 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetTagInfo */

/* 15366 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15368 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15372 */	NdrFcShort( 0x15 ),	/* 21 */
/* 15374 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 15376 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15378 */	NdrFcShort( 0xae ),	/* 174 */
/* 15380 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0xa,		/* 10 */
/* 15382 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 15384 */	NdrFcShort( 0x2 ),	/* 2 */
/* 15386 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15388 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchGuid */

/* 15390 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 15392 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15394 */	NdrFcShort( 0xaa8 ),	/* Type Offset=2728 */

	/* Parameter phvo */

/* 15396 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15398 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15400 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrAbbr */

/* 15402 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15404 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15406 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Parameter pbstrName */

/* 15408 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15410 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15412 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Parameter pclrFore */

/* 15414 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15416 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 15418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrBack */

/* 15420 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15422 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 15424 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrUnder */

/* 15426 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15428 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 15430 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter punt */

/* 15432 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15434 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 15436 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfHidden */

/* 15438 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15440 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 15442 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15444 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15446 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 15448 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDlgTagInfo */

/* 15450 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15452 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15456 */	NdrFcShort( 0x16 ),	/* 22 */
/* 15458 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 15460 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15462 */	NdrFcShort( 0x92 ),	/* 146 */
/* 15464 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x9,		/* 9 */
/* 15466 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15468 */	NdrFcShort( 0x2 ),	/* 2 */
/* 15470 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15472 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter itag */

/* 15474 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15476 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15478 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfHidden */

/* 15480 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15482 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15484 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pclrFore */

/* 15486 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15488 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15490 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrBack */

/* 15492 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15494 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrUnder */

/* 15498 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15500 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 15502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter punt */

/* 15504 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15506 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 15508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrAbbr */

/* 15510 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15512 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 15514 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Parameter pbstrName */

/* 15516 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15518 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 15520 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 15522 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15524 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 15526 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDispTagInfo */

/* 15528 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15530 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15534 */	NdrFcShort( 0x17 ),	/* 23 */
/* 15536 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 15538 */	NdrFcShort( 0x10 ),	/* 16 */
/* 15540 */	NdrFcShort( 0xca ),	/* 202 */
/* 15542 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0xd,		/* 13 */
/* 15544 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 15546 */	NdrFcShort( 0x2 ),	/* 2 */
/* 15548 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15550 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchGuid */

/* 15552 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 15554 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15556 */	NdrFcShort( 0xaa8 ),	/* Type Offset=2728 */

	/* Parameter pfHidden */

/* 15558 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15560 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15562 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pclrFore */

/* 15564 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15566 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15568 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrBack */

/* 15570 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15572 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15574 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pclrUnder */

/* 15576 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15578 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 15580 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter punt */

/* 15582 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15584 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 15586 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchAbbr */

/* 15588 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 15590 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 15592 */	NdrFcShort( 0xac8 ),	/* Type Offset=2760 */

	/* Parameter cchMaxAbbr */

/* 15594 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15596 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 15598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcchAbbr */

/* 15600 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15602 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 15604 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchName */

/* 15606 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 15608 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 15610 */	NdrFcShort( 0xad8 ),	/* Type Offset=2776 */

	/* Parameter cchMaxName */

/* 15612 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15614 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 15616 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcchName */

/* 15618 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15620 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 15622 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15624 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15626 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 15628 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RemoveTag */

/* 15630 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15632 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15636 */	NdrFcShort( 0x18 ),	/* 24 */
/* 15638 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15640 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15642 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15644 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 15646 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15648 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15650 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15652 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchGuid */

/* 15654 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 15656 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15658 */	NdrFcShort( 0xaa8 ),	/* Type Offset=2728 */

	/* Return value */

/* 15660 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15662 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Sort */

/* 15666 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15668 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15672 */	NdrFcShort( 0x19 ),	/* 25 */
/* 15674 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15676 */	NdrFcShort( 0x6 ),	/* 6 */
/* 15678 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15680 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15682 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15684 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15688 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fByAbbr */

/* 15690 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15692 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15694 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15696 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15698 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Merge */

/* 15702 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15704 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15708 */	NdrFcShort( 0x1a ),	/* 26 */
/* 15710 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15712 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15714 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15716 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 15718 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15720 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15722 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15724 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvo */

/* 15726 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 15728 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15730 */	NdrFcShort( 0x688 ),	/* Type Offset=1672 */

	/* Parameter ppvoMerged */

/* 15732 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 15734 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15736 */	NdrFcShort( 0x69a ),	/* Type Offset=1690 */

	/* Return value */

/* 15738 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15740 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15742 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Graphics */

/* 15744 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15746 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15750 */	NdrFcShort( 0x3 ),	/* 3 */
/* 15752 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15754 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15756 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15758 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15760 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15762 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15766 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppvg */

/* 15768 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 15770 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15772 */	NdrFcShort( 0xae4 ),	/* Type Offset=2788 */

	/* Return value */

/* 15774 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15776 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15778 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FirstPageNumber */

/* 15780 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15782 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15786 */	NdrFcShort( 0x4 ),	/* 4 */
/* 15788 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15790 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15792 */	NdrFcShort( 0x24 ),	/* 36 */
/* 15794 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15796 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15800 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15802 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pn */

/* 15804 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15806 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15808 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15810 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15812 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15814 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsPageWanted */

/* 15816 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15818 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15822 */	NdrFcShort( 0x5 ),	/* 5 */
/* 15824 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15826 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15828 */	NdrFcShort( 0x22 ),	/* 34 */
/* 15830 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 15832 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15834 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15836 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15838 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nPageNo */

/* 15840 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15842 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15844 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfWanted */

/* 15846 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15848 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15850 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15852 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15854 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15856 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_AreMorePagesWanted */

/* 15858 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15860 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15864 */	NdrFcShort( 0x6 ),	/* 6 */
/* 15866 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15868 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15870 */	NdrFcShort( 0x22 ),	/* 34 */
/* 15872 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 15874 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15876 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15878 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15880 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nPageNo */

/* 15882 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15884 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15886 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfWanted */

/* 15888 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15890 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15892 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15894 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15896 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15898 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Collate */

/* 15900 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15902 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15906 */	NdrFcShort( 0x9 ),	/* 9 */
/* 15908 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15910 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15912 */	NdrFcShort( 0x22 ),	/* 34 */
/* 15914 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15916 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15918 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15920 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15922 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfCollate */

/* 15924 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15926 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15928 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15930 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15932 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_HeaderString */

/* 15936 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15938 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15942 */	NdrFcShort( 0xa ),	/* 10 */
/* 15944 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 15946 */	NdrFcShort( 0x10 ),	/* 16 */
/* 15948 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15950 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 15952 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15954 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15956 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15958 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter grfvhp */

/* 15960 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15962 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15964 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pn */

/* 15966 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15968 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 15972 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 15974 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15976 */	NdrFcShort( 0xa28 ),	/* Type Offset=2600 */

	/* Return value */

/* 15978 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15980 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15982 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetMargins */

/* 15984 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15986 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15990 */	NdrFcShort( 0xb ),	/* 11 */
/* 15992 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 15994 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15996 */	NdrFcShort( 0xb0 ),	/* 176 */
/* 15998 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x7,		/* 7 */
/* 16000 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16004 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16006 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pdxpLeft */

/* 16008 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16010 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16012 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdxpRight */

/* 16014 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16016 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16018 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdypHeader */

/* 16020 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16022 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16024 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdypTop */

/* 16026 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16028 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16030 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdypBottom */

/* 16032 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16034 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16036 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdypFooter */

/* 16038 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16040 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 16042 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 16044 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16046 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 16048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenPage */

/* 16050 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16052 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16056 */	NdrFcShort( 0xc ),	/* 12 */
/* 16058 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16060 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16062 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16064 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 16066 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16070 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16072 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 16074 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16076 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16078 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ClosePage */

/* 16080 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16082 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16086 */	NdrFcShort( 0xd ),	/* 13 */
/* 16088 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16090 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16092 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16094 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 16096 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16098 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16100 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16102 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 16104 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16106 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16108 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure OpenDoc */

/* 16110 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16112 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16116 */	NdrFcShort( 0xe ),	/* 14 */
/* 16118 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16120 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16122 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16124 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 16126 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16128 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16130 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16132 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 16134 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16136 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16138 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseDoc */

/* 16140 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16142 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16146 */	NdrFcShort( 0xf ),	/* 15 */
/* 16148 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16152 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16154 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 16156 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16158 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16160 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16162 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 16164 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16166 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16168 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_HeaderMask */

/* 16170 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16172 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16176 */	NdrFcShort( 0x11 ),	/* 17 */
/* 16178 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16180 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16182 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16184 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16186 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16188 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16190 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16192 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter grfvhp */

/* 16194 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16196 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16198 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 16200 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16202 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16204 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetHeaderString */

/* 16206 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16208 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16212 */	NdrFcShort( 0x12 ),	/* 18 */
/* 16214 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16216 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16218 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16220 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 16222 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16224 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16226 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16228 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter grfvhp */

/* 16230 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16232 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16234 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 16236 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16238 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16240 */	NdrFcShort( 0x9be ),	/* Type Offset=2494 */

	/* Return value */

/* 16242 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16244 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetMargins */

/* 16248 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16250 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16254 */	NdrFcShort( 0x13 ),	/* 19 */
/* 16256 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 16258 */	NdrFcShort( 0x30 ),	/* 48 */
/* 16260 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16262 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x7,		/* 7 */
/* 16264 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16266 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16270 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter dxpLeft */

/* 16272 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16274 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16276 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dxpRight */

/* 16278 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16280 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16282 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dypHeader */

/* 16284 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16286 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16288 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dypTop */

/* 16290 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16292 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16294 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dypBottom */

/* 16296 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16298 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16300 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dypFooter */

/* 16302 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16304 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 16306 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 16308 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16310 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 16312 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetPagePrintInfo */

/* 16314 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16316 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16320 */	NdrFcShort( 0x14 ),	/* 20 */
/* 16322 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 16324 */	NdrFcShort( 0x26 ),	/* 38 */
/* 16326 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16328 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 16330 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16332 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16336 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nFirstPageNo */

/* 16338 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16340 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16342 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nFirstPrintPage */

/* 16344 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16346 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16348 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nLastPrintPage */

/* 16350 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16352 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16354 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nCopies */

/* 16356 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16358 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16360 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fCollate */

/* 16362 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16364 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16366 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 16368 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16370 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 16372 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetGraphics */

/* 16374 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16376 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16380 */	NdrFcShort( 0x15 ),	/* 21 */
/* 16382 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16384 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16386 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16388 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 16390 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16392 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16394 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16396 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 16398 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16400 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16402 */	NdrFcShort( 0x71a ),	/* Type Offset=1818 */

	/* Return value */

/* 16404 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16406 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16408 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddRedoCommand */

/* 16410 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16412 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16416 */	NdrFcShort( 0x3 ),	/* 3 */
/* 16418 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16420 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16422 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16424 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 16426 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16428 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16430 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16432 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 16434 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16436 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16438 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter podc */

/* 16440 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16442 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16444 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter bstrSql */

/* 16446 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16448 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16450 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 16452 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16454 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16456 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddUndoCommand */

/* 16458 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16460 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16464 */	NdrFcShort( 0x4 ),	/* 4 */
/* 16466 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16468 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16470 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16472 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 16474 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16476 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16478 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16480 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 16482 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16484 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16486 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter podc */

/* 16488 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16490 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16492 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter bstrSql */

/* 16494 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16496 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16498 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 16500 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16502 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16504 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure VerifyUndoable */

/* 16506 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16508 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16512 */	NdrFcShort( 0x5 ),	/* 5 */
/* 16514 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16516 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16518 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16520 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 16522 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16524 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16526 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16528 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 16530 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16532 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16534 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter podc */

/* 16536 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16538 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16540 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter bstrSql */

/* 16542 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16544 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16546 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 16548 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16550 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16552 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure VerifyRedoable */

/* 16554 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16556 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16560 */	NdrFcShort( 0x6 ),	/* 6 */
/* 16562 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16564 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16566 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16568 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 16570 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16572 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16574 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16576 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 16578 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16580 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16582 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter podc */

/* 16584 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16586 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16588 */	NdrFcShort( 0xae8 ),	/* Type Offset=2792 */

	/* Parameter bstrSql */

/* 16590 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16592 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16594 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 16596 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16598 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddRedoReloadInfo */

/* 16602 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16604 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16608 */	NdrFcShort( 0x7 ),	/* 7 */
/* 16610 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 16612 */	NdrFcShort( 0x10 ),	/* 16 */
/* 16614 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16616 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 16618 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16620 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16622 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16624 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter podda */

/* 16626 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16628 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16630 */	NdrFcShort( 0xafa ),	/* Type Offset=2810 */

	/* Parameter bstrSqlReloadData */

/* 16632 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16634 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16636 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pdcs */

/* 16638 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16640 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16642 */	NdrFcShort( 0xb0c ),	/* Type Offset=2828 */

	/* Parameter hvoBase */

/* 16644 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16646 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16648 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nrowMax */

/* 16650 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16652 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16654 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter padvi */

/* 16656 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16658 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 16660 */	NdrFcShort( 0xb1e ),	/* Type Offset=2846 */

	/* Return value */

/* 16662 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16664 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 16666 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddUndoReloadInfo */

/* 16668 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16670 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16674 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16676 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 16678 */	NdrFcShort( 0x10 ),	/* 16 */
/* 16680 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16682 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 16684 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16688 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16690 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter podda */

/* 16692 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16694 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16696 */	NdrFcShort( 0xafa ),	/* Type Offset=2810 */

	/* Parameter bstrSqlReloadData */

/* 16698 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16700 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16702 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter pdcs */

/* 16704 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16706 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16708 */	NdrFcShort( 0xb0c ),	/* Type Offset=2828 */

	/* Parameter hvoBase */

/* 16710 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16712 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nrowMax */

/* 16716 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16718 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter padvi */

/* 16722 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16724 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 16726 */	NdrFcShort( 0xb1e ),	/* Type Offset=2846 */

	/* Return value */

/* 16728 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16730 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 16732 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_Pattern */

/* 16734 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16736 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16740 */	NdrFcShort( 0x3 ),	/* 3 */
/* 16742 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16744 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16746 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16748 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 16750 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16754 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16756 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptssPattern */

/* 16758 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16760 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16762 */	NdrFcShort( 0xb30 ),	/* Type Offset=2864 */

	/* Return value */

/* 16764 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16766 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16768 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Pattern */

/* 16770 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16772 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16776 */	NdrFcShort( 0x4 ),	/* 4 */
/* 16778 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16780 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16782 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16784 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16786 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16788 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16790 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16792 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptssPattern */

/* 16794 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 16796 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16798 */	NdrFcShort( 0xb42 ),	/* Type Offset=2882 */

	/* Return value */

/* 16800 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16802 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16804 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_Overlay */

/* 16806 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16808 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16812 */	NdrFcShort( 0x5 ),	/* 5 */
/* 16814 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16818 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16820 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 16822 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16824 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16826 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16828 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvo */

/* 16830 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16832 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16834 */	NdrFcShort( 0xb46 ),	/* Type Offset=2886 */

	/* Return value */

/* 16836 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16838 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16840 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Overlay */

/* 16842 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16844 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16848 */	NdrFcShort( 0x6 ),	/* 6 */
/* 16850 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16852 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16854 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16856 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16858 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16860 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16862 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16864 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppvo */

/* 16866 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 16868 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16870 */	NdrFcShort( 0xb58 ),	/* Type Offset=2904 */

	/* Return value */

/* 16872 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16874 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16876 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_MatchCase */

/* 16878 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16880 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16884 */	NdrFcShort( 0x7 ),	/* 7 */
/* 16886 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16888 */	NdrFcShort( 0x6 ),	/* 6 */
/* 16890 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16892 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16894 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16896 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16898 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16900 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fMatch */

/* 16902 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16904 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16906 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 16908 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16910 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16912 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MatchCase */

/* 16914 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16916 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16920 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16922 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16926 */	NdrFcShort( 0x22 ),	/* 34 */
/* 16928 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16930 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16932 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16934 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16936 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfMatch */

/* 16938 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16940 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16942 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 16944 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16946 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16948 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_MatchDiacritics */

/* 16950 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16952 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16956 */	NdrFcShort( 0x9 ),	/* 9 */
/* 16958 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16960 */	NdrFcShort( 0x6 ),	/* 6 */
/* 16962 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16964 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16966 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16970 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16972 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fMatch */

/* 16974 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16976 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16978 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 16980 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16982 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16984 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MatchDiacritics */

/* 16986 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16988 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16992 */	NdrFcShort( 0xa ),	/* 10 */
/* 16994 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16996 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16998 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17000 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17002 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17004 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17006 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17008 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfMatch */

/* 17010 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17012 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17014 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17016 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17018 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17020 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Writeable */


	/* Procedure put_MatchWholeWord */

/* 17022 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17024 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17028 */	NdrFcShort( 0xb ),	/* 11 */
/* 17030 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17032 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17034 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17036 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17038 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17040 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17042 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17044 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter f */


	/* Parameter fMatch */

/* 17046 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17048 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17050 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 17052 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17054 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17056 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Writeable */


	/* Procedure get_MatchWholeWord */

/* 17058 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17060 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17064 */	NdrFcShort( 0xc ),	/* 12 */
/* 17066 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17070 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17072 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17074 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17076 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17078 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17080 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pf */


	/* Parameter pfMatch */

/* 17082 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17084 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17086 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 17088 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17090 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17092 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_ComputeEveryTime */


	/* Procedure put_MatchOldWritingSystem */

/* 17094 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17096 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17100 */	NdrFcShort( 0xd ),	/* 13 */
/* 17102 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17104 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17106 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17108 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17110 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17112 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17114 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17116 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter f */


	/* Parameter fMatch */

/* 17118 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17120 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17122 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 17124 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17126 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17128 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ComputeEveryTime */


	/* Procedure get_MatchOldWritingSystem */

/* 17130 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17132 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17136 */	NdrFcShort( 0xe ),	/* 14 */
/* 17138 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17140 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17142 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17144 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17146 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17148 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17152 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pf */


	/* Parameter pfMatch */

/* 17154 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17156 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17158 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 17160 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17162 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17164 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_MatchExactly */

/* 17166 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17168 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17172 */	NdrFcShort( 0xf ),	/* 15 */
/* 17174 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17176 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17178 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17180 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17182 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17184 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17186 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17188 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fMatch */

/* 17190 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17192 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17194 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17196 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17198 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17200 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MatchExactly */

/* 17202 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17204 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17208 */	NdrFcShort( 0x10 ),	/* 16 */
/* 17210 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17212 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17214 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17216 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17218 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17220 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17222 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17224 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfMatch */

/* 17226 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17228 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17230 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17232 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17234 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17236 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_MatchCompatibility */

/* 17238 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17240 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17244 */	NdrFcShort( 0x11 ),	/* 17 */
/* 17246 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17248 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17250 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17252 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17254 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17256 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17260 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fMatch */

/* 17262 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17264 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17266 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17268 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17270 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17272 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Find */

/* 17274 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17276 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17280 */	NdrFcShort( 0x13 ),	/* 19 */
/* 17282 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17284 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17286 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17288 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 17290 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17292 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17294 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17296 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prootb */

/* 17298 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17300 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17302 */	NdrFcShort( 0xb5c ),	/* Type Offset=2908 */

	/* Parameter fForward */

/* 17304 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17306 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17308 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pxserkl */

/* 17310 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17312 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17314 */	NdrFcShort( 0xb6e ),	/* Type Offset=2926 */

	/* Return value */

/* 17316 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17318 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17320 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FindFrom */

/* 17322 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17324 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17328 */	NdrFcShort( 0x14 ),	/* 20 */
/* 17330 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17332 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17334 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17336 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 17338 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17340 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17344 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psel */

/* 17346 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17348 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17350 */	NdrFcShort( 0xb80 ),	/* Type Offset=2944 */

	/* Parameter fForward */

/* 17352 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17354 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17356 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pxserkl */

/* 17358 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17360 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17362 */	NdrFcShort( 0xb6e ),	/* Type Offset=2926 */

	/* Return value */

/* 17364 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17366 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17368 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FindNext */

/* 17370 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17372 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17376 */	NdrFcShort( 0x15 ),	/* 21 */
/* 17378 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17380 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17382 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17384 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 17386 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17390 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17392 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fForward */

/* 17394 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17396 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17398 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pxserkl */

/* 17400 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17402 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17404 */	NdrFcShort( 0xb6e ),	/* Type Offset=2926 */

	/* Return value */

/* 17406 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17408 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17410 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FindIn */

/* 17412 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17414 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17418 */	NdrFcShort( 0x16 ),	/* 22 */
/* 17420 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 17422 */	NdrFcShort( 0x16 ),	/* 22 */
/* 17424 */	NdrFcShort( 0x40 ),	/* 64 */
/* 17426 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 17428 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17430 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17432 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17434 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pts */

/* 17436 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17438 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17440 */	NdrFcShort( 0xb92 ),	/* Type Offset=2962 */

	/* Parameter ichStart */

/* 17442 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17444 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17446 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichEnd */

/* 17448 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17450 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17452 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fForward */

/* 17454 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17456 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17458 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pichMinFound */

/* 17460 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17462 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17464 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLimFound */

/* 17466 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17468 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 17470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pxserkl */

/* 17472 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17474 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 17476 */	NdrFcShort( 0xb6e ),	/* Type Offset=2926 */

	/* Return value */

/* 17478 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17480 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 17482 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Found */

/* 17484 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17486 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17490 */	NdrFcShort( 0x18 ),	/* 24 */
/* 17492 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17494 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17496 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17498 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17500 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17502 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17504 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17506 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfFound */

/* 17508 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17510 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17512 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17514 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17516 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17518 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetSelection */

/* 17520 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17522 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17526 */	NdrFcShort( 0x19 ),	/* 25 */
/* 17528 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17530 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17532 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17534 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 17536 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17538 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17540 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17542 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fInstall */

/* 17544 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17546 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17548 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 17550 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 17552 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17554 */	NdrFcShort( 0xba4 ),	/* Type Offset=2980 */

	/* Return value */

/* 17556 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17558 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17560 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CLevels */

/* 17562 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17564 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17568 */	NdrFcShort( 0x1a ),	/* 26 */
/* 17570 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17572 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17574 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17576 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17578 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17580 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17582 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17584 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pclev */

/* 17586 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17588 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17590 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 17592 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17594 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17596 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AllTextSelInfo */

/* 17598 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17600 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17604 */	NdrFcShort( 0x1b ),	/* 27 */
/* 17606 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 17608 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17610 */	NdrFcShort( 0xb0 ),	/* 176 */
/* 17612 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x9,		/* 9 */
/* 17614 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 17616 */	NdrFcShort( 0x1 ),	/* 1 */
/* 17618 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17620 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pihvoRoot */

/* 17622 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17624 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17626 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvlsi */

/* 17628 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17630 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17632 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsli */

/* 17634 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 17636 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17638 */	NdrFcShort( 0x6d6 ),	/* Type Offset=1750 */

	/* Parameter ptagTextProp */

/* 17640 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17642 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17644 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcpropPrevious */

/* 17646 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17648 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17650 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichAnchor */

/* 17652 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17654 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 17656 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichEnd */

/* 17658 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17660 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 17662 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 17664 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17666 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 17668 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 17670 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17672 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 17674 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MatchWhole */

/* 17676 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17678 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17682 */	NdrFcShort( 0x1c ),	/* 28 */
/* 17684 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17688 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17690 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 17692 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17694 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17696 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17698 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psel */

/* 17700 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17702 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17704 */	NdrFcShort( 0xb80 ),	/* Type Offset=2944 */

	/* Parameter pfMatch */

/* 17706 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17708 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17710 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17712 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17714 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17716 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_Limit */

/* 17718 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17720 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17724 */	NdrFcShort( 0x1d ),	/* 29 */
/* 17726 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17728 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17730 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17732 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 17734 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17736 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17738 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17740 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psel */

/* 17742 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17744 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17746 */	NdrFcShort( 0xb80 ),	/* Type Offset=2944 */

	/* Return value */

/* 17748 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17750 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17752 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Limit */

/* 17754 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17756 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17760 */	NdrFcShort( 0x1e ),	/* 30 */
/* 17762 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17766 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17768 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 17770 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17772 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17776 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsel */

/* 17778 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 17780 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17782 */	NdrFcShort( 0xba4 ),	/* Type Offset=2980 */

	/* Return value */

/* 17784 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17786 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17788 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_StartingPoint */

/* 17790 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17792 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17796 */	NdrFcShort( 0x1f ),	/* 31 */
/* 17798 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17800 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17802 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17804 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 17806 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17808 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17810 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17812 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter psel */

/* 17814 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17816 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17818 */	NdrFcShort( 0xb80 ),	/* Type Offset=2944 */

	/* Return value */

/* 17820 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17822 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17824 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StartingPoint */

/* 17826 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17828 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17832 */	NdrFcShort( 0x20 ),	/* 32 */
/* 17834 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17836 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17838 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17840 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 17842 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17844 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17846 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17848 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsel */

/* 17850 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 17852 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17854 */	NdrFcShort( 0xba4 ),	/* Type Offset=2980 */

	/* Return value */

/* 17856 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17858 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17860 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_SearchWindow */

/* 17862 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17864 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17868 */	NdrFcShort( 0x21 ),	/* 33 */
/* 17870 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17872 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17874 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17876 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17878 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17880 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17882 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17884 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hwnd */

/* 17886 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17888 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17890 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 17892 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17894 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17896 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SearchWindow */

/* 17898 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17900 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17904 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17906 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17908 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17910 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17912 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17914 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17916 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17918 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17920 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phwnd */

/* 17922 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17924 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17926 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 17928 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17930 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17932 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StoppedAtLimit */

/* 17934 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17936 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17940 */	NdrFcShort( 0x23 ),	/* 35 */
/* 17942 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17944 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17946 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17948 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17950 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17952 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17954 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17956 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfAtLimit */

/* 17958 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17960 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17962 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17964 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17966 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17968 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_StoppedAtLimit */

/* 17970 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17972 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17976 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17978 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17980 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17982 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17984 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 17986 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17988 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17990 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17992 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fAtLimit */

/* 17994 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17996 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17998 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18000 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18002 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18004 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LastDirection */

/* 18006 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18008 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18012 */	NdrFcShort( 0x25 ),	/* 37 */
/* 18014 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18016 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18018 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18020 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18022 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18024 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18028 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfForward */

/* 18030 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18032 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18034 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18036 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18038 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18040 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_ReplaceWith */

/* 18042 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18044 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18048 */	NdrFcShort( 0x26 ),	/* 38 */
/* 18050 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18052 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18054 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18056 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 18058 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18060 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18062 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18064 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptssPattern */

/* 18066 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18068 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18070 */	NdrFcShort( 0xb30 ),	/* Type Offset=2864 */

	/* Return value */

/* 18072 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18074 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18076 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ReplaceWith */

/* 18078 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18080 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18084 */	NdrFcShort( 0x27 ),	/* 39 */
/* 18086 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18088 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18090 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18092 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 18094 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18096 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18098 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18100 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptssPattern */

/* 18102 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 18104 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18106 */	NdrFcShort( 0xb42 ),	/* Type Offset=2882 */

	/* Return value */

/* 18108 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18110 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_ShowMore */

/* 18114 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18116 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18120 */	NdrFcShort( 0x28 ),	/* 40 */
/* 18122 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18124 */	NdrFcShort( 0x6 ),	/* 6 */
/* 18126 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18128 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18130 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18132 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18134 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18136 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fMore */

/* 18138 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18140 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18142 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18144 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18146 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18148 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ShowMore */

/* 18150 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18152 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18156 */	NdrFcShort( 0x29 ),	/* 41 */
/* 18158 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18160 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18162 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18164 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18166 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18168 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18170 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18172 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfMore */

/* 18174 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18176 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18178 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18180 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18182 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18184 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IcuLocale */

/* 18186 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18188 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18192 */	NdrFcShort( 0x2a ),	/* 42 */
/* 18194 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18196 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18198 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18200 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 18202 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 18204 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18206 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18208 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrLocale */

/* 18210 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18212 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18214 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 18216 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18218 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18220 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_IcuLocale */

/* 18222 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18224 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18228 */	NdrFcShort( 0x2b ),	/* 43 */
/* 18230 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18232 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18234 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18236 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 18238 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 18240 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18242 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18244 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrLocale */

/* 18246 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18248 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18250 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 18252 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18254 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18256 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IcuCollatingRules */

/* 18258 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18260 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18264 */	NdrFcShort( 0x2c ),	/* 44 */
/* 18266 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18270 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18272 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 18274 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 18276 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18278 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18280 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrRules */

/* 18282 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18284 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18286 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 18288 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18290 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18292 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_IcuCollatingRules */

/* 18294 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18296 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18300 */	NdrFcShort( 0x2d ),	/* 45 */
/* 18302 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18304 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18306 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18308 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 18310 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 18312 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18314 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18316 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrRules */

/* 18318 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18320 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18322 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 18324 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18326 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18328 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Window */

/* 18330 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18332 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18336 */	NdrFcShort( 0x3 ),	/* 3 */
/* 18338 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18340 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18342 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18344 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18346 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18348 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18350 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18352 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hwnd */

/* 18354 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18356 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18358 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18360 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18362 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18364 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FlushMessages */

/* 18366 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18368 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18372 */	NdrFcShort( 0x4 ),	/* 4 */
/* 18374 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18376 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18378 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18380 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 18382 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18384 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18386 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18388 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 18390 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18392 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18394 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_AbortRequest */

/* 18396 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18398 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18402 */	NdrFcShort( 0x5 ),	/* 5 */
/* 18404 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18406 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18408 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18410 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18412 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18416 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18418 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfAbort */

/* 18420 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18422 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18424 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18426 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18428 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18430 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_AbortRequest */

/* 18432 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18434 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18438 */	NdrFcShort( 0x6 ),	/* 6 */
/* 18440 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18442 */	NdrFcShort( 0x6 ),	/* 6 */
/* 18444 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18446 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18448 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18450 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18452 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18454 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fAbort */

/* 18456 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18458 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18460 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18462 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18464 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18466 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawTheRoot */

/* 18468 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18470 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18474 */	NdrFcShort( 0x3 ),	/* 3 */
/* 18476 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 18478 */	NdrFcShort( 0x2e ),	/* 46 */
/* 18480 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18482 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 18484 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 18486 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18488 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18490 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prootb */

/* 18492 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18494 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18496 */	NdrFcShort( 0xb5c ),	/* Type Offset=2908 */

	/* Parameter hdc */

/* 18498 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18500 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18502 */	NdrFcShort( 0xbc0 ),	/* Type Offset=3008 */

	/* Parameter rcpDraw */

/* 18504 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 18506 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18508 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter bkclr */

/* 18510 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18512 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18514 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fDrawSel */

/* 18516 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18518 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 18520 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pvrs */

/* 18522 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18524 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 18526 */	NdrFcShort( 0xbca ),	/* Type Offset=3018 */

	/* Return value */

/* 18528 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18530 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 18532 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawTheRootAt */

/* 18534 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18536 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18540 */	NdrFcShort( 0x4 ),	/* 4 */
/* 18542 */	NdrFcShort( 0x54 ),	/* x86 Stack size/offset = 84 */
/* 18544 */	NdrFcShort( 0x7e ),	/* 126 */
/* 18546 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18548 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xb,		/* 11 */
/* 18550 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 18552 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18554 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18556 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prootb */

/* 18558 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18560 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18562 */	NdrFcShort( 0xb5c ),	/* Type Offset=2908 */

	/* Parameter hdc */

/* 18564 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18566 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18568 */	NdrFcShort( 0xbc0 ),	/* Type Offset=3008 */

	/* Parameter rcpDraw */

/* 18570 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 18572 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18574 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter bkclr */

/* 18576 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18578 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18580 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fDrawSel */

/* 18582 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18584 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 18586 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 18588 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18590 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 18592 */	NdrFcShort( 0xbdc ),	/* Type Offset=3036 */

	/* Parameter rcSrc */

/* 18594 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 18596 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 18598 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter rcDst */

/* 18600 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 18602 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 18604 */	NdrFcShort( 0x70c ),	/* Type Offset=1804 */

	/* Parameter ysTop */

/* 18606 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18608 */	NdrFcShort( 0x48 ),	/* x86 Stack size/offset = 72 */
/* 18610 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dysHeight */

/* 18612 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18614 */	NdrFcShort( 0x4c ),	/* x86 Stack size/offset = 76 */
/* 18616 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18618 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18620 */	NdrFcShort( 0x50 ),	/* x86 Stack size/offset = 80 */
/* 18622 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddRoot */

/* 18624 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18626 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18630 */	NdrFcShort( 0x3 ),	/* 3 */
/* 18632 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18634 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18636 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18638 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 18640 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18642 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18646 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prootb */

/* 18648 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18650 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18652 */	NdrFcShort( 0xb5c ),	/* Type Offset=2908 */

	/* Return value */

/* 18654 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18656 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18658 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddField */

/* 18660 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18662 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18666 */	NdrFcShort( 0x3 ),	/* 3 */
/* 18668 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18670 */	NdrFcShort( 0x20 ),	/* 32 */
/* 18672 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18674 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 18676 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18678 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18680 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18682 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter clsid */

/* 18684 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18686 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18688 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 18690 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18692 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ft */

/* 18696 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18698 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18700 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pwsf */

/* 18702 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18704 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18706 */	NdrFcShort( 0xbee ),	/* Type Offset=3054 */

	/* Parameter ws */

/* 18708 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18710 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18712 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18714 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18716 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18718 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ObjDeleted */

/* 18720 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18722 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18726 */	NdrFcShort( 0x3 ),	/* 3 */
/* 18728 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18730 */	NdrFcShort( 0x44 ),	/* 68 */
/* 18732 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18734 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18736 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18738 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18740 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18742 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pguid */

/* 18744 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 18746 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18748 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 18750 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18752 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18754 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetManager */

/* 18756 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18758 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18762 */	NdrFcShort( 0x3 ),	/* 3 */
/* 18764 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18768 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18770 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 18772 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18776 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18778 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter plm */

/* 18780 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18782 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18784 */	NdrFcShort( 0xc00 ),	/* Type Offset=3072 */

	/* Return value */

/* 18786 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18788 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18790 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LayoutObj */

/* 18792 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18794 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18798 */	NdrFcShort( 0x4 ),	/* 4 */
/* 18800 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 18802 */	NdrFcShort( 0x20 ),	/* 32 */
/* 18804 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18806 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 18808 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 18810 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18812 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18814 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 18816 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18818 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18820 */	NdrFcShort( 0xbdc ),	/* Type Offset=3036 */

	/* Parameter dxsAvailWidth */

/* 18822 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18824 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoRoot */

/* 18828 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18830 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18832 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cvsli */

/* 18834 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18836 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18838 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvsli */

/* 18840 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 18842 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18844 */	NdrFcShort( 0x6fc ),	/* Type Offset=1788 */

	/* Parameter hPage */

/* 18846 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18848 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18850 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18852 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18854 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18856 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LayoutPage */

/* 18858 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18860 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18864 */	NdrFcShort( 0x5 ),	/* 5 */
/* 18866 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 18868 */	NdrFcShort( 0x34 ),	/* 52 */
/* 18870 */	NdrFcShort( 0x5c ),	/* 92 */
/* 18872 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 18874 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18876 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18878 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18880 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 18882 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 18884 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18886 */	NdrFcShort( 0xbdc ),	/* Type Offset=3036 */

	/* Parameter dxsAvailWidth */

/* 18888 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18890 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18892 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dysAvailHeight */

/* 18894 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18896 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18898 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pysStartThisPage */

/* 18900 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 18902 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18904 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hPage */

/* 18906 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18908 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18910 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdysUsedHeight */

/* 18912 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18914 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18916 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pysStartNextPage */

/* 18918 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18920 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18922 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18924 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18926 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 18928 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DiscardPage */

/* 18930 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18932 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18936 */	NdrFcShort( 0x6 ),	/* 6 */
/* 18938 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18940 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18942 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18944 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 18946 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18948 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18950 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18952 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hPage */

/* 18954 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18956 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18958 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18960 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18962 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18964 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PageBoundary */

/* 18966 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18968 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18972 */	NdrFcShort( 0x7 ),	/* 7 */
/* 18974 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18976 */	NdrFcShort( 0xe ),	/* 14 */
/* 18978 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18980 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 18982 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18984 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18986 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18988 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hPage */

/* 18990 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18992 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18994 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fEnd */

/* 18996 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18998 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19000 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsel */

/* 19002 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 19004 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19006 */	NdrFcShort( 0xba4 ),	/* Type Offset=2980 */

	/* Return value */

/* 19008 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19010 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19012 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CommitLayoutObjects */

/* 19014 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19016 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19020 */	NdrFcShort( 0xb ),	/* 11 */
/* 19022 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19024 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19026 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19028 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 19030 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19032 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19034 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19036 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hPage */

/* 19038 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19040 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19042 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19044 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19046 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddDependentObjects */

/* 19050 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19052 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19056 */	NdrFcShort( 0x3 ),	/* 3 */
/* 19058 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 19060 */	NdrFcShort( 0x32 ),	/* 50 */
/* 19062 */	NdrFcShort( 0x3e ),	/* 62 */
/* 19064 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x9,		/* 9 */
/* 19066 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19070 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19072 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter play */

/* 19074 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19076 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19078 */	NdrFcShort( 0xc12 ),	/* Type Offset=3090 */

	/* Parameter pvg */

/* 19080 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19082 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19084 */	NdrFcShort( 0xbdc ),	/* Type Offset=3036 */

	/* Parameter hPage */

/* 19086 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19088 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19090 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cguid */

/* 19092 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19094 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgguidObj */

/* 19098 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19100 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19102 */	NdrFcShort( 0xc28 ),	/* Type Offset=3112 */

	/* Parameter fAllowFail */

/* 19104 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19106 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19108 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfFailed */

/* 19110 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19112 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19114 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pdysAvailHeight */

/* 19116 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 19118 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 19120 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19122 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19124 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 19126 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PageBroken */

/* 19128 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19130 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19134 */	NdrFcShort( 0x4 ),	/* 4 */
/* 19136 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19138 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19140 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19142 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 19144 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19146 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19148 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19150 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter play */

/* 19152 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19154 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19156 */	NdrFcShort( 0xc12 ),	/* Type Offset=3090 */

	/* Parameter hPage */

/* 19158 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19160 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19162 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19164 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19166 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19168 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PageBoundaryMoved */

/* 19170 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19172 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19176 */	NdrFcShort( 0x5 ),	/* 5 */
/* 19178 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19180 */	NdrFcShort( 0x10 ),	/* 16 */
/* 19182 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19184 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 19186 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19188 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19190 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19192 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter play */

/* 19194 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19196 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19198 */	NdrFcShort( 0xc12 ),	/* Type Offset=3090 */

	/* Parameter hPage */

/* 19200 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19202 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19204 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichOld */

/* 19206 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19208 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19210 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19212 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19214 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19216 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure EstimateHeight */

/* 19218 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19220 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19224 */	NdrFcShort( 0x6 ),	/* 6 */
/* 19226 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19228 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19230 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19232 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 19234 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19236 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19238 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19240 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter dxpWidth */

/* 19242 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19244 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdxpHeight */

/* 19248 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19250 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19252 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19254 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19256 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19258 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_ClassName */

/* 19260 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19262 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19266 */	NdrFcShort( 0x3 ),	/* 3 */
/* 19268 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19272 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19274 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 19276 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19278 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19280 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19282 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 19284 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19286 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19288 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 19290 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19292 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19294 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ClassName */

/* 19296 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19298 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19302 */	NdrFcShort( 0x4 ),	/* 4 */
/* 19304 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19306 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19308 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19310 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 19312 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 19314 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19318 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 19320 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 19322 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19324 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 19326 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19328 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19330 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_FieldName */

/* 19332 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19334 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19338 */	NdrFcShort( 0x5 ),	/* 5 */
/* 19340 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19344 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19346 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 19348 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19350 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19352 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19354 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 19356 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19358 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19360 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 19362 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19364 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19366 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FieldName */

/* 19368 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19370 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19374 */	NdrFcShort( 0x6 ),	/* 6 */
/* 19376 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19378 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19380 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19382 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 19384 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 19386 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19390 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 19392 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 19394 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19396 */	NdrFcShort( 0x476 ),	/* Type Offset=1142 */

	/* Return value */

/* 19398 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19400 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19402 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Type */

/* 19404 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19406 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19410 */	NdrFcShort( 0xa ),	/* 10 */
/* 19412 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19416 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19418 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 19420 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19422 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19424 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19426 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcpt */

/* 19428 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19430 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19432 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19434 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19436 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19438 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Load */

/* 19440 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19442 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19446 */	NdrFcShort( 0xf ),	/* 15 */
/* 19448 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19450 */	NdrFcShort( 0x18 ),	/* 24 */
/* 19452 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19454 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 19456 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19458 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19460 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19462 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 19464 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19466 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19468 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 19470 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19472 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19474 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 19476 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19478 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19480 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcda */

/* 19482 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19484 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19486 */	NdrFcShort( 0xc38 ),	/* Type Offset=3128 */

	/* Return value */

/* 19488 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19490 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19492 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Replace */

/* 19494 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19496 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19500 */	NdrFcShort( 0x10 ),	/* 16 */
/* 19502 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 19504 */	NdrFcShort( 0x28 ),	/* 40 */
/* 19506 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19508 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 19510 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19512 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19514 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19516 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 19518 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19520 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19522 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 19524 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19526 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19528 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoMin */

/* 19530 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19532 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19534 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ihvoLim */

/* 19536 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19538 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19540 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prghvo */

/* 19542 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19544 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19546 */	NdrFcShort( 0x4d0 ),	/* Type Offset=1232 */

	/* Parameter chvo */

/* 19548 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19550 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19552 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter psda */

/* 19554 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19556 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19558 */	NdrFcShort( 0x604 ),	/* Type Offset=1540 */

	/* Return value */

/* 19560 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19562 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 19564 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteObj */

/* 19566 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19568 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19572 */	NdrFcShort( 0x11 ),	/* 17 */
/* 19574 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19576 */	NdrFcShort( 0x18 ),	/* 24 */
/* 19578 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19580 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19582 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19584 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19588 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 19590 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19592 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19594 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 19596 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19598 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 19602 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19604 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19606 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter punk */

/* 19608 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19610 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19612 */	NdrFcShort( 0x5ca ),	/* Type Offset=1482 */

	/* Parameter psda */

/* 19614 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19616 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19618 */	NdrFcShort( 0x604 ),	/* Type Offset=1540 */

	/* Return value */

/* 19620 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19622 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19624 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteInt64 */

/* 19626 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19628 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19632 */	NdrFcShort( 0x12 ),	/* 18 */
/* 19634 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19636 */	NdrFcShort( 0x20 ),	/* 32 */
/* 19638 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19640 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 19642 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19646 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19648 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 19650 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19652 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19654 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 19656 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19658 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19660 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter val */

/* 19662 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19664 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19666 */	0xb,		/* FC_HYPER */
			0x0,		/* 0 */

	/* Parameter psda */

/* 19668 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19670 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19672 */	NdrFcShort( 0x604 ),	/* Type Offset=1540 */

	/* Return value */

/* 19674 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19676 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteUnicode */

/* 19680 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19682 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19686 */	NdrFcShort( 0x13 ),	/* 19 */
/* 19688 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19690 */	NdrFcShort( 0x10 ),	/* 16 */
/* 19692 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19694 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 19696 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19698 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19700 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19702 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvo */

/* 19704 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19706 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19708 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 19710 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19712 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */

/* 19716 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19718 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19720 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Parameter psda */

/* 19722 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19724 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19726 */	NdrFcShort( 0x604 ),	/* Type Offset=1540 */

	/* Return value */

/* 19728 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19730 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19732 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PreLoad */

/* 19734 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19736 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19740 */	NdrFcShort( 0x14 ),	/* 20 */
/* 19742 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19744 */	NdrFcShort( 0x18 ),	/* 24 */
/* 19746 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19748 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19750 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19754 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19756 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter chvo */

/* 19758 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19760 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19762 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prghvo */

/* 19764 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19766 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19768 */	NdrFcShort( 0x528 ),	/* Type Offset=1320 */

	/* Parameter tag */

/* 19770 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19772 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19774 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 19776 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19778 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcda */

/* 19782 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19784 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19786 */	NdrFcShort( 0xc38 ),	/* Type Offset=3128 */

	/* Return value */

/* 19788 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19790 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19792 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Initialize */

/* 19794 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19796 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19800 */	NdrFcShort( 0x15 ),	/* 21 */
/* 19802 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19804 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19806 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19808 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 19810 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19812 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19814 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19816 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrData */

/* 19818 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19820 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19822 */	NdrFcShort( 0x484 ),	/* Type Offset=1156 */

	/* Return value */

/* 19824 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19826 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19828 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DoesResultDependOnProp */

/* 19830 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19832 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19836 */	NdrFcShort( 0x16 ),	/* 22 */
/* 19838 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19840 */	NdrFcShort( 0x20 ),	/* 32 */
/* 19842 */	NdrFcShort( 0x22 ),	/* 34 */
/* 19844 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 19846 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19848 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19850 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19852 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hvoObj */

/* 19854 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19856 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19858 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoChange */

/* 19860 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19862 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19864 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tag */

/* 19866 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19868 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19870 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 19872 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19874 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19876 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfDepends */

/* 19878 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19880 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19882 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 19884 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19886 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19888 */	0x8,		/* FC_LONG */
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
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/*  4 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/*  6 */
			0x11, 0x0,	/* FC_RP */
/*  8 */	NdrFcShort( 0x2 ),	/* Offset= 2 (10) */
/* 10 */
			0x1c,		/* FC_CVARRAY */
			0x3,		/* 3 */
/* 12 */	NdrFcShort( 0x4 ),	/* 4 */
/* 14 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 16 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 20 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 22 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 24 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 26 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 28 */
			0x11, 0x0,	/* FC_RP */
/* 30 */	NdrFcShort( 0x2 ),	/* Offset= 2 (32) */
/* 32 */
			0x1c,		/* FC_CVARRAY */
			0x0,		/* 0 */
/* 34 */	NdrFcShort( 0x1 ),	/* 1 */
/* 36 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 38 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 40 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 42 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 44 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 46 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 48 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 50 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 52 */	NdrFcShort( 0x8 ),	/* Offset= 8 (60) */
/* 54 */
			0x1d,		/* FC_SMFARRAY */
			0x0,		/* 0 */
/* 56 */	NdrFcShort( 0x8 ),	/* 8 */
/* 58 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 60 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 62 */	NdrFcShort( 0x10 ),	/* 16 */
/* 64 */	0x8,		/* FC_LONG */
			0x6,		/* FC_SHORT */
/* 66 */	0x6,		/* FC_SHORT */
			0x4c,		/* FC_EMBEDDED_COMPLEX */
/* 68 */	0x0,		/* 0 */
			NdrFcShort( 0xfff1 ),	/* Offset= -15 (54) */
			0x5b,		/* FC_END */
/* 72 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 74 */	0xb,		/* FC_HYPER */
			0x5c,		/* FC_PAD */
/* 76 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 78 */	NdrFcShort( 0x2 ),	/* Offset= 2 (80) */
/* 80 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 82 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 86 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 88 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 90 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 92 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 94 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 96 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 98 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 100 */	NdrFcShort( 0x2 ),	/* Offset= 2 (102) */
/* 102 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 104 */	NdrFcLong( 0xdd409520 ),	/* -582970080 */
/* 108 */	NdrFcShort( 0xc212 ),	/* -15854 */
/* 110 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 112 */	0x9b,		/* 155 */
			0xb7,		/* 183 */
/* 114 */	0x0,		/* 0 */
			0x40,		/* 64 */
/* 116 */	0x5,		/* 5 */
			0x41,		/* 65 */
/* 118 */	0xf9,		/* 249 */
			0xe9,		/* 233 */
/* 120 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 122 */	NdrFcShort( 0x3ee ),	/* Offset= 1006 (1128) */
/* 124 */
			0x13, 0x0,	/* FC_OP */
/* 126 */	NdrFcShort( 0x3d6 ),	/* Offset= 982 (1108) */
/* 128 */
			0x2b,		/* FC_NON_ENCAPSULATED_UNION */
			0x9,		/* FC_ULONG */
/* 130 */	0x7,		/* Corr desc: FC_USHORT */
			0x0,		/*  */
/* 132 */	NdrFcShort( 0xfff8 ),	/* -8 */
/* 134 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 136 */	NdrFcShort( 0x2 ),	/* Offset= 2 (138) */
/* 138 */	NdrFcShort( 0x10 ),	/* 16 */
/* 140 */	NdrFcShort( 0x2f ),	/* 47 */
/* 142 */	NdrFcLong( 0x14 ),	/* 20 */
/* 146 */	NdrFcShort( 0x800b ),	/* Simple arm type: FC_HYPER */
/* 148 */	NdrFcLong( 0x3 ),	/* 3 */
/* 152 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 154 */	NdrFcLong( 0x11 ),	/* 17 */
/* 158 */	NdrFcShort( 0x8001 ),	/* Simple arm type: FC_BYTE */
/* 160 */	NdrFcLong( 0x2 ),	/* 2 */
/* 164 */	NdrFcShort( 0x8006 ),	/* Simple arm type: FC_SHORT */
/* 166 */	NdrFcLong( 0x4 ),	/* 4 */
/* 170 */	NdrFcShort( 0x800a ),	/* Simple arm type: FC_FLOAT */
/* 172 */	NdrFcLong( 0x5 ),	/* 5 */
/* 176 */	NdrFcShort( 0x800c ),	/* Simple arm type: FC_DOUBLE */
/* 178 */	NdrFcLong( 0xb ),	/* 11 */
/* 182 */	NdrFcShort( 0x8006 ),	/* Simple arm type: FC_SHORT */
/* 184 */	NdrFcLong( 0xa ),	/* 10 */
/* 188 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 190 */	NdrFcLong( 0x6 ),	/* 6 */
/* 194 */	NdrFcShort( 0xe8 ),	/* Offset= 232 (426) */
/* 196 */	NdrFcLong( 0x7 ),	/* 7 */
/* 200 */	NdrFcShort( 0x800c ),	/* Simple arm type: FC_DOUBLE */
/* 202 */	NdrFcLong( 0x8 ),	/* 8 */
/* 206 */	NdrFcShort( 0xe2 ),	/* Offset= 226 (432) */
/* 208 */	NdrFcLong( 0xd ),	/* 13 */
/* 212 */	NdrFcShort( 0xf6 ),	/* Offset= 246 (458) */
/* 214 */	NdrFcLong( 0x9 ),	/* 9 */
/* 218 */	NdrFcShort( 0x102 ),	/* Offset= 258 (476) */
/* 220 */	NdrFcLong( 0x2000 ),	/* 8192 */
/* 224 */	NdrFcShort( 0x10e ),	/* Offset= 270 (494) */
/* 226 */	NdrFcLong( 0x24 ),	/* 36 */
/* 230 */	NdrFcShort( 0x324 ),	/* Offset= 804 (1034) */
/* 232 */	NdrFcLong( 0x4024 ),	/* 16420 */
/* 236 */	NdrFcShort( 0x31e ),	/* Offset= 798 (1034) */
/* 238 */	NdrFcLong( 0x4011 ),	/* 16401 */
/* 242 */	NdrFcShort( 0x31c ),	/* Offset= 796 (1038) */
/* 244 */	NdrFcLong( 0x4002 ),	/* 16386 */
/* 248 */	NdrFcShort( 0x31a ),	/* Offset= 794 (1042) */
/* 250 */	NdrFcLong( 0x4003 ),	/* 16387 */
/* 254 */	NdrFcShort( 0x318 ),	/* Offset= 792 (1046) */
/* 256 */	NdrFcLong( 0x4014 ),	/* 16404 */
/* 260 */	NdrFcShort( 0x316 ),	/* Offset= 790 (1050) */
/* 262 */	NdrFcLong( 0x4004 ),	/* 16388 */
/* 266 */	NdrFcShort( 0x314 ),	/* Offset= 788 (1054) */
/* 268 */	NdrFcLong( 0x4005 ),	/* 16389 */
/* 272 */	NdrFcShort( 0x312 ),	/* Offset= 786 (1058) */
/* 274 */	NdrFcLong( 0x400b ),	/* 16395 */
/* 278 */	NdrFcShort( 0x2fc ),	/* Offset= 764 (1042) */
/* 280 */	NdrFcLong( 0x400a ),	/* 16394 */
/* 284 */	NdrFcShort( 0x2fa ),	/* Offset= 762 (1046) */
/* 286 */	NdrFcLong( 0x4006 ),	/* 16390 */
/* 290 */	NdrFcShort( 0x304 ),	/* Offset= 772 (1062) */
/* 292 */	NdrFcLong( 0x4007 ),	/* 16391 */
/* 296 */	NdrFcShort( 0x2fa ),	/* Offset= 762 (1058) */
/* 298 */	NdrFcLong( 0x4008 ),	/* 16392 */
/* 302 */	NdrFcShort( 0x2fc ),	/* Offset= 764 (1066) */
/* 304 */	NdrFcLong( 0x400d ),	/* 16397 */
/* 308 */	NdrFcShort( 0x2fa ),	/* Offset= 762 (1070) */
/* 310 */	NdrFcLong( 0x4009 ),	/* 16393 */
/* 314 */	NdrFcShort( 0x2f8 ),	/* Offset= 760 (1074) */
/* 316 */	NdrFcLong( 0x6000 ),	/* 24576 */
/* 320 */	NdrFcShort( 0x2f6 ),	/* Offset= 758 (1078) */
/* 322 */	NdrFcLong( 0x400c ),	/* 16396 */
/* 326 */	NdrFcShort( 0x2f4 ),	/* Offset= 756 (1082) */
/* 328 */	NdrFcLong( 0x10 ),	/* 16 */
/* 332 */	NdrFcShort( 0x8002 ),	/* Simple arm type: FC_CHAR */
/* 334 */	NdrFcLong( 0x12 ),	/* 18 */
/* 338 */	NdrFcShort( 0x8006 ),	/* Simple arm type: FC_SHORT */
/* 340 */	NdrFcLong( 0x13 ),	/* 19 */
/* 344 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 346 */	NdrFcLong( 0x15 ),	/* 21 */
/* 350 */	NdrFcShort( 0x800b ),	/* Simple arm type: FC_HYPER */
/* 352 */	NdrFcLong( 0x16 ),	/* 22 */
/* 356 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 358 */	NdrFcLong( 0x17 ),	/* 23 */
/* 362 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 364 */	NdrFcLong( 0xe ),	/* 14 */
/* 368 */	NdrFcShort( 0x2d2 ),	/* Offset= 722 (1090) */
/* 370 */	NdrFcLong( 0x400e ),	/* 16398 */
/* 374 */	NdrFcShort( 0x2d6 ),	/* Offset= 726 (1100) */
/* 376 */	NdrFcLong( 0x4010 ),	/* 16400 */
/* 380 */	NdrFcShort( 0x2d4 ),	/* Offset= 724 (1104) */
/* 382 */	NdrFcLong( 0x4012 ),	/* 16402 */
/* 386 */	NdrFcShort( 0x290 ),	/* Offset= 656 (1042) */
/* 388 */	NdrFcLong( 0x4013 ),	/* 16403 */
/* 392 */	NdrFcShort( 0x28e ),	/* Offset= 654 (1046) */
/* 394 */	NdrFcLong( 0x4015 ),	/* 16405 */
/* 398 */	NdrFcShort( 0x28c ),	/* Offset= 652 (1050) */
/* 400 */	NdrFcLong( 0x4016 ),	/* 16406 */
/* 404 */	NdrFcShort( 0x282 ),	/* Offset= 642 (1046) */
/* 406 */	NdrFcLong( 0x4017 ),	/* 16407 */
/* 410 */	NdrFcShort( 0x27c ),	/* Offset= 636 (1046) */
/* 412 */	NdrFcLong( 0x0 ),	/* 0 */
/* 416 */	NdrFcShort( 0x0 ),	/* Offset= 0 (416) */
/* 418 */	NdrFcLong( 0x1 ),	/* 1 */
/* 422 */	NdrFcShort( 0x0 ),	/* Offset= 0 (422) */
/* 424 */	NdrFcShort( 0xffff ),	/* Offset= -1 (423) */
/* 426 */
			0x15,		/* FC_STRUCT */
			0x7,		/* 7 */
/* 428 */	NdrFcShort( 0x8 ),	/* 8 */
/* 430 */	0xb,		/* FC_HYPER */
			0x5b,		/* FC_END */
/* 432 */
			0x13, 0x0,	/* FC_OP */
/* 434 */	NdrFcShort( 0xe ),	/* Offset= 14 (448) */
/* 436 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 438 */	NdrFcShort( 0x2 ),	/* 2 */
/* 440 */	0x9,		/* Corr desc: FC_ULONG */
			0x0,		/*  */
/* 442 */	NdrFcShort( 0xfffc ),	/* -4 */
/* 444 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 446 */	0x6,		/* FC_SHORT */
			0x5b,		/* FC_END */
/* 448 */
			0x17,		/* FC_CSTRUCT */
			0x3,		/* 3 */
/* 450 */	NdrFcShort( 0x8 ),	/* 8 */
/* 452 */	NdrFcShort( 0xfff0 ),	/* Offset= -16 (436) */
/* 454 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 456 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 458 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 460 */	NdrFcLong( 0x0 ),	/* 0 */
/* 464 */	NdrFcShort( 0x0 ),	/* 0 */
/* 466 */	NdrFcShort( 0x0 ),	/* 0 */
/* 468 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 470 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 472 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 474 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 476 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 478 */	NdrFcLong( 0x20400 ),	/* 132096 */
/* 482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 484 */	NdrFcShort( 0x0 ),	/* 0 */
/* 486 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 488 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 490 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 492 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 494 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 496 */	NdrFcShort( 0x2 ),	/* Offset= 2 (498) */
/* 498 */
			0x13, 0x0,	/* FC_OP */
/* 500 */	NdrFcShort( 0x204 ),	/* Offset= 516 (1016) */
/* 502 */
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x49,		/* 73 */
/* 504 */	NdrFcShort( 0x18 ),	/* 24 */
/* 506 */	NdrFcShort( 0xa ),	/* 10 */
/* 508 */	NdrFcLong( 0x8 ),	/* 8 */
/* 512 */	NdrFcShort( 0x5a ),	/* Offset= 90 (602) */
/* 514 */	NdrFcLong( 0xd ),	/* 13 */
/* 518 */	NdrFcShort( 0x7e ),	/* Offset= 126 (644) */
/* 520 */	NdrFcLong( 0x9 ),	/* 9 */
/* 524 */	NdrFcShort( 0x9e ),	/* Offset= 158 (682) */
/* 526 */	NdrFcLong( 0xc ),	/* 12 */
/* 530 */	NdrFcShort( 0xc8 ),	/* Offset= 200 (730) */
/* 532 */	NdrFcLong( 0x24 ),	/* 36 */
/* 536 */	NdrFcShort( 0x124 ),	/* Offset= 292 (828) */
/* 538 */	NdrFcLong( 0x800d ),	/* 32781 */
/* 542 */	NdrFcShort( 0x12e ),	/* Offset= 302 (844) */
/* 544 */	NdrFcLong( 0x10 ),	/* 16 */
/* 548 */	NdrFcShort( 0x148 ),	/* Offset= 328 (876) */
/* 550 */	NdrFcLong( 0x2 ),	/* 2 */
/* 554 */	NdrFcShort( 0x162 ),	/* Offset= 354 (908) */
/* 556 */	NdrFcLong( 0x3 ),	/* 3 */
/* 560 */	NdrFcShort( 0x17c ),	/* Offset= 380 (940) */
/* 562 */	NdrFcLong( 0x14 ),	/* 20 */
/* 566 */	NdrFcShort( 0x196 ),	/* Offset= 406 (972) */
/* 568 */	NdrFcShort( 0xffff ),	/* Offset= -1 (567) */
/* 570 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 572 */	NdrFcShort( 0x4 ),	/* 4 */
/* 574 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 576 */	NdrFcShort( 0x0 ),	/* 0 */
/* 578 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 580 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 582 */
			0x48,		/* FC_VARIABLE_REPEAT */
			0x49,		/* FC_FIXED_OFFSET */
/* 584 */	NdrFcShort( 0x4 ),	/* 4 */
/* 586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 588 */	NdrFcShort( 0x1 ),	/* 1 */
/* 590 */	NdrFcShort( 0x0 ),	/* 0 */
/* 592 */	NdrFcShort( 0x0 ),	/* 0 */
/* 594 */	0x13, 0x0,	/* FC_OP */
/* 596 */	NdrFcShort( 0xff6c ),	/* Offset= -148 (448) */
/* 598 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 600 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 602 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 604 */	NdrFcShort( 0x8 ),	/* 8 */
/* 606 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 608 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 610 */	NdrFcShort( 0x4 ),	/* 4 */
/* 612 */	NdrFcShort( 0x4 ),	/* 4 */
/* 614 */	0x11, 0x0,	/* FC_RP */
/* 616 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (570) */
/* 618 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 620 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 622 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 624 */	NdrFcShort( 0x0 ),	/* 0 */
/* 626 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 628 */	NdrFcShort( 0x0 ),	/* 0 */
/* 630 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 632 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 636 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 638 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 640 */	NdrFcShort( 0xff4a ),	/* Offset= -182 (458) */
/* 642 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 644 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 646 */	NdrFcShort( 0x8 ),	/* 8 */
/* 648 */	NdrFcShort( 0x0 ),	/* 0 */
/* 650 */	NdrFcShort( 0x6 ),	/* Offset= 6 (656) */
/* 652 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 654 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 656 */
			0x11, 0x0,	/* FC_RP */
/* 658 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (622) */
/* 660 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 662 */	NdrFcShort( 0x0 ),	/* 0 */
/* 664 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 666 */	NdrFcShort( 0x0 ),	/* 0 */
/* 668 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 670 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 674 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 676 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 678 */	NdrFcShort( 0xff36 ),	/* Offset= -202 (476) */
/* 680 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 682 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 684 */	NdrFcShort( 0x8 ),	/* 8 */
/* 686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 688 */	NdrFcShort( 0x6 ),	/* Offset= 6 (694) */
/* 690 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 692 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 694 */
			0x11, 0x0,	/* FC_RP */
/* 696 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (660) */
/* 698 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 700 */	NdrFcShort( 0x4 ),	/* 4 */
/* 702 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 704 */	NdrFcShort( 0x0 ),	/* 0 */
/* 706 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 708 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 710 */
			0x48,		/* FC_VARIABLE_REPEAT */
			0x49,		/* FC_FIXED_OFFSET */
/* 712 */	NdrFcShort( 0x4 ),	/* 4 */
/* 714 */	NdrFcShort( 0x0 ),	/* 0 */
/* 716 */	NdrFcShort( 0x1 ),	/* 1 */
/* 718 */	NdrFcShort( 0x0 ),	/* 0 */
/* 720 */	NdrFcShort( 0x0 ),	/* 0 */
/* 722 */	0x13, 0x0,	/* FC_OP */
/* 724 */	NdrFcShort( 0x180 ),	/* Offset= 384 (1108) */
/* 726 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 728 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 730 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 732 */	NdrFcShort( 0x8 ),	/* 8 */
/* 734 */	NdrFcShort( 0x0 ),	/* 0 */
/* 736 */	NdrFcShort( 0x6 ),	/* Offset= 6 (742) */
/* 738 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 740 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 742 */
			0x11, 0x0,	/* FC_RP */
/* 744 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (698) */
/* 746 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 748 */	NdrFcLong( 0x2f ),	/* 47 */
/* 752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 754 */	NdrFcShort( 0x0 ),	/* 0 */
/* 756 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 758 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 760 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 762 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 764 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 766 */	NdrFcShort( 0x1 ),	/* 1 */
/* 768 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 770 */	NdrFcShort( 0x4 ),	/* 4 */
/* 772 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 774 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 776 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 778 */	NdrFcShort( 0x10 ),	/* 16 */
/* 780 */	NdrFcShort( 0x0 ),	/* 0 */
/* 782 */	NdrFcShort( 0xa ),	/* Offset= 10 (792) */
/* 784 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 786 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 788 */	NdrFcShort( 0xffd6 ),	/* Offset= -42 (746) */
/* 790 */	0x36,		/* FC_POINTER */
			0x5b,		/* FC_END */
/* 792 */
			0x13, 0x0,	/* FC_OP */
/* 794 */	NdrFcShort( 0xffe2 ),	/* Offset= -30 (764) */
/* 796 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 798 */	NdrFcShort( 0x4 ),	/* 4 */
/* 800 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 802 */	NdrFcShort( 0x0 ),	/* 0 */
/* 804 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 806 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 808 */
			0x48,		/* FC_VARIABLE_REPEAT */
			0x49,		/* FC_FIXED_OFFSET */
/* 810 */	NdrFcShort( 0x4 ),	/* 4 */
/* 812 */	NdrFcShort( 0x0 ),	/* 0 */
/* 814 */	NdrFcShort( 0x1 ),	/* 1 */
/* 816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 818 */	NdrFcShort( 0x0 ),	/* 0 */
/* 820 */	0x13, 0x0,	/* FC_OP */
/* 822 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (776) */
/* 824 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 826 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 828 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 830 */	NdrFcShort( 0x8 ),	/* 8 */
/* 832 */	NdrFcShort( 0x0 ),	/* 0 */
/* 834 */	NdrFcShort( 0x6 ),	/* Offset= 6 (840) */
/* 836 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 838 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 840 */
			0x11, 0x0,	/* FC_RP */
/* 842 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (796) */
/* 844 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 846 */	NdrFcShort( 0x18 ),	/* 24 */
/* 848 */	NdrFcShort( 0x0 ),	/* 0 */
/* 850 */	NdrFcShort( 0xa ),	/* Offset= 10 (860) */
/* 852 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 854 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 856 */	NdrFcShort( 0xfce4 ),	/* Offset= -796 (60) */
/* 858 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 860 */
			0x11, 0x0,	/* FC_RP */
/* 862 */	NdrFcShort( 0xff10 ),	/* Offset= -240 (622) */
/* 864 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 866 */	NdrFcShort( 0x1 ),	/* 1 */
/* 868 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 870 */	NdrFcShort( 0x0 ),	/* 0 */
/* 872 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 874 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 876 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 878 */	NdrFcShort( 0x8 ),	/* 8 */
/* 880 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 882 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 884 */	NdrFcShort( 0x4 ),	/* 4 */
/* 886 */	NdrFcShort( 0x4 ),	/* 4 */
/* 888 */	0x13, 0x0,	/* FC_OP */
/* 890 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (864) */
/* 892 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 894 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 896 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 898 */	NdrFcShort( 0x2 ),	/* 2 */
/* 900 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 904 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 906 */	0x6,		/* FC_SHORT */
			0x5b,		/* FC_END */
/* 908 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 910 */	NdrFcShort( 0x8 ),	/* 8 */
/* 912 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 914 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 916 */	NdrFcShort( 0x4 ),	/* 4 */
/* 918 */	NdrFcShort( 0x4 ),	/* 4 */
/* 920 */	0x13, 0x0,	/* FC_OP */
/* 922 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (896) */
/* 924 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 926 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 928 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 930 */	NdrFcShort( 0x4 ),	/* 4 */
/* 932 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 934 */	NdrFcShort( 0x0 ),	/* 0 */
/* 936 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 938 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 940 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 942 */	NdrFcShort( 0x8 ),	/* 8 */
/* 944 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 946 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 948 */	NdrFcShort( 0x4 ),	/* 4 */
/* 950 */	NdrFcShort( 0x4 ),	/* 4 */
/* 952 */	0x13, 0x0,	/* FC_OP */
/* 954 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (928) */
/* 956 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 958 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 960 */
			0x1b,		/* FC_CARRAY */
			0x7,		/* 7 */
/* 962 */	NdrFcShort( 0x8 ),	/* 8 */
/* 964 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 966 */	NdrFcShort( 0x0 ),	/* 0 */
/* 968 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 970 */	0xb,		/* FC_HYPER */
			0x5b,		/* FC_END */
/* 972 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 974 */	NdrFcShort( 0x8 ),	/* 8 */
/* 976 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 978 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 980 */	NdrFcShort( 0x4 ),	/* 4 */
/* 982 */	NdrFcShort( 0x4 ),	/* 4 */
/* 984 */	0x13, 0x0,	/* FC_OP */
/* 986 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (960) */
/* 988 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 990 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 992 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 994 */	NdrFcShort( 0x8 ),	/* 8 */
/* 996 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 998 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1000 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1002 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1004 */	0x7,		/* Corr desc: FC_USHORT */
			0x0,		/*  */
/* 1006 */	NdrFcShort( 0xffd8 ),	/* -40 */
/* 1008 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1010 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1012 */	NdrFcShort( 0xffec ),	/* Offset= -20 (992) */
/* 1014 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1016 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 1018 */	NdrFcShort( 0x28 ),	/* 40 */
/* 1020 */	NdrFcShort( 0xffec ),	/* Offset= -20 (1000) */
/* 1022 */	NdrFcShort( 0x0 ),	/* Offset= 0 (1022) */
/* 1024 */	0x6,		/* FC_SHORT */
			0x6,		/* FC_SHORT */
/* 1026 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1028 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1030 */	NdrFcShort( 0xfdf0 ),	/* Offset= -528 (502) */
/* 1032 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1034 */
			0x13, 0x0,	/* FC_OP */
/* 1036 */	NdrFcShort( 0xfefc ),	/* Offset= -260 (776) */
/* 1038 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 1040 */	0x1,		/* FC_BYTE */
			0x5c,		/* FC_PAD */
/* 1042 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 1044 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 1046 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 1048 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 1050 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 1052 */	0xb,		/* FC_HYPER */
			0x5c,		/* FC_PAD */
/* 1054 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 1056 */	0xa,		/* FC_FLOAT */
			0x5c,		/* FC_PAD */
/* 1058 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 1060 */	0xc,		/* FC_DOUBLE */
			0x5c,		/* FC_PAD */
/* 1062 */
			0x13, 0x0,	/* FC_OP */
/* 1064 */	NdrFcShort( 0xfd82 ),	/* Offset= -638 (426) */
/* 1066 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 1068 */	NdrFcShort( 0xfd84 ),	/* Offset= -636 (432) */
/* 1070 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 1072 */	NdrFcShort( 0xfd9a ),	/* Offset= -614 (458) */
/* 1074 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 1076 */	NdrFcShort( 0xfda8 ),	/* Offset= -600 (476) */
/* 1078 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 1080 */	NdrFcShort( 0xfdb6 ),	/* Offset= -586 (494) */
/* 1082 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 1084 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1086) */
/* 1086 */
			0x13, 0x0,	/* FC_OP */
/* 1088 */	NdrFcShort( 0x14 ),	/* Offset= 20 (1108) */
/* 1090 */
			0x15,		/* FC_STRUCT */
			0x7,		/* 7 */
/* 1092 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1094 */	0x6,		/* FC_SHORT */
			0x1,		/* FC_BYTE */
/* 1096 */	0x1,		/* FC_BYTE */
			0x8,		/* FC_LONG */
/* 1098 */	0xb,		/* FC_HYPER */
			0x5b,		/* FC_END */
/* 1100 */
			0x13, 0x0,	/* FC_OP */
/* 1102 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (1090) */
/* 1104 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 1106 */	0x2,		/* FC_CHAR */
			0x5c,		/* FC_PAD */
/* 1108 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x7,		/* 7 */
/* 1110 */	NdrFcShort( 0x20 ),	/* 32 */
/* 1112 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1114 */	NdrFcShort( 0x0 ),	/* Offset= 0 (1114) */
/* 1116 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1118 */	0x6,		/* FC_SHORT */
			0x6,		/* FC_SHORT */
/* 1120 */	0x6,		/* FC_SHORT */
			0x6,		/* FC_SHORT */
/* 1122 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1124 */	NdrFcShort( 0xfc1c ),	/* Offset= -996 (128) */
/* 1126 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1128 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 1130 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1132 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1134 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1136 */	NdrFcShort( 0xfc0c ),	/* Offset= -1012 (124) */
/* 1138 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 1140 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1142) */
/* 1142 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 1144 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1146 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1148 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1150 */	NdrFcShort( 0xfd32 ),	/* Offset= -718 (432) */
/* 1152 */
			0x12, 0x0,	/* FC_UP */
/* 1154 */	NdrFcShort( 0xfd3e ),	/* Offset= -706 (448) */
/* 1156 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 1158 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1160 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1162 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1164 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (1152) */
/* 1166 */
			0x11, 0x0,	/* FC_RP */
/* 1168 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1170) */
/* 1170 */
			0x1c,		/* FC_CVARRAY */
			0x1,		/* 1 */
/* 1172 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1174 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1176 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1178 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1180 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 1182 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1184 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1186 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 1188 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1190 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1192) */
/* 1192 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1194 */	NdrFcLong( 0x32c2020c ),	/* 851575308 */
/* 1198 */	NdrFcShort( 0x3094 ),	/* 12436 */
/* 1200 */	NdrFcShort( 0x42bc ),	/* 17084 */
/* 1202 */	0x80,		/* 128 */
			0xff,		/* 255 */
/* 1204 */	0x45,		/* 69 */
			0xad,		/* 173 */
/* 1206 */	0x89,		/* 137 */
			0x82,		/* 130 */
/* 1208 */	0x6f,		/* 111 */
			0x62,		/* 98 */
/* 1210 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1212 */	NdrFcLong( 0xd77c0dbc ),	/* -679735876 */
/* 1216 */	NdrFcShort( 0xc7bc ),	/* -14404 */
/* 1218 */	NdrFcShort( 0x441d ),	/* 17437 */
/* 1220 */	0x95,		/* 149 */
			0x87,		/* 135 */
/* 1222 */	0x1e,		/* 30 */
			0x36,		/* 54 */
/* 1224 */	0x64,		/* 100 */
			0xe1,		/* 225 */
/* 1226 */	0xbc,		/* 188 */
			0xd3,		/* 211 */
/* 1228 */
			0x11, 0x0,	/* FC_RP */
/* 1230 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1232) */
/* 1232 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1234 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1236 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1238 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1240 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1242 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1244 */
			0x11, 0x0,	/* FC_RP */
/* 1246 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1248) */
/* 1248 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 1250 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1252 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1254 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1256 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1258 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 1260 */
			0x11, 0x0,	/* FC_RP */
/* 1262 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1264) */
/* 1264 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 1266 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1268 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1270 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1272 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1274 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 1276 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1278 */	NdrFcLong( 0x6c456541 ),	/* 1816487233 */
/* 1282 */	NdrFcShort( 0xc2b6 ),	/* -15690 */
/* 1284 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1286 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1288 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1290 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1292 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1294 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1296 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1298) */
/* 1298 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1300 */	NdrFcLong( 0x2c4636e3 ),	/* 742799075 */
/* 1304 */	NdrFcShort( 0x4f49 ),	/* 20297 */
/* 1306 */	NdrFcShort( 0x4966 ),	/* 18790 */
/* 1308 */	0x96,		/* 150 */
			0x6f,		/* 111 */
/* 1310 */	0x9,		/* 9 */
			0x53,		/* 83 */
/* 1312 */	0xf9,		/* 249 */
			0x7f,		/* 127 */
/* 1314 */	0x51,		/* 81 */
			0xc8,		/* 200 */
/* 1316 */
			0x11, 0x0,	/* FC_RP */
/* 1318 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1320) */
/* 1320 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1322 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1324 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1326 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1328 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1330 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1332 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 1334 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 1336 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1338 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1340) */
/* 1340 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1342 */	NdrFcLong( 0x6aa9042e ),	/* 1789461550 */
/* 1346 */	NdrFcShort( 0xa4d ),	/* 2637 */
/* 1348 */	NdrFcShort( 0x4f33 ),	/* 20275 */
/* 1350 */	0x88,		/* 136 */
			0x1b,		/* 27 */
/* 1352 */	0x3f,		/* 63 */
			0xbe,		/* 190 */
/* 1354 */	0x48,		/* 72 */
			0x86,		/* 134 */
/* 1356 */	0x1d,		/* 29 */
			0x14,		/* 20 */
/* 1358 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1360 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1362 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1364 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1366 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1368 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1370 */
			0x11, 0x0,	/* FC_RP */
/* 1372 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1374) */
/* 1374 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 1376 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1378 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1380 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 1382 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1384 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 1386 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1388 */	NdrFcLong( 0xf8851137 ),	/* -125497033 */
/* 1392 */	NdrFcShort( 0x6562 ),	/* 25954 */
/* 1394 */	NdrFcShort( 0x4120 ),	/* 16672 */
/* 1396 */	0xa3,		/* 163 */
			0x4e,		/* 78 */
/* 1398 */	0x1a,		/* 26 */
			0x51,		/* 81 */
/* 1400 */	0xee,		/* 238 */
			0x59,		/* 89 */
/* 1402 */	0x8e,		/* 142 */
			0xa7,		/* 167 */
/* 1404 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1406 */	NdrFcShort( 0xffec ),	/* Offset= -20 (1386) */
/* 1408 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1410 */	NdrFcLong( 0xa25318c8 ),	/* -1571612472 */
/* 1414 */	NdrFcShort( 0xeb1f ),	/* -5345 */
/* 1416 */	NdrFcShort( 0x4f38 ),	/* 20280 */
/* 1418 */	0x8e,		/* 142 */
			0x8d,		/* 141 */
/* 1420 */	0x80,		/* 128 */
			0xbf,		/* 191 */
/* 1422 */	0x28,		/* 40 */
			0x49,		/* 73 */
/* 1424 */	0x0,		/* 0 */
			0x1b,		/* 27 */
/* 1426 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1428 */	NdrFcLong( 0x5f74ab40 ),	/* 1601481536 */
/* 1432 */	NdrFcShort( 0xefe8 ),	/* -4120 */
/* 1434 */	NdrFcShort( 0x4a0d ),	/* 18957 */
/* 1436 */	0xb9,		/* 185 */
			0xae,		/* 174 */
/* 1438 */	0x30,		/* 48 */
			0xf4,		/* 244 */
/* 1440 */	0x93,		/* 147 */
			0xfe,		/* 254 */
/* 1442 */	0x6e,		/* 110 */
			0x21,		/* 33 */
/* 1444 */
			0x11, 0x0,	/* FC_RP */
/* 1446 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1448) */
/* 1448 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1450 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1452 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1454 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1456 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1458 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1460 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1462 */	NdrFcLong( 0xdc9a7c08 ),	/* -593855480 */
/* 1466 */	NdrFcShort( 0x138e ),	/* 5006 */
/* 1468 */	NdrFcShort( 0x41c0 ),	/* 16832 */
/* 1470 */	0x85,		/* 133 */
			0x32,		/* 50 */
/* 1472 */	0x5f,		/* 95 */
			0xd6,		/* 214 */
/* 1474 */	0x4b,		/* 75 */
			0x5e,		/* 94 */
/* 1476 */	0x72,		/* 114 */
			0xbf,		/* 191 */
/* 1478 */
			0x11, 0x0,	/* FC_RP */
/* 1480 */	NdrFcShort( 0xfa74 ),	/* Offset= -1420 (60) */
/* 1482 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1484 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1488 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1490 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1492 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 1494 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1496 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1498 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 1500 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1502 */	NdrFcLong( 0x32c2020c ),	/* 851575308 */
/* 1506 */	NdrFcShort( 0x3094 ),	/* 12436 */
/* 1508 */	NdrFcShort( 0x42bc ),	/* 17084 */
/* 1510 */	0x80,		/* 128 */
			0xff,		/* 255 */
/* 1512 */	0x45,		/* 69 */
			0xad,		/* 173 */
/* 1514 */	0x89,		/* 137 */
			0x82,		/* 130 */
/* 1516 */	0x6f,		/* 111 */
			0x62,		/* 98 */
/* 1518 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1520 */	NdrFcShort( 0xffda ),	/* Offset= -38 (1482) */
/* 1522 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1524 */	NdrFcLong( 0xc999413c ),	/* -912703172 */
/* 1528 */	NdrFcShort( 0x28c8 ),	/* 10440 */
/* 1530 */	NdrFcShort( 0x481c ),	/* 18460 */
/* 1532 */	0x95,		/* 149 */
			0x43,		/* 67 */
/* 1534 */	0xb0,		/* 176 */
			0x6c,		/* 108 */
/* 1536 */	0x92,		/* 146 */
			0xb8,		/* 184 */
/* 1538 */	0x12,		/* 18 */
			0xd1,		/* 209 */
/* 1540 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1542 */	NdrFcLong( 0x88c81964 ),	/* -2000152220 */
/* 1546 */	NdrFcShort( 0xdb97 ),	/* -9321 */
/* 1548 */	NdrFcShort( 0x4cdc ),	/* 19676 */
/* 1550 */	0xa9,		/* 169 */
			0x42,		/* 66 */
/* 1552 */	0x73,		/* 115 */
			0xc,		/* 12 */
/* 1554 */	0xf1,		/* 241 */
			0xdf,		/* 223 */
/* 1556 */	0x73,		/* 115 */
			0xa4,		/* 164 */
/* 1558 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1560 */	NdrFcShort( 0xffec ),	/* Offset= -20 (1540) */
/* 1562 */
			0x11, 0x0,	/* FC_RP */
/* 1564 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1566) */
/* 1566 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1568 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1570 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1572 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1574 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1576 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1578 */
			0x11, 0x0,	/* FC_RP */
/* 1580 */	NdrFcShort( 0x14 ),	/* Offset= 20 (1600) */
/* 1582 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1584 */	NdrFcLong( 0xee103481 ),	/* -300927871 */
/* 1588 */	NdrFcShort( 0x48bb ),	/* 18619 */
/* 1590 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1592 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1594 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1596 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1598 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1600 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 1602 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1604 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1606 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1608 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1610 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 1614 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1616 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1618 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (1582) */
/* 1620 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1622 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1624 */	NdrFcLong( 0xd77c0dbc ),	/* -679735876 */
/* 1628 */	NdrFcShort( 0xc7bc ),	/* -14404 */
/* 1630 */	NdrFcShort( 0x441d ),	/* 17437 */
/* 1632 */	0x95,		/* 149 */
			0x87,		/* 135 */
/* 1634 */	0x1e,		/* 30 */
			0x36,		/* 54 */
/* 1636 */	0x64,		/* 100 */
			0xe1,		/* 225 */
/* 1638 */	0xbc,		/* 188 */
			0xd3,		/* 211 */
/* 1640 */
			0x12, 0x0,	/* FC_UP */
/* 1642 */	NdrFcShort( 0xfdea ),	/* Offset= -534 (1108) */
/* 1644 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 1646 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1648 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1650 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1652 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (1640) */
/* 1654 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1656 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 1660 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 1662 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 1664 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 1666 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 1668 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 1670 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 1672 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1674 */	NdrFcLong( 0x7d9089c1 ),	/* 2106624449 */
/* 1678 */	NdrFcShort( 0x3bb9 ),	/* 15289 */
/* 1680 */	NdrFcShort( 0x11d4 ),	/* 4564 */
/* 1682 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1684 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1686 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1688 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1690 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1692 */	NdrFcShort( 0xffec ),	/* Offset= -20 (1672) */
/* 1694 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1696 */	NdrFcLong( 0xc ),	/* 12 */
/* 1700 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1702 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1704 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 1706 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1708 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1710 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 1712 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1714 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1716) */
/* 1716 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1718 */	NdrFcLong( 0x4f8b678d ),	/* 1334536077 */
/* 1722 */	NdrFcShort( 0xc5ba ),	/* -14918 */
/* 1724 */	NdrFcShort( 0x4a2f ),	/* 18991 */
/* 1726 */	0xb9,		/* 185 */
			0xb3,		/* 179 */
/* 1728 */	0x27,		/* 39 */
			0x80,		/* 128 */
/* 1730 */	0x95,		/* 149 */
			0x6e,		/* 110 */
/* 1732 */	0x36,		/* 54 */
			0x16,		/* 22 */
/* 1734 */
			0x11, 0x0,	/* FC_RP */
/* 1736 */	NdrFcShort( 0xe ),	/* Offset= 14 (1750) */
/* 1738 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 1740 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1742 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1744 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1746 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1748 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1750 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1752 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1754 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1756 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1758 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1760 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1762 */	NdrFcShort( 0xffe8 ),	/* Offset= -24 (1738) */
/* 1764 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1766 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1768 */	NdrFcLong( 0x4fa0b99a ),	/* 1335933338 */
/* 1772 */	NdrFcShort( 0x5a56 ),	/* 23126 */
/* 1774 */	NdrFcShort( 0x41a4 ),	/* 16804 */
/* 1776 */	0xbe,		/* 190 */
			0x8b,		/* 139 */
/* 1778 */	0xb8,		/* 184 */
			0x9b,		/* 155 */
/* 1780 */	0xc6,		/* 198 */
			0x22,		/* 34 */
/* 1782 */	0x51,		/* 81 */
			0xa5,		/* 165 */
/* 1784 */
			0x11, 0x0,	/* FC_RP */
/* 1786 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1788) */
/* 1788 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1790 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1792 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1794 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1796 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1798 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1800 */	NdrFcShort( 0xffc2 ),	/* Offset= -62 (1738) */
/* 1802 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1804 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 1806 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1808 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1810 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1812 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1814 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 1816 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (1804) */
/* 1818 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1820 */	NdrFcLong( 0x3a3ce0a1 ),	/* 977068193 */
/* 1824 */	NdrFcShort( 0xb5eb ),	/* -18965 */
/* 1826 */	NdrFcShort( 0x43bd ),	/* 17341 */
/* 1828 */	0x9c,		/* 156 */
			0x89,		/* 137 */
/* 1830 */	0x35,		/* 53 */
			0xea,		/* 234 */
/* 1832 */	0xa1,		/* 161 */
			0x10,		/* 16 */
/* 1834 */	0xf1,		/* 241 */
			0x2b,		/* 43 */
/* 1836 */
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/* 1838 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 1840 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 1842 */	0xe,		/* FC_ENUM32 */
			0x5c,		/* FC_PAD */
/* 1844 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1846 */	NdrFcLong( 0xff2e1dc2 ),	/* -13754942 */
/* 1850 */	NdrFcShort( 0x95a8 ),	/* -27224 */
/* 1852 */	NdrFcShort( 0x41c6 ),	/* 16838 */
/* 1854 */	0x85,		/* 133 */
			0xf4,		/* 244 */
/* 1856 */	0xff,		/* 255 */
			0xca,		/* 202 */
/* 1858 */	0x3a,		/* 58 */
			0x64,		/* 100 */
/* 1860 */	0x21,		/* 33 */
			0x6a,		/* 106 */
/* 1862 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1864 */	NdrFcLong( 0x86b6ae62 ),	/* -2034848158 */
/* 1868 */	NdrFcShort( 0x3dfa ),	/* 15866 */
/* 1870 */	NdrFcShort( 0x4020 ),	/* 16416 */
/* 1872 */	0xb5,		/* 181 */
			0xd1,		/* 209 */
/* 1874 */	0x7f,		/* 127 */
			0xa2,		/* 162 */
/* 1876 */	0x8e,		/* 142 */
			0x77,		/* 119 */
/* 1878 */	0x26,		/* 38 */
			0xe4,		/* 228 */
/* 1880 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1882 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1884) */
/* 1884 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1886 */	NdrFcLong( 0xc999413c ),	/* -912703172 */
/* 1890 */	NdrFcShort( 0x28c8 ),	/* 10440 */
/* 1892 */	NdrFcShort( 0x481c ),	/* 18460 */
/* 1894 */	0x95,		/* 149 */
			0x43,		/* 67 */
/* 1896 */	0xb0,		/* 176 */
			0x6c,		/* 108 */
/* 1898 */	0x92,		/* 146 */
			0xb8,		/* 184 */
/* 1900 */	0x12,		/* 18 */
			0xd1,		/* 209 */
/* 1902 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1904 */	NdrFcLong( 0xf696b01e ),	/* -157896674 */
/* 1908 */	NdrFcShort( 0x974b ),	/* -26805 */
/* 1910 */	NdrFcShort( 0x4065 ),	/* 16485 */
/* 1912 */	0xb4,		/* 180 */
			0x64,		/* 100 */
/* 1914 */	0xbd,		/* 189 */
			0xf4,		/* 244 */
/* 1916 */	0x59,		/* 89 */
			0x15,		/* 21 */
/* 1918 */	0x40,		/* 64 */
			0x54,		/* 84 */
/* 1920 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1922 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1924) */
/* 1924 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1926 */	NdrFcLong( 0xd77c0dbc ),	/* -679735876 */
/* 1930 */	NdrFcShort( 0xc7bc ),	/* -14404 */
/* 1932 */	NdrFcShort( 0x441d ),	/* 17437 */
/* 1934 */	0x95,		/* 149 */
			0x87,		/* 135 */
/* 1936 */	0x1e,		/* 30 */
			0x36,		/* 54 */
/* 1938 */	0x64,		/* 100 */
			0xe1,		/* 225 */
/* 1940 */	0xbc,		/* 188 */
			0xd3,		/* 211 */
/* 1942 */
			0x11, 0x0,	/* FC_RP */
/* 1944 */	NdrFcShort( 0xa ),	/* Offset= 10 (1954) */
/* 1946 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 1948 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1950 */	0x8,		/* FC_LONG */
			0xe,		/* FC_ENUM32 */
/* 1952 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1954 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1956 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1958 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1960 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1962 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1964 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1966 */	NdrFcShort( 0xffec ),	/* Offset= -20 (1946) */
/* 1968 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1970 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1972 */	NdrFcLong( 0xcf1e5d07 ),	/* -820093689 */
/* 1976 */	NdrFcShort( 0xb479 ),	/* -19335 */
/* 1978 */	NdrFcShort( 0x4195 ),	/* 16789 */
/* 1980 */	0xb6,		/* 182 */
			0x4c,		/* 76 */
/* 1982 */	0x2,		/* 2 */
			0x93,		/* 147 */
/* 1984 */	0x1f,		/* 31 */
			0x86,		/* 134 */
/* 1986 */	0x1,		/* 1 */
			0x4d,		/* 77 */
/* 1988 */	0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1990 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1992) */
/* 1992 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1994 */	NdrFcLong( 0xee103481 ),	/* -300927871 */
/* 1998 */	NdrFcShort( 0x48bb ),	/* 18619 */
/* 2000 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 2002 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 2004 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2006 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 2008 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 2010 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2012 */	NdrFcLong( 0x28bc5edc ),	/* 683433692 */
/* 2016 */	NdrFcShort( 0x3ef3 ),	/* 16115 */
/* 2018 */	NdrFcShort( 0x4db2 ),	/* 19890 */
/* 2020 */	0x8b,		/* 139 */
			0x90,		/* 144 */
/* 2022 */	0x55,		/* 85 */
			0x62,		/* 98 */
/* 2024 */	0x0,		/* 0 */
			0xfd,		/* 253 */
/* 2026 */	0x97,		/* 151 */
			0xed,		/* 237 */
/* 2028 */
			0x11, 0x0,	/* FC_RP */
/* 2030 */	NdrFcShort( 0xfc88 ),	/* Offset= -888 (1142) */
/* 2032 */
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/* 2034 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 2036 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2038 */	NdrFcLong( 0xb5a11cc3 ),	/* -1247732541 */
/* 2042 */	NdrFcShort( 0xb1d4 ),	/* -20012 */
/* 2044 */	NdrFcShort( 0x4ae4 ),	/* 19172 */
/* 2046 */	0xa1,		/* 161 */
			0xe4,		/* 228 */
/* 2048 */	0x2,		/* 2 */
			0xa6,		/* 166 */
/* 2050 */	0xa8,		/* 168 */
			0x19,		/* 25 */
/* 2052 */	0x8c,		/* 140 */
			0xeb,		/* 235 */
/* 2054 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2056 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2058) */
/* 2058 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2060 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 2064 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 2066 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 2068 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 2070 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 2072 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 2074 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 2076 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2078 */	NdrFcLong( 0xb5a11cc3 ),	/* -1247732541 */
/* 2082 */	NdrFcShort( 0xb1d4 ),	/* -20012 */
/* 2084 */	NdrFcShort( 0x4ae4 ),	/* 19172 */
/* 2086 */	0xa1,		/* 161 */
			0xe4,		/* 228 */
/* 2088 */	0x2,		/* 2 */
			0xa6,		/* 166 */
/* 2090 */	0xa8,		/* 168 */
			0x19,		/* 25 */
/* 2092 */	0x8c,		/* 140 */
			0xeb,		/* 235 */
/* 2094 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2096 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2098) */
/* 2098 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2100 */	NdrFcLong( 0x7bf80980 ),	/* 2079852928 */
/* 2104 */	NdrFcShort( 0xbf32 ),	/* -16590 */
/* 2106 */	NdrFcShort( 0x101a ),	/* 4122 */
/* 2108 */	0x8b,		/* 139 */
			0xbb,		/* 187 */
/* 2110 */	0x0,		/* 0 */
			0xaa,		/* 170 */
/* 2112 */	0x0,		/* 0 */
			0x30,		/* 48 */
/* 2114 */	0xc,		/* 12 */
			0xab,		/* 171 */
/* 2116 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2118 */	NdrFcLong( 0x4f8b678d ),	/* 1334536077 */
/* 2122 */	NdrFcShort( 0xc5ba ),	/* -14918 */
/* 2124 */	NdrFcShort( 0x4a2f ),	/* 18991 */
/* 2126 */	0xb9,		/* 185 */
			0xb3,		/* 179 */
/* 2128 */	0x27,		/* 39 */
			0x80,		/* 128 */
/* 2130 */	0x95,		/* 149 */
			0x6e,		/* 110 */
/* 2132 */	0x36,		/* 54 */
			0x16,		/* 22 */
/* 2134 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2136 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 2140 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 2142 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 2144 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 2146 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 2148 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 2150 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 2152 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2154 */	NdrFcShort( 0xffec ),	/* Offset= -20 (2134) */
/* 2156 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2158 */	NdrFcLong( 0xb5a11cc3 ),	/* -1247732541 */
/* 2162 */	NdrFcShort( 0xb1d4 ),	/* -20012 */
/* 2164 */	NdrFcShort( 0x4ae4 ),	/* 19172 */
/* 2166 */	0xa1,		/* 161 */
			0xe4,		/* 228 */
/* 2168 */	0x2,		/* 2 */
			0xa6,		/* 166 */
/* 2170 */	0xa8,		/* 168 */
			0x19,		/* 25 */
/* 2172 */	0x8c,		/* 140 */
			0xeb,		/* 235 */
/* 2174 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2176 */	NdrFcLong( 0x88c81964 ),	/* -2000152220 */
/* 2180 */	NdrFcShort( 0xdb97 ),	/* -9321 */
/* 2182 */	NdrFcShort( 0x4cdc ),	/* 19676 */
/* 2184 */	0xa9,		/* 169 */
			0x42,		/* 66 */
/* 2186 */	0x73,		/* 115 */
			0xc,		/* 12 */
/* 2188 */	0xf1,		/* 241 */
			0xdf,		/* 223 */
/* 2190 */	0x73,		/* 115 */
			0xa4,		/* 164 */
/* 2192 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2194 */	NdrFcLong( 0x24717cb1 ),	/* 611417265 */
/* 2198 */	NdrFcShort( 0xc4d ),	/* 3149 */
/* 2200 */	NdrFcShort( 0x485e ),	/* 18526 */
/* 2202 */	0xba,		/* 186 */
			0x7f,		/* 127 */
/* 2204 */	0x7b,		/* 123 */
			0x28,		/* 40 */
/* 2206 */	0xde,		/* 222 */
			0x86,		/* 134 */
/* 2208 */	0x1a,		/* 26 */
			0x3f,		/* 63 */
/* 2210 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2212 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2214) */
/* 2214 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2216 */	NdrFcLong( 0x3a3ce0a1 ),	/* 977068193 */
/* 2220 */	NdrFcShort( 0xb5eb ),	/* -18965 */
/* 2222 */	NdrFcShort( 0x43bd ),	/* 17341 */
/* 2224 */	0x9c,		/* 156 */
			0x89,		/* 137 */
/* 2226 */	0x35,		/* 53 */
			0xea,		/* 234 */
/* 2228 */	0xa1,		/* 161 */
			0x10,		/* 16 */
/* 2230 */	0xf1,		/* 241 */
			0x2b,		/* 43 */
/* 2232 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2234 */	NdrFcLong( 0x4f8b678d ),	/* 1334536077 */
/* 2238 */	NdrFcShort( 0xc5ba ),	/* -14918 */
/* 2240 */	NdrFcShort( 0x4a2f ),	/* 18991 */
/* 2242 */	0xb9,		/* 185 */
			0xb3,		/* 179 */
/* 2244 */	0x27,		/* 39 */
			0x80,		/* 128 */
/* 2246 */	0x95,		/* 149 */
			0x6e,		/* 110 */
/* 2248 */	0x36,		/* 54 */
			0x16,		/* 22 */
/* 2250 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2252 */	NdrFcLong( 0x7d9089c1 ),	/* 2106624449 */
/* 2256 */	NdrFcShort( 0x3bb9 ),	/* 15289 */
/* 2258 */	NdrFcShort( 0x11d4 ),	/* 4564 */
/* 2260 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 2262 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2264 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 2266 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 2268 */
			0x11, 0x0,	/* FC_RP */
/* 2270 */	NdrFcShort( 0xfb02 ),	/* Offset= -1278 (992) */
/* 2272 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2274 */	NdrFcLong( 0x4fa0b99a ),	/* 1335933338 */
/* 2278 */	NdrFcShort( 0x5a56 ),	/* 23126 */
/* 2280 */	NdrFcShort( 0x41a4 ),	/* 16804 */
/* 2282 */	0xbe,		/* 190 */
			0x8b,		/* 139 */
/* 2284 */	0xb8,		/* 184 */
			0x9b,		/* 155 */
/* 2286 */	0xc6,		/* 198 */
			0x22,		/* 34 */
/* 2288 */	0x51,		/* 81 */
			0xa5,		/* 165 */
/* 2290 */
			0x11, 0x0,	/* FC_RP */
/* 2292 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2294) */
/* 2294 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 2296 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2298 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2300 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2302 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2304 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 2308 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2310 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2312 */	NdrFcShort( 0xffd8 ),	/* Offset= -40 (2272) */
/* 2314 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2316 */
			0x11, 0x0,	/* FC_RP */
/* 2318 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2320) */
/* 2320 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 2322 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2324 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2326 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2328 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2330 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 2334 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2336 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2338 */	NdrFcShort( 0xfee8 ),	/* Offset= -280 (2058) */
/* 2340 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2342 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2344 */	NdrFcShort( 0xff68 ),	/* Offset= -152 (2192) */
/* 2346 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2348 */	NdrFcLong( 0xee103481 ),	/* -300927871 */
/* 2352 */	NdrFcShort( 0x48bb ),	/* 18619 */
/* 2354 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 2356 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 2358 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2360 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 2362 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 2364 */
			0x11, 0x0,	/* FC_RP */
/* 2366 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2368) */
/* 2368 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 2370 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2372 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2374 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2376 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2378 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2380 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2382 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2384) */
/* 2384 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2386 */	NdrFcLong( 0x88c81964 ),	/* -2000152220 */
/* 2390 */	NdrFcShort( 0xdb97 ),	/* -9321 */
/* 2392 */	NdrFcShort( 0x4cdc ),	/* 19676 */
/* 2394 */	0xa9,		/* 169 */
			0x42,		/* 66 */
/* 2396 */	0x73,		/* 115 */
			0xc,		/* 12 */
/* 2398 */	0xf1,		/* 241 */
			0xdf,		/* 223 */
/* 2400 */	0x73,		/* 115 */
			0xa4,		/* 164 */
/* 2402 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2404 */	NdrFcLong( 0xf6d10646 ),	/* -154073530 */
/* 2408 */	NdrFcShort( 0xc00c ),	/* -16372 */
/* 2410 */	NdrFcShort( 0x11d2 ),	/* 4562 */
/* 2412 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 2414 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2416 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 2418 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 2420 */
			0x11, 0x0,	/* FC_RP */
/* 2422 */	NdrFcShort( 0x34 ),	/* Offset= 52 (2474) */
/* 2424 */
			0x1d,		/* FC_SMFARRAY */
			0x1,		/* 1 */
/* 2426 */	NdrFcShort( 0x40 ),	/* 64 */
/* 2428 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 2430 */
			0x1d,		/* FC_SMFARRAY */
			0x1,		/* 1 */
/* 2432 */	NdrFcShort( 0x80 ),	/* 128 */
/* 2434 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 2436 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 2438 */	NdrFcShort( 0xf0 ),	/* 240 */
/* 2440 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2442 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2444 */	0x8,		/* FC_LONG */
			0x1,		/* FC_BYTE */
/* 2446 */	0x3f,		/* FC_STRUCTPAD3 */
			0x8,		/* FC_LONG */
/* 2448 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2450 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2452 */	0x8,		/* FC_LONG */
			0x4c,		/* FC_EMBEDDED_COMPLEX */
/* 2454 */	0x0,		/* 0 */
			NdrFcShort( 0xffe1 ),	/* Offset= -31 (2424) */
			0x4c,		/* FC_EMBEDDED_COMPLEX */
/* 2458 */	0x0,		/* 0 */
			NdrFcShort( 0xffe3 ),	/* Offset= -29 (2430) */
			0x5b,		/* FC_END */
/* 2462 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 2464 */	NdrFcShort( 0xf8 ),	/* 248 */
/* 2466 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2468 */	NdrFcShort( 0xffe0 ),	/* Offset= -32 (2436) */
/* 2470 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2472 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2474 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 2476 */	NdrFcShort( 0xf8 ),	/* 248 */
/* 2478 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2480 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2482 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2484 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2486 */	NdrFcShort( 0xffe8 ),	/* Offset= -24 (2462) */
/* 2488 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2490 */
			0x11, 0x0,	/* FC_RP */
/* 2492 */	NdrFcShort( 0xfdde ),	/* Offset= -546 (1946) */
/* 2494 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2496 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 2500 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 2502 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 2504 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 2506 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 2508 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 2510 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 2512 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2514 */	NdrFcLong( 0x7bf80980 ),	/* 2079852928 */
/* 2518 */	NdrFcShort( 0xbf32 ),	/* -16590 */
/* 2520 */	NdrFcShort( 0x101a ),	/* 4122 */
/* 2522 */	0x8b,		/* 139 */
			0xbb,		/* 187 */
/* 2524 */	0x0,		/* 0 */
			0xaa,		/* 170 */
/* 2526 */	0x0,		/* 0 */
			0x30,		/* 48 */
/* 2528 */	0xc,		/* 12 */
			0xab,		/* 171 */
/* 2530 */
			0x11, 0x0,	/* FC_RP */
/* 2532 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2534) */
/* 2534 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 2536 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2538 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2540 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2542 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2544 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 2548 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2550 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2552 */	NdrFcShort( 0xfcee ),	/* Offset= -786 (1766) */
/* 2554 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2556 */
			0x11, 0x0,	/* FC_RP */
/* 2558 */	NdrFcShort( 0x14 ),	/* Offset= 20 (2578) */
/* 2560 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2562 */	NdrFcLong( 0x3d4847fe ),	/* 1028147198 */
/* 2566 */	NdrFcShort( 0xea2d ),	/* -5587 */
/* 2568 */	NdrFcShort( 0x4255 ),	/* 16981 */
/* 2570 */	0xa4,		/* 164 */
			0x96,		/* 150 */
/* 2572 */	0x77,		/* 119 */
			0x0,		/* 0 */
/* 2574 */	0x59,		/* 89 */
			0xa1,		/* 161 */
/* 2576 */	0x34,		/* 52 */
			0xcc,		/* 204 */
/* 2578 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 2580 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2582 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2584 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2586 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2588 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 2592 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2594 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2596 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (2560) */
/* 2598 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2600 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2602 */	NdrFcShort( 0xff94 ),	/* Offset= -108 (2494) */
/* 2604 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2606 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (2560) */
/* 2608 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2610 */	NdrFcShort( 0xfcb4 ),	/* Offset= -844 (1766) */
/* 2612 */
			0x11, 0x0,	/* FC_RP */
/* 2614 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2616) */
/* 2616 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 2618 */	NdrFcShort( 0x18 ),	/* 24 */
/* 2620 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2622 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2624 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2626 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2628 */	NdrFcShort( 0xfc86 ),	/* Offset= -890 (1738) */
/* 2630 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2632 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 2634 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2636) */
/* 2636 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 2638 */	NdrFcShort( 0x14 ),	/* 20 */
/* 2640 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2642 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2644 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2646 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2648 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2650) */
/* 2650 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2652 */	NdrFcLong( 0x24717cb1 ),	/* 611417265 */
/* 2656 */	NdrFcShort( 0xc4d ),	/* 3149 */
/* 2658 */	NdrFcShort( 0x485e ),	/* 18526 */
/* 2660 */	0xba,		/* 186 */
			0x7f,		/* 127 */
/* 2662 */	0x7b,		/* 123 */
			0x28,		/* 40 */
/* 2664 */	0xde,		/* 222 */
			0x86,		/* 134 */
/* 2666 */	0x1a,		/* 26 */
			0x3f,		/* 63 */
/* 2668 */
			0x11, 0x0,	/* FC_RP */
/* 2670 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2672) */
/* 2672 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 2674 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2676 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2678 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2680 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2682 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 2684 */
			0x11, 0x0,	/* FC_RP */
/* 2686 */	NdrFcShort( 0xff06 ),	/* Offset= -250 (2436) */
/* 2688 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2690 */	NdrFcLong( 0xd77c0dbc ),	/* -679735876 */
/* 2694 */	NdrFcShort( 0xc7bc ),	/* -14404 */
/* 2696 */	NdrFcShort( 0x441d ),	/* 17437 */
/* 2698 */	0x95,		/* 149 */
			0x87,		/* 135 */
/* 2700 */	0x1e,		/* 30 */
			0x36,		/* 54 */
/* 2702 */	0x64,		/* 100 */
			0xe1,		/* 225 */
/* 2704 */	0xbc,		/* 188 */
			0xd3,		/* 211 */
/* 2706 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2708 */	NdrFcLong( 0x2c4636e3 ),	/* 742799075 */
/* 2712 */	NdrFcShort( 0x4f49 ),	/* 20297 */
/* 2714 */	NdrFcShort( 0x4966 ),	/* 18790 */
/* 2716 */	0x96,		/* 150 */
			0x6f,		/* 111 */
/* 2718 */	0x9,		/* 9 */
			0x53,		/* 83 */
/* 2720 */	0xf9,		/* 249 */
			0x7f,		/* 127 */
/* 2722 */	0x51,		/* 81 */
			0xc8,		/* 200 */
/* 2724 */
			0x11, 0x0,	/* FC_RP */
/* 2726 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2728) */
/* 2728 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 2730 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2732 */	0x40,		/* Corr desc:  constant, val=8 */
			0x0,		/* 0 */
/* 2734 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2736 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2738 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 2740 */
			0x11, 0x0,	/* FC_RP */
/* 2742 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2744) */
/* 2744 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 2746 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2748 */	0x40,		/* Corr desc:  constant, val=32 */
			0x0,		/* 0 */
/* 2750 */	NdrFcShort( 0x20 ),	/* 32 */
/* 2752 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2754 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 2756 */
			0x11, 0x0,	/* FC_RP */
/* 2758 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2760) */
/* 2760 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 2762 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2764 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2766 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 2768 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2770 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 2772 */
			0x11, 0x0,	/* FC_RP */
/* 2774 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2776) */
/* 2776 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 2778 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2780 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2782 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 2784 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2786 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 2788 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2790 */	NdrFcShort( 0xfc34 ),	/* Offset= -972 (1818) */
/* 2792 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2794 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2800 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2802 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 2804 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2806 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2808 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 2810 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2812 */	NdrFcLong( 0xaaaa731d ),	/* -1431669987 */
/* 2816 */	NdrFcShort( 0xe34e ),	/* -7346 */
/* 2818 */	NdrFcShort( 0x4742 ),	/* 18242 */
/* 2820 */	0x94,		/* 148 */
			0x8f,		/* 143 */
/* 2822 */	0xc8,		/* 200 */
			0x8b,		/* 139 */
/* 2824 */	0xbd,		/* 189 */
			0xa,		/* 10 */
/* 2826 */	0xe1,		/* 225 */
			0x36,		/* 54 */
/* 2828 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2830 */	NdrFcLong( 0xa25318c8 ),	/* -1571612472 */
/* 2834 */	NdrFcShort( 0xeb1f ),	/* -5345 */
/* 2836 */	NdrFcShort( 0x4f38 ),	/* 20280 */
/* 2838 */	0x8e,		/* 142 */
			0x8d,		/* 141 */
/* 2840 */	0x80,		/* 128 */
			0xbf,		/* 191 */
/* 2842 */	0x28,		/* 40 */
			0x49,		/* 73 */
/* 2844 */	0x0,		/* 0 */
			0x1b,		/* 27 */
/* 2846 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2848 */	NdrFcLong( 0x5f74ab40 ),	/* 1601481536 */
/* 2852 */	NdrFcShort( 0xefe8 ),	/* -4120 */
/* 2854 */	NdrFcShort( 0x4a0d ),	/* 18957 */
/* 2856 */	0xb9,		/* 185 */
			0xae,		/* 174 */
/* 2858 */	0x30,		/* 48 */
			0xf4,		/* 244 */
/* 2860 */	0x93,		/* 147 */
			0xfe,		/* 254 */
/* 2862 */	0x6e,		/* 110 */
			0x21,		/* 33 */
/* 2864 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2866 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 2870 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 2872 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 2874 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 2876 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 2878 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 2880 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 2882 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2884 */	NdrFcShort( 0xffec ),	/* Offset= -20 (2864) */
/* 2886 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2888 */	NdrFcLong( 0x7d9089c1 ),	/* 2106624449 */
/* 2892 */	NdrFcShort( 0x3bb9 ),	/* 15289 */
/* 2894 */	NdrFcShort( 0x11d4 ),	/* 4564 */
/* 2896 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 2898 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2900 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 2902 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 2904 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2906 */	NdrFcShort( 0xffec ),	/* Offset= -20 (2886) */
/* 2908 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2910 */	NdrFcLong( 0x24717cb1 ),	/* 611417265 */
/* 2914 */	NdrFcShort( 0xc4d ),	/* 3149 */
/* 2916 */	NdrFcShort( 0x485e ),	/* 18526 */
/* 2918 */	0xba,		/* 186 */
			0x7f,		/* 127 */
/* 2920 */	0x7b,		/* 123 */
			0x28,		/* 40 */
/* 2922 */	0xde,		/* 222 */
			0x86,		/* 134 */
/* 2924 */	0x1a,		/* 26 */
			0x3f,		/* 63 */
/* 2926 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2928 */	NdrFcLong( 0xff1b39de ),	/* -14992930 */
/* 2932 */	NdrFcShort( 0x20d3 ),	/* 8403 */
/* 2934 */	NdrFcShort( 0x4cdd ),	/* 19677 */
/* 2936 */	0xa1,		/* 161 */
			0x34,		/* 52 */
/* 2938 */	0xdc,		/* 220 */
			0xbe,		/* 190 */
/* 2940 */	0x3b,		/* 59 */
			0xe2,		/* 226 */
/* 2942 */	0x3f,		/* 63 */
			0x3e,		/* 62 */
/* 2944 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2946 */	NdrFcLong( 0x4f8b678d ),	/* 1334536077 */
/* 2950 */	NdrFcShort( 0xc5ba ),	/* -14918 */
/* 2952 */	NdrFcShort( 0x4a2f ),	/* 18991 */
/* 2954 */	0xb9,		/* 185 */
			0xb3,		/* 179 */
/* 2956 */	0x27,		/* 39 */
			0x80,		/* 128 */
/* 2958 */	0x95,		/* 149 */
			0x6e,		/* 110 */
/* 2960 */	0x36,		/* 54 */
			0x16,		/* 22 */
/* 2962 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2964 */	NdrFcLong( 0x92ac8be4 ),	/* -1834185756 */
/* 2968 */	NdrFcShort( 0xedc8 ),	/* -4664 */
/* 2970 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 2972 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 2974 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2976 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 2978 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 2980 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2982 */	NdrFcShort( 0xffda ),	/* Offset= -38 (2944) */
/* 2984 */
			0x12, 0x0,	/* FC_UP */
/* 2986 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2988) */
/* 2988 */
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x48,		/* 72 */
/* 2990 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2992 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2994 */	NdrFcLong( 0x48746457 ),	/* 1215587415 */
/* 2998 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 3000 */	NdrFcLong( 0x52746457 ),	/* 1383359575 */
/* 3004 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 3006 */	NdrFcShort( 0xffff ),	/* Offset= -1 (3005) */
/* 3008 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 3010 */	NdrFcShort( 0x2 ),	/* 2 */
/* 3012 */	NdrFcShort( 0x4 ),	/* 4 */
/* 3014 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3016 */	NdrFcShort( 0xffe0 ),	/* Offset= -32 (2984) */
/* 3018 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 3020 */	NdrFcLong( 0xc999413c ),	/* -912703172 */
/* 3024 */	NdrFcShort( 0x28c8 ),	/* 10440 */
/* 3026 */	NdrFcShort( 0x481c ),	/* 18460 */
/* 3028 */	0x95,		/* 149 */
			0x43,		/* 67 */
/* 3030 */	0xb0,		/* 176 */
			0x6c,		/* 108 */
/* 3032 */	0x92,		/* 146 */
			0xb8,		/* 184 */
/* 3034 */	0x12,		/* 18 */
			0xd1,		/* 209 */
/* 3036 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 3038 */	NdrFcLong( 0x3a3ce0a1 ),	/* 977068193 */
/* 3042 */	NdrFcShort( 0xb5eb ),	/* -18965 */
/* 3044 */	NdrFcShort( 0x43bd ),	/* 17341 */
/* 3046 */	0x9c,		/* 156 */
			0x89,		/* 137 */
/* 3048 */	0x35,		/* 53 */
			0xea,		/* 234 */
/* 3050 */	0xa1,		/* 161 */
			0x10,		/* 16 */
/* 3052 */	0xf1,		/* 241 */
			0x2b,		/* 43 */
/* 3054 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 3056 */	NdrFcLong( 0x2c4636e3 ),	/* 742799075 */
/* 3060 */	NdrFcShort( 0x4f49 ),	/* 20297 */
/* 3062 */	NdrFcShort( 0x4966 ),	/* 18790 */
/* 3064 */	0x96,		/* 150 */
			0x6f,		/* 111 */
/* 3066 */	0x9,		/* 9 */
			0x53,		/* 83 */
/* 3068 */	0xf9,		/* 249 */
			0x7f,		/* 127 */
/* 3070 */	0x51,		/* 81 */
			0xc8,		/* 200 */
/* 3072 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 3074 */	NdrFcLong( 0x13f3a421 ),	/* 334734369 */
/* 3078 */	NdrFcShort( 0x4915 ),	/* 18709 */
/* 3080 */	NdrFcShort( 0x455b ),	/* 17755 */
/* 3082 */	0xb5,		/* 181 */
			0x7f,		/* 127 */
/* 3084 */	0xaf,		/* 175 */
			0xd4,		/* 212 */
/* 3086 */	0x7,		/* 7 */
			0x3c,		/* 60 */
/* 3088 */	0xff,		/* 255 */
			0xa0,		/* 160 */
/* 3090 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 3092 */	NdrFcLong( 0x963e6a91 ),	/* -1774294383 */
/* 3096 */	NdrFcShort( 0x513f ),	/* 20799 */
/* 3098 */	NdrFcShort( 0x4490 ),	/* 17552 */
/* 3100 */	0xa2,		/* 162 */
			0x82,		/* 130 */
/* 3102 */	0xe,		/* 14 */
			0x99,		/* 153 */
/* 3104 */	0xb5,		/* 181 */
			0x42,		/* 66 */
/* 3106 */	0xb4,		/* 180 */
			0xcc,		/* 204 */
/* 3108 */
			0x11, 0x0,	/* FC_RP */
/* 3110 */	NdrFcShort( 0x2 ),	/* Offset= 2 (3112) */
/* 3112 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 3114 */	NdrFcShort( 0x10 ),	/* 16 */
/* 3116 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 3118 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3120 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 3122 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 3124 */	NdrFcShort( 0xf408 ),	/* Offset= -3064 (60) */
/* 3126 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 3128 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 3130 */	NdrFcLong( 0x146aa200 ),	/* 342532608 */
/* 3134 */	NdrFcShort( 0x7061 ),	/* 28769 */
/* 3136 */	NdrFcShort( 0x4f79 ),	/* 20345 */
/* 3138 */	0xa8,		/* 168 */
			0xd8,		/* 216 */
/* 3140 */	0x7c,		/* 124 */
			0xbb,		/* 187 */
/* 3142 */	0xa1,		/* 161 */
			0xb5,		/* 181 */
/* 3144 */	0xca,		/* 202 */
			0xda,		/* 218 */

			0x0
		}
	};

static const USER_MARSHAL_ROUTINE_QUADRUPLE UserMarshalRoutines[ WIRE_MARSHAL_TABLE_SIZE ] =
		{

			{
			VARIANT_UserSize
			,VARIANT_UserMarshal
			,VARIANT_UserUnmarshal
			,VARIANT_UserFree
			},
			{
			BSTR_UserSize
			,BSTR_UserMarshal
			,BSTR_UserUnmarshal
			,BSTR_UserFree
			},
			{
			HDC_UserSize
			,HDC_UserMarshal
			,HDC_UserUnmarshal
			,HDC_UserFree
			}

		};



/* Standard interface: __MIDL_itf_ViewsPs_0000, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IUnknown, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}} */


/* Object interface: IVwNotifyChange, ver. 0.0,
   GUID={0x6C456541,0xC2B6,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short IVwNotifyChange_FormatStringOffsetTable[] =
	{
	0
	};

static const MIDL_STUBLESS_PROXY_INFO IVwNotifyChange_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwNotifyChange_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwNotifyChange_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwNotifyChange_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IVwNotifyChangeProxyVtbl =
{
	&IVwNotifyChange_ProxyInfo,
	&IID_IVwNotifyChange,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwNotifyChange::PropChanged */
};

const CInterfaceStubVtbl _IVwNotifyChangeStubVtbl =
{
	&IID_IVwNotifyChange,
	&IVwNotifyChange_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0327, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDbColSpec, ver. 0.0,
   GUID={0xA25318C8,0xEB1F,0x4f38,{0x8E,0x8D,0x80,0xBF,0x28,0x49,0x00,0x1B}} */

#pragma code_seg(".orpc")
static const unsigned short IDbColSpec_FormatStringOffsetTable[] =
	{
	60,
	90,
	144,
	180,
	240,
	282,
	324,
	366
	};

static const MIDL_STUBLESS_PROXY_INFO IDbColSpec_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IDbColSpec_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IDbColSpec_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IDbColSpec_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(11) _IDbColSpecProxyVtbl =
{
	&IDbColSpec_ProxyInfo,
	&IID_IDbColSpec,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IDbColSpec::Clear */ ,
	(void *) (INT_PTR) -1 /* IDbColSpec::Push */ ,
	(void *) (INT_PTR) -1 /* IDbColSpec::Size */ ,
	(void *) (INT_PTR) -1 /* IDbColSpec::GetColInfo */ ,
	(void *) (INT_PTR) -1 /* IDbColSpec::GetDbColType */ ,
	(void *) (INT_PTR) -1 /* IDbColSpec::GetBaseCol */ ,
	(void *) (INT_PTR) -1 /* IDbColSpec::GetTag */ ,
	(void *) (INT_PTR) -1 /* IDbColSpec::GetWs */
};

const CInterfaceStubVtbl _IDbColSpecStubVtbl =
{
	&IID_IDbColSpec,
	&IDbColSpec_ServerInfo,
	11,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0328, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ISilDataAccess, ver. 0.0,
   GUID={0x88C81964,0xDB97,0x4cdc,{0xA9,0x42,0x73,0x0C,0xF1,0xDF,0x73,0xA4}} */

#pragma code_seg(".orpc")
static const unsigned short ISilDataAccess_FormatStringOffsetTable[] =
	{
	408,
	456,
	510,
	558,
	606,
	666,
	726,
	774,
	822,
	870,
	924,
	972,
	1020,
	1068,
	1116,
	1164,
	1212,
	(unsigned short) -1,
	1272,
	1314,
	1344,
	1374,
	1404,
	1446,
	1482,
	1518,
	1554,
	1608,
	1668,
	1728,
	1800,
	1866,
	1914,
	1950,
	2004,
	2052,
	2100,
	2148,
	2202,
	2250,
	2298,
	2352,
	2400,
	2436,
	2508,
	2544,
	2580,
	2616,
	2664,
	2724,
	2778,
	2832,
	2892,
	2928,
	2958,
	2994
	};

static const MIDL_STUBLESS_PROXY_INFO ISilDataAccess_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ISilDataAccess_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ISilDataAccess_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ISilDataAccess_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(59) _ISilDataAccessProxyVtbl =
{
	&ISilDataAccess_ProxyInfo,
	&IID_ISilDataAccess,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_ObjectProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_VecItem */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_VecSize */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_VecSizeAssumeCached */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::VecProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::BinaryPropRgb */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_GuidProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_IntProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_Int64Prop */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_MultiStringAlt */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_MultiStringProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_Prop */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_StringProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_TimeProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_UnicodeProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::put_UnicodeProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::UnicodePropRgch */ ,
	0 /* (void *) (INT_PTR) -1 /* ISilDataAccess::get_UnknownProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::BeginUndoTask */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::EndUndoTask */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::ContinueUndoTask */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::EndOuterUndoTask */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::BreakUndoTask */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::GetActionHandler */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetActionHandler */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::DeleteObj */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::DeleteObjOwner */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::InsertNew */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::MakeNewObject */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::MoveOwnSeq */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::Replace */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetObjProp */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::RemoveObjRefs */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetBinary */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetGuid */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetInt */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetInt64 */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetMultiStringAlt */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetString */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetTime */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetUnicode */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::SetUnknown */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::AddNotification */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::PropChanged */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::RemoveNotification */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::putref_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_WritingSystemsOfInterest */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::InsertRelExtra */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::UpdateRelExtra */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::GetRelExtra */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_IsPropInCache */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::IsDirty */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::ClearDirty */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::get_MetaDataCache */ ,
	(void *) (INT_PTR) -1 /* ISilDataAccess::putref_MetaDataCache */
};

const CInterfaceStubVtbl _ISilDataAccessStubVtbl =
{
	&IID_ISilDataAccess,
	&ISilDataAccess_ServerInfo,
	59,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0329, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwCacheDa, ver. 0.0,
   GUID={0x146AA200,0x7061,0x4f79,{0xA8,0xD8,0x7C,0xBB,0xA1,0xB5,0xCA,0xDA}} */

#pragma code_seg(".orpc")
static const unsigned short IVwCacheDa_FormatStringOffsetTable[] =
	{
	3030,
	3078,
	3132,
	3198,
	3252,
	3300,
	3348,
	3396,
	3450,
	3516,
	3564,
	3612,
	3666,
	3714,
	3774,
	3828,
	3882,
	3924,
	3978,
	4008,
	4044,
	4086,
	4134
	};

static const MIDL_STUBLESS_PROXY_INFO IVwCacheDa_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwCacheDa_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwCacheDa_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwCacheDa_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(26) _IVwCacheDaProxyVtbl =
{
	&IVwCacheDa_ProxyInfo,
	&IID_IVwCacheDa,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheObjProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheVecProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheReplace */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheBinaryProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheGuidProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheInt64Prop */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheIntProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheStringAlt */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheStringFields */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheStringProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheTimeProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheUnicodeProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::CacheUnknown */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::NewObject */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::GetObjIndex */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::GetOutlineNumber */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::ClearInfoAbout */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::get_CachedIntProp */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::ClearAllData */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::InstallVirtual */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::GetVirtualHandlerId */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::GetVirtualHandlerName */ ,
	(void *) (INT_PTR) -1 /* IVwCacheDa::ClearVirtualProperties */
};

const CInterfaceStubVtbl _IVwCacheDaStubVtbl =
{
	&IID_IVwCacheDa,
	&IVwCacheDa_ServerInfo,
	26,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0330, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwOleDbDa, ver. 0.0,
   GUID={0xAAAA731D,0xE34E,0x4742,{0x94,0x8F,0xC8,0x8B,0xBD,0x0A,0xE1,0x36}} */

#pragma code_seg(".orpc")
static const unsigned short IVwOleDbDa_FormatStringOffsetTable[] =
	{
	4164,
	4200,
	4266,
	4296,
	4326,
	4362,
	4398,
	4434,
	4470,
	4500,
	4542,
	4584,
	4626,
	4692,
	4746
	};

static const MIDL_STUBLESS_PROXY_INFO IVwOleDbDa_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwOleDbDa_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwOleDbDa_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwOleDbDa_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(18) _IVwOleDbDaProxyVtbl =
{
	&IVwOleDbDa_ProxyInfo,
	&IID_IVwOleDbDa,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::CreateDummyID */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::Load */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::Save */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::Clear */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::CheckTimeStamp */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::SetTimeStamp */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::CacheCurrTimeStamp */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::CacheCurrTimeStampAndOwner */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::Close */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::get_ObjOwner */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::get_ObjClid */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::get_ObjOwnFlid */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::LoadData */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::UpdatePropIfCached */ ,
	(void *) (INT_PTR) -1 /* IVwOleDbDa::GetIdFromGuid */
};

const CInterfaceStubVtbl _IVwOleDbDaStubVtbl =
{
	&IID_IVwOleDbDa,
	&IVwOleDbDa_ServerInfo,
	18,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0331, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ISetupVwOleDbDa, ver. 0.0,
   GUID={0x8645fA4F,0xEE90,0x11D2,{0xA9,0xB8,0x00,0x80,0xC8,0x7B,0x60,0x86}} */

#pragma code_seg(".orpc")
static const unsigned short ISetupVwOleDbDa_FormatStringOffsetTable[] =
	{
	4788,
	4842
	};

static const MIDL_STUBLESS_PROXY_INFO ISetupVwOleDbDa_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ISetupVwOleDbDa_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ISetupVwOleDbDa_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ISetupVwOleDbDa_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _ISetupVwOleDbDaProxyVtbl =
{
	&ISetupVwOleDbDa_ProxyInfo,
	&IID_ISetupVwOleDbDa,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ISetupVwOleDbDa::Init */ ,
	(void *) (INT_PTR) -1 /* ISetupVwOleDbDa::GetOleDbEncap */
};

const CInterfaceStubVtbl _ISetupVwOleDbDaStubVtbl =
{
	&IID_ISetupVwOleDbDa,
	&ISetupVwOleDbDa_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0332, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwRootBox, ver. 0.0,
   GUID={0x24717CB1,0x0C4D,0x485e,{0xBA,0x7F,0x7B,0x28,0xDE,0x86,0x1A,0x3F}} */

#pragma code_seg(".orpc")
static const unsigned short IVwRootBox_FormatStringOffsetTable[] =
	{
	0,
	4878,
	4914,
	4950,
	4986,
	5046,
	5100,
	5154,
	5208,
	5244,
	5280,
	5316,
	5352,
	5388,
	5424,
	5460,
	5490,
	5598,
	5652,
	5712,
	5808,
	5874,
	5940,
	6018,
	6078,
	6144,
	6234,
	6300,
	6336,
	6372,
	6420,
	6450,
	6504,
	6558,
	6612,
	6666,
	6720,
	6756,
	6810,
	6864,
	6906,
	6942,
	6978,
	7014,
	7062,
	7104,
	7146,
	7182,
	7218,
	7248,
	7284,
	7320,
	7350,
	2928,
	7380,
	7416,
	7458,
	7494,
	7530,
	7566,
	7620,
	7686
	};

static const MIDL_STUBLESS_PROXY_INFO IVwRootBox_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwRootBox_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwRootBox_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwRootBox_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(65) _IVwRootBoxProxyVtbl =
{
	&IVwRootBox_ProxyInfo,
	&IID_IVwRootBox,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwNotifyChange::PropChanged */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::SetSite */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::putref_DataAccess */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_DataAccess */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::SetRootObjects */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::SetRootObject */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::SetRootVariant */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::SetRootString */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::putref_Overlay */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_Overlay */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::GetRootVariant */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::Serialize */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::Deserialize */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::WriteWpx */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_Selection */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::DestroySelection */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MakeTextSelection */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MakeRangeSelection */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MakeSimpleSel */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MakeTextSelInObj */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MakeSelInObj */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MakeSelAt */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MakeSelInBox */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_IsClickInText */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_IsClickInObject */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_IsClickInOverlayTag */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::OnTyping */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::OnChar */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::OnSysChar */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::OnExtendedKey */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::FlashInsertionPoint */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MouseDown */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MouseDblClk */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MouseMoveDrag */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MouseDownExtended */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::MouseUp */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::Activate */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::PrepareToDraw */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::DrawRoot */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::Layout */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_Height */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_Width */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::InitializePrinting */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::GetTotalPrintPages */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::PrintSinglePage */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::Print */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_Site */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::LoseFocus */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::Close */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::AddSelChngListener */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::DelSelChngListener */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::Reconstruct */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::OnStylesheetChange */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::DrawingErrors */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_Stylesheet */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::SetTableColWidths */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::IsDirty */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::get_XdPos */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::RequestObjCharDeleteNotification */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::GetRootObject */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::DrawRoot2 */ ,
	(void *) (INT_PTR) -1 /* IVwRootBox::SetKeyboardForWs */
};

const CInterfaceStubVtbl _IVwRootBoxStubVtbl =
{
	&IID_IVwRootBox,
	&IVwRootBox_ServerInfo,
	65,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0333, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwViewConstructor, ver. 0.0,
   GUID={0xEE103481,0x48BB,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short IVwViewConstructor_FormatStringOffsetTable[] =
	{
	7746,
	7794,
	7848,
	7908,
	7974,
	8040,
	8094,
	8166,
	8208,
	8268,
	8316
	};

static const MIDL_STUBLESS_PROXY_INFO IVwViewConstructor_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwViewConstructor_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwViewConstructor_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwViewConstructor_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(14) _IVwViewConstructorProxyVtbl =
{
	&IVwViewConstructor_ProxyInfo,
	&IID_IVwViewConstructor,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::Display */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::DisplayVec */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::DisplayVariant */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::DisplayPicture */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::UpdateProp */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::EstimateHeight */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::LoadDataFor */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::GetStrForGuid */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::DoHotLinkAction */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::GetIdFromGuid */ ,
	(void *) (INT_PTR) -1 /* IVwViewConstructor::DisplayEmbeddedObject */
};

const CInterfaceStubVtbl _IVwViewConstructorStubVtbl =
{
	&IID_IVwViewConstructor,
	&IVwViewConstructor_ServerInfo,
	14,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0334, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwRootSite, ver. 0.0,
   GUID={0xC999413C,0x28C8,0x481c,{0x95,0x43,0xB0,0x6C,0x92,0xB8,0x12,0xD1}} */

#pragma code_seg(".orpc")
static const unsigned short IVwRootSite_FormatStringOffsetTable[] =
	{
	8358,
	8418,
	8472,
	8514,
	8556,
	8610,
	8664,
	8706,
	8748,
	8784,
	8820,
	8886,
	8928,
	8970,
	9012,
	9054,
	9096,
	9138,
	9192,
	9240,
	9312,
	9354,
	9408,
	9450,
	9486
	};

static const MIDL_STUBLESS_PROXY_INFO IVwRootSite_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwRootSite_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwRootSite_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwRootSite_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(28) _IVwRootSiteProxyVtbl =
{
	&IVwRootSite_ProxyInfo,
	&IID_IVwRootSite,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwRootSite::InvalidateRect */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::GetGraphics */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::get_LayoutGraphics */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::get_ScreenGraphics */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::GetTransformAtDst */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::GetTransformAtSrc */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::ReleaseGraphics */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::GetAvailWidth */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::DoUpdates */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::SizeChanged */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::AdjustScrollRange */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::SelectionChanged */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::OverlayChanged */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::get_SemiTagging */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::ScreenToClient */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::ClientToScreen */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::GetAndClearPendingWs */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::IsOkToMakeLazy */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::OnProblemDeletion */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::OnInsertDiffParas */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::get_TextRepOfObj */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::get_MakeObjFromText */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::ScrollSelectionIntoView */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::get_RootBox */ ,
	(void *) (INT_PTR) -1 /* IVwRootSite::get_Hwnd */
};

const CInterfaceStubVtbl _IVwRootSiteStubVtbl =
{
	&IID_IVwRootSite,
	&IVwRootSite_ServerInfo,
	28,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0335, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwObjDelNotification, ver. 0.0,
   GUID={0x913B1BED,0x6199,0x4b6e,{0xA6,0x3F,0x57,0xB2,0x25,0xB4,0x49,0x97}} */

#pragma code_seg(".orpc")
static const unsigned short IVwObjDelNotification_FormatStringOffsetTable[] =
	{
	9522
	};

static const MIDL_STUBLESS_PROXY_INFO IVwObjDelNotification_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwObjDelNotification_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwObjDelNotification_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwObjDelNotification_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IVwObjDelNotificationProxyVtbl =
{
	&IVwObjDelNotification_ProxyInfo,
	&IID_IVwObjDelNotification,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwObjDelNotification::AboutToDelete */
};

const CInterfaceStubVtbl _IVwObjDelNotificationStubVtbl =
{
	&IID_IVwObjDelNotification,
	&IVwObjDelNotification_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0336, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwEnv, ver. 0.0,
   GUID={0xB5A11CC3,0xB1D4,0x4ae4,{0xA1,0xE4,0x02,0xA6,0xA8,0x19,0x8C,0xEB}} */

#pragma code_seg(".orpc")
static const unsigned short IVwEnv_FormatStringOffsetTable[] =
	{
	9588,
	9636,
	9684,
	9732,
	9780,
	9828,
	9882,
	9930,
	9984,
	10032,
	10074,
	10122,
	10158,
	10218,
	10266,
	10302,
	10350,
	10386,
	10428,
	10464,
	10500,
	10536,
	10572,
	10626,
	10662,
	10716,
	1554,
	10746,
	10776,
	10806,
	6420,
	10836,
	10866,
	10896,
	10950,
	10992,
	11022,
	11052,
	11082,
	11112,
	11142,
	11220,
	11250,
	11280,
	11310,
	11352,
	11382,
	11424,
	11454,
	11496,
	11538,
	7320,
	7350,
	2928,
	11568,
	11598,
	11628,
	11676,
	11718,
	11754,
	11808
	};

static const MIDL_STUBLESS_PROXY_INFO IVwEnv_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwEnv_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwEnv_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwEnv_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(64) _IVwEnvProxyVtbl =
{
	&IVwEnv_ProxyInfo,
	&IID_IVwEnv,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddObjProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddObjVec */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddObjVecItems */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddObj */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddLazyVecItems */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddLazyItems */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddDerivedProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::NoteDependency */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddStringProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddUnicodeProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddIntProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddIntPropPic */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddStringAltMember */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddStringAlt */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddStringAltSeq */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddString */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddTimeProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddGenDateProp */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CurrentObject */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::get_OpenObject */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::get_EmbeddingLevel */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::GetOuterObject */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::get_DataAccess */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddWindow */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddSeparatorBar */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddSimpleRect */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenDiv */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseDiv */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenParagraph */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTaggedPara */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenMappedPara */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenMappedTaggedPara */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenConcPara */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenOverridePara */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseParagraph */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenInnerPile */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseInnerPile */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenSpan */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseSpan */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTable */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseTable */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTableRow */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseTableRow */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTableCell */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseTableCell */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTableHeaderCell */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseTableHeaderCell */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::MakeColumns */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::MakeColumnGroup */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTableHeader */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseTableHeader */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTableFooter */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseTableFooter */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::OpenTableBody */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::CloseTableBody */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::put_IntProperty */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::put_StringProperty */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::put_Props */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::get_StringWidth */ ,
	(void *) (INT_PTR) -1 /* IVwEnv::AddPicture */
};

const CInterfaceStubVtbl _IVwEnvStubVtbl =
{
	&IID_IVwEnv,
	&IVwEnv_ServerInfo,
	64,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0337, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwEmbeddedWindow, ver. 0.0,
   GUID={0xf6d10646,0xc00c,0x11d2,{0x80,0x78,0x00,0x00,0xc0,0xfb,0x81,0xb5}} */

#pragma code_seg(".orpc")
static const unsigned short IVwEmbeddedWindow_FormatStringOffsetTable[] =
	{
	11850,
	11910,
	4266,
	11946,
	11982,
	12018
	};

static const MIDL_STUBLESS_PROXY_INFO IVwEmbeddedWindow_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwEmbeddedWindow_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwEmbeddedWindow_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwEmbeddedWindow_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(9) _IVwEmbeddedWindowProxyVtbl =
{
	&IVwEmbeddedWindow_ProxyInfo,
	&IID_IVwEmbeddedWindow,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwEmbeddedWindow::MoveWindow */ ,
	(void *) (INT_PTR) -1 /* IVwEmbeddedWindow::get_IsWindowVisible */ ,
	(void *) (INT_PTR) -1 /* IVwEmbeddedWindow::ShowWindow */ ,
	(void *) (INT_PTR) -1 /* IVwEmbeddedWindow::DrawWindow */ ,
	(void *) (INT_PTR) -1 /* IVwEmbeddedWindow::get_Width */ ,
	(void *) (INT_PTR) -1 /* IVwEmbeddedWindow::get_Height */
};

const CInterfaceStubVtbl _IVwEmbeddedWindowStubVtbl =
{
	&IID_IVwEmbeddedWindow,
	&IVwEmbeddedWindow_ServerInfo,
	9,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0338, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDispatch, ver. 0.0,
   GUID={0x00020400,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}} */


/* Object interface: IVwSelection, ver. 0.0,
   GUID={0x4F8B678D,0xC5BA,0x4a2f,{0xB9,0xB3,0x27,0x80,0x95,0x6E,0x36,0x16}} */

#pragma code_seg(".orpc")
static const unsigned short IVwSelection_FormatStringOffsetTable[] =
	{
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	12054,
	12090,
	12144,
	12198,
	12246,
	12306,
	12348,
	12420,
	12462,
	12534,
	12630,
	12720,
	12756,
	12798,
	12828,
	12864,
	12936,
	12972,
	13008,
	13050,
	13098,
	13140,
	13176,
	13212,
	10776,
	13248,
	13290,
	13326,
	13368,
	13404,
	13440,
	13476,
	13518,
	13554,
	13596,
	13644,
	13692
	};

static const MIDL_STUBLESS_PROXY_INFO IVwSelection_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwSelection_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwSelection_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwSelection_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(44) _IVwSelectionProxyVtbl =
{
	&IVwSelection_ProxyInfo,
	&IID_IVwSelection,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	0 /* (void *) (INT_PTR) -1 /* IDispatch::GetTypeInfoCount */ ,
	0 /* (void *) (INT_PTR) -1 /* IDispatch::GetTypeInfo */ ,
	0 /* (void *) (INT_PTR) -1 /* IDispatch::GetIDsOfNames */ ,
	0 /* IDispatch_Invoke_Proxy */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_IsRange */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GetSelectionProps */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GetHardAndSoftCharProps */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GetParaProps */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GetHardAndSoftParaProps */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::SetSelectionProps */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::TextSelInfo */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::CLevels */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::PropInfo */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::AllTextSelInfo */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::AllSelEndInfo */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::Commit */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::CompleteEdits */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::ExtendToStringBoundaries */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_EndBeforeAnchor */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::Location */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GetParaLocation */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::ReplaceWithTsString */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GetSelectionString */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GetFirstParaString */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::SetIPLocation */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_CanFormatPara */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_CanFormatChar */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_CanFormatOverlay */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::Install */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_Follows */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_IsValid */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_ParagraphOffset */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_SelType */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_RootBox */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::GrowToWord */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::EndPoint */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::SetIpTypingProps */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_BoxDepth */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_BoxIndex */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_BoxCount */ ,
	(void *) (INT_PTR) -1 /* IVwSelection::get_BoxType */
};


static const PRPC_STUB_FUNCTION IVwSelection_table[] =
{
	STUB_FORWARDING_FUNCTION,
	STUB_FORWARDING_FUNCTION,
	STUB_FORWARDING_FUNCTION,
	STUB_FORWARDING_FUNCTION,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2
};

CInterfaceStubVtbl _IVwSelectionStubVtbl =
{
	&IID_IVwSelection,
	&IVwSelection_ServerInfo,
	44,
	&IVwSelection_table[-3],
	CStdStubBuffer_DELEGATING_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0339, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IEventListener, ver. 0.0,
   GUID={0xF696B01E,0x974B,0x4065,{0xB4,0x64,0xBD,0xF4,0x59,0x15,0x40,0x54}} */

#pragma code_seg(".orpc")
static const unsigned short IEventListener_FormatStringOffsetTable[] =
	{
	13740
	};

static const MIDL_STUBLESS_PROXY_INFO IEventListener_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IEventListener_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IEventListener_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IEventListener_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IEventListenerProxyVtbl =
{
	&IEventListener_ProxyInfo,
	&IID_IEventListener,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IEventListener::Notify */
};

const CInterfaceStubVtbl _IEventListenerStubVtbl =
{
	&IID_IEventListener,
	&IEventListener_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0340, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwStylesheet, ver. 0.0,
   GUID={0xD77C0DBC,0xC7BC,0x441d,{0x95,0x87,0x1E,0x36,0x64,0xE1,0xBC,0xD3}} */

#pragma code_seg(".orpc")
static const unsigned short IVwStylesheet_FormatStringOffsetTable[] =
	{
	13782,
	13818,
	13860,
	13944,
	13992,
	14034,
	14076,
	14118,
	14160,
	14202,
	14244,
	14280,
	14316,
	14352,
	14388,
	14430,
	14472,
	14508,
	14550
	};

static const MIDL_STUBLESS_PROXY_INFO IVwStylesheet_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwStylesheet_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwStylesheet_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwStylesheet_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(22) _IVwStylesheetProxyVtbl =
{
	&IVwStylesheet_ProxyInfo,
	&IID_IVwStylesheet,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::GetDefaultBasedOnStyleName */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::GetDefaultStyleForContext */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::PutStyle */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::GetStyleRgch */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::GetNextStyle */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::GetBasedOn */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::GetType */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::GetContext */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::IsBuiltIn */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::IsModified */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::get_DataAccess */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::MakeNewStyle */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::Delete */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::get_CStyles */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::get_NthStyle */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::get_NthStyleName */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::get_NormalFontStyle */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::get_IsStyleProtected */ ,
	(void *) (INT_PTR) -1 /* IVwStylesheet::CacheProps */
};

const CInterfaceStubVtbl _IVwStylesheetStubVtbl =
{
	&IID_IVwStylesheet,
	&IVwStylesheet_ServerInfo,
	22,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0341, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwPropertyStore, ver. 0.0,
   GUID={0x3D4847FE,0xEA2D,0x4255,{0xA4,0x96,0x77,0x00,0x59,0xA1,0x34,0xCC}} */

#pragma code_seg(".orpc")
static const unsigned short IVwPropertyStore_FormatStringOffsetTable[] =
	{
	14604,
	13818,
	14646,
	14688,
	14724,
	14760,
	14796,
	14832
	};

static const MIDL_STUBLESS_PROXY_INFO IVwPropertyStore_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwPropertyStore_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwPropertyStore_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwPropertyStore_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(11) _IVwPropertyStoreProxyVtbl =
{
	&IVwPropertyStore_ProxyInfo,
	&IID_IVwPropertyStore,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::get_IntProperty */ ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::get_StringProperty */ ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::get_ChrpFor */ ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::putref_Stylesheet */ ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::putref_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::get_ParentStore */ ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::get_TextProps */ ,
	(void *) (INT_PTR) -1 /* IVwPropertyStore::get_DerivedPropertiesForTtp */
};

const CInterfaceStubVtbl _IVwPropertyStoreStubVtbl =
{
	&IID_IVwPropertyStore,
	&IVwPropertyStore_ServerInfo,
	11,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0342, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwOverlay, ver. 0.0,
   GUID={0x7D9089C1,0x3BB9,0x11d4,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short IVwOverlay_FormatStringOffsetTable[] =
	{
	13782,
	14874,
	14910,
	14946,
	11982,
	4362,
	14982,
	15018,
	15054,
	15090,
	15126,
	14280,
	14316,
	14352,
	10266,
	15162,
	15198,
	15276,
	15366,
	15450,
	15528,
	15630,
	15666,
	15702
	};

static const MIDL_STUBLESS_PROXY_INFO IVwOverlay_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwOverlay_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwOverlay_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwOverlay_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(27) _IVwOverlayProxyVtbl =
{
	&IVwOverlay_ProxyInfo,
	&IID_IVwOverlay,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_Name */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::put_Name */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_Guid */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::put_Guid */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_PossListId */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::put_PossListId */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_Flags */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::put_Flags */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_FontName */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::put_FontName */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::FontNameRgch */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_FontSize */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::put_FontSize */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_MaxShowTags */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::put_MaxShowTags */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::get_CTags */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::GetDbTagInfo */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::SetTagInfo */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::GetTagInfo */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::GetDlgTagInfo */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::GetDispTagInfo */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::RemoveTag */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::Sort */ ,
	(void *) (INT_PTR) -1 /* IVwOverlay::Merge */
};

const CInterfaceStubVtbl _IVwOverlayStubVtbl =
{
	&IID_IVwOverlay,
	&IVwOverlay_ServerInfo,
	27,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0343, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwPrintContext, ver. 0.0,
   GUID={0xFF2E1DC2,0x95A8,0x41c6,{0x85,0xF4,0xFF,0xCA,0x3A,0x64,0x21,0x6A}} */

#pragma code_seg(".orpc")
static const unsigned short IVwPrintContext_FormatStringOffsetTable[] =
	{
	15744,
	15780,
	15816,
	15858,
	12054,
	12018,
	15900,
	15936,
	15984,
	16050,
	16080,
	16110,
	16140,
	14352,
	16170,
	16206,
	16248,
	16314,
	16374,
	1314,
	1344
	};

static const MIDL_STUBLESS_PROXY_INFO IVwPrintContext_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwPrintContext_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwPrintContext_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwPrintContext_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(24) _IVwPrintContextProxyVtbl =
{
	&IVwPrintContext_ProxyInfo,
	&IID_IVwPrintContext,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_Graphics */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_FirstPageNumber */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_IsPageWanted */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_AreMorePagesWanted */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_Aborted */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_Copies */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_Collate */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_HeaderString */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::GetMargins */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::OpenPage */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::ClosePage */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::OpenDoc */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::CloseDoc */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::get_LastPageNo */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::put_HeaderMask */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::SetHeaderString */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::SetMargins */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::SetPagePrintInfo */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::SetGraphics */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::RequestAbort */ ,
	(void *) (INT_PTR) -1 /* IVwPrintContext::AbortDoc */
};

const CInterfaceStubVtbl _IVwPrintContextStubVtbl =
{
	&IID_IVwPrintContext,
	&IVwPrintContext_ServerInfo,
	24,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0344, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ISqlUndoAction, ver. 0.0,
   GUID={0x2225FCC7,0x51AE,0x4461,{0x93,0x0C,0xA4,0x2A,0x8D,0xC5,0xA8,0x1A}} */

#pragma code_seg(".orpc")
static const unsigned short ISqlUndoAction_FormatStringOffsetTable[] =
	{
	16410,
	16458,
	16506,
	16554,
	16602,
	16668
	};

static const MIDL_STUBLESS_PROXY_INFO ISqlUndoAction_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ISqlUndoAction_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ISqlUndoAction_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ISqlUndoAction_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(9) _ISqlUndoActionProxyVtbl =
{
	&ISqlUndoAction_ProxyInfo,
	&IID_ISqlUndoAction,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ISqlUndoAction::AddRedoCommand */ ,
	(void *) (INT_PTR) -1 /* ISqlUndoAction::AddUndoCommand */ ,
	(void *) (INT_PTR) -1 /* ISqlUndoAction::VerifyUndoable */ ,
	(void *) (INT_PTR) -1 /* ISqlUndoAction::VerifyRedoable */ ,
	(void *) (INT_PTR) -1 /* ISqlUndoAction::AddRedoReloadInfo */ ,
	(void *) (INT_PTR) -1 /* ISqlUndoAction::AddUndoReloadInfo */
};

const CInterfaceStubVtbl _ISqlUndoActionStubVtbl =
{
	&IID_ISqlUndoAction,
	&ISqlUndoAction_ServerInfo,
	9,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0345, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwPattern, ver. 0.0,
   GUID={0xFACD01D9,0xBAF4,0x4ef0,{0xBE,0xD6,0xA8,0x96,0x61,0x60,0xC9,0x4D}} */

#pragma code_seg(".orpc")
static const unsigned short IVwPattern_FormatStringOffsetTable[] =
	{
	16734,
	16770,
	16806,
	16842,
	16878,
	16914,
	16950,
	16986,
	17022,
	17058,
	17094,
	17130,
	17166,
	17202,
	17238,
	12720,
	17274,
	17322,
	17370,
	17412,
	1344,
	17484,
	17520,
	17562,
	17598,
	17676,
	17718,
	17754,
	17790,
	17826,
	17862,
	17898,
	17934,
	17970,
	18006,
	18042,
	18078,
	18114,
	18150,
	18186,
	18222,
	18258,
	18294
	};

static const MIDL_STUBLESS_PROXY_INFO IVwPattern_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwPattern_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwPattern_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwPattern_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(46) _IVwPatternProxyVtbl =
{
	&IVwPattern_ProxyInfo,
	&IID_IVwPattern,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwPattern::putref_Pattern */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_Pattern */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::putref_Overlay */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_Overlay */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_MatchCase */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_MatchCase */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_MatchDiacritics */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_MatchDiacritics */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_MatchWholeWord */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_MatchWholeWord */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_MatchOldWritingSystem */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_MatchOldWritingSystem */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_MatchExactly */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_MatchExactly */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_MatchCompatibility */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_MatchCompatibility */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::Find */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::FindFrom */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::FindNext */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::FindIn */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::Install */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_Found */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::GetSelection */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::CLevels */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::AllTextSelInfo */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::MatchWhole */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::putref_Limit */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_Limit */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::putref_StartingPoint */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_StartingPoint */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_SearchWindow */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_SearchWindow */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_StoppedAtLimit */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_StoppedAtLimit */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_LastDirection */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::putref_ReplaceWith */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_ReplaceWith */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_ShowMore */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_ShowMore */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_IcuLocale */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_IcuLocale */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::get_IcuCollatingRules */ ,
	(void *) (INT_PTR) -1 /* IVwPattern::put_IcuCollatingRules */
};

const CInterfaceStubVtbl _IVwPatternStubVtbl =
{
	&IID_IVwPattern,
	&IVwPattern_ServerInfo,
	46,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0346, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwSearchKiller, ver. 0.0,
   GUID={0xFF1B39DE,0x20D3,0x4cdd,{0xA1,0x34,0xDC,0xBE,0x3B,0xE2,0x3F,0x3E}} */

#pragma code_seg(".orpc")
static const unsigned short IVwSearchKiller_FormatStringOffsetTable[] =
	{
	18330,
	18366,
	18396,
	18432
	};

static const MIDL_STUBLESS_PROXY_INFO IVwSearchKiller_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwSearchKiller_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwSearchKiller_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwSearchKiller_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IVwSearchKillerProxyVtbl =
{
	&IVwSearchKiller_ProxyInfo,
	&IID_IVwSearchKiller,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwSearchKiller::put_Window */ ,
	(void *) (INT_PTR) -1 /* IVwSearchKiller::FlushMessages */ ,
	(void *) (INT_PTR) -1 /* IVwSearchKiller::get_AbortRequest */ ,
	(void *) (INT_PTR) -1 /* IVwSearchKiller::put_AbortRequest */
};

const CInterfaceStubVtbl _IVwSearchKillerStubVtbl =
{
	&IID_IVwSearchKiller,
	&IVwSearchKiller_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0347, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwDrawRootBuffered, ver. 0.0,
   GUID={0x09752C4C,0xCC1E,0x4268,{0x89,0x1E,0x52,0x6B,0xBB,0xAC,0x0D,0xE8}} */

#pragma code_seg(".orpc")
static const unsigned short IVwDrawRootBuffered_FormatStringOffsetTable[] =
	{
	18468,
	18534
	};

static const MIDL_STUBLESS_PROXY_INFO IVwDrawRootBuffered_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwDrawRootBuffered_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwDrawRootBuffered_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwDrawRootBuffered_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _IVwDrawRootBufferedProxyVtbl =
{
	&IVwDrawRootBuffered_ProxyInfo,
	&IID_IVwDrawRootBuffered,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwDrawRootBuffered::DrawTheRoot */ ,
	(void *) (INT_PTR) -1 /* IVwDrawRootBuffered::DrawTheRootAt */
};

const CInterfaceStubVtbl _IVwDrawRootBufferedStubVtbl =
{
	&IID_IVwDrawRootBuffered,
	&IVwDrawRootBuffered_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0348, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwSynchronizer, ver. 0.0,
   GUID={0xC5C1E9DC,0x5880,0x4ee3,{0xB3,0xCD,0xEB,0xDD,0x13,0x2A,0x62,0x94}} */

#pragma code_seg(".orpc")
static const unsigned short IVwSynchronizer_FormatStringOffsetTable[] =
	{
	18624
	};

static const MIDL_STUBLESS_PROXY_INFO IVwSynchronizer_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwSynchronizer_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwSynchronizer_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwSynchronizer_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IVwSynchronizerProxyVtbl =
{
	&IVwSynchronizer_ProxyInfo,
	&IID_IVwSynchronizer,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwSynchronizer::AddRoot */
};

const CInterfaceStubVtbl _IVwSynchronizerStubVtbl =
{
	&IID_IVwSynchronizer,
	&IVwSynchronizer_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0349, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwDataSpec, ver. 0.0,
   GUID={0xDC9A7C08,0x138E,0x41C0,{0x85,0x32,0x5F,0xD6,0x4B,0x5E,0x72,0xBF}} */

#pragma code_seg(".orpc")
static const unsigned short IVwDataSpec_FormatStringOffsetTable[] =
	{
	18660
	};

static const MIDL_STUBLESS_PROXY_INFO IVwDataSpec_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwDataSpec_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwDataSpec_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwDataSpec_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IVwDataSpecProxyVtbl =
{
	&IVwDataSpec_ProxyInfo,
	&IID_IVwDataSpec,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwDataSpec::AddField */
};

const CInterfaceStubVtbl _IVwDataSpecStubVtbl =
{
	&IID_IVwDataSpec,
	&IVwDataSpec_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0350, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwNotifyObjCharDeletion, ver. 0.0,
   GUID={0xCF1E5D07,0xB479,0x4195,{0xB6,0x4C,0x02,0x93,0x1F,0x86,0x01,0x4D}} */

#pragma code_seg(".orpc")
static const unsigned short IVwNotifyObjCharDeletion_FormatStringOffsetTable[] =
	{
	18720
	};

static const MIDL_STUBLESS_PROXY_INFO IVwNotifyObjCharDeletion_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwNotifyObjCharDeletion_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwNotifyObjCharDeletion_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwNotifyObjCharDeletion_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IVwNotifyObjCharDeletionProxyVtbl =
{
	&IVwNotifyObjCharDeletion_ProxyInfo,
	&IID_IVwNotifyObjCharDeletion,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwNotifyObjCharDeletion::ObjDeleted */
};

const CInterfaceStubVtbl _IVwNotifyObjCharDeletionStubVtbl =
{
	&IID_IVwNotifyObjCharDeletion,
	&IVwNotifyObjCharDeletion_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0351, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwLayoutStream, ver. 0.0,
   GUID={0x963E6A91,0x513F,0x4490,{0xA2,0x82,0x0E,0x99,0xB5,0x42,0xB4,0xCC}} */

#pragma code_seg(".orpc")
static const unsigned short IVwLayoutStream_FormatStringOffsetTable[] =
	{
	18756,
	18792,
	18858,
	18930,
	18966,
	282,
	324,
	4434,
	19014
	};

static const MIDL_STUBLESS_PROXY_INFO IVwLayoutStream_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwLayoutStream_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwLayoutStream_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwLayoutStream_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _IVwLayoutStreamProxyVtbl =
{
	&IVwLayoutStream_ProxyInfo,
	&IID_IVwLayoutStream,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::SetManager */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::LayoutObj */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::LayoutPage */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::DiscardPage */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::PageBoundary */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::PageHeight */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::PagePostion */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::RollbackLayoutObjects */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutStream::CommitLayoutObjects */
};

const CInterfaceStubVtbl _IVwLayoutStreamStubVtbl =
{
	&IID_IVwLayoutStream,
	&IVwLayoutStream_ServerInfo,
	12,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0352, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwLayoutManager, ver. 0.0,
   GUID={0x13F3A421,0x4915,0x455b,{0xB5,0x7F,0xAF,0xD4,0x07,0x3C,0xFF,0xA0}} */

#pragma code_seg(".orpc")
static const unsigned short IVwLayoutManager_FormatStringOffsetTable[] =
	{
	19050,
	19128,
	19170,
	19218
	};

static const MIDL_STUBLESS_PROXY_INFO IVwLayoutManager_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwLayoutManager_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwLayoutManager_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwLayoutManager_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IVwLayoutManagerProxyVtbl =
{
	&IVwLayoutManager_ProxyInfo,
	&IID_IVwLayoutManager,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwLayoutManager::AddDependentObjects */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutManager::PageBroken */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutManager::PageBoundaryMoved */ ,
	(void *) (INT_PTR) -1 /* IVwLayoutManager::EstimateHeight */
};

const CInterfaceStubVtbl _IVwLayoutManagerStubVtbl =
{
	&IID_IVwLayoutManager,
	&IVwLayoutManager_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_ViewsPs_0353, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwVirtualHandler, ver. 0.0,
   GUID={0xF8851137,0x6562,0x4120,{0xA3,0x4E,0x1A,0x51,0xEE,0x59,0x8E,0xA7}} */

#pragma code_seg(".orpc")
static const unsigned short IVwVirtualHandler_FormatStringOffsetTable[] =
	{
	19260,
	19296,
	19332,
	19368,
	4326,
	12018,
	4398,
	19404,
	17022,
	17058,
	17094,
	17130,
	19440,
	19494,
	19566,
	19626,
	19680,
	19734,
	19794,
	19830
	};

static const MIDL_STUBLESS_PROXY_INFO IVwVirtualHandler_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwVirtualHandler_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwVirtualHandler_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwVirtualHandler_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(23) _IVwVirtualHandlerProxyVtbl =
{
	&IVwVirtualHandler_ProxyInfo,
	&IID_IVwVirtualHandler,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::put_ClassName */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::get_ClassName */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::put_FieldName */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::get_FieldName */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::put_Tag */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::get_Tag */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::put_Type */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::get_Type */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::put_Writeable */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::get_Writeable */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::put_ComputeEveryTime */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::get_ComputeEveryTime */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::Load */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::Replace */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::WriteObj */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::WriteInt64 */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::WriteUnicode */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::PreLoad */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::Initialize */ ,
	(void *) (INT_PTR) -1 /* IVwVirtualHandler::DoesResultDependOnProp */
};

const CInterfaceStubVtbl _IVwVirtualHandlerStubVtbl =
{
	&IID_IVwVirtualHandler,
	&IVwVirtualHandler_ServerInfo,
	23,
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

const CInterfaceProxyVtbl * _ViewsPs_ProxyVtblList[] =
{
	( CInterfaceProxyVtbl *) &_IVwCacheDaProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwNotifyObjCharDeletionProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwDataSpecProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwOleDbDaProxyVtbl,
	( CInterfaceProxyVtbl *) &_IEventListenerProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwLayoutManagerProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwVirtualHandlerProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwRootSiteProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwNotifyChangeProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwEmbeddedWindowProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwDrawRootBufferedProxyVtbl,
	( CInterfaceProxyVtbl *) &_ISetupVwOleDbDaProxyVtbl,
	( CInterfaceProxyVtbl *) &_ISilDataAccessProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwViewConstructorProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwSelectionProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwLayoutStreamProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwRootBoxProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwStylesheetProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwOverlayProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwPrintContextProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwEnvProxyVtbl,
	( CInterfaceProxyVtbl *) &_ISqlUndoActionProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDbColSpecProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwPatternProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwSynchronizerProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwSearchKillerProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwObjDelNotificationProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwPropertyStoreProxyVtbl,
	0
};

const CInterfaceStubVtbl * _ViewsPs_StubVtblList[] =
{
	( CInterfaceStubVtbl *) &_IVwCacheDaStubVtbl,
	( CInterfaceStubVtbl *) &_IVwNotifyObjCharDeletionStubVtbl,
	( CInterfaceStubVtbl *) &_IVwDataSpecStubVtbl,
	( CInterfaceStubVtbl *) &_IVwOleDbDaStubVtbl,
	( CInterfaceStubVtbl *) &_IEventListenerStubVtbl,
	( CInterfaceStubVtbl *) &_IVwLayoutManagerStubVtbl,
	( CInterfaceStubVtbl *) &_IVwVirtualHandlerStubVtbl,
	( CInterfaceStubVtbl *) &_IVwRootSiteStubVtbl,
	( CInterfaceStubVtbl *) &_IVwNotifyChangeStubVtbl,
	( CInterfaceStubVtbl *) &_IVwEmbeddedWindowStubVtbl,
	( CInterfaceStubVtbl *) &_IVwDrawRootBufferedStubVtbl,
	( CInterfaceStubVtbl *) &_ISetupVwOleDbDaStubVtbl,
	( CInterfaceStubVtbl *) &_ISilDataAccessStubVtbl,
	( CInterfaceStubVtbl *) &_IVwViewConstructorStubVtbl,
	( CInterfaceStubVtbl *) &_IVwSelectionStubVtbl,
	( CInterfaceStubVtbl *) &_IVwLayoutStreamStubVtbl,
	( CInterfaceStubVtbl *) &_IVwRootBoxStubVtbl,
	( CInterfaceStubVtbl *) &_IVwStylesheetStubVtbl,
	( CInterfaceStubVtbl *) &_IVwOverlayStubVtbl,
	( CInterfaceStubVtbl *) &_IVwPrintContextStubVtbl,
	( CInterfaceStubVtbl *) &_IVwEnvStubVtbl,
	( CInterfaceStubVtbl *) &_ISqlUndoActionStubVtbl,
	( CInterfaceStubVtbl *) &_IDbColSpecStubVtbl,
	( CInterfaceStubVtbl *) &_IVwPatternStubVtbl,
	( CInterfaceStubVtbl *) &_IVwSynchronizerStubVtbl,
	( CInterfaceStubVtbl *) &_IVwSearchKillerStubVtbl,
	( CInterfaceStubVtbl *) &_IVwObjDelNotificationStubVtbl,
	( CInterfaceStubVtbl *) &_IVwPropertyStoreStubVtbl,
	0
};

PCInterfaceName const _ViewsPs_InterfaceNamesList[] =
{
	"IVwCacheDa",
	"IVwNotifyObjCharDeletion",
	"IVwDataSpec",
	"IVwOleDbDa",
	"IEventListener",
	"IVwLayoutManager",
	"IVwVirtualHandler",
	"IVwRootSite",
	"IVwNotifyChange",
	"IVwEmbeddedWindow",
	"IVwDrawRootBuffered",
	"ISetupVwOleDbDa",
	"ISilDataAccess",
	"IVwViewConstructor",
	"IVwSelection",
	"IVwLayoutStream",
	"IVwRootBox",
	"IVwStylesheet",
	"IVwOverlay",
	"IVwPrintContext",
	"IVwEnv",
	"ISqlUndoAction",
	"IDbColSpec",
	"IVwPattern",
	"IVwSynchronizer",
	"IVwSearchKiller",
	"IVwObjDelNotification",
	"IVwPropertyStore",
	0
};

const IID *  _ViewsPs_BaseIIDList[] =
{
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	&IID_IDispatch,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0,
	0
};


#define _ViewsPs_CHECK_IID(n)	IID_GENERIC_CHECK_IID( _ViewsPs, pIID, n)

int __stdcall _ViewsPs_IID_Lookup( const IID * pIID, int * pIndex )
{
	IID_BS_LOOKUP_SETUP

	IID_BS_LOOKUP_INITIAL_TEST( _ViewsPs, 28, 16 )
	IID_BS_LOOKUP_NEXT_TEST( _ViewsPs, 8 )
	IID_BS_LOOKUP_NEXT_TEST( _ViewsPs, 4 )
	IID_BS_LOOKUP_NEXT_TEST( _ViewsPs, 2 )
	IID_BS_LOOKUP_NEXT_TEST( _ViewsPs, 1 )
	IID_BS_LOOKUP_RETURN_RESULT( _ViewsPs, 28, *pIndex )

}

const ExtendedProxyFileInfo ViewsPs_ProxyFileInfo =
{
	(PCInterfaceProxyVtblList *) & _ViewsPs_ProxyVtblList,
	(PCInterfaceStubVtblList *) & _ViewsPs_StubVtblList,
	(const PCInterfaceName * ) & _ViewsPs_InterfaceNamesList,
	(const IID ** ) & _ViewsPs_BaseIIDList,
	& _ViewsPs_IID_Lookup,
	28,
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
