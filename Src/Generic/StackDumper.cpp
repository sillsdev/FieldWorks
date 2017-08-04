/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: SampleInterface.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	Supports stack dumping using ImageHelp.Dll

Modified: 24 Apr 2008 	- Non Win32 null implementation that attempted to preserve interface
			- TODO-Linux: - could unix stack dump functionality be implemented?
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

StackDumper s_dumper;

/*----------------------------------------------------------------------------------------------
	Generate a dump of the stack in the given context, starting with the given header if any.
----------------------------------------------------------------------------------------------*/
void StackDumper::ShowStack(HANDLE hThread, CONTEXT& c, SDCHAR * pszHdr)
{
	InitDump(pszHdr);
	s_dumper.ShowStackCore(hThread, c);
}

/*----------------------------------------------------------------------------------------------
	Prepare for AppendShowStack by setting the header in the stack dump.
----------------------------------------------------------------------------------------------*/
void StackDumper::InitDump(SDCHAR * pszHdr)
{
	if (!s_dumper.m_pstaDump)
	{
		try
		{
			s_dumper.m_pstaDump = NewObj StrAnsiBufHuge;
		}
		catch (...)
		{
			return;
		}
	}

	if (pszHdr)
		s_dumper.m_pstaDump->Assign(pszHdr);
}

/*----------------------------------------------------------------------------------------------
	Generate a dump of the stack in the given context. Append this to anything already in the
	dump buffer (typically a header set by InitDump).
----------------------------------------------------------------------------------------------*/
void StackDumper::AppendShowStack( HANDLE hThread, CONTEXT& c)
{
	s_dumper.ShowStackCore(hThread, c);
}

const char * StackDumper::GetDump()
{
	if (s_dumper.m_pstaDump)
		return s_dumper.m_pstaDump->Chars();
	return NULL;
}

StackDumper::StackDumper()
	: m_pstaDump(NULL)
{

}

StackDumper::~StackDumper()
{
	if (m_pstaDump)
		delete m_pstaDump;
	m_pstaDump = NULL;
}

// Find the start of a frame, at least ichStart characters into m_pstaDump.
// For now two newlines is a good marker.
int StackDumper::FindStartOfFrame(int ichStart)
{
	Assert(ichStart < m_pstaDump->Length());
	// This is very Windows-dependent. It assumes newlines are represented as crlf pairs.
	int ichRet = m_pstaDump->FindCh('\r', ichStart);
	// Reinstate this if doing multi-line frames with double newline between.
	//int cch = m_pstaDump->Length();
	//while (ichRet > 0 && ichRet + 2 < cch && m_pstaDump->GetAt(ichRet + 1) != '\r\n')
	//	ichRet = m_pstaDump->FindCh('\r\n', ichRet + 1);

	// This is very unlikely to happen, but if we can't find a stack frame we will
	// just delete to some arbitrary position.
	if (ichRet < 0)
		return ichStart + 1;
	return ichRet;
}

/*----------------------------------------------------------------------------------------------
	This function can be used as an __except filter to generate a stack dump and then
	continue to execute the __except clause.
----------------------------------------------------------------------------------------------*/
DWORD Filter( EXCEPTION_POINTERS *ep )
{
#ifdef WIN32
	HANDLE hThread;

	DuplicateHandle( GetCurrentProcess(), GetCurrentThread(),
		GetCurrentProcess(), &hThread, 0, false, DUPLICATE_SAME_ACCESS );
	StackDumper::AppendShowStack( hThread, *(ep->ContextRecord) );
	CloseHandle( hThread );

	return EXCEPTION_EXECUTE_HANDLER;
#else
	return 0;
#endif
}


/*----------------------------------------------------------------------------------------------
	This function can be used as an __except filter to generate a stack dump and then
	continue execution as if the error had not happened. It is currently not used.
----------------------------------------------------------------------------------------------*/
DWORD FilterContinue( EXCEPTION_POINTERS *ep )
{
#ifdef WIN32
	HANDLE hThread;

	DuplicateHandle( GetCurrentProcess(), GetCurrentThread(),
		GetCurrentProcess(), &hThread, 0, false, DUPLICATE_SAME_ACCESS );
	StackDumper::AppendShowStack( hThread, *(ep->ContextRecord) );
	CloseHandle( hThread );

	return (DWORD) EXCEPTION_CONTINUE_EXECUTION;
#else
	return 0;
#endif
}


/*----------------------------------------------------------------------------------------------
	This function can be installed to translate windows exceptions into C++ internal error
	exceptions. Only the main program should do this. To install, just call
	_set_se_translator(TransFuncDump);.

	This is of dubious value because if the error occurs inside a COM component, the Throwable
	exception will not be recognized, and exception handling will just catch "..." and generate
	a new stack dump. However, installing it at least achieves better stack dumps for errors
	in code linked into the main program.

	We could install at the start of every COM interface method, and restore upon return.
	However this would be computationally expensive. Consider doing this in a particular method
	if you are trying to track down a problem in that method.

	We could have each module install this function on being loaded, and check whether the
	error occurs in its own code, and if not call the previous error handler. But this
	only works reliably if modules are never unloaded, or only in reverse order of loading,
	which I don't see how to ensure.

	We could also get really fancy, with some sort of central manager which knows which error
	translator to use for each module. This has not seemed worthwhile to me so far.
----------------------------------------------------------------------------------------------*/
void TransFuncDump( unsigned int u, EXCEPTION_POINTERS * pExp)
{
#ifdef WIN32
	HANDLE hThread;
	DWORD dwCode = pExp->ExceptionRecord->ExceptionCode;
	StrUni stuException = ConvertException(dwCode);
	DuplicateHandle( GetCurrentProcess(), GetCurrentThread(),
		GetCurrentProcess(), &hThread, 0, false, DUPLICATE_SAME_ACCESS );
	StrAnsi staMsg;
	staMsg.Format("Stack Dump for exception: %S (%d)", stuException.Chars(), dwCode);
	StackDumper::ShowStack( hThread, *(pExp->ContextRecord), const_cast<char *>(staMsg.Chars()) );
	CloseHandle( hThread );
	StrUni stuMsg(staMsg.Chars());
	throw ThrowableSd(E_UNEXPECTED, stuMsg.Chars(), 0, StackDumper::GetDump());
#endif
}

/*----------------------------------------------------------------------------------------------
	Generate a dump of the stack at the point where this is called.
----------------------------------------------------------------------------------------------*/
void DumpStackHere(SDCHAR * pszMsg)
{
#ifdef WIN32
		StackDumper::InitDump(pszMsg);
	__try
	{
		//RaiseException(0, 0, 0, NULL); // Just to get FilterContiue called.
		// Make a deliberate exception. For some reason, if we use RaiseException as above,
		// the code crashes while returning from the exception handler. The exception won't
		// be reported to the end user (since it is caught right here), but unfortunately
		// it will show up in the debugger if you have it set to stop always.
		// Enhance JohnT: maybe we could cause a less drastic exception that most people
		// don't want to stop always?
		char * pch = NULL;
		*pch = 3;
	}
	__except ( Filter( GetExceptionInformation() ) )
	{
	}
#else
	CONTEXT unused;
	StackDumper::ShowStack(NULL, unused, pszMsg);
#endif
}

/*----------------------------------------------------------------------------------------------
	Throw an exception of type ThrowableSd, with a stack dump. This may eventually replace
	ThrowHr in many or most places.
----------------------------------------------------------------------------------------------*/
void ThrowInternalError(HRESULT hr, const wchar_t * pszMsg, int hHelpId, IErrorInfo* pErrInfo)
{
	DumpStackHere("Stack Dump:\r\n");
	throw ThrowableSd(hr, pszMsg, hHelpId, StackDumper::GetDump(), pErrInfo);
}

#if !WIN32
/*----------------------------------------------------------------------------------------------
	Throw an exception of type ThrowableSd, with a stack dump. This may eventually replace
	ThrowHr in many or most places.
----------------------------------------------------------------------------------------------*/
void ThrowInternalError(HRESULT hr, const OLECHAR * pszMsg, int hHelpId, IErrorInfo* pErrInfo)
{
	DumpStackHere("Stack Dump:\r\n");
	throw ThrowableSd(hr, pszMsg, hHelpId, StackDumper::GetDump(), pErrInfo);
}
#endif

/*----------------------------------------------------------------------------------------------
	Throw an exception of type ThrowableSd, with a stack dump. This may eventually replace
	ThrowHr in many or most places.
----------------------------------------------------------------------------------------------*/
void ThrowInternalError(HRESULT hr, const char * pszMsg, int hHelpId, IErrorInfo* pErrInfo)
{
	DumpStackHere("Stack Dump:\r\n");
	StrUni stuMsg(pszMsg);
	throw ThrowableSd(hr, stuMsg.Chars(), hHelpId, StackDumper::GetDump(), pErrInfo);
}


/*----------------------------------------------------------------------------------------------
	Throw an OUT_OF_MEMORY exception. This is called out as a method with the thought that one
	day we may implement a strategy for recovering some before we try to report the problem.
	Or, we may need to hold a memory reserve to guarantee we can show the dialog and save.
----------------------------------------------------------------------------------------------*/
void ThrowOutOfMemory()
{
	ThrowHr(WarnHr(E_OUTOFMEMORY));
}

/*----------------------------------------------------------------------------------------------
	Throw an exception indicating UNEXPECTED failure of a built-in function. This should be
	used only when we believe there is no reason ever to expect failure, but are checking it
	just to be sure. For now treat it as an internal error.
----------------------------------------------------------------------------------------------*/
void ThrowBuiltInError(char * pchFnName)
{
	StrUni stuMsg;
	stuMsg.Format(L"%S unexpectedly failed", pchFnName);
	ThrowInternalError(E_UNEXPECTED, stuMsg.Chars());
}

/*----------------------------------------------------------------------------------------------
	Throw an exception indicating an expected failure. Generate a nice error message by
	looking up the resource string indicated.
	ENHANCE: should there be another version of this method for where we need a distinct help
	ID, or don't have one?
----------------------------------------------------------------------------------------------*/
void ThrowNice(HRESULT hr, int hid)
{
	StrUni stuMsg(hid);
	ThrowHr(hr, stuMsg.Chars(), hid);
}


/*----------------------------------------------------------------------------------------------
	This method is called by CheckHr (with punk and iid null) or CheckExtHr, after confirming
	that hrErr is an error HRESULT.

	It first confirms that there is an error info object available. If punk and iid
	are supplied, it further checks that the error info object is relevant to that object and
	interface.

	If a relevant error object is available, and it indicates that a programming error has
	occurred, and its Description does not yet contain a stack dump, we add one if possible.
	Then we ThrowHr, with a special HelpId to indicate that HandleThrowable need not generate
	a new error object.

	If no relevant error object is available, we generate a stack dump and treat the problem
	as an internal error.
----------------------------------------------------------------------------------------------*/
void CheckHrCore(HRESULT hrErr)
{
	IErrorInfoPtr qerrinfo;
	::GetErrorInfo(0, &qerrinfo); // This clears the system wide error info
	if (!qerrinfo)
	{
		// We didn't have any (relevant) error info
		ThrowInternalError(hrErr);
	}

	SmartBstr sbstrDesc;
	qerrinfo->GetDescription(&sbstrDesc);

	// We have an error info object, and presume that it is relevant.
	// If it indicates a programming error, and doesn't already contain a
	// stack dump, try to add one.
	if (hrErr == E_INVALIDARG || hrErr == E_POINTER || hrErr == E_UNEXPECTED)
	{
		// If so look for stack dump type info.
		std::wstring strDesc = sbstrDesc;
		if (!wcsstr(strDesc.c_str(), ThrowableSd::MoreSep()))
		{
			// no stack there, so add one
			DumpStackHere("Error was detected by CheckHr here:\r\n");
			StrUni stuDescNew;
			stuDescNew.Format(L"%s%s%S", sbstrDesc.Chars(), ThrowableSd::MoreSep(),
				StackDumper::GetDump());
			sbstrDesc.Append(const_cast<OLECHAR *>(stuDescNew.Chars()));

			// Now modify the error info
			ICreateErrorInfoPtr qcerrinfo;
			if (SUCCEEDED(qerrinfo->QueryInterface(IID_ICreateErrorInfo, (LPVOID FAR*) &qcerrinfo)))
				qcerrinfo->SetDescription(sbstrDesc);
		}
	}
	// Throw an error indicating there is already a good error object in place.
	ThrowHr(hrErr, sbstrDesc.Bstr(), -1, qerrinfo);
}

/*----------------------------------------------------------------------------------------------
	Get the version information for a module. It is passed the path name to the module.
----------------------------------------------------------------------------------------------*/
StrUni GetModuleVersion(const OLECHAR * pchPathName)
{
	StrUni stuRet;
#ifdef WIN32
	StrApp staPathName = pchPathName;
	achar * pchaPathName = const_cast<achar *>(staPathName.Chars());

	DWORD dwDum; // Always set to zero.
	DWORD cb = GetFileVersionInfoSize(pchaPathName, &dwDum);

	LPVOID pBlock = (LPVOID) _alloca(cb);

	if (!GetFileVersionInfo(pchaPathName, 0, cb, pBlock))
		return stuRet; // Can't get requested info

	VS_FIXEDFILEINFO * pffi;
	uint nT;
	::VerQueryValue(pBlock, _T("\\"), (void **)&pffi, &nT);

	stuRet.Format(L"Version: %d, %d, %d, %d", HIWORD(pffi->dwFileVersionMS),
		LOWORD(pffi->dwFileVersionMS), HIWORD(pffi->dwFileVersionLS),
		LOWORD(pffi->dwFileVersionLS));
#endif
	return stuRet;
}
/*----------------------------------------------------------------------------------------------
	Get a path name for the help file for this module.
	By convention this is the path of the DLL changed to .chm.
----------------------------------------------------------------------------------------------*/
StrUni GetModuleHelpFilePath()
{
	StrUni stuHelpPath = ModuleEntry::GetModulePathName();
	int cch = stuHelpPath.Length();
	int ichStartRep = cch; // Add to end if no dot.
	if (cch >= 3 && stuHelpPath[cch - 4] == '.')
		ichStartRep = cch - 3;
	stuHelpPath.Replace(ichStartRep, cch, L"chm");
	return stuHelpPath;
}

// Error objects used to report out-of-memory conditions. These are pre-created so we don't
// have to use memory to create them when we need to make the report.
ICreateErrorInfoPtr s_qcerrinfoMem;
IErrorInfoPtr s_qerrinfoMem;

/*----------------------------------------------------------------------------------------------
	This class and the following instance arrange to create an "out-of-memory" error object
	that we can use if we ever run out of memory without allocating more.
	Fill in as much as we can, and defaults for the rest, in case there isn't enough memory
	to update them when we want it.
----------------------------------------------------------------------------------------------*/
class MemMsgMaker : public ModuleEntry
{
	virtual void ProcessAttach(void)
	{
		HRESULT hr;

		if (FAILED(hr = ::CreateErrorInfo(&s_qcerrinfoMem)))
			ThrowInternalError(hr);
		if (FAILED(hr = s_qcerrinfoMem->QueryInterface(IID_IErrorInfo,
			(LPVOID FAR*) &s_qerrinfoMem)))
		{
			// We should have plenty of memory as we start up components!
			ThrowInternalError(hr);
		}
		StrUni stuDesc(kstidOutOfMemory);
		s_qcerrinfoMem->SetDescription(stuDesc.Bstr());
		// We can't set the IID or source yet.
		s_qcerrinfoMem->SetHelpFile(const_cast<OLECHAR *>(GetModuleHelpFilePath().Chars()));
		s_qcerrinfoMem->SetHelpContext(khcidHelpOutOfMemory);
	}
};

static MemMsgMaker mmm; // existence of an instance gets the above called.

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HRESULT HandleDefaultException(REFGUID iid, DummyFactory * pfact)
{
	DumpStackHere("Unknown exception caught in COM method\r\n");
	ThrowableSd thr(E_UNEXPECTED, reinterpret_cast<const wchar *>(NULL), 0, StackDumper::GetDump());
	return HandleThrowable(thr, iid, pfact);
}

/*----------------------------------------------------------------------------------------------
// This method is called when a Throwable exception is caught at the end of a COM method,
// or (with a dummy ThrowableSd) when some other exception is caught. It transforms the info
// in the Throwable into a standard COM error report (creating an IErrorInfo and registering
// it.) It returns the HRESULT that should be returned by the COM method.

// There are several different situations which this method has to handle. The following
// comments describe the situations, and how they are indicated. Each "indication" presumes
// that the previous "indications" failed.
// 1. We called a COM method which supports IErrorInfo and already provides all the information
//		we need to pass to our own caller. This is indicated by a help ID of -1.
// 2. We, or a method we called that doesn't support IErrorInfo, ran out of memory.
//		We need to set up the special error object pre-created for this case. This is
//		indicated by thr.Error() being E_OUTOFMEMORY.
// 3. A programming error has been caught and a stack dump generated, either in our own code
//		or in the code that called us. This is indicated by finding that thr is actually
//		a ThrowableSd. Make an error object, with a description that includes the stack dump.
----------------------------------------------------------------------------------------------*/
HRESULT HandleThrowable(Throwable & thr, REFGUID iid, DummyFactory * pfact)
{
	StrUni stuDesc;
	HRESULT hrErr = thr.Error();
	HRESULT hr;

	// If we already have error info, we set it again (very likely got cleared by previous
	// CheckHr), and then just return the HRESULT.
	if (thr.GetErrorInfo())
	{
		::SetErrorInfo(0, thr.GetErrorInfo());
		return hrErr;
	}

	// We need a Unicode version of the ProgId, but we should avoid allocating memory
	// since we don't yet know that we have not run out.
	// Since all our progids are in simple ascii, we can do the simplest possible conversion.
	// Since we hopefully have at least a little stack to work with, use _alloca.
	OLECHAR * pszSrc = (OLECHAR *)_alloca((StrLen(pfact->GetProgId()) + 1) * isizeof(OLECHAR));
	OLECHAR * pchw = pszSrc;
	for (const TCHAR * pch = pfact->GetProgId(); *pch; pch++, pchw++)
		*pchw = *pch;
	*pchw = 0;

	if (hrErr == E_OUTOFMEMORY)
	{
		// Use the pre-created error info object so we don't have to allocate now.
		// It already has a description, help file path, and help context ID.
		// If a further E_OUTOFMEMORY occurs calling SetGUID or SetSource, just ignore it.
		s_qcerrinfoMem->SetGUID(iid);
		s_qcerrinfoMem->SetSource(pszSrc);
		SetErrorInfo(0, s_qerrinfoMem);
		return hrErr;
	}

	// Otherwise we are going to make a new error info object.

	// Get any message supplied by the Throwable.
	StrUni stuUserMsg(thr.Message());
	// See if a stack dump is available.
	ThrowableSd * pthrs = dynamic_cast<ThrowableSd *>(&thr);
	char * pchDump = NULL;
	if (pthrs)
		pchDump = const_cast<char *>(pthrs->GetDump());
	else if (!stuUserMsg.Length())
	{
		// If we don't have any sort of nice message, treat it as an internal error.
		DumpStackHere("HandleThrowable caught an error with no description");
		pchDump = const_cast<char *>(StackDumper::GetDump());
	}
	if (pchDump)
	{
		// We have a stack dump.
		StrUni stuModName = ModuleEntry::GetModulePathName();

		// Do we already have a description? If not make one.
		if (!stuUserMsg.Length())
		{
			// No, use a default one.
			StrUni stuHrMsg = ConvertException((DWORD)hrErr);

			StrUni stuUserMsgFmt;
			stuUserMsgFmt.Load(kstidInternalError);
			// Would it be better to strip off the path?
			stuUserMsg.Format(stuUserMsgFmt, stuHrMsg.Chars(), stuModName.Chars());
		}
		stuDesc.Format(L"%s%s%S\r\n\r\n%s", stuUserMsg.Chars(), ThrowableSd::MoreSep(), pchDump,
			GetModuleVersion(stuModName.Chars()).Chars());
	}
	else
	{
		// We've made sure we have a message already; use it.
		stuDesc = stuUserMsg;
	}

	StrUni stuSource(pszSrc);
	hr = StackDumper::RecordError(iid, stuDesc, stuSource, thr.HelpId(),
		GetModuleHelpFilePath());
	if (FAILED(hr))
	{
		if (hr == E_OUTOFMEMORY)
		{
			Throwable thr2(E_OUTOFMEMORY);
			return HandleThrowable(thr2, iid, pfact);
		}

		// just report the failure to the developer
		WarnHr(hr);

		// Hard to know what do do here. It should never happen. For paranoia's sake at least
		// return the original problem.
	}
	return hrErr;
}

/*----------------------------------------------------------------------------------------------
	General purpose method to set up and store an error, rather than throw an exception
	in the normal way.
----------------------------------------------------------------------------------------------*/
HRESULT StackDumper::RecordError(REFGUID iid, StrUni stuDescr, StrUni stuSource,
	int hcidHelpId, StrUni stuHelpFile)
{
	// We are going to make a new error info object.
	ICreateErrorInfoPtr qcerrinfo;
	IErrorInfoPtr qerrinfo;
	HRESULT hr;

	// If we can't get a new error object, the only documented cause is E_OUTOFMEMORY.
	if (FAILED(hr = ::CreateErrorInfo(&qcerrinfo)))
	{
		return E_OUTOFMEMORY;
	}
	if (FAILED(hr = qcerrinfo->QueryInterface(IID_IErrorInfo, (LPVOID FAR*) &qerrinfo)))
	{
		return E_UNEXPECTED;
	}

	hr = qcerrinfo->SetDescription(const_cast<OLECHAR *>(stuDescr.Chars()));
	if (FAILED(hr))
		return hr;
	hr = qcerrinfo->SetGUID(iid);
	if (FAILED(hr))
		return hr;
	hr = qcerrinfo->SetSource(const_cast<OLECHAR *>(stuSource.Chars()));
	if (FAILED(hr))
		return hr;
	hr = qcerrinfo->SetHelpFile(const_cast<OLECHAR *>(stuHelpFile.Chars()));
	if (FAILED(hr))
		return hr;
	if (!hcidHelpId)
		hcidHelpId = khcidNoHelpAvailable;
	hr = qcerrinfo->SetHelpContext(hcidHelpId);
	if (FAILED(hr))
		return hr;

	::SetErrorInfo(0, qerrinfo);
	return hr;
}
