/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: WinFont.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Stock implementation of a Windows font class. It is based on a device context, which is
	used to access tables and metrics from the system font.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef WINFONT_INCLUDED
#define WINFONT_INCLUDED

//:End Ignore

namespace gr
{

class FontFace;

/*----------------------------------------------------------------------------------------------
	WinFont class.
----------------------------------------------------------------------------------------------*/
class WinFont : public Font
{
public:
	WinFont() : Font()
	{ }
	WinFont(HDC hdc);
	virtual ~WinFont();

	virtual Font * copyThis();
	WinFont(WinFont & font);

	//virtual FontErrorCode isValidForGraphite(int * pnVersion = NULL, int * pnSubVersion = NULL);

	virtual const void * getTable(fontTableId32 tableID, size_t * pcbSize);
	virtual unsigned int getDPIx();
	virtual unsigned int getDPIy();
	virtual void getFontMetrics(float * pAscent, float * pDescent = NULL,
		float * pEmSquare = NULL);
	virtual void getGlyphPoint(gid16 gid, unsigned int pointNum, gr::Point & xyReturn);
	virtual void getGlyphMetrics(gid16 glyphID, gr::Rect & boundingBox, gr::Point & advances);

	static bool FontHasGraphiteTables(HDC hdc);

	virtual float ascent()
	{
		float pixAscent;
		getFontMetrics(&pixAscent);
		return pixAscent;
	}
	virtual float descent()
	{
		float pixDescent;
		getFontMetrics(NULL, &pixDescent);
		return pixDescent;
	}
	virtual float height()
	{
		float pixAscent;
		float pixDescent;
		getFontMetrics(&pixAscent, &pixDescent);
		return (pixAscent + pixDescent);
	}
	virtual bool bold()
	{
		return m_fpropDef.fBold;
	}
	virtual bool italic()
	{
		return m_fpropDef.fItalic;
	}

	// Specific to WinFont:
	void replaceDC(HDC hdc);
	void restoreDC();
	void restorePreviousFont();
	GrResult SetInternalFont();
	GrResult SetInternalFont(unsigned long clrFore, unsigned long clrBack);

protected:
	// Member variables:
	HDC m_hdc;
	HFONT m_hfont;			// font object needed for Graphite processing/rendering
	HFONT m_hfontClient;	// need to replace this HFONT into the DC when we are finished
							// with it

	// Debugging:
	//OLECHAR m_rgchTemp[32];

	//std::wstring m_strFaceName;
	//int m_pixHeight; // height of font in pixels--value for LOGFONT
	//bool m_fBold;
	//bool m_fItalic;

	FontProps m_fpropDef;	// how this Font is defined
	FontProps m_fpropSet;	// what is set up in m_hfont

	// font table caches:
	byte * m_pbCmapTbl;
	byte * m_pbHeadTbl;
	byte * m_pbHmtxTbl;
	byte * m_pbLocaTbl;
	byte * m_pbGlyfTbl;
	byte * m_pbNameTbl;
	byte * m_pbSileTbl;
	byte * m_pbSilfTbl;
	byte * m_pbFeatTbl;
	byte * m_pbGlatTbl;
	byte * m_pbGlocTbl;
	byte * m_pbSillTbl;
	byte * m_pbOs2Tbl;

	// Methods:
	int IntFromFixed(FIXED f)
	{
		if (f.fract >= 0x8000)
			return(f.value + 1);
		else
			return(f.value);
	}

	float FloatFromFixed(FIXED f)
	{
		return (float)IntFromFixed(f);
	}

	bool CompareFixed(FIXED f1, FIXED f2)
	{
		if (f1.value == f2.value && f1.fract == f2.fract)
			return true;
		return false;
	}

	virtual void UniqueCacheInfo(std::wstring &, bool &, bool &);

//	void WinFont::DebugDC();

	/*------------------------------------------------------------------------------------------
		These internal classes implement a hashmap for caching font handles.
	------------------------------------------------------------------------------------------*/

	class LogFontWrapper
	{
	public:
		LogFontWrapper(LOGFONT lf)	// hopefully the constructor that will actually be used
			: m_lf(lf)
		{
		}
		LogFontWrapper()	// default constructor
		{
			memset(&m_lf, 0, sizeof(LOGFONT));
		}

		// This class exists to make it possible to compare two LOGFONT objects:
		bool operator==(const LogFontWrapper & lf) const;

		// member variable:
		LOGFONT m_lf;
	};

	class LogFontHashFuncs
	{
	public:
		const static size_t bucket_size = 4; // copied these from hash_compare
		const static size_t min_buckets = 8;

		size_t operator() (const WinFont::LogFontWrapper & key) const;	// hash function
		bool operator() (const WinFont::LogFontWrapper & key1, const WinFont::LogFontWrapper & key2) const;	// comparison function
	};

	class FontHandleCache
	{
	public:
		struct FontCacheValue
		{
			int nRefs;   // reference count
			HFONT hfont; // font handle

			bool operator==(const FontCacheValue & val) const
			{
				return (hfont == val.hfont);
			}
		};

		~FontHandleCache();

		HFONT GetFont(LOGFONT & lf);
		void DeleteFont(HFONT hfont);

		typedef stdext::hash_map<LogFontWrapper, FontCacheValue, LogFontHashFuncs> FontHandleHashMap;

	protected:
		FontHandleHashMap m_hmlffcv;

		void ResetFontCache(); // delete all the fonts in the cache
	};

	static FontHandleCache g_fhc;

}; // WinFont

} // namespace gr


#endif // !WINFONT_INCLUDED
