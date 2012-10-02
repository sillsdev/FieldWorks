/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: GrSegmentPlatform.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Platform-specific data structures for GrSegment.

	On Windows, these data structures are organized to match the ExtTextOutW API function.
	Specifically, we organize the glyphs into groups by their y-offset and color, and then
	order each group by x-offset.

OBSOLETE - any platform-specific behavior now belongs in a subclass of SegmentPainter.
----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GR_SEGPLATFORM_INCLUDED
#define GR_SEGPLATFORM_INCLUDED

//:End Ignore

// Intended to be #included inside GrSegment.h

protected:
	//	Platform-specific member variables:

	//	The y-offset and color are what identifies each glyph stream.
	struct GlyphStrmKey		// hungarian gsk
	{
		float ys;			// distance above the baseline
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
		std::vector<int> vdxs;
		std::vector<int> vigbb;
		float xsStart;
		int xsStartInt;	// optimization

		GlyphStrm() : xsStart(-1)	// constructor
		{ };
	};
	GlyphStrm * m_prggstrm;
	int m_cgstrm;

	//	Data for a single glyph:
	struct GlyphBb {		// hungarian: gbb
		int igstrm;	// which glyph stream it has been assigned to
		int iGlyph;	// which glyph in the stream
		int islout; // slot index
		float xsBbLeft;
		float ysBbTop;
		float xsBbRight;
		float ysBbBottom;
		float xsPosLeft;	// x-position
		float xsPosRight;	// x-position + advance width
//		int clrFore;
//		int clrBack;
//		bool fUnderline;
	};
	GlyphBb * m_prggbb;
	int m_cgbb;

	static float GlyphKeySort(const void *, const void *);
	static int GlyphBbSort(const void *, const void *);

	float LeftEdge()
	{
		return m_prggbb[0].xsBbLeft;
	}



#endif  // !GR_SEGPLATFORM_INCLUDED
