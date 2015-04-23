/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: WinSegmentPainter.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	A segment-painter to use on the Windows platform.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GR_WINSEGPNTR_INCLUDED
#define GR_WINSEGPNTR_INCLUDED

// undo automagic DrawTextA DrawTextW stuff
// #undef DrawText

//:End Ignore


namespace gr
{

/*----------------------------------------------------------------------------------------------
	The WinSegmentPainter knows how to draw on a Windows device context.

	Hungarian: wsegp
----------------------------------------------------------------------------------------------*/

class WinSegmentPainter : public SegmentPainter
{
public:
	WinSegmentPainter(Segment * pseg, HDC hdc, float xsOrigin = 0, float ysOrigin = 0);
	~WinSegmentPainter();

	void paint();
	void drawInsertionPoint(int ichwIP, bool fAssocPrev,
		bool bOn, bool fForceSplit);  // LgIPDrawMode dm);
	bool drawSelectionRange(int ichwAnchor, int ichwEnd,
		float ydLineTop, float ydLineBottom, bool bOn);

	virtual void setOrigin(float xsOrigin, float ysOrigin);
	virtual void setPosition(float xdPosition, float ydPosition);
	virtual void setScalingFactors(float xFactor, float yFactor);

	// Additional methods in the Windows-specfic interface:
	void setDC(HDC hdc)
	{
		m_hdc = hdc;
	}
	HDC getDC()
	{
		return m_hdc;
	}

protected:
	void ReplaceDC(HDC hdc);
	void RestoreDC();
	void RestorePreviousFont();

	void SetSeg(Segment * pseg)
	{
		m_pseg = pseg;
	}
	virtual void DrawTextExt(int x, int y,
		int cgid, const OLECHAR __RPC_FAR * prggid,
		UINT uOptions, const RECT __RPC_FAR * pRect, int __RPC_FAR * prgdx);
	virtual void InvertRect(float xLeft, float yTop, float xRight, float yBottom);
	virtual void GetMyClipRect(RECT * prect);

	//static COLORREF SetTextColor(HDC hdc, COLORREF clr)
	//{
	//	return ::SetTextColor(hdc, PALETTERGB(GetRValue(clr), GetGValue(clr), GetBValue(clr)));
	//}
	//static COLORREF SetBkColor(HDC hdc, COLORREF clr)
	//{
	//	return ::SetBkColor(hdc, PALETTERGB(GetRValue(clr), GetGValue(clr), GetBValue(clr)));
	//}

	void WinSegmentPainter::paintAux();
	virtual void paintBackground(int xs, int ys);
	virtual void paintForeground(int xs, int ys);

protected:
	// member variables:
	HDC m_hdc;

	// The following is a representation of all the glyphs optimized for the way we need to
	// draw on the Windows platform. That is, the glyphs are divided into "streams" that can
	// be drawn using a single ExtTextOut command. The glyphs in each "stream" share a common
	// color and y_offset.
	//
	// Also note that the coordinates all use integers, not floats.
	void SetUpFromSegment();
	void ClearSegmentCache();
	void SetFontProps(unsigned long clrFore, unsigned long clrBack);

protected:
	struct GlyphStrmKey		// hungarian gsk
	{
		//	The y-offset and color are what identifies each glyph stream.
		int ys;			// distance above the baseline
		int clrFore;
		int clrBack;
		// bool fUnderline

		bool LessThan(GlyphStrmKey * pgsk2)
		{
			if (pgsk2->ys > ys)
				return true;
			else if (pgsk2->ys < ys)
				return false;
			else if (pgsk2->clrFore > clrFore)
				return true;
			else if (pgsk2->clrFore < clrFore)
				return false;
			else if (pgsk2->clrBack > clrBack)
				return true;
			else
				return false;
		}
	};

	//	Each GlyphStrm includes a sequence of glyphs that have a common y-offset and color,
	//	sorted by x-position.
	struct GlyphStrm		// hungarian: gstrm
	{
		GlyphStrmKey gsk;
		std::vector<OLECHAR> vchwGlyphId;
		std::vector<int> vdxd;
		std::vector<int> vigbb;
		int xsStart;

		GlyphStrm() : xsStart(-1)	// constructor
		{ };
	};
	GlyphStrm * m_prggstrm;
	int m_cgstrm;

	//	Data for a single glyph:
	struct GlyphBb {		// hungarian: gbb
		int igstrm;	// which glyph stream it has been assigned to
		int iGlyph;	// which glyph in that stream
		GlyphInfo * pginf;
		int ig;		// segment glyph index
	};
	GlyphBb * m_prggbb;
	int m_cgbb;

protected:

	static int GlyphKeySort(const void *, const void *);
	static int GlyphBbSort(const void *, const void *);
	virtual void setForegroundPaintColor(GlyphStrmKey & gsk);

	int LeftEdge();
};

} // namespace gr

#if defined(GR_NO_NAMESPACE)
using namespace gr;
#endif

#endif  // !GR_WINSEGPNTR_INCLUDED
