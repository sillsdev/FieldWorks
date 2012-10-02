/*
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
 * OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 * Authors:
 *      Alexandre Pigolkine (pigolkine@gmx.de)
 *      Duncan Mak (duncan@ximian.com)
 *      Miguel de Icaza (miguel@ximian.com)
 *      Ravindra (rkumar@novell.com)
 *  	Sanjay Gupta (gsanjay@novell.com)
 *	Vladimir Vukicevic (vladimir@pobox.com)
 *	Geoff Norton (gnorton@customerdna.com)
 *      Jonathan Gilbert (logic@deltaq.org)
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2003-2007 Novell, Inc (http://www.novell.com)
 */

/* Code Taken from Mono project and placed in namespace gdiplus */

namespace gdiplus
{

#include <X11/Xatom.h>
#include <X11/Xlib.h>

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	CompositingModeSourceOver,
	CompositingModeSourceCopy
} CompositingMode;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	TextRenderingHintSystemDefault = 0,
	TextRenderingHintSingleBitPerPixelGridFit,
	TextRenderingHintSingleBitPerPixel,
	TextRenderingHintAntiAliasGridFit,
	TextRenderingHintAntiAlias,
	TextRenderingHintClearTypeGridFit
} TextRenderingHint;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	QualityModeInvalid	= -1,
	QualityModeDefault	= 0,
	QualityModeLow		= 1,
	QualityModeHigh		= 2
} QualityMode;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	SmoothingModeInvalid		= QualityModeInvalid,
	SmoothingModeDefault		= QualityModeDefault,
	SmoothingModeHighSpeed		= QualityModeLow,
	SmoothingModeHighQuality	= QualityModeHigh,
	SmoothingModeNone,
	SmoothingModeAntiAlias
} SmoothingMode;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	PixelOffsetModeInvalid		= QualityModeInvalid,
	PixelOffsetModeDefault		= QualityModeDefault,
	PixelOffsetModeHighSpeed	= QualityModeLow,
	PixelOffsetModeHighQuality	= QualityModeHigh,
	PixelOffsetModeNone,
	PixelOffsetModeHalf
} PixelOffsetMode;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	CompositingQualityInvalid	= QualityModeInvalid,
	CompositingQualityDefault	= QualityModeDefault,
	CompositingQualityHighSpeed	= QualityModeLow,
	CompositingQualityHighQuality	= QualityModeHigh,
	CompositingQualityGammaCorrected,
	CompositingQualityAssumeLinear
} CompositingQuality;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	InterpolationModeInvalid	= QualityModeInvalid,
	InterpolationModeDefault	= QualityModeDefault,
	InterpolationModeLowQuality	= QualityModeLow,
	InterpolationModeHighQuality	= QualityModeHigh,
	InterpolationModeBilinear,
	InterpolationModeBicubic,
	InterpolationModeNearestNeighbor,
	InterpolationModeHighQualityBilinear,
	InterpolationModeHighQualityBicubic
} InterpolationMode;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	UnitWorld	= 0,
	UnitDisplay	= 1,
	UnitPixel	= 2,
	UnitPoint	= 3,
	UnitInch	= 4,
	UnitDocument	= 5,
	UnitMillimeter	= 6,
	UnitCairoPoint	= 7
} Unit, GpUnit;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef struct {
	int X, Y, Width, Height;
} Rect, GpRect;

// takend from mono implementaiton of gdiplus gdiplus/src/..
typedef enum {
	GraphicsBackEndInvalid	= -1,
	GraphicsBackEndCairo	= 0,
	GraphicsBackEndMetafile	= 1
} GraphicsBackEnd;

// takend from mono implementaiton of gdiplus gdiplus/src/graphics-private.h (modified)
typedef struct _Graphics {
	GraphicsBackEnd		backend;
	/* cairo-specific stuff */
	cairo_t			*ct;
	void* /*GpMatrix*/		*copy_of_ctm;
	cairo_matrix_t		previous_matrix;
	Display			*display;
	Drawable		drawable;
	void			*image;
	int			type;
	void * /*GpPen**/			last_pen;	/* caching pen and brush to avoid unnecessary sets */
	void * /*GpBrush**/		last_brush;
	float			aa_offset_x;
	float			aa_offset_y;
	/* metafile-specific stuff */
	GraphicsBackEnd /*EmfType*/			emf_type;
	void */*GpMetafile		**/metafile;
	cairo_surface_t		*metasurface;	/* bogus surface to satisfy some API calls */
	/* common-stuff */
	void/*GpRegion*/*		clip;
	void/*GpMatrix*/*		clip_matrix;
	GpRect			bounds;
	GpUnit			page_unit;
	float			scale;
	InterpolationMode	interpolation;
	SmoothingMode		draw_mode;
	TextRenderingHint	text_mode;
	void /*GpState*/*		saved_status;
	int			saved_status_pos;
	CompositingMode		composite_mode;
	CompositingQuality	composite_quality;
	PixelOffsetMode		pixel_mode;
	int			render_origin_x;
	int			render_origin_y;
	float			dpi_x;
	float			dpi_y;
	int			text_contrast;
#ifdef CAIRO_HAS_QUARTZ_SURFACE
	void		*cg_context;
#endif
} Graphics;

} // end namespace gdiplus
