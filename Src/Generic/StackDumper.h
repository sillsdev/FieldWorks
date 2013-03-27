/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: Main.h
Responsibility: John Thomson
Last reviewed:

	Header for a class to dump the execution stack.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef STACK_DUMP_H
#define STACK_DUMP_H 1

#ifdef WIN32
typedef char SDCHAR;
#else
typedef const char SDCHAR;
typedef ComSmartPtr<IErrorInfo> IErrorInfoPtr;
// We don't use CONTEXT on Linux, but it gets passed around by value in function arguments
struct CONTEXT {};
struct EXCEPTION_POINTERS;
#endif

// Generate a stack dump, with the given message as header.
void DumpStackHere(char * pchMsg);
void TransFuncDump( unsigned int u, EXCEPTION_POINTERS * pExp);

#define MAXDUMPLEN 10000
class StackDumper
{
public:
	static void ShowStack( HANDLE hThread, CONTEXT& c, SDCHAR * pszHdr = NULL); // dump a stack
	static void InitDump(SDCHAR * pszHdr);
	static void AppendShowStack( HANDLE hThread, CONTEXT& c);
	static const char * GetDump();
	static HRESULT RecordError(REFGUID iid, StrUni stuDescr, StrUni stuSource,
		int hcidHelpId, StrUni stuHelpFile);
	StackDumper();
	~StackDumper();
protected:
	int FindStartOfFrame(int ichStart);
	void ShowStackCore( HANDLE hThread, CONTEXT& c );

	// Buffer we will dump into.
	StrAnsiBufHuge * m_pstaDump;
};

// This function may be called as the argument of an __except clause to produce
// a stack dump, which may be retrieved from .

// if you use C++ exception handling: install a translator function
// with set_se_translator(). In the context of that function (but *not*
// afterwards), you can either do your stack dump, or save the CONTEXT
// record as a local copy. Note that you must do the stack sump at the
// earliest opportunity, to avoid the interesting stackframes being gone
// by the time you do the dump.
DWORD Filter( EXCEPTION_POINTERS *ep );

StrUni GetModuleHelpFilePath();
StrUni GetModuleVersion(const OLECHAR * pchPathName);

StrUni ConvertException(DWORD dwExcept);

/*----------------------------------------------------------------------------------------------
	Standard class to wrap an HRESULT, HelpID, and message.
----------------------------------------------------------------------------------------------*/
class Throwable
{
public:
	// Constructors and Destructor.
#ifdef WIN32
	Throwable(HRESULT hr = S_OK, const wchar * pszMsg = NULL, int hHelpId = 0,
		IErrorInfo* pErrInfo = NULL)
#else
	Throwable(HRESULT hr = S_OK, const wchar_t * pszMsg = NULL, int hHelpId = 0,
		IErrorInfo* pErrInfo = NULL)
#endif
	{
		AssertPszN(pszMsg);
		m_hr = hr;
		m_stuMsg.Assign(pszMsg);
		m_hHelpId = hHelpId;
		m_qErrInfo = pErrInfo;
	}

#ifndef WIN32
	Throwable(HRESULT hr, const OLECHAR * pszMsg, int hHelpId = 0,
		IErrorInfo* pErrInfo = NULL)
	{
		AssertPszN(pszMsg);
		m_hr = hr;
		m_stuMsg.Assign(pszMsg);
		m_hHelpId = hHelpId;
		m_qErrInfo = pErrInfo;
	}

	Throwable(HRESULT hr, const char * pszMsg, int hHelpId = 0,
		IErrorInfo* pErrInfo = NULL)
	{
		AssertPszN(pszMsg);
		m_hr = hr;
		m_stuMsg.Assign(pszMsg);
		m_hHelpId = hHelpId;
		m_qErrInfo = pErrInfo;
	}
#endif

	virtual ~Throwable(void)
	{
	}

	HRESULT Result(void) const
	{
		return m_hr;
	}

	HRESULT Error(void) const
	{
		if (FAILED(m_hr))
			return m_hr;
		return WarnHr(E_FAIL);
	}

#ifdef WIN32
	const wchar * Message() const
#else
	const OLECHAR* Message() const
#endif
	{
		return m_stuMsg.Chars();
	}
	int HelpId() const {return m_hHelpId;}

	IErrorInfo* GetErrorInfo() const { return m_qErrInfo; }

protected:
	HRESULT m_hr;
	StrUni m_stuMsg;
	int m_hHelpId;
	IErrorInfoPtr m_qErrInfo;
};

/*----------------------------------------------------------------------------------------------
	This variety of Throwable adds information about a stack dump.

	Non-inline methods are in StackDumper.cpp
----------------------------------------------------------------------------------------------*/
class ThrowableSd : public Throwable
{
public:
	const char * GetDump() {return m_staDump.Chars();}

	// Finding this constant in the Description of an ErrorInfo is a signal to
	// the FieldWorks error handler of an error message that contains information the
	// average user should not see. It should be displayed if "details" is clicked, and copied
	// to the clipboard.
#ifdef WIN32
	static const OLECHAR * MoreSep() {return (OLECHAR*)L"\n---***More***---\n";}
#else
	static const wchar_t * MoreSep() {return L"\n---***More***---\n";}
#endif

#ifdef WIN32
	ThrowableSd(HRESULT hr = S_OK, const wchar * pszMsg = NULL, int hHelpId = 0,
		const char * pszDump = NULL, IErrorInfo* pErrInfo = NULL)
		:Throwable(hr, pszMsg, hHelpId, pErrInfo), m_staDump(pszDump)
#else
	ThrowableSd(HRESULT hr = S_OK, const wchar_t * pszMsg = NULL, int hHelpId = 0,
		const char * pszDump = NULL, IErrorInfo* pErrInfo = NULL)
		:Throwable(hr, pszMsg, hHelpId, pErrInfo), m_staDump(pszDump)
#endif
	{}

#ifndef WIN32
	ThrowableSd(HRESULT hr, const OLECHAR * pszMsg, int hHelpId = 0,
		const char * pszDump = NULL, IErrorInfo* pErrInfo = NULL)
		:Throwable(hr, pszMsg, hHelpId, pErrInfo), m_staDump(pszDump)
	{}

	ThrowableSd(HRESULT hr, const char * pszMsg, int hHelpId = 0,
		const char * pszDump = NULL, IErrorInfo* pErrInfo = NULL)
		:Throwable(hr, pszMsg, hHelpId, pErrInfo), m_staDump(pszDump)
	{}
#endif

protected:
	StrAnsi m_staDump; // stack dump
};


/*----------------------------------------------------------------------------------------------
	Function to throw an HRESULT as a Throwable object.
----------------------------------------------------------------------------------------------*/
#ifdef WIN32
inline void ThrowHr(HRESULT hr, const wchar * pszMsg, int hHelpId, IErrorInfo* pErrInfo)
#else
template<class ZChar>
inline void ThrowHr(HRESULT hr, const ZChar* pszMsg, int hHelpId = 0, IErrorInfo* pErrInfo = NULL)
#endif
{
	// hHelpId == -1 means that an error info object is already in place. We shouldn't
	// create a new stack dump.
	if ((hr != E_INVALIDARG && hr != E_POINTER && hr != E_UNEXPECTED) || hHelpId == -1)
		throw Throwable(hr, pszMsg, hHelpId, pErrInfo);
	else // E_INVALIDARG || E_POINTER || E_UNEXPECTED
		ThrowInternalError(hr, pszMsg, hHelpId, pErrInfo);
}
#ifndef WIN32
inline void ThrowHr(HRESULT hr)
{
	throw Throwable(hr, (OLECHAR*)0, 0);
}
#endif


#endif //!STACK_DUMP_H
