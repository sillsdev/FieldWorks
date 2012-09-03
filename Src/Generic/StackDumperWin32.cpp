// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
//
// Contains the Windows specific methods of the StackDumper class
// --------------------------------------------------------------------------------------------
#ifdef WIN32

//:>********************************************************************************************
//:> Include files
//:>********************************************************************************************
#include "main.h"
#pragma hdrstop
// any other headers (not precompiled)

#undef THIS_FILE
DEFINE_THIS_FILE

#define gle (GetLastError())
#define lenof(a) (sizeof(a) / sizeof((a)[0]))
#define MAXNAMELEN 1024 // max name length for found symbols
#define IMGSYMLEN ( sizeof IMAGEHLP_SYMBOL )
#define TTBUFLEN 65536 // for a temp buffer

/// Add the given string to Sta. If Sta is not empty, add a semi-colon first
void AppendToStaWithSep(StrApp sta, const achar * pch)
{
	if (sta.Length())
		sta.Append(";");
	sta.Append(pch);
}

typedef BOOL (__stdcall * PFNSYMGETLINEFROMADDR)
				(IN  HANDLE         hProcess         ,
				 IN  DWORD          dwAddr           ,
				 OUT PDWORD         pdwDisplacement  ,
				 OUT PIMAGEHLP_LINE Line              ) ;

// The pointer to the SymGetLineFromAddr function I GetProcAddress out
//  of IMAGEHLP.DLL in case the user has an older version that does not
//  support the new extensions.
PFNSYMGETLINEFROMADDR g_pfnSymGetLineFromAddr = NULL;


// Enumerate the modules we have running and load their symbols.
// Return true if successful.
bool EnumAndLoadModuleSymbols(HANDLE hProcess, DWORD pid )
{
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
}

void StackDumper::ShowStackCore( HANDLE hThread, CONTEXT& c )
{
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

}

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

void ThrowHrEx(HRESULT hr, int hHelpId)
{
	StrUni msg = ConvertException(::GetLastError());
	ThrowHr(hr, msg.Chars(), hHelpId);
}

#endif
