

/* this ALWAYS GENERATED file contains the proxy stub code */


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


#include "DbServicesPs.h"

#define TYPE_FORMAT_STRING_SIZE   127
#define PROC_FORMAT_STRING_SIZE   823
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


extern const MIDL_SERVER_INFO IBackupDelegates_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IBackupDelegates_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO DIFwBackupDb_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO DIFwBackupDb_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IDisconnectDb_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IDisconnectDb_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IRemoteDbWarn_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IRemoteDbWarn_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IDbWarnSetup_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IDbWarnSetup_ProxyInfo;


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

	/* Procedure GetLocalServer_Bkupd */

			0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x3 ),	/* 3 */
/*  8 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 18 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrSvrName */

/* 24 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 26 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 28 */	NdrFcShort( 0x20 ),	/* Type Offset=32 */

	/* Return value */

/* 30 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 32 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 34 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetLogPointer_Bkupd */

/* 36 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 38 */	NdrFcLong( 0x0 ),	/* 0 */
/* 42 */	NdrFcShort( 0x4 ),	/* 4 */
/* 44 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 46 */	NdrFcShort( 0x0 ),	/* 0 */
/* 48 */	NdrFcShort( 0x8 ),	/* 8 */
/* 50 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 52 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 54 */	NdrFcShort( 0x0 ),	/* 0 */
/* 56 */	NdrFcShort( 0x0 ),	/* 0 */
/* 58 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppfist */

/* 60 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 62 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 64 */	NdrFcShort( 0x2a ),	/* Type Offset=42 */

	/* Return value */

/* 66 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 68 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 70 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SaveAllData_Bkupd */

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
			0x1,		/* Ext Flags:  new corr desc, */
/* 90 */	NdrFcShort( 0x0 ),	/* 0 */
/* 92 */	NdrFcShort( 0x0 ),	/* 0 */
/* 94 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszServer */

/* 96 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 98 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 100 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Parameter pszDbName */

/* 102 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 104 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 106 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Return value */

/* 108 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 110 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseDbAndWindows_Bkupd */

/* 114 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 116 */	NdrFcLong( 0x0 ),	/* 0 */
/* 120 */	NdrFcShort( 0x6 ),	/* 6 */
/* 122 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 124 */	NdrFcShort( 0x6 ),	/* 6 */
/* 126 */	NdrFcShort( 0x22 ),	/* 34 */
/* 128 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 130 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 132 */	NdrFcShort( 0x0 ),	/* 0 */
/* 134 */	NdrFcShort( 0x0 ),	/* 0 */
/* 136 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszSvrName */

/* 138 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 140 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 142 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Parameter pszDbName */

/* 144 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 146 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 148 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Parameter fOkToClose */

/* 150 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 152 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 154 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfWindowsClosed */

/* 156 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 158 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 160 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 162 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 164 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 166 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IncExportedObjects_Bkupd */

/* 168 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 170 */	NdrFcLong( 0x0 ),	/* 0 */
/* 174 */	NdrFcShort( 0x7 ),	/* 7 */
/* 176 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 178 */	NdrFcShort( 0x0 ),	/* 0 */
/* 180 */	NdrFcShort( 0x8 ),	/* 8 */
/* 182 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 184 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 186 */	NdrFcShort( 0x0 ),	/* 0 */
/* 188 */	NdrFcShort( 0x0 ),	/* 0 */
/* 190 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 192 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 194 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CheckForMissedSchedules */


	/* Procedure DecExportedObjects_Bkupd */

/* 198 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 200 */	NdrFcLong( 0x0 ),	/* 0 */
/* 204 */	NdrFcShort( 0x8 ),	/* 8 */
/* 206 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 208 */	NdrFcShort( 0x0 ),	/* 0 */
/* 210 */	NdrFcShort( 0x8 ),	/* 8 */
/* 212 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 214 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 216 */	NdrFcShort( 0x0 ),	/* 0 */
/* 218 */	NdrFcShort( 0x0 ),	/* 0 */
/* 220 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 222 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 224 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 226 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CheckDbVerCompatibility_Bkupd */

/* 228 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 230 */	NdrFcLong( 0x0 ),	/* 0 */
/* 234 */	NdrFcShort( 0x9 ),	/* 9 */
/* 236 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 238 */	NdrFcShort( 0x0 ),	/* 0 */
/* 240 */	NdrFcShort( 0x22 ),	/* 34 */
/* 242 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 244 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 246 */	NdrFcShort( 0x0 ),	/* 0 */
/* 248 */	NdrFcShort( 0x0 ),	/* 0 */
/* 250 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszSvrName */

/* 252 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 254 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 256 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Parameter pszDbName */

/* 258 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 260 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 262 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Parameter pfCompatible */

/* 264 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 266 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 268 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 270 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 272 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 274 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReopenDbAndOneWindow_Bkupd */

/* 276 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 278 */	NdrFcLong( 0x0 ),	/* 0 */
/* 282 */	NdrFcShort( 0xa ),	/* 10 */
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

	/* Parameter pszSvrName */

/* 300 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 302 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 304 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Parameter pszDbName */

/* 306 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 308 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 310 */	NdrFcShort( 0x42 ),	/* Type Offset=66 */

	/* Return value */

/* 312 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 314 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 316 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 318 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 320 */	NdrFcLong( 0x0 ),	/* 0 */
/* 324 */	NdrFcShort( 0x7 ),	/* 7 */
/* 326 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 328 */	NdrFcShort( 0x8 ),	/* 8 */
/* 330 */	NdrFcShort( 0x8 ),	/* 8 */
/* 332 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 334 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 336 */	NdrFcShort( 0x0 ),	/* 0 */
/* 338 */	NdrFcShort( 0x0 ),	/* 0 */
/* 340 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbkupd */

/* 342 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 344 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 346 */	NdrFcShort( 0x48 ),	/* Type Offset=72 */

	/* Parameter hwndParent */

/* 348 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 350 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 354 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 356 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 358 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Backup */

/* 360 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 362 */	NdrFcLong( 0x0 ),	/* 0 */
/* 366 */	NdrFcShort( 0x9 ),	/* 9 */
/* 368 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 372 */	NdrFcShort( 0x8 ),	/* 8 */
/* 374 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 376 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 378 */	NdrFcShort( 0x0 ),	/* 0 */
/* 380 */	NdrFcShort( 0x0 ),	/* 0 */
/* 382 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 384 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 386 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 388 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Remind */

/* 390 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 392 */	NdrFcLong( 0x0 ),	/* 0 */
/* 396 */	NdrFcShort( 0xa ),	/* 10 */
/* 398 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 400 */	NdrFcShort( 0x0 ),	/* 0 */
/* 402 */	NdrFcShort( 0x8 ),	/* 8 */
/* 404 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 406 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 408 */	NdrFcShort( 0x0 ),	/* 0 */
/* 410 */	NdrFcShort( 0x0 ),	/* 0 */
/* 412 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 414 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 416 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure UserConfigure */

/* 420 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 422 */	NdrFcLong( 0x0 ),	/* 0 */
/* 426 */	NdrFcShort( 0xb ),	/* 11 */
/* 428 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 430 */	NdrFcShort( 0x6 ),	/* 6 */
/* 432 */	NdrFcShort( 0x24 ),	/* 36 */
/* 434 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 436 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 438 */	NdrFcShort( 0x0 ),	/* 0 */
/* 440 */	NdrFcShort( 0x0 ),	/* 0 */
/* 442 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phtprovHelpUrls */

/* 444 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 446 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 448 */	NdrFcShort( 0x5a ),	/* Type Offset=90 */

	/* Parameter fShowRestore */

/* 450 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 452 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 454 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pnUserAction */

/* 456 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 458 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 460 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 462 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 464 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 466 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 468 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 470 */	NdrFcLong( 0x0 ),	/* 0 */
/* 474 */	NdrFcShort( 0x3 ),	/* 3 */
/* 476 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 478 */	NdrFcShort( 0xe ),	/* 14 */
/* 480 */	NdrFcShort( 0x8 ),	/* 8 */
/* 482 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 484 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 486 */	NdrFcShort( 0x0 ),	/* 0 */
/* 488 */	NdrFcShort( 0x5 ),	/* 5 */
/* 490 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDatabase */

/* 492 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 494 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 496 */	NdrFcShort( 0x74 ),	/* Type Offset=116 */

	/* Parameter bstrServer */

/* 498 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 500 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 502 */	NdrFcShort( 0x74 ),	/* Type Offset=116 */

	/* Parameter bstrReason */

/* 504 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 506 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 508 */	NdrFcShort( 0x74 ),	/* Type Offset=116 */

	/* Parameter bstrExternalReason */

/* 510 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 512 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 514 */	NdrFcShort( 0x74 ),	/* Type Offset=116 */

	/* Parameter fConfirmCancel */

/* 516 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 518 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 520 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter bstrCancelQuestion */

/* 522 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 524 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 526 */	NdrFcShort( 0x74 ),	/* Type Offset=116 */

	/* Parameter hwndParent */

/* 528 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 530 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 532 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 534 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 536 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CheckConnections */

/* 540 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 542 */	NdrFcLong( 0x0 ),	/* 0 */
/* 546 */	NdrFcShort( 0x4 ),	/* 4 */
/* 548 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 550 */	NdrFcShort( 0x0 ),	/* 0 */
/* 552 */	NdrFcShort( 0x24 ),	/* 36 */
/* 554 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 556 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 558 */	NdrFcShort( 0x0 ),	/* 0 */
/* 560 */	NdrFcShort( 0x0 ),	/* 0 */
/* 562 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnResponse */

/* 564 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 566 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 568 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 570 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 572 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 574 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DisconnectAll */

/* 576 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 578 */	NdrFcLong( 0x0 ),	/* 0 */
/* 582 */	NdrFcShort( 0x5 ),	/* 5 */
/* 584 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 588 */	NdrFcShort( 0x22 ),	/* 34 */
/* 590 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 592 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 594 */	NdrFcShort( 0x0 ),	/* 0 */
/* 596 */	NdrFcShort( 0x0 ),	/* 0 */
/* 598 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfResult */

/* 600 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 602 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 604 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 606 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 608 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 610 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ForceDisconnectAll */

/* 612 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 614 */	NdrFcLong( 0x0 ),	/* 0 */
/* 618 */	NdrFcShort( 0x6 ),	/* 6 */
/* 620 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 622 */	NdrFcShort( 0x0 ),	/* 0 */
/* 624 */	NdrFcShort( 0x8 ),	/* 8 */
/* 626 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 628 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 630 */	NdrFcShort( 0x0 ),	/* 0 */
/* 632 */	NdrFcShort( 0x0 ),	/* 0 */
/* 634 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 636 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 638 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 640 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WarnSimple */

/* 642 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 644 */	NdrFcLong( 0x0 ),	/* 0 */
/* 648 */	NdrFcShort( 0x3 ),	/* 3 */
/* 650 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 652 */	NdrFcShort( 0x8 ),	/* 8 */
/* 654 */	NdrFcShort( 0x24 ),	/* 36 */
/* 656 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 658 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 660 */	NdrFcShort( 0x0 ),	/* 0 */
/* 662 */	NdrFcShort( 0x1 ),	/* 1 */
/* 664 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrMessage */

/* 666 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 668 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 670 */	NdrFcShort( 0x74 ),	/* Type Offset=116 */

	/* Parameter nFlags */

/* 672 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 674 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 676 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnResponse */

/* 678 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 680 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 682 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 684 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 686 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 688 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WarnWithTimeout */

/* 690 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 692 */	NdrFcLong( 0x0 ),	/* 0 */
/* 696 */	NdrFcShort( 0x4 ),	/* 4 */
/* 698 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 700 */	NdrFcShort( 0x8 ),	/* 8 */
/* 702 */	NdrFcShort( 0x8 ),	/* 8 */
/* 704 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 706 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 708 */	NdrFcShort( 0x0 ),	/* 0 */
/* 710 */	NdrFcShort( 0x1 ),	/* 1 */
/* 712 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrMessage */

/* 714 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 716 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 718 */	NdrFcShort( 0x74 ),	/* Type Offset=116 */

	/* Parameter nTimeLeft */

/* 720 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 722 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 724 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 726 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 728 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 730 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Cancel */

/* 732 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 734 */	NdrFcLong( 0x0 ),	/* 0 */
/* 738 */	NdrFcShort( 0x5 ),	/* 5 */
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

	/* Procedure PermitRemoteWarnings */

/* 762 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 764 */	NdrFcLong( 0x0 ),	/* 0 */
/* 768 */	NdrFcShort( 0x3 ),	/* 3 */
/* 770 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 772 */	NdrFcShort( 0x0 ),	/* 0 */
/* 774 */	NdrFcShort( 0x8 ),	/* 8 */
/* 776 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 778 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 780 */	NdrFcShort( 0x0 ),	/* 0 */
/* 782 */	NdrFcShort( 0x0 ),	/* 0 */
/* 784 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 786 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 788 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 790 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RefuseRemoteWarnings */

/* 792 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 794 */	NdrFcLong( 0x0 ),	/* 0 */
/* 798 */	NdrFcShort( 0x4 ),	/* 4 */
/* 800 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 802 */	NdrFcShort( 0x0 ),	/* 0 */
/* 804 */	NdrFcShort( 0x8 ),	/* 8 */
/* 806 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 808 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 810 */	NdrFcShort( 0x0 ),	/* 0 */
/* 812 */	NdrFcShort( 0x0 ),	/* 0 */
/* 814 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 816 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 818 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 820 */	0x8,		/* FC_LONG */
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
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/*  4 */	NdrFcShort( 0x1c ),	/* Offset= 28 (32) */
/*  6 */
			0x13, 0x0,	/* FC_OP */
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
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 44 */	NdrFcShort( 0x2 ),	/* Offset= 2 (46) */
/* 46 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 48 */	NdrFcLong( 0xc ),	/* 12 */
/* 52 */	NdrFcShort( 0x0 ),	/* 0 */
/* 54 */	NdrFcShort( 0x0 ),	/* 0 */
/* 56 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 58 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 60 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 62 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 64 */
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/* 66 */
			0x25,		/* FC_C_WSTRING */
			0x5c,		/* FC_PAD */
/* 68 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 70 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 72 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 74 */	NdrFcLong( 0x1c0fa5af ),	/* 470787503 */
/* 78 */	NdrFcShort( 0xb4 ),	/* 180 */
/* 80 */	NdrFcShort( 0x4dc1 ),	/* 19905 */
/* 82 */	0x8f,		/* 143 */
			0x9e,		/* 158 */
/* 84 */	0x16,		/* 22 */
			0x8a,		/* 138 */
/* 86 */	0xf3,		/* 243 */
			0xf8,		/* 248 */
/* 88 */	0x92,		/* 146 */
			0xb0,		/* 176 */
/* 90 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 92 */	NdrFcLong( 0x0 ),	/* 0 */
/* 96 */	NdrFcShort( 0x0 ),	/* 0 */
/* 98 */	NdrFcShort( 0x0 ),	/* 0 */
/* 100 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 102 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 104 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 106 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 108 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 110 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 112 */
			0x12, 0x0,	/* FC_UP */
/* 114 */	NdrFcShort( 0xffa4 ),	/* Offset= -92 (22) */
/* 116 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 120 */	NdrFcShort( 0x4 ),	/* 4 */
/* 122 */	NdrFcShort( 0x0 ),	/* 0 */
/* 124 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (112) */

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



/* Standard interface: __MIDL_itf_DbServicesPs_0000, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IUnknown, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}} */


/* Object interface: IBackupDelegates, ver. 0.0,
   GUID={0x1C0FA5AF,0x00B4,0x4dc1,{0x8F,0x9E,0x16,0x8A,0xF3,0xF8,0x92,0xB0}} */

#pragma code_seg(".orpc")
static const unsigned short IBackupDelegates_FormatStringOffsetTable[] =
	{
	0,
	36,
	72,
	114,
	168,
	198,
	228,
	276
	};

static const MIDL_STUBLESS_PROXY_INFO IBackupDelegates_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IBackupDelegates_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IBackupDelegates_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IBackupDelegates_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(11) _IBackupDelegatesProxyVtbl =
{
	&IBackupDelegates_ProxyInfo,
	&IID_IBackupDelegates,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::GetLocalServer_Bkupd */ ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::GetLogPointer_Bkupd */ ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::SaveAllData_Bkupd */ ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::CloseDbAndWindows_Bkupd */ ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::IncExportedObjects_Bkupd */ ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::DecExportedObjects_Bkupd */ ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::CheckDbVerCompatibility_Bkupd */ ,
	(void *) (INT_PTR) -1 /* IBackupDelegates::ReopenDbAndOneWindow_Bkupd */
};

const CInterfaceStubVtbl _IBackupDelegatesStubVtbl =
{
	&IID_IBackupDelegates,
	&IBackupDelegates_ServerInfo,
	11,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_DbServicesPs_0257, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDispatch, ver. 0.0,
   GUID={0x00020400,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}} */


/* Object interface: DIFwBackupDb, ver. 0.0,
   GUID={0x00A94783,0x8F5F,0x42af,{0xA9,0x93,0x49,0xF2,0x15,0x4A,0x67,0xE2}} */

#pragma code_seg(".orpc")
static const unsigned short DIFwBackupDb_FormatStringOffsetTable[] =
	{
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	318,
	198,
	360,
	390,
	420
	};

static const MIDL_STUBLESS_PROXY_INFO DIFwBackupDb_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&DIFwBackupDb_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO DIFwBackupDb_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&DIFwBackupDb_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _DIFwBackupDbProxyVtbl =
{
	&DIFwBackupDb_ProxyInfo,
	&IID_DIFwBackupDb,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	0 /* (void *) (INT_PTR) -1 /* IDispatch::GetTypeInfoCount */ ,
	0 /* (void *) (INT_PTR) -1 /* IDispatch::GetTypeInfo */ ,
	0 /* (void *) (INT_PTR) -1 /* IDispatch::GetIDsOfNames */ ,
	0 /* IDispatch_Invoke_Proxy */ ,
	(void *) (INT_PTR) -1 /* DIFwBackupDb::Init */ ,
	(void *) (INT_PTR) -1 /* DIFwBackupDb::CheckForMissedSchedules */ ,
	(void *) (INT_PTR) -1 /* DIFwBackupDb::Backup */ ,
	(void *) (INT_PTR) -1 /* DIFwBackupDb::Remind */ ,
	(void *) (INT_PTR) -1 /* DIFwBackupDb::UserConfigure */
};


static const PRPC_STUB_FUNCTION DIFwBackupDb_table[] =
{
	STUB_FORWARDING_FUNCTION,
	STUB_FORWARDING_FUNCTION,
	STUB_FORWARDING_FUNCTION,
	STUB_FORWARDING_FUNCTION,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2,
	NdrStubCall2
};

CInterfaceStubVtbl _DIFwBackupDbStubVtbl =
{
	&IID_DIFwBackupDb,
	&DIFwBackupDb_ServerInfo,
	12,
	&DIFwBackupDb_table[-3],
	CStdStubBuffer_DELEGATING_METHODS
};


/* Standard interface: __MIDL_itf_DbServicesPs_0259, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDisconnectDb, ver. 0.0,
   GUID={0x0CC74E0C,0x3017,0x4c02,{0xA5,0x07,0x3F,0xB8,0xCE,0x62,0x1C,0xDC}} */

#pragma code_seg(".orpc")
static const unsigned short IDisconnectDb_FormatStringOffsetTable[] =
	{
	468,
	540,
	576,
	612
	};

static const MIDL_STUBLESS_PROXY_INFO IDisconnectDb_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IDisconnectDb_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IDisconnectDb_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IDisconnectDb_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IDisconnectDbProxyVtbl =
{
	&IDisconnectDb_ProxyInfo,
	&IID_IDisconnectDb,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IDisconnectDb::Init */ ,
	(void *) (INT_PTR) -1 /* IDisconnectDb::CheckConnections */ ,
	(void *) (INT_PTR) -1 /* IDisconnectDb::DisconnectAll */ ,
	(void *) (INT_PTR) -1 /* IDisconnectDb::ForceDisconnectAll */
};

const CInterfaceStubVtbl _IDisconnectDbStubVtbl =
{
	&IID_IDisconnectDb,
	&IDisconnectDb_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_DbServicesPs_0261, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IRemoteDbWarn, ver. 0.0,
   GUID={0x004C42AE,0xCB07,0x47b5,{0xA9,0x36,0xD9,0xCA,0x4A,0xC4,0x66,0xD7}} */

#pragma code_seg(".orpc")
static const unsigned short IRemoteDbWarn_FormatStringOffsetTable[] =
	{
	642,
	690,
	732
	};

static const MIDL_STUBLESS_PROXY_INFO IRemoteDbWarn_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IRemoteDbWarn_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IRemoteDbWarn_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IRemoteDbWarn_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(6) _IRemoteDbWarnProxyVtbl =
{
	&IRemoteDbWarn_ProxyInfo,
	&IID_IRemoteDbWarn,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IRemoteDbWarn::WarnSimple */ ,
	(void *) (INT_PTR) -1 /* IRemoteDbWarn::WarnWithTimeout */ ,
	(void *) (INT_PTR) -1 /* IRemoteDbWarn::Cancel */
};

const CInterfaceStubVtbl _IRemoteDbWarnStubVtbl =
{
	&IID_IRemoteDbWarn,
	&IRemoteDbWarn_ServerInfo,
	6,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_DbServicesPs_0263, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDbWarnSetup, ver. 0.0,
   GUID={0x06082023,0xC2BA,0x4425,{0x90,0xFD,0x2F,0x76,0xB7,0x4C,0xCB,0xE7}} */

#pragma code_seg(".orpc")
static const unsigned short IDbWarnSetup_FormatStringOffsetTable[] =
	{
	762,
	792
	};

static const MIDL_STUBLESS_PROXY_INFO IDbWarnSetup_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IDbWarnSetup_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IDbWarnSetup_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IDbWarnSetup_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _IDbWarnSetupProxyVtbl =
{
	&IDbWarnSetup_ProxyInfo,
	&IID_IDbWarnSetup,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IDbWarnSetup::PermitRemoteWarnings */ ,
	(void *) (INT_PTR) -1 /* IDbWarnSetup::RefuseRemoteWarnings */
};

const CInterfaceStubVtbl _IDbWarnSetupStubVtbl =
{
	&IID_IDbWarnSetup,
	&IDbWarnSetup_ServerInfo,
	5,
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

const CInterfaceProxyVtbl * _DbServicesPs_ProxyVtblList[] =
{
	( CInterfaceProxyVtbl *) &_IDisconnectDbProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDbWarnSetupProxyVtbl,
	( CInterfaceProxyVtbl *) &_DIFwBackupDbProxyVtbl,
	( CInterfaceProxyVtbl *) &_IRemoteDbWarnProxyVtbl,
	( CInterfaceProxyVtbl *) &_IBackupDelegatesProxyVtbl,
	0
};

const CInterfaceStubVtbl * _DbServicesPs_StubVtblList[] =
{
	( CInterfaceStubVtbl *) &_IDisconnectDbStubVtbl,
	( CInterfaceStubVtbl *) &_IDbWarnSetupStubVtbl,
	( CInterfaceStubVtbl *) &_DIFwBackupDbStubVtbl,
	( CInterfaceStubVtbl *) &_IRemoteDbWarnStubVtbl,
	( CInterfaceStubVtbl *) &_IBackupDelegatesStubVtbl,
	0
};

PCInterfaceName const _DbServicesPs_InterfaceNamesList[] =
{
	"IDisconnectDb",
	"IDbWarnSetup",
	"DIFwBackupDb",
	"IRemoteDbWarn",
	"IBackupDelegates",
	0
};

const IID *  _DbServicesPs_BaseIIDList[] =
{
	0,
	0,
	&IID_IDispatch,
	0,
	0,
	0
};


#define _DbServicesPs_CHECK_IID(n)	IID_GENERIC_CHECK_IID( _DbServicesPs, pIID, n)

int __stdcall _DbServicesPs_IID_Lookup( const IID * pIID, int * pIndex )
{
	IID_BS_LOOKUP_SETUP

	IID_BS_LOOKUP_INITIAL_TEST( _DbServicesPs, 5, 4 )
	IID_BS_LOOKUP_NEXT_TEST( _DbServicesPs, 2 )
	IID_BS_LOOKUP_NEXT_TEST( _DbServicesPs, 1 )
	IID_BS_LOOKUP_RETURN_RESULT( _DbServicesPs, 5, *pIndex )

}

const ExtendedProxyFileInfo DbServicesPs_ProxyFileInfo =
{
	(PCInterfaceProxyVtblList *) & _DbServicesPs_ProxyVtblList,
	(PCInterfaceStubVtblList *) & _DbServicesPs_StubVtblList,
	(const PCInterfaceName * ) & _DbServicesPs_InterfaceNamesList,
	(const IID ** ) & _DbServicesPs_BaseIIDList,
	& _DbServicesPs_IID_Lookup,
	5,
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
