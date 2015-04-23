/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwGrSegment.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:
	Defines the class for a Graphite text segment to be used within FieldWorks.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef FWGRSEGMENT_INCLUDED
#define FWGRSEGMENT_INCLUDED

//:End Ignore

ATTACH_GUID_TO_CLASS(class, F0C462A2-3258-11d4-9273-00400543A57C, FwGrSegment);
// Trick GUID for getting the actual implementation of the FwGrSegment object.
#define CLSID_FwGrSegment __uuidof(FwGrSegment)

//static bool g_fDrawing;

/*----------------------------------------------------------------------------------------------
	A Graphite segment consists of a sequence of well-positioned glyphs all on one line,
	Each glyph understands its relationship to the underlying text that was used to generate
	the segment.

	Hungarian: seg
----------------------------------------------------------------------------------------------*/
class FwGrSegment : public ILgSegment
{
	//friend class gr::GrEngine;
public:
	// Static methods

	// Constructors/destructors/etc.
	FwGrSegment();
	virtual ~FwGrSegment();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		AssertPtr(this);
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		AssertPtr(this);
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ILgSegment methods
	STDMETHOD(DrawText)(int ichwBase, IVwGraphics * pvg,
		RECT rcSrc, RECT rcDst, int * pdxdWidth);

	STDMETHOD(Recompute)(int ichwBase, IVwGraphics * pvg);
	STDMETHOD(get_Width)(int ichwBase, IVwGraphics * pvg, int * pxs);
	STDMETHOD(get_RightOverhang)(int ichwBase, IVwGraphics * pvg, int * pxs);
	STDMETHOD(get_LeftOverhang)(int ichwBase, IVwGraphics * pvg, int * pxs);
	STDMETHOD(get_Height)(int ichwBase, IVwGraphics * pvg, int * pys);
	STDMETHOD(get_Ascent)(int ichwBase, IVwGraphics * pvg, int * pys);
	STDMETHOD(Extent)(int ichwBase, IVwGraphics * pvg, int * pdxsWidth, int * pdysHt);
	STDMETHOD(BoundingRect)(int ichwBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		RECT * prcBounds);
	STDMETHOD(GetActualWidth)(int ichwBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		int * pdxdWidth);
	STDMETHOD(get_AscentOverhang)(int ichwBase, IVwGraphics * pvg, int * pys);
	STDMETHOD(get_DescentOverhang)(int ichwBase, IVwGraphics * pvg, int * pys);

	STDMETHOD(get_RightToLeft)(int ichwBase, ComBool * pfResult);
	STDMETHOD(get_DirectionDepth)(int ichwBase, int * pnDepth, ComBool * pfWeak);
	STDMETHOD(SetDirectionDepth)(int ichwBase, int nNewDepth);
	STDMETHOD(get_WritingSystem)(int ichwBase, int * pws);

	STDMETHOD(get_Lim)(int ichwBase, int * pdichw);
	STDMETHOD(get_LimInterest)(int ichwBase, int * pdichw);

	STDMETHOD(put_EndLine)(int ichwBase, IVwGraphics * pvg, ComBool fNewVal);
	STDMETHOD(put_StartLine)(int ichwBase, IVwGraphics * pvg, ComBool fNewVal);
	STDMETHOD(get_StartBreakWeight)(int ichwBase, IVwGraphics * pvg, LgLineBreak * plb);
	STDMETHOD(get_EndBreakWeight)(int ichwBase, IVwGraphics * pvg, LgLineBreak * plb);
	STDMETHOD(get_Stretch)(int ichwBase, int * pxs);
	STDMETHOD(put_Stretch)(int ichwBase, int xs);

	STDMETHOD(IsValidInsertionPoint)(int ichwBase, IVwGraphics * pvg,
		int ichwIP, LgIpValidResult * pipvr);
	STDMETHOD(DoBoundariesCoincide)(int ichwBase, IVwGraphics * pvg,
		ComBool fBoundaryEnd, ComBool fBoundaryRight,
		ComBool * pfResult);
	STDMETHOD(DrawInsertionPoint)(int ichwBase, IVwGraphics * pvg,
		RECT rcSrc, RECT rcDst, int ichwIP, ComBool fAssocPrev,
		ComBool bOn, LgIPDrawMode dm);
	STDMETHOD(PositionsOfIP)(int ichwBase, IVwGraphics * pvg,
		RECT rs, RECT rd, int ichwIP, ComBool fAssocPrev, LgIPDrawMode dm,
		RECT * prdPrimary, RECT * prdSecondary,
		ComBool * pfPrimaryHere, ComBool * pfSecHere);
	STDMETHOD(DrawRange)(int ichwBase, IVwGraphics * pvg,
		RECT rs, RECT rd,
		int ichwMin, int ichwLim,
		int ydTop, int ydBottom,
		ComBool bOn, ComBool fIsLastLineOfSelection, RECT * prsBounds);
	STDMETHOD(PositionOfRange)(int ichwBase, IVwGraphics* pvg,
		RECT rs, RECT rd,
		int ichwMin, int ichwLim,
		int ydTop, int ydBottom, ComBool fIsLastLineOfSelection,
		RECT * prsBounds,
		ComBool * pfAnythingToDraw);
	STDMETHOD(PointToChar)(int ichwBase, IVwGraphics * pvg,
		RECT rs, RECT rd,
		POINT zptdClickPosition, int * pichw,
		ComBool * pfAssocPrev);
	STDMETHOD(ArrowKeyPosition)(int ichwBase, IVwGraphics * pvg,
		int * pichwIP, ComBool * pfAssocPrev,
		ComBool fRight, ComBool fMovingIn,
		ComBool * pfResult);
	STDMETHOD(ExtendSelectionPosition)(int ichwBase, IVwGraphics * pvg,
		int * pichw, ComBool fAssocPrevMatch, ComBool fAssocPrevNeeded, int ichAnchor,
		ComBool fRight, ComBool fMovingIn,
		ComBool* pfRet);
	STDMETHOD(GetCharPlacement)(int ichwBase, IVwGraphics * pvg,
		int ichwMin, int ichwLim,
		RECT rs, RECT rd,
		ComBool fSkipSpace,
		int crgMax,	// number of ranges allowed
		int * pcxd,	// number actually made
		int * prgxdLefts, int * prgxdRights, int * prgydUnderTops);
	STDMETHOD(DrawTextNoBackground)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst, int * dxdWidth);
	HRESULT DrawTextInternal(int ichBase, IVwGraphics * pvg,
		RECT rcSrc1, RECT rcDst1, int * pdxdWidth, bool fSuppressBackground);

//	STDMETHOD(GetGlyphsAndPositions)(int ichwBase, IVwGraphics * pvg,
//		RECT rsArg, RECT rdArg,	int cchMax, int * pcchRet, OLECHAR * prgchGlyphs,
//		int * prgxd, int * prgyd);

//	STDMETHOD(GetCharData)(int ichBase, int cchMax, OLECHAR * prgch, int * pcchRet);

	void SetGraphiteSegment(Segment * pseg)
	{
		if (m_pseg && pseg != m_pseg)
			delete m_pseg;
		m_pseg = pseg;
	}
	Segment * GraphiteSegment()
	{
		return m_pseg;
	}

	void SetFwGrEngine(FwGrEngine * preneng)
	{
		m_qreneng = preneng;
	}

	void SetTextSource(FwGrTxtSrc * pgts)
	{
		m_pgts = pgts;
	}

public:
	//	For test procedures:
	int DeltaLim()	{ return 0; }  // return m_pseg->m_dichwLim; }

	//	called from debugger class GrSegmentDebug
	HRESULT debug_OutputText(BSTR * pbstrOutput);
	HRESULT debug_LogicalSurfaceToUnderlying(int ichwBase, int islout, ComBool fBefore,
		int * pichw);
	HRESULT debug_UnderlyingToLogicalSurface(int ichwBase, int ichw, ComBool fBefore,
		int * pislout);

	HRESULT debug_Ligature(int ichwBase, int ichw, int * pislout);
	HRESULT debug_LigComponent(int ichwBase, int ichw, int * piComp);
	HRESULT debug_UnderlyingComponent(int ichwBase, int islout, int iComp, int * pichw);

protected:
	long m_cref;

	Segment * m_pseg;
	// This must be smart pointer to prevent crashes when the user changes a Graphite font
	// (or even a Graphite font property) associated with a writing system.  That action
	// clears the rendering engine stored with the writing system object, which releases it
	// and thus deletes it if it has no other reference counts.  The smart pointer holds a
	// reference count, and thus keeps the engine from being deleted from underneath this
	// segment object.  (FieldWorks JIRA issue LT-2339)
	ComSmartPtr<FwGrEngine> m_qreneng;
	FwGrTxtSrc * m_pgts;		// FW wrapper
	WinSegmentPainter * m_pwsegp;

	float m_dxsStretchNeeded;
	bool m_fStretched; // or shrunk

	bool m_fChangeLineEnds;
	bool m_fNewStart;
	bool m_fNewEnd;
	Segment * m_psegAltLineEnd;

	HRESULT JustifyIfNeeded(int ichwBase, IVwGraphics * pvg);
	HRESULT ChangeLineEnds(int ichwBase, IVwGraphics * pvg);
	void SetTransformRects(SegmentPainter * psegp, gr::Rect rs, gr::Rect rd);
};

DEFINE_COM_PTR(FwGrSegment);

/*----------------------------------------------------------------------------------------------
	This is a special segment-painter for use by FieldWorks.
	Hungarian: fwsegp
----------------------------------------------------------------------------------------------*/
class FwGrWinSegmentPainter : public WinSegmentPainter
{
public:
	FwGrWinSegmentPainter(Segment * pseg, HDC hdc, float xsOrigin = 0, float ysOrigin = 0,
		bool fIsLastLineOfSelection = true);

	bool drawSelectionRange(int ichwAnchor, int ichwEnd,
		float ydLineTop, float ydLineBottom, bool bOn);

	void SuppressBackground(bool val) { m_fSuppressBackground = val; }

protected:
	bool m_fIsLastLineOfSelection;
	bool m_fAppendIndicator;	// true if we need to append a little extra to the end
								// of the selection to show that the end of the paragraph
								// is selected.
	bool m_fSuppressBackground; // true to suppress painting background colors.
	bool m_fPaintErase; // true when forcing foreground color to erase a previous paint.

	void AdjustHighlightRectangles(float ydLineTop, float ydLineBottom,
		float xdSegLeft, float xdSegRight, std::vector<Rect> & vrd);
	virtual void paintBackground(int xs, int ys);
	virtual void paintForeground(int xs, int ys);
	virtual void setForegroundPaintColor(GlyphStrmKey & gsk);
};


#endif  // !FWGRSEGMENT_INCLUDED
