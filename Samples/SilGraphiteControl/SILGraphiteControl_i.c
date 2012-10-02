
#pragma warning( disable: 4049 )  /* more than 64k source lines */

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 6.00.0347 */
/* at Fri Jun 27 14:46:43 2003
 */
/* Compiler settings for SILGraphiteControl.idl:
	Os, W1, Zp8, env=Win32 (32b run)
	protocol : dce , ms_ext, c_ext
	error checks: allocation ref bounds_check enum stub_data
	VC __declspec() decoration level:
		 __declspec(uuid()), __declspec(selectany), __declspec(novtable)
		 DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#if !defined(_M_IA64) && !defined(_M_AMD64)

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

MIDL_DEFINE_GUID(IID, LIBID_SILGraphiteControlLib,0xDBA69D4C,0xA68F,0x4156,0xA4,0x11,0x47,0x91,0x1C,0xEA,0x62,0x75);


MIDL_DEFINE_GUID(IID, DIID__DSILGraphiteControl,0xCFFFE4B0,0x0E6E,0x4D86,0x82,0xC5,0x10,0xFB,0xD2,0xCA,0x6F,0x16);


MIDL_DEFINE_GUID(IID, DIID__DSILGraphiteControlEvents,0xF6C724C7,0xCC62,0x46DC,0xAB,0x50,0x1E,0x88,0x02,0xAB,0xD0,0xD4);


MIDL_DEFINE_GUID(CLSID, CLSID_SILGraphiteControl,0x62631FFE,0x1185,0x44CC,0x8A,0xD2,0x60,0x2A,0x05,0xD1,0xD0,0x71);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



#endif /* !defined(_M_IA64) && !defined(_M_AMD64)*/
