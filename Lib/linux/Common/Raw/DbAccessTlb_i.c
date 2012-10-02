

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


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

MIDL_DEFINE_GUID(IID, LIBID_DbAccess,0xAAB4A4A1,0x3C83,0x11d4,0xA1,0xBB,0x00,0xC0,0x4F,0x0C,0x95,0x93);


MIDL_DEFINE_GUID(IID, IID_IOleDbCommand,0x21993161,0x3E24,0x11d4,0xA1,0xBD,0x00,0xC0,0x4F,0x0C,0x95,0x93);


MIDL_DEFINE_GUID(IID, IID_IOleDbEncap,0xCB7BEA0F,0x960A,0x4b23,0x80,0xD3,0xDE,0x06,0xC0,0x53,0x0E,0x04);


MIDL_DEFINE_GUID(IID, IID_IFwMetaDataCache,0x6AA9042E,0x0A4D,0x4f33,0x88,0x1B,0x3F,0xBE,0x48,0x86,0x1D,0x14);


MIDL_DEFINE_GUID(IID, IID_IDbAdmin,0x2A861F95,0x63D0,0x480d,0xB5,0xAF,0x4F,0xAF,0x0D,0x22,0x12,0x5D);


MIDL_DEFINE_GUID(CLSID, CLSID_OleDbEncap,0xAAB4A4A3,0x3C83,0x11d4,0xA1,0xBB,0x00,0xC0,0x4F,0x0C,0x95,0x93);


MIDL_DEFINE_GUID(CLSID, CLSID_FwMetaDataCache,0x3A1B1AC6,0x24C5,0x4ffe,0x85,0xD5,0x67,0x5D,0xB4,0xB9,0xFC,0xBB);


MIDL_DEFINE_GUID(CLSID, CLSID_DbAdmin,0xD584A725,0x8CF4,0x4699,0x94,0x1F,0xD1,0x33,0x7A,0xC7,0xDB,0x5C);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



#endif /* !defined(_M_IA64) && !defined(_M_AMD64)*/
