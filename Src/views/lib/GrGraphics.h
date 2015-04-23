/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrGraphics.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	The version of the VwGraphics that is used for Graphite.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef GRGRAPHICS_INCLUDED
#define GRGRAPHICS_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: GrGraphics
Description:
----------------------------------------------------------------------------------------------*/
class GrGraphics : public IGrGraphics
{
public:
	GrGraphics();
//	GrGraphics(SilTestSite * psts, bool fDraw, bool fFile);
	~GrGraphics();

	virtual long IncRefCount(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	virtual long DecRefCount(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	virtual GrResult InvertRect(int xLeft, int yTop, int xRight, int yBottom);
	virtual GrResult put_ForeColor(int nRGB);
	virtual GrResult put_BackColor(int nRGB);
	virtual GrResult DrawRectangle(int xLeft, int yTop, int xRight, int yBottom);
	virtual GrResult DrawHorzLine(int xLeft, int xRight, int y, int dyHeight,
		int cdx, int * prgdx, int * pdxStart);
	virtual GrResult DrawLine(int xStart, int yStart, int xEnd, int yEnd);
	virtual GrResult DrawText(int ixTwips, int iyTwips, int cch, const OLECHAR * prgch,
		int nStretch);
	virtual GrResult DrawTextExt(int x, int y, int cch, const OLECHAR __RPC_FAR * prgchw,
		UINT uOptions, const RECT __RPC_FAR * pRect, int __RPC_FAR * prgdx);
	virtual GrResult GetTextExtent(int cch, const OLECHAR * prgch,
		int *pnTwipsWidth, int *pnTwipsHeight);
	virtual GrResult GetTextLeadWidth(int cch, const OLECHAR * prgch, int ich,
		int dxStretch, int * pdx);
	virtual GrResult GetClipRect(int * pxLeft, int * pyTop, int * pxRight, int * pyBottom);
	virtual GrResult GetFontEmSquare(int * pxyFontEmSquare);
	virtual GrResult GetGlyphMetrics(int gid,
		int * psBoundingWidth, int * pyBoundingHeight,
		int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY);
	virtual GrResult GetFontData(int nTableId, int * pcbTableSz, byte * prgb, int cbMax);
	virtual GrResult XYFromGlyphPoint(int gid, int nPoint, int * pxRet, int * pyRet);
	virtual GrResult get_FontAscent(int* pdy);
	virtual GrResult get_FontDescent(int* pdy);
	virtual GrResult get_FontCharProperties(LgCharRenderProps * pchrp);
	virtual GrResult ReleaseDC();
	virtual GrResult get_XUnitsPerInch(int * pxInch);
	virtual GrResult put_XUnitsPerInch(int xInch);
	virtual GrResult get_YUnitsPerInch(int * pyInch);
	virtual GrResult put_YUnitsPerInch(int yInch);
	virtual GrResult SetupGraphics(LgCharRenderProps * pchrp);
	virtual GrResult PushClipRect(RECT rcClip);
	virtual GrResult PopClipRect();

	virtual GrResult Initialize(HDC hdc);
	virtual GrResult GetDeviceContext(HDC * phdc);
	virtual GrResult SetMeasureDc(HDC hdc);
	virtual GrResult SetClipRect(RECT * prcClip);

	// Other public methods
	HDC DeviceContext()
	{
		return m_hdc;
	}
	int GetXInch();
	int GetYInch();
	void SetFont(HFONT hfont);

protected:
	// Baselining variables
#ifdef BASELINE
	bool m_fFile;
	bool m_fDraw;

	SilTestSite *m_psts;
#endif

	void MyGetClipRect(RECT * prc);
	// Member variables
	long m_cref;
	int m_rgbForeColor;
	int m_rgbBackColor;
	HDC m_hdc;
	HDC m_hdcMeasure;
	Rect m_rcClip;

	typedef Vector<HRGN> VecHRgn;

	// Stack of clip regions used by PushClipRect and PopClipRect.
	VecHRgn m_vhrgnClipStack;
	HFONT m_hfontOld; // original font to restore into the DC.
	// If we have a distinct measure DC, we save its original font here to restore into it.
	HFONT m_hfontOldMeasure;
	HFONT m_hfont; // current font selected into DC, if any
	LgCharRenderProps m_chrp;

	// Vertical and horizontal resolution. Zero indicates not yet initialized.
	int m_xInch;
	int m_yInch;

	// Other protected methods

	void Init();

	int IntFromFixed(FIXED f)
	{
		if (f.fract >= 0x8000)
			return(f.value + 1);
		else
			return(f.value);
	}

	bool CompareFixed(FIXED f1, FIXED f2)
	{
		if (f1.value == f2.value && f1.fract == f2.fract)
			return true;
		return false;
	}

	void CheckDc();
};

#endif  // GRGRAPHICS_INCLUDED
