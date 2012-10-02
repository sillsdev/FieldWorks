/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwPropertyStore.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Store computed values of all the formatting properties, together with maps linking
	this store to related ones.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWPROPERTYSTORE_INCLUDED
#define VWPROPERTYSTORE_INCLUDED

// forward declaration of VwPropertyStorePtr because there are some in the class.
class VwPropertyStore;
DEFINE_COM_PTR(VwPropertyStore);

typedef enum CellsSides
{
	kfcsLeading = 1,
	kfcsTrailing = 2,
	kfcsTop = 4,
	kfcsBottom = 8,
} CellSides;

/*----------------------------------------------------------------------------------------------
Class: CachedProps
Description: This class extends LgCharRenderProps with underlining fields, which don't need
to be passed to the renderer because we handle underlining in the main app.
Hungarian: chrp (This is the same as LgCharRenderProps. Saves changing a lot of stuff. The
distinction is seldom important.)
NOTE: LgCharRenderProps now contains unt and clrUnder, since it was merged with CharRenderProps.
TODO: Get rid of CachedProps as well?
----------------------------------------------------------------------------------------------*/
class CachedProps : public LgCharRenderProps
{
public:
	int m_unt;
	COLORREF m_clrUnder;
	CachedProps();
};

/*----------------------------------------------------------------------------------------------
Class: VwPropertyStore
Description:
Hungarian: vps
----------------------------------------------------------------------------------------------*/
class VwPropertyStore : public IVwPropertyStore
{
public:
	// Static methods

	// Constructors/destructors/etc.
	VwPropertyStore();
	virtual ~VwPropertyStore();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	void CommonInit();
	void SetInitialState();

	// Member variable access

	// Other public methods

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		totalrefs++;
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		totalrefs--;
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}
	static int totalrefs;

	// IVwComputedProperty methods. (This is not a public interface).
	STDMETHOD(get_ChrpFor)(ITsTextProps * pttp, LgCharRenderProps * pchrp);
	void GetChrp(LgCharRenderProps * pchrp);
	STDMETHOD(putref_Stylesheet)(IVwStylesheet * pss);
	STDMETHOD(putref_WritingSystemFactory)(ILgWritingSystemFactory * pwsf);
	STDMETHOD(get_UsesMultipleFonts)(ComBool *pf);
	STDMETHOD(get_FontFamily)(BSTR* pbstr);
	STDMETHOD(get_FontStyle)(ComBool * pf);
	STDMETHOD(get_FontWeight)(int* pnWeight);
	STDMETHOD(get_FontWeightIncrements)(int* pcactBolder);
	STDMETHOD(get_FontSize)(int* pnP1000);
	STDMETHOD(get_FontSuperscript)(int* pnP1000);
	STDMETHOD(get_RightToLeft)(ComBool* pfRet);
	STDMETHOD(get_DirectionDepth)(int* pn);
	STDMETHOD(get_FontVariations)(BSTR* pbstr);
	STDMETHOD(get_ForeColor)(int* pnRGB);
	STDMETHOD(get_BackColor)(int* pnRGB);
	STDMETHOD(get_IntProperty)(int nID, int * pnValue);
	STDMETHOD(get_StringProperty)(int sp, BSTR * bstrValue);

	// IVwPropertyStore method introduced by JohnL.
	STDMETHOD(get_TextProps)(ITsTextProps ** ppttp);

	STDMETHOD(get_ParentStore)(IVwPropertyStore ** ppvps);

	// IVwPropertyStore methods.
	// These may only be used when first creating a property store by
	// copying another--i.e., by ComputedPropertiesFor... methods.
	STDMETHOD(put_IntProperty)(int sp, int xpv, int nValue);
	STDMETHOD(put_StringProperty)(int sp, BSTR bstrValue);


	// other methods
	STDMETHOD(ComputedPropertiesForInt)(int sp, int pv,
		int nValue, VwPropertyStore ** ppvps);
	STDMETHOD(ComputedPropertiesForString)(int sp,
		BSTR bstrValue, VwPropertyStore ** ppvps);
	STDMETHOD(ComputedPropertiesForEmbedding)(VwPropertyStore ** ppvps);
	STDMETHOD(ComputedPropertiesForTtp)(ITsTextProps * pttp,
		VwPropertyStore ** ppvps);
	STDMETHOD(get_DerivedPropertiesForTtp)(ITsTextProps * pttp,
		IVwPropertyStore ** ppvps);

	VwPropertyStore * InitialStyle();
	VwPropertyStore * PropertiesForTtp(ITsTextProps * pttp);

	// Within the view subsystem, we can very efficiently read those properties
	// that it is useful to get that way. We don't bother with this sort of method
	// for text properties, because they are handled using the ActualTextProperties
	// cache, and hence relatively rarely read from these objects; in any case, the
	// reader in that case is likely to be in another compilation unit.
	// Values are returned in mp.
	int ForeColor()
	{
		return m_chrp.clrFore;
	}
	int BackColor()
	{
		return m_chrp.clrBack;
	}
	int BorderColor()
	{
		return m_clrBorderColor;
	}
	virtual int MarginTop()
	{
		return m_mpMarginTop;
	}
	int MswMarginTop()
	{
		return m_mpMswMarginTop;
	}
	virtual int MarginBottom()
	{
		return m_mpMarginBottom;
	}
	int MarginLeading()
	{
		return m_mpMarginLeading;
	}
	int MarginTrailing()
	{
		return m_mpMarginTrailing;
	}
	int MarginTop(CellSides cs);
	int MarginBottom(CellSides cs);
	int MarginLeading(CellSides cs);
	int MarginTrailing(CellSides cs);
	virtual int PadTop()
	{
		return m_mpPadTop;
	}
	virtual int PadBottom()
	{
		return m_mpPadBottom;
	}
	int PadLeading()
	{
		return m_mpPadLeading;
	}
	int PadTrailing()
	{
		return m_mpPadTrailing;
	}
	virtual int BorderTop()
	{
		return m_mpBorderTop;
	}
	virtual int BorderBottom()
	{
		return m_mpBorderBottom;
	}
	int BorderLeading()
	{
		return m_mpBorderLeading;
	}
	int BorderTrailing()
	{
		return m_mpBorderTrailing;
	}
	bool HasAnyBorder()
	{
		return m_mpBorderTop != 0 || m_mpBorderBottom != 0 || m_mpBorderTrailing != 0 || m_mpBorderLeading != 0;
	}
	int FirstIndent()
	{
		return m_mpFirstIndent;
	}
	int LineHeight()
	{
		return abs(m_mpLineHeight);
	}
	bool ExactLineHeight()
	{
		return (m_mpLineHeight < 0);
	}
	bool KeepWithNext()
	{
		return m_fKeepWithNext;
	}
	bool KeepTogether()
	{
		return m_fKeepTogether;
	}
	bool WidowOrphanControl()
	{
		return m_fWidowOrphanControl;
	}
	bool Hyphenate()
	{
		return m_fHyphenate;
	}
	int MaxLines()
	{
		return m_nMaxLines ? m_nMaxLines : INT_MAX;
	}

	bool DropCaps()
	{
		return m_fDropCaps;
	}

	bool Editable()
	{
		return m_fEditable;
	}

	TptEditable EditableEnum()
	{
		return m_fEditable;
	}

	int ParaAlign()
	{
		return m_ta;
	}
	SpellingModes SpellingMode()
	{
		return m_smSpellMode;
	}
	int BulNumScheme()
	{
		return m_vbnBulNumScheme;
	}

	int NumStartAt()
	{
		return m_nNumStartAt;
	}

	StrUni NumTxtBefore()
	{
		return m_stuNumTxtBef; // correct other var names too
	}

	StrUni NumTxtAfter()
	{
		return m_stuNumTxtAft;
	}

	StrUni NumFont()
	{
		return m_stuNumFontInfo;
	}

	bool RightToLeft()
	{
		return m_fRightToLeft;
	}

	void Lock()
	{
		// ENHANCE JohnT: This might be a good place to
		// check whether we know an writing system yet, and if so figure our direction and hence
		// direction depth.
		m_fLocked = true;
	}

	// Implemented for RecomputeEffects();
	void Unlock()
	{
		m_fLocked = false;
	}

	void DisconnectParent();
	CachedProps * Chrp();
	CachedProps * VwPropertyStore::ChrpFor(ITsTextProps * pttp);

	// Recompute the effects of this property store, and recursively fix its children.
	void RecomputeEffects();
	void SetStyleSheet(IVwStylesheet * pss)
	{
		m_qss = pss;
	}
	// Initializes the text props for the root property store.
	void InitRootTextProps(IVwViewConstructor * pvc);

	HRESULT DrawingErrors();

	int DefaultWritingSystem()
	{
		return m_wsBase;
	}
	int AdjustedLineHeight(VwPropertyStore * pzvpsLeaf, int * pdympAscent = NULL,
		int * pdympDescent = NULL, int * pdympEmHeight = NULL);
	virtual VwPropertyStore * MakePropertyStore();

protected:
	// Member variables
	long m_cref;

	CachedProps m_chrp; // The actual character properties to render text for this vps.

	bool m_fInitChrp; // Have we figured out our chrp?

	bool m_fLocked; // true when properties are finalized and may not be modified.

	StrUni m_stuFontFamily;
	// This variable stores the string kept at kspWsStyle,
	// a string that encapsulates properties defined on a per-writing-system basis.
	StrUni m_stuWsStyle;
	int m_nWeight;   // degree of boldness, scale 0-1000
	int m_cactBolder; // number of requests for bolder since last absolute
						// -ve for lighter requests
	bool m_fRightToLeft;
	StrUni m_stuFontVariations;		// defined for particular font
	int m_mpMswMarginTop;
	int m_mpMarginTop;
	int m_mpMarginBottom;
	int m_mpMarginLeading;
	int m_mpMarginTrailing;

	StrUni m_stuTags; // value of ktptTags

	int m_mpPadTop;
	int m_mpPadBottom;
	int m_mpPadLeading;
	int m_mpPadTrailing;
	int m_mpBorderTop;
	int m_clrBorderColor;
	int m_mpBorderBottom;
	int m_mpBorderLeading;
	int m_mpBorderTrailing;

	int m_vbnBulNumScheme;
	int m_nNumStartAt;
	StrUni m_stuNumTxtBef;
	StrUni m_stuNumTxtAft;
	StrUni m_stuNumFontInfo;

	int m_mpFirstIndent;
	// These next two work together. m_mpLineHeight specifies the (minimum) separation of
	// baselines. This is the value that is actually used in layout.
	// If the most recent act of setting the line height was an absolute one, m_nRelLineHeight
	// is zero; otherwise, it is the relative value used. Default is 0, 0.
	int m_mpLineHeight;
	int m_nRelLineHeight;
	int m_mpTableBorder;
	int m_mpTableSpacing;
	int m_mpTablePadding;
	int m_nMaxLines;
	VwRule m_vwrule;
	bool m_fKeepWithNext;
	bool m_fKeepTogether;
	bool m_fWidowOrphanControl;
	bool m_fHyphenate;
//	bool m_fEditable;
	TptEditable m_fEditable;
	// Determines whether this property store represents Drop Caps text. Currently this is based
	// on looking for a particular style.
	bool m_fDropCaps;
	FwTextAlign m_ta;
	SpellingModes m_smSpellMode;
	// To produce the gaps we want between cells of a table, we set the default margins
	// of the "reset" property store for the table. The user may then override for
	// individual cells. The default margins, but not any explicitly set ones, need to
	// be overridden for cells at the boundary of the table. So we keep track here of
	// which margins have been set explicitly.
	CellSides m_grfcsExplicitMargins;

	// Property store obtained when uninheritable properties are reset, starting from
	// this one.
	VwPropertyStorePtr m_qzvpsReset;

	int m_ws;				// top-level actual writing system
	int m_wsBase;			// default empty string writing system

	// Variables for linking to related objects.
	VwPropertyStore* m_pzvpsParent;	// The one this is derived from, if any.

	// Three maps used to record results of applying ttp objects, and int and
	// string literal property settings. There is probably some way we could
	// use a single data structure for all three. This first map would work
	// if there were a really efficient way to get a Ttp for one-prop Ttps.
	typedef ComHashMap<ITsTextProps *, VwPropertyStore> MapTtpPropStore;
	MapTtpPropStore m_hmttpzvps;

	// The ComHashMap does not keep reference counts for keys. Thus there is a
	// danger that
	// the reference count on a key ttp could go to zero. To prevent that, we take advantage
	// of the fact that each property store is a value in at most one MapTtpPropStore,
	// and have the target property store keep a reference to the rule which is its key.
	ITsTextPropsPtr m_qttpKey;

	class IntPropKey
	{
	public:
		int m_nID;
		int m_nVariation;
		int m_nValue;
		IntPropKey(int nID, int nVariation, int nValue)
		{
			m_nID = nID;
			m_nVariation = nVariation;
			m_nValue = nValue;
		}
		IntPropKey()  // use as key in ComHashMap requires default constructor
		{
			m_nID = -1;		// should prevent it being equal to any valid key.
			m_nVariation = -1;
			m_nValue = 0;
		}
	};

	typedef ComHashMap<IntPropKey, VwPropertyStore> MapIPKPropStore;  // int keys
	MapIPKPropStore m_hmipkzvps;

	class StrPropRec
	{
	public:
		int m_nID;
		StrUni m_su;
		VwPropertyStore* m_pzvps;

		StrPropRec(int nID, BSTR bstr, VwPropertyStore* pzvps)
		{
			m_nID = nID;
			m_su = bstr;
			m_pzvps = pzvps;
			if (m_pzvps) m_pzvps->AddRef();
		}
		StrPropRec() // default constructor required for key class
		{
			m_nID = -1; // won't match any real key
			m_pzvps = 0;
			// StrUni inited by own constructor to null
		}
		// Need a copy constructor to get the proper AddRef when copying into
		// the vector
		StrPropRec(const StrPropRec& sprSource)
		{
			m_nID = sprSource.m_nID;
			StrUni * psu = const_cast<StrUni *>(&sprSource.m_su);
			m_su = psu->Bstr();
			m_pzvps = sprSource.m_pzvps;
			if (m_pzvps) m_pzvps->AddRef();
		}
		// Likewise an assignment operator
		StrPropRec & operator=(const StrPropRec& sprSource)
		{
			if (m_pzvps) m_pzvps->Release();
			m_nID = sprSource.m_nID;
			StrUni * psu = const_cast<StrUni *>(&sprSource.m_su);
			m_su = psu->Bstr();
			m_pzvps = sprSource.m_pzvps;
			if (m_pzvps) m_pzvps->AddRef();
			return *this;
		}

		~StrPropRec()
		{
			ReleaseObj(m_pzvps);
		}

		// For our purposes the first two items are a key, and if they match
		// the objects are equal.
		bool operator==(StrPropRec& spr)
		{
			return m_nID == spr.m_nID &&
				m_su == spr.m_su;
		}
	};

	typedef Vector<StrPropRec> VecStrProps;
	VecStrProps m_vstrprrec;

	IVwStylesheetPtr m_qss;
	ILgWritingSystemFactoryPtr m_qwsf;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	void CopyFrom(VwPropertyStore* pzvpsParent);
	void CopyInheritedFrom(VwPropertyStore* pzvpsParent);
	void ApplyTtp(ITsTextProps * pttp);
	void InitChrp();
	void GetUnderlineInfo(int * punt, COLORREF * pclr);
	void DoWsDefaultFontVar(int ws);
	void DoWsStyles(int ws);
	int FontSizeForWs(int ws);
	void EnsureWritingSystemFactory();
};

typedef ComHashMapStrUni<ITsTextProps> MapStrTtp; // Hungarian hmsuttp
/*----------------------------------------------------------------------------------------------
Class: VwStylesheet
Description: Provides a place to store a collection of styles and pass them as one argument
Hungarian: ss
----------------------------------------------------------------------------------------------*/
class VwStylesheet : public IVwStylesheet
{
public:
	// Constructors/destructors/etc.
	VwStylesheet();
	virtual ~VwStylesheet();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(ULONG, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(ULONG, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwStylesheet methods
	STDMETHOD(GetDefaultBasedOnStyleName)(BSTR * pbstrNormal);
	// Gets the style name that is the default style to use for the given context
	STDMETHOD(GetDefaultStyleForContext)(int nContext, ComBool fCharStyle, BSTR * pbstrStyleName);
	STDMETHOD(PutStyle)(BSTR bstrName, BSTR bstrUsage, HVO hvoStyle, HVO hvoBasedOn,
		HVO hvoNext, int nType, ComBool fBuiltIn, ComBool fModified, ITsTextProps * pttp);
	STDMETHOD(GetStyleRgch)(int cch, OLECHAR * prgchName, ITsTextProps ** ppttp);
	STDMETHOD(GetNextStyle)(BSTR bstrName, BSTR * pbstrNext);
	STDMETHOD(GetBasedOn)(BSTR bstrName, BSTR * pbstrBasedOn);
	STDMETHOD(GetType)(BSTR bstrName, int * pnType);
	// Get the context (in pnContext) for the style named bstrName.
	STDMETHOD(GetContext)(BSTR bstrName, int * pnContext);
	// Is the style a predefined style?
	STDMETHOD(IsBuiltIn)(BSTR bstrName, ComBool * pfBuiltIn);
	// Was the (predefined) style changed by the user?
	STDMETHOD(IsModified)(BSTR bstrName, ComBool * pfModified);
	// ENHANCE EberhardB: Add method for usage
	// Return the line height (ascender + descender) for the given style
	STDMETHOD(get_DataAccess)(ISilDataAccess ** ppsda);
	STDMETHOD(MakeNewStyle)(HVO * phvoNewStyle);
	STDMETHOD(Delete)(HVO hvoStyle);
	STDMETHOD(get_CStyles)(int * pcttp);
	STDMETHOD(get_NthStyle)(int ihvo, HVO * phvo);
	STDMETHOD(get_NthStyleName)(int ihvo, BSTR * pbstrStyleName);
	STDMETHOD(get_NormalFontStyle)(ITsTextProps ** ppttp);
	STDMETHOD(get_IsStyleProtected)(BSTR bstrName, ComBool * pfProtected);
	STDMETHOD(CacheProps)(int cch, OLECHAR * prgchName, HVO hvoStyle, ITsTextProps * pttp);
protected:
	// member variables
	long m_cref;
	MapStrTtp m_hmsuttp;
};


#endif  //VWPROPERTYSTORE_INCLUDED
