

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:12:41 2006
 */
/* Compiler settings for C:\fw\Output\Common\CmnFwDlgsTlb.idl:
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

MIDL_DEFINE_GUID(IID, LIBID_CmnFwDlgs,0xE6F6FD84,0x2E27,0x407e,0xAD,0xBC,0xB8,0x96,0x12,0x03,0x4F,0x7C);


MIDL_DEFINE_GUID(IID, IID_IOpenFWProjectDlg,0x8cb6f2f9,0x3b0a,0x4030,0x89,0x92,0xc5,0x0f,0xb7,0x8e,0x77,0xf3);


MIDL_DEFINE_GUID(CLSID, CLSID_OpenFWProjectDlg,0xD7C505D0,0xF132,0x4e40,0xBF,0xE7,0xA2,0xE6,0x6A,0x46,0x99,0x1A);


MIDL_DEFINE_GUID(IID, IID_IFwExportDlg,0x67A68372,0x5727,0x4bd4,0x94,0xA7,0xC2,0xD7,0x03,0xA7,0x5C,0x36);


MIDL_DEFINE_GUID(CLSID, CLSID_FwExportDlg,0x86DD56A8,0xCDD0,0x49d2,0xBD,0x57,0xC7,0x8F,0x83,0x67,0xD6,0xC4);


MIDL_DEFINE_GUID(IID, IID_IFwStylesDlg,0x0D598D88,0xC17D,0x4E46,0xAC,0x89,0x51,0xFF,0xC5,0xDA,0x07,0x99);


MIDL_DEFINE_GUID(CLSID, CLSID_FwStylesDlg,0x158F638D,0xD344,0x47FC,0xAB,0x39,0x4C,0x1A,0x74,0x2F,0xD0,0x6B);


MIDL_DEFINE_GUID(IID, IID_IFwDbMergeStyles,0xA7CD703C,0x6199,0x4097,0xA5,0xC0,0xAB,0x78,0xDD,0x23,0x12,0x0E);


MIDL_DEFINE_GUID(CLSID, CLSID_FwDbMergeStyles,0x217874B4,0x90FE,0x469d,0xBF,0x80,0x3D,0x23,0x06,0xF3,0xBB,0x06);


MIDL_DEFINE_GUID(IID, IID_IFwDbMergeWrtSys,0xDE96B989,0x91A5,0x4104,0x97,0x64,0x69,0xAB,0xE0,0xBF,0x0B,0x9A);


MIDL_DEFINE_GUID(CLSID, CLSID_FwDbMergeWrtSys,0x40E4B757,0x4B7F,0x4B7C,0xA4,0x98,0x3E,0xB9,0x42,0xE7,0xC6,0xD6);


MIDL_DEFINE_GUID(IID, IID_IFwCheckAnthroList,0x8AC06CED,0x7B73,0x4E34,0x81,0xA3,0x85,0x2A,0x43,0xE2,0x8B,0xD8);


MIDL_DEFINE_GUID(CLSID, CLSID_FwCheckAnthroList,0x4D84B554,0xD3C8,0x4E0F,0x94,0x16,0x4B,0x26,0xA4,0xF0,0x32,0x4B);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



#endif /* !defined(_M_IA64) && !defined(_M_AMD64)*/
