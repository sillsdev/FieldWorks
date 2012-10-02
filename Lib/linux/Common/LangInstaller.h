// AUTOMATICALLY GENERATED ON Tue Jun  8 09:04:12 MDT 2010 FROM ../../../Lib/linux/Common/LangInstaller.h.raw by ../../../../COM/test/fix-midl.sh


/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0500 */
/* at Tue Jun 08 08:59:23 2010
 */
/* Compiler settings for LangInstaller.idl:
	Oicf, W1, Zp8, env=Win32 (32b run)
	protocol : dce , ms_ext, c_ext, robust
	error checks: allocation ref bounds_check enum stub_data
	VC __declspec() decoration level:
		 __declspec(uuid()), __declspec(selectany), __declspec(novtable)
		 DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __LangInstaller_h__
#define __LangInstaller_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __ILangInstaller_FWD_DEFINED__
#define __ILangInstaller_FWD_DEFINED__
typedef interface ILangInstaller ILangInstaller;
#endif 	/* __ILangInstaller_FWD_DEFINED__ */


#ifndef __LangInstaller_FWD_DEFINED__
#define __LangInstaller_FWD_DEFINED__

#ifdef __cplusplus
typedef class LangInstaller LangInstaller;
#else
typedef struct LangInstaller LangInstaller;
#endif /* __cplusplus */

#endif 	/* __LangInstaller_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif


/* interface __MIDL_itf_LangInstaller_0000_0000 */
/* [local] */


#undef ATTACH_GUID_TO_CLASS
#if defined(__cplusplus)
#define ATTACH_GUID_TO_CLASS(type, guid, cls) \
	type __declspec(uuid(#guid)) cls;
#else // !defined(__cplusplus)
#define ATTACH_GUID_TO_CLASS(type, guid, cls)
#endif // !defined(__cplusplus)

#ifndef DEFINE_COM_PTR
#define DEFINE_COM_PTR(cls)
#endif

#undef GENERIC_DECLARE_SMART_INTERFACE_PTR
#define GENERIC_DECLARE_SMART_INTERFACE_PTR(cls, iid) \
	ATTACH_GUID_TO_CLASS(interface, iid, cls); \
	DEFINE_COM_PTR(cls);


#ifndef CUSTOM_COM_BOOL
typedef VARIANT_BOOL ComBool;

#endif

#if 0
// This is so there is an equivalent VB type.
typedef CY SilTime;

#elif defined(SILTIME_IS_STRUCT)
// This is for code that compiles UtilTime.*.
struct SilTime;
#else
// This is for code that uses a 64-bit integer for SilTime.
typedef __int64 SilTime;
#endif

ATTACH_GUID_TO_CLASS(class,
C13F5F35-1FD2-4388-B905-394D18D28EFB
,
LangInstaller
);


extern RPC_IF_HANDLE __MIDL_itf_LangInstaller_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_LangInstaller_0000_0000_v0_0_s_ifspec;


#ifndef __LangInstaller_LIBRARY_DEFINED__
#define __LangInstaller_LIBRARY_DEFINED__

/* library LangInstaller */
/* [helpstring][version][uuid] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILangInstaller
,
EB5B7CFA-6EC8-4641-97D2-5FE338FF5434
);
ATTACH_GUID_TO_CLASS(class,
5EDF610A-F38F-4034-8714-76B95FDF70EC
,
LangInstaller
);

#define LIBID_LangInstaller __uuidof(LangInstaller)

#ifndef __ILangInstaller_INTERFACE_DEFINED__
#define __ILangInstaller_INTERFACE_DEFINED__

/* interface ILangInstaller */
/* [unique][object][uuid] */


#define IID_ILangInstaller __uuidof(ILangInstaller)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("EB5B7CFA-6EC8-4641-97D2-5FE338FF5434")
	ILangInstaller : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Install(
			/* [in] */ BSTR locale,
			/* [in] */ VARIANT_BOOL fNewLang,
			/* [in] */ VARIANT_BOOL fAddPUA,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE AddPUAChars(
			/* [in] */ BSTR locale,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE Uninstall(
			/* [in] */ BSTR locale,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE ShowCustomLocales(
			/* [retval][out] */ VARIANT_BOOL *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE ShowCustomLanguages(
			/* [retval][out] */ VARIANT_BOOL *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE RestoreOriginalSettings(
			/* [in] */ VARIANT_BOOL fNewLang,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess) = 0;

		virtual HRESULT STDMETHODCALLTYPE get_ErrorCode(
			/* [retval][out] */ long *pErrorCode) = 0;

		virtual HRESULT STDMETHODCALLTYPE Cleanup( void) = 0;

	};

#else 	/* C style interface */

	typedef struct ILangInstallerVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			ILangInstaller * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */
			__RPC__deref_out  void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			ILangInstaller * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			ILangInstaller * This);

		HRESULT ( STDMETHODCALLTYPE *Install )(
			ILangInstaller * This,
			/* [in] */ BSTR locale,
			/* [in] */ VARIANT_BOOL fNewLang,
			/* [in] */ VARIANT_BOOL fAddPUA,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess);

		HRESULT ( STDMETHODCALLTYPE *AddPUAChars )(
			ILangInstaller * This,
			/* [in] */ BSTR locale,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess);

		HRESULT ( STDMETHODCALLTYPE *Uninstall )(
			ILangInstaller * This,
			/* [in] */ BSTR locale,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess);

		HRESULT ( STDMETHODCALLTYPE *ShowCustomLocales )(
			ILangInstaller * This,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess);

		HRESULT ( STDMETHODCALLTYPE *ShowCustomLanguages )(
			ILangInstaller * This,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess);

		HRESULT ( STDMETHODCALLTYPE *RestoreOriginalSettings )(
			ILangInstaller * This,
			/* [in] */ VARIANT_BOOL fNewLang,
			/* [retval][out] */ VARIANT_BOOL *pfSuccess);

		HRESULT ( STDMETHODCALLTYPE *get_ErrorCode )(
			ILangInstaller * This,
			/* [retval][out] */ long *pErrorCode);

		HRESULT ( STDMETHODCALLTYPE *Cleanup )(
			ILangInstaller * This);

		END_INTERFACE
	} ILangInstallerVtbl;

	interface ILangInstaller
	{
		CONST_VTBL struct ILangInstallerVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define ILangInstaller_QueryInterface(This,riid,ppvObject)	\
	( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) )

#define ILangInstaller_AddRef(This)	\
	( (This)->lpVtbl -> AddRef(This) )

#define ILangInstaller_Release(This)	\
	( (This)->lpVtbl -> Release(This) )


#define ILangInstaller_Install(This,locale,fNewLang,fAddPUA,pfSuccess)	\
	( (This)->lpVtbl -> Install(This,locale,fNewLang,fAddPUA,pfSuccess) )

#define ILangInstaller_AddPUAChars(This,locale,pfSuccess)	\
	( (This)->lpVtbl -> AddPUAChars(This,locale,pfSuccess) )

#define ILangInstaller_Uninstall(This,locale,pfSuccess)	\
	( (This)->lpVtbl -> Uninstall(This,locale,pfSuccess) )

#define ILangInstaller_ShowCustomLocales(This,pfSuccess)	\
	( (This)->lpVtbl -> ShowCustomLocales(This,pfSuccess) )

#define ILangInstaller_ShowCustomLanguages(This,pfSuccess)	\
	( (This)->lpVtbl -> ShowCustomLanguages(This,pfSuccess) )

#define ILangInstaller_RestoreOriginalSettings(This,fNewLang,pfSuccess)	\
	( (This)->lpVtbl -> RestoreOriginalSettings(This,fNewLang,pfSuccess) )

#define ILangInstaller_get_ErrorCode(This,pErrorCode)	\
	( (This)->lpVtbl -> get_ErrorCode(This,pErrorCode) )

#define ILangInstaller_Cleanup(This)	\
	( (This)->lpVtbl -> Cleanup(This) )

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILangInstaller_INTERFACE_DEFINED__ */


#define CLSID_LangInstaller __uuidof(LangInstaller)

#ifdef __cplusplus

class DECLSPEC_UUID("5EDF610A-F38F-4034-8714-76B95FDF70EC")
LangInstaller;
#endif
#endif /* __LangInstaller_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
