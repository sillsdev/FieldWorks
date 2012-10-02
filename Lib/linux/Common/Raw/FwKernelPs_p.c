

/* this ALWAYS GENERATED file contains the proxy stub code */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:01:32 2006
 */
/* Compiler settings for C:\fw\Output\Common\FwKernelPs.idl:
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


#include "FwKernelPs.h"

#define TYPE_FORMAT_STRING_SIZE   2699
#define PROC_FORMAT_STRING_SIZE   22153
#define TRANSMIT_AS_TABLE_SIZE    0
#define WIRE_MARSHAL_TABLE_SIZE   2

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


extern const MIDL_SERVER_INFO IFwCustomExport_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IFwCustomExport_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IFwTool_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IFwTool_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IUndoAction_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IUndoAction_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IActionHandler_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IActionHandler_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IAdvInd_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IAdvInd_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IAdvInd2_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IAdvInd2_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IAdvInd3_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IAdvInd3_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IDebugReportSink_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IDebugReportSink_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IDebugReport_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IDebugReport_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IHelpTopicProvider_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IHelpTopicProvider_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IFwFldSpec_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IFwFldSpec_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IUndoGrouper_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IUndoGrouper_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsString_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsString_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsTextProps_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsTextProps_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsStrFactory_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsStrFactory_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsPropsFactory_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsPropsFactory_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsStrBldr_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsStrBldr_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsIncStrBldr_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsIncStrBldr_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsPropsBldr_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsPropsBldr_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsMultiString_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsMultiString_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ITsStreamWrapper_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ITsStreamWrapper_ProxyInfo;


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


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ISimpleInit_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ISimpleInit_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwGraphics_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwGraphics_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwGraphicsWin32_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwGraphicsWin32_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwTextSource_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwTextSource_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IVwJustifier_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IVwJustifier_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgSegment_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgSegment_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IRenderEngine_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IRenderEngine_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IRenderingFeatures_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IRenderingFeatures_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO IJustifyingRenderer_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO IJustifyingRenderer_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgCollation_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgCollation_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgWritingSystem_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgWritingSystem_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgInputMethodEditor_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgInputMethodEditor_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgFontManager_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgFontManager_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgCollatingEngine_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgCollatingEngine_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgCharacterPropertyEngine_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgCharacterPropertyEngine_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgSearchEngine_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgSearchEngine_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgStringConverter_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgStringConverter_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgTokenizer_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgTokenizer_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgSpellChecker_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgSpellChecker_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgSpellCheckFactory_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgSpellCheckFactory_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgNumericEngine_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgNumericEngine_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgWritingSystemFactory_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgWritingSystemFactory_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgWritingSystemFactoryBuilder_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgWritingSystemFactoryBuilder_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgTsStringPlusWss_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgTsStringPlusWss_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgTsDataObject_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgTsDataObject_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgKeymanHandler_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgKeymanHandler_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgTextServices_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgTextServices_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgCodePageEnumerator_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgCodePageEnumerator_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgLanguageEnumerator_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgLanguageEnumerator_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgIcuConverterEnumerator_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgIcuConverterEnumerator_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgIcuTransliteratorEnumerator_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgIcuTransliteratorEnumerator_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgIcuLocaleEnumerator_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgIcuLocaleEnumerator_ProxyInfo;


extern const MIDL_STUB_DESC Object_StubDesc;


extern const MIDL_SERVER_INFO ILgIcuResourceBundle_ServerInfo;
extern const MIDL_STUBLESS_PROXY_INFO ILgIcuResourceBundle_ProxyInfo;


extern const EXPR_EVAL ExprEvalRoutines[];
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

	/* Procedure Init */


	/* Procedure CopyDatabase */


	/* Procedure BeginUndoTask */


	/* Procedure SetLabelStyles */

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

	/* Parameter bstrPath */


	/* Parameter bstrSrcPathName */


	/* Parameter bstrUndo */


	/* Parameter bstrLabel */

/* 24 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 26 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 28 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter locale */


	/* Parameter bstrDstPathName */


	/* Parameter bstrRedo */


	/* Parameter bstrSubLabel */

/* 30 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 32 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 34 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 36 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 38 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 40 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Name */


	/* Procedure AddFlidCharStyleMapping */

/* 42 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 44 */	NdrFcLong( 0x0 ),	/* 0 */
/* 48 */	NdrFcShort( 0x4 ),	/* 4 */
/* 50 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 52 */	NdrFcShort( 0x8 ),	/* 8 */
/* 54 */	NdrFcShort( 0x8 ),	/* 8 */
/* 56 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 58 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 60 */	NdrFcShort( 0x0 ),	/* 0 */
/* 62 */	NdrFcShort( 0x1 ),	/* 1 */
/* 64 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */


	/* Parameter flid */

/* 66 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 68 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 70 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */


	/* Parameter bstrStyle */

/* 72 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 74 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 76 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */


	/* Return value */

/* 78 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 80 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 82 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BuildSubItemsString */

/* 84 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 86 */	NdrFcLong( 0x0 ),	/* 0 */
/* 90 */	NdrFcShort( 0x5 ),	/* 5 */
/* 92 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 94 */	NdrFcShort( 0x10 ),	/* 16 */
/* 96 */	NdrFcShort( 0x8 ),	/* 8 */
/* 98 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 100 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 102 */	NdrFcShort( 0x0 ),	/* 0 */
/* 104 */	NdrFcShort( 0x0 ),	/* 0 */
/* 106 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pffsp */

/* 108 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 110 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 112 */	NdrFcShort( 0x26 ),	/* Type Offset=38 */

	/* Parameter hvoRec */

/* 114 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 116 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 118 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 120 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 122 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 126 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 128 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 130 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 132 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 134 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 136 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BuildObjRefSeqString */

/* 138 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 140 */	NdrFcLong( 0x0 ),	/* 0 */
/* 144 */	NdrFcShort( 0x6 ),	/* 6 */
/* 146 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 148 */	NdrFcShort( 0x10 ),	/* 16 */
/* 150 */	NdrFcShort( 0x8 ),	/* 8 */
/* 152 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 154 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 156 */	NdrFcShort( 0x0 ),	/* 0 */
/* 158 */	NdrFcShort( 0x0 ),	/* 0 */
/* 160 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pffsp */

/* 162 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 164 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 166 */	NdrFcShort( 0x26 ),	/* Type Offset=38 */

	/* Parameter hvoRec */

/* 168 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 170 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 174 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 176 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 178 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 180 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 182 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 184 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 186 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 188 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BuildObjRefAtomicString */

/* 192 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 194 */	NdrFcLong( 0x0 ),	/* 0 */
/* 198 */	NdrFcShort( 0x7 ),	/* 7 */
/* 200 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 202 */	NdrFcShort( 0x10 ),	/* 16 */
/* 204 */	NdrFcShort( 0x8 ),	/* 8 */
/* 206 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 208 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 210 */	NdrFcShort( 0x0 ),	/* 0 */
/* 212 */	NdrFcShort( 0x0 ),	/* 0 */
/* 214 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pffsp */

/* 216 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 218 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 220 */	NdrFcShort( 0x26 ),	/* Type Offset=38 */

	/* Parameter hvoRec */

/* 222 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 224 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 226 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 228 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 230 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 232 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 234 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 236 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 238 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 240 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 242 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 244 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BuildExpandableString */

/* 246 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 248 */	NdrFcLong( 0x0 ),	/* 0 */
/* 252 */	NdrFcShort( 0x8 ),	/* 8 */
/* 254 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 256 */	NdrFcShort( 0x10 ),	/* 16 */
/* 258 */	NdrFcShort( 0x8 ),	/* 8 */
/* 260 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 262 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 264 */	NdrFcShort( 0x0 ),	/* 0 */
/* 266 */	NdrFcShort( 0x0 ),	/* 0 */
/* 268 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pffsp */

/* 270 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 272 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 274 */	NdrFcShort( 0x26 ),	/* Type Offset=38 */

	/* Parameter hvoRec */

/* 276 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 278 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 280 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 282 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 284 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 286 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 288 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 290 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 292 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 294 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 296 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 298 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetEnumString */

/* 300 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 302 */	NdrFcLong( 0x0 ),	/* 0 */
/* 306 */	NdrFcShort( 0x9 ),	/* 9 */
/* 308 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 310 */	NdrFcShort( 0x10 ),	/* 16 */
/* 312 */	NdrFcShort( 0x8 ),	/* 8 */
/* 314 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 316 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 318 */	NdrFcShort( 0x1 ),	/* 1 */
/* 320 */	NdrFcShort( 0x0 ),	/* 0 */
/* 322 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter flid */

/* 324 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 326 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 328 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter itss */

/* 330 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 332 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 334 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrName */

/* 336 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 338 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 340 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 342 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 344 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 346 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetActualLevel */

/* 348 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 350 */	NdrFcLong( 0x0 ),	/* 0 */
/* 354 */	NdrFcShort( 0xa ),	/* 10 */
/* 356 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 358 */	NdrFcShort( 0x18 ),	/* 24 */
/* 360 */	NdrFcShort( 0x24 ),	/* 36 */
/* 362 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 364 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 366 */	NdrFcShort( 0x0 ),	/* 0 */
/* 368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 370 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nLevel */

/* 372 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 374 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 376 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoRec */

/* 378 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 380 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 382 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 384 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 386 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 388 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnActualLevel */

/* 390 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 392 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 394 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 396 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 398 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 400 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BuildRecordTags */

/* 402 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 404 */	NdrFcLong( 0x0 ),	/* 0 */
/* 408 */	NdrFcShort( 0xb ),	/* 11 */
/* 410 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 412 */	NdrFcShort( 0x18 ),	/* 24 */
/* 414 */	NdrFcShort( 0x8 ),	/* 8 */
/* 416 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 418 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 420 */	NdrFcShort( 0x2 ),	/* 2 */
/* 422 */	NdrFcShort( 0x0 ),	/* 0 */
/* 424 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nLevel */

/* 426 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 428 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 430 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvo */

/* 432 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 434 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 436 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter clid */

/* 438 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 440 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 442 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrStartTag */

/* 444 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 446 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 448 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pbstrEndTag */

/* 450 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 452 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 454 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 456 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 458 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 460 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetPageSetupInfo */

/* 462 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 464 */	NdrFcLong( 0x0 ),	/* 0 */
/* 468 */	NdrFcShort( 0xc ),	/* 12 */
/* 470 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 472 */	NdrFcShort( 0x0 ),	/* 0 */
/* 474 */	NdrFcShort( 0x120 ),	/* 288 */
/* 476 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0xd,		/* 13 */
/* 478 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 480 */	NdrFcShort( 0x0 ),	/* 0 */
/* 482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 484 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnOrientation */

/* 486 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 488 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 490 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnPaperSize */

/* 492 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 494 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdxmpLeftMargin */

/* 498 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 500 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdxmpRightMargin */

/* 504 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 506 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdympTopMargin */

/* 510 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 512 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 514 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdympBottomMargin */

/* 516 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 518 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 520 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdympHeaderMargin */

/* 522 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 524 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 526 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdympFooterMargin */

/* 528 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 530 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 532 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdxmpPageWidth */

/* 534 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 536 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdympPageHeight */

/* 540 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 542 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 544 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptssHeader */

/* 546 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 548 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 550 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Parameter pptssFooter */

/* 552 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 554 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 556 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 558 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 560 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 562 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PostProcessFile */

/* 564 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 566 */	NdrFcLong( 0x0 ),	/* 0 */
/* 570 */	NdrFcShort( 0xd ),	/* 13 */
/* 572 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 574 */	NdrFcShort( 0x0 ),	/* 0 */
/* 576 */	NdrFcShort( 0x8 ),	/* 8 */
/* 578 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 580 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 582 */	NdrFcShort( 0x1 ),	/* 1 */
/* 584 */	NdrFcShort( 0x1 ),	/* 1 */
/* 586 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrInputFile */

/* 588 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 590 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 592 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstrOutputFile */

/* 594 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 596 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 598 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 600 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 602 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 604 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CanUndo */


	/* Procedure IncludeObjectData */

/* 606 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 608 */	NdrFcLong( 0x0 ),	/* 0 */
/* 612 */	NdrFcShort( 0xe ),	/* 14 */
/* 614 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 616 */	NdrFcShort( 0x0 ),	/* 0 */
/* 618 */	NdrFcShort( 0x22 ),	/* 34 */
/* 620 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 622 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 624 */	NdrFcShort( 0x0 ),	/* 0 */
/* 626 */	NdrFcShort( 0x0 ),	/* 0 */
/* 628 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfCanUndo */


	/* Parameter pbWriteObjData */

/* 630 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 632 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 634 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 636 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 638 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 640 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NewMainWnd */

/* 642 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 644 */	NdrFcLong( 0x0 ),	/* 0 */
/* 648 */	NdrFcShort( 0x3 ),	/* 3 */
/* 650 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 652 */	NdrFcShort( 0x28 ),	/* 40 */
/* 654 */	NdrFcShort( 0x40 ),	/* 64 */
/* 656 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xa,		/* 10 */
/* 658 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 660 */	NdrFcShort( 0x0 ),	/* 0 */
/* 662 */	NdrFcShort( 0x2 ),	/* 2 */
/* 664 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrServerName */

/* 666 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 668 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 670 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrDbName */

/* 672 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 674 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 676 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter hvoLangProj */

/* 678 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 680 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 682 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoMainObj */

/* 684 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 686 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 688 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter encUi */

/* 690 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 692 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nTool */

/* 696 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 698 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nParam */

/* 702 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 704 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 706 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppidNew */

/* 708 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 710 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 712 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phtool */

/* 714 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 716 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 718 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 720 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 722 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 724 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NewMainWndWithSel */

/* 726 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 728 */	NdrFcLong( 0x0 ),	/* 0 */
/* 732 */	NdrFcShort( 0x4 ),	/* 4 */
/* 734 */	NdrFcShort( 0x44 ),	/* x86 Stack size/offset = 68 */
/* 736 */	NdrFcShort( 0x48 ),	/* 72 */
/* 738 */	NdrFcShort( 0x40 ),	/* 64 */
/* 740 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x10,		/* 16 */
/* 742 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 744 */	NdrFcShort( 0x0 ),	/* 0 */
/* 746 */	NdrFcShort( 0x4 ),	/* 4 */
/* 748 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrServerName */

/* 750 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 752 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 754 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrDbName */

/* 756 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 758 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 760 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter hvoLangProj */

/* 762 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 764 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 766 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter hvoMainObj */

/* 768 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 770 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 772 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter encUi */

/* 774 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 776 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 778 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nTool */

/* 780 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 782 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 784 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nParam */

/* 786 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 788 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 790 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prghvo */

/* 792 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 794 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 796 */	NdrFcShort( 0x6c ),	/* Type Offset=108 */

	/* Parameter chvo */

/* 798 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 800 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 802 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgflid */

/* 804 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 806 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 808 */	NdrFcShort( 0x7c ),	/* Type Offset=124 */

	/* Parameter cflid */

/* 810 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 812 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 814 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichCur */

/* 816 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 818 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 820 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nView */

/* 822 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 824 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppidNew */

/* 828 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 830 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 832 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter phtool */

/* 834 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 836 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 838 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 840 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 842 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 844 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsLetter */


	/* Procedure CloseMainWnd */

/* 846 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 848 */	NdrFcLong( 0x0 ),	/* 0 */
/* 852 */	NdrFcShort( 0x5 ),	/* 5 */
/* 854 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 856 */	NdrFcShort( 0x8 ),	/* 8 */
/* 858 */	NdrFcShort( 0x22 ),	/* 34 */
/* 860 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 862 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 864 */	NdrFcShort( 0x0 ),	/* 0 */
/* 866 */	NdrFcShort( 0x0 ),	/* 0 */
/* 868 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */


	/* Parameter htool */

/* 870 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 872 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 874 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */


	/* Parameter pfCancelled */

/* 876 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 878 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 880 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 882 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 884 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 886 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseDbAndWindows */

/* 888 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 890 */	NdrFcLong( 0x0 ),	/* 0 */
/* 894 */	NdrFcShort( 0x6 ),	/* 6 */
/* 896 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 898 */	NdrFcShort( 0x6 ),	/* 6 */
/* 900 */	NdrFcShort( 0x8 ),	/* 8 */
/* 902 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 904 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 906 */	NdrFcShort( 0x0 ),	/* 0 */
/* 908 */	NdrFcShort( 0x2 ),	/* 2 */
/* 910 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSvrName */

/* 912 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 914 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 916 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrDbName */

/* 918 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 920 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 922 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter fOkToClose */

/* 924 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 926 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 928 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 930 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 932 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Undo */

/* 936 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 938 */	NdrFcLong( 0x0 ),	/* 0 */
/* 942 */	NdrFcShort( 0x3 ),	/* 3 */
/* 944 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 948 */	NdrFcShort( 0x22 ),	/* 34 */
/* 950 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 952 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 954 */	NdrFcShort( 0x0 ),	/* 0 */
/* 956 */	NdrFcShort( 0x0 ),	/* 0 */
/* 958 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfSuccess */

/* 960 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 962 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 964 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 966 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 968 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Redo */

/* 972 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 974 */	NdrFcLong( 0x0 ),	/* 0 */
/* 978 */	NdrFcShort( 0x4 ),	/* 4 */
/* 980 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 982 */	NdrFcShort( 0x0 ),	/* 0 */
/* 984 */	NdrFcShort( 0x22 ),	/* 34 */
/* 986 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 988 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 990 */	NdrFcShort( 0x0 ),	/* 0 */
/* 992 */	NdrFcShort( 0x0 ),	/* 0 */
/* 994 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfSuccess */

/* 996 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 998 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1000 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 1002 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1004 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1006 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Cancel */


	/* Procedure ContinueUndoTask */


	/* Procedure Commit */

/* 1008 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1010 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1014 */	NdrFcShort( 0x5 ),	/* 5 */
/* 1016 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1018 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1020 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1022 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1024 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1028 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1030 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 1032 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1034 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1036 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_HideLabel */


	/* Procedure IsDataChange */

/* 1038 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1040 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1044 */	NdrFcShort( 0x6 ),	/* 6 */
/* 1046 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1048 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1050 */	NdrFcShort( 0x22 ),	/* 34 */
/* 1052 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1054 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1056 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1058 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1060 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfHide */


	/* Parameter pfRet */

/* 1062 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1064 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1066 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 1068 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1070 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1072 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FontIsValid */


	/* Procedure CommitTrans */


	/* Procedure RefuseRemoteWarnings */


	/* Procedure NextStage */


	/* Procedure EndUndoTask */

/* 1074 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1076 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1080 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1082 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1084 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1086 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1088 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1090 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1092 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1094 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1096 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 1098 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1100 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1102 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ShutdownAllFactories */


	/* Procedure RefreshFontList */


	/* Procedure ForceDisconnectAll */


	/* Procedure EndOuterUndoTask */

/* 1104 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1106 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1110 */	NdrFcShort( 0x6 ),	/* 6 */
/* 1112 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1114 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1116 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1118 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1120 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1122 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1124 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1126 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 1128 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1130 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1132 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BreakUndoTask */

/* 1134 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1136 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1140 */	NdrFcShort( 0x7 ),	/* 7 */
/* 1142 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1144 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1146 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1148 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 1150 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1152 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1154 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1156 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrUndo */

/* 1158 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1160 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1162 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrRedo */

/* 1164 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1166 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1168 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 1170 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1172 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1174 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure StartSeq */

/* 1176 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1178 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1182 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1184 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1186 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1188 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1190 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 1192 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1194 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1196 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1198 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrUndo */

/* 1200 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1202 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1204 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrRedo */

/* 1206 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1208 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1210 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter puact */

/* 1212 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 1214 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1216 */	NdrFcShort( 0x88 ),	/* Type Offset=136 */

	/* Return value */

/* 1218 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1220 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1222 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddAction */

/* 1224 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1226 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1230 */	NdrFcShort( 0x9 ),	/* 9 */
/* 1232 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1234 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1236 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1238 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 1240 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1242 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1244 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1246 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter puact */

/* 1248 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 1250 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1252 */	NdrFcShort( 0x88 ),	/* Type Offset=136 */

	/* Return value */

/* 1254 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1256 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1258 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WinCollation */


	/* Procedure get_FwDatabaseDir */


	/* Procedure SetSavePoint */


	/* Procedure GetUndoText */

/* 1260 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1262 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1266 */	NdrFcShort( 0xa ),	/* 10 */
/* 1268 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1272 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1274 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 1276 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1278 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1280 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1282 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstr */


	/* Parameter pbstr */


	/* Parameter pbstrUndoText */

/* 1284 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1286 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1288 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 1290 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1292 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1294 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StringEx */


	/* Procedure GetUndoTextN */

/* 1296 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1298 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1302 */	NdrFcShort( 0xb ),	/* 11 */
/* 1304 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1306 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1308 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1310 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1312 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1314 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1318 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irb */


	/* Parameter iAct */

/* 1320 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1322 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1324 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstrUndoText */

/* 1326 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1328 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1330 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 1332 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1334 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1336 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IcuResourceName */


	/* Procedure get_ClassName */


	/* Procedure GetRedoText */

/* 1338 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1340 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1344 */	NdrFcShort( 0xc ),	/* 12 */
/* 1346 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1348 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1350 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1352 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 1354 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1356 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1358 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1360 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstrClsName */


	/* Parameter pbstrRedoText */

/* 1362 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1364 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1366 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 1368 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1370 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1372 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldLabel */


	/* Procedure GetRedoTextN */

/* 1374 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1376 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1380 */	NdrFcShort( 0xd ),	/* 13 */
/* 1382 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1384 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1386 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1388 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 1390 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 1392 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1394 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1396 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */


	/* Parameter iAct */

/* 1398 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1400 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1402 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldLabel */


	/* Parameter pbstrRedoText */

/* 1404 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 1406 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1408 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 1410 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1412 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1414 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CanRedo */

/* 1416 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1418 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1422 */	NdrFcShort( 0xf ),	/* 15 */
/* 1424 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1426 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1428 */	NdrFcShort( 0x22 ),	/* 34 */
/* 1430 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1432 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1434 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1436 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1438 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfCanRedo */

/* 1440 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1442 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1444 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 1446 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1448 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1450 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Undo */

/* 1452 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1454 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1458 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1460 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1462 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1464 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1466 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1468 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1470 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1472 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1474 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pures */

/* 1476 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1478 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1480 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 1482 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1484 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1486 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Redo */

/* 1488 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1490 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1494 */	NdrFcShort( 0x11 ),	/* 17 */
/* 1496 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1498 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1500 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1502 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1504 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1506 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1508 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1510 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pures */

/* 1512 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1514 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1516 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 1518 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1520 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1522 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SaveWritingSystems */


	/* Procedure Commit */

/* 1524 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1526 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1530 */	NdrFcShort( 0x12 ),	/* 18 */
/* 1532 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1534 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1536 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1538 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1540 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1542 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1544 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1546 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 1548 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1550 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1552 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Close */

/* 1554 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1556 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1560 */	NdrFcShort( 0x13 ),	/* 19 */
/* 1562 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1564 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1566 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1568 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 1570 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1572 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1574 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1576 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 1578 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1580 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1582 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_UserWs */


	/* Procedure get_FontDescent */


	/* Procedure get_ClassCount */


	/* Procedure Mark */

/* 1584 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1586 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1590 */	NdrFcShort( 0x14 ),	/* 20 */
/* 1592 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1594 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1596 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1598 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1600 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1602 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1604 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1606 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pws */


	/* Parameter pyRet */


	/* Parameter pcclid */


	/* Parameter phMark */

/* 1608 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1610 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1612 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 1614 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1616 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1618 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CollapseToMark */

/* 1620 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1622 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1626 */	NdrFcShort( 0x15 ),	/* 21 */
/* 1628 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 1630 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1632 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1634 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 1636 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 1638 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1640 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1642 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hMark */

/* 1644 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1646 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1648 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrUndo */

/* 1650 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1652 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1654 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrRedo */

/* 1656 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 1658 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1660 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 1662 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1664 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1666 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DiscardToMark */

/* 1668 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1670 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1674 */	NdrFcShort( 0x16 ),	/* 22 */
/* 1676 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1678 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1680 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1682 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1684 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1686 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1688 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1690 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter hMark */

/* 1692 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1694 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1696 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1698 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1700 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1702 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_XUnitsPerInch */


	/* Procedure get_TopMarkHandle */

/* 1704 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1706 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1710 */	NdrFcShort( 0x17 ),	/* 23 */
/* 1712 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1714 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1716 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1718 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1720 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1722 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1724 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1726 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pxInch */


	/* Parameter phMark */

/* 1728 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1730 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1732 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 1734 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1736 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1738 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_TasksSinceMark */

/* 1740 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1742 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1746 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1748 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1750 */	NdrFcShort( 0x6 ),	/* 6 */
/* 1752 */	NdrFcShort( 0x22 ),	/* 34 */
/* 1754 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 1756 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1758 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1760 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1762 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fUndo */

/* 1764 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1766 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1768 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pf */

/* 1770 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1772 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1774 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 1776 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1778 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_YUnitsPerInch */


	/* Procedure get_UndoableActionCount */

/* 1782 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1784 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1788 */	NdrFcShort( 0x19 ),	/* 25 */
/* 1790 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1792 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1794 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1796 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1798 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1800 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1802 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1804 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pyInch */


	/* Parameter pcSeq */

/* 1806 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1808 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1810 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 1812 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1814 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1816 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_UndoableSequenceCount */

/* 1818 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1820 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1824 */	NdrFcShort( 0x1a ),	/* 26 */
/* 1826 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1828 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1830 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1832 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1834 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1836 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1840 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcSeq */

/* 1842 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1844 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1846 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1848 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1850 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1852 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RedoableSequenceCount */

/* 1854 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1856 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1860 */	NdrFcShort( 0x1b ),	/* 27 */
/* 1862 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1864 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1866 */	NdrFcShort( 0x24 ),	/* 36 */
/* 1868 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1870 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1872 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1874 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1876 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcSeq */

/* 1878 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 1880 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1882 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 1884 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1886 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1888 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_UndoGrouper */

/* 1890 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1892 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1896 */	NdrFcShort( 0x1c ),	/* 28 */
/* 1898 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1900 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1902 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1904 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 1906 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1908 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1910 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1912 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppundg */

/* 1914 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 1916 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1918 */	NdrFcShort( 0x9e ),	/* Type Offset=158 */

	/* Return value */

/* 1920 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1922 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1924 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_UndoGrouper */

/* 1926 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1928 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1932 */	NdrFcShort( 0x1d ),	/* 29 */
/* 1934 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1936 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1938 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1940 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 1942 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1944 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1948 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pundg */

/* 1950 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 1952 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1954 */	NdrFcShort( 0xa2 ),	/* Type Offset=162 */

	/* Return value */

/* 1956 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1958 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1960 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Visibility */


	/* Procedure Step */

/* 1962 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 1964 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1968 */	NdrFcShort( 0x3 ),	/* 3 */
/* 1970 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 1972 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1974 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1976 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 1978 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 1980 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1982 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1984 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nVis */


	/* Parameter nStepAmt */

/* 1986 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 1988 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 1990 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 1992 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 1994 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1996 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetReplacePattern */


	/* Procedure Append */


	/* Procedure put_Title */

/* 1998 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2000 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2004 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2006 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2008 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2010 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2012 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2014 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2016 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2018 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2020 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrPattern */


	/* Parameter bstrIns */


	/* Parameter bstrTitle */

/* 2022 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2024 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2026 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 2028 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2030 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2032 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DetachDatabase */


	/* Procedure put_Contents */


	/* Procedure put_Message */

/* 2034 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2036 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2040 */	NdrFcShort( 0x5 ),	/* 5 */
/* 2042 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2044 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2046 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2048 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2050 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2052 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2054 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2056 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDatabaseName */


	/* Parameter bstr */


	/* Parameter bstrMessage */

/* 2058 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2060 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2062 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 2064 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2066 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2068 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RemoveEngine */


	/* Procedure put_Position */

/* 2070 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2072 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2076 */	NdrFcShort( 0x6 ),	/* 6 */
/* 2078 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2080 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2082 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2084 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2086 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2088 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2090 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2092 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */


	/* Parameter nPos */

/* 2094 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2096 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2098 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 2100 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2102 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2104 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_StepSize */

/* 2106 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2108 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2112 */	NdrFcShort( 0x7 ),	/* 7 */
/* 2114 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2116 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2118 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2120 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2122 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2124 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2126 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2128 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nStepInc */

/* 2130 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2132 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2134 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2136 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2138 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2140 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetRange */

/* 2142 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2144 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2148 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2150 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2152 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2154 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2156 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 2158 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2160 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2162 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2164 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nMin */

/* 2166 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2168 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2170 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nMax */

/* 2172 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2174 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2176 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 2178 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2180 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2182 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Report */

/* 2184 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2186 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2190 */	NdrFcShort( 0x3 ),	/* 3 */
/* 2192 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2194 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2196 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2198 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 2200 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2202 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2204 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2206 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nReportType */

/* 2208 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2210 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2212 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter szMsg */

/* 2214 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2216 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2218 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 2220 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2222 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2224 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */


	/* Procedure ShowAssertMessageBox */

/* 2226 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2228 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2232 */	NdrFcShort( 0x3 ),	/* 3 */
/* 2234 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2236 */	NdrFcShort( 0x6 ),	/* 6 */
/* 2238 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2240 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2242 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2244 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2246 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2248 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fForce */


	/* Parameter fShowMessageBox */

/* 2250 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2252 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2254 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 2256 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2258 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2260 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetSink */

/* 2262 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2264 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2268 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2270 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2272 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2274 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2276 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2278 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2280 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2282 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2284 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pSink */

/* 2286 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2288 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2290 */	NdrFcShort( 0xb4 ),	/* Type Offset=180 */

	/* Return value */

/* 2292 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2294 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2296 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetHelpString */

/* 2298 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2300 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2304 */	NdrFcShort( 0x3 ),	/* 3 */
/* 2306 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 2308 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2310 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2312 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 2314 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 2316 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2318 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2320 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrPropName */

/* 2322 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2324 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2326 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter iKey */

/* 2328 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2330 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2332 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrPropValue */

/* 2334 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 2336 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2338 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 2340 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2342 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2344 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NLayout */


	/* Procedure GetClipboardType */


	/* Procedure get_NameWsCount */


	/* Procedure get_Length */


	/* Procedure CheckConnections */


	/* Procedure get_Length */


	/* Procedure get_Length */


	/* Procedure get_Visibility */

/* 2346 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2348 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2352 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2354 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2356 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2358 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2360 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2362 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2364 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2366 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2368 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pclayout */


	/* Parameter type */


	/* Parameter pcws */


	/* Parameter pcch */


	/* Parameter pnResponse */


	/* Parameter pcch */


	/* Parameter pcch */


	/* Parameter pnVis */

/* 2370 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2372 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2374 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 2376 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2378 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2380 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_HideLabel */

/* 2382 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2384 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2388 */	NdrFcShort( 0x5 ),	/* 5 */
/* 2390 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2392 */	NdrFcShort( 0x6 ),	/* 6 */
/* 2394 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2396 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2398 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2400 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2402 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2404 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fHide */

/* 2406 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2408 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2410 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 2412 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2414 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2416 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Label */

/* 2418 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2420 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2424 */	NdrFcShort( 0x7 ),	/* 7 */
/* 2426 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2428 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2430 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2432 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2434 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2436 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2438 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2440 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptssLabel */

/* 2442 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 2444 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2446 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 2448 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2450 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2452 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Label */

/* 2454 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2456 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2460 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2462 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2464 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2466 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2468 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2470 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2472 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2474 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2476 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptssLabel */

/* 2478 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 2480 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2482 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 2484 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2486 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2488 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Locale */


	/* Procedure put_WinLCID */


	/* Procedure put_FieldId */

/* 2490 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2492 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2496 */	NdrFcShort( 0x9 ),	/* 9 */
/* 2498 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2500 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2502 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2504 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2506 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2508 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2510 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2512 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nLocale */


	/* Parameter nCode */


	/* Parameter flid */

/* 2514 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2516 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2518 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 2520 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2522 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2524 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Size */


	/* Procedure get_FieldId */

/* 2526 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2528 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2532 */	NdrFcShort( 0xa ),	/* 10 */
/* 2534 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2536 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2538 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2540 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2542 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2544 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2546 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2548 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcrb */


	/* Parameter pflid */

/* 2550 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2552 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2554 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 2556 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2558 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2560 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_WinCollation */


	/* Procedure put_ClassName */

/* 2562 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2564 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2568 */	NdrFcShort( 0xb ),	/* 11 */
/* 2570 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2572 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2574 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2576 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2578 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2580 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2582 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2584 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */


	/* Parameter bstrClsName */

/* 2586 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2588 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2590 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */


	/* Return value */

/* 2592 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2594 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2596 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_IcuResourceName */


	/* Procedure put_FieldName */

/* 2598 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2600 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2604 */	NdrFcShort( 0xd ),	/* 13 */
/* 2606 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2608 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2610 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2612 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2614 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2616 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2618 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2620 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */


	/* Parameter bstrFieldName */

/* 2622 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2624 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2626 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */


	/* Return value */

/* 2628 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2630 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2632 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IcuResourceText */


	/* Procedure get_Database */


	/* Procedure get_FieldName */

/* 2634 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2636 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2640 */	NdrFcShort( 0xe ),	/* 14 */
/* 2642 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2646 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2648 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2650 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2652 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2654 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2656 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstrDb */


	/* Parameter pbstrFieldName */

/* 2658 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 2660 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2662 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 2664 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2666 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2668 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_IcuResourceText */


	/* Procedure put_Style */

/* 2670 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2672 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2676 */	NdrFcShort( 0xf ),	/* 15 */
/* 2678 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2680 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2682 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2684 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 2686 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 2688 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2690 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2692 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */


	/* Parameter bstrStyle */

/* 2694 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 2696 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2698 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */


	/* Return value */

/* 2700 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2702 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2704 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Style */

/* 2706 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2708 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2712 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2714 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2716 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2718 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2720 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2722 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2724 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2726 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2728 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrStyle */

/* 2730 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 2732 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2734 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 2736 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2738 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2740 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Count */


	/* Procedure get_Count */


	/* Procedure get_Count */


	/* Procedure get_WritingSystem */


	/* Procedure ColValWasNull */


	/* Procedure get_StringCount */


	/* Procedure get_IntPropCount */


	/* Procedure get_IntPropCount */


	/* Procedure BeginGroup */

/* 2742 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2744 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2748 */	NdrFcShort( 0x3 ),	/* 3 */
/* 2750 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2754 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2756 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2758 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2760 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2762 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2764 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pclocale */


	/* Parameter pctrans */


	/* Parameter pcconv */


	/* Parameter pws */


	/* Parameter pfIsNull */


	/* Parameter pctss */


	/* Parameter pcv */


	/* Parameter pcv */


	/* Parameter phndl */

/* 2766 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2768 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2770 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 2772 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2774 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2776 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetOptions */


	/* Procedure put_ForeColor */


	/* Procedure EndGroup */

/* 2778 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2780 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2784 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2786 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2788 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2790 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2792 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2794 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2796 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2800 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter grfsplc */


	/* Parameter clr */


	/* Parameter hndl */

/* 2802 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2804 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2806 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 2808 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2810 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2812 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_BackColor */


	/* Procedure CancelGroup */

/* 2814 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2816 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2820 */	NdrFcShort( 0x5 ),	/* 5 */
/* 2822 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2824 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2826 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2828 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2830 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2832 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2834 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2836 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter clr */


	/* Parameter hndl */

/* 2838 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2840 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2842 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 2844 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2846 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2848 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetLocalServer_Bkupd */


	/* Procedure get_Text */


	/* Procedure get_Text */


	/* Procedure get_Text */

/* 2850 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2852 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2856 */	NdrFcShort( 0x3 ),	/* 3 */
/* 2858 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2860 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2862 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2864 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 2866 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 2868 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2870 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2872 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrSvrName */


	/* Parameter pbstr */


	/* Parameter pbstr */


	/* Parameter pbstr */

/* 2874 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 2876 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2878 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 2880 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2882 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2884 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NameWsCount */


	/* Procedure get_SegDatMaxLength */


	/* Procedure get_RunCount */


	/* Procedure get_RunCount */

/* 2886 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2888 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2892 */	NdrFcShort( 0x5 ),	/* 5 */
/* 2894 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2896 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2898 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2900 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 2902 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2904 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2906 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2908 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcws */


	/* Parameter cb */


	/* Parameter pcrun */


	/* Parameter pcrun */

/* 2910 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2912 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2914 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 2916 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2918 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2920 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetInt */


	/* Procedure get_RunAt */


	/* Procedure get_RunAt */

/* 2922 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2924 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2928 */	NdrFcShort( 0x6 ),	/* 6 */
/* 2930 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2932 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2934 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2936 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 2938 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2940 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2942 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2944 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iColIndex */


	/* Parameter ich */


	/* Parameter ich */

/* 2946 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2948 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2950 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnValue */


	/* Parameter pirun */


	/* Parameter pirun */

/* 2952 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2954 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2956 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 2958 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 2960 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 2962 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_MinOfRun */

/* 2964 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 2966 */	NdrFcLong( 0x0 ),	/* 0 */
/* 2970 */	NdrFcShort( 0x7 ),	/* 7 */
/* 2972 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 2974 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2976 */	NdrFcShort( 0x24 ),	/* 36 */
/* 2978 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 2980 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 2982 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2984 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2986 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 2988 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 2990 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 2992 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichMin */

/* 2994 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 2996 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2998 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3000 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3002 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3004 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LimOfRun */

/* 3006 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3008 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3012 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3014 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3016 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3018 */	NdrFcShort( 0x24 ),	/* 36 */
/* 3020 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 3022 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3024 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3028 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 3030 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3032 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3034 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 3036 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3038 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3040 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3042 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3044 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3046 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBoundsOfRun */

/* 3048 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3050 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3054 */	NdrFcShort( 0x9 ),	/* 9 */
/* 3056 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3058 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3060 */	NdrFcShort( 0x40 ),	/* 64 */
/* 3062 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 3064 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3066 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3070 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 3072 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3074 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3076 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichMin */

/* 3078 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3080 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3082 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 3084 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3086 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3088 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 3090 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3092 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3094 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FetchRunInfoAt */

/* 3096 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3098 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3102 */	NdrFcShort( 0xa ),	/* 10 */
/* 3104 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3106 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3108 */	NdrFcShort( 0x38 ),	/* 56 */
/* 3110 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 3112 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3114 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3118 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 3120 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3122 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptri */

/* 3126 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 3128 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3130 */	NdrFcShort( 0xca ),	/* Type Offset=202 */

	/* Parameter ppttp */

/* 3132 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3134 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3136 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 3138 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3140 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3142 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FetchRunInfo */

/* 3144 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3146 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3150 */	NdrFcShort( 0xb ),	/* 11 */
/* 3152 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3154 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3156 */	NdrFcShort( 0x38 ),	/* 56 */
/* 3158 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 3160 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3162 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3164 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3166 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 3168 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3170 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3172 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptri */

/* 3174 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 3176 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3178 */	NdrFcShort( 0xca ),	/* Type Offset=202 */

	/* Parameter ppttp */

/* 3180 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3182 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3184 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 3186 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3188 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldName */


	/* Procedure get_RunText */

/* 3192 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3194 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3198 */	NdrFcShort( 0xc ),	/* 12 */
/* 3200 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3202 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3204 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3206 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 3208 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 3210 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3212 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3214 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */


	/* Parameter irun */

/* 3216 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3218 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3220 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldName */


	/* Parameter pbstr */

/* 3222 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 3224 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3226 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 3228 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3230 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3232 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetChars */

/* 3234 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3236 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3240 */	NdrFcShort( 0xd ),	/* 13 */
/* 3242 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3244 */	NdrFcShort( 0x10 ),	/* 16 */
/* 3246 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3248 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 3250 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 3252 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3254 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3256 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 3258 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3260 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3262 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 3264 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3266 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3268 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 3270 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 3272 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3274 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 3276 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3278 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3280 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_PropertiesAt */

/* 3282 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3284 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3288 */	NdrFcShort( 0x13 ),	/* 19 */
/* 3290 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3292 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3294 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3296 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 3298 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3302 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3304 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 3306 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3308 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3310 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppttp */

/* 3312 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3314 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3316 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 3318 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3320 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3322 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Properties */

/* 3324 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3326 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3330 */	NdrFcShort( 0x14 ),	/* 20 */
/* 3332 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3334 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3336 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3338 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 3340 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3346 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 3348 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3350 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppttp */

/* 3354 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3356 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3358 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 3360 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3362 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3364 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBldr */

/* 3366 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3368 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3372 */	NdrFcShort( 0x15 ),	/* 21 */
/* 3374 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3376 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3378 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3380 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 3382 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3384 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3386 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3388 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptsb */

/* 3390 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3392 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3394 */	NdrFcShort( 0xe8 ),	/* Type Offset=232 */

	/* Return value */

/* 3396 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3398 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3400 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIncBldr */

/* 3402 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3404 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3408 */	NdrFcShort( 0x16 ),	/* 22 */
/* 3410 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3412 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3414 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3416 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 3418 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3420 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3422 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3424 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptisb */

/* 3426 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3428 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3430 */	NdrFcShort( 0xfe ),	/* Type Offset=254 */

	/* Return value */

/* 3432 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3434 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3436 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBldrClsid */


	/* Procedure GetFactoryClsid */

/* 3438 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3440 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3444 */	NdrFcShort( 0x17 ),	/* 23 */
/* 3446 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3448 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3450 */	NdrFcShort( 0x4c ),	/* 76 */
/* 3452 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 3454 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3456 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3458 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3460 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pclsid */


	/* Parameter pclsid */

/* 3462 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 3464 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3466 */	NdrFcShort( 0x11e ),	/* Type Offset=286 */

	/* Return value */


	/* Return value */

/* 3468 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3470 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3472 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SerializeFmt */


	/* Procedure SerializeFmt */

/* 3474 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3476 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3480 */	NdrFcShort( 0x18 ),	/* 24 */
/* 3482 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3484 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3486 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3488 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 3490 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3492 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3494 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3496 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */


	/* Parameter pstrm */

/* 3498 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3500 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3502 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Return value */


	/* Return value */

/* 3504 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3506 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SerializeFmtRgb */


	/* Procedure SerializeFmtRgb */

/* 3510 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3512 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3516 */	NdrFcShort( 0x19 ),	/* 25 */
/* 3518 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3520 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3522 */	NdrFcShort( 0x24 ),	/* 36 */
/* 3524 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 3526 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 3528 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3530 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3532 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgb */


	/* Parameter prgb */

/* 3534 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 3536 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3538 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cbMax */


	/* Parameter cbMax */

/* 3540 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3542 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3544 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcbNeeded */


	/* Parameter pcbNeeded */

/* 3546 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3548 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3550 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 3552 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3554 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3556 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Equals */

/* 3558 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3560 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3564 */	NdrFcShort( 0x1a ),	/* 26 */
/* 3566 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3568 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3570 */	NdrFcShort( 0x22 ),	/* 34 */
/* 3572 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 3574 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3576 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3578 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3580 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptss */

/* 3582 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3584 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3586 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Parameter pfEqual */

/* 3588 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3590 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3592 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 3594 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3596 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteAsXml */

/* 3600 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3602 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3606 */	NdrFcShort( 0x1b ),	/* 27 */
/* 3608 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 3610 */	NdrFcShort( 0x16 ),	/* 22 */
/* 3612 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3614 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 3616 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3618 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3620 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3622 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 3624 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3626 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3628 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter pwsf */

/* 3630 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 3632 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3634 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Parameter cchIndent */

/* 3636 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3638 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3640 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 3642 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3644 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3646 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fWriteObjData */

/* 3648 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3650 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3652 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 3654 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3656 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3658 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsNormalizedForm */

/* 3660 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3662 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3666 */	NdrFcShort( 0x1c ),	/* 28 */
/* 3668 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3670 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3672 */	NdrFcShort( 0x22 ),	/* 34 */
/* 3674 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 3676 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3678 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3680 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3682 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nm */

/* 3684 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3686 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3688 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 3690 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3692 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3694 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 3696 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3698 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NormalizedForm */

/* 3702 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3704 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3708 */	NdrFcShort( 0x1d ),	/* 29 */
/* 3710 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3712 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3714 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3716 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 3718 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3720 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3722 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3724 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nm */

/* 3726 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3728 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3730 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pptssRet */

/* 3732 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3734 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3736 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 3738 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3740 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3742 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIntProp */


	/* Procedure GetIntProp */

/* 3744 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3746 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3750 */	NdrFcShort( 0x4 ),	/* 4 */
/* 3752 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 3754 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3756 */	NdrFcShort( 0x5c ),	/* 92 */
/* 3758 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 3760 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3762 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3766 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iv */


	/* Parameter iv */

/* 3768 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3770 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3772 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptpt */


	/* Parameter ptpt */

/* 3774 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3776 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3778 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnVar */


	/* Parameter pnVar */

/* 3780 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3782 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3784 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnVal */


	/* Parameter pnVal */

/* 3786 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3788 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3790 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 3792 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3794 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3796 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIntPropValues */


	/* Procedure GetIntPropValues */

/* 3798 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3800 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3804 */	NdrFcShort( 0x5 ),	/* 5 */
/* 3806 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3808 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3810 */	NdrFcShort( 0x40 ),	/* 64 */
/* 3812 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 3814 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3818 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3820 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tpt */


	/* Parameter tpt */

/* 3822 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3824 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnVar */


	/* Parameter pnVar */

/* 3828 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3830 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3832 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnVal */


	/* Parameter pnVal */

/* 3834 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3836 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3838 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 3840 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3842 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3844 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FieldCount */


	/* Procedure get_StrPropCount */


	/* Procedure get_StrPropCount */

/* 3846 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3848 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3852 */	NdrFcShort( 0x6 ),	/* 6 */
/* 3854 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3856 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3858 */	NdrFcShort( 0x24 ),	/* 36 */
/* 3860 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 3862 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3864 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3866 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3868 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcflid */


	/* Parameter pcv */


	/* Parameter pcv */

/* 3870 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3872 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3874 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 3876 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3878 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3880 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetStrProp */


	/* Procedure GetStrProp */

/* 3882 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3884 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3888 */	NdrFcShort( 0x7 ),	/* 7 */
/* 3890 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 3892 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3894 */	NdrFcShort( 0x24 ),	/* 36 */
/* 3896 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 3898 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 3900 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3904 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iv */


	/* Parameter iv */

/* 3906 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3908 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3910 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptpt */


	/* Parameter ptpt */

/* 3912 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 3914 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3916 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrVal */


	/* Parameter pbstrVal */

/* 3918 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 3920 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3922 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 3924 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3926 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3928 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetStrFromWs */


	/* Procedure GetOwnClsName */


	/* Procedure GetStrPropValue */


	/* Procedure GetStrPropValue */

/* 3930 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3932 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3936 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3938 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 3940 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3942 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3944 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 3946 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 3948 */	NdrFcShort( 0x1 ),	/* 1 */
/* 3950 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3952 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter wsId */


	/* Parameter luFlid */


	/* Parameter tpt */


	/* Parameter tpt */

/* 3954 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 3956 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 3958 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstrOwnClsName */


	/* Parameter pbstrVal */


	/* Parameter pbstrVal */

/* 3960 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 3962 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 3964 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 3966 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 3968 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBldr */

/* 3972 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 3974 */	NdrFcLong( 0x0 ),	/* 0 */
/* 3978 */	NdrFcShort( 0x9 ),	/* 9 */
/* 3980 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 3982 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3984 */	NdrFcShort( 0x8 ),	/* 8 */
/* 3986 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 3988 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 3990 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3992 */	NdrFcShort( 0x0 ),	/* 0 */
/* 3994 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptpb */

/* 3996 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 3998 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4000 */	NdrFcShort( 0x15e ),	/* Type Offset=350 */

	/* Return value */

/* 4002 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4004 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4006 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFactoryClsid */

/* 4008 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4010 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4014 */	NdrFcShort( 0xa ),	/* 10 */
/* 4016 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4018 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4020 */	NdrFcShort( 0x4c ),	/* 76 */
/* 4022 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 4024 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4028 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4030 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pclsid */

/* 4032 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 4034 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4036 */	NdrFcShort( 0x11e ),	/* Type Offset=286 */

	/* Return value */

/* 4038 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4040 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4042 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Serialize */

/* 4044 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4046 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4050 */	NdrFcShort( 0xb ),	/* 11 */
/* 4052 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4054 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4056 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4058 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 4060 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4062 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4064 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4066 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 4068 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4070 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4072 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Return value */

/* 4074 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4076 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4078 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SerializeRgb */

/* 4080 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4082 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4086 */	NdrFcShort( 0xc ),	/* 12 */
/* 4088 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4090 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4092 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4094 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 4096 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 4098 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4100 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4102 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgb */

/* 4104 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 4106 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4108 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cbMax */

/* 4110 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4112 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4114 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcb */

/* 4116 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4118 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4120 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4122 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4124 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4126 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SerializeRgPropsRgb */

/* 4128 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4130 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4134 */	NdrFcShort( 0xd ),	/* 13 */
/* 4136 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 4138 */	NdrFcShort( 0x2c ),	/* 44 */
/* 4140 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4142 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 4144 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 4146 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4148 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4150 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cpttp */

/* 4152 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4154 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4156 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rgpttp */

/* 4158 */	NdrFcShort( 0x200b ),	/* Flags:  must size, must free, in, srv alloc size=8 */
/* 4160 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4162 */	NdrFcShort( 0x174 ),	/* Type Offset=372 */

	/* Parameter rgich */

/* 4164 */	NdrFcShort( 0x148 ),	/* Flags:  in, base type, simple ref, */
/* 4166 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4168 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgb */

/* 4170 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 4172 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4174 */	NdrFcShort( 0x180 ),	/* Type Offset=384 */

	/* Parameter cbMax */

/* 4176 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4178 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4180 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcb */

/* 4182 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4184 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4186 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4188 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4190 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 4192 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteAsXml */

/* 4194 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4196 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4200 */	NdrFcShort( 0xe ),	/* 14 */
/* 4202 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4204 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4206 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4208 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 4210 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4212 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4214 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4216 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 4218 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4220 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4222 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter pwsf */

/* 4224 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4226 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4228 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Parameter cchIndent */

/* 4230 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4232 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4234 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4236 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4238 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4240 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeserializeStringStreams */

/* 4242 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4244 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4248 */	NdrFcShort( 0x3 ),	/* 3 */
/* 4250 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4252 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4254 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4256 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 4258 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4260 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4262 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4264 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrmTxt */

/* 4266 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4268 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4270 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter pstrmFmt */

/* 4272 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4274 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4276 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter pptss */

/* 4278 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4280 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4282 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 4284 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4286 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4288 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeserializeString */

/* 4290 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4292 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4296 */	NdrFcShort( 0x4 ),	/* 4 */
/* 4298 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4302 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4304 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 4306 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4308 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4310 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4312 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrTxt */

/* 4314 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 4316 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4318 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pstrmFmt */

/* 4320 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4322 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4324 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter pptss */

/* 4326 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4328 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4330 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 4332 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4334 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4336 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeserializeStringRgb */

/* 4338 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4340 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4344 */	NdrFcShort( 0x5 ),	/* 5 */
/* 4346 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4348 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4350 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4352 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 4354 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4356 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4358 */	NdrFcShort( 0x2 ),	/* 2 */
/* 4360 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrTxt */

/* 4362 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 4364 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4366 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter prgbFmt */

/* 4368 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4370 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4372 */	NdrFcShort( 0x190 ),	/* Type Offset=400 */

	/* Parameter cbFmt */

/* 4374 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4376 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4378 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 4380 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4382 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4384 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 4386 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4388 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4390 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeserializeStringRgch */

/* 4392 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4394 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4398 */	NdrFcShort( 0x6 ),	/* 6 */
/* 4400 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 4402 */	NdrFcShort( 0x38 ),	/* 56 */
/* 4404 */	NdrFcShort( 0x40 ),	/* 64 */
/* 4406 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 4408 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4410 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4412 */	NdrFcShort( 0x2 ),	/* 2 */
/* 4414 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchTxt */

/* 4416 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4418 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4420 */	NdrFcShort( 0x1a0 ),	/* Type Offset=416 */

	/* Parameter pcchTxt */

/* 4422 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 4424 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4426 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgbFmt */

/* 4428 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4430 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4432 */	NdrFcShort( 0x1ac ),	/* Type Offset=428 */

	/* Parameter pcbFmt */

/* 4434 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 4436 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4438 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 4440 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4442 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4444 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 4446 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4448 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4450 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeString */

/* 4452 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4454 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4458 */	NdrFcShort( 0x7 ),	/* 7 */
/* 4460 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4462 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4464 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4466 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 4468 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4470 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4472 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4474 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 4476 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 4478 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4480 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter ws */

/* 4482 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4484 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4486 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 4488 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4490 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4492 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 4494 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4496 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4498 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeStringRgch */

/* 4500 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4502 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4506 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4508 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4510 */	NdrFcShort( 0x10 ),	/* 16 */
/* 4512 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4514 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 4516 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4520 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4522 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgch */

/* 4524 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4526 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4528 */	NdrFcShort( 0x1bc ),	/* Type Offset=444 */

	/* Parameter cch */

/* 4530 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4532 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4534 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 4536 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4538 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4540 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 4542 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4544 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4546 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 4548 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4550 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4552 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeStringWithPropsRgch */

/* 4554 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4556 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4560 */	NdrFcShort( 0x9 ),	/* 9 */
/* 4562 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4564 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4566 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4568 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 4570 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4572 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4574 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4576 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgch */

/* 4578 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4580 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4582 */	NdrFcShort( 0x1c8 ),	/* Type Offset=456 */

	/* Parameter cch */

/* 4584 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4586 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4588 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pttp */

/* 4590 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4592 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4594 */	NdrFcShort( 0xd6 ),	/* Type Offset=214 */

	/* Parameter pptss */

/* 4596 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4598 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4600 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 4602 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4604 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4606 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBldr */

/* 4608 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4610 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4614 */	NdrFcShort( 0xa ),	/* 10 */
/* 4616 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4618 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4620 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4622 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 4624 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4626 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4628 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4630 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptsb */

/* 4632 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4634 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4636 */	NdrFcShort( 0xe8 ),	/* Type Offset=232 */

	/* Return value */

/* 4638 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4640 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4642 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIncBldr */

/* 4644 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4646 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4650 */	NdrFcShort( 0xb ),	/* 11 */
/* 4652 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4654 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4656 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4658 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 4660 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4662 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4664 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4666 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptisb */

/* 4668 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4670 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4672 */	NdrFcShort( 0xfe ),	/* Type Offset=254 */

	/* Return value */

/* 4674 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4676 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RunCount */

/* 4680 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4682 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4686 */	NdrFcShort( 0xc ),	/* 12 */
/* 4688 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4690 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4692 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4694 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 4696 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4698 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4700 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4702 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgbFmt */

/* 4704 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4706 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4708 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cbFmt */

/* 4710 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4712 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcrun */

/* 4716 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4718 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 4722 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4724 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4726 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FetchRunInfoAt */

/* 4728 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4730 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4734 */	NdrFcShort( 0xd ),	/* 13 */
/* 4736 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 4738 */	NdrFcShort( 0x10 ),	/* 16 */
/* 4740 */	NdrFcShort( 0x38 ),	/* 56 */
/* 4742 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 4744 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4746 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4748 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4750 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgbFmt */

/* 4752 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4754 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4756 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cbFmt */

/* 4758 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4760 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4762 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ich */

/* 4764 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4766 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4768 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptri */

/* 4770 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 4772 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4774 */	NdrFcShort( 0xca ),	/* Type Offset=202 */

	/* Parameter ppttp */

/* 4776 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4778 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4780 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 4782 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4784 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4786 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FetchRunInfo */

/* 4788 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4790 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4794 */	NdrFcShort( 0xe ),	/* 14 */
/* 4796 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 4798 */	NdrFcShort( 0x10 ),	/* 16 */
/* 4800 */	NdrFcShort( 0x38 ),	/* 56 */
/* 4802 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 4804 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4806 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4808 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4810 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgbFmt */

/* 4812 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4814 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4816 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cbFmt */

/* 4818 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4820 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4822 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter irun */

/* 4824 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4826 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4828 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptri */

/* 4830 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 4832 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4834 */	NdrFcShort( 0xca ),	/* Type Offset=202 */

	/* Parameter ppttp */

/* 4836 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4838 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4840 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 4842 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4844 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4846 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeserializeProps */

/* 4848 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4850 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4854 */	NdrFcShort( 0x3 ),	/* 3 */
/* 4856 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4858 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4860 */	NdrFcShort( 0x8 ),	/* 8 */
/* 4862 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 4864 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 4866 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4868 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4870 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 4872 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 4874 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4876 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter ppttp */

/* 4878 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4880 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4882 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 4884 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4886 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4888 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeserializePropsRgb */

/* 4890 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4892 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4896 */	NdrFcShort( 0x4 ),	/* 4 */
/* 4898 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4900 */	NdrFcShort( 0x1c ),	/* 28 */
/* 4902 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4904 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 4906 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 4908 */	NdrFcShort( 0x0 ),	/* 0 */
/* 4910 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4912 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgb */

/* 4914 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4916 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4918 */	NdrFcShort( 0x1d4 ),	/* Type Offset=468 */

	/* Parameter pcb */

/* 4920 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 4922 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4924 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppttp */

/* 4926 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 4928 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4930 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 4932 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 4934 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4936 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeserializeRgPropsRgb */

/* 4938 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 4940 */	NdrFcLong( 0x0 ),	/* 0 */
/* 4944 */	NdrFcShort( 0x5 ),	/* 5 */
/* 4946 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 4948 */	NdrFcShort( 0x24 ),	/* 36 */
/* 4950 */	NdrFcShort( 0x40 ),	/* 64 */
/* 4952 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 4954 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 4956 */	NdrFcShort( 0x2 ),	/* 2 */
/* 4958 */	NdrFcShort( 0x1 ),	/* 1 */
/* 4960 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cpttpMax */

/* 4962 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 4964 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 4966 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgb */

/* 4968 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 4970 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 4972 */	NdrFcShort( 0x1e4 ),	/* Type Offset=484 */

	/* Parameter pcb */

/* 4974 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 4976 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 4978 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcpttpRet */

/* 4980 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 4982 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 4984 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rgpttp */

/* 4986 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 4988 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 4990 */	NdrFcShort( 0x1f4 ),	/* Type Offset=500 */

	/* Parameter rgich */

/* 4992 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 4994 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 4996 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Return value */

/* 4998 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5000 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5002 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakeProps */

/* 5004 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5006 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5010 */	NdrFcShort( 0x6 ),	/* 6 */
/* 5012 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5014 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5016 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5018 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 5020 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5022 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5024 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5026 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrStyle */

/* 5028 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 5030 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5032 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter ws */

/* 5034 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5036 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5038 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ows */

/* 5040 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5042 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5044 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppttp */

/* 5046 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5048 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5050 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 5052 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5054 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5056 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakePropsRgch */

/* 5058 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5060 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5064 */	NdrFcShort( 0x7 ),	/* 7 */
/* 5066 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5068 */	NdrFcShort( 0x18 ),	/* 24 */
/* 5070 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5072 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 5074 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5076 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5078 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5080 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchStyle */

/* 5082 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5084 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5086 */	NdrFcShort( 0x21e ),	/* Type Offset=542 */

	/* Parameter cch */

/* 5088 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5090 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5092 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 5094 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5096 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5098 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ows */

/* 5100 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5102 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5104 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppttp */

/* 5106 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5108 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5110 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 5112 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5114 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5116 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetPropsBldr */

/* 5118 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5120 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5124 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5126 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5128 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5130 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5132 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 5134 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5136 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5138 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5140 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptpb */

/* 5142 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5144 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5146 */	NdrFcShort( 0x15e ),	/* Type Offset=350 */

	/* Return value */

/* 5148 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5150 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5152 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBoundsOfRun */

/* 5154 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5156 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5160 */	NdrFcShort( 0x7 ),	/* 7 */
/* 5162 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5164 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5166 */	NdrFcShort( 0x40 ),	/* 64 */
/* 5168 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 5170 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5172 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5174 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5176 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 5178 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5180 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5182 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichMin */

/* 5184 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 5186 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5188 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 5190 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 5192 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5194 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 5196 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5198 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5200 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FetchRunInfoAt */

/* 5202 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5204 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5208 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5210 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5212 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5214 */	NdrFcShort( 0x38 ),	/* 56 */
/* 5216 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 5218 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5220 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5222 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5224 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 5226 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5228 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5230 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptri */

/* 5232 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 5234 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5236 */	NdrFcShort( 0xca ),	/* Type Offset=202 */

	/* Parameter ppttp */

/* 5238 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5240 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5242 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 5244 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5246 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5248 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FetchRunInfo */

/* 5250 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5252 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5256 */	NdrFcShort( 0x9 ),	/* 9 */
/* 5258 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5260 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5262 */	NdrFcShort( 0x38 ),	/* 56 */
/* 5264 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 5266 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5272 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 5274 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5276 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptri */

/* 5280 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 5282 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5284 */	NdrFcShort( 0xca ),	/* Type Offset=202 */

	/* Parameter ppttp */

/* 5286 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5288 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5290 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 5292 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5294 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5296 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RunText */

/* 5298 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5300 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5304 */	NdrFcShort( 0xa ),	/* 10 */
/* 5306 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5308 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5310 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5312 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 5314 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 5316 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5318 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5320 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 5322 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5324 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5326 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 5328 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 5330 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5332 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 5334 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5336 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5338 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetChars */

/* 5340 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5342 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5346 */	NdrFcShort( 0xb ),	/* 11 */
/* 5348 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5350 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5352 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5354 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 5356 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 5358 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5360 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5362 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 5364 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5366 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5368 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 5370 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5372 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5374 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 5376 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 5378 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5380 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 5382 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5384 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5386 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_PropertiesAt */

/* 5388 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5390 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5394 */	NdrFcShort( 0xd ),	/* 13 */
/* 5396 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5398 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5400 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5402 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 5404 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5406 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5408 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5410 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 5412 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5414 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5416 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pttp */

/* 5418 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5420 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5422 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 5424 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5426 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5428 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Properties */

/* 5430 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5432 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5436 */	NdrFcShort( 0xe ),	/* 14 */
/* 5438 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5440 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5442 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5444 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 5446 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5448 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5450 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5452 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter irun */

/* 5454 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5456 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5458 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pttp */

/* 5460 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5462 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5464 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 5466 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5468 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Replace */

/* 5472 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5474 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5478 */	NdrFcShort( 0xf ),	/* 15 */
/* 5480 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5482 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5484 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5486 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 5488 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5490 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5492 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5494 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 5496 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5498 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5500 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 5502 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5504 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5506 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrIns */

/* 5508 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 5510 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5512 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pttp */

/* 5514 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5516 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5518 */	NdrFcShort( 0xd6 ),	/* Type Offset=214 */

	/* Return value */

/* 5520 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5522 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5524 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReplaceTsString */

/* 5526 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5528 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5532 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5534 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5536 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5538 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5540 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 5542 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5544 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5546 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5548 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 5550 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5552 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5554 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 5556 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5558 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5560 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptssIns */

/* 5562 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5564 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5566 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 5568 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5570 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5572 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReplaceRgch */

/* 5574 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5576 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5580 */	NdrFcShort( 0x11 ),	/* 17 */
/* 5582 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5584 */	NdrFcShort( 0x18 ),	/* 24 */
/* 5586 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5588 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 5590 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5592 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5594 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5596 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 5598 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5600 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5602 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 5604 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5606 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5608 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchIns */

/* 5610 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5612 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5614 */	NdrFcShort( 0x22a ),	/* Type Offset=554 */

	/* Parameter cchIns */

/* 5616 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5618 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5620 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pttp */

/* 5622 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5624 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5626 */	NdrFcShort( 0xd6 ),	/* Type Offset=214 */

	/* Return value */

/* 5628 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5630 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5632 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetProperties */

/* 5634 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5636 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5640 */	NdrFcShort( 0x12 ),	/* 18 */
/* 5642 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5644 */	NdrFcShort( 0x10 ),	/* 16 */
/* 5646 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5648 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 5650 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5652 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5654 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5656 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 5658 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5660 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5662 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 5664 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5666 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5668 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pttp */

/* 5670 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5672 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5674 */	NdrFcShort( 0xd6 ),	/* Type Offset=214 */

	/* Return value */

/* 5676 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5678 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5680 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetIntPropValues */

/* 5682 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5684 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5688 */	NdrFcShort( 0x13 ),	/* 19 */
/* 5690 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 5692 */	NdrFcShort( 0x28 ),	/* 40 */
/* 5694 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5696 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 5698 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5700 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5702 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5704 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 5706 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5708 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5710 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 5712 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5714 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5716 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tpt */

/* 5718 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5720 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5722 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nVar */

/* 5724 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5726 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5728 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nVal */

/* 5730 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5732 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5734 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 5736 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5738 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5740 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetStrPropValue */

/* 5742 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5744 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5748 */	NdrFcShort( 0x14 ),	/* 20 */
/* 5750 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 5752 */	NdrFcShort( 0x18 ),	/* 24 */
/* 5754 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5756 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 5758 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5760 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5762 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5764 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 5766 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5768 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5770 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 5772 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5774 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5776 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter tpt */

/* 5778 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5780 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5782 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrVal */

/* 5784 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 5786 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5788 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 5790 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5792 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5794 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetString */

/* 5796 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5798 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5802 */	NdrFcShort( 0x15 ),	/* 21 */
/* 5804 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5806 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5808 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5810 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 5812 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5814 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5818 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptss */

/* 5820 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 5822 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5824 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 5826 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5828 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5830 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReleaseDC */


	/* Procedure Clear */

/* 5832 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5834 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5838 */	NdrFcShort( 0x16 ),	/* 22 */
/* 5840 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5842 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5844 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5846 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 5848 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5850 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5852 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5854 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 5856 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5858 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5860 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AppendTsString */

/* 5862 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5864 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5868 */	NdrFcShort( 0x5 ),	/* 5 */
/* 5870 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5872 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5874 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5876 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 5878 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5880 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5882 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5884 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptssIns */

/* 5886 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 5888 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5890 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 5892 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5894 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5896 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AppendRgch */

/* 5898 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5900 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5904 */	NdrFcShort( 0x6 ),	/* 6 */
/* 5906 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5908 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5910 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5912 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 5914 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 5916 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5918 */	NdrFcShort( 0x1 ),	/* 1 */
/* 5920 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIns */

/* 5922 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 5924 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5926 */	NdrFcShort( 0x236 ),	/* Type Offset=566 */

	/* Parameter cchIns */

/* 5928 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5930 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5932 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 5934 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5936 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5938 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetIntPropValues */

/* 5940 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5942 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5946 */	NdrFcShort( 0x7 ),	/* 7 */
/* 5948 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 5950 */	NdrFcShort( 0x18 ),	/* 24 */
/* 5952 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5954 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 5956 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 5958 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5960 */	NdrFcShort( 0x0 ),	/* 0 */
/* 5962 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tpt */

/* 5964 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5966 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 5968 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nVar */

/* 5970 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5972 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 5974 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nVal */

/* 5976 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 5978 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 5980 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 5982 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 5984 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5986 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetStrPropValue */

/* 5988 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 5990 */	NdrFcLong( 0x0 ),	/* 0 */
/* 5994 */	NdrFcShort( 0x8 ),	/* 8 */
/* 5996 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 5998 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6000 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6002 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6004 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 6006 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6008 */	NdrFcShort( 0x1 ),	/* 1 */
/* 6010 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tpt */

/* 6012 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6014 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6016 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrVal */

/* 6018 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 6020 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6022 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 6024 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6026 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6028 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetString */

/* 6030 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6032 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6036 */	NdrFcShort( 0x9 ),	/* 9 */
/* 6038 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6040 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6042 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6044 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 6046 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6048 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6050 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6052 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptss */

/* 6054 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6056 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6058 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 6060 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6062 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6064 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FlushIgnoreList */


	/* Procedure Remind */


	/* Procedure Clear */

/* 6066 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6068 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6072 */	NdrFcShort( 0xa ),	/* 10 */
/* 6074 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6076 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6078 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6080 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 6082 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6084 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6086 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6088 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 6090 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6092 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6094 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIncBldrClsid */

/* 6096 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6098 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6102 */	NdrFcShort( 0xb ),	/* 11 */
/* 6104 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6106 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6108 */	NdrFcShort( 0x4c ),	/* 76 */
/* 6110 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 6112 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6114 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6118 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pclsid */

/* 6120 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 6122 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6124 */	NdrFcShort( 0x11e ),	/* Type Offset=286 */

	/* Return value */

/* 6126 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6128 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6130 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SerializeFmt */

/* 6132 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6134 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6138 */	NdrFcShort( 0xc ),	/* 12 */
/* 6140 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6142 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6144 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6146 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 6148 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6152 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6154 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 6156 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6158 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6160 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Return value */

/* 6162 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6164 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6166 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SerializeFmtRgb */

/* 6168 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6170 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6174 */	NdrFcShort( 0xd ),	/* 13 */
/* 6176 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6178 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6180 */	NdrFcShort( 0x24 ),	/* 36 */
/* 6182 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 6184 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 6186 */	NdrFcShort( 0x1 ),	/* 1 */
/* 6188 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6190 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgb */

/* 6192 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 6194 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6196 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cbMax */

/* 6198 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6200 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6202 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcbNeeded */

/* 6204 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6206 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6208 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6210 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6212 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6214 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetIntPropValues */

/* 6216 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6218 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6222 */	NdrFcShort( 0x9 ),	/* 9 */
/* 6224 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6226 */	NdrFcShort( 0x18 ),	/* 24 */
/* 6228 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6230 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 6232 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6234 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6236 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6238 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tpt */

/* 6240 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6242 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6244 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nVar */

/* 6246 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6248 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6250 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nVal */

/* 6252 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6254 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6256 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6258 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6260 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6262 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetStrPropValue */

/* 6264 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6266 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6270 */	NdrFcShort( 0xa ),	/* 10 */
/* 6272 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6274 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6276 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6278 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6280 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 6282 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6284 */	NdrFcShort( 0x1 ),	/* 1 */
/* 6286 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tpt */

/* 6288 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6290 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6292 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrVal */

/* 6294 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 6296 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6298 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 6300 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6302 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6304 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetStrPropValueRgch */

/* 6306 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6308 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6312 */	NdrFcShort( 0xb ),	/* 11 */
/* 6314 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6316 */	NdrFcShort( 0x10 ),	/* 16 */
/* 6318 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6320 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 6322 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 6324 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6326 */	NdrFcShort( 0x1 ),	/* 1 */
/* 6328 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter tpt */

/* 6330 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6332 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6334 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rgchVal */

/* 6336 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6338 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6340 */	NdrFcShort( 0x190 ),	/* Type Offset=400 */

	/* Parameter nValLength */

/* 6342 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6344 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6346 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 6348 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6350 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetTextProps */

/* 6354 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6356 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6360 */	NdrFcShort( 0xc ),	/* 12 */
/* 6362 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6364 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6366 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6368 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 6370 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6374 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6376 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppttp */

/* 6378 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6380 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6382 */	NdrFcShort( 0xd2 ),	/* Type Offset=210 */

	/* Return value */

/* 6384 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6386 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6388 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetStringFromIndex */

/* 6390 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6392 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6396 */	NdrFcShort( 0x4 ),	/* 4 */
/* 6398 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6400 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6402 */	NdrFcShort( 0x24 ),	/* 36 */
/* 6404 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 6406 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6408 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6410 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6412 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iws */

/* 6414 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6416 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */

/* 6420 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6422 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6424 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 6426 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6428 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6430 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 6432 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6434 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6436 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_String */

/* 6438 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6440 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6444 */	NdrFcShort( 0x5 ),	/* 5 */
/* 6446 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6448 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6450 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6452 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 6454 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6456 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6458 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6460 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 6462 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6464 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6466 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 6468 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6470 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6472 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 6474 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6476 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6478 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_String */

/* 6480 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6482 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6486 */	NdrFcShort( 0x6 ),	/* 6 */
/* 6488 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6490 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6492 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6494 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6496 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6498 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6500 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6502 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 6504 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6506 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 6510 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6512 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6514 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 6516 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6518 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6520 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Stream */

/* 6522 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6524 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6528 */	NdrFcShort( 0x3 ),	/* 3 */
/* 6530 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6532 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6534 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6536 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 6538 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6540 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6542 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6544 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppstrm */

/* 6546 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6548 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6550 */	NdrFcShort( 0x23e ),	/* Type Offset=574 */

	/* Return value */

/* 6552 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6554 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6556 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Key */


	/* Procedure get_InitializationData */


	/* Procedure get_Contents */

/* 6558 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6560 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6564 */	NdrFcShort( 0x4 ),	/* 4 */
/* 6566 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6568 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6570 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6572 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 6574 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 6576 */	NdrFcShort( 0x1 ),	/* 1 */
/* 6578 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6580 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrKey */


	/* Parameter pbstr */


	/* Parameter pbstr */

/* 6582 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 6584 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6586 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 6588 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6590 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteTssAsXml */

/* 6594 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6596 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6600 */	NdrFcShort( 0x6 ),	/* 6 */
/* 6602 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 6604 */	NdrFcShort( 0x16 ),	/* 22 */
/* 6606 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6608 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 6610 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6612 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6614 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6616 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptss */

/* 6618 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6620 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6622 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Parameter pwsf */

/* 6624 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6626 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6628 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Parameter cchIndent */

/* 6630 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6632 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6634 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ws */

/* 6636 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6638 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6640 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fWriteObjData */

/* 6642 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6644 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6646 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 6648 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6650 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 6652 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReadTssFromXml */

/* 6654 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6656 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6660 */	NdrFcShort( 0x7 ),	/* 7 */
/* 6662 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6664 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6666 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6668 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6670 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6672 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6674 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6676 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 6678 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 6680 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6682 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Parameter pptss */

/* 6684 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6686 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6688 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 6690 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6692 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetLogPointer_Bkupd */

/* 6696 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6698 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6702 */	NdrFcShort( 0x4 ),	/* 4 */
/* 6704 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6706 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6708 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6710 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 6712 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6714 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6716 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6718 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppfist */

/* 6720 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 6722 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6724 */	NdrFcShort( 0x23e ),	/* Type Offset=574 */

	/* Return value */

/* 6726 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6728 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6730 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SaveAllData_Bkupd */

/* 6732 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6734 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6738 */	NdrFcShort( 0x5 ),	/* 5 */
/* 6740 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6742 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6744 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6746 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6748 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6750 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6754 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszServer */

/* 6756 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6758 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6760 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter pszDbName */

/* 6762 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6764 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6766 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Return value */

/* 6768 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6770 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6772 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CloseDbAndWindows_Bkupd */

/* 6774 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6776 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6780 */	NdrFcShort( 0x6 ),	/* 6 */
/* 6782 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 6784 */	NdrFcShort( 0x6 ),	/* 6 */
/* 6786 */	NdrFcShort( 0x22 ),	/* 34 */
/* 6788 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 6790 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6792 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6794 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6796 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszSvrName */

/* 6798 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6800 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6802 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter pszDbName */

/* 6804 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6806 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6808 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter fOkToClose */

/* 6810 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 6812 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6814 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfWindowsClosed */

/* 6816 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6818 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6820 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 6822 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6824 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IncExportedObjects_Bkupd */

/* 6828 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6830 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6834 */	NdrFcShort( 0x7 ),	/* 7 */
/* 6836 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6840 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6842 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 6844 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6846 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6848 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6850 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 6852 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6854 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6856 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RollbackTrans */


	/* Procedure CheckForMissedSchedules */


	/* Procedure DecExportedObjects_Bkupd */

/* 6858 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6860 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6864 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6866 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6868 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6870 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6872 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 6874 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6876 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6878 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6880 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 6882 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6884 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6886 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CheckDbVerCompatibility_Bkupd */

/* 6888 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6890 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6894 */	NdrFcShort( 0x9 ),	/* 9 */
/* 6896 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 6898 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6900 */	NdrFcShort( 0x22 ),	/* 34 */
/* 6902 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 6904 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6906 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6908 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6910 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszSvrName */

/* 6912 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6914 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6916 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter pszDbName */

/* 6918 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6920 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6922 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter pfCompatible */

/* 6924 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 6926 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6928 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 6930 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6932 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ReopenDbAndOneWindow_Bkupd */

/* 6936 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6938 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6942 */	NdrFcShort( 0xa ),	/* 10 */
/* 6944 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6948 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6950 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6952 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6954 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6956 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6958 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszSvrName */

/* 6960 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6962 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 6964 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter pszDbName */

/* 6966 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 6968 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 6970 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Return value */

/* 6972 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 6974 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 6976 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 6978 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 6980 */	NdrFcLong( 0x0 ),	/* 0 */
/* 6984 */	NdrFcShort( 0x7 ),	/* 7 */
/* 6986 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 6988 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6990 */	NdrFcShort( 0x8 ),	/* 8 */
/* 6992 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 6994 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 6996 */	NdrFcShort( 0x0 ),	/* 0 */
/* 6998 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7000 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbkupd */

/* 7002 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7004 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7006 */	NdrFcShort( 0x246 ),	/* Type Offset=582 */

	/* Parameter hwndParent */

/* 7008 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7010 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7012 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7014 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7016 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7018 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Backup */

/* 7020 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7022 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7026 */	NdrFcShort( 0x9 ),	/* 9 */
/* 7028 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7030 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7032 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7034 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 7036 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7038 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7040 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7042 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 7044 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7046 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure UserConfigure */

/* 7050 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7052 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7056 */	NdrFcShort( 0xb ),	/* 11 */
/* 7058 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7060 */	NdrFcShort( 0x6 ),	/* 6 */
/* 7062 */	NdrFcShort( 0x24 ),	/* 36 */
/* 7064 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 7066 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7070 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7072 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phtprovHelpUrls */

/* 7074 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7076 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7078 */	NdrFcShort( 0x258 ),	/* Type Offset=600 */

	/* Parameter fShowRestore */

/* 7080 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7082 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7084 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pnUserAction */

/* 7086 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7088 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7090 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7092 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7094 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 7098 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7100 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7104 */	NdrFcShort( 0x3 ),	/* 3 */
/* 7106 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 7108 */	NdrFcShort( 0xe ),	/* 14 */
/* 7110 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7112 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 7114 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7118 */	NdrFcShort( 0x5 ),	/* 5 */
/* 7120 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDatabase */

/* 7122 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7124 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7126 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrServer */

/* 7128 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7130 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7132 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrReason */

/* 7134 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7136 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7138 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrExternalReason */

/* 7140 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7142 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7144 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter fConfirmCancel */

/* 7146 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7148 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7150 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter bstrCancelQuestion */

/* 7152 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7154 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7156 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter hwndParent */

/* 7158 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7160 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7162 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7164 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7166 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 7168 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DisconnectAll */

/* 7170 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7172 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7176 */	NdrFcShort( 0x5 ),	/* 5 */
/* 7178 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7180 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7182 */	NdrFcShort( 0x22 ),	/* 34 */
/* 7184 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 7186 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7188 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7190 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7192 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfResult */

/* 7194 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7196 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7198 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7200 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7202 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7204 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WarnSimple */

/* 7206 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7208 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7212 */	NdrFcShort( 0x3 ),	/* 3 */
/* 7214 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7216 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7218 */	NdrFcShort( 0x24 ),	/* 36 */
/* 7220 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 7222 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7224 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7226 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7228 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrMessage */

/* 7230 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7232 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7234 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter nFlags */

/* 7236 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7238 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7240 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnResponse */

/* 7242 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7244 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7248 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7250 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7252 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ExecCommand */


	/* Procedure WarnWithTimeout */

/* 7254 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7256 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7260 */	NdrFcShort( 0x4 ),	/* 4 */
/* 7262 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7264 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7266 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7268 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 7270 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7272 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7274 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7276 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSqlStatement */


	/* Parameter bstrMessage */

/* 7278 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7280 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7282 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter nStatementType */


	/* Parameter nTimeLeft */

/* 7284 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7286 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7288 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 7290 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7292 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7294 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */


	/* Procedure Init */


	/* Procedure Setup */


	/* Procedure BeginTrans */


	/* Procedure PermitRemoteWarnings */

/* 7296 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7298 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7302 */	NdrFcShort( 0x3 ),	/* 3 */
/* 7304 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7306 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7308 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7310 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 7312 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7314 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7318 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 7320 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7322 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7324 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetColValue */

/* 7326 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7328 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7332 */	NdrFcShort( 0x5 ),	/* 5 */
/* 7334 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 7336 */	NdrFcShort( 0x18 ),	/* 24 */
/* 7338 */	NdrFcShort( 0x3e ),	/* 62 */
/* 7340 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x7,		/* 7 */
/* 7342 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 7344 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7346 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7348 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluColIndex */

/* 7350 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7352 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7354 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluDataBuffer */

/* 7356 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 7358 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7360 */	NdrFcShort( 0x26e ),	/* Type Offset=622 */

	/* Parameter cbBufferLength */

/* 7362 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7364 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7366 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcbAmtBuffUsed */

/* 7368 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7370 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7372 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfIsNull */

/* 7374 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7376 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7378 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter cbPad */

/* 7380 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7382 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7384 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7386 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7388 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7390 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetParameter */

/* 7392 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7394 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7398 */	NdrFcShort( 0x7 ),	/* 7 */
/* 7400 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7402 */	NdrFcShort( 0x10 ),	/* 16 */
/* 7404 */	NdrFcShort( 0x22 ),	/* 34 */
/* 7406 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 7408 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 7410 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7412 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7414 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluColIndex */

/* 7416 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7418 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7420 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluDataBuffer */

/* 7422 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 7424 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7426 */	NdrFcShort( 0x26e ),	/* Type Offset=622 */

	/* Parameter cluBufferLength */

/* 7428 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7430 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7432 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfIsNull */

/* 7434 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7436 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7438 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7440 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7442 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7444 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetRowset */

/* 7446 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7448 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7452 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7454 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7456 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7458 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7460 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 7462 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7464 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7466 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7468 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nRowsBuffered */

/* 7470 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7472 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7474 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7476 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7478 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7480 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 7482 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7484 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7488 */	NdrFcShort( 0x9 ),	/* 9 */
/* 7490 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7492 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7494 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7496 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 7498 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7500 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7502 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7504 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter punkSession */

/* 7506 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7508 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7510 */	NdrFcShort( 0x258 ),	/* Type Offset=600 */

	/* Parameter pfistLog */

/* 7512 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7514 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7516 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Return value */

/* 7518 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7520 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7522 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NextRow */

/* 7524 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7526 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7530 */	NdrFcShort( 0xa ),	/* 10 */
/* 7532 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7534 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7536 */	NdrFcShort( 0x22 ),	/* 34 */
/* 7538 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 7540 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7542 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7544 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7546 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfMoreRows */

/* 7548 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7550 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7552 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7554 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7556 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7558 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetParameter */

/* 7560 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7562 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7566 */	NdrFcShort( 0xb ),	/* 11 */
/* 7568 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 7570 */	NdrFcShort( 0x1e ),	/* 30 */
/* 7572 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7574 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 7576 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7578 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7580 */	NdrFcShort( 0x2 ),	/* 2 */
/* 7582 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluParamIndex */

/* 7584 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7586 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7588 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dwFlags */

/* 7590 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7592 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7594 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrParamName */

/* 7596 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7598 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7600 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter nDataType */

/* 7602 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7604 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7606 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter prgluDataBuffer */

/* 7608 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 7610 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7612 */	NdrFcShort( 0x27e ),	/* Type Offset=638 */

	/* Parameter cluBufferLength */

/* 7614 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7616 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7618 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7620 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7622 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7624 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetByteBuffParameter */

/* 7626 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7628 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7632 */	NdrFcShort( 0xc ),	/* 12 */
/* 7634 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7636 */	NdrFcShort( 0x18 ),	/* 24 */
/* 7638 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7640 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 7642 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7646 */	NdrFcShort( 0x2 ),	/* 2 */
/* 7648 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluParamIndex */

/* 7650 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7652 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7654 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dwFlags */

/* 7656 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7658 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7660 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrParamName */

/* 7662 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7664 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7666 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter prgbDataBuffer */

/* 7668 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 7670 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7672 */	NdrFcShort( 0x28e ),	/* Type Offset=654 */

	/* Parameter cluBufferLength */

/* 7674 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7676 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7680 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7682 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7684 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetStringParameter */

/* 7686 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7688 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7692 */	NdrFcShort( 0xd ),	/* 13 */
/* 7694 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7696 */	NdrFcShort( 0x18 ),	/* 24 */
/* 7698 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7700 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 7702 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7704 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7706 */	NdrFcShort( 0x2 ),	/* 2 */
/* 7708 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iluParamIndex */

/* 7710 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7712 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dwFlags */

/* 7716 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7718 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrParamName */

/* 7722 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7724 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7726 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter prgchDataBuffer */

/* 7728 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 7730 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7732 */	NdrFcShort( 0x29e ),	/* Type Offset=670 */

	/* Parameter cluBufferLength */

/* 7734 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7736 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7738 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7740 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7742 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7744 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CreateCommand */

/* 7746 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7748 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7752 */	NdrFcShort( 0x5 ),	/* 5 */
/* 7754 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7756 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7758 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7760 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 7762 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7768 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppodc */

/* 7770 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 7772 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7774 */	NdrFcShort( 0x2aa ),	/* Type Offset=682 */

	/* Return value */

/* 7776 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7778 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 7782 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7784 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7788 */	NdrFcShort( 0x6 ),	/* 6 */
/* 7790 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 7792 */	NdrFcShort( 0x10 ),	/* 16 */
/* 7794 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7796 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 7798 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7800 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7802 */	NdrFcShort( 0x2 ),	/* 2 */
/* 7804 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrServer */

/* 7806 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7808 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7810 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrDatabase */

/* 7812 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7814 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7816 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pfistLog */

/* 7818 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7820 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7822 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter olt */

/* 7824 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7826 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7828 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter nmsTimeout */

/* 7830 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7832 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 7834 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 7836 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7838 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 7840 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsTransactionOpen */

/* 7842 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7844 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7848 */	NdrFcShort( 0x7 ),	/* 7 */
/* 7850 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7852 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7854 */	NdrFcShort( 0x22 ),	/* 34 */
/* 7856 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 7858 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7860 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7862 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7864 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfTransactionOpen */

/* 7866 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 7868 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7870 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7872 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7874 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7876 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RollbackSavePoint */

/* 7878 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7880 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7884 */	NdrFcShort( 0x9 ),	/* 9 */
/* 7886 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7888 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7890 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7892 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 7894 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 7896 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7898 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7900 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSavePoint */

/* 7902 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 7904 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7906 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 7908 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7910 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7912 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FwTemplateDir */


	/* Procedure SetSavePointOrBeginTrans */

/* 7914 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7916 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7920 */	NdrFcShort( 0xb ),	/* 11 */
/* 7922 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7926 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7928 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 7930 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 7932 */	NdrFcShort( 0x1 ),	/* 1 */
/* 7934 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7936 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */


	/* Parameter pbstr */

/* 7938 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 7940 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7942 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 7944 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7946 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7948 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InitMSDE */

/* 7950 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7952 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7956 */	NdrFcShort( 0xc ),	/* 12 */
/* 7958 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 7960 */	NdrFcShort( 0x6 ),	/* 6 */
/* 7962 */	NdrFcShort( 0x8 ),	/* 8 */
/* 7964 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 7966 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 7968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7970 */	NdrFcShort( 0x0 ),	/* 0 */
/* 7972 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfistLog */

/* 7974 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 7976 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 7978 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter fForce */

/* 7980 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 7982 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 7984 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 7986 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 7988 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 7990 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Server */

/* 7992 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 7994 */	NdrFcLong( 0x0 ),	/* 0 */
/* 7998 */	NdrFcShort( 0xd ),	/* 13 */
/* 8000 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8002 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8004 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8006 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 8008 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8010 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8012 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8014 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrSvr */

/* 8016 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 8018 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8020 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 8022 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8024 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8026 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFreeLogKb */

/* 8028 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8030 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8034 */	NdrFcShort( 0xf ),	/* 15 */
/* 8036 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8038 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8040 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8042 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8044 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8046 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8048 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8050 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nReservespace */

/* 8052 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8054 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8056 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnKbFree */

/* 8058 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8060 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8062 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8064 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8066 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8068 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 8070 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8072 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8076 */	NdrFcShort( 0x3 ),	/* 3 */
/* 8078 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8080 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8082 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8084 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 8086 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8088 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8090 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8092 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 8094 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8096 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8098 */	NdrFcShort( 0x2c0 ),	/* Type Offset=704 */

	/* Return value */

/* 8100 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8102 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8104 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Reload */

/* 8106 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8108 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8112 */	NdrFcShort( 0x4 ),	/* 4 */
/* 8114 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8116 */	NdrFcShort( 0x6 ),	/* 6 */
/* 8118 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8120 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8122 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8124 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8126 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8128 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 8130 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 8132 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8134 */	NdrFcShort( 0x2c0 ),	/* Type Offset=704 */

	/* Parameter fKeepVirtuals */

/* 8136 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8138 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8140 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 8142 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8144 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8146 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InitXml */

/* 8148 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8150 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8154 */	NdrFcShort( 0x5 ),	/* 5 */
/* 8156 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8158 */	NdrFcShort( 0x6 ),	/* 6 */
/* 8160 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8162 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8164 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 8166 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8168 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8170 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrPathname */

/* 8172 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 8174 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8176 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter fClearPrevCache */

/* 8178 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8180 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8182 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 8184 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8186 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8188 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldIds */

/* 8190 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8192 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8196 */	NdrFcShort( 0x7 ),	/* 7 */
/* 8198 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8200 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8202 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8204 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 8206 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8208 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8210 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8212 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cflid */

/* 8214 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8216 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8218 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rgflid */

/* 8220 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 8222 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8224 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Return value */

/* 8226 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8228 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8230 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDstClsName */

/* 8232 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8234 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8238 */	NdrFcShort( 0x9 ),	/* 9 */
/* 8240 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8242 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8244 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8246 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 8248 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8250 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8252 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8254 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8256 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8258 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8260 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrDstClsName */

/* 8262 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 8264 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8266 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 8268 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8270 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8272 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetOwnClsId */

/* 8274 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8276 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8280 */	NdrFcShort( 0xa ),	/* 10 */
/* 8282 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8284 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8286 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8288 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8290 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8292 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8294 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8296 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8298 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8300 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8302 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pluOwnClsid */

/* 8304 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8306 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8308 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8310 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8312 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8314 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDstClsId */

/* 8316 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8318 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8322 */	NdrFcShort( 0xb ),	/* 11 */
/* 8324 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8326 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8328 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8330 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8332 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8336 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8338 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8340 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8342 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8344 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pluDstClsid */

/* 8346 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8348 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8350 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8352 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8354 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8356 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldHelp */

/* 8358 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8360 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8364 */	NdrFcShort( 0xe ),	/* 14 */
/* 8366 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8368 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8370 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8372 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 8374 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8376 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8378 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8380 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8382 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8384 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8386 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldHelp */

/* 8388 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 8390 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8392 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 8394 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8396 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8398 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldXml */

/* 8400 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8402 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8406 */	NdrFcShort( 0xf ),	/* 15 */
/* 8408 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8410 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8412 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8414 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 8416 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8418 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8420 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8422 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8424 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8426 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8428 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrFieldXml */

/* 8430 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 8432 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8434 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 8436 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8438 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8440 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldListRoot */

/* 8442 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8444 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8448 */	NdrFcShort( 0x10 ),	/* 16 */
/* 8450 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8452 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8454 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8456 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8458 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8460 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8462 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8464 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8466 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8468 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter piListRoot */

/* 8472 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8474 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8476 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8478 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8480 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8482 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldWs */

/* 8484 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8486 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8490 */	NdrFcShort( 0x11 ),	/* 17 */
/* 8492 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8494 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8496 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8498 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8500 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8502 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8504 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8506 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8508 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8510 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8512 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter piWs */

/* 8514 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8516 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8518 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8520 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8522 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8524 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WritingSystem */


	/* Procedure GetFieldType */

/* 8526 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8528 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8532 */	NdrFcShort( 0x12 ),	/* 18 */
/* 8534 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8536 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8538 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8540 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8542 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8544 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8546 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8548 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */


	/* Parameter luFlid */

/* 8550 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8552 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8554 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pws */


	/* Parameter piType */

/* 8556 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8558 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8560 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 8562 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8564 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8566 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsValidClass */

/* 8568 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8570 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8574 */	NdrFcShort( 0x13 ),	/* 19 */
/* 8576 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8578 */	NdrFcShort( 0x10 ),	/* 16 */
/* 8580 */	NdrFcShort( 0x22 ),	/* 34 */
/* 8582 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 8584 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8588 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8590 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luFlid */

/* 8592 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8594 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8596 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter luClid */

/* 8598 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8600 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8602 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfValid */

/* 8604 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8606 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8608 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 8610 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8612 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8614 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetClassIds */

/* 8616 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8618 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8622 */	NdrFcShort( 0x15 ),	/* 21 */
/* 8624 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8626 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8628 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8630 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 8632 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8634 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8636 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8638 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cclid */

/* 8640 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8642 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8644 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rgclid */

/* 8646 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 8648 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8650 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Return value */

/* 8652 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8654 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8656 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetClassName */

/* 8658 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8660 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8664 */	NdrFcShort( 0x16 ),	/* 22 */
/* 8666 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8668 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8670 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8672 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 8674 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8676 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8678 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8680 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 8682 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8684 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8686 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrClassName */

/* 8688 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 8690 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8692 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 8694 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8696 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8698 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetAbstract */

/* 8700 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8702 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8706 */	NdrFcShort( 0x17 ),	/* 23 */
/* 8708 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8710 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8712 */	NdrFcShort( 0x22 ),	/* 34 */
/* 8714 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8716 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8718 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8720 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8722 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 8724 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8726 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8728 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfAbstract */

/* 8730 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8732 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8734 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 8736 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8738 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8740 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ToTitleCh */


	/* Procedure GetBaseClsId */

/* 8742 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8744 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8748 */	NdrFcShort( 0x18 ),	/* 24 */
/* 8750 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8752 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8754 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8756 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 8758 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 8760 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8762 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8764 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */


	/* Parameter luClid */

/* 8766 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8768 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8770 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pch */


	/* Parameter pluClid */

/* 8772 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8774 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8776 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 8778 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8780 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8782 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetBaseClsName */

/* 8784 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8786 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8790 */	NdrFcShort( 0x19 ),	/* 25 */
/* 8792 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8794 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8796 */	NdrFcShort( 0x8 ),	/* 8 */
/* 8798 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 8800 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8802 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8804 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8806 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 8808 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8810 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8812 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrBaseClsName */

/* 8814 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 8816 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8818 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 8820 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8822 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8824 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFields */

/* 8826 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8828 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8832 */	NdrFcShort( 0x1a ),	/* 26 */
/* 8834 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 8836 */	NdrFcShort( 0x1e ),	/* 30 */
/* 8838 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8840 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x7,		/* 7 */
/* 8842 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 8844 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8846 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8848 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 8850 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8852 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8854 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fIncludeSuperclasses */

/* 8856 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8858 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8860 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter grfcpt */

/* 8862 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8864 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8866 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cflidMax */

/* 8868 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8870 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8872 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgflid */

/* 8874 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 8876 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8878 */	NdrFcShort( 0x2d6 ),	/* Type Offset=726 */

	/* Parameter pcflid */

/* 8880 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8882 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8884 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8886 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8888 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 8890 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetClassId */

/* 8892 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8894 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8898 */	NdrFcShort( 0x1b ),	/* 27 */
/* 8900 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8904 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8906 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 8908 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 8910 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8912 */	NdrFcShort( 0x1 ),	/* 1 */
/* 8914 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrClassName */

/* 8916 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 8918 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8920 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pluClid */

/* 8922 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8924 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8926 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8928 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8930 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8932 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldId */

/* 8934 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8936 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8940 */	NdrFcShort( 0x1c ),	/* 28 */
/* 8942 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8944 */	NdrFcShort( 0x6 ),	/* 6 */
/* 8946 */	NdrFcShort( 0x24 ),	/* 36 */
/* 8948 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 8950 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 8952 */	NdrFcShort( 0x0 ),	/* 0 */
/* 8954 */	NdrFcShort( 0x2 ),	/* 2 */
/* 8956 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrClassName */

/* 8958 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 8960 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 8962 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrFieldName */

/* 8964 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 8966 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 8968 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter fIncludeBaseClasses */

/* 8970 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 8972 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 8974 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pluFlid */

/* 8976 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 8978 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 8980 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 8982 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 8984 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 8986 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFieldId2 */

/* 8988 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 8990 */	NdrFcLong( 0x0 ),	/* 0 */
/* 8994 */	NdrFcShort( 0x1d ),	/* 29 */
/* 8996 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 8998 */	NdrFcShort( 0xe ),	/* 14 */
/* 9000 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9002 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 9004 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9006 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9008 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9010 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 9012 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9014 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9016 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrFieldName */

/* 9018 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9020 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9022 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter fIncludeBaseClasses */

/* 9024 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9026 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9028 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pluFlid */

/* 9030 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9032 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9034 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9036 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9038 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9040 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetDirectSubclasses */

/* 9042 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9044 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9048 */	NdrFcShort( 0x1e ),	/* 30 */
/* 9050 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9052 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9054 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9056 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 9058 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 9060 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9062 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9064 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 9066 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9068 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9070 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cluMax */

/* 9072 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9074 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9076 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcluOut */

/* 9078 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9080 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9082 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluSubclasses */

/* 9084 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 9086 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9088 */	NdrFcShort( 0x2e6 ),	/* Type Offset=742 */

	/* Return value */

/* 9090 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9092 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9094 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetAllSubclasses */

/* 9096 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9098 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9102 */	NdrFcShort( 0x1f ),	/* 31 */
/* 9104 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9106 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9108 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9110 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 9112 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 9114 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9118 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter luClid */

/* 9120 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9122 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cluMax */

/* 9126 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9128 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9130 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcluOut */

/* 9132 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9134 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9136 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgluSubclasses */

/* 9138 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 9140 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9142 */	NdrFcShort( 0x2e6 ),	/* Type Offset=742 */

	/* Return value */

/* 9144 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9146 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9148 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddVirtualProp */

/* 9150 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9152 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9156 */	NdrFcShort( 0x20 ),	/* 32 */
/* 9158 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9160 */	NdrFcShort( 0x10 ),	/* 16 */
/* 9162 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9164 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 9166 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9168 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9170 */	NdrFcShort( 0x2 ),	/* 2 */
/* 9172 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrClass */

/* 9174 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9176 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9178 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrField */

/* 9180 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9182 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9184 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter luFlid */

/* 9186 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9188 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter type */

/* 9192 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9194 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9198 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9200 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9202 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AttachDatabase */

/* 9204 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9206 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9210 */	NdrFcShort( 0x4 ),	/* 4 */
/* 9212 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9214 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9216 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9218 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 9220 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9222 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9224 */	NdrFcShort( 0x2 ),	/* 2 */
/* 9226 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDatabaseName */

/* 9228 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9230 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9232 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrPathName */

/* 9234 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9236 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9238 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 9240 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9242 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9244 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RenameDatabase */

/* 9246 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9248 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9252 */	NdrFcShort( 0x6 ),	/* 6 */
/* 9254 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 9256 */	NdrFcShort( 0xc ),	/* 12 */
/* 9258 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9260 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 9262 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9264 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9266 */	NdrFcShort( 0x3 ),	/* 3 */
/* 9268 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrDirName */

/* 9270 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9272 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9274 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrOldName */

/* 9276 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9278 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9280 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrNewName */

/* 9282 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 9284 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9286 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter fDetachBefore */

/* 9288 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9290 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9292 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fAttachAfter */

/* 9294 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9296 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9298 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 9300 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9302 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9304 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_LogStream */

/* 9306 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9308 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9312 */	NdrFcShort( 0x7 ),	/* 7 */
/* 9314 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9318 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9320 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 9322 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9324 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9326 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9328 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 9330 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 9332 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9334 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Return value */

/* 9336 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9338 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9340 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FwRootDir */

/* 9342 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9344 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9348 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9350 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9352 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9354 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9356 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 9358 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 9360 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9362 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9364 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 9366 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 9368 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9370 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 9372 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9374 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9376 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FwMigrationScriptDir */

/* 9378 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9380 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9384 */	NdrFcShort( 0x9 ),	/* 9 */
/* 9386 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9390 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9392 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 9394 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 9396 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9398 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9400 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 9402 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 9404 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9406 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 9408 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9410 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9412 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InitNew */

/* 9414 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9416 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9420 */	NdrFcShort( 0x3 ),	/* 3 */
/* 9422 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9424 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9426 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9428 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 9430 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9432 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9434 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9436 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgb */

/* 9438 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9440 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9442 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cb */

/* 9444 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9446 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9448 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9450 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9452 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9454 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InvertRect */

/* 9456 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9458 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9462 */	NdrFcShort( 0x3 ),	/* 3 */
/* 9464 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9466 */	NdrFcShort( 0x20 ),	/* 32 */
/* 9468 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9470 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 9472 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9474 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9476 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9478 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xLeft */

/* 9480 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9482 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9484 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yTop */

/* 9486 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9488 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9490 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter xRight */

/* 9492 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9494 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yBottom */

/* 9498 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9500 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9504 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9506 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetGlyphAttributeInt */


	/* Procedure DrawRectangle */

/* 9510 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9512 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9516 */	NdrFcShort( 0x6 ),	/* 6 */
/* 9518 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9520 */	NdrFcShort( 0x20 ),	/* 32 */
/* 9522 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9524 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 9526 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9528 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9530 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9532 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iGlyph */


	/* Parameter xLeft */

/* 9534 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9536 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter kjgatId */


	/* Parameter yTop */

/* 9540 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9542 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9544 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nLevel */


	/* Parameter xRight */

/* 9546 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9548 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9550 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter value */


	/* Parameter yBottom */

/* 9552 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9554 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9556 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 9558 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9560 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9562 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawHorzLine */

/* 9564 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9566 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9570 */	NdrFcShort( 0x7 ),	/* 7 */
/* 9572 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 9574 */	NdrFcShort( 0x44 ),	/* 68 */
/* 9576 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9578 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 9580 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9582 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9584 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9586 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xLeft */

/* 9588 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9590 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter xRight */

/* 9594 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9596 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter y */

/* 9600 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9602 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9604 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dyHeight */

/* 9606 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9608 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9610 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cdx */

/* 9612 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9614 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9616 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgdx */

/* 9618 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9620 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9622 */	NdrFcShort( 0x2fc ),	/* Type Offset=764 */

	/* Parameter pdxStart */

/* 9624 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 9626 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 9628 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9630 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9632 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 9634 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawLine */

/* 9636 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9638 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9642 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9644 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9646 */	NdrFcShort( 0x20 ),	/* 32 */
/* 9648 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9650 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 9652 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9654 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9656 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9658 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xLeft */

/* 9660 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9662 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yTop */

/* 9666 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9668 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9670 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter xRight */

/* 9672 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9674 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9676 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter yBottom */

/* 9678 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9680 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9682 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9684 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9686 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9688 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawText */

/* 9690 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9692 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9696 */	NdrFcShort( 0x9 ),	/* 9 */
/* 9698 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 9700 */	NdrFcShort( 0x20 ),	/* 32 */
/* 9702 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9704 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 9706 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9708 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9710 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9712 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter x */

/* 9714 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9716 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9718 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter y */

/* 9720 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9722 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9724 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cch */

/* 9726 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9728 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9730 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 9732 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9734 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9736 */	NdrFcShort( 0x30c ),	/* Type Offset=780 */

	/* Parameter xStretch */

/* 9738 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9740 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9742 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9744 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9746 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9748 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawTextExt */

/* 9750 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9752 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9756 */	NdrFcShort( 0xa ),	/* 10 */
/* 9758 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 9760 */	NdrFcShort( 0x70 ),	/* 112 */
/* 9762 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9764 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 9766 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9768 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9770 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9772 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter x */

/* 9774 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9776 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9778 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter y */

/* 9780 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9782 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9784 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cch */

/* 9786 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9788 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9790 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchw */

/* 9792 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9794 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9796 */	NdrFcShort( 0x30c ),	/* Type Offset=780 */

	/* Parameter uOptions */

/* 9798 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9800 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9802 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prect */

/* 9804 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 9806 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9808 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter prgdx */

/* 9810 */	NdrFcShort( 0x148 ),	/* Flags:  in, base type, simple ref, */
/* 9812 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 9814 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9816 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9818 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 9820 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetTextExtent */

/* 9822 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9824 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9828 */	NdrFcShort( 0xb ),	/* 11 */
/* 9830 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9832 */	NdrFcShort( 0x8 ),	/* 8 */
/* 9834 */	NdrFcShort( 0x40 ),	/* 64 */
/* 9836 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 9838 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9840 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9842 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9844 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cch */

/* 9846 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9848 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9850 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 9852 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9854 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9856 */	NdrFcShort( 0x32a ),	/* Type Offset=810 */

	/* Parameter px */

/* 9858 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9860 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9862 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter py */

/* 9864 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9866 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9868 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9870 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9872 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9874 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetTextLeadWidth */

/* 9876 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9878 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9882 */	NdrFcShort( 0xc ),	/* 12 */
/* 9884 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 9886 */	NdrFcShort( 0x18 ),	/* 24 */
/* 9888 */	NdrFcShort( 0x24 ),	/* 36 */
/* 9890 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 9892 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 9894 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9896 */	NdrFcShort( 0x1 ),	/* 1 */
/* 9898 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cch */

/* 9900 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9902 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9904 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 9906 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 9908 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9910 */	NdrFcShort( 0x32a ),	/* Type Offset=810 */

	/* Parameter ich */

/* 9912 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9914 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9916 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter xStretch */

/* 9918 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 9920 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9922 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter px */

/* 9924 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9926 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9928 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9930 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9932 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetClipRect */

/* 9936 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9938 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9942 */	NdrFcShort( 0xd ),	/* 13 */
/* 9944 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 9946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9948 */	NdrFcShort( 0x78 ),	/* 120 */
/* 9950 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 9952 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 9954 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9956 */	NdrFcShort( 0x0 ),	/* 0 */
/* 9958 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pxLeft */

/* 9960 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9962 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 9964 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pyTop */

/* 9966 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9968 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 9970 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pxRight */

/* 9972 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9974 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 9976 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pyBottom */

/* 9978 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 9980 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 9982 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 9984 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 9986 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 9988 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFontEmSquare */

/* 9990 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 9992 */	NdrFcLong( 0x0 ),	/* 0 */
/* 9996 */	NdrFcShort( 0xe ),	/* 14 */
/* 9998 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10000 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10002 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10004 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10006 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10008 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10010 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10012 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pxyFontEmSquare */

/* 10014 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10016 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10018 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10020 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10022 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10024 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetGlyphMetrics */

/* 10026 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10028 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10032 */	NdrFcShort( 0xf ),	/* 15 */
/* 10034 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 10036 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10038 */	NdrFcShort( 0xb0 ),	/* 176 */
/* 10040 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x8,		/* 8 */
/* 10042 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10044 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10046 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10048 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter chw */

/* 10050 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10052 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10054 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter psBoundingWidth */

/* 10056 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10058 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10060 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pyBoundingHeight */

/* 10062 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10064 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10066 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pxBoundingX */

/* 10068 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10070 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10072 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pyBoundingY */

/* 10074 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10076 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10078 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pxAdvanceX */

/* 10080 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10082 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10084 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pyAdvanceY */

/* 10086 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10088 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 10090 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10092 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10094 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 10096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFontData */

/* 10098 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10100 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10104 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10106 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10108 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10110 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10112 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 10114 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 10116 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10120 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nTableId */

/* 10122 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10124 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10126 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcbTableSz */

/* 10128 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10130 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10132 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrTableData */

/* 10134 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 10136 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10138 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 10140 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10142 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10144 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFontDataRgch */

/* 10146 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10148 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10152 */	NdrFcShort( 0x11 ),	/* 17 */
/* 10154 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10156 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10158 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10160 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 10162 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 10164 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10166 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10168 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nTableId */

/* 10170 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10172 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10174 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcbTableSz */

/* 10176 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10178 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10180 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 10182 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 10184 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10186 */	NdrFcShort( 0x33a ),	/* Type Offset=826 */

	/* Parameter cchMax */

/* 10188 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10190 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10192 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10194 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10196 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10198 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure XYFromGlyphPoint */

/* 10200 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10202 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10206 */	NdrFcShort( 0x12 ),	/* 18 */
/* 10208 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10210 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10212 */	NdrFcShort( 0x40 ),	/* 64 */
/* 10214 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 10216 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10218 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10220 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10222 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter chw */

/* 10224 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10226 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10228 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nPoint */

/* 10230 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10232 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10234 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pxRet */

/* 10236 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10238 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10240 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pyRet */

/* 10242 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10244 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10246 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10248 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10250 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10252 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FontAscent */

/* 10254 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10256 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10260 */	NdrFcShort( 0x13 ),	/* 19 */
/* 10262 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10264 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10266 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10268 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10270 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10272 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10274 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10276 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter py */

/* 10278 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10280 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10282 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10284 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10286 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10288 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FontCharProperties */

/* 10290 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10292 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10296 */	NdrFcShort( 0x15 ),	/* 21 */
/* 10298 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10300 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10302 */	NdrFcShort( 0x13c ),	/* 316 */
/* 10304 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10306 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10308 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10310 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10312 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pchrp */

/* 10314 */	NdrFcShort( 0x112 ),	/* Flags:  must free, out, simple ref, */
/* 10316 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10318 */	NdrFcShort( 0x356 ),	/* Type Offset=854 */

	/* Return value */

/* 10320 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10322 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10324 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_XUnitsPerInch */

/* 10326 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10328 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10332 */	NdrFcShort( 0x18 ),	/* 24 */
/* 10334 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10336 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10338 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10340 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10342 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10346 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10348 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter xInch */

/* 10350 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10352 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10354 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10356 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10358 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10360 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_YUnitsPerInch */

/* 10362 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10364 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10368 */	NdrFcShort( 0x1a ),	/* 26 */
/* 10370 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10372 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10374 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10376 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10378 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10380 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10382 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10384 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter yInch */

/* 10386 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10388 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10390 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10392 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10394 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10396 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetupGraphics */

/* 10398 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10400 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10404 */	NdrFcShort( 0x1b ),	/* 27 */
/* 10406 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10408 */	NdrFcShort( 0x134 ),	/* 308 */
/* 10410 */	NdrFcShort( 0x13c ),	/* 316 */
/* 10412 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10414 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10416 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10418 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10420 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pchrp */

/* 10422 */	NdrFcShort( 0x11a ),	/* Flags:  must free, in, out, simple ref, */
/* 10424 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10426 */	NdrFcShort( 0x356 ),	/* Type Offset=854 */

	/* Return value */

/* 10428 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10430 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10432 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PushClipRect */

/* 10434 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10436 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10440 */	NdrFcShort( 0x1c ),	/* 28 */
/* 10442 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10444 */	NdrFcShort( 0x20 ),	/* 32 */
/* 10446 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10448 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10450 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10452 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10454 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10456 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter rcClip */

/* 10458 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 10460 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10462 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Return value */

/* 10464 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10466 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10468 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PopClipRect */

/* 10470 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10472 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10476 */	NdrFcShort( 0x1d ),	/* 29 */
/* 10478 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10480 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10482 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10484 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 10486 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10488 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10490 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10492 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 10494 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10496 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10498 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawPolygon */

/* 10500 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10502 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10506 */	NdrFcShort( 0x1e ),	/* 30 */
/* 10508 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10510 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10512 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10514 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 10516 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 10518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10520 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10522 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cvpnt */

/* 10524 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10526 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10528 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgvpnt */

/* 10530 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10532 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10534 */	NdrFcShort( 0x378 ),	/* Type Offset=888 */

	/* Return value */

/* 10536 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10538 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10540 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RenderPicture */

/* 10542 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10544 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10548 */	NdrFcShort( 0x1f ),	/* 31 */
/* 10550 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 10552 */	NdrFcShort( 0x74 ),	/* 116 */
/* 10554 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10556 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xb,		/* 11 */
/* 10558 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10560 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10562 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10564 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppic */

/* 10566 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 10568 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10570 */	NdrFcShort( 0x388 ),	/* Type Offset=904 */

	/* Parameter x */

/* 10572 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10574 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10576 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter y */

/* 10578 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10580 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10582 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cx */

/* 10584 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10586 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10588 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cy */

/* 10590 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10592 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10594 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter xSrc */

/* 10596 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10598 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ySrc */

/* 10602 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10604 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 10606 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cxSrc */

/* 10608 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10610 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 10612 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cySrc */

/* 10614 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10616 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 10618 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prcWBounds */

/* 10620 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 10622 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 10624 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Return value */

/* 10626 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10628 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 10630 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure MakePicture */

/* 10632 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10634 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10638 */	NdrFcShort( 0x20 ),	/* 32 */
/* 10640 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10642 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10644 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10646 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 10648 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 10650 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10652 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10654 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbData */

/* 10656 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 10658 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10660 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cbData */

/* 10662 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10664 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10666 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pppic */

/* 10668 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 10670 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10672 */	NdrFcShort( 0x39a ),	/* Type Offset=922 */

	/* Return value */

/* 10674 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10676 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetClipRect */

/* 10680 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10682 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10686 */	NdrFcShort( 0x24 ),	/* 36 */
/* 10688 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10690 */	NdrFcShort( 0x34 ),	/* 52 */
/* 10692 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10694 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 10696 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10698 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10700 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10702 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prcClip */

/* 10704 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 10706 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10708 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Return value */

/* 10710 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10712 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10714 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Fetch */

/* 10716 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10718 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10722 */	NdrFcShort( 0x3 ),	/* 3 */
/* 10724 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10726 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10728 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10730 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 10732 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 10734 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10736 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10738 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichMin */

/* 10740 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10742 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10744 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 10746 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10748 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10750 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchBuf */

/* 10752 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 10754 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10756 */	NdrFcShort( 0x3a2 ),	/* Type Offset=930 */

	/* Return value */

/* 10758 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10760 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10762 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetCharProps */

/* 10764 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10766 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10770 */	NdrFcShort( 0x5 ),	/* 5 */
/* 10772 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10774 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10776 */	NdrFcShort( 0x174 ),	/* 372 */
/* 10778 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 10780 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10782 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10784 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10786 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 10788 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10790 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10792 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pchrp */

/* 10794 */	NdrFcShort( 0x112 ),	/* Flags:  must free, out, simple ref, */
/* 10796 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10798 */	NdrFcShort( 0x356 ),	/* Type Offset=854 */

	/* Parameter pichMin */

/* 10800 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10802 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10804 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 10806 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10808 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10810 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10812 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10814 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10816 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetParaProps */

/* 10818 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10820 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10824 */	NdrFcShort( 0x6 ),	/* 6 */
/* 10826 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10828 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10830 */	NdrFcShort( 0x40 ),	/* 64 */
/* 10832 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 10834 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 10836 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10840 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 10842 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10844 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10846 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pchrp */

/* 10848 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 10850 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10852 */	NdrFcShort( 0x3b2 ),	/* Type Offset=946 */

	/* Parameter pichMin */

/* 10854 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10856 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10858 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 10860 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10862 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10864 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10866 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10868 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10870 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetCharStringProp */

/* 10872 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10874 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10878 */	NdrFcShort( 0x7 ),	/* 7 */
/* 10880 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 10882 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10884 */	NdrFcShort( 0x40 ),	/* 64 */
/* 10886 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 10888 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 10890 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10892 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10894 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 10896 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10898 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10900 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nId */

/* 10902 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10904 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10906 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 10908 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 10910 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10912 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pichMin */

/* 10914 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10916 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10918 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 10920 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10922 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10924 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10926 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10928 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10930 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetParaStringProp */

/* 10932 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10934 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10938 */	NdrFcShort( 0x8 ),	/* 8 */
/* 10940 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 10942 */	NdrFcShort( 0x10 ),	/* 16 */
/* 10944 */	NdrFcShort( 0x40 ),	/* 64 */
/* 10946 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 10948 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 10950 */	NdrFcShort( 0x1 ),	/* 1 */
/* 10952 */	NdrFcShort( 0x0 ),	/* 0 */
/* 10954 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 10956 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10958 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 10960 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nId */

/* 10962 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 10964 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10966 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 10968 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 10970 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 10972 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pichMin */

/* 10974 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10976 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 10978 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 10980 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 10982 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 10984 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 10986 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 10988 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 10990 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AdjustGlyphWidths */

/* 10992 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 10994 */	NdrFcLong( 0x0 ),	/* 0 */
/* 10998 */	NdrFcShort( 0x3 ),	/* 3 */
/* 11000 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 11002 */	NdrFcShort( 0x20 ),	/* 32 */
/* 11004 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11006 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 11008 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11010 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11012 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11014 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pjren */

/* 11016 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11018 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11020 */	NdrFcShort( 0x3be ),	/* Type Offset=958 */

	/* Parameter iGlyphMin */

/* 11022 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11024 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11026 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter iGlyphLim */

/* 11028 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11030 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11032 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter dxCurrentWidth */

/* 11034 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11036 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11038 */	0xa,		/* FC_FLOAT */
			0x0,		/* 0 */

	/* Parameter dxDesiredWidth */

/* 11040 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11042 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11044 */	0xa,		/* FC_FLOAT */
			0x0,		/* 0 */

	/* Return value */

/* 11046 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11048 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 11050 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawText */

/* 11052 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11054 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11058 */	NdrFcShort( 0x3 ),	/* 3 */
/* 11060 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 11062 */	NdrFcShort( 0x48 ),	/* 72 */
/* 11064 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11066 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 11068 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11070 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11072 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11074 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11076 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11078 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11080 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11082 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11084 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11086 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 11088 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11090 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11092 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 11094 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11096 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 11098 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter dxdWidth */

/* 11100 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11102 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 11104 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11106 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11108 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 11110 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Recompute */

/* 11112 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11114 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11118 */	NdrFcShort( 0x4 ),	/* 4 */
/* 11120 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11122 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11124 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11126 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 11128 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11130 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11132 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11134 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11136 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11138 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11140 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11142 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11144 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11146 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Return value */

/* 11148 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11150 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11152 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Width */

/* 11154 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11156 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11160 */	NdrFcShort( 0x5 ),	/* 5 */
/* 11162 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11164 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11166 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11168 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11170 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11172 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11174 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11176 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11178 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11180 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11182 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11184 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11186 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11188 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter px */

/* 11190 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11192 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11194 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11196 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11198 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11200 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RightOverhang */

/* 11202 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11204 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11208 */	NdrFcShort( 0x6 ),	/* 6 */
/* 11210 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11212 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11214 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11216 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11218 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11220 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11222 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11224 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11226 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11228 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11230 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11232 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11234 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11236 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter px */

/* 11238 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11240 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11242 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11244 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11246 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11248 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LeftOverhang */

/* 11250 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11252 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11256 */	NdrFcShort( 0x7 ),	/* 7 */
/* 11258 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11260 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11262 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11264 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11266 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11270 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11272 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11274 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11276 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11280 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11282 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11284 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter px */

/* 11286 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11288 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11290 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11292 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11294 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11296 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Height */

/* 11298 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11300 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11304 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11306 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11308 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11310 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11312 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11314 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11318 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11320 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11322 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11324 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11326 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11328 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11330 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11332 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter py */

/* 11334 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11336 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11338 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11340 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11342 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11344 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Ascent */

/* 11346 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11348 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11352 */	NdrFcShort( 0x9 ),	/* 9 */
/* 11354 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11356 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11358 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11360 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11362 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11364 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11366 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11368 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11370 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11372 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11374 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11376 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11378 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11380 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter py */

/* 11382 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11384 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11386 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11388 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11390 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11392 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Extent */

/* 11394 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11396 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11400 */	NdrFcShort( 0xa ),	/* 10 */
/* 11402 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 11404 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11406 */	NdrFcShort( 0x40 ),	/* 64 */
/* 11408 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 11410 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11412 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11416 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11418 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11420 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11422 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11424 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11426 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11428 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter px */

/* 11430 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11432 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11434 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter py */

/* 11436 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11438 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11440 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11442 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11444 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11446 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure BoundingRect */

/* 11448 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11450 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11454 */	NdrFcShort( 0xb ),	/* 11 */
/* 11456 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 11458 */	NdrFcShort( 0x48 ),	/* 72 */
/* 11460 */	NdrFcShort( 0x3c ),	/* 60 */
/* 11462 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 11464 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11466 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11468 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11470 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11472 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11474 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11476 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11478 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11480 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11482 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 11484 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11486 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11488 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 11490 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11492 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 11494 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter prcBounds */

/* 11496 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 11498 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 11500 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Return value */

/* 11502 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11504 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 11506 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetActualWidth */

/* 11508 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11510 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11514 */	NdrFcShort( 0xc ),	/* 12 */
/* 11516 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 11518 */	NdrFcShort( 0x48 ),	/* 72 */
/* 11520 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11522 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 11524 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11526 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11528 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11530 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11532 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11534 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11536 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11538 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11540 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11542 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 11544 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11546 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11548 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 11550 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 11552 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 11554 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter dxdWidth */

/* 11556 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11558 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 11560 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11562 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11564 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 11566 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_AscentOverhang */

/* 11568 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11570 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11574 */	NdrFcShort( 0xd ),	/* 13 */
/* 11576 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11578 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11580 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11582 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11584 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11588 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11590 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11592 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11594 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11596 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11598 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11600 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11602 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter py */

/* 11604 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11606 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11608 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11610 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11612 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11614 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DescentOverhang */

/* 11616 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11618 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11622 */	NdrFcShort( 0xe ),	/* 14 */
/* 11624 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11626 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11628 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11630 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11632 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11634 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11636 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11638 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11640 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11642 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11644 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11646 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11648 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11650 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter py */

/* 11652 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11654 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11656 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11658 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11660 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11662 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsTitle */


	/* Procedure get_RightToLeft */

/* 11664 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11666 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11670 */	NdrFcShort( 0xf ),	/* 15 */
/* 11672 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11674 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11676 */	NdrFcShort( 0x22 ),	/* 34 */
/* 11678 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11680 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11682 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11684 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11686 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */


	/* Parameter ichBase */

/* 11688 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11690 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11692 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */


	/* Parameter pfResult */

/* 11694 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11696 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11698 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 11700 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11702 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11704 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DirectionDepth */

/* 11706 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11708 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11712 */	NdrFcShort( 0x10 ),	/* 16 */
/* 11714 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11716 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11718 */	NdrFcShort( 0x3e ),	/* 62 */
/* 11720 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 11722 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11724 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11726 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11728 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11730 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11732 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11734 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pnDepth */

/* 11736 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11738 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11740 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfWeak */

/* 11742 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11744 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11746 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 11748 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11750 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11752 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetDirectionDepth */

/* 11754 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11756 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11760 */	NdrFcShort( 0x11 ),	/* 17 */
/* 11762 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11764 */	NdrFcShort( 0x10 ),	/* 16 */
/* 11766 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11768 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11770 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11772 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11776 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichwBase */

/* 11778 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11780 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11782 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nNewDepth */

/* 11784 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11786 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11788 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11790 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11792 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11794 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Lim */

/* 11796 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11798 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11802 */	NdrFcShort( 0x13 ),	/* 19 */
/* 11804 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11806 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11808 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11810 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11812 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11814 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11816 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11818 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11820 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11822 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11824 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdich */

/* 11826 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11828 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11830 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11832 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11834 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11836 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LimInterest */

/* 11838 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11840 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11844 */	NdrFcShort( 0x14 ),	/* 20 */
/* 11846 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11848 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11850 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11852 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 11854 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11856 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11858 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11860 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11862 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11864 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11866 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdich */

/* 11868 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 11870 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11872 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 11874 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11876 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11878 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_EndLine */

/* 11880 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11882 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11886 */	NdrFcShort( 0x15 ),	/* 21 */
/* 11888 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11890 */	NdrFcShort( 0xe ),	/* 14 */
/* 11892 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11894 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11896 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11898 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11900 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11902 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11904 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11906 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11908 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11910 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11912 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11914 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter fNewVal */

/* 11916 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11918 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11920 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 11922 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11924 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11926 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_StartLine */

/* 11928 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11930 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11934 */	NdrFcShort( 0x16 ),	/* 22 */
/* 11936 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11938 */	NdrFcShort( 0xe ),	/* 14 */
/* 11940 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11942 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11944 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11948 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11950 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 11952 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11954 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 11956 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 11958 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 11960 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 11962 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter fNewVal */

/* 11964 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 11966 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 11968 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 11970 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 11972 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 11974 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StartBreakWeight */

/* 11976 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 11978 */	NdrFcLong( 0x0 ),	/* 0 */
/* 11982 */	NdrFcShort( 0x17 ),	/* 23 */
/* 11984 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 11986 */	NdrFcShort( 0x8 ),	/* 8 */
/* 11988 */	NdrFcShort( 0x24 ),	/* 36 */
/* 11990 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 11992 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 11994 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11996 */	NdrFcShort( 0x0 ),	/* 0 */
/* 11998 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12000 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12002 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12004 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12006 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12008 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12010 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter plb */

/* 12012 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12014 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12016 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 12018 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12020 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12022 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_EndBreakWeight */

/* 12024 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12026 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12030 */	NdrFcShort( 0x18 ),	/* 24 */
/* 12032 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12034 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12036 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12038 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 12040 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12042 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12044 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12046 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12048 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12050 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12052 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12054 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12056 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12058 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter plb */

/* 12060 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12062 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12064 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 12066 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12068 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12070 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Stretch */

/* 12072 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12074 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12078 */	NdrFcShort( 0x19 ),	/* 25 */
/* 12080 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12082 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12084 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12086 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 12088 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12090 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12092 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12094 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12096 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12098 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12100 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pxs */

/* 12102 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12104 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12106 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12108 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12110 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Stretch */

/* 12114 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12116 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12120 */	NdrFcShort( 0x1a ),	/* 26 */
/* 12122 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12124 */	NdrFcShort( 0x10 ),	/* 16 */
/* 12126 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12128 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 12130 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12132 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12134 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12136 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12138 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12140 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12142 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter xs */

/* 12144 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12146 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12148 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 12150 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12152 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12154 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsValidInsertionPoint */

/* 12156 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12158 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12162 */	NdrFcShort( 0x1b ),	/* 27 */
/* 12164 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12166 */	NdrFcShort( 0x10 ),	/* 16 */
/* 12168 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12170 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 12172 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12174 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12176 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12178 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12180 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12182 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12184 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12186 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12188 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12190 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter ich */

/* 12192 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12194 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pipvr */

/* 12198 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12200 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12202 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 12204 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12206 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12208 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DoBoundariesCoincide */

/* 12210 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12212 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12216 */	NdrFcShort( 0x1c ),	/* 28 */
/* 12218 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12220 */	NdrFcShort( 0x14 ),	/* 20 */
/* 12222 */	NdrFcShort( 0x22 ),	/* 34 */
/* 12224 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 12226 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12228 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12230 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12232 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12234 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12236 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12240 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12242 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12244 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter fBoundaryEnd */

/* 12246 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12248 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12250 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fBoundaryRight */

/* 12252 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12254 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12256 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfResult */

/* 12258 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12260 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12262 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12264 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12266 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12268 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawInsertionPoint */

/* 12270 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12272 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12276 */	NdrFcShort( 0x1d ),	/* 29 */
/* 12278 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 12280 */	NdrFcShort( 0x64 ),	/* 100 */
/* 12282 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12284 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x9,		/* 9 */
/* 12286 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12288 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12290 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12292 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12294 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12296 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12298 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12300 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12302 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12304 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 12306 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12308 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12310 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 12312 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12314 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12316 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter ich */

/* 12318 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12320 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12322 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fAssocPrev */

/* 12324 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12326 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 12328 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fOn */

/* 12330 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12332 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12334 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter dm */

/* 12336 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12338 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 12340 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 12342 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12344 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 12346 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PositionsOfIP */

/* 12348 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12350 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12354 */	NdrFcShort( 0x1e ),	/* 30 */
/* 12356 */	NdrFcShort( 0x4c ),	/* x86 Stack size/offset = 76 */
/* 12358 */	NdrFcShort( 0x5e ),	/* 94 */
/* 12360 */	NdrFcShort( 0xa4 ),	/* 164 */
/* 12362 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xc,		/* 12 */
/* 12364 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12366 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12368 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12370 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12372 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12374 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12376 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12378 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12380 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12382 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 12384 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12386 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12388 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 12390 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12392 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12394 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter ich */

/* 12396 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12398 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12400 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fAssocPrev */

/* 12402 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12404 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 12406 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter dm */

/* 12408 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12410 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12412 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter rectPrimary */

/* 12414 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 12416 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 12418 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rectSecondary */

/* 12420 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 12422 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 12424 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter pfPrimaryHere */

/* 12426 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12428 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 12430 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfSecHere */

/* 12432 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12434 */	NdrFcShort( 0x44 ),	/* x86 Stack size/offset = 68 */
/* 12436 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12438 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12440 */	NdrFcShort( 0x48 ),	/* x86 Stack size/offset = 72 */
/* 12442 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DrawRange */

/* 12444 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12446 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12450 */	NdrFcShort( 0x1f ),	/* 31 */
/* 12452 */	NdrFcShort( 0x44 ),	/* x86 Stack size/offset = 68 */
/* 12454 */	NdrFcShort( 0x6e ),	/* 110 */
/* 12456 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12458 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xa,		/* 10 */
/* 12460 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12462 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12464 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12466 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12468 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12470 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12472 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12474 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12476 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12478 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 12480 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12482 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12484 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 12486 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12488 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12490 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter ichMin */

/* 12492 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12494 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 12498 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12500 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 12502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ydTop */

/* 12504 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12506 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ydBottom */

/* 12510 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12512 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 12514 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bOn */

/* 12516 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12518 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 12520 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12522 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12524 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 12526 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PositionOfRange */

/* 12528 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12530 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12534 */	NdrFcShort( 0x20 ),	/* 32 */
/* 12536 */	NdrFcShort( 0x48 ),	/* x86 Stack size/offset = 72 */
/* 12538 */	NdrFcShort( 0x9c ),	/* 156 */
/* 12540 */	NdrFcShort( 0x22 ),	/* 34 */
/* 12542 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xb,		/* 11 */
/* 12544 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12546 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12548 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12550 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12552 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12554 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12556 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12558 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12560 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12562 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 12564 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12566 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12568 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 12570 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12572 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12574 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter ichMin */

/* 12576 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12578 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12580 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichim */

/* 12582 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12584 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 12586 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ydTop */

/* 12588 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12590 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ydBottom */

/* 12594 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12596 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 12598 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rsBounds */

/* 12600 */	NdrFcShort( 0x10a ),	/* Flags:  must free, in, simple ref, */
/* 12602 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 12604 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter pfAnythingToDraw */

/* 12606 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12608 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 12610 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12612 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12614 */	NdrFcShort( 0x44 ),	/* x86 Stack size/offset = 68 */
/* 12616 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure PointToChar */

/* 12618 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12620 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12624 */	NdrFcShort( 0x21 ),	/* 33 */
/* 12626 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 12628 */	NdrFcShort( 0x60 ),	/* 96 */
/* 12630 */	NdrFcShort( 0x3e ),	/* 62 */
/* 12632 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 12634 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12636 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12638 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12640 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12642 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12644 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12646 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12648 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12650 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12652 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter rcSrc */

/* 12654 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12656 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12658 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 12660 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12662 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12664 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter ptdClickPosition */

/* 12666 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12668 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12670 */	NdrFcShort( 0x370 ),	/* Type Offset=880 */

	/* Parameter pich */

/* 12672 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12674 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12676 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfAssocPrev */

/* 12678 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12680 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 12682 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12684 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12686 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 12688 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ArrowKeyPosition */

/* 12690 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12692 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12696 */	NdrFcShort( 0x22 ),	/* 34 */
/* 12698 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 12700 */	NdrFcShort( 0x4a ),	/* 74 */
/* 12702 */	NdrFcShort( 0x58 ),	/* 88 */
/* 12704 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 12706 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12708 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12710 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12712 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12714 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12716 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12718 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12720 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12722 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12724 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter pich */

/* 12726 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 12728 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12730 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfAssocPrev */

/* 12732 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 12734 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12736 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fRight */

/* 12738 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12740 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12742 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fMovingIn */

/* 12744 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12746 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12748 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfResult */

/* 12750 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12752 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12754 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12756 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12758 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 12760 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ExtendSelectionPosition */

/* 12762 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12764 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12768 */	NdrFcShort( 0x23 ),	/* 35 */
/* 12770 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 12772 */	NdrFcShort( 0x44 ),	/* 68 */
/* 12774 */	NdrFcShort( 0x3e ),	/* 62 */
/* 12776 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0xa,		/* 10 */
/* 12778 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 12780 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12782 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12784 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12786 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12788 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12790 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12792 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12794 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12796 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter pich */

/* 12798 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 12800 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12802 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fAssocPrevMatch */

/* 12804 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12806 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12808 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fAssocPrevNeeded */

/* 12810 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12812 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12814 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ichAnchor */

/* 12816 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12818 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 12820 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fRight */

/* 12822 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12824 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 12826 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fMovingIn */

/* 12828 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12830 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 12832 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 12834 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12836 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 12838 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 12840 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12842 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 12844 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetCharPlacement */

/* 12846 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12848 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12852 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12854 */	NdrFcShort( 0x50 ),	/* x86 Stack size/offset = 80 */
/* 12856 */	NdrFcShort( 0x66 ),	/* 102 */
/* 12858 */	NdrFcShort( 0x24 ),	/* 36 */
/* 12860 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0xd,		/* 13 */
/* 12862 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 12864 */	NdrFcShort( 0x3 ),	/* 3 */
/* 12866 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12868 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichBase */

/* 12870 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12872 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12874 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 12876 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12878 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12880 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter ichMin */

/* 12882 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12884 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12886 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 12888 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12890 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12892 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter rcSrc */

/* 12894 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12896 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 12898 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter rcDst */

/* 12900 */	NdrFcShort( 0x8a ),	/* Flags:  must free, in, by val, */
/* 12902 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 12904 */	NdrFcShort( 0x31c ),	/* Type Offset=796 */

	/* Parameter fSkipSpace */

/* 12906 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12908 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 12910 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter cxdMax */

/* 12912 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 12914 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 12916 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcxd */

/* 12918 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 12920 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 12922 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgxdLefts */

/* 12924 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 12926 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 12928 */	NdrFcShort( 0x3ee ),	/* Type Offset=1006 */

	/* Parameter prgxdRights */

/* 12930 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 12932 */	NdrFcShort( 0x44 ),	/* x86 Stack size/offset = 68 */
/* 12934 */	NdrFcShort( 0x3ee ),	/* Type Offset=1006 */

	/* Parameter prgydUnderTops */

/* 12936 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 12938 */	NdrFcShort( 0x48 ),	/* x86 Stack size/offset = 72 */
/* 12940 */	NdrFcShort( 0x3ee ),	/* Type Offset=1006 */

	/* Return value */

/* 12942 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12944 */	NdrFcShort( 0x4c ),	/* x86 Stack size/offset = 76 */
/* 12946 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InitRenderer */

/* 12948 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12950 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12954 */	NdrFcShort( 0x3 ),	/* 3 */
/* 12956 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 12958 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12960 */	NdrFcShort( 0x8 ),	/* 8 */
/* 12962 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 12964 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 12966 */	NdrFcShort( 0x0 ),	/* 0 */
/* 12968 */	NdrFcShort( 0x1 ),	/* 1 */
/* 12970 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 12972 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 12974 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 12976 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter bstrData */

/* 12978 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 12980 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 12982 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 12984 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 12986 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 12988 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FindBreakPoint */

/* 12990 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 12992 */	NdrFcLong( 0x0 ),	/* 0 */
/* 12996 */	NdrFcShort( 0x6 ),	/* 6 */
/* 12998 */	NdrFcShort( 0x50 ),	/* x86 Stack size/offset = 80 */
/* 13000 */	NdrFcShort( 0x4a ),	/* 74 */
/* 13002 */	NdrFcShort( 0x5c ),	/* 92 */
/* 13004 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x13,		/* 19 */
/* 13006 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13008 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13010 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13012 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 13014 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13016 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13018 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter pts */

/* 13020 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13022 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13024 */	NdrFcShort( 0x3fa ),	/* Type Offset=1018 */

	/* Parameter pvjus */

/* 13026 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13028 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13030 */	NdrFcShort( 0x40c ),	/* Type Offset=1036 */

	/* Parameter ichMin */

/* 13032 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13034 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13036 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 13038 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13040 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13042 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLimBacktrack */

/* 13044 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13046 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 13048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fNeedFinalBreak */

/* 13050 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13052 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 13054 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fStartLine */

/* 13056 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13058 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 13060 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter dxMaxWidth */

/* 13062 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13064 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 13066 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter lbPref */

/* 13068 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13070 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 13072 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter lbMax */

/* 13074 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13076 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 13078 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter twsh */

/* 13080 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13082 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 13084 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter fParaRightToLeft */

/* 13086 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13088 */	NdrFcShort( 0x34 ),	/* x86 Stack size/offset = 52 */
/* 13090 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter ppsegRet */

/* 13092 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13094 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 13096 */	NdrFcShort( 0x41e ),	/* Type Offset=1054 */

	/* Parameter pdichLimSeg */

/* 13098 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13100 */	NdrFcShort( 0x3c ),	/* x86 Stack size/offset = 60 */
/* 13102 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdxWidth */

/* 13104 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13106 */	NdrFcShort( 0x40 ),	/* x86 Stack size/offset = 64 */
/* 13108 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pest */

/* 13110 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13112 */	NdrFcShort( 0x44 ),	/* x86 Stack size/offset = 68 */
/* 13114 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter psegPrev */

/* 13116 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13118 */	NdrFcShort( 0x48 ),	/* x86 Stack size/offset = 72 */
/* 13120 */	NdrFcShort( 0x422 ),	/* Type Offset=1058 */

	/* Return value */

/* 13122 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13124 */	NdrFcShort( 0x4c ),	/* x86 Stack size/offset = 76 */
/* 13126 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Hvo */


	/* Procedure get_ScriptDirection */

/* 13128 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13130 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13134 */	NdrFcShort( 0x7 ),	/* 7 */
/* 13136 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13138 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13140 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13142 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13144 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13146 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13148 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13150 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter phvo */


	/* Parameter pgrfsdc */

/* 13152 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13154 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13156 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */

/* 13158 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13160 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13162 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ClassId */

/* 13164 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13166 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13170 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13172 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13174 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13176 */	NdrFcShort( 0x4c ),	/* 76 */
/* 13178 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13180 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13182 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13184 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13186 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pguid */

/* 13188 */	NdrFcShort( 0x4112 ),	/* Flags:  must free, out, simple ref, srv alloc size=16 */
/* 13190 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13192 */	NdrFcShort( 0x11e ),	/* Type Offset=286 */

	/* Return value */

/* 13194 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13196 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13198 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InterpretChrp */

/* 13200 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13202 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13206 */	NdrFcShort( 0x9 ),	/* 9 */
/* 13208 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13210 */	NdrFcShort( 0x134 ),	/* 308 */
/* 13212 */	NdrFcShort( 0x13c ),	/* 316 */
/* 13214 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13216 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13218 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13220 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13222 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pchrp */

/* 13224 */	NdrFcShort( 0x11a ),	/* Flags:  must free, in, out, simple ref, */
/* 13226 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13228 */	NdrFcShort( 0x356 ),	/* Type Offset=854 */

	/* Return value */

/* 13230 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13232 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13234 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WritingSystemFactory */

/* 13236 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13238 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13242 */	NdrFcShort( 0xa ),	/* 10 */
/* 13244 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13246 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13248 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13250 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 13252 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13254 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13256 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13258 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppwsf */

/* 13260 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 13262 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13264 */	NdrFcShort( 0x434 ),	/* Type Offset=1076 */

	/* Return value */

/* 13266 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13268 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13270 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_WritingSystemFactory */

/* 13272 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13274 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13278 */	NdrFcShort( 0xb ),	/* 11 */
/* 13280 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13282 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13284 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13286 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 13288 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13290 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13292 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13294 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 13296 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13298 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13300 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Return value */

/* 13302 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13304 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13306 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFeatureIDs */

/* 13308 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13310 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13314 */	NdrFcShort( 0x3 ),	/* 3 */
/* 13316 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13318 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13320 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13322 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 13324 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13326 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13328 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13330 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cMax */

/* 13332 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13334 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13336 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgFids */

/* 13338 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 13340 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13342 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Parameter pcfid */

/* 13344 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13346 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13348 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13350 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13352 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13354 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFeatureLabel */

/* 13356 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13358 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13362 */	NdrFcShort( 0x4 ),	/* 4 */
/* 13364 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13366 */	NdrFcShort( 0x10 ),	/* 16 */
/* 13368 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13370 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 13372 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13374 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13376 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13378 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fid */

/* 13380 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13382 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13384 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nLanguage */

/* 13386 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13388 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13390 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrLabel */

/* 13392 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 13394 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13396 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 13398 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13400 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13402 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFeatureValues */

/* 13404 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13406 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13410 */	NdrFcShort( 0x5 ),	/* 5 */
/* 13412 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 13414 */	NdrFcShort( 0x10 ),	/* 16 */
/* 13416 */	NdrFcShort( 0x40 ),	/* 64 */
/* 13418 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x6,		/* 6 */
/* 13420 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13422 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13424 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13426 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fid */

/* 13428 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13430 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13432 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cfvalMax */

/* 13434 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13436 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13438 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgfval */

/* 13440 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 13442 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13444 */	NdrFcShort( 0x43c ),	/* Type Offset=1084 */

	/* Parameter pcfval */

/* 13446 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13448 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13450 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfvalDefault */

/* 13452 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13454 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13456 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13458 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13460 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 13462 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetFeatureValueLabel */

/* 13464 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13466 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13470 */	NdrFcShort( 0x6 ),	/* 6 */
/* 13472 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 13474 */	NdrFcShort( 0x18 ),	/* 24 */
/* 13476 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13478 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x5,		/* 5 */
/* 13480 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13482 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13484 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13486 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fid */

/* 13488 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13490 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13492 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fval */

/* 13494 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13496 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13498 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nLanguage */

/* 13500 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13502 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13504 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrLabel */

/* 13506 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 13508 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13510 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 13512 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13514 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13516 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetGlyphAttributeFloat */

/* 13518 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13520 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13524 */	NdrFcShort( 0x3 ),	/* 3 */
/* 13526 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 13528 */	NdrFcShort( 0x18 ),	/* 24 */
/* 13530 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13532 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 13534 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13536 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13538 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13540 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iGlyph */

/* 13542 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13544 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13546 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter kjgatId */

/* 13548 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13550 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13552 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nLevel */

/* 13554 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13556 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13558 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pValueRet */

/* 13560 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13562 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13564 */	0xa,		/* FC_FLOAT */
			0x0,		/* 0 */

	/* Return value */

/* 13566 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13568 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13570 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetGlyphAttributeInt */

/* 13572 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13574 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13578 */	NdrFcShort( 0x4 ),	/* 4 */
/* 13580 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 13582 */	NdrFcShort( 0x18 ),	/* 24 */
/* 13584 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13586 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 13588 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13590 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13592 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13594 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iGlyph */

/* 13596 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13598 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter kjgatId */

/* 13602 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13604 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13606 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nLevel */

/* 13608 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13610 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13612 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pValueRet */

/* 13614 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13616 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13618 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13620 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13622 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13624 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetGlyphAttributeFloat */

/* 13626 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13628 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13632 */	NdrFcShort( 0x5 ),	/* 5 */
/* 13634 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 13636 */	NdrFcShort( 0x20 ),	/* 32 */
/* 13638 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13640 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x5,		/* 5 */
/* 13642 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13646 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13648 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iGlyph */

/* 13650 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13652 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13654 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter kjgatId */

/* 13656 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13658 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13660 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter nLevel */

/* 13662 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13664 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13666 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter value */

/* 13668 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13670 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13672 */	0xa,		/* FC_FLOAT */
			0x0,		/* 0 */

	/* Return value */

/* 13674 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13676 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 13678 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IntToString */


	/* Procedure get_Name */

/* 13680 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13682 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13686 */	NdrFcShort( 0x3 ),	/* 3 */
/* 13688 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13690 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13692 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13694 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 13696 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13698 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13700 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13702 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter n */


	/* Parameter ws */

/* 13704 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13706 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13708 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */


	/* Parameter pbstr */

/* 13710 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 13712 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13714 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 13716 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13718 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NameWss */

/* 13722 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13724 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13728 */	NdrFcShort( 0x6 ),	/* 6 */
/* 13730 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13732 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13734 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13736 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 13738 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 13740 */	NdrFcShort( 0x1 ),	/* 1 */
/* 13742 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13744 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cws */

/* 13746 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13748 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13750 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgws */

/* 13752 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 13754 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13756 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Return value */

/* 13758 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13760 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13762 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_KeymanWindowsMessage */


	/* Procedure get_Locale */


	/* Procedure get_WinLCID */

/* 13764 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13766 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13770 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13772 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13776 */	NdrFcShort( 0x24 ),	/* 36 */
/* 13778 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13780 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13782 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13784 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13786 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwm */


	/* Parameter pnLocale */


	/* Parameter pnCode */

/* 13788 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13790 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13792 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 13794 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13796 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13798 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Dirty */

/* 13800 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13802 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13806 */	NdrFcShort( 0x10 ),	/* 16 */
/* 13808 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13810 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13812 */	NdrFcShort( 0x22 ),	/* 34 */
/* 13814 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13816 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13818 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13820 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13822 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pf */

/* 13824 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 13826 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13828 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13830 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13832 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13834 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Dirty */

/* 13836 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13838 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13842 */	NdrFcShort( 0x11 ),	/* 17 */
/* 13844 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13846 */	NdrFcShort( 0x6 ),	/* 6 */
/* 13848 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13850 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 13852 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13854 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13856 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13858 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fDirty */

/* 13860 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13862 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13864 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 13866 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13868 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13870 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteAsXml */

/* 13872 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13874 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13878 */	NdrFcShort( 0x12 ),	/* 18 */
/* 13880 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 13882 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13884 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13886 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 13888 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13890 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13892 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13894 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 13896 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13898 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13900 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter cchIndent */

/* 13902 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 13904 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13906 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 13908 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13910 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13912 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Serialize */

/* 13914 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13916 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13920 */	NdrFcShort( 0x13 ),	/* 19 */
/* 13922 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13926 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13928 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 13930 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13932 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13934 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13936 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 13938 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13940 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13942 */	NdrFcShort( 0x44c ),	/* Type Offset=1100 */

	/* Return value */

/* 13944 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13946 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13948 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Deserialize */

/* 13950 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13952 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13956 */	NdrFcShort( 0x14 ),	/* 20 */
/* 13958 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13960 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13962 */	NdrFcShort( 0x8 ),	/* 8 */
/* 13964 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 13966 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 13968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13970 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13972 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 13974 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 13976 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 13978 */	NdrFcShort( 0x44c ),	/* Type Offset=1100 */

	/* Return value */

/* 13980 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 13982 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 13984 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IcuRules */

/* 13986 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 13988 */	NdrFcLong( 0x0 ),	/* 0 */
/* 13992 */	NdrFcShort( 0x15 ),	/* 21 */
/* 13994 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 13996 */	NdrFcShort( 0x0 ),	/* 0 */
/* 13998 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14000 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14002 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 14004 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14006 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14008 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 14010 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 14012 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14014 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 14016 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14018 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14020 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_IcuRules */

/* 14022 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14024 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14028 */	NdrFcShort( 0x16 ),	/* 22 */
/* 14030 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14032 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14034 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14036 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14038 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14040 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14042 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14044 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 14046 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14048 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14050 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 14052 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14054 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14056 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WritingSystemFactory */

/* 14058 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14060 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14064 */	NdrFcShort( 0x17 ),	/* 23 */
/* 14066 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14070 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14072 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14074 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14076 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14078 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14080 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppwsf */

/* 14082 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14084 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14086 */	NdrFcShort( 0x434 ),	/* Type Offset=1076 */

	/* Return value */

/* 14088 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14090 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14092 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_WritingSystemFactory */

/* 14094 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14096 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14100 */	NdrFcShort( 0x18 ),	/* 24 */
/* 14102 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14104 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14106 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14108 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14110 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14112 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14114 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14116 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 14118 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14120 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14122 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Return value */

/* 14124 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14126 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14128 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NameWss */

/* 14130 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14132 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14136 */	NdrFcShort( 0x5 ),	/* 5 */
/* 14138 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14140 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14142 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14144 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 14146 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 14148 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14152 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cws */

/* 14154 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14156 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14158 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgws */

/* 14160 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 14162 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14164 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Return value */

/* 14166 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14168 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14170 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Country */


	/* Procedure get_Name */

/* 14172 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14174 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14178 */	NdrFcShort( 0x6 ),	/* 6 */
/* 14180 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14182 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14184 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14186 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 14188 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 14190 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14192 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14194 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iloc */


	/* Parameter ws */

/* 14196 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14198 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14200 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrName */


	/* Parameter pbstrName */

/* 14202 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 14204 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14206 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 14208 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14210 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14212 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Name */

/* 14214 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14216 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14220 */	NdrFcShort( 0x7 ),	/* 7 */
/* 14222 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14224 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14226 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14228 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14230 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14232 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14234 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14236 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 14238 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14240 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14242 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrName */

/* 14244 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14246 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14248 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 14250 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14252 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14254 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ConverterFrom */

/* 14256 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14258 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14262 */	NdrFcShort( 0xa ),	/* 10 */
/* 14264 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14266 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14268 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14270 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 14272 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14274 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14276 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14278 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 14280 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14282 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14284 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppstrconv */

/* 14286 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14288 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14290 */	NdrFcShort( 0x45e ),	/* Type Offset=1118 */

	/* Return value */

/* 14292 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14294 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14296 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NormalizeEngine */

/* 14298 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14300 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14304 */	NdrFcShort( 0xb ),	/* 11 */
/* 14306 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14308 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14310 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14312 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14314 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14316 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14318 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14320 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppstrconv */

/* 14322 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14324 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14326 */	NdrFcShort( 0x45e ),	/* Type Offset=1118 */

	/* Return value */

/* 14328 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14330 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14332 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WordBreakEngine */

/* 14334 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14336 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14340 */	NdrFcShort( 0xc ),	/* 12 */
/* 14342 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14346 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14348 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14350 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14352 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14354 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14356 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pptoker */

/* 14358 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14360 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14362 */	NdrFcShort( 0x474 ),	/* Type Offset=1140 */

	/* Return value */

/* 14364 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14366 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14368 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SpellingFactory */

/* 14370 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14372 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14376 */	NdrFcShort( 0xd ),	/* 13 */
/* 14378 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14380 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14382 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14384 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14386 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14390 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14392 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppspfact */

/* 14394 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14396 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14398 */	NdrFcShort( 0x48a ),	/* Type Offset=1162 */

	/* Return value */

/* 14400 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14402 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14404 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SpellCheckEngine */

/* 14406 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14408 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14412 */	NdrFcShort( 0xe ),	/* 14 */
/* 14414 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14416 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14418 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14420 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14422 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14424 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14426 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14428 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppspchk */

/* 14430 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14432 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14434 */	NdrFcShort( 0x4a0 ),	/* Type Offset=1184 */

	/* Return value */

/* 14436 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14438 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14440 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SearchEngine */

/* 14442 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14444 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14448 */	NdrFcShort( 0xf ),	/* 15 */
/* 14450 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14452 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14454 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14456 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14458 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14460 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14462 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14464 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppsrcheng */

/* 14466 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14468 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14470 */	NdrFcShort( 0x4b6 ),	/* Type Offset=1206 */

	/* Return value */

/* 14472 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14474 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14476 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Shutdown */


	/* Procedure CompileEngines */

/* 14478 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14480 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14484 */	NdrFcShort( 0x10 ),	/* 16 */
/* 14486 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14488 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14490 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14492 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 14494 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14496 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14498 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14500 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */


	/* Return value */

/* 14502 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14504 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14506 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Dirty */

/* 14508 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14510 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14514 */	NdrFcShort( 0x11 ),	/* 17 */
/* 14516 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14520 */	NdrFcShort( 0x22 ),	/* 34 */
/* 14522 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14524 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14526 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14528 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14530 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pf */

/* 14532 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14534 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14536 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 14538 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14540 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14542 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Dirty */

/* 14544 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14546 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14550 */	NdrFcShort( 0x12 ),	/* 18 */
/* 14552 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14554 */	NdrFcShort( 0x6 ),	/* 6 */
/* 14556 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14558 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14560 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14562 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14564 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14566 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fDirty */

/* 14568 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14570 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14572 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 14574 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14576 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14578 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WritingSystemFactory */

/* 14580 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14582 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14586 */	NdrFcShort( 0x13 ),	/* 19 */
/* 14588 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14590 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14592 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14594 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14596 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14598 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14600 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14602 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppwsf */

/* 14604 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14606 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14608 */	NdrFcShort( 0x434 ),	/* Type Offset=1076 */

	/* Return value */

/* 14610 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14612 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14614 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_WritingSystemFactory */

/* 14616 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14618 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14622 */	NdrFcShort( 0x14 ),	/* 20 */
/* 14624 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14626 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14628 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14630 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14632 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14634 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14636 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14638 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 14640 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14642 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14644 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Return value */

/* 14646 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14648 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14650 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure WriteAsXml */

/* 14652 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14654 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14658 */	NdrFcShort( 0x15 ),	/* 21 */
/* 14660 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14662 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14664 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14666 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14668 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14670 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14672 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14674 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstrm */

/* 14676 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14678 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14680 */	NdrFcShort( 0x12a ),	/* Type Offset=298 */

	/* Parameter cchIndent */

/* 14682 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14684 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14686 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 14688 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14690 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14692 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Serialize */

/* 14694 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14696 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14700 */	NdrFcShort( 0x16 ),	/* 22 */
/* 14702 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14704 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14706 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14708 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14710 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14712 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14714 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14716 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 14718 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14720 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14722 */	NdrFcShort( 0x44c ),	/* Type Offset=1100 */

	/* Return value */

/* 14724 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14726 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14728 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Deserialize */

/* 14730 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14732 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14736 */	NdrFcShort( 0x17 ),	/* 23 */
/* 14738 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14740 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14742 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14744 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14746 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14748 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14750 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14752 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 14754 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14756 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14758 */	NdrFcShort( 0x44c ),	/* Type Offset=1100 */

	/* Return value */

/* 14760 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14762 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14764 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RightToLeft */

/* 14766 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14768 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14772 */	NdrFcShort( 0x18 ),	/* 24 */
/* 14774 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14776 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14778 */	NdrFcShort( 0x22 ),	/* 34 */
/* 14780 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14782 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14784 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14786 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14788 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfRightToLeft */

/* 14790 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 14792 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14794 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 14796 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14798 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14800 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_RightToLeft */

/* 14802 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14804 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14808 */	NdrFcShort( 0x19 ),	/* 25 */
/* 14810 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14812 */	NdrFcShort( 0x6 ),	/* 6 */
/* 14814 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14816 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 14818 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14820 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14822 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14824 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fRightToLeft */

/* 14826 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 14828 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14830 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 14832 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14834 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14836 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Renderer */

/* 14838 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14840 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14844 */	NdrFcShort( 0x1a ),	/* 26 */
/* 14846 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 14848 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14850 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14852 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 14854 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 14856 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14858 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14860 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pvg */

/* 14862 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 14864 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14866 */	NdrFcShort( 0x3d0 ),	/* Type Offset=976 */

	/* Parameter ppreneng */

/* 14868 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 14870 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14872 */	NdrFcShort( 0x4cc ),	/* Type Offset=1228 */

	/* Return value */

/* 14874 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14876 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14878 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FontVariation */

/* 14880 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14882 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14886 */	NdrFcShort( 0x1b ),	/* 27 */
/* 14888 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14890 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14892 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14894 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14896 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 14898 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14900 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14902 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 14904 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 14906 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14908 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 14910 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14912 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14914 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_FontVariation */

/* 14916 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14918 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14922 */	NdrFcShort( 0x1c ),	/* 28 */
/* 14924 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14926 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14928 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14930 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 14932 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 14934 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14936 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14938 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 14940 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 14942 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14944 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 14946 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14948 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14950 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SansFontVariation */

/* 14952 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14954 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14958 */	NdrFcShort( 0x1d ),	/* 29 */
/* 14960 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14962 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14964 */	NdrFcShort( 0x8 ),	/* 8 */
/* 14966 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 14968 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 14970 */	NdrFcShort( 0x1 ),	/* 1 */
/* 14972 */	NdrFcShort( 0x0 ),	/* 0 */
/* 14974 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 14976 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 14978 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 14980 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 14982 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 14984 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 14986 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_SansFontVariation */

/* 14988 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 14990 */	NdrFcLong( 0x0 ),	/* 0 */
/* 14994 */	NdrFcShort( 0x1e ),	/* 30 */
/* 14996 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 14998 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15000 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15002 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 15004 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15006 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15008 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15010 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 15012 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15014 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15016 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 15018 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15020 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15022 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DefaultSerif */

/* 15024 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15026 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15030 */	NdrFcShort( 0x1f ),	/* 31 */
/* 15032 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15034 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15036 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15038 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15040 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15042 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15044 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15046 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 15048 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15050 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15052 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 15054 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15056 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15058 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_DefaultSerif */

/* 15060 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15062 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15066 */	NdrFcShort( 0x20 ),	/* 32 */
/* 15068 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15070 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15072 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15074 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 15076 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15078 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15080 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15082 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 15084 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15086 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15088 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 15090 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15092 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15094 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DefaultSansSerif */

/* 15096 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15098 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15102 */	NdrFcShort( 0x21 ),	/* 33 */
/* 15104 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15106 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15108 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15110 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15112 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15114 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15116 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15118 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 15120 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15122 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15124 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 15126 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15128 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15130 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_DefaultSansSerif */

/* 15132 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15134 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15138 */	NdrFcShort( 0x22 ),	/* 34 */
/* 15140 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15142 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15144 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15146 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 15148 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15150 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15152 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15154 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 15156 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15158 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15160 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 15162 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15164 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15166 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DefaultMonospace */

/* 15168 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15170 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15174 */	NdrFcShort( 0x23 ),	/* 35 */
/* 15176 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15178 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15180 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15182 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15184 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15186 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15188 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15190 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 15192 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15194 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15196 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 15198 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15200 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15202 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_DefaultMonospace */

/* 15204 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15206 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15210 */	NdrFcShort( 0x24 ),	/* 36 */
/* 15212 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15214 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15216 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15218 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 15220 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15222 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15224 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15226 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 15228 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15230 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15232 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 15234 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15236 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_KeyMan */

/* 15240 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15242 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15246 */	NdrFcShort( 0x25 ),	/* 37 */
/* 15248 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15250 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15252 */	NdrFcShort( 0x22 ),	/* 34 */
/* 15254 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15256 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15260 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15262 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfKeyMan */

/* 15264 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15266 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15268 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15270 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15272 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15274 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_KeyMan */

/* 15276 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15278 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15282 */	NdrFcShort( 0x26 ),	/* 38 */
/* 15284 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15286 */	NdrFcShort( 0x6 ),	/* 6 */
/* 15288 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15290 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15292 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15294 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15296 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15298 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fKeyMan */

/* 15300 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15302 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15304 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 15306 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15308 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15310 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_UiName */

/* 15312 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15314 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15318 */	NdrFcShort( 0x27 ),	/* 39 */
/* 15320 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15322 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15324 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15326 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 15328 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15330 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15332 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15334 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 15336 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15338 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15340 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 15342 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15344 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15346 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 15348 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15350 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15352 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CollationCount */

/* 15354 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15356 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15360 */	NdrFcShort( 0x28 ),	/* 40 */
/* 15362 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15364 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15366 */	NdrFcShort( 0x24 ),	/* 36 */
/* 15368 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15370 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15374 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15376 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pccoll */

/* 15378 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15380 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15382 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15384 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15386 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15388 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Collation */

/* 15390 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15392 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15396 */	NdrFcShort( 0x29 ),	/* 41 */
/* 15398 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15400 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15402 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15404 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 15406 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15408 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15410 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15412 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter icoll */

/* 15414 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15416 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppcoll */

/* 15420 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 15422 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15424 */	NdrFcShort( 0x4e2 ),	/* Type Offset=1250 */

	/* Return value */

/* 15426 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15428 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15430 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_Collation */

/* 15432 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15434 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15438 */	NdrFcShort( 0x2a ),	/* 42 */
/* 15440 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15442 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15444 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15446 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 15448 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15450 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15452 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15454 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter icoll */

/* 15456 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15458 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15460 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcoll */

/* 15462 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 15464 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15466 */	NdrFcShort( 0x4e6 ),	/* Type Offset=1254 */

	/* Return value */

/* 15468 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15470 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15472 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure RemoveCollation */

/* 15474 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15476 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15480 */	NdrFcShort( 0x2b ),	/* 43 */
/* 15482 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15484 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15486 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15488 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15490 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15492 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15494 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15496 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter icoll */

/* 15498 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15500 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15504 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15506 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15508 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Abbr */

/* 15510 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15512 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15516 */	NdrFcShort( 0x2c ),	/* 44 */
/* 15518 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15520 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15522 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15524 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 15526 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15528 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15530 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15532 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 15534 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15536 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 15540 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 15542 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15544 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 15546 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15548 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15550 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Abbr */

/* 15552 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15554 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15558 */	NdrFcShort( 0x2d ),	/* 45 */
/* 15560 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15562 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15564 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15566 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 15568 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 15570 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15572 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15574 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 15576 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15578 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15580 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */

/* 15582 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 15584 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15586 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 15588 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15590 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_AbbrWsCount */

/* 15594 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15596 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15600 */	NdrFcShort( 0x2e ),	/* 46 */
/* 15602 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15604 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15606 */	NdrFcShort( 0x24 ),	/* 36 */
/* 15608 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15610 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15612 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15614 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15616 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcws */

/* 15618 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15620 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15622 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15624 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15626 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15628 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_AbbrWss */

/* 15630 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15632 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15636 */	NdrFcShort( 0x2f ),	/* 47 */
/* 15638 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15640 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15642 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15644 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 15646 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15648 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15650 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15652 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cws */

/* 15654 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15656 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15658 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgws */

/* 15660 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 15662 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15664 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Return value */

/* 15666 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15668 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15670 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Description */

/* 15672 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15674 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15678 */	NdrFcShort( 0x30 ),	/* 48 */
/* 15680 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15682 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15684 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15686 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 15688 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15690 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15692 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15694 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 15696 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15698 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15700 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pptss */

/* 15702 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 15704 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15706 */	NdrFcShort( 0x38 ),	/* Type Offset=56 */

	/* Return value */

/* 15708 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15710 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15712 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Description */

/* 15714 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15716 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15720 */	NdrFcShort( 0x31 ),	/* 49 */
/* 15722 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15724 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15726 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15728 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 15730 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15732 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15734 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15736 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 15738 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15740 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15742 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 15744 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 15746 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15748 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Return value */

/* 15750 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15752 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15754 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DescriptionWsCount */

/* 15756 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15758 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15762 */	NdrFcShort( 0x32 ),	/* 50 */
/* 15764 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15768 */	NdrFcShort( 0x24 ),	/* 36 */
/* 15770 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15772 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15774 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15776 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15778 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcws */

/* 15780 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 15782 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15784 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15786 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15788 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15790 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DescriptionWss */

/* 15792 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15794 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15798 */	NdrFcShort( 0x33 ),	/* 51 */
/* 15800 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 15802 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15804 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15806 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 15808 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15810 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15812 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15814 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cws */

/* 15816 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15818 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15820 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgws */

/* 15822 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 15824 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15826 */	NdrFcShort( 0x20e ),	/* Type Offset=526 */

	/* Return value */

/* 15828 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15830 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15832 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CollatingEngine */

/* 15834 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15836 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15840 */	NdrFcShort( 0x34 ),	/* 52 */
/* 15842 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15844 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15846 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15848 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15850 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15852 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15854 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15856 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppcoleng */

/* 15858 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 15860 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15862 */	NdrFcShort( 0x4f8 ),	/* Type Offset=1272 */

	/* Return value */

/* 15864 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15866 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15868 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CharPropEngine */

/* 15870 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15872 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15876 */	NdrFcShort( 0x35 ),	/* 53 */
/* 15878 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15880 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15882 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15884 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15886 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15888 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15890 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15892 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pppropeng */

/* 15894 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 15896 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15898 */	NdrFcShort( 0x50e ),	/* Type Offset=1294 */

	/* Return value */

/* 15900 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15902 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15904 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetTracing */

/* 15906 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15908 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15912 */	NdrFcShort( 0x36 ),	/* 54 */
/* 15914 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15916 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15918 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15920 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15922 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15926 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15928 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter n */

/* 15930 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 15932 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 15936 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15938 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15940 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InterpretChrp */

/* 15942 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15944 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15948 */	NdrFcShort( 0x37 ),	/* 55 */
/* 15950 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15952 */	NdrFcShort( 0x134 ),	/* 308 */
/* 15954 */	NdrFcShort( 0x13c ),	/* 316 */
/* 15956 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 15958 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 15960 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15962 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15964 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pchrp */

/* 15966 */	NdrFcShort( 0x11a ),	/* Flags:  must free, in, out, simple ref, */
/* 15968 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 15970 */	NdrFcShort( 0x356 ),	/* Type Offset=854 */

	/* Return value */

/* 15972 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 15974 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 15976 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IcuLocale */

/* 15978 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 15980 */	NdrFcLong( 0x0 ),	/* 0 */
/* 15984 */	NdrFcShort( 0x38 ),	/* 56 */
/* 15986 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 15988 */	NdrFcShort( 0x0 ),	/* 0 */
/* 15990 */	NdrFcShort( 0x8 ),	/* 8 */
/* 15992 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 15994 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 15996 */	NdrFcShort( 0x1 ),	/* 1 */
/* 15998 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16000 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16002 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16004 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16006 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16008 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16010 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16012 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_IcuLocale */

/* 16014 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16016 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16020 */	NdrFcShort( 0x39 ),	/* 57 */
/* 16022 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16024 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16026 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16028 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 16030 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16032 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16034 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16036 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 16038 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16040 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16042 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 16044 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16046 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetIcuLocaleParts */

/* 16050 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16052 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16056 */	NdrFcShort( 0x3a ),	/* 58 */
/* 16058 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16060 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16062 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16064 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 16066 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16068 */	NdrFcShort( 0x3 ),	/* 3 */
/* 16070 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16072 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrLanguage */

/* 16074 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16076 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16078 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pbstrCountry */

/* 16080 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16082 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16084 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pbstrVariant */

/* 16086 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16088 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16090 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16092 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16094 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LegacyMapping */

/* 16098 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16100 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16104 */	NdrFcShort( 0x3b ),	/* 59 */
/* 16106 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16108 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16110 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16112 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16114 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16116 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16118 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16120 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16122 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16124 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16126 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16128 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16130 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16132 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_LegacyMapping */

/* 16134 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16136 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16140 */	NdrFcShort( 0x3c ),	/* 60 */
/* 16142 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16144 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16146 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16148 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 16150 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16152 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16154 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16156 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 16158 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16160 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16162 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 16164 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16166 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16168 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_KeymanKbdName */

/* 16170 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16172 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16176 */	NdrFcShort( 0x3d ),	/* 61 */
/* 16178 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16180 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16182 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16184 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16186 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16188 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16190 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16192 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16194 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16196 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16198 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16200 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16202 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16204 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_KeymanKbdName */

/* 16206 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16208 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16212 */	NdrFcShort( 0x3e ),	/* 62 */
/* 16214 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16216 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16218 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16220 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 16222 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16224 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16226 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16228 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 16230 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16232 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16234 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 16236 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16238 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16240 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LanguageName */

/* 16242 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16244 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16248 */	NdrFcShort( 0x3f ),	/* 63 */
/* 16250 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16252 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16254 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16256 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16258 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16260 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16262 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16264 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16266 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16268 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16270 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16272 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16274 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16276 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CountryName */

/* 16278 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16280 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16284 */	NdrFcShort( 0x40 ),	/* 64 */
/* 16286 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16288 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16290 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16292 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16294 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16296 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16298 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16300 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16302 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16304 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16306 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16308 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16310 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16312 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_VariantName */

/* 16314 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16316 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16320 */	NdrFcShort( 0x41 ),	/* 65 */
/* 16322 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16324 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16326 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16328 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16330 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16332 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16336 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16338 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16340 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16342 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16344 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16346 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16348 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LanguageAbbr */

/* 16350 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16352 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16356 */	NdrFcShort( 0x42 ),	/* 66 */
/* 16358 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16360 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16362 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16364 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16366 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16368 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16370 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16372 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16374 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16376 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16378 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16380 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16382 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16384 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CountryAbbr */

/* 16386 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16388 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16392 */	NdrFcShort( 0x43 ),	/* 67 */
/* 16394 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16396 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16398 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16400 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16402 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16404 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16406 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16408 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16410 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16412 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16414 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16416 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16418 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16420 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_VariantAbbr */

/* 16422 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16424 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16428 */	NdrFcShort( 0x44 ),	/* 68 */
/* 16430 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16432 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16434 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16436 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16438 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16440 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16442 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16444 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstr */

/* 16446 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16448 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16450 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 16452 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16454 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16456 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SaveIfDirty */

/* 16458 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16460 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16464 */	NdrFcShort( 0x45 ),	/* 69 */
/* 16466 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16468 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16470 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16472 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 16474 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16476 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16478 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16480 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 16482 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16484 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16486 */	NdrFcShort( 0x2c0 ),	/* Type Offset=704 */

	/* Return value */

/* 16488 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16490 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16492 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure InstallLanguage */

/* 16494 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16496 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16500 */	NdrFcShort( 0x46 ),	/* 70 */
/* 16502 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16504 */	NdrFcShort( 0x6 ),	/* 6 */
/* 16506 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16508 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16510 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16512 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16514 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16516 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fForce */

/* 16518 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16520 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16522 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 16524 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16526 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16528 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_LastModified */

/* 16530 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16532 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16536 */	NdrFcShort( 0x47 ),	/* 71 */
/* 16538 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16540 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16542 */	NdrFcShort( 0x2c ),	/* 44 */
/* 16544 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16546 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16548 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16550 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16552 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pdate */

/* 16554 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16556 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16558 */	0xc,		/* FC_DOUBLE */
			0x0,		/* 0 */

	/* Return value */

/* 16560 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16562 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16564 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_LastModified */

/* 16566 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16568 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16572 */	NdrFcShort( 0x48 ),	/* 72 */
/* 16574 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16576 */	NdrFcShort( 0x10 ),	/* 16 */
/* 16578 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16580 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16582 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16584 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16588 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter date */

/* 16590 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16592 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16594 */	0xc,		/* FC_DOUBLE */
			0x0,		/* 0 */

	/* Return value */

/* 16596 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16598 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16600 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CurrentInputLanguage */

/* 16602 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16604 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16608 */	NdrFcShort( 0x49 ),	/* 73 */
/* 16610 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16612 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16614 */	NdrFcShort( 0x24 ),	/* 36 */
/* 16616 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16618 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16620 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16622 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16624 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnLangId */

/* 16626 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16628 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16630 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 16632 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16634 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16636 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_CurrentInputLanguage */

/* 16638 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16640 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16644 */	NdrFcShort( 0x4a ),	/* 74 */
/* 16646 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16648 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16650 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16652 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 16654 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16656 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16658 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16660 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nLangId */

/* 16662 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16664 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16666 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 16668 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16670 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16672 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Backspace */

/* 16674 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16676 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16680 */	NdrFcShort( 0x5 ),	/* 5 */
/* 16682 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 16684 */	NdrFcShort( 0x10 ),	/* 16 */
/* 16686 */	NdrFcShort( 0x78 ),	/* 120 */
/* 16688 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 16690 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16692 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16694 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16696 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pichStart */

/* 16698 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16700 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16702 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cactBackspace */

/* 16704 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16706 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16708 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptsbOld */

/* 16710 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16712 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16714 */	NdrFcShort( 0xec ),	/* Type Offset=236 */

	/* Parameter pichModMin */

/* 16716 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16718 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16720 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichModLim */

/* 16722 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16724 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16726 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichIP */

/* 16728 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16730 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 16732 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcactBsRemaining */

/* 16734 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16736 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 16738 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 16740 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16742 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 16744 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DeleteForward */

/* 16746 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16748 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16752 */	NdrFcShort( 0x6 ),	/* 6 */
/* 16754 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 16756 */	NdrFcShort( 0x10 ),	/* 16 */
/* 16758 */	NdrFcShort( 0x78 ),	/* 120 */
/* 16760 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 16762 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16764 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16766 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16768 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pichStart */

/* 16770 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16772 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16774 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cactDelForward */

/* 16776 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16778 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16780 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptsbInOut */

/* 16782 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16784 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16786 */	NdrFcShort( 0xec ),	/* Type Offset=236 */

	/* Parameter pichModMin */

/* 16788 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16790 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16792 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichModLim */

/* 16794 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16796 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16798 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichIP */

/* 16800 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16802 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 16804 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcactDfRemaining */

/* 16806 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16808 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 16810 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 16812 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16814 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 16816 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsValidInsertionPoint */

/* 16818 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16820 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16824 */	NdrFcShort( 0x7 ),	/* 7 */
/* 16826 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16828 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16830 */	NdrFcShort( 0x24 ),	/* 36 */
/* 16832 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 16834 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16836 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16840 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ich */

/* 16842 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16844 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16846 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ptss */

/* 16848 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 16850 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16852 */	NdrFcShort( 0x3c ),	/* Type Offset=60 */

	/* Parameter pfValid */

/* 16854 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16856 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16858 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 16860 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16862 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16864 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsFontAvailable */

/* 16866 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16868 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16872 */	NdrFcShort( 0x3 ),	/* 3 */
/* 16874 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16876 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16878 */	NdrFcShort( 0x22 ),	/* 34 */
/* 16880 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 16882 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 16884 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16886 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16888 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 16890 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 16892 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16894 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pfAvail */

/* 16896 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16898 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16900 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 16902 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16904 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16906 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IsFontAvailableRgch */

/* 16908 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16910 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16914 */	NdrFcShort( 0x4 ),	/* 4 */
/* 16916 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 16918 */	NdrFcShort( 0x22 ),	/* 34 */
/* 16920 */	NdrFcShort( 0x22 ),	/* 34 */
/* 16922 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 16924 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 16926 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16928 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16930 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cch */

/* 16932 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 16934 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16936 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchName */

/* 16938 */	NdrFcShort( 0x148 ),	/* Flags:  in, base type, simple ref, */
/* 16940 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16942 */	0x5,		/* FC_WCHAR */
			0x0,		/* 0 */

	/* Parameter pfAvail */

/* 16944 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 16946 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16948 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 16950 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16952 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 16954 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_String */


	/* Procedure get_Text */


	/* Procedure AvailableFonts */

/* 16956 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16958 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16962 */	NdrFcShort( 0x5 ),	/* 5 */
/* 16964 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 16966 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16968 */	NdrFcShort( 0x8 ),	/* 8 */
/* 16970 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 16972 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 16974 */	NdrFcShort( 0x1 ),	/* 1 */
/* 16976 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16978 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrString */


	/* Parameter pbstr */


	/* Parameter pbstrNames */

/* 16980 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 16982 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 16984 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */

/* 16986 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 16988 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 16990 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SortKey */

/* 16992 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 16994 */	NdrFcLong( 0x0 ),	/* 0 */
/* 16998 */	NdrFcShort( 0x3 ),	/* 3 */
/* 17000 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17002 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17004 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17006 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 17008 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 17010 */	NdrFcShort( 0x1 ),	/* 1 */
/* 17012 */	NdrFcShort( 0x1 ),	/* 1 */
/* 17014 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrValue */

/* 17016 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 17018 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17020 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter colopt */

/* 17022 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17024 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17026 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pbstrKey */

/* 17028 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 17030 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17032 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 17034 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17036 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17038 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SortKeyRgch */

/* 17040 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17042 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17046 */	NdrFcShort( 0x4 ),	/* 4 */
/* 17048 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 17050 */	NdrFcShort( 0x18 ),	/* 24 */
/* 17052 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17054 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 17056 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 17058 */	NdrFcShort( 0x1 ),	/* 1 */
/* 17060 */	NdrFcShort( 0x1 ),	/* 1 */
/* 17062 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pch */

/* 17064 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 17066 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17068 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 17070 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17072 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17074 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter colopt */

/* 17076 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17078 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17080 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter cchMaxOut */

/* 17082 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17084 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17086 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pchKey */

/* 17088 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 17090 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17092 */	NdrFcShort( 0x540 ),	/* Type Offset=1344 */

	/* Parameter pcchOut */

/* 17094 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17096 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 17098 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 17100 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17102 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 17104 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Compare */

/* 17106 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17108 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17112 */	NdrFcShort( 0x5 ),	/* 5 */
/* 17114 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 17116 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17118 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17120 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 17122 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 17124 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17126 */	NdrFcShort( 0x2 ),	/* 2 */
/* 17128 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrValue1 */

/* 17130 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 17132 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17134 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrValue2 */

/* 17136 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 17138 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17140 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter colopt */

/* 17142 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17144 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17146 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pnVal */

/* 17148 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17150 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17152 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 17154 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17156 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17158 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_WritingSystemFactory */

/* 17160 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17162 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17166 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17168 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17170 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17172 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17174 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 17176 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17178 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17180 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17182 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppwsf */

/* 17184 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 17186 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17188 */	NdrFcShort( 0x434 ),	/* Type Offset=1076 */

	/* Return value */

/* 17190 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17192 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17194 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_WritingSystemFactory */

/* 17196 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17198 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17202 */	NdrFcShort( 0x7 ),	/* 7 */
/* 17204 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17206 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17208 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17210 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 17212 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17214 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17216 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17218 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 17220 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 17222 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17224 */	NdrFcShort( 0x14c ),	/* Type Offset=332 */

	/* Return value */

/* 17226 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17228 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17230 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SortKeyVariant */

/* 17232 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17234 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17238 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17240 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17242 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17244 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17246 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 17248 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 17250 */	NdrFcShort( 0x20 ),	/* 32 */
/* 17252 */	NdrFcShort( 0x1 ),	/* 1 */
/* 17254 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrValue */

/* 17256 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 17258 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17260 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter colopt */

/* 17262 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17264 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17266 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter psaKey */

/* 17268 */	NdrFcShort( 0x4113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=16 */
/* 17270 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17272 */	NdrFcShort( 0x930 ),	/* Type Offset=2352 */

	/* Return value */

/* 17274 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17276 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17278 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure CompareVariant */

/* 17280 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17282 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17286 */	NdrFcShort( 0x9 ),	/* 9 */
/* 17288 */	NdrFcShort( 0x30 ),	/* x86 Stack size/offset = 48 */
/* 17290 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17292 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17294 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 17296 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 17298 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17300 */	NdrFcShort( 0x40 ),	/* 64 */
/* 17302 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter saValue1 */

/* 17304 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 17306 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17308 */	NdrFcShort( 0x93e ),	/* Type Offset=2366 */

	/* Parameter saValue2 */

/* 17310 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 17312 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 17314 */	NdrFcShort( 0x93e ),	/* Type Offset=2366 */

	/* Parameter colopt */

/* 17316 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17318 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 17320 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Parameter pnVal */

/* 17322 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17324 */	NdrFcShort( 0x28 ),	/* x86 Stack size/offset = 40 */
/* 17326 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 17328 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17330 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 17332 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Open */

/* 17334 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17336 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17340 */	NdrFcShort( 0xa ),	/* 10 */
/* 17342 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17346 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17348 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 17350 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 17352 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17354 */	NdrFcShort( 0x1 ),	/* 1 */
/* 17356 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrLocale */

/* 17358 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 17360 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17362 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 17364 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17366 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17368 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Close */

/* 17370 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17372 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17376 */	NdrFcShort( 0xb ),	/* 11 */
/* 17378 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17380 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17382 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17384 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 17386 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17390 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17392 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 17394 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17396 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17398 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_GeneralCategory */

/* 17400 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17402 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17406 */	NdrFcShort( 0x3 ),	/* 3 */
/* 17408 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17410 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17412 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17414 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17416 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17418 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17420 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17422 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17424 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17426 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17428 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcc */

/* 17430 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17432 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17434 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 17436 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17438 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17440 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_BidiCategory */

/* 17442 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17444 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17448 */	NdrFcShort( 0x4 ),	/* 4 */
/* 17450 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17452 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17454 */	NdrFcShort( 0x24 ),	/* 36 */
/* 17456 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17458 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17460 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17462 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17464 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17466 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17468 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbic */

/* 17472 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17474 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17476 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 17478 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17480 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17482 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsWordForming */

/* 17484 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17486 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17490 */	NdrFcShort( 0x6 ),	/* 6 */
/* 17492 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17494 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17496 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17498 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17500 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17502 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17504 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17506 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17508 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17510 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17512 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17514 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17516 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17518 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17520 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17522 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17524 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsPunctuation */

/* 17526 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17528 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17532 */	NdrFcShort( 0x7 ),	/* 7 */
/* 17534 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17536 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17538 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17540 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17542 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17544 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17546 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17548 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17550 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17552 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17554 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17556 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17558 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17560 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17562 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17564 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17566 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsNumber */

/* 17568 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17570 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17574 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17576 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17578 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17580 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17582 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17584 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17586 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17588 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17590 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17592 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17594 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17596 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17598 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17600 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17602 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17604 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17606 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17608 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsSeparator */

/* 17610 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17612 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17616 */	NdrFcShort( 0x9 ),	/* 9 */
/* 17618 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17620 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17622 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17624 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17626 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17628 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17630 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17632 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17634 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17636 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17638 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17640 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17642 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17644 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17646 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17648 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17650 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsSymbol */

/* 17652 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17654 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17658 */	NdrFcShort( 0xa ),	/* 10 */
/* 17660 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17662 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17664 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17666 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17668 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17670 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17672 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17674 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17676 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17678 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17680 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17682 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17684 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17686 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17688 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17690 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17692 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsMark */

/* 17694 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17696 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17700 */	NdrFcShort( 0xb ),	/* 11 */
/* 17702 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17704 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17706 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17708 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17710 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17712 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17714 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17716 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17718 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17720 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17722 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17724 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17726 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17728 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17730 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17732 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17734 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsOther */

/* 17736 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17738 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17742 */	NdrFcShort( 0xc ),	/* 12 */
/* 17744 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17746 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17748 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17750 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17752 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17754 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17756 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17758 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17760 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17762 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17764 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17766 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17768 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17770 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17772 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17774 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17776 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsUpper */

/* 17778 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17780 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17784 */	NdrFcShort( 0xd ),	/* 13 */
/* 17786 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17788 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17790 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17792 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17794 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17796 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17800 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17802 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17804 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17806 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17808 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17810 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17812 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17814 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17816 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17818 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsLower */

/* 17820 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17822 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17826 */	NdrFcShort( 0xe ),	/* 14 */
/* 17828 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17830 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17832 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17834 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17836 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17840 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17842 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17844 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17846 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17848 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17850 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17852 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17854 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17856 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17858 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17860 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsModifier */

/* 17862 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17864 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17868 */	NdrFcShort( 0x10 ),	/* 16 */
/* 17870 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17872 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17874 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17876 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17878 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17880 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17882 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17884 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17886 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17888 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17890 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17892 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17894 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17896 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17898 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17900 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17902 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsOtherLetter */

/* 17904 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17906 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17910 */	NdrFcShort( 0x11 ),	/* 17 */
/* 17912 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17914 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17916 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17918 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17920 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17922 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17926 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17928 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17930 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17932 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17934 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17936 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17938 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17940 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17942 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17944 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsOpen */

/* 17946 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17948 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17952 */	NdrFcShort( 0x12 ),	/* 18 */
/* 17954 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17956 */	NdrFcShort( 0x8 ),	/* 8 */
/* 17958 */	NdrFcShort( 0x22 ),	/* 34 */
/* 17960 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 17962 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 17964 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17966 */	NdrFcShort( 0x0 ),	/* 0 */
/* 17968 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 17970 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 17972 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 17974 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 17976 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 17978 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 17980 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 17982 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 17984 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 17986 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsClose */

/* 17988 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 17990 */	NdrFcLong( 0x0 ),	/* 0 */
/* 17994 */	NdrFcShort( 0x13 ),	/* 19 */
/* 17996 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 17998 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18000 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18002 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 18004 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18006 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18008 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18010 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18012 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18014 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18016 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 18018 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18020 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18022 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18024 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18026 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18028 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsWordMedial */

/* 18030 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18032 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18036 */	NdrFcShort( 0x14 ),	/* 20 */
/* 18038 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18040 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18042 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18044 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 18046 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18048 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18050 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18052 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18054 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18056 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18058 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 18060 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18062 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18064 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18066 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18068 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18070 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsControl */

/* 18072 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18074 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18078 */	NdrFcShort( 0x15 ),	/* 21 */
/* 18080 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18082 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18084 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18086 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 18088 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18090 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18092 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18094 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18096 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18098 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18100 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 18102 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18104 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18106 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18108 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18110 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ToLowerCh */

/* 18114 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18116 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18120 */	NdrFcShort( 0x16 ),	/* 22 */
/* 18122 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18124 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18126 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18128 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 18130 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18132 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18134 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18136 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18138 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18140 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18142 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pch */

/* 18144 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18146 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18148 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18150 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18152 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18154 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_ToUpperCh */

/* 18156 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18158 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18162 */	NdrFcShort( 0x17 ),	/* 23 */
/* 18164 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18166 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18168 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18170 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 18172 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18174 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18176 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18178 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18180 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18182 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18184 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pch */

/* 18186 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18188 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18190 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18192 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18194 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ToLower */

/* 18198 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18200 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18204 */	NdrFcShort( 0x19 ),	/* 25 */
/* 18206 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18208 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18210 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18212 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 18214 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18216 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18218 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18220 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 18222 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18224 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18226 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstr */

/* 18228 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18230 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18232 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18234 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18236 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18238 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ToUpper */

/* 18240 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18242 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18246 */	NdrFcShort( 0x1a ),	/* 26 */
/* 18248 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18250 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18252 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18254 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 18256 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18258 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18260 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18262 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 18264 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18266 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18268 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstr */

/* 18270 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18272 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18274 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18276 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18278 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18280 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ToTitle */

/* 18282 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18284 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18288 */	NdrFcShort( 0x1b ),	/* 27 */
/* 18290 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18292 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18294 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18296 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 18298 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18300 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18302 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18304 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 18306 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18308 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18310 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstr */

/* 18312 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18314 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18316 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18318 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18320 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18322 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ToLowerRgch */

/* 18324 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18326 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18330 */	NdrFcShort( 0x1c ),	/* 28 */
/* 18332 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18334 */	NdrFcShort( 0x10 ),	/* 16 */
/* 18336 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18338 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 18340 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18342 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18344 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18346 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 18348 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 18350 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18352 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 18354 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18356 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18358 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchOut */

/* 18360 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 18362 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18364 */	NdrFcShort( 0x33a ),	/* Type Offset=826 */

	/* Parameter cchOut */

/* 18366 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18368 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18370 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcchRet */

/* 18372 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18374 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18376 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18378 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18380 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18382 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ToUpperRgch */

/* 18384 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18386 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18390 */	NdrFcShort( 0x1d ),	/* 29 */
/* 18392 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18394 */	NdrFcShort( 0x10 ),	/* 16 */
/* 18396 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18398 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 18400 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18402 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18404 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18406 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 18408 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 18410 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18412 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 18414 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18416 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18418 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchOut */

/* 18420 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 18422 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18424 */	NdrFcShort( 0x33a ),	/* Type Offset=826 */

	/* Parameter cchOut */

/* 18426 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18428 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18430 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcchRet */

/* 18432 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18434 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18436 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18438 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18440 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18442 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ToTitleRgch */

/* 18444 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18446 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18450 */	NdrFcShort( 0x1e ),	/* 30 */
/* 18452 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18454 */	NdrFcShort( 0x10 ),	/* 16 */
/* 18456 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18458 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 18460 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18462 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18464 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18466 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 18468 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 18470 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18472 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 18474 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18476 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18478 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchOut */

/* 18480 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 18482 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18484 */	NdrFcShort( 0x33a ),	/* Type Offset=826 */

	/* Parameter cchOut */

/* 18486 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18488 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18490 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcchRet */

/* 18492 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18494 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18498 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18500 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18502 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_IsUserDefinedClass */

/* 18504 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18506 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18510 */	NdrFcShort( 0x1f ),	/* 31 */
/* 18512 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18514 */	NdrFcShort( 0x10 ),	/* 16 */
/* 18516 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18518 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 18520 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18522 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18524 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18526 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18528 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18530 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18532 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter chClass */

/* 18534 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18536 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfRet */

/* 18540 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18542 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18544 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18546 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18548 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18550 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_SoundAlikeKey */

/* 18552 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18554 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18558 */	NdrFcShort( 0x20 ),	/* 32 */
/* 18560 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18562 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18564 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18566 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 18568 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18570 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18572 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18574 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrValue */

/* 18576 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 18578 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18580 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstrKey */

/* 18582 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18584 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18586 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18588 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18590 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18592 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CharacterName */

/* 18594 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18596 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18600 */	NdrFcShort( 0x21 ),	/* 33 */
/* 18602 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18604 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18606 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18608 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 18610 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 18612 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18614 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18616 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18618 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18620 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18622 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrName */

/* 18624 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18626 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18628 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18630 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18632 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18634 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Decomposition */

/* 18636 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18638 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18642 */	NdrFcShort( 0x22 ),	/* 34 */
/* 18644 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18646 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18648 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18650 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 18652 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 18654 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18656 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18658 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18660 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18662 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18664 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 18666 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18668 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18670 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18672 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18674 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18676 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure DecompositionRgch */

/* 18678 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18680 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18684 */	NdrFcShort( 0x23 ),	/* 35 */
/* 18686 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18688 */	NdrFcShort( 0x10 ),	/* 16 */
/* 18690 */	NdrFcShort( 0x58 ),	/* 88 */
/* 18692 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 18694 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18696 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18698 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18700 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18702 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18704 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18706 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cchMax */

/* 18708 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18710 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18712 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 18714 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18716 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18718 */	0x5,		/* FC_WCHAR */
			0x0,		/* 0 */

	/* Parameter pcch */

/* 18720 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18722 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18724 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfHasDecomp */

/* 18726 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18728 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18730 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18732 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18734 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18736 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_FullDecomp */

/* 18738 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18740 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18744 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18746 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18748 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18750 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18752 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 18754 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 18756 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18758 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18760 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18762 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18764 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18766 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrOut */

/* 18768 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18770 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18772 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18774 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18776 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18778 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FullDecompRgch */

/* 18780 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18782 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18786 */	NdrFcShort( 0x25 ),	/* 37 */
/* 18788 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 18790 */	NdrFcShort( 0x10 ),	/* 16 */
/* 18792 */	NdrFcShort( 0x58 ),	/* 88 */
/* 18794 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x6,		/* 6 */
/* 18796 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18798 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18800 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18802 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18804 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18806 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18808 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cchMax */

/* 18810 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18812 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18814 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgch */

/* 18816 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18818 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18820 */	0x5,		/* FC_WCHAR */
			0x0,		/* 0 */

	/* Parameter pcch */

/* 18822 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18824 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18826 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfHasDecomp */

/* 18828 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18830 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18832 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 18834 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18836 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 18838 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NumericValue */

/* 18840 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18842 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18846 */	NdrFcShort( 0x26 ),	/* 38 */
/* 18848 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18850 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18852 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18854 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 18856 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18858 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18860 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18862 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18864 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18866 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18868 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pn */

/* 18870 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18872 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18874 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18876 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18878 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18880 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CombiningClass */

/* 18882 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18884 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18888 */	NdrFcShort( 0x27 ),	/* 39 */
/* 18890 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18892 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18894 */	NdrFcShort( 0x24 ),	/* 36 */
/* 18896 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x3,		/* 3 */
/* 18898 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 18900 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18904 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18906 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18908 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18910 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pn */

/* 18912 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 18914 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18916 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 18918 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18920 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18922 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Comment */

/* 18924 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18926 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18930 */	NdrFcShort( 0x28 ),	/* 40 */
/* 18932 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 18934 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18936 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18938 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 18940 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 18942 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18944 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18946 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ch */

/* 18948 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18950 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18952 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstr */

/* 18954 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 18956 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 18958 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 18960 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 18962 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 18964 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetLineBreakProps */

/* 18966 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 18968 */	NdrFcLong( 0x0 ),	/* 0 */
/* 18972 */	NdrFcShort( 0x29 ),	/* 41 */
/* 18974 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 18976 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18978 */	NdrFcShort( 0x8 ),	/* 8 */
/* 18980 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 18982 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 18984 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18986 */	NdrFcShort( 0x1 ),	/* 1 */
/* 18988 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 18990 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 18992 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 18994 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 18996 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 18998 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19000 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prglbOut */

/* 19002 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 19004 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19006 */	NdrFcShort( 0x950 ),	/* Type Offset=2384 */

	/* Return value */

/* 19008 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19010 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19012 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetLineBreakStatus */

/* 19014 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19016 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19020 */	NdrFcShort( 0x2a ),	/* 42 */
/* 19022 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19024 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19026 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19028 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 19030 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19032 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19034 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19036 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prglbpIn */

/* 19038 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19040 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19042 */	NdrFcShort( 0x140 ),	/* Type Offset=320 */

	/* Parameter cb */

/* 19044 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19046 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19048 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prglbsOut */

/* 19050 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 19052 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19054 */	NdrFcShort( 0x950 ),	/* Type Offset=2384 */

	/* Return value */

/* 19056 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19058 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19060 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetLineBreakInfo */

/* 19062 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19064 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19068 */	NdrFcShort( 0x2b ),	/* 43 */
/* 19070 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 19072 */	NdrFcShort( 0x18 ),	/* 24 */
/* 19074 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19076 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x7,		/* 7 */
/* 19078 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19080 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19082 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19084 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 19086 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19088 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19090 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 19092 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19094 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19096 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichMin */

/* 19098 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19100 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19102 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLim */

/* 19104 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19106 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19108 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prglbsOut */

/* 19110 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 19112 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19114 */	NdrFcShort( 0x960 ),	/* Type Offset=2400 */

	/* Parameter pichBreak */

/* 19116 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19118 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19120 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19122 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19124 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19126 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure StripDiacritics */

/* 19128 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19130 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19134 */	NdrFcShort( 0x2c ),	/* 44 */
/* 19136 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19138 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19140 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19142 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 19144 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19146 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19148 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19150 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 19152 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19154 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19156 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstr */

/* 19158 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 19160 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19162 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 19164 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19166 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19168 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure StripDiacriticsRgch */

/* 19170 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19172 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19176 */	NdrFcShort( 0x2d ),	/* 45 */
/* 19178 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19180 */	NdrFcShort( 0x10 ),	/* 16 */
/* 19182 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19184 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19186 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19188 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19190 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19192 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 19194 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19196 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19198 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 19200 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19202 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19204 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchOut */

/* 19206 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 19208 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19210 */	NdrFcShort( 0x33a ),	/* Type Offset=826 */

	/* Parameter cchMaxOut */

/* 19212 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19214 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19216 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcchOut */

/* 19218 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19220 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19222 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19224 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19226 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19228 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NormalizeKd */

/* 19230 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19232 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19236 */	NdrFcShort( 0x2e ),	/* 46 */
/* 19238 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19240 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19242 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19244 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 19246 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19248 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19250 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19252 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 19254 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19256 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19258 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstr */

/* 19260 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 19262 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19264 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 19266 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19268 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19270 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure NormalizeKdRgch */

/* 19272 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19274 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19278 */	NdrFcShort( 0x2f ),	/* 47 */
/* 19280 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19282 */	NdrFcShort( 0x10 ),	/* 16 */
/* 19284 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19286 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19288 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19290 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19292 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19294 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 19296 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19298 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19300 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchIn */

/* 19302 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19304 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19306 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchOut */

/* 19308 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 19310 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19312 */	NdrFcShort( 0x33a ),	/* Type Offset=826 */

	/* Parameter cchMaxOut */

/* 19314 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19316 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19318 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pcchOut */

/* 19320 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19322 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19324 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19326 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19328 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19330 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Locale */

/* 19332 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19334 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19338 */	NdrFcShort( 0x30 ),	/* 48 */
/* 19340 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19344 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19346 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 19348 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19350 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19352 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19354 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnLocale */

/* 19356 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19358 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19360 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19362 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19364 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19366 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_Locale */

/* 19368 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19370 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19374 */	NdrFcShort( 0x31 ),	/* 49 */
/* 19376 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19378 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19380 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19382 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 19384 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19386 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19388 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19390 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nLocale */

/* 19392 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19394 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19396 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19398 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19400 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19402 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetLineBreakText */

/* 19404 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19406 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19410 */	NdrFcShort( 0x32 ),	/* 50 */
/* 19412 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19414 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19416 */	NdrFcShort( 0x3e ),	/* 62 */
/* 19418 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 19420 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19422 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19424 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19426 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter cchMax */

/* 19428 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19430 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19432 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchOut */

/* 19434 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19436 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19438 */	0x5,		/* FC_WCHAR */
			0x0,		/* 0 */

	/* Parameter pcchOut */

/* 19440 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19442 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19444 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19446 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19448 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19450 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_LineBreakText */

/* 19452 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19454 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19458 */	NdrFcShort( 0x33 ),	/* 51 */
/* 19460 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19462 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19464 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19466 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 19468 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19470 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19472 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19474 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchIn */

/* 19476 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19478 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19480 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchMax */

/* 19482 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19484 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19486 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19488 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19490 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19492 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LineBreakBefore */

/* 19494 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19496 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19500 */	NdrFcShort( 0x34 ),	/* 52 */
/* 19502 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19504 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19506 */	NdrFcShort( 0x40 ),	/* 64 */
/* 19508 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 19510 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19512 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19514 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19516 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichIn */

/* 19518 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19520 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19522 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichOut */

/* 19524 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19526 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19528 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter plbWeight */

/* 19530 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19532 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19534 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 19536 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19538 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19540 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure LineBreakAfter */

/* 19542 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19544 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19548 */	NdrFcShort( 0x35 ),	/* 53 */
/* 19550 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19552 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19554 */	NdrFcShort( 0x40 ),	/* 64 */
/* 19556 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x4,		/* 4 */
/* 19558 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 19560 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19562 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19564 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ichIn */

/* 19566 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19568 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19570 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichOut */

/* 19572 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19574 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19576 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter plbWeight */

/* 19578 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19580 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19582 */	0xe,		/* FC_ENUM32 */
			0x0,		/* 0 */

	/* Return value */

/* 19584 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19586 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19588 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetPattern */

/* 19590 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19592 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19596 */	NdrFcShort( 0x3 ),	/* 3 */
/* 19598 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19600 */	NdrFcShort( 0x18 ),	/* 24 */
/* 19602 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19604 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19606 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19608 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19610 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19612 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrPattern */

/* 19614 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19616 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19618 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter fIgnoreCase */

/* 19620 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19622 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19624 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fIgnoreModifiers */

/* 19626 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19628 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19630 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fUseSoundAlike */

/* 19632 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19634 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19636 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter fUseWildCards */

/* 19638 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19640 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19642 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 19644 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19646 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19648 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ShowPatternDialog */

/* 19650 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19652 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19656 */	NdrFcShort( 0x5 ),	/* 5 */
/* 19658 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19660 */	NdrFcShort( 0x6 ),	/* 6 */
/* 19662 */	NdrFcShort( 0x22 ),	/* 34 */
/* 19664 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 19666 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19668 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19670 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19672 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrTitle */

/* 19674 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19676 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19678 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pwse */

/* 19680 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 19682 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19684 */	NdrFcShort( 0x96c ),	/* Type Offset=2412 */

	/* Parameter fForReplace */

/* 19686 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19688 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19690 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pfGoAhead */

/* 19692 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19694 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19696 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 19698 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19700 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19702 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FindString */

/* 19704 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19706 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19710 */	NdrFcShort( 0x6 ),	/* 6 */
/* 19712 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19714 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19716 */	NdrFcShort( 0x5a ),	/* 90 */
/* 19718 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19720 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19722 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19724 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19726 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSource */

/* 19728 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19730 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19732 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter ichFirst */

/* 19734 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19736 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19738 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichMinFound */

/* 19740 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19742 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19744 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLimFound */

/* 19746 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19748 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19750 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pfFound */

/* 19752 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19754 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19756 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 19758 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19760 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19762 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FindReplace */

/* 19764 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19766 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19770 */	NdrFcShort( 0x7 ),	/* 7 */
/* 19772 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19774 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19776 */	NdrFcShort( 0x40 ),	/* 64 */
/* 19778 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19780 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19782 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19784 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19786 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSource */

/* 19788 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19790 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19792 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter ichFirst */

/* 19794 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19796 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19798 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichMinFound */

/* 19800 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19802 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19804 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ichLimFound */

/* 19806 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19808 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19810 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrReplacement */

/* 19812 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 19814 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19816 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 19818 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19820 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19822 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ConvertString */

/* 19824 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19826 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19830 */	NdrFcShort( 0x3 ),	/* 3 */
/* 19832 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19834 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19836 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19838 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 19840 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19842 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19844 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19846 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrIn */

/* 19848 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 19850 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19852 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstrOut */

/* 19854 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 19856 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19858 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 19860 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19862 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19864 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure ConvertStringRgch */

/* 19866 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19868 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19872 */	NdrFcShort( 0x4 ),	/* 4 */
/* 19874 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 19876 */	NdrFcShort( 0x10 ),	/* 16 */
/* 19878 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19880 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 19882 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 19884 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19886 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19888 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgch */

/* 19890 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19892 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19894 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cch */

/* 19896 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19898 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19900 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter cchMax */

/* 19902 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19904 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19906 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter prgchOut */

/* 19908 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 19910 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19912 */	NdrFcShort( 0x30c ),	/* Type Offset=780 */

	/* Parameter pcchOut */

/* 19914 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19916 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19918 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19920 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19922 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19924 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetToken */

/* 19926 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19928 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19932 */	NdrFcShort( 0x3 ),	/* 3 */
/* 19934 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 19936 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19938 */	NdrFcShort( 0x40 ),	/* 64 */
/* 19940 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 19942 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19944 */	NdrFcShort( 0x0 ),	/* 0 */
/* 19946 */	NdrFcShort( 0x1 ),	/* 1 */
/* 19948 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchInput */

/* 19950 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 19952 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 19954 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cch */

/* 19956 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 19958 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 19960 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichMin */

/* 19962 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19964 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 19966 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 19968 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 19970 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 19972 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 19974 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 19976 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19978 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_TokenStart */

/* 19980 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 19982 */	NdrFcLong( 0x0 ),	/* 0 */
/* 19986 */	NdrFcShort( 0x4 ),	/* 4 */
/* 19988 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 19990 */	NdrFcShort( 0x8 ),	/* 8 */
/* 19992 */	NdrFcShort( 0x24 ),	/* 36 */
/* 19994 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 19996 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 19998 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20000 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20002 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrInput */

/* 20004 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 20006 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20008 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter ichFirst */

/* 20010 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20012 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20014 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichMin */

/* 20016 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20018 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20020 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 20022 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20024 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20026 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_TokenEnd */

/* 20028 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20030 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20034 */	NdrFcShort( 0x5 ),	/* 5 */
/* 20036 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20038 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20040 */	NdrFcShort( 0x24 ),	/* 36 */
/* 20042 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 20044 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 20046 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20048 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20050 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrInput */

/* 20052 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 20054 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20056 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter ichFirst */

/* 20058 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20060 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20062 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLim */

/* 20064 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20066 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20068 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 20070 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20072 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20074 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 20076 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20078 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20082 */	NdrFcShort( 0x3 ),	/* 3 */
/* 20084 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20086 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20088 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20090 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 20092 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20094 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20096 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20098 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszwCustom */

/* 20100 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20102 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20104 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Return value */

/* 20106 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20108 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20110 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Check */

/* 20112 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20114 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20118 */	NdrFcShort( 0x5 ),	/* 5 */
/* 20120 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 20122 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20124 */	NdrFcShort( 0x5c ),	/* 92 */
/* 20126 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x8,		/* 8 */
/* 20128 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 20130 */	NdrFcShort( 0x2 ),	/* 2 */
/* 20132 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20134 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchw */

/* 20136 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20138 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20140 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchw */

/* 20142 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20144 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20146 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichMinBad */

/* 20148 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20150 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20152 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichLimBad */

/* 20154 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20156 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20158 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrBad */

/* 20160 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 20162 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20164 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pbstrSuggest */

/* 20166 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 20168 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 20170 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pscrs */

/* 20172 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20174 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 20176 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 20178 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20180 */	NdrFcShort( 0x20 ),	/* x86 Stack size/offset = 32 */
/* 20182 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Suggest */

/* 20184 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20186 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20190 */	NdrFcShort( 0x6 ),	/* 6 */
/* 20192 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 20194 */	NdrFcShort( 0xe ),	/* 14 */
/* 20196 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20198 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 20200 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 20202 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20204 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20206 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgchw */

/* 20208 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20210 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20212 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cchw */

/* 20214 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20216 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20218 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter fFirst */

/* 20220 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20222 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20224 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Parameter pbstrSuggest */

/* 20226 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 20228 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20230 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 20232 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20234 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20236 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure IgnoreAll */

/* 20238 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20240 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20244 */	NdrFcShort( 0x7 ),	/* 7 */
/* 20246 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20248 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20250 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20252 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 20254 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20256 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20258 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20260 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszw */

/* 20262 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20264 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20266 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Return value */

/* 20268 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20270 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20272 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Change */

/* 20274 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20276 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20280 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20282 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20284 */	NdrFcShort( 0x6 ),	/* 6 */
/* 20286 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20288 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 20290 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20292 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20294 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20296 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszwSrc */

/* 20298 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20300 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20302 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter pszwDst */

/* 20304 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20306 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20308 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Parameter fAll */

/* 20310 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20312 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20314 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 20316 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20318 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20320 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddToUser */

/* 20322 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20324 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20328 */	NdrFcShort( 0x9 ),	/* 9 */
/* 20330 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20332 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20334 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20336 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 20338 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20340 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20344 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszw */

/* 20346 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20348 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20350 */	NdrFcShort( 0x244 ),	/* Type Offset=580 */

	/* Return value */

/* 20352 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20354 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20356 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure FlushChangeList */

/* 20358 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20360 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20364 */	NdrFcShort( 0xb ),	/* 11 */
/* 20366 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20368 */	NdrFcShort( 0x6 ),	/* 6 */
/* 20370 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20372 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 20374 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20376 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20378 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20380 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fAll */

/* 20382 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20384 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20386 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 20388 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20390 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20392 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Checker */

/* 20394 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20396 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20400 */	NdrFcShort( 0x3 ),	/* 3 */
/* 20402 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20404 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20406 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20408 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 20410 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20412 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20416 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ppspchk */

/* 20418 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 20420 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20422 */	NdrFcShort( 0x97e ),	/* Type Offset=2430 */

	/* Return value */

/* 20424 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20426 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20428 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Name */


	/* Procedure get_TransliteratorName */


	/* Procedure get_ConverterName */


	/* Procedure get_IntToPrettyString */

/* 20430 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20432 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20436 */	NdrFcShort( 0x4 ),	/* 4 */
/* 20438 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20440 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20442 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20444 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 20446 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 20448 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20450 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20452 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iloc */


	/* Parameter itrans */


	/* Parameter iconv */


	/* Parameter n */

/* 20454 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20456 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20458 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrName */


	/* Parameter pbstrName */


	/* Parameter pbstrName */


	/* Parameter bstr */

/* 20460 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 20462 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20464 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 20466 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20468 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20470 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StringToInt */

/* 20472 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20474 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20478 */	NdrFcShort( 0x5 ),	/* 5 */
/* 20480 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20482 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20484 */	NdrFcShort( 0x24 ),	/* 36 */
/* 20486 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 20488 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 20490 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20492 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20494 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 20496 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 20498 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20500 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pn */

/* 20502 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20504 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20506 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 20508 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20510 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20512 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure StringToIntRgch */

/* 20514 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20516 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20520 */	NdrFcShort( 0x6 ),	/* 6 */
/* 20522 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 20524 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20526 */	NdrFcShort( 0x40 ),	/* 64 */
/* 20528 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 20530 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 20532 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20534 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20536 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgch */

/* 20538 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20540 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20542 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cch */

/* 20544 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20546 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20548 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pn */

/* 20550 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20552 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20554 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pichUnused */

/* 20556 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20558 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20560 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 20562 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20564 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20566 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DblToString */

/* 20568 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20570 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20574 */	NdrFcShort( 0x7 ),	/* 7 */
/* 20576 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 20578 */	NdrFcShort( 0x18 ),	/* 24 */
/* 20580 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20582 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 20584 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 20586 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20588 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20590 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter dbl */

/* 20592 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20594 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20596 */	0xc,		/* FC_DOUBLE */
			0x0,		/* 0 */

	/* Parameter cchFracDigits */

/* 20598 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20600 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20602 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */

/* 20604 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 20606 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20608 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 20610 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20612 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20614 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DblToPrettyString */

/* 20616 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20618 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20622 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20624 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 20626 */	NdrFcShort( 0x18 ),	/* 24 */
/* 20628 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20630 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 20632 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 20634 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20636 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20638 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter dbl */

/* 20640 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20642 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20644 */	0xc,		/* FC_DOUBLE */
			0x0,		/* 0 */

	/* Parameter cchFracDigits */

/* 20646 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20648 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20650 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */

/* 20652 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 20654 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20656 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 20658 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20660 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20662 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DblToExpString */

/* 20664 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20666 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20670 */	NdrFcShort( 0x9 ),	/* 9 */
/* 20672 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 20674 */	NdrFcShort( 0x18 ),	/* 24 */
/* 20676 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20678 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x4,		/* 4 */
/* 20680 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 20682 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20684 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20686 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter dbl */

/* 20688 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20690 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20692 */	0xc,		/* FC_DOUBLE */
			0x0,		/* 0 */

	/* Parameter cchFracDigits */

/* 20694 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20696 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20698 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstr */

/* 20700 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 20702 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20704 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 20706 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20708 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20710 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_StringToDbl */

/* 20712 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20714 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20718 */	NdrFcShort( 0xa ),	/* 10 */
/* 20720 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20722 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20724 */	NdrFcShort( 0x2c ),	/* 44 */
/* 20726 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 20728 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 20730 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20732 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20734 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 20736 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 20738 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20740 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pdbl */

/* 20742 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20744 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20746 */	0xc,		/* FC_DOUBLE */
			0x0,		/* 0 */

	/* Return value */

/* 20748 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20750 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20752 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure StringToDblRgch */

/* 20754 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20756 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20760 */	NdrFcShort( 0xb ),	/* 11 */
/* 20762 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 20764 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20766 */	NdrFcShort( 0x48 ),	/* 72 */
/* 20768 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 20770 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 20772 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20774 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20776 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter prgch */

/* 20778 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 20780 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20782 */	NdrFcShort( 0x530 ),	/* Type Offset=1328 */

	/* Parameter cch */

/* 20784 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20786 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20788 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pdbl */

/* 20790 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20792 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20794 */	0xc,		/* FC_DOUBLE */
			0x0,		/* 0 */

	/* Parameter pichUnused */

/* 20796 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20798 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20800 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 20802 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20804 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 20806 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Engine */

/* 20808 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20810 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20814 */	NdrFcShort( 0x3 ),	/* 3 */
/* 20816 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20818 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20820 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20822 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 20824 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 20826 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20828 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20830 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrIcuLocale */

/* 20832 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 20834 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20836 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter ppwseng */

/* 20838 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 20840 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20842 */	NdrFcShort( 0x994 ),	/* Type Offset=2452 */

	/* Return value */

/* 20844 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20846 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20848 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_EngineOrNull */

/* 20850 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20852 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20856 */	NdrFcShort( 0x4 ),	/* 4 */
/* 20858 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20860 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20862 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20864 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 20866 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20868 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20870 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20872 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 20874 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 20876 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20878 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppwseng */

/* 20880 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 20882 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20884 */	NdrFcShort( 0x994 ),	/* Type Offset=2452 */

	/* Return value */

/* 20886 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20888 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20890 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure AddEngine */

/* 20892 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20894 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20898 */	NdrFcShort( 0x5 ),	/* 5 */
/* 20900 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20902 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20904 */	NdrFcShort( 0x8 ),	/* 8 */
/* 20906 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 20908 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20910 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20912 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20914 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwseng */

/* 20916 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 20918 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20920 */	NdrFcShort( 0x96c ),	/* Type Offset=2412 */

	/* Return value */

/* 20922 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20924 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20926 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetWsFromStr */

/* 20928 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20930 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20934 */	NdrFcShort( 0x7 ),	/* 7 */
/* 20936 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 20938 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20940 */	NdrFcShort( 0x24 ),	/* 36 */
/* 20942 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 20944 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 20946 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20948 */	NdrFcShort( 0x1 ),	/* 1 */
/* 20950 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstr */

/* 20952 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 20954 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20956 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pwsId */

/* 20958 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20960 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 20962 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 20964 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 20966 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20968 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_NumberOfWs */

/* 20970 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 20972 */	NdrFcLong( 0x0 ),	/* 0 */
/* 20976 */	NdrFcShort( 0x9 ),	/* 9 */
/* 20978 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 20980 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20982 */	NdrFcShort( 0x24 ),	/* 36 */
/* 20984 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 20986 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 20988 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20990 */	NdrFcShort( 0x0 ),	/* 0 */
/* 20992 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pcws */

/* 20994 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 20996 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 20998 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 21000 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21002 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21004 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetWritingSystems */

/* 21006 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21008 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21012 */	NdrFcShort( 0xa ),	/* 10 */
/* 21014 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21016 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21018 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21020 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 21022 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 21024 */	NdrFcShort( 0x1 ),	/* 1 */
/* 21026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21028 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter rgws */

/* 21030 */	NdrFcShort( 0x113 ),	/* Flags:  must size, must free, out, simple ref, */
/* 21032 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21034 */	NdrFcShort( 0x99c ),	/* Type Offset=2460 */

	/* Parameter cws */

/* 21036 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21038 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21040 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Return value */

/* 21042 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21044 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21046 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_UnicodeCharProps */

/* 21048 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21050 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21054 */	NdrFcShort( 0xb ),	/* 11 */
/* 21056 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21058 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21060 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21062 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 21064 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21066 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21068 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21070 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pplcpe */

/* 21072 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21074 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21076 */	NdrFcShort( 0x9a8 ),	/* Type Offset=2472 */

	/* Return value */

/* 21078 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21080 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21082 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DefaultCollater */

/* 21084 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21086 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21090 */	NdrFcShort( 0xc ),	/* 12 */
/* 21092 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21094 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21096 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21098 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 21100 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21102 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21104 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21106 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 21108 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21110 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21112 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter ppcoleng */

/* 21114 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21116 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21118 */	NdrFcShort( 0x4f8 ),	/* Type Offset=1272 */

	/* Return value */

/* 21120 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21122 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21124 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_CharPropEngine */

/* 21126 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21128 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21132 */	NdrFcShort( 0xd ),	/* 13 */
/* 21134 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21136 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21138 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21140 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 21142 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21144 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21146 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21148 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 21150 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21152 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21154 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pplcpe */

/* 21156 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21158 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21160 */	NdrFcShort( 0x50e ),	/* Type Offset=1294 */

	/* Return value */

/* 21162 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21164 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21166 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Renderer */

/* 21168 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21170 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21174 */	NdrFcShort( 0xe ),	/* 14 */
/* 21176 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 21178 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21180 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21182 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 21184 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21186 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21188 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21190 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ws */

/* 21192 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21194 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21196 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pvg */

/* 21198 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21200 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21202 */	NdrFcShort( 0x9be ),	/* Type Offset=2494 */

	/* Parameter ppre */

/* 21204 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21206 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21208 */	NdrFcShort( 0x9d0 ),	/* Type Offset=2512 */

	/* Return value */

/* 21210 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21212 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21214 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_RendererFromChrp */

/* 21216 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21218 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21222 */	NdrFcShort( 0xf ),	/* 15 */
/* 21224 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21226 */	NdrFcShort( 0x134 ),	/* 308 */
/* 21228 */	NdrFcShort( 0x13c ),	/* 316 */
/* 21230 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 21232 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21234 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21236 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21238 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pchrp */

/* 21240 */	NdrFcShort( 0x11a ),	/* Flags:  must free, in, out, simple ref, */
/* 21242 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21244 */	NdrFcShort( 0x356 ),	/* Type Offset=854 */

	/* Parameter ppre */

/* 21246 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21248 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21250 */	NdrFcShort( 0x9d0 ),	/* Type Offset=2512 */

	/* Return value */

/* 21252 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21254 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21256 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Clear */

/* 21258 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21260 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21264 */	NdrFcShort( 0x11 ),	/* 17 */
/* 21266 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21268 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21270 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21272 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x1,		/* 1 */
/* 21274 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21276 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21278 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21280 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Return value */

/* 21282 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21284 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21286 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Serialize */

/* 21288 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21290 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21294 */	NdrFcShort( 0x13 ),	/* 19 */
/* 21296 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21298 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21300 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21302 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 21304 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21306 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21308 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21310 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 21312 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21314 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21316 */	NdrFcShort( 0x9e6 ),	/* Type Offset=2534 */

	/* Return value */

/* 21318 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21320 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21322 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_BypassInstall */

/* 21324 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21326 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21330 */	NdrFcShort( 0x15 ),	/* 21 */
/* 21332 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21334 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21336 */	NdrFcShort( 0x22 ),	/* 34 */
/* 21338 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 21340 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21342 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21344 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21346 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfBypass */

/* 21348 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 21350 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21352 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 21354 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21356 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21358 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_BypassInstall */

/* 21360 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21362 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21366 */	NdrFcShort( 0x16 ),	/* 22 */
/* 21368 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21370 */	NdrFcShort( 0x6 ),	/* 6 */
/* 21372 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21374 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 21376 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21378 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21380 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21382 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter fBypass */

/* 21384 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21386 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21388 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 21390 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21392 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21394 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetWritingSystemFactory */

/* 21396 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21398 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21402 */	NdrFcShort( 0x3 ),	/* 3 */
/* 21404 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 21406 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21408 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21410 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 21412 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21414 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21416 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21418 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pode */

/* 21420 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21422 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21424 */	NdrFcShort( 0x2c0 ),	/* Type Offset=704 */

	/* Parameter pfistLog */

/* 21426 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21428 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21430 */	NdrFcShort( 0x9f8 ),	/* Type Offset=2552 */

	/* Parameter ppwsf */

/* 21432 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21434 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21436 */	NdrFcShort( 0xa0a ),	/* Type Offset=2570 */

	/* Return value */

/* 21438 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21440 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21442 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure GetWritingSystemFactoryNew */

/* 21444 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21446 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21450 */	NdrFcShort( 0x4 ),	/* 4 */
/* 21452 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 21454 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21456 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21458 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x5,		/* 5 */
/* 21460 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 21462 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21464 */	NdrFcShort( 0x2 ),	/* 2 */
/* 21466 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrServer */

/* 21468 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 21470 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21472 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter bstrDatabase */

/* 21474 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 21476 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21478 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pfistLog */

/* 21480 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21482 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21484 */	NdrFcShort( 0x9f8 ),	/* Type Offset=2552 */

	/* Parameter ppwsf */

/* 21486 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21488 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21490 */	NdrFcShort( 0xa0a ),	/* Type Offset=2570 */

	/* Return value */

/* 21492 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21494 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 21496 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Deserialize */

/* 21498 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21500 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21504 */	NdrFcShort( 0x5 ),	/* 5 */
/* 21506 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21508 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21510 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21512 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 21514 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21516 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21518 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21520 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 21522 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21524 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21526 */	NdrFcShort( 0xa20 ),	/* Type Offset=2592 */

	/* Parameter ppwsf */

/* 21528 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21530 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21532 */	NdrFcShort( 0xa32 ),	/* Type Offset=2610 */

	/* Return value */

/* 21534 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21536 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21538 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_String */

/* 21540 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21542 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21546 */	NdrFcShort( 0x3 ),	/* 3 */
/* 21548 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21550 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21552 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21554 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 21556 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21558 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21560 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21562 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 21564 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21566 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21568 */	NdrFcShort( 0xa36 ),	/* Type Offset=2614 */

	/* Parameter pptss */

/* 21570 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 21572 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21574 */	NdrFcShort( 0xa48 ),	/* Type Offset=2632 */

	/* Return value */

/* 21576 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21578 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21580 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure putref_String */

/* 21582 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21584 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21588 */	NdrFcShort( 0x4 ),	/* 4 */
/* 21590 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21592 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21594 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21596 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 21598 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21600 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21602 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21604 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pwsf */

/* 21606 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21608 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21610 */	NdrFcShort( 0xa36 ),	/* Type Offset=2614 */

	/* Parameter ptss */

/* 21612 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21614 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21616 */	NdrFcShort( 0xa4c ),	/* Type Offset=2636 */

	/* Return value */

/* 21618 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21620 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21622 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Serialize */

/* 21624 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21626 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21630 */	NdrFcShort( 0x6 ),	/* 6 */
/* 21632 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21634 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21636 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21638 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 21640 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21642 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21644 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21646 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 21648 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21650 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21652 */	NdrFcShort( 0xa20 ),	/* Type Offset=2592 */

	/* Return value */

/* 21654 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21656 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21658 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Deserialize */

/* 21660 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21662 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21666 */	NdrFcShort( 0x7 ),	/* 7 */
/* 21668 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21670 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21672 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21674 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 21676 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21678 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21680 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21682 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pstg */

/* 21684 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21686 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21688 */	NdrFcShort( 0xa20 ),	/* Type Offset=2592 */

	/* Return value */

/* 21690 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21692 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21694 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Init */

/* 21696 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21698 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21702 */	NdrFcShort( 0x3 ),	/* 3 */
/* 21704 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21706 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21708 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21710 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 21712 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 21714 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21716 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21718 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter ptsswss */

/* 21720 */	NdrFcShort( 0xb ),	/* Flags:  must size, must free, in, */
/* 21722 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21724 */	NdrFcShort( 0xa5e ),	/* Type Offset=2654 */

	/* Return value */

/* 21726 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21728 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21730 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Language */


	/* Procedure get_TransliteratorId */


	/* Procedure get_ConverterId */


	/* Procedure get_Name */

/* 21732 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21734 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21738 */	NdrFcShort( 0x5 ),	/* 5 */
/* 21740 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21742 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21744 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21746 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 21748 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 21750 */	NdrFcShort( 0x1 ),	/* 1 */
/* 21752 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21754 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iloc */


	/* Parameter iconv */


	/* Parameter iconv */


	/* Parameter ilayout */

/* 21756 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21758 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21760 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrName */


	/* Parameter pbstrName */


	/* Parameter pbstrName */


	/* Parameter pbstrName */

/* 21762 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 21764 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21766 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */


	/* Return value */


	/* Return value */

/* 21768 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21770 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21772 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Name */


	/* Procedure get_ActiveKeyboardName */

/* 21774 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21776 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21780 */	NdrFcShort( 0x6 ),	/* 6 */
/* 21782 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21784 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21786 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21788 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 21790 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 21792 */	NdrFcShort( 0x1 ),	/* 1 */
/* 21794 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21796 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pbstrName */


	/* Parameter pbstrName */

/* 21798 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 21800 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21802 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 21804 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21806 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21808 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure put_ActiveKeyboardName */

/* 21810 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21812 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21816 */	NdrFcShort( 0x7 ),	/* 7 */
/* 21818 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21820 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21822 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21824 */	0x46,		/* Oi2 Flags:  clt must size, has return, has ext, */
			0x2,		/* 2 */
/* 21826 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 21828 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21830 */	NdrFcShort( 0x1 ),	/* 1 */
/* 21832 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrName */

/* 21834 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 21836 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21838 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Return value */

/* 21840 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21842 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21844 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure SetKeyboard */

/* 21846 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21848 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21852 */	NdrFcShort( 0x3 ),	/* 3 */
/* 21854 */	NdrFcShort( 0x1c ),	/* x86 Stack size/offset = 28 */
/* 21856 */	NdrFcShort( 0x3e ),	/* 62 */
/* 21858 */	NdrFcShort( 0x3e ),	/* 62 */
/* 21860 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x6,		/* 6 */
/* 21862 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 21864 */	NdrFcShort( 0x1 ),	/* 1 */
/* 21866 */	NdrFcShort( 0x2 ),	/* 2 */
/* 21868 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter nLcid */

/* 21870 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21872 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21874 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrKeymanKbd */

/* 21876 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 21878 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21880 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pnActiveLangId */

/* 21882 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 21884 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21886 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrActiveKeymanKbd */

/* 21888 */	NdrFcShort( 0x11b ),	/* Flags:  must size, must free, in, out, simple ref, */
/* 21890 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21892 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Parameter pfSelectLangPending */

/* 21894 */	NdrFcShort( 0x158 ),	/* Flags:  in, out, base type, simple ref, */
/* 21896 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 21898 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 21900 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21902 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 21904 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure Next */


	/* Procedure Next */

/* 21906 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21908 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21912 */	NdrFcShort( 0x4 ),	/* 4 */
/* 21914 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21916 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21918 */	NdrFcShort( 0x24 ),	/* 36 */
/* 21920 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 21922 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 21924 */	NdrFcShort( 0x1 ),	/* 1 */
/* 21926 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21928 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pnId */


	/* Parameter pnId */

/* 21930 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 21932 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21934 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrName */


	/* Parameter pbstrName */

/* 21936 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 21938 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21940 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */


	/* Return value */

/* 21942 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21944 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21946 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Variant */

/* 21948 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21950 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21954 */	NdrFcShort( 0x7 ),	/* 7 */
/* 21956 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 21958 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21960 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21962 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x3,		/* 3 */
/* 21964 */	0x8,		/* 8 */
			0x3,		/* Ext Flags:  new corr desc, clt corr check, */
/* 21966 */	NdrFcShort( 0x1 ),	/* 1 */
/* 21968 */	NdrFcShort( 0x0 ),	/* 0 */
/* 21970 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iloc */

/* 21972 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 21974 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 21976 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter pbstrName */

/* 21978 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 21980 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 21982 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 21984 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 21986 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 21988 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_DisplayName */

/* 21990 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 21992 */	NdrFcLong( 0x0 ),	/* 0 */
/* 21996 */	NdrFcShort( 0x8 ),	/* 8 */
/* 21998 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 22000 */	NdrFcShort( 0x8 ),	/* 8 */
/* 22002 */	NdrFcShort( 0x8 ),	/* 8 */
/* 22004 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x4,		/* 4 */
/* 22006 */	0x8,		/* 8 */
			0x7,		/* Ext Flags:  new corr desc, clt corr check, srv corr check, */
/* 22008 */	NdrFcShort( 0x1 ),	/* 1 */
/* 22010 */	NdrFcShort( 0x1 ),	/* 1 */
/* 22012 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter iloc */

/* 22014 */	NdrFcShort( 0x48 ),	/* Flags:  in, base type, */
/* 22016 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 22018 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Parameter bstrLocaleName */

/* 22020 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 22022 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 22024 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pbstrName */

/* 22026 */	NdrFcShort( 0x2113 ),	/* Flags:  must size, must free, out, simple ref, srv alloc size=8 */
/* 22028 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 22030 */	NdrFcShort( 0x56 ),	/* Type Offset=86 */

	/* Return value */

/* 22032 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 22034 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 22036 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_GetSubsection */

/* 22038 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 22040 */	NdrFcLong( 0x0 ),	/* 0 */
/* 22044 */	NdrFcShort( 0x7 ),	/* 7 */
/* 22046 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 22048 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22050 */	NdrFcShort( 0x8 ),	/* 8 */
/* 22052 */	0x47,		/* Oi2 Flags:  srv must size, clt must size, has return, has ext, */
			0x3,		/* 3 */
/* 22054 */	0x8,		/* 8 */
			0x5,		/* Ext Flags:  new corr desc, srv corr check, */
/* 22056 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22058 */	NdrFcShort( 0x1 ),	/* 1 */
/* 22060 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter bstrSectionName */

/* 22062 */	NdrFcShort( 0x8b ),	/* Flags:  must size, must free, in, by val, */
/* 22064 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 22066 */	NdrFcShort( 0x1c ),	/* Type Offset=28 */

	/* Parameter pprb */

/* 22068 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 22070 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 22072 */	NdrFcShort( 0xa74 ),	/* Type Offset=2676 */

	/* Return value */

/* 22074 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 22076 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 22078 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_HasNext */

/* 22080 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 22082 */	NdrFcLong( 0x0 ),	/* 0 */
/* 22086 */	NdrFcShort( 0x8 ),	/* 8 */
/* 22088 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 22090 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22092 */	NdrFcShort( 0x22 ),	/* 34 */
/* 22094 */	0x44,		/* Oi2 Flags:  has return, has ext, */
			0x2,		/* 2 */
/* 22096 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 22098 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22100 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22102 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pfHasNext */

/* 22104 */	NdrFcShort( 0x2150 ),	/* Flags:  out, base type, simple ref, srv alloc size=8 */
/* 22106 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 22108 */	0x6,		/* FC_SHORT */
			0x0,		/* 0 */

	/* Return value */

/* 22110 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 22112 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 22114 */	0x8,		/* FC_LONG */
			0x0,		/* 0 */

	/* Procedure get_Next */

/* 22116 */	0x33,		/* FC_AUTO_HANDLE */
			0x6c,		/* Old Flags:  object, Oi2 */
/* 22118 */	NdrFcLong( 0x0 ),	/* 0 */
/* 22122 */	NdrFcShort( 0x9 ),	/* 9 */
/* 22124 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 22126 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22128 */	NdrFcShort( 0x8 ),	/* 8 */
/* 22130 */	0x45,		/* Oi2 Flags:  srv must size, has return, has ext, */
			0x2,		/* 2 */
/* 22132 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 22134 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22136 */	NdrFcShort( 0x0 ),	/* 0 */
/* 22138 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pprb */

/* 22140 */	NdrFcShort( 0x13 ),	/* Flags:  must size, must free, out, */
/* 22142 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 22144 */	NdrFcShort( 0xa74 ),	/* Type Offset=2676 */

	/* Return value */

/* 22146 */	NdrFcShort( 0x70 ),	/* Flags:  out, return, base type, */
/* 22148 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 22150 */	0x8,		/* FC_LONG */
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
/* 40 */	NdrFcLong( 0xfe44e19b ),	/* -29040229 */
/* 44 */	NdrFcShort( 0xe710 ),	/* -6384 */
/* 46 */	NdrFcShort( 0x4635 ),	/* 17973 */
/* 48 */	0x96,		/* 150 */
			0x90,		/* 144 */
/* 50 */	0x1a,		/* 26 */
			0xfb,		/* 251 */
/* 52 */	0x45,		/* 69 */
			0x1b,		/* 27 */
/* 54 */	0x12,		/* 18 */
			0x26,		/* 38 */
/* 56 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 58 */	NdrFcShort( 0x2 ),	/* Offset= 2 (60) */
/* 60 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 62 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 66 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 68 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 70 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 72 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 74 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 76 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 78 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 80 */	NdrFcShort( 0x6 ),	/* Offset= 6 (86) */
/* 82 */
			0x13, 0x0,	/* FC_OP */
/* 84 */	NdrFcShort( 0xffbe ),	/* Offset= -66 (18) */
/* 86 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 88 */	NdrFcShort( 0x0 ),	/* 0 */
/* 90 */	NdrFcShort( 0x4 ),	/* 4 */
/* 92 */	NdrFcShort( 0x0 ),	/* 0 */
/* 94 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (82) */
/* 96 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 98 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 100 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 102 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 104 */
			0x11, 0x0,	/* FC_RP */
/* 106 */	NdrFcShort( 0x2 ),	/* Offset= 2 (108) */
/* 108 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 110 */	NdrFcShort( 0x4 ),	/* 4 */
/* 112 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 114 */	NdrFcShort( 0x24 ),	/* x86 Stack size/offset = 36 */
/* 116 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 118 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 120 */
			0x11, 0x0,	/* FC_RP */
/* 122 */	NdrFcShort( 0x2 ),	/* Offset= 2 (124) */
/* 124 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 126 */	NdrFcShort( 0x4 ),	/* 4 */
/* 128 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 130 */	NdrFcShort( 0x2c ),	/* x86 Stack size/offset = 44 */
/* 132 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 134 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 136 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 138 */	NdrFcLong( 0x2f6bb7c9 ),	/* 795588553 */
/* 142 */	NdrFcShort( 0x1b3a ),	/* 6970 */
/* 144 */	NdrFcShort( 0x4e94 ),	/* 20116 */
/* 146 */	0xa7,		/* 167 */
			0xbf,		/* 191 */
/* 148 */	0x78,		/* 120 */
			0x2c,		/* 44 */
/* 150 */	0x23,		/* 35 */
			0x69,		/* 105 */
/* 152 */	0xf6,		/* 246 */
			0x81,		/* 129 */
/* 154 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 156 */	0xe,		/* FC_ENUM32 */
			0x5c,		/* FC_PAD */
/* 158 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 160 */	NdrFcShort( 0x2 ),	/* Offset= 2 (162) */
/* 162 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 164 */	NdrFcLong( 0xc38348d3 ),	/* -1014806317 */
/* 168 */	NdrFcShort( 0x392c ),	/* 14636 */
/* 170 */	NdrFcShort( 0x4e02 ),	/* 19970 */
/* 172 */	0xbd,		/* 189 */
			0x50,		/* 80 */
/* 174 */	0xa0,		/* 160 */
			0x1d,		/* 29 */
/* 176 */	0xc4,		/* 196 */
			0x18,		/* 24 */
/* 178 */	0x9e,		/* 158 */
			0x1d,		/* 29 */
/* 180 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 182 */	NdrFcLong( 0x14e389c6 ),	/* 350456262 */
/* 186 */	NdrFcShort( 0xc986 ),	/* -13946 */
/* 188 */	NdrFcShort( 0x4e31 ),	/* 20017 */
/* 190 */	0xae,		/* 174 */
			0x70,		/* 112 */
/* 192 */	0x1c,		/* 28 */
			0xc1,		/* 193 */
/* 194 */	0xc,		/* 12 */
			0xc3,		/* 195 */
/* 196 */	0x54,		/* 84 */
			0x71,		/* 113 */
/* 198 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 200 */	NdrFcShort( 0x2 ),	/* Offset= 2 (202) */
/* 202 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 204 */	NdrFcShort( 0xc ),	/* 12 */
/* 206 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 208 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 210 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 212 */	NdrFcShort( 0x2 ),	/* Offset= 2 (214) */
/* 214 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 216 */	NdrFcLong( 0x4fa0b99a ),	/* 1335933338 */
/* 220 */	NdrFcShort( 0x5a56 ),	/* 23126 */
/* 222 */	NdrFcShort( 0x41a4 ),	/* 16804 */
/* 224 */	0xbe,		/* 190 */
			0x8b,		/* 139 */
/* 226 */	0xb8,		/* 184 */
			0x9b,		/* 155 */
/* 228 */	0xc6,		/* 198 */
			0x22,		/* 34 */
/* 230 */	0x51,		/* 81 */
			0xa5,		/* 165 */
/* 232 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 234 */	NdrFcShort( 0x2 ),	/* Offset= 2 (236) */
/* 236 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 238 */	NdrFcLong( 0xf1ef76e6 ),	/* -235964698 */
/* 242 */	NdrFcShort( 0xbe04 ),	/* -16892 */
/* 244 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 246 */	0x8d,		/* 141 */
			0x9a,		/* 154 */
/* 248 */	0x0,		/* 0 */
			0x50,		/* 80 */
/* 250 */	0x4,		/* 4 */
			0xde,		/* 222 */
/* 252 */	0xfe,		/* 254 */
			0xc4,		/* 196 */
/* 254 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 256 */	NdrFcShort( 0x2 ),	/* Offset= 2 (258) */
/* 258 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 260 */	NdrFcLong( 0xf1ef76e7 ),	/* -235964697 */
/* 264 */	NdrFcShort( 0xbe04 ),	/* -16892 */
/* 266 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 268 */	0x8d,		/* 141 */
			0x9a,		/* 154 */
/* 270 */	0x0,		/* 0 */
			0x50,		/* 80 */
/* 272 */	0x4,		/* 4 */
			0xde,		/* 222 */
/* 274 */	0xfe,		/* 254 */
			0xc4,		/* 196 */
/* 276 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 278 */	NdrFcShort( 0x8 ),	/* Offset= 8 (286) */
/* 280 */
			0x1d,		/* FC_SMFARRAY */
			0x0,		/* 0 */
/* 282 */	NdrFcShort( 0x8 ),	/* 8 */
/* 284 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 286 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 288 */	NdrFcShort( 0x10 ),	/* 16 */
/* 290 */	0x8,		/* FC_LONG */
			0x6,		/* FC_SHORT */
/* 292 */	0x6,		/* FC_SHORT */
			0x4c,		/* FC_EMBEDDED_COMPLEX */
/* 294 */	0x0,		/* 0 */
			NdrFcShort( 0xfff1 ),	/* Offset= -15 (280) */
			0x5b,		/* FC_END */
/* 298 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 300 */	NdrFcLong( 0xc ),	/* 12 */
/* 304 */	NdrFcShort( 0x0 ),	/* 0 */
/* 306 */	NdrFcShort( 0x0 ),	/* 0 */
/* 308 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 310 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 312 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 314 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 316 */
			0x11, 0x0,	/* FC_RP */
/* 318 */	NdrFcShort( 0x2 ),	/* Offset= 2 (320) */
/* 320 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 322 */	NdrFcShort( 0x1 ),	/* 1 */
/* 324 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 326 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 328 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 330 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 332 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 334 */	NdrFcLong( 0x2c4636e3 ),	/* 742799075 */
/* 338 */	NdrFcShort( 0x4f49 ),	/* 20297 */
/* 340 */	NdrFcShort( 0x4966 ),	/* 18790 */
/* 342 */	0x96,		/* 150 */
			0x6f,		/* 111 */
/* 344 */	0x9,		/* 9 */
			0x53,		/* 83 */
/* 346 */	0xf9,		/* 249 */
			0x7f,		/* 127 */
/* 348 */	0x51,		/* 81 */
			0xc8,		/* 200 */
/* 350 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 352 */	NdrFcShort( 0x2 ),	/* Offset= 2 (354) */
/* 354 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 356 */	NdrFcLong( 0xf1ef76e8 ),	/* -235964696 */
/* 360 */	NdrFcShort( 0xbe04 ),	/* -16892 */
/* 362 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 364 */	0x8d,		/* 141 */
			0x9a,		/* 154 */
/* 366 */	0x0,		/* 0 */
			0x50,		/* 80 */
/* 368 */	0x4,		/* 4 */
			0xde,		/* 222 */
/* 370 */	0xfe,		/* 254 */
			0xc4,		/* 196 */
/* 372 */
			0x11, 0x14,	/* FC_RP [alloced_on_stack] [pointer_deref] */
/* 374 */	NdrFcShort( 0xff60 ),	/* Offset= -160 (214) */
/* 376 */
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/* 378 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 380 */
			0x11, 0x0,	/* FC_RP */
/* 382 */	NdrFcShort( 0x2 ),	/* Offset= 2 (384) */
/* 384 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 386 */	NdrFcShort( 0x1 ),	/* 1 */
/* 388 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 390 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 392 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 394 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 396 */
			0x11, 0x0,	/* FC_RP */
/* 398 */	NdrFcShort( 0x2 ),	/* Offset= 2 (400) */
/* 400 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 402 */	NdrFcShort( 0x1 ),	/* 1 */
/* 404 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 406 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 408 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 410 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 412 */
			0x11, 0x0,	/* FC_RP */
/* 414 */	NdrFcShort( 0x2 ),	/* Offset= 2 (416) */
/* 416 */
			0x25,		/* FC_C_WSTRING */
			0x44,		/* FC_STRING_SIZED */
/* 418 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 420 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 422 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 424 */
			0x11, 0x0,	/* FC_RP */
/* 426 */	NdrFcShort( 0x2 ),	/* Offset= 2 (428) */
/* 428 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 430 */	NdrFcShort( 0x1 ),	/* 1 */
/* 432 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 434 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 436 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 438 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 440 */
			0x11, 0x0,	/* FC_RP */
/* 442 */	NdrFcShort( 0x2 ),	/* Offset= 2 (444) */
/* 444 */
			0x25,		/* FC_C_WSTRING */
			0x44,		/* FC_STRING_SIZED */
/* 446 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 448 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 450 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 452 */
			0x11, 0x0,	/* FC_RP */
/* 454 */	NdrFcShort( 0x2 ),	/* Offset= 2 (456) */
/* 456 */
			0x25,		/* FC_C_WSTRING */
			0x44,		/* FC_STRING_SIZED */
/* 458 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 460 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 462 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 464 */
			0x11, 0x0,	/* FC_RP */
/* 466 */	NdrFcShort( 0x2 ),	/* Offset= 2 (468) */
/* 468 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 470 */	NdrFcShort( 0x1 ),	/* 1 */
/* 472 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 474 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 476 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 478 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 480 */
			0x11, 0x0,	/* FC_RP */
/* 482 */	NdrFcShort( 0x2 ),	/* Offset= 2 (484) */
/* 484 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 486 */	NdrFcShort( 0x1 ),	/* 1 */
/* 488 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 490 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 492 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 494 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 496 */
			0x11, 0x0,	/* FC_RP */
/* 498 */	NdrFcShort( 0x2 ),	/* Offset= 2 (500) */
/* 500 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 502 */	NdrFcShort( 0x0 ),	/* 0 */
/* 504 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 506 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 508 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 510 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 514 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 516 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 518 */	NdrFcShort( 0xfed0 ),	/* Offset= -304 (214) */
/* 520 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 522 */
			0x11, 0x0,	/* FC_RP */
/* 524 */	NdrFcShort( 0x2 ),	/* Offset= 2 (526) */
/* 526 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 528 */	NdrFcShort( 0x4 ),	/* 4 */
/* 530 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 532 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 534 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 536 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 538 */
			0x11, 0x0,	/* FC_RP */
/* 540 */	NdrFcShort( 0x2 ),	/* Offset= 2 (542) */
/* 542 */
			0x25,		/* FC_C_WSTRING */
			0x44,		/* FC_STRING_SIZED */
/* 544 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 546 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 548 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 550 */
			0x11, 0x0,	/* FC_RP */
/* 552 */	NdrFcShort( 0x2 ),	/* Offset= 2 (554) */
/* 554 */
			0x25,		/* FC_C_WSTRING */
			0x44,		/* FC_STRING_SIZED */
/* 556 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 558 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 560 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 562 */
			0x11, 0x0,	/* FC_RP */
/* 564 */	NdrFcShort( 0x2 ),	/* Offset= 2 (566) */
/* 566 */
			0x25,		/* FC_C_WSTRING */
			0x44,		/* FC_STRING_SIZED */
/* 568 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 570 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 572 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 574 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 576 */	NdrFcShort( 0xfeea ),	/* Offset= -278 (298) */
/* 578 */
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/* 580 */
			0x25,		/* FC_C_WSTRING */
			0x5c,		/* FC_PAD */
/* 582 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 584 */	NdrFcLong( 0x1c0fa5af ),	/* 470787503 */
/* 588 */	NdrFcShort( 0xb4 ),	/* 180 */
/* 590 */	NdrFcShort( 0x4dc1 ),	/* 19905 */
/* 592 */	0x8f,		/* 143 */
			0x9e,		/* 158 */
/* 594 */	0x16,		/* 22 */
			0x8a,		/* 138 */
/* 596 */	0xf3,		/* 243 */
			0xf8,		/* 248 */
/* 598 */	0x92,		/* 146 */
			0xb0,		/* 176 */
/* 600 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 602 */	NdrFcLong( 0x0 ),	/* 0 */
/* 606 */	NdrFcShort( 0x0 ),	/* 0 */
/* 608 */	NdrFcShort( 0x0 ),	/* 0 */
/* 610 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 612 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 614 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 616 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 618 */
			0x11, 0x0,	/* FC_RP */
/* 620 */	NdrFcShort( 0x2 ),	/* Offset= 2 (622) */
/* 622 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 624 */	NdrFcShort( 0x4 ),	/* 4 */
/* 626 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 628 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 630 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 632 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 634 */
			0x11, 0x0,	/* FC_RP */
/* 636 */	NdrFcShort( 0x2 ),	/* Offset= 2 (638) */
/* 638 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 640 */	NdrFcShort( 0x4 ),	/* 4 */
/* 642 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 644 */	NdrFcShort( 0x18 ),	/* x86 Stack size/offset = 24 */
/* 646 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 648 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 650 */
			0x11, 0x0,	/* FC_RP */
/* 652 */	NdrFcShort( 0x2 ),	/* Offset= 2 (654) */
/* 654 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 656 */	NdrFcShort( 0x1 ),	/* 1 */
/* 658 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 660 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 662 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 664 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 666 */
			0x11, 0x0,	/* FC_RP */
/* 668 */	NdrFcShort( 0x2 ),	/* Offset= 2 (670) */
/* 670 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 672 */	NdrFcShort( 0x2 ),	/* 2 */
/* 674 */	0x29,		/* Corr desc:  parameter, FC_ULONG */
			0x0,		/*  */
/* 676 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 678 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 680 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 682 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 684 */	NdrFcShort( 0x2 ),	/* Offset= 2 (686) */
/* 686 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 688 */	NdrFcLong( 0x21993161 ),	/* 563687777 */
/* 692 */	NdrFcShort( 0x3e24 ),	/* 15908 */
/* 694 */	NdrFcShort( 0x11d4 ),	/* 4564 */
/* 696 */	0xa1,		/* 161 */
			0xbd,		/* 189 */
/* 698 */	0x0,		/* 0 */
			0xc0,		/* 192 */
/* 700 */	0x4f,		/* 79 */
			0xc,		/* 12 */
/* 702 */	0x95,		/* 149 */
			0x93,		/* 147 */
/* 704 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 706 */	NdrFcLong( 0xcb7bea0f ),	/* -881071601 */
/* 710 */	NdrFcShort( 0x960a ),	/* -27126 */
/* 712 */	NdrFcShort( 0x4b23 ),	/* 19235 */
/* 714 */	0x80,		/* 128 */
			0xd3,		/* 211 */
/* 716 */	0xde,		/* 222 */
			0x6,		/* 6 */
/* 718 */	0xc0,		/* 192 */
			0x53,		/* 83 */
/* 720 */	0xe,		/* 14 */
			0x4,		/* 4 */
/* 722 */
			0x11, 0x0,	/* FC_RP */
/* 724 */	NdrFcShort( 0x2 ),	/* Offset= 2 (726) */
/* 726 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 728 */	NdrFcShort( 0x4 ),	/* 4 */
/* 730 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 732 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 734 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 736 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 738 */
			0x11, 0x0,	/* FC_RP */
/* 740 */	NdrFcShort( 0x2 ),	/* Offset= 2 (742) */
/* 742 */
			0x1c,		/* FC_CVARRAY */
			0x3,		/* 3 */
/* 744 */	NdrFcShort( 0x4 ),	/* 4 */
/* 746 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 748 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 750 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 752 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x54,		/* FC_DEREFERENCE */
/* 754 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 756 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 758 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 760 */
			0x11, 0x0,	/* FC_RP */
/* 762 */	NdrFcShort( 0x2 ),	/* Offset= 2 (764) */
/* 764 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 766 */	NdrFcShort( 0x4 ),	/* 4 */
/* 768 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 770 */	NdrFcShort( 0x14 ),	/* x86 Stack size/offset = 20 */
/* 772 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 774 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 776 */
			0x11, 0x0,	/* FC_RP */
/* 778 */	NdrFcShort( 0x2 ),	/* Offset= 2 (780) */
/* 780 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 782 */	NdrFcShort( 0x2 ),	/* 2 */
/* 784 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 786 */	NdrFcShort( 0xc ),	/* x86 Stack size/offset = 12 */
/* 788 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 790 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 792 */
			0x11, 0x0,	/* FC_RP */
/* 794 */	NdrFcShort( 0x2 ),	/* Offset= 2 (796) */
/* 796 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 798 */	NdrFcShort( 0x10 ),	/* 16 */
/* 800 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 802 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 804 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 806 */
			0x11, 0x0,	/* FC_RP */
/* 808 */	NdrFcShort( 0x2 ),	/* Offset= 2 (810) */
/* 810 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 812 */	NdrFcShort( 0x2 ),	/* 2 */
/* 814 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 816 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 818 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 820 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 822 */
			0x11, 0x0,	/* FC_RP */
/* 824 */	NdrFcShort( 0x2 ),	/* Offset= 2 (826) */
/* 826 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 828 */	NdrFcShort( 0x2 ),	/* 2 */
/* 830 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 832 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 834 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 836 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 838 */
			0x11, 0x0,	/* FC_RP */
/* 840 */	NdrFcShort( 0xe ),	/* Offset= 14 (854) */
/* 842 */
			0x1d,		/* FC_SMFARRAY */
			0x1,		/* 1 */
/* 844 */	NdrFcShort( 0x40 ),	/* 64 */
/* 846 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 848 */
			0x1d,		/* FC_SMFARRAY */
			0x1,		/* 1 */
/* 850 */	NdrFcShort( 0x80 ),	/* 128 */
/* 852 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 854 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 856 */	NdrFcShort( 0xf0 ),	/* 240 */
/* 858 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 860 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 862 */	0x8,		/* FC_LONG */
			0x1,		/* FC_BYTE */
/* 864 */	0x3f,		/* FC_STRUCTPAD3 */
			0x8,		/* FC_LONG */
/* 866 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 868 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 870 */	0x8,		/* FC_LONG */
			0x4c,		/* FC_EMBEDDED_COMPLEX */
/* 872 */	0x0,		/* 0 */
			NdrFcShort( 0xffe1 ),	/* Offset= -31 (842) */
			0x4c,		/* FC_EMBEDDED_COMPLEX */
/* 876 */	0x0,		/* 0 */
			NdrFcShort( 0xffe3 ),	/* Offset= -29 (848) */
			0x5b,		/* FC_END */
/* 880 */
			0x15,		/* FC_STRUCT */
			0x3,		/* 3 */
/* 882 */	NdrFcShort( 0x8 ),	/* 8 */
/* 884 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 886 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 888 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 890 */	NdrFcShort( 0x8 ),	/* 8 */
/* 892 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 894 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 896 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 898 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 900 */	NdrFcShort( 0xffec ),	/* Offset= -20 (880) */
/* 902 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 904 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 906 */	NdrFcLong( 0x7bf80980 ),	/* 2079852928 */
/* 910 */	NdrFcShort( 0xbf32 ),	/* -16590 */
/* 912 */	NdrFcShort( 0x101a ),	/* 4122 */
/* 914 */	0x8b,		/* 139 */
			0xbb,		/* 187 */
/* 916 */	0x0,		/* 0 */
			0xaa,		/* 170 */
/* 918 */	0x0,		/* 0 */
			0x30,		/* 48 */
/* 920 */	0xc,		/* 12 */
			0xab,		/* 171 */
/* 922 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 924 */	NdrFcShort( 0xffec ),	/* Offset= -20 (904) */
/* 926 */
			0x11, 0x0,	/* FC_RP */
/* 928 */	NdrFcShort( 0x2 ),	/* Offset= 2 (930) */
/* 930 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 932 */	NdrFcShort( 0x2 ),	/* 2 */
/* 934 */	0x20,		/* Corr desc:  parameter,  */
			0x59,		/* FC_CALLBACK */
/* 936 */	NdrFcShort( 0x0 ),	/* 0 */
/* 938 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 940 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 942 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 944 */	NdrFcShort( 0x2 ),	/* Offset= 2 (946) */
/* 946 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x1,		/* 1 */
/* 948 */	NdrFcShort( 0x8 ),	/* 8 */
/* 950 */	NdrFcShort( 0x0 ),	/* 0 */
/* 952 */	NdrFcShort( 0x0 ),	/* Offset= 0 (952) */
/* 954 */	0xd,		/* FC_ENUM16 */
			0xd,		/* FC_ENUM16 */
/* 956 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 958 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 960 */	NdrFcLong( 0xd7364ef2 ),	/* -684306702 */
/* 964 */	NdrFcShort( 0x43c0 ),	/* 17344 */
/* 966 */	NdrFcShort( 0x4440 ),	/* 17472 */
/* 968 */	0x87,		/* 135 */
			0x2a,		/* 42 */
/* 970 */	0x33,		/* 51 */
			0x6a,		/* 106 */
/* 972 */	0x46,		/* 70 */
			0x47,		/* 71 */
/* 974 */	0xb9,		/* 185 */
			0xa3,		/* 163 */
/* 976 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 978 */	NdrFcLong( 0x3a3ce0a1 ),	/* 977068193 */
/* 982 */	NdrFcShort( 0xb5eb ),	/* -18965 */
/* 984 */	NdrFcShort( 0x43bd ),	/* 17341 */
/* 986 */	0x9c,		/* 156 */
			0x89,		/* 137 */
/* 988 */	0x35,		/* 53 */
			0xea,		/* 234 */
/* 990 */	0xa1,		/* 161 */
			0x10,		/* 16 */
/* 992 */	0xf1,		/* 241 */
			0x2b,		/* 43 */
/* 994 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 996 */	NdrFcShort( 0xff38 ),	/* Offset= -200 (796) */
/* 998 */
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/* 1000 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 1002 */
			0x11, 0x0,	/* FC_RP */
/* 1004 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1006) */
/* 1006 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1008 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1010 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1012 */	NdrFcShort( 0x38 ),	/* x86 Stack size/offset = 56 */
/* 1014 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1016 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1018 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1020 */	NdrFcLong( 0x92ac8be4 ),	/* -1834185756 */
/* 1024 */	NdrFcShort( 0xedc8 ),	/* -4664 */
/* 1026 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1028 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1030 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1032 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1034 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1036 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1038 */	NdrFcLong( 0xbac7725f ),	/* -1161334177 */
/* 1042 */	NdrFcShort( 0x1d26 ),	/* 7462 */
/* 1044 */	NdrFcShort( 0x42b2 ),	/* 17074 */
/* 1046 */	0x8e,		/* 142 */
			0x9d,		/* 157 */
/* 1048 */	0x8b,		/* 139 */
			0x91,		/* 145 */
/* 1050 */	0x75,		/* 117 */
			0x78,		/* 120 */
/* 1052 */	0x2c,		/* 44 */
			0xc7,		/* 199 */
/* 1054 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1056 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1058) */
/* 1058 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1060 */	NdrFcLong( 0x7407f0fc ),	/* 1946677500 */
/* 1064 */	NdrFcShort( 0x58b0 ),	/* 22704 */
/* 1066 */	NdrFcShort( 0x4476 ),	/* 17526 */
/* 1068 */	0xa0,		/* 160 */
			0xc8,		/* 200 */
/* 1070 */	0x69,		/* 105 */
			0x43,		/* 67 */
/* 1072 */	0x18,		/* 24 */
			0x1,		/* 1 */
/* 1074 */	0xe5,		/* 229 */
			0x60,		/* 96 */
/* 1076 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1078 */	NdrFcShort( 0xfd16 ),	/* Offset= -746 (332) */
/* 1080 */
			0x11, 0x0,	/* FC_RP */
/* 1082 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1084) */
/* 1084 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1086 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1088 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1090 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1092 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1094 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1096 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 1098 */	0xa,		/* FC_FLOAT */
			0x5c,		/* FC_PAD */
/* 1100 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1102 */	NdrFcLong( 0xb ),	/* 11 */
/* 1106 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1108 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1110 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 1112 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1114 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1116 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 1118 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1120 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1122) */
/* 1122 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1124 */	NdrFcLong( 0xd224002 ),	/* 220348418 */
/* 1128 */	NdrFcShort( 0x3c7 ),	/* 967 */
/* 1130 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1132 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1134 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1136 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1138 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1140 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1142 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1144) */
/* 1144 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1146 */	NdrFcLong( 0xd224003 ),	/* 220348419 */
/* 1150 */	NdrFcShort( 0x3c7 ),	/* 967 */
/* 1152 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1154 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1156 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1158 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1160 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1162 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1164 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1166) */
/* 1166 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1168 */	NdrFcLong( 0xfc1c0d01 ),	/* -65270527 */
/* 1172 */	NdrFcShort( 0x483 ),	/* 1155 */
/* 1174 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1176 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1178 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1180 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1182 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1184 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1186 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1188) */
/* 1188 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1190 */	NdrFcLong( 0xd224006 ),	/* 220348422 */
/* 1194 */	NdrFcShort( 0x3c7 ),	/* 967 */
/* 1196 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1198 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1200 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1202 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1204 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1206 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1208 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1210) */
/* 1210 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1212 */	NdrFcLong( 0xd224001 ),	/* 220348417 */
/* 1216 */	NdrFcShort( 0x3c7 ),	/* 967 */
/* 1218 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 1220 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 1222 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1224 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 1226 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 1228 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1230 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1232) */
/* 1232 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1234 */	NdrFcLong( 0x93cb892f ),	/* -1815377617 */
/* 1238 */	NdrFcShort( 0x16d1 ),	/* 5841 */
/* 1240 */	NdrFcShort( 0x4dca ),	/* 19914 */
/* 1242 */	0x9c,		/* 156 */
			0x71,		/* 113 */
/* 1244 */	0x2e,		/* 46 */
			0x80,		/* 128 */
/* 1246 */	0x4b,		/* 75 */
			0xc9,		/* 201 */
/* 1248 */	0x39,		/* 57 */
			0x5c,		/* 92 */
/* 1250 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1252 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1254) */
/* 1254 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1256 */	NdrFcLong( 0x254db9e3 ),	/* 625850851 */
/* 1260 */	NdrFcShort( 0x265 ),	/* 613 */
/* 1262 */	NdrFcShort( 0x49cf ),	/* 18895 */
/* 1264 */	0xa1,		/* 161 */
			0x9f,		/* 159 */
/* 1266 */	0x3c,		/* 60 */
			0x75,		/* 117 */
/* 1268 */	0xe8,		/* 232 */
			0x52,		/* 82 */
/* 1270 */	0x5a,		/* 90 */
			0x28,		/* 40 */
/* 1272 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1274 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1276) */
/* 1276 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1278 */	NdrFcLong( 0xdb78d60b ),	/* -612837877 */
/* 1282 */	NdrFcShort( 0xe43e ),	/* -7106 */
/* 1284 */	NdrFcShort( 0x4464 ),	/* 17508 */
/* 1286 */	0xb8,		/* 184 */
			0xae,		/* 174 */
/* 1288 */	0xc5,		/* 197 */
			0xc9,		/* 201 */
/* 1290 */	0xa0,		/* 160 */
			0xe,		/* 14 */
/* 1292 */	0x2c,		/* 44 */
			0x4,		/* 4 */
/* 1294 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 1296 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1298) */
/* 1298 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1300 */	NdrFcLong( 0x7c8b7f40 ),	/* 2089516864 */
/* 1304 */	NdrFcShort( 0x40c8 ),	/* 16584 */
/* 1306 */	NdrFcShort( 0x47f7 ),	/* 18423 */
/* 1308 */	0xb1,		/* 177 */
			0xb,		/* 11 */
/* 1310 */	0x45,		/* 69 */
			0x37,		/* 55 */
/* 1312 */	0x24,		/* 36 */
			0x15,		/* 21 */
/* 1314 */	0x77,		/* 119 */
			0x8d,		/* 141 */
/* 1316 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 1318 */	0xc,		/* FC_DOUBLE */
			0x5c,		/* FC_PAD */
/* 1320 */
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/* 1322 */	0x5,		/* FC_WCHAR */
			0x5c,		/* FC_PAD */
/* 1324 */
			0x11, 0x0,	/* FC_RP */
/* 1326 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1328) */
/* 1328 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 1330 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1332 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1334 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 1336 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1338 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 1340 */
			0x11, 0x0,	/* FC_RP */
/* 1342 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1344) */
/* 1344 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 1346 */	NdrFcShort( 0x2 ),	/* 2 */
/* 1348 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 1350 */	NdrFcShort( 0x10 ),	/* x86 Stack size/offset = 16 */
/* 1352 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1354 */	0x5,		/* FC_WCHAR */
			0x5b,		/* FC_END */
/* 1356 */
			0x11, 0x4,	/* FC_RP [alloced_on_stack] */
/* 1358 */	NdrFcShort( 0x3e2 ),	/* Offset= 994 (2352) */
/* 1360 */
			0x13, 0x0,	/* FC_OP */
/* 1362 */	NdrFcShort( 0x3ca ),	/* Offset= 970 (2332) */
/* 1364 */
			0x2b,		/* FC_NON_ENCAPSULATED_UNION */
			0x9,		/* FC_ULONG */
/* 1366 */	0x7,		/* Corr desc: FC_USHORT */
			0x0,		/*  */
/* 1368 */	NdrFcShort( 0xfff8 ),	/* -8 */
/* 1370 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1372 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1374) */
/* 1374 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1376 */	NdrFcShort( 0x2f ),	/* 47 */
/* 1378 */	NdrFcLong( 0x14 ),	/* 20 */
/* 1382 */	NdrFcShort( 0x800b ),	/* Simple arm type: FC_HYPER */
/* 1384 */	NdrFcLong( 0x3 ),	/* 3 */
/* 1388 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 1390 */	NdrFcLong( 0x11 ),	/* 17 */
/* 1394 */	NdrFcShort( 0x8001 ),	/* Simple arm type: FC_BYTE */
/* 1396 */	NdrFcLong( 0x2 ),	/* 2 */
/* 1400 */	NdrFcShort( 0x8006 ),	/* Simple arm type: FC_SHORT */
/* 1402 */	NdrFcLong( 0x4 ),	/* 4 */
/* 1406 */	NdrFcShort( 0x800a ),	/* Simple arm type: FC_FLOAT */
/* 1408 */	NdrFcLong( 0x5 ),	/* 5 */
/* 1412 */	NdrFcShort( 0x800c ),	/* Simple arm type: FC_DOUBLE */
/* 1414 */	NdrFcLong( 0xb ),	/* 11 */
/* 1418 */	NdrFcShort( 0x8006 ),	/* Simple arm type: FC_SHORT */
/* 1420 */	NdrFcLong( 0xa ),	/* 10 */
/* 1424 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 1426 */	NdrFcLong( 0x6 ),	/* 6 */
/* 1430 */	NdrFcShort( 0xe8 ),	/* Offset= 232 (1662) */
/* 1432 */	NdrFcLong( 0x7 ),	/* 7 */
/* 1436 */	NdrFcShort( 0x800c ),	/* Simple arm type: FC_DOUBLE */
/* 1438 */	NdrFcLong( 0x8 ),	/* 8 */
/* 1442 */	NdrFcShort( 0xfab0 ),	/* Offset= -1360 (82) */
/* 1444 */	NdrFcLong( 0xd ),	/* 13 */
/* 1448 */	NdrFcShort( 0xfcb0 ),	/* Offset= -848 (600) */
/* 1450 */	NdrFcLong( 0x9 ),	/* 9 */
/* 1454 */	NdrFcShort( 0xd6 ),	/* Offset= 214 (1668) */
/* 1456 */	NdrFcLong( 0x2000 ),	/* 8192 */
/* 1460 */	NdrFcShort( 0xe2 ),	/* Offset= 226 (1686) */
/* 1462 */	NdrFcLong( 0x24 ),	/* 36 */
/* 1466 */	NdrFcShort( 0x318 ),	/* Offset= 792 (2258) */
/* 1468 */	NdrFcLong( 0x4024 ),	/* 16420 */
/* 1472 */	NdrFcShort( 0x312 ),	/* Offset= 786 (2258) */
/* 1474 */	NdrFcLong( 0x4011 ),	/* 16401 */
/* 1478 */	NdrFcShort( 0x310 ),	/* Offset= 784 (2262) */
/* 1480 */	NdrFcLong( 0x4002 ),	/* 16386 */
/* 1484 */	NdrFcShort( 0x30e ),	/* Offset= 782 (2266) */
/* 1486 */	NdrFcLong( 0x4003 ),	/* 16387 */
/* 1490 */	NdrFcShort( 0x30c ),	/* Offset= 780 (2270) */
/* 1492 */	NdrFcLong( 0x4014 ),	/* 16404 */
/* 1496 */	NdrFcShort( 0x30a ),	/* Offset= 778 (2274) */
/* 1498 */	NdrFcLong( 0x4004 ),	/* 16388 */
/* 1502 */	NdrFcShort( 0x308 ),	/* Offset= 776 (2278) */
/* 1504 */	NdrFcLong( 0x4005 ),	/* 16389 */
/* 1508 */	NdrFcShort( 0x306 ),	/* Offset= 774 (2282) */
/* 1510 */	NdrFcLong( 0x400b ),	/* 16395 */
/* 1514 */	NdrFcShort( 0x2f0 ),	/* Offset= 752 (2266) */
/* 1516 */	NdrFcLong( 0x400a ),	/* 16394 */
/* 1520 */	NdrFcShort( 0x2ee ),	/* Offset= 750 (2270) */
/* 1522 */	NdrFcLong( 0x4006 ),	/* 16390 */
/* 1526 */	NdrFcShort( 0x2f8 ),	/* Offset= 760 (2286) */
/* 1528 */	NdrFcLong( 0x4007 ),	/* 16391 */
/* 1532 */	NdrFcShort( 0x2ee ),	/* Offset= 750 (2282) */
/* 1534 */	NdrFcLong( 0x4008 ),	/* 16392 */
/* 1538 */	NdrFcShort( 0x2f0 ),	/* Offset= 752 (2290) */
/* 1540 */	NdrFcLong( 0x400d ),	/* 16397 */
/* 1544 */	NdrFcShort( 0x2ee ),	/* Offset= 750 (2294) */
/* 1546 */	NdrFcLong( 0x4009 ),	/* 16393 */
/* 1550 */	NdrFcShort( 0x2ec ),	/* Offset= 748 (2298) */
/* 1552 */	NdrFcLong( 0x6000 ),	/* 24576 */
/* 1556 */	NdrFcShort( 0x2ea ),	/* Offset= 746 (2302) */
/* 1558 */	NdrFcLong( 0x400c ),	/* 16396 */
/* 1562 */	NdrFcShort( 0x2e8 ),	/* Offset= 744 (2306) */
/* 1564 */	NdrFcLong( 0x10 ),	/* 16 */
/* 1568 */	NdrFcShort( 0x8002 ),	/* Simple arm type: FC_CHAR */
/* 1570 */	NdrFcLong( 0x12 ),	/* 18 */
/* 1574 */	NdrFcShort( 0x8006 ),	/* Simple arm type: FC_SHORT */
/* 1576 */	NdrFcLong( 0x13 ),	/* 19 */
/* 1580 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 1582 */	NdrFcLong( 0x15 ),	/* 21 */
/* 1586 */	NdrFcShort( 0x800b ),	/* Simple arm type: FC_HYPER */
/* 1588 */	NdrFcLong( 0x16 ),	/* 22 */
/* 1592 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 1594 */	NdrFcLong( 0x17 ),	/* 23 */
/* 1598 */	NdrFcShort( 0x8008 ),	/* Simple arm type: FC_LONG */
/* 1600 */	NdrFcLong( 0xe ),	/* 14 */
/* 1604 */	NdrFcShort( 0x2c6 ),	/* Offset= 710 (2314) */
/* 1606 */	NdrFcLong( 0x400e ),	/* 16398 */
/* 1610 */	NdrFcShort( 0x2ca ),	/* Offset= 714 (2324) */
/* 1612 */	NdrFcLong( 0x4010 ),	/* 16400 */
/* 1616 */	NdrFcShort( 0x2c8 ),	/* Offset= 712 (2328) */
/* 1618 */	NdrFcLong( 0x4012 ),	/* 16402 */
/* 1622 */	NdrFcShort( 0x284 ),	/* Offset= 644 (2266) */
/* 1624 */	NdrFcLong( 0x4013 ),	/* 16403 */
/* 1628 */	NdrFcShort( 0x282 ),	/* Offset= 642 (2270) */
/* 1630 */	NdrFcLong( 0x4015 ),	/* 16405 */
/* 1634 */	NdrFcShort( 0x280 ),	/* Offset= 640 (2274) */
/* 1636 */	NdrFcLong( 0x4016 ),	/* 16406 */
/* 1640 */	NdrFcShort( 0x276 ),	/* Offset= 630 (2270) */
/* 1642 */	NdrFcLong( 0x4017 ),	/* 16407 */
/* 1646 */	NdrFcShort( 0x270 ),	/* Offset= 624 (2270) */
/* 1648 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1652 */	NdrFcShort( 0x0 ),	/* Offset= 0 (1652) */
/* 1654 */	NdrFcLong( 0x1 ),	/* 1 */
/* 1658 */	NdrFcShort( 0x0 ),	/* Offset= 0 (1658) */
/* 1660 */	NdrFcShort( 0xffff ),	/* Offset= -1 (1659) */
/* 1662 */
			0x15,		/* FC_STRUCT */
			0x7,		/* 7 */
/* 1664 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1666 */	0xb,		/* FC_HYPER */
			0x5b,		/* FC_END */
/* 1668 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1670 */	NdrFcLong( 0x20400 ),	/* 132096 */
/* 1674 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1676 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1678 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 1680 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1682 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1684 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 1686 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 1688 */	NdrFcShort( 0x2 ),	/* Offset= 2 (1690) */
/* 1690 */
			0x13, 0x0,	/* FC_OP */
/* 1692 */	NdrFcShort( 0x224 ),	/* Offset= 548 (2240) */
/* 1694 */
			0x2a,		/* FC_ENCAPSULATED_UNION */
			0x49,		/* 73 */
/* 1696 */	NdrFcShort( 0x18 ),	/* 24 */
/* 1698 */	NdrFcShort( 0xa ),	/* 10 */
/* 1700 */	NdrFcLong( 0x8 ),	/* 8 */
/* 1704 */	NdrFcShort( 0x5a ),	/* Offset= 90 (1794) */
/* 1706 */	NdrFcLong( 0xd ),	/* 13 */
/* 1710 */	NdrFcShort( 0x90 ),	/* Offset= 144 (1854) */
/* 1712 */	NdrFcLong( 0x9 ),	/* 9 */
/* 1716 */	NdrFcShort( 0xb0 ),	/* Offset= 176 (1892) */
/* 1718 */	NdrFcLong( 0xc ),	/* 12 */
/* 1722 */	NdrFcShort( 0xda ),	/* Offset= 218 (1940) */
/* 1724 */	NdrFcLong( 0x24 ),	/* 36 */
/* 1728 */	NdrFcShort( 0x136 ),	/* Offset= 310 (2038) */
/* 1730 */	NdrFcLong( 0x800d ),	/* 32781 */
/* 1734 */	NdrFcShort( 0x156 ),	/* Offset= 342 (2076) */
/* 1736 */	NdrFcLong( 0x10 ),	/* 16 */
/* 1740 */	NdrFcShort( 0x170 ),	/* Offset= 368 (2108) */
/* 1742 */	NdrFcLong( 0x2 ),	/* 2 */
/* 1746 */	NdrFcShort( 0x18a ),	/* Offset= 394 (2140) */
/* 1748 */	NdrFcLong( 0x3 ),	/* 3 */
/* 1752 */	NdrFcShort( 0x1a4 ),	/* Offset= 420 (2172) */
/* 1754 */	NdrFcLong( 0x14 ),	/* 20 */
/* 1758 */	NdrFcShort( 0x1be ),	/* Offset= 446 (2204) */
/* 1760 */	NdrFcShort( 0xffff ),	/* Offset= -1 (1759) */
/* 1762 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1764 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1766 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 1768 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1770 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1772 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 1774 */
			0x48,		/* FC_VARIABLE_REPEAT */
			0x49,		/* FC_FIXED_OFFSET */
/* 1776 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1778 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1780 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1782 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1784 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1786 */	0x13, 0x0,	/* FC_OP */
/* 1788 */	NdrFcShort( 0xf916 ),	/* Offset= -1770 (18) */
/* 1790 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 1792 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1794 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 1796 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1798 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 1800 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 1802 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1804 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1806 */	0x11, 0x0,	/* FC_RP */
/* 1808 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (1762) */
/* 1810 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 1812 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 1814 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1816 */	NdrFcLong( 0x0 ),	/* 0 */
/* 1820 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1822 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1824 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 1826 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1828 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1830 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 1832 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 1834 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1836 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 1838 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1840 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1842 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 1846 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1848 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1850 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (1814) */
/* 1852 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1854 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 1856 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1858 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1860 */	NdrFcShort( 0x6 ),	/* Offset= 6 (1866) */
/* 1862 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 1864 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1866 */
			0x11, 0x0,	/* FC_RP */
/* 1868 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (1832) */
/* 1870 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 1872 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1874 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 1876 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1878 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1880 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 1884 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 1886 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1888 */	NdrFcShort( 0xff24 ),	/* Offset= -220 (1668) */
/* 1890 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1892 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 1894 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1896 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1898 */	NdrFcShort( 0x6 ),	/* Offset= 6 (1904) */
/* 1900 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 1902 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1904 */
			0x11, 0x0,	/* FC_RP */
/* 1906 */	NdrFcShort( 0xffdc ),	/* Offset= -36 (1870) */
/* 1908 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 1910 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1912 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 1914 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1916 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1918 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 1920 */
			0x48,		/* FC_VARIABLE_REPEAT */
			0x49,		/* FC_FIXED_OFFSET */
/* 1922 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1924 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1926 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1928 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1930 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1932 */	0x13, 0x0,	/* FC_OP */
/* 1934 */	NdrFcShort( 0x18e ),	/* Offset= 398 (2332) */
/* 1936 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 1938 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1940 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 1942 */	NdrFcShort( 0x8 ),	/* 8 */
/* 1944 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1946 */	NdrFcShort( 0x6 ),	/* Offset= 6 (1952) */
/* 1948 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 1950 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 1952 */
			0x11, 0x0,	/* FC_RP */
/* 1954 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (1908) */
/* 1956 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 1958 */	NdrFcLong( 0x2f ),	/* 47 */
/* 1962 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1964 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1966 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 1968 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1970 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 1972 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 1974 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 1976 */	NdrFcShort( 0x1 ),	/* 1 */
/* 1978 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 1980 */	NdrFcShort( 0x4 ),	/* 4 */
/* 1982 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 1984 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 1986 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 1988 */	NdrFcShort( 0x10 ),	/* 16 */
/* 1990 */	NdrFcShort( 0x0 ),	/* 0 */
/* 1992 */	NdrFcShort( 0xa ),	/* Offset= 10 (2002) */
/* 1994 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 1996 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 1998 */	NdrFcShort( 0xffd6 ),	/* Offset= -42 (1956) */
/* 2000 */	0x36,		/* FC_POINTER */
			0x5b,		/* FC_END */
/* 2002 */
			0x13, 0x0,	/* FC_OP */
/* 2004 */	NdrFcShort( 0xffe2 ),	/* Offset= -30 (1974) */
/* 2006 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 2008 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2010 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 2012 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2014 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2016 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 2018 */
			0x48,		/* FC_VARIABLE_REPEAT */
			0x49,		/* FC_FIXED_OFFSET */
/* 2020 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2022 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2024 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2026 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2028 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2030 */	0x13, 0x0,	/* FC_OP */
/* 2032 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (1986) */
/* 2034 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 2036 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2038 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 2040 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2042 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2044 */	NdrFcShort( 0x6 ),	/* Offset= 6 (2050) */
/* 2046 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 2048 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2050 */
			0x11, 0x0,	/* FC_RP */
/* 2052 */	NdrFcShort( 0xffd2 ),	/* Offset= -46 (2006) */
/* 2054 */
			0x21,		/* FC_BOGUS_ARRAY */
			0x3,		/* 3 */
/* 2056 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2058 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 2060 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2062 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2064 */	NdrFcLong( 0xffffffff ),	/* -1 */
/* 2068 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2070 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2072 */	NdrFcShort( 0xfa40 ),	/* Offset= -1472 (600) */
/* 2074 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2076 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 2078 */	NdrFcShort( 0x18 ),	/* 24 */
/* 2080 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2082 */	NdrFcShort( 0xa ),	/* Offset= 10 (2092) */
/* 2084 */	0x8,		/* FC_LONG */
			0x36,		/* FC_POINTER */
/* 2086 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2088 */	NdrFcShort( 0xf8f6 ),	/* Offset= -1802 (286) */
/* 2090 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2092 */
			0x11, 0x0,	/* FC_RP */
/* 2094 */	NdrFcShort( 0xffd8 ),	/* Offset= -40 (2054) */
/* 2096 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 2098 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2100 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 2102 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2104 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2106 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 2108 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 2110 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2112 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 2114 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 2116 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2118 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2120 */	0x13, 0x0,	/* FC_OP */
/* 2122 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (2096) */
/* 2124 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 2126 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2128 */
			0x1b,		/* FC_CARRAY */
			0x1,		/* 1 */
/* 2130 */	NdrFcShort( 0x2 ),	/* 2 */
/* 2132 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 2134 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2136 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2138 */	0x6,		/* FC_SHORT */
			0x5b,		/* FC_END */
/* 2140 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 2142 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2144 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 2146 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 2148 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2150 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2152 */	0x13, 0x0,	/* FC_OP */
/* 2154 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (2128) */
/* 2156 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 2158 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2160 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 2162 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2164 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 2166 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2168 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2170 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2172 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 2174 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2176 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 2178 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 2180 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2182 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2184 */	0x13, 0x0,	/* FC_OP */
/* 2186 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (2160) */
/* 2188 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 2190 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2192 */
			0x1b,		/* FC_CARRAY */
			0x7,		/* 7 */
/* 2194 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2196 */	0x19,		/* Corr desc:  field pointer, FC_ULONG */
			0x0,		/*  */
/* 2198 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2200 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2202 */	0xb,		/* FC_HYPER */
			0x5b,		/* FC_END */
/* 2204 */
			0x16,		/* FC_PSTRUCT */
			0x3,		/* 3 */
/* 2206 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2208 */
			0x4b,		/* FC_PP */
			0x5c,		/* FC_PAD */
/* 2210 */
			0x46,		/* FC_NO_REPEAT */
			0x5c,		/* FC_PAD */
/* 2212 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2214 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2216 */	0x13, 0x0,	/* FC_OP */
/* 2218 */	NdrFcShort( 0xffe6 ),	/* Offset= -26 (2192) */
/* 2220 */
			0x5b,		/* FC_END */

			0x8,		/* FC_LONG */
/* 2222 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2224 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 2226 */	NdrFcShort( 0x8 ),	/* 8 */
/* 2228 */	0x7,		/* Corr desc: FC_USHORT */
			0x0,		/*  */
/* 2230 */	NdrFcShort( 0xffd8 ),	/* -40 */
/* 2232 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2234 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2236 */	NdrFcShort( 0xfab4 ),	/* Offset= -1356 (880) */
/* 2238 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2240 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x3,		/* 3 */
/* 2242 */	NdrFcShort( 0x28 ),	/* 40 */
/* 2244 */	NdrFcShort( 0xffec ),	/* Offset= -20 (2224) */
/* 2246 */	NdrFcShort( 0x0 ),	/* Offset= 0 (2246) */
/* 2248 */	0x6,		/* FC_SHORT */
			0x6,		/* FC_SHORT */
/* 2250 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2252 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2254 */	NdrFcShort( 0xfdd0 ),	/* Offset= -560 (1694) */
/* 2256 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2258 */
			0x13, 0x0,	/* FC_OP */
/* 2260 */	NdrFcShort( 0xfeee ),	/* Offset= -274 (1986) */
/* 2262 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 2264 */	0x1,		/* FC_BYTE */
			0x5c,		/* FC_PAD */
/* 2266 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 2268 */	0x6,		/* FC_SHORT */
			0x5c,		/* FC_PAD */
/* 2270 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 2272 */	0x8,		/* FC_LONG */
			0x5c,		/* FC_PAD */
/* 2274 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 2276 */	0xb,		/* FC_HYPER */
			0x5c,		/* FC_PAD */
/* 2278 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 2280 */	0xa,		/* FC_FLOAT */
			0x5c,		/* FC_PAD */
/* 2282 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 2284 */	0xc,		/* FC_DOUBLE */
			0x5c,		/* FC_PAD */
/* 2286 */
			0x13, 0x0,	/* FC_OP */
/* 2288 */	NdrFcShort( 0xfd8e ),	/* Offset= -626 (1662) */
/* 2290 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 2292 */	NdrFcShort( 0xf75e ),	/* Offset= -2210 (82) */
/* 2294 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 2296 */	NdrFcShort( 0xf960 ),	/* Offset= -1696 (600) */
/* 2298 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 2300 */	NdrFcShort( 0xfd88 ),	/* Offset= -632 (1668) */
/* 2302 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 2304 */	NdrFcShort( 0xfd96 ),	/* Offset= -618 (1686) */
/* 2306 */
			0x13, 0x10,	/* FC_OP [pointer_deref] */
/* 2308 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2310) */
/* 2310 */
			0x13, 0x0,	/* FC_OP */
/* 2312 */	NdrFcShort( 0x14 ),	/* Offset= 20 (2332) */
/* 2314 */
			0x15,		/* FC_STRUCT */
			0x7,		/* 7 */
/* 2316 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2318 */	0x6,		/* FC_SHORT */
			0x1,		/* FC_BYTE */
/* 2320 */	0x1,		/* FC_BYTE */
			0x8,		/* FC_LONG */
/* 2322 */	0xb,		/* FC_HYPER */
			0x5b,		/* FC_END */
/* 2324 */
			0x13, 0x0,	/* FC_OP */
/* 2326 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (2314) */
/* 2328 */
			0x13, 0x8,	/* FC_OP [simple_pointer] */
/* 2330 */	0x2,		/* FC_CHAR */
			0x5c,		/* FC_PAD */
/* 2332 */
			0x1a,		/* FC_BOGUS_STRUCT */
			0x7,		/* 7 */
/* 2334 */	NdrFcShort( 0x20 ),	/* 32 */
/* 2336 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2338 */	NdrFcShort( 0x0 ),	/* Offset= 0 (2338) */
/* 2340 */	0x8,		/* FC_LONG */
			0x8,		/* FC_LONG */
/* 2342 */	0x6,		/* FC_SHORT */
			0x6,		/* FC_SHORT */
/* 2344 */	0x6,		/* FC_SHORT */
			0x6,		/* FC_SHORT */
/* 2346 */	0x4c,		/* FC_EMBEDDED_COMPLEX */
			0x0,		/* 0 */
/* 2348 */	NdrFcShort( 0xfc28 ),	/* Offset= -984 (1364) */
/* 2350 */	0x5c,		/* FC_PAD */
			0x5b,		/* FC_END */
/* 2352 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 2354 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2356 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2358 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2360 */	NdrFcShort( 0xfc18 ),	/* Offset= -1000 (1360) */
/* 2362 */
			0x12, 0x0,	/* FC_UP */
/* 2364 */	NdrFcShort( 0xffe0 ),	/* Offset= -32 (2332) */
/* 2366 */	0xb4,		/* FC_USER_MARSHAL */
			0x83,		/* 131 */
/* 2368 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2370 */	NdrFcShort( 0x10 ),	/* 16 */
/* 2372 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2374 */	NdrFcShort( 0xfff4 ),	/* Offset= -12 (2362) */
/* 2376 */
			0x11, 0xc,	/* FC_RP [alloced_on_stack] [simple_pointer] */
/* 2378 */	0x5,		/* FC_WCHAR */
			0x5c,		/* FC_PAD */
/* 2380 */
			0x11, 0x0,	/* FC_RP */
/* 2382 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2384) */
/* 2384 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 2386 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2388 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2390 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2392 */	NdrFcShort( 0x1 ),	/* Corr flags:  early, */
/* 2394 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 2396 */
			0x11, 0x0,	/* FC_RP */
/* 2398 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2400) */
/* 2400 */
			0x1b,		/* FC_CARRAY */
			0x0,		/* 0 */
/* 2402 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2404 */	0x20,		/* Corr desc:  parameter,  */
			0x59,		/* FC_CALLBACK */
/* 2406 */	NdrFcShort( 0x1 ),	/* 1 */
/* 2408 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2410 */	0x1,		/* FC_BYTE */
			0x5b,		/* FC_END */
/* 2412 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2414 */	NdrFcLong( 0x28bc5edc ),	/* 683433692 */
/* 2418 */	NdrFcShort( 0x3ef3 ),	/* 16115 */
/* 2420 */	NdrFcShort( 0x4db2 ),	/* 19890 */
/* 2422 */	0x8b,		/* 139 */
			0x90,		/* 144 */
/* 2424 */	0x55,		/* 85 */
			0x62,		/* 98 */
/* 2426 */	0x0,		/* 0 */
			0xfd,		/* 253 */
/* 2428 */	0x97,		/* 151 */
			0xed,		/* 237 */
/* 2430 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2432 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2434) */
/* 2434 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2436 */	NdrFcLong( 0xd224006 ),	/* 220348422 */
/* 2440 */	NdrFcShort( 0x3c7 ),	/* 967 */
/* 2442 */	NdrFcShort( 0x11d3 ),	/* 4563 */
/* 2444 */	0x80,		/* 128 */
			0x78,		/* 120 */
/* 2446 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2448 */	0xc0,		/* 192 */
			0xfb,		/* 251 */
/* 2450 */	0x81,		/* 129 */
			0xb5,		/* 181 */
/* 2452 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2454 */	NdrFcShort( 0xffd6 ),	/* Offset= -42 (2412) */
/* 2456 */
			0x11, 0x0,	/* FC_RP */
/* 2458 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2460) */
/* 2460 */
			0x1b,		/* FC_CARRAY */
			0x3,		/* 3 */
/* 2462 */	NdrFcShort( 0x4 ),	/* 4 */
/* 2464 */	0x28,		/* Corr desc:  parameter, FC_LONG */
			0x0,		/*  */
/* 2466 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 2468 */	NdrFcShort( 0x0 ),	/* Corr flags:  */
/* 2470 */	0x8,		/* FC_LONG */
			0x5b,		/* FC_END */
/* 2472 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2474 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2476) */
/* 2476 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2478 */	NdrFcLong( 0x7c8b7f40 ),	/* 2089516864 */
/* 2482 */	NdrFcShort( 0x40c8 ),	/* 16584 */
/* 2484 */	NdrFcShort( 0x47f7 ),	/* 18423 */
/* 2486 */	0xb1,		/* 177 */
			0xb,		/* 11 */
/* 2488 */	0x45,		/* 69 */
			0x37,		/* 55 */
/* 2490 */	0x24,		/* 36 */
			0x15,		/* 21 */
/* 2492 */	0x77,		/* 119 */
			0x8d,		/* 141 */
/* 2494 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2496 */	NdrFcLong( 0x3a3ce0a1 ),	/* 977068193 */
/* 2500 */	NdrFcShort( 0xb5eb ),	/* -18965 */
/* 2502 */	NdrFcShort( 0x43bd ),	/* 17341 */
/* 2504 */	0x9c,		/* 156 */
			0x89,		/* 137 */
/* 2506 */	0x35,		/* 53 */
			0xea,		/* 234 */
/* 2508 */	0xa1,		/* 161 */
			0x10,		/* 16 */
/* 2510 */	0xf1,		/* 241 */
			0x2b,		/* 43 */
/* 2512 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2514 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2516) */
/* 2516 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2518 */	NdrFcLong( 0x93cb892f ),	/* -1815377617 */
/* 2522 */	NdrFcShort( 0x16d1 ),	/* 5841 */
/* 2524 */	NdrFcShort( 0x4dca ),	/* 19914 */
/* 2526 */	0x9c,		/* 156 */
			0x71,		/* 113 */
/* 2528 */	0x2e,		/* 46 */
			0x80,		/* 128 */
/* 2530 */	0x4b,		/* 75 */
			0xc9,		/* 201 */
/* 2532 */	0x39,		/* 57 */
			0x5c,		/* 92 */
/* 2534 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2536 */	NdrFcLong( 0xb ),	/* 11 */
/* 2540 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2542 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2544 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 2546 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2548 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2550 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 2552 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2554 */	NdrFcLong( 0xc ),	/* 12 */
/* 2558 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2560 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2562 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 2564 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2566 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2568 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 2570 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2572 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2574) */
/* 2574 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2576 */	NdrFcLong( 0x2c4636e3 ),	/* 742799075 */
/* 2580 */	NdrFcShort( 0x4f49 ),	/* 20297 */
/* 2582 */	NdrFcShort( 0x4966 ),	/* 18790 */
/* 2584 */	0x96,		/* 150 */
			0x6f,		/* 111 */
/* 2586 */	0x9,		/* 9 */
			0x53,		/* 83 */
/* 2588 */	0xf9,		/* 249 */
			0x7f,		/* 127 */
/* 2590 */	0x51,		/* 81 */
			0xc8,		/* 200 */
/* 2592 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2594 */	NdrFcLong( 0xb ),	/* 11 */
/* 2598 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2600 */	NdrFcShort( 0x0 ),	/* 0 */
/* 2602 */	0xc0,		/* 192 */
			0x0,		/* 0 */
/* 2604 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2606 */	0x0,		/* 0 */
			0x0,		/* 0 */
/* 2608 */	0x0,		/* 0 */
			0x46,		/* 70 */
/* 2610 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2612 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2614) */
/* 2614 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2616 */	NdrFcLong( 0x2c4636e3 ),	/* 742799075 */
/* 2620 */	NdrFcShort( 0x4f49 ),	/* 20297 */
/* 2622 */	NdrFcShort( 0x4966 ),	/* 18790 */
/* 2624 */	0x96,		/* 150 */
			0x6f,		/* 111 */
/* 2626 */	0x9,		/* 9 */
			0x53,		/* 83 */
/* 2628 */	0xf9,		/* 249 */
			0x7f,		/* 127 */
/* 2630 */	0x51,		/* 81 */
			0xc8,		/* 200 */
/* 2632 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2634 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2636) */
/* 2636 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2638 */	NdrFcLong( 0xe9e5a6c ),	/* 245258860 */
/* 2642 */	NdrFcShort( 0xba20 ),	/* -17888 */
/* 2644 */	NdrFcShort( 0x4245 ),	/* 16965 */
/* 2646 */	0x8e,		/* 142 */
			0x26,		/* 38 */
/* 2648 */	0x71,		/* 113 */
			0x9a,		/* 154 */
/* 2650 */	0x67,		/* 103 */
			0xfe,		/* 254 */
/* 2652 */	0x18,		/* 24 */
			0x92,		/* 146 */
/* 2654 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2656 */	NdrFcLong( 0x71c8d1ed ),	/* 1908986349 */
/* 2660 */	NdrFcShort( 0x49b0 ),	/* 18864 */
/* 2662 */	NdrFcShort( 0x40ef ),	/* 16623 */
/* 2664 */	0x84,		/* 132 */
			0x23,		/* 35 */
/* 2666 */	0x92,		/* 146 */
			0xb0,		/* 176 */
/* 2668 */	0xa5,		/* 165 */
			0xf0,		/* 240 */
/* 2670 */	0x4b,		/* 75 */
			0x89,		/* 137 */
/* 2672 */
			0x11, 0x0,	/* FC_RP */
/* 2674 */	NdrFcShort( 0xf5e4 ),	/* Offset= -2588 (86) */
/* 2676 */
			0x11, 0x10,	/* FC_RP [pointer_deref] */
/* 2678 */	NdrFcShort( 0x2 ),	/* Offset= 2 (2680) */
/* 2680 */
			0x2f,		/* FC_IP */
			0x5a,		/* FC_CONSTANT_IID */
/* 2682 */	NdrFcLong( 0x4518189c ),	/* 1159207068 */
/* 2686 */	NdrFcShort( 0xe545 ),	/* -6843 */
/* 2688 */	NdrFcShort( 0x48b4 ),	/* 18612 */
/* 2690 */	0x86,		/* 134 */
			0x53,		/* 83 */
/* 2692 */	0xd8,		/* 216 */
			0x29,		/* 41 */
/* 2694 */	0xd1,		/* 209 */
			0xec,		/* 236 */
/* 2696 */	0xb7,		/* 183 */
			0x78,		/* 120 */

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
			},
			{
			VARIANT_UserSize
			,VARIANT_UserMarshal
			,VARIANT_UserUnmarshal
			,VARIANT_UserFree
			}

		};


static void __RPC_USER IVwTextSource_FetchExprEval_0000( PMIDL_STUB_MESSAGE pStubMsg )
{
	#pragma pack(4)
	struct _PARAM_STRUCT
		{
		ILgIcuResourceBundle *This;
		int ichMin;
		int ichLim;
		OLECHAR *prgchBuf;
		HRESULT _RetVal;
		};
	#pragma pack()
	struct _PARAM_STRUCT *pS	=	( struct _PARAM_STRUCT * )pStubMsg->StackTop;

	pStubMsg->Offset = 0;
	pStubMsg->MaxCount = ( unsigned long ) ( pS->ichLim - pS->ichMin );
}

static void __RPC_USER ILgCharacterPropertyEngine_GetLineBreakInfoExprEval_0001( PMIDL_STUB_MESSAGE pStubMsg )
{
	#pragma pack(4)
	struct _PARAM_STRUCT
		{
		ILgIcuResourceBundle *This;
		const OLECHAR *prgchIn;
		int cchIn;
		int ichMin;
		int ichLim;
		byte *prglbsOut;
		int *pichBreak;
		HRESULT _RetVal;
		};
	#pragma pack()
	struct _PARAM_STRUCT *pS	=	( struct _PARAM_STRUCT * )pStubMsg->StackTop;

	pStubMsg->Offset = 0;
	pStubMsg->MaxCount = ( unsigned long ) ( pS->ichLim - pS->ichMin );
}

static const EXPR_EVAL ExprEvalRoutines[] =
	{
	IVwTextSource_FetchExprEval_0000
	,ILgCharacterPropertyEngine_GetLineBreakInfoExprEval_0001
	};



/* Standard interface: __MIDL_itf_FwKernelPs_0000, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IUnknown, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}} */


/* Object interface: IFwCustomExport, ver. 0.0,
   GUID={0x40300033,0xD5F9,0x4136,{0x9A,0x8C,0xB4,0x01,0xD8,0x58,0x2E,0x9B}} */

#pragma code_seg(".orpc")
static const unsigned short IFwCustomExport_FormatStringOffsetTable[] =
	{
	0,
	42,
	84,
	138,
	192,
	246,
	300,
	348,
	402,
	462,
	564,
	606
	};

static const MIDL_STUBLESS_PROXY_INFO IFwCustomExport_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IFwCustomExport_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IFwCustomExport_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IFwCustomExport_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(15) _IFwCustomExportProxyVtbl =
{
	&IFwCustomExport_ProxyInfo,
	&IID_IFwCustomExport,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::SetLabelStyles */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::AddFlidCharStyleMapping */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::BuildSubItemsString */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::BuildObjRefSeqString */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::BuildObjRefAtomicString */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::BuildExpandableString */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::GetEnumString */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::GetActualLevel */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::BuildRecordTags */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::GetPageSetupInfo */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::PostProcessFile */ ,
	(void *) (INT_PTR) -1 /* IFwCustomExport::IncludeObjectData */
};

const CInterfaceStubVtbl _IFwCustomExportStubVtbl =
{
	&IID_IFwCustomExport,
	&IFwCustomExport_ServerInfo,
	15,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0257, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IFwTool, ver. 0.0,
   GUID={0x37396941,0x4DD1,0x11d4,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short IFwTool_FormatStringOffsetTable[] =
	{
	642,
	726,
	846,
	888
	};

static const MIDL_STUBLESS_PROXY_INFO IFwTool_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IFwTool_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IFwTool_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IFwTool_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IFwToolProxyVtbl =
{
	&IFwTool_ProxyInfo,
	&IID_IFwTool,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IFwTool::NewMainWnd */ ,
	(void *) (INT_PTR) -1 /* IFwTool::NewMainWndWithSel */ ,
	(void *) (INT_PTR) -1 /* IFwTool::CloseMainWnd */ ,
	(void *) (INT_PTR) -1 /* IFwTool::CloseDbAndWindows */
};

const CInterfaceStubVtbl _IFwToolStubVtbl =
{
	&IID_IFwTool,
	&IFwTool_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0258, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IUndoAction, ver. 0.0,
   GUID={0x2F6BB7C9,0x1B3A,0x4e94,{0xA7,0xBF,0x78,0x2C,0x23,0x69,0xF6,0x81}} */

#pragma code_seg(".orpc")
static const unsigned short IUndoAction_FormatStringOffsetTable[] =
	{
	936,
	972,
	1008,
	1038
	};

static const MIDL_STUBLESS_PROXY_INFO IUndoAction_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IUndoAction_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IUndoAction_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IUndoAction_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IUndoActionProxyVtbl =
{
	&IUndoAction_ProxyInfo,
	&IID_IUndoAction,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IUndoAction::Undo */ ,
	(void *) (INT_PTR) -1 /* IUndoAction::Redo */ ,
	(void *) (INT_PTR) -1 /* IUndoAction::Commit */ ,
	(void *) (INT_PTR) -1 /* IUndoAction::IsDataChange */
};

const CInterfaceStubVtbl _IUndoActionStubVtbl =
{
	&IID_IUndoAction,
	&IUndoAction_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0259, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IActionHandler, ver. 0.0,
   GUID={0x32C2020C,0x3094,0x42bc,{0x80,0xFF,0x45,0xAD,0x89,0x82,0x6F,0x62}} */

#pragma code_seg(".orpc")
static const unsigned short IActionHandler_FormatStringOffsetTable[] =
	{
	0,
	1074,
	1008,
	1104,
	1134,
	1176,
	1224,
	1260,
	1296,
	1338,
	1374,
	606,
	1416,
	1452,
	1488,
	1524,
	1554,
	1584,
	1620,
	1668,
	1704,
	1740,
	1782,
	1818,
	1854,
	1890,
	1926
	};

static const MIDL_STUBLESS_PROXY_INFO IActionHandler_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IActionHandler_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IActionHandler_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IActionHandler_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(30) _IActionHandlerProxyVtbl =
{
	&IActionHandler_ProxyInfo,
	&IID_IActionHandler,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IActionHandler::BeginUndoTask */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::EndUndoTask */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::ContinueUndoTask */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::EndOuterUndoTask */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::BreakUndoTask */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::StartSeq */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::AddAction */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::GetUndoText */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::GetUndoTextN */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::GetRedoText */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::GetRedoTextN */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::CanUndo */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::CanRedo */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::Undo */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::Redo */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::Commit */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::Close */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::Mark */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::CollapseToMark */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::DiscardToMark */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::get_TopMarkHandle */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::get_TasksSinceMark */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::get_UndoableActionCount */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::get_UndoableSequenceCount */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::get_RedoableSequenceCount */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::get_UndoGrouper */ ,
	(void *) (INT_PTR) -1 /* IActionHandler::put_UndoGrouper */
};

const CInterfaceStubVtbl _IActionHandlerStubVtbl =
{
	&IID_IActionHandler,
	&IActionHandler_ServerInfo,
	30,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0260, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IAdvInd, ver. 0.0,
   GUID={0x5F74AB40,0xEFE8,0x4a0d,{0xB9,0xAE,0x30,0xF4,0x93,0xFE,0x6E,0x21}} */

#pragma code_seg(".orpc")
static const unsigned short IAdvInd_FormatStringOffsetTable[] =
	{
	1962
	};

static const MIDL_STUBLESS_PROXY_INFO IAdvInd_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IAdvInd_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IAdvInd_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IAdvInd_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IAdvIndProxyVtbl =
{
	&IAdvInd_ProxyInfo,
	&IID_IAdvInd,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IAdvInd::Step */
};

const CInterfaceStubVtbl _IAdvIndStubVtbl =
{
	&IID_IAdvInd,
	&IAdvInd_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0261, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IAdvInd2, ver. 0.0,
   GUID={0x639C98DB,0xA241,0x496d,{0xBE,0x19,0x1E,0xFC,0x85,0xCA,0x1D,0xD7}} */

#pragma code_seg(".orpc")
static const unsigned short IAdvInd2_FormatStringOffsetTable[] =
	{
	1962,
	1074
	};

static const MIDL_STUBLESS_PROXY_INFO IAdvInd2_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IAdvInd2_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IAdvInd2_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IAdvInd2_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _IAdvInd2ProxyVtbl =
{
	&IAdvInd2_ProxyInfo,
	&IID_IAdvInd2,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IAdvInd::Step */ ,
	(void *) (INT_PTR) -1 /* IAdvInd2::NextStage */
};

const CInterfaceStubVtbl _IAdvInd2StubVtbl =
{
	&IID_IAdvInd2,
	&IAdvInd2_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0262, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IAdvInd3, ver. 0.0,
   GUID={0x86b6ae62,0x3dfa,0x4020,{0xb5,0xd1,0x7f,0xa2,0x8e,0x77,0x26,0xe4}} */

#pragma code_seg(".orpc")
static const unsigned short IAdvInd3_FormatStringOffsetTable[] =
	{
	1962,
	1998,
	2034,
	2070,
	2106,
	2142
	};

static const MIDL_STUBLESS_PROXY_INFO IAdvInd3_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IAdvInd3_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IAdvInd3_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IAdvInd3_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(9) _IAdvInd3ProxyVtbl =
{
	&IAdvInd3_ProxyInfo,
	&IID_IAdvInd3,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IAdvInd::Step */ ,
	(void *) (INT_PTR) -1 /* IAdvInd3::put_Title */ ,
	(void *) (INT_PTR) -1 /* IAdvInd3::put_Message */ ,
	(void *) (INT_PTR) -1 /* IAdvInd3::put_Position */ ,
	(void *) (INT_PTR) -1 /* IAdvInd3::put_StepSize */ ,
	(void *) (INT_PTR) -1 /* IAdvInd3::SetRange */
};

const CInterfaceStubVtbl _IAdvInd3StubVtbl =
{
	&IID_IAdvInd3,
	&IAdvInd3_ServerInfo,
	9,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0263, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDebugReportSink, ver. 0.0,
   GUID={0x14E389C6,0xC986,0x4e31,{0xAE,0x70,0x1C,0xC1,0x0C,0xC3,0x54,0x71}} */

#pragma code_seg(".orpc")
static const unsigned short IDebugReportSink_FormatStringOffsetTable[] =
	{
	2184
	};

static const MIDL_STUBLESS_PROXY_INFO IDebugReportSink_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IDebugReportSink_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IDebugReportSink_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IDebugReportSink_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IDebugReportSinkProxyVtbl =
{
	&IDebugReportSink_ProxyInfo,
	&IID_IDebugReportSink,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IDebugReportSink::Report */
};

const CInterfaceStubVtbl _IDebugReportSinkStubVtbl =
{
	&IID_IDebugReportSink,
	&IDebugReportSink_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0264, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDebugReport, ver. 0.0,
   GUID={0x7AE7CF67,0x67BE,0x4860,{0x8E,0x72,0xAA,0xC8,0x82,0x94,0xC3,0x97}} */

#pragma code_seg(".orpc")
static const unsigned short IDebugReport_FormatStringOffsetTable[] =
	{
	2226,
	2262
	};

static const MIDL_STUBLESS_PROXY_INFO IDebugReport_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IDebugReport_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IDebugReport_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IDebugReport_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _IDebugReportProxyVtbl =
{
	&IDebugReport_ProxyInfo,
	&IID_IDebugReport,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IDebugReport::ShowAssertMessageBox */ ,
	(void *) (INT_PTR) -1 /* IDebugReport::SetSink */
};

const CInterfaceStubVtbl _IDebugReportStubVtbl =
{
	&IID_IDebugReport,
	&IDebugReport_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0265, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IHelpTopicProvider, ver. 0.0,
   GUID={0xAF8960FB,0xB7AF,0x4259,{0x83,0x2B,0x38,0xA3,0xF5,0x62,0x90,0x52}} */

#pragma code_seg(".orpc")
static const unsigned short IHelpTopicProvider_FormatStringOffsetTable[] =
	{
	2298
	};

static const MIDL_STUBLESS_PROXY_INFO IHelpTopicProvider_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IHelpTopicProvider_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IHelpTopicProvider_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IHelpTopicProvider_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IHelpTopicProviderProxyVtbl =
{
	&IHelpTopicProvider_ProxyInfo,
	&IID_IHelpTopicProvider,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IHelpTopicProvider::GetHelpString */
};

const CInterfaceStubVtbl _IHelpTopicProviderStubVtbl =
{
	&IID_IHelpTopicProvider,
	&IHelpTopicProvider_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0266, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IFwFldSpec, ver. 0.0,
   GUID={0xFE44E19B,0xE710,0x4635,{0x96,0x90,0x1A,0xFB,0x45,0x1B,0x12,0x26}} */

#pragma code_seg(".orpc")
static const unsigned short IFwFldSpec_FormatStringOffsetTable[] =
	{
	1962,
	2346,
	2382,
	1038,
	2418,
	2454,
	2490,
	2526,
	2562,
	1338,
	2598,
	2634,
	2670,
	2706
	};

static const MIDL_STUBLESS_PROXY_INFO IFwFldSpec_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IFwFldSpec_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IFwFldSpec_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IFwFldSpec_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(17) _IFwFldSpecProxyVtbl =
{
	&IFwFldSpec_ProxyInfo,
	&IID_IFwFldSpec,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::put_Visibility */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::get_Visibility */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::put_HideLabel */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::get_HideLabel */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::put_Label */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::get_Label */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::put_FieldId */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::get_FieldId */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::put_ClassName */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::get_ClassName */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::put_FieldName */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::get_FieldName */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::put_Style */ ,
	(void *) (INT_PTR) -1 /* IFwFldSpec::get_Style */
};

const CInterfaceStubVtbl _IFwFldSpecStubVtbl =
{
	&IID_IFwFldSpec,
	&IFwFldSpec_ServerInfo,
	17,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0267, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IUndoGrouper, ver. 0.0,
   GUID={0xC38348D3,0x392C,0x4e02,{0xBD,0x50,0xA0,0x1D,0xC4,0x18,0x9E,0x1D}} */

#pragma code_seg(".orpc")
static const unsigned short IUndoGrouper_FormatStringOffsetTable[] =
	{
	2742,
	2778,
	2814
	};

static const MIDL_STUBLESS_PROXY_INFO IUndoGrouper_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IUndoGrouper_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IUndoGrouper_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IUndoGrouper_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(6) _IUndoGrouperProxyVtbl =
{
	&IUndoGrouper_ProxyInfo,
	&IID_IUndoGrouper,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IUndoGrouper::BeginGroup */ ,
	(void *) (INT_PTR) -1 /* IUndoGrouper::EndGroup */ ,
	(void *) (INT_PTR) -1 /* IUndoGrouper::CancelGroup */
};

const CInterfaceStubVtbl _IUndoGrouperStubVtbl =
{
	&IID_IUndoGrouper,
	&IUndoGrouper_ServerInfo,
	6,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0268, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsString, ver. 0.0,
   GUID={0x0E9E5A6C,0xBA20,0x4245,{0x8E,0x26,0x71,0x9A,0x67,0xFE,0x18,0x92}} */

#pragma code_seg(".orpc")
static const unsigned short ITsString_FormatStringOffsetTable[] =
	{
	2850,
	2346,
	2886,
	2922,
	2964,
	3006,
	3048,
	3096,
	3144,
	3192,
	3234,
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	3282,
	3324,
	3366,
	3402,
	3438,
	3474,
	3510,
	3558,
	3600,
	3660,
	3702,
	(unsigned short) -1
	};

static const MIDL_STUBLESS_PROXY_INFO ITsString_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsString_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsString_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsString_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(31) _ITsStringProxyVtbl =
{
	&ITsString_ProxyInfo,
	&IID_ITsString,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsString::get_Text */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_Length */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_RunCount */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_RunAt */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_MinOfRun */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_LimOfRun */ ,
	(void *) (INT_PTR) -1 /* ITsString::GetBoundsOfRun */ ,
	(void *) (INT_PTR) -1 /* ITsString::FetchRunInfoAt */ ,
	(void *) (INT_PTR) -1 /* ITsString::FetchRunInfo */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_RunText */ ,
	(void *) (INT_PTR) -1 /* ITsString::GetChars */ ,
	0 /* (void *) (INT_PTR) -1 /* ITsString::FetchChars */ ,
	0 /* (void *) (INT_PTR) -1 /* ITsString::LockText */ ,
	0 /* (void *) (INT_PTR) -1 /* ITsString::UnlockText */ ,
	0 /* (void *) (INT_PTR) -1 /* ITsString::LockRun */ ,
	0 /* (void *) (INT_PTR) -1 /* ITsString::UnlockRun */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_PropertiesAt */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_Properties */ ,
	(void *) (INT_PTR) -1 /* ITsString::GetBldr */ ,
	(void *) (INT_PTR) -1 /* ITsString::GetIncBldr */ ,
	(void *) (INT_PTR) -1 /* ITsString::GetFactoryClsid */ ,
	(void *) (INT_PTR) -1 /* ITsString::SerializeFmt */ ,
	(void *) (INT_PTR) -1 /* ITsString::SerializeFmtRgb */ ,
	(void *) (INT_PTR) -1 /* ITsString::Equals */ ,
	(void *) (INT_PTR) -1 /* ITsString::WriteAsXml */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_IsNormalizedForm */ ,
	(void *) (INT_PTR) -1 /* ITsString::get_NormalizedForm */ ,
	0 /* (void *) (INT_PTR) -1 /* ITsString::NfdAndFixOffsets */
};

const CInterfaceStubVtbl _ITsStringStubVtbl =
{
	&IID_ITsString,
	&ITsString_ServerInfo,
	31,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0269, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsTextProps, ver. 0.0,
   GUID={0x4FA0B99A,0x5A56,0x41A4,{0xBE,0x8B,0xB8,0x9B,0xC6,0x22,0x51,0xA5}} */

#pragma code_seg(".orpc")
static const unsigned short ITsTextProps_FormatStringOffsetTable[] =
	{
	2742,
	3744,
	3798,
	3846,
	3882,
	3930,
	3972,
	4008,
	4044,
	4080,
	4128,
	4194
	};

static const MIDL_STUBLESS_PROXY_INFO ITsTextProps_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsTextProps_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsTextProps_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsTextProps_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(15) _ITsTextPropsProxyVtbl =
{
	&ITsTextProps_ProxyInfo,
	&IID_ITsTextProps,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsTextProps::get_IntPropCount */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::GetIntProp */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::GetIntPropValues */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::get_StrPropCount */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::GetStrProp */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::GetStrPropValue */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::GetBldr */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::GetFactoryClsid */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::Serialize */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::SerializeRgb */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::SerializeRgPropsRgb */ ,
	(void *) (INT_PTR) -1 /* ITsTextProps::WriteAsXml */
};

const CInterfaceStubVtbl _ITsTextPropsStubVtbl =
{
	&IID_ITsTextProps,
	&ITsTextProps_ServerInfo,
	15,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0270, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsStrFactory, ver. 0.0,
   GUID={0xF1EF76E4,0xBE04,0x11d3,{0x8D,0x9A,0x00,0x50,0x04,0xDE,0xFE,0xC4}} */

#pragma code_seg(".orpc")
static const unsigned short ITsStrFactory_FormatStringOffsetTable[] =
	{
	4242,
	4290,
	4338,
	4392,
	4452,
	4500,
	4554,
	4608,
	4644,
	4680,
	4728,
	4788
	};

static const MIDL_STUBLESS_PROXY_INFO ITsStrFactory_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsStrFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsStrFactory_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsStrFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(15) _ITsStrFactoryProxyVtbl =
{
	&ITsStrFactory_ProxyInfo,
	&IID_ITsStrFactory,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::DeserializeStringStreams */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::DeserializeString */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::DeserializeStringRgb */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::DeserializeStringRgch */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::MakeString */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::MakeStringRgch */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::MakeStringWithPropsRgch */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::GetBldr */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::GetIncBldr */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::get_RunCount */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::FetchRunInfoAt */ ,
	(void *) (INT_PTR) -1 /* ITsStrFactory::FetchRunInfo */
};

const CInterfaceStubVtbl _ITsStrFactoryStubVtbl =
{
	&IID_ITsStrFactory,
	&ITsStrFactory_ServerInfo,
	15,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0271, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsPropsFactory, ver. 0.0,
   GUID={0x8DCE56A6,0xCFF1,0x4402,{0x95,0xFE,0x2B,0x57,0x49,0x12,0xB5,0x4E}} */

#pragma code_seg(".orpc")
static const unsigned short ITsPropsFactory_FormatStringOffsetTable[] =
	{
	4848,
	4890,
	4938,
	5004,
	5058,
	5118
	};

static const MIDL_STUBLESS_PROXY_INFO ITsPropsFactory_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsPropsFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsPropsFactory_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsPropsFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(9) _ITsPropsFactoryProxyVtbl =
{
	&ITsPropsFactory_ProxyInfo,
	&IID_ITsPropsFactory,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsPropsFactory::DeserializeProps */ ,
	(void *) (INT_PTR) -1 /* ITsPropsFactory::DeserializePropsRgb */ ,
	(void *) (INT_PTR) -1 /* ITsPropsFactory::DeserializeRgPropsRgb */ ,
	(void *) (INT_PTR) -1 /* ITsPropsFactory::MakeProps */ ,
	(void *) (INT_PTR) -1 /* ITsPropsFactory::MakePropsRgch */ ,
	(void *) (INT_PTR) -1 /* ITsPropsFactory::GetPropsBldr */
};

const CInterfaceStubVtbl _ITsPropsFactoryStubVtbl =
{
	&IID_ITsPropsFactory,
	&ITsPropsFactory_ServerInfo,
	9,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0272, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsStrBldr, ver. 0.0,
   GUID={0xF1EF76E6,0xBE04,0x11d3,{0x8D,0x9A,0x00,0x50,0x04,0xDE,0xFE,0xC4}} */

#pragma code_seg(".orpc")
static const unsigned short ITsStrBldr_FormatStringOffsetTable[] =
	{
	2850,
	2346,
	2886,
	2922,
	5154,
	5202,
	5250,
	5298,
	5340,
	(unsigned short) -1,
	5388,
	5430,
	5472,
	5526,
	5574,
	5634,
	5682,
	5742,
	5796,
	5832,
	3438,
	3474,
	3510
	};

static const MIDL_STUBLESS_PROXY_INFO ITsStrBldr_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsStrBldr_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsStrBldr_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsStrBldr_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(26) _ITsStrBldrProxyVtbl =
{
	&ITsStrBldr_ProxyInfo,
	&IID_ITsStrBldr,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::get_Text */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::get_Length */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::get_RunCount */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::get_RunAt */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::GetBoundsOfRun */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::FetchRunInfoAt */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::FetchRunInfo */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::get_RunText */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::GetChars */ ,
	0 /* (void *) (INT_PTR) -1 /* ITsStrBldr::FetchChars */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::get_PropertiesAt */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::get_Properties */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::Replace */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::ReplaceTsString */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::ReplaceRgch */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::SetProperties */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::SetIntPropValues */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::SetStrPropValue */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::GetString */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::Clear */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::GetBldrClsid */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::SerializeFmt */ ,
	(void *) (INT_PTR) -1 /* ITsStrBldr::SerializeFmtRgb */
};

const CInterfaceStubVtbl _ITsStrBldrStubVtbl =
{
	&IID_ITsStrBldr,
	&ITsStrBldr_ServerInfo,
	26,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0273, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsIncStrBldr, ver. 0.0,
   GUID={0xF1EF76E7,0xBE04,0x11d3,{0x8D,0x9A,0x00,0x50,0x04,0xDE,0xFE,0xC4}} */

#pragma code_seg(".orpc")
static const unsigned short ITsIncStrBldr_FormatStringOffsetTable[] =
	{
	2850,
	1998,
	5862,
	5898,
	5940,
	5988,
	6030,
	6066,
	6096,
	6132,
	6168
	};

static const MIDL_STUBLESS_PROXY_INFO ITsIncStrBldr_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsIncStrBldr_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsIncStrBldr_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsIncStrBldr_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(14) _ITsIncStrBldrProxyVtbl =
{
	&ITsIncStrBldr_ProxyInfo,
	&IID_ITsIncStrBldr,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::get_Text */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::Append */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::AppendTsString */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::AppendRgch */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::SetIntPropValues */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::SetStrPropValue */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::GetString */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::Clear */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::GetIncBldrClsid */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::SerializeFmt */ ,
	(void *) (INT_PTR) -1 /* ITsIncStrBldr::SerializeFmtRgb */
};

const CInterfaceStubVtbl _ITsIncStrBldrStubVtbl =
{
	&IID_ITsIncStrBldr,
	&ITsIncStrBldr_ServerInfo,
	14,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0274, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsPropsBldr, ver. 0.0,
   GUID={0xF1EF76E8,0xBE04,0x11d3,{0x8D,0x9A,0x00,0x50,0x04,0xDE,0xFE,0xC4}} */

#pragma code_seg(".orpc")
static const unsigned short ITsPropsBldr_FormatStringOffsetTable[] =
	{
	2742,
	3744,
	3798,
	3846,
	3882,
	3930,
	6216,
	6264,
	6306,
	6354
	};

static const MIDL_STUBLESS_PROXY_INFO ITsPropsBldr_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsPropsBldr_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsPropsBldr_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsPropsBldr_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(13) _ITsPropsBldrProxyVtbl =
{
	&ITsPropsBldr_ProxyInfo,
	&IID_ITsPropsBldr,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::get_IntPropCount */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::GetIntProp */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::GetIntPropValues */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::get_StrPropCount */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::GetStrProp */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::GetStrPropValue */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::SetIntPropValues */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::SetStrPropValue */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::SetStrPropValueRgch */ ,
	(void *) (INT_PTR) -1 /* ITsPropsBldr::GetTextProps */
};

const CInterfaceStubVtbl _ITsPropsBldrStubVtbl =
{
	&IID_ITsPropsBldr,
	&ITsPropsBldr_ServerInfo,
	13,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0275, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsMultiString, ver. 0.0,
   GUID={0xDD409520,0xC212,0x11d3,{0x9B,0xB7,0x00,0x40,0x05,0x41,0xF9,0xE9}} */

#pragma code_seg(".orpc")
static const unsigned short ITsMultiString_FormatStringOffsetTable[] =
	{
	2742,
	6390,
	6438,
	6480
	};

static const MIDL_STUBLESS_PROXY_INFO ITsMultiString_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsMultiString_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsMultiString_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsMultiString_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _ITsMultiStringProxyVtbl =
{
	&ITsMultiString_ProxyInfo,
	&IID_ITsMultiString,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsMultiString::get_StringCount */ ,
	(void *) (INT_PTR) -1 /* ITsMultiString::GetStringFromIndex */ ,
	(void *) (INT_PTR) -1 /* ITsMultiString::get_String */ ,
	(void *) (INT_PTR) -1 /* ITsMultiString::putref_String */
};

const CInterfaceStubVtbl _ITsMultiStringStubVtbl =
{
	&IID_ITsMultiString,
	&ITsMultiString_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0276, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ITsStreamWrapper, ver. 0.0,
   GUID={0x4516897E,0x314B,0x49d8,{0x83,0x78,0xF2,0xE1,0x05,0xC8,0x00,0x09}} */

#pragma code_seg(".orpc")
static const unsigned short ITsStreamWrapper_FormatStringOffsetTable[] =
	{
	6522,
	6558,
	2034,
	6594,
	6654
	};

static const MIDL_STUBLESS_PROXY_INFO ITsStreamWrapper_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ITsStreamWrapper_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ITsStreamWrapper_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ITsStreamWrapper_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(8) _ITsStreamWrapperProxyVtbl =
{
	&ITsStreamWrapper_ProxyInfo,
	&IID_ITsStreamWrapper,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ITsStreamWrapper::get_Stream */ ,
	(void *) (INT_PTR) -1 /* ITsStreamWrapper::get_Contents */ ,
	(void *) (INT_PTR) -1 /* ITsStreamWrapper::put_Contents */ ,
	(void *) (INT_PTR) -1 /* ITsStreamWrapper::WriteTssAsXml */ ,
	(void *) (INT_PTR) -1 /* ITsStreamWrapper::ReadTssFromXml */
};

const CInterfaceStubVtbl _ITsStreamWrapperStubVtbl =
{
	&IID_ITsStreamWrapper,
	&ITsStreamWrapper_ServerInfo,
	8,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0277, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IBackupDelegates, ver. 0.0,
   GUID={0x1C0FA5AF,0x00B4,0x4dc1,{0x8F,0x9E,0x16,0x8A,0xF3,0xF8,0x92,0xB0}} */

#pragma code_seg(".orpc")
static const unsigned short IBackupDelegates_FormatStringOffsetTable[] =
	{
	2850,
	6696,
	6732,
	6774,
	6828,
	6858,
	6888,
	6936
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


/* Standard interface: __MIDL_itf_FwKernelPs_0278, ver. 0.0,
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
	6978,
	6858,
	7020,
	6066,
	7050
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


/* Standard interface: __MIDL_itf_FwKernelPs_0280, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDisconnectDb, ver. 0.0,
   GUID={0x0CC74E0C,0x3017,0x4c02,{0xA5,0x07,0x3F,0xB8,0xCE,0x62,0x1C,0xDC}} */

#pragma code_seg(".orpc")
static const unsigned short IDisconnectDb_FormatStringOffsetTable[] =
	{
	7098,
	2346,
	7170,
	1104
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


/* Standard interface: __MIDL_itf_FwKernelPs_0282, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IRemoteDbWarn, ver. 0.0,
   GUID={0x004C42AE,0xCB07,0x47b5,{0xA9,0x36,0xD9,0xCA,0x4A,0xC4,0x66,0xD7}} */

#pragma code_seg(".orpc")
static const unsigned short IRemoteDbWarn_FormatStringOffsetTable[] =
	{
	7206,
	7254,
	1008
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


/* Standard interface: __MIDL_itf_FwKernelPs_0284, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDbWarnSetup, ver. 0.0,
   GUID={0x06082023,0xC2BA,0x4425,{0x90,0xFD,0x2F,0x76,0xB7,0x4C,0xCB,0xE7}} */

#pragma code_seg(".orpc")
static const unsigned short IDbWarnSetup_FormatStringOffsetTable[] =
	{
	7296,
	1074
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


/* Standard interface: __MIDL_itf_FwKernelPs_0285, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IOleDbCommand, ver. 0.0,
   GUID={0x21993161,0x3E24,0x11d4,{0xA1,0xBD,0x00,0xC0,0x4F,0x0C,0x95,0x93}} */

#pragma code_seg(".orpc")
static const unsigned short IOleDbCommand_FormatStringOffsetTable[] =
	{
	2742,
	7254,
	7326,
	2922,
	7392,
	7446,
	7482,
	7524,
	7560,
	7626,
	7686
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


/* Standard interface: __MIDL_itf_FwKernelPs_0287, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IOleDbEncap, ver. 0.0,
   GUID={0xCB7BEA0F,0x960A,0x4b23,{0x80,0xD3,0xDE,0x06,0xC0,0x53,0x0E,0x04}} */

#pragma code_seg(".orpc")
static const unsigned short IOleDbEncap_FormatStringOffsetTable[] =
	{
	7296,
	1074,
	7746,
	7782,
	7842,
	6858,
	7878,
	1260,
	7914,
	7950,
	7992,
	2634,
	8028
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


/* Standard interface: __MIDL_itf_FwKernelPs_0288, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IFwMetaDataCache, ver. 0.0,
   GUID={0x6AA9042E,0x0A4D,0x4f33,{0x88,0x1B,0x3F,0xBE,0x48,0x86,0x1D,0x14}} */

#pragma code_seg(".orpc")
static const unsigned short IFwMetaDataCache_FormatStringOffsetTable[] =
	{
	8070,
	8106,
	8148,
	3846,
	8190,
	3930,
	8232,
	8274,
	8316,
	3192,
	1374,
	8358,
	8400,
	8442,
	8484,
	8526,
	8568,
	1584,
	8616,
	8658,
	8700,
	8742,
	8784,
	8826,
	8892,
	8934,
	8988,
	9042,
	9096,
	9150
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


/* Standard interface: __MIDL_itf_FwKernelPs_0289, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IDbAdmin, ver. 0.0,
   GUID={0x2A861F95,0x63D0,0x480d,{0xB5,0xAF,0x4F,0xAF,0x0D,0x22,0x12,0x5D}} */

#pragma code_seg(".orpc")
static const unsigned short IDbAdmin_FormatStringOffsetTable[] =
	{
	0,
	9204,
	2034,
	9246,
	9306,
	9342,
	9378,
	1260,
	7914
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


/* Standard interface: __MIDL_itf_FwKernelPs_0290, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ISimpleInit, ver. 0.0,
   GUID={0xFC1C0D0D,0x0483,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short ISimpleInit_FormatStringOffsetTable[] =
	{
	9414,
	6558
	};

static const MIDL_STUBLESS_PROXY_INFO ISimpleInit_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ISimpleInit_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ISimpleInit_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ISimpleInit_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _ISimpleInitProxyVtbl =
{
	&ISimpleInit_ProxyInfo,
	&IID_ISimpleInit,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ISimpleInit::InitNew */ ,
	(void *) (INT_PTR) -1 /* ISimpleInit::get_InitializationData */
};

const CInterfaceStubVtbl _ISimpleInitStubVtbl =
{
	&IID_ISimpleInit,
	&ISimpleInit_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0291, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwGraphics, ver. 0.0,
   GUID={0x3A3CE0A1,0xB5EB,0x43bd,{0x9C,0x89,0x35,0xEA,0xA1,0x10,0xF1,0x2B}} */

#pragma code_seg(".orpc")
static const unsigned short IVwGraphics_FormatStringOffsetTable[] =
	{
	9456,
	2778,
	2814,
	9510,
	9564,
	9636,
	9690,
	9750,
	9822,
	9876,
	9936,
	9990,
	10026,
	10098,
	10146,
	10200,
	10254,
	1584,
	10290,
	5832,
	1704,
	10326,
	1782,
	10362,
	10398,
	10434,
	10470,
	10500,
	10542,
	10632
	};

static const MIDL_STUBLESS_PROXY_INFO IVwGraphics_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwGraphics_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwGraphics_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwGraphics_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(33) _IVwGraphicsProxyVtbl =
{
	&IVwGraphics_ProxyInfo,
	&IID_IVwGraphics,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwGraphics::InvertRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_ForeColor */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_BackColor */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawRectangle */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawHorzLine */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawLine */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawText */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawTextExt */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetTextExtent */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetTextLeadWidth */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetClipRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetFontEmSquare */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetGlyphMetrics */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetFontData */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetFontDataRgch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::XYFromGlyphPoint */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_FontAscent */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_FontDescent */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_FontCharProperties */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::ReleaseDC */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_XUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_XUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_YUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_YUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::SetupGraphics */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::PushClipRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::PopClipRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawPolygon */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::RenderPicture */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::MakePicture */
};

const CInterfaceStubVtbl _IVwGraphicsStubVtbl =
{
	&IID_IVwGraphics,
	&IVwGraphics_ServerInfo,
	33,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0292, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwGraphicsWin32, ver. 0.0,
   GUID={0x8E6828A3,0x8681,0x4822,{0xB7,0x6D,0x6C,0x4A,0x25,0xCA,0xEC,0xE6}} */

#pragma code_seg(".orpc")
static const unsigned short IVwGraphicsWin32_FormatStringOffsetTable[] =
	{
	9456,
	2778,
	2814,
	9510,
	9564,
	9636,
	9690,
	9750,
	9822,
	9876,
	9936,
	9990,
	10026,
	10098,
	10146,
	10200,
	10254,
	1584,
	10290,
	5832,
	1704,
	10326,
	1782,
	10362,
	10398,
	10434,
	10470,
	10500,
	10542,
	10632,
	(unsigned short) -1,
	(unsigned short) -1,
	(unsigned short) -1,
	10680
	};

static const MIDL_STUBLESS_PROXY_INFO IVwGraphicsWin32_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwGraphicsWin32_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwGraphicsWin32_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwGraphicsWin32_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(37) _IVwGraphicsWin32ProxyVtbl =
{
	&IVwGraphicsWin32_ProxyInfo,
	&IID_IVwGraphicsWin32,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwGraphics::InvertRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_ForeColor */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_BackColor */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawRectangle */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawHorzLine */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawLine */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawText */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawTextExt */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetTextExtent */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetTextLeadWidth */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetClipRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetFontEmSquare */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetGlyphMetrics */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetFontData */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::GetFontDataRgch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::XYFromGlyphPoint */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_FontAscent */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_FontDescent */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_FontCharProperties */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::ReleaseDC */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_XUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_XUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::get_YUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::put_YUnitsPerInch */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::SetupGraphics */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::PushClipRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::PopClipRect */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::DrawPolygon */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::RenderPicture */ ,
	(void *) (INT_PTR) -1 /* IVwGraphics::MakePicture */ ,
	0 /* (void *) (INT_PTR) -1 /* IVwGraphicsWin32::Initialize */ ,
	0 /* (void *) (INT_PTR) -1 /* IVwGraphicsWin32::GetDeviceContext */ ,
	0 /* (void *) (INT_PTR) -1 /* IVwGraphicsWin32::SetMeasureDc */ ,
	(void *) (INT_PTR) -1 /* IVwGraphicsWin32::SetClipRect */
};

const CInterfaceStubVtbl _IVwGraphicsWin32StubVtbl =
{
	&IID_IVwGraphicsWin32,
	&IVwGraphicsWin32_ServerInfo,
	37,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0293, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwTextSource, ver. 0.0,
   GUID={0x92AC8BE4,0xEDC8,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short IVwTextSource_FormatStringOffsetTable[] =
	{
	10716,
	2346,
	10764,
	10818,
	10872,
	10932
	};

static const MIDL_STUBLESS_PROXY_INFO IVwTextSource_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwTextSource_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwTextSource_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwTextSource_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(9) _IVwTextSourceProxyVtbl =
{
	&IVwTextSource_ProxyInfo,
	&IID_IVwTextSource,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwTextSource::Fetch */ ,
	(void *) (INT_PTR) -1 /* IVwTextSource::get_Length */ ,
	(void *) (INT_PTR) -1 /* IVwTextSource::GetCharProps */ ,
	(void *) (INT_PTR) -1 /* IVwTextSource::GetParaProps */ ,
	(void *) (INT_PTR) -1 /* IVwTextSource::GetCharStringProp */ ,
	(void *) (INT_PTR) -1 /* IVwTextSource::GetParaStringProp */
};

const CInterfaceStubVtbl _IVwTextSourceStubVtbl =
{
	&IID_IVwTextSource,
	&IVwTextSource_ServerInfo,
	9,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0294, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IVwJustifier, ver. 0.0,
   GUID={0xBAC7725F,0x1D26,0x42b2,{0x8E,0x9D,0x8B,0x91,0x75,0x78,0x2C,0xC7}} */

#pragma code_seg(".orpc")
static const unsigned short IVwJustifier_FormatStringOffsetTable[] =
	{
	10992
	};

static const MIDL_STUBLESS_PROXY_INFO IVwJustifier_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IVwJustifier_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IVwJustifier_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IVwJustifier_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _IVwJustifierProxyVtbl =
{
	&IVwJustifier_ProxyInfo,
	&IID_IVwJustifier,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IVwJustifier::AdjustGlyphWidths */
};

const CInterfaceStubVtbl _IVwJustifierStubVtbl =
{
	&IID_IVwJustifier,
	&IVwJustifier_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0295, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgSegment, ver. 0.0,
   GUID={0x7407F0FC,0x58B0,0x4476,{0xA0,0xC8,0x69,0x43,0x18,0x01,0xE5,0x60}} */

#pragma code_seg(".orpc")
static const unsigned short ILgSegment_FormatStringOffsetTable[] =
	{
	11052,
	11112,
	11154,
	11202,
	11250,
	11298,
	11346,
	11394,
	11448,
	11508,
	11568,
	11616,
	11664,
	11706,
	11754,
	8526,
	11796,
	11838,
	11880,
	11928,
	11976,
	12024,
	12072,
	12114,
	12156,
	12210,
	12270,
	12348,
	12444,
	12528,
	12618,
	12690,
	12762,
	12846
	};

static const MIDL_STUBLESS_PROXY_INFO ILgSegment_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgSegment_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgSegment_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgSegment_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(37) _ILgSegmentProxyVtbl =
{
	&ILgSegment_ProxyInfo,
	&IID_ILgSegment,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgSegment::DrawText */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::Recompute */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_Width */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_RightOverhang */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_LeftOverhang */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_Height */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_Ascent */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::Extent */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::BoundingRect */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::GetActualWidth */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_AscentOverhang */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_DescentOverhang */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_RightToLeft */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_DirectionDepth */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::SetDirectionDepth */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_WritingSystem */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_Lim */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_LimInterest */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::put_EndLine */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::put_StartLine */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_StartBreakWeight */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_EndBreakWeight */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::get_Stretch */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::put_Stretch */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::IsValidInsertionPoint */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::DoBoundariesCoincide */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::DrawInsertionPoint */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::PositionsOfIP */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::DrawRange */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::PositionOfRange */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::PointToChar */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::ArrowKeyPosition */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::ExtendSelectionPosition */ ,
	(void *) (INT_PTR) -1 /* ILgSegment::GetCharPlacement */
};

const CInterfaceStubVtbl _ILgSegmentStubVtbl =
{
	&IID_ILgSegment,
	&ILgSegment_ServerInfo,
	37,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0296, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IRenderEngine, ver. 0.0,
   GUID={0x93CB892F,0x16D1,0x4dca,{0x9C,0x71,0x2E,0x80,0x4B,0xC9,0x39,0x5C}} */

#pragma code_seg(".orpc")
static const unsigned short IRenderEngine_FormatStringOffsetTable[] =
	{
	12948,
	1074,
	2886,
	12990,
	13128,
	13164,
	13200,
	13236,
	13272
	};

static const MIDL_STUBLESS_PROXY_INFO IRenderEngine_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IRenderEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IRenderEngine_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IRenderEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _IRenderEngineProxyVtbl =
{
	&IRenderEngine_ProxyInfo,
	&IID_IRenderEngine,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IRenderEngine::InitRenderer */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::FontIsValid */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::get_SegDatMaxLength */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::FindBreakPoint */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::get_ScriptDirection */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::get_ClassId */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::InterpretChrp */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::get_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* IRenderEngine::putref_WritingSystemFactory */
};

const CInterfaceStubVtbl _IRenderEngineStubVtbl =
{
	&IID_IRenderEngine,
	&IRenderEngine_ServerInfo,
	12,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0297, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IRenderingFeatures, ver. 0.0,
   GUID={0x0A439F99,0x7BF2,0x4e11,{0xA8,0x71,0x8A,0xFA,0xEB,0x2B,0x7D,0x53}} */

#pragma code_seg(".orpc")
static const unsigned short IRenderingFeatures_FormatStringOffsetTable[] =
	{
	13308,
	13356,
	13404,
	13464
	};

static const MIDL_STUBLESS_PROXY_INFO IRenderingFeatures_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IRenderingFeatures_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IRenderingFeatures_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IRenderingFeatures_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IRenderingFeaturesProxyVtbl =
{
	&IRenderingFeatures_ProxyInfo,
	&IID_IRenderingFeatures,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IRenderingFeatures::GetFeatureIDs */ ,
	(void *) (INT_PTR) -1 /* IRenderingFeatures::GetFeatureLabel */ ,
	(void *) (INT_PTR) -1 /* IRenderingFeatures::GetFeatureValues */ ,
	(void *) (INT_PTR) -1 /* IRenderingFeatures::GetFeatureValueLabel */
};

const CInterfaceStubVtbl _IRenderingFeaturesStubVtbl =
{
	&IID_IRenderingFeatures,
	&IRenderingFeatures_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0298, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: IJustifyingRenderer, ver. 0.0,
   GUID={0xD7364EF2,0x43C0,0x4440,{0x87,0x2A,0x33,0x6A,0x46,0x47,0xB9,0xA3}} */

#pragma code_seg(".orpc")
static const unsigned short IJustifyingRenderer_FormatStringOffsetTable[] =
	{
	13518,
	13572,
	13626,
	9510
	};

static const MIDL_STUBLESS_PROXY_INFO IJustifyingRenderer_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&IJustifyingRenderer_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO IJustifyingRenderer_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&IJustifyingRenderer_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _IJustifyingRendererProxyVtbl =
{
	&IJustifyingRenderer_ProxyInfo,
	&IID_IJustifyingRenderer,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* IJustifyingRenderer::GetGlyphAttributeFloat */ ,
	(void *) (INT_PTR) -1 /* IJustifyingRenderer::GetGlyphAttributeInt */ ,
	(void *) (INT_PTR) -1 /* IJustifyingRenderer::SetGlyphAttributeFloat */ ,
	(void *) (INT_PTR) -1 /* IJustifyingRenderer::SetGlyphAttributeInt */
};

const CInterfaceStubVtbl _IJustifyingRendererStubVtbl =
{
	&IID_IJustifyingRenderer,
	&IJustifyingRenderer_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0299, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgCollation, ver. 0.0,
   GUID={0x254DB9E3,0x0265,0x49CF,{0xA1,0x9F,0x3C,0x75,0xE8,0x52,0x5A,0x28}} */

#pragma code_seg(".orpc")
static const unsigned short ILgCollation_FormatStringOffsetTable[] =
	{
	13680,
	42,
	2886,
	13722,
	13128,
	13764,
	2490,
	1260,
	2562,
	1338,
	2598,
	2634,
	2670,
	13800,
	13836,
	13872,
	13914,
	13950,
	13986,
	14022,
	14058,
	14094
	};

static const MIDL_STUBLESS_PROXY_INFO ILgCollation_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgCollation_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgCollation_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgCollation_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(25) _ILgCollationProxyVtbl =
{
	&ILgCollation_ProxyInfo,
	&IID_ILgCollation,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_Name */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::put_Name */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_NameWsCount */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_NameWss */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_Hvo */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_WinLCID */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::put_WinLCID */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_WinCollation */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::put_WinCollation */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_IcuResourceName */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::put_IcuResourceName */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_IcuResourceText */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::put_IcuResourceText */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_Dirty */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::put_Dirty */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::WriteAsXml */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::Serialize */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::Deserialize */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_IcuRules */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::put_IcuRules */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::get_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ILgCollation::putref_WritingSystemFactory */
};

const CInterfaceStubVtbl _ILgCollationStubVtbl =
{
	&IID_ILgCollation,
	&ILgCollation_ServerInfo,
	25,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0300, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgWritingSystem, ver. 0.0,
   GUID={0x28BC5EDC,0x3EF3,0x4db2,{0x8B,0x90,0x55,0x62,0x00,0xFD,0x97,0xED}} */

#pragma code_seg(".orpc")
static const unsigned short ILgWritingSystem_FormatStringOffsetTable[] =
	{
	2742,
	2346,
	14130,
	14172,
	14214,
	13764,
	2490,
	14256,
	14298,
	14334,
	14370,
	14406,
	14442,
	14478,
	14508,
	14544,
	14580,
	14616,
	14652,
	14694,
	14730,
	14766,
	14802,
	14838,
	14880,
	14916,
	14952,
	14988,
	15024,
	15060,
	15096,
	15132,
	15168,
	15204,
	15240,
	15276,
	15312,
	15354,
	15390,
	15432,
	15474,
	15510,
	15552,
	15594,
	15630,
	15672,
	15714,
	15756,
	15792,
	15834,
	15870,
	15906,
	15942,
	15978,
	16014,
	16050,
	16098,
	16134,
	16170,
	16206,
	16242,
	16278,
	16314,
	16350,
	16386,
	16422,
	16458,
	16494,
	16530,
	16566,
	16602,
	16638
	};

static const MIDL_STUBLESS_PROXY_INFO ILgWritingSystem_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgWritingSystem_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgWritingSystem_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgWritingSystem_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(75) _ILgWritingSystemProxyVtbl =
{
	&ILgWritingSystem_ProxyInfo,
	&IID_ILgWritingSystem,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_WritingSystem */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_NameWsCount */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_NameWss */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_Name */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_Name */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_Locale */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_Locale */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_ConverterFrom */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_NormalizeEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_WordBreakEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_SpellingFactory */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_SpellCheckEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_SearchEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::CompileEngines */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_Dirty */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_Dirty */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::putref_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::WriteAsXml */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::Serialize */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::Deserialize */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_RightToLeft */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_RightToLeft */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_Renderer */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_FontVariation */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_FontVariation */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_SansFontVariation */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_SansFontVariation */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_DefaultSerif */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_DefaultSerif */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_DefaultSansSerif */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_DefaultSansSerif */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_DefaultMonospace */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_DefaultMonospace */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_KeyMan */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_KeyMan */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_UiName */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_CollationCount */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_Collation */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::putref_Collation */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::RemoveCollation */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_Abbr */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_Abbr */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_AbbrWsCount */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_AbbrWss */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_Description */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_Description */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_DescriptionWsCount */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_DescriptionWss */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_CollatingEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_CharPropEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::SetTracing */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::InterpretChrp */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_IcuLocale */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_IcuLocale */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::GetIcuLocaleParts */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_LegacyMapping */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_LegacyMapping */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_KeymanKbdName */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_KeymanKbdName */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_LanguageName */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_CountryName */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_VariantName */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_LanguageAbbr */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_CountryAbbr */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_VariantAbbr */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::SaveIfDirty */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::InstallLanguage */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_LastModified */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_LastModified */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::get_CurrentInputLanguage */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystem::put_CurrentInputLanguage */
};

const CInterfaceStubVtbl _ILgWritingSystemStubVtbl =
{
	&IID_ILgWritingSystem,
	&ILgWritingSystem_ServerInfo,
	75,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0301, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgInputMethodEditor, ver. 0.0,
   GUID={0x17aebfe0,0xc00a,0x11d2,{0x80,0x78,0x00,0x00,0xc0,0xfb,0x81,0xb5}} */

#pragma code_seg(".orpc")
static const unsigned short ILgInputMethodEditor_FormatStringOffsetTable[] =
	{
	7296,
	(unsigned short) -1,
	16674,
	16746,
	16818
	};

static const MIDL_STUBLESS_PROXY_INFO ILgInputMethodEditor_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgInputMethodEditor_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgInputMethodEditor_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgInputMethodEditor_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(8) _ILgInputMethodEditorProxyVtbl =
{
	&ILgInputMethodEditor_ProxyInfo,
	&IID_ILgInputMethodEditor,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgInputMethodEditor::Setup */ ,
	0 /* (void *) (INT_PTR) -1 /* ILgInputMethodEditor::Replace */ ,
	(void *) (INT_PTR) -1 /* ILgInputMethodEditor::Backspace */ ,
	(void *) (INT_PTR) -1 /* ILgInputMethodEditor::DeleteForward */ ,
	(void *) (INT_PTR) -1 /* ILgInputMethodEditor::IsValidInsertionPoint */
};

const CInterfaceStubVtbl _ILgInputMethodEditorStubVtbl =
{
	&IID_ILgInputMethodEditor,
	&ILgInputMethodEditor_ServerInfo,
	8,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0302, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgFontManager, ver. 0.0,
   GUID={0x10894680,0xF384,0x11d3,{0xB5,0xD1,0x00,0x40,0x05,0x43,0xA2,0x66}} */

#pragma code_seg(".orpc")
static const unsigned short ILgFontManager_FormatStringOffsetTable[] =
	{
	16866,
	16908,
	16956,
	1104
	};

static const MIDL_STUBLESS_PROXY_INFO ILgFontManager_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgFontManager_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgFontManager_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgFontManager_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _ILgFontManagerProxyVtbl =
{
	&ILgFontManager_ProxyInfo,
	&IID_ILgFontManager,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgFontManager::IsFontAvailable */ ,
	(void *) (INT_PTR) -1 /* ILgFontManager::IsFontAvailableRgch */ ,
	(void *) (INT_PTR) -1 /* ILgFontManager::AvailableFonts */ ,
	(void *) (INT_PTR) -1 /* ILgFontManager::RefreshFontList */
};

const CInterfaceStubVtbl _ILgFontManagerStubVtbl =
{
	&IID_ILgFontManager,
	&ILgFontManager_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0303, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgCollatingEngine, ver. 0.0,
   GUID={0xDB78D60B,0xE43E,0x4464,{0xB8,0xAE,0xC5,0xC9,0xA0,0x0E,0x2C,0x04}} */

#pragma code_seg(".orpc")
static const unsigned short ILgCollatingEngine_FormatStringOffsetTable[] =
	{
	16992,
	17040,
	17106,
	17160,
	17196,
	17232,
	17280,
	17334,
	17370
	};

static const MIDL_STUBLESS_PROXY_INFO ILgCollatingEngine_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgCollatingEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgCollatingEngine_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgCollatingEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _ILgCollatingEngineProxyVtbl =
{
	&ILgCollatingEngine_ProxyInfo,
	&IID_ILgCollatingEngine,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::get_SortKey */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::SortKeyRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::Compare */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::get_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::putref_WritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::get_SortKeyVariant */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::CompareVariant */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::Open */ ,
	(void *) (INT_PTR) -1 /* ILgCollatingEngine::Close */
};

const CInterfaceStubVtbl _ILgCollatingEngineStubVtbl =
{
	&IID_ILgCollatingEngine,
	&ILgCollatingEngine_ServerInfo,
	12,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0304, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgCharacterPropertyEngine, ver. 0.0,
   GUID={0x7C8B7F40,0x40C8,0x47f7,{0xB1,0x0B,0x45,0x37,0x24,0x15,0x77,0x8D}} */

#pragma code_seg(".orpc")
static const unsigned short ILgCharacterPropertyEngine_FormatStringOffsetTable[] =
	{
	17400,
	17442,
	846,
	17484,
	17526,
	17568,
	17610,
	17652,
	17694,
	17736,
	17778,
	17820,
	11664,
	17862,
	17904,
	17946,
	17988,
	18030,
	18072,
	18114,
	18156,
	8742,
	18198,
	18240,
	18282,
	18324,
	18384,
	18444,
	18504,
	18552,
	18594,
	18636,
	18678,
	18738,
	18780,
	18840,
	18882,
	18924,
	18966,
	19014,
	19062,
	19128,
	19170,
	19230,
	19272,
	19332,
	19368,
	19404,
	19452,
	19494,
	19542
	};

static const MIDL_STUBLESS_PROXY_INFO ILgCharacterPropertyEngine_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgCharacterPropertyEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgCharacterPropertyEngine_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgCharacterPropertyEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(54) _ILgCharacterPropertyEngineProxyVtbl =
{
	&ILgCharacterPropertyEngine_ProxyInfo,
	&IID_ILgCharacterPropertyEngine,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_GeneralCategory */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_BidiCategory */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsLetter */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsWordForming */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsPunctuation */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsNumber */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsSeparator */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsSymbol */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsMark */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsOther */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsUpper */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsLower */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsTitle */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsModifier */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsOtherLetter */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsOpen */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsClose */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsWordMedial */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsControl */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_ToLowerCh */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_ToUpperCh */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_ToTitleCh */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::ToLower */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::ToUpper */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::ToTitle */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::ToLowerRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::ToUpperRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::ToTitleRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_IsUserDefinedClass */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_SoundAlikeKey */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_CharacterName */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_Decomposition */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::DecompositionRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_FullDecomp */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::FullDecompRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_NumericValue */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_CombiningClass */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_Comment */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::GetLineBreakProps */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::GetLineBreakStatus */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::GetLineBreakInfo */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::StripDiacritics */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::StripDiacriticsRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::NormalizeKd */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::NormalizeKdRgch */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::get_Locale */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::put_Locale */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::GetLineBreakText */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::put_LineBreakText */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::LineBreakBefore */ ,
	(void *) (INT_PTR) -1 /* ILgCharacterPropertyEngine::LineBreakAfter */
};

const CInterfaceStubVtbl _ILgCharacterPropertyEngineStubVtbl =
{
	&IID_ILgCharacterPropertyEngine,
	&ILgCharacterPropertyEngine_ServerInfo,
	54,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0305, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgSearchEngine, ver. 0.0,
   GUID={0x0D224001,0x03C7,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short ILgSearchEngine_FormatStringOffsetTable[] =
	{
	19590,
	1998,
	19650,
	19704,
	19764
	};

static const MIDL_STUBLESS_PROXY_INFO ILgSearchEngine_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgSearchEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgSearchEngine_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgSearchEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(8) _ILgSearchEngineProxyVtbl =
{
	&ILgSearchEngine_ProxyInfo,
	&IID_ILgSearchEngine,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgSearchEngine::SetPattern */ ,
	(void *) (INT_PTR) -1 /* ILgSearchEngine::SetReplacePattern */ ,
	(void *) (INT_PTR) -1 /* ILgSearchEngine::ShowPatternDialog */ ,
	(void *) (INT_PTR) -1 /* ILgSearchEngine::FindString */ ,
	(void *) (INT_PTR) -1 /* ILgSearchEngine::FindReplace */
};

const CInterfaceStubVtbl _ILgSearchEngineStubVtbl =
{
	&IID_ILgSearchEngine,
	&ILgSearchEngine_ServerInfo,
	8,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0306, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgStringConverter, ver. 0.0,
   GUID={0x0D224002,0x03C7,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short ILgStringConverter_FormatStringOffsetTable[] =
	{
	19824,
	19866
	};

static const MIDL_STUBLESS_PROXY_INFO ILgStringConverter_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgStringConverter_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgStringConverter_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgStringConverter_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _ILgStringConverterProxyVtbl =
{
	&ILgStringConverter_ProxyInfo,
	&IID_ILgStringConverter,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgStringConverter::ConvertString */ ,
	(void *) (INT_PTR) -1 /* ILgStringConverter::ConvertStringRgch */
};

const CInterfaceStubVtbl _ILgStringConverterStubVtbl =
{
	&IID_ILgStringConverter,
	&ILgStringConverter_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0307, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgTokenizer, ver. 0.0,
   GUID={0x0D224003,0x03C7,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short ILgTokenizer_FormatStringOffsetTable[] =
	{
	19926,
	19980,
	20028
	};

static const MIDL_STUBLESS_PROXY_INFO ILgTokenizer_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgTokenizer_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgTokenizer_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgTokenizer_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(6) _ILgTokenizerProxyVtbl =
{
	&ILgTokenizer_ProxyInfo,
	&IID_ILgTokenizer,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgTokenizer::GetToken */ ,
	(void *) (INT_PTR) -1 /* ILgTokenizer::get_TokenStart */ ,
	(void *) (INT_PTR) -1 /* ILgTokenizer::get_TokenEnd */
};

const CInterfaceStubVtbl _ILgTokenizerStubVtbl =
{
	&IID_ILgTokenizer,
	&ILgTokenizer_ServerInfo,
	6,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0308, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgSpellChecker, ver. 0.0,
   GUID={0x0D224006,0x03C7,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short ILgSpellChecker_FormatStringOffsetTable[] =
	{
	20076,
	2778,
	20112,
	20184,
	20238,
	20274,
	20322,
	6066,
	20358
	};

static const MIDL_STUBLESS_PROXY_INFO ILgSpellChecker_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgSpellChecker_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgSpellChecker_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgSpellChecker_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _ILgSpellCheckerProxyVtbl =
{
	&ILgSpellChecker_ProxyInfo,
	&IID_ILgSpellChecker,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::Init */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::SetOptions */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::Check */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::Suggest */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::IgnoreAll */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::Change */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::AddToUser */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::FlushIgnoreList */ ,
	(void *) (INT_PTR) -1 /* ILgSpellChecker::FlushChangeList */
};

const CInterfaceStubVtbl _ILgSpellCheckerStubVtbl =
{
	&IID_ILgSpellChecker,
	&ILgSpellChecker_ServerInfo,
	12,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0309, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgSpellCheckFactory, ver. 0.0,
   GUID={0xFC1C0D01,0x0483,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short ILgSpellCheckFactory_FormatStringOffsetTable[] =
	{
	20394
	};

static const MIDL_STUBLESS_PROXY_INFO ILgSpellCheckFactory_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgSpellCheckFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgSpellCheckFactory_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgSpellCheckFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _ILgSpellCheckFactoryProxyVtbl =
{
	&ILgSpellCheckFactory_ProxyInfo,
	&IID_ILgSpellCheckFactory,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgSpellCheckFactory::get_Checker */
};

const CInterfaceStubVtbl _ILgSpellCheckFactoryStubVtbl =
{
	&IID_ILgSpellCheckFactory,
	&ILgSpellCheckFactory_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0310, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgNumericEngine, ver. 0.0,
   GUID={0xFC1C0D04,0x0483,0x11d3,{0x80,0x78,0x00,0x00,0xC0,0xFB,0x81,0xB5}} */

#pragma code_seg(".orpc")
static const unsigned short ILgNumericEngine_FormatStringOffsetTable[] =
	{
	13680,
	20430,
	20472,
	20514,
	20568,
	20616,
	20664,
	20712,
	20754
	};

static const MIDL_STUBLESS_PROXY_INFO ILgNumericEngine_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgNumericEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgNumericEngine_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgNumericEngine_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _ILgNumericEngineProxyVtbl =
{
	&ILgNumericEngine_ProxyInfo,
	&IID_ILgNumericEngine,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::get_IntToString */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::get_IntToPrettyString */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::get_StringToInt */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::StringToIntRgch */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::get_DblToString */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::get_DblToPrettyString */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::get_DblToExpString */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::get_StringToDbl */ ,
	(void *) (INT_PTR) -1 /* ILgNumericEngine::StringToDblRgch */
};

const CInterfaceStubVtbl _ILgNumericEngineStubVtbl =
{
	&IID_ILgNumericEngine,
	&ILgNumericEngine_ServerInfo,
	12,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0311, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgWritingSystemFactory, ver. 0.0,
   GUID={0x2C4636E3,0x4F49,0x4966,{0x96,0x6F,0x09,0x53,0xF9,0x7F,0x51,0xC8}} */

#pragma code_seg(".orpc")
static const unsigned short ILgWritingSystemFactory_FormatStringOffsetTable[] =
	{
	20808,
	20850,
	20892,
	2070,
	20928,
	3930,
	20970,
	21006,
	21048,
	21084,
	21126,
	21168,
	21216,
	14478,
	21258,
	1524,
	21288,
	1584,
	21324,
	21360
	};

static const MIDL_STUBLESS_PROXY_INFO ILgWritingSystemFactory_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgWritingSystemFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgWritingSystemFactory_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgWritingSystemFactory_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(23) _ILgWritingSystemFactoryProxyVtbl =
{
	&ILgWritingSystemFactory_ProxyInfo,
	&IID_ILgWritingSystemFactory,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_Engine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_EngineOrNull */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::AddEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::RemoveEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::GetWsFromStr */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::GetStrFromWs */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_NumberOfWs */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::GetWritingSystems */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_UnicodeCharProps */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_DefaultCollater */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_CharPropEngine */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_Renderer */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_RendererFromChrp */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::Shutdown */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::Clear */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::SaveWritingSystems */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::Serialize */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_UserWs */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::get_BypassInstall */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactory::put_BypassInstall */
};

const CInterfaceStubVtbl _ILgWritingSystemFactoryStubVtbl =
{
	&IID_ILgWritingSystemFactory,
	&ILgWritingSystemFactory_ServerInfo,
	23,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0312, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgWritingSystemFactoryBuilder, ver. 0.0,
   GUID={0x8AD52AF0,0x13A8,0x4d28,{0xA1,0xEE,0x71,0x92,0x4B,0x36,0x98,0x9F}} */

#pragma code_seg(".orpc")
static const unsigned short ILgWritingSystemFactoryBuilder_FormatStringOffsetTable[] =
	{
	21396,
	21444,
	21498,
	1104
	};

static const MIDL_STUBLESS_PROXY_INFO ILgWritingSystemFactoryBuilder_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgWritingSystemFactoryBuilder_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgWritingSystemFactoryBuilder_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgWritingSystemFactoryBuilder_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(7) _ILgWritingSystemFactoryBuilderProxyVtbl =
{
	&ILgWritingSystemFactoryBuilder_ProxyInfo,
	&IID_ILgWritingSystemFactoryBuilder,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactoryBuilder::GetWritingSystemFactory */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactoryBuilder::GetWritingSystemFactoryNew */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactoryBuilder::Deserialize */ ,
	(void *) (INT_PTR) -1 /* ILgWritingSystemFactoryBuilder::ShutdownAllFactories */
};

const CInterfaceStubVtbl _ILgWritingSystemFactoryBuilderStubVtbl =
{
	&IID_ILgWritingSystemFactoryBuilder,
	&ILgWritingSystemFactoryBuilder_ServerInfo,
	7,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0313, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgTsStringPlusWss, ver. 0.0,
   GUID={0x71C8D1ED,0x49B0,0x40ef,{0x84,0x23,0x92,0xB0,0xA5,0xF0,0x4B,0x89}} */

#pragma code_seg(".orpc")
static const unsigned short ILgTsStringPlusWss_FormatStringOffsetTable[] =
	{
	21540,
	21582,
	16956,
	21624,
	21660
	};

static const MIDL_STUBLESS_PROXY_INFO ILgTsStringPlusWss_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgTsStringPlusWss_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgTsStringPlusWss_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgTsStringPlusWss_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(8) _ILgTsStringPlusWssProxyVtbl =
{
	&ILgTsStringPlusWss_ProxyInfo,
	&IID_ILgTsStringPlusWss,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgTsStringPlusWss::get_String */ ,
	(void *) (INT_PTR) -1 /* ILgTsStringPlusWss::putref_String */ ,
	(void *) (INT_PTR) -1 /* ILgTsStringPlusWss::get_Text */ ,
	(void *) (INT_PTR) -1 /* ILgTsStringPlusWss::Serialize */ ,
	(void *) (INT_PTR) -1 /* ILgTsStringPlusWss::Deserialize */
};

const CInterfaceStubVtbl _ILgTsStringPlusWssStubVtbl =
{
	&IID_ILgTsStringPlusWss,
	&ILgTsStringPlusWss_ServerInfo,
	8,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0314, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgTsDataObject, ver. 0.0,
   GUID={0x56CD4356,0xC349,0x4927,{0x9E,0x3D,0xCC,0x0C,0xF0,0xEF,0xF0,0x4E}} */

#pragma code_seg(".orpc")
static const unsigned short ILgTsDataObject_FormatStringOffsetTable[] =
	{
	21696,
	2346
	};

static const MIDL_STUBLESS_PROXY_INFO ILgTsDataObject_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgTsDataObject_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgTsDataObject_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgTsDataObject_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _ILgTsDataObjectProxyVtbl =
{
	&ILgTsDataObject_ProxyInfo,
	&IID_ILgTsDataObject,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgTsDataObject::Init */ ,
	(void *) (INT_PTR) -1 /* ILgTsDataObject::GetClipboardType */
};

const CInterfaceStubVtbl _ILgTsDataObjectStubVtbl =
{
	&IID_ILgTsDataObject,
	&ILgTsDataObject_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0315, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgKeymanHandler, ver. 0.0,
   GUID={0xD43F4C58,0x5E24,0x4b54,{0x8E,0x4D,0xF0,0x23,0x3B,0x82,0x36,0x78}} */

#pragma code_seg(".orpc")
static const unsigned short ILgKeymanHandler_FormatStringOffsetTable[] =
	{
	2226,
	2346,
	21732,
	21774,
	21810,
	13764
	};

static const MIDL_STUBLESS_PROXY_INFO ILgKeymanHandler_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgKeymanHandler_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgKeymanHandler_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgKeymanHandler_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(9) _ILgKeymanHandlerProxyVtbl =
{
	&ILgKeymanHandler_ProxyInfo,
	&IID_ILgKeymanHandler,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgKeymanHandler::Init */ ,
	(void *) (INT_PTR) -1 /* ILgKeymanHandler::get_NLayout */ ,
	(void *) (INT_PTR) -1 /* ILgKeymanHandler::get_Name */ ,
	(void *) (INT_PTR) -1 /* ILgKeymanHandler::get_ActiveKeyboardName */ ,
	(void *) (INT_PTR) -1 /* ILgKeymanHandler::put_ActiveKeyboardName */ ,
	(void *) (INT_PTR) -1 /* ILgKeymanHandler::get_KeymanWindowsMessage */
};

const CInterfaceStubVtbl _ILgKeymanHandlerStubVtbl =
{
	&IID_ILgKeymanHandler,
	&ILgKeymanHandler_ServerInfo,
	9,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0316, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgTextServices, ver. 0.0,
   GUID={0x03D86B2C,0x9FB3,0x4E33,{0x9B,0x23,0x6C,0x8B,0xFC,0x18,0xFB,0x1E}} */

#pragma code_seg(".orpc")
static const unsigned short ILgTextServices_FormatStringOffsetTable[] =
	{
	21846
	};

static const MIDL_STUBLESS_PROXY_INFO ILgTextServices_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgTextServices_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgTextServices_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgTextServices_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(4) _ILgTextServicesProxyVtbl =
{
	&ILgTextServices_ProxyInfo,
	&IID_ILgTextServices,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgTextServices::SetKeyboard */
};

const CInterfaceStubVtbl _ILgTextServicesStubVtbl =
{
	&IID_ILgTextServices,
	&ILgTextServices_ServerInfo,
	4,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0317, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgCodePageEnumerator, ver. 0.0,
   GUID={0x62811E4D,0x5572,0x4f76,{0xB7,0x1F,0x9F,0x17,0x23,0x83,0x38,0xE1}} */

#pragma code_seg(".orpc")
static const unsigned short ILgCodePageEnumerator_FormatStringOffsetTable[] =
	{
	7296,
	21906
	};

static const MIDL_STUBLESS_PROXY_INFO ILgCodePageEnumerator_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgCodePageEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgCodePageEnumerator_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgCodePageEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _ILgCodePageEnumeratorProxyVtbl =
{
	&ILgCodePageEnumerator_ProxyInfo,
	&IID_ILgCodePageEnumerator,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgCodePageEnumerator::Init */ ,
	(void *) (INT_PTR) -1 /* ILgCodePageEnumerator::Next */
};

const CInterfaceStubVtbl _ILgCodePageEnumeratorStubVtbl =
{
	&IID_ILgCodePageEnumerator,
	&ILgCodePageEnumerator_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0318, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgLanguageEnumerator, ver. 0.0,
   GUID={0x76470164,0xE990,0x411d,{0xAF,0x66,0x42,0xA7,0x19,0x2E,0x4C,0x49}} */

#pragma code_seg(".orpc")
static const unsigned short ILgLanguageEnumerator_FormatStringOffsetTable[] =
	{
	7296,
	21906
	};

static const MIDL_STUBLESS_PROXY_INFO ILgLanguageEnumerator_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgLanguageEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgLanguageEnumerator_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgLanguageEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(5) _ILgLanguageEnumeratorProxyVtbl =
{
	&ILgLanguageEnumerator_ProxyInfo,
	&IID_ILgLanguageEnumerator,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgLanguageEnumerator::Init */ ,
	(void *) (INT_PTR) -1 /* ILgLanguageEnumerator::Next */
};

const CInterfaceStubVtbl _ILgLanguageEnumeratorStubVtbl =
{
	&IID_ILgLanguageEnumerator,
	&ILgLanguageEnumerator_ServerInfo,
	5,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0319, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgIcuConverterEnumerator, ver. 0.0,
   GUID={0x34D4E39C,0xC3B6,0x413e,{0x9A,0x4E,0x44,0x57,0xBB,0xB0,0x2F,0xE8}} */

#pragma code_seg(".orpc")
static const unsigned short ILgIcuConverterEnumerator_FormatStringOffsetTable[] =
	{
	2742,
	20430,
	21732
	};

static const MIDL_STUBLESS_PROXY_INFO ILgIcuConverterEnumerator_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgIcuConverterEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgIcuConverterEnumerator_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgIcuConverterEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(6) _ILgIcuConverterEnumeratorProxyVtbl =
{
	&ILgIcuConverterEnumerator_ProxyInfo,
	&IID_ILgIcuConverterEnumerator,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgIcuConverterEnumerator::get_Count */ ,
	(void *) (INT_PTR) -1 /* ILgIcuConverterEnumerator::get_ConverterName */ ,
	(void *) (INT_PTR) -1 /* ILgIcuConverterEnumerator::get_ConverterId */
};

const CInterfaceStubVtbl _ILgIcuConverterEnumeratorStubVtbl =
{
	&IID_ILgIcuConverterEnumerator,
	&ILgIcuConverterEnumerator_ServerInfo,
	6,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0320, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgIcuTransliteratorEnumerator, ver. 0.0,
   GUID={0xB26A6461,0x582C,0x4873,{0xB3,0xF5,0x67,0x31,0x04,0xD1,0xAC,0x37}} */

#pragma code_seg(".orpc")
static const unsigned short ILgIcuTransliteratorEnumerator_FormatStringOffsetTable[] =
	{
	2742,
	20430,
	21732
	};

static const MIDL_STUBLESS_PROXY_INFO ILgIcuTransliteratorEnumerator_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgIcuTransliteratorEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgIcuTransliteratorEnumerator_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgIcuTransliteratorEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(6) _ILgIcuTransliteratorEnumeratorProxyVtbl =
{
	&ILgIcuTransliteratorEnumerator_ProxyInfo,
	&IID_ILgIcuTransliteratorEnumerator,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgIcuTransliteratorEnumerator::get_Count */ ,
	(void *) (INT_PTR) -1 /* ILgIcuTransliteratorEnumerator::get_TransliteratorName */ ,
	(void *) (INT_PTR) -1 /* ILgIcuTransliteratorEnumerator::get_TransliteratorId */
};

const CInterfaceStubVtbl _ILgIcuTransliteratorEnumeratorStubVtbl =
{
	&IID_ILgIcuTransliteratorEnumerator,
	&ILgIcuTransliteratorEnumerator_ServerInfo,
	6,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0321, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgIcuLocaleEnumerator, ver. 0.0,
   GUID={0x00C88119,0xF57D,0x4e7b,{0xA0,0x3B,0xED,0xB0,0xBC,0x3B,0x57,0xEE}} */

#pragma code_seg(".orpc")
static const unsigned short ILgIcuLocaleEnumerator_FormatStringOffsetTable[] =
	{
	2742,
	20430,
	21732,
	14172,
	21948,
	21990
	};

static const MIDL_STUBLESS_PROXY_INFO ILgIcuLocaleEnumerator_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgIcuLocaleEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgIcuLocaleEnumerator_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgIcuLocaleEnumerator_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(9) _ILgIcuLocaleEnumeratorProxyVtbl =
{
	&ILgIcuLocaleEnumerator_ProxyInfo,
	&IID_ILgIcuLocaleEnumerator,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgIcuLocaleEnumerator::get_Count */ ,
	(void *) (INT_PTR) -1 /* ILgIcuLocaleEnumerator::get_Name */ ,
	(void *) (INT_PTR) -1 /* ILgIcuLocaleEnumerator::get_Language */ ,
	(void *) (INT_PTR) -1 /* ILgIcuLocaleEnumerator::get_Country */ ,
	(void *) (INT_PTR) -1 /* ILgIcuLocaleEnumerator::get_Variant */ ,
	(void *) (INT_PTR) -1 /* ILgIcuLocaleEnumerator::get_DisplayName */
};

const CInterfaceStubVtbl _ILgIcuLocaleEnumeratorStubVtbl =
{
	&IID_ILgIcuLocaleEnumerator,
	&ILgIcuLocaleEnumerator_ServerInfo,
	9,
	0, /* pure interpreted */
	CStdStubBuffer_METHODS
};


/* Standard interface: __MIDL_itf_FwKernelPs_0322, ver. 0.0,
   GUID={0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}} */


/* Object interface: ILgIcuResourceBundle, ver. 0.0,
   GUID={0x4518189C,0xE545,0x48b4,{0x86,0x53,0xD8,0x29,0xD1,0xEC,0xB7,0x78}} */

#pragma code_seg(".orpc")
static const unsigned short ILgIcuResourceBundle_FormatStringOffsetTable[] =
	{
	0,
	6558,
	16956,
	21774,
	22038,
	22080,
	22116,
	2526,
	1296
	};

static const MIDL_STUBLESS_PROXY_INFO ILgIcuResourceBundle_ProxyInfo =
	{
	&Object_StubDesc,
	__MIDL_ProcFormatString.Format,
	&ILgIcuResourceBundle_FormatStringOffsetTable[-3],
	0,
	0,
	0
	};


static const MIDL_SERVER_INFO ILgIcuResourceBundle_ServerInfo =
	{
	&Object_StubDesc,
	0,
	__MIDL_ProcFormatString.Format,
	&ILgIcuResourceBundle_FormatStringOffsetTable[-3],
	0,
	0,
	0,
	0};
CINTERFACE_PROXY_VTABLE(12) _ILgIcuResourceBundleProxyVtbl =
{
	&ILgIcuResourceBundle_ProxyInfo,
	&IID_ILgIcuResourceBundle,
	IUnknown_QueryInterface_Proxy,
	IUnknown_AddRef_Proxy,
	IUnknown_Release_Proxy ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::Init */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_Key */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_String */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_Name */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_GetSubsection */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_HasNext */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_Next */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_Size */ ,
	(void *) (INT_PTR) -1 /* ILgIcuResourceBundle::get_StringEx */
};

const CInterfaceStubVtbl _ILgIcuResourceBundleStubVtbl =
{
	&IID_ILgIcuResourceBundle,
	&ILgIcuResourceBundle_ServerInfo,
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
	ExprEvalRoutines,
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

const CInterfaceProxyVtbl * _FwKernelPs_ProxyVtblList[] =
{
	( CInterfaceProxyVtbl *) &_ILgSpellCheckFactoryProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgSearchEngineProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgStringConverterProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgTokenizerProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgNumericEngineProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgSpellCheckerProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgCollatingEngineProxyVtbl,
	( CInterfaceProxyVtbl *) &_IActionHandlerProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDisconnectDbProxyVtbl,
	( CInterfaceProxyVtbl *) &_ISimpleInitProxyVtbl,
	( CInterfaceProxyVtbl *) &_IOleDbEncapProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgIcuLocaleEnumeratorProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsMultiStringProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDbWarnSetupProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgTextServicesProxyVtbl,
	( CInterfaceProxyVtbl *) &_IFwMetaDataCacheProxyVtbl,
	( CInterfaceProxyVtbl *) &_IRenderEngineProxyVtbl,
	( CInterfaceProxyVtbl *) &_IFwCustomExportProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgCharacterPropertyEngineProxyVtbl,
	( CInterfaceProxyVtbl *) &_IAdvIndProxyVtbl,
	( CInterfaceProxyVtbl *) &_IFwToolProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgCodePageEnumeratorProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgTsDataObjectProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgKeymanHandlerProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwJustifierProxyVtbl,
	( CInterfaceProxyVtbl *) &_IOleDbCommandProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgIcuTransliteratorEnumeratorProxyVtbl,
	( CInterfaceProxyVtbl *) &_IAdvInd3ProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgLanguageEnumeratorProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDebugReportProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsStringProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsStreamWrapperProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgFontManagerProxyVtbl,
	( CInterfaceProxyVtbl *) &_DIFwBackupDbProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDbAdminProxyVtbl,
	( CInterfaceProxyVtbl *) &_IRenderingFeaturesProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsTextPropsProxyVtbl,
	( CInterfaceProxyVtbl *) &_IFwFldSpecProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgIcuResourceBundleProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgIcuConverterEnumeratorProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwGraphicsProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwGraphicsWin32ProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsPropsFactoryProxyVtbl,
	( CInterfaceProxyVtbl *) &_IRemoteDbWarnProxyVtbl,
	( CInterfaceProxyVtbl *) &_IBackupDelegatesProxyVtbl,
	( CInterfaceProxyVtbl *) &_IDebugReportSinkProxyVtbl,
	( CInterfaceProxyVtbl *) &_IUndoActionProxyVtbl,
	( CInterfaceProxyVtbl *) &_IUndoGrouperProxyVtbl,
	( CInterfaceProxyVtbl *) &_IAdvInd2ProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgWritingSystemProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgInputMethodEditorProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgWritingSystemFactoryProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgCollationProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsStrFactoryProxyVtbl,
	( CInterfaceProxyVtbl *) &_IVwTextSourceProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsStrBldrProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsIncStrBldrProxyVtbl,
	( CInterfaceProxyVtbl *) &_ITsPropsBldrProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgTsStringPlusWssProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgWritingSystemFactoryBuilderProxyVtbl,
	( CInterfaceProxyVtbl *) &_IJustifyingRendererProxyVtbl,
	( CInterfaceProxyVtbl *) &_IHelpTopicProviderProxyVtbl,
	( CInterfaceProxyVtbl *) &_ILgSegmentProxyVtbl,
	0
};

const CInterfaceStubVtbl * _FwKernelPs_StubVtblList[] =
{
	( CInterfaceStubVtbl *) &_ILgSpellCheckFactoryStubVtbl,
	( CInterfaceStubVtbl *) &_ILgSearchEngineStubVtbl,
	( CInterfaceStubVtbl *) &_ILgStringConverterStubVtbl,
	( CInterfaceStubVtbl *) &_ILgTokenizerStubVtbl,
	( CInterfaceStubVtbl *) &_ILgNumericEngineStubVtbl,
	( CInterfaceStubVtbl *) &_ILgSpellCheckerStubVtbl,
	( CInterfaceStubVtbl *) &_ILgCollatingEngineStubVtbl,
	( CInterfaceStubVtbl *) &_IActionHandlerStubVtbl,
	( CInterfaceStubVtbl *) &_IDisconnectDbStubVtbl,
	( CInterfaceStubVtbl *) &_ISimpleInitStubVtbl,
	( CInterfaceStubVtbl *) &_IOleDbEncapStubVtbl,
	( CInterfaceStubVtbl *) &_ILgIcuLocaleEnumeratorStubVtbl,
	( CInterfaceStubVtbl *) &_ITsMultiStringStubVtbl,
	( CInterfaceStubVtbl *) &_IDbWarnSetupStubVtbl,
	( CInterfaceStubVtbl *) &_ILgTextServicesStubVtbl,
	( CInterfaceStubVtbl *) &_IFwMetaDataCacheStubVtbl,
	( CInterfaceStubVtbl *) &_IRenderEngineStubVtbl,
	( CInterfaceStubVtbl *) &_IFwCustomExportStubVtbl,
	( CInterfaceStubVtbl *) &_ILgCharacterPropertyEngineStubVtbl,
	( CInterfaceStubVtbl *) &_IAdvIndStubVtbl,
	( CInterfaceStubVtbl *) &_IFwToolStubVtbl,
	( CInterfaceStubVtbl *) &_ILgCodePageEnumeratorStubVtbl,
	( CInterfaceStubVtbl *) &_ILgTsDataObjectStubVtbl,
	( CInterfaceStubVtbl *) &_ILgKeymanHandlerStubVtbl,
	( CInterfaceStubVtbl *) &_IVwJustifierStubVtbl,
	( CInterfaceStubVtbl *) &_IOleDbCommandStubVtbl,
	( CInterfaceStubVtbl *) &_ILgIcuTransliteratorEnumeratorStubVtbl,
	( CInterfaceStubVtbl *) &_IAdvInd3StubVtbl,
	( CInterfaceStubVtbl *) &_ILgLanguageEnumeratorStubVtbl,
	( CInterfaceStubVtbl *) &_IDebugReportStubVtbl,
	( CInterfaceStubVtbl *) &_ITsStringStubVtbl,
	( CInterfaceStubVtbl *) &_ITsStreamWrapperStubVtbl,
	( CInterfaceStubVtbl *) &_ILgFontManagerStubVtbl,
	( CInterfaceStubVtbl *) &_DIFwBackupDbStubVtbl,
	( CInterfaceStubVtbl *) &_IDbAdminStubVtbl,
	( CInterfaceStubVtbl *) &_IRenderingFeaturesStubVtbl,
	( CInterfaceStubVtbl *) &_ITsTextPropsStubVtbl,
	( CInterfaceStubVtbl *) &_IFwFldSpecStubVtbl,
	( CInterfaceStubVtbl *) &_ILgIcuResourceBundleStubVtbl,
	( CInterfaceStubVtbl *) &_ILgIcuConverterEnumeratorStubVtbl,
	( CInterfaceStubVtbl *) &_IVwGraphicsStubVtbl,
	( CInterfaceStubVtbl *) &_IVwGraphicsWin32StubVtbl,
	( CInterfaceStubVtbl *) &_ITsPropsFactoryStubVtbl,
	( CInterfaceStubVtbl *) &_IRemoteDbWarnStubVtbl,
	( CInterfaceStubVtbl *) &_IBackupDelegatesStubVtbl,
	( CInterfaceStubVtbl *) &_IDebugReportSinkStubVtbl,
	( CInterfaceStubVtbl *) &_IUndoActionStubVtbl,
	( CInterfaceStubVtbl *) &_IUndoGrouperStubVtbl,
	( CInterfaceStubVtbl *) &_IAdvInd2StubVtbl,
	( CInterfaceStubVtbl *) &_ILgWritingSystemStubVtbl,
	( CInterfaceStubVtbl *) &_ILgInputMethodEditorStubVtbl,
	( CInterfaceStubVtbl *) &_ILgWritingSystemFactoryStubVtbl,
	( CInterfaceStubVtbl *) &_ILgCollationStubVtbl,
	( CInterfaceStubVtbl *) &_ITsStrFactoryStubVtbl,
	( CInterfaceStubVtbl *) &_IVwTextSourceStubVtbl,
	( CInterfaceStubVtbl *) &_ITsStrBldrStubVtbl,
	( CInterfaceStubVtbl *) &_ITsIncStrBldrStubVtbl,
	( CInterfaceStubVtbl *) &_ITsPropsBldrStubVtbl,
	( CInterfaceStubVtbl *) &_ILgTsStringPlusWssStubVtbl,
	( CInterfaceStubVtbl *) &_ILgWritingSystemFactoryBuilderStubVtbl,
	( CInterfaceStubVtbl *) &_IJustifyingRendererStubVtbl,
	( CInterfaceStubVtbl *) &_IHelpTopicProviderStubVtbl,
	( CInterfaceStubVtbl *) &_ILgSegmentStubVtbl,
	0
};

PCInterfaceName const _FwKernelPs_InterfaceNamesList[] =
{
	"ILgSpellCheckFactory",
	"ILgSearchEngine",
	"ILgStringConverter",
	"ILgTokenizer",
	"ILgNumericEngine",
	"ILgSpellChecker",
	"ILgCollatingEngine",
	"IActionHandler",
	"IDisconnectDb",
	"ISimpleInit",
	"IOleDbEncap",
	"ILgIcuLocaleEnumerator",
	"ITsMultiString",
	"IDbWarnSetup",
	"ILgTextServices",
	"IFwMetaDataCache",
	"IRenderEngine",
	"IFwCustomExport",
	"ILgCharacterPropertyEngine",
	"IAdvInd",
	"IFwTool",
	"ILgCodePageEnumerator",
	"ILgTsDataObject",
	"ILgKeymanHandler",
	"IVwJustifier",
	"IOleDbCommand",
	"ILgIcuTransliteratorEnumerator",
	"IAdvInd3",
	"ILgLanguageEnumerator",
	"IDebugReport",
	"ITsString",
	"ITsStreamWrapper",
	"ILgFontManager",
	"DIFwBackupDb",
	"IDbAdmin",
	"IRenderingFeatures",
	"ITsTextProps",
	"IFwFldSpec",
	"ILgIcuResourceBundle",
	"ILgIcuConverterEnumerator",
	"IVwGraphics",
	"IVwGraphicsWin32",
	"ITsPropsFactory",
	"IRemoteDbWarn",
	"IBackupDelegates",
	"IDebugReportSink",
	"IUndoAction",
	"IUndoGrouper",
	"IAdvInd2",
	"ILgWritingSystem",
	"ILgInputMethodEditor",
	"ILgWritingSystemFactory",
	"ILgCollation",
	"ITsStrFactory",
	"IVwTextSource",
	"ITsStrBldr",
	"ITsIncStrBldr",
	"ITsPropsBldr",
	"ILgTsStringPlusWss",
	"ILgWritingSystemFactoryBuilder",
	"IJustifyingRenderer",
	"IHelpTopicProvider",
	"ILgSegment",
	0
};

const IID *  _FwKernelPs_BaseIIDList[] =
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
	0,
	0,
	0
};


#define _FwKernelPs_CHECK_IID(n)	IID_GENERIC_CHECK_IID( _FwKernelPs, pIID, n)

int __stdcall _FwKernelPs_IID_Lookup( const IID * pIID, int * pIndex )
{
	IID_BS_LOOKUP_SETUP

	IID_BS_LOOKUP_INITIAL_TEST( _FwKernelPs, 63, 32 )
	IID_BS_LOOKUP_NEXT_TEST( _FwKernelPs, 16 )
	IID_BS_LOOKUP_NEXT_TEST( _FwKernelPs, 8 )
	IID_BS_LOOKUP_NEXT_TEST( _FwKernelPs, 4 )
	IID_BS_LOOKUP_NEXT_TEST( _FwKernelPs, 2 )
	IID_BS_LOOKUP_NEXT_TEST( _FwKernelPs, 1 )
	IID_BS_LOOKUP_RETURN_RESULT( _FwKernelPs, 63, *pIndex )

}

const ExtendedProxyFileInfo FwKernelPs_ProxyFileInfo =
{
	(PCInterfaceProxyVtblList *) & _FwKernelPs_ProxyVtblList,
	(PCInterfaceStubVtblList *) & _FwKernelPs_StubVtblList,
	(const PCInterfaceName * ) & _FwKernelPs_InterfaceNamesList,
	(const IID ** ) & _FwKernelPs_BaseIIDList,
	& _FwKernelPs_IID_Lookup,
	63,
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
