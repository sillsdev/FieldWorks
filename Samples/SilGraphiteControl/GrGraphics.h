#ifndef GRGRAPHICS_INCLUDED
#define GRGRAPHICS_INCLUDED

#include "IGrGraphics.h"

class GrGraphics : public IGrGraphics
{
public:
	GrGraphics();
	GrGraphics(const GrGraphics& grfx);
	~GrGraphics();

	// use pixels for coordinates instead of twips as IGrGraphics wrongly indicates
	virtual GrResult InvertRect(int xLeft, int yTop, int xRight, int yBottom);
	virtual GrResult DrawTextExt(int x, int y, int cch, const OLECHAR __RPC_FAR * prgchw,
		UINT uOptions, const RECT * pRect, int * prgdx);
	virtual GrResult GetFontEmSquare(int * pxyFontEmSquare);
	// fix psBoundingWidth inherited from IGrGraphics
	virtual GrResult GetGlyphMetrics(int chw,
		int * pxBoundingWidth, int * pyBoundingHeight,
		int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY);
	virtual GrResult GetFontData(int nTableId, int * pcbTableSz, byte * prgb, int cbMax);
	virtual GrResult XYFromGlyphPoint(int chw, int nPoint, int * pxRet, int * pyRet);
	virtual GrResult get_FontAscent(int* pdy);
	virtual GrResult get_FontDescent(int* pdy);
	virtual GrResult get_YUnitsPerInch(int * pyInch);
	//will NOT change font face name regardless of LgCharRenderProps.szFaceName
	virtual GrResult SetupGraphics(LgCharRenderProps * pchrp);
	virtual GrResult get_FontCharProperties(LgCharRenderProps * pchrp)
	{
		// TODO: implement this.
		return kresNotImpl;
	}

	//not inherited from IGrGraphics but needed to set up GrGraphics
	GrResult Initialize(HDC hdc); //load dc into obj
	GrResult SetFont(HFONT hfont); //set font initially, call before SetupGraphics

protected:
	// Member variables
	HDC m_hdc;
	HFONT m_hfont; // current font selected into DC, if any
	LgCharRenderProps m_chrp;
	wchar_t m_szFaceName[32];

	// Vertical resolution. Zero indicates not yet initialized.
	int m_yInch;

public:
	HDC get_m_hdc(void);
	HFONT get_m_hfont(void);
	LgCharRenderProps get_m_chrp(void);
	int get_m_yInch(void);
	wchar_t* get_m_szFaceName(void);
};

#endif
