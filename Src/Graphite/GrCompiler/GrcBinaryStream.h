/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrcBinaryStream.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef WRC_BINSTRM_INCLUDED
#define WRC_BINSTRM_INCLUDED

using std::fstream;

/*----------------------------------------------------------------------------------------------
Class: GrcBinaryStream
Description: Our stream for writing to the TrueType font.
Hungarian: bstrm
----------------------------------------------------------------------------------------------*/

class GrcBinaryStream : public fstream
{
public:
	GrcBinaryStream(char * stFileName)
		: fstream(stFileName, std::ios::binary | std::ios::out | std::ios::in | std::ios::trunc)
	{
	}

	~GrcBinaryStream()
	{
	}

public:
	void WriteByte(int);
	void WriteShort(int);
	void WriteInt(int);
	void Write(char * pbTable, long cbSize)
	{
		write(pbTable, cbSize);
	}

	long Position()
	{
		return tellp();
	}

	void SetPosition(long lPos)
	{
		seekp(lPos);
	}

	long SeekPadLong(long ibOffset);

	void Close(void)
	{
		close();
	}
};


/*----------------------------------------------------------------------------------------------
Class: GrcSubStream
Description: A substream that will eventually be output on the main stream.
Hungarian: substrm
----------------------------------------------------------------------------------------------*/
class GrcSubStream
{
public:
	void WriteByte(int);
	void WriteShort(int);
	void WriteInt(int);

public:
	std::ostream m_strm;
};

#endif // !WRC_BINSTRM_INCLUDED
