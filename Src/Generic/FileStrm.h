/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FileStrm.h
Original author: John Landon
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	This class provides an IStream wrapper around a standard FILE object.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FILESTRM_H_INCLUDED
#define FILESTRM_H_INCLUDED

// Mode bits for opening files (same values as in VC98\Include\Objbase.h)
enum
{
	kfstgmRead = STGM_READ,
	kfstgmWrite = STGM_WRITE,
	kfstgmReadWrite = STGM_READWRITE,
	kfstgmFailIfThere = STGM_FAILIFTHERE,
	kfstgmCreate = STGM_CREATE,			// Allows replacing/overwriting existing file
	kfstgmShareDenyNone = STGM_SHARE_DENY_NONE,
	kfstgmShareDenyRead = STGM_SHARE_DENY_READ,
	kfstgmShareDenyWrite = STGM_SHARE_DENY_WRITE,
	kfstgmShareExclusive = STGM_SHARE_EXCLUSIVE
};

/*----------------------------------------------------------------------------------------------
This class provides IStream I/O to "normal" files. IStream I/O to "structured files"
(aka "compound documents") is already provided in the implementation which comes with VC++.
@h3{Hungarian: fist}
----------------------------------------------------------------------------------------------*/
class FileStream : public IStream
{
public:
	//:> Static methods
	static void Create(LPCOLESTR pszFile, int grfstgm, IStream ** ppstrm);
	static void Create(LPCSTR pszFile, int grfstgm, IStream ** ppstrm);

	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID riid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	//:> IStream methods
	STDMETHOD(Read)(void * pv, UCOMINT32 cb, UCOMINT32 * pcbRead);
	STDMETHOD(Write)(const void * pv, UCOMINT32 cb, UCOMINT32 * pcbWritten);
	STDMETHOD(Seek)(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER * plibNewPosition);
	STDMETHOD(SetSize)(ULARGE_INTEGER libNewSize);
	STDMETHOD(CopyTo)(IStream * pstm, ULARGE_INTEGER cb, ULARGE_INTEGER * pcbRead,
		ULARGE_INTEGER * pcbWritten);
	STDMETHOD(Commit)(DWORD grfCommitFlags);
	STDMETHOD(Revert)(void);
	STDMETHOD(LockRegion)(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
	STDMETHOD(UnlockRegion)(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
	STDMETHOD(Stat)(STATSTG * pstatstg, DWORD grfStatFlag);
	STDMETHOD(Clone)(IStream ** ppstm);

#if !WIN32
	void time_tToFiletime(time_t intime, FILETIME* outtime);
#endif

	static int ErrorStringId(HRESULT hr);

protected:
	//:> Member variables.
	int m_cref;
#if WIN32
	ULARGE_INTEGER m_ibFilePos;
	HANDLE m_hfile;
	// Smart pointer to the original (base) IStream object. This member is NULL in
	// the base itself. (Note that the default constructor sets it to NULL.)
	IStreamPtr m_qstrmBase;
#else
	int m_file;
	int m_flags;
#endif
	StrAnsi m_staPath;
	int m_grfstgm;  // Passed as a parameter to Create method; used by Clone method.

	//:> Constructors and destructors.
	FileStream();
	~FileStream();

	//:> Methods
	void Init(LPCOLESTR pszFile, int grfstgm);
	bool SetFilePosRaw(); // Updates the file's seek pointer to the stream's seek position
};
DEFINE_COM_PTR(FileStream);

#endif  /*FILESTRM_H_INCLUDED*/
