/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwGraphics.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Implements the actual drawing functions we need. Some methods are just used by helper
	classes like ActualTextProperties.

Original location: Src/views/lib/VwGraphics.cpp
-------------------------------------------------------------------------------*//*:End Ignore*/

/***********************************************************************************************
	Include files
***********************************************************************************************/

#define GLIB_VERSION_MIN_REQUIRED GLIB_VERSION_2_26

// Fieldworks includes
#include "Main.h"
#include "UnicodeString8.h"

// System includes
#include <cairomm/cairomm.h>
#include <cairomm/context.h>
#include <pangomm/layout.h>
#include <algorithm>
#include <stdexcept>
#include <cairo-xlib.h>
#include <vector>

#include <pango/pangocairo.h>
/* includes gdiplus namespace - contains internal structures from mono gdi-plus implementation*/
#include "GdiPlusMono.h"

#include <assert.h>

#ifdef DEBUG
#include <sys/types.h>
#include <unistd.h>
#endif

// Debugging
#ifdef _VW_GRAPHICS_DEBUG
#include <iostream>
#define TRACE(x) (std::cerr << x << std::endl)
#define XTRACE(x) (std::cout << x << std::endl)
#else
#define TRACE(x)
#define XTRACE(x)
#endif

// (Hopefully) temporary replacement COM macros
#undef BEGIN_COM_METHOD
#undef END_COM_METHOD
#define BEGIN_COM_METHOD try {
#define END_COM_METHOD }\
catch (Cairo::logic_error & e) {\
	TRACE("A Cairo::logic_error of type " << e.what() << " occured");\
}\
catch (std::exception& e) {\
	TRACE("A std::exception of type " << e.what() << " occured");\
}\
catch (...) {\
	TRACE("An exception occured in VwGraphics on line " << __LINE__ );\
}\
return S_OK;\

// If VwGraphicsCario is created with a NULL hdc then use these values to create
// a dummy cairo suface.
#define DEFAULT_WIDTH 800
#define DEFAULT_HEIGHT 600

/***********************************************************************************************
	Forward declarations
***********************************************************************************************/

/***********************************************************************************************
	Local Constants and static variables
***********************************************************************************************/

/***********************************************************************************************
	Two local classes, copied from AfGfx.h. Maybe we should move them to somewhere they
	can be shared more easily?
***********************************************************************************************/

/***********************************************************************************************
	Constructors/Destructor
***********************************************************************************************/
VwGraphicsCairo::VwGraphicsCairo() : m_cref(1), m_hdc(NULL)
{
	g_type_init(); // provide more info from asserts.
	TRACE("VwGraphics constructor called\n");

	m_xInch = 0;
	m_yInch = 0;

	m_enabled = 0;
	m_foregroundColor = 0;
	// ModuleEntry::ModuleAddRef();

	m_pangoFontDescription = NULL;
	m_fontMap = NULL;
	m_fontMapForFontContext = NULL;
	m_fontContext = NULL;
	m_context = NULL;
	m_layout = NULL;

	m_rcClip.left = 0;
	m_rcClip.right = 0;
	m_rcClip.top = 0;
	m_rcClip.bottom = 0;

#if DEBUG
	if (getenv("LOG_VWGRAPHICS") != NULL)
	{
		char filename[1024];
		sprintf(filename, "/tmp/VwGraphicsCairo.%d", getpid());
		m_loggingFile = fopen(filename, "at");
	}
	else
	{
		m_loggingFile = NULL;
	}
#endif
}

#ifdef BASELINE
VwGraphicsCairo::VwGraphicsCairo(SilTestSite *psts, bool fDraw, bool fFile)
{
}
#endif

VwGraphicsCairo::~VwGraphicsCairo()
{
	 TRACE("VwGraphics destructor called");

#if DEBUG
	if (m_loggingFile != NULL)
		fclose(m_loggingFile);
#endif

	// ModuleEntry::ModuleRelease();
}

HRESULT VwGraphicsCairo::QueryInterface(REFIID riid, void** ppv)
{
	 if (!ppv)
		return /*WarnHr*/(E_POINTER);

	*ppv = NULL;

	if (&riid == &CLID_VWGRAPHICS_IMPL)
	{
		*ppv = static_cast<VwGraphics *>(this);
	}
	else if (riid == __uuidof(IUnknown))
	{
		*ppv = static_cast<IUnknown*>(this);
	}
	else if (riid == __uuidof(IVwGraphicsWin32))
	{
		*ppv = static_cast<IVwGraphicsWin32*>(this);
	}
	else if (riid == __uuidof(IVwGraphics))
	{
		*ppv = static_cast<IVwGraphics*>(this);
	}
	else
	{
		return E_NOINTERFACE;
	}
	AddRef();

	return S_OK;
}

// VwGraphicsWin32 methods

// IVwGraphicsWin32 methods
HRESULT VwGraphicsCairo::Initialize(HDC hdc)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "Initialize %p\n", this);
		fflush(m_loggingFile);
	}
#endif

	if (hdc)
	{
		m_hdc = hdc;
		gdiplus::Graphics* graphics = reinterpret_cast<gdiplus::Graphics*>(m_hdc);

		m_ctxt = Cairo::RefPtr<Cairo::Context>(new Cairo::Context(graphics->ct));
		m_enabled = 1;
	}
	else
	{
		// Initialize with a some default cairo object
		// TODO-Linux: Improve this by using cairo_xlib_surface_create with a X11 Display.
		cairo_surface_t* crs = cairo_image_surface_create(CAIRO_FORMAT_RGB24, DEFAULT_WIDTH, DEFAULT_HEIGHT);
		cairo_t* cr = cairo_create(crs);

		m_ctxt = Cairo::RefPtr<Cairo::Context>(new Cairo::Context(cr));
		m_enabled = 1;

		cairo_destroy(cr); // release ref
		cairo_surface_destroy(crs);	// release ref
	}

	return S_OK;
}

HRESULT VwGraphicsCairo::GetDeviceContext(HDC *phdc)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "GetDeviceContext %p\n", this);
		fflush(m_loggingFile);
	}
#endif
	*phdc = m_hdc;
	return S_OK;
}

HRESULT VwGraphicsCairo::SetMeasureDc(HDC hdc)
{
	assert(false);
	return S_OK;
}


/*----------------------------------------------------------------------------------------------
	Initialize a new instance.
----------------------------------------------------------------------------------------------*/
void VwGraphicsCairo::Init()
{
	StrUtil::InitIcuDataDir();
}

/***********************************************************************************************
	Generic factory stuff to allow creating an instance with CoCreateInstance.
***********************************************************************************************/

static GenericFactory g_fact(
	_T("SIL.Text.VwGraphicsWin32"),
	&CLSID_VwGraphicsWin32,
	_T("SIL Graphics"),
	_T("Apartment"),
	&VwGraphicsCairo::CreateCom);


void VwGraphicsCairo::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);

	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	VwGraphicsCairo* pTest = new VwGraphicsCairo(); // ref count initialy 1

	if (pTest == NULL)
		ThrowHr(WarnHr(E_OUTOFMEMORY));

	// Get the requested interface.
	HRESULT hr = pTest->QueryInterface(riid, ppv);
	TRACE("VwGraphics Interface query result:" << std::hex << hr);

	// Release the IUnknown pointer.
	// (If QueryInterface failed, component will delete itself.)
	pTest->Release();

	CheckHr(WarnHr(hr));
}

/***********************************************************************************************
	IVwGraphics Interface Methods
***********************************************************************************************/
/*----------------------------------------------------------------------------------------------
	Invert the specified rectangle.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::InvertRect(int xLeft, int yTop, int xRight, int yBottom)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "InvertRect %p %d %d %d %d\n", this, xLeft, yTop, xRight, yBottom);
		fflush(m_loggingFile);
	}
#endif
	BEGIN_COM_METHOD;
	/*
	 * TODO:
	 * Make this work
	 * Cairo OPERATOR_XOR doesn't seem to work
	 *
	 * Dispite what 'some' documentation may say - reading up on the operators suggests
	 * that it doesn't do what you may expect (ie work like a bitwise XOR)
	 *
	 * in Addition to this given the way we are currently paint the background,
	 * (as a normal rectangle which we draw first) the background would also
	 * interfere with XOR operation.
	 * The internet says that there is no easy way for inverting text using cairo.
	 * The suggested why is using paths (ie remembering when we have drawn the text)
	 * however given the views framework this is likley to be infesible/hugly complicated.
	 *
	 * Therefore given the need to have a carret that stands out more I have special cased it
	 * with a HACK that displays the carrat black when its 2 pixcels wide or smaller and
	 * alpha blends it when its not.
	 */

	CheckDc();

	// See if rectangle is outside clipping box
	RECT rectClip;
	MyGetClipRect(&rectClip);
	RECT rectDraw;
	rectDraw.left = xLeft;
	rectDraw.right = xRight;
	rectDraw.top = yTop;
	rectDraw.bottom = yBottom;
	if(!rectIntersect(&rectClip, &rectDraw)) {
		return S_OK;
	}

	// Only draw rectangles thats in the clipping region, by setting a cairo clipping region.
	m_ctxt->reset_clip();
	m_ctxt->rectangle(rectClip.left, rectClip.top, rectClip.right - rectClip.left, rectClip.bottom - rectClip.top);
	m_ctxt->clip();

	if (xRight - xLeft <= 2)
		m_ctxt->set_source_rgba(0.0, 0.0, 0.0, 1.0);
	else
		m_ctxt->set_source_rgba(0.5, 0.5, 1.0, 0.4);

	m_ctxt->rectangle(xLeft, yTop, xRight - xLeft, yBottom - yTop);
	m_ctxt->fill();

	// Undo the cairo clipping region.
	m_ctxt->reset_clip();

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Set the foreground color used for lines, text
	Arguments:
		nRGB			RGB color value or kclrTransparent
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::put_ForeColor(int nRGB)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "put_ForeColor %p %d\n", this, nRGB);
		fflush(m_loggingFile);
	}
#endif

 BEGIN_COM_METHOD;
	// Set values for color from nRGB
	if ((unsigned int)nRGB == kclrTransparent) {
		VwColor newFore;
		newFore.m_transparent = true;
		m_foregroundColor = newFore;
	}
	else {
		VwColor newFore(nRGB);
		m_foregroundColor = newFore;
	}

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Background color, used for shape interior, text background
	Arguments:
		nRGB			RGB color value or kclrTransparent
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::put_BackColor(int nRGB)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "put_BackColor %p %d\n", this, nRGB);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	// Set values for color from nRGB
	if ((unsigned int)nRGB == kclrTransparent) {
		VwColor newBack;
		newBack.m_transparent = true;
		m_backgroundColor = newBack;
	}
	else {
		VwColor newBack(nRGB);
		m_backgroundColor = newBack;
	}

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a rectangle, filled with the current background color
	ENHANCE: should we outline it in the foreground color?
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::DrawRectangle(int xLeft, int yTop, int xRight, int yBottom)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "DrawRectangle %p %d %d %d %d\n", this, xLeft, yTop, xRight, yBottom);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	// Trivially exit if the color is set to transparent
	if(m_backgroundColor.m_transparent) {
		return S_OK;
	}

	// See if rectangle is outside clipping box
	RECT rectClip;
	MyGetClipRect(&rectClip);
	RECT rectDraw;
	rectDraw.left = xLeft;
	rectDraw.right = xRight;
	rectDraw.top = yTop;
	rectDraw.bottom = yBottom;
	if(!rectIntersect(&rectClip, &rectDraw)) {
		return S_OK;
	}

	CheckDc();

	// Only draw rectangles thats in the clipping region, by setting a cairo clipping region.
	m_ctxt->reset_clip();
	m_ctxt->rectangle(rectClip.left, rectClip.top, rectClip.right - rectClip.left, rectClip.bottom - rectClip.top);
	m_ctxt->clip();

	SetCairoColor(m_ctxt, &m_backgroundColor);
	m_ctxt->rectangle(xLeft, yTop, xRight - xLeft, yBottom - yTop);
	m_ctxt->fill();


	// Undo the cairo clipping region.
	m_ctxt->reset_clip();

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a line
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::DrawLine(int xStart, int yStart, int xEnd, int yEnd)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "DrawLine %p %d %d %d %d\n", this, xStart, yStart, xEnd, yEnd);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	// Trivially exit if the color is set to transparent
	if(m_foregroundColor.m_transparent) return S_OK;
	CheckDc();

	// See if the line to be rendered is out of bounds
	RECT rectClip;
	MyGetClipRect(&rectClip);

	if (yStart < rectClip.top && yEnd < rectClip.top)
		return S_OK;
	if (yStart > rectClip.bottom && yEnd > rectClip.bottom)
		return S_OK;

	SetCairoColor(m_ctxt, &m_foregroundColor);

	// Set thickness to around 1px, taken from Windows code
	int thickness = (GetXInch() + GetYInch()) / (96 * 2);
	m_ctxt->set_line_width(thickness);

	// Have to add 0.5 to prevent Cario from doing unnecessary anti-aliasing
	m_ctxt->move_to(xStart + .5, yStart + .5);
	m_ctxt->line_to(xEnd + .5, yEnd + .5);
	m_ctxt->stroke();

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a horizontal line that may be dotted or dashed.
	Consider first the behavior if *pdxStart is 0.
	If cdx is 0 or, equivalently, cdx is 1 and *prgdx is MAXINT, it draws a regular line
	line DrawLine from (xLeft, y) to (xRight, y).
	If there are multiple dx values, the line is dashed. A segment of length prgdx[0] is
	first drawn, then a gap of length prgdx[1] is left, then a segment of length prgdx[2],
	and so forth until the specified width is reached (the last segment, if drawn, is
	truncated to the specified length).
	If all values in prgdx are used up, we start again at prgdx[0]. An odd number of segments
	means that on and off alternate; thus, for example, a single width may be used to create
	a dashed line with equal gaps and dashes.
	The function then sets *pdxStart to the distance from xRight to the start of the last
	place where prgdx[0] was drawn (not where it was skipped, if there is an odd number).
	If *pdxStart is not initially zero, it gives a distance to skip through prgdx before
	starting. The normal use is to pass the value returned from a previous call, which
	can be used to draw several adjacent segments and have them seem continuous. You can also
	leave a gap in a dashed line by adding its width to pdxStart.
	(Another good way to use pdxStart is to set it to xLeft. This causes all patterns
	to be aligned, as if they were segments of one continuous pattern from the left margin.)
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::DrawHorzLine(int xLeft, int xRight, int y, int dyHeight,
	int cdx, int * prgdx, int * pdxStart)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "DrawHorzLine %p %d %d %d %d %d\n", this, xLeft, xRight, y, dyHeight, cdx);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	// Trivially exit if the color is set to transparent
	if(m_foregroundColor.m_transparent) return S_OK;

	// See if the line to be rendered is out of bounds
	RECT rectClip;
	MyGetClipRect(&rectClip);

	if (y + dyHeight < rectClip.top || y - dyHeight > rectClip.bottom)
		return S_OK;

	CheckDc();
	//PenWrap xpwr(PS_SOLID, dyHeight, m_rgbForeColor, m_hdc);
	//Cairo::Context ctxt(m_cr);

	SetCairoColor(m_ctxt, &m_foregroundColor);
	m_ctxt->set_line_width(dyHeight);

	int * pdxLim = prgdx + cdx;
	// Compute total length of pattern
	int dxPattern = 0;
	int * pdx = prgdx;
	for (; pdx < pdxLim; pdx++)
		dxPattern += *pdx;
	int dxStartOffset = *pdxStart - (*pdxStart / dxPattern) * dxPattern;
	// Now do the drawing...
	int xRightSeg;
	pdx = prgdx;
	// The starting point stays here until we get to the first segment we actually
	// want to draw.
	m_ctxt->move_to(xLeft + .5, y + .5);
	bool fDraw = false;
	for (int x = xLeft - dxStartOffset; x < xRight; x = xRightSeg)
	{
		// Figure the end of the current segment, or the end of the whole line,
		// whichever is smaller.
		xRightSeg = std::min(x + *pdx, xRight);
		// Advance to next segment (circularly)
		if (++pdx >= pdxLim)
		{
			*pdxStart = xLeft;
			pdx = prgdx;
		}
		// Alternate drawing segments and moving past them
		fDraw = !fDraw;
		// If we're in the range we want to draw...
		if (xRightSeg > xLeft)
		{
			if (fDraw)
			{
				m_ctxt->line_to(xRightSeg + .5, y + .5);
				m_ctxt->stroke();
			}
			else
			{
				m_ctxt->move_to(xRightSeg, y + .5);
			}
		}

	}

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a piece of text (below and to the right of the specified point)
	OPTIMIZE: for performance, should we make the caller responsible to ensure that only text
	reasonably near the clipping box gets drawn?
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::DrawText(int x, int y, int cch, const OLECHAR * prgch, int unused)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		UnicodeString8 text(prgch, (int)cch);
		char * strWithoutQuotes = strdup(text.c_str());
		for(char * p = strWithoutQuotes; *p != '\0'; p++)
			if (*p == '\"')
				*p = '\'';
		fprintf(m_loggingFile, "DrawText %p %d %d %d \"%s\" %d\n", this, x, y, cch, strWithoutQuotes, unused);
		fflush(m_loggingFile);
		free(strWithoutQuotes);
	}
#endif
 BEGIN_COM_METHOD;
	RECT rcClip;
	MyGetClipRect(&rcClip);
	// First, see if the text to be drawn is above or below the current clipping rectangle
	if(y > rcClip.bottom || y + (m_chrp.dympHeight * GetYInch() / kdzmpInch) < rcClip.top) {
		return S_OK;
	}

	CheckDc();

	int pangoX, pangoY; // store the offset that pango uses to draw text.
	int ascent;
	int descent;
	FontAscentAndDescent(&ascent, &descent);
	int fontHeight = ascent + descent;

	UnicodeString8 text(prgch, (int)cch);

	// Only draw text thats in the clipping region, by setting a cairo clipping region.
	m_ctxt->reset_clip();
	m_ctxt->rectangle(rcClip.left, rcClip.top, rcClip.right - rcClip.left, rcClip.bottom - rcClip.top);
	m_ctxt->clip();

	// Draw background if required

	if(!m_textBackColor.m_transparent) {
		SetCairoColor(m_ctxt, &m_textBackColor);
		int width, height, xoff, yoff;

		GetTextExtentHelper(cch, prgch, &pangoX, &pangoY, &width, &height);

		xoff = x;
		yoff = y;

		m_ctxt->rectangle(x, y, width, height);

		m_ctxt->fill();
	}

	if(!m_textColor.m_transparent)
	{
		// don't call g_object_unref on the returned layout
		PangoLayout *layout = GetPangoLayoutHelper();

		pango_layout_set_font_description (layout, m_pangoFontDescription);
		pango_layout_set_text (layout, text.data(), text.size());

		SetCairoColor(m_ctxt, &m_textColor);
		m_ctxt->move_to(x, y);

		pango_cairo_show_layout (m_ctxt.operator ->()->cobj(), layout);
	}

	// Undo the cairo clipping region.
	m_ctxt->reset_clip();

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Helper method that gets x and y offsets as well as width and height of the Text Extent.
----------------------------------------------------------------------------------------------*/
bool VwGraphicsCairo::GetTextExtentHelper(int cch, const OLECHAR * prgch, int * x, int * y, int * pdx, int * pdy)
{

	// don't call g_object_unref on the returned layout
	PangoLayout *layout = GetPangoLayoutHelper();
	pango_layout_set_font_description (layout, m_pangoFontDescription);

	UnicodeString8 text(prgch, (int)cch);

	pango_layout_set_text (layout, text.data(), text.size());
	PangoRectangle logical_rect;
	pango_layout_get_pixel_extents(layout, NULL, &logical_rect);

	*x = logical_rect.x;
	*y = logical_rect.y;
	*pdx = logical_rect.width;
	*pdy = logical_rect.height;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the Font Line Height for the current font.
----------------------------------------------------------------------------------------------*/
int VwGraphicsCairo::GetFontLineHeight()
{
	int ascent, descent;
	FontAscentAndDescent(&ascent, &descent);

	if ((ascent + descent) <= 0)
	{
		// The only reason this should happen is if the Font isn't set yet.
		// I expect this only to happen in unittests.
		// If the following asserts then a font is reporting 0 ascent + descent.
		Assert(m_pangoFontDescription == NULL);
		return 20;
	}

	return ascent + descent;
}

/*----------------------------------------------------------------------------------------------
	Measure the given text.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::GetTextExtent(int cch, const OLECHAR * prgch, int * pdx, int * pdy)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		UnicodeString8 text(prgch, (int)cch);
		fprintf(m_loggingFile, "GetTextExtent %p %d \"%s\"\n", this, cch, text.c_str());
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	CheckDc();

	int x,y;
	if (cch)
	{
		GetTextExtentHelper(cch, prgch, &x,&y, pdx, pdy);
	}
	else
	{
		*pdx = 0;
		*pdy = GetFontLineHeight();
	}

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get a rectangle that bounds the area to be drawn. (Some further parts of it may be clipped)
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::GetClipRect(int * pxLeft, int * pyTop, int * pxRight, int * pyBottom)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "GetClipRect %p\n", this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	RECT rcClip;
	MyGetClipRect(&rcClip);

	*pxLeft   = rcClip.left;
	*pxRight  = rcClip.right;
	*pyTop    = rcClip.top;
	*pyBottom = rcClip.bottom;

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get Font Ascent and Descent in one go more efficent that making two calls.
----------------------------------------------------------------------------------------------*/
bool VwGraphicsCairo::FontAscentAndDescent(int * ascent, int * descent)
{
	// cache the PangoFont and PangoFontMetricsfont to the m_pangoFontDescription
	if (m_ascent == -1 && m_descent == -1)
	{
		if (m_fontMapForFontContext == NULL)
			m_fontMapForFontContext = pango_cairo_font_map_get_default();

		if (m_fontContext == NULL)
			m_fontContext = pango_font_map_create_context (m_fontMapForFontContext);

		PangoFont * font = pango_context_load_font(m_fontContext, m_pangoFontDescription);

		// TODO-Linux: should we specify a language - for the font?
		PangoFontMetrics * metrics =  pango_font_get_metrics (font, NULL);

		m_ascent = pango_font_metrics_get_ascent (metrics) / PANGO_SCALE;
		m_descent = pango_font_metrics_get_descent (metrics) / PANGO_SCALE;

		pango_font_metrics_unref(metrics);
		g_object_unref(font);
	}

	if (ascent != NULL)
		*ascent = m_ascent;

	if (descent != NULL)
		*descent = m_descent;




	return true;
}

/*----------------------------------------------------------------------------------------------
	Get the ascent of the currently selected font
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::get_FontAscent(int * pdy)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "get_FontAscent %p\n",  this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	CheckDc();

	FontAscentAndDescent(pdy, NULL);

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get the descent of the currently selected font, in logical units.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::get_FontDescent(int * pdyRet)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "get_FontDescent %p\n", this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	CheckDc();

	FontAscentAndDescent(NULL, pdyRet);

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get the descent of the currently selected font, in logical units.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::get_FontCharProperties(VwCharRenderProps * pchrp)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "get_FontCharProperties %p\n", this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	CheckDc();
	memcpy(pchrp, &m_chrp, sizeof(*pchrp));

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Release the DC and set pointer to NULL.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::ReleaseDC()
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "ReleaseDC %p\n", this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	if (m_pangoFontDescription != NULL)
		pango_font_description_free (m_pangoFontDescription);

	if (m_fontContext != NULL)
		g_object_unref(m_fontContext);

	if (m_context != NULL)
		g_object_unref(m_context);

	if (m_layout != NULL)
		g_object_unref (m_layout);

	m_pangoFontDescription = NULL;
	m_fontMapForFontContext = NULL;
	m_fontContext = NULL;
	m_fontMap = NULL;
	m_context = NULL;
	m_layout = NULL;

	m_ctxt = Cairo::RefPtr<Cairo::Context>(0);
	m_enabled = 0;
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get current actual or simulated X resolution
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::get_XUnitsPerInch(int * pxInch)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "get_XUnitsPerInch %p\n", this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	*pxInch = GetXInch();
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Set (simulated) X resolution. Setting to zero restores actual device resolution.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::put_XUnitsPerInch(int xInch)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "put_XUnitsPerInch %p %d\n", this, xInch);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	m_xInch = xInch;
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Get current actual or simulated Y resolution
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::get_YUnitsPerInch(int * pyInch)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "get_YUnitsPerInch %p\n", this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	*pyInch = GetYInch();
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Set (simulated) Y resolution. Setting to zero restores actual device resolution.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::put_YUnitsPerInch(int yInch)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "put_YUnitsPerInch %p %d\n", this, yInch);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	m_yInch = yInch;
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

PangoLayout * VwGraphicsCairo::GetPangoLayoutHelper()
{
	if (m_fontMap == NULL)
		m_fontMap = pango_cairo_font_map_get_default();

	if (m_context == NULL)
	{	m_context = pango_context_new();
		pango_context_set_font_map(m_context, m_fontMap);
	}

	if (m_layout == NULL)
	{
		m_layout = pango_layout_new(m_context);

		PangoAttrList* list = pango_attr_list_new();
		PangoAttribute * fallbackAttrib = pango_attr_fallback_new(true);
		pango_attr_list_insert(list, fallbackAttrib);
		pango_layout_set_attributes(m_layout, list);
		pango_attr_list_unref(list);

		pango_layout_set_single_paragraph_mode(m_layout, true);
	}

	return m_layout;
}

/*----------------------------------------------------------------------------------------------
	Set up to draw text using the properties specified.
	super/subscript are ignored, as is baseline adjust; client is
	presumed to have handled them. Sets colors and HFONT.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::SetupGraphics(VwCharRenderProps * pchrp)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		UnicodeString8 szFaceName(pchrp->szFaceName, (int)32);
		UnicodeString8 szFontVar(pchrp->szFontVar, (int)64);
		fprintf(m_loggingFile, "SetupGraphics %p %u %u %u %d %d %d %d %d %d %d %d %d \"%s\" \"%s\"\n",
			this,
			pchrp->clrFore, pchrp->clrBack, pchrp->clrUnder, pchrp->dympOffset, pchrp->ws, pchrp->fWsRtl,
			pchrp->nDirDepth, pchrp->ssv, pchrp->unt, pchrp->ttvBold, pchrp->ttvItalic, pchrp->dympHeight,
			szFaceName.c_str(), szFontVar.c_str());
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	CheckDc();

	m_chrp = *pchrp;

	double fontSize = 0;

	// TODO-Linux: Work around for FWNX-179.
	// dympHeight should be in mp.
	const int tooSmall = 100; // A Converative hueristic of a small milli point value which means a invalid value has been specified.
	Assert(m_chrp.dympHeight >= tooSmall || m_chrp.dympHeight == 0);
	if (m_chrp.dympHeight < tooSmall)
	{
		fontSize = m_chrp.dympHeight;
	}
	else
	{
		fontSize = m_chrp.dympHeight * GetYInch() / kdzmpInch;
	}

	UnicodeString8 fontNameUtf8(pchrp->szFaceName);
	const char* fontName = fontNameUtf8.c_str();

	if (pchrp->clrFore == kclrTransparent) {
		VwColor newCol;
		newCol.m_transparent = true;
		m_textColor = newCol;
	}
	else {
		VwColor newCol(pchrp->clrFore);
		m_textColor = newCol;
	}

	if (pchrp->clrBack == kclrTransparent) {
		VwColor newCol;
		newCol.m_transparent = true;
		m_textBackColor = newCol;
	}
	else {
		VwColor newCol(pchrp->clrBack);
		m_textBackColor = newCol;
	}

	m_pangoFontDescription = pango_font_description_new();
	m_ascent = -1;
	m_descent = -1;
	pango_font_description_set_family(m_pangoFontDescription, fontName);
	pango_font_description_set_weight(m_pangoFontDescription, m_chrp.ttvBold == kttvOff ? PANGO_WEIGHT_NORMAL : PANGO_WEIGHT_BOLD);
	pango_font_description_set_style(m_pangoFontDescription, m_chrp.ttvItalic == kttvOff ? PANGO_STYLE_NORMAL : PANGO_STYLE_ITALIC);
	pango_font_description_set_absolute_size(m_pangoFontDescription, fontSize * PANGO_SCALE);

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Invoke a new clipping rectangle; the previous clipping state can be
	restored using PopClipRect.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::PushClipRect(RECT rcClip)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "PushClipRect %p %d %d %d %d\n", this, rcClip.left, rcClip.top, rcClip.right, rcClip.bottom);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;

	int stackLength = m_vrectClipStack.size();
	if(stackLength >= 1) {
		RECT previous = m_vrectClipStack[stackLength - 1];
		rcClip.left = max(rcClip.left, previous.left);
		rcClip.top = max(rcClip.top, previous.top);
		rcClip.bottom = min(rcClip.bottom, previous.bottom);
		rcClip.right = min(rcClip.right, previous.right);
	}

	m_vrectClipStack.push_back(rcClip);

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Restore the previous clipping rectangle.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::PopClipRect()
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "PopClipRect %p\n", this);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	// Needs to be at least two clipping rects on the stack
	// (Current one and one to be restored)
	int stackLength = m_vrectClipStack.size();
	if(stackLength >= 1) {
		m_vrectClipStack.pop_back();
	}

	RECT rcClip = m_vrectClipStack[stackLength - 2];

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Draw a polygon.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::DrawPolygon(int cVertices, POINT prgvpnt[])
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "DrawPolygon %p %d", this, cVertices);
		for (int i = 0; i < cVertices; i++)
		{
			fprintf(m_loggingFile, "%d %d", prgvpnt[i].x, prgvpnt[i].y);
		}

		fprintf(m_loggingFile, "\n");
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	// Trivially exit if the color is set to transparent
	if(m_backgroundColor.m_transparent) return S_OK;

	RECT rectBounds;
	rectBounds.left = prgvpnt[0].x;
	rectBounds.top = prgvpnt[0].y;
	rectBounds.right = rectBounds.left;
	rectBounds.bottom = rectBounds.top;

	// determine a bounding rectangle
	for (int i = 0; i < cVertices; i++)
	{
		int x = prgvpnt[i].x;
		int y = prgvpnt[i].y;
		if (x < rectBounds.left)
			rectBounds.left = x;
		if (x > rectBounds.right)
			rectBounds.right = x;
		if (y < rectBounds.top)
			rectBounds.top = y;
		if (y > rectBounds.bottom)
			rectBounds.bottom = y;
	}

	// check whether the bounding rectangle is visible
	RECT rectClip;
	MyGetClipRect(&rectClip);
	if(!rectIntersect(&rectClip, &rectBounds)) {
		return S_OK;
	}

	// Solid, in background color
	CheckDc();
	//Cairo::Context ctxt(m_cr);

	SetCairoColor(m_ctxt, &m_backgroundColor);
	m_ctxt->move_to(prgvpnt[cVertices - 1].x, prgvpnt[cVertices - 1].y);

	for ( int i = 0; i < cVertices; i++) {
	  m_ctxt->line_to(prgvpnt[i].x, prgvpnt[i].y);
	}

	m_ctxt->fill();

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Render an IPicture using its Render method
	@param ppic The picture to render
	@param x Horizontal position of image in hdc
	@param y Vertical position of image in hdc
	@param cx Horizontal dimension of destination rectangle
	@param cy Vertical dimension of destination rectangle
	@param xSrc Horizontal offset in source picture
	@param ySrc Vertical offset in source picture
	@param cxSrc Amount to copy horizontally in source picture
	@param cySrc Amount to copy vertically in source picture
	@param prcWBounds Pointer to position of destination for a metafile hdc (Not currently used on Linux)
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::RenderPicture(IPicture * ppic, int x, int y, int cx, int cy,
	OLE_XPOS_HIMETRIC xSrc, OLE_YPOS_HIMETRIC ySrc,
	OLE_XSIZE_HIMETRIC cxSrc, OLE_YSIZE_HIMETRIC cySrc,
	LPCRECT prcWBounds)
{
#if DEBUG
	// TODO: represent Picture some how.
#endif
 BEGIN_COM_METHOD;
	/* TODO:
	 * Use all the co-ordinates (yup, all 12)
	 */
	CheckDc();
	//Cairo::Context ctxt(m_cr);

	// Exit if picture is outside of clipping rectangle
	RECT rectClip;
	MyGetClipRect(&rectClip);
	RECT rectDraw;
	rectDraw.left = x;
	rectDraw.right = x + cx;
	rectDraw.top = y;
	rectDraw.bottom = y + cy;
	if(!rectIntersect(&rectClip, &rectDraw)) {
		return S_OK;
	}

	ppic->Render(m_hdc, x, y, cx, cy, xSrc, ySrc, cxSrc, cySrc, prcWBounds);


 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/*----------------------------------------------------------------------------------------------
	Make an IPicture object from binary data.
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::MakePicture(byte * pbData, int cbData, IPicture ** pppic)
{
 BEGIN_COM_METHOD;

	IPictureFactoryPtr m_qpf;
	m_qpf.CreateInstance(CLSID_PictureFactory);
	CheckHr(m_qpf->ImageFromBytes(pbData, cbData, pppic));

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/***********************************************************************************************
	IVwGraphics methods
***********************************************************************************************/

/*----------------------------------------------------------------------------------------------
	Set a clip rectangle, if it is not workable to read it from the HDC (e.g., because
	it is a metafile).
----------------------------------------------------------------------------------------------*/
HRESULT VwGraphicsCairo::SetClipRect(RECT * prcClip)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		fprintf(m_loggingFile, "SetClipRect %p %d %d %d %d\n", this, prcClip->left, prcClip->top, prcClip->right, prcClip->bottom);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;
	CheckDc();

	m_rcClip = *prcClip;

 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

HRESULT VwGraphicsCairo::GetTextStyleContext(HDC * pContext)
{
	BEGIN_COM_METHOD;

	if (m_context == NULL)
		GetPangoLayoutHelper();

	// if a font decription has been set ensure its associated with m_context.
	if (m_pangoFontDescription != NULL)
		pango_context_set_font_description(m_context, m_pangoFontDescription);

	*pContext = reinterpret_cast<HDC>(m_context);

	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

/***********************************************************************************************
	Utility methods
***********************************************************************************************/

int VwGraphicsCairo::GetXInch()
{
	if (m_xInch == 0) {
		return GetDpiX(m_hdc);
	}
	else {
		return m_xInch;
	}
}

int VwGraphicsCairo::GetYInch()
{
	if (m_yInch == 0) {
		return GetDpiY(m_hdc);
	}
	else {
		return m_yInch;
	}
}

/*----------------------------------------------------------------------------------------------
	Everything that needs an HDC calls this to check it is present. For now, if it isn't, we
	consider it an internal error on the part of the client.
----------------------------------------------------------------------------------------------*/
void VwGraphicsCairo::CheckDc()
{
	// First graphics operation going missing?
	// Cairo needs to cause error before status is changed

	// TODO-Linux: Implement Sensible DC checking here.
}

/*----------------------------------------------------------------------------------------------
	Everything that needs an HDC calls this to check it is present. For now, if it isn't, we
	consider it an internal error on the part of the client.
----------------------------------------------------------------------------------------------*/
void VwGraphicsCairo::MyGetClipRect(RECT * prc)
{
	// if SetClipRect has been called explicitly just call that
	if (!(	m_rcClip.left == 0 &&
			m_rcClip.right == 0 &&
			m_rcClip.top == 0 &&
			m_rcClip.bottom == 0))
	{
		*prc = m_rcClip;
		return;
	}
	int stackSize = m_vrectClipStack.size();
	if (stackSize > 0) {
		*prc = m_vrectClipStack[stackSize - 1];
	}
	else {
			// No cliping rect set return whole region.

			if (m_hdc != NULL)
			{
				gdiplus::Graphics* graphics = (reinterpret_cast<gdiplus::Graphics*>(m_hdc));
				prc->left = graphics->bounds.X;
				prc->top = graphics->bounds.Y;
				prc->right = graphics->bounds.X + graphics->bounds.Width;
				prc->bottom = graphics->bounds.Y + graphics->bounds.Height;
			}
			else
			{
				prc->left = 0;
				prc->top = 0;
				prc->right = DEFAULT_WIDTH;
				prc->bottom = DEFAULT_HEIGHT;
			}
	}
}

bool VwGraphicsCairo::rectIntersect(RECT* a, RECT* b) {
	if( a->bottom < b->top || a->top > b->bottom ||
		a->right < b->left || a->left > b->right) {
		return false;
	}
	else {
		return true;
	}
}

// Unused in FieldWorks
HRESULT VwGraphicsCairo::DrawTextExt(int, int, int, const OLECHAR *, UINT, const RECT *, int *)
{
	BEGIN_COM_METHOD;
		ThrowHr(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_IVwGraphics);
}

HRESULT VwGraphicsCairo::GetTextLeadWidth(int cch, const OLECHAR * prgch, int ich, int dxStretch,
		int * pdx)
{
#if DEBUG
	if (m_loggingFile != NULL)
	{
		UnicodeString8 text(prgch, (int)cch);
		fprintf(m_loggingFile, "GetTextLeadWidth %p %d \"%s\" %d %d\n", this, cch, text.c_str(), ich, dxStretch);
		fflush(m_loggingFile);
	}
#endif
 BEGIN_COM_METHOD;

	int x = 0, y = 0;
	GetTextExtent(ich, prgch, &x, &y);
	if (dxStretch)
	{
		// TODO-Linux: Review - get the break char from the font.
		//
		const OLECHAR BreakChar = ' ';
		double cbrk = 0;
		double cbrkPrev = 0;
		const OLECHAR * pch = prgch;
		for (int i = 0; i < cch; i++, pch++)
		{

			if (*pch == BreakChar)
			{
				cbrk++;
				if (i < ich)
					cbrkPrev++;
			}
		}
		if (!cbrkPrev)
		{
			// justification can't alter things
			*pdx = x;
			return S_OK;
		}
		int dxStetchPrev = ((double)dxStretch) * cbrkPrev / cbrk; // even distribution of extra space
		*pdx = x + dxStetchPrev;
		return S_OK;
	}
	else
	{
		*pdx = x;
		return S_OK;
	}

	return S_OK;
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

HRESULT VwGraphicsCairo::GetFontEmSquare(int * pxyFontEmSquare)
{
 BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}
HRESULT VwGraphicsCairo::GetGlyphMetrics(int chw,
	int * psBoundingWidth, int * pyBoundingHeight,
	int * pxBoundingX, int * pyBoundingY, int * pxAdvanceX, int * pyAdvanceY)
{
 BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}
HRESULT VwGraphicsCairo::GetFontData(int nTableId, int * pcbTableSz, BSTR * pbstrTableData)
{
 BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}
HRESULT VwGraphicsCairo::GetFontDataRgch(int nTableId, int * pcbTableSz, OLECHAR * prgch, int cchMax)
{
 BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}
HRESULT VwGraphicsCairo::XYFromGlyphPoint(int chw, int nPoint, int * pxRet, int * pyRet)
{
 BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
 END_COM_METHOD(g_fact, IID_IVwGraphics);
}

HRESULT VwGraphicsCairo::GetSuperscriptHeightRatio(int* piNumerator, int* piDenominator)
{
BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
END_COM_METHOD(g_fact, IID_IVwGraphics);
}

HRESULT VwGraphicsCairo::GetSuperscriptYOffsetRatio(int* piNumerator, int* piDenominator)
{
BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
END_COM_METHOD(g_fact, IID_IVwGraphics);
}

HRESULT VwGraphicsCairo::GetSubscriptHeightRatio(int* piNumerator, int* piDenominator)
{
BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
END_COM_METHOD(g_fact, IID_IVwGraphics);
}

HRESULT VwGraphicsCairo::GetSubscriptYOffsetRatio(int* piNumerator, int* piDenominator)
{
BEGIN_COM_METHOD;
	ThrowHr(E_NOTIMPL);
END_COM_METHOD(g_fact, IID_IVwGraphics);
}
