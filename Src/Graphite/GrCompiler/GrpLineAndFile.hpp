/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrpLineAndFile.cpp
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	A simple class to hold the line number and file name for tokens and objects.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef LINENFILE_INCLUDED
#define LINENFILE_INCLUDED

#include <iostream>

class GrpLineAndFile	// hungarian: lnf (line-n-file)
{
public:
	//	Constructors:
	GrpLineAndFile()
		:	m_nLinePre(-1),
			m_nLineOrig(-1)
	{
	}

	GrpLineAndFile(int nPre, int nOrig, StrAnsi sta)
		:	m_nLinePre(nPre),
			m_nLineOrig(nOrig),
			m_staFile(sta)
	{
	}

	//	copy constructor
	GrpLineAndFile(const GrpLineAndFile & lnf)
	{
		m_nLinePre = lnf.m_nLinePre;
		m_nLineOrig = lnf.m_nLineOrig;
		m_staFile = lnf.m_staFile;
	}

	GrpLineAndFile & operator=(const GrpLineAndFile & lnf)
	{
		m_nLinePre = lnf.m_nLinePre;
		m_nLineOrig = lnf.m_nLineOrig;
		m_staFile = lnf.m_staFile;
		return *this;
	}

	//	Getters and setters:
	int PreProcessedLine() const			{ return m_nLinePre; };
	int OriginalLine() const			{ return m_nLineOrig; }
	StrAnsi File() const				{ return m_staFile; }

	void SetOriginalLine(int n)		{ m_nLineOrig = n; }
	void SetPreProcessedLine(int n)	{ m_nLinePre = n; }
	void SetFile(StrAnsi sta)		{ m_staFile = sta; }

	void CopyLineAndFile(GrpLineAndFile & lnf)
	{
		m_nLinePre = lnf.m_nLinePre;
		m_nLineOrig = lnf.m_nLineOrig;
		m_staFile = lnf.m_staFile;
	}

	bool NotSet()
	{
		return m_nLinePre == -1;
	}

	bool operator==(GrpLineAndFile & lnf)
	{
		if (m_nLinePre == 0 && lnf.m_nLinePre == 0)
			return (m_nLineOrig == lnf.m_nLineOrig);	// eg, preprocessor errors
		else
			return m_nLinePre == lnf.m_nLinePre;
	}

	bool operator<(GrpLineAndFile & lnf)
	{
		if (m_nLinePre == 0 && lnf.m_nLinePre == 0)
			return (m_nLineOrig < lnf.m_nLineOrig);	// eg, preprocessor errors
		else
			return m_nLinePre < lnf.m_nLinePre;
	}

	void WriteToStream(std::ostream & strmOut, bool fNoPath)
	{
		int ich = m_staFile.Length();
		// Strip off the file path.
		while (fNoPath && ich > 0 && m_staFile[ich - 1] != '\\')
			ich--;

		StrAnsi staStripped = m_staFile.Right(m_staFile.Length() - ich);
		strmOut << staStripped.Chars() << "(" << m_nLineOrig << ")";
	}

protected:
	//	instance variables:
	int m_nLinePre;			// line in pre-processed file
	int	m_nLineOrig;		// actual line in original file
	StrAnsi m_staFile;		// original file name

public:
	//	for test procedures:
	void test_SetLineNumbers(int n)
	{
		m_nLinePre = n;
		m_nLineOrig = n;
	}
};


#endif // !LINENFILE_INCLUDED
