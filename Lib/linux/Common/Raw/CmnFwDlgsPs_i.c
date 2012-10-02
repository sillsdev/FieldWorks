

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


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

#if !defined(_M_IA64) && !defined(_M_AMD64)


#pragma warning( disable: 4049 )  /* more than 64k source lines */


#ifdef __cplusplus
extern "C"{
#endif


#include <rpc.h>
#include <rpcndr.h>

#ifdef _MIDL_USE_GUIDDEF_

#ifndef INITGUID
#define INITGUID
#include <guiddef.h>
#undef INITGUID
#else
#include <guiddef.h>
#endif

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
		DEFINE_GUID(name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8)

#else // !_MIDL_USE_GUIDDEF_

#ifndef __IID_DEFINED__
#define __IID_DEFINED__

typedef struct _IID
{
	unsigned long x;
	unsigned short s1;
	unsigned short s2;
	unsigned char  c[8];
} IID;

#endif // __IID_DEFINED__

#ifndef CLSID_DEFINED
#define CLSID_DEFINED
typedef IID CLSID;
#endif // CLSID_DEFINED

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
		const type name = {l,w1,w2,{b1,b2,b3,b4,b5,b6,b7,b8}}

#endif !_MIDL_USE_GUIDDEF_

MIDL_DEFINE_GUID(IID, IID_IOpenFWProjectDlg,0x8cb6f2f9,0x3b0a,0x4030,0x89,0x92,0xc5,0x0f,0xb7,0x8e,0x77,0xf3);


MIDL_DEFINE_GUID(IID, IID_IFwExportDlg,0x67A68372,0x5727,0x4bd4,0x94,0xA7,0xC2,0xD7,0x03,0xA7,0x5C,0x36);


MIDL_DEFINE_GUID(IID, IID_IFwStylesDlg,0x0D598D88,0xC17D,0x4E46,0xAC,0x89,0x51,0xFF,0xC5,0xDA,0x07,0x99);


MIDL_DEFINE_GUID(IID, IID_IFwDbMergeStyles,0xA7CD703C,0x6199,0x4097,0xA5,0xC0,0xAB,0x78,0xDD,0x23,0x12,0x0E);


MIDL_DEFINE_GUID(IID, IID_IFwDbMergeWrtSys,0xDE96B989,0x91A5,0x4104,0x97,0x64,0x69,0xAB,0xE0,0xBF,0x0B,0x9A);


MIDL_DEFINE_GUID(IID, IID_IFwCheckAnthroList,0x8AC06CED,0x7B73,0x4E34,0x81,0xA3,0x85,0x2A,0x43,0xE2,0x8B,0xD8);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



#endif /* !defined(_M_IA64) && !defined(_M_AMD64)*/
