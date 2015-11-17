/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GraphiteSegment.h
Responsibility: Damien Daspit
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef GRAPHITESEGMENT_INCLUDED
#define GRAPHITESEGMENT_INCLUDED

ATTACH_GUID_TO_CLASS(class, CFB69FDC-8C5F-4D3E-836C-4BA4F5D9769B, GraphiteSegment);
// Trick GUID for getting the actual implementation of the GraphiteSegment object.
#define CLSID_GraphiteSegment __uuidof(GraphiteSegment)

#include <graphite2/Segment.h>

/*----------------------------------------------------------------------------------------------
Class: GraphiteSegment
Description:
Hungarian: rrs
----------------------------------------------------------------------------------------------*/
class GraphiteSegment : public ILgSegment
{
	friend class GraphiteEngine;
public:
	// Constructors/destructors/etc.
	GraphiteSegment();
	GraphiteSegment(IVwTextSource* pts, GraphiteEngine* pgre, int ichMin, int ichLim,
		int dirDepth, bool paraRtl, bool wsOnly);
	virtual ~GraphiteSegment();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ILgSegment methods
	STDMETHOD(DrawText)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		int * dxdWidth);
	STDMETHOD(Recompute)(int ichBase, IVwGraphics* pvg);
	STDMETHOD(get_Width)(int ichBase, IVwGraphics* pvg, int* px);
	STDMETHOD(get_RightOverhang)(int ichBase, IVwGraphics* pvg, int* px);
	STDMETHOD(get_LeftOverhang)(int ichBase, IVwGraphics* pvg, int* px);
	STDMETHOD(get_Height)(int ichBase, IVwGraphics* pvg, int* py);
	STDMETHOD(get_Ascent)(int ichBase, IVwGraphics* pvg, int* py);
	STDMETHOD(Extent)(int ichBase, IVwGraphics* pvg, int* px, int* py);
	STDMETHOD(BoundingRect)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		RECT* prcBounds);
	STDMETHOD(GetActualWidth)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		int* dxdWidth);
	STDMETHOD(get_AscentOverhang)(int ichBase, IVwGraphics* pvg, int* py);
	STDMETHOD(get_DescentOverhang)(int ichBase, IVwGraphics* pvg, int* py);
	STDMETHOD(get_RightToLeft)(int ichBase, ComBool* pfResult);
	STDMETHOD(get_DirectionDepth)(int ichBase, int* pnDepth, ComBool* pfWeak);
	STDMETHOD(SetDirectionDepth)(int ichBase, int nNewDepth);
	STDMETHOD(get_WritingSystem)(int ichBase, int* pws);
	STDMETHOD(get_Lim)(int ichBase, int* pdich);
	STDMETHOD(get_LimInterest)(int ichBase, int* pdich);
	STDMETHOD(put_EndLine)(int ichBase, IVwGraphics* pvg, ComBool fNewVal);
	STDMETHOD(put_StartLine)(int ichBase, IVwGraphics* pvg, ComBool fNewVal);
	STDMETHOD(get_StartBreakWeight)(int ichBase, IVwGraphics* pvg, LgLineBreak* pnTwips);
	STDMETHOD(get_EndBreakWeight)(int ichBase, IVwGraphics* pvg, LgLineBreak* pnTwips);
	STDMETHOD(get_Stretch)(int ichBase, int* px);
	STDMETHOD(put_Stretch)(int ichBase, int x);
	STDMETHOD(IsValidInsertionPoint)(int ichBase, IVwGraphics* pvg, int ich,
		LgIpValidResult* pipvr);
	STDMETHOD(DoBoundariesCoincide)(int ichBase, IVwGraphics* pvg,
		ComBool fBoundaryEnd, ComBool fBoundaryRight, ComBool* pfResult);
	STDMETHOD(DrawInsertionPoint)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		int ich, ComBool fAssocPrev, ComBool fOn, LgIPDrawMode dm);
	STDMETHOD(PositionsOfIP)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		int ich, ComBool fAssocPrev, LgIPDrawMode dm,
		RECT* rectPrimary, RECT* rectSecondary,
		ComBool* pfPrimaryHere, ComBool* pfSecHere);
	STDMETHOD(DrawRange)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		int ichMin, int ichLim, int ydTop, int ydBottom, ComBool bOn,
		ComBool fIsLastLineOfSelection, RECT* prsBounds);
	STDMETHOD(PositionOfRange)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		int ichMin, int ichim, int ydTop, int ydBottom, ComBool fIsLastLineOfSelection,
		RECT* rsBounds, ComBool* pfAnythingToDraw);
	STDMETHOD(PointToChar)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst,
		POINT ptdClickPosition, int* pich, ComBool* pfAssocPrev);
	STDMETHOD(ArrowKeyPosition)(int ichBase, IVwGraphics* pvg, int* pich,
		ComBool* pfAssocPrev, ComBool fRight, ComBool fMovingIn, ComBool* pfResult);
	STDMETHOD(ExtendSelectionPosition)(int ichBase, IVwGraphics* pvg,
		int* pich, ComBool fAssocPrevMatch, ComBool fAssocPrevNeeded, int ichAnchor,
		ComBool fRight, ComBool fMovingIn, ComBool* pfRet);
	STDMETHOD(GetCharPlacement)(int ichBase, IVwGraphics* pvg, int ichMin, int ichLim,
		RECT rcSrc, RECT rcDst,
		ComBool fSkipSpace, int crgMax, int* pcxd, int* prgxdLefts, int* prgxdRights,
		int* prgydUnderTops);
	STDMETHOD(DrawTextNoBackground)(int ichBase, IVwGraphics* pvg, RECT rcSrc, RECT rcDst, int* dxdWidth);

protected:

	// This struct represents a cluster of glyphs that are never reordered or split.
	// Each cluster is associated with a sequence of characters from the original string.
	// A cursor can only occur between clusters.
	struct Cluster
	{
		// the offset in the text where this cluster begins
		int ichBase;
		// the number of characters in this cluster
		int length;
		// the index of the base glyph in m_glyphs
		int baseGlyph;
		// the number of glyphs in this cluster
		int glyphCount;
		// the x offset to the beginning of this cluster
		int beforeX;

		Cluster(int iIchBase)
			: ichBase(iIchBase), length(0), baseGlyph(0), glyphCount(0), beforeX(0)
		{
		}

		Cluster(int iIchBase, int iLength, int iBaseGlyph, int iGlyphCount, int iBeforeX)
			: ichBase(iIchBase), length(iLength), baseGlyph(iBaseGlyph), glyphCount(iGlyphCount), beforeX(iBeforeX)
		{
		}

		friend bool operator== (const Cluster& c1, const Cluster& c2)
		{
			return c1.ichBase == c2.ichBase;
		}

		friend bool operator!= (const Cluster& c1, const Cluster& c2)
		{
			return !(c1 == c2);
		}

		friend bool operator< (const Cluster& c1, const Cluster& c2)
		{
			return c1.ichBase < c2.ichBase;
		}
	};

	static int Round(const float n)
	{
		return int(n < 0 ? n - 0.5 : n + 0.5);
	}

	void InterpretChrp(LgCharRenderProps& chrp);
	void InitializeGlyphs(gr_segment* segment, gr_font* font);
	void Compute(int ichBase, IVwGraphics* pvg);
	bool CanDrawIP(int ich, ComBool fAssocPrev);
	bool CheckForOrcUs(int ich);
	int GetSelectionX(int ich, const Rect& srcRect, const Rect& dstRect);
	bool IsRtl()
	{
		return (m_dirDepth % 2) == 1;
	}

	// Member variables
	long m_cref;				// standard COM ref count
	IVwTextSourcePtr m_qts;		// the source of our text
	int m_ichMin;
	int m_ichLim;
	GraphiteEnginePtr m_qgre;
	bool m_wsOnly;
	int m_dirDepth;
	bool m_paraRtl;
	int m_stretch;

	int m_fontAscent;
	int m_fontDescent;
	int m_width;
	// the clusters in logical order
	vector<Cluster> m_clusters;
	// the glyphs in visual order
	vector<GlyphInfo> m_glyphs;
};
DEFINE_COM_PTR(GraphiteSegment);

#endif  //GRAPHITESEGMENT_INCLUDED
