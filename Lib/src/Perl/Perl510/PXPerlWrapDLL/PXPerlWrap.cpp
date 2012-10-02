//
//  PXPerlWrap - A full-featured Perl wrapper for C++ language.
//  Copyright © 2003 - 2005 Grégoire Péan, aka PixiGreg.
//
//  See PXPerlWrap.h for licensing information.
//
//  Sorry for the lack of comments in the code...
//  I know I should get used to commenting my code;
//  if you need some enlightement don't hesitate to ask me.
//
/////////////////////////////////////////////////////////////////////


#include "StdAfx.h"

#include <locale.h>
#include <setjmp.h>
#include <errno.h>
#include <io.h>
#include <fcntl.h>
#include <process.h>
#include <stddef.h>
#include <stdlib.h>
#include <conio.h>
#include <signal.h>
#include <sys/types.h>
#include <sys/stat.h>

#include <shlwapi.h>
#include <urlmon.h>

#include "PXPerlWrap.h"
#include "perlsistent.h"

#pragma warning(disable: 4244)

#pragma push_macro("stdout")
#pragma push_macro("stderr")
#pragma push_macro("stdin")

#include "config.h"
#include "EXTERN.h"
#include "perl.h"
#include "XSUB.h"

#pragma pop_macro("stdout")
#pragma pop_macro("stderr")
#pragma pop_macro("stdin")

#undef bool
#undef Copy
#undef Pause
#undef Move
#undef open
#undef write
#undef read
#undef eof
#undef close
#undef IsWinNT
#undef ftell
#undef fseek
#undef free
#undef malloc
#undef scalar
#undef crypt
#undef Copy
#undef fprintf
#undef printf
#undef sv
#undef setvbuf
/*
#undef stdin
#undef stdout
#undef stderr
*/
#undef IsSet
/*
#define stdin  (&_iob[0])
#define stdout (&_iob[1])
#define stderr (&_iob[2])
*/
#define NOID		0
#define NEXT_ID(id)	(++id)

#ifdef _UNICODE
#define PXCASTR(str) ((LPCSTR)CW2A((LPCWSTR)str))
#define PXCWSTR(str) ((LPCWSTR)CA2W((LPCSTR)str))
#else
#define PXCASTR(str) (LPCTSTR(str))
#define PXCWSTR(str) (LPCTSTR(str))
#endif


#define DEFAULT_PIPE_SIZE		4096

#define USE_EVAL // don't change this or it crashes :p for experimental purposes

#define MAX_LINE_SIZE 512

#define MAX_SCRIPTID_SIZE 256

#define BYTELOADER _T("-MByteLoader")

#define AvREADONLY(av) SvREADONLY((SV*)av) // +203
#define HvREADONLY(hv) SvREADONLY((SV*)hv) // +203

#define ERRORMSG_SVREADONLY _T("Scalar \"%s\" (%X) is read-only.\n")
#define ON_SVREADONLY _ftprintf(stderr, ERRORMSG_SVREADONLY, LPCTSTR(GetName()), (INT_PTR)GetParam())
#define ERRORMSG_AVREADONLY _T("Array \"%s\" (%X) is read-only.\n")
#define ON_AVREADONLY _ftprintf(stderr, ERRORMSG_SVREADONLY, LPCTSTR(GetName()), (INT_PTR)GetParam())
#define ERRORMSG_HVREADONLY _T("Hash \"%s\" (%X) is read-only.\n")
#define ON_HVREADONLY _ftprintf(stderr, ERRORMSG_SVREADONLY, LPCTSTR(GetName()), (INT_PTR)GetParam())


#ifdef _DEBUG
#define new DEBUG_NEW
#endif

/////////////////////////////////////////////////

namespace PXPerlWrap
{

	/////////////////////////////////////////////////
	// Static variables

	static LPCTSTR s_szOPTFile = _T("PXPerlWrap.opt");

	//static LPCTSTR s_szNil = _T("");
	static const CString s_strNil(_T(""));

	static UTF8Mode s_UTF8Mode = UTF8_off;

	static PerlID s_idUnique = NOID;

	static HWND s_hErrorWnd = 0;

	static HANDLE s_hRunEvent = 0;
	static HANDLE s_hRunThread = 0;
	static bool s_bRunning;
	static UINT s_nRunThreadID = 0;

	static UINT s_nErrorMessage = WM_PXPW_OUTPUT;

	static DWORD s_dwLogStart = 0;
	static int s_nErrorLevel = 0;

	static int s_fdErrors = -1;

	static XSItem *s_pXSList = 0;
	static CStringArray s_strAModules, s_strAOptions;

	/////////////////////////////////////////////////
	// CInitDestroy class
	// General initialization/destruction

	class CInitDestroy
	{
	public:
		CInitDestroy()
		{
	#ifdef _DEBUG
	#ifdef _UNICODE
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Debug Unicode DLL");
	#else
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Debug MBCS DLL");
	#endif
	#else
	#ifdef _UNICODE
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Release Unicode DLL");
	#else
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Release MBCS DLL");
	#endif
	#endif
			TRACE(": starting up.\n");

			PERL_SYS_INIT3(&__argc, &__argv, &environ);
		};

		~CInitDestroy()
		{
	#ifdef _DEBUG
	#ifdef _UNICODE
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Debug Unicode DLL: shutting down\n");
	#else
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Debug MBCS DLL: shutting down\n");
	#endif
	#else
	#ifdef _UNICODE
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Release Unicode DLL: shutting down\n");
	#else
			TRACE("[PXPW] PXPerlWrap " PXPW_VERSION " Release MBCS DLL: shutting down\n");
	#endif
	#endif
			PERL_SYS_TERM();

			PXPerlWrap::PXSetErrorMsgDestFile(0); // stop logging
			PXPerlWrap::PXSetXS(0);
		};
	};

	/////////////////////////////////////////////////
	// CPool class
	// Prevents passing pointers to CPerlVariable objects
	// which may become invalid when an interpreter object goes out of scope

	template<class T>
		class CPool
	{
	public:
		PerlID at(T t)
		{
			if (m_pool.size() < 1)
				m_pool.push_back(0); // dummy element because of NOID
			for (std::vector<T>::size_type i=0; i<m_pool.size(); i++)
			{
				if (m_pool.at(i) == t)
					return (PerlID)i;
			}
			m_pool.push_back(t);
			return (PerlID)(m_pool.size() - 1);
		}

		const T& at(PerlID id) const
		{
			return m_pool.at(id);
		}

		void remove(T t)
		{
			for (std::vector<T>::iterator i=m_pool.begin(); i!=m_pool.end(); i++)
			{
				if (*i == t)
					*i = 0;
			}
		}

	protected:
		std::vector<T> m_pool;
	};

	class CInterpPool : public CPool<CPerlInterpreter*>
	{};

	// The one and only CInitDestroy object
	static const CInitDestroy s_init;

	// The one and only CInterpPool object
	static CInterpPool s_interp_pool;



	/////////////////////////////////////////////////
	// General functions prototypes

	CString PXGetUniqueID();
	LPVOID PXReadPerlResource(HMODULE hModule, int nResID, DWORD &dwSize);
	PXPERL_API CString& PXGetExeDir(CString &strRet);
	void PXMakeAbsolutePath(CString &strPath);
	long PXGetFileSize(LPCTSTR szFile);
	int PXGetFileSize(int fh);
	LPBYTE PXReadFile(LPCTSTR szFile, DWORD &dwSize);
	bool PXReadTextFile(LPCTSTR szFile, CString &strRead);
	bool PXReadTextFileLines(LPCTSTR szFile, CStringArray &strALines, bool bTrim=true);
	bool PXWriteCreateFile(LPCTSTR szFile, LPCVOID lpData, DWORD dwSize);
	bool PXWriteCreateTextFile(LPCTSTR szFile, const CString &strWrite);
	bool PXCreateResetFile(LPCTSTR szFile);
	bool PXURLDownloadToFile(LPCTSTR szURL, LPCTSTR szLocalFile);
	CString& PXTempName(CString &strRet);

	void PXSetErrorMsgDestWindow(HWND hWnd, UINT nWindowMessage)
	{
		s_hErrorWnd = hWnd;
		s_nErrorMessage = nWindowMessage;
	}

	LPCSTR PXGetTime(void)
	{
		struct tm newtime;
		memset(&newtime, 0, sizeof(newtime));
		time_t aclock;
		time(&aclock);
		/* newtime = */ localtime_s(&newtime, &aclock);
		static char buf[256];
		asctime_s(buf,&newtime);
		return buf;
	}

	// message & error processing stuff

	static UINT s_nPostedMessagesCount = 0;

	#define IOBUF_SET_WIDE(cType) { cType &= 1 << 7; }
	#define IOBUF_IS_WIDE(cType) ((cType & (1 << 7)) != 0)
	#define IOBUF_GET_TYPE(cType) (cType & ~(1 << 7))

	bool PXPostBufferMessage(HWND hWnd, UINT nMsg, PXIORedirect::CIOBuffer::Type type, LPVOID buffer, UINT nLen, bool bWide=false)
	{
		PXIORedirect::CIOBuffer::BufferInfo info;
		info.cSignature = (BYTE)PXIORedirect::CIOBuffer::signature;
		info.cType = (BYTE)type;
		if (bWide)
			IOBUF_SET_WIDE(info.cType);
		info.nBufSize = (USHORT)nLen;
		if (s_nPostedMessagesCount > 32)
		{
			Sleep(50);
			if (s_nPostedMessagesCount > 256)
			{
				PXPerlWrap::PXError(_T("PXPostBufferMessage(): exceeded number of posted messages (256); application may be crashed or you may not be using PXIORedirect::CIOBuffer properly"));
				return false;
			}
		}
		s_nPostedMessagesCount++;
		return 0!=::PostMessage(hWnd, nMsg, (WPARAM)*(LPINT)&info, (LPARAM)buffer);
	}

	void PXSetErrorMsgLevel(int nLevel)
	{
		if (nLevel >= 0 && nLevel <= 4)
			s_nErrorLevel = nLevel;
	}

	bool PXSetErrorMsgDestFile(LPCTSTR szFile, bool bAppend)
	{
		bool bRes = false;

		CString strFile(szFile);
		PXMakeAbsolutePath(strFile);
		szFile = LPCTSTR(strFile);

		if (s_fdErrors != -1)
		{
			char buf[256];
			strcpy_s(buf, "\n-- PXPerlWrap " PXPW_VERSION " -- log stopped at ");
			strcat_s(buf, PXGetTime());
			_write(s_fdErrors, buf, strlen(buf));
			_close(s_fdErrors);
		}

		if (szFile && *szFile)
		{
			if (bAppend)
			{
				/*s_fdErrors =*/ _tsopen_s(&s_fdErrors, szFile, _O_WRONLY | _O_APPEND | _O_CREAT /*| _O_TEXT*/, _SH_DENYWR, _S_IREAD | _S_IWRITE);
			}

			if (s_fdErrors == -1)
			{
				_tunlink(szFile);
				/*s_fdErrors =*/ _tsopen_s(&s_fdErrors, szFile, _O_WRONLY | _O_TRUNC | _O_CREAT, _SH_DENYWR, _S_IREAD | _S_IWRITE);
			}

			if (s_fdErrors != -1)
			{
				char buf[256];
				strcpy_s(buf, "-- PXPerlWrap " PXPW_VERSION " -- log started at ");
				strcat_s(buf, PXGetTime());
				strcat_s(buf, "\n   legend: [  ]=info, [!  ]=warning, [!! ]=error, [!!!]=critical error\n\n");
				if (_write(s_fdErrors, buf, strlen(buf)) > 0)
					bRes = true;
				else
				{
					_close(s_fdErrors);
				}

				s_dwLogStart = GetTickCount();
			}
		}

		return bRes;
	}

	bool PXDispatchErrorBuffer(PXIORedirect::CIOBuffer::Type type, LPTSTR buffer, UINT nLen)
	{
		bool bRes = false;

		switch (type)
		{
		case PXIORedirect::CIOBuffer::TypeInfo:
			if (s_nErrorLevel >= 1)
				bRes = true;
		case PXIORedirect::CIOBuffer::TypeWarning:
			if (s_nErrorLevel >= 2)
				bRes = true;
			break;
		case PXIORedirect::CIOBuffer::TypeError:
			if (s_nErrorLevel >= 3)
				bRes = true;
			break;
		case PXIORedirect::CIOBuffer::TypeCritical:
			if (s_nErrorLevel >= 4)
				bRes = true;
			break;
		}

		if (bRes)
			return false;

		if (::IsWindow(s_hErrorWnd)
			&& PXPostBufferMessage(s_hErrorWnd,
			s_nErrorMessage,
			type,
			buffer, nLen,
#ifdef _UNICODE
			true
#else
			false
#endif
			))
		{
			bRes = true;
		}

		if (s_fdErrors != -1)
		{
			char tick[64];
			char *buf = new char[nLen+64];
			switch (type)
			{
			case PXIORedirect::CIOBuffer::TypeWarning:
				strcpy_s(buf, nLen + 64, "!  ");
				break;
			case PXIORedirect::CIOBuffer::TypeError:
				strcpy_s(buf, nLen + 64, "!! ");
				break;
			case PXIORedirect::CIOBuffer::TypeCritical:
				strcpy_s(buf, nLen + 64, "!!!");
				break;
			default:
				strcpy_s(buf, nLen + 64, "   ");
			}
			_ultoa_s(GetTickCount()-s_dwLogStart, tick, 10);
			strcat_s(buf, nLen + 64, tick);
			strcat_s(buf, nLen + 64, ":");
			strcat_s(buf, nLen + 64, CT2A(buffer));
			if (_write(s_fdErrors, buf, (nLen+strlen(buf+nLen))) > 0)
				bRes = true;
			delete [] buf;
		}

		if (!bRes)
		{
#ifdef _UNICODE
			TRACE("[PXPW] %S", buffer);
#else
			TRACE("[PXPW] %s", buffer);
#endif
		}

		return bRes;
	}

	bool PXInfo(LPCTSTR szText, ...)
	{
		bool bRes = false;
		va_list params;
		va_start(params, szText);
		size_t len = _vsctprintf(szText, params) + 2;
		TCHAR *buffer = new TCHAR[len];
		if (buffer)
		{
			_vstprintf_s(buffer, len, szText, params);
			_tcscat_s(buffer, len, _T("\n"));
			bRes = PXDispatchErrorBuffer(PXIORedirect::CIOBuffer::TypeInfo, buffer, len - 1);
			delete [] buffer;
		}
		return bRes;
	}

	bool PXWarning(LPCTSTR szText, ...)
	{
		bool bRes = false;
		va_list params;
		va_start(params, szText);
		size_t len = _vsctprintf(szText, params) + 2;
		TCHAR *buffer = new TCHAR[len];
		if (buffer)
		{
			_vstprintf_s(buffer, len, szText, params);
			_tcscat_s(buffer, len, _T("\n"));
			bRes = PXDispatchErrorBuffer(PXIORedirect::CIOBuffer::TypeWarning, buffer, len - 1);
			delete [] buffer;
		}
		return bRes;
	}

	bool PXError(LPCTSTR szText, ...)
	{
		bool bRes = false;
		va_list params;
		va_start(params, szText);
		size_t len = _vsctprintf(szText, params) + 2;
		TCHAR *buffer = new TCHAR[len];
		if (buffer)
		{
			_vstprintf_s(buffer, len, szText, params);
			_tcscat_s(buffer, len, _T("\n"));
			bRes = PXDispatchErrorBuffer(PXIORedirect::CIOBuffer::TypeError, buffer, len - 1);
			delete [] buffer;
		}
		return bRes;
	}

	bool PXCriticalError(LPCTSTR szText, ...)
	{
		bool bRes = false;
		va_list params;
		va_start(params, szText);
		size_t len = _vsctprintf(szText, params) + 2;
		TCHAR *buffer = new TCHAR[len];
		if (buffer)
		{
			_vstprintf_s(buffer, len, szText, params);
			_tcscat_s(buffer, len, _T("\n"));
			bRes = PXDispatchErrorBuffer(PXIORedirect::CIOBuffer::TypeCritical, buffer, len - 1);
			delete [] buffer;
		}
		return bRes;
	}

}; //namespace PXPerlWrap


/////////////////////////////////////////////////
/////////////////////////////////////////////////
/////////////////////////////////////////////////
// PXIORedirect

namespace PXIORedirect
{
	typedef struct sRedirectThreadData
	{
		sRedirectThreadData()
		{
			hThread = 0;
			fdRead = -1;
			nMessage = WM_PXPW_OUTPUT;

			bRun = false;
			bFlush = false;
			bHurry = false;
			bPaused = false;
			bPause = false;
			destSave = dest = DestNone;
			pDestDataSave = pDestData = 0;
			bFirstDest = true;
			dwStream = 0;

			hActionDoneEvent = CreateEvent(0, FALSE, FALSE, 0);
			hPauseEvent = CreateEvent(0, FALSE, FALSE, 0);
			hResumeEvent = CreateEvent(0, FALSE, FALSE, 0);
			hAbortEvent = CreateEvent(0, FALSE, FALSE, 0);
		};

		~sRedirectThreadData()
		{
			if (hActionDoneEvent)
				CloseHandle(hActionDoneEvent);
			if (hPauseEvent)
				CloseHandle(hPauseEvent);
			if (hResumeEvent)
				CloseHandle(hResumeEvent);
			if (hAbortEvent)
				CloseHandle(hAbortEvent);
		};

		DWORD dwStream;
		HANDLE hThread;

		int fdRead;
		UINT nMessage;
		bool bRun;
		bool bPause;
		bool bPaused;
		bool bFlush;
		bool bHurry;

		bool bFirstDest;

		RedirectDestination dest, destSave;
		LPVOID pDestData, pDestDataSave;
		HANDLE hActionDoneEvent, hPauseEvent, hResumeEvent, hAbortEvent;

	} RedirectThreadData;


	static UINT __stdcall RedirectProc(LPVOID pData);
	static int s_handlesStdIn[2];

	static RedirectThreadData *s_prtd = 0;

	CIOBuffer::CIOBuffer(WPARAM wParam, LPARAM lParam)
	{
		memcpy(&m_info, &wParam, sizeof(WPARAM));
		if (m_info.cSignature == (BYTE)signature && m_info.nBufSize
			&& (   (IOBUF_IS_WIDE(m_info.cType) && !::IsBadStringPtrW((LPCWSTR)lParam, (UINT)m_info.nBufSize+1))
			|| (!IOBUF_IS_WIDE(m_info.cType) && !::IsBadStringPtrA((LPCSTR)lParam, (UINT)m_info.nBufSize+1))
			)
			)
		{
			m_buffer = (LPVOID)lParam;
		}
		else
		{
			PXPerlWrap::PXError(_T("CIOBuffer::CIOBuffer(): Parameters invalid, or inaccessible memory area."));
			m_info.cType = 0;
			m_info.nBufSize = 0;
			m_info.cSignature = (BYTE)signature;
			m_buffer = 0;
		}
	}

	CIOBuffer::~CIOBuffer()
	{
		if (m_buffer)
		{
			delete [] m_buffer;
		}
		PXPerlWrap::s_nPostedMessagesCount--;
	}

	CIOBuffer::Type CIOBuffer::GetType(void) const
	{
		return (CIOBuffer::Type)IOBUF_GET_TYPE(m_info.cType);
	}

	USHORT CIOBuffer::GetSize(void) const
	{
		return m_info.nBufSize;
	}

	bool CIOBuffer::IsDefaultWide(void) const
	{
		return IOBUF_IS_WIDE(m_info.cType);
	}

	CIOBuffer::operator LPCSTR()
	{
		LPCSTR szRet = 0;

		if (m_buffer)
		{
			if (IsDefaultWide())
			{
#ifdef _UNICODE
				if (m_strABuffer.length() < 1)
					m_strABuffer.assign((LPCSTR)CW2A((LPCWSTR)m_buffer));
				szRet = m_strABuffer.c_str();
#else
				// error
#endif
			}
			else
			{
				szRet = (LPCSTR)m_buffer;
			}
		}

		return szRet;
	}

#ifdef _UNICODE
	CIOBuffer::operator LPCWSTR()
	{
		LPCWSTR szRet = 0;

		if (m_buffer)
		{
			if (IsDefaultWide())
			{
				szRet = (LPCWSTR)m_buffer;
			}
			else
			{
				if (m_strWBuffer.length() < 1)
					m_strWBuffer.assign((LPCWSTR)CA2W((LPCSTR)m_buffer));
				szRet = m_strWBuffer.c_str();
			}
		}

		return szRet;
	}
#endif


	bool Initialize(UINT nWindowMessage, UINT nPipeSize)
	{
		int handlesStdOut[2], handlesStdErr[2];

		PXPerlWrap::PXInfo(_T("Initialize(): Initializing redirection."));

		// stop any previous redirection
		Uninitialize();

		UINT thread_id;
		FILE *fp = 0;

		STARTUPINFO si;
		si.cb = sizeof(STARTUPINFO);
		GetStartupInfo(&si);

		// create pipes
		if (_pipe(s_handlesStdIn, nPipeSize, _O_TEXT | _O_NOINHERIT) == -1)
			goto _redirect_failure;
		if (_pipe(handlesStdOut, nPipeSize, _O_TEXT | _O_NOINHERIT) == -1)
			goto _redirect_failure;
		if (_pipe(handlesStdErr, nPipeSize, _O_TEXT | _O_NOINHERIT) == -1)
			goto _redirect_failure;

		// close the existing output and errors file descriptors
		_close(0); // input
		_close(1); // output
		_close(2); // errors

		// reassign file descriptors
		if (-1 == _dup2(s_handlesStdIn[0], 0))
			goto _redirect_failure;
		if (-1 == _dup2(handlesStdOut[1], 1))
			goto _redirect_failure;
		if (-1 == _dup2(handlesStdErr[1], 2))
			goto _redirect_failure;

		// close the read file descriptor of the input pipe
		_close(s_handlesStdIn[0]);
		// close the write file descriptors of the output/error pipes, which we don't need anymore (we only need to read by now)
		_close(handlesStdOut[1]);
		_close(handlesStdErr[1]);

		s_handlesStdIn[0] = -1;
		handlesStdOut[1] = -1;
		handlesStdErr[1] = -1;

		// this is not really needed, we just make sure the standard console output and errors handles of our application are the same as
		if (si.hStdInput != (HANDLE)-1)
			SetStdHandle(STD_INPUT_HANDLE, si.hStdInput);
		if (si.hStdOutput != (HANDLE)-1)
			SetStdHandle(STD_OUTPUT_HANDLE, si.hStdOutput);
		if (si.hStdError != (HANDLE)-1)
			SetStdHandle(STD_ERROR_HANDLE, si.hStdError);

		// let's start the threads now

		s_prtd = new RedirectThreadData[2];

		s_prtd[0].bRun = true;
		s_prtd[0].dwStream = PXPW_REDIR_OUTPUT;
		s_prtd[0].nMessage = nWindowMessage;
		s_prtd[0].fdRead = handlesStdOut[0];

		s_prtd[1].bRun = true;
		s_prtd[1].dwStream = PXPW_REDIR_ERRORS;
		s_prtd[1].nMessage = nWindowMessage;
		s_prtd[1].fdRead = handlesStdErr[0];

		s_prtd[0].hThread = (HANDLE)_beginthreadex(0, 0, RedirectProc, (LPVOID)0, 1, &thread_id);
		if (!s_prtd[0].hThread
			|| WaitForSingleObject(s_prtd[0].hActionDoneEvent, DEFAULT_TIMEOUT) == WAIT_TIMEOUT)
			goto _redirect_failure;

		s_prtd[1].hThread = (HANDLE)_beginthreadex(0, 0, RedirectProc, (LPVOID)1, 1, &thread_id);
		if (!s_prtd[1].hThread
			|| WaitForSingleObject(s_prtd[1].hActionDoneEvent, DEFAULT_TIMEOUT) == WAIT_TIMEOUT)
			goto _redirect_failure;

		// important: reset the stdin, stdout and stderr structures for the current process,
		// so not only low level IO access function are being redirected, but also printf() and so on.
		fp = _fdopen(0, "r");
		memcpy(stdin, fp, sizeof(FILE));

		fp = _fdopen(1, "w");
		setvbuf(fp, 0, _IONBF, 0);
		memcpy(stdout, fp, sizeof(FILE));

		fp = _fdopen(2, "w");
		setvbuf(fp, 0, _IONBF, 0);
		memcpy(stderr, fp, sizeof(FILE));

		Sleep(0);

		PXPerlWrap::PXInfo(_T("Initialize(): Redirection successfuly initialized."));

		return true;

_redirect_failure:
		PXPerlWrap::PXError(_T("Initialize(): Failed to initialize redirection. GetLastError()=%d; errno=%d."), GetLastError(), errno);
		Uninitialize(); // close handles still open
		return false;
	}

	void DispatchBuffer(int idx, LPSTR buffer, UINT nSize)
	{
		bool bError = false;

		switch (s_prtd[idx].dest)
		{
		case DestCallback:
			if (s_prtd[idx].pDestData)
			{
				((fnPXIOCallback)s_prtd[idx].pDestData)(s_prtd[idx].dwStream, buffer, nSize);
				delete [] buffer;
			}
			else
				bError = true;
			break;
		case DestWindow:
			if (!::IsWindow((HWND)s_prtd[idx].pDestData)
				|| !PXPerlWrap::PXPostBufferMessage((HWND)s_prtd[idx].pDestData,
				s_prtd[idx].nMessage,
				s_prtd[idx].dwStream == PXPW_REDIR_OUTPUT
				? PXIORedirect::CIOBuffer::TypeOutputData
				: PXIORedirect::CIOBuffer::TypeErrorsData,
				buffer, nSize))
			{
				bError = true;
			}
			break;
		case DestFileFH:
			if (s_prtd[idx].pDestData)
			{
				fwrite((void*)buffer, 1, nSize, (FILE*)s_prtd[idx].pDestData);
				delete [] buffer;
			}
			else
				bError = true;
			break;
		case DestFileFD:
			if ((int)s_prtd[idx].pDestData >= 0)
			{
				_write((int)s_prtd[idx].pDestData, (void*)buffer, nSize);
				delete [] buffer;
			}
			else
				bError = true;
			break;
		case DestNone:
		default:
			delete [] buffer;
			break;
		}

		if (bError)
		{
			PXPerlWrap::PXError(_T("DispatchBuffer(0x%08X): destination parameter invalid (i.e. window handle, callback function, or file handle/descriptor), or failed writing to it. Data lost."), s_prtd[idx].hThread);
			delete [] buffer;
		}
	}

	UINT __stdcall RedirectProc(LPVOID pData)
	{
		int nBytesRead;
		char *buffer; // not TCHAR because data written is the pipes is likely to be non-Unicode text
		const int idx = (int)pData;

		// signal the mother thread we are done with copying the thread data
		SetEvent(s_prtd[idx].hActionDoneEvent);

		PXPerlWrap::PXInfo(_T("RedirectProc(0x%08X): Thread started."), s_prtd[idx].hThread);

		// begin the read/send loop
		while (s_prtd[idx].bRun)
		{
			if (s_prtd[idx].bFlush || s_prtd[idx].bPause)
			{
				//TRACE(_T("RedirectProc(0x%08X): Flushing.\n"), s_prtd[idx].hThread);
				if (s_prtd[idx].bFlush)
				{
					PXPerlWrap::PXInfo(_T("RedirectProc(0x%08X): Flushing."), s_prtd[idx].hThread);

					buffer = new CHAR[DEFAULT_PIPE_SIZE]; //(char*)PXMalloc(DEFAULT_PIPE_SIZE);
					if (!buffer)
					{
						Sleep(50);
						continue;
					}
					char *flushBuffer = new CHAR[DEFAULT_PIPE_SIZE]; //(char*)PXMalloc(DEFAULT_PIPE_SIZE);
					if (!flushBuffer)
					{
						delete [] buffer;
						Sleep(50);
						continue;
					}

					*buffer = 0;
					*flushBuffer = 0;

					while (!_eof(s_prtd[idx].fdRead) && !s_prtd[idx].bHurry && s_prtd[idx].bRun)
					{
						if ((nBytesRead = _read(s_prtd[idx].fdRead, buffer, DEFAULT_PIPE_SIZE)) < 1)
						{
							if (!s_prtd[idx].bRun || errno)
							{
								PXPerlWrap::PXError(_T("RedirectProc(0x%08X): Error while reading pipe."), s_prtd[idx].hThread);
								delete [] buffer;
								delete [] flushBuffer;
								goto _thread_break;
							}
							Sleep(50);
						}

						buffer[nBytesRead] = 0;

						if (strlen(flushBuffer) < DEFAULT_PIPE_SIZE)
							strcat_s(flushBuffer, DEFAULT_PIPE_SIZE, buffer);
						else
							break;

						Sleep(20);
					}

					//PXFree(buffer);
					delete [] buffer;

					if (!s_prtd[idx].bRun)
						break;

					if (*flushBuffer)
					{
						DispatchBuffer(idx, flushBuffer, nBytesRead);
					}
					else
					{
						//PXFree(flushBuffer);
						delete [] flushBuffer;
					}

					PXPerlWrap::PXInfo(_T("RedirectProc(0x%08X): Flush done."), s_prtd[idx].hThread);
				}

				s_prtd[idx].bPaused = true;

				PXPerlWrap::PXInfo(_T("RedirectProc(0x%08X): Paused."), s_prtd[idx].hThread);

				SetEvent(s_prtd[idx].hActionDoneEvent);

				WaitForSingleObject(s_prtd[idx].hResumeEvent, INFINITE);

				PXPerlWrap::PXInfo(_T("RedirectProc(0x%08X): Resumed..."), s_prtd[idx].hThread);

				s_prtd[idx].bPaused = false;

				SetEvent(s_prtd[idx].hActionDoneEvent);
			}
			else
			{
				if (_eof(s_prtd[idx].fdRead))
				{
					Sleep(50);
					continue;
				}

				buffer = new CHAR[DEFAULT_PIPE_SIZE]; //(char*)PXMalloc(DEFAULT_PIPE_SIZE);
				if (!buffer)
				{
					TRACE("RedirectProc(0x%08X): MEMORY PROBLEM! could not allocate buffer (%s, %d)\n", s_prtd[idx].hThread, __FILE__, __LINE__);
					continue;
				}

				if ((nBytesRead = _read(s_prtd[idx].fdRead, buffer, DEFAULT_PIPE_SIZE)) < 1)
				{
					if (!s_prtd[idx].bRun || errno)
					{
						delete [] buffer;
						PXPerlWrap::PXError(_T("RedirectProc(0x%08X): Error while reading pipe."), s_prtd[idx].hThread);
						goto _thread_break;
					}
					Sleep(50);
				}

				buffer[nBytesRead] = 0;

				DispatchBuffer(idx, buffer, nBytesRead);

				Sleep(20);
			}
		}

_thread_break:
		PXPerlWrap::PXWarning(_T("RedirectProc(0x%08X): thread ending."), s_prtd[idx].hThread);
		_endthreadex(0);
		return 0;
	}

#define IS_REDIRECTING (s_prtd != 0)
	bool IsRedirecting(void)
	{
		return IS_REDIRECTING;
	}

	void Uninitialize(bool bFlush, UINT nTimeout)
	{
		if (IS_REDIRECTING)
		{
			PXPerlWrap::PXInfo(_T("Uninitialize(): Uninitializing."));

			for (int i=0; i<2; i++)
			{
				if (s_prtd[i].bRun)
				{
					if (bFlush)
						Flush(s_prtd[i].dwStream, DEFAULT_TIMEOUT);
					else
						s_prtd[i].bRun = false;
				}

				if (s_prtd[i].hThread)
				{
					if (s_prtd[i].bPaused)
					{
						s_prtd[i].bRun = false;
						SetEvent(s_prtd[i].hResumeEvent);

						if (WaitForSingleObject(s_prtd[i].hActionDoneEvent, DEFAULT_TIMEOUT) == WAIT_TIMEOUT)
						{
							PXPerlWrap::PXError(_T("Uninitialize(0x%08X): Unable to resume (timeout)."), s_prtd[i].hThread);
						}
					}

					if (WaitForSingleObject(s_prtd[i].hThread, DEFAULT_TIMEOUT) == WAIT_TIMEOUT)
					{
						PXPerlWrap::PXError(_T("Uninitialize(): redirection thread (0x%08X) failed to return (timeout). Terminating thread."), s_prtd[i].hThread);
						if (!TerminateThread(s_prtd[i].hThread, -1))
							PXPerlWrap::PXCriticalError(_T("Uninitialize(): redirection thread (0x%08X) failed to terminate."), s_prtd[i].hThread);
					}
					CloseHandle(s_prtd[i].hThread);
				}

				if (s_prtd[i].fdRead != -1)
					_close(s_prtd[i].fdRead);
			}

			delete [] s_prtd;

			PXPerlWrap::PXInfo(_T("Uninitialize(): Successfuly uninitialized."));

			DWORD dwTick = GetTickCount();
			while (PXPerlWrap::s_nPostedMessagesCount > 0 && (GetTickCount() - dwTick) < nTimeout)
			{
				Sleep(20);
			}

			PXPerlWrap::s_nPostedMessagesCount = 0;

			//if (s_nPostedMessagesCount)
			//{
			//PXPerlWrap::PXError(_T("Uninitialize(): A few messages (%d) failed to be processed; memory leaks may occur."), s_nPostedMessagesCount);
			//}
			//s_nPostedMessagesCount = 0;
		}

		s_prtd = 0;
	}

	bool Flush(DWORD dwStream, UINT nTimeout)
	{
		bool bRet = false;

		if (IS_REDIRECTING)
		{
			int i;

_begin_flush:
			if (dwStream & PXPW_REDIR_OUTPUT)
			{
				i = 0;
				dwStream &= ~PXPW_REDIR_OUTPUT;
				PXPerlWrap::PXInfo(_T("Flush(0x%08X): Requesting output thread to flush."), s_prtd[i].hThread);
				goto _do_flush;
			}

			if (dwStream & PXPW_REDIR_ERRORS)
			{
				i = 1;
				dwStream &= ~PXPW_REDIR_ERRORS;
				PXPerlWrap::PXInfo(_T("Flush(0x%08X): Requesting errors thread to flush."), s_prtd[i].hThread);
				goto _do_flush;
			}

			goto _end_flush;

_do_flush:
			if (s_prtd[i].bPaused)
			{
				bRet = true;
				goto _begin_flush;
			}

			if (!s_prtd[i].bFlush && !s_prtd[i].bPause)
			{
				s_prtd[i].bHurry = false;
				s_prtd[i].bFlush = true;

				if (WaitForSingleObject(s_prtd[i].hActionDoneEvent, nTimeout) == WAIT_TIMEOUT)
				{
					s_prtd[i].bHurry = true;
					if (WaitForSingleObject(s_prtd[i].hActionDoneEvent, 100) == WAIT_TIMEOUT)
					{
						bRet = false;
					}
					else
					{
						bRet = true;
					}
				}
				else
				{
					bRet = true;
				}

				if (bRet)
					PXPerlWrap::PXInfo(_T("Flush(0x%08X): Flushed OK, paused."), s_prtd[i].hThread);
				else
					PXPerlWrap::PXError(_T("Flush(0x%08X): Unable to flush (timeout)."), s_prtd[i].hThread);

				s_prtd[i].bFlush = false;
				s_prtd[i].bHurry = false;
			}

			goto _begin_flush;
		}

_end_flush:
		return bRet;
	}

	bool Pause(DWORD dwStream, UINT nTimeout)
	{
		bool bRet = false;

		if (IS_REDIRECTING)
		{
			int i;

_begin_pause:
			if (dwStream & PXPW_REDIR_OUTPUT)
			{
				i = 0;
				dwStream &= ~PXPW_REDIR_OUTPUT;
				PXPerlWrap::PXInfo(_T("Pause(0x%08X): Requesting output thread to pause."), s_prtd[0].hThread);
				goto _do_pause;
			}

			if (dwStream & PXPW_REDIR_ERRORS)
			{
				i = 1;
				dwStream &= ~PXPW_REDIR_ERRORS;
				PXPerlWrap::PXInfo(_T("Pause(0x%08X): Requesting errors thread to pause."), s_prtd[1].hThread);
				goto _do_pause;
			}

			goto _end_pause;

_do_pause:
			if (s_prtd[i].bPaused)
			{
				bRet = true;
				goto _begin_pause;
			}

			if (!s_prtd[i].bPause)
			{
				s_prtd[i].bPause = true;

				if (WaitForSingleObject(s_prtd[i].hActionDoneEvent, nTimeout) == WAIT_TIMEOUT)
				{
					s_prtd[i].bPause = false;
					bRet = false;
					PXPerlWrap::PXError(_T("Pause(0x%08X): Unable to pause (timeout)."), s_prtd[i].hThread);
				}
				else
				{
					bRet = true;
					PXPerlWrap::PXInfo(_T("Pause(0x%08X): Paused."), s_prtd[i].hThread);
				}
			}

			goto _begin_pause;
		}

_end_pause:
		return bRet;
	}

	bool Resume(DWORD dwStream)
	{
		bool bRet = false;

		if (IS_REDIRECTING)
		{
			int i;

_begin_resume:
			if (dwStream & PXPW_REDIR_OUTPUT)
			{
				i = 0;
				dwStream &= ~PXPW_REDIR_OUTPUT;
				PXPerlWrap::PXInfo(_T("Resume(0x%08X): Requesting output thread to resume."), s_prtd[0].hThread);
				goto _do_resume;
			}

			if (dwStream & PXPW_REDIR_ERRORS)
			{
				i = 1;
				dwStream &= ~PXPW_REDIR_ERRORS;
				PXPerlWrap::PXInfo(_T("Resume(0x%08X): Requesting errors thread to resume."), s_prtd[1].hThread);
				goto _do_resume;
			}

			goto _end_resume;

_do_resume:
			if (!s_prtd[i].bPaused)
			{
				PXPerlWrap::PXWarning(_T("Resume(0x%08X): cannot resume, thread not paused."), s_prtd[i].hThread);
			}
			else
			{
				SetEvent(s_prtd[i].hResumeEvent);

				if (WaitForSingleObject(s_prtd[i].hActionDoneEvent, DEFAULT_TIMEOUT) == WAIT_TIMEOUT)
				{
					bRet = false;
					PXPerlWrap::PXError(_T("Resume(0x%08X): Unable to resume (timeout)."), s_prtd[i].hThread);
				}
				else
				{
					bRet = true;
					PXPerlWrap::PXInfo(_T("Resume(0x%08X): Resumed OK."), s_prtd[i].hThread);
				}
			}

			goto _begin_resume;
		}

_end_resume:
		return bRet;
	}

	bool SetDestination(DWORD dwStream, RedirectDestination dest, LPVOID pParam, UINT nTimeout)
	{
		bool bRet = false;

		if (IS_REDIRECTING)
		{
			int i;

_begin_setdest:
			if (dwStream & PXPW_REDIR_OUTPUT)
			{
				i = 0;
				dwStream &= ~PXPW_REDIR_OUTPUT;
				PXPerlWrap::PXInfo(_T("SetDestination(0x%08X): Setting destination for output thread."), s_prtd[0].hThread);
				goto _do_setdest;
			}

			if (dwStream & PXPW_REDIR_ERRORS)
			{
				i = 1;
				dwStream &= ~PXPW_REDIR_ERRORS;
				PXPerlWrap::PXInfo(_T("SetDestination(0x%08X): Setting destination for errors thread."), s_prtd[1].hThread);
				goto _do_setdest;
			}

			goto _end_setdest;

_do_setdest:
			if (!s_prtd[i].bFirstDest)
			{
				bRet = Flush(s_prtd[i].dwStream, nTimeout);
				if (!bRet)
					goto _begin_setdest;
			}

			s_prtd[i].dest = dest;
			s_prtd[i].pDestData = pParam;

			if (!s_prtd[i].bFirstDest)
			{
				bRet = Resume(s_prtd[i].dwStream);
				if (!bRet)
					goto _begin_setdest;
			}

			s_prtd[i].bFirstDest = false;

			goto _begin_setdest;
		}

_end_setdest:
		return bRet;
	}

	bool ChangeDestination(DWORD dwStream, RedirectDestination dest, LPVOID pParam, UINT nTimeout)
	{
		bool bRet = false;

		if (IS_REDIRECTING)
		{
			if (dwStream & PXPW_REDIR_OUTPUT)
			{
				s_prtd[0].destSave = s_prtd[0].dest;
				s_prtd[0].pDestDataSave = s_prtd[0].pDestData;
				bRet = SetDestination(PXPW_REDIR_OUTPUT, dest, pParam, nTimeout);
			}

			if (dwStream & PXPW_REDIR_ERRORS)
			{
				s_prtd[1].destSave = s_prtd[1].dest;
				s_prtd[1].pDestDataSave = s_prtd[1].pDestData;
				bRet = SetDestination(PXPW_REDIR_ERRORS, dest, pParam, nTimeout);
			}
		}

		return bRet;
	}

	bool RestoreDestination(DWORD dwStream, UINT nTimeout)
	{
		bool bRet = false;

		if (IS_REDIRECTING)
		{
			if (dwStream & PXPW_REDIR_OUTPUT)
			{
				bRet = SetDestination(PXPW_REDIR_OUTPUT, s_prtd[0].destSave, s_prtd[0].pDestDataSave, nTimeout);
			}

			if (dwStream & PXPW_REDIR_ERRORS)
			{
				bRet = SetDestination(PXPW_REDIR_ERRORS, s_prtd[1].destSave, s_prtd[1].pDestDataSave, nTimeout);
			}
		}

		return bRet;
	}

	bool Write(LPVOID lpData, UINT nSize)
	{
		return nSize == _write(s_handlesStdIn[1], lpData, nSize);
	}

	bool Write(LPCTSTR szData)
	{
		size_t len = _tcslen(szData)*sizeof(TCHAR);
		return len == _write(s_handlesStdIn[1], szData, len);
	}

}; // namespace PXIORedirect


/////////////////////////////////////////////////
/////////////////////////////////////////////////
/////////////////////////////////////////////////

namespace PXPerlWrap
{
	/////////////////////////////////////////////////
	// Utility Functions

	CString PXGetUniqueID()
	{
		CString strRet;
		_itot_s(NEXT_ID(s_idUnique), strRet.GetBuffer(64), 64, 10);
		strRet.ReleaseBuffer();
		return strRet;
	}

	LPVOID PXReadPerlResource(HMODULE hModule, int nResID, DWORD &dwSize)
	{
		if (!hModule)
			return 0;
		HRSRC hRes = FindResource(hModule, MAKEINTRESOURCE(nResID), MAKEINTRESOURCE(RT_PERL));
		if (!hRes)
			return 0;
		dwSize = SizeofResource(hModule, hRes);
		if (!dwSize)
			return 0;
		HANDLE hResData = LoadResource(hModule, hRes);
		if (!hResData)
			return 0;
		return LockResource(hResData);
	}

	PXPERL_API CString& PXGetExeDir(CString &strRet)
	{
		TCHAR buffer[MAX_PATH+1];
		GetModuleFileName(0, buffer, MAX_PATH+1);
		PathRemoveFileSpec(buffer);
		return (strRet = buffer);
	}

	void PXMakeAbsolutePath(CString &strPath)
	{
		if (PathIsRelative(LPCTSTR(strPath)))
		{
			TCHAR buffer[MAX_PATH+1];
			GetModuleFileName(0, buffer, MAX_PATH+1);
			PathRemoveFileSpec(buffer);
			PathAppend(buffer, LPCTSTR(strPath));
			strPath = (LPCTSTR)buffer;
		}
	}

	long PXGetFileSize(LPCTSTR szFile)
	{
		int fh;
		CString strFile(szFile);
		PXMakeAbsolutePath(strFile);
		_tsopen_s(&fh, LPCTSTR(strFile), _O_RDONLY, _SH_DENYWR, _S_IREAD);
		if (fh == -1)
			return -1;
		long nFileSize = _lseek(fh, 0L, SEEK_END);
		_close(fh);
		return nFileSize;
	}

	int PXGetFileSize(int fh)
	{
		int nPrevPos = _tell(fh);
		_lseek(fh, 0, SEEK_SET);
		int size = _lseek(fh, 0, SEEK_END);
		_lseek(fh, nPrevPos, SEEK_SET);
		return size;
	}

	LPBYTE PXReadFile(LPCTSTR szFile, DWORD &dwSize)
	{
		int fh;
		CString strFile(szFile);
		PXMakeAbsolutePath(strFile);
		_tsopen_s(&fh, LPCTSTR(strFile), _O_RDONLY | _O_BINARY, _SH_DENYWR, _S_IREAD);
		if (fh == -1)
			return false;
		dwSize = (DWORD)PXGetFileSize(fh);
		if (dwSize > 0)
		{
			LPBYTE lpData = new BYTE[dwSize+1]; // +1 to allow nul terminator to be appended in case of string dealing
			dwSize = (DWORD)_read(fh, (LPVOID)lpData, dwSize);
			return lpData;
		}
		_close(fh);
		return 0;
	}

	bool PXReadTextFile(LPCTSTR szFile, CString &strRead)
	{
		LPBYTE lpData;
		DWORD dwSize = 0;
		if ((lpData = PXReadFile(szFile, dwSize)) && dwSize)
		{
			lpData[dwSize] = '\0';
			strRead = PXCWSTR(lpData);
			delete [] lpData;
			return true;
		}
		return false;
	}

	bool PXReadTextFileLines(LPCTSTR szFile, CStringArray &strALines, bool bTrim)
	{
		//bool bRet = false;
		int nCurIndex = 0;
		CString strFile(szFile), strTemp;
		CStdioFile file;
		PXMakeAbsolutePath(strFile);
		if (!file.Open(strFile, CFile::modeRead | CFile::typeText))
			return false;
		while (file.ReadString(strTemp))
		{
			strTemp.Trim(_T(" \n\t"));
			strALines.SetAtGrow(nCurIndex++, strTemp);
		}

		return true;
	}

	bool PXWriteCreateFile(LPCTSTR szFile, LPCVOID lpData, DWORD dwSize)
	{
		int fh;
		bool bRet;
		CString strFile(szFile);
		PXMakeAbsolutePath(strFile);
		_tsopen_s(&fh, LPCTSTR(strFile), _O_WRONLY | _O_CREAT | _O_TRUNC | _O_BINARY, _SH_DENYWR, _S_IREAD | _S_IWRITE);
		if (fh == -1)
			return false;
		if (dwSize)
			bRet = ((DWORD)_write(fh, (LPCVOID)lpData, dwSize) == dwSize);
		else
			bRet = true;
		_close(fh);
		return bRet;
	}

	bool PXWriteCreateTextFile(LPCTSTR szFile, const CString &strWrite)
	{
		return PXWriteCreateFile(szFile, PXCASTR(strWrite), strWrite.GetLength());
	}

	bool PXCreateResetFile(LPCTSTR szFile)
	{
		return PXWriteCreateFile(szFile, 0, 0);
	}

	bool PXURLDownloadToFile(LPCTSTR szURL, LPCTSTR szLocalFile)
	{
		return S_OK == URLDownloadToFile(0, szURL, szLocalFile, 0, 0);
	}

	CString& PXTempName(CString &strRet)
	{
		TCHAR *buf = _ttempnam(LPCTSTR(PXGetExeDir(strRet)), _T("pxpwtmp"));
		if (buf == 0)
		{
			TCHAR buf[_MAX_PATH + 1];
			_ttmpnam_s(buf, 0);
		}
		strRet = (LPCTSTR)buf;
		free(buf);
		return strRet;
	}

	/////////////////////////////////////////////////
	// UNICODE/UTF8 Aware Code

#ifdef _UNICODE

	LPSTR PXWideCharToUTF8(LPCWSTR wszIn)
	{
		LPSTR bytes = 0;
		int len, wlen = (int)wcslen(wszIn);
		if (wlen > 0)
		{
			len = WideCharToMultiByte(CP_UTF8, 0, wszIn, wlen, 0, 0, 0, 0);
			if (len > 0)
			{
				bytes = new CHAR[len+4];
				WideCharToMultiByte(CP_UTF8, 0, wszIn, wlen, bytes, len, 0, 0);
				bytes[len] = '\0';
				bytes[len+1] = '\0';
			}
		}
		return bytes;
	}

#else

	LPSTR PXWideCharToUTF8(LPCSTR szIn)
	{
		return 0;
	}

#endif

	LPWSTR PXUTF8ToWideChar(LPCSTR szIn)
	{
		LPWSTR wide = 0;
		int len = (int)strlen(szIn), wlen;
		if (len > 0)
		{
			wlen = MultiByteToWideChar(CP_UTF8, 0, szIn, len, 0, 0);
			if (wlen > 0)
			{
				wide = new WCHAR[wlen+4];
				MultiByteToWideChar(CP_UTF8, 0, szIn, len, wide, wlen);
				wide[wlen] = 0;
			}
		}
		return wide;
	}

	/*
	bool PXUTF8ToWideChar(LPCSTR szIn, CString &strRet)
	{
	bool bRet = false;
	int len = (int)strlen(szIn), wlen;
	if (len > 0)
	{
	wlen = MultiByteToWideChar(CP_UTF8, 0, szIn, len, 0, 0);
	if (wlen > 0)
	{
	bRet = 0!=MultiByteToWideChar(CP_UTF8, 0, szIn, len, strRet.GetBuffer(wlen+4), wlen);
	strRet.ReleaseBuffer();
	}
	}
	return bRet;
	}
	*/

	bool has_highbit(CONST char *s, size_t l)
	{
		const char *e = s+l;
		while (s < e)
		{
			if (*s++ & 0x80)
				return true;
		}
		return false;
	}

	bool sv_maybe_utf8(SV *sv)
	{
		if (SvUTF8(sv))
			return true;
		//	TRACE("%d %d %d %s\n", SvIOK(sv), SvNOK(sv), SvPOK(sv), SvPV_nolen(sv));
		//	if (*SvPV_nolen(sv) == 'Ã')
		//		AfxDebugBreak();
		if (SvPOK(sv))
		{
			if (has_highbit(SvPVX(sv), SvCUR(sv)))
			{
				if (!SvREADONLY(sv)) // +203
					SvUTF8_on(sv);
				return true;
			}
			else
				SvUTF8_off(sv);
		}
		return false;
	}


	int PXsv_len(void *pMyPerl, SV *sv)
	{
		PERL_SET_CONTEXT(pMyPerl);
		int len = 0;
		if (sv_maybe_utf8(sv))
			len = (int)sv_len_utf8(sv);
		else
			len = (int)sv_len(sv);
		return len;
	}

	SV *PXnewSVpv(void *pMyPerl, LPCTSTR value, bool bAllowUTF8=true)
	{
		SV *sv = 0;

		PERL_SET_CONTEXT(pMyPerl);

		if (bAllowUTF8)
		{
			switch (s_UTF8Mode)
			{
			case UTF8_on:
				{
					char *bytes = PXWideCharToUTF8(value);
					if (bytes)
					{
						// conversion successful
						sv = newSVpv(bytes, 0);
						SvUTF8_on(sv); // tell it's UTF8 encoded
						delete [] bytes;
					}
					else
					{
						// Windows API conversion failed, attempt upgrading with Perl
						sv = newSVpv(PXCASTR(value), 0);
						sv_utf8_upgrade(sv); // the UT8 flag will be automatically set
					}
				} break;

			case UTF8_auto: // check if string requires conversion?
				// => lengthy operation, so do nothing more
			case UTF8_off:
			default:
				{
					sv = newSVpv(PXCASTR(value), 0);
				}
			}
		}
		else
		{
			sv = newSVpv(PXCASTR(value), 0);
		}

		return sv;
	}

	CString PXSvPV(void *pMyPerl, SV *sv)
	{
		CString strRet;

		PERL_SET_CONTEXT(pMyPerl);
		LPCSTR pv = SvPV_nolen(sv);

		if (sv_maybe_utf8(sv))
		{
			WCHAR *wide = PXUTF8ToWideChar(pv);
			if (wide)
			{
				strRet = wide;
				delete [] wide;
			}
			else
			{
				// in case of error, return as if it were non-UTF8
				strRet = PXCWSTR(pv);
			}
		}
		else
		{
			strRet = PXCWSTR(pv);
		}

		//AfxMessageBox(strRet);

		return strRet;
	}

	std::string PXSvPVA(void *pMyPerl, SV *sv)
	{
		std::string strRet;

		PERL_SET_CONTEXT(pMyPerl);
		LPCSTR pv = SvPV_nolen(sv);

		if (sv_maybe_utf8(sv))
		{
			WCHAR *wide = PXUTF8ToWideChar(pv);
			if (wide)
			{
				strRet = PXCASTR(wide);
				delete [] wide;
			}
			else
			{
				// in case of error, return as if it were non-UTF8
				strRet = pv;
			}
		}
		else
		{
			strRet = pv;
		}

		//MessageBoxA(0, strRet.c_str(), "PXPerlAAA", 0);

		return strRet;
	}

	std::wstring PXSvPVW(void *pMyPerl, SV *sv)
	{
		std::wstring strRet;

		PERL_SET_CONTEXT(pMyPerl);
		LPCSTR pv = SvPV_nolen(sv);

		if (sv_maybe_utf8(sv))
		{
			WCHAR *wide = PXUTF8ToWideChar(pv);
			if (wide)
			{
				strRet = wide;
				delete [] wide;
			}
			else
			{
				// in case of error, return as if it were non-UTF8
				strRet = CA2W(pv);
			}
		}
		else
		{
			strRet = CA2W(pv);
		}

		MessageBoxW(0, strRet.c_str(), L"PXPerlWWW", 0);

		return strRet;
	}

	void PXsv_setpv(void *pMyPerl, SV *sv, LPCTSTR value)
	{
		PERL_SET_CONTEXT(pMyPerl);

		switch (s_UTF8Mode)
		{
		case UTF8_on:
			{
				char *bytes = PXWideCharToUTF8(value);
				if (bytes)
				{
					sv_setpv(sv, bytes);
					SvUTF8_on(sv);
					delete [] bytes;
				}
				else
				{
					SvUTF8_off(sv);
					sv_setpv(sv, PXCASTR(value));
					sv_utf8_upgrade(sv);
				}
			} break;

		case UTF8_auto:
			{
				// The SV is aleady UTF8, so let it UTF8
				if (sv_maybe_utf8(sv))
				{
					char *bytes = PXWideCharToUTF8(value);
					if (bytes)
					{
						sv_setpv(sv, bytes);
						SvUTF8_on(sv);
						delete [] bytes;
					}
					else
					{
						SvUTF8_off(sv);
						sv_setpv(sv, PXCASTR(value));
						sv_utf8_upgrade(sv);
					}
				}
				else
				{
					sv_setpv(sv, PXCASTR(value));
				}
			} break;

		case UTF8_off:
		default:
			{
				SvUTF8_off(sv);
				sv_setpv(sv, PXCASTR(value));
			}
		}
	}

	void PXsv_setsv(void *pMyPerl, SV *sv, SV *new_sv)
	{
		PERL_SET_CONTEXT(pMyPerl);

		switch (s_UTF8Mode)
		{
		case UTF8_on:
			//{
			//sv_setsv(sv, new_sv);
			//if (SvUTF8(new_sv))
			//	SvUTF8_on(sv);
			//else
			//	sv_utf8_upgrade(sv);
			//} break;

		case UTF8_auto:
		case UTF8_off:
		default:
			{
				sv_setsv(sv, new_sv);
				if (sv_maybe_utf8(new_sv))
					SvUTF8_on(sv);
			}
		}
	}

	void PXsv_catpv(void *pMyPerl, SV *sv, LPCTSTR value)
	{
		PERL_SET_CONTEXT(pMyPerl);

		switch (s_UTF8Mode)
		{
		case UTF8_on:
			{
				if (!sv_maybe_utf8(sv))
					sv_utf8_upgrade(sv);
				char *bytes = PXWideCharToUTF8(value);
				if (bytes)
				{
					sv_catpv(sv, bytes);
					SvUTF8_on(sv);
					delete [] bytes;
				}
				else
				{
					sv_utf8_downgrade(sv, TRUE);
					sv_catpv(sv, PXCASTR(value));
				}
			} break;

		case UTF8_auto:
			{
				if (sv_maybe_utf8(sv))
				{
					char *bytes = PXWideCharToUTF8(value);
					if (bytes)
					{
						sv_catpv(sv, bytes);
						delete [] bytes;
					}
					else
					{
						sv_utf8_downgrade(sv, TRUE);
						sv_catpv(sv, PXCASTR(value));
					}
				}
				else
				{
					sv_catpv(sv, PXCASTR(value));
				}
			} break;

		case UTF8_off:
		default:
			{
				if (sv_maybe_utf8(sv))
					sv_utf8_downgrade(sv, TRUE);
				sv_catpv(sv, PXCASTR(value));
			}
		}
	}

#define PXav_store_ref(av, pos, sv) \
	{ \
	SvREFCNT_inc(sv); \
	if (!av_store(av, pos, sv)) \
	SvREFCNT_dec(sv); \
	}

#define PXav_store_ref_noinc(av, pos, sv) \
	{ \
	SV* svStore = sv; \
	if (!av_store(av, pos, svStore)) \
	SvREFCNT_dec(svStore); \
	}

#define PXhv_store_ref(hv, key, val) \
	{ \
	SvREFCNT_inc(val); \
	if (!hv_store(hv, PXCASTR(key), (I32)_tcslen(key), val, 0)) \
	SvREFCNT_dec(val); \
	}

#define PXhv_store_ref_noinc(hv, key, val) \
	{ \
	SV* svVal = val; \
	if (!hv_store(hv, PXCASTR(key), (I32)_tcslen(key), svVal, 0)) \
	SvREFCNT_dec(svVal); \
	}

#define PXhv_store_ent_ref(hv, key, val) \
	{ \
	SvREFCNT_inc(val); \
	if (!hv_store_ent(hv, key, val, 0)) \
	SvREFCNT_dec(val); \
	}

	UTF8Mode PXPERL_API PXSetUTF8(UTF8Mode mode)
	{
#ifdef _UNICODE
		s_UTF8Mode = mode;
#else
		s_UTF8Mode = UTF8_off;
#endif
		return mode;
	}


	void PXSetXS(const XSItem *pItems)
	{
		if (s_pXSList)
		{
			delete [] s_pXSList;
			s_pXSList = 0;
		}
		if (pItems)
		{
			int i;
			for (i=0;
				//!::IsBadReadPtr(pItems + i*sizeof(XSItem), sizeof(XSItem))&&
				pItems[i].sName
				&& pItems[i].lpFunc;
			i++)
			{}
			s_pXSList = new XSItem[i+1];
			for (int j=0; j<=i; j++)
			{
				s_pXSList[j] = pItems[j];
				//TRACE(">>> %s=%X\n", s_pXSList[j].sName, s_pXSList[j].lpFunc);
			}

		}
	}

	void PXSetDefaultModules(const CStringArray* pStrAModules)
	{
		if (pStrAModules)
		{
			s_strAModules.Copy(*pStrAModules);
		}
		else
		{
			s_strAModules.RemoveAll();
		}
	}


	void PXSetCommandLineOptions(const CStringArray* pStrAOptions)
	{
		if (pStrAOptions)
		{
			s_strAOptions.Copy(*pStrAOptions);
		}
		else
		{
			s_strAOptions.RemoveAll();
		}
	}


	/////////////////////////////////////////////////
	// Perl Interfacing Functions

	SV* PXCallVSV(void *pMyPerl, int dummy, LPCTSTR szCall, va_list params)
	{
		UNUSED_ALWAYS(dummy);

		PERL_SET_CONTEXT(pMyPerl);

		dSP;
		int count;
		SV* ret = 0;

		ENTER;
		SAVETMPS;

		PUSHMARK(SP);
		INT_PTR sv = va_arg(params, INT_PTR);
		while (sv > 0)
		{
			XPUSHs(sv_2mortal((SV*)sv));
			sv = va_arg(params, INT_PTR);
		}
		PUTBACK;

#ifdef USE_EVAL
		count = call_pv(PXCASTR(szCall), G_SCALAR | G_EVAL);
#else
		count = call_pv(PXCASTR(szCall), G_SCALAR);
#endif

		SPAGAIN;

		if (count != 1)
		{
			PXPerlWrap::PXCriticalError(_T("PXCallVSV(0x%08X): Return value count not expected (%d)."), pMyPerl, count);
		}
#ifdef USE_EVAL
		else if (SvTRUE(ERRSV))
		{
			PXPerlWrap::PXCriticalError(PXSvPV(pMyPerl, ERRSV));
		}
#endif
		else
		{
			ret = POPs;
		}

		PUTBACK;
		FREETMPS;
		LEAVE;

		return ret;
	}

	SV* PXCallVSV(void *pMyPerl, LPCTSTR szCall, ...)
	{
		SV* ret = 0;
		va_list marker;
		va_start(marker, szCall);
		ret = PXCallVSV(pMyPerl, 0, szCall, marker);
		va_end(marker);
		return ret;
	}

	int PXCallVInt(void *pMyPerl, int dummy, LPCTSTR szCall, int nDefRet, va_list params)
	{
		UNUSED_ALWAYS(dummy);

		PERL_SET_CONTEXT(pMyPerl);

		dSP;
		int count, ret = nDefRet;

		ENTER;
		SAVETMPS;

		PUSHMARK(SP);
		INT_PTR sv = va_arg(params, INT_PTR);
		while (sv > 0)
		{
			XPUSHs(sv_2mortal((SV*)sv));
			sv = va_arg(params, INT_PTR);
		}
		PUTBACK;

#ifdef USE_EVAL
		count = call_pv(PXCASTR(szCall), G_SCALAR | G_EVAL);
#else
		count = call_pv(PXCASTR(szCall), G_SCALAR);
#endif

		SPAGAIN;

		if (count != 1)
		{
			PXPerlWrap::PXCriticalError(_T("PXCallVInt(0x%08X): Return value count not expected (%d)."), pMyPerl, count);
		}
#ifdef USE_EVAL
		else if (SvTRUE(ERRSV))
		{
			PXPerlWrap::PXCriticalError(PXSvPV(pMyPerl, ERRSV));
		}
#endif
		else
		{
			ret = POPi;
		}

		PUTBACK;
		FREETMPS;
		LEAVE;

		return ret;
	}

	int PXCallVInt(void *pMyPerl, LPCTSTR szCall, int nDefRet=0, ...)
	{
		int ret;
		va_list marker;
		va_start(marker, nDefRet);
		ret = PXCallVInt(pMyPerl, 0, szCall, nDefRet, marker);
		va_end(marker);
		return ret;
	}

	SV* PXEvalSV(void *pMyPerl, LPCTSTR szEval)
	{
		PERL_SET_CONTEXT(pMyPerl);
		SV* ret_sv = eval_pv(PXCASTR(szEval), false);
		if (SvTRUE(ERRSV))
		{
			PXPerlWrap::PXError(PXSvPV(pMyPerl, ERRSV));
		}
		return ret_sv;
	}

	bool PXConstruct(void *&pMyPerl)
	{
		if ((pMyPerl = (void*)perl_alloc()) == 0)
		{
			PXPerlWrap::PXCriticalError(_T("PXConstruct(): Could not allocate memory for Perl."));
		}
		else
		{
			PERL_SET_CONTEXT((PerlInterpreter*)pMyPerl);
			perl_construct((PerlInterpreter*)pMyPerl);
			//PL_perl_destruct_level = 1;
			// OR
			//PL_perl_destruct_level = 0; //PL_exit_flags |= PERL_EXIT_DESTRUCT_END;
			return true;
		}
		return false;
	}

	// The "core" function
	bool PXParse(void *pMyPerl, LPCTSTR program, bool bIsFile=false,
		LPCTSTR extra_command = 0, bool bSimple=false, bool bSilent=false)
	{
		bool bRet = true;
		int ret = 0;
		char **embedding;
		INT_PTR i = 0;

		CStringArray strACommand;
		INT_PTR nCurIndex = 0;

		PXPerlWrap::PXInfo(_T("PXParse(0x%08X): Parsing a script..."), pMyPerl);

		if (s_strAOptions.GetSize())
		{
			nCurIndex = s_strAOptions.GetSize();
			strACommand.Copy(s_strAOptions);
			strACommand.SetSize(nCurIndex + 4, 8);
		}
		else
		{
			CStringArray strAOpts;
			if (PXReadTextFileLines(s_szOPTFile, strAOpts))
			{
				CString strTemp;
				INT_PTR nOptsSize = strAOpts.GetSize();
				strACommand.SetSize(nOptsSize + 4, 8);
				for (i=0; i<nOptsSize; i++)
				{
					strTemp = strAOpts[i].Left(2);
					if (strAOpts[i].GetLength()
						&& strTemp != _T("-e")
						&& strTemp != _T("-h")
						&& strTemp != _T("--"))
					{
						if (bSimple && strTemp != _T("-w") && strTemp != _T("-I"))
							continue;
						if (strTemp == _T("-I"))
						{
							TCHAR buffer[MAX_PATH+1];
							strTemp = strAOpts[i].Right(strAOpts[i].GetLength() - 2);
							PXMakeAbsolutePath(strTemp);
							//if (!PathIsDirectory(LPCTSTR(strTemp)))
							//	PXPerlWrap::PXCriticalError(_T("Include path not found: ") + strTemp);
							GetShortPathName(LPCTSTR(strTemp), buffer, MAX_PATH+1);
							strTemp = _T("-I");
							strTemp += buffer;
							strACommand.SetAtGrow(nCurIndex++, strTemp);
						}
						else
							strACommand.SetAtGrow(nCurIndex++, strAOpts[i]);
					}
				}
			}
			else
			{
				PXPerlWrap::PXWarning(CString(_T("Could not read from command line options file: ")) + s_szOPTFile);
				//return false;
				// OR Automatically use defaults
				strACommand.SetSize(6, 8);
				strACommand.SetAtGrow(nCurIndex++, _T("-w"));
				TCHAR buffer[MAX_PATH+3];
				buffer[0] = '-';
				buffer[1] = 'I';
				CString strTemp(_T("lib"));
				PXMakeAbsolutePath(strTemp);
				GetShortPathName(LPCTSTR(strTemp), buffer+2, MAX_PATH+1);
				//AfxMessageBox(strTemp);
				strACommand.SetAtGrow(nCurIndex++, strTemp);
			}
		}

		if (extra_command && *extra_command)
			strACommand.SetAtGrow(nCurIndex++, extra_command);

		TCHAR buffer[MAX_PATH+1];
		if (bIsFile)
		{
			GetShortPathName(program, buffer, MAX_PATH+1);
			program = (LPCTSTR)buffer;
		}
		else
			strACommand.SetAtGrow(nCurIndex++, _T("-e"));

		if (program && *program)
			strACommand.SetAtGrow(nCurIndex++, program);
		else
			strACommand.SetAtGrow(nCurIndex++, _T("1;"));

		/*if (pstrA_ARGV && pstrA_ARGV->GetSize())
		{
		strACommand.SetAtGrow(nCurIndex++, _T("--"));
		strACommand.Append(*pstrA_ARGV);
		nCurIndex += pstrA_ARGV->GetSize();
		}*/

		embedding = new char* [nCurIndex];

#ifdef VERBOSE
		CString strTemp;
#endif

		size_t len;
		for (i=0; i<nCurIndex; i++)
		{
#ifdef VERBOSE
			strTemp += strACommand[i];
			strTemp += _T(" ");
#endif
			len = strACommand[i].GetLength();
			embedding[i] = new char[len+1];
			strncpy_s(embedding[i], len+1, PXCASTR(strACommand[i]), len+1);
		}

#ifdef VERBOSE
		AfxMessageBox(strTemp);
#endif

		PERL_SET_CONTEXT(pMyPerl);

		ret = perl_parse((PerlInterpreter*)pMyPerl, (XSINIT_t)CPerlInterpreter::m_xs_init_addr,
			(int)nCurIndex - 1, embedding, 0);

		for (i=0; i<nCurIndex; i++)
		{
			delete [] embedding[i];
		}

		delete [] embedding;

		if (ret != 0)
		{
			if (!bSilent)
			{
				CString strErr(_T("Perl parse error"));
				PERL_SET_CONTEXT(pMyPerl);
				if (SvTRUE(ERRSV))
				{
					strErr += _T(":\n");
					strErr += PXSvPV(pMyPerl, ERRSV);
					//sv_undef(ERRSV);
				}
				else
				{
					strErr += _T(".");
				}
				PXPerlWrap::PXError(LPCTSTR(strErr));
			}
			bRet = false;
		}

		if (bRet)
			PXPerlWrap::PXInfo(_T("PXParse(0x%08X): Script parsed successfuly."), pMyPerl);

		return bRet;
	}

	bool PXRun(void *pMyPerl)
	{
		PERL_SET_CONTEXT(pMyPerl);
		if (perl_run((PerlInterpreter*)pMyPerl) == 0)
			return true;
		PXPerlWrap::PXError(_T("PXRun(0x%08X): Failed running a script."), pMyPerl);
		return false;
	}

	void PXDestruct(void *&pMyPerl)
	{
		//PXPerlWrap::PXInfo(_T("PXDestruct(0x%08X): Interpreter is being destructed..."), pMyPerl);
		PERL_SET_CONTEXT(pMyPerl);
		PL_perl_destruct_level = 0;
		perl_destruct((PerlInterpreter*)pMyPerl);
		perl_free((PerlInterpreter*)pMyPerl);
		pMyPerl = 0;
	}

	EXTERN_C void boot_DynaLoader(pTHXo_ CV* cv);

	EXTERN_C void
		xs_init(pTHXo)
	{
		UNUSED_ALWAYS(my_perl);

		char *file = __FILE__;
		dXSUB_SYS;

		newXS("DynaLoader::boot_DynaLoader", boot_DynaLoader, file);
		PXPerlWrap::PXInfo(_T("xs_init(): newXS(\"DynaLoader::boot_DynaLoader\", 0x%08X)"), boot_DynaLoader);

		/*if (s_xsList.size() > 0)
		{
		for (XSList::size_type i=0; i<s_xsList.size(); i++)
		{
		if (*s_xsList.at(i).sName && s_xsList.at(i).lpFunc)
		{
		PXPerlWrap::PXInfo(_T("xs_init(): newXS(\"%s\", 0x%08X)"),
		s_xsList.at(i).sName, s_xsList.at(i).lpFunc);
		newXS(s_xsList.at(i).sName, (XSUBADDR_t)s_xsList.at(i).lpFunc, file);
		}
		}
		}*/

		if (s_pXSList)
		{
			for (int j=0; s_pXSList[j].lpFunc!=0; j++)
			{
				PXPerlWrap::PXInfo(_T("xs_init(): newXS(\"%s\", 0x%08X)"), CA2T(s_pXSList[j].sName), s_pXSList[j].lpFunc);
				newXS(s_pXSList[j].sName, (XSUBADDR_t)s_pXSList[j].lpFunc, file);
			}
		}


	}

	fnXSInitProc CPerlInterpreter::m_xs_init_addr = (fnXSInitProc)xs_init;


	/////////////////////////////////////////////////
	// Classes
	/////////////////////////////////////////////////
	// CScriptAttributes, CScript

	CScriptAttributes::CScriptAttributes(LPCTSTR szPersistentPackageName, DWORD dwFlags)
	{
		m_strPersistentPackageName = szPersistentPackageName;
		m_dwFlags = dwFlags;
	}

	CScript::CScript()
	{
		Destroy();
	}

	CScript::CScript(LPCTSTR szInlineScript)
	{
		Destroy();
		Load(szInlineScript, CScript::SourceInline);
	}

	CScript& CScript::operator=(LPCTSTR szInlineScript)
	{
		Destroy();
		Load(szInlineScript, CScript::SourceInline);
		return *this;
	}

	CScript::~CScript()
	{
		Destroy();
	}

	CScript& CScript::operator=(const CScript &script)
	{
		Destroy();
		m_bLoaded = script.m_bLoaded;
		m_bReadOnly = script.m_bReadOnly;
		LPVOID pInterp;
		CScriptAttributes attr;
		for (POSITION pos = script.m_interpAttrMap.GetStartPosition(); pos != 0;)
		{
			script.m_interpAttrMap.GetNextAssoc(pos, pInterp, attr);
			m_interpAttrMap.SetAt(pInterp, attr);
		}
		//	m_strA_ARGV.Copy(script.m_strA_ARGV);
		//	m_strACustomOpts.Copy(script.m_strACustomOpts);
		m_strFile = script.m_strFile;
		m_strScript = script.m_strScript;
		m_type = script.m_type;
		return *this;
	}

	void CScript::Destroy(void)
	{
		Unlink();
		m_bLoaded = false;
		m_bReadOnly = false;
		m_type = TypeNone;
		m_strScript.Empty();
		m_strFile.Empty();
		//	m_strA_ARGV.RemoveAll();
		//	m_strACustomOpts.RemoveAll();
		m_interpAttrMap.RemoveAll();
	}

	CString& CScript::Flush(void)
	{
		if (m_strFile.IsEmpty())
		{
			PXTempName(m_strFile);
			PXCreateResetFile(LPCTSTR(m_strFile));
		}
		if (m_bReadOnly)
		{
			CString strFile;
			PXTempName(strFile);
			CopyFile(LPCTSTR(m_strFile), LPCTSTR(strFile), true);
			m_bReadOnly = false;
			m_strFile = strFile;
		}
		else if (!m_strScript.IsEmpty())
		{
			if (PXWriteCreateTextFile(LPCTSTR(m_strFile), m_strScript))
				m_strScript.Empty();
		}
		return m_strFile;
	}

	void CScript::Unlink(void)
	{
		if (!m_bReadOnly && !m_strFile.IsEmpty())
			_tunlink(LPCTSTR(m_strFile));
	}

	bool CScript::Load(LPCTSTR szSource, CScript::Source source, CScript::Type type)
	{
		Destroy();

		if (type == TypeBytecode)
		{
			PXPerlWrap::PXError(_T("CScript::ChangeType(): Bytecode support removed for the moment."));
			return false;
		}

		switch (source)
		{
		case CScript::SourceInline:
			m_strScript = szSource;
			m_bLoaded = true;
			break;
		case CScript::SourceFile:
			{
				CString strFile(szSource);
				PXMakeAbsolutePath(strFile);
				if (!_taccess(LPCTSTR(strFile), 4))
				{
					m_strFile = strFile;
					m_bReadOnly = true;
					m_bLoaded = true;
				}
			};
			break;
		case CScript::SourceResource:
			{
				DWORD dwSize = 0;
				LPVOID lpData = PXReadPerlResource(AfxGetResourceHandle(),
					(INT_PTR)(LPVOID)szSource, dwSize);
				if (lpData && dwSize)
				{
					PXWriteCreateFile(LPCTSTR(Flush()), (LPCVOID)lpData, dwSize);
					m_bLoaded = true;
				}
			}; break;
		case CScript::SourceURL:
			m_bLoaded = PXURLDownloadToFile(szSource, LPCTSTR(Flush()));
			break;
		default:
			break;
		}

		if (m_bLoaded)
		{
			m_type = type;
			PXPerlWrap::PXInfo(_T("CScript::Load(): Successfuly loaded script (source=%d; type=%d)."), source, type);
		}
		else
			PXPerlWrap::PXError(_T("CScript::Load(): Failed loading script (source=%d; type=%d)."), source, type);

		return m_bLoaded;
	}

	bool CScript::IsLoaded(void) const
	{
		return m_bLoaded;
	}

	bool CScript::SaveToFile(LPCTSTR szFile)
	{
		if (!m_bLoaded)
			return false;

		bool bRet = false;

		if (m_strFile.IsEmpty())
			Flush();

		if (!m_strFile.IsEmpty())
		{
			CString strDestFile(szFile);
			PXMakeAbsolutePath(strDestFile);
			bRet = 0!=::CopyFile(LPCTSTR(m_strFile), LPCTSTR(strDestFile), false);
		}

		return bRet;
	}
	/*
	CStringArray& CScript::GetARGV(void)
	{
	return m_strA_ARGV;
	}*/
	/*CStringArray& CScript::GetCustomOpts(void)
	{
	return m_strACustomOpts;
	}*/

	const CString& CScript::GetScript(void)
	{
		if (!m_strFile.IsEmpty())
		{
			m_strScript.Empty();
			if (!PXReadTextFile(LPCTSTR(m_strFile), m_strScript))
				PXPerlWrap::PXError(_T("CScript::GetScript(): Failed reading file %s."), LPCTSTR(m_strFile));
		}
		return m_strScript;
	}

	CString& CScript::GetScript(CString& strRet)
	{
		strRet = GetScript();
		return strRet;
	}

	LPVOID CScript::GetScript(DWORD &dwSize)
	{
		if (m_strFile.IsEmpty())
		{
			if (!m_strScript.IsEmpty())
			{
				Flush();
			}
		}

		if (!m_strFile.IsEmpty())
		{
			return PXReadFile(LPCTSTR(m_strFile), dwSize);
		}

		return 0;
	}

	CString& CScript::GetScriptP(void)
	{
		GetScript();
		return m_strScript;
	}

	CScript::Type CScript::GetType(void) const
	{
		return m_type;
	}

	bool CScript::Test(void)
	{
		bool bRet = false;

		if (m_type == CScript::TypePlain)
		{
			PXIORedirect::ChangeDestination(PXPW_REDIR_ERRORS, PXIORedirect::DestNone);

			void *my_perl = 0;
			if (PXConstruct(my_perl)
				&& PXParse(my_perl, LPCTSTR(Flush()), true, _T("-c"), true, true)
				&& PXRun(my_perl))
			{
				PERL_SET_CONTEXT(my_perl);
				if (!SvTRUE(ERRSV))
					bRet = true;
			}

			if (my_perl)
				PXDestruct(my_perl);

			PXIORedirect::RestoreDestination(PXPW_REDIR_ERRORS);
		}

		return bRet;
	}

	bool CScript::Reformat(void)
	{
		bool bRet = false;

		PXPerlWrap::PXInfo(_T("CScript::Reformat(): Reformating a script (current stdout destination type: %d)..."), PXIORedirect::s_prtd ? PXIORedirect::s_prtd[0].dest : -1);

		if (m_type == CScript::TypePlain)
		{
			Flush();

			TCHAR out[MAX_PATH+1];
			_tcscpy_s(out, MAX_PATH, _T("-MO=Deparse"));

			void *my_perl = 0;
			if (PXConstruct(my_perl)
				&& PXParse(my_perl, LPCTSTR(m_strFile), true, out, true)
				&& PXRun(my_perl))
			{

				bRet = true;
				goto end;
			}

end:
			if (my_perl)
				PXDestruct(my_perl);
		}

		if (bRet)
			PXPerlWrap::PXInfo(_T("CScript::Reformat(): Successfuly reformated."));
		else
			PXPerlWrap::PXError(_T("CScript::Reformat(): Error while reformating."));

		return bRet;
	}

	bool CScript::ChangeType(CScript::Type newType)
	{
		if (newType == m_type)
			return true;

		bool bRet = false;
		switch (newType)
		{
		case CScript::TypeNone:
			break;
		case CScript::TypePlain:
			PXPerlWrap::PXError(_T("CScript::ChangeType(): Bytecode disassembly not implemented yet."));
			break;
		case CScript::TypeBytecode:
			/*{
			PXPerlWrap::PXInfo(_T("CScript::ChangeType(): Compiling to bytecode..."));

			Flush();
			GetScript();

			TCHAR out[MAX_PATH+1], buf[MAX_PATH+1];
			_tcscpy(out, _T("-MO=PXBytecode,-o"));
			GetShortPathName(LPCTSTR(m_strFile), buf, MAX_PATH+1);
			_tcscat(out, buf);
			TCHAR *program = new TCHAR[m_strScript.GetLength()+1];
			_tcsncpy(program, LPCTSTR(m_strScript), m_strScript.GetLength()+1);

			void *my_perl = 0;
			if (PXConstruct(my_perl)
			&& PXParse(my_perl, program, false, 0, out, 0, true)
			&& PXRun(my_perl))
			{
			bRet = true;
			}

			if (my_perl)
			PXDestruct(my_perl);

			delete [] program;

			if (bRet)
			PXPerlWrap::PXInfo(_T("CScript::ChangeType(): Successfuly compiled to bytecode."));
			else
			PXPerlWrap::PXError(_T("CScript::ChangeType(): Error while compiling to bytecode."));


			};*/
			PXPerlWrap::PXError(_T("CScript::ChangeType(): Bytecode support removed for the moment."));
			break;
		}

		return bRet;
	}

	bool CScript::IsFlagSet(CPerlInterpreter *pInterp, DWORD dwFlags)
	{
		CScriptAttributes attr;
		if (m_interpAttrMap.Lookup(pInterp->GetMyPerl(), attr))
			return (attr.m_dwFlags & dwFlags) != 0;
		return false;
	}

	bool CScript::IsParsed(CPerlInterpreter *pInterp) const
	{
		CScriptAttributes attr;
		if (m_interpAttrMap.Lookup(pInterp->GetMyPerl(), attr))
			return (attr.m_dwFlags & CScriptAttributes::FlagParsed) != 0;
		return false;
	}

	bool CScript::IsRun(CPerlInterpreter *pInterp) const
	{
		CScriptAttributes attr;
		if (m_interpAttrMap.Lookup(pInterp->GetMyPerl(), attr))
			return (attr.m_dwFlags & CScriptAttributes::FlagRun) != 0;
		return false;
	}

	CString CScript::GetPersistentPackage(CPerlInterpreter *pInterp) const
	{
		CString strRet;
		CScriptAttributes attr;
		if (m_interpAttrMap.Lookup(pInterp->GetMyPerl(), attr))
			strRet = LPCTSTR(attr.m_strPersistentPackageName);
		else
			strRet = _T("");
		return strRet;
	}

	void CScript::SetPersistentPackage(CPerlInterpreter *pInterp, LPCTSTR szPackage)
	{
		CScriptAttributes attr(szPackage);
		if (m_interpAttrMap.Lookup(pInterp->GetMyPerl(), attr))
			attr.m_strPersistentPackageName = szPackage;
		m_interpAttrMap.SetAt(pInterp->GetMyPerl(), attr);
	}

	void CScript::SetFlag(CPerlInterpreter *pInterp, DWORD dwFlags)
	{
		CScriptAttributes attr(_T(""), dwFlags);
		if (m_interpAttrMap.Lookup(pInterp->GetMyPerl(), attr))
			attr.m_dwFlags |= dwFlags;
		m_interpAttrMap.SetAt(pInterp->GetMyPerl(), attr);
	}

	void CScript::ResetFlag(CPerlInterpreter *pInterp, DWORD dwFlags)
	{
		CScriptAttributes attr(_T(""), CScriptAttributes::FlagNone);
		if (m_interpAttrMap.Lookup(pInterp->GetMyPerl(), attr))
			attr.m_dwFlags &= ~dwFlags;
		m_interpAttrMap.SetAt(pInterp->GetMyPerl(), attr);
	}

	void CScript::SetParsed(CPerlInterpreter *pInterp, bool bParsed)
	{
		if (bParsed)
			SetFlag(pInterp, CScriptAttributes::FlagParsed);
		else
			ResetFlag(pInterp, CScriptAttributes::FlagParsed);
	}

	void CScript::SetRun(CPerlInterpreter *pInterp, bool bRun)
	{
		if (bRun)
			SetFlag(pInterp, CScriptAttributes::FlagRun);
		else
			ResetFlag(pInterp, CScriptAttributes::FlagRun);
	}

	/*void CScript::SetDebug(CPerlDebugInterpreter *pInterp, bool bDebug)
	{
	if (bDebug)
	SetFlag(pInterp, CScriptAttributes::FlagDebug);
	else
	ResetFlag(pInterp, CScriptAttributes::FlagDebug);
	}
	*/
	void CScript::DestroyAttributes(CPerlInterpreter *pInterp)
	{
		m_interpAttrMap.RemoveKey(pInterp->GetMyPerl());
	}


	/////////////////////////////////////////////////
	// CPerlVariable Defines

#define OBJECT_MYPERL(obj) (obj.GetMyPerl())
#define OBJECT_INTERP(obj) (obj.GetInterp())

#define THIS_OBJECT_MYPERL (GetMyPerl())
#define THIS_OBJECT_INTERP (GetInterp())

#define SET_THIS_OBJECT_CONTEXT PERL_SET_CONTEXT(THIS_OBJECT_MYPERL)

#define IS_PERLOBJ_VALID(obj) (OBJECT_MYPERL(obj) && !obj.GetName().IsEmpty())
#define IS_PERLOBJ_INVALID(obj) (!OBJECT_MYPERL(obj) || obj.GetName().IsEmpty())

#define IS_THIS_PERLOBJ_VALID (THIS_OBJECT_MYPERL && !GetName().IsEmpty())
#define IS_THIS_PERLOBJ_INVALID (!THIS_OBJECT_MYPERL || GetName().IsEmpty())

#define GET_SV(obj) ((SV*)obj.GetParam())
#define GET_AV(obj) ((AV*)obj.GetParam())
#define GET_HV(obj) ((HV*)obj.GetParam())

#define THIS_SV ((SV*)GetParam())
#define THIS_AV ((AV*)GetParam())
#define THIS_HV ((HV*)GetParam())

	//#define SET_THIS_SV(newsv) { PXsv_setsv(THIS_SV, newsv); }

	/////////////////////////////////////////////////
	// CPerlVariable

	CPerlVariable::CPerlVariable()
	{
		Destroy();
	}

	CPerlVariable::~CPerlVariable()
	{
		Destroy();
	}

	bool CPerlVariable::IsValid(void) const
	{
		return m_idInterp!=NOID && m_pParam!=0;
	}

	const CString& CPerlVariable::GetName(void) const
	{
		return m_strName;
	}

	void CPerlVariable::Create(CPerlInterpreter *pInterp, LPCTSTR szName, void *pParam)
	{
		m_idInterp = s_interp_pool.at(pInterp);
		m_strName = szName;
		m_pParam = pParam;
	}

	void CPerlVariable::Clone(const CPerlVariable& var)
	{
		m_idInterp = var.m_idInterp;
		m_strName = var.m_strName;
		m_pParam = var.m_pParam;
	}

	void CPerlVariable::Destroy(void)
	{
		m_idInterp = NOID;
		m_strName = _T("");
		m_pParam = 0;
	}

	void * CPerlVariable::GetMyPerl(void) const
	{
		void * pMyPerl = 0;
		if (IsValid())
		{
			CPerlInterpreter *pInterp = s_interp_pool.at(m_idInterp);
			if (pInterp)
			{
				pMyPerl = pInterp->GetMyPerl();
			}
			else
			{
				// don't complain since it is used to test if object is valid
				//PXPerlWrap::PXError(_T("CPerlVariable::GetMyPerl(): attached interpreter is no longer valid!"), this);
			}
		}
		else
		{
			// don't complain since it is used to test if object is valid
			//PXPerlWrap::PXError(_T("CPerlVariable::GetMyPerl(): object is associated with no Perl variable."), this);
		}
		return pMyPerl;
	}

	CPerlInterpreter* CPerlVariable::GetInterp(void) const
	{
		CPerlInterpreter * pInterp = 0;
		if (IsValid())
		{
			pInterp = s_interp_pool.at(m_idInterp);
		}
		else
		{
			// don't complain since it is used to test if object is valid
			//PXPerlWrap::PXError(_T("CPerlVariable::GetInterp(): object is associated with no Perl variable."), this);
		}
		return pInterp;
	}

	void* CPerlVariable::GetParam(void)
	{
		if (IsValid())
			return m_pParam;
		return 0;
	}

	const void* CPerlVariable::GetParam(void) const
	{
		if (IsValid())
			return m_pParam;
		return 0;
	}


	/////////////////////////////////////////////////
	// CPerlScalar

	CPerlScalar::CPerlScalar()
	{
	}

	CPerlScalar::CPerlScalar(const CPerlScalar &scalar)
	{
		*this = scalar;
	}

	CPerlScalar::~CPerlScalar()
	{
		/*if (IS_THIS_PERLOBJ_VALID)
		{
		SvREFCNT_dec(THIS_SV);
		}*/
		CPerlScalar::Destroy();
	}

	void CPerlScalar::Create(CPerlInterpreter *pInterp, LPCTSTR szName, void *pParam)
	{
		CPerlVariable::Create(pInterp, szName, pParam);
		if (IS_THIS_PERLOBJ_VALID)
		{
			//if (SvREFCNT((SV*)pParam) < 1) // ensure refcount not 0
			//SvREFCNT_inc(THIS_SV);
			sv_maybe_utf8(THIS_SV);
		}
	}
	/*
	void CPerlScalar::Destroy(void)
	{
	if (IS_THIS_PERLOBJ_VALID)
	{
	if (m_bMortal)
	sv_clear(THIS_SV);
	}
	m_bMortal = false;
	CPerlVariable::Destroy();
	}*/

	const CPerlScalar& CPerlScalar::operator= (const CPerlScalar &scalar)
	{
		if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(scalar))
		{
			SET_THIS_OBJECT_CONTEXT;
			if (SvREADONLY(THIS_SV))
				ON_SVREADONLY;
			else
				PXsv_setsv(GetMyPerl(), THIS_SV, GET_SV(scalar));
		}
		else
		{
			Clone(scalar);
			//m_bMortal = scalar.m_bMortal;
		}
		return scalar;
	}

	int CPerlScalar::Int(int nDefault) const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return SvIV(THIS_SV);

		}
		return nDefault;
	}

	int CPerlScalar::Int(int nDefault, int nMin, int nMax) const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			int nRes = SvIV(THIS_SV);
			return (nRes > nMax || nRes < nMin) ? nDefault : nRes;
		}
		return nDefault;
	}

	CPerlScalar::operator int() const
	{
		return Int();
	}

	int CPerlScalar::operator*= (int value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setiv(THIS_SV, SvIV(THIS_SV) * value);
			return SvIV(THIS_SV);
		}
		return 0;
	}

	int CPerlScalar::operator/= (int value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setiv(THIS_SV, SvIV(THIS_SV) / value);
			return SvIV(THIS_SV);
		}
		return 0;
	}

	int CPerlScalar::operator+= (int value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setiv(THIS_SV, SvIV(THIS_SV) + value);
			return SvIV(THIS_SV);
		}
		return 0;
	}

	int CPerlScalar::operator-= (int value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setiv(THIS_SV, SvIV(THIS_SV) - value);
			return SvIV(THIS_SV);
		}
		return 0;
	}

	int CPerlScalar::operator= (int value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setiv(THIS_SV, value);
			return value;
		}
		return 0;
	}

	double CPerlScalar::Double(double fDefault) const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return SvNV(THIS_SV);
		}
		return fDefault;
	}

	double CPerlScalar::Double(double fDefault, double fMin, double fMax) const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			double fRes = SvIV(THIS_SV);
			return (fRes > fMax || fRes < fMin) ? fDefault : fRes;
		}
		return fDefault;
	}

	CPerlScalar::operator double() const
	{
		return Double();
	}

	double CPerlScalar::operator*= (double value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setnv(THIS_SV, SvNV(THIS_SV) * value);
			return SvNV(THIS_SV);
		}
		return (double)0;
	}

	double CPerlScalar::operator/= (double value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setnv(THIS_SV, SvNV(THIS_SV) / value);
			return SvNV(THIS_SV);
		}
		return (double)0;
	}

	double CPerlScalar::operator+= (double value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setnv(THIS_SV, SvNV(THIS_SV) + value);
			return SvNV(THIS_SV);
		}
		return (double)0;
	}

	double CPerlScalar::operator-= (double value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setnv(THIS_SV, SvNV(THIS_SV) - value);
			return SvNV(THIS_SV);
		}
		return (double)0;
	}

	double CPerlScalar::operator= (double value)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_setnv(THIS_SV, value);
			return value;
		}
		return (double)0;
	}

	/*
	operator CPerlScalar::bool() const
	{
	return Bool();
	}

	bool CPerlScalar::Bool() const
	{
	bool bRet = false;
	if (IS_THIS_PERLOBJ_VALID)
	{
	PERL_SET_CONTEXT(m_pMyPerl);
	bRet = 0!=SvTRUE(THIS_SV);
	}
	return bRet;
	}*/

	bool CPerlScalar::IsTrue() const
	{
		bool bRet = false;
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			bRet = (0!=SvTRUE(THIS_SV));
		}
		return bRet;
	}

	bool CPerlScalar::IsInt() const
	{
		bool bRet = false;
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			bRet = (0!=SvIOK(THIS_SV));
		}
		return bRet;
	}


	bool CPerlScalar::IsDouble() const
	{
		bool bRet = false;
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			bRet = (0!=SvNOK(THIS_SV));
		}
		return bRet;
	}

	bool CPerlScalar::IsString() const
	{
		bool bRet = false;
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			bRet = (0!=SvPOK(THIS_SV));
		}
		return bRet;
	}

	bool CPerlScalar::IsUTF8() const
	{
		bool bRet = false;
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			bRet = (0!=sv_maybe_utf8(THIS_SV));
		}
		return bRet;
	}

	void CPerlScalar::UTF8CheckSetFlag()
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_maybe_utf8(THIS_SV);
		}
	}

	void CPerlScalar::UTF8SetForceFlag(bool bIsUTF8)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (bIsUTF8)
				SvUTF8_on(THIS_SV);
			else
				SvUTF8_off(THIS_SV);
		}
	}

	void CPerlScalar::UTF8Upgrade()
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_utf8_upgrade(THIS_SV);
		}
	}

	void CPerlScalar::UTF8Downgrade()
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_utf8_downgrade(THIS_SV, true);
		}
	}

	char* CPerlScalar::GetPV()
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return SvPV_nolen(THIS_SV);
		}
		return 0;
	}

	CString CPerlScalar::String() const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return PXSvPV(THIS_OBJECT_MYPERL, THIS_SV);
		}
		return /*szDefault ? CString(szDefault) :*/ CString();
	}

	std::string CPerlScalar::StdStringA() const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return PXSvPVA(THIS_OBJECT_MYPERL, THIS_SV);
		}
		return /*szDefault ? std::string(szDefault) :*/ std::string();
	}

#ifdef _UNICODE
	std::wstring CPerlScalar::StdStringW() const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return PXSvPVW(THIS_OBJECT_MYPERL, THIS_SV);
		}
		return /*szDefault ? std::wstring(szDefault) :*/ std::wstring();
	}
#endif

	tstring CPerlScalar::StdString() const
	{
#ifdef _UNICODE
		return StdStringW();
#else
		return StdStringA();
#endif
	}

	LPCTSTR CPerlScalar::operator= (LPCTSTR value)
	{
		if (IS_THIS_PERLOBJ_VALID && value)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (SvREADONLY(THIS_SV))
				ON_SVREADONLY;
			else
				PXsv_setpv(THIS_OBJECT_MYPERL, THIS_SV, value);
			return value;
		}
		return value;
	}

	/*const CString& CPerlScalar::operator= (const CString& value)
	{
		*this = LPCTSTR(value);
		return value;
	}

	const std::string& CPerlScalar::operator= (const std::string& value)
	{
		*this = LPCTSTR(value.c_str());
		return value;
	}

	const std::wstring& CPerlScalar::operator= (const std::wstring& value)
	{
		*this = LPCTSTR(value.c_str());
		return value;
	}*/

	LPCTSTR CPerlScalar::operator+= (LPCTSTR value)
	{
		if (IS_THIS_PERLOBJ_VALID && value)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (SvREADONLY(THIS_SV))
				ON_SVREADONLY;
			else
				PXsv_catpv(THIS_OBJECT_MYPERL, THIS_SV, value);
		}
		return value;
	}

	void CPerlScalar::undef(void)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (SvREADONLY(THIS_SV))
				ON_SVREADONLY;
			else
				PXsv_setpv(THIS_OBJECT_MYPERL, THIS_SV, _T(""));
		}
	}
	/*
	void CPerlScalar::clear(void)
	{
	if (IS_THIS_PERLOBJ_VALID)
	{
	SET_THIS_OBJECT_CONTEXT;
	if (SvREADONLY(THIS_SV))
	ON_SVREADONLY;
	else
	sv_clear(THIS_SV);
	}
	}*/

	int CPerlScalar::length(void)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return PXsv_len(GetMyPerl(), THIS_SV);
		}
		return -1;
	}

	CPerlArray CPerlScalar::split(LPCTSTR szPattern)
	{
		CPerlArray array;

		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			sv_maybe_utf8(THIS_SV); // to prevent splitting UTF8 encoded chars (ie. raw char's), if a run has been made since creation of variable
			CString strEval;
			//CString strPackageName = LPCTSTR(GetName());
			CString strNewVarName(_T("_split_") + PXGetUniqueID());
			/*if (*(LPCTSTR(strPackageName)))
			{
				INT_PTR len = strPackageName.GetLength()-2;
				while (--len && !(strPackageName[len] == ':' && strPackageName[len+1] == ':'));
				if (len > 0)
				{
					strPackageName = strPackageName.Left(len);
					strEval += _T("package ");
					strEval += strPackageName;
					strEval += _T("; ");
					strNewVarName = strPackageName + _T("::") + strNewVarName;
				}
				else
					strPackageName = _T("");
			}*/
			strEval += _T("@");
			strEval += strNewVarName;
			strEval += _T(" = split(");
			strEval += szPattern;
			strEval += _T(", $");
			strEval += GetName();
			strEval += _T(");");
			if (PXEvalSV(GetMyPerl(), strEval))
			{
				AV *av = get_av(PXCASTR(strNewVarName), false);
				if (av)
					array.Create(GetInterp(), strNewVarName, av);
			}
		}

		return array;
	}

	/////////////////////////////////////////////////
	// CPerlArray

	CPerlArray::CPerlArray()
	{

	}

	CPerlArray::CPerlArray(const CPerlArray &array)
	{
		*this = array;
	}

	CPerlArray::~CPerlArray()
	{
		//CPerlVariable::Destroy();
	}

	const CPerlArray& CPerlArray::operator= (const CPerlArray &array)
	{
		if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(array))
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				clear();
				int len = av_len(GET_AV(array));
				if (len >= 0)
				{
					av_extend(THIS_AV, len);
					SV** lpsv;
					for (; len>=0; len--)
					{
						lpsv = av_fetch(GET_AV(array), len, false); // not lval
						if (lpsv)
						{
							PXav_store_ref(THIS_AV, len, *lpsv);
						}
					}
				}
			}
		}
		else
		{
			Clone(array);
		}
		return array;
	}

	int CPerlArray::operator+= (const CPerlArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(array))
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int src_len = av_len(GET_AV(array)),
					new_total_len = src_len + av_len(THIS_AV) + 1;
				av_extend(THIS_AV, new_total_len);
				SV** lpsv;
				for (; src_len>=0; new_total_len--, src_len--)
				{
					lpsv = av_fetch(GET_AV(array), src_len, false); // not lval
					if (lpsv)
					{
						PXav_store_ref(THIS_AV, new_total_len, *lpsv);
					}
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::operator+= (const CStringArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int src_size = (int)array.GetSize(),
					new_total_len = src_size + av_len(THIS_AV);
				av_extend(THIS_AV, new_total_len);
				src_size--; // becomes src_len
				for (; src_size>=0; new_total_len--, src_size--)
				{
					PXav_store_ref_noinc(THIS_AV, new_total_len, PXnewSVpv(THIS_OBJECT_MYPERL, LPCTSTR(array.GetAt(src_size))));
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::operator+= (LPCTSTR element)
	{
		if (IS_THIS_PERLOBJ_VALID && element)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				av_push(THIS_AV, PXnewSVpv(THIS_OBJECT_MYPERL, element));
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	CStringArray& CPerlArray::StringArray(CStringArray &strARet, bool bAppend) const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (!bAppend)
				strARet.RemoveAll();
			int len = av_len(THIS_AV);
			INT_PTR nOrigSize = strARet.GetSize();
			strARet.SetSize(nOrigSize + len + 1);
			SV** lpsv;
			for (; len>=0; len--)
			{
				lpsv = av_fetch(THIS_AV, len, false); // not lval
				if (!lpsv)
				{
					strARet.SetSize(nOrigSize + len);
					return strARet;
				}
				strARet.SetAt(nOrigSize + len, PXSvPV(THIS_OBJECT_MYPERL, *lpsv));
			}
		}
		return strARet;
	}

	const CStringArray& CPerlArray::operator= (const CStringArray &array)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				clear();
				int len = (int)array.GetUpperBound();
				if (len >= 0)
				{
					av_extend(THIS_AV, len);
					for (; len>=0; len--)
					{
						PXav_store_ref_noinc(THIS_AV, len, PXnewSVpv(THIS_OBJECT_MYPERL, LPCTSTR(array.GetAt(len))));
					}
				}
			}
		}
		return array;
	}

	CPerlScalar CPerlArray::operator[](int nIndex)
	{
		return GetAt(nIndex);
	}

	int CPerlArray::GetSize() const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return 1 + av_len(THIS_AV);
		}
		return -1;
	}

	int CPerlArray::GetCount() const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return 1 + av_len(THIS_AV);
		}
		return -1;
	}

	bool CPerlArray::IsEmpty() const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return av_len(THIS_AV) == -1;
		}
		return true;
	}

	int CPerlArray::GetUpperBound() const
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			return av_len(THIS_AV);
		}
		return -1;
	}

	void CPerlArray::SetSize(int nNewSize)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				nNewSize--;
				int len = av_len(THIS_AV);
				if (nNewSize > len)
					av_extend(THIS_AV, nNewSize);
				else
				{
					for (int i=len; i>nNewSize; i--)
						av_delete(THIS_AV, i, 0);
				}
			}
		}
	}

	void CPerlArray::RemoveAll()
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
				av_clear(THIS_AV);
		}
	}

	CPerlScalar CPerlArray::GetAt(int nIndex, bool bCanCreate)
	{
		CPerlScalar scalar;
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (!bCanCreate && !av_exists(THIS_AV, nIndex))
				return scalar;
			SV** lpsv = av_fetch(THIS_AV, nIndex, true); // lval
			if (!lpsv)
				return scalar;
			CString strTemp;
			strTemp.Format(_T("%s[%d]"), GetName(), nIndex);
			scalar.Create(THIS_OBJECT_INTERP, LPCTSTR(strTemp), (void*)*lpsv);
		}
		return scalar;
	}

	void CPerlArray::SetAt(int nIndex, LPCTSTR newElement)
	{
		if (IS_THIS_PERLOBJ_VALID && newElement)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				if (nIndex > av_len(THIS_AV))
					return;
				PXav_store_ref_noinc(THIS_AV, nIndex, PXnewSVpv(THIS_OBJECT_INTERP, newElement));
			}
		}
	}

	void CPerlArray::SetAtGrow(int nIndex, LPCTSTR newElement)
	{
		if (IS_THIS_PERLOBJ_VALID && newElement)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				if (nIndex > av_len(THIS_AV))
					av_extend(THIS_AV, nIndex);
				PXav_store_ref_noinc(THIS_AV, nIndex, PXnewSVpv(THIS_OBJECT_INTERP, newElement));
			}
		}
	}

	int CPerlArray::Add(LPCTSTR newElement)
	{
		return *this += newElement;
	}

	int CPerlArray::Add(const CString& newElement)
	{
		return *this += newElement;
	}

	int CPerlArray::Append(const CStringArray& newArray)
	{
		return *this += newArray;
	}

	int CPerlArray::Append(const CPerlArray& newArray)
	{
		return *this += newArray;
	}

	void CPerlArray::Copy(const CStringArray& newArray)
	{
		*this = newArray;
	}

	CPerlScalar CPerlArray::pop(int nCount)
	{
		CPerlScalar scalar;
		if (IS_THIS_PERLOBJ_VALID && nCount > 0)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				if (nCount > GetSize())
					nCount = GetSize();

				SV *sv = 0;
				while (nCount--)
					sv = av_pop(THIS_AV);

				CString strTemp(_T("_pop_") + PXGetUniqueID());
				//strTemp.Format(_T("%s[%d]"), GetName(), av_len(THIS_AV));
				//sv_maybe_utf8(sv);
				scalar.Create(THIS_OBJECT_INTERP, LPCTSTR(strTemp), sv);
			}
		}
		return scalar;
	}

	int CPerlArray::push(LPCTSTR szFirst, int nCount, ...)
	{
		if (IS_THIS_PERLOBJ_VALID && szFirst)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				av_extend(THIS_AV, av_len(THIS_AV) + nCount + 1);
				av_push(THIS_AV, PXnewSVpv(THIS_OBJECT_MYPERL, szFirst));
				if (nCount)
				{
					va_list marker;
					va_start(marker, nCount);
					LPCTSTR s = va_arg(marker, LPCTSTR);
					while (s && s!=(LPCTSTR)-1 && nCount--)
					{
						av_push(THIS_AV, PXnewSVpv(THIS_OBJECT_MYPERL, s));
						s  = va_arg(marker, LPCTSTR);
					}
					va_end(marker);
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}
	/*
	int CPerlArray::push(const CStringArray& array)
	{
	if (IS_THIS_PERLOBJ_VALID)
	{
	SET_THIS_OBJECT_CONTEXT;
	if (AvREADONLY(THIS_AV))
	ON_AVREADONLY;
	else
	{
	int nSize = (int)array.GetSize();
	if (nSize > 0)
	{
	av_extend(THIS_AV, av_len(THIS_AV) + nSize);
	for (int i=0; i<nSize; i++)
	av_push(THIS_AV, PXnewSVpv(THIS_OBJECT_MYPERL, LPCTSTR(array.GetAt(i))));
	}
	return av_len(THIS_AV);
	}
	}
	return -1;
	}

	int CPerlArray::push(const CPerlArray& array)
	{
	if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(array))
	{
	SET_THIS_OBJECT_CONTEXT;
	if (AvREADONLY(THIS_AV))
	ON_AVREADONLY;
	else
	{
	int len = av_len(GET_AV(array));
	if (len >= 0)
	{
	av_extend(THIS_AV, av_len(THIS_AV) + len + 1);
	SV** lpsv;
	for (int i=0; i<=len; i++)
	{
	if (lpsv = av_fetch(GET_AV(array), i, true))
	av_push(THIS_AV, *lpsv);
	}
	}
	return av_len(THIS_AV);
	}
	}
	return -1;
	}*/

	CPerlScalar CPerlArray::shift(int nCount)
	{
		CPerlScalar scalar;
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				if (nCount > GetSize())
					nCount = GetSize();

				SV *sv = 0;
				while (nCount--)
					sv = av_shift(THIS_AV);

				CString strTemp(_T("_shift_") + PXGetUniqueID());
				//sv_maybe_utf8(sv);
				scalar.Create(THIS_OBJECT_INTERP, LPCTSTR(strTemp), sv);
			}
		}
		return scalar;
	}

	int CPerlArray::unshift(LPCTSTR szFirst, int nCount, ...)
	{
		if (IS_THIS_PERLOBJ_VALID && szFirst)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				av_unshift(THIS_AV, nCount + 1);
				PXav_store_ref_noinc(THIS_AV, 0, PXnewSVpv(THIS_OBJECT_MYPERL, szFirst));
				if (nCount)
				{
					va_list marker;
					va_start(marker, nCount);
					LPCTSTR s = va_arg(marker, LPCTSTR);
					int i = 1;
					while (s && s!=(LPCTSTR)-1  && nCount--)
					{
						PXav_store_ref_noinc(THIS_AV, i, PXnewSVpv(THIS_OBJECT_MYPERL, s));
						s = va_arg(marker, LPCTSTR);
						i++;
					}
					va_end(marker);
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::unshift(const CStringArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int nSize = (int)array.GetSize();
				if (nSize > 0)
				{
					av_unshift(THIS_AV, nSize);
					for (int i=0; i<nSize; i++)
					{
						PXav_store_ref_noinc(THIS_AV, i, PXnewSVpv(THIS_OBJECT_MYPERL, LPCTSTR(array.GetAt(i))));
					}
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::unshift(const CPerlArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(array))
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int nSize = array.GetSize();
				if (nSize > 0)
				{
					int len = av_len(GET_AV(array));
					if (len >= 0)
					{
						len++;
						av_unshift(THIS_AV, len);
						if (array.GetParam() != GetParam())
							nSize = 0;
						SV** lpsv;
						for (int i=0; i<len; i++)
						{
							lpsv = av_fetch(GET_AV(array), i+nSize, false); // not lval
							if (lpsv)
							{
								PXav_store_ref(THIS_AV, i, *lpsv);
							}
						}
					}
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::reverse_push(const CStringArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int nSize = (int)array.GetSize();
				if (nSize > 0)
				{
					av_extend(THIS_AV, av_len(THIS_AV) + nSize);
					while (--nSize >= 0)
						av_push(THIS_AV, PXnewSVpv(THIS_OBJECT_MYPERL, LPCTSTR(array.GetAt(nSize))));
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::reverse_push(const CPerlArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(array))
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int len = av_len(GET_AV(array));
				if (len >= 0)
				{
					av_extend(THIS_AV, av_len(THIS_AV) + len + 1);
					SV** lpsv;
					while (len >= 0)
					{
						lpsv = av_fetch(GET_AV(array), len, false); // not lval
						if (lpsv)
							av_push(THIS_AV, *lpsv);
						len--;
					}
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::reverse_unshift(const CStringArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int nSize = (int)array.GetSize();
				if (nSize > 0)
				{
					int i=0;
					av_unshift(THIS_AV, nSize);
					/*if (array.GetParam() == GetParam())
					nSize += nSize;*/
					while (--nSize >= 0)
					{
						PXav_store_ref_noinc(THIS_AV, i++, PXnewSVpv(THIS_OBJECT_MYPERL, LPCTSTR(array.GetAt(nSize))));
					}
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	int CPerlArray::reverse_unshift(const CPerlArray& array)
	{
		if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(array))
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
			{
				int nSize = array.GetSize();
				if (nSize > 0)
				{
					int len = av_len(GET_AV(array));
					if (len >= 0)
					{
						int delta = 0, i = 0;
						av_unshift(THIS_AV, len + 1);
						if (array.GetParam() == GetParam())
							delta = nSize;
						SV** lpsv;
						while (len >= 0)
						{
							lpsv = av_fetch(GET_AV(array), len + delta, false); // not lval
							if (lpsv)
							{
								PXav_store_ref(THIS_AV, i++, *lpsv);
							}
							len--;
						}
					}
				}
				return av_len(THIS_AV);
			}
		}
		return -1;
	}

	void CPerlArray::clear(void)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			else
				av_clear(THIS_AV);
		}
	}

	void CPerlArray::undef(void)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (AvREADONLY(THIS_AV))
				ON_AVREADONLY;
			av_undef(THIS_AV);
		}
	}

	CPerlScalar CPerlArray::join(LPCTSTR szGlue)
	{
		CPerlScalar scalar;

		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			CString strEval;
			//CString strPackageName = LPCTSTR(GetName());
			CString strNewVarName(_T("_join_") + PXGetUniqueID());
			/*if (*(LPCTSTR(strPackageName)))
			{
				INT_PTR len = strPackageName.GetLength()-2;
				while (--len && !(strPackageName[len] == ':' && strPackageName[len+1] == ':'));
				if (len > 0)
				{
					strPackageName = strPackageName.Left(len);
					strEval += _T("package ");
					strEval += strPackageName;
					strEval += _T("; ");
					strNewVarName = strPackageName + _T("::") + strNewVarName;
				}
				else
					strPackageName = _T("");
			}*/
			strEval += _T("$");
			strEval += strNewVarName;
			strEval += _T(" = join(");
			strEval += szGlue;
			strEval += _T(", @");
			strEval += GetName();
			strEval += _T(");");
			//AfxMessageBox(strEval);
			SV *sv = PXEvalSV(GetMyPerl(), strEval);
			if (sv)
			{
				//sv_maybe_utf8(sv);
				scalar.Create(GetInterp(), strNewVarName, sv);
				//scalar.m_bMortal = true;
			}
		}

		return scalar;
	}

	/////////////////////////////////////////////////
	// CPerlHash

	CPerlHash::CPerlHash()
	{
		m_he = 0;
	}

	CMapStringToString& CPerlHash::MapStringToString(CMapStringToString &mapRet, bool bAppend)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;

			if (!bAppend)
				mapRet.RemoveAll();

			hv_iterinit(THIS_HV);
			HE *hash_entry;
			SV *svKey, *svVal;

			while (hash_entry = hv_iternext(THIS_HV))
			{
				svKey = hv_iterkeysv(hash_entry);
				if (!svKey)
					continue;
				svVal = hv_iterval(THIS_HV, hash_entry);
				mapRet.SetAt(PXSvPV(THIS_OBJECT_MYPERL, svKey), svVal ? PXSvPV(THIS_OBJECT_MYPERL, svVal) : _T(""));
			}
		}
		return mapRet;
	}

	const CMapStringToString& CPerlHash::operator= (const CMapStringToString& map)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (HvREADONLY(THIS_AV))
				ON_HVREADONLY;
			else
			{
				clear();
				CString strKey, strValue;
				for (POSITION pos = map.GetStartPosition(); pos != 0;)
				{
					map.GetNextAssoc(pos, strKey, strValue);
					PXhv_store_ref_noinc(THIS_HV, LPCTSTR(strKey), PXnewSVpv(THIS_OBJECT_MYPERL, LPCTSTR(strValue)));
				}
			}
		}
		return map;
	}

	const CPerlHash& CPerlHash::operator= (const CPerlHash& hash)
	{
		if (IS_THIS_PERLOBJ_VALID && IS_PERLOBJ_VALID(hash))
		{
			SET_THIS_OBJECT_CONTEXT;
			if (HvREADONLY(THIS_HV))
				ON_HVREADONLY;
			else
			{
				clear();
				hv_iterinit(GET_HV(hash));
				HE *hash_entry;
				SV *svKey, *svVal;
				while (hash_entry = hv_iternext(GET_HV(hash)))
				{
					svKey = hv_iterkeysv(hash_entry);
					if (!svKey)
						continue;
					svVal = hv_iterval(GET_HV(hash), hash_entry);
					if (!svVal)
						continue;
					PXhv_store_ent_ref(THIS_HV, svKey, svVal);
				}
			}
		}
		else
		{
			Clone(hash);
		}
		return hash;
	}

	int CPerlHash::GetCount() const
	{
		return GetSize();
	}

	int CPerlHash::GetSize() const
	{
		int nCount = 0;

		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;

			hv_iterinit(THIS_HV);
			HE *hash_entry;

			while (hash_entry = hv_iternext(THIS_HV))
				nCount++;
		}

		return nCount;
	}

	bool CPerlHash::IsEmpty() const
	{
		return GetSize()==0;
	}

	bool CPerlHash::Lookup(LPCTSTR key, CString& rValue) const
	{
		bool bRet = false;
		if (IS_THIS_PERLOBJ_VALID && key)
		{
			SET_THIS_OBJECT_CONTEXT;
			SV** lpsv = hv_fetch(THIS_HV, PXCASTR(key), (I32)_tcslen(key), false); // not lval
			if (lpsv)
			{
				rValue = PXSvPV(THIS_OBJECT_MYPERL, *lpsv);
				bRet = true;
			}
		}
		return bRet;
	}

	CPerlScalar CPerlHash::Lookup(LPCTSTR key, bool bCanCreate)
	{
		CPerlScalar scalar;
		if (IS_THIS_PERLOBJ_VALID && key)
		{
			SET_THIS_OBJECT_CONTEXT;
			int len = (I32)_tcslen(key);
			if (!bCanCreate && !hv_exists(THIS_HV, PXCASTR(key), len))
				return scalar;
			SV** lpsv = hv_fetch(THIS_HV, PXCASTR(key), len, true); // lval!
			if (lpsv)
			{
				CString strTemp;
				strTemp.Format(_T("%s{'%s'}"), GetName(), key);
				//sv_maybe_utf8(*lpsv);
				scalar.Create(THIS_OBJECT_INTERP, LPCTSTR(strTemp), (void*)*lpsv);
			}
		}
		return scalar;
	}

	CPerlScalar CPerlHash::operator[](LPCTSTR key)
	{
		return Lookup(key);
	}

	void CPerlHash::SetAt(LPCTSTR key, LPCTSTR newValue)
	{
		if (IS_THIS_PERLOBJ_VALID && key && newValue)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (HvREADONLY(THIS_HV))
				ON_HVREADONLY;
			else
			{
				PXhv_store_ref_noinc(THIS_HV, LPCTSTR(key), PXnewSVpv(THIS_OBJECT_MYPERL, newValue));
			}
		}
	}

	bool CPerlHash::RemoveKey(LPCTSTR key)
	{
		bool bRet = false;
		if (IS_THIS_PERLOBJ_VALID && key)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (HvREADONLY(THIS_HV))
				ON_HVREADONLY;
			else
			{
				bRet = (0!=hv_delete(THIS_HV, PXCASTR(key), (I32)_tcslen(key), 0));
			}
		}
		return bRet;
	}

	void CPerlHash::RemoveAll()
	{
		clear();
	}

	bool CPerlHash::each(CString &strKey, CString &strValue)
	{
		bool bRet = false;

		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;

			if (!m_he)
				hv_iterinit(THIS_HV);

			m_he = hv_iternext(THIS_HV);

			if (m_he)
			{
				SV *svKey = hv_iterkeysv((HE*)m_he);
				SV *svVal = hv_iterval(THIS_HV, (HE*)m_he);
				if (svKey && svVal)
				{
					strKey = PXSvPV(THIS_OBJECT_MYPERL, svKey);
					strValue = PXSvPV(THIS_OBJECT_MYPERL, svVal);
					bRet = true;
				}
			}
		}

		if (!bRet)
			m_he = 0;

		return bRet;
	}

	bool CPerlHash::each(CPerlScalar & key, CPerlScalar & value)
	{
		bool bRet = false;

		if (IS_THIS_PERLOBJ_VALID)
		{
			CString strTemp;

			SET_THIS_OBJECT_CONTEXT;

			if (!m_he)
				hv_iterinit(THIS_HV);

			m_he = hv_iternext(THIS_HV);

			if (m_he)
			{
				SV *svKey = hv_iterkeysv((HE*)m_he);
				SV *svVal = hv_iterval(THIS_HV, (HE*)m_he);
				if (svKey && svVal)
				{
					strTemp = _T("_each_") + PXGetUniqueID();
					key.Create(THIS_OBJECT_INTERP, LPCTSTR(strTemp), (void*)svKey);
					strTemp.Format(_T("%s{'%s'}"), GetName(), LPCTSTR(PXSvPV(THIS_OBJECT_MYPERL, svKey)));
					value.Create(THIS_OBJECT_INTERP, LPCTSTR(strTemp), (void*)svVal);
					bRet = true;
				}
			}
		}

		if (!bRet)
			m_he = 0;

		return bRet;
	}

	CStringArray& CPerlHash::keys(CStringArray &strARet)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;

			m_he = 0;

			strARet.RemoveAll();

			int nIndex = 0;
			strARet.SetSize(8,8);

			hv_iterinit(THIS_HV);
			HE *hash_entry;

			SV *sv;
			while (hash_entry = hv_iternext(THIS_HV))
			{
				sv = hv_iterkeysv(hash_entry);
				strARet.SetAtGrow(nIndex++, sv ? PXSvPV(THIS_OBJECT_MYPERL, sv) : _T(""));
			}

			strARet.SetSize(nIndex);
		}
		return strARet;
	}

	/*CPerlArray CPerlHash::keys()
	{
		CPerlArray array;

		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			CString strEval;
			//CString strPackageName = LPCTSTR(GetName());
			CString strNewVarName(GetName() + _T("_keys_") + PXGetUniqueID());
			strEval += _T("@");
			strEval += strNewVarName;
			strEval += _T(" = keys %");
			strEval += GetName();
			strEval += _T(";");
			if (PXEvalSV(GetMyPerl(), strEval))
			{
				AV *av = get_av(PXCASTR(strNewVarName), false);
				if (av)
					array.Create(GetInterp(), strNewVarName, av);
			}
		}
		return array;
	}*/

	CStringArray& CPerlHash::values(CStringArray &strARet)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;

			m_he = 0;

			strARet.RemoveAll();

			int nIndex = 0;
			strARet.SetSize(8,8);

			hv_iterinit(THIS_HV);
			HE *hash_entry;
			SV *sv;

			while (hash_entry = hv_iternext(THIS_HV))
			{
				sv = hv_iterval(THIS_HV, hash_entry);
				strARet.SetAtGrow(nIndex++, sv ? PXSvPV(THIS_OBJECT_MYPERL, sv) : _T(""));
			}

			strARet.SetSize(nIndex);
		}
		return strARet;
	}

	/*CPerlArray CPerlHash::values()
	{
		CPerlArray array;

		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			CString strEval;
			//CString strPackageName = LPCTSTR(GetName());
			CString strNewVarName(GetName() + _T("_keys_") + PXGetUniqueID());
			strEval += _T("@");
			strEval += strNewVarName;
			strEval += _T(" = values %");
			strEval += GetName();
			strEval += _T(");");
			if (PXEvalSV(GetMyPerl(), strEval))
			{
				AV *av = get_av(PXCASTR(strNewVarName), false);
				if (av)
					array.Create(GetInterp(), strNewVarName, av);
			}
		}
		return array;
	}*/

	bool CPerlHash::exists(LPCTSTR key) const
	{
		bool bExists = false;
		if (IS_THIS_PERLOBJ_VALID && key)
		{
			SET_THIS_OBJECT_CONTEXT;
			bExists = 0!=hv_exists(THIS_HV, PXCASTR(key), (I32)_tcslen(key));
		}
		return bExists;
	}

	void CPerlHash::undef(void)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (HvREADONLY(THIS_HV))
				ON_HVREADONLY;
			else
				hv_undef(THIS_HV);
		}
	}

	void CPerlHash::clear(void)
	{
		if (IS_THIS_PERLOBJ_VALID)
		{
			SET_THIS_OBJECT_CONTEXT;
			if (HvREADONLY(THIS_HV))
				ON_HVREADONLY;
			else
				hv_clear(THIS_HV);
		}
	}


	/////////////////////////////////////////////////
	// CPerlInterpreter

#define INTERP_LOADED_OK (m_pMyPerl!=0)

	CPerlInterpreter::CPerlInterpreter()
	{
		m_pMyPerl = 0;
		m_bPersistent = false;
		m_idCurPackage = NOID;
		m_pParsedScriptNonPersistent = 0;
		m_hNotifyWnd = 0;
		m_nMessage = WM_PXPW_THREAD_NOTIFY;
	}

	CPerlInterpreter::~CPerlInterpreter()
	{
		Unload();
	}

	bool CPerlInterpreter::Load(/*bool bPersistent , fnXSInitProc xs_init_addr*/)
	{
		bool bPersistent = true;

		if (!INTERP_LOADED_OK)
		{
			PXPerlWrap::PXInfo(_T("CPerlInterpreter::Load(): Loading an interpreter (persistent: %s)..."), bPersistent ? _T("yes") : _T("no"));

			bool bLoaded = PXConstruct(m_pMyPerl);

			/*m_xs_init_addr = xs_init_addr ? xs_init_addr : (fnXSInitProc)xs_init;*/

			if (m_bPersistent = bPersistent)
			{
				CString strPlsFile, strModules;
				UINT nSize = s_strAModules.GetSize();
				if (nSize)
				{
					for (UINT i=0; i<nSize; i++)
					{
						if (s_strAModules.GetAt(i).GetLength())
							strModules += _T("\tuse ") + s_strAModules.GetAt(i) + _T(";\n");
					}
				}

				PXTempName(strPlsFile);
				int fd = 0;
				_tsopen_s(&fd, LPCTSTR(strPlsFile),
					_O_WRONLY | _O_TRUNC | _O_CREAT, _SH_DENYWR, _S_IREAD | _S_IWRITE);

				if (fd != -1)
				{
					_write(fd, perlsistent_top, perlsistent_top_size);
					_write(fd, PXCASTR(strModules), strModules.GetLength());
					_write(fd, perlsistent_bot, perlsistent_bot_size);
					_close(fd);

					if (PXParse(m_pMyPerl, LPCTSTR(strPlsFile), true) && PXRun(m_pMyPerl))
					{
						char *args[] = { 0 };
						int nCount = call_argv("test", G_EVAL | G_SCALAR, args);
						if (nCount == 1 && !(SvTRUE(ERRSV)))
							bLoaded = true;
						else
							PXPerlWrap::PXCriticalError(_T("CPerlInterpreter::Load(): Error while testing persistent script."));
					}
					else
					{
						PXDestruct(m_pMyPerl);
						PXPerlWrap::PXCriticalError(_T("CPerlInterpreter::Load(): Error while interpreting persistent script."));
					}

					_tunlink(LPCTSTR(strPlsFile));
				}
				else
				{
					PXPerlWrap::PXCriticalError(_T("CPerlInterpreter::Load(): Failed to create temporary file %s."), LPCTSTR(strPlsFile));
				}
			}

			if (bLoaded)
			{
				PXPerlWrap::PXInfo(_T("CPerlInterpreter::Load(0x%08X): Loaded Perl interpreter 0x%08X."), m_pMyPerl, m_pMyPerl);
			}
			else
			{
				PXPerlWrap::PXCriticalError(_T("CPerlInterpreter::Load(): Failed to load Perl interpreter."));
				m_pMyPerl = 0;
			}

			return bLoaded;
		}

		return true;
	}

	bool CPerlInterpreter::IsLoaded(void) const
	{
		return INTERP_LOADED_OK;
	}

	void CPerlInterpreter::Unload(void)
	{
		if (INTERP_LOADED_OK)
		{
			PXPerlWrap::PXInfo(_T("CPerlInterpreter::Unload(0x%08X): Unloading Perl interpreter."), m_pMyPerl);
			StopThread();
			PXDestruct(m_pMyPerl);
			s_interp_pool.remove(this);
			//m_bPersistent = false;
			m_idCurPackage = NOID;
			m_pParsedScriptNonPersistent = 0;
		}
	}

	void* CPerlInterpreter::GetMyPerl(void)
	{
		return m_pMyPerl;
	}

	bool CPerlInterpreter::IsPersistent(void) const
	{
		return m_bPersistent;
	}

	bool CPerlInterpreter::Parse(CScript& script)
	{
		if (!INTERP_LOADED_OK
			|| !script.IsLoaded()
			|| script.IsParsed(this))
			return false;

		bool bParsed = false;

		if (m_bPersistent)
		{
			TCHAR buffer[256], buf[64];
			_tcscpy_s(buffer, 255, _T("Script"));
			_itot_s(NEXT_ID(m_idCurPackage), buf, 63, 16);
			_tcscat_s(buffer, 255, buf);
			script.SetPersistentPackage(this, buffer);

			if (script.m_type == CScript::TypePlain) // can't parse bytecode with persistent interpreter
				bParsed = (0!=PXCallVInt(m_pMyPerl, _T("parse"), 0,
				PXnewSVpv(m_pMyPerl, LPCTSTR(script.GetPersistentPackage(this)), false),
				PXnewSVpv(m_pMyPerl, LPCTSTR(script.GetScript()), false),
				0));
			else
				PXPerlWrap::PXError(_T("CPerlInterpreter::Parse(0x%08X): Cannot parse Perl bytecode with persistent interpreter."), m_pMyPerl);
		}
		else
		{
			switch (script.GetType())
			{
			case CScript::TypePlain:
				{
					bParsed = PXParse(m_pMyPerl,
						LPCTSTR(script.GetScriptP()),
						false,
						//&script.GetCustomOpts(),
						0
						/*&script.GetARGV()*/);
				}; break;
			case CScript::TypeBytecode:
				{
					bParsed = PXParse(m_pMyPerl,
						LPCTSTR(script.Flush()),
						true,
						//&script.GetCustomOpts(),
						BYTELOADER
						/*&script.GetARGV()*/);
				}; break;
			}
			m_pParsedScriptNonPersistent = bParsed ? &script : 0;
		}

		script.SetFlag(this, CScriptAttributes::FlagParsed);

		return bParsed;
	}

	bool CPerlInterpreter::Run(CScript& script)
	{
		if (!INTERP_LOADED_OK
			|| !script.IsLoaded()
			|| !script.IsParsed(this))
			return false;

		bool bRun = false;

		if (m_bPersistent)
		{
			bRun = 0!=PXCallVInt(m_pMyPerl, _T("run"), 0,
				PXnewSVpv(m_pMyPerl, LPCTSTR(script.GetPersistentPackage(this)), false),
				0);
		}
		else
		{
			if (&script == m_pParsedScriptNonPersistent)
				bRun = PXRun(m_pMyPerl);
		}

		script.SetFlag(this, CScriptAttributes::FlagRun);
		//script.SetRun(this, bRun);

		return bRun;
	}

	bool CPerlInterpreter::LoadModule(LPCTSTR szModuleName)
	{
		if (!INTERP_LOADED_OK)
			return false;

		CString strModule(_T("use "));
		strModule += szModuleName;
		strModule += _T(";");
		return 0!=PXEvalSV(m_pMyPerl, LPCTSTR(strModule));
	}

	bool CPerlInterpreter::ParseRun(CScript& script)
	{
		if (Parse(script))
			return Run(script);
		return false;
	}

	class RunThreadData
	{
	public:
		bool bEval;
		CString strEval;
		CPerlInterpreter *pInterp;
		CScript script;
		HWND hNotifyWnd;
		UINT nMessage;

		RunThreadData()
		{
		}

		RunThreadData(const RunThreadData & info)
		{
			*this = info;
		}

		RunThreadData & operator =(const RunThreadData & info)
		{
			bEval = info.bEval;
			strEval = info.strEval;
			pInterp = info.pInterp;
			script = info.script;
			hNotifyWnd = info.hNotifyWnd;
			nMessage = info.nMessage;
			return *this;
		}
	};

	//static CScript s_script;

	UINT __stdcall RunProc(LPVOID pData)
	{
		DWORD dwTick;
		//RunThreadData data;
		//memcpy(&data, (RunThreadData*)pData, sizeof(RunThreadData));

		if (pData)
		{
			RunThreadData data;
			data = *(RunThreadData*)pData;

			SetEvent(s_hRunEvent);

			PXPerlWrap::PXInfo(_T("CPerlInterpreter::RunThread(0x%08X, 0x%08X): Thread launched, script is about to be run/statement to be evaluated."), data.pInterp->GetMyPerl(), s_hRunThread);

			if (::IsWindow(data.hNotifyWnd))
				::PostMessage(data.hNotifyWnd, data.nMessage, PXPW_THREAD_STARTED, 0);

			dwTick = GetTickCount();
			//data.pInterp->Parse(*data.pScript);
			if (data.bEval)
			{
				/*CPerlScalar ret_sv =*/ PXEvalSV(data.pInterp->GetMyPerl(), LPCTSTR(data.strEval));

				//if (data.pInterp->Eval(data.script, strEval))
				//	PXPerlWrap::PXInfo(_T("CPerlInterpreter::RunThread(0x%08X, 0x%08X): statement evaluated successfuly."), data.pInterp->m_pMyPerl, s_hRunThread);
				//else
				//	PXPerlWrap::PXError(_T("CPerlInterpreter::RunThread(0x%08X, 0x%08X): error while evaluating statement."), data.pInterp->m_pMyPerl, s_hRunThread);
			}
			else
			{
				if (data.pInterp->Run(data.script))
					PXPerlWrap::PXInfo(_T("CPerlInterpreter::RunThread(0x%08X, 0x%08X): script run successfuly."), data.pInterp->GetMyPerl(), s_hRunThread);
				else
					PXPerlWrap::PXError(_T("CPerlInterpreter::RunThread(0x%08X, 0x%08X): error while running script."), data.pInterp->GetMyPerl(), s_hRunThread);
			}

			PXPerlWrap::PXInfo(_T("CPerlInterpreter::RunThread(0x%08X, 0x%08X): Run time: %.03f secs."), data.pInterp->GetMyPerl(), s_hRunThread, (GetTickCount()-dwTick)/1000.0f);

			s_bRunning = false;

			if (::IsWindow(data.hNotifyWnd))
				::PostMessage(data.hNotifyWnd, data.nMessage, PXPW_THREAD_ENDED, 0);
		}

		_endthreadex(0);
		return 0;
	}

	bool CPerlInterpreter::RunThread(CScript& script, HWND hNotifyWnd, UINT nMessage)
	{
		if (!INTERP_LOADED_OK || !script.IsLoaded())
			return false;

		if (s_bRunning)
		{
			PXPerlWrap::PXError(_T("CPerlInterpreter::RunThread(0x%08X): A thread (0x%08X) is already running for this interpreter."), m_pMyPerl, s_hRunThread);
			return false;
		}

		PXPerlWrap::PXInfo(_T("CPerlInterpreter::RunThread(0x%08X): Running a threaded script..."), m_pMyPerl);

		s_hRunEvent = CreateEvent(0, FALSE, FALSE, 0);

		s_bRunning = true;

		RunThreadData data;
		data.pInterp = this;
		data.script = script;
		data.hNotifyWnd = hNotifyWnd;
		data.nMessage = nMessage;
		data.bEval = false;
		m_hNotifyWnd = data.hNotifyWnd = hNotifyWnd;
		m_nMessage = data.nMessage = nMessage;

		s_hRunThread = (HANDLE)_beginthreadex(0, 0, RunProc, (void*)&data, 1, &s_nRunThreadID);
		if (s_hRunThread)
			WaitForSingleObject(s_hRunEvent, INFINITE);

		return s_hRunThread != 0;
	}

	bool CPerlInterpreter::IsThreadRunning(void)
	{
		return s_bRunning;
	}

	void CPerlInterpreter::StopThread(UINT nTimeout)
	{
		bool bTerminated = false;

		if (s_bRunning)
		{
			if (WAIT_TIMEOUT == WaitForSingleObject(s_hRunThread, nTimeout))
			{
				PXPerlWrap::PXWarning(_T("CPerlInterpreter::StopThread(0x%08X): Terminating thread 0x%08X."), m_pMyPerl, s_hRunThread);
				if (::TerminateThread(s_hRunThread, -1))
				{
					bTerminated = true;
					PXPerlWrap::PXWarning(_T("CPerlInterpreter::StopThread(0x%08X): Thread 0x%08X terminated."), m_pMyPerl, s_hRunThread);
				}
				else
					PXPerlWrap::PXCriticalError(_T("CPerlInterpreter::StopThread(0x%08X): Thread 0x%08X failed to terminate."), m_pMyPerl, s_hRunThread);
			}
			else
				PXPerlWrap::PXInfo(_T("CPerlInterpreter::StopThread(0x%08X): Thread 0x%08X ended successfuly."), m_pMyPerl, s_hRunThread);

			s_bRunning = false;
		}

		if (s_hRunThread)
		{
			::CloseHandle(s_hRunThread);
			s_hRunThread = 0;
		}

		if (s_hRunThread)
		{
			CloseHandle(s_hRunThread);
			s_hRunThread = 0;
		}

		if (bTerminated)
		{
			if (::IsWindow(m_hNotifyWnd))
				::PostMessage(m_hNotifyWnd, m_nMessage, PXPW_THREAD_ABORTED, 0);
		}
	}

	bool CPerlInterpreter::Clean(CScript& script)
	{
		if (!INTERP_LOADED_OK
			|| !m_bPersistent
			|| !script.IsLoaded()
			|| !script.IsParsed(this))
			return false;

		bool bClean = 0!=PXCallVInt(m_pMyPerl, _T("clean"), 0,
			PXnewSVpv(m_pMyPerl, LPCTSTR(script.GetPersistentPackage(this)), false),
			0);

		if (bClean)
		{
			script.ResetFlag(this, CScriptAttributes::FlagParsed | CScriptAttributes::FlagRun);
		}

		return bClean;
	}

	CPerlScalar CPerlInterpreter::Eval(CScript& script, LPCTSTR szEval, ...)
	{
		CPerlScalar scalar;

		if (!INTERP_LOADED_OK || !script.IsLoaded())
			return scalar;

		SV *ret_sv = 0;
		TCHAR *buffer = 0;
		CString strPackageName;
		va_list params;
		va_start(params, szEval);

		strPackageName = LPCTSTR(script.GetPersistentPackage(this));

		PXPerlWrap::PXInfo(_T("CPerlInterpreter::Eval(0x%08X): Evaluating a Perl statement..."), m_pMyPerl);

		if (m_bPersistent)
		{
			//if (script.IsFlagSet(this, CScriptAttributes::FlagParsed))
			{
				CString strEval;
				if (*(LPCTSTR(strPackageName)))
				{
					strEval += _T("package ");
					strEval += strPackageName;
					strEval += _T("; ");
				}
				strEval += szEval;
				size_t len = _vsctprintf(LPCTSTR(strEval), params);
				buffer = new TCHAR[len+8];
				_vstprintf_s(buffer, len+7, LPCTSTR(strEval), params);
				//ret_sv = PXEvalSV(m_pMyPerl, (LPCTSTR)buffer);

				ret_sv = PXEvalSV(m_pMyPerl, (LPCTSTR)buffer);/*PXCallVSV(m_pMyPerl, _T("evalcode"),
					PXnewSVpv(m_pMyPerl, LPCTSTR(script.GetPersistentPackage(this)), false),
					PXnewSVpv(m_pMyPerl, (LPCTSTR)buffer, false),
					0);*/
			}
			/*else
			{
			PXPerlWrap::PXError(_T("CPerlInterpreter::Eval(0x%08X): Failed to evaluate statement, the associated script has to be parsed first."), m_pMyPerl);
			}*/
		}
		else
		{
			size_t len = _vsctprintf(szEval, params);
			buffer = new TCHAR[len+8];
			_vstprintf_s(buffer, len+7, szEval, params);
			ret_sv = PXEvalSV(m_pMyPerl, (LPCTSTR)buffer);
		}

		va_end(params);

		if (buffer)
			delete [] buffer;

		if (ret_sv)
		{
			CString strNewVarName(_T("eval_res_") + PXGetUniqueID());
			if (*(LPCTSTR(strPackageName)))
				strNewVarName = strPackageName + _T("::") + strNewVarName;
			scalar.Create(this, strNewVarName, ret_sv);
			PXPerlWrap::PXInfo(_T("CPerlInterpreter::Eval(0x%08X): Statement evaluated successfuly."), m_pMyPerl);
		}
		else
		{
			scalar.Create(this, _T("@"), ERRSV);
			PXPerlWrap::PXError(_T("CPerlInterpreter::Eval(0x%08X): Failed to evaluate statement, returning ERRSV."), m_pMyPerl);
		}

		return scalar;
	}

	// SetLastError() => logs

	bool CPerlInterpreter::EvalThread(CScript& script, LPCTSTR szEval, HWND hNotifyWnd, UINT nMessage, ...)
	{
		if (!INTERP_LOADED_OK || !script.IsLoaded())
			return false;

		if (s_bRunning)
		{
			PXPerlWrap::PXError(_T("CPerlInterpreter::EvalThread(0x%08X): A thread (0x%08X) is already running for this interpreter."), m_pMyPerl, s_hRunThread);
			return false;
		}

		PXPerlWrap::PXInfo(_T("CPerlInterpreter::EvalThread(0x%08X): Evaluating a statement in a thread..."), m_pMyPerl);

		TCHAR *buffer = 0;
		CString strPackageName;
		va_list params;
		va_start(params, szEval);

		s_hRunEvent = CreateEvent(0, FALSE, FALSE, 0);

		s_bRunning = true;

		RunThreadData data;
		data.pInterp = this;
		data.script = script;
		data.hNotifyWnd = hNotifyWnd;
		data.nMessage = nMessage;
		data.bEval = false;
		m_hNotifyWnd = data.hNotifyWnd = hNotifyWnd;
		m_nMessage = data.nMessage = nMessage;

		strPackageName = LPCTSTR(script.GetPersistentPackage(this));

		if (*(LPCTSTR(strPackageName)))
		{
			data.strEval += _T("package ");
			data.strEval += strPackageName;
			data.strEval += _T("; ");
		}
		data.strEval += szEval;
		size_t len = _vsctprintf(LPCTSTR(data.strEval), params);
		buffer = new TCHAR[len+8];
		_vstprintf_s(buffer, len+7, LPCTSTR(data.strEval), params);

		data.strEval = buffer;

		va_end(params);

		if (buffer)
			delete [] buffer;

		s_hRunThread = (HANDLE)_beginthreadex(0, 0, RunProc, (void*)&data, 1, &s_nRunThreadID);
		if (s_hRunThread)
			WaitForSingleObject(s_hRunEvent, INFINITE);

		return s_hRunThread != 0;
	}

	CPerlScalar CPerlInterpreter::GetScalar(CScript& script, LPCTSTR szVariable, bool bCreateIfNotExisting)
	{
		CPerlScalar scalar;
		if (INTERP_LOADED_OK
			&& script.IsLoaded()
			//&& script.IsFlagSet(this, CScriptAttributes::FlagParsed)
			&& szVariable && *szVariable)
		{
			PERL_SET_CONTEXT(m_pMyPerl);
			CString strName;
			SV *sv;
			strName = LPCTSTR(script.GetPersistentPackage(this));
			if (!strName.IsEmpty())
				strName += _T("::");
			strName += szVariable;
			sv = get_sv(PXCASTR(strName), bCreateIfNotExisting ? (true|GV_ADDMULTI) : false);
			scalar.Create(this, LPCTSTR(strName), (void*)sv);
		}
		return scalar;
	}

	CPerlArray CPerlInterpreter::GetArray(CScript& script, LPCTSTR szVariable, bool bCreateIfNotExisting)
	{
		CPerlArray array;
		if (INTERP_LOADED_OK
			&& script.IsLoaded()
			//&& script.IsFlagSet(this, CScriptAttributes::FlagParsed)
			&& szVariable && *szVariable)
		{
			PERL_SET_CONTEXT(m_pMyPerl);
			CString strName;
			strName = LPCTSTR(script.GetPersistentPackage(this));
			if (!strName.IsEmpty())
				strName += _T("::");
			strName += szVariable;
			AV *av = get_av(PXCASTR(strName), bCreateIfNotExisting ? (true|GV_ADDMULTI) : false);
			array.Create(this, LPCTSTR(strName), (void*)av);
		}
		return array;
	}

	CPerlHash CPerlInterpreter::GetHash(CScript& script, LPCTSTR szVariable, bool bCreateIfNotExisting)
	{
		CPerlHash hash;
		if (INTERP_LOADED_OK
			&& script.IsLoaded()
			//&& script.IsFlagSet(this, CScriptAttributes::FlagParsed)
			&& szVariable && *szVariable)
		{
			PERL_SET_CONTEXT(m_pMyPerl);
			CString strName;
			strName = LPCTSTR(script.GetPersistentPackage(this));
			if (!strName.IsEmpty())
				strName += _T("::");
			strName += szVariable;
			HV *hv = get_hv(PXCASTR(strName), bCreateIfNotExisting ? (true|GV_ADDMULTI) : false);
			hash.Create(this, LPCTSTR(strName), (void*)hv);
		}
		return hash;
	}

}; // namespace PXPerlWrap;

// EOF