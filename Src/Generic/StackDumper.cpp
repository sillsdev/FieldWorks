/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

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

#define gle (GetLastError())
#define lenof(a) (sizeof(a) / sizeof((a)[0]))
#define MAXNAMELEN 1024 // max name length for found symbols
#define IMGSYMLEN ( sizeof IMAGEHLP_SYMBOL )
#define TTBUFLEN 65536 // for a temp buffer

// Add the given string to Sta. If Sta is not empty, add a semi-colon first
void AppendToStaWithSep(StrApp sta, const achar * pch)
{
	if (sta.Length())
		sta.Append(";");
	sta.Append(pch);
}

/*----------------------------------------------------------------------------------------------
	Generate a dump of the stack in the given context, starting with the given header if any.
----------------------------------------------------------------------------------------------*/
void StackDumper::ShowStack( HANDLE hThread, CONTEXT& c, char * pszHdr)
{
#ifdef WIN32
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

	s_dumper.ShowStackCore(hThread, c);
#endif
}

/*----------------------------------------------------------------------------------------------
	Prepare for AppendShowStack by setting the header in the stack dump.
----------------------------------------------------------------------------------------------*/
void StackDumper::InitDump(char * pszHdr)
{
#ifdef WIN32
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

	s_dumper.m_pstaDump->Assign(pszHdr);
#endif
}

/*----------------------------------------------------------------------------------------------
	Generate a dump of the stack in the given context. Append this to anything already in the
	dump buffer (typically a header set by InitDump).
----------------------------------------------------------------------------------------------*/
void StackDumper::AppendShowStack( HANDLE hThread, CONTEXT& c)
{
#ifdef WIN32
	s_dumper.ShowStackCore(hThread, c);
#endif
}

#ifdef WIN32

/*----------------------------------------------------------------------------------------------
	Translate the more common C++ exceptions into (somewhat) user-friendly strings--at least
	programmer-friendly.  This is based on the August-98 version of Bugslayer, which uses
	(but does not explain) this function before trying FormatMessage.
	I suspect some of these are never going to show up in FieldWorks (for example, I certainly
	hope page fault doesn't ever show up as an internal error!) but have left them in just in
	case. Some I (JohnT) don't even know the meaning of.
----------------------------------------------------------------------------------------------*/
OLECHAR * ConvertSimpleException(DWORD dwExcept)
{
	switch (dwExcept){
	case EXCEPTION_ACCESS_VIOLATION:
		return (L"Access violation");
		break ;

	case EXCEPTION_DATATYPE_MISALIGNMENT:
		return (L"Data type misalignment");
		break;

	case EXCEPTION_BREAKPOINT:
		return (L"Breakpoint");
		break;

	case EXCEPTION_SINGLE_STEP:
		return (L"Single step");
		break;

	case EXCEPTION_ARRAY_BOUNDS_EXCEEDED:
		return (L"Array bounds exceeded");
		break;

	case EXCEPTION_FLT_DENORMAL_OPERAND:
		return (L"FLT_DENORMAL_OPERAND");
		break;

	case EXCEPTION_FLT_DIVIDE_BY_ZERO:
		return (L"Float div by zero");
		break;

	case EXCEPTION_FLT_INEXACT_RESULT:
		return (L"Float inexact result");
		break;

	case EXCEPTION_FLT_INVALID_OPERATION:
		return (L"Float invalid operation");
		break;

	case EXCEPTION_FLT_OVERFLOW:
		return (L"Float overflow");
		break;

	case EXCEPTION_FLT_STACK_CHECK:
		return (L"Float stack check");
		break;

	case EXCEPTION_FLT_UNDERFLOW:
		return (L"Float underflow");
		break;

	case EXCEPTION_INT_DIVIDE_BY_ZERO:
		return (L"Divide by zero");
		break;

	case EXCEPTION_INT_OVERFLOW:
		return (L"INT_OVERFLOW");
		break;

	case EXCEPTION_PRIV_INSTRUCTION:
		return (L"Privileged instruction");
		break;

	case EXCEPTION_IN_PAGE_ERROR:
		return (L"IN_PAGE_ERROR");
		break;

	case EXCEPTION_ILLEGAL_INSTRUCTION:
		return (L"Illegal instruction");
		break;

	case EXCEPTION_NONCONTINUABLE_EXCEPTION:
		return (L"Noncontinuable exception");
		break;

	case EXCEPTION_STACK_OVERFLOW:
		return (L"Stack overflow");
		break;

	case EXCEPTION_INVALID_DISPOSITION:
		return (L"Invalid disposition");
		break;

	case EXCEPTION_GUARD_PAGE:
		return (L"Guard page");
		break ;

	case EXCEPTION_INVALID_HANDLE:
		return (L"Invalid handle");
		break;

	default:
		return (NULL);
		break;
	}
}

StrUni ConvertException(DWORD dwExcept)
{
	StrUni stuResult;
	OLECHAR * pszSimple = ConvertSimpleException(dwExcept);

	if (NULL != pszSimple)
	{
		stuResult = pszSimple;
	}
	else
	{
		LPTSTR lpstrMsgBuf;
		::FormatMessage( FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
			NULL,
			dwExcept,
			0, // smart search for useful languages
			reinterpret_cast<achar *>(&lpstrMsgBuf),
			0,
			NULL);
		stuResult = lpstrMsgBuf;
		int cch = stuResult.Length();
		if (cch > 1 && stuResult[cch - 2] == '\r')
			stuResult.Replace(cch - 2, cch, (OLECHAR *)NULL);

		// Free the buffer.
		::LocalFree( lpstrMsgBuf );
	}
	return stuResult;
}

#endif //end ifdef WIN32

const char * StackDumper::GetDump()
{
#ifdef WIN32
	if (s_dumper.m_pstaDump)
		return s_dumper.m_pstaDump->Chars();
#endif
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

#ifdef WIN32

typedef
BOOL (__stdcall * PFNSYMGETLINEFROMADDR)
						   ( IN  HANDLE         hProcess         ,
							 IN  DWORD          dwAddr           ,
							 OUT PDWORD         pdwDisplacement  ,
							 OUT PIMAGEHLP_LINE Line              ) ;

// The pointer to the SymGetLineFromAddr function I GetProcAddress out
//  of IMAGEHLP.DLL in case the user has an older version that does not
//  support the new extensions.
PFNSYMGETLINEFROMADDR g_pfnSymGetLineFromAddr = NULL;
#endif

void StackDumper::ShowStackCore( HANDLE hThread, CONTEXT& c )
{
#ifdef WIN32
	// This makes this code custom for 32-bit windows. There is a technique to find out what
	// machine type we are running on, but this should do us for a good while.
	DWORD imageType = IMAGE_FILE_MACHINE_I386;

	HANDLE hProcess = GetCurrentProcess();
	int frameNum; // counts walked frames
	DWORD offsetFromSymbol; // tells us how far from the symbol we were
	DWORD symOptions; // symbol handler settings
	IMAGEHLP_SYMBOL *pSym = (IMAGEHLP_SYMBOL *) malloc( IMGSYMLEN + MAXNAMELEN );
	IMAGEHLP_MODULE Module;
	IMAGEHLP_LINE Line;
	StrApp strSearchPath; // path to search for symbol tables (I think...JT)
	achar *tt = 0;

	STACKFRAME s; // in/out stackframe
	memset( &s, '\0', sizeof s );

	tt = new achar[TTBUFLEN];
	if (!tt)
		return;

	// Build symbol search path.
	// Add current directory
	if (::GetCurrentDirectory( TTBUFLEN, tt ) )
		AppendToStaWithSep(strSearchPath, tt);
	// Add directory containing executable or DLL we are running in.
	if (::GetModuleFileName( 0, tt, TTBUFLEN ) )
	{
		StrUni stuPath = tt; // convert to Unicode if necessary, allows use of wchars
		const OLECHAR * pchPath =  stuPath.Chars();

		const OLECHAR * pch;
		for (pch = pchPath + wcslen(pchPath) - 1; pch >= pchPath; -- pch )
		{
			// locate the rightmost path separator
			if ( *pch == L'\\' || *pch == L'/' || *pch == L':' )
				break;
		}
		// if we found one, p is pointing at it; if not, tt only contains
		// an exe name (no path), and p points before its first byte
		if ( pch != pchPath ) // path sep found?
		{
			if ( *pch == L':' ) // we leave colons in place
				++ pch;
			if (strSearchPath.Length())
				strSearchPath.Append(";");
			strSearchPath.Append(pchPath, (pch - pchPath));
		}
	}
	// environment variable _NT_SYMBOL_PATH
	if (::GetEnvironmentVariable( _T("_NT_SYMBOL_PATH"), tt, TTBUFLEN ))
		AppendToStaWithSep(strSearchPath, tt);
	// environment variable _NT_ALTERNATE_SYMBOL_PATH
	if (::GetEnvironmentVariable( _T("_NT_ALTERNATE_SYMBOL_PATH"), tt, TTBUFLEN ))
		AppendToStaWithSep(strSearchPath, tt);
	// environment variable SYSTEMROOT
	if (::GetEnvironmentVariable( _T("SYSTEMROOT"), tt, TTBUFLEN ))
		AppendToStaWithSep(strSearchPath, tt);

	// Why oh why does SymInitialize() want a writeable string? Surely it doesn't modify it...
	// The doc clearly says it is an [in] parameter.
	// Also, there is not a wide character version of this function!
	StrAnsi staT(strSearchPath);
	if ( !::SymInitialize( hProcess, const_cast<char *>(staT.Chars()), false ) )
		goto LCleanup;

	// SymGetOptions()
	symOptions = SymGetOptions();
	symOptions |= SYMOPT_LOAD_LINES;
	symOptions &= ~SYMOPT_UNDNAME;
	SymSetOptions( symOptions ); // SymSetOptions()

	// Enumerate modules and tell imagehlp.dll about them.
	// On NT, this is not necessary, but it won't hurt.
	EnumAndLoadModuleSymbols( hProcess, GetCurrentProcessId() );

	// init STACKFRAME for first call
	// Notes: AddrModeFlat is just an assumption. I hate VDM debugging.
	// Notes: will have to be #ifdef-ed for Alphas; MIPSes are dead anyway,
	// and good riddance.
	s.AddrPC.Offset = c.Eip;
	s.AddrPC.Mode = AddrModeFlat;
	s.AddrFrame.Offset = c.Ebp;
	s.AddrFrame.Mode = AddrModeFlat;

	memset( pSym, '\0', IMGSYMLEN + MAXNAMELEN );
	pSym->SizeOfStruct = IMGSYMLEN;
	pSym->MaxNameLength = MAXNAMELEN;

	memset( &Line, '\0', sizeof Line );
	Line.SizeOfStruct = sizeof Line;

	memset( &Module, '\0', sizeof Module );
	Module.SizeOfStruct = sizeof Module;

	offsetFromSymbol = 0;

	if (!m_pstaDump)
	{
		try
		{
			m_pstaDump = NewObj StrAnsiBufHuge;
		}
		catch (...)
		{
			goto LCleanup;
		}
	}

	// If the stack dump gets too big, we remove some entries from near the
	// middle, and insert a marker. This counts the characters up to the
	// end of the marker.
	int ichEndLowHalf;
	ichEndLowHalf = 0;

	m_pstaDump->FormatAppend( "\r\n--# FV EIP----- RetAddr- FramePtr StackPtr Symbol\r\n" );
	// EberhardB: a stack of 1.000 frames should be enough in most cases; limiting it
	// prevents a mysterious infinite(?) loop on our build machine.
	for ( frameNum = 0; frameNum < 1000; ++ frameNum )
	{
		// get next stack frame (StackWalk(), SymFunctionTableAccess(), SymGetModuleBase())
		// if this returns ERROR_INVALID_ADDRESS (487) or ERROR_NOACCESS (998), you can
		// assume that either you are done, or that the stack is so hosed that the next
		// deeper frame could not be found.
		if ( ! StackWalk( imageType, hProcess, hThread, &s, &c, NULL,
			SymFunctionTableAccess, SymGetModuleBase, NULL ) )
			break;

		// display its contents
		m_pstaDump->FormatAppend( "%3d %c%c %08x %08x %08x %08x ",
			frameNum, s.Far? 'F': '.', s.Virtual? 'V': '.',
			s.AddrPC.Offset, s.AddrReturn.Offset,
			s.AddrFrame.Offset, s.AddrStack.Offset );

		if ( s.AddrPC.Offset == 0 )
		{
			m_pstaDump->Append( "(-nosymbols- PC == 0)\r\n" );
		}
		else
		{ // we seem to have a valid PC
			char undName[MAXNAMELEN]; // undecorated name
			//char undFullName[MAXNAMELEN]; // undecorated name with all shenanigans
			// show procedure info (SymGetSymFromAddr())
			if ( ! SymGetSymFromAddr( hProcess, s.AddrPC.Offset, &offsetFromSymbol, pSym ) )
			{
				if ( gle != 487 )
					m_pstaDump->FormatAppend( "SymGetSymFromAddr(): gle = %u\r\n", gle );
			}
			else
			{
				UnDecorateSymbolName( pSym->Name, undName, MAXNAMELEN, UNDNAME_NAME_ONLY );
				//UnDecorateSymbolName( pSym->Name, undFullName, MAXNAMELEN, UNDNAME_COMPLETE );
				m_pstaDump->Append( undName );
				//if ( offsetFromSymbol != 0 )
				//	m_pstaDump->FormatAppend( " %+d bytes", offsetFromSymbol );
				//m_pstaDump->FormatAppend( "\r\n    Sig:  %s\r\n", pSym->Name );
				//m_pstaDump->FormatAppend( "\r\n    Decl: %s\r\n", undFullName );
			}

			// show line number info, NT5.0-method (SymGetLineFromAddr()). If we can't get this function,
			// or it doesn't work, leave out line number info.
			if (! g_pfnSymGetLineFromAddr)
			{
				StrApp staModName("IMAGEHLP.DLL");
				g_pfnSymGetLineFromAddr = (PFNSYMGETLINEFROMADDR) GetProcAddress(
					GetModuleHandle(staModName.Chars()), "SymGetLineFromAddr");
			}
			if (! g_pfnSymGetLineFromAddr ||
				! g_pfnSymGetLineFromAddr( hProcess, s.AddrPC.Offset, &offsetFromSymbol, &Line ) )
			{
				if ( g_pfnSymGetLineFromAddr && gle != 487 ) // apparently a magic number indicating not in symbol file.
					m_pstaDump->FormatAppend( "SymGetLineFromAddr(): gle = %u\r\n", gle );
				else
					m_pstaDump->FormatAppend( "   (no line # avail)\r\n");

			}
			else
			{
				m_pstaDump->FormatAppend( "   %s(%u)\r\n",
					Line.FileName, Line.LineNumber );
			}

#ifdef JT_20010626_WantModuleInfo
			// If we want this info adapt the printf and _snprintf in the following.

			// show module info (SymGetModuleInfo())
			if ( ! SymGetModuleInfo( hProcess, s.AddrPC.Offset, &Module ) )
			{
				m_pstaDump->FormatAppend( "SymGetModuleInfo): gle = %u\r\n", gle );
			}
			else
			{ // got module info OK
				m_pstaDump->FormatAppend( "    Mod:  %s[%s], base: 0x%x\r\n    Sym:  type: ",
					Module.ModuleName, Module.ImageName, Module.BaseOfImage );
				switch ( Module.SymType )
					{
					case SymNone:
						m_pstaDump->FormatAppend( "-nosymbols-");
						break;
					case SymCoff:
						m_pstaDump->FormatAppend( "COFF");
						break;
					case SymCv:
						m_pstaDump->FormatAppend( "CV");
						break;
					case SymPdb:
						m_pstaDump->FormatAppend( "PDB");
						break;
					case SymExport:
						m_pstaDump->FormatAppend( "-exported-");
						break;
					case SymDeferred:
						m_pstaDump->FormatAppend( "-deferred-");
						break;
					case SymSym:
						m_pstaDump->FormatAppend( "SYM");
						break;
					default:
						m_pstaDump->FormatAppend( "symtype=%d", (long) Module.SymType);
						break;
					}
				m_pstaDump->FormatAppend( ", file: %s\r\n", Module.LoadedImageName);
			} // got module info OK
#endif // JT_20010626_WantModuleInfo

			// We don't want to return more than about 10K of info (enough for an email).
			// This also serves to make sure there's enough room in the buffer for more.
			// The idea is that we'd like to keep both the top and bottom of the stack.
			// So we delete frames from the middle until we have less than 10K.
			if (m_pstaDump->Length() > MAXDUMPLEN)
			{
				if (!ichEndLowHalf)
				{
					static char * pszGap =
						"\r\n\r\n\r\n******************Frames skipped here***************\r\n\r\n\r\n";
					int cchGap = strlen(pszGap);
					ichEndLowHalf = FindStartOfFrame(MAXDUMPLEN / 2);
					// Overwrite some of what's there with the gap marker. The incomplete
					// frame will be part of what gets deleted.
					m_pstaDump->Replace(ichEndLowHalf, ichEndLowHalf + cchGap, pszGap, cchGap);
					ichEndLowHalf += cchGap;
				}
				int cchLeave = m_pstaDump->Length();
				int ichKeep = ichEndLowHalf;
				while (cchLeave > MAXDUMPLEN)
				{
					int ichKeepT = FindStartOfFrame(ichKeep + 1);
					cchLeave -= ichKeepT - ichKeep;
					ichKeep = ichKeepT;
				}
				m_pstaDump->Replace(ichEndLowHalf, ichKeep, (char *)NULL, 0);
			}

		} // we seem to have a valid PC

		// no return address means no deeper stackframe
		if ( s.AddrReturn.Offset == 0 )
		{
			// avoid misunderstandings in the printf() following the loop
			SetLastError( 0 );
			break;
		}

	} // for ( frameNum )

	if ( gle != 0 )
		printf( "\r\nStackWalk(): gle = %u\r\n", gle );

LCleanup:
	ResumeThread( hThread );
	// de-init symbol handler etc.
	SymCleanup( hProcess );
	free( pSym );
	delete [] tt;

#ifdef DEBUG
	::OutputDebugStringA(m_pstaDump->Chars());
#endif

#endif
}


// Enumerate the modules we have running and load their symbols.
// Return true if successful.
bool StackDumper::EnumAndLoadModuleSymbols( HANDLE hProcess, DWORD pid )
{
#ifdef WIN32
	HANDLE hSnapShot;
	MODULEENTRY32 me = { sizeof me };
	bool keepGoing;
	hSnapShot = CreateToolhelp32Snapshot( TH32CS_SNAPMODULE, pid );
	if ( hSnapShot == (HANDLE) -1 )
		return false;

	keepGoing = Module32First( hSnapShot, &me );
	while ( keepGoing )
	{
		// here, we have a filled-in MODULEENTRY32. Use it to load symbols.
		// Don't check errors, if we can't load symbols for some modules we just
		// won't be able to do symbolic reports on them.
		StrAnsi staExePath(me.szExePath);
		StrAnsi staModule(me.szModule);
//		SymLoadModule( hProcess, 0, me.szExePath, me.szModule, (DWORD) me.modBaseAddr,
//			me.modBaseSize);
		::SymLoadModule( hProcess, 0, const_cast<char *>(staExePath.Chars()),
			const_cast<char *>(staModule.Chars()), (DWORD)me.modBaseAddr, me.modBaseSize);
		keepGoing = Module32Next( hSnapShot, &me );
	}

	CloseHandle( hSnapShot );
	return true;
#else
	return false;
#endif

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
#ifdef WIN32
DWORD Filter( EXCEPTION_POINTERS *ep )
{
	HANDLE hThread;

	DuplicateHandle( GetCurrentProcess(), GetCurrentThread(),
		GetCurrentProcess(), &hThread, 0, false, DUPLICATE_SAME_ACCESS );
	StackDumper::AppendShowStack( hThread, *(ep->ContextRecord) );
	CloseHandle( hThread );

	return EXCEPTION_EXECUTE_HANDLER;
}
#else
DWORD Filter( EXCEPTION_POINTERS *ep )
{
	return 0;
}
#endif

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
#ifdef WIN32
void DumpStackHere(char * pszMsg)
{
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
}
#else
// NOTE: If I use the same method declaration as on Windows (char * pszMsg) we get an error:
// deprecated conversion from string constant to ‘char*’ when calling this method.
// Using (const char * pszMsg) on Windows gives errors as well.
void DumpStackHere(const char * pszMsg)
{
	// TODO-Linux: implement
}
#endif

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
#ifdef WIN32
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
		if (!wcsstr(sbstrDesc, ThrowableSd::MoreSep()))
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
#else
	ThrowInternalError(hrErr);
#endif
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
#ifdef WIN32
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
#endif
	}
};

static MemMsgMaker mmm; // existence of an instance gets the above called.

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
#ifdef WIN32
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
	OLECHAR * pszSrc = (OLECHAR *)_alloca((_tcslen(pfact->GetProgId()) + 1) * isizeof(OLECHAR));
	OLECHAR * pchw = pszSrc;
	for (const achar * pch = pfact->GetProgId(); *pch; pch++, pchw++)
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
#else

// TODO-Linux: REVIEW a full linux implementation of this may be neccessary.
HRESULT HandleThrowable(Throwable & thr, REFGUID iid, DummyFactory * pfact)
{
	HRESULT hrErr = thr.Error();

	return hrErr;
}

#endif

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
HRESULT HandleDefaultException(REFGUID iid, DummyFactory * pfact)
{
	DumpStackHere("Unknown exception caught in COM method\r\n");
	ThrowableSd thr(E_UNEXPECTED, reinterpret_cast<const wchar *>(NULL), 0, StackDumper::GetDump());
	return HandleThrowable(thr, iid, pfact);
}

/*----------------------------------------------------------------------------------------------
	General purpose method to set up and store an error, rather than throw an exception
	in the normal way.
----------------------------------------------------------------------------------------------*/
#ifdef WIN32
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
#else
HRESULT StackDumper::RecordError(REFGUID iid, StrUni stuDescr, StrUni stuSource,
	int hcidHelpId, StrUni stuHelpFile)
{
	return S_OK;
}

#endif
