/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwTxtSrc.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	An implementation of IVwTextSource based on having a VwStringBoxMain for each span in a
	paragraph (or one for the whole paragraph, if no embedded spans), and a list of strings.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VWTXTSRC_INCLUDED
#define VWTXTSRC_INCLUDED

// ENHANCE JohnT: create an interface ITsMutString, the same as ITsString except the
// contract does not guarantee it immutable. Make the builders inherit from it
// instead of duplicating the methods. Remove this define.
#define ITsMutString ITsString
#define ITsMutStringPtr ITsStringPtr

// JohnT: moved here from VwTextBoxes.h as this file uses it and needs to be included first.
enum VwSourceType
{
	kvstNormal, kvstTagged, kvstMapped, kvstMappedTagged, kvstConc, kvstOverride
}; // Hungarian vst

class VwMappedTxtSrc;

/*	Struct: VpsTssRec: ViewPropertyStore Ts[Multi]String Record*/
struct VpsTssRec
{
	VwPropertyStorePtr qzvps;
	// May store an actual string, or null to indicate a slot that stands for an embedded box.
	ITsMutStringPtr qtms;

	VpsTssRec(VwPropertyStore * pzvps, ITsMutString * qtmsA)
	{
		qzvps = pzvps;
		qtms = qtmsA;
	}
	VpsTssRec()
	{
	}
};

typedef Vector<VpsTssRec> VpsTssVec; // Hungarian vpst
/*----------------------------------------------------------------------------------------------
	Class: VwTxtSrc
	This class really just amounts to an interface definition: it specifies the functions
	that the source object of a VwParagraphBox must have. It's not a COM interface, because
	(except for the inherited IVwTextSource functions) it isn't used outside the Views DLL.
	However, everything is pure virtual, because one implementation is the VwOverrideTxtSrc,
	which passes most of its calls on to an additional text source that is embedded.
	VwTxtSrc is therefore an abstract class.
	Hungarian: ts
----------------------------------------------------------------------------------------------*/
class VwTxtSrc : public IVwTextSource
{
	friend class VwParagraphBox;
	friend class VwStringBox;
	friend class VwEnv;
	friend class VwNotifier;
	friend class VwOverrideTxtSrc;
	friend class VwTextStore;
public:
	VwTxtSrc();
	virtual ~VwTxtSrc();

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwTextSource method.
	STDMETHOD(GetWsFactory)(ILgWritingSystemFactory ** ppwsf)
	{
		GetWritingSystemFactory(ppwsf);
		return S_OK;
	}

	STDMETHOD(LogToSearch)(int ichlog, int * pichSearch) = 0;
	STDMETHOD(SearchToLog)(int ichSearch, ComBool fAssocPrev, int * pichLog) = 0;
	STDMETHOD(LogToRen)(int ichlog, int * pichRen) = 0;
	STDMETHOD(RenToLog)(int ichRen, int * pichLog) = 0;
	STDMETHOD(SearchToRen)(int ichSearch, ComBool fAssocPrev, int * pichRen) = 0;
	STDMETHOD(RenToSearch)(int ichRen, int * pichSearch) = 0;

	virtual void FetchLog(int ichMin, int ichLim, OLECHAR * prgchBuf) = 0;
	virtual int CStrings() = 0;
	virtual IVwOverlay * Overlay() = 0;
	virtual int Cch() = 0;
	virtual int CchRen() = 0;
	virtual int CchSearch() = 0;
	virtual void ReplaceContents(int itssMin, int itssLim, VwTxtSrc *pts) = 0;
	virtual int IchStartString(int itss) = 0;
	virtual void StringFromIch(int ich, bool fAssocPrev, ITsString ** pptss,
		int *pichMin, int * pichLim, VwPropertyStore ** ppzvps, int * pitss) = 0;
	virtual void StringAtIndex(int itss, ITsString ** pptss) = 0;
	virtual void StyleAtIndex(int itss, VwPropertyStore ** ppzvps) = 0;
	virtual void GetUnderlineInfo(int ich, int * punt, COLORREF * pclrUnder, int * pichLim) = 0;
	virtual VpsTssVec & Vpst() = 0;
	virtual void CharAndPropsAt(int ich, OLECHAR * pch, ITsTextProps ** ppttp) = 0;
	virtual int LogToRen(int ichlog) = 0;
	virtual int RenToLog(int ichren) = 0;
	virtual void AddString(ITsMutString * ptss, VwPropertyStore * pzvps,
		IVwViewConstructor * pvc) = 0;
	virtual VwSourceType SourceType()  = 0;
	virtual void SetWritingSystemFactory(ILgWritingSystemFactory * pwsf) = 0;
	virtual void GetWritingSystemFactory(ILgWritingSystemFactory ** ppwsf) = 0;
	virtual bool IsMapped() = 0;
	virtual bool DoesOverlays() = 0;
	virtual void AdjustOverrideOffsets() = 0;
	virtual CachedProps * GetCharPropInfo(int ich,
		int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
		VwPropertyStore ** ppzvps) = 0;

protected:
	// Member variables
	long m_cref;
	virtual int CchTss(int itss) = 0;
};
DEFINE_COM_PTR(VwTxtSrc);

/*----------------------------------------------------------------------------------------------
	Class: VwSimpleTxtSrc
	This class provides a text source for the rendering engine, and also keeps track of the
	paragraph contents. It consists basically of a sequence of (TsString, VwPropertyStore) pairs.
	This provides the association between each string object and the properties used to display
	it.
	Hungarian: sts
----------------------------------------------------------------------------------------------*/
class VwSimpleTxtSrc : public VwTxtSrc
{
	friend class VwParagraphBox;
	friend class VwStringBox;
	friend class VwEnv;
	friend class VwNotifier;
	// JT: I don't know why, but without this it won't let a VwMappedTxtSrc access the
	// member variables of another VwTxtSrc.
	friend class VwMappedTxtSrc;
public:
	// Static methods
	// For now, it does not look as if we needed to CoCreateInstance of these.
	// static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	VwSimpleTxtSrc();
#if !WIN32
	~VwSimpleTxtSrc()
	{
		// work around TeDllTests hang on exit
		m_qwsf.Detach();
	}
#endif

	// IVwTextSource methods
	STDMETHOD(Fetch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	STDMETHOD(get_Length)(int * pcch);
	STDMETHOD(FetchSearch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	STDMETHOD(get_LengthSearch)(int * pcch);
	STDMETHOD(GetCharProps)(int ich, LgCharRenderProps * pchrp, int * pichMin, int * pichLim);
	STDMETHOD(GetParaProps)(int ich, LgParaRenderProps * pchrp, int * pichMin, int * pichLim);
	STDMETHOD(GetCharStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim);
	STDMETHOD(GetParaStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim);
	STDMETHOD(GetSubString)(int ichMin, int ichLim, ITsString ** pptss);
	STDMETHOD(LogToSearch)(int ichlog, int * pichSearch);
	STDMETHOD(SearchToLog)(int ichSearch, ComBool fAssocPrev, int * pichLog);
	STDMETHOD(RenToLog)(int ichRen, int * pichLog);
	STDMETHOD(LogToRen)(int ichLog, int * pichRen);
	STDMETHOD(RenToSearch)(int ichRen, int * pichSearch);
	STDMETHOD(SearchToRen)(int ichSearch, ComBool fAssocPrev, int * pichRen);

	virtual void FetchLog(int ichMin, int ichLim, OLECHAR * prgchBuf);

	// Other public methods
	virtual int CStrings()
	{
		return m_vpst.Size();
	}

	virtual IVwOverlay * Overlay()
	{
		return NULL;
	}

	// Total number of characters (logical and rendered)
	virtual int Cch();
	virtual int CchRen();
	virtual int CchSearch();

	virtual void ReplaceContents(int itssMin, int itssLim, VwTxtSrc *pts);
	virtual int IchStartString(int itss);
	virtual void StringFromIch(int ich,	bool fAssocPrev, ITsString ** pptss,
		int *pichMin, int * pichLim, VwPropertyStore ** ppzvps, int * pitss);
	virtual void StringAtIndex(int itss, ITsString ** pptss);
	virtual void StyleAtIndex(int itss, VwPropertyStore ** ppzvps);
	virtual void GetUnderlineInfo(int ich, int * punt, COLORREF * pclrUnder, int * pichLim);
	virtual VpsTssVec & Vpst()
	{
		return m_vpst;
	}
	virtual void CharAndPropsAt(int ich, OLECHAR * pch, ITsTextProps ** ppttp);

	// convert between logical and rendered character indexes. Default conversion is trivial;
	// VwMappedTxtSrc overrides.
	virtual int LogToRen(int ichlog)
	{
		return ichlog;
	}
	virtual int RenToLog(int ichren)
	{
		return ichren;
	}
	virtual void AddString(ITsMutString * ptss, VwPropertyStore * pzvps,
		IVwViewConstructor * pvc);

	// Answer the constant that should be passed to the VwParagraphBox constructor
	// to get a paragraph with this kind of text source.
	virtual VwSourceType SourceType() {return kvstNormal;}

	virtual void SetWritingSystemFactory(ILgWritingSystemFactory * pwsf)
	{
		m_qwsf = pwsf;
	}
	virtual void GetWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
	{
		AssertPtr(ppwsf);
		*ppwsf = m_qwsf;
		AddRefObj(*ppwsf);
	}
	virtual bool IsMapped() {return false;}
	virtual bool DoesOverlays() {return false;}
	virtual void AdjustOverrideOffsets() {/* do nothing */ }

protected:
	// Member variables
	VpsTssVec m_vpst;
	LgParaRenderProps m_parp;
	ILgWritingSystemFactoryPtr m_qwsf;

	virtual CachedProps * GetCharPropInfo(int ich,
		int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
		VwPropertyStore ** ppzvps = NULL);
	virtual int CchTss(int itss);
};

DEFINE_COM_PTR(VwSimpleTxtSrc);

class VwOverlayTxtSrc : public VwSimpleTxtSrc
{
public:
	STDMETHOD(GetCharProps)(int ich, LgCharRenderProps * pchrp, int * pichMin, int * pichLim);
	// We use one layer of indirection so we can easily replace the overlay.
	virtual IVwOverlay * Overlay();
	void SetRoot(VwRootBox * prootb)
	{
		m_prootb = prootb;
	}
	virtual void GetUnderlineInfo(int ich, int * punt, COLORREF * pclrUnder, int * pichLim);
	virtual bool DoesOverlays() {return true;}
protected:
	// It has a direct pointer to the root box for efficiency. May be null if not doing overlays.
	VwRootBox * m_prootb;
	virtual VwSourceType SourceType() {return kvstTagged;}
};

DEFINE_COM_PTR(VwOverlayTxtSrc);

class TextMapItem
{
public:
	// One more than the index of the object character in the original text source.
	// This is thus the index of the first character that is offset by (ichren - ichlog)
	// in the rendered string.
	int ichlog;
	ITsStringPtr qtss; // The string that replaces it in the output
	int ichren; // index of the first char following the replacement in the output
	int DichRenLog()
	{
		return ichren - ichlog;
	}
}; // Hungarian vtmi

typedef Vector<TextMapItem> TmiVec; // Hungarian vtmi

/*----------------------------------------------------------------------------------------------
	Class: VwMappedTxtSrc
	This class adds the functionality of handling hot links where the text of the link comes
	from a related object. This requires that for rendering purposes, the string contains
	different characters than for logical purposes. Hence the distinction, arbitrary in the
	other classes, between ichren and ichlog. It maintains a special vector to store the
	strings obtained from the substitutions.

	Although it inherits from VwOverlayTxtSrc, it deliberately suppresses the overlay
	behavior. The inheritance is so that the VwMappedOverlayTxtSrc can turn it back on.
	Hungarian: mts
----------------------------------------------------------------------------------------------*/
class VwMappedTxtSrc : public VwOverlayTxtSrc, public IVwTxtSrcInit2
{
	friend class MappedFetcher;
	friend class MappedSearchFetcher;
	typedef VwOverlayTxtSrc SuperClass;
public:
	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);
	STDMETHOD(Fetch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	STDMETHOD(FetchSearch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	// IVwTxtSrcInit2 method
	STDMETHOD(SetString)(ITsString * ptss, IVwViewConstructor * pvc,
		ILgWritingSystemFactory * pwsf);
	STDMETHOD(LogToSearch)(int ichlog, int * pichSearch);  // override
	STDMETHOD(SearchToLog)(int ichSearch, ComBool fAssocPrev, int * pichLog); // override
	STDMETHOD(RenToLog)(int ichRen, int * pichLog); // override
	STDMETHOD(LogToRen)(int ichLog, int * pichRen); // override
	STDMETHOD(RenToSearch)(int ichRen, int * pichSearch); // override
	STDMETHOD(SearchToRen)(int ichSearch, ComBool fAssocPrev, int * pichRen); // override

	// This function is defined because I am not sure yet whether it will be best to store the
	// actual vector as a member of this class, or just a pointer to it. If most mts's actually
	// have embedded strings, what we have here is better; if not, the other approach will save
	// memory. Always using this function keeps the option open.
	TmiVec & Mapper()
	{
		return m_vtmi;
	}
	virtual void AddString(ITsMutString * ptss, VwPropertyStore * pzvps,
		IVwViewConstructor * pvc);
	virtual int LogToRen(int ichlog);
	virtual int RenToLog(int ichren);
	virtual void ReplaceContents(int itssMin, int itssLim, VwTxtSrc *pts);
	virtual VwSourceType SourceType() {return kvstMappedTagged;}
	virtual IVwOverlay * Overlay() {return NULL;}
	virtual bool IsMapped() {return true;}
	bool OmitTmiFromSearch(int itmi);
	static bool OmitOrcFromSearch(ITsString * ptss, int ich);
protected:
	// member variables
	TmiVec m_vtmi;
	int GetSourceTmi(int ichlog);
	int GetRenderTmi(int ichren);
	virtual CachedProps * GetCharPropInfo(int ich,
		int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
		VwPropertyStore ** ppzvps = NULL);
};

DEFINE_COM_PTR(VwMappedTxtSrc);

/*----------------------------------------------------------------------------------------------
	This class implements a mapped text source which also handles overlays.
	The class hierarchy here is awkward: in principle handling overlays and handling mapping
	are orthogonal. However, historically the overlay handling came first and mapping was added.
	The overlay-capable text source readily handles not having an overlay (they can be turned
	off!). But it was easiest to build the combined functionality as a subclass of the overlay
	class. Then it proves trivial to make a class that does mapping without overlays by
	just changing one method so it always thinks it has no overlay.
	Hungarian: mots
----------------------------------------------------------------------------------------------*/
class VwMappedOverlayTxtSrc : public VwMappedTxtSrc
{
	typedef VwMappedTxtSrc SuperClass;
public:
	virtual IVwOverlay * Overlay() {return VwOverlayTxtSrc::Overlay();}
	virtual VwSourceType SourceType() {return kvstMapped;}
};

/*----------------------------------------------------------------------------------------------
	This class implements a text source which truncates much of the string, because it will be
	used only to display one line of a (possibly very long) string. It can also adjust text
	props to bold one run of the string.
	Hungarian: cts
----------------------------------------------------------------------------------------------*/
class VwConcTxtSrc : public VwMappedOverlayTxtSrc
{
	friend class VwConcParaBox;
	typedef VwMappedOverlayTxtSrc SuperClass;
public:
	void Init(int ichMinItem, int ichLimItem, bool fBold);
	virtual CachedProps * GetCharPropInfo(int ich,
		int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
		VwPropertyStore ** ppzvps = NULL);
	virtual VwSourceType SourceType() {return kvstConc;}
	STDMETHOD(Fetch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	STDMETHOD(FetchSearch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	// Not these ones! They call GetCharPropInfo, which makes the adjustment.
	//STDMETHOD(GetCharProps)(int ich, LgCharRenderProps * pchrp, int * pichMin, int * pichLim);
	//virtual void GetUnderlineInfo(int ich, int * punt, COLORREF * pclrUnder, int * pichLim);

	virtual int LogToRen(int ichlog);
	virtual int RenToLog(int ichren);
	STDMETHOD(LogToSearch)(int ichlog, int * pichSearch);  // override
	STDMETHOD(SearchToLog)(int ichSearch, ComBool fAssocPrev, int * pichLog); // override
	//STDMETHOD(get_Length)(int * pcch)
	//STDMETHOD(get_LengthSearch)(int * pcch)
	//virtual int Cch() { return SuperClass::Cch() - m_cchDiscardInitial - m_cchDiscardFinal; }
	// Adjust a 'start of range' value returned from a baseclass method.
	//STDMETHOD(GetParaProps)(int ich, LgParaRenderProps * pchrp, int * pichMin, int * pichLim)
	//STDMETHOD(GetCharStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim)
	//STDMETHOD(GetParaStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim)
	void AdjustMin(int * pichMin, int cchDiscardInitial)
	{
		// If not returning a value at all, nothing to do.
		if (!pichMin)
			return;
		// Default correction is to subtract the offset
		*pichMin -= cchDiscardInitial;
		// If the minimum we found is before the simulated start, move it to the simulated start.
		if (*pichMin < 0)
			*pichMin = 0;
	}
	void AdjustLim(int *pichLim, int cchDiscardInitial, int cchLim)
	{
		if (!*pichLim)
			return;
		*pichLim -= cchDiscardInitial;
		if (*pichLim > cchLim)
			*pichLim = cchLim;
	}
	STDMETHOD(GetSubString)(int ichMin, int ichLim, ITsString ** pptss)
	{
		// Works as long as the indexes are in the range for the cut-down string.
		return SuperClass::GetSubString(ichMin + m_cchDiscardInitial, ichLim + m_cchDiscardInitial, pptss);
	}

	virtual void FetchLog(int ichMin, int ichLim, OLECHAR * prgchBuf)
	{
		SuperClass::FetchLog(ichMin + m_cchDiscardInitial, ichLim + m_cchDiscardInitial, prgchBuf);
	}

	virtual int IchStartString(int itss)
	{
		// Offset it suitably, but don't answer less than zero or more than the simulated length.
		return std::min(std::max(SuperClass::IchStartString(itss) - m_cchDiscardInitial, 0),
			SuperClass::IchStartString(CStrings()) - m_cchDiscardInitial - m_cchDiscardFinal);
	}
	virtual void StringFromIch(int ich,	bool fAssocPrev, ITsString ** pptss,
		int *pichMin, int * pichLim, VwPropertyStore ** ppzvps, int * pitss)
	{
		SuperClass::StringFromIch(ich + m_cchDiscardInitial, fAssocPrev, pptss, pichMin, pichLim, ppzvps, pitss);
		AdjustMin(pichMin, m_cchDiscardInitial);
		AdjustLim(pichLim, m_cchDiscardInitial, Cch());
	}
	virtual void AddString(ITsMutString * ptss, VwPropertyStore * pzvps,
		IVwViewConstructor * pvc);
	virtual void ReplaceContents(int itssMin, int itssLim, VwTxtSrc *pts);
protected:
	int m_ichMinItem; // start of item to align and bold, relative to ORIGINAL strings
	int m_ichLimItem;
	int m_ichMinItemRen; // similar, but in rendered characters.
	int m_ichLimItemRen;
	bool m_fBold; // true to override property.
	// count of characters to discard at start and end of text source, since we only
	// expect to display the stuff around m_ichMinItem...m_ichLimItem.
	int m_cchDiscardInitial;
	int m_cchDiscardFinal;
	int m_cchDiscardInitialRen; // similar, but rendered characters.
	int m_cchDiscardFinalRen;

	OLECHAR CharAt(OLECHAR * prgchBuf, int bufSize, int & ichStartBuf, int & cchBuf,
		int cch, int ich, bool fForward);
	void AdjustDiscards();

};
DEFINE_COM_PTR(VwConcTxtSrc);

typedef Vector<DispPropOverride> PropOverrideVec;

/*----------------------------------------------------------------------------------------------
	This class implements a text source which wraps another text source, but overrides certain
	display properties. It is currently used to implement display attributes for TSF. We plan
	eventually to use it also to implement things like display of spell check results. Most
	methods are passed straight through to the wrapped text source.
	Hungarian: cts
----------------------------------------------------------------------------------------------*/
class VwOverrideTxtSrc : public VwTxtSrc
{
public:
	VwOverrideTxtSrc(VwTxtSrc * pts)
	{
		m_qts = pts;
	}
	// IVwTextSource methods
	// This one has nothing to do with properties.
	STDMETHOD(Fetch)(int ichMin, int ichLim, OLECHAR * prgchBuf)
		{return m_qts->Fetch(ichMin, ichLim, prgchBuf);}
	STDMETHOD(get_Length)(int * pcch) {return m_qts->get_Length(pcch);}
	STDMETHOD(FetchSearch)(int ichMin, int ichLim, OLECHAR * prgchBuf)
		{return m_qts->FetchSearch(ichMin, ichLim, prgchBuf);}
	STDMETHOD(get_LengthSearch)(int * pcch)
		{return m_qts->get_LengthSearch(pcch);}
	STDMETHOD(GetCharProps)(int ich, LgCharRenderProps * pchrp, int * pichMin, int * pichLim);
	// We don't use this method, and anyway aren't interested in overriding paragraph props.
	STDMETHOD(GetParaProps)(int ich, LgParaRenderProps * pchrp, int * pichMin, int * pichLim)
		{return m_qts->GetParaProps(ich, pchrp, pichMin, pichLim);}
	// For now we aren't supporting overrides of string properties, only integer ones.
	STDMETHOD(GetCharStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim)
		{return m_qts->GetCharStringProp(ich, id, pbstr, pichMin, pichLim);}
	// Not used, and we aren't interested in overriding paragraph props.
	STDMETHOD(GetParaStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim)
		{return m_qts->GetParaStringProp(ich, id, pbstr, pichMin, pichLim);}
	// For copy/Replace purposes ignore any temporary overrides of display properties.
	STDMETHOD(GetSubString)(int ichMin, int ichLim, ITsString ** pptss)
		{return m_qts->GetSubString(ichMin, ichLim, pptss); }
	STDMETHOD(LogToSearch)(int ichlog, int * pichSearch)
		{ return m_qts->LogToSearch(ichlog, pichSearch); }
	STDMETHOD(SearchToLog)(int ichSearch, ComBool fAssocPrev, int * pichLog)
		{ return m_qts->SearchToLog(ichSearch, fAssocPrev, pichLog); }
	STDMETHOD(RenToLog)(int ichRen, int * pichLog)
		{ return m_qts->RenToLog(ichRen, pichLog); }
	STDMETHOD(LogToRen)(int ichLog, int * pichRen)
		{ return m_qts->LogToRen(ichLog, pichRen); }
	STDMETHOD(RenToSearch)(int ichRen, int * pichSearch)
		{ return m_qts->RenToSearch(ichRen, pichSearch); }
	STDMETHOD(SearchToRen)(int ichSearch, ComBool fAssocPrev, int * pichRen)
		{ return m_qts->SearchToRen(ichSearch, fAssocPrev, pichRen); }

	virtual void FetchLog(int ichMin, int ichLim, OLECHAR * prgchBuf)
		{m_qts->FetchLog(ichMin, ichLim, prgchBuf);}
	virtual int CStrings() {return m_qts->CStrings();}
	virtual IVwOverlay * Overlay() {return m_qts->Overlay();}
	virtual int Cch() {return m_qts->Cch();}
	virtual int CchRen() {return m_qts->CchRen();}
	virtual int CchSearch() { return m_qts->CchSearch();}
	virtual void ReplaceContents(int itssMin, int itssLim, VwTxtSrc *pts)
		{m_qts->ReplaceContents(itssMin, itssLim, pts);}
	virtual int IchStartString(int itss) {return m_qts->IchStartString(itss);}
	virtual void StringFromIch(int ich,	bool fAssocPrev, ITsString ** pptss,
		int *pichMin, int * pichLim, VwPropertyStore ** ppzvps, int * pitss)
		{m_qts->StringFromIch(ich, fAssocPrev, pptss, pichMin, pichLim, ppzvps, pitss);}
	virtual void StringAtIndex(int itss, ITsString ** pptss) {m_qts->StringAtIndex(itss, pptss);}
	virtual void StyleAtIndex(int itss, VwPropertyStore ** ppzvps) {m_qts->StyleAtIndex(itss, ppzvps);}
	virtual void GetUnderlineInfo(int ich, int * punt, COLORREF * pclrUnder, int * pichLim);
	virtual VpsTssVec & Vpst() {return m_qts->Vpst();}
	virtual void CharAndPropsAt(int ich, OLECHAR * pch, ITsTextProps ** ppttp)
		{m_qts->CharAndPropsAt(ich, pch, ppttp);}
	virtual int LogToRen(int ichlog) {return m_qts->LogToRen(ichlog);}
	virtual int RenToLog(int ichren) {return m_qts->RenToLog(ichren);}
	virtual void AddString(ITsMutString * ptss, VwPropertyStore * pzvps,
		IVwViewConstructor * pvc)
		{m_qts->AddString(ptss, pzvps, pvc);}
	virtual VwSourceType SourceType()  {return m_qts->SourceType();}
	virtual void SetWritingSystemFactory(ILgWritingSystemFactory * pwsf)
		{m_qts->SetWritingSystemFactory(pwsf);}
	virtual void GetWritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
		{m_qts->GetWritingSystemFactory(ppwsf);}
	virtual bool IsMapped() {return m_qts->IsMapped();}
	virtual bool DoesOverlays() {return m_qts->DoesOverlays();}
	virtual void AdjustOverrideOffsets();
	void SetOverrides(PropOverrideVec & vdpOverrides)
	{
		m_vdpOverrides = vdpOverrides;
	}
	VwTxtSrc * EmbeddedSrc()
	{
		return m_qts;
	}
protected:
	VwTxtSrcPtr m_qts;
	PropOverrideVec m_vdpOverrides;
	// This one is the whole point of the class!
	virtual CachedProps * GetCharPropInfo(int ich,
		int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
		VwPropertyStore ** ppzvps)
		{return m_qts->GetCharPropInfo(ich, pichMin, pichLim, pisbt, pirun, ppttp, ppzvps);}
	virtual int CchTss(int itss) {return m_qts->CchTss(itss);}
	static void Merge(LgCharRenderProps * pchrpOrig, LgCharRenderProps & pchrpOverride);
};

// Marker class which allows us to distinguish override text sources used for spelling squiggles.
class VwSpellingOverrideTxtSrc : public VwOverrideTxtSrc
{
	friend class SpellCheckMethod;
public:
	VwSpellingOverrideTxtSrc(VwTxtSrc * pts) : VwOverrideTxtSrc(pts)
	{
	}
	bool UpdateOverrides(PropOverrideVec & vdpOverrides);
};

// Marker class which allows us to distinguish override text sources used for IME Display Attrs
// (See VwTextStore::DoDisplayAttrs).
class VwImeDisplayAttrsOverrideTxtSrc : public VwOverrideTxtSrc
{
public:
	VwImeDisplayAttrsOverrideTxtSrc(VwTxtSrc * pts) : VwOverrideTxtSrc(pts)
	{
	}
};

// Class that implements a minimal form of IVwTextSource sufficient for FindIn, based on a single TsString.
class TrivialTextSrc : public IVwTextSource, public IVwTxtSrcInit
{
public:
	TrivialTextSrc();
	virtual ~TrivialTextSrc();
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}
	// IVwTextSource methods
	STDMETHOD(Fetch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	STDMETHOD(get_Length)(int * pcch);
	STDMETHOD(FetchSearch)(int ichMin, int ichLim, OLECHAR * prgchBuf);
	STDMETHOD(get_LengthSearch)(int * pcch);
	STDMETHOD(GetCharProps)(int ich, LgCharRenderProps * pchrp, int * pichMin, int * pichLim);
	STDMETHOD(GetParaProps)(int ich, LgParaRenderProps * pchrp, int * pichMin, int * pichLim);
	STDMETHOD(GetCharStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim);
	STDMETHOD(GetParaStringProp)(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim);
	STDMETHOD(GetSubString)(int ichMin, int ichLim, ITsString ** pptss);
	STDMETHOD(GetWsFactory)(ILgWritingSystemFactory ** ppwsf);
	STDMETHOD(LogToSearch)(int ichlog, int * pichSearch);
	STDMETHOD(SearchToLog)(int ichSearch, ComBool fAssocPrev, int * pichLog);
	STDMETHOD(LogToRen)(int ichlog, int * pichRen);
	STDMETHOD(RenToLog)(int ichRen, int * pichLog);
	STDMETHOD(SearchToRen)(int ichSearch, ComBool fAssocPrev, int * pichRen);
	STDMETHOD(RenToSearch)(int ichRen, int * pichSearch);

	// IVwTxtSrcInit2 methods
	STDMETHOD(SetString)(ITsString * ptss);
protected:
	// Member variables
	long m_cref;
	ITsStringPtr m_qtss;
};

DEFINE_COM_PTR(VwOverrideTxtSrc);
DEFINE_COM_PTR(VwSpellingOverrideTxtSrc);
#endif // !VWTXTSRC_INCLUDED
