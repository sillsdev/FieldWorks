

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:12:23 2006
 */
/* Compiler settings for C:\fw\Output\Common\DbServicesTlb.idl:
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

MIDL_DEFINE_GUID(IID, LIBID_FwDbServices,0x03BAB68B,0x6C7F,0x42ff,0x84,0x38,0xB6,0xD5,0x23,0xCB,0x79,0xB9);


MIDL_DEFINE_GUID(IID, IID_IBackupDelegates,0x1C0FA5AF,0x00B4,0x4dc1,0x8F,0x9E,0x16,0x8A,0xF3,0xF8,0x92,0xB0);


MIDL_DEFINE_GUID(IID, IID_DIFwBackupDb,0x00A94783,0x8F5F,0x42af,0xA9,0x93,0x49,0xF2,0x15,0x4A,0x67,0xE2);


MIDL_DEFINE_GUID(CLSID, CLSID_FwBackup,0x0783E03E,0x5208,0x4d71,0x9D,0x98,0x3D,0x49,0x74,0xC8,0xE6,0x33);


MIDL_DEFINE_GUID(IID, IID_IDisconnectDb,0x0CC74E0C,0x3017,0x4c02,0xA5,0x07,0x3F,0xB8,0xCE,0x62,0x1C,0xDC);


MIDL_DEFINE_GUID(CLSID, CLSID_FwDisconnect,0x008B93C5,0x866A,0x4238,0x96,0x3B,0x3F,0x6C,0x51,0xB5,0xBB,0x03);


MIDL_DEFINE_GUID(IID, IID_IRemoteDbWarn,0x004C42AE,0xCB07,0x47b5,0xA9,0x36,0xD9,0xCA,0x4A,0xC4,0x66,0xD7);


MIDL_DEFINE_GUID(IID, IID_IDbWarnSetup,0x06082023,0xC2BA,0x4425,0x90,0xFD,0x2F,0x76,0xB7,0x4C,0xCB,0xE7);


MIDL_DEFINE_GUID(CLSID, CLSID_FwRemote,0x0732A981,0x4921,0x4d4b,0x9E,0x1D,0xAF,0x93,0x62,0xE2,0x70,0x8D);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



#endif /* !defined(_M_IA64) && !defined(_M_AMD64)*/
