/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwOverlay.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Manages the VwOverlay object which specifies display of text overlays.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwOverlay_INCLUDED
#define VwOverlay_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: TagSpecKey
Description: Hashmap seems to want an object as a key. This class serves as it.
Hungarian: tds
----------------------------------------------------------------------------------------------*/
struct TagSpecKey
{
	OLECHAR m_rgchGuid[kcchGuidRepLength];
};

/*----------------------------------------------------------------------------------------------
Class: TagDispSpec
Description: Specifies all the info about how to display one tag
Hungarian: tds
----------------------------------------------------------------------------------------------*/
struct TagDispSpec
{
	OLECHAR m_rgchGuid[kcchGuidRepLength];
	HVO m_hvoPossibility;
	COLORREF m_clrFore;
	COLORREF m_clrBack;
	COLORREF m_clrUnder;
	int m_unt; // FwUnderlineType
	bool m_fHidden;
	StrUni m_stuAbbr;
	StrUni m_stuName;

	bool operator <(const TagDispSpec & arg);
};

typedef Vector<TagDispSpec> VecTagDispSpec; // vtds

// Map from a 26-char representation of a Guid to an index.
typedef HashMap<TagSpecKey, int> MapGuidInt; // hmgi

/*----------------------------------------------------------------------------------------------
Class: VwOverlay
Description:
Hungarian: zvo
----------------------------------------------------------------------------------------------*/
class VwOverlay : public IVwOverlay
{
public:
	// Static methods

	// Constructors/destructors/etc.
	VwOverlay();
	virtual ~VwOverlay();
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
		if (cref == 0) {
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwOverlay methods
	STDMETHOD(get_Name)(BSTR * pbstr);
	STDMETHOD(put_Name)(BSTR bstr);
	STDMETHOD(get_Guid)(OLECHAR * prgchGuid);
	STDMETHOD(put_Guid)(OLECHAR * prgchGuid);
	STDMETHOD(get_PossListId)(HVO * ppsslId);
	STDMETHOD(put_PossListId)(HVO psslId);
	STDMETHOD(get_Flags)(VwOverlayFlags * pvof);
	STDMETHOD(put_Flags)(VwOverlayFlags vof);
	STDMETHOD(get_FontName)(BSTR * pbstr);
	STDMETHOD(put_FontName)(BSTR bstr);
	STDMETHOD(FontNameRgch)(OLECHAR * prgch);
	STDMETHOD(get_FontSize)(int * pmp);
	STDMETHOD(put_FontSize)(int mp);
	STDMETHOD(get_MaxShowTags)(int * pctag);
	STDMETHOD(put_MaxShowTags)(int ctag);
	STDMETHOD(get_CTags)(int * pctag);
	STDMETHOD(GetDbTagInfo)(int itag, HVO * phvo, COLORREF * pclrFore,
		COLORREF * pclrBack, COLORREF * pclrUnder, int * punt, ComBool * pfHidden,
		OLECHAR * prgchGuid);
	STDMETHOD(SetTagInfo)(OLECHAR * prgchGuid, HVO hvo, int osm, BSTR bstrAbbr, BSTR bstrName,
		COLORREF clrFore, COLORREF clrBack, COLORREF clrUnder, int unt, ComBool fHidden);
	STDMETHOD(GetTagInfo)(OLECHAR * prgchGuid, HVO * phvo, BSTR * pbstrAbbr, BSTR * pbstrName,
		COLORREF * pclrFore, COLORREF * pclrBack, COLORREF * pclrUnder, int * punt, ComBool * pfHidden);
	STDMETHOD(GetDlgTagInfo)(int itag, ComBool * pfHidden, COLORREF * pclrFore,
		COLORREF * pclrBack, COLORREF * pclrUnder, int * punt, BSTR * pbstrAbbr, BSTR * pbstrName);
	STDMETHOD(GetDispTagInfo)(OLECHAR * prgchGuid, ComBool * pfHidden, COLORREF * pclrFore,
		COLORREF * pclrBack, COLORREF * pclrUnder, int * punt,
		OLECHAR * prgchAbbr, int cchMaxAbbr, int * pcchAbbr,
		OLECHAR * prgchName, int cchMaxName, int * pcchName);
	STDMETHOD(RemoveTag)(OLECHAR * prgchGuid);
	STDMETHOD(Sort)(ComBool fByAbbr);
	STDMETHOD(Merge)(IVwOverlay * pvo, IVwOverlay ** ppvoMerged);

	static void MergeOverlayProps1(COLORREF * pclrFore, COLORREF clrFore,
		COLORREF * pclrBack, COLORREF clrBack);
	static void MergeOverlayProps2(COLORREF * pclrUnder, COLORREF clrUnder,
		int * punt, int unt);

protected:
	// Member variables
	long m_cref;
	StrUni m_stuName;
	OLECHAR m_rgchGuid[kcchGuidRepLength];
	VwOverlayFlags m_vof;
	StrUni m_stuFontName;
	int m_mpFontSize;
	int m_ctagShow;
	VecTagDispSpec m_vtds; // All the information about the tags
	MapGuidInt m_hmgi; // Let us find one quickly from a string representation of a guid.
	// This vector imposes an ordering on m_vtds, which is never actually ordered as m_hmgi contains
	// indexes into it. It is a vector of indexes into m_vtds
	// Typically, m_citdsUsed items at the start of m_vitdsOrder indicate recently-used items;
	// the others are either in the same order as m_vtds (i.e., the order added) or the result
	// of a sort request.
	int m_citdsUsed;
	IntVec m_vitdsOrder;
	HVO m_psslId;
};
DEFINE_COM_PTR(VwOverlay);
#endif  //VwOverlay_INCLUDED
