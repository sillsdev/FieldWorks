/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: ResourceStrm.h
Original author: John Landon
Responsibility: Alistair Imrie
Last reviewed: Not yet.

Description:
	This is the header file for the ResourceStream class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef RESOURCESTRM_H_INCLUDED
#define RESOURCESTRM_H_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: ResourceStream
Description: This class provides an IStream interface to a Windows resource object.
@h3{Hungarian: rest}
----------------------------------------------------------------------------------------------*/
class ResourceStream : public IStream
{
public:
	//:> Static methods
	static void Create(HMODULE hModule, const achar * pszType, int rid, IStream ** ppstrm);

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

protected:
	//:> Member variables
	int m_cref;
	ULONG m_cbData;		// total size of resource data in bytes
	byte * m_prgbData;	// pointer to start of resource data
	byte * m_pbCur;		// pointer to current (seek) position in Resource data

	//:> Constructors/destructors/etc.
	ResourceStream();
	~ResourceStream();

	//:> Methods
	void Init(HMODULE hmod, const achar * pszType, int rid);
};

#endif  /*RESOURCESTRM_H_INCLUDED*/
