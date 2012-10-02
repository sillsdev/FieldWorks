/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: StringStrm.h
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Definition of StrAnsiStream, an IStream wrapper for StrAnsi objects.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef STRINGSTRM_H_INCLUDED
#define STRINGSTRM_H_INCLUDED
//:End Ignore

/*----------------------------------------------------------------------------------------------
	This class is used to wrap a StrAnsi string inside an IStream interface.

	Hungarian: stas
----------------------------------------------------------------------------------------------*/
class StrAnsiStream : public IStream
{
public:
	//:> Static methods
	static void Create(StrAnsiStream ** ppstas);

	//:> IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)();
	STDMETHOD_(ULONG, Release)();
	//:> IStream methods
	STDMETHOD(Read)(void * pv, ULONG cb, ULONG * pcbRead);
	STDMETHOD(Write)(const void * pv, ULONG cb, ULONG * pcbWritten);
	STDMETHOD(Seek)(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER * plibNewPosition);
	STDMETHOD(SetSize)(ULARGE_INTEGER libNewSize);
	STDMETHOD(CopyTo)(IStream * pstm, ULARGE_INTEGER cb, ULARGE_INTEGER * pcbRead,
		ULARGE_INTEGER * pcbWritten);
	STDMETHOD(Commit)(DWORD grfCommitFlags);
	STDMETHOD(Revert)();
	STDMETHOD(LockRegion)(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
	STDMETHOD(UnlockRegion)(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType);
	STDMETHOD(Stat)(STATSTG * pstatstg, DWORD grfStatFlag);
	STDMETHOD(Clone)(IStream ** ppstm);

	// Making this public allows us to read/set without a separate interface.
	StrAnsi m_sta;

protected:
	//:Ignore
	StrAnsiStream()
	{
		ModuleEntry::ModuleAddRef();
		m_cref = 1;
	}

	~StrAnsiStream()
	{
		ModuleEntry::ModuleRelease();
	}
	//:End Ignore

	//:> Member variables
	long m_cref;
	int m_ich;
};

typedef GenSmartPtr<StrAnsiStream> StrAnsiStreamPtr;

// Local Variables:
// mode:C++
// End:

#endif /*STRINGSTRM_H_INCLUDED*/
