/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Throwable.h
Responsibility: Shon Katzenberger
Last reviewed:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef Throwable_H
#define Throwable_H 1

class Throwable;	// now in StackDumper.h
class ThrowableSd;	// now in StackDumper.h

void CheckHrCore(HRESULT hr);

inline void AssertNoErrorInfo()
{
#ifdef _DEBUG
	// Make sure GetErrorInfo has already been handled before trying to issue the ComMethodCall
	IErrorInfo * pIErrorInfo = NULL;
	HRESULT hr = ::GetErrorInfo(0, &pIErrorInfo);
	Assert(SUCCEEDED(hr));

	// ----------------------------------------------------------------------------------
	// If this asserts here, you probably need to wrap a preceding COM call with CheckHr.
	// You may need to check the stack to find what the preceding COM call was.
	// You may also have used CheckHr(hr); In which case, you need to put the com method
	//  call within the CheckHr and put a try-catch(Throwable) block around the statement
	//  so you can get the hr value.
	// e.g. Instead of:
	//		HRESULT hr = ComMethodCall();
	//		if(hr == E_FAIL)
	//		    return; // Ignore failures
	//
	//		CheckHr(hr);
	//
	// do:
	//		try
	//		{
	//			CheckHr(ComMethodCall());
	//		}
	//		catch (Throwable& thr)
	//		{
	//			// ComMethodCall() failed;
	//			HRESULT hr = thr.Result();
	//		}
	//		// if not caught, ComMethodCall() succeeded i.e. !(FAILED(hr))
	//
	// If you are checking a success condition (S_FALSE) then you need to do:
	//
	//    HRESULT hr;
	//    CheckHr(hr = qtpbBulNumFont->GetStrPropValue(ktptFontFamily, &sbstrVal));
	//    if (hr == S_FALSE)
	//		{ do something here; }
	//
	// The one caveat is due to the verification that there is no ErrorInfo set,
	//   you can't wrap GetErrorInfo with CheckHr!
	//
	// IF THIS ASSERTS, READ ABOVE!!
	Assert(pIErrorInfo == NULL);
	// ----------------------------------------------------------------------------------

	if(pIErrorInfo != NULL)
	{
		BSTR dscr = NULL;
		hr = pIErrorInfo->GetDescription(&dscr);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(dscr);
		::SysFreeString(dscr);
		BSTR src = NULL;
		hr = pIErrorInfo->GetSource(&src);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(src);
		::SysFreeString(src);
		pIErrorInfo->Release();
	}
#else
	// In non debug mode, if a GetErrorInfo isn't handled before we call the method, the worst thing
	// that may happen is that the user gets different error message than they should
	// do nothing.
#endif
}

// This is needed to trick the compiler into allowing us to have a constant condition.
inline bool ____False() {
	return false;
}

// the do { } while (____False) is to ensure that the entire macro is considered a single statement which will
//  then not break an 'if' with a single expression followed by 'else'.
#define CheckHr(ComMethodCall) \
	do {\
	AssertNoErrorInfo();\
	HRESULT ____hr____ = ComMethodCall;\
	if (FAILED(____hr____)) {CheckHrCore(____hr____);}\
	} while(____False())


#define CheckExtHr(ComMethodCall, toss1, toss2) CheckHr(ComMethodCall)

inline void ClearErrorInfo()
{
#ifdef _DEBUG
	IErrorInfo * pIErrorInfo = NULL;
	HRESULT hr = ::GetErrorInfo(0, &pIErrorInfo);
	Assert(SUCCEEDED(hr));
	if(pIErrorInfo != NULL)
	{
		BSTR dscr = NULL;
		hr = pIErrorInfo->GetDescription(&dscr);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(dscr);
		::SysFreeString(dscr);
		BSTR src = NULL;
		hr = pIErrorInfo->GetSource(&src);
		Assert(SUCCEEDED(hr));
		::OutputDebugString(src);
		::SysFreeString(src);
		pIErrorInfo->Release();
	}
#else
	// for performance reasons we don't clear the error info, similar to AssertNoErrorInfo().
#endif
}



class DummyFactory;

// See StackDumper.cpp for implementations and comments.
HRESULT HandleThrowable(Throwable & thr, REFGUID iid, DummyFactory * pfact);
HRESULT HandleDefaultException(REFGUID iid, DummyFactory * pfact);

// use this instead of CheckHr to ignore the result. This clears the error info
// so that a subsequent CheckHr call won't assert.
#define IgnoreHr(ComMethodCall) \
	do {\
	AssertNoErrorInfo();\
	ComMethodCall;\
	ClearErrorInfo();\
	} while(____False())


// Call this to report detecting an internal error. It may be an error in the module where
// the call is made (E_UNEXPECTED) or the calling module (E_POINTER, E_INVALIDARG).
// Implementations of these are in StackDumper.cpp
#ifdef WIN32
void ThrowInternalError(HRESULT hr, const wchar* pszMsg = NULL, int hHelpId = 0, IErrorInfo* pErrInfo = NULL);
#else // WIN32
void ThrowInternalError(HRESULT hr, const wchar_t* pszMsg, int hHelpId = 0, IErrorInfo* pErrInfo = NULL);
void ThrowInternalError(HRESULT hr, const OLECHAR* pszMsg = NULL, int hHelpId = 0, IErrorInfo* pErrInfo = NULL);
#endif
void ThrowInternalError(HRESULT hr, const char* pszMsg, int hHelpId = 0, IErrorInfo* pErrInfo = NULL);
void ThrowOutOfMemory();
void ThrowBuiltInError(const char * pchFnName);

// Throw an HRESULT; message text is in resource hid, which also serves as help file ID.
void ThrowNice(HRESULT hr, int hid);

#ifdef WIN32
void ThrowHr(HRESULT hr, const wchar * pszMsg = NULL, int hHelpId = 0, IErrorInfo* pErrInfo = NULL); // now in StackDumper.h
#else
template<class ZChar>
void ThrowHr(HRESULT hr, const ZChar * pszMsg = NULL, int hHelpId = 0, IErrorInfo* pErrInfo = NULL); // now in StackDumper.h
inline void ThrowHr(HRESULT hr);
#endif

// Use ThrowHrEx() instead of ThrowHr() when you want to throw a COM error, but the reason
// for the error is a failed Win-API method call. This version gets the information associated
// with the Windows error as description.
void ThrowHrEx(HRESULT hr, int hHelpId = 0);

// Use ReturnHr() instead of a "return WarnHr()" in case of an user error. This will
// set up ErrorInfo and thus can be handled properly in C#. (TE-4716)
// We set up a dummy empty call stack so that we don't have to retrieve the entire call stack.
// Since this is the result of some user error we want to save the time getting the call stack
// takes and we don't want to crash the program.
// NOTE: If this gets called inside of a COM method (surrounded by BEGIN/END_COM_METHOD),
// the END_COM_METHOD will catch the exception we throw here. Because we already added a
// "call stack" it will just set up the error info and return the hr value.
#if WIN32
#define ReturnHr(hr) {WarnHr(hr); throw ThrowableSd((hr), NULL, 0, " ");}
#else
#define ReturnHr(hr) {WarnHr(hr); throw ThrowableSd((hr), static_cast<const char*>(NULL), 0, " ");}
#endif

// Use ReturnHrEx() instead of ReturnHr() when you want to return a COM error, but the reason
// for the error is a failed Win-API method call. This version gets the information associated
// with the Windows error as description.
#define ReturnHrEx(hr) {WarnHr(hr); \
	throw ThrowableSd((hr), ConvertException(::GetLastError()).Chars(), 0, " ");}

#endif // !Throwable_H
