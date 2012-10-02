/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FileStrm.cpp
Original author: John Landon
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	See FileStrm.h for a description of the FileStream class.
----------------------------------------------------------------------------------------------*/
#include "main.h"
#if !WIN32
#include "COMInterfaces.h"
#endif
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

#if !WIN32
// Unix file API
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
#endif


/***********************************************************************************************
	Methods
***********************************************************************************************/
//:End Ignore

static DummyFactory g_fact(_T("SIL.AppCore.FileStream"));

/*----------------------------------------------------------------------------------------------
	Constructor
----------------------------------------------------------------------------------------------*/
FileStream::FileStream()
{
	ModuleEntry::ModuleAddRef();
	m_cref = 1;
#if WIN32
	m_ibFilePos.QuadPart = 0;
	m_hfile = NULL;
#else
	m_file = -1;
#endif
}


/*----------------------------------------------------------------------------------------------
	Destructor
----------------------------------------------------------------------------------------------*/
FileStream::~FileStream()
{
	// If this is not a clone the file must be closed before the destructor is called.
	// If this is a clone the file can still be open.
#if WIN32
	Assert(!m_hfile || m_qstrmBase);
#else
	Assert(m_file == -1);
#endif
	ModuleEntry::ModuleRelease();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new FileStream object, opening the specified file in the
	specified mode.
		@param pszFile Name (or path) of the desired file (LPCOLESTR)
		@param grfstgm Combination of flags kfstgmXxxx from enum in FileStrm.h
		@return The associated IStream interface pointer if successful (third parameter).
----------------------------------------------------------------------------------------------*/
void FileStream::Create(LPCOLESTR pszFile, int grfstgm, IStream ** ppstrm)
{
	AssertPsz(pszFile);
	AssertPtr(ppstrm);
	Assert(!*ppstrm);

	ComSmartPtr<FileStream> qfist;
	qfist.Attach(NewObj FileStream);
	qfist->Init(pszFile, grfstgm);
	*ppstrm = qfist.Detach();
}


/*----------------------------------------------------------------------------------------------
	Static method to create a new FileStream object, opening the specified file in the
	specified mode.
		@param pszFile Name (or path) of the desired file (LPCSTR)
		@param grfstgm Combination of flags kfstgmXxxx from enum in FileStrm.h
		@return The associated IStream interface pointer if successful (third parameter).
----------------------------------------------------------------------------------------------*/
void FileStream::Create(LPCSTR pszFile, int grfstgm, IStream ** ppstrm)
{
	AssertPsz(pszFile);
	AssertPtr(ppstrm);
	Assert(!*ppstrm);

	StrUniBufPath stubp(pszFile);	// converts from ANSI string to Unicode string
	if (stubp.Overflow())
		ThrowHr(WarnHr(STG_E_INVALIDPARAMETER));
	Create(stubp.Chars(), grfstgm, ppstrm);
}


/*----------------------------------------------------------------------------------------------
	This method opens the given file according to the given mode settings.
	NOTE: This now creates a file if it doesn't already exist, but opens it if it does exist,
		  if STGM_READWRITE alone is set.
	NOTE: STGM_SHARE_* flags are ignored on Linux, files are always 'shared'
		@param pszFile Name (or path) of the desired file
		@param grfstgm Combination of flags kfstgmXxxx from enum in FileStrm.h
----------------------------------------------------------------------------------------------*/
void FileStream::Init(LPCOLESTR pszFile, int grfstgm)
{
	/* TODO: To access another file this stream should be closed and another created.
	 */
	AssertPsz(pszFile);
	Assert(*pszFile);
#if WIN32
	Assert(!m_hfile);

	HANDLE hfile = 0;
	DWORD dwDesiredAccess = 0;
	DWORD dwShareMode;
	DWORD dwCreationDisposition = 0;

	// Set the dwShareMode parameter.
	if (grfstgm & kfstgmShareDenyNone)
		dwShareMode = FILE_SHARE_READ | FILE_SHARE_WRITE;
	else if (grfstgm & kfstgmShareDenyRead)
		dwShareMode = FILE_SHARE_WRITE;
	else if (grfstgm & kfstgmShareExclusive)
		dwShareMode = 0;
	else
		dwShareMode = FILE_SHARE_READ;

	if (grfstgm & kfstgmReadWrite)
		dwDesiredAccess = GENERIC_READ | GENERIC_WRITE;
	else if (grfstgm & kfstgmWrite)
		dwDesiredAccess = GENERIC_WRITE;
	else
		dwDesiredAccess = GENERIC_READ;

	if (dwDesiredAccess & GENERIC_WRITE)
	{
		if (grfstgm & kfstgmCreate)
			dwCreationDisposition = CREATE_ALWAYS;
		else if (grfstgm & kfstgmReadWrite)
			dwCreationDisposition = OPEN_ALWAYS;	// Allows writing to existing file.
		else
			dwCreationDisposition = CREATE_NEW;
	}
	else
		dwCreationDisposition = OPEN_EXISTING;
	hfile = ::CreateFileW(pszFile, dwDesiredAccess, dwShareMode, NULL, dwCreationDisposition,
		FILE_ATTRIBUTE_NORMAL, NULL);
	if (hfile == INVALID_HANDLE_VALUE)
	{
		HRESULT hr = (HRESULT)::GetLastError();
		int stid = ErrorStringId(hr);
		StrUni stuRes(stid);
		StrUni stuMsg;
		stuMsg.Format(L"%s%n%s", stuRes.Chars(), pszFile);
		ThrowHr(hr, stuMsg.Chars());	// Caller should handle. (This is not a COM method).
	}

	m_hfile = hfile;
	m_staPath = pszFile;
	m_grfstgm = grfstgm;  // Store the access mode for the stat method.
#else
	Assert(m_file < 0);

	int file = -1;
	int flags = 0;
	mode_t mode = S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH;

	//std::cout << kfstgmReadWrite << ", " << kfstgmRead << ", " << kfstgmWrite << ", " << kfstgmCreate << std::endl;
	//std::cout << O_RDWR << ", " << O_RDONLY << ", " << O_WRONLY << ", " << O_CREAT << std::endl;

	if (grfstgm & kfstgmReadWrite) {
		flags |= O_RDWR;
	}
	else if (grfstgm & kfstgmWrite) {
		flags |= O_WRONLY;
	}
	else {
		flags |= O_RDONLY;
	}

	if (grfstgm & kfstgmCreate) {
		flags |= O_CREAT;
	}

	if (grfstgm == kfstgmReadWrite) {
		flags |= O_CREAT;
	}

	if (flags & O_WRONLY | flags & O_RDWR)
	{
		if (grfstgm & kfstgmCreate)
			flags |= O_TRUNC;
		else if (grfstgm & kfstgmReadWrite)
			flags |= O_CREAT;
		else
			flags |= O_CREAT | O_EXCL;
	}

	m_staPath = pszFile;

	file = open(m_staPath, flags, mode);
	if (file < 0) {
		//File open failed, throw error
		ThrowHr(ERROR_OPEN_FAILED);
	}

	m_flags = flags;
	m_file = file;
#endif
}

/*----------------------------------------------------------------------------------------------
	Return the appropriate error string ID for the given error code. - WINDOWS
	// TODO-Linux Can this be removed on Linux?
----------------------------------------------------------------------------------------------*/
int FileStream::ErrorStringId(HRESULT hr)
{
	switch (hr)
	{
		case ERROR_FILE_NOT_FOUND:			return kstidFileErrNotFound;
		case ERROR_PATH_NOT_FOUND:			return kstidFileErrPathNotFound;
		case ERROR_TOO_MANY_OPEN_FILES:		return kstidFileErrTooManyFiles;
		case ERROR_ACCESS_DENIED:			return kstidFileErrAccDenied;
		case ERROR_INVALID_HANDLE: 			return kstidFileErrBadHandle;
		case ERROR_INVALID_DRIVE: 			return kstidFileErrBadDrive;
		case ERROR_WRITE_PROTECT:			return kstidFileErrWriteProtect;
		case ERROR_BAD_UNIT:				return kstidFileErrBadUnit;
		case ERROR_NOT_READY:				return kstidFileErrNotReady;
		case ERROR_SEEK:					return kstidFileErrSeek;
		case ERROR_NOT_DOS_DISK:			return kstidFileErrNotDosDisk;
		case ERROR_SECTOR_NOT_FOUND:	 	return kstidFileErrBadSector;
		case ERROR_WRITE_FAULT:				return kstidFileErrWriteFault;
		case ERROR_READ_FAULT:				return kstidFileErrReadFault;
		case ERROR_GEN_FAILURE:				return kstidFileErrGeneral;
		case ERROR_SHARING_VIOLATION: 		return kstidFileErrSharing;
		case ERROR_LOCK_VIOLATION:			return kstidFileErrLock;
		case ERROR_HANDLE_EOF: 				return kstidFileErrEof;
		case ERROR_HANDLE_DISK_FULL: 		return kstidFileErrHandleDiskFull;
		case ERROR_BAD_NETPATH:				return kstidFileErrBadNetPath;
		case ERROR_NETWORK_BUSY: 			return kstidFileErrNetworkBusy;
		case ERROR_DEV_NOT_EXIST: 			return kstidFileErrNoDevice;
		case ERROR_NETWORK_ACCESS_DENIED: 	return kstidFileErrNoNetAccess;
		case ERROR_BAD_DEV_TYPE:			return kstidFileErrBadDevice;
		case ERROR_BAD_NET_NAME: 			return kstidFileErrBadNetName;
		case ERROR_FILE_EXISTS: 			return kstidFileErrExists;
		case ERROR_CANNOT_MAKE: 			return kstidFileErrCantMake;
		case ERROR_INVALID_PASSWORD: 		return kstidFileErrBadPassword;
		case ERROR_NET_WRITE_FAULT: 		return kstidFileErrNetWriteFault;
		case ERROR_DRIVE_LOCKED:			return kstidFileErrDriveLocked;
		case ERROR_OPEN_FAILED: 			return kstidFileErrOpenFailed;
		case ERROR_BUFFER_OVERFLOW: 		return kstidFileErrBufOverflow;
		case ERROR_DISK_FULL: 				return kstidFileErrDiskFull;
		case ERROR_INVALID_NAME: 			return kstidFileErrBadName;
		case ERROR_NO_VOLUME_LABEL: 		return kstidFileErrNoVolLabel;
		case ERROR_ALREADY_EXISTS: 			return kstidFileErrAlreadyExists;
		default:							return kstidFileErrUnknown;
	}
}

/*----------------------------------------------------------------------------------------------
		Update the file pointer to the stream's seek pointer m_ibFilePos.
		@return false if an error occurs in the update.
----------------------------------------------------------------------------------------------*/
bool FileStream::SetFilePosRaw()
{
#ifdef WIN32
		long dwHigh = (long)m_ibFilePos.HighPart;
		DWORD dwLow;

		dwLow = SetFilePointer(m_hfile, m_ibFilePos.LowPart, &dwHigh, FILE_BEGIN);
		return !(dwLow == 0xFFFFFFFF && GetLastError() != NO_ERROR);
#else
	// TODO-Linux: port
	return true;
#endif
}


/*----------------------------------------------------------------------------------------------
	Get a pointer to the desired interface if possible.  Only IUnknown and IStream are
	supported.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(STG_E_INVALIDPOINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IStream)
		*ppv = static_cast<IStream *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IStream);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Increment the reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) FileStream::AddRef(void)
{
	Assert(m_cref > 0);
	return ++m_cref;
}


/*----------------------------------------------------------------------------------------------
	Decrement the reference count.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP_(UCOMINT32) FileStream::Release(void)
{
	Assert(m_cref > 0);
	if (--m_cref > 0)
		return m_cref;
	m_cref = 1;

#if WIN32
		if (m_hfile && !m_qstrmBase)
	{
		// Close the file if this is not a clone.
		CloseHandle(m_hfile);
		m_hfile = NULL;
	}
#else
	if (m_file >= 0)
	{
		// Close the file
		close(m_file);
		m_file = -1;
	}
#endif

	delete this;
	return 0;
}


/*----------------------------------------------------------------------------------------------
	Read the given number of bytes from the stream / file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::Read(void * pv, UCOMINT32 cb, UCOMINT32 * pcbRead)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg((byte *)pv, cb);
	ChkComArgPtrN(pcbRead);

#if WIN32
	if (m_hfile == NULL)
		ThrowHr(WarnHr(E_UNEXPECTED));
#else
	if (m_file < 0)
		ThrowHr(WarnHr(E_UNEXPECTED));
#endif

	if (cb == 0)
	{
		if (pcbRead)
			*pcbRead = 0;
		return S_OK;
	}


	if (!SetFilePosRaw())
		ThrowHr(WarnHr(STG_E_SEEKERROR));

	DWORD cbRead = 0;
#if WIN32
	if (!ReadFile(m_hfile, pv, cb, &cbRead, NULL))
		ThrowHr(WarnHr(STG_E_READFAULT));
	m_ibFilePos.QuadPart += cbRead;
#else
	cbRead = read(m_file, pv, cb);
	if (cbRead < 0)
		ThrowHr(WarnHr(STG_E_READFAULT));
#endif

	if (pcbRead)
		*pcbRead = cbRead;

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Write the given number of bytes to the stream / file.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::Write(const void * pv, UCOMINT32 cb, UCOMINT32 * pcbWritten)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg((byte *)pv, cb);
	ChkComArgPtrN(pcbWritten);

#if WIN32
	if (m_hfile == NULL)
		ThrowHr(WarnHr(E_UNEXPECTED));
#else
	if (m_file < 0)
		ThrowHr(WarnHr(E_UNEXPECTED));
#endif
	if (cb == 0)
	{
		if (pcbWritten)
			*pcbWritten = 0;
		return S_OK;
	}

#if WIN32
	if (!SetFilePosRaw())
		ThrowHr(WarnHr(STG_E_SEEKERROR));
	DWORD cbWritten = 0;
	if (!WriteFile(m_hfile, pv, cb, &cbWritten, NULL))
		ThrowHr(WarnHr(STG_E_WRITEFAULT));

	m_ibFilePos.QuadPart += cbWritten;
#else // !WIN32
	ssize_t cbWritten = -1;
	cbWritten = write(m_file, pv, cb);
	if (cbWritten < 0)
		ThrowHr(WarnHr(STG_E_WRITEFAULT));
#endif // !WIN32

	if (pcbWritten)
		*pcbWritten = cbWritten;

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Adjust the stream seek pointer, returning the new value.
	@return STG_E_SEEKERROR if the new position would be negative.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin,
	ULARGE_INTEGER * plibNewPosition)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(plibNewPosition);

#if WIN32
	if (m_hfile == NULL)
		ThrowHr(WarnHr(E_UNEXPECTED));

	DWORD dwLow;
	long dwHigh;
	LARGE_INTEGER dlibNew; // attempted new seek position

	switch (dwOrigin)
	{
	case STREAM_SEEK_SET:
		dlibNew.QuadPart = dlibMove.QuadPart;
		break;
	case STREAM_SEEK_CUR:
		dlibNew.QuadPart = (int64)m_ibFilePos.QuadPart + dlibMove.QuadPart;
		break;
	case STREAM_SEEK_END:
		// Find out where EOF is by calling for a zero move of the file pointer
		dwHigh = 0;
		dwLow = SetFilePointer(m_hfile, 0, &dwHigh, FILE_END);
		if (dwLow == 0xFFFFFFFF && GetLastError() != NO_ERROR)
			ThrowHr(WarnHr(STG_E_SEEKERROR));

		// Work out new attempted seek pointer value
		dlibNew.LowPart = dwLow;
		dlibNew.HighPart = dwHigh;
		dlibNew.QuadPart += dlibMove.QuadPart;
		break;
	default:
		ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));
	}

	if (dlibNew.QuadPart < 0)
		ThrowHr(WarnHr(STG_E_SEEKERROR));

	// Update the current position.
	m_ibFilePos.QuadPart = (uint64)dlibNew.QuadPart;

	if (plibNewPosition)
		plibNewPosition->QuadPart = (uint64)dlibNew.QuadPart;
#else
	// TODO-Linux: Examine for cross-platform compatibility what was introduced with r576 and semi-addressed in r1482.
	// TODO-Linux: Check correct behavior for seek past EOF
	if (m_file < 0)
		ThrowHr(WarnHr(E_UNEXPECTED));
	off_t dMove = dlibMove.QuadPart;
	if (dMove != dlibMove.QuadPart) {
		return WarnHr(E_UNEXPECTED);
	}

	int whence;

	switch (dwOrigin)
	{
	case STREAM_SEEK_SET:
		whence = SEEK_SET;
		break;
	case STREAM_SEEK_CUR:
		whence = SEEK_CUR;
		break;
	case STREAM_SEEK_END:
		whence = SEEK_END;
		break;
	default:
		ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));
	}

	off_t newPos = lseek(m_file, dMove, whence);

	if (newPos < 0) {
		ThrowHr(WarnHr(STG_E_SEEKERROR));
	}

	if (plibNewPosition) {
		plibNewPosition->QuadPart = newPos;
	}
#endif

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Set the size of the stream. Here, this means setting the file pointer to the new size
	and then setting EOF to the position of the file pointer. Note that the stream seek pointer
	is not affected by this method. Note also that SetEndOfFile() fails unless we have write
	access to the file.

	NOTE This doesn't seem to ever be used - not ported for now
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::SetSize(ULARGE_INTEGER libNewSize)
{
	BEGIN_COM_METHOD;
#if WIN32
	if (libNewSize.QuadPart < 0)
		ThrowHr(WarnHr(STG_E_INVALIDPARAMETER));

	DWORD dwLow;
	long dwHigh = (long)libNewSize.HighPart;
	// Could check for write access before going further, but setting the file pointer
	// doesn't really do any harm so we may as well let it go on and fail later if it is
	// going to.
	dwLow = SetFilePointer(m_hfile, libNewSize.LowPart, &dwHigh, FILE_BEGIN);
	if (dwLow == 0xFFFFFFFF && GetLastError() != NO_ERROR)
		ThrowHr(WarnHr(STG_E_SEEKERROR));
	if (!SetEndOfFile(m_hfile))
		ThrowHr(WarnHr(STG_E_ACCESSDENIED)); // probably the right error code
#endif
	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Copy cb bytes from the current position in this stream to the current position in
	the stream *pstm.
	Uses FileStream ${#Read} and ${#Write} methods. Note that though it would be more efficient
	in some	cases to bypass the Read method for several consecutive reads, the case of copying
	to a clone would require special handling.
	There is no check for overlapping read & write areas. REVIEW (JohnL): Should there be?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::CopyTo(IStream * pstm, ULARGE_INTEGER cb, ULARGE_INTEGER * pcbRead,
	ULARGE_INTEGER * pcbWritten)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstm);
	ChkComArgPtrN(pcbRead);
	ChkComArgPtrN(pcbWritten);

	if (cb.HighPart)
		ThrowHr(WarnHr(STG_E_INVALIDPARAMETER)); // handle only 32-bit byte counts
	// REVIEW JohnL: should we nevertheless handle cb == max ULARGE_INTEGER as a special case?
	if (pstm == this)
		ThrowHr(WarnHr(STG_E_INVALIDPARAMETER)); // prevent copy to self
	// REVIEW JohnL: is this correct?

	if (pcbRead)
		(*pcbRead).QuadPart = 0;
	if (pcbWritten)
		(*pcbWritten).QuadPart = 0;

	const UCOMINT32 kcbBufferSize = 4096;
	UCOMINT32 cbReadTotal;
	UCOMINT32 cbWrittenTotal = 0;
	byte prgbBuffer[kcbBufferSize];
	UCOMINT32 cbRead = 0;
	UCOMINT32 cbWritten;
	UCOMINT32 cbr = 0;

	for (cbReadTotal = 0; (cbReadTotal < cb.LowPart) && (cbRead == cbr); )
	{
		cbr = cb.LowPart - cbReadTotal;
		if (cbr > kcbBufferSize)
			cbr = kcbBufferSize;
		CheckHr(Read((void *)prgbBuffer, cbr, &cbRead));
		cbReadTotal += cbRead;
		if (cbRead)
		{
			CheckHr(pstm->Write((void *)prgbBuffer, cbRead, &cbWritten));
			cbWrittenTotal += cbWritten;
		}
	}
	if (pcbRead)
		(*pcbRead).LowPart = cbReadTotal;
	if (pcbWritten)
		(*pcbWritten).LowPart = cbWrittenTotal;

	// REVIEW JohnL: How do we define "success" for CopyTo? Should we return a failure if
	//                 cbWrittenTotal != cbReadTotal?
	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Flush the file's buffer. Since we are dealing with a file opened in director mode, we
	just flush the buffer and ignore the grfCommitFlags.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::Commit(DWORD grfCommitFlags)
{
	BEGIN_COM_METHOD;
#if WIN32
		// FlushFileBuffers may return an error if m_hfile doesn't have GENERIC_WRITE access.
	if (!(m_grfstgm & (kfstgmReadWrite | kfstgmWrite)))
		ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	if (!FlushFileBuffers(m_hfile))
	{
		// REVIEW JohnL: Should we check for medium full before returning this code?
		ThrowHr(WarnHr(STG_E_MEDIUMFULL));
	}
#else
	// FlushFileBuffers may return an error if m_hfile doesn't have GENERIC_WRITE access.
	if (!(m_flags & (O_RDWR | O_WRONLY)))
		ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	if (fsync(m_file))
		return S_OK;
#endif

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	This should just return the appropriate error code. See the definition of IStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::Revert(void)
{
	BEGIN_COM_METHOD;

	// This should not be implemented, no transaction support.
	ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Restrict access to a specified range of bytes in this stream.
	NOTE: this is not supported by FileStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Remove the access restriction on a range of bytes previously restricted with
	IStream::LockRegion.
	NOTE: this is not supported by FileStream.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb,
	DWORD dwLockType)
{
	BEGIN_COM_METHOD;

	ThrowHr(WarnHr(STG_E_INVALIDFUNCTION));

	END_COM_METHOD(g_fact, IID_IStream);
}


/*----------------------------------------------------------------------------------------------
	Get the status of an open IStream (file).
	If the caller uses the value STATFLAG_DEFAULT for grfStatFlag then the user must free
	the memory which this method allocates for the file name at pstatstg->pwcsName.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::Stat(STATSTG * pstatstg, DWORD grfStatFlag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pstatstg);

#if WIN32
		if (m_hfile == NULL)
		ThrowHr(WarnHr(E_UNEXPECTED));

	BY_HANDLE_FILE_INFORMATION bhfi;

	if (!GetFileInformationByHandle(m_hfile, &bhfi))
	{
		// The caller does not have sufficient permissions for accessing statistics for this
		// stream object.
		ThrowHr(WarnHr(STG_E_ACCESSDENIED));
	}
#else
	if (m_file < 0)
		ThrowHr(WarnHr(E_UNEXPECTED));

	struct stat filestats;

	//errno = 0;
	if (fstat(m_file, &filestats)) {
		//int err = errno;
		//std::cerr << "Error number " << err << " - " << strerror(err) << '\n';
		ThrowHr(WarnHr(STG_E_ACCESSDENIED));
	}
#endif

	pstatstg->pwcsName = NULL;

	switch (grfStatFlag)
	{
		case STATFLAG_DEFAULT:
		{
			// Requests that the statistics include the pwcsName member of the STATSTG structure.
			StrUniBufPath stubpName = m_staPath.Chars();

			pstatstg->pwcsName = (wchar *)CoTaskMemAlloc(
				(stubpName.Length() + 1) * isizeof(wchar));
			if (NULL == pstatstg->pwcsName)
				ThrowHr(WarnHr(STG_E_INSUFFICIENTMEMORY));

			memcpy(pstatstg->pwcsName, stubpName.Chars(), stubpName.Length() * isizeof(wchar));
			pstatstg->pwcsName[stubpName.Length()] = 0;
		}
		// Fall Through.

		case STATFLAG_NONAME:
			// Requests that the statistics not include the pwcsName member of the STATSTG
			// structure. If the name is omitted, there is no need for the Stat methods to allocate
			// and free memory for the string value for the name and the method can save an Alloc
			// and the caller a Free operation.
			pstatstg->type = STGTY_STREAM;
#if WIN32
			pstatstg->cbSize.HighPart = bhfi.nFileSizeHigh;
			pstatstg->cbSize.LowPart = bhfi.nFileSizeLow;
			pstatstg->mtime = bhfi.ftLastWriteTime;
			pstatstg->ctime = bhfi.ftCreationTime;
			pstatstg->atime = bhfi.ftLastAccessTime;
#else
			pstatstg->cbSize.QuadPart = filestats.st_size;
			time_tToFiletime(filestats.st_mtime, &(pstatstg->mtime));
			time_tToFiletime(filestats.st_ctime, &(pstatstg->ctime));
			time_tToFiletime(filestats.st_atime, &(pstatstg->atime));
#endif
			pstatstg->grfMode = m_grfstgm;
			pstatstg->grfLocksSupported = 0;
			pstatstg->clsid = CLSID_NULL;
			pstatstg->grfStateBits = 0;
			return S_OK;

	default:
		ThrowHr(WarnHr(STG_E_INVALIDFLAG));

	}
	END_COM_METHOD(g_fact, IID_IStream);
}

#if !WIN32
/*----------------------------------------------------------------------------------------------
Utility function to convert time_t to FILETIME.
----------------------------------------------------------------------------------------------*/
void FileStream::time_tToFiletime(time_t intime, FILETIME* outtime) {
	int fileTicksPerSec = 10000000;
	int64 secs1601To1970 = 11644473600LL;

	int64 filetime = (int64)((secs1601To1970 + intime) * fileTicksPerSec);
	memcpy(outtime, &filetime, sizeof(FILETIME));
}
#endif

/*----------------------------------------------------------------------------------------------
Creates a new stream object with its own seek pointer that references the same bytes
as the original stream.
The m_qstrmBase member in the clone is set to the IStream interface pointer of the original
FileStream object for ${#AddRef} and ${#Release} calls which are used to prevent that object
from being deleted until after all clones have been deleted.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP FileStream::Clone(IStream ** ppstm)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppstm);

	FileStream * pfist;

	pfist = NewObj FileStream;

	if (!pfist)
		ThrowHr(WarnHr(STG_E_INSUFFICIENTMEMORY));

#if WIN32
	// If this is the first clone, a pointer to the current object is fine. Otherwise (if we
	// are making a clone from a clone) copy the current pointer.
	if (!m_qstrmBase)
	{
		pfist->m_qstrmBase = this;
	}
	else
	{
		pfist->m_qstrmBase = m_qstrmBase;
	}

	pfist->m_hfile = m_hfile;
	pfist->m_ibFilePos = m_ibFilePos;
	pfist->m_grfstgm = m_grfstgm;
#else
	int file = open(m_staPath, m_flags);
	if (file < 0) {
		//File open failed, throw error
		ThrowHr(ERROR_OPEN_FAILED);
	}

	pfist->m_file = file;
	pfist->m_flags = m_flags;
#endif
	pfist->m_staPath = m_staPath;

	*ppstm = pfist;

	END_COM_METHOD(g_fact, IID_IStream);
}
