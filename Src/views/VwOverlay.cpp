/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwOverlay.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)


#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Methods
//:>********************************************************************************************

// Protected default constructor used for CreateCom
VwOverlay::VwOverlay()
{
	m_cref = 1;
	m_psslId = (HVO)-1;
	ModuleEntry::ModuleAddRef();
}


VwOverlay::~VwOverlay()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwOverlay::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwOverlay)
		*ppv = static_cast<IVwOverlay *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Views.VwOverlay"),
	&CLSID_VwOverlay,
	_T("SIL Overlay Spec"),
	_T("Apartment"),
	&VwOverlay::CreateCom);


void VwOverlay::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwOverlay> qov;
	qov.Attach(NewObj VwOverlay());		// ref count initialy 1
	CheckHr(qov->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	IVwOverlay methods
//:>********************************************************************************************
		// Name of the overlay itself.
/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_Name(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pbstr);

	if (pbstr == NULL)
		return E_UNEXPECTED;

	*pbstr = NULL;
	m_stuName.GetBstr(pbstr);

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::put_Name(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	m_stuName.Assign(bstr, BstrLen(bstr));

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_Guid(OLECHAR * prgchGuid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchGuid);

	AssertArray(prgchGuid, kcchGuidRepLength);
	MoveItems(m_rgchGuid, prgchGuid, kcchGuidRepLength);
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::put_Guid(OLECHAR * prgchGuid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchGuid);

	AssertArray(prgchGuid, kcchGuidRepLength);
	MoveItems(prgchGuid, m_rgchGuid, kcchGuidRepLength);
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_PossListId(HVO * ppsslId)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsslId);
	*ppsslId = m_psslId;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::put_PossListId(HVO psslId)
{
	BEGIN_COM_METHOD;
	m_psslId = psslId;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_Flags(VwOverlayFlags * pvof)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvof);
	*pvof = m_vof;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::put_Flags(VwOverlayFlags vof)
{
	BEGIN_COM_METHOD;
	m_vof = vof;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
	// Name and point size of the font to use to display tags.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_FontName(BSTR * pbstr)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstr);

	*pbstr = NULL;
	m_stuFontName.GetBstr(pbstr);

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::put_FontName(BSTR bstr)
{
	BEGIN_COM_METHOD;
	ChkComBstrArgN(bstr);

	m_stuFontName.Assign(bstr, BstrLen(bstr));

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
	Get up to 31 characters of the font name into prgch. (This is a magic number because it
	is the most that can be stored in a LOGFONT struct.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::FontNameRgch(OLECHAR * prgch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgch);

	wcsncpy_s(prgch, 32, m_stuFontName.Chars(), 31);

	prgch[31] = 0;

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------

	@param pmp millipoints
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_FontSize(int * pmp)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pmp);
	*pmp = m_mpFontSize;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::put_FontSize(int mp)
{
	BEGIN_COM_METHOD;
	if (mp < 0)
		return E_INVALIDARG;
	m_mpFontSize = mp;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------
	Number of tags (max) to show at one boundary position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_MaxShowTags(int * pctag)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pctag);
	*pctag = m_ctagShow;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------

	Arguments:
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::put_MaxShowTags(int ctag)
{
	BEGIN_COM_METHOD;
	if (ctag < 0)
		return E_INVALIDARG;
	m_ctagShow = ctag;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
		// Number of tags in overlay
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::get_CTags(int * pctag)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pctag);
	*pctag = m_vtds.Size();
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
	Info about nth tag, all that is needed to make a column in the database.
	@param pid Record ID in database of CmPossibility
	@param punt FwUnderlineType
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::GetDbTagInfo(int itag, HVO * phvo, COLORREF * pclrFore,
	COLORREF * pclrBack, COLORREF * pclrUnder, int * punt, ComBool * pfHidden,
	OLECHAR * prgchGuid)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phvo);
	ChkComOutPtr(pclrFore);
	ChkComOutPtr(pclrBack);
	ChkComOutPtr(pclrUnder);
	ChkComOutPtr(punt);
	ChkComOutPtr(pfHidden);
	ChkComArgPtr(prgchGuid);
	if ((uint)(itag) >= (uint)(m_vtds.Size()))
		return E_INVALIDARG;
	TagDispSpec & tds = m_vtds[itag];
	*phvo = tds.m_hvoPossibility;
	*pclrFore = tds.m_clrFore;
	*pclrBack = tds.m_clrBack;
	*pclrUnder = tds.m_clrUnder;
	*punt = tds.m_unt;
	*pfHidden = tds.m_fHidden;
	MoveItems(tds.m_rgchGuid, prgchGuid, kcchGuidRepLength);
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
	Add (or replace) the information about the specified GUID.
	@param id Record ID in database of CmPossibility
	@param unt FwUnderlineType
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::SetTagInfo(OLECHAR * prgchGuid, HVO hvo, int osm, BSTR bstrAbbr,
	BSTR bstrName, COLORREF clrFore, COLORREF clrBack, COLORREF clrUnder, int unt,
	ComBool fHidden)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchGuid);
	ChkComBstrArgN(bstrAbbr);
	ChkComBstrArgN(bstrName);

	if (!osm)
		return S_OK; // Nothing to update.
	if ((uint) unt >= kuntLim)
		return E_INVALIDARG;

	int itds;
	// If already present we overwrite, otherwise, add.
	TagSpecKey tdk;
	MoveItems(prgchGuid, tdk.m_rgchGuid, kcchGuidRepLength);
	if (!m_hmgi.Retrieve (tdk, &itds))
	{
		itds = m_vtds.Size();
		m_vtds.Push(TagDispSpec());
		// And put it at the end of the ordering, for now.
		m_vitdsOrder.Push(itds);
		MoveItems(prgchGuid, tdk.m_rgchGuid, kcchGuidRepLength);
		m_hmgi.Insert(tdk, itds);
	}
	TagDispSpec & tds = m_vtds[itds];
	MoveItems(prgchGuid, tds.m_rgchGuid, kcchGuidRepLength);
	tds.m_hvoPossibility = hvo;
	if (osm & kosmClrFore)
		tds.m_clrFore = clrFore;
	if (osm & kosmClrBack)
		tds.m_clrBack = clrBack;
	if (osm & kosmClrUnder)
		tds.m_clrUnder = clrUnder;
	if (osm & kosmUnderType)
		tds.m_unt = unt;
	if (osm & kosmHidden)
		tds.m_fHidden = bool(fHidden);
	if (osm & kosmAbbr)
		tds.m_stuAbbr.Assign(bstrAbbr, BstrLen(bstrAbbr));
	if (osm & kosmName)
		tds.m_stuName.Assign(bstrName, BstrLen(bstrName));
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
	Get all the info about a particular tag. This is used in merging.
	Return S_FAIL if no info about this GUID. However, this is a normal usage.
	@param phvo Record ID in database of CmPossibility
	@param punt FwUnderlineType
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::GetTagInfo(OLECHAR * prgchGuid, HVO * phvo, BSTR * pbstrAbbr,
	BSTR * pbstrName, COLORREF * pclrFore, COLORREF * pclrBack, COLORREF * pclrUnder,
	int * punt, ComBool * pfHidden)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchGuid);
	ChkComOutPtr(phvo);
	ChkComOutPtr(pbstrAbbr);
	ChkComOutPtr(pbstrName);
	ChkComOutPtr(pclrFore);
	ChkComOutPtr(pclrBack);
	ChkComOutPtr(pclrUnder);
	ChkComOutPtr(punt);
	ChkComOutPtr(pfHidden);

	int itds;
	TagSpecKey tdk;
	MoveItems(prgchGuid, tdk.m_rgchGuid, kcchGuidRepLength);
	if (!m_hmgi.Retrieve (tdk, &itds))
	{
		return E_FAIL;
	}
	else
	{
		TagDispSpec & tds = m_vtds[itds];
		*phvo = tds.m_hvoPossibility;
		tds.m_stuAbbr.GetBstr(pbstrAbbr);
		tds.m_stuName.GetBstr(pbstrName);
		*pclrFore = tds.m_clrFore;
		*pclrBack = tds.m_clrBack;
		*pclrUnder = tds.m_clrUnder;
		*punt = tds.m_unt;
		*pfHidden = tds.m_fHidden;
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------
	Get the info that is relevant for displaying the tag in a dialog or pop-up menu.
	This uses an index because it should obey the ordering produced by Sort.
	Arguments:
		punt			FwUnderlineType
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::GetDlgTagInfo(int itag, ComBool * pfHidden, COLORREF * pclrFore,
	COLORREF * pclrBack, COLORREF * pclrUnder, int * punt, BSTR * pbstrAbbr, BSTR * pbstrName)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfHidden);
	ChkComOutPtr(pclrFore);
	ChkComOutPtr(pclrBack);
	ChkComOutPtr(pclrUnder);
	ChkComOutPtr(punt);
	ChkComOutPtr(pbstrAbbr);
	ChkComOutPtr(pbstrName);
	if (itag != 0 && (uint)(itag) >= (uint)(m_vtds.Size()))
		return E_INVALIDARG;

	if (m_vtds.Size())
	{
		TagDispSpec & tds = m_vtds[m_vitdsOrder[itag]];
		*pfHidden = tds.m_fHidden;
		*pclrFore = tds.m_clrFore;
		*pclrBack = tds.m_clrBack;
		*pclrUnder = tds.m_clrUnder;
		*punt = tds.m_unt;
		tds.m_stuAbbr.GetBstr(pbstrAbbr);
		tds.m_stuName.GetBstr(pbstrName);
	}
	else
	{
		// Do something reasonable if the list is empty.
		*pfHidden = true;
		*pclrFore = kclrBlack;
		*pclrBack = kclrWhite;
		*pclrUnder = kclrBlack;
		*punt = kuntNone;
		*pbstrAbbr = NULL;
		*pbstrName = NULL;
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
	Get the info that the view code wants for actual display.
	If nothing is known about the GUID, it is treated as hidden; the
	call succeeds. As with fields that are really hidden, the name is returned
	as an empty string, and all other return results are knNinch.
	Arguments:
		punt			FwUnderlineType
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::GetDispTagInfo(OLECHAR * prgchGuid, ComBool * pfHidden,
	COLORREF * pclrFore, COLORREF * pclrBack, COLORREF * pclrUnder,
	int * punt, OLECHAR * prgchAbbr, int cchMaxAbbr, int * pcchAbbr,
	OLECHAR * prgchName, int cchMaxName, int * pcchName)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchGuid);
	ChkComOutPtr(pfHidden);
	ChkComOutPtr(pclrFore);
	ChkComOutPtr(pclrBack);
	ChkComOutPtr(pclrUnder);
	ChkComOutPtr(punt);
	ChkComArrayArg(prgchAbbr, cchMaxAbbr);
	ChkComOutPtr(pcchAbbr);
	ChkComArrayArg(prgchName, cchMaxName);
	ChkComOutPtr(pcchName);

	int itds;
	TagSpecKey tdk;
	MoveItems(prgchGuid, tdk.m_rgchGuid, kcchGuidRepLength);
	if (!m_hmgi.Retrieve (tdk, &itds) || m_vtds[itds].m_fHidden)
	{
		// Make it hidden and don't care for everything
		*pfHidden = true;
		*pclrFore = (COLORREF) knNinch;
		*pclrBack = (COLORREF) knNinch;
		*pclrUnder = (COLORREF) knNinch;
		*punt = knNinch;
		if (prgchAbbr)
			*prgchAbbr = 0;
		*pcchAbbr  = 0;
		if (prgchName)
			*prgchName = 0;
		*pcchName  = 0;
	}
	else
	{
		TagDispSpec & tds = m_vtds[itds];
		*pfHidden = false;
		*pclrFore = tds.m_clrFore;
		*pclrBack = tds.m_clrBack;
		*pclrUnder = tds.m_clrUnder;
		*punt = tds.m_unt;
		int cchCopy = std::min(cchMaxAbbr, tds.m_stuAbbr.Length());
		MoveItems(tds.m_stuAbbr.Chars(), prgchAbbr, cchCopy);
		*pcchAbbr = cchCopy;
		cchCopy = std::min(cchMaxName, tds.m_stuName.Length());
		MoveItems(tds.m_stuName.Chars(), prgchName, cchCopy);
		*pcchName = cchCopy;
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


/*----------------------------------------------------------------------------------------------
	Remove the selected tag from the overlay.
	If nothing is known about the GUID, the call succeeds (without removing anything).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::RemoveTag(OLECHAR * prgchGuid)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchGuid);

	int itds;
	TagSpecKey tdk;
	MoveItems(prgchGuid, tdk.m_rgchGuid, kcchGuidRepLength);
	if (m_hmgi.Retrieve(tdk, &itds))
	{
		m_vtds.Delete(itds);
		m_hmgi.Delete(tdk);
	}
	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}


static ComBool s_fByAbbr;

// a smart comparison operator for TagDispSpecs. It would be cleaner to enhance SortIndirect
// to take a comparison operator as a customization parameter, but I don't have time.
// Using these static variables makes this code non-thread-safe.
bool TagDispSpec::operator < (const TagDispSpec & arg)
{
	StrUni stuMyKey = s_fByAbbr ? m_stuAbbr : m_stuName;
	StrUni stuItsKey = s_fByAbbr ? arg.m_stuAbbr : arg.m_stuName;
	int nResult;
	CheckHr(ViewsGlobals::s_qcoleng->Compare(stuMyKey.Bstr(), stuItsKey.Bstr(), fcoDefault, &nResult));
	return nResult < 0;
}

/*----------------------------------------------------------------------------------------------
	Sort the tags, with the most recently used at the start ordered by how
	recently used, the rest alphabetical by abbr or name.
	Arguments:
		fByAbbr         otherwise by name
		ctagMaxRecent   # most-recently-used to keep at start
		pctagRecent     actual number (may be less)
	Warning: uses static variables; not thread safe.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::Sort(ComBool fByAbbr)
{
	BEGIN_COM_METHOD

	s_fByAbbr = fByAbbr;
	ViewsGlobals::s_qcoleng.CreateInstance(CLSID_LgUnicodeCollater);
	// Sort the items after the MRU list, ordering the indexes, not the items.
	// Note that the indexes are relative to the start of m_vtds, so we DON'T
	// add ctagRecent to that.
	SortIndirect(m_vtds.Begin(), m_vitdsOrder.Size(),
		m_vitdsOrder.Begin());
	ViewsGlobals::s_qcoleng.Clear();

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

/*----------------------------------------------------------------------------------------------
	Merge two sets of properties. The first argument of each pair gets changed to the
	combination.
	If the second is hidden make no changes to the first.
----------------------------------------------------------------------------------------------*/
void VwOverlay::MergeOverlayProps1(COLORREF * pclrFore, COLORREF clrFore,
	COLORREF * pclrBack, COLORREF clrBack)
{
	COLORREF clrDark = RGB(100,100,100);
	COLORREF clrLight = RGB(210, 210, 210);
	// Colors. If the old color is knNinch or kclrTransparent, use the other.
	// Otherwise, if the new one is knNinch or kclrTransparent, or the same as the old, no change.
	// If there is a real conflict, use the appropriate conflict color.
	if (*pclrFore == knNinch || *pclrFore == kclrTransparent)
		*pclrFore = clrFore;
	else if (clrFore != knNinch && clrFore != kclrTransparent && clrFore != *pclrFore)
		*pclrFore = clrDark;

	if (*pclrBack == knNinch || *pclrBack == kclrTransparent)
		*pclrBack = clrBack;
	else if (clrBack != knNinch && clrBack != kclrTransparent && clrBack != *pclrBack)
		*pclrBack = clrLight;

}
void VwOverlay::MergeOverlayProps2(COLORREF * pclrUnder, COLORREF clrUnder,
	int * punt, int unt)
{
	COLORREF clrDark = RGB(40,40,40);
	// Colors. If the old color is knNinch or kclrTransparent, use the other.
	// Otherwise, if the new one is knNinch or kclrTransparent, or the same as the old, no change.
	// If there is a real conflict, use the appropriate conflict color,
	// except that, if one called for no underlining,
	// ignore its color requested for underlining. (The default underline color
	// is black rather than ninch).

	if (*pclrUnder == knNinch || *pclrUnder == kclrTransparent || *punt == kuntNone)
		*pclrUnder = clrUnder;
	else if (clrUnder != knNinch && clrUnder != kclrTransparent && clrUnder != *pclrUnder &&
		unt != kuntNone)
	{
		*pclrUnder = clrDark;
	}
	// otherwise stick to current value.

	// Underline style. Go for the double underlining if there is a conflict
	// (except that 'none', like ninch, does not conflict with anything).
	if (*punt == knNinch || *punt == kuntNone)
		*punt = unt;
	else if (unt != knNinch && unt != kuntNone && unt != *punt)
		*punt = kuntDouble;
	// Otherwise stick to current value.
}


/*----------------------------------------------------------------------------------------------
	Obtain a new overlay, formed by merging existing ones. Conflicting requests for the same
	tag are resolved (unless one is knNinch) in the standard way, that is, DARK GRAY for text
	and underline and LIGHT GRAY for background;
	An error occurs if different abbreviations are given for the same GUID.
	(The new overlay is typically only temporary, and does not initially have a name.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlay::Merge(IVwOverlay * pvo, IVwOverlay ** ppvoMerged)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pvo);
	ChkComOutPtr(ppvoMerged);

	TagSpecKey tdk;
	HVO hvoPossibility;
	COLORREF clrFore;
	COLORREF clrBack;
	COLORREF clrUnder;
	int unt;
	ComBool fHidden;
	SmartBstr sbstrAbbr;
	SmartBstr sbstrName;

	VwOverlayPtr qzvo;
	qzvo.Attach(NewObj VwOverlay());
	int itds;
	for (itds = 0; itds < m_vtds.Size(); itds++)
	{
		TagDispSpec & tds = m_vtds[itds];

		// See if the other list has info about the same tag.
		ComBool fHidden2;
		HRESULT hr;
		hr = pvo->GetTagInfo(tds.m_rgchGuid, &hvoPossibility, &sbstrAbbr, &sbstrName,
			&clrFore, &clrBack, &clrUnder, &unt, &fHidden2);
		// Only likely reason for failure is, not in the other list. Treat that the
		// same as if it is hidden, that is, use the original values. If we got a value
		// and it is not hidden, do the merge. Since we now have at least one
		// unhidden set of properties, the end result can't be hidden.
		if (SUCCEEDED(hr) && !fHidden2)
		{

			// We can't merge unless all these items match.

			if (hvoPossibility != tds.m_hvoPossibility ||
				wcscmp(tds.m_stuAbbr.Chars(), sbstrAbbr) ||
				wcscmp(tds.m_stuName.Chars(), sbstrName))
			{
				ThrowHr(WarnHr(E_FAIL));
			}

			if (!tds.m_fHidden)
			{
				MergeOverlayProps1(&clrFore, tds.m_clrFore, &clrBack, tds.m_clrBack);
				MergeOverlayProps2(&clrUnder, tds.m_clrUnder, &unt, tds.m_unt);
			}
			fHidden = false;
		}
		else
		{
			// By default these have the current values.
			clrFore = tds.m_clrFore;
			clrBack = tds.m_clrBack;
			clrUnder = tds.m_clrUnder;
			unt = tds.m_unt;
			fHidden = tds.m_fHidden;
		}
		// Add it into the new object
		CheckHr(qzvo->SetTagInfo(tds.m_rgchGuid, tds.m_hvoPossibility, kosmAll,
			tds.m_stuAbbr.Bstr(), tds.m_stuName.Bstr(), clrFore, clrBack, clrUnder, unt,
			fHidden));
	}
	int ctdsOther;
	CheckHr(pvo->get_CTags(&ctdsOther));

	// Loop over items of the other overlay. Any that don't occur in this
	// need to be added unmodified to the new one.
	for (itds = 0; itds < ctdsOther; itds ++)
	{
		CheckHr(pvo->GetDbTagInfo(itds, &hvoPossibility, &clrFore, &clrBack, &clrUnder,
			&unt, &fHidden, tdk.m_rgchGuid));
		int itdsDummy;
		if (!m_hmgi.Retrieve(tdk, &itdsDummy))
		{
			// If it was in both we already merged. Since it wasn't, add it in.
			// Get all the info this time.
			CheckHr(pvo->GetTagInfo(tdk.m_rgchGuid, &hvoPossibility, &sbstrAbbr, &sbstrName,
				&clrFore, &clrBack, &clrUnder, &unt, &fHidden));
			CheckHr(qzvo->SetTagInfo(tdk.m_rgchGuid, hvoPossibility, kosmAll, sbstrAbbr,
				sbstrName, clrFore, clrBack, clrUnder, unt, fHidden));
		}
	}

	// Copy global values from the current overlay into the new one.
	qzvo->m_vof = m_vof;
	qzvo->m_stuFontName = m_stuFontName;
	qzvo->m_mpFontSize = m_mpFontSize;
	qzvo->m_ctagShow = m_ctagShow;

	*ppvoMerged = qzvo.Detach();

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwOverlay);
}

// Explicit instantiation
#include "Vector_i.cpp"
#include "HashMap_i.cpp"

template class Vector<TagDispSpec>; // VecTagDispSpec; // vtds

// Map from a 26-char representation of a Guid to an index.
template class HashMap<TagSpecKey, int>; // MapGuidInt; // hmgi
