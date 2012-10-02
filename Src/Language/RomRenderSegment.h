/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: RomRenderSegment.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef ROMRENDERSEGMENT_INCLUDED
#define ROMRENDERSEGMENT_INCLUDED

ATTACH_GUID_TO_CLASS(class, A124E0C1-DD4B-11d2-8078-0000C0FB81B5, RomRenderSegment);
// Trick GUID for getting the actual implementation of the RomRenderSegment object.
#define CLSID_RomRenderSegment __uuidof(RomRenderSegment)

// ENHANCE JohnT: put this somewhere better (where kdzmpInch is finally put)
const int kdzptInch = 72;
const int kdzmpInch = 72000;


/*----------------------------------------------------------------------------------------------
Class: RomRenderSegment
Description:
Hungarian: rrs
----------------------------------------------------------------------------------------------*/
class RomRenderSegment : public ILgSegment
{
	friend class RomRenderEngine;
public:
	// Static methods

	// Constructors/destructors/etc.
	RomRenderSegment();
	RomRenderSegment(IVwTextSource * pts, RomRenderEngine * prre, int dichLim,
		LgLineBreak lbrkStart, LgLineBreak lbrkEnd, ComBool fEndLine);
	virtual ~RomRenderSegment();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// ILgSegment methods
	STDMETHOD(DrawText)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst, int * dxdWidth);
	STDMETHOD(Recompute)(int ichBase, IVwGraphics * pvg);
	STDMETHOD(get_Width)(int ichBase, IVwGraphics * pvg, int * px);
	STDMETHOD(get_RightOverhang)(int ichBase, IVwGraphics * pvg, int * px);
	STDMETHOD(get_LeftOverhang)(int ichBase, IVwGraphics * pvg, int * px);
	STDMETHOD(get_Height)(int ichBase, IVwGraphics * pvg, int * py);
	STDMETHOD(get_Ascent)(int ichBase, IVwGraphics * pvg, int * py);
	STDMETHOD(Extent)(int ichBase, IVwGraphics * pvg, int* px, int* py);
	STDMETHOD(BoundingRect)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		RECT * prcBounds);
	STDMETHOD(GetActualWidth)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst, int * dxdWidth);
	STDMETHOD(get_AscentOverhang)(int ichBase, IVwGraphics * pvg, int * py);
	STDMETHOD(get_DescentOverhang)(int ichBase, IVwGraphics * pvg, int * py);
	STDMETHOD(get_RightToLeft)(int ichBase, ComBool * pfResult);
	STDMETHOD(get_DirectionDepth)(int ichBase, int * pnDepth, ComBool * pfWeak);
	STDMETHOD(SetDirectionDepth)(int ichBase, int nNewDepth);
	STDMETHOD(get_WritingSystem)(int ichBase, int * pws);
	STDMETHOD(get_Lim)(int ichBase, int * pdich);
	STDMETHOD(get_LimInterest)(int ichBase, int * pdich);
	STDMETHOD(put_EndLine)(int ichBase, IVwGraphics* pvg, ComBool fNewVal);
	STDMETHOD(put_StartLine)(int ichBase, IVwGraphics* pvg, ComBool fNewVal);
	STDMETHOD(get_StartBreakWeight)(int ichBase, IVwGraphics* pvg, LgLineBreak* pnTwips);
	STDMETHOD(get_EndBreakWeight)(int ichBase, IVwGraphics* pvg, LgLineBreak* pnTwips);
	STDMETHOD(get_Stretch)(int ichBase, int* px);
	STDMETHOD(put_Stretch)(int ichBase, int x);
	STDMETHOD(IsValidInsertionPoint)(int ichBase, IVwGraphics * pvg, int ich,
		LgIpValidResult * pipvr);
	STDMETHOD(DoBoundariesCoincide)(int ichBase, IVwGraphics * pvg,
		ComBool fBoundaryEnd, ComBool fBoundaryRight, ComBool * pfResult);
	STDMETHOD(DrawInsertionPoint)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		int ich, ComBool fAssocPrev, ComBool fOn, LgIPDrawMode dm);
	STDMETHOD(PositionsOfIP)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		int ich, ComBool fAssocPrev, LgIPDrawMode dm,
		RECT * rectPrimary, RECT * rectSecondary,
		ComBool * pfPrimaryHere, ComBool * pfSecHere);
	STDMETHOD(DrawRange)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		int ichMin, int ichLim, int ydTop, int ydBottom, ComBool bOn,
		ComBool fIsLastLineOfSelection, RECT * prsBounds);
	STDMETHOD(PositionOfRange)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		int ichMin, int ichim, int ydTop, int ydBottom, ComBool fIsLastLineOfSelection,
		RECT * rsBounds, ComBool * pfAnythingToDraw);
	STDMETHOD(PointToChar)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		POINT ptdClickPosition, int * pich, ComBool * pfAssocPrev);
	STDMETHOD(ArrowKeyPosition)(int ichBase, IVwGraphics * pvg, int * pich,
		ComBool * pfAssocPrev, ComBool fRight, ComBool fMovingIn, ComBool * pfResult);
	STDMETHOD(ExtendSelectionPosition)(int ichBase, IVwGraphics * pvg,
		int * pich, ComBool fAssocPrevMatch, ComBool fAssocPrevNeeded, int ichAnchor,
		ComBool fRight, ComBool fMovingIn, ComBool * pfRet);
	STDMETHOD(GetCharPlacement)(int ichBase, IVwGraphics * pvg, int ichMin, int ichLim,
		RECT rcSrc, RECT rcDst,
		ComBool fSkipSpace, int crgMax, int * pcxd, int * prgxdLefts, int * prgxdRights,
		int * prgydUnderTops);
	STDMETHOD(DrawTextNoBackground)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst, int * dxdWidth);

//	STDMETHOD(GetGlyphsAndPositions)(int ichwBase, IVwGraphics * pvg,
//		RECT rsArg, RECT rdArg,	int cchMax, int * pcchRet, OLECHAR * prgchGlyphs,
//		int * prgxd, int * prgyd);

//	STDMETHOD(GetCharData)(int ichBase, int cchMax, OLECHAR * prgch, int * pcchRet);

	// Other public methods
	IVwTextSource * GetSource() // temp use; caller does not get ref count automatically
	{
		return m_qts;
	}
	void SetLim(int dichLim)
	{
		m_dichLim = dichLim;
		m_dxsWidth = -1; // changing limit invalidates
		m_dxsTotalWidth = -1;
	}
	void AdjustEndForWidth(int ichBase, IVwGraphics * pvg);
	int GetLim()
	{
		return m_dichLim;
	}
	void SetDirectionInfo(int nDirDepth, bool fWsOnly)
	{
		m_nDirDepth = nDirDepth;
		m_fWsOnly = fWsOnly;
	}

protected:
	// Member variables
	long m_cref;				// standard COM ref count
	IVwTextSourcePtr m_qts;		// the source of our text
	RomRenderEnginePtr m_qrre;
	int m_dichLim;				// How far beyond ichBase we end
	LgLineBreak m_lbrkStart;	// ENHANCE JohnT: could we merge these three into one int somehow?
	LgLineBreak m_lbrkEnd;
	bool m_fEndLine;			// Segment ends its line. For now we don't care whether
								// it starts its line.
	int m_dxsStretch;

	// Values computed by ComputeDimensions
	int m_dxsWidth;				// width in absence of any stretch; -1 if not computed;
								// does not include trailing white-space at the end of the line
	int m_dxsTotalWidth;		// equal to m_dxsWidth unless there is trailing ows at the
								// end of the line
	int m_dysHeight;
	int m_dysAscent;			// distance from common baseline to top of segment

	bool m_fWsOnly;
	int m_nDirDepth;
	bool m_fReversed;	// for upstream white-space at the end of the line

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	template<class Op> void DoAllRuns(int ichBase, IVwGraphics * pvg,
		Rect rcSrc, Rect rcDst, Op& f, bool fNeedWidth = true);
	void ComputeDimensions(int ichBase, IVwGraphics * pvg, Rect rcSrc, Rect rcDst,
		bool fNeedWidth = true);

	// scale an integer value by a mul/div factor. Use this instead of the regular MulDiv
	// function to provide a safety net for divide-by-0 errors.
	int ScaleInt(int source, int mul, int div)
	{
		Assert(div);
		return (div == 0) ? source : MulDiv(source, mul, div);
	}
};
DEFINE_COM_PTR(RomRenderSegment);

#endif  //ROMRENDERSEGMENT_INCLUDED
