/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

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
