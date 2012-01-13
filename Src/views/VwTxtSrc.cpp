/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwTxtSrc.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	An implementation of IVwTextSource based on having a VwStringBoxMain for each span in a
	paragraph (or one for the whole paragraph, if no embedded spans), and a list of strings.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)

using namespace std;

#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables
//:>********************************************************************************************

// For error reporting:
static DummyFactory g_fact(_T("SIL.Views.VwTxtSrc"));

//:>********************************************************************************************
//:>	Constructor/Destructor/Initializer
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwTxtSrc::VwTxtSrc()
{
	// COM object behavior
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

VwSimpleTxtSrc::VwSimpleTxtSrc()
{
	memset(&m_parp, 0, sizeof(m_parp));
}

/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
VwTxtSrc::~VwTxtSrc()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP VwTxtSrc::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwTextSource)
		*ppv = static_cast<IVwTextSource *>(this);
	else if (&riid == &CLSID_VwStringTextSource) // kludge for recovering our own implementation.
		*ppv = static_cast<VwTxtSrc *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwTextSource);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	IVwTextSource Methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Get the specified range of text
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::Fetch(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchBuf, ichLim-ichMin);

	FetchLog(ichMin, ichLim, prgchBuf);

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get the text for searching
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::FetchSearch(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchBuf, ichLim - ichMin);

	// Enhnance JohnT: This may conceivably need to be changed to skip owned ORCs, but we are
	// not bothering for now, since a simple text source should never be used for data that
	// may contain these, since it couldn't display the strings properly.
	FetchLog(ichMin, ichLim, prgchBuf);

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Like Fetch, but not overridden by mapped class; gets the logical characters.
----------------------------------------------------------------------------------------------*/
void VwSimpleTxtSrc::FetchLog(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	// Note that we adjust ichMin and ichLim as we go so they are relative to the curr str
	OLECHAR * pch = prgchBuf;
	int csbt = m_vpst.Size();
	for (int isbt = 0; isbt < csbt && ichLim > 0; ++isbt)
	{
		int cch;
		ITsMutString * qtms = m_vpst[isbt].qtms;
		if (qtms)
		{
			CheckHr(qtms->get_Length(&cch));
			if (cch > ichMin)
			{
				// We have some relevant characters. Get the limit in this string
				int ichMinTss = std::max(0, ichMin);
				int ichLimTss = std::min(ichLim, cch);
				CheckHr(qtms->FetchChars(ichMinTss, ichLimTss, pch));
				pch += ichLimTss - ichMinTss;
			}
		}
		else
		{
			cch = 1; // Null stands for an embedded box, which we present as one Obj rep.
			if (cch > ichMin)
			{
				*pch = 0xfffc; // Unicode object replacement character
				pch++;
			}
		}
		ichMin -= cch;
		ichLim -= cch;
	}
}


/*----------------------------------------------------------------------------------------------
	Get the specified character and its properties.
	ich is relative to logical coords.
----------------------------------------------------------------------------------------------*/
void VwSimpleTxtSrc::CharAndPropsAt(int ichMin, OLECHAR * pch, ITsTextProps ** ppttp)
{
	// Note that we adjust ichMin and ichLim as we go so they are relative to the curr str
	Assert(*ppttp == NULL);
	AssertPtr(pch);
	int csbt = m_vpst.Size();
	for (int isbt = 0; isbt < csbt; ++isbt)
	{
		int cch;
		ITsMutString * qtms = m_vpst[isbt].qtms;
		if (qtms)
		{
			CheckHr(qtms->get_Length(&cch));
			if (cch > ichMin)
			{
				// We have some relevant characters. Get the limit in this string
				CheckHr(qtms->FetchChars(ichMin, ichMin + 1, pch));
				TsRunInfo tri;
				CheckHr(qtms->FetchRunInfoAt(ichMin, &tri, ppttp));
				return;
			}
		}
		else
		{
			cch = 1; // Null stands for an embedded box, which we present as one Obj rep.
			if (cch > ichMin)
			{
				*pch = 0xfffc; // Unicode object replacement char; No props stored in string.
				// If cch > ichMin, we must be done with ichMin == 0.  The next step would
				// cause ichMin to go negative, which would have bad side-effects!
				return;
			}
		}
		ichMin -= cch;
	}
	if (ichMin == 0)
	{
		// We're asking for info at the very end of the string, possibly an empty string.
		// Return the info for the end of the last string, and a null character.
		*pch = 0;
		ITsMutString * qtms = m_vpst[csbt - 1].qtms;
		if (qtms != NULL)
		{
			int crun;
			qtms->get_RunCount(&crun);
			qtms->get_Properties(crun - 1, ppttp);
		}
		return;
	}
	Assert(false);	// out of range, don't do this
	*pch = 0;		// but just in case ...
}

/*----------------------------------------------------------------------------------------------
	Add a string to your main collection. The view constructor is not used in the default
	implementation, but only in the mapped text source.
----------------------------------------------------------------------------------------------*/
void VwSimpleTxtSrc::AddString(ITsMutString * ptms, VwPropertyStore * pzvps,
	IVwViewConstructor * pvc)
{
	m_vpst.Push(VpsTssRec(pzvps, ptms));
}


/*----------------------------------------------------------------------------------------------
	Get the properties of a particular character and indicate the range over which they apply.
	It is possible they also apply to a larger range.
	If ppzvps is not null, return the property store which was used to obtain the result
	cached props, either by calling ChrpFor(*ppttp) or, if that is null, Chrp().
	Char indexes are relative to the list of rendered characters.
----------------------------------------------------------------------------------------------*/
CachedProps * VwSimpleTxtSrc::GetCharPropInfo(int ich,
	int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
	VwPropertyStore ** ppzvps)
{
	int cchPrev = 0;
	int ichString = ich; // ich relative to current string
	int csbt = m_vpst.Size();
	Assert(*pirun == 0);
	Assert(*ppttp == NULL);
	CachedProps * pchrp;
	// Note that we adjust ichString as we go so it is eventually relative to current str
	for (int isbt = 0; isbt < csbt; ++isbt)
	{
		int cch;
		ITsMutString * qtms = m_vpst[isbt].qtms;
		if (qtms)
			CheckHr(qtms->get_Length(&cch));
		else
			cch = 1;
		// If it is within this string or exactly at the end of the last string, go ahead
		// here.
		if (cch > ichString ||
			(cch == ichString && isbt == csbt - 1))
		{
			// We have the relevant character. Get the run info.
			VwPropertyStore * pzvps = m_vpst[isbt].qzvps;
			if (m_qwsf)
				pzvps->putref_WritingSystemFactory(m_qwsf);		// Just to be safe.
			if (qtms)
			{
				ITsTextPropsPtr qttp;
				TsRunInfo tri;
				CheckHr(qtms->FetchRunInfoAt(ichString, &tri, &qttp));
				*pichMin = tri.ichMin + cchPrev;
				*pichLim = tri.ichLim + cchPrev;
				// OK, given this ttp, get the corresponding LgCharRenderProps
				pzvps = pzvps->PropertiesForTtp(qttp);
				pchrp = pzvps->Chrp();
				*pirun = tri.irun;
				*ppttp = qttp.Detach();
			}
			else
			{
				// Not a real character in a real string, just a dummy. The run is one char
				// at cchPrev.
				pchrp = pzvps->Chrp();
				*pichMin = cchPrev;
				*pichLim = cchPrev + 1;
				// *pirun = 0; // best approx we can do; leave it how the caller initialized it
				// leave *ppttp null also.
			}
			// OPTIMIZE JohnT: See if subsequent runs have the same props, if so expand range.
			// Ideally we should check for resolving to the same chrp, but
			// it would be more efficient to just check for the same vps and ttp.
			// That would catch most cases.
			// Theoretically, we should also check earlier runs, but
			// it does not matter for current clients, and we don't guarantee to find
			// the largest possible range.
			*pisbt = isbt;
			if (ppzvps)
			{
				*ppzvps = pzvps;
				AddRefObj(pzvps);
			}
			return pchrp;
		}
		ichString -= cch;
		cchPrev += cch;
	}
	// If we drop out the argument is too large

	// Collect some info to track down problems (eg for TE-7714)
	int cch = 0;
	int cchSum = 0;
	StrUni stuText;
	for (int i = 0; i < csbt; i++)
	{
		ITsMutString * qtms = m_vpst[i].qtms;
		if (qtms)
		{
			qtms->get_Length(&cch);
			SmartBstr sbstr;
			qtms->get_Text(&sbstr);
			if (stuText.Length() > 0)
				stuText.Append(L"|");
			stuText.Append(sbstr.Chars(), sbstr.Length());
		}
		else
		{
			cch = 1;
		}
		cchSum += cch;
	}
	StrUni msg;
	msg.Format(L"Argument is too large (Details: ich=%d, csbt=%d, last string length=%d, cumulated string length=%d, string=\"%s\")",
		ich, csbt, cch, cchSum, stuText.Chars());
	ThrowInternalError(E_INVALIDARG, msg.Chars());
	return NULL; // Dummy to keep compiler happy
}

/*----------------------------------------------------------------------------------------------
	Get the properties of a particular character and indicate the range over which they apply.
	It is possible they also apply to a larger range.
	ich is in rendered characters.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::GetCharProps(int ich, LgCharRenderProps * pchrp,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pchrp);
	ChkComArgPtrN(pichLim);
	ChkComArgPtrN(pichMin);

	int isbt = 0;
	int irun = 0;
	ITsTextPropsPtr qttp;
	CachedProps * pchrp2 = GetCharPropInfo(ich, pichMin, pichLim, &isbt, &irun, &qttp);
	// Copy the relevant part of the CachedProps
	CopyBytes(pchrp2, pchrp, isizeof(LgCharRenderProps));

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Simlar, but doesn't need to be a COM method, and we don't care about the min.
	char indexes are in rendered coords.
----------------------------------------------------------------------------------------------*/
void VwSimpleTxtSrc::GetUnderlineInfo(int ich, int * punt,
	COLORREF * pclrUnder, int * pichLim)
{
	AssertPtr(punt);
	AssertPtr(pclrUnder);
	AssertPtr(pichLim);
	int isbt = 0;
	int irun = 0;
	ITsTextPropsPtr qttp;
	int ichMin;
	CachedProps * pchrp2 = GetCharPropInfo(ich, &ichMin, pichLim, &isbt, &irun, &qttp);
	*punt = pchrp2->m_unt;
	*pclrUnder = pchrp2->m_clrUnder;
	if (*pclrUnder == (unsigned long) kclrTransparent)
		*pclrUnder = pchrp2->clrFore;
}

const int kmaxGuids = 1000;

/*----------------------------------------------------------------------------------------------
	Get the properties of a particular character and indicate the range over which they apply.
	It is possible they also apply to a larger range.
	Overridden to merge any changes from the overlay.
	indexes are in rendered characters.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverlayTxtSrc::GetCharProps(int ich, LgCharRenderProps * pchrp,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pchrp);
	ChkComArgPtrN(pichMin);
	ChkComArgPtrN(pichLim);

	int isbt = 0;
	int irun = 0;
	ITsTextPropsPtr qttp;
	CachedProps * pchrp2 = GetCharPropInfo(ich, pichMin, pichLim, &isbt, &irun, &qttp);
	// Copy the relevant part of the CachedProps
	CopyBytes(pchrp2, pchrp, isizeof(LgCharRenderProps));
	// If we aren't doing overlays we are done; also we can't go any further if we have
	// no props for the run (e.g., a grey box separator).
	if (!Overlay() || !qttp)
		return S_OK;
	SmartBstr sbstrGuids;
	CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrGuids));
	OLECHAR * prgchGuids = const_cast<OLECHAR *>(sbstrGuids.Chars());
	int cguid = BstrLen(sbstrGuids) / kcchGuidRepLength;
	OLECHAR * pchEndGuids = prgchGuids + cguid * kcchGuidRepLength; // ignore any surplus

	bool fGotOne = false;

	for (; prgchGuids < pchEndGuids; prgchGuids += kcchGuidRepLength)
	{
		ComBool fHidden;
		COLORREF clrFore;
		COLORREF clrBack;
		COLORREF clrUnder;
		int unt;
		int cchAbbr;
		int cchName;
		CheckHr(Overlay()->GetDispTagInfo(prgchGuids, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
			NULL, 0, // Don't want the abbr at this point
			&cchAbbr,
			NULL, 0, &cchName));
		if (!fHidden)
		{
			if (fGotOne)
			{
				// Merge the properties. We don't care about the underlining ones at this point.
				VwOverlay::MergeOverlayProps1(&pchrp->clrFore, clrFore, &pchrp->clrBack, clrBack);
			}
			else
			{
				// First visible tag at this point. Just use its scheme, unless it is knNinch.
				fGotOne = true;
				if (clrFore != knNinch)
					pchrp->clrFore = clrFore;
				if (clrBack != knNinch)
					pchrp->clrBack = clrBack;
			}
		}
	}

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get length of whole text source in rendered chars
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::get_Length(int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcch);

	*pcch = CchRen();

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get length of whole text source in search chars
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::get_LengthSearch(int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcch);

	return LogToSearch(Cch(), pcch);

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Simlar, but doesn't need to be a COM method, and we don't care about the min.
	Overridden to merge any changes from the overlay.
	indexes are in rendered characters.
----------------------------------------------------------------------------------------------*/
void VwOverlayTxtSrc::GetUnderlineInfo(int ich, int * punt,
	COLORREF * pclrUnder, int * pichLim)
{
	AssertPtr(punt);
	AssertPtr(pclrUnder);
	AssertPtr(pichLim);
	int isbt = 0;
	int irun = 0;
	ITsTextPropsPtr qttp;
	int ichMin;
	CachedProps * pchrp2 = GetCharPropInfo(ich, &ichMin, pichLim, &isbt, &irun, &qttp);
	*punt = pchrp2->m_unt;
	*pclrUnder = pchrp2->m_clrUnder;
	if (*pclrUnder == (unsigned long) kclrTransparent)
		*pclrUnder = pchrp2->clrFore;
	if (!Overlay() || !qttp)
		return;

	// Apply any tagging.
	SmartBstr sbstrGuids;
	CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrGuids));
	OLECHAR * prgchGuids = const_cast<OLECHAR *>(sbstrGuids.Chars());
	int cguid = BstrLen(sbstrGuids) / kcchGuidRepLength;
	OLECHAR * pchEndGuids = prgchGuids + cguid * kcchGuidRepLength; // ignore any surplus

	for (; prgchGuids < pchEndGuids; prgchGuids += kcchGuidRepLength)
	{
		ComBool fHidden;
		COLORREF clrFore;
		COLORREF clrBack;
		COLORREF clrUnder;
		int unt;
		int cchAbbr;
		int cchName;
		CheckHr(Overlay()->GetDispTagInfo(prgchGuids, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
			NULL, 0, // Don't want the abbr at this point
			&cchAbbr,
			NULL, 0, &cchName));
		if (!fHidden)
			// Merge the underlining-related properties.
			VwOverlay::MergeOverlayProps2(pclrUnder, clrUnder, punt, unt);
	}
}


/*----------------------------------------------------------------------------------------------
	// Similarly get paragraph level properties and the range for which they apply.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::GetParaProps(int ich, LgParaRenderProps * pchrp,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	// This gets a string property. Some old writing systems may support using extra props
	// as a way to vary appearance in ways unique to a particular old writing system,
	// such as showing or hiding Greek breathings.
	// It is possible we will eventually handle having the WS look for specified props.
	// It is possible we will define a property in which we make available to the WS
	// information like the reason for italics ('emphasized'; 'foreign LG'; 'book title').
	// Renderers should be prepared not to get anything, or to get values not meaningful
	// to them (intended for some other renderer).
	// The property in question is string-valued and applies to arbitrary char runs.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::GetCharStringProp(int ich, int id, BSTR * pbstr,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr);
	ChkComOutPtr(pichMin);
	ChkComOutPtr(pichLim);

	*pichMin = 0;
	*pichLim = CchRen();

	int isbt = 0;
	int irun = 0;
	ITsTextPropsPtr qttp;
	GetCharPropInfo(ich, pichMin, pichLim, &isbt, &irun, &qttp);
	if (qttp)
		CheckHr(qttp->GetStrPropValue(id, pbstr));

	//pbstr = NULL;

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	// Similar, but a property that is constrained to have the same value for all chars
	// in a paragraph. Some ids may possibly allow both para and char level values.
	Arguments:
		id		which particular property we want (ENHANCE JohnT: which enumeration?)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::GetParaStringProp(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;

	Assert(false);
	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}


//:>********************************************************************************************
//:>	Other public Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Char offset from string index: sum of lengths of previous strings. It is permissible
	to pass an index equal to the number of strings, to obtain the total length.
	Indexes are in logical characters.
----------------------------------------------------------------------------------------------*/
int VwSimpleTxtSrc::IchStartString(int itss)
{
	Assert(itss <= m_vpst.Size());
	int cchPrev = 0; // Haven't found any chars before itss yet.
	for (int isbt = 0; isbt < itss; ++isbt)
	{
		int cch;
		ITsMutString * qtms = m_vpst[isbt].qtms;
		if (qtms)
			CheckHr(qtms->get_Length(&cch));
		else
			cch = 1;
		cchPrev += cch;
	}
	return cchPrev;
}

/*----------------------------------------------------------------------------------------------
	Part of the contents of the para (from itssMin to itssLim of the strings associated
	with it) is to be replaced with the strings in vpst.
	Note that itssLim may be -1 to indicate the last string associated with this.
----------------------------------------------------------------------------------------------*/
void VwSimpleTxtSrc::ReplaceContents(int itssMin, int itssLim, VwTxtSrc *pts)
{
	// Do the replacement.
	if (itssLim < 0)
		itssLim = Vpst().Size();
	VpsTssRec * pbtr = pts->Vpst().Begin();
	int csbtNew = pts->Vpst().Size();
	Vpst().Replace(itssMin, itssLim, pbtr, csbtNew);
	if (m_vpst.Size() == 0)
		ThrowInternalError(E_UNEXPECTED, L"VwSimpleTxtSrc::ReplaceContents removed all para contents - connect report to LT-9233");
}

/*----------------------------------------------------------------------------------------------
	Given an ich relative to your whole text, answer the TsString that contains
	the character at that offset, and the range of characters relative to that string that
	it occupies, and its string box and string index.
	As a special case, if ich is the lim of the whole paragraph, the last string is returned.
----------------------------------------------------------------------------------------------*/
void VwSimpleTxtSrc::StringFromIch(int ich, bool fAssocPrev, ITsString ** pptss, int *pichMin,
	int * pichLim, VwPropertyStore ** ppzvps, int * pitss)
{
	AssertPtr(pptss);
	Assert(!*pptss);
	AssertPtr(pichMin);
	AssertPtr(pichLim);
	AssertPtr(ppzvps);
	AssertPtr(pitss);
	Assert(ich >= 0);
	int cchPrev = 0; // characters in strings before isbt
	int isbt;
	int cch;
	for (isbt = 0; isbt < m_vpst.Size(); ++isbt)
	{
		ITsMutString * ptms = m_vpst[isbt].qtms;
		if (ptms)
			CheckHr(ptms->get_Length(&cch));
		else
			cch = 1; // embedded boxes have no tss and count as 1 char
		if ((ich <= cchPrev + cch && fAssocPrev) || (ich < cchPrev + cch && !fAssocPrev))
		{
			// It's in this string!
			goto Lgotit;
		}
		cchPrev += cch;
	}
	// The only valid reason for dropping out of the loop is an ich that is the lim of the
	// paragraph
	Assert(cchPrev == ich);
	isbt--; // makes it equal to the last string index
	cchPrev -= cch; // back to before the last string
Lgotit:
	*pptss = m_vpst[isbt].qtms;
	AddRefObj(*pptss);
	*pichMin = cchPrev;
	*pichLim = cchPrev + cch;
	*ppzvps = m_vpst[isbt].qzvps;
	AddRefObj(*ppzvps);
	*pitss = isbt;
}

void VwSimpleTxtSrc::StringAtIndex(int itss, ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(*pptss == NULL);
	AssertArray(m_vpst.Begin(), itss);
	*pptss = m_vpst[itss].qtms;
	AddRefObj(*pptss);
}

void VwSimpleTxtSrc::StyleAtIndex(int itss, VwPropertyStore ** ppzvps)
{
	AssertPtr(ppzvps);
	Assert(*ppzvps == NULL);
	*ppzvps = m_vpst[itss].qzvps;
	AddRefObj(*ppzvps);
}

// Logical coords.
int VwSimpleTxtSrc::Cch()
{
	return IchStartString(m_vpst.Size());
}

int VwSimpleTxtSrc::CchRen()
{
	return LogToRen(Cch());
}

int VwSimpleTxtSrc::CchSearch()
{
	int ich;
	CheckHr(LogToSearch(Cch(), &ich));
	return ich;
}

IVwOverlay * VwOverlayTxtSrc::Overlay()
{
	if (m_prootb)
		return m_prootb->Overlay();
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Compute the length of the ith string. If it is null (a separator box) answer 1. This is
	in underlying characters (logical).
----------------------------------------------------------------------------------------------*/
int VwSimpleTxtSrc::CchTss(int ipst)
{
	ITsMutString * ptms = m_vpst[ipst].qtms;
	if (!ptms)
		return 1;
	int cch;
	CheckHr(ptms->get_Length(&cch));
	return cch;
}


//:>********************************************************************************************
//:>	VwMappedTxtSrc methods
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factMts(
	_T("SIL.Views.VwMappedTxtSrc"),
	&CLSID_VwMappedTxtSrc,
	_T("SIL mapped text source"),
	_T("Apartment"),
	&VwMappedTxtSrc::CreateCom);


void VwMappedTxtSrc::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwMappedTxtSrc> qtts;
	qtts.Attach(NewObj VwMappedTxtSrc());		// ref count initialy 1
	CheckHr(qtts->QueryInterface(riid, ppv));
}

STDMETHODIMP VwMappedTxtSrc::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<VwTxtSrc*>(this));
	else if (&riid == &CLSID_VwStringTextSource) // kludge for recovering our own implementation.
		*ppv = static_cast<VwTxtSrc *>(this);
	else if (riid == IID_IVwTextSource)
		*ppv = static_cast<IVwTextSource *>(this);
	else if (riid == IID_IVwTxtSrcInit2)
		*ppv = static_cast<IVwTxtSrcInit2 *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(static_cast<VwTxtSrc*>(this),
			IID_IVwTextSource);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/*----------------------------------------------------------------------------------------------
	Sets the string to be a text source for.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::SetString(ITsString * ptss, IVwViewConstructor * pvc,
	ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);
	ChkComArgPtr(pvc);
	ChkComArgPtr(pwsf);

	m_vpst.Clear();
	m_vtmi.Clear();
	VwPropertyStorePtr qzvps;
	qzvps.Attach(NewObj VwPropertyStore());
	SetWritingSystemFactory(pwsf);
	AddString(ptss, qzvps, pvc);
	END_COM_METHOD(g_factMts, IID_IVwTxtSrcInit2);
}

/*----------------------------------------------------------------------------------------------
	Add a string to your main collection. If it contains any embedded object characters, and
	they have string substitutions, add the appropriate substitutions to your collection.
	Note: pvc may be null iff the string contains no ORCs.
----------------------------------------------------------------------------------------------*/
void VwMappedTxtSrc::AddString(ITsMutString * ptms, VwPropertyStore * pzvps,
	IVwViewConstructor * pvc)
{
	m_vpst.Push(VpsTssRec(pzvps, ptms));
	const OLECHAR * prgch;
	int cch;
	CheckHr(ptms->LockText(&prgch, &cch));
	for (const OLECHAR * pch = prgch; pch < prgch + cch; pch++)
	{
		if (*pch == 0xfffc)
		{
			// See if it has a replacement string
			TsRunInfo tri;
			ITsTextPropsPtr qttp;
			CheckHr(ptms->FetchRunInfoAt(pch - prgch, &tri, &qttp));
			SmartBstr sbstr;
			CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
			if (!sbstr.Length())
				continue;
			const OLECHAR * pchData = sbstr.Chars();
			if (*pchData != kodtNameGuidHot && *pchData != kodtOwnNameGuidHot &&
				*pchData != kodtGuidMoveableObjDisp && *pchData != kodtContextString)
			{
				continue; // only values that produce substitution
			}
			if (sbstr.Length() != 1 + isizeof(GUID) / isizeof(OLECHAR))
			{
				Assert(false);
				continue;
			}
			int cchSubs = 0;
			ITsStringPtr qtss = NULL;
			StrUniBufSmall stuGuid(pchData + 1, 8);
			CheckHr(pvc->GetStrForGuid(stuGuid.Bstr(), &qtss));
			if (!qtss)
			{
				if (*pchData == kodtGuidMoveableObjDisp) // picture
				{
					ITsStrBldrPtr qtsb;
					CheckHr(ptms->GetBldr(&qtsb));
					int cchTmp;
					CheckHr(qtsb->get_Length(&cchTmp));
					CheckHr(qtsb->ReplaceRgch(0, cchTmp, pch, 1, qttp));
					CheckHr(qtsb->GetString(&qtss));
				}
				else
					Assert(false); // Caller failed to generate a valid string for the object
			}
			CheckHr(qtss->get_Length(&cchSubs));
			TextMapItem tmi;
			tmi.ichlog = Cch() - cch + pch - prgch + 1; // index in whole source
			tmi.ichren = tmi.ichlog + cchSubs - 1; // initially just allow for this subs
			tmi.qtss = qtss;
			TmiVec & vtmi = Mapper();
			if (vtmi.Size())
			{
				TextMapItem * ptmi = vtmi.Top();
				// Bump our own offset by the net effect of all previous ones
				tmi.ichren += ptmi->DichRenLog();
			}
			vtmi.Push(tmi);
		}
	}
	CheckHr(ptms->UnlockText(prgch));
}

/*----------------------------------------------------------------------------------------------
	Convert a logical to a search position. For non-substituted characters, give the index
	of the corresponding character in the output string. For substituted characters, give the
	index of the first replacement character.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::LogToSearch(int ichlog, int * pichSearch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichSearch);
	TmiVec & vtmi = Mapper();
	if (!vtmi.Size())
	{
		*pichSearch = ichlog;
		return S_OK;
	}
	int itmi = GetSourceTmi(ichlog);
	// If it exactly matched, we want to use the offset of the one matched;
	// otherwise, we want the offset of the previous one.
	if (itmi >= vtmi.Size() || ichlog != vtmi[itmi].ichlog)
		itmi--;
	if (itmi < 0)
	{
		// implicit no-change item before the first.
		*pichSearch = ichlog;
		return S_OK;
	}
	int ichAns = ichlog + vtmi[itmi].DichRenLog(); // this is what LogToRen would return
	for (int i = 0; (i < itmi) || (i == itmi && ichlog >= vtmi[itmi].ichlog); i++)
	{
		if (OmitTmiFromSearch(i))
		{
			// The value we computed is too large by the length of the string
			int cch;
			CheckHr(m_vtmi[i].qtss->get_Length(&cch));
			ichAns -= cch;
		}
	}
	*pichSearch = ichAns;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

STDMETHODIMP VwConcTxtSrc::LogToSearch(int ichlog, int * pichSearch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichSearch);
	int cchSearchInitial;
	int cchSearchFull;
	CheckHr(SuperClass::LogToSearch(ichlog + m_cchDiscardInitial, &cchSearchFull));
	CheckHr(SuperClass::LogToSearch(m_cchDiscardInitial, &cchSearchInitial));
	*pichSearch = cchSearchFull - cchSearchInitial;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a search to a logical position. For non-substituted characters, give the index
	of the corresponding character in the input string. For substituted characters, give the
	index of the corresponding object replacement character.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::SearchToLog(int ichTarget, ComBool fAssocPrev, int * pichLog)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLog);
	TmiVec & vtmi = Mapper();
	if (!vtmi.Size())
	{
		*pichLog = ichTarget;
		return S_OK;
	}
	int ichLog = 0; // logical characters up to (and including) the orc at vtmi[itmi].ichLog - 1.
	int ichSearch = 0; // search characters up to (and including) the orc at vtmi[itmi].ichLog - 1
	for (int itmi = 0; itmi < vtmi.Size(); itmi++)
	{
		// See whether ichTarget indicates one of the non-substituted characters between ichSearch
		// and the ORC at vtmi[itmi].ichLog - 1. At this point ichSearch is still the count
		// of search characters up to and including the previous ORC (or zero on first iteration).
		// The difference between the two ichLogs (-1) is the number of non-substituted characters.
		int ichLimSearchRun = ichSearch + vtmi[itmi].ichlog - 1 - ichLog;
		if ((fAssocPrev && ichTarget <= ichLimSearchRun) ||
			(!fAssocPrev && ichTarget < ichLimSearchRun))
		{
			*pichLog = ichLog + ichTarget - ichSearch;
			return S_OK;
		}

		int cchSubs = 0;
		if (!OmitTmiFromSearch(itmi))
			CheckHr(vtmi[itmi].qtss->get_Length(&cchSubs));
		ichSearch = ichLimSearchRun + cchSubs;
		ichLog = vtmi[itmi].ichlog;
		if (ichTarget < ichSearch)
		{
			// It's within the substitution; return the index of the ORC
			*pichLog = ichLog - 1;
			return S_OK;
		}
	}
	// If we didn't find it so far, it's beyond the last ORC.
	*pichLog = m_vtmi[m_vtmi.Size() - 1].ichlog + ichTarget - ichSearch;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

STDMETHODIMP VwConcTxtSrc::SearchToLog(int ichTarget, ComBool fAssocPrev, int * pichLog)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLog);

	int cchSearchInitial;
	CheckHr(SuperClass::LogToSearch(m_cchDiscardInitial, &cchSearchInitial));

	int cchLog;
	CheckHr(SuperClass::SearchToLog(ichTarget + cchSearchInitial, fAssocPrev, &cchLog));
	*pichLog = cchLog - m_cchDiscardInitial;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}
/*----------------------------------------------------------------------------------------------
	Convert a render position to a logical position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::RenToLog(int ichRen, int * pichLog)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLog);
	*pichLog = RenToLog(ichRen);
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a logical position to a render position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::LogToRen(int ichLog, int * pichRen)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichRen);
	*pichRen = LogToRen(ichLog);
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a render position to a search position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::RenToSearch(int ichRen, int * pichSearch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichSearch);
	int ichLog = RenToLog(ichRen);
	CheckHr(LogToSearch(ichLog, pichSearch));
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a search position to a render position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::SearchToRen(int ichSearch, ComBool fAssocPrev, int * pichRen)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichRen);
	int ichLog;
	CheckHr(SearchToLog(ichSearch, fAssocPrev, &ichLog));
	*pichRen = LogToRen(ichLog);
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

// Answer true if the orc at m_vtmi[itm].ichlog - 1 should be omitted from searches
bool VwMappedTxtSrc::OmitTmiFromSearch(int itmi)
{
	int ich = m_vtmi[itmi].ichlog - 1;
	ITsStringPtr qtss;
	int ichMin, ichLim, itss;
	VwPropertyStorePtr qzvps;
	StringFromIch(ich, false, &qtss, &ichMin, &ichLim, &qzvps, &itss);
	return OmitOrcFromSearch(qtss, ich - ichMin);
}

// Answer true if the ORC at the indicated index in the indicated string should be
// omitted from text to be searched.
bool VwMappedTxtSrc::OmitOrcFromSearch(ITsString * ptss, int ich)
{
	ITsTextPropsPtr qttp;
	CheckHr(ptss->get_PropertiesAt(ich, &qttp));
	SmartBstr sbstr;
	CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
	if (!sbstr.Length())
		return false; // only omit ORCs with the property we want.
	const OLECHAR * pchData = sbstr.Chars();
	return *pchData == kodtGuidMoveableObjDisp || *pchData == kodtOwnNameGuidHot;
}

/*----------------------------------------------------------------------------------------------
	Convert a logical to a rendering position. For non-substituted characters, give the index
	of the corresponding character in the output string. For substituted characters, give the
	index of the first replacement character.
----------------------------------------------------------------------------------------------*/
int VwMappedTxtSrc::LogToRen(int ichlog)
{
	TmiVec & vtmi = Mapper();
	if (!vtmi.Size())
		return ichlog;
	int itmi = GetSourceTmi(ichlog);
	// If it exactly matched, we want to use the offset of the one matched;
	// otherwise, we want the offset of the previous one.
	if (itmi >= vtmi.Size() || ichlog != vtmi[itmi].ichlog)
		itmi--;
	if (itmi < 0)
		return ichlog; // implicit no-change item before the first.
	return ichlog + vtmi[itmi].DichRenLog();
}

int VwConcTxtSrc::LogToRen(int ichlog)
{
	return SuperClass::LogToRen(ichlog + m_cchDiscardInitial) - m_cchDiscardInitialRen;
}

/*----------------------------------------------------------------------------------------------
	Convert a rendering to a logical position. For non-substituted characters, give the index
	of the corresponding character in the input string. For substituted characters, give the
	index of the corresponding object replacement character.
----------------------------------------------------------------------------------------------*/
int VwMappedTxtSrc::RenToLog(int ichren)
{
	TmiVec & vtmi = Mapper();
	if (!vtmi.Size())
		return ichren;
	int itmi = GetRenderTmi(ichren);
	// vtmi[itmi].ichren >= ichren, and vtmi[itmi-1].ichren < ichren, if itmi and
	// itmi-1 are in range. Otherwise only the other condition holds.

	if (itmi == vtmi.Size())
	{
		// it is after the last substitution. Subtract the appropriate offset
		return ichren - vtmi[itmi - 1].DichRenLog();
	}

	// Otherwise we have a valid itmi. Check for an exact match.
	if (ichren == vtmi[itmi].ichren)
	{
		// It is the exact first char after the itmi'th subs. Return the corresponding
		// logical position.
		return vtmi[itmi].ichlog;
	}

	// The position we need is computed from the offset of previous itmi, or unchanged
	// if there is no previous one.
	int ichOut = ichren;
	if (itmi)
	{
		ichOut -= vtmi[itmi - 1].DichRenLog();
	}

	// but, if that gives a value greater than the position of the
	// itmi'th object character, our rendered character must be IN the substitute tss.
	// Therefore return the actual index of the object character.
	if (ichOut >= vtmi[itmi].ichlog)
		return vtmi[itmi].ichlog - 1;
	else
		return ichOut;
}

int VwConcTxtSrc::RenToLog(int ichren)
{
	return SuperClass::RenToLog(ichren + m_cchDiscardInitialRen) - m_cchDiscardInitial;
}
/*----------------------------------------------------------------------------------------------
	Find the index of the tmi whose ichlog is equal to the argument, if any.
	Otherwise, find an index such that its own ichlog is > the argument, and the previous
	ichlog is less;
	or, answer 0 if the argument is less than the first ichlog, or m_vtmi.Size() if it is
	greater than the last (or there are none);
----------------------------------------------------------------------------------------------*/
int VwMappedTxtSrc::GetSourceTmi(int ichlog)
{
	int itmiMin = 0;
	int itmiLim = m_vtmi.Size();

	// Perform a binary search
	// Loop invariant: itmiMin <= itmiT < itmiLim, or itmiMin = itmiT = itmiLim,
	// where itmiT is the target.
	while (itmiMin < itmiLim)
	{
		int itmiT = (itmiMin + itmiLim) >> 1;
		if (m_vtmi[itmiT].ichlog < ichlog)
			itmiMin = itmiT + 1;
		else
			itmiLim = itmiT;
	}
	return itmiMin;
}

/*----------------------------------------------------------------------------------------------
	Similar, but based on ichren
----------------------------------------------------------------------------------------------*/
int VwMappedTxtSrc::GetRenderTmi(int ichren)
{
	int itmiMin = 0;
	int itmiLim = m_vtmi.Size();

	// Perform a binary search
	while (itmiMin < itmiLim)
	{
		int itmiT = (itmiMin + itmiLim) >> 1;
		if (m_vtmi[itmiT].ichren < ichren)
			itmiMin = itmiT + 1;
		else
			itmiLim = itmiT;
	}
	// It is possible that some logical characters map to empty strings, and hence, that
	// multiple adjacent Tmi's have the same ichren. In this case, we want to return the
	// largest, since returning a logical character index for an invisible character causes
	// infinite loops in paragraph layout.
	while (itmiMin < m_vtmi.Size() - 1 && m_vtmi[itmiMin + 1].ichren == ichren)
		itmiMin++;
	return itmiMin;
}

// Implementation class for VwMappedTxtSrc::Fetch, subclassed for VwMappedTxtSrc::FetchSearch
class MappedFetcher
{
public:
	VwMappedTxtSrc * m_psrc;
	int ichrenMin; // start of range we want
	int ichrenLim; // end of range we want
	OLECHAR * prgchBuf; // buffer to put output into
	OLECHAR * pch; // where to put next stuff in prgchBuf
	int csbt; // m_vpst.Size()
	OLECHAR * pchLim; // limit of output buffer (prgchBuf)
	// index to next substitution that might be relevant. Incremented when we skip
	// or copy one.
	int itmi;
	// Index of output chars we have processed (including ones before ichrenMin).
	int ichren;
	// Index of logical chars we have processed (from start of string)
	int ichlog;
	TmiVec & vtmi; // Mapper()
	// Adjusted when we change itmi; index of next ORC.
	int ichlogNextObj;
	int ibst; // main loop index into m_vpst;
	int ichlogMinThisString;
	ITsMutString * qtms; // current string
	// Remaining characters in this string.
	int cchlogThis;

	MappedFetcher(int ichMin, int ichLim, OLECHAR * prgchOut, VwMappedTxtSrc * psrc)
		: vtmi(psrc->Mapper())
	{
		m_psrc = psrc;
		ichrenMin = ichMin;
		ichrenLim = ichLim;
		prgchBuf = prgchOut;
	}

	// This is the key function that MappedSearchFetcher overrides.
	virtual bool WantCharsFromObject(ITsMutString * ptms, int ich)
	{
		return true;
	}

	HRESULT Run()
	{
		pch = prgchBuf;
		csbt = m_psrc->m_vpst.Size();
		pchLim = prgchBuf + (ichrenLim - ichrenMin);
		itmi = 0;
		ichren = 0;
		ichlog = 0;
		ichlogNextObj = vtmi.Size() ? vtmi[0].ichlog - 1 : INT_MAX;

		for (int isbt = 0; isbt < csbt && pch < pchLim; ++isbt)
		{
			// Get whatever is useful from this string (and its embedded strings,
			// if any)
			ichlogMinThisString = ichlog;
			qtms = m_psrc->m_vpst[isbt].qtms;
			if (qtms)
			{
				// starting point in this string.
				CheckHr(qtms->get_Length(&cchlogThis));
				while (ichlog < ichlogMinThisString + cchlogThis && pch < pchLim)
				{
					// process a (non-substituted) segment of this string,
					// or a substitute string.
					// Determine what range of what string to copy in these three variables.
					// Default is to copy from next unused char in the base string up to
					// the end of it.
					int ichMinSource = ichlog - ichlogMinThisString;
					int ichLimSource = cchlogThis;
					ITsMutString * ptmsSource = qtms;
					if (ichLimSource + ichlogMinThisString > ichlogNextObj)
					{
						// We can't just copy the rest of this string, because a character
						// we need to substitute comes first.
						if (ichlog == ichlogNextObj)
						{
							// the very next character is an embedded string: insert that.
							ptmsSource = vtmi[itmi].qtss;
							int ichObj = ichMinSource; // save before changing ichMinSource!
							ichMinSource = 0;
							CheckHr(ptmsSource->get_Length(&ichLimSource));
							itmi++;
							ichlogNextObj = itmi < vtmi.Size() ? vtmi[itmi].ichlog - 1 : INT_MAX;
							ichlog++;
							if (!WantCharsFromObject(qtms, ichObj))
							{
								ichLimSource = ichMinSource; // suppresses copy.
							}
						}
						else
						{
							ichLimSource = ichlogNextObj - ichlogMinThisString;
							ichlog += ichLimSource - ichMinSource;
						}
					}
					else
					{
						// We can copy (the rest of) this whole string to the output
						ichlog += ichLimSource - ichMinSource;
					}
					// The last character index in the overall source covered by the
					// current chunk. The chunk we are proposing to copy, if we want it
					// all, runs from ichren to ichrenLimTmp. This will also be the new
					// value of ichren, after processing this chunk of output.
					int ichrenLimTmp = ichren + ichLimSource - ichMinSource;
					if (ichren < ichrenMin)
					{
						// we don't want characters before ichrenMin. This addition
						// may push us past ichLimSource, in which case this iteration
						// copies nothing.
						ichMinSource += ichrenMin - ichren; // skip ones not wanted
					}
					ichren = ichrenLimTmp; // done with old value
					// Don't copy more than will fit.
					// (If it does not all fit, ichlog may have been advanced too far,
					// but it does not matter because if we fill the buffer all loops exit.)
					if (ichLimSource - ichMinSource > pchLim - pch)
						ichLimSource = ichMinSource + (pchLim - pch);
					// See if there is anything to actually copy from this section of source.
					if (ichLimSource > ichMinSource)
					{
						CheckHr(ptmsSource->FetchChars(ichMinSource, ichLimSource, pch));
						pch += ichLimSource - ichMinSource;
					}
				}
			}
			else // !qtmsMutString
			{
				// Null stands for an embedded box, which we present as one Obj rep char.
				// Since there is no string it can't be substituted.
				if (ichren >= ichrenMin)
				{
					*pch = 0xfffc; // Unicode object replacement character
					pch++;
				}
				ichlog += 1;
				ichren += 1;
			}
		}
		return S_OK;
	}
};

class MappedSearchFetcher : public MappedFetcher
{
public:
	MappedSearchFetcher(int ichMin, int ichLim, OLECHAR * prgchOut, VwMappedTxtSrc * psrc)
		: MappedFetcher(ichMin, ichLim, prgchOut, psrc)
	{
	}

	virtual bool WantCharsFromObject(ITsMutString * ptms, int ich)
	{
		return !m_psrc->OmitOrcFromSearch(ptms, ich);
	}
};

/*----------------------------------------------------------------------------------------------
	Get the specified range of text
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::Fetch(int ichrenMin, int ichrenLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchBuf, ichrenLim - ichrenMin);

	MappedFetcher fetcher(ichrenMin, ichrenLim, prgchBuf, this);
	return fetcher.Run();

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

STDMETHODIMP VwConcTxtSrc::Fetch(int ichrenMin, int ichrenLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;

	return SuperClass::Fetch(ichrenMin + m_cchDiscardInitialRen, ichrenLim + m_cchDiscardInitialRen,
		prgchBuf);

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get the entire text for a search
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwMappedTxtSrc::FetchSearch(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchBuf, ichLim - ichMin);

	MappedSearchFetcher fetcher(ichMin, ichLim, prgchBuf, this);
	return fetcher.Run();

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

STDMETHODIMP VwConcTxtSrc::FetchSearch(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;

	int delta;
	CheckHr(LogToSearch(m_cchDiscardInitial, &delta));
	return SuperClass::FetchSearch(ichMin + delta, ichLim + delta, prgchBuf);

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

void VwMappedTxtSrc::ReplaceContents(int itssMin, int itssLim, VwTxtSrc * pts)
{
	// Adjust index
	if (itssLim < 0)
		itssLim = m_vpst.Size();
	TmiVec & vtmi = Mapper();
	// Get total length of replaced stuff
	int cchlogOld = 0;
	for (int ipst = itssMin; ipst < itssLim; ipst++)
		cchlogOld += CchTss(ipst);
	int cchlogNew = pts->Cch();
	//-int cchrenNew = pts->CchRen();

	int ichlogMinOld = IchStartString(itssMin);
	int ichlogLimOld = ichlogMinOld + cchlogOld;

	int itmiMin = GetSourceTmi(ichlogMinOld);
	int itmiLim = GetSourceTmi(ichlogLimOld);
	if (itmiLim < vtmi.Size() && vtmi[itmiLim].ichlog == ichlogLimOld)
	{
		// The limit is exactly at this itmi. Since it is actually for the
		// previous character, we need to increment the limit, so that tmi actually
		// gets replaced.
		itmiLim++;
	}
	// For each itmi,
	// vtmi[itmi].ichlog >= ichlog, and vtmi[itmi-1].ichlog < ichlog, if itmi and
	// itmi-1 are in range. Otherwise only the other condition holds.
	// This gives us exactly the range of items to delete.

	// Figure the lengths of those strings, and use to compute cchrenOld
	int cchrenOld = cchlogOld;
	for (int itmi = itmiMin; itmi < itmiLim; itmi++)
	{
		// We could figure this from the various ich values, but there are all
		// kinds of special cases at the start and end of the vector...
		int cchStr;
		CheckHr(vtmi[itmi].qtss->get_Length(&cchStr));
		cchrenOld += cchStr - 1; // subtract 1 for the object char itself.
	}
	// If pts is mapping, insert its tmi's, replacing the ones deleted.
	// Adjust the inserted ones to allow for the number of log and ren
	// characters ahead of them.
	// Otherwise, just delete any tmi's that belong to the inserted range.
	// Update itmiLim so that it indicates the limit of the inserted tmis
	// (or equals itmiMin, if nothing was inserted).
	VwMappedTxtSrc * pmts = dynamic_cast<VwMappedTxtSrc *>(pts);
	// Compute the amount that the number of rendered characters exceeds logical,
	// at the start of the "tail", the part of the source not being replaced.
	int dichRenLogOld = 0;
	if (itmiLim > 0)
		dichRenLogOld = vtmi[itmiLim - 1].DichRenLog();
	if (pmts)
	{
		vtmi.Replace(itmiMin, itmiLim, pmts->Mapper().Begin(), pmts->Mapper().Size());
		itmiLim = itmiMin + pmts->Mapper().Size();
		// Number of logical and rendered characters that have been placed in front
		// of the inserted items
		int dichlog = ichlogMinOld;
		int dichren = ichlogMinOld;
		if (itmiMin > 0)
		{
			// At least one object character before this one. The last one indicates
			// the total number of extra rendered characters
			dichren += vtmi[itmiMin - 1].DichRenLog();
		}
		for (int itmi = itmiMin; itmi < itmiLim; itmi++)
		{
			vtmi[itmi].ichlog += dichlog;
			vtmi[itmi].ichren += dichren;
		}
	}
	else
	{
		vtmi.Replace(itmiMin, itmiLim, NULL, 0);
		itmiLim = itmiMin;
	}
	// Adjust char positions in any tmi's that come after the replacement.
	// dichlog is determined by the number of characters replaced.
	int dichlog = cchlogNew - cchlogOld;
	int dichRenLogNew = 0;
	if (itmiLim > 0)
		dichRenLogNew = vtmi[itmiLim - 1].DichRenLog();
	// The amount we have to adjust any render positions is the difference between
	// the old and new logical/render discrepancies at the end of the replaced string,
	// plus the actual difference in logical characters.
	int dichren = dichRenLogNew - dichRenLogOld + dichlog;
	for (int itmi = itmiLim; itmi < vtmi.Size(); itmi++)
	{
		vtmi[itmi].ichlog += dichlog;
		vtmi[itmi].ichren += dichren;
	}

	// Do the actual replacement in m_vpst
	VpsTssRec * pbtr = pts->Vpst().Begin();
	int csbtNew = pts->Vpst().Size();
	m_vpst.Replace(itssMin, itssLim, pbtr, csbtNew);
	if (m_vpst.Size() == 0)
		ThrowInternalError(E_UNEXPECTED, L"VwMappedTxtSrc::ReplaceContents removed all para contents - connect report to LT-9233");
}

/*----------------------------------------------------------------------------------------------
	Get the properties of a particular character and indicate the range over which they apply.
	It is possible they also apply to a larger range.
	Char indexes are relative to the list of rendered characters.
----------------------------------------------------------------------------------------------*/
CachedProps * VwMappedTxtSrc::GetCharPropInfo(int ichren,
	int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
	VwPropertyStore ** ppzvps)
{
	int ichlog = RenToLog(ichren);
	VwPropertyStorePtr qzvps;
	CachedProps * pcpResult =
		SuperClass::GetCharPropInfo(ichlog, pichMin, pichLim, pisbt, pirun, ppttp, &qzvps);
	*pichMin = LogToRen(*pichMin);
	*pichLim = LogToRen(*pichLim);
	VwPropertyStore * pzvps = qzvps; // Keep even if we detach
	if (ppzvps)
		*ppzvps = qzvps.Detach();
	// That does it unless we are actually looking at an object character.
	int itmi = GetSourceTmi(ichlog);
	TmiVec vtmi = Mapper();
	if (itmi >= vtmi.Size())
		return pcpResult;
	if (ichlog == vtmi[itmi].ichlog)
	{
		// We are looking at the character right after an ORC..we found
		// the mapping for the previous ORC. That mapping can't tell us
		// whether this character is one, however...that depends on whether
		// we the next mapping has a position one greater than ours.
		itmi++;
		if (itmi >= vtmi.Size())
			return pcpResult;
	}

	// The TMI whose ichlog is greater than ichlog will be exactly one character
	// greater iff ichlog is itself an ORC, because ichlog is always one more
	// than the index of the ORC.
	if (ichlog != vtmi[itmi].ichlog - 1)
		return pcpResult;
	// It's an actual obejct character. We have to check for info from the embedded string
	// as well.
	int ichrenStart = LogToRen(ichlog);
	int ichSubs = ichren - ichrenStart;
	TsRunInfo tri;
	ITsTextPropsPtr qttp;
	CheckHr(vtmi[itmi].qtss->FetchRunInfoAt(ichSubs, &tri, &qttp));
	// Get the property store obtained by applying the properties of the object character
	VwPropertyStore * pzvps2 = pzvps->PropertiesForTtp(*ppttp);
	// And then the properties of the actual run of the replacement string
	VwPropertyStore * pzvps3 = pzvps2->PropertiesForTtp(qttp);
	if (ppzvps)
	{
		(*ppzvps)->Release();
		*ppzvps = pzvps3;
		(*ppzvps)->AddRef();
	}
	ReleaseObj(*ppttp);
	*ppttp = qttp.Detach();
	return pzvps3->Chrp();
}

/*----------------------------------------------------------------------------------------------
	Get the properties of a particular character and indicate the range over which they apply.
	It is possible they also apply to a larger range.
	If ppzvps is not null, return the property store which was used to obtain the result
	cached props, either by calling ChrpFor(*ppttp) or, if that is null, Chrp().
	Char indexes are relative to the list of rendered characters.
----------------------------------------------------------------------------------------------*/
CachedProps * VwConcTxtSrc::GetCharPropInfo(int ichReal,
	int * pichMin, int * pichLim, int * pisbt, int * pirun, ITsTextProps ** ppttp,
	VwPropertyStore ** ppzvps)
{
	int ich = ichReal + m_cchDiscardInitialRen;
	VwPropertyStorePtr qzvps;
	CachedProps * pchrp;
	pchrp = SuperClass::GetCharPropInfo(ich, pichMin, pichLim, pisbt, pirun, ppttp, &qzvps);
	AdjustMin(pichMin, m_cchDiscardInitialRen);
	AdjustLim(pichLim, m_cchDiscardInitialRen, CchRen());

	if (ppzvps)
	{
		*ppzvps = qzvps;
		(*ppzvps)->AddRef();
	}
	// If we aren't making any modification, return the result we got.
	if (!m_fBold)
		return pchrp;
	// If the key character is in the range to modify, do the modification
	if (ich >= m_ichMinItemRen && ich < m_ichLimItemRen)
	{
		VwPropertyStorePtr qzvpsBold;
		CheckHr(qzvps->ComputedPropertiesForInt(ktptBold, ktpvEnum, kttvForceOn, &qzvpsBold));
		pchrp = qzvpsBold->ChrpFor(*ppttp);
		if (ppzvps)
		{
			(*ppzvps)->Release(); // not returning orginal value, don't ret ref count on it
			*ppzvps = qzvpsBold;
			(*ppzvps)->AddRef();
		}
		// Now fake out the run length: it has to be inside the modified range.
		if (*pichMin < m_ichMinItemRen - m_cchDiscardInitialRen)
			*pichMin = m_ichMinItemRen - m_cchDiscardInitialRen;
		if (*pichLim > m_ichLimItemRen - m_cchDiscardInitialRen)
			*pichLim = m_ichLimItemRen - m_cchDiscardInitialRen;
		return pchrp;
	}
	// OK, we're not in the range. But we may still need to adjust the run limits
	// If the run we're returning is entirely before or entirely after the modified bit,
	// we can return as usual.
	if (*pichLim <= m_ichMinItemRen - m_cchDiscardInitialRen)
		return pchrp;
	if (*pichMin >= m_ichLimItemRen - m_cchDiscardInitialRen)
		return pchrp;
	// OK, there is some overlap.
	if (ich < m_ichMinItemRen)
		*pichLim = m_ichMinItemRen - m_cchDiscardInitialRen; // end of run overlaps modified, fix it
	else
		*pichMin = m_ichLimItemRen - m_cchDiscardInitialRen; // start of run overlaps
	return pchrp;
}

void VwConcTxtSrc::Init(int ichMinItem, int ichLimItem, bool fBold)
{
	m_ichMinItem = m_ichMinItemRen = ichMinItem; // Make both variables the same till we add some text.
	m_ichLimItem = m_ichLimItemRen = ichLimItem;
	m_fBold = fBold;
}

// Given ich, an index into the whole range of characters accessible to FetchLog,
// and a buffer of the specified size which holds a subset of cchBuf of those characters
// starting at ichStartBuf, return the character at ich in the main array.
// If this character is not already in the buffer, load it with up to bufSize characters
// in the direction which fForward indicates we are iterating.
OLECHAR VwConcTxtSrc::CharAt(OLECHAR * prgchBuf, int bufSize, int & ichStartBuf, int & cchBuf,
	int cch, int ich, bool fForward)
{
	if (ich < ichStartBuf || ich >= ichStartBuf + cchBuf)
	{
		// reload the buffer
		int ichMin, ichLim;
		if (fForward)
		{
			ichMin = ich;
			ichLim = min(cch, ich + bufSize);
		}
		else
		{
			ichLim = ich + 1;
			ichMin = max(ichLim - bufSize, 0);
		}
		cchBuf = ichLim - ichMin;
		ichStartBuf = ichMin;
		FetchLog(ichMin, ichLim, prgchBuf);
	}
	return prgchBuf[ich - ichStartBuf];
}

#define cchInitial 150 // minimum number of characters of initial context we want.
#define cchFinal 200 // minimum number of characters of final context we want.
/*----------------------------------------------------------------------------------------------
	When we add a string to a conc para, we need to decide whether to adjust the discard
	counts.
----------------------------------------------------------------------------------------------*/
void VwConcTxtSrc::AddString(ITsMutString * ptss, VwPropertyStore * pzvps,
		IVwViewConstructor * pvc)
{
	// Reset everything before doing the superclass addString; AdjustDiscards will recompute,
	// and it is vital that correct offsets relative to the total string are calculated for
	// any object characters we find in the new one.
	m_cchDiscardFinal = m_cchDiscardInitial = m_cchDiscardFinalRen = m_cchDiscardInitialRen = 0;
	SuperClass::AddString(ptss, pzvps, pvc);
	AdjustDiscards();
}

/*----------------------------------------------------------------------------------------------
	Part of the contents of the para (from itssMin to itssLim of the strings associated
	with it) is to be replaced with the strings in vpst.
	Note that itssLim may be -1 to indicate the last string associated with this.
----------------------------------------------------------------------------------------------*/
void VwConcTxtSrc::ReplaceContents(int itssMin, int itssLim, VwTxtSrc *pts)
{
	SuperClass::ReplaceContents(itssMin, itssLim, pts);
	AdjustDiscards();
}

void VwConcTxtSrc::AdjustDiscards()
{
	// Clear both of these by default; also allow us to get a true cch.
	m_cchDiscardInitial = 0;
	m_cchDiscardFinal = 0;
	int cch = Cch();
	UChar32 uch32;
	int ichStartBuf = 0; // doesn't matter, empty to start with
	int cchBuf = 0; // none loaded yet.
	ILgCharacterPropertyEnginePtr qcpe;
	LgGeneralCharCategory gcc;
	if (m_ichMinItem > cchInitial)
	{
		qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
		const int bufSize = cchInitial * 3/2;
		OLECHAR rgchBuf[bufSize];
		int cchBase = 0; // count of base characters from ich to m_ichMinItem
		int ich = m_ichMinItem - 1;
		for (; ich > 0 && cchBase < cchInitial; --ich)
		{
			OLECHAR ch = CharAt(rgchBuf, bufSize, ichStartBuf, cchBuf, cch, ich, false);
			if (IsLowSurrogate(ch) && ich > 0)
			{
				OLECHAR chPrev = CharAt(rgchBuf, bufSize, ichStartBuf, cchBuf, cch, ich - 1, false);
				bool fOk = FromSurrogate(chPrev, ch, (uint *)&uch32);
				if (fOk)
					--ich;
				else
					uch32 = (unsigned)ch;
			}
			else
			{
				uch32 = (unsigned)ch;
			}
			// Check for a diacritic mark.
			CheckHr(qcpe->get_GeneralCategory(uch32, &gcc));
			if (gcc < kccMn || gcc > kccMe)
				cchBase++;
		}
		m_cchDiscardInitial = max(ich, 0);
	}
	if (cch - m_ichLimItem > cchFinal)
	{
		qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
		const int bufSize = cchFinal * 3/2;
		OLECHAR rgchBuf[bufSize];
		int cchBase = 0; // count of base characters from m_ichLimItem to ich
		int ich = m_ichLimItem;
		int ichLastBase = ich;
		for (; ich < cch && cchBase <= cchFinal; ++ich)
		{
			OLECHAR ch = CharAt(rgchBuf, bufSize, ichStartBuf, cchBuf, cch, ich, true);
			if (IsHighSurrogate(ch) && ich < cch - 1)
			{
				OLECHAR chNext = CharAt(rgchBuf, bufSize, ichStartBuf, cchBuf, cch, ich - 1, true);
				bool fOk = FromSurrogate(ch, chNext, (uint *)&uch32);
				if (fOk)
					++ich;
				else
					uch32 = (unsigned)ch;
			}
			else
			{
				uch32 = (unsigned)ch;
			}
			// Check for a diacritic mark.
			CheckHr(qcpe->get_GeneralCategory(uch32, &gcc));
			if (gcc < kccMn || gcc > kccMe)
			{
				cchBase++;
				ichLastBase = ich;
			}
		}
		if (ich < cch) // stopped because we found enough base characters. Our limit needs to be a base character.
			m_cchDiscardFinal = cch - ichLastBase;
		else
			m_cchDiscardFinal = 0;
	}
	m_cchDiscardInitialRen = SuperClass::LogToRen(m_cchDiscardInitial);
	// Figure the index in the original rendered characters of the first discard, and subtract from
	// the original rendered length.
	m_cchDiscardFinalRen = SuperClass::CchRen() - SuperClass::LogToRen(SuperClass::Cch() - m_cchDiscardFinal);
	m_ichMinItemRen = SuperClass::LogToRen(m_ichMinItem);
	m_ichLimItemRen = SuperClass::LogToRen(m_ichLimItem);
}


/*----------------------------------------------------------------------------------------------
	Get the properties of a particular character and indicate the range over which they apply.
	It is possible they also apply to a larger range.
	ich is in rendered characters.
	Note: irun refers to the original, underlying string; it is not altered to reflect any
	fake runs produced by the overridden properties. (Most callers don't use this argument.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwOverrideTxtSrc::GetCharProps(int ich, LgCharRenderProps * pchrp,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pchrp);
	ChkComArgPtrN(pichLim);
	ChkComArgPtrN(pichMin);

	// Set idp to the largest index into m_vdpOverrides such that
	// ich < m_vdpOverrides[idp].ichLim; or to m_vdpOverrides.Size().
	int idp = 0;
	for (; idp < m_vdpOverrides.Size(); idp++)
	{
		if (ich < m_vdpOverrides[idp].ichLim)
			break;
	}
	if (idp < m_vdpOverrides.Size() && ich >= m_vdpOverrides[idp].ichMin)
	{
		CheckHr(m_qts->GetCharProps(ich, pchrp, pichMin, pichLim));
		VwOverrideTxtSrc::Merge(pchrp, m_vdpOverrides[idp].chrp);

		// index passed is in an override range.
		*pichMin = m_vdpOverrides[idp].ichMin;
		*pichLim = m_vdpOverrides[idp].ichLim;
		return S_OK;
	}

	// Otherwise get the ordinary result...
	CheckHr(m_qts->GetCharProps(ich, pchrp, pichMin, pichLim));
	// ...but the range may have to be reduced
	if (idp > 0 && *pichMin < m_vdpOverrides[idp - 1].ichLim)
		*pichMin = m_vdpOverrides[idp - 1].ichLim;
	if (idp < m_vdpOverrides.Size() && *pichLim > m_vdpOverrides[idp].ichMin)
		*pichLim = m_vdpOverrides[idp].ichMin;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}


/*----------------------------------------------------------------------------------------------
	Update the overrides to the given vector, answering true if there was a change in offsets
	or underline color (spell checking always uses same value for other chrps).
	Return true if any override offsets changed.
----------------------------------------------------------------------------------------------*/
bool VwSpellingOverrideTxtSrc::UpdateOverrides(PropOverrideVec & vdpOverrides)
{
	bool fSame = m_vdpOverrides.Size() == vdpOverrides.Size();
	for (int i = 0; fSame && i < vdpOverrides.Size(); i++)
	{
		fSame = vdpOverrides[i].ichMin == m_vdpOverrides[i].ichMin
			&& vdpOverrides[i].ichLim == m_vdpOverrides[i].ichLim
			&& vdpOverrides[i].chrp.clrUnder == m_vdpOverrides[i].chrp.clrUnder;
	}
	m_vdpOverrides = vdpOverrides;
	return !fSame;
}


/*----------------------------------------------------------------------------------------------
	Merge the override properties with the original properties. Properties set to knNinch
	or -1 are not merged; ws, fWsRtl, nDirDepth are never merged.
----------------------------------------------------------------------------------------------*/
void VwOverrideTxtSrc::Merge(LgCharRenderProps * pchrpOrig, LgCharRenderProps & pchrpOverride)
{
	if (pchrpOverride.clrFore != knNinch)
		pchrpOrig->clrFore = pchrpOverride.clrFore;
	if (pchrpOverride.clrBack != knNinch)
		pchrpOrig->clrBack = pchrpOverride.clrBack;
	if (pchrpOverride.dympOffset > -1)
		pchrpOrig->dympOffset = pchrpOverride.dympOffset;
	if (pchrpOverride.ssv > -1)
		pchrpOrig->ssv = pchrpOverride.ssv;
	if (pchrpOverride.ttvBold > -1)
		pchrpOrig->ttvBold = pchrpOverride.ttvBold;
	if (pchrpOverride.ttvItalic > -1)
		pchrpOrig->ttvItalic = pchrpOverride.ttvItalic;
	if (pchrpOverride.dympHeight > -1)
		pchrpOrig->dympHeight = pchrpOverride.dympHeight;
	if (pchrpOverride.szFaceName != NULL && wcslen(pchrpOverride.szFaceName) != 0)
		wcscpy(pchrpOrig->szFaceName, pchrpOverride.szFaceName);
	if (pchrpOverride.szFontVar != NULL && wcslen(pchrpOverride.szFontVar) != 0)
		wcscpy(pchrpOrig->szFontVar, pchrpOverride.szFontVar);
}

/*----------------------------------------------------------------------------------------------
	Adjust the ichMin and ichLim for the overrides to rendering coordinates.
----------------------------------------------------------------------------------------------*/
void VwOverrideTxtSrc::AdjustOverrideOffsets()
{
	for (int i = 0; i < m_vdpOverrides.Size(); i++)
	{
		m_vdpOverrides[i].ichMin = LogToRen(m_vdpOverrides[i].ichMin);
		m_vdpOverrides[i].ichLim = LogToRen(m_vdpOverrides[i].ichLim);
	}
}

void VwOverrideTxtSrc::GetUnderlineInfo(int ich, int * punt,
	COLORREF * pclrUnder, int * pichLim)
{
	// Set idp to the largest index into m_vdpOverrides such that
	// ich < m_vdpOverrides[idp].ichLim; or to m_vdpOverrides.Size().
	int idp = 0;
	for (; idp < m_vdpOverrides.Size(); idp++)
	{
		if (ich < m_vdpOverrides[idp].ichLim)
			break;
	}
	if (idp < m_vdpOverrides.Size() && ich >= m_vdpOverrides[idp].ichMin)
	{
		// index passed is in an override range.
		m_qts->GetUnderlineInfo(ich, punt, pclrUnder, pichLim);
		if (m_vdpOverrides[idp].chrp.clrUnder != knNinch)
			*pclrUnder = m_vdpOverrides[idp].chrp.clrUnder;
		if (m_vdpOverrides[idp].chrp.unt > -1)
			*punt = m_vdpOverrides[idp].chrp.unt;
		*pichLim = m_vdpOverrides[idp].ichLim;
#ifdef DEBUG_UNDERLINEPROPS
		StrAnsi sta;
		sta.Format("retrieved overridden underline props with underline = %d%n", *punt);
		OutputDebugStringA(sta.Chars());
#endif
		return;
	}

	// Otherwise get the ordinary result...
	m_qts->GetUnderlineInfo(ich, punt, pclrUnder, pichLim);
	// ...but the range may have to be reduced
	if (idp < m_vdpOverrides.Size() && *pichLim > m_vdpOverrides[idp].ichMin)
		*pichLim = m_vdpOverrides[idp].ichMin;
}


//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_factTt(
	_T("SIL.Views.VwStringTextSource"),
	&CLSID_VwStringTextSource,
	_T("SIL string text source"),
	_T("Apartment"),
	&TrivialTextSrc::CreateCom);


void TrivialTextSrc::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<TrivialTextSrc> qtts;
	qtts.Attach(NewObj TrivialTextSrc());		// ref count initialy 1
	CheckHr(qtts->QueryInterface(riid, ppv));
}
//:>********************************************************************************************
//:>	TrivialTextSrc Methods
//:>********************************************************************************************
STDMETHODIMP TrivialTextSrc::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<IVwTextSource *>(this));
	else if (riid == IID_IVwTextSource)
		*ppv = static_cast<IVwTextSource *>(this);
	else if (riid == IID_IVwTxtSrcInit)
		*ppv = static_cast<IVwTxtSrcInit *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IVwTextSource *>(this), IID_IVwTextSource, IID_IVwTxtSrcInit);
		return NOERROR;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}

/*----------------------------------------------------------------------------------------------
	Constructor and destructor.
----------------------------------------------------------------------------------------------*/
TrivialTextSrc::TrivialTextSrc()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}
TrivialTextSrc::~TrivialTextSrc()
{
	ModuleEntry::ModuleRelease();
}

/*----------------------------------------------------------------------------------------------
	Sets the string to be a text source for.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::SetString(ITsString * ptss)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(ptss);
	m_qtss = ptss;
	END_COM_METHOD(g_factTt, IID_IVwTxtSrcInit);
}

/*----------------------------------------------------------------------------------------------
	Get the specified range of text
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::Fetch(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchBuf, ichLim-ichMin);

	CheckHr(m_qtss->FetchChars(ichMin, ichLim, prgchBuf));

	END_COM_METHOD(g_factTt, IID_IVwTextSource);
}


/*----------------------------------------------------------------------------------------------
	Get the text for searching
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::FetchSearch(int ichMin, int ichLim, OLECHAR * prgchBuf)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchBuf, ichLim - ichMin);

	// REVIEW JohnT (DavidE & TomB): This may need to be changed to skip owned ORCs, assuming
	// a SimpleTxtSrc can have ORCs
	CheckHr(Fetch(ichMin, ichLim, prgchBuf));

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get the properties of a particular character and indicate the range over which they apply.
	It is possible they also apply to a larger range.
	ich is in rendered characters.
	This implementation sets only the ws field of pchrp to a meaningful value at present.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::GetCharProps(int ich, LgCharRenderProps * pchrp,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pchrp);
	ChkComArgPtrN(pichLim);
	ChkComArgPtrN(pichMin);
	TsRunInfo tri;
	ITsTextPropsPtr qttp;
	CheckHr(m_qtss->FetchRunInfoAt(ich, &tri, &qttp));
	*pichMin = tri.ichMin;
	*pichLim = tri.ichLim;
	int var;
	CheckHr(qttp->GetIntPropValues(ktptWs, &var, &pchrp->ws));

	END_COM_METHOD(g_factTt, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get length of whole text source in rendered chars
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::get_Length(int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcch);

	CheckHr(m_qtss->get_Length(pcch));

	END_COM_METHOD(g_factTt, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get length of whole text source for searching (excludes owned ORCs)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::get_LengthSearch(int * pcch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pcch);

	// TODO: JohnT (DaveE, TomB) Need to skip owned ORCs
	CheckHr(m_qtss->get_Length(pcch));

	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	// Similarly get paragraph level properties and the range for which they apply.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::GetParaProps(int ich, LgParaRenderProps * pchrp,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;

	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_factTt, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	// This gets a string property. Some writing systems may support using extra props
	// as a way to vary appearance in ways unique to a particular writing system,
	// such as showing or hiding Greek breathings.
	// This simple implementation just indicates no value for the whole string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::GetCharStringProp(int ich, int id, BSTR * pbstr,
	int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstr); // makes it empty string.
	ChkComOutPtr(pichMin);
	ChkComOutPtr(pichLim);

	TsRunInfo tri;
	ITsTextPropsPtr qttp;
	CheckHr(m_qtss->FetchRunInfoAt(ich, &tri, &qttp));
	*pichMin = tri.ichMin;
	*pichLim = tri.ichLim;
	CheckHr(qttp->GetStrPropValue(id, pbstr));

	END_COM_METHOD(g_factTt, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	// Similar, but a property that is constrained to have the same value for all chars
	// in a paragraph. Some ids may possibly allow both para and char level values.
	Arguments:
		id		which particular property we want (ENHANCE JohnT: which enumeration?)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::GetParaStringProp(int ich, int id, BSTR * pbstr, int * pichMin, int * pichLim)
{
	BEGIN_COM_METHOD;

	ThrowInternalError(E_NOTIMPL);

	END_COM_METHOD(g_factTt, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get a TsString representing a range of the text of the input.
	This is not fully implemented by all text sources, especially if the range spans
	multiple strings. It is not required for rendering, but only for find/replace operations.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::GetSubString(int ichMin, int ichLim, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	ITsStrBldrPtr qtsb;
	CheckHr(m_qtss->GetBldr(&qtsb));
	int cch;
	CheckHr(m_qtss->get_Length(&cch));
	if (cch > ichLim)
		qtsb->ReplaceTsString(ichLim, cch, NULL);
	if (ichMin > 0)
		qtsb->ReplaceTsString(0, ichMin, NULL);
	CheckHr(qtsb->GetString(pptss));
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Return a relevant writing system factory.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::GetWsFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD;
	return E_NOTIMPL;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a logical to a search position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::LogToSearch(int ichlog, int * pichSearch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichSearch);
	*pichSearch = ichlog;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a search to a logical position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::SearchToLog(int ichSearch, ComBool fAssocPrev, int * pichLog)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLog);
	*pichLog = ichSearch;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a logical to a render position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::LogToRen(int ichlog, int * pichRen)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichRen);
	*pichRen = ichlog;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a render to a logical position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::RenToLog(int ichRen, int * pichLog)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLog);
	*pichLog = ichRen;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a search to a render position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::SearchToRen(int ichSearch, ComBool fAssocPrev, int * pichRen)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichRen);
	*pichRen = ichSearch;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a render to a Search position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP TrivialTextSrc::RenToSearch(int ichRen, int * pichSearch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichSearch);
	*pichSearch = ichRen;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Get a TsString representing a range of the text of the input.
	This is not fully implemented by all text sources, especially if the range spans
	multiple strings. It is not required for rendering, but only for find/replace operations.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::GetSubString(int ichMinRen, int ichLimRen, ITsString ** pptss)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptss);
	int ichMin = RenToLog(ichMinRen);
	int ichLim = RenToLog(ichLimRen);
	int itss1, ichMinString1, ichLimString1;
	ITsStringPtr qtss1;
	VwPropertyStorePtr qzvps;
	StringFromIch(ichMin, false, &qtss1, &ichMinString1, &ichLimString1, &qzvps, &itss1);
	if (!qtss1)
		ThrowHr(WarnHr(E_NOTIMPL)); // somehow we got a match involving an object character
	ITsStrBldrPtr qtsb;
	CheckHr(qtss1->GetBldr(&qtsb));
	if (ichLimString1 > ichLim)
		qtsb->ReplaceTsString(ichLim - ichMinString1, ichLimString1 - ichMinString1, NULL);
	if (ichMin > ichMinString1)
		qtsb->ReplaceTsString(0, ichMin - ichMinString1, NULL);
	if (ichLim > ichLimString1)
	{
		// not tested
		int itss2, ichMinString2, ichLimString2;
		ITsStringPtr qtss2;
		StringFromIch(ichLim, false, &qtss2, &ichMinString2, &ichLimString2, &qzvps, &itss2);
		for (int itss = itss1 + 1; itss < itss2; itss++)
		{
			int cch;
			CheckHr(qtsb->get_Length(&cch));
			ITsStringPtr qtss;
			StringAtIndex(itss, &qtss);
			if (qtss)
				CheckHr(qtsb->ReplaceTsString(cch, cch, qtss));
			else
			{
				StrUni stuOrc(L"\xfffc");
				CheckHr(qtsb->Replace(cch, cch, stuOrc.Bstr(), NULL));
			}
		}
		if (ichLim > ichMinString2)
		{
			ITsStrBldrPtr qtsb2;
			CheckHr(qtss2->GetBldr(&qtsb2));
			CheckHr(qtsb2->ReplaceTsString(ichLim - ichMinString2, ichLimString2 - ichMinString2, NULL));
			ITsStringPtr qtssTail;
			CheckHr(qtsb2->GetString(&qtssTail));
			int cch;
			CheckHr(qtsb->get_Length(&cch));
			CheckHr(qtsb->ReplaceTsString(cch, cch, qtssTail));
		}
	}
	CheckHr(qtsb->GetString(pptss));
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a logical to a search position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::LogToSearch(int ichlog, int * pichSearch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichSearch);
	*pichSearch = ichlog;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a search to a logical position.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::SearchToLog(int ichSearch, ComBool fAssocPrev, int * pichLog)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLog);
	*pichLog = ichSearch;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a render position to a logical position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::RenToLog(int ichRen, int * pichLog)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichLog);
	*pichLog = ichRen;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a logical position to a render position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::LogToRen(int ichLog, int * pichRen)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichRen);
	*pichRen = ichLog;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a render position to a search position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::RenToSearch(int ichRen, int * pichSearch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichSearch);
	*pichSearch = ichRen;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

/*----------------------------------------------------------------------------------------------
	Convert a Search position to a render position
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSimpleTxtSrc::SearchToRen(int ichSearch, ComBool fAssocPrev, int * pichRen)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichRen);
	*pichRen = ichSearch;
	END_COM_METHOD(g_fact, IID_IVwTextSource);
}

// Explicit instantiation
#include "Vector_i.cpp"
template class Vector<TextMapItem>; // TmiVec;
template class Vector<DispPropOverride>; // PropOverrideVec;
