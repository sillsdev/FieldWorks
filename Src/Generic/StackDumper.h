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

// Generate a stack dump, with the given message as header.
void DumpStackHere(char * pchMsg);
void TransFuncDump( unsigned int u, EXCEPTION_POINTERS * pExp);

#define MAXDUMPLEN 10000
class StackDumper
{
public:
	static void ShowStack( HANDLE hThread, CONTEXT& c, char * pszHdr = NULL); // dump a stack
	static void InitDump(char * pszHdr);
	static void AppendShowStack( HANDLE hThread, CONTEXT& c);
	static const char * GetDump();
	static HRESULT RecordError(REFGUID iid, StrUni stuDescr, StrUni stuSource,
		int hcidHelpId, StrUni stuHelpFile);
	~StackDumper();
protected:
	static bool EnumAndLoadModuleSymbols( HANDLE hProcess, DWORD pid );
	void ShowStackCore( HANDLE hThread, CONTEXT& c );
	int FindStartOfFrame(int ichStart);

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

/*----------------------------------------------------------------------------------------------
	Standard class to wrap an HRESULT, HelpID, and message.
----------------------------------------------------------------------------------------------*/
class Throwable
{
public:
	// Constructors and Destructor.
	Throwable(HRESULT hr = S_OK, const wchar * pszMsg = NULL, int hHelpId = 0,
		IErrorInfo* pErrInfo = NULL)
	{
		AssertPszN(pszMsg);
		m_hr = hr;
		m_stuMsg.Assign(pszMsg);
		m_hHelpId = hHelpId;
		m_qErrInfo = pErrInfo;
	}

	virtual ~Throwable(void)
	{
	}

	HRESULT Result(void)
	{
		return m_hr;
	}

	HRESULT Error(void)
	{
		if (FAILED(m_hr))
			return m_hr;
		return WarnHr(E_FAIL);
	}

	const wchar * Message()
	{
		return m_stuMsg.Chars();
	}
	int HelpId() {return m_hHelpId;}
	IErrorInfo* GetErrorInfo() { return m_qErrInfo; }

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
	// Finding this constant in the Description of an ErrorInfo is a signal to
	// the FieldWorks error handler of an error message that contains information the
	// average user should not see. It should be displayed if "details" is clicked, and copied
	// to the clipboard.
	static const OLECHAR * MoreSep() {return L"\n---***More***---\n";}
	const char * GetDump() {return m_staDump.Chars();}

	ThrowableSd(HRESULT hr = S_OK, const wchar * pszMsg = NULL, int hHelpId = 0,
		const char * pszDump = NULL, IErrorInfo* pErrInfo = NULL)
		:Throwable(hr, pszMsg, hHelpId, pErrInfo), m_staDump(pszDump)
	{}
protected:
	StrAnsi m_staDump; // stack dump
};


/*----------------------------------------------------------------------------------------------
	Function to throw an HRESULT as a Throwable object.
----------------------------------------------------------------------------------------------*/
inline void ThrowHr(HRESULT hr, const wchar * pszMsg, int hHelpId, IErrorInfo* pErrInfo)
{
	// hHelpId == -1 means that an error info object is already in place. We shouldn't
	// create a new stack dump.
	if ((hr != E_INVALIDARG && hr != E_POINTER && hr != E_UNEXPECTED) || hHelpId == -1)
		throw Throwable(hr, pszMsg, hHelpId, pErrInfo);
	else // E_INVALIDARG || E_POINTER || E_UNEXPECTED
		ThrowInternalError(hr, pszMsg, hHelpId, pErrInfo);
}


#endif //!STACK_DUMP_H
