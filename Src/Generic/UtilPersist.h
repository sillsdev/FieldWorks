/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: UtilPersist.h
Responsibility: Shon Katzenberger
Last reviewed:

	Persistance related utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UtilPersist_H
#define UtilPersist_H 1


/*----------------------------------------------------------------------------------------------
	Read a 32 bit integer from the stream.
----------------------------------------------------------------------------------------------*/
inline void ReadInt(IStream * pstrm, int * pn)
{
	AssertPtr(pstrm);
	AssertPtr(pn);
	ulong luRead;
	CheckHr(pstrm->Read(pn, isizeof(*pn), &luRead));
	if (isizeof(*pn) != luRead)
		ThrowHr(WarnHr(STG_E_READFAULT));
}


/*----------------------------------------------------------------------------------------------
	Write a 32 bit integer to the stream.
----------------------------------------------------------------------------------------------*/
inline void WriteInt(IStream * pstrm, int n)
{
	AssertPtr(pstrm);
	ulong luWrote;
	CheckHr(pstrm->Write(&n, isizeof(n), &luWrote));
	if (isizeof(n) != luWrote)
		ThrowHr(WarnHr(STG_E_WRITEFAULT));
}


/*----------------------------------------------------------------------------------------------
	Read cb bytes from the stream.
----------------------------------------------------------------------------------------------*/
inline void ReadBuf(IStream * pstrm, void * pv, int cb)
{
	AssertPtr(pstrm);
	AssertArray((byte *)pv, cb);
	ulong luRead;
	CheckHr(pstrm->Read(pv, cb, &luRead));
	if ((ulong)cb != luRead)
		ThrowHr(WarnHr(STG_E_READFAULT));
}


/*----------------------------------------------------------------------------------------------
	Write cb bytes to the stream.
----------------------------------------------------------------------------------------------*/
inline void WriteBuf(IStream * pstrm, const void * pv, int cb)
{
	AssertPtr(pstrm);
	AssertArray((byte *)pv, cb);
	ulong luWrote;
	CheckHr(pstrm->Write(pv, cb, &luWrote));
	if ((ulong)cb != luWrote)
		ThrowHr(WarnHr(STG_E_WRITEFAULT));
}


/*----------------------------------------------------------------------------------------------
	Seek in the stream.
----------------------------------------------------------------------------------------------*/
inline void SeekStream(IStream * pstrm, int64 lln, uint uOrigin = STREAM_SEEK_SET,
	int64 * plln = NULL)
{
	AssertPtr(pstrm);
	AssertPtrN(plln);

	ULARGE_INTEGER uli;

	CheckHr(pstrm->Seek(*(LARGE_INTEGER *)&lln, uOrigin, &uli));
	if (plln)
		*plln = uli.QuadPart;
}


/*----------------------------------------------------------------------------------------------
	Seek in a stream but only allow integer positions.
----------------------------------------------------------------------------------------------*/
inline void SeekStream(IStream * pstrm, int64 lib, uint uOrigin, int * pib)
{
	AssertPtr(pstrm);
	AssertPtr(pib);

	ULARGE_INTEGER uli;

	CheckHr(pstrm->Seek(*(LARGE_INTEGER *)&lib, uOrigin, &uli));

	*pib = (int)uli.QuadPart;
	if ((uint64)*pib != uli.QuadPart)
		ThrowHr(WarnHr(E_UNEXPECTED));
}


/*----------------------------------------------------------------------------------------------
	Get the current seek position in the stream.
----------------------------------------------------------------------------------------------*/
inline void GetStreamLoc(IStream * pstrm, int64 * plln)
{
	AssertPtr(pstrm);
	AssertPtr(plln);

	LARGE_INTEGER li;

	li.QuadPart = 0;
	CheckHr(pstrm->Seek(li, STREAM_SEEK_CUR, (ULARGE_INTEGER *)plln));
}


/*----------------------------------------------------------------------------------------------
	Get the current seek position in the stream but only allow integer positions.
----------------------------------------------------------------------------------------------*/
inline void GetStreamLoc(IStream * pstrm, int * pib)
{
	AssertPtr(pstrm);
	AssertPtr(pib);

	LARGE_INTEGER li;
	ULARGE_INTEGER uli;

	li.QuadPart = 0;
	CheckHr(pstrm->Seek(li, STREAM_SEEK_CUR, &uli));

	*pib = (int)uli.QuadPart;
	if ((uint64)*pib != uli.QuadPart)
		ThrowHr(WarnHr(E_UNEXPECTED));
}


/*----------------------------------------------------------------------------------------------
	Get the current size of the stream.
----------------------------------------------------------------------------------------------*/
inline void GetStreamSize(IStream * pstrm, int64 * plln)
{
	AssertPtr(pstrm);
	AssertPtr(plln);

	STATSTG stat;

	CheckHr(pstrm->Stat(&stat, STATFLAG_NONAME));

	*plln = stat.cbSize.QuadPart;
}


/*----------------------------------------------------------------------------------------------
	Get the current size of the stream but only allow integer sizes.
----------------------------------------------------------------------------------------------*/
inline void GetStreamSize(IStream * pstrm, int * pcb)
{
	AssertPtr(pstrm);
	AssertPtr(pcb);

	STATSTG stat;

	CheckHr(pstrm->Stat(&stat, STATFLAG_NONAME));

	*pcb = (int)stat.cbSize.QuadPart;
	if ((uint64)*pcb != stat.cbSize.QuadPart)
		ThrowHr(WarnHr(E_UNEXPECTED));
}


/*----------------------------------------------------------------------------------------------
	Read/write StrUni and StrAnsi strings to/from streams.
----------------------------------------------------------------------------------------------*/
template<typename XChar>
	void ReadString(IStream * pstrm, StrBase<XChar> & stb);
template<typename XChar>
	void WriteString(IStream * pstrm, StrBase<XChar> & stb);


/*----------------------------------------------------------------------------------------------
	Resolve psz into a full path name.
----------------------------------------------------------------------------------------------*/
void GetFullPathName(const char * psz, StrAnsi & staPath);

/*----------------------------------------------------------------------------------------------
	Find the long path name.
----------------------------------------------------------------------------------------------*/
void GetLongPathname(const char * psz, StrAnsi & staPath);


/*----------------------------------------------------------------------------------------------
	Copy bytes from one stream to another.
----------------------------------------------------------------------------------------------*/
void CopyBytes(IStream * pstrmSrc, IStream * pstrmDst, int cb);

void FillBytes(IStream * pstrmDst, byte b, int cb);


/*----------------------------------------------------------------------------------------------
	Write the proper line end character sequence to the stream.
----------------------------------------------------------------------------------------------*/
inline void WriteLineEnd(IStream * pstrm)
{
#ifdef WIN32
	// for MS-DOS and MS Windows, line end = carriage return, linefeed
	WriteBuf(pstrm, "\r\n", 2);
#else
	// for Unix, \n = linefeed; for Macintosh, \n = carriage return (in most compilers)
	WriteBuf(pstrm, "\n", 1);
#endif
}

#endif // !UtilPersist_H
