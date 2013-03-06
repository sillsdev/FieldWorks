/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 1999, 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwPropertyStore.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Store computed values of all the formatting properties, together with maps linking
	this store to related ones.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

int VwPropertyStore::totalrefs = 0;

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

static int g_rgnFontSizes[] = {
	5000,	// kvfsXXSmall
	7000,	// kvfsXSmall
	9000,	// kvfsSmall
	12000,	// kvfsNormal
	14000,	// kvfsLarge
	18000,	// kvfsXLarge
	24000	// kvfsXXLarge
};
	// kvfsSmaller and kvfsLarger don't have absolute values
static int knDefaultFontSize = 10000;   // 10 point default

// The order of these is signficant--it is the order the font properties are recorded in
// for each writing system, in the wsStyle string.
// A copy of this list is in VwPropertyStore.cpp -- the two lists must be kept in sync.
static const int s_rgtptWsStyleProps[] = {
	ktptFontSize, ktptBold, ktptItalic, ktptSuperscript,
	ktptForeColor, ktptBackColor, ktptUnderColor, ktptUnderline, ktptOffset
};
static const int s_ctptWsStyleProps = (isizeof(s_rgtptWsStyleProps) / isizeof(int));

// Magic font strings that are used in markup:
static OleStringLiteral g_pszDefaultFont(L"<default font>");

//:>********************************************************************************************
//:>	Constructor/Destructor
//:>********************************************************************************************

CachedProps::CachedProps()
{
	memset(this, 0, isizeof(this));
}


/*----------------------------------------------------------------------------------------------
	Init common to constructor and SetInitialState.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::CommonInit()
{
	m_stuFontFamily = (OLECHAR*)g_pszDefaultFont;
	// m_stuWsStyle is self-initialized to empty
	m_nWeight = kvfwNormal;
	m_chrp.dympHeight = knDefaultFontSize;
	m_chrp.m_unt = kuntNone;
	m_chrp.m_clrUnder = (unsigned long) kclrTransparent; // cheat: means same as foreground
#if WIN32
	m_chrp.clrFore = m_clrBorderColor = ::GetSysColor(COLOR_WINDOWTEXT);
#else //WIN32
	// set to default black RGB color
	m_chrp.clrFore = m_clrBorderColor = RGB(0,0,0);
#endif //WIN32
	m_chrp.clrBack = (unsigned long) kclrTransparent;
	m_nNumStartAt = INT_MIN; // a very unlikely value to signify not specified.
	m_fEditable = ktptIsEditable;
	m_ta = ktalLeading; // This is actually 0, but play safe.
	m_smSpellMode = ksmNormalCheck; // also zero.
}

/*----------------------------------------------------------------------------------------------
	RE-create the initial state of a newly constructed property store, except:
		- don't alter ref count, locked status, parent, maps, reset, key.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::SetInitialState()
{
	ClearItems(&m_chrp, 1); // zeros everything in m_chrp
	CommonInit();
	m_stuWsStyle.Clear();
	m_cactBolder = 0;
	m_fRightToLeft = FALSE;
	m_stuFontVariations.Clear();
	m_mpMswMarginTop = 0;
	m_mpMarginTop = m_mpMarginBottom = m_mpMarginLeading = m_mpMarginTrailing = 0;
	m_stuTags.Clear();
	m_mpPadTop = m_mpPadBottom = m_mpPadLeading = m_mpPadTrailing = 0;
	m_mpBorderTop = m_mpBorderBottom = m_mpBorderLeading = m_mpBorderTrailing = 0;
	m_clrBorderColor = 0;
	m_vbnBulNumScheme = 0;
	m_stuNumTxtBef.Clear();
	m_stuNumTxtAft.Clear();
	m_stuNumFontInfo.Clear();
	m_mpFirstIndent = 0;
	m_mpLineHeight = 0;  // default is just enough for font height
	m_nRelLineHeight = 0; // default is absolute
	m_mpTableBorder = m_mpTableSpacing = m_mpTablePadding = 0;
	m_vwrule = (VwRule) 0;
	m_nMaxLines = 0; // interpreted as unlimited
	m_fKeepWithNext = FALSE;
	m_fKeepTogether = FALSE;
	m_fWidowOrphanControl = TRUE; // default should be true
	m_fHyphenate = FALSE;
	m_grfcsExplicitMargins = (CellSides) 0;
	m_pzvpsParent = 0;
	m_ws = m_wsBase = 0;
}

VwPropertyStore::VwPropertyStore()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_fLocked = false;
	Assert(m_cactBolder == 0);
	Assert(m_mpMarginTop == 0);
	Assert(m_grfcsExplicitMargins == 0);
	Assert(m_nMaxLines == 0); // interpreted as unlimited
	Assert(m_pzvpsParent == 0);
	CommonInit();
}

VwPropertyStore::~VwPropertyStore()
{
	ModuleEntry::ModuleRelease();
	// Call DisconnectParent on all children: forces them to get rid of their
	// (uncounted) pointer to this.

	for (int ispr = 0; ispr < m_vstrprrec.Size(); ++ispr)
	{
		m_vstrprrec[ispr].m_pzvps->DisconnectParent();
		// m_vstrprrec[ispr].m_pzvps->Release(); No! Destructor of Vector and StrPropRec does it!
	}

	MapTtpPropStore::iterator ithmvprzvps;
	for (ithmvprzvps = m_hmttpzvps.Begin();
		ithmvprzvps != m_hmttpzvps.End();
		++ithmvprzvps)
	{
		ithmvprzvps.GetValue()->DisconnectParent();
	}

	MapIPKPropStore::iterator ithmipkzvps;
	for (ithmipkzvps = m_hmipkzvps.Begin();
		ithmipkzvps != m_hmipkzvps.End();
		++ithmipkzvps)
	{
		ithmipkzvps.GetValue()->DisconnectParent();
	}

#if !WIN32
	// work around TeDllTests hang on exit
	if (m_qwsf)
		m_qwsf.Detach();
#endif
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwPropertyStore::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwPropertyStore)
		*ppv = static_cast<IVwPropertyStore *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwPropertyStore);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factVps(
	_T("SIL.Views.VwPropertyStore"),
	&CLSID_VwPropertyStore,
	_T("SIL Property Store"),
	_T("Apartment"),
	&VwPropertyStore::CreateCom);


void VwPropertyStore::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwPropertyStore> qvps;
	qvps.Attach(NewObj VwPropertyStore());		// ref count initialy 1
	qvps->Lock();	// This will be a root store and should be initially locked.
	CheckHr(qvps->QueryInterface(riid, ppv));
}

VwPropertyStore * VwPropertyStore::MakePropertyStore()
{
	return NewObj VwPropertyStore();
}

//:>********************************************************************************************
//:>	IVwTextPropertyMap Interface Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the actual text properties object associated with the given key (actually a
	TxTextProps, typically), if any. Caller is responsible to Release once.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_ChrpFor(ITsTextProps * pttp, LgCharRenderProps * pchrp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pttp);
	ChkComArgPtr(pchrp);
	VwPropertyStore * pvps = PropertiesForTtp(pttp);
	pvps->GetChrp(pchrp);
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

CachedProps * VwPropertyStore::Chrp()
{
	if (!m_fInitChrp)
	{
		InitChrp();
	}
	return &m_chrp;
}

CachedProps * VwPropertyStore::ChrpFor(ITsTextProps * pttp)
{
	return PropertiesForTtp(pttp)->Chrp();
}


void VwPropertyStore::GetChrp(LgCharRenderProps * pchrp)
{
	if (!m_fInitChrp)
	{
		InitChrp();
	}
	CopyBytes(&m_chrp, pchrp, isizeof(LgCharRenderProps));
}

/*----------------------------------------------------------------------------------------------
	Create the writing system factory if it does not yet exist.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::EnsureWritingSystemFactory()
{
	if (!m_qwsf)
	{
		AssertPtr(m_qss);
		ISilDataAccessPtr qsda;
		CheckHr(m_qss->get_DataAccess(&qsda));
		AssertPtr(qsda);
		CheckHr(qsda->get_WritingSystemFactory(&m_qwsf));
	}
	AssertPtr(m_qwsf);
}

/*----------------------------------------------------------------------------------------------
	Compute an adjusted line height (in mp) that is
	- the exact line height the user specified, if using exact line height
	- otherwise, the max of the line height the user specified and the
		height of the height of the font.
	Also optionally returns some font metrics based on this property store, but using the
	writing system and font family from the specified leaf property store (since this property
	store is presumably that of a paragraph (which means it won't have a real WS and might
	have a magic font name).
----------------------------------------------------------------------------------------------*/
int VwPropertyStore::AdjustedLineHeight(VwPropertyStore * pzvpsLeaf, int * pdympAscent,
	int * pdympDescent, int * pdympEmHeight)
{
	VwPropertyStorePtr qzvpsWithWsAndFont;
	qzvpsWithWsAndFont.Attach(MakePropertyStore());
	qzvpsWithWsAndFont->CopyInheritedFrom(this);
	qzvpsWithWsAndFont->m_stuFontFamily = pzvpsLeaf->m_stuFontFamily;
	qzvpsWithWsAndFont->m_chrp.ws = pzvpsLeaf->m_chrp.ws;
	qzvpsWithWsAndFont->Lock();

#if WIN32
	// We want to make some measurements on the font. They don't have to be super precise,
	// so we'll just use a current screen DC.
	HDC hdc = AfGdi::GetDC(NULL);
	int dpiY = ::GetDeviceCaps(hdc, LOGPIXELSY);

	LgCharRenderProps * pchrp = qzvpsWithWsAndFont->Chrp();

	// This is very similar to code in VwGraphics::SetupGraphics, but we don't have
	// an IVwGraphics here so I don't see how to share it.
	LOGFONT lf;
	lf.lfItalic = pchrp->ttvItalic == kttvOff ? false : true;
	lf.lfWeight = pchrp->ttvBold == kttvOff ? 400 : 700;
	// The minus causes this to be the font height (roughly, from top of ascenders
	// to bottom of descenders). A positive number indicates we want a font with
	// this distance as the total line spacing, which makes them too small.
	lf.lfHeight = -MulDiv(pchrp->dympHeight, dpiY, kdzmpInch);
	lf.lfUnderline = false;
	lf.lfWidth = 0;			// default width, based on height
	lf.lfEscapement = 0;	// no rotation of text (is this how to do slanted?)
	lf.lfOrientation = 0;	// no rotation of character baselines

	lf.lfStrikeOut = 0;		// not strike-out
	lf.lfCharSet = DEFAULT_CHARSET;			// let name determine it; WS should specify valid
	lf.lfOutPrecision = OUT_TT_ONLY_PRECIS;	// only work with TrueType fonts
	lf.lfClipPrecision = CLIP_DEFAULT_PRECIS; // ??
	lf.lfQuality = DRAFT_QUALITY; // I (JohnT) don't think this matters for TrueType fonts.
	lf.lfPitchAndFamily = 0; // must be zero for EnumFontFamiliesEx
	wcscpy_s(lf.lfFaceName, LF_FACESIZE, pchrp->szFaceName);
	qzvpsWithWsAndFont->Unlock();

	HFONT hfont;
	hfont = AfGdi::CreateFontIndirect(&lf);
	if (!hfont)
		ThrowHr(WarnHr(gr::kresFail));

	AfGdi::SelectObjectFont(hdc, hfont);

	int tmAscent;
	int tmDescent;
	int tmEmHeight;

	OUTLINETEXTMETRIC otm;
	if (::GetOutlineTextMetrics(hdc, sizeof(OUTLINETEXTMETRIC), &otm))
	{
		tmAscent = otm.otmAscent;
		tmDescent = -otm.otmDescent;
		tmEmHeight = otm.otmsCapEmHeight;
	}
	else
	{
		TEXTMETRIC tm;
		if (::GetTextMetrics(hdc, &tm))
		{
			tmAscent = tm.tmAscent;
			tmDescent = tm.tmDescent;
			// This is not guaranteed to be the EM height because it assumes the internal leading is
			// all above the baseline. This is typically mostly true for many fonts.
			tmEmHeight = tmAscent - tm.tmInternalLeading;
		}
		else
		{
			// We can't get the information in a useful way, so just guess. (TE-6764)
			// The calculations are based on the font values we tested at 10 and 20 point
			// font sizes. Hopefully this will create a result that looks fairly accurate.
			tmAscent = (int)(abs(lf.lfHeight) * 0.7);
			tmDescent = (int)(abs(lf.lfHeight) * 0.25);
			tmEmHeight = (int)(abs(lf.lfHeight) * 0.5);
		}
	}
	AfGdi::DeleteObjectFont(hfont);
	AfGdi::ReleaseDC(NULL, hdc);
#else

	int dpiY = GetDpiY(NULL);

	// TODO-Linux: Only the fallback route has been ported like done for (TE-6764)
	LgCharRenderProps * pchrp = qzvpsWithWsAndFont->Chrp();

	int height = -MulDiv(pchrp->dympHeight, dpiY, kdzmpInch);

	qzvpsWithWsAndFont->Unlock();

	int tmAscent;
	int tmDescent;
	int tmEmHeight;

	if(!pzvpsLeaf->DropCaps())
	{
		IVwGraphicsWin32Ptr qvg;
		qvg.CreateInstance(CLSID_VwGraphicsWin32);
		qvg->Initialize(NULL);
		CheckHr(qvg->SetupGraphics(pchrp));
		qvg->get_FontAscent(&tmAscent);
		qvg->get_FontDescent(&tmDescent);

		// TODO-Linux: (FWNX-80) we have to implement this to get the LBearing
		// from the font matrix. For now internaleading or LBearing is defaulted to 0
		int tmInternalLeading = 0;
		tmEmHeight = tmAscent - tmInternalLeading;
	}
	else
	{
		tmAscent = (int)(abs(height) * 0.7);
		tmDescent = (int)(abs(height) * 0.25);
		tmEmHeight = (int)(abs(height) * 0.5);
	}

	Assert(tmEmHeight > 0);

#endif

	int dympAscent = MulDiv(tmAscent, kdzmpInch, dpiY);
	int dympDescent = MulDiv(tmDescent, kdzmpInch, dpiY);
	if (pdympAscent)
		*pdympAscent = dympAscent;
	if (pdympDescent)
		*pdympDescent = dympDescent;
	if (pdympEmHeight)
		*pdympEmHeight = MulDiv(tmEmHeight, kdzmpInch, dpiY);
	if (ExactLineHeight())
		return LineHeight();
	int dympLineHeight = LineHeight();
	if (dympLineHeight < dympAscent + dympDescent)
		dympLineHeight = dympAscent + dympDescent;
	return dympLineHeight;
}

/*----------------------------------------------------------------------------------------------
	This is called for a 'leaf' property store when it is about to be used for actual
	rendering. It figures out what chrp should actually be passed to the writing system
	to figure out what renderer to use, and is then passed on to the renderer itself to do
	the actual drawing.
	Currently this means simplifying the weight information to a true/false value for bold;
	fill in the direction and depth fields in the chrp;
	and get the writing system to interpret any magic font names.
	Also, if DropCaps is on, figure a modified font size.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::InitChrp()
{
	Assert(m_fLocked); // Must be locked, we won't figure it again.

	EnsureWritingSystemFactory();

	m_chrp.ttvBold = (byte)((m_nWeight > 400) ? kttvForceOn : kttvOff);

	// All the above is trivial. However, finding a real font name is a bit more
	// interesting...theoretically the views code allows us to have a comma-separated
	// list of fonts here, in which case we should use the first one installed...
	// for now we just use the first one if we find a list.
	const wchar * pch = m_stuFontFamily;
	for ( ; ; )
	{
		pch = StrUtil::SkipLeadingWhiteSpace(pch);

		const wchar * pchStartName = pch;
		while (*pch && !(*pch == ','))
			pch++;
		if (pch == pchStartName)
		{
			// Got no useable font name. Substitute <default> and let the WS
			// find a useable font.
			static OleStringLiteral defaultName(L"<default>");
			wcscpy_s(m_chrp.szFaceName, LF_FACESIZE, defaultName); // TODO: should this use FwStyledText::FontDefaultMarkup()?

			break;
		}
		// If we get here we have either a valid, installed font name, or a
		// standard magic name. In either case, just use it.
		int cchCopy = pch - pchStartName;
		if (cchCopy > 31)
			cchCopy = 31;
		wcsncpy_s(m_chrp.szFaceName, 32, pchStartName, cchCopy);
		m_chrp.szFaceName[cchCopy] = 0; // ensure null termination
		break;
	}
	// Figure the ws direction and note it in the chrp.

	ILgWritingSystemPtr qws;
	if (!m_chrp.ws)
		CheckHr(m_qwsf->get_UserWs(&m_chrp.ws));	// Get default writing system id.
	Assert(m_chrp.ws);
	CheckHr(m_qwsf->get_EngineOrNull(m_chrp.ws, &qws));
	AssertPtr(qws);
	ComBool fRtl;
	CheckHr(qws->get_RightToLeftScript(&fRtl));
	m_chrp.fWsRtl = (bool)fRtl;
	m_chrp.nDirDepth = (fRtl) ? 1 : 0;

	// Interpret any magic font names.
	CheckHr(qws->InterpretChrp(&m_chrp));

	// Other fields in m_chrp have the exact same meaning as the corresponding property,
	// and are already used to store it.

	if (DropCaps())
	{
		// If we're not careful here, we can get into an infinite recursion, because
		// AdjustedLineHeight() calls Chrp() on a new property store, which calls InitChrp(),
		// which can repeat until the stack overflows and the program silently disappears.
		// See LT-8904 for how this can be triggered.
		// If the parent property store already has the ws and font family set, and is also set
		// for "Drop Caps", then it also already has the dympHeight set properly.
		if (!m_pzvpsParent->DropCaps() ||
			m_pzvpsParent->m_chrp.ws == 0 ||
			m_pzvpsParent->m_stuFontFamily.Length() == 0)
		{
			int dympAscent;
			int dympDescent;
			// The EM Height is the ascent minus portion of the 'Internal Leading' that is above the baseline
			// (Many/most/all TrueType fonts just report this as the otmEMSquare value)
			int dympEmHeight;

			int dympLineHeight = m_pzvpsParent->AdjustedLineHeight(this,
				&dympAscent, &dympDescent, &dympEmHeight);

			// Adding the baseline separation gives the desired internal ascent of the drop cap.
			int dympDropCapEmHeight = dympEmHeight + dympLineHeight;
			int dympDropCapDescent = dympDescent * dympDropCapEmHeight / dympEmHeight;
			// when we request a font height, we request the total height (ascent + descent), minus the
			// internal leading
			int dympDropCapHeightToRequest = dympDropCapEmHeight + dympDropCapDescent;
			// Using that as the 'point size' of the font may make it look about right...
			// But it doesn't, so we need to do two more things:
			// First, we observe that some fonts don't return a physical font having the requested logical size, so
			// we calculate a factor that takes this into account.
			// Second, we use a universal fudge factor that makes everything right...
#if WIN32
			HDC hdc = AfGdi::GetDC(NULL);
			int dpiY = ::GetDeviceCaps(hdc, LOGPIXELSY);
#else
			int dpiY = GetDpiY(NULL);
#endif
			double requestedToReceivedRatio = m_pzvpsParent->m_chrp.dympHeight * kdzmpInch / (dpiY * 1000.0 * (dympDescent + dympEmHeight));
#if WIN32
			AfGdi::ReleaseDC(NULL, hdc);
#endif
			double fudgeFactor = 1.125;
			m_chrp.dympHeight = (int)(dympDropCapHeightToRequest * fudgeFactor * requestedToReceivedRatio);
		}
		else
		{
			Assert(m_chrp.ws == m_pzvpsParent->m_chrp.ws);
			Assert(m_stuFontFamily == m_pzvpsParent->m_stuFontFamily);
		}
		// Our goal here is to use the information obtained above to figure
		// m_chrp.dympHeight, which is the ascent in millipoints (mp) of the
		// drop caps font.
		// We'd basically like this to fill the space from the top of the paragraph
		// down to the baseline of the second line. That's one complete line plus
		// the font ascent.
		// This '1.7 * line height plus ascent' has worked best so far.
		//m_chrp.dympHeight = dympLineHeight * 170 / 100 + dympAscent;
		//m_chrp.dympHeight = dympLineHeight + dympAscent;
		//int dmpHeight1 = dympLineHeight + dympAscent;
		//// However, we also want to try to avoid a situation where the ascent plus
		//// descent of the drop cap is greater than twice the line height. The ascent
		//// we compute here is one that would make the drop cap's total height (including
		//// descenders) two full lines at the paragraph default font size.
		//int dmpHeight2 = dympLineHeight * 2 * dympAscent / (dympAscent + dympDescent);
		//// The min of those seems to give the best overall result.

		//// Review: consider taking the max of this and the original height, currently stored
		//// in m_chrp.dympHeight.
		//m_chrp.dympHeight = min(dmpHeight1, dmpHeight2);
	}

	m_fInitChrp = true; // last, once we got it all figured.
}

void VwPropertyStore::GetUnderlineInfo(int * punt, COLORREF * pclr)
{
	*punt = m_chrp.m_unt;
	*pclr = m_chrp.m_clrUnder;
}

//:>********************************************************************************************
//:>	IVwComputedProperty Interface Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Tells (from the writing system) whether it is necessary to use multiple distinct fonts to
	render different ranges of code points for the current writing system.  Delegated to the
	writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_UsesMultipleFonts(ComBool *pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf);
#if 0 // ENHANCE: reinstate something like this if we implement writing systems that use
	// multiple fonts.
	if (!m_pwse) {
		Warn("premature use of ws-dependent method");
		return E_UNEXPECTED;
	}
	IRenderEngine * preneng;
	IgnoreHr(hr = m_pwse->Renderer(&preneng));
	if (FAILED(hr))
	{
		Warn("writing system w/o renderer");
		return E_FAIL;
	}
	return preneng->get_UsesMultipleFonts(pf);
#else // placeholder, delete eventually
	*pf = FALSE;
#endif // placeholder
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

		// For greater efficiency the most common properties have
		// individual methods.
/*----------------------------------------------------------------------------------------------
	Get the font name(s) to use to display text using these computed properties.
	@param pbstr A comma-separated list of fonts
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_FontFamily(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);

	if (m_stuFontFamily.Length() == 0)
	{
		static OleStringLiteral serif(L"<serif>");
		*pbstr = SysAllocString(serif); // last ditch default
		if (*pbstr)
			return S_OK;
		else
			return E_OUTOFMEMORY;
	}
	CopyBstr(pbstr, m_stuFontFamily.Bstr());

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Normal, italic, slanted
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_FontStyle(ComBool * pf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pf);
	*pf = m_chrp.ttvItalic;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Get how bold to make the font
	@param pnWeight A number from 0 to 1000 where 400 is normal, 700 bold
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_FontWeight(int * pnWeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnWeight);
	*pnWeight = m_nWeight;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Get the number of requests for bolder (or -ve, lighter). We can't apply this info at once,
	because it is the actual text property thing that works out which fonts are even available.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_FontWeightIncrements(int * pcactBolder)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcactBolder);
	*pcactBolder = m_cactBolder;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	The font size to try for
	@param pnP1000 In thousandths of a point
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_FontSize(int * pnP1000)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnP1000);
	*pnP1000 = m_chrp.dympHeight;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Get the superscripting to use (-ve for subscript)
	@param pnP1000 In thousandths of a point
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_FontSuperscript(int * pnP1000)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnP1000);
	*pnP1000 = m_chrp.dympOffset;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Get whether the old writing system is primarily right-to-left
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_RightToLeft(ComBool * pfRet)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfRet);
	*pfRet = m_fRightToLeft;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Get how many levels of direction change are implied by all the properties here
	@param pn # of embedded direction changes
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_DirectionDepth(int * pn)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pn);
	*pn = m_chrp.nDirDepth;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	This is a property that allows special customizations of rendering with WinRend and
	GX fonts (when implmented).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_FontVariations(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	CopyBstr(pbstr, m_stuFontFamily.Bstr());
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Color used for text and lines (including borders of figures)
	@param pnRGB RGB color value
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_ForeColor(int * pnRGB)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnRGB);
	*pnRGB = m_chrp.clrFore;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Color used for text background and figure fill
	@param pnRGB RGB color value or kclrTransparent
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_BackColor(int * pnRGB)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnRGB);
	*pnRGB = m_chrp.clrBack;
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Read any int propery
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_IntProperty(int nID, int * pnValue)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pnValue);

	switch(nID)
	{
	case ktptAlign:
		*pnValue = m_ta;
		break;
	case ktptItalic:
		if (m_chrp.ttvItalic == kttvInvert)
		{
			// NOTE:  This code will probably not get called since
			//        VwPropertyStore::put_IntProperty() case kttvInvert:
			//        no longer allows m_chrp.ttvItalic to be set to kttvInvert.
			VwPropertyStore * pvps = m_pzvpsParent;
			int iInv = 1;
			while ((pvps->m_chrp.ttvItalic == kttvInvert) && (iInv < 100))
			{
				iInv++;  // count the inversions
				pvps = pvps->m_pzvpsParent;
			}
			Assert(iInv <= 1);
			int tmp = pvps->m_chrp.ttvItalic;
			for (int i=0; i < iInv; i++)
			{
				// invert
				if (tmp == kttvOff)
					tmp = kttvForceOn;
				else
					tmp = kttvOff;
			}
			*pnValue = tmp;
		}
		else
			*pnValue = m_chrp.ttvItalic;
		break;
	case ktptBold:
//		Assert(m_chrp.fBold == (m_nWeight > 550)); // can happen, but shouldn't
		*pnValue = m_nWeight;
//		*pnValue = m_chrp.fBold ? kttvForceOn : kttvOff;
		break;
	case ktptFontSize:
		*pnValue = m_chrp.dympHeight;
		break;
	case ktptSuperscript:
		*pnValue = m_chrp.ssv;
		break;
	case ktptOffset:
		*pnValue = m_chrp.dympOffset;
		break;
	case ktptUnderline:
		*pnValue = m_chrp.m_unt;
		break;
	case ktptUnderColor:
		*pnValue = m_chrp.m_clrUnder;
		break;
	case ktptRightToLeft:
		*pnValue = m_fRightToLeft;
		break;
	case ktptDirectionDepth:
		*pnValue = m_chrp.nDirDepth;
		break;
	case ktptMswMarginTop:
		*pnValue = m_mpMswMarginTop;
		break;
	case ktptMarginTop:
		*pnValue = m_mpMarginTop;
		break;
	case ktptMarginBottom:
		*pnValue = m_mpMarginBottom;
		break;
	case ktptMarginLeading:
		*pnValue = m_mpMarginLeading;
		break;
	case ktptMarginTrailing:
		*pnValue = m_mpMarginTrailing;
		break;
	case ktptPadTop:
		*pnValue = m_mpPadTop;
		break;
	case ktptPadBottom:
		*pnValue = m_mpPadBottom;
		break;
	case ktptPadLeading:
		*pnValue = m_mpPadLeading;
		break;
	case ktptPadTrailing:
		*pnValue = m_mpPadTrailing;
		break;
	case ktptBorderTop:
		*pnValue = m_mpBorderTop;
		break;
	case ktptBorderBottom:
		*pnValue = m_mpBorderBottom;
		break;
	case ktptBorderLeading:
		*pnValue = m_mpBorderLeading;
		break;
	case ktptBorderTrailing:
		*pnValue = m_mpBorderTrailing;
		break;
	case ktptBulNumScheme:
		*pnValue = m_vbnBulNumScheme;
		break;
	case ktptBulNumStartAt:
//		*pnValue = m_nNumStartAt;	WRONG! INT_MIN is not suitable for XML export!
		*pnValue = m_nNumStartAt == INT_MIN ? 0 : m_nNumStartAt;
		break;
	case ktptForeColor:
		*pnValue = m_chrp.clrFore;
		break;
	case ktptParaColor: // functions much like back color; kludge.
	case ktptBackColor:
		*pnValue = m_chrp.clrBack;
		break;
	case ktptBorderColor:
		*pnValue = m_clrBorderColor;
		break;
	case ktptFirstIndent:
		*pnValue = m_mpFirstIndent;
		break;
	case ktptLineHeight:
		*pnValue = m_mpLineHeight;
		break;
	case ktptRelLineHeight:
		*pnValue = m_nRelLineHeight;
		break;
	case ktptKeepWithNext:
		*pnValue = m_fKeepWithNext;
		break;
	case ktptKeepTogether:
		*pnValue = m_fKeepTogether;
		break;
	case ktptWidowOrphanControl:
		*pnValue = m_fWidowOrphanControl;
		break;
	case ktptHyphenate:
		*pnValue = m_fHyphenate;
		break;
	case ktptMaxLines:
		*pnValue = m_nMaxLines;
		break;
	case ktptEditable:
		*pnValue = m_fEditable;
		break;
	case ktptBaseWs:
		*pnValue = m_wsBase;
		break;
	case ktptSpellCheck:
		*pnValue = m_smSpellMode;
		break;
	case ktptTableRule:
		return E_FAIL;
	default:
		StrAnsi sta;
		sta.Format("invalid ID %d for reading int property", nID);
		Warn(sta.Chars());
		return E_NOTIMPL;
	}

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Read any string property
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_StringProperty(int nID, BSTR * bstrValue)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(bstrValue);

	switch(nID)
	{
	case ktptFontFamily:
		CopyBstr(bstrValue, m_stuFontFamily.Bstr());
		break;
	case ktptWsStyle:
		CopyBstr(bstrValue, m_stuWsStyle.Bstr());
		break;
	case ktptFontVariations:
		CopyBstr(bstrValue, m_stuFontVariations.Bstr());
		break;
	case ktptBulNumTxtBef:
		CopyBstr(bstrValue, m_stuNumTxtBef.Bstr());
		break;
	case ktptBulNumTxtAft:
		CopyBstr(bstrValue, m_stuNumTxtAft.Bstr());
		break;
	case ktptBulNumFontInfo:
		CopyBstr(bstrValue, m_stuNumFontInfo.Bstr());
		break;
	default:
		StrAnsi sta;
		sta.Format("invalid ID %d for reading string property", nID);
		Warn(sta.Chars());
		ThrowHr(WarnHr(E_NOTIMPL));
	}

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}


//:>********************************************************************************************
//:>	IVwPropertyStore Interface Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Gets an ITsTextProps interfce pointer on a TsTextProps object whose properties are
	those currently in the store. In particular, if the current VwPropertyStore has just
	been created and not changed, the properties will be the system default properties.
----------------------------------------------------------------------------------------------*/

STDMETHODIMP VwPropertyStore::get_TextProps(ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppttp);

	HRESULT hr;
	ITsPropsBldrPtr qtpb;

	struct IntPropsInfo
	{
		int tpt;
		int nvar;
	};

	// This information is presented like this because of numerous gaps in what might be a
	// continuous range of integer text property ids, added to the fact that we need to add
	// variation information in any case, and this is not easily predictable.
	// These values are the ones handled in the case statements of the get_IntProperty method.
	static const IntPropsInfo rgipi[] =
	{
		ktptItalic,				ktpvEnum,		// 2
		ktptBold,				ktpvEnum,		// 3
		ktptSuperscript,		ktpvEnum,		// 4
		ktptUnderline,			ktpvEnum,		// 5
		ktptFontSize,			ktpvMilliPoint,	// 6
		ktptOffset,				ktpvMilliPoint,	// 7
		ktptForeColor,			ktpvDefault,	// 8
		ktptBackColor,			ktpvDefault,	// 9
		ktptUnderColor,			ktpvDefault,	// 10
		ktptAlign,				ktpvEnum,		// 17
		ktptFirstIndent,		ktpvMilliPoint,	// 18
		ktptLeadingIndent,		ktpvMilliPoint,	// 19, same as ktptMarginLeading
		ktptTrailingIndent,		ktpvMilliPoint,	// 20, same as ktptMarginTrailing
		ktptSpaceBefore,		ktpvMilliPoint,	// 21, same as ktptMswMarginTop
		ktptSpaceAfter,			ktpvMilliPoint,	// 22, same as ktptMarginBottom
		ktptLineHeight,			ktpvMilliPoint,	// 24
		ktptParaColor,			ktpvDefault,	// 25
		ktptMswMarginTop,		ktpvMilliPoint, // 26
		ktptRightToLeft,		ktpvEnum,		// 128
		ktptDirectionDepth,		ktpvDefault,	// 129
		ktptMarginLeading,		ktpvMilliPoint,	// 134
		ktptMarginTrailing,		ktpvMilliPoint,	// 135
		ktptMarginTop,			ktpvMilliPoint,	// 136
		ktptMarginBottom,		ktpvMilliPoint,	// 137
		ktptBorderTop,			ktpvMilliPoint,	// 138
		ktptBorderBottom,		ktpvMilliPoint,	// 139
		ktptBorderLeading,		ktpvMilliPoint,	// 140
		ktptBorderTrailing,		ktpvMilliPoint,	// 141
		ktptBorderColor,		ktpvDefault,	// 142
		ktptBulNumScheme,		ktpvEnum,		// 143
		ktptBulNumStartAt,		ktpvDefault,	// 144
		ktptKeepWithNext,		ktpvEnum,		// 148
		ktptKeepTogether,		ktpvEnum,		// 149
		ktptHyphenate,			ktpvEnum,		// 150
		ktptMaxLines,			ktpvDefault,	// 151
		ktptEditable,			ktpvEnum,		// 155
		ktptRelLineHeight,		ktpvMilliPoint,	// 160
		ktptTableRule,			ktpvEnum,		// 161
		ktptWidowOrphanControl,	ktpvEnum,		// 162
	};
	const int kctptIntTotal = isizeof(rgipi) / isizeof(IntPropsInfo);

	static const int rgtptString[] =
	{
		ktptFontFamily,			// 1
		ktptFontVariations,		// 130
		ktptBulNumTxtBef,		// 145
		ktptBulNumTxtAft,		// 146
		ktptBulNumFontInfo,		// 147
		ktptWsStyle				// 156
	};
	const int kctptStrTotal = isizeof(rgtptString) / isizeof(int);

	// Create a TsPropsBldr with no properties.
	qtpb.CreateInstance(CLSID_TsPropsBldr);

	// Get the int properties using get_IntProperty(nId, pnValue).
	// Using this means that any changes to get_IntProperty will be inherited here.
	// Put the values, with the associated variations, into the builder.
	int iipi;
	int nval;
	for (iipi = 0; iipi < kctptIntTotal; ++iipi)
	{
		hr = get_IntProperty(rgipi[iipi].tpt, &nval);
		if (SUCCEEDED(hr))
			qtpb->SetIntPropValues(rgipi[iipi].tpt, rgipi[iipi].nvar, nval);
	}

	// Get the string properties using get_StringProperty(nid, pbstr).
	int itpt;
	SmartBstr sbstrValue;
	for (itpt = 0; itpt < kctptStrTotal; ++itpt)
	{
		hr = get_StringProperty(rgtptString[itpt], &sbstrValue);
		if (SUCCEEDED(hr))
			qtpb->SetStrPropValue(rgtptString[itpt], sbstrValue);
	}

	// Now make a TsTextProps from the builder and return its ITsTextprops pointer.
	return qtpb->GetTextProps(ppttp);

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}


//:>********************************************************************************************
//:>	IVwPropertyStore Interface Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Record a property whose value is an int. Some properties have a 'variation' code indicating
	which of several methods is being used to set them.
	Should only be called by a rule being added to the tree by ComputedPropertiesFor...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::put_IntProperty(int tpt, int xpv, int nValue)
{
	BEGIN_COM_METHOD;
	if (m_fLocked)
		return E_UNEXPECTED;
	if ((tpt == ktptWs || tpt == ktptBaseWs) && (uint)nValue > kwsLim)
		ThrowInternalError(E_INVALIDARG, "Magic writing system invalid in string");

	// If the text property is more than 10,000 it is user defined: ignore it.
	if (tpt < 10000)
	{
		switch(tpt)
		{
		case ktptAlign:
			if (xpv != ktpvEnum)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_ta = (FwTextAlign) nValue;
			break;
		case ktptItalic:
			if (xpv != ktpvEnum)
				ThrowHr(WarnHr(E_NOTIMPL));
			switch(nValue)
			{
			case kttvForceOn:
				m_chrp.ttvItalic = kttvForceOn;
				break;
			case kttvOff:
				m_chrp.ttvItalic = kttvOff;
				break;
			case kttvInvert:
				if (m_chrp.ttvItalic == kttvOff)
					nValue = kttvForceOn; // was not italic
				else if (m_chrp.ttvItalic == kttvForceOn)
					nValue = kttvOff; // was italic
				else
				{
					Assert(false);
				}
				m_chrp.ttvItalic = nValue;
				break;
			default:
				ThrowHr(WarnHr(E_NOTIMPL));
			}
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, ktpvEnum, nValue);
			break;
		case ktptBold:
			if (xpv != ktpvEnum)
				ThrowHr(WarnHr(E_NOTIMPL));
			if (nValue >= 0)
			{
				m_cactBolder = 0;		// absolute value resets this
				// Commonly a member of the FwTextToggleVal in TextServe.idh
				if (nValue == kttvOff)
					nValue = kvfwNormal;
				else if (nValue == kttvForceOn)
					nValue = kvfwBold;
				else if (nValue == kttvInvert)
				{
					if (m_nWeight < 550)
						nValue = kvfwBold; // was not bold
					else
						nValue = kvfwNormal; // was bold
				}
				// Other options currently only used by view programming,
				// from VwFontWeights enumeration.
				else if (nValue > 900)
				{
					Warn("out of range font weight");
					nValue = 900;
				}
				if (nValue <100)
				{
					Warn("out of range font weight");
					nValue = 100;
				}
				m_nWeight = nValue;
			}
			else
			{
				if (kvfwBolder == nValue)
				{
					m_cactBolder++;
				}
				if (kvfwLighter == nValue)
				{
					m_cactBolder--;
				} else {
					ThrowHr(WarnHr(E_NOTIMPL));
				}
			}
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, ktpvEnum, nValue);
			break;
		case ktptFontSize:
			if (ktpvMilliPoint == xpv || ktpvDefault == xpv)
			{
				m_chrp.dympHeight = nValue;
			}
			else if (ktpvRelative == xpv)
			{
				m_chrp.dympHeight = m_chrp.dympHeight * nValue / kdenTextPropRel;
			}
			else if (xpv == ktpvEnum)
			{
				int i;
				if (kvfsSmaller == nValue)
				{
					// find the largest value smaller than current (if any);
					// otherwise, go for 33% smaller than current.
					for (i = 1; i <= kvfsXXLarge && g_rgnFontSizes[i] < m_chrp.dympHeight; i++)
						;
					int nNewSize = g_rgnFontSizes[i-1];
					if (nNewSize < m_chrp.dympHeight)
						m_chrp.dympHeight = nNewSize;
					else
						m_chrp.dympHeight = m_chrp.dympHeight * 2 / 3;
				}
				else if (kvfsLarger == nValue)
				{
					// try for next larger size in table, otherwise go 50% larger
					for (i = kvfsXXLarge - 1;
						i >= 0 && g_rgnFontSizes[i] > m_chrp.dympHeight;
						i++)
						;
					int nNewSize = g_rgnFontSizes[i+1];
					if (nNewSize > m_chrp.dympHeight)
						m_chrp.dympHeight = nNewSize;
					else
						m_chrp.dympHeight = m_chrp.dympHeight * 3 / 2;
				}
				else
				{
					if (nValue < 0 || nValue > kvfsXXLarge)
						ThrowHr(WarnHr(E_NOTIMPL));
					m_chrp.dympHeight = g_rgnFontSizes[nValue];
				}
			}
			else
			{
				ThrowHr(WarnHr(E_NOTIMPL));
			}
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, xpv, nValue);
			break;
		case ktptOffset:
			if (ktpvMilliPoint == xpv)
			{
				m_chrp.dympOffset = nValue;
			}
			else if (ktpvRelative == xpv)
			{
				VwPropertyStorePtr qvps;
				int nInheritSize =  knDefaultFontSize;
	//			qvps = Parent();
				if (m_pzvpsParent)
				{
					CheckHr(m_pzvpsParent->get_FontSize(&nInheritSize));
				}
				m_chrp.dympOffset = nInheritSize * nValue / kdenTextPropRel;
			}
			else {
				ThrowHr(WarnHr(E_NOTIMPL));
			}
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, xpv, nValue);
			break;
		case ktptSuperscript:
			// For most simple properties, if there is only one valid variation, we now allow
			// variation ktpvDefault to be ok. Apparently loadxml is using ktpvDefault in some of
			// these cases.
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_chrp.ssv = (byte)(FwSuperscriptVal) nValue;
			break;
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, xpv, nValue);
		case ktptUnderline:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			// ENHANCE: maybe check it is a member of the enumeration?
			m_chrp.m_unt = nValue;
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, xpv, nValue);
			break;
		case ktptUnderColor:
			if (xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_chrp.m_clrUnder = nValue;
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, xpv, nValue);
			break;
		case ktptRightToLeft:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_fRightToLeft = nValue ? TRUE : FALSE;
			break;
		case ktptForeColor:
			if (xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_chrp.clrFore = nValue;
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, xpv, nValue);
			break;
		case ktptParaColor: // functions as back color
		case ktptBackColor:
			if (xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_chrp.clrBack = nValue;
			FwStyledText::ZapWsStyle(m_stuWsStyle, tpt, xpv, nValue);
			break;
		case ktptBorderColor:
			if (xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_clrBorderColor = nValue;
			break;
		case ktptWs:
#if 1
			if (m_chrp.ws != nValue)
			{
				m_chrp.ws = nValue;
				Assert(m_chrp.ws);
				// Recompute m_chrp.fRtl and m_chrp.nDirDepth
				EnsureWritingSystemFactory();
				ILgWritingSystemPtr qws;
				CheckHr(m_qwsf->get_EngineOrNull(m_chrp.ws, &qws));
				AssertPtr(qws);
				ComBool fRtl;
				if (qws) // If by some chance we're trying to use an unknown WS, default to LTR.
					CheckHr(qws->get_RightToLeftScript(&fRtl));
				m_chrp.fWsRtl = (bool)fRtl;
				m_chrp.nDirDepth = (fRtl) ? 1 : 0;
			}
#endif
			break;
		case ktptBaseWs:
			m_wsBase = nValue;
			break;
		case ktptMswMarginTop:
			m_mpMswMarginTop = nValue;
			goto checkVariation;
		case ktptMarginTop:
			m_mpMarginTop = nValue;
			m_grfcsExplicitMargins = (CellsSides)((int)m_grfcsExplicitMargins | (int)kfcsTop);
			goto checkVariation;
		case ktptMarginBottom:
			m_mpMarginBottom = nValue;
			m_grfcsExplicitMargins = (CellsSides)((int)m_grfcsExplicitMargins | (int)kfcsBottom);
			goto checkVariation;
		case ktptMarginLeading:
			m_mpMarginLeading = nValue;
			m_grfcsExplicitMargins = (CellsSides)((int)m_grfcsExplicitMargins | (int)kfcsLeading);
			goto checkVariation;
		case ktptMarginTrailing:
			m_mpMarginTrailing = nValue;
			m_grfcsExplicitMargins = (CellsSides)((int)m_grfcsExplicitMargins | (int)kfcsTrailing);
			goto checkVariation;
		case ktptPadTop:
			m_mpPadTop = nValue;
			goto checkVariation;
		case ktptPadBottom:
			m_mpPadBottom = nValue;
			goto checkVariation;
		case ktptPadLeading:
			m_mpPadLeading = nValue;
			goto checkVariation;
		case ktptPadTrailing:
			m_mpPadTrailing = nValue;
			goto checkVariation;
		case ktptBorderTop:
			m_mpBorderTop = nValue;
			goto checkVariation;
		case ktptBorderBottom:
			m_mpBorderBottom = nValue;
			goto checkVariation;
		case ktptBorderLeading:
			m_mpBorderLeading = nValue;
			goto checkVariation;
		case ktptBorderTrailing:
			m_mpBorderTrailing = nValue;
			goto checkVariation;
		case ktptBulNumScheme:
			Assert(nValue >= 0);
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_vbnBulNumScheme = nValue;
			break;
		case ktptBulNumStartAt:
			if (xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_nNumStartAt = nValue;
			break;
		case ktptFirstIndent:
			m_mpFirstIndent = nValue;
			goto checkVariation;
		case ktptLineHeight:
			if (ktpvRelative == xpv)
			{
				VwPropertyStorePtr qvps;
				int nFontSize;
				if (m_wsBase == 0)
					nFontSize = 10; // a sort of generic default (useful e.g. in ChrpFor).
				else
					nFontSize =  FontSizeForWs(m_wsBase);
				// The height needed to handle the font size is larger than the font size itself.
				// ENHANCE (SharonC): Ideally we should use a graphics device to measure the size
				// that would be generated for the given font size.
				int nLnHt = (int)(nFontSize * 1.4);
				// The following is left over from an attempt to follow the CSS model, but
				// it doesn't seem to fit with the way we're doing things:
				// More precisely, the problem is that if a single TsTextProps invokes a style that
				// sets a font/size for the default old writing system, and then a relative line height,
				// the user expects the line height to be based on the local font setting; but of
				// course it isn't set in the parent.
				// Because of this, it's important that anything that could affect the result of
				// FontSizeForWs happens before the line height is set.
//				CheckHr(get_ParentStore(&qvps));
//				if (qvps)
//				{
//					CheckHr(qvps->get_FontSize(&nFontSize));
//				}
				m_mpLineHeight = nLnHt * nValue / 10000;
				m_nRelLineHeight = nValue;
				break;
			}
			m_mpLineHeight = nValue;
			m_nRelLineHeight = 0;
			goto checkVariation;
		case ktptCellBorderWidth:
			m_mpTableBorder = nValue;
			goto checkVariation;
		case ktptCellSpacing:
			m_mpTableSpacing = nValue;
			goto checkVariation;
		case ktptCellPadding:
			m_mpTablePadding = nValue;
			goto checkVariation;
checkVariation:
			if (xpv != ktpvMilliPoint && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			break;
		case ktptTableRule:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_vwrule = (VwRule) nValue;
			break;
		case ktptKeepWithNext:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_fKeepWithNext = nValue ? TRUE : FALSE;
			break;
		case ktptKeepTogether:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_fKeepTogether = nValue ? TRUE : FALSE;
			break;
		case ktptWidowOrphanControl:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_fWidowOrphanControl = nValue ? TRUE : FALSE;
			break;
		case ktptHyphenate:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_fHyphenate = nValue ? TRUE : FALSE;
			break;
		case ktptMaxLines:
			if (xpv != ktpvDefault && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
			if (nValue < 0)
				nValue = 0;
			m_nMaxLines = nValue;
			break;
		case ktptEditable:
			if (xpv != ktpvEnum && xpv != ktpvDefault)
				ThrowHr(WarnHr(E_NOTIMPL));
//			m_fEditable = nValue ? true : false;
			m_fEditable = (TptEditable) nValue;
			break;
		case ktptSpellCheck:
			if (xpv != ktpvEnum)
				ThrowHr(WarnHr(E_NOTIMPL));
			m_smSpellMode = (SpellingModes) nValue;
			break;
		default:
			ThrowHr(WarnHr(E_NOTIMPL));
			break;
		}
	}

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Record a property whose value is a string. Some properties have a 'variation' code
	indicating which of several methods is being used to set them.
	Should only be called by a rule being added to the tree by ComputedPropertiesFor...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::put_StringProperty(int sp, BSTR bstrValue)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrValue);

	StrUni stuTmp;
	int max;

	if (m_fLocked)
		ThrowHr(WarnHr(E_UNEXPECTED));
	switch(sp)
	{
	case ktptFontFamily:
		m_stuFontFamily = bstrValue;
		break;
	case ktptWsStyle:
		// MUST use Assign; value may contain significant nulls.
		if (m_stuWsStyle.Length())
		{
			// Need to merge
			SmartBstr sbstr;
			FwStyledText::ComputeWsStyleInheritance(m_stuWsStyle.Bstr(), bstrValue, sbstr);
			m_stuWsStyle.Assign(sbstr.Bstr(), sbstr.Length());
		}
		else
		{
			m_stuWsStyle.Assign(bstrValue, BstrLen(bstrValue));
		}
		break;

	case ktptFontVariations:
		// Append font variations strings.
		// TODO (SharonC): clean up this mechanism. Right now the concatenation approach can
		// easily overflow the szFontVar buffer.
		if (m_stuFontVariations.Length())
			m_stuFontVariations += L",";
		stuTmp = bstrValue;
		m_stuFontVariations += stuTmp;
		stuTmp = m_stuFontVariations;
		max = isizeof (m_chrp.szFontVar);
		while (stuTmp.Length() >= (isizeof(m_chrp.szFontVar) / isizeof(OLECHAR)))
		{
			// Pretruncate to avoid overflow.
			int ichComma = stuTmp.FindCh(L',', 0);
			stuTmp = stuTmp.Right(stuTmp.Length() - ichComma - 1);
		}
		wcscpy_s(m_chrp.szFontVar, 64, stuTmp.Chars());
		break;
	case ktptTags:
		m_stuTags = bstrValue;
		break;
	case ktptNamedStyle:
		if (BstrLen(bstrValue) && m_qss)
		{
			// For now, drop caps is invoked by using a particular known named style.
			static OleStringLiteral chapterNumber(L"Chapter Number");
			m_fDropCaps = (u_strcmp(chapterNumber, bstrValue) == 0);
			// Ttp invokes a named style. Apply it.
			ITsTextPropsPtr qttpNamed;
			CheckHr(m_qss->GetStyleRgch(BstrLen(bstrValue), bstrValue, &qttpNamed));
			if (qttpNamed)
			{
				SmartBstr sbstr;
				// Make sure it does not also invoke something named! Could lead to loop.
				Debug(CheckHr(qttpNamed->GetStrPropValue(ktptNamedStyle, &sbstr)));
				Assert(!sbstr);
				ApplyTtp(qttpNamed);
				// Don't apply Ws styles here; no way we can do this after a writing system
				// is specified.
			}
		}
		break;
	case ktptBulNumTxtBef:
		m_stuNumTxtBef = bstrValue;
		break;
	case ktptBulNumTxtAft:
		m_stuNumTxtAft = bstrValue;
		break;
	case ktptBulNumFontInfo:
		// Be sure to use Assign, it may contain nulls.
		m_stuNumFontInfo.Assign(bstrValue, BstrLen(bstrValue));
		break;
	default:
		// Quietly igore other properties. This is helpful for both forwards compatibility,
		// and for properties like ktptObjData which we don't care about in the prop store.
		break;
	}
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Add an entry to the map used to build a tree in which the nodes are VwPropertyStores,
	and the arcs are labelled with text properties. The child at the end of an arc is obtained
	by applying the rule to the parent. When first called with a given argument, clone self,
	apply rule to clone, and add it to the tree. Subsequent calls with the same argument
	return the object created the first time.
	May also be called with pttp null, in which case, it return the recipient.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_DerivedPropertiesForTtp(ITsTextProps * pttp,
	IVwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pttp);
	ChkComOutPtr(ppvps);
	VwPropertyStorePtr qzvps;
	CheckHr(ComputedPropertiesForTtp(pttp, &qzvps));
	*ppvps = qzvps.Detach();
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Add an entry to the map used to build a tree in which the nodes are VwPropertyStores,
	and the arcs are labelled with text properties. The child at the end of an arc is obtained
	by applying the rule to the parent. When first called with a given argument, clone self,
	apply rule to clone, and add it to the tree. Subsequent calls with the same argument
	return the object created the first time.
	May also be called with pttp null, in which case, it return the recipient.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::ComputedPropertiesForTtp(ITsTextProps * pttp,
	VwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pttp);
	ChkComOutPtr(ppvps);

	if (!pttp)
	{
		AddRefObj(this);
		*ppvps = this;
		return S_OK;
	}
	*ppvps = NULL;
	*ppvps = PropertiesForTtp(pttp);
	AddRefObj(*ppvps);
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

// Get an (un-ref-counted) pointer to the properties for a given ttp. Create if needed.
VwPropertyStore * VwPropertyStore::PropertiesForTtp(ITsTextProps * pttp)
{
	VwPropertyStorePtr qzvps;
	if (!m_hmttpzvps.Retrieve(pttp, qzvps))
	{
		qzvps.Attach(MakePropertyStore()); // ref count = 1
		qzvps->CopyFrom(this);

		// Now put the properties into effect
		qzvps->ApplyTtp(pttp);

		qzvps->Lock();
		m_hmttpzvps.Insert(pttp, qzvps); // ref count = 2
		qzvps->m_qttpKey = pttp; // keep a reference to the key
	}
	if (m_qwsf)
		qzvps->putref_WritingSystemFactory(m_qwsf);		// Better safe than sorry.

	return qzvps; // Don't use Detach; we are NOT giving a ref count
}

/*----------------------------------------------------------------------------------------------
	Answer the node, if any, from which this one was derived using ComputedPropertiesFor.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::get_ParentStore(IVwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvps);

	*ppvps = m_pzvpsParent;
	AddRefObj(*ppvps);
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	An arc in the property store tree can also be literal values applied to a VwEnv using
	methods.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::ComputedPropertiesForInt(int sp,
	int vpv, int nValue, VwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvps);

	VwPropertyStorePtr qzvps;
	IntPropKey ipk(sp, vpv, nValue);

	if (!m_hmipkzvps.Retrieve(ipk, qzvps))
	{
		qzvps.Attach(MakePropertyStore());
		qzvps->CopyFrom(this);
		CheckHr(qzvps->put_IntProperty(sp, vpv, nValue));
		qzvps->Lock();
		m_hmipkzvps.Insert(ipk, qzvps);
	}
	*ppvps = qzvps.Detach();

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Similarly an arc can be a literally set string property.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::ComputedPropertiesForString(int sp, BSTR bstrValue,
	VwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrValue);
	ChkComOutPtr(ppvps);

	// This is a little harder because we can't use the map class.
	VwPropertyStorePtr qzvps;
	// The WsStyle property can have embedded NUL characters, or even trailing NUL characters.
	// See FWR-2779 for what can happen without using the length to initialize the StrUni.
	StrUni suValue(bstrValue, BstrLen(bstrValue));

	StrPropRec sprKey(sp, suValue.Bstr(), NULL);

	bool found = false;
	int ispr;
	for (ispr = 0; ispr < m_vstrprrec.Size(); ++ispr)
	{
		if (m_vstrprrec[ispr] == sprKey)
		{
			found = true;
			break;
		}
	}

	if (found)
	{
		qzvps = m_vstrprrec[ispr].m_pzvps;
	}
	else
	{
		qzvps.Attach(MakePropertyStore());
		qzvps->CopyFrom(this);
		CheckHr(qzvps->put_StringProperty(sp, bstrValue));
		qzvps->Lock();
		sprKey.m_pzvps = qzvps;
		qzvps.Ptr()->AddRef();  // since it now referenced by the key
		// Add the current key to the table
		m_vstrprrec.Push(sprKey);
	}
	*ppvps = qzvps.Detach();

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	If this is the properties for a grouping flow object (other than a table), make a default
	one for flow objects embedded inside it by resetting all uninheritable properties.
	OPTIMIZE: it may be helpful to have it answer this (itself) if no uninheritable properties
	have been altered from the parent.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPropertyStore::ComputedPropertiesForEmbedding(VwPropertyStore ** ppvps)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvps);

	if (!m_qzvpsReset)
	{
		try
		{
			m_qzvpsReset.Attach(MakePropertyStore());
			m_qzvpsReset->CopyInheritedFrom(this);
			m_qzvpsReset->Lock();
		}
		catch (Throwable & thr)
		{
			return thr.Error();
		}
		catch (...)
		{
			ThrowHr(WarnHr(E_FAIL));
		}
	}
	AddRefObj(m_qzvpsReset.Ptr());
	*ppvps = m_qzvpsReset;

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

//:>********************************************************************************************
//:>	Other Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Make self a copy of its parent (and note the parent).
	Don't copy any of the tree structure.
	Copy only those properties that are inherited.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::CopyInheritedFrom(VwPropertyStore* pzvpsParent)
{
	m_stuFontFamily = pzvpsParent->m_stuFontFamily;
	m_stuWsStyle = pzvpsParent->m_stuWsStyle;
	m_chrp.ttvItalic = pzvpsParent->m_chrp.ttvItalic;
	m_ta = pzvpsParent->m_ta;
	m_smSpellMode = pzvpsParent->m_smSpellMode;
	m_nWeight = pzvpsParent->m_nWeight;
	m_cactBolder = pzvpsParent->m_cactBolder;
	m_chrp.dympHeight = pzvpsParent->m_chrp.dympHeight;
	m_chrp.dympOffset = pzvpsParent->m_chrp.dympOffset;
	m_chrp.ssv = pzvpsParent->m_chrp.ssv;
	m_chrp.m_unt = pzvpsParent->m_chrp.m_unt;
	m_chrp.m_clrUnder = pzvpsParent->m_chrp.m_clrUnder;
	m_fRightToLeft = pzvpsParent->m_fRightToLeft;
	m_chrp.nDirDepth = pzvpsParent->m_chrp.nDirDepth;
	m_stuFontVariations = pzvpsParent->m_stuFontVariations;
	StrUni stuTmp = m_stuFontVariations;
	while (stuTmp.Length() >= (isizeof(m_chrp.szFontVar) / isizeof(OLECHAR)))
	{
		// Pretruncate to avoid overflow.
		// TODO (SharonC): Rework.
		int ichComma = stuTmp.FindCh(L',', 0);
		stuTmp = stuTmp.Right(stuTmp.Length() - ichComma - 1);
	}
	wcscpy_s(m_chrp.szFontVar, 64, stuTmp.Chars());
	m_chrp.clrFore = pzvpsParent->m_chrp.clrFore;
	m_clrBorderColor = pzvpsParent->m_clrBorderColor;
	m_nMaxLines = pzvpsParent->m_nMaxLines;
	m_mpLineHeight = pzvpsParent->m_mpLineHeight;
	m_nRelLineHeight = pzvpsParent->m_nRelLineHeight;
	// Copy the map of old writing system overrides; not any of the other maps.
	m_qss = pzvpsParent->m_qss;
	m_qwsf = pzvpsParent->m_qwsf;

	m_fEditable = pzvpsParent->m_fEditable;
	m_fDropCaps = pzvpsParent->m_fDropCaps; // Should this really be inheritable??
	m_wsBase = pzvpsParent->m_wsBase;

	// don't copy this, init it from argument.
	// DON'T put a ref count on it, that will make an undeleteable cycle
	m_pzvpsParent = pzvpsParent;
}

/*----------------------------------------------------------------------------------------------
	Make self a copy of its parent (and note the parent).
	Don't copy any of the tree structure.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::CopyFrom(VwPropertyStore * pzvpsParent)
{
	CopyInheritedFrom(pzvpsParent);
	m_chrp.clrBack = pzvpsParent->m_chrp.clrBack;
	m_mpMarginTop = pzvpsParent->m_mpMarginTop;
	m_mpMswMarginTop = pzvpsParent->m_mpMswMarginTop;
	m_mpMarginBottom = pzvpsParent->m_mpMarginBottom;
	m_mpMarginLeading = pzvpsParent->m_mpMarginLeading;
	m_mpMarginTrailing = pzvpsParent->m_mpMarginTrailing;
	m_grfcsExplicitMargins = pzvpsParent->m_grfcsExplicitMargins;
	m_mpPadTop = pzvpsParent->m_mpPadTop;
	m_mpPadBottom = pzvpsParent->m_mpPadBottom;
	m_mpPadLeading = pzvpsParent->m_mpPadLeading;
	m_mpPadTrailing = pzvpsParent->m_mpPadTrailing;
	m_mpBorderTop = pzvpsParent->m_mpBorderTop;
	m_mpBorderBottom = pzvpsParent->m_mpBorderBottom;
	m_mpBorderLeading = pzvpsParent->m_mpBorderLeading;
	m_mpBorderTrailing = pzvpsParent->m_mpBorderTrailing;
	m_vbnBulNumScheme = pzvpsParent->m_vbnBulNumScheme;
	m_nNumStartAt = pzvpsParent->m_nNumStartAt;
	m_stuNumTxtBef = pzvpsParent->m_stuNumTxtBef;
	m_stuNumTxtAft = pzvpsParent->m_stuNumTxtAft;
	m_stuNumFontInfo = pzvpsParent->m_stuNumFontInfo;
	m_mpFirstIndent = pzvpsParent->m_mpFirstIndent;
	m_fKeepWithNext = pzvpsParent->m_fKeepWithNext;
	m_fKeepTogether = pzvpsParent->m_fKeepTogether;
	m_fWidowOrphanControl = pzvpsParent->m_fWidowOrphanControl;
	m_fHyphenate = pzvpsParent->m_fHyphenate;
	m_chrp.ws = pzvpsParent->m_chrp.ws;
	m_chrp.fWsRtl = pzvpsParent->m_chrp.fWsRtl;
	m_chrp.nDirDepth = pzvpsParent->m_chrp.nDirDepth;
	// QUESTION: Do we need to copy the rest of the m_chrp fields?

	// Don't need to copy table border, spacing, padding, because always set by the special
	// Ttp that causes them to be used.
}

/*----------------------------------------------------------------------------------------------
	The parent object is going away, so forget you ever had a parent.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::DisconnectParent()
{
	// Do NOT release it, we don't have a counted reference.
	m_pzvpsParent = NULL;
}
/*----------------------------------------------------------------------------------------------
	Answer this or the closest parent which is the m_qzvpsReset of its own parent...that is, the
	style to which the current style should reset when a flow object using this style closes.
	If no such parent, answer the top-most VwPropertyStore (the one with no parent).
----------------------------------------------------------------------------------------------*/
VwPropertyStore * VwPropertyStore::InitialStyle()
{
	VwPropertyStore * pzvpsInitial = this; // it might be this style itself that is a reset..
	VwPropertyStore * pzvpsParent;
	for ( ; ; )
	{
		pzvpsParent = pzvpsInitial->m_pzvpsParent;
		if (!pzvpsParent || pzvpsParent->m_qzvpsReset == pzvpsInitial)
			break;
		pzvpsInitial = pzvpsParent;
	}
	return pzvpsInitial;
}

STDMETHODIMP VwPropertyStore::putref_Stylesheet(IVwStylesheet * pss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pss);

	m_qss = pss;
	if (!pss)
		return S_OK; // using all default properties.
	ITsTextPropsPtr qttp;
	CheckHr(pss->get_NormalFontStyle(&qttp));
	if (qttp)
	{
		// If we allow this at all, we have to unlock, or it crashes immediately.
		Unlock();
		ApplyTtp(qttp);
		Lock();
	}
	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

STDMETHODIMP VwPropertyStore::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_factVps, IID_IVwPropertyStore);
}

/*----------------------------------------------------------------------------------------------
	Set up the default font-variation properties associated with the given writing system.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::DoWsDefaultFontVar(int ws)
{
	if (!m_qwsf)
		return; // Tests may fail here.
	ILgWritingSystemPtr qws;
	CheckHr(m_qwsf->get_EngineOrNull(ws, &qws));
	if (!qws)
		return; // Tests may fail here.
	SmartBstr sbstrFontFamily;
	get_FontFamily(&sbstrFontFamily);
	SmartBstr sbstrFeatures;
	CheckHr(qws->get_DefaultFontFeatures(&sbstrFeatures));
	if (sbstrFeatures)
		CheckHr(put_StringProperty(ktptFontVariations, sbstrFeatures));
}

/*----------------------------------------------------------------------------------------------
	Set up the properties associated with the given writing system/old writing system in the
	wsStyle string.
	NOTE: This method must be kept in sync with the FwStyledText functions.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::DoWsStyles(int ws)
{
	if (!m_stuWsStyle.Length())
		return;

	// Copy m_stuWsStyle to a temporary variable, so that put_IntProperty doesn't try
	// to change it out from underneath of us.
	StrUni stuWsStyle = m_stuWsStyle;
	m_stuWsStyle.Clear();

	const OLECHAR * pch = stuWsStyle.Chars();
	const OLECHAR * pchLim = pch + stuWsStyle.Length();
	while (pch < pchLim)
	{
		int wsCur;
		// The minimum size of a valid field is 4 chars: 2 for ws,
		// a length for the font name, if any; and a number of properties.
		if (pchLim - pch < 4)
			ThrowHr(WarnHr(E_UNEXPECTED));
		wsCur = *pch | (*(pch + 1)) << 16;
		pch += 2;
		const OLECHAR * pchFont = pch + 1;
		int cchFont = *pch;
		pch = pchFont + cchFont;
		if (pch >= pchLim)
			ThrowHr(WarnHr(E_UNEXPECTED));
		int cprop = SignedInt(*pch++);
		if (ws != wsCur)
		{
			// Not the entry we want--move on--unless we are past the one we want, then stop.
			// (Encs are stored in a numerical order that reflects their alphabetical order,
			// hence the unsigned int.)
			if ((unsigned int)ws < (unsigned int)wsCur)
				break;
			// Else skip this writing system.
			int cchStrProps = 0;
			if (cprop < 0)
			{
				// Additional string properties.
				cchStrProps = 1; // counter
				OLECHAR * pchTmp = const_cast<OLECHAR *>(pchFont) + cchFont + 1;
				for (; cprop < 0; cprop++)
				{
					int cch = *(pchTmp + 1);
					cchStrProps += cch + 2;
					pchTmp += cch + 2;
				}
				cprop = *pchTmp;
			}
			pch += cchStrProps + (cprop * 4);
			continue;
		}
		// Got the one we want!
		if (cchFont)
			m_stuFontFamily.Assign(pchFont, cchFont);
		if (cprop < 0)
		{
			// String properties.
			for (; cprop < 0; cprop++)
			{
				int tpt = *pch++;
				int cch = *pch++;
				StrUni stu(pch, cch);
				CheckHr(put_StringProperty(tpt, stu.Bstr()));
				pch += cch;
			}
			cprop = *pch++;
		}
		// Integer properties.
		for (; --cprop >= 0; )
		{
			int tpt = *pch++;
			int ttv = *pch++;
			int nVal = *pch | (*(pch + 1)) << 16;
			pch += 2;
			CheckHr(put_IntProperty(tpt, ttv, nVal));
		}
		break; // No point in searching further.
	}
}

/*----------------------------------------------------------------------------------------------
	This method is responsible for initializing the root property store's text props. It
	should only be called on the root property store. It uses the normal font style from
	the stylesheet as the default text props. It calls the specified view constructor
	to update the text props.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::InitRootTextProps(IVwViewConstructor * pvc)
{
	ITsTextPropsPtr qttp;
	if (m_qss)
		CheckHr(m_qss->get_NormalFontStyle(&qttp));
	// If we allow this at all, we have to unlock, or it crashes immediately.
	Unlock();
	SetInitialState();
	if (pvc)
	{
		if (!qttp)
			CheckHr(get_TextProps(&qttp));

		ITsTextPropsPtr qnttp;
		CheckHr(pvc->UpdateRootBoxTextProps(qttp, &qnttp));
		if (qnttp)
			ApplyTtp(qnttp);
		else
			ApplyTtp(qttp);
	}
	else if (qttp)
	{
		ApplyTtp(qttp);
	}
	Lock();	// root property store never changes (once stylesheet is set).
}

/*----------------------------------------------------------------------------------------------
	Apply the TsTextProps and determine what concrete style properties should result.
	In general, we just need to work through the properties in the ttp and invoke each of them.
	But there are a few complications. If a ttp invokes a named style, we want to apply the
	style so named before this one, so that the same ttp can both invoke a named style and
	override some of the settings from it.
	We also have to make use of information from the ktptWsStyle field. If this occurs in a ttp
	(e.g., a paragraph style ttp) which does not also specify an writing system and old writing
	system, we save it away as usual. But we have to get the right effect even in an extreme
	case such as a paragraph style which specifies a named style which specifies 10 point, then
	overrides it with an explicit 12-point, then within the paragraph there is a run which
	specfies a character style that (for this ws) specifies 14 pt, and finally the run ttp
	itself overrides with 16 pt.
	The problem is that we can't make use of the paragraph WS-dependent styles until we
	get into the run and know the old writing system. But logically an explicit font size set
	on the paragraph would override this. To get around this we just disallow explicit font
	properties on paragraph styles; they would be anomalous anyway, since generally such
	styles are dependent on old writing system to some extent.
	So, when we process the paragraph style, we detect the named style, and apply the paragraph
	style sheet ttp. This stores the string containing the ws-dependent properties.
	When we process the run ttp, the first step is to obtain the named style, and if there is
	one, we merge its effects with those of the paragraph style. Then, since it is a run ttp,
	it will have writing system information, which we use to look up the appropriate named
	style effects and apply them. Finally, we apply any explicit formatting in the ttp.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::ApplyTtp(ITsTextProps * pttp)
{
	ITsTextPropsPtr qttpNamed;
	int tpt; // property type
	int xpv; // variation
	SmartBstr sbstr; // value
	int nVal; // value
	// See if this is a "leaf" ttp that specifies an writing system and old writing system
	int ws;
	int nVar;
	HRESULT hr;
	CheckHr(hr = pttp->GetIntPropValues(ktptWs, &nVar, &ws));
	CheckHr(pttp->GetStrPropValue(ktptNamedStyle, &sbstr));
	if (sbstr)
		CheckHr(put_StringProperty(ktptNamedStyle, sbstr.Bstr()));
	// If we are a leaf node--an actual character run, which are the only ttps that have
	// writing system and old writing system specified--actually compute the effect of currently
	// active character style requests.
	if (hr == S_OK && ws != -1)
	{
		// It's a leaf node!
		// Font features may be specified in the writing system, in a style, or explicitly.
		// Explicit settings are appended to (and thus may override) ones from styles,
		// which similarly override ones from the writing system. To achieve this, we must
		// start with the writing system info, then add the style info, then (in the main loop
		// below) add any explicit font feature information for this run.
		DoWsDefaultFontVar(ws);
		DoWsStyles(ws);
		// Clear it out in case a more local named style wants to fill it in.
		m_stuWsStyle = L"";
	}
	int cpropInt;
	CheckHr(pttp->get_IntPropCount(&cpropInt));
	// Flag gets set true if we encounter prop ktptSetRowDefaults true
	bool fDoTableRowStuff = false;
	for (int ipropInt = 0; ipropInt < cpropInt; ++ipropInt)
	{
		CheckHr(pttp->GetIntProp(ipropInt, &tpt, &xpv, &nVal));
		switch (tpt)
		{
			case ktptSetRowDefaults:
				if (nVal)
					fDoTableRowStuff = true;
				break;

			case ktptLineHeight:
				// Do this last.
				break;

			case ktptDirectionDepth:
			case ktptRelLineHeight:
				// ignore any read-only text properties,
				// we don't want to try and put any read-only
				// properties
				break;

			default:
				CheckHr(put_IntProperty(tpt, xpv, nVal));
		}
	}

	int cpropStr;
	CheckHr(pttp->get_StrPropCount(&cpropStr));
	for (int ipropStr = 0; ipropStr < cpropStr; ++ipropStr)
	{
		CheckHr(pttp->GetStrProp(ipropStr, &tpt, &sbstr));
		// Named style was handled first (above). Don't repeat it.
		// Don't try to store NULL values.  See LT-6419.
		if (tpt != ktptNamedStyle && sbstr.Bstr() != NULL)
			CheckHr(put_StringProperty(tpt, sbstr));
	}

	// Do line height last, because it needs to have all the other information (particularly
	// font size) set up. (JohnT later: since we don't put explicit font size on the same
	// property store as line height, I think we only need to be sure that named style
	// stuff happens before integer properties. So the separation of this property may be
	// unnecessary. But it seemed safer to leave it in, in case there is some reason for the
	// change that I haven't realized.)
	CheckHr(hr = pttp->GetIntPropValues(ktptLineHeight, &xpv, &nVal));
	if (hr == S_OK)
		CheckHr(put_IntProperty(ktptLineHeight, xpv, nVal));

	if (fDoTableRowStuff)
	{
		// To produce a border between cells, we arbitrarily pick the left and top borders
		// to turn on, unless no rule has been requested in a particular direction.
		if (m_vwrule & kvrlRowNoGroups)
		{
			CheckHr(put_IntProperty(ktptBorderTop, ktpvMilliPoint, m_mpTableBorder));
			//CheckHr(put_IntProperty(ktptBorderBottom, ktpvMilliPoint, m_mpTableBorder));
		}
		if (m_vwrule & kvrlColsNoGroups)
		{
			CheckHr(put_IntProperty(ktptBorderLeading, ktpvMilliPoint, m_mpTableBorder));
			//CheckHr(put_IntProperty(ktptBorderTrailing, ktpvMilliPoint, m_mpTableBorder));
		}

		// Set default margins for the cells to produce the desired spacing between cells
		int mpMarginLeading = m_mpTableSpacing / 2;
		int mpMarginTrailing = m_mpTableSpacing - mpMarginLeading;

		CheckHr(put_IntProperty(ktptMarginTop, ktpvMilliPoint, mpMarginLeading));
		CheckHr(put_IntProperty(ktptMarginLeading, ktpvMilliPoint, mpMarginLeading));
		CheckHr(put_IntProperty(ktptMarginBottom, ktpvMilliPoint, mpMarginTrailing));
		CheckHr(put_IntProperty(ktptMarginTrailing, ktpvMilliPoint, mpMarginTrailing));

		// Set default padding to produce the desired padding inside the cells.
		CheckHr(put_IntProperty(ktptPadTop, ktpvMilliPoint, m_mpTablePadding));
		CheckHr(put_IntProperty(ktptPadLeading, ktpvMilliPoint, m_mpTablePadding));
		CheckHr(put_IntProperty(ktptPadBottom, ktpvMilliPoint, m_mpTablePadding));
		CheckHr(put_IntProperty(ktptPadTrailing, ktpvMilliPoint, m_mpTablePadding));

		// If it is the special default property set for table cells, this is the initial state
		// where no explicit formatting has been done.
		m_grfcsExplicitMargins = (CellsSides)0;
	}
}

/*----------------------------------------------------------------------------------------------
	Recompute the effects of this property store, and recursively fix its children. The caller
	is responsible for reinitializing the text props for the root property store.
----------------------------------------------------------------------------------------------*/
void VwPropertyStore::RecomputeEffects()
{
	// Set m_fInitChrp to false to recompute the actual character properties, m_chrp, when next
	// needed.
	m_fInitChrp = false;

	// Fix the property store obtained when uninheritable properties are reset.
	if (m_qzvpsReset)
	{
		bool fWasLocked = m_qzvpsReset->m_fLocked;
		if (fWasLocked)
			m_qzvpsReset->Unlock();
		m_qzvpsReset->CopyInheritedFrom(this);
		m_qzvpsReset->RecomputeEffects();
		if (fWasLocked)
			m_qzvpsReset->Lock();
	}

	// Fix each vps in the map used to record results of applying ttp objects.
	MapTtpPropStore::iterator ithmvprzvps;
	for (ithmvprzvps = m_hmttpzvps.Begin(); ithmvprzvps != m_hmttpzvps.End(); ++ithmvprzvps)
	{
		VwPropertyStore * pzvps = ithmvprzvps.GetValue();
		pzvps->Unlock();
		pzvps->CopyFrom(this);
		pzvps->ApplyTtp(ithmvprzvps.GetKey());
		pzvps->RecomputeEffects();
		pzvps->Lock();
	}

	// Fix each vps in the map used to record results of applying int property settings.
	MapIPKPropStore::iterator ithmipkzvps;
	for (ithmipkzvps = m_hmipkzvps.Begin(); ithmipkzvps != m_hmipkzvps.End(); ++ithmipkzvps)
	{
		VwPropertyStore * pzvps = ithmipkzvps.GetValue();
		IntPropKey ipk = ithmipkzvps.GetKey();
		pzvps->Unlock();
		pzvps->CopyFrom(this);
		CheckHr(pzvps->put_IntProperty(ipk.m_nID, ipk.m_nVariation, ipk.m_nValue));
		pzvps->RecomputeEffects();
		pzvps->Lock();
	}

	// Fix each vps in the map used to record results of applying string property settings.
	for (int ispr = 0; ispr < m_vstrprrec.Size(); ++ispr)
	{
		VwPropertyStore * pzvps = m_vstrprrec[ispr].m_pzvps;
		pzvps->Unlock();
		pzvps->CopyFrom(this);
		CheckHr(pzvps->put_StringProperty(m_vstrprrec[ispr].m_nID,
			m_vstrprrec[ispr].m_su.Bstr()));
		pzvps->RecomputeEffects();
		pzvps->Lock();
	}
}

/*----------------------------------------------------------------------------------------------
	These routines return the appropriate margin for a table cell, based on which edges of the
	table the cell is adjacent to, as passed in the grfcs argument.
----------------------------------------------------------------------------------------------*/
int VwPropertyStore::MarginTop(CellSides grfcs)
{
	// If the cell is at the top, and the programmer has not requested an explicit top margin,
	// change to 0.
	if ((!(grfcs & kfcsTop)) && !(m_grfcsExplicitMargins & kfcsTop))
		return m_mpMarginTop;
	else
		return 0;
}
int VwPropertyStore::MarginBottom(CellSides grfcs)
{
	if ((!(grfcs & kfcsBottom)) && !(m_grfcsExplicitMargins & kfcsBottom))
		return m_mpMarginBottom;
	else
		return 0;
}
int VwPropertyStore::MarginLeading(CellSides grfcs)
{
	if ((!(grfcs & kfcsLeading)) && !(m_grfcsExplicitMargins & kfcsLeading))
		return m_mpMarginLeading;
	else
		return 0;

}
int VwPropertyStore::MarginTrailing(CellSides grfcs)
{
	if ((!(grfcs & kfcsTrailing)) && !(m_grfcsExplicitMargins & kfcsTrailing))
		return m_mpMarginTrailing;
	else
		return 0;
}

/*----------------------------------------------------------------------------------------------
	See if any errors occured while drawing. Return an error code, or S_OK if none.
	Right now the only error we're checking for is the renderer being not properly set-up
	(eg, if there were errors loading a Graphite font).
----------------------------------------------------------------------------------------------*/
HRESULT VwPropertyStore::DrawingErrors(IVwGraphics* pvg)
{
	HRESULT hr;

	if (m_fInitChrp)
	{
		AssertPtr(m_qwsf);
		ILgWritingSystemPtr qws;
		CheckHr(m_qwsf->get_EngineOrNull(m_chrp.ws, &qws));
		if (qws)
		{
			IRenderEnginePtr qreneng;
			CheckHr(m_qwsf->get_RendererFromChrp(pvg, &m_chrp, &qreneng));
			if (qreneng)
			{
				IgnoreHr(hr = qreneng->FontIsValid());
				if (FAILED(hr) && hr != E_NOTIMPL)
					return hr;
			}
		}
	}

	if (m_qzvpsReset)
	{
		IgnoreHr(hr = m_qzvpsReset->DrawingErrors(pvg));
		if (FAILED(hr))
			return hr;
	}

	// Recurse through prop-stores resulting from ttps
	MapTtpPropStore::iterator ithmvprzvps;
	for (ithmvprzvps = m_hmttpzvps.Begin(); ithmvprzvps != m_hmttpzvps.End(); ++ithmvprzvps)
	{
		VwPropertyStore * pzvps = ithmvprzvps.GetValue();
		IgnoreHr(hr = pzvps->DrawingErrors(pvg));
		if (FAILED(hr))
			// Found an error.
			return hr;
	}

	// Recurse through prop-stores resulting from int property settings
	MapIPKPropStore::iterator ithmipkzvps;
	for (ithmipkzvps = m_hmipkzvps.Begin(); ithmipkzvps != m_hmipkzvps.End(); ++ithmipkzvps)
	{
		VwPropertyStore * pzvps = ithmipkzvps.GetValue();
		IgnoreHr(hr = pzvps->DrawingErrors(pvg));
		if (FAILED(hr))
			// Found an error.
			return hr;
	}

	// Recurse through prop-stores resulting from string settings
	for (int ispr = 0; ispr < m_vstrprrec.Size(); ++ispr)
	{
		VwPropertyStore * pzvps = m_vstrprrec[ispr].m_pzvps;
		IgnoreHr(hr = pzvps->DrawingErrors(pvg));
		if (FAILED(hr))
			// Found an error.
			return hr;
	}

	return S_OK;
}


//:>********************************************************************************************
//:>	VwStylesheet
//:>********************************************************************************************


//:>********************************************************************************************
//:>	Generic factory stuff for CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Views.VwStylesheet"),
	&CLSID_VwStylesheet,
	_T("SIL View style sheet"),
	_T("Apartment"),
	&VwStylesheet::CreateCom);

void VwStylesheet::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwStylesheet> qss;
	qss.Attach(NewObj VwStylesheet());				// ref count initialy 1
	CheckHr(qss->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	VwStylesheet constructor/destructor
//:>********************************************************************************************

VwStylesheet::VwStylesheet()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

VwStylesheet::~VwStylesheet() {
	ModuleEntry::ModuleRelease();
}

STDMETHODIMP VwStylesheet::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwStylesheet)
		*ppv = static_cast<IVwStylesheet *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwStylesheet);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:> VwStylesheet methods. Note: this class is not fully implemented and not ready to
//:> use. See AfVwStyleSheet and subclasses for real ones.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Implement these methods to make an instance of the appropriate subclass and init it.
----------------------------------------------------------------------------------------------*/

/*----------------------------------------------------------------------------------------------
	Gets the name of the default paragraph style to use as the base for new styles (Usually
	"Normal")
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::GetDefaultBasedOnStyleName(BSTR * pbstrNormal)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrNormal);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Gets the style name that is the default style to use for the given context, or maybe just
	throw an exception.

	@param nContext The context
	@param fCharStyle whether the style is a character style or not.
	@param pbstrStyleName Out Name of the style that is the default for the context
	@return S_OK if successful, or E_FAIL if not.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::GetDefaultStyleForContext(int nContext, ComBool fCharStyle,
													 BSTR * pbstrStyleName)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrStyleName);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Store a style. bstrUsage, hvoBasedOn, hvoNext, nType, fBuiltIn, and fModified
	are ignored.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::PutStyle(BSTR bstrName, BSTR bstrUsage, HVO hvoStyle,
		HVO hvoBasedOn, HVO hvoNext, int nType, ComBool fBuiltIn, ComBool fModified,
		ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pttp);

	StrUni suKey(bstrName);
	m_hmsuttp.Insert(suKey, pttp, true); // allow replacements

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Retrieve one.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::GetStyleRgch(int cch, OLECHAR * prgchName, ITsTextProps ** ppttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchName, cch);
	ChkComOutPtr(ppttp);

	ITsTextPropsPtr qttp;
	StrUni suKey(prgchName, cch);
	m_hmsuttp.Retrieve(suKey, qttp);
	*ppttp = qttp.Detach();

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the next style that will be used if the user types a CR at the end of this paragraph.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::GetNextStyle(BSTR bstrName, BSTR * pbstrNext)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pbstrNext);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the basedOn style name.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::GetBasedOn(BSTR bstrName, BSTR * pbstrBasedOn)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pbstrBasedOn);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the type.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::GetType(BSTR bstrName, int * pnType)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pnType);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get the role.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::GetContext(BSTR bstrName, int * pnContext)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pnContext);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Is the style named bstrName a predefined style?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::IsBuiltIn(BSTR bstrName, ComBool * pfBuiltIn)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pfBuiltIn);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Was the (predefined) style named bstrName changed by the user?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::IsModified(BSTR bstrName, ComBool * pfModified)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstrName);
	ChkComOutPtr(pfModified);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

// ENHANCE EberhardB: Add method for usage

/*----------------------------------------------------------------------------------------------
	Return the associated Data Access object.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::get_DataAccess(ISilDataAccess ** ppsda)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsda);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Return an HVO for a newly created style.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::MakeNewStyle(HVO * phvoNewStyle)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvoNewStyle);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Delete the type.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::Delete(HVO hvoStyle)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Get number of styles in sheet.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::get_CStyles(int * pcttp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcttp);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}
/*----------------------------------------------------------------------------------------------
	Get the HVO of the Nth style (in an arbitrary order).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::get_NthStyle(int ihvo, HVO * phvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}
/*----------------------------------------------------------------------------------------------
	Get the name of the Nth style (in an arbitrary order).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::get_NthStyleName(int ihvo, BSTR * pbstrStyleName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrStyleName);

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	A special style that contains only the parts of "Normal" that relate to the Font tab.
	This is automatically maintained as "Normal" is edited.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::get_NormalFontStyle(ITsTextProps ** ppttp)
{
	*ppttp = 0;
	return S_OK;
}

/*----------------------------------------------------------------------------------------------
	Return true if the given style is one that is protected within the style sheet.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::get_IsStyleProtected(BSTR bstrName, ComBool * pfProtected)
{
	Assert(false);
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Cache a (subtly modified) new set of text props without recording an undo action.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwStylesheet::CacheProps(int cch, OLECHAR * prgchName, HVO hvoStyle,
	ITsTextProps * pttp)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchName, cch);
	ChkComArgPtr(pttp);

	Assert(false);
	return E_NOTIMPL;

	END_COM_METHOD(g_fact, IID_IVwStylesheet);
}

/*----------------------------------------------------------------------------------------------
	Calculate the font size that would be used for text in the given writing system.
----------------------------------------------------------------------------------------------*/
int VwPropertyStore::FontSizeForWs(int ws)
{
	// Create a text properties object containing the given writing system. Apply it to the
	// current store, and ask the resulting store for its font size.
	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, ws));
	ITsTextPropsPtr qttp;
	CheckHr(qtpb->GetTextProps(&qttp));

	// DON'T cache this temporary store in the current one. The reason is that this store may
	// be in the process of being modified, so the cached store may become out of date.
	ITsTextProps * pttp = qttp;
	VwPropertyStorePtr qzvps;
	if (!m_hmttpzvps.Retrieve(pttp, qzvps))
	{
		qzvps.Attach(MakePropertyStore()); // ref count = 1
		qzvps->CopyFrom(this);
		qzvps->ApplyTtp(pttp);
	}

	int nRet;
	CheckHr(qzvps->get_FontSize(&nRet));
	return nRet;
}

#include "HashMap_i.cpp"
template class HashMap<OLECHAR, OLECHAR>;
