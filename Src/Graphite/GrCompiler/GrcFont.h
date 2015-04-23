/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrcFont.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	A class to access the font.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GRC_FONT_INCLUDED
#define GRC_FONT_INCLUDED

#ifdef UINT_MAX
#define GRCFONT_END UINT_MAX
#else
#define GRCFONT_END -1
#endif

/*----------------------------------------------------------------------------------------------
Class: GrcFont
Description: A class representing the font file and used to access its information.
				None of the methods with wGlyphID as an argument can handle pseudo glyph ids.
Hungarian: font
----------------------------------------------------------------------------------------------*/

class GrcFont
{
public:
	GrcFont(char * pchFileName);
	GrcFont(bool fDebug);

	~GrcFont(); //Review: should this be virtual?
	int Init(GrcManager *); // must call before using any of the below methods; clean up handled by dtor

	void GetFontFamilyName(utf16 * rgchwName, int cchMax);

	utf16 FirstFreeGlyph();
	int AutoPseudos(Vector<unsigned int> & vnUnicode, Vector<utf16> & vwGlyphID);

	void GetGlyphsFromCmap(utf16 * rgchwUniToGlyphID);
	unsigned int UnicodeFromCodePage(utf16 wCodePage, utf16 wCodePoint, GdlObject * pgdlobj);
	utf16 GlyphFromCmap(unsigned int nUnicode, GdlObject * pgdlobj);
	utf16 GlyphFromPostscript(StrAnsi staPostscriptName, GdlObject * pgdlobj, bool fError);

	int ConvertGPathToGPoint(utf16 wGlyphID, int nPathNumber, GdlObject * pgdlobj);

	int ScaledToAbsolute(int nValue, int mScale);
	int DesignUnits();

	int GetGlyphMetric(utf16 wGlyphID, GlyphMetric gmet, GdlObject * pgdlobj);

	bool IsPointAlone(utf16 wGlyphID, int nPointNumber, GdlObject * pgdlobj);
	int GetXYAtPoint(utf16 wGlyphID, int nPointNumber, int * mX, int * mY, GdlObject * pgdlobj);
	int GetPointAtXY(utf16 wGlyphID, int mX, int mY, int mPointRadius, GdlObject * pgdlobj);

	// Class for iterating over the potentially wide range of Unicode codepoints in the cmap.
	class iterator
	{
		friend class GrcFont;
	public:
		iterator() // default iterator
		{}

		iterator(GrcFont * pfont, bool fAtEnd = false)
		{
			m_pfont = pfont;
			if (fAtEnd)
			{
				m_iBlock = m_pfont->CBlocks();
				m_nUni = GRCFONT_END;
			}
			else
			{
				m_nUni = m_pfont->m_vnMinUnicode[0];
				m_iBlock = 0;
			}
		}

		iterator & operator ++()
		{
			Assert(m_nUni != GRCFONT_END);
			Assert(m_iBlock < m_pfont->CBlocks() || m_nUni < m_pfont->m_vnLimUnicode[m_iBlock]);
			Assert(m_nUni < m_pfont->m_vnLimUnicode[m_iBlock]);

			m_nUni++;
			if (m_nUni >= m_pfont->m_vnLimUnicode[m_iBlock])
			{
				m_iBlock++;
				if (m_iBlock >= m_pfont->CBlocks())
					m_nUni = GRCFONT_END; // at end
				else
					m_nUni = m_pfont->m_vnMinUnicode[m_iBlock];
			}

			return *this;
		}

		bool operator == (const iterator & fit)
		{
			return (this->m_nUni == fit.m_nUni);
		}
		bool operator != (const iterator & fit)
		{
			return (this->m_nUni != fit.m_nUni);
		}

		unsigned int operator *()
		{
			return m_nUni;
		}

	protected:
		GrcFont * m_pfont;
		unsigned int m_nUni;   // current unicode codepoint
		int m_iBlock; // which block of unicode is current
	};

	friend class iterator;

	// iterators
	iterator Begin()
	{
		iterator fit(this, false);
		return fit;
	}
	iterator End()
	{
		iterator fit(this, true);
		return fit;
	}

	int NumUnicode()
	{
		return m_cnUnicode;
	}

	bool AnySupplementaryPlaneChars()
	{
		return (m_vnLimUnicode[m_vnLimUnicode.Size() - 1] > 0xFFFF);
	}

protected:
	int OpenFile(void);
	int CloseFile(void);
	int ReadData(byte ** ppData, long lnOffset, long lnSize);
	int ReadTable(TableId ktiTableId, void * pHdr, void * pTableDir, byte ** ppTable, long * plnSize);
	int ReadTable(byte*& pTable);

	bool IsGraphiteFont(void * pHdr, void * pTableDir);
	int ScanGlyfIds(void);
	int GetGlyfContours(utf16 wGlyphID, Vector<int> * pvnEndPt);
	int GetGlyfPts(utf16 wGlyphID, Vector<int> * pvnEndPt,
						 Vector<int> * pvnX, Vector<int> * pvnY, Vector<bool> * pvfOnCurve);

protected:
	//	Member variables:

	char *m_pchFileName; // Review: should this use a string class
	FILE *m_pFile;

	byte * m_pCmap;
	long m_cCmap;
	byte * m_pGlyf;
	long m_cGlyf;
	byte * m_pHead;
	long m_cHead;
	byte * m_pHhea;
	long m_cHhea;
	byte * m_pHmtx;
	long m_cHmtx;
	byte * m_pLoca;
	long m_cLoca;
	byte * m_pMaxp;
	long m_cMaxp;
	byte * m_pOs2;
	long m_cOs2;
	byte * m_pPost;
	long m_cPost;
	byte * m_pName;
	long m_cName;

	// point to MS cmap subtables within m_pCmap for MS data
	// try to use the 3-10 pointer first. this is for MS UCS-4 encoding (UTF-32)
	void * m_pCmap_3_10;
	// the 3_1 pointer is for MS Unicode encoding (UTF-16)
	// it should be present even if the 3-10 subtable is also present
	// this could point to a 3-0 table instead of a 3-1 table though 3-1 is attempted first
	void * m_pCmap_3_1;
	int m_nMaxGlyfId;

	// ranges of unicode codepoints in the cmap
	Vector<unsigned int> m_vnMinUnicode;
	Vector<unsigned int> m_vnLimUnicode;
	int m_cnUnicode;

	Vector<unsigned int> m_vnCollisions; // Unicode ids with colliding glyph ids

	bool m_fDebug;

	// for interator
	int CBlocks()
	{
		Assert(m_vnMinUnicode.Size() == m_vnLimUnicode.Size());
		return m_vnMinUnicode.Size();
	}
};

#endif // GRC_FONT_INCLUDED
