/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Throwable.h
Responsibility: Shon Katzenberger
Last reviewed:

----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef Throwable_H
#define Throwable_H 1

#include <cstring>
#ifndef _WIN32
#include <cwchar>
#endif
#include "GrPlatform.h"
#include "GrDebug.h"

namespace gr
{


/*----------------------------------------------------------------------------------------------
	Standard class to wrap an HRESULT, HelpID, and message.
----------------------------------------------------------------------------------------------*/
class Throwable
{
public:
	// Constructors and Destructor.
	Throwable(unsigned long hr = S_OK, const wchar_t *pszMsg = 0, int hHelpId = 0)
	: m_hr(hr)
	{
		AssertPszN(pszMsg);
		if(pszMsg)
		{
			m_pszMsg = new wchar_t[wcslen(pszMsg)+1];//.Assign(pszMsg);
			wcscpy(m_pszMsg, pszMsg);
		}
		else
		{
			m_pszMsg = new wchar_t[1];
			*m_pszMsg = 0;
		}
		m_hHelpId = hHelpId;
	}

	virtual ~Throwable(void)
	{
		delete[] m_pszMsg;
	}

	unsigned long Result(void)
	{
		return m_hr;
	}

	unsigned long Error(void)
	{
		if (FAILED(m_hr))
			return m_hr;
		return WarnHr(E_FAIL);
	}

	const wchar_t * Message()
	{
		return m_pszMsg;
	}
	int HelpId() {return m_hHelpId;}

protected:
	unsigned long m_hr;
	wchar_t *m_pszMsg;
	//StrUni m_stuMsg;
	int m_hHelpId;
};

/*----------------------------------------------------------------------------------------------
	This variety of Throwable adds information about a stack dump.

	Non-inline methods are in StackDumper.cpp
----------------------------------------------------------------------------------------------*/
class ThrowableSd : public Throwable
{
public:
	// Finding this constant in the Description of an ErrorInfo is a signal to
	// the FieldWorks error handler of an error message that contains information the
	// average user should not see. It should be displayed if "details" is clicked, and copied
	// to the clipboard.
	static const wchar_t * MoreSep() {return L"\n---***More***---\n";}
	const char * GetDump() {return m_pszDump;}
	ThrowableSd(unsigned long hr = S_OK, const wchar_t * pszMsg = 0,
		int hHelpId = 0, const char * pszDump = 0)
		:Throwable(hr, pszMsg, hHelpId)
	{
		if(pszDump)
		{
			m_pszDump = new char[strlen(pszDump)+1];
			strcpy(m_pszDump, pszDump);
		}
		else
		{
			m_pszDump = new char[1];
			*m_pszDump = 0;
		}
		//m_staDump(pszDump)
	}
	~ThrowableSd()
	{
		delete[] m_pszDump;
	}
protected:
	char *m_pszDump; // stack dump
};


//HRESULT CheckHrCore(HRESULT hr, IUnknown * punk, REFGUID iid);

/*----------------------------------------------------------------------------------------------
	This is used when we call an interface that may not support error info. It allows a more
	reliable job of checking for error info to be done.
----------------------------------------------------------------------------------------------*/
/*inline HRESULT CheckExtHr(HRESULT hr, IUnknown * punk, REFGUID iid)
{
	if (FAILED(hr))
		return CheckHrCore(hr, punk, iid);
	return hr;
}*/

//class DummyFactory;

// See StackDumper.cpp for implementations and comments.
/*HRESULT HandleThrowable(Throwable & thr, REFGUID iid, DummyFactory * pfact);
HRESULT HandleDefaultException(REFGUID iid, DummyFactory * pfact);
*/

/*----------------------------------------------------------------------------------------------
	Function to throw an HRESULT as a Throwable object.
----------------------------------------------------------------------------------------------*/
inline void ThrowHr(unsigned long hr, const wchar_t * pszMsg = NULL, int hHelpId = 0)
{
	throw Throwable(hr, pszMsg, hHelpId);
}

/*----------------------------------------------------------------------------------------------
	If the hr is a failure code, it is thrown, after possibly generating a stack dump if one
	might be needed. Otherwise it is returned.

	Use this only when we strongly believe that the interface we're calling will either always
	succeed or will generate relevant IErrorInfo if it fails. CheckHr will generate an
	internal error if hr is an error code and there is no error info. It is not able to detect
	irrelevant error info, as that requires a pointer to the object and an IID. When in doubt,
	use CheckExtHr.
----------------------------------------------------------------------------------------------*/
inline unsigned long CheckHr(unsigned long hr)
{
	if (FAILED(hr))
		ThrowHr(hr);
		//return CheckHrCore(hr, NULL, IID_NULL);
	return hr;
}

// Call this to report detecting an internal error. It may be an error in the module where
// the call is made (E_UNEXPECTED) or the calling module (E_POINTER, E_INVALIDARG).
// Implementations of these are in StackDumper.cpp
/*void ThrowInternalError(HRESULT hr, const wchar * pszMsg = NULL, int hHelpId = 0);
void ThrowInternalError(HRESULT hr, const char * pszMsg, int hHelpId = 0);
void ThrowOutOfMemory();
void ThrowBuiltInError(char * pchFnName);

// Throw an HRESULT; message text is in resource hid, which also serves as help file ID.
void ThrowNice(HRESULT hr, int hid);
*/

}

#if !defined(GR_NAMESPACE)
using namespace gr;
#endif

#endif // !Throwable_H
