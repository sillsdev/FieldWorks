/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2001-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UniscribeSegment.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UNISCRIBESEGMENT_INCLUDED
#define UNISCRIBESEGMENT_INCLUDED

ATTACH_GUID_TO_CLASS(class, 61299C3B-54D6-4c46-ACE5-72B9128F2048, UniscribeSegment);
// Trick GUID for getting the actual implementation of the UniscribeSegment object.
#define CLSID_UniscribeSegment __uuidof(UniscribeSegment)

// Define this to cause strings sent to Uniscribe to be normalized to NFC. This seems to be
// necessary for Korean and some Arabic fonts to work properly.
#define UNISCRIBE_NFC 1

// These constants are currently defined in RomRenderSegment.h; this component needs them too.
//const kdzptInch = 72;
//const kdzmpInch = 72000;

typedef Vector<WORD> WordVec; // Hungarian vw
typedef Vector<SCRIPT_VISATTR> ScrVisAttrVec; // Hungarian vsva
typedef Vector<int> IntVec; // Hungarian vi (or specific meaning)
typedef Vector<GOFFSET> OffsetVec; // Hungarian voff
typedef Vector<SCRIPT_ITEM> ScrItemVec; // Hungarian vscri;
typedef Vector<SCRIPT_LOGATTR> ScrLogAttrVec; // Hungarian vsla.

/*----------------------------------------------------------------------------------------------
Class: UniscribeRunInfo
Description: This is the block of information that is passed to all our functors.
Hungarian: uri
----------------------------------------------------------------------------------------------*/
class UniscribeRunInfo
{
private:
	int m_cglyphMax;		// count of glyphs we allocated memory for
	int m_cClusterMax;		// count of clusters we allocated memory for
	bool m_fFromCopy;		// True if this run info is a copy of another (false otherwise)
public:
	UniscribeRunInfo(int cglyphMax = 0, int cClusterMax = 0);
	UniscribeRunInfo(const UniscribeRunInfo & oriUri);
	~UniscribeRunInfo();
	void Detach(); // detach all allocated pointers from this instance
	void UpdateGlyphSize(int cglyphMax);
	void UpdateClusterSize(int cClusterMax);
	int CGlyphMax() { return m_cglyphMax; }
	int CClusterMax() { return m_cClusterMax; }

public:
	IVwGraphics * pvg;		// For drawing/measuring info
	HDC hdc;				// wrapped by pvg
	const OLECHAR * prgch;	// chars of run
	int cch;				// count in run
	bool fLast;				// true for last run of segment
	int xd;					// destination coord where run is to be drawn
	int dxdStretch;			// amount of stretch allocated to run (currently always 0)
	Rect rcSrc; Rect rcDst;	// ccordinate transformation
	LgCharRenderProps * pchrp; // char props desired for run (already set in pvg)
	SCRIPT_ANALYSIS * psa;	// script analysis for the run

	int cglyph;
	WORD * prgGlyph;			// cglyph glyphs (from ScriptShape)
	SCRIPT_VISATTR * prgsva;	// cglyph SCRIPT_VISATTRs (from ScriptShape)
	int * prgAdvance;			// cglyph advance widths for glyphs (from ScriptPlace)
	int * prgcst;				// cglyph stretch-type for each glyph (used when creating a segment)
	int * prgJustAdv;			// cglyph justified advance widths for glyphs
	GOFFSET * prgoff;			// cglyph x,y offsets for glyphs (from ScriptPlace)

	WORD * prgCluster;	// cch clusters (from ScriptShape)
	SCRIPT_CACHE sc;	// update when changing pvg props.
	int dxdWidth;		// of run
	bool fScriptPlaceFailed; // true if call to ScriptPlace failed.
	// If ScriptPlace failed, should not do any more calls to Uniscribe methods for run,
	// in particular, prgsva, prgAdvance, and prgoff are useless. prgcst will behave as
	// if all characters are letters.
};

// Character-stretch types:
enum {
	kcstLetter = 0,
	kcstDiac = 1,
	kcstWhiteSpace = 2,
};

/*----------------------------------------------------------------------------------------------
Class: UniscribeSegment
Description:
Hungarian: rrs
----------------------------------------------------------------------------------------------*/
class UniscribeSegment : public ILgSegment
{
	friend class UniscribeEngine;
public:
	// Static methods
	/*------------------------------------------------------------------------------------------
		Store a SCRIPT_CACHE value if the LgCharRenderProps isn't already stored.
	------------------------------------------------------------------------------------------*/
	static void StoreScriptCache(UniscribeRunInfo & uri)
	{
		g_fsc.StoreScriptCache(uri);
	}

	/*------------------------------------------------------------------------------------------
		Return the SCRIPT_CACHE value associated with the LgCharRenderProps, or NULL if it's
		not found.
	------------------------------------------------------------------------------------------*/
	static SCRIPT_CACHE FindScriptCache(UniscribeRunInfo & uri)
	{
		return g_fsc.FindScriptCache(uri);
	}

	// Constructors/destructors/etc.
	UniscribeSegment();
	UniscribeSegment(IVwTextSource * pts, UniscribeEngine * prre, int dichLim,
		LgLineBreak lbrkStart, LgLineBreak lbrkEnd, ComBool fEndLine, ComBool fParaRTL);
	virtual ~UniscribeSegment();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
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
	STDMETHOD(DrawText)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		int * dxdWidth);
	STDMETHOD(Recompute)(int ichBase, IVwGraphics * pvg);
	STDMETHOD(get_Width)(int ichBase, IVwGraphics * pvg, int * px);
	STDMETHOD(get_RightOverhang)(int ichBase, IVwGraphics * pvg, int * px);
	STDMETHOD(get_LeftOverhang)(int ichBase, IVwGraphics * pvg, int * px);
	STDMETHOD(get_Height)(int ichBase, IVwGraphics * pvg, int * py);
	STDMETHOD(get_Ascent)(int ichBase, IVwGraphics * pvg, int * py);
	STDMETHOD(Extent)(int ichBase, IVwGraphics * pvg, int* px, int* py);
	STDMETHOD(BoundingRect)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		RECT * prcBounds);
	STDMETHOD(GetActualWidth)(int ichBase, IVwGraphics * pvg, RECT rcSrc, RECT rcDst,
		int * dxdWidth);
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

	HRESULT DrawTextInternal(int ichBase, IVwGraphics * pvg,
		RECT rcSrc1, RECT rcDst1, int * pdxdWidth, bool fSuppressBackground);

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

	void SetStretchValues(int cGlyphs, int * pdxsStretch, const Vector<int>& vcst)
	{
		m_cGlyphsInSeg = cGlyphs;
		m_pdxsAvailStretch = pdxsStretch;
		((Vector<int>&)vcst).CopyTo(m_vcst);
	}

	static int OffsetInNfc(int ich, int ichBase, IVwTextSource * pts);
	static int OffsetToOrig(int ich, int ichBase, IVwTextSource * pts);

protected:
	// Static variables
	static ScrItemVec g_vscri; // vector of script items from ScriptItemize.
	static int g_cscri; // number of valid items in ScriptItemize.

	// Member variables
	long m_cref;				// standard COM ref count
	IVwTextSourcePtr m_qts;		// the source of our text
	UniscribeEnginePtr m_qure;
	int m_dichLim;				// How far beyond ichBase we end
	LgLineBreak m_lbrkStart;	// ENHANCE JohnT: could we merge these 3 into 1 int somehow?
	LgLineBreak m_lbrkEnd;
	bool m_fEndLine;			// Segment ends its line. For now we don't care whether
								// it starts its line.
	int m_dxsStretch;
	int m_cGlyphsInSeg;			// total glyphs in all runs
	int * m_pdxsAvailStretch;	// stretch possible for each glyph
	Vector<int> m_vcst; 		// stretch-type of each glyph

	// Values computed by ComputeDimensions
	int m_dxsWidth;				// width in absence of any stretch; -1 if not computed;
								// does not include trailing white-space at the end of the line
	int m_dxsTotalWidth;		// equal to m_dxsWidth unless there is trailing ows at the
								// end of the line
	int m_dysHeight;
	int m_dysAscent;			// distance from common baseline to top of segment

	bool m_fWsOnly;
	int m_nDirDepth;
//	bool m_fReversed;	// for upstream white-space at the end of the line
	bool m_fParaRTL;
	// Character property engine used by IsValidInsertionPoint() and ArrowKeyPosition().
	ILgCharacterPropertyEnginePtr m_qcpe;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	bool RightToLeft()
	{
		return m_nDirDepth % 2;
	}

	// scale an integer value by a mul/div factor. Use this instead of the regular MulDiv
	// function to provide a safety net for divide-by-0 errors.
	int ScaleIntX(int source, const Rect & div, const Rect & mul)
	{
		Assert(div.Width());
		return (div.Width() == 0) ? source : (mul.MapXTo(source, div) - mul.MapXTo(0, div));
	}

	int ScaleIntY(int source, const Rect & div, const Rect & mul)
	{
		Assert(div.Height());
		return (div.Height() == 0) ? source : (mul.MapYTo(source, div) - mul.MapYTo(0, div));
	}

	template<class Op> int DoAllRuns(int ichBase, IVwGraphics * pvg,
		Rect rcSrc, Rect rcDst, Op& f, bool fSuppressBackgroundColor = false);
	void EnsureDefaultDimensions(int ichBase, IVwGraphics * pvg);
	void ComputeDimensions(int ichBase, IVwGraphics * pvg, Rect rcSrc, Rect rcDst,
		bool fNeedWidth = true);
	void AdjustForRtlWhiteSpace(Rect & rcSrc);
	static void ShapePlaceRun(UniscribeRunInfo& uri, bool fCreatingSeg = false);
	static int CallScriptItemize(OLECHAR * prgchDefBuf, int cchBuf, Vector<OLECHAR> & vch,
		IVwTextSource * pts, int ichMin, int cch, OLECHAR ** pprgchBuf, int & citem,
		bool fParaRTL);

	int NumStretchableGlyphs();
	int StretchGlyphs(UniscribeRunInfo & uri,
		int cglyphToStretch, int * pdxdStretchRemaining, int iglyphSeg);
	void FindValidIPBackward(int ichBase, int * pich);
	void FindValidIPForward(int ichBase, int * pich);

	void InterpretChrp(LgCharRenderProps & chrp);

	/*------------------------------------------------------------------------------------------
		This internal class wraps a hashmap for caching SCRIPT_CACHE values.
		MSDN documentation says, and i quote,

			The client must allocate and retain one SCRIPT_CACHE variable for each character
			style used.

		As far as i can tell, each different LgCharRenderProps defines a "character style", so
		that's what we map from to get the stored values.  (SteveMc)
	------------------------------------------------------------------------------------------*/
	class FwScriptCache
	{
	public:
		~FwScriptCache();
		/*--------------------------------------------------------------------------------------
			Store a SCRIPT_CACHE value if the LgCharRenderProps isn't already stored.
		--------------------------------------------------------------------------------------*/
		void StoreScriptCache(UniscribeRunInfo & uri);
		/*--------------------------------------------------------------------------------------
			Return the SCRIPT_CACHE value associated with the LgCharRenderProps, or NULL if it's
			not found.
		--------------------------------------------------------------------------------------*/
		SCRIPT_CACHE FindScriptCache(UniscribeRunInfo & uri);

	protected:
		HashMap<LgCharRenderProps, SCRIPT_CACHE> m_hmchrpsc;
		HashMap<LgCharRenderProps, SCRIPT_CACHE> m_hmchrpscOther;

		/*--------------------------------------------------------------------------------------
			Delete all the stored SCRIPT_CACHE values in one of the hash maps.
		--------------------------------------------------------------------------------------*/
		void ResetScriptCacheMap(HashMap<LgCharRenderProps, SCRIPT_CACHE>& hmchrpsc);

		/*--------------------------------------------------------------------------------------
			Check if another hash map entry maps to same SCRIPT_CACHE value.
		--------------------------------------------------------------------------------------*/
		void CheckDuplicateMapping(UniscribeRunInfo& uri,
			HashMap<LgCharRenderProps, SCRIPT_CACHE>& hmchrpsc);
	};

	static FwScriptCache g_fsc;

};
DEFINE_COM_PTR(UniscribeSegment);

#endif  //UNISCRIBESEGMENT_INCLUDED
