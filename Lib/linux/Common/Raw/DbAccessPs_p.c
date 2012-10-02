

/* this ALWAYS GENERATED file contains the proxy stub code */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:01:43 2006
 */
/* Compiler settings for C:\fw\Output\Common\DbAccessPs.idl:
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


#include "DbAccessPs.h"

#define TYPE_FORMAT_STRING_SIZE   259
#define PROC_FORMAT_STRING_SIZE   2647
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


extern const MIDL_SERVER_INFO IOleDbCommand_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IOleDbCommand_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IOleDbEncap_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IOleDbEncap_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IFwMetaDataCache_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IFwMetaDataCache_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IDbAdmin_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IDbAdmin_ProxyInfo;


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

	/* Procedure ColValWasNull */

			0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x3 ),	/* 3 */
/*  8 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12 */	NdrFcShort( 0x24 ),	/* 36 */
/* 14 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfIsNull */

/* 24 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 26 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 28 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 30 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 32 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 34 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ExecCommand */

/* 36 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 38 */	NdrFcLong( 0x0 ),	/* 0 */
/* 42 */	NdrFcShort( 0x4 ),	/* 4 */
/* 44 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 46 */	NdrFcShort( 0x8 ),	/* 8 */
/* 48 */	NdrFcShort( 0x8 ),	/* 8 */
/* 50 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 52 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 54 */	NdrFcShort( 0x0 ),	/* 0 */
/* 56 */	NdrFcShort( 0x1 ),	/* 1 */
/* 58 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSqlStatement */

/* 60 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 62 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 64 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter nStatementType */

/* 66 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 68 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 70 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 72 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 74 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 76 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetColValue */

/* 78 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 80 */	NdrFcLong( 0x0 ),	/* 0 */
/* 84 */	NdrFcShort( 0x5 ),	/* 5 */
/* 86 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 88 */	NdrFcShort( 0x18 ),	/* 24 */
/* 90 */	NdrFcShort( 0x3e ),	/* 62 */
/* 92 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x7,		/* 7 */
/* 94 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 96 */	NdrFcShort( 0x1 ),	/* 1 */
/* 98 */	NdrFcShort( 0x0 ),	/* 0 */
/* 100 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluColIndex */

/* 102 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 104 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 106 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluDataBuffer */

/* 108 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 110 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 112 */	NdrFcShort( 0x2e ),	/* Type Offset=46 */

	/* Parameter cbBufferLength */

/* 114 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 116 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 118 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcbAmtBuffUsed */

/* 120 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 122 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfIsNull */

/* 126 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 128 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 130 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter cbPad */

/* 132 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 134 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 136 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 138 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 140 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 142 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetInt */

/* 144 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 146 */	NdrFcLong( 0x0 ),	/* 0 */
/* 150 */	NdrFcShort( 0x6 ),	/* 6 */
/* 152 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 154 */	NdrFcShort( 0x8 ),	/* 8 */
/* 156 */	NdrFcShort( 0x24 ),	/* 36 */
/* 158 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 160 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 162 */	NdrFcShort( 0x0 ),	/* 0 */
/* 164 */	NdrFcShort( 0x0 ),	/* 0 */
/* 166 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iColIndex */

/* 168 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 170 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnValue */

/* 174 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 176 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 178 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 180 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 182 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 184 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetParameter */

/* 186 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 188 */	NdrFcLong( 0x0 ),	/* 0 */
/* 192 */	NdrFcShort( 0x7 ),	/* 7 */
/* 194 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 196 */	NdrFcShort( 0x10 ),	/* 16 */
/* 198 */	NdrFcShort( 0x22 ),	/* 34 */
/* 200 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 202 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 204 */	NdrFcShort( 0x1 ),	/* 1 */
/* 206 */	NdrFcShort( 0x0 ),	/* 0 */
/* 208 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluColIndex */

/* 210 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 212 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 214 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluDataBuffer */

/* 216 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 218 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 220 */	NdrFcShort( 0x2e ),	/* Type Offset=46 */

	/* Parameter cluBufferLength */

/* 222 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 224 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 226 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfIsNull */

/* 228 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 230 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 232 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 234 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 236 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetRowset */

/* 240 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 242 */	NdrFcLong( 0x0 ),	/* 0 */
/* 246 */	NdrFcShort( 0x8 ),	/* 8 */
/* 248 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 250 */	NdrFcShort( 0x8 ),	/* 8 */
/* 252 */	NdrFcShort( 0x8 ),	/* 8 */
/* 254 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 256 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 260 */	NdrFcShort( 0x0 ),	/* 0 */
/* 262 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nRowsBuffered */

/* 264 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 266 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 268 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 270 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 272 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 274 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 276 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 278 */	NdrFcLong( 0x0 ),	/* 0 */
/* 282 */	NdrFcShort( 0x9 ),	/* 9 */
/* 284 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 286 */	NdrFcShort( 0x0 ),	/* 0 */
/* 288 */	NdrFcShort( 0x8 ),	/* 8 */
/* 290 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 292 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 294 */	NdrFcShort( 0x0 ),	/* 0 */
/* 296 */	NdrFcShort( 0x0 ),	/* 0 */
/* 298 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter punkSession */

/* 300 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 302 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 304 */	NdrFcShort( 0x3e ),	/* Type Offset=62 */

	/* Parameter pfistLog */

/* 306 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 308 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 310 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Return value */

/* 312 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 314 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 316 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NextRow */

/* 318 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 320 */	NdrFcLong( 0x0 ),	/* 0 */
/* 324 */	NdrFcShort( 0xa ),	/* 10 */
/* 326 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 328 */	NdrFcShort( 0x0 ),	/* 0 */
/* 330 */	NdrFcShort( 0x22 ),	/* 34 */
/* 332 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 334 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 336 */	NdrFcShort( 0x0 ),	/* 0 */
/* 338 */	NdrFcShort( 0x0 ),	/* 0 */
/* 340 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfMoreRows */

/* 342 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 344 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 346 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 348 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 350 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetParameter */

/* 354 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 356 */	NdrFcLong( 0x0 ),	/* 0 */
/* 360 */	NdrFcShort( 0xb ),	/* 11 */
/* 362 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 364 */	NdrFcShort( 0x1e ),	/* 30 */
/* 366 */	NdrFcShort( 0x8 ),	/* 8 */
/* 368 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 370 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 374 */	NdrFcShort( 0x2 ),	/* 2 */
/* 376 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluParamIndex */

/* 378 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 380 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 382 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dwFlags */

/* 384 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 386 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 388 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrParamName */

/* 390 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 392 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 394 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter nDataType */

/* 396 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 398 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 400 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter prgluDataBuffer */

/* 402 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 404 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 406 */	NdrFcShort( 0x66 ),	/* Type Offset=102 */

	/* Parameter cluBufferLength */

/* 408 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 410 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 412 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 414 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 416 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetByteBuffParameter */

/* 420 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 422 */	NdrFcLong( 0x0 ),	/* 0 */
/* 426 */	NdrFcShort( 0xc ),	/* 12 */
/* 428 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 430 */	NdrFcShort( 0x18 ),	/* 24 */
/* 432 */	NdrFcShort( 0x8 ),	/* 8 */
/* 434 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 436 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 438 */	NdrFcShort( 0x0 ),	/* 0 */
/* 440 */	NdrFcShort( 0x2 ),	/* 2 */
/* 442 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluParamIndex */

/* 444 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 446 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 448 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dwFlags */

/* 450 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 452 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 454 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrParamName */

/* 456 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 458 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 460 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter prgbDataBuffer */

/* 462 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 464 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 466 */	NdrFcShort( 0x76 ),	/* Type Offset=118 */

	/* Parameter cluBufferLength */

/* 468 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 470 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 472 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 474 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 476 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 478 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetStringParameter */

/* 480 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 482 */	NdrFcLong( 0x0 ),	/* 0 */
/* 486 */	NdrFcShort( 0xd ),	/* 13 */
/* 488 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 490 */	NdrFcShort( 0x18 ),	/* 24 */
/* 492 */	NdrFcShort( 0x8 ),	/* 8 */
/* 494 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 496 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 498 */	NdrFcShort( 0x0 ),	/* 0 */
/* 500 */	NdrFcShort( 0x2 ),	/* 2 */
/* 502 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluParamIndex */

/* 504 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 506 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dwFlags */

/* 510 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 512 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 514 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrParamName */

/* 516 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 518 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 520 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter prgchDataBuffer */

/* 522 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 524 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 526 */	NdrFcShort( 0x86 ),	/* Type Offset=134 */

	/* Parameter cluBufferLength */

/* 528 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 530 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 532 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 534 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 536 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BeginTrans */

/* 540 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 542 */	NdrFcLong( 0x0 ),	/* 0 */
/* 546 */	NdrFcShort( 0x3 ),	/* 3 */
/* 548 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 550 */	NdrFcShort( 0x0 ),	/* 0 */
/* 552 */	NdrFcShort( 0x8 ),	/* 8 */
/* 554 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 556 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 558 */	NdrFcShort( 0x0 ),	/* 0 */
/* 560 */	NdrFcShort( 0x0 ),	/* 0 */
/* 562 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 564 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 566 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 568 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CommitTrans */

/* 570 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 572 */	NdrFcLong( 0x0 ),	/* 0 */
/* 576 */	NdrFcShort( 0x4 ),	/* 4 */
/* 578 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 580 */	NdrFcShort( 0x0 ),	/* 0 */
/* 582 */	NdrFcShort( 0x8 ),	/* 8 */
/* 584 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 586 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 588 */	NdrFcShort( 0x0 ),	/* 0 */
/* 590 */	NdrFcShort( 0x0 ),	/* 0 */
/* 592 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 594 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 596 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CreateCommand */

/* 600 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 602 */	NdrFcLong( 0x0 ),	/* 0 */
/* 606 */	NdrFcShort( 0x5 ),	/* 5 */
/* 608 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 610 */	NdrFcShort( 0x0 ),	/* 0 */
/* 612 */	NdrFcShort( 0x8 ),	/* 8 */
/* 614 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 616 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 618 */	NdrFcShort( 0x0 ),	/* 0 */
/* 620 */	NdrFcShort( 0x0 ),	/* 0 */
/* 622 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppodc */

/* 624 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 626 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 628 */	NdrFcShort( 0x92 ),	/* Type Offset=146 */

	/* Return value */

/* 630 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 632 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 634 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 636 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 638 */	NdrFcLong( 0x0 ),	/* 0 */
/* 642 */	NdrFcShort( 0x6 ),	/* 6 */
/* 644 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 646 */	NdrFcShort( 0x10 ),	/* 16 */
/* 648 */	NdrFcShort( 0x8 ),	/* 8 */
/* 650 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 652 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 654 */	NdrFcShort( 0x0 ),	/* 0 */
/* 656 */	NdrFcShort( 0x2 ),	/* 2 */
/* 658 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrServer */

/* 660 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 662 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 664 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter bstrDatabase */

/* 666 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 668 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 670 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter pfistLog */

/* 672 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 674 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 676 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Parameter olt */

/* 678 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 680 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 682 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter nmsTimeout */

/* 684 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 686 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 688 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 690 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 692 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsTransactionOpen */

/* 696 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 698 */	NdrFcLong( 0x0 ),	/* 0 */
/* 702 */	NdrFcShort( 0x7 ),	/* 7 */
/* 704 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 706 */	NdrFcShort( 0x0 ),	/* 0 */
/* 708 */	NdrFcShort( 0x22 ),	/* 34 */
/* 710 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 712 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 714 */	NdrFcShort( 0x0 ),	/* 0 */
/* 716 */	NdrFcShort( 0x0 ),	/* 0 */
/* 718 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfTransactionOpen */

/* 720 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 722 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 724 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 726 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 728 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 730 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RollbackTrans */

/* 732 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 734 */	NdrFcLong( 0x0 ),	/* 0 */
/* 738 */	NdrFcShort( 0x8 ),	/* 8 */
/* 740 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 742 */	NdrFcShort( 0x0 ),	/* 0 */
/* 744 */	NdrFcShort( 0x8 ),	/* 8 */
/* 746 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 748 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 750 */	NdrFcShort( 0x0 ),	/* 0 */
/* 752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 754 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 756 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 758 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 760 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RollbackSavePoint */

/* 762 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 764 */	NdrFcLong( 0x0 ),	/* 0 */
/* 768 */	NdrFcShort( 0x9 ),	/* 9 */
/* 770 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 772 */	NdrFcShort( 0x0 ),	/* 0 */
/* 774 */	NdrFcShort( 0x8 ),	/* 8 */
/* 776 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 778 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 780 */	NdrFcShort( 0x0 ),	/* 0 */
/* 782 */	NdrFcShort( 0x1 ),	/* 1 */
/* 784 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSavePoint */

/* 786 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 788 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 790 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Return value */

/* 792 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 794 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 796 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FwDatabaseDir */


	/* Procedure SetSavePoint */

/* 798 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 800 */	NdrFcLong( 0x0 ),	/* 0 */
/* 804 */	NdrFcShort( 0xa ),	/* 10 */
/* 806 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 808 */	NdrFcShort( 0x0 ),	/* 0 */
/* 810 */	NdrFcShort( 0x8 ),	/* 8 */
/* 812 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 814 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 816 */	NdrFcShort( 0x1 ),	/* 1 */
/* 818 */	NdrFcShort( 0x0 ),	/* 0 */
/* 820 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstr */

/* 822 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 824 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 826 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */


	/* Return value */

/* 828 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 830 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 832 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FwTemplateDir */


	/* Procedure SetSavePointOrBeginTrans */

/* 834 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 836 */	NdrFcLong( 0x0 ),	/* 0 */
/* 840 */	NdrFcShort( 0xb ),	/* 11 */
/* 842 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 844 */	NdrFcShort( 0x0 ),	/* 0 */
/* 846 */	NdrFcShort( 0x8 ),	/* 8 */
/* 848 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 850 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 852 */	NdrFcShort( 0x1 ),	/* 1 */
/* 854 */	NdrFcShort( 0x0 ),	/* 0 */
/* 856 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstr */

/* 858 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 860 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 862 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */


	/* Return value */

/* 864 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 866 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 868 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InitMSDE */

/* 870 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 872 */	NdrFcLong( 0x0 ),	/* 0 */
/* 876 */	NdrFcShort( 0xc ),	/* 12 */
/* 878 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 880 */	NdrFcShort( 0x6 ),	/* 6 */
/* 882 */	NdrFcShort( 0x8 ),	/* 8 */
/* 884 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 886 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 888 */	NdrFcShort( 0x0 ),	/* 0 */
/* 890 */	NdrFcShort( 0x0 ),	/* 0 */
/* 892 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfistLog */

/* 894 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 896 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 898 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Parameter fForce */

/* 900 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 902 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 904 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 906 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 908 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 910 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Server */

/* 912 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 914 */	NdrFcLong( 0x0 ),	/* 0 */
/* 918 */	NdrFcShort( 0xd ),	/* 13 */
/* 920 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 922 */	NdrFcShort( 0x0 ),	/* 0 */
/* 924 */	NdrFcShort( 0x8 ),	/* 8 */
/* 926 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 928 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 930 */	NdrFcShort( 0x1 ),	/* 1 */
/* 932 */	NdrFcShort( 0x0 ),	/* 0 */
/* 934 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrSvr */

/* 936 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 938 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 940 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 942 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 944 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 946 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Database */

/* 948 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 950 */	NdrFcLong( 0x0 ),	/* 0 */
/* 954 */	NdrFcShort( 0xe ),	/* 14 */
/* 956 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 958 */	NdrFcShort( 0x0 ),	/* 0 */
/* 960 */	NdrFcShort( 0x8 ),	/* 8 */
/* 962 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 964 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 966 */	NdrFcShort( 0x1 ),	/* 1 */
/* 968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 970 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrDb */

/* 972 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 974 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 976 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 978 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 980 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 982 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFreeLogKb */

/* 984 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 986 */	NdrFcLong( 0x0 ),	/* 0 */
/* 990 */	NdrFcShort( 0xf ),	/* 15 */
/* 992 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 994 */	NdrFcShort( 0x8 ),	/* 8 */
/* 996 */	NdrFcShort( 0x24 ),	/* 36 */
/* 998 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1000 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1004 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1006 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nReservespace */

/* 1008 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1010 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1012 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnKbFree */

/* 1014 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1016 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1018 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1020 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1022 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1024 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 1026 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1028 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1032 */	NdrFcShort( 0x3 ),	/* 3 */
/* 1034 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1036 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1038 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1040 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 1042 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1044 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1046 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1048 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 1050 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 1052 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1054 */	NdrFcShort( 0xba ),	/* Type Offset=186 */

	/* Return value */

/* 1056 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1058 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1060 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Reload */

/* 1062 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1064 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1068 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1070 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1072 */	NdrFcShort( 0x6 ),	/* 6 */
/* 1074 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1076 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 1078 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1080 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1082 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1084 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 1086 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 1088 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1090 */	NdrFcShort( 0xba ),	/* Type Offset=186 */

	/* Parameter fKeepVirtuals */

/* 1092 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1094 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1096 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 1098 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1100 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1102 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InitXml */

/* 1104 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1106 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1110 */	NdrFcShort( 0x5 ),	/* 5 */
/* 1112 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1114 */	NdrFcShort( 0x6 ),	/* 6 */
/* 1116 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1118 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 1120 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1122 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1124 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1126 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrPathname */

/* 1128 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1130 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1132 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter fClearPrevCache */

/* 1134 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1136 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1138 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 1140 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1142 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1144 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FieldCount */

/* 1146 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1148 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1152 */	NdrFcShort( 0x6 ),	/* 6 */
/* 1154 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1156 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1158 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1160 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1162 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1164 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1166 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1168 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcflid */

/* 1170 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1172 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1174 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1176 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1178 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1180 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldIds */

/* 1182 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1184 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1188 */	NdrFcShort( 0x7 ),	/* 7 */
/* 1190 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1192 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1194 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1196 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1198 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1200 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1202 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1204 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cflid */

/* 1206 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1208 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1210 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rgflid */

/* 1212 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 1214 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1216 */	NdrFcShort( 0xd0 ),	/* Type Offset=208 */

	/* Return value */

/* 1218 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1220 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1222 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetOwnClsName */

/* 1224 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1226 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1230 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1232 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1234 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1236 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1238 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1240 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1242 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1244 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1246 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1248 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1250 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1252 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrOwnClsName */

/* 1254 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1256 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1258 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1260 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1262 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1264 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDstClsName */

/* 1266 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1268 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1272 */	NdrFcShort( 0x9 ),	/* 9 */
/* 1274 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1276 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1278 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1280 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1282 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1284 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1286 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1288 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1290 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1292 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1294 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrDstClsName */

/* 1296 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1298 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1300 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1302 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1304 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1306 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetOwnClsId */

/* 1308 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1310 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1314 */	NdrFcShort( 0xa ),	/* 10 */
/* 1316 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1318 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1320 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1322 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1324 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1326 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1328 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1330 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1332 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1334 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1336 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pluOwnClsid */

/* 1338 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1340 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1342 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1344 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1346 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1348 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDstClsId */

/* 1350 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1352 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1356 */	NdrFcShort( 0xb ),	/* 11 */
/* 1358 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1360 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1362 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1364 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1366 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1372 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1374 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1376 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1378 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pluDstClsid */

/* 1380 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1382 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1384 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1386 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1388 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1390 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldName */

/* 1392 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1394 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1398 */	NdrFcShort( 0xc ),	/* 12 */
/* 1400 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1402 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1404 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1406 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1408 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1410 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1412 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1414 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1416 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1418 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1420 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldName */

/* 1422 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1424 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1426 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1428 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1430 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1432 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldLabel */

/* 1434 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1436 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1440 */	NdrFcShort( 0xd ),	/* 13 */
/* 1442 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1444 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1446 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1448 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1450 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1452 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1454 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1456 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1458 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1460 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1462 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldLabel */

/* 1464 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1466 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1468 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1470 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1472 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1474 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldHelp */

/* 1476 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1478 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1482 */	NdrFcShort( 0xe ),	/* 14 */
/* 1484 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1486 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1488 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1490 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1492 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1494 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1496 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1498 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1500 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1502 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1504 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldHelp */

/* 1506 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1508 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1510 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1512 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1514 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1516 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldXml */

/* 1518 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1520 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1524 */	NdrFcShort( 0xf ),	/* 15 */
/* 1526 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1528 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1530 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1532 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1534 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1536 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1538 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1540 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1542 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1544 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1546 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldXml */

/* 1548 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1550 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1552 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1554 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1556 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1558 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldListRoot */

/* 1560 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1562 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1566 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1568 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1570 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1572 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1574 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1576 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1578 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1580 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1582 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1584 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1586 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1588 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter piListRoot */

/* 1590 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1592 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1594 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1596 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1598 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldWs */

/* 1602 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1604 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1608 */	NdrFcShort( 0x11 ),	/* 17 */
/* 1610 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1612 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1614 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1616 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1618 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1620 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1622 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1624 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1626 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1628 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1630 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter piWs */

/* 1632 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1634 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1636 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1638 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1640 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1642 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldType */

/* 1644 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1646 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1650 */	NdrFcShort( 0x12 ),	/* 18 */
/* 1652 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1654 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1656 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1658 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1660 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1662 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1664 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1666 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1668 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1670 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1672 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter piType */

/* 1674 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1676 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1680 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1682 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1684 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsValidClass */

/* 1686 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1688 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1692 */	NdrFcShort( 0x13 ),	/* 19 */
/* 1694 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1696 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1698 */	NdrFcShort( 0x22 ),	/* 34 */
/* 1700 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 1702 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1704 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1706 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1708 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 1710 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1712 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter luClid */

/* 1716 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1718 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfValid */

/* 1722 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1724 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1726 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 1728 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1730 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1732 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ClassCount */

/* 1734 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1736 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1740 */	NdrFcShort( 0x14 ),	/* 20 */
/* 1742 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1744 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1746 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1748 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1750 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1754 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1756 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcclid */

/* 1758 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1760 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1762 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1764 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1766 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1768 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetClassIds */

/* 1770 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1772 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1776 */	NdrFcShort( 0x15 ),	/* 21 */
/* 1778 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1780 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1782 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1784 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1786 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1788 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1790 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1792 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cclid */

/* 1794 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1796 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1798 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rgclid */

/* 1800 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 1802 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1804 */	NdrFcShort( 0xd0 ),	/* Type Offset=208 */

	/* Return value */

/* 1806 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1808 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1810 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetClassName */

/* 1812 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1814 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1818 */	NdrFcShort( 0x16 ),	/* 22 */
/* 1820 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1822 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1824 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1826 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1828 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1830 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1832 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1834 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 1836 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1838 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1840 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrClassName */

/* 1842 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1844 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1846 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1848 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1850 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1852 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetAbstract */

/* 1854 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1856 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1860 */	NdrFcShort( 0x17 ),	/* 23 */
/* 1862 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1864 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1866 */	NdrFcShort( 0x22 ),	/* 34 */
/* 1868 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1870 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1872 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1874 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1876 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 1878 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1880 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1882 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfAbstract */

/* 1884 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1886 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1888 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 1890 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1892 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1894 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBaseClsId */

/* 1896 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1898 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1902 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1904 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1906 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1908 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1910 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1912 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1914 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1916 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1918 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 1920 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1922 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1924 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pluClid */

/* 1926 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1928 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1930 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1932 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1934 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1936 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBaseClsName */

/* 1938 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1940 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1944 */	NdrFcShort( 0x19 ),	/* 25 */
/* 1946 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1948 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1950 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1952 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1954 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1956 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1958 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1960 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 1962 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1964 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1966 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrBaseClsName */

/* 1968 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1970 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1972 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 1974 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1976 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1978 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFields */

/* 1980 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1982 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1986 */	NdrFcShort( 0x1a ),	/* 26 */
/* 1988 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 1990 */	NdrFcShort( 0x1e ),	/* 30 */
/* 1992 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1994 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x7,		/* 7 */
/* 1996 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1998 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2000 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2002 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 2004 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2006 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2008 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fIncludeSuperclasses */

/* 2010 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2012 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2014 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter grfcpt */

/* 2016 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2018 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2020 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cflidMax */

/* 2022 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2024 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2026 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgflid */

/* 2028 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 2030 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2032 */	NdrFcShort( 0xe0 ),	/* Type Offset=224 */

	/* Parameter pcflid */

/* 2034 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2036 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2038 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2040 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2042 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 2044 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetClassId */

/* 2046 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2048 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2052 */	NdrFcShort( 0x1b ),	/* 27 */
/* 2054 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2056 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2058 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2060 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 2062 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2064 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2066 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2068 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrClassName */

/* 2070 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2072 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2074 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter pluClid */

/* 2076 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2078 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2080 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2082 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2084 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2086 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldId */

/* 2088 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2090 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2094 */	NdrFcShort( 0x1c ),	/* 28 */
/* 2096 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2098 */	NdrFcShort( 0x6 ),	/* 6 */
/* 2100 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2102 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 2104 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2106 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2108 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2110 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrClassName */

/* 2112 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2114 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2116 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter bstrFieldName */

/* 2118 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2120 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2122 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter fIncludeBaseClasses */

/* 2124 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2126 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2128 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pluFlid */

/* 2130 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2132 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2134 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2136 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2138 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2140 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldId2 */

/* 2142 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2144 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2148 */	NdrFcShort( 0x1d ),	/* 29 */
/* 2150 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2152 */	NdrFcShort( 0xe ),	/* 14 */
/* 2154 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2156 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 2158 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2160 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2162 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2164 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 2166 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2168 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2170 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrFieldName */

/* 2172 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2174 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2176 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter fIncludeBaseClasses */

/* 2178 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2180 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2182 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pluFlid */

/* 2184 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2186 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2188 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2190 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2192 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2194 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDirectSubclasses */

/* 2196 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2198 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2202 */	NdrFcShort( 0x1e ),	/* 30 */
/* 2204 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2206 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2208 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2210 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 2212 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2214 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2216 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2218 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 2220 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2222 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2224 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cluMax */

/* 2226 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2228 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2230 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcluOut */

/* 2232 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2234 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2236 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluSubclasses */

/* 2238 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 2240 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2242 */	NdrFcShort( 0xf0 ),	/* Type Offset=240 */

	/* Return value */

/* 2244 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2246 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2248 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetAllSubclasses */

/* 2250 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2252 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2256 */	NdrFcShort( 0x1f ),	/* 31 */
/* 2258 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2260 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2262 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2264 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 2266 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2268 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2272 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 2274 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2276 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cluMax */

/* 2280 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2282 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2284 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcluOut */

/* 2286 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2288 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2290 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluSubclasses */

/* 2292 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 2294 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2296 */	NdrFcShort( 0xf0 ),	/* Type Offset=240 */

	/* Return value */

/* 2298 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2300 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2302 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddVirtualProp */

/* 2304 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2306 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2310 */	NdrFcShort( 0x20 ),	/* 32 */
/* 2312 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2314 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2316 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2318 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 2320 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2322 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2324 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2326 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrClass */

/* 2328 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2330 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2332 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter bstrField */

/* 2334 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2336 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2338 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter luFlid */

/* 2340 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2342 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2344 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter type */

/* 2346 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2348 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2350 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2352 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2354 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2356 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CopyDatabase */

/* 2358 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2360 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2364 */	NdrFcShort( 0x3 ),	/* 3 */
/* 2366 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2370 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2372 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 2374 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2376 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2378 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2380 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSrcPathName */

/* 2382 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2384 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2386 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter bstrDstPathName */

/* 2388 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2390 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2392 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Return value */

/* 2394 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2396 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2398 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AttachDatabase */

/* 2400 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2402 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2406 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2408 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2410 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2412 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2414 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 2416 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2418 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2420 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2422 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDatabaseName */

/* 2424 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2426 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2428 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter bstrPathName */

/* 2430 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2432 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2434 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Return value */

/* 2436 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2438 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2440 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DetachDatabase */

/* 2442 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2444 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2448 */	NdrFcShort( 0x5 ),	/* 5 */
/* 2450 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2452 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2454 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2456 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2458 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2460 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2462 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2464 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDatabaseName */

/* 2466 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2468 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2470 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Return value */

/* 2472 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2474 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2476 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RenameDatabase */

/* 2478 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2480 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2484 */	NdrFcShort( 0x6 ),	/* 6 */
/* 2486 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 2488 */	NdrFcShort( 0xc ),	/* 12 */
/* 2490 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2492 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 2494 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2496 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2498 */	NdrFcShort( 0x3 ),	/* 3 */
/* 2500 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDirName */

/* 2502 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2504 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2506 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter bstrOldName */

/* 2508 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2510 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2512 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter bstrNewName */

/* 2514 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2516 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2518 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Parameter fDetachBefore */

/* 2520 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2522 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2524 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fAttachAfter */

/* 2526 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2528 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2530 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 2532 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2534 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 2536 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_LogStream */

/* 2538 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2540 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2544 */	NdrFcShort( 0x7 ),	/* 7 */
/* 2546 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2548 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2550 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2552 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2554 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2556 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2558 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2560 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 2562 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2564 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2566 */	NdrFcShort( 0x50 ),	/* Type Offset=80 */

	/* Return value */

/* 2568 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2570 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2572 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FwRootDir */

/* 2574 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2576 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2580 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2582 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2584 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2586 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2588 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2590 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2592 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2594 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2596 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 2598 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 2600 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2602 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 2604 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2606 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2608 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FwMigrationScriptDir */

/* 2610 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2612 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2616 */	NdrFcShort( 0x9 ),	/* 9 */
/* 2618 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2620 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2622 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2624 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2626 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2628 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2630 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2632 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 2634 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 2636 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2638 */	NdrFcShort( 0xb0 ),	/* Type Offset=176 */

	/* Return value */

/* 2640 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2642 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2644 */	0x8,		/* FC_LONG */
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
			0x12, 0x0,	/* FC_UP */
/*  8 */	NdrFcShort( 0xe ),	/* Offset= 14 (22) */
/* 10 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 12 */	NdrFcShort( 0x2 ),	/* 2 */
/* 14 */	0x9,		/* Corr desc: FC_ULONG */
			0x0,		/*  */
/* 16 */	NdrFcShort( 0xfffc ),	/* -4 */
/* 18 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 20 */	0x6,		/* FC_SHORT */
			0x5b,		/* FC_END */
/* 22 */
			0x17,		/* FC_CSTRUCT */
			0x3,		/* 3 */
/* 24 */	NdrFcShort( 0x8 ),	/* 8 */
/* 26 */	NdrFcShort( 0xfff0 ),	/* Offset= -16 (10) */
/* 28 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 30 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 32 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 34 */	NdrFcShort( 0x0 ),	/* 0 */
/* 36 */	NdrFcShort( 0x4 ),	/* 4 */
/* 38 */	NdrFcShort( 0x0 ),	/* 0 */
/* 40 */	NdrFcShort( 0xffde ),	/* Offset= -34 (6) */
/* 42 */
			0x11, 0x0,	/* FC_RP */
/* 44 */	NdrFcShort( 0x2 ),	/* Offset= 2 (46) */
/* 46 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 48 */	NdrFcShort( 0x4 ),	/* 4 */
/* 50 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 52 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 54 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 56 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 58 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 60 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 62 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 64 */	NdrFcLong( 0x0 ),	/* 0 */
/* 68 */	NdrFcShort( 0x0 ),	/* 0 */
/* 70 */	NdrFcShort( 0x0 ),	/* 0 */
/* 72 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 74 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 76 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 78 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 80 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 82 */	NdrFcLong( 0xc ),	/* 12 */
/* 86 */	NdrFcShort( 0x0 ),	/* 0 */
/* 88 */	NdrFcShort( 0x0 ),	/* 0 */
/* 90 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 92 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 94 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 96 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 98 */
			0x11, 0x0,	/* FC_RP */
/* 100 */	NdrFcShort( 0x2 ),	/* Offset= 2 (102) */
/* 102 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 104 */	NdrFcShort( 0x4 ),	/* 4 */
/* 106 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 108 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 110 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 112 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 114 */
			0x11, 0x0,	/* FC_RP */
/* 116 */	NdrFcShort( 0x2 ),	/* Offset= 2 (118) */
/* 118 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 120 */	NdrFcShort( 0x1 ),	/* 1 */
/* 122 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 124 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 126 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 128 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 130 */
			0x11, 0x0,	/* FC_RP */
/* 132 */	NdrFcShort( 0x2 ),	/* Offset= 2 (134) */
/* 134 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 136 */	NdrFcShort( 0x2 ),	/* 2 */
/* 138 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 140 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 142 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 144 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 146 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 148 */	NdrFcShort( 0x2 ),	/* Offset= 2 (150) */
/* 150 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 152 */	NdrFcLong( 0x21993161 ),	/* 563687777 */
/* 156 */	NdrFcShort( 0x3e24 ),	/* 15908 */
/* 158 */	NdrFcShort( 0x11d4 ),	/* 4564 */
/* 160 */	0xa1,		/* 161 */
			0xbd,		/* 189 */
/* 162 */	0x0,		/* 0 */
			0xc0,		/* 192 */
/* 164 */	0x4f,		/* 79 */
			0xc,		/* 12 */
/* 166 */	0x95,		/* 149 */
			0x93,		/* 147 */
/* 168 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 170 */	NdrFcShort( 0x6 ),	/* Offset= 6 (176) */
/* 172 */
			0x13, 0x0,	/* FC_OP */
/* 174 */	NdrFcShort( 0xff68 ),	/* Offset= -152 (22) */
/* 176 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 178 */	NdrFcShort( 0x0 ),	/* 0 */
/* 180 */	NdrFcShort( 0x4 ),	/* 4 */
/* 182 */	NdrFcShort( 0x0 ),	/* 0 */
/* 184 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (172) */
/* 186 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 188 */	NdrFcLong( 0xcb7bea0f ),	/* -881071601 */
/* 192 */	NdrFcShort( 0x960a ),	/* -27126 */
/* 194 */	NdrFcShort( 0x4b23 ),	/* 19235 */
/* 196 */	0x80,		/* 128 */
			0xd3,		/* 211 */
/* 198 */	0xde,		/* 222 */
			0x6,		/* 6 */
/* 200 */	0xc0,		/* 192 */
			0x53,		/* 83 */
/* 202 */	0xe,		/* 14 */
			0x4,		/* 4 */
/* 204 */
			0x11, 0x0,	/* FC_RP */
/* 206 */	NdrFcShort( 0x2 ),	/* Offset= 2 (208) */
/* 208 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 210 */	NdrFcShort( 0x4 ),	/* 4 */
/* 212 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 214 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 216 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 218 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 220 */
			0x11, 0x0,	/* FC_RP */
/* 222 */	NdrFcShort( 0x2 ),	/* Offset= 2 (224) */
/* 224 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 226 */	NdrFcShort( 0x4 ),	/* 4 */
/* 228 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 230 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 232 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 234 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 236 */
			0x11, 0x0,	/* FC_RP */
/* 238 */	NdrFcShort( 0x2 ),	/* Offset= 2 (240) */
/* 240 */
			0x1c,		/* FC_CVARRAY */
			0x3,		/* 3 */
/* 242 */	NdrFcShort( 0x4 ),	/* 4 */
/* 244 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 246 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 248 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 250 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 252 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 254 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 256 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */

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



/* Standard interface: __MIDL_itf_DbAccessPs_0000, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IUnknown, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}} */


/* Object interface: IOleDbCommand, ver. 0.0,
   GUID={0x21993161,0x3E24,0x11d4,{0xA1,0xBD,0x00,0xC0,0x4F,0x0C,0x95,0x93}} */

#pragma code_seg(".orpc")
static const unsigned short IOleDbCommand_FormatStringOffsetTable[] =
	{
	0,
	36,
	78,
	144,
	186,
	240,
	276,
	318,
	354,
	420,
	480
	};

static const MIDL_STUBLESS_PROXY_INFO IOleDbCommand_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IOleDbCommand_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IOleDbCommand_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IOleDbCommand_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(14) _IOleDbCommandProxyVtbl =
{
	&IOleDbCommand_ProxyInfo,
	&IID_IOleDbCommand,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::ColValWasNull */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::ExecCommand */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::GetColValue */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::GetInt */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::GetParameter */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::GetRowset */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::Init */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::NextRow */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::SetParameter */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::SetByteBuffParameter */ ,
	(void *) (INT_PTR) -1 /* IOleDbCommand::SetStringParameter */
};

const CInterfaceStubVtbl _IOleDbCommandStubVtbl =
{
	&IID_IOleDbCommand,
	&IOleDbCommand_ServerInfo,
	14,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_DbAccessPs_0258, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IOleDbEncap, ver. 0.0,
   GUID={0xCB7BEA0F,0x960A,0x4b23,{0x80,0xD3,0xDE,0x06,0xC0,0x53,0x0E,0x04}} */

#pragma code_seg(".orpc")
static const unsigned short IOleDbEncap_FormatStringOffsetTable[] =
	{
	540,
	570,
	600,
	636,
	696,
	732,
	762,
	798,
	834,
	870,
	912,
	948,
	984
	};

static const MIDL_STUBLESS_PROXY_INFO IOleDbEncap_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IOleDbEncap_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IOleDbEncap_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IOleDbEncap_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(16) _IOleDbEncapProxyVtbl =
{
	&IOleDbEncap_ProxyInfo,
	&IID_IOleDbEncap,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::BeginTrans */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::CommitTrans */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::CreateCommand */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::Init */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::IsTransactionOpen */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::RollbackTrans */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::RollbackSavePoint */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::SetSavePoint */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::SetSavePointOrBeginTrans */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::InitMSDE */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::get_Server */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::get_Database */ ,
	(void *) (INT_PTR) -1 /* IOleDbEncap::GetFreeLogKb */
};

const CInterfaceStubVtbl _IOleDbEncapStubVtbl =
{
	&IID_IOleDbEncap,
	&IOleDbEncap_ServerInfo,
	16,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_DbAccessPs_0259, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IFwMetaDataCache, ver. 0.0,
   GUID={0x6AA9042E,0x0A4D,0x4f33,{0x88,0x1B,0x3F,0xBE,0x48,0x86,0x1D,0x14}} */

#pragma code_seg(".orpc")
static const unsigned short IFwMetaDataCache_FormatStringOffsetTable[] =
	{
	1026,
	1062,
	1104,
	1146,
	1182,
	1224,
	1266,
	1308,
	1350,
	1392,
	1434,
	1476,
	1518,
	1560,
	1602,
	1644,
	1686,
	1734,
	1770,
	1812,
	1854,
	1896,
	1938,
	1980,
	2046,
	2088,
	2142,
	2196,
	2250,
	2304
	};

static const MIDL_STUBLESS_PROXY_INFO IFwMetaDataCache_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IFwMetaDataCache_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IFwMetaDataCache_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IFwMetaDataCache_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(33) _IFwMetaDataCacheProxyVtbl =
{
	&IFwMetaDataCache_ProxyInfo,
	&IID_IFwMetaDataCache,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::Init */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::Reload */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::InitXml */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::get_FieldCount */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldIds */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetOwnClsName */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetDstClsName */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetOwnClsId */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetDstClsId */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldName */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldLabel */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldHelp */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldXml */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldListRoot */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldWs */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldType */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::get_IsValidClass */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::get_ClassCount */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetClassIds */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetClassName */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetAbstract */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetBaseClsId */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetBaseClsName */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFields */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetClassId */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldId */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetFieldId2 */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetDirectSubclasses */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::GetAllSubclasses */ ,
	(void *) (INT_PTR) -1 /* IFwMetaDataCache::AddVirtualProp */
};

const CInterfaceStubVtbl _IFwMetaDataCacheStubVtbl =
{
	&IID_IFwMetaDataCache,
	&IFwMetaDataCache_ServerInfo,
	33,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_DbAccessPs_0260, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDbAdmin, ver. 0.0,
   GUID={0x2A861F95,0x63D0,0x480d,{0xB5,0xAF,0x4F,0xAF,0x0D,0x22,0x12,0x5D}} */

#pragma code_seg(".orpc")
static const unsigned short IDbAdmin_FormatStringOffsetTable[] =
	{
	2358,
	2400,
	2442,
	2478,
	2538,
	2574,
	2610,
	798,
	834
	};

static const MIDL_STUBLESS_PROXY_INFO IDbAdmin_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IDbAdmin_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IDbAdmin_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IDbAdmin_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _IDbAdminProxyVtbl =
{
	&IDbAdmin_ProxyInfo,
	&IID_IDbAdmin,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IDbAdmin::CopyDatabase */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::AttachDatabase */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::DetachDatabase */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::RenameDatabase */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::putref_LogStream */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::get_FwRootDir */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::get_FwMigrationScriptDir */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::get_FwDatabaseDir */ ,
	(void *) (INT_PTR) -1 /* IDbAdmin::get_FwTemplateDir */
};

const CInterfaceStubVtbl _IDbAdminStubVtbl =
{
	&IID_IDbAdmin,
	&IDbAdmin_ServerInfo,
	12,
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

const CInterfaceProxyVtbl * _DbAccessPs_ProxyVtblList[] =
{
	( CInterfaceProxyVtbl *) &_IOleDbEncapProxyVtbl,
	( CInterfaceProxyVtbl *) &_IFwMetaDataCacheProxyVtbl,
	( CInterfaceProxyVtbl *) &_IOleDbCommandProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDbAdminProxyVtbl,
	0
};

const CInterfaceStubVtbl * _DbAccessPs_StubVtblList[] =
{
	( CInterfaceStubVtbl *) &_IOleDbEncapStubVtbl,
	( CInterfaceStubVtbl *) &_IFwMetaDataCacheStubVtbl,
	( CInterfaceStubVtbl *) &_IOleDbCommandStubVtbl,
	( CInterfaceStubVtbl *) &_IDbAdminStubVtbl,
	0
};

PCInterfaceName const _DbAccessPs_InterfaceNamesList[] =
{
	"IOleDbEncap",
	"IFwMetaDataCache",
	"IOleDbCommand",
	"IDbAdmin",
	0
};


#define _DbAccessPs_CHECK_IID(n)	IID_GENERIC_CHECK_IID( _DbAccessPs, pIID, n)

int __stdcall _DbAccessPs_IID_Lookup( const IID * pIID, int * pIndex )
{
	IID_BS_LOOKUP_SETUP

	IID_BS_LOOKUP_INITIAL_TEST( _DbAccessPs, 4, 2 )
	IID_BS_LOOKUP_NEXT_TEST( _DbAccessPs, 1 )
	IID_BS_LOOKUP_RETURN_RESULT( _DbAccessPs, 4, *pIndex )

}

const ExtendedProxyFileInfo DbAccessPs_ProxyFileInfo =
{
	(PCInterfaceProxyVtblList *) & _DbAccessPs_ProxyVtblList,
	(PCInterfaceStubVtblList *) & _DbAccessPs_StubVtblList,
	(const PCInterfaceName * ) & _DbAccessPs_InterfaceNamesList,
	0, // no delegation
	& _DbAccessPs_IID_Lookup,
	4,
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
