/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilCom.h
Responsibility: Shon Katzenberger
Last reviewed:

	COM related utilities.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef UtilCom_H
#define UtilCom_H 1
/*:End Ignore*/

/***********************************************************************************************
	GUID comparison.
***********************************************************************************************/

inline bool operator < (REFGUID guid1, REFGUID guid2)
{
	return memcmp(&guid1, &guid2, isizeof(GUID)) < 0;
}


inline bool operator > (REFGUID guid1, REFGUID guid2)
{
	return memcmp(&guid1, &guid2, isizeof(GUID)) > 0;
}


inline bool operator <= (REFGUID guid1, REFGUID guid2)
{
	return memcmp(&guid1, &guid2, isizeof(GUID)) <= 0;
}


inline bool operator >= (REFGUID guid1, REFGUID guid2)
{
	return memcmp(&guid1, &guid2, isizeof(GUID)) >= 0;
}


/***********************************************************************************************
	Smart pointers.
***********************************************************************************************/

#ifdef WIN32
#include <comutil.h>
#endif
// This is our replacement for <comip.h>
#include "ComSmartPtr.h"
#ifdef WIN32
#include <comdef.h>
#else
#include "COMPointers.h"
#include "COMPointersMore.h"
#endif
#include "ComSmartPtrImpl.h"

#define DEFINE_COM_PTR(intf) _COM_SMARTPTR_TYPEDEF(intf, __uuidof(intf))

DEFINE_COM_PTR(IClassFactory);

/***********************************************************************************************
	Smart pointer validation.
***********************************************************************************************/


/*----------------------------------------------------------------------------------------------
	ValidReadPtr for a smart pointer. This is so AssertPtr works on a smart pointer.
----------------------------------------------------------------------------------------------*/
template<typename T> inline bool ValidReadPtr(const ComSmartPtr<T> & qt)
{
	return !::IsBadReadPtr((T *)qt, isizeof(T));
}


/*----------------------------------------------------------------------------------------------
	ValidWritePtr for a smart pointer.
----------------------------------------------------------------------------------------------*/
template<typename T> inline bool ValidWritePtr(const ComSmartPtr<T> & qt)
{
	return !::IsBadWritePtr((T *)qt, isizeof(T));
}


/*----------------------------------------------------------------------------------------------
	This acts like a cast operator but really does a QueryInterface.
----------------------------------------------------------------------------------------------*/
template<typename T> inline ComSmartPtr<T> com_cast(IUnknown * punk)
{
	ComSmartPtr<T> qt;
	if (punk)
	{
		HRESULT hr = punk->QueryInterface(qt.GetIID(), (void **)&qt);
		Assert(FAILED(hr) == !qt);
	}
	return qt;
}


/*----------------------------------------------------------------------------------------------
	Release and set a COM pointer to NULL.
----------------------------------------------------------------------------------------------*/
template<typename T> inline void ReleaseObj(T *& pobj)
{
	AssertPtrN(pobj);
	if (pobj)
	{
		pobj->Release();
		pobj = NULL;
	}
}


/*----------------------------------------------------------------------------------------------
	AddRef a COM pointer if not NULL.
----------------------------------------------------------------------------------------------*/
template<typename T> inline void AddRefObj(T * pobj)
{
	AssertPtrN(pobj);
	if (pobj)
		pobj->AddRef();
}


/*----------------------------------------------------------------------------------------------
	Set a COM pointer value, after first releasing the old value.
----------------------------------------------------------------------------------------------*/
template<typename T> inline void SetObj(T ** ppobj, T * pobj)
{
	AssertPtr(ppobj);
	AssertPtrN(*ppobj);
	AssertPtrN(pobj);
	if (pobj)
		pobj->AddRef();
	if (*ppobj)
		(*ppobj)->Release();
	*ppobj = pobj;
}


/*----------------------------------------------------------------------------------------------
	Test two COM objects for equality.
----------------------------------------------------------------------------------------------*/
bool SameObject(IUnknown *punk1, IUnknown *punk2);


/*----------------------------------------------------------------------------------------------
	BSTR related utilities.
----------------------------------------------------------------------------------------------*/
inline void ReleaseBstr(BSTR &bstr)
{
	AssertPtrN(bstr);
	if (bstr)
	{
		SysFreeString(bstr);
		bstr = NULL;
	}
}


inline int BstrLen(BSTR bstr)
{
	AssertBstrN(bstr);
	if (!bstr)
		return 0;
	return ::SysStringLen(bstr);
}


inline int BstrSize(BSTR bstr)
{
	AssertBstrN(bstr);
	if (!bstr)
		return 0;
	return ::SysStringLen(bstr) * isizeof(OLECHAR);
}


inline void CopyBstr(BSTR *pbstr, BSTR bstr)
{
	AssertPtr(pbstr);
	AssertPtrN(*pbstr);
	AssertPtrN(bstr);

	ReleaseBstr(*pbstr);
	*pbstr = SysAllocStringLen(bstr, BstrLen(bstr));
	if (!*pbstr && 0 != BstrLen(bstr))
		ThrowOutOfMemory();
}


inline void SetBstr(BSTR *pbstr, LPCOLESTR psz)
{
	AssertPtr(pbstr);
	AssertPtrN(*pbstr);
	AssertPtrN(psz);

	ReleaseBstr(*pbstr);
	*pbstr = SysAllocString(psz);
	if (!*pbstr && psz)
		ThrowOutOfMemory();
}

inline void SetBstr(BSTR *pbstr, OLECHAR * prgch, int cch)
{
	AssertPtr(pbstr);
	AssertPtrN(*pbstr);
	AssertArray(prgch, cch);

	ReleaseBstr(*pbstr);
	*pbstr = SysAllocStringLen(prgch, cch);
	if (!*pbstr && cch)
		ThrowOutOfMemory();
}

// Allocate an uninitialized BSTR, typically as a buffer to copy into
inline void AllocBstr(BSTR *pbstr, int cch)
{
	AssertPtr(pbstr);
	AssertPtrN(*pbstr);
	Assert(cch > 0);

	ReleaseBstr(*pbstr);
	*pbstr = SysAllocStringLen(NULL, cch);
	if (!*pbstr)
		ThrowOutOfMemory();
}



inline void SetBstr(BSTR *pbstr, LPCSTR psz)
{
	AssertPtr(pbstr);
	AssertPtrN(*pbstr);
	AssertPszN(psz);

	ReleaseBstr(*pbstr);
	if (!psz || !psz[0])
		return;

	int cch = MultiByteToWideChar(CP_UTF8, 0, psz, -1, NULL, 0);
	// According to the doc, it can only fail if (1) buffer too small, (2) invalid flags, or
	// (3) invalid parameter, apart from a case that only applies with a flag we are not using.
	// None of these ought to happen with the arguments we're using.
	if (cch == 0)
		ThrowBuiltInError("MultiByletoWideChar");

	*pbstr = SysAllocStringLen(NULL, cch - 1);
	if (!*pbstr)
		ThrowOutOfMemory();

	MultiByteToWideChar(CP_UTF8, 0, psz, -1, *pbstr, cch);
}

// Create a new BSTR and copy text to it. Input characters must be strictly ASCII (7-bit).
inline BSTR AsciiToBstr(const char * pch)
{
	if (pch == NULL)
		return NULL;
	int cch = strlen(pch);
	BSTR bstr = ::SysAllocStringLen(NULL, cch);
	if (!bstr)
		ThrowHr(WarnHr(E_OUTOFMEMORY), L"AsciiToBstr");
	::MultiByteToWideChar(CP_UTF8, 0, pch, cch, bstr, cch);
	return bstr;
}

// Create a new BSTR from a UnicodeString (this is an ICU class).
inline BSTR UnicodeStringToBstr(UnicodeString & ust)
{
	BSTR bstr = ::SysAllocStringLen(ust.getBuffer(), ust.length());
	if (!bstr)
		ThrowHr(WarnHr(E_OUTOFMEMORY), L"UnicodeStringToBstr");
	return bstr;
}

/***********************************************************************************************
	ComBool Definition.
***********************************************************************************************/

// This is so the .h files generated from .idl files don't typedef ComBool to be VARIANT_BOOL.
#define CUSTOM_COM_BOOL 1


class ComBool
{
public:
	ComBool(void)
	{
		AssertPtr(this);
#if WIN32
		Debug(m_f = (VARIANT_BOOL)0xCCCC);
#else
		m_f = 0;
#endif
	}

	ComBool(int fT)
	{
		AssertPtr(this);
		m_f = fT ? VARIANT_TRUE : VARIANT_FALSE;
	}

	operator bool(void) const
	{
		AssertObj(this);
		return m_f != VARIANT_FALSE;
	}

	ComBool & operator=(int fT)
	{
		AssertPtr(this);
		m_f = fT ? VARIANT_TRUE : VARIANT_FALSE;
		return *this;
	}

private:
	VARIANT_BOOL m_f;

#ifdef DEBUG
	bool AssertValid(void) const
	{
		AssertPtr(this);
		Assert(m_f == VARIANT_TRUE || m_f == VARIANT_FALSE);
		return true;
	}
#endif // DEBUG
};


/*************************************************************************************
	Useful macros to AddRef "this" or any COM object in the current scope.
*************************************************************************************/
class _Lock_Unknown
{
private:
	IUnknown *m_punk;
public:
	_Lock_Unknown(IUnknown *punk) {
		m_punk = punk;
		if (m_punk)
			m_punk->AddRef();
	}
	~_Lock_Unknown(void) {
		if (m_punk)
			m_punk->Release();
	}
};

#define LockThis() _Lock_Unknown _lock_this_##__LINE__(this)
#define LockObj(pobj) _Lock_Unknown _lock_obj_##__LINE__(pobj)

/*************************************************************************************
	Miscellaneous COM related utility functions.
*************************************************************************************/
const char * AsciiHresult(HRESULT hr);
const wchar * UnicodeHresult(HRESULT hr);

/*************************************************************************************
	Macros to use at the start and end of COM methods. The second argument to
	END_COM_METHOD is meant to be a class factory that is used to create instances of
	the class, or if there isn't one, a DummyFactory that just knows a fake progid.
	The IID is that of the interface which this method implements.
*************************************************************************************/
#define BEGIN_COM_METHOD \
	{IErrorInfo * pIErrorInfo = NULL;\
	GetErrorInfo(0, &pIErrorInfo);\
	if(pIErrorInfo){\
		pIErrorInfo->Release();\
	}}\
	try \
	{\

#define END_COM_METHOD(factory, iid) \
	}\
	catch (Throwable & thr) \
	{ \
		return HandleThrowable(thr, iid, &factory); \
	} \
	catch (...) \
	{ \
		return HandleDefaultException(iid, &factory); \
	} \
	return S_OK; \

/*************************************************************************************
	Argument validation. We may modify these as we develop our error handling
	strategy.
*************************************************************************************/

#define ChkComArgPtr(p) \
{ \
	AssertPtrN(p); \
	if (!p) \
		ThrowInternalError(E_POINTER); \
} \


// Check a COM argument to be used to return a value. Also inits to null/0/false, if
// valid.
#define ChkComOutPtr(p) \
{ \
	AssertPtrN(p); \
	if (!p) \
		ThrowInternalError(E_POINTER); \
	*p = 0; \
} \

#define ChkComArgPtrN(p) \
	AssertPtrN(p); \

#define ChkComArrayArg(prgv, cv) \
{ \
	AssertArray(prgv, cv); \
	if (cv && !prgv) \
		ThrowInternalError(E_POINTER); \
} \

#define ChkComBstrArg(bstr) \
	AssertBstrN(bstr); \
	if (!bstr) \
		ThrowInternalError(E_POINTER); \

#define ChkComBstrArgN(bstr) \
	AssertBstrN(bstr); \

#if WIN32

/*----------------------------------------------------------------------------------------------
	This class provides an IDataObject wrapper around a simple string.  This facilitates
	passing String data via the clipboard or "drag and drop".

	Microsoft really ought to provide such a class but I can't find any indication they have.

	Hungarian: sdobj
----------------------------------------------------------------------------------------------*/
class StringDataObject : public IDataObject
{
public:
	//:> Static methods

	static void Create(OLECHAR * pszSrc, IDataObject ** ppdobj);

	//:> IUnknown methods

	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	//:> IDataObject methods

	STDMETHOD(GetData)(FORMATETC * pformatetcIn, STGMEDIUM * pmedium);
	STDMETHOD(GetDataHere)(FORMATETC * pformatetc, STGMEDIUM * pmedium);
	STDMETHOD(QueryGetData)(FORMATETC * pformatetc);
	STDMETHOD(GetCanonicalFormatEtc)(FORMATETC * pformatectIn, FORMATETC * pformatetcOut);
	STDMETHOD(SetData)(FORMATETC * pformatetc, STGMEDIUM * pmedium, BOOL fRelease);
	STDMETHOD(EnumFormatEtc)(DWORD dwDirection, IEnumFORMATETC ** ppenumFormatEtc);
	STDMETHOD(DAdvise)(FORMATETC * pformatetc, DWORD advf, IAdviseSink * pAdvSink,
		DWORD * pdwConnection);
	STDMETHOD(DUnadvise)(DWORD dwConnection);
	STDMETHOD(EnumDAdvise)(IEnumSTATDATA ** ppenumAdvise);

protected:
	//:> Static member variables.

	//:> Member variables.
	int m_cref;				// Standard reference count variable.
	StrUni m_stuContents;		// Contents of the data object.

	//:> Constructors and destructors.
	StringDataObject();
	~StringDataObject();

	//:> Methods
	void Init(OLECHAR * pchSrc);
};
typedef ComSmartPtr<StringDataObject> StringDataObjectPtr;

/*----------------------------------------------------------------------------------------------
	This class provides an IEnumFORMATETC COM object which supports CF_UNICODETEXT,
	CF_OEMTEXT, and CF_TEXT formatted clipboard formats. It works together with
	StringDataObject to implement proper OLE clipboard handling for simple Unicode strings.

	Microsoft really ought to provide such a class but I can't find any indication they have.

	Hungarian: senum
----------------------------------------------------------------------------------------------*/
class StrEnumFORMATETC : IEnumFORMATETC
{
public:
	//:> Static methods
	static void Create(IEnumFORMATETC ** ppenum);

	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	//:> IEnumFORMATETC methods
	STDMETHOD(Next)(UCOMINT32 celt, FORMATETC * rgelt, UCOMINT32 * pceltFetched);
	STDMETHOD(Skip)(UCOMINT32 celt);
	STDMETHOD(Reset)(void);
	STDMETHOD(Clone)(IEnumFORMATETC ** ppenum);

protected:
	//:> Member variables.
	int m_cref;		// Standard reference count variable.
	int m_ifmte;	// Current index into StrEnumFORMATETC::g_rgfmte for this enumerator.

	//:> Static member variables.
	enum { kcfmteLim = 3 };
	// Global array of FORMATETC data structures containing all the supported formats.
	static FORMATETC g_rgfmte[kcfmteLim];

	//:> Constructors and destructors.
	StrEnumFORMATETC();
	~StrEnumFORMATETC();
};

#endif //WIN32

#endif // !UtilCom_H
