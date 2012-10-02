/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2002 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwStyledText.cpp
Responsibility: Steve McConnel
Last reviewed: Not yet.

Description:
	Utility functions for dealing with wsStyles.  These have been pulled from a variety of
	other files: views/VwPropertyStore.cpp, AppCore/AfStyleSheet.cpp, AppCore/FmtFntDlg.cpp.

	Functions that need to stay in sync with those in this namespace:
		FwXml::WriteWsStyles() [AppCore/WriteXml.cpp]
		VwPropertyStore::DoWsStyles() [Views/VwPropertyStore.cpp]
		WpXmlImportData::AddWsStyles() [AppCore/AfWpXml.cpp] (if ever synced)
		WpXmlImportData::AddWsStyles() [WorldPad/WpXml.cpp]
		FwXmlImportData::StoreWsProps() [FwXmlImport.cpp]

	This file is designed to be included in the build (makefile) of various compilation units,
	much like a library but at the source level.  It might fit in the AppCore library except
	that several lower level DLLs use these functions.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "Main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

// TODO: remove pragma and refactor calls to wcscpy
#pragma warning(disable: 4996)

//:>********************************************************************************************
//:>	Local Constants and static variables.
//:>********************************************************************************************

// The order of these is signficant--it is the order the font properties are recorded in
// for each writing system, in the wsStyle string.
// A copy of this list is in VwPropertyStore.cpp -- the two lists must be kept in sync.
static const int s_rgtptWsStyleProps[] = {
	ktptFontSize, ktptBold, ktptItalic, ktptSuperscript,
	ktptForeColor, ktptBackColor, ktptUnderColor, ktptUnderline, ktptOffset
};
static const int s_ctptWsStyleProps = (isizeof(s_rgtptWsStyleProps) / isizeof(int));

// Magic font strings that are used in markup:
static OleStringLiteral g_pszDefaultSerif(L"<default serif>");
static OleStringLiteral g_pszDefaultSans(L"<default sans serif>");
static OleStringLiteral g_pszDefaultMono(L"<default monospace>");
static OleStringLiteral g_pszDefaultBodyFont(L"<default body>");


//:>********************************************************************************************
//:>	Local utility functions.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Return the User Interface name for the default font.  (This is complicated by a DLL being
	unable to properly initialize a global StrUni from the resources, at least when called from
	C# code.)
----------------------------------------------------------------------------------------------*/
static inline StrUni DefaultFontUi()
{
	static StrUni g_stuDefaultFontUi;
	if (!g_stuDefaultFontUi.Length())
		g_stuDefaultFontUi.Load(kstidDefaultSerif);
	return g_stuDefaultFontUi;
}


/*----------------------------------------------------------------------------------------------
	Return the User Interface name for the default "heading" font.  (This is complicated by a
	DLL being unable to properly initialize a global StrUni from the resources, at least when
	called from C# code.)
----------------------------------------------------------------------------------------------*/
static inline StrUni DefaultHeadingFontUi()
{
	static StrUni g_stuDefaultHeadingFontUi;
	if (!g_stuDefaultHeadingFontUi.Length())
		g_stuDefaultHeadingFontUi.Load(kstidDefaultSans);
	return g_stuDefaultHeadingFontUi;
}

/*----------------------------------------------------------------------------------------------
	Return the User Interface name for the default "body" font.
----------------------------------------------------------------------------------------------*/
static inline StrUni DefaultBodyFontUi()
{
	static StrUni g_stuDefaultBodyFontUi;
	if (!g_stuDefaultBodyFontUi.Length())
		g_stuDefaultBodyFontUi.Load(kstidDefaultBodyFont);
	return g_stuDefaultBodyFontUi;
}

/*----------------------------------------------------------------------------------------------
	Return the User Interface name for the default fixed width font.  (This is complicated by a
	DLL being unable to properly initialize a global StrUni from the resources, at least when
	called from C# code.)
----------------------------------------------------------------------------------------------*/
static inline StrUni DefaultMonoFontUi()
{
	static StrUni g_stuDefaultMonoFontUi;
	if (!g_stuDefaultMonoFontUi.Length())
		g_stuDefaultMonoFontUi.Load(kstidDefaultMono);
	return g_stuDefaultMonoFontUi;
}


/*----------------------------------------------------------------------------------------------
	Copy one writing system's worth of info from Src to Dst and update the pointers.

	@param pchSrc
	@param pchDst
----------------------------------------------------------------------------------------------*/
static void CopyOneWsFontInfo(const OLECHAR * & pchSrc, OLECHAR * & pchDst)
{
	// Copy ws info and length of font family name and font family name itself
	// and # int props and int props themselves.
	int cchFF = *(pchSrc + 2); // Follows ws.
	int cpropInt = SignedInt(pchSrc[3 + cchFF]); // Follows ws, cchFF and ff itself.
	int cchStrProps = 0;
	if (cpropInt < 0)
	{
		// Additional string properties.
		int cpropStr = cpropInt * -1;
		cchStrProps = 1; // counter
		// Point at the data right after the ws (2), char count for FF, FF itself, and
		// the cprop that turned out to be a cpropStr.
		OLECHAR * pchTmp = const_cast<OLECHAR *>(pchSrc) + 3 + cchFF + 1;
		for (int iprop = 0; iprop < cpropStr; iprop++)
		{
			int cch = *pchTmp;
			cchStrProps += 1 + cch;
			pchTmp += 1 + cch;
		}
		// The character following the extra strings is the real integer property count.
		cpropInt = *pchTmp;
	}
	// First 4 = 2 (ws) + 1 (cchFF) + 1 (cprop).
	int cchCopy = 4 + cchFF + cchStrProps + (cpropInt * 4);
	MoveItems(pchSrc, pchDst, cchCopy);
	pchSrc += cchCopy;
	pchDst += cchCopy;
}

/*----------------------------------------------------------------------------------------------
	Put an integer property value into a string.
----------------------------------------------------------------------------------------------*/
static void MakeProp(OLECHAR * & pch, int tpt, int nVar, int nVal, int & cprop)
{
	// If the value was conflicting and has not changed, or if it is now
	// unspecified, leave it out.
	if (nVal == FwStyledText::knConflicting)
		return;
	if (nVal == FwStyledText::knUnspecified)
		return;
	// Otherwise, write it into the buffer.
	cprop++;
	*pch++ = (OLECHAR) tpt;
	*pch++ = (OLECHAR) nVar;
	*pch++ = (OLECHAR) nVal;
	*pch++ = (OLECHAR) (nVal >> 16);
}

//:>********************************************************************************************
//:>	FwStyledText functions.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	This class encapsulates various methods for dealing with WsProps strings represented as
	an StrUni.
----------------------------------------------------------------------------------------------*/
class WsPropsManipulator
{
public:
	StrUni m_stu; // Holds a ws props string.
	int m_ich; // a position in m_stu, the main loop variable.
	int m_ichWs; // a position in m_stu where the info about one ws starts.
	int m_ichCIntProp; // a position in m_stu where the count of int props occurs.
	WsPropsManipulator(StrUni stu)
	{
		m_stu = stu;
		m_ich = 0;
		m_ichWs = 0;
	}

	// m_ich is pointing at a wsid. Advance it past font family char count,
	// and return the count.
	int FontFamilyFromWsid()
	{
		m_ichWs = m_ich;
		m_ich += 3; // Wsid is 2 chars, cchFF is one.
		return m_stu[m_ich - 1];
	}

	// m_ich is pointing at the character that is either the count of additional
	// string properties (if negative), or the count of integer properties (if >= 0).
	// Advance it past the string properties if any, and past the count of integer
	// properties, and return the count of integer properties.
	// Also set m_ichCIntProp to the actual location of the integer property count.
	int SkipStrPropsAndGetIntPropCount()
	{
		int cpropInt = (short)m_stu[m_ich];
		if (cpropInt < 0)
		{
			// Additional string properties.
			int cpropStr = (-cpropInt);
			m_ich++;
			for (int iprop = 0; iprop < cpropStr; iprop++)
			{
				int cch = m_stu[m_ich + 1]; // after the tpt character
				m_ich += 2 + cch; // past tpt, char count and actual chars of one string prop
			}
			// The character following the extra strings is the real integer property count.
			cpropInt = m_stu[m_ich];
		}
		m_ichCIntProp = m_ich;
		m_ich++; // past original int prop count or extra one at end of string props.
		return cpropInt;
	}

	// If there is no real information about a writing system (it has just four characters,
	// which must be the wsid and a zero cchFF and a zero cpropInt), delete it from m_stu,
	// and move m_ich back to where it used to be, so it is pointing at the next ws.
	// This routine must be called when m_ich has advanced through all the data about one
	// ws so it is pointing at the start of the next.
	void DeleteEmptyWsInfo()
	{
		if (m_ich == m_ichWs + 4)
		{
			// No remaining overrides for this ws; delete all mention of it
			m_stu.Replace(m_ichWs, m_ich, (OLECHAR *)NULL, 0);
			m_ich = m_ichWs;
		}
	}

	// Remove any information about font family.
	StrUni RemoveFontFamily()
	{
		// Each iteration processes one writing system.
		while (m_ich < m_stu.Length())
		{
			int cchFF = FontFamilyFromWsid();
			if (cchFF > 0)
			{
				m_stu.SetAt(m_ich - 1, '\0'); // change length to zero.
				m_stu.Replace(m_ich , m_ich + cchFF, (OLECHAR *)NULL, 0);
			}
			int cpropInt = SkipStrPropsAndGetIntPropCount();
//			m_ich += 1 + 4 * cpropInt; // Past count and 4 chars per prop.
			m_ich += 4 * cpropInt; // 4 chars per prop (???ALREADY PAST COUNT? - JohnL).
			DeleteEmptyWsInfo();
		}
		return m_stu;
	}

	// Remove any information about integer property tpt.
	StrUni RemoveIntProp(int tpt)
	{
		// Each iteration processes one writing system.
		while (m_ich < m_stu.Length())
		{
			int cchFF = FontFamilyFromWsid();
			m_ich += cchFF; // skip past font family chars

			int cpropInt = SkipStrPropsAndGetIntPropCount();

			for (int iprop = 0; iprop < cpropInt; iprop++)
			{
				if (m_stu[m_ich] == tpt) // first char of int property is tpt.
				{
					// Remove it...
					m_stu.Replace(m_ich, m_ich + 4, (OLECHAR *)NULL, 0);
					// ...and decrement the count of integer properties.
					m_stu.SetAt(m_ichCIntProp, (OLECHAR)(m_stu[m_ichCIntProp] - 1));
				}
				else
					m_ich += 4; // 4 chars represent each int property
			}
			DeleteEmptyWsInfo();
		}
		return m_stu;
	}

	// Remove any properties from m_stu that are identical with those in ptpb.
	// (Except currently we don't delete any string props except font family.
	// This is because the purpose of this routine is mainly to delete from the
	// ws overrides stuff that is specified in the <default props>.
	// Currently the only other string prop we store in a WsProps is font
	// variations, and that is never set for <default props>.
	StrUni RemovePropsMatching(ITsPropsBldr * ptpb)
	{
		// Each iteration processes one writing system.
		while (m_ich < m_stu.Length())
		{
			int cchFF = FontFamilyFromWsid();
			if (cchFF > 0)
			{
				SmartBstr sbstr;
				CheckHr(ptpb->GetStrPropValue(ktptFontFamily, &sbstr));
				if (cchFF == sbstr.Length() &&
					u_strncmp(sbstr.Chars(), m_stu.Chars() + m_ich, cchFF) == 0)
				{
					m_stu.SetAt(m_ich - 1, '\0'); // change length to zero.
					m_stu.Replace(m_ich , m_ich + cchFF, (OLECHAR *)NULL, 0);
				}
				else
				{
					m_ich += cchFF; // skip FF chars if not deleting them.
				}
			}
			int cpropInt = SkipStrPropsAndGetIntPropCount();
			for (int iprop = 0; iprop < cpropInt; iprop++)
			{
				int tpt = m_stu[m_ich];
				int var = m_stu[m_ich + 1];
				// Val is combining lsw in char 2 with msw in char 3
				int val =  m_stu[m_ich + 2] | (m_stu[m_ich + 3] << 16);
				int var2, val2;
				CheckHr(ptpb->GetIntPropValues(tpt, &var2, &val2));
				if (var2 == var && val2 == val) // override matches default.
				{
					// Remove it...
					m_stu.Replace(m_ich, m_ich + 4, (OLECHAR *)NULL, 0);
					// ...and decrement the count of integer properties.
					m_stu.SetAt(m_ichCIntProp, (OLECHAR)(m_stu[m_ichCIntProp] - 1));
				}
				else
					m_ich += 4; // 4 chars represent each int property
			}
			DeleteEmptyWsInfo();
		}
		return m_stu;
	}
};

/*----------------------------------------------------------------------------------------------
	Remove from stuWsStyle, which is about to be written to the props builder ptpb, any
	information which is redudant because the props builder already specifies it as a
	<default properties> value.
----------------------------------------------------------------------------------------------*/
StrUni FwStyledText::RemoveSpuriousOverrides(StrUni stuWsStyle, ITsPropsBldr * ptpb)
{
	return WsPropsManipulator(stuWsStyle).RemovePropsMatching(ptpb);
}

/*----------------------------------------------------------------------------------------------
	Compute the net effect of an override on a base style.
	ENHANCE JohnT (version 2 or later): When we make use of property settings that are
	non-absolute, we will need more subtle algorithms here.

	@param pttpBase Base Text Properties of the style.
	@param pttpOverride Override Text Properties.
	@param ppttpEffect Out The net Text Properties including all inheritance.
----------------------------------------------------------------------------------------------*/
void FwStyledText::ComputeInheritance(ITsTextProps * pttpBase, ITsTextProps * pttpOverride,
	ITsTextProps ** ppttpEffect)
{
	AssertPtr(pttpBase);
	AssertPtr(ppttpEffect);
	Assert(!*ppttpEffect);
	if (!pttpOverride)
	{
		*ppttpEffect = pttpBase;
		pttpBase->AddRef();
		return;
	}
	ITsPropsBldrPtr qtpb;
	CheckHr(pttpBase->GetBldr(&qtpb));
	int cip;
	CheckHr(pttpOverride->get_IntPropCount(&cip));
	int csp;
	CheckHr(pttpOverride->get_StrPropCount(&csp));
	int iprop;

	// Get the base WsStyle property before we start. Any property set in the normal
	// way on pttpOverride needs to override anything set in base for a particular
	// writing system. The easy way to achieve this is to just remove any mention
	// of that property from the base string.
	SmartBstr bstrBase;
	CheckHr(pttpBase->GetStrPropValue(ktptWsStyle, &bstrBase));
	StrUni stuBase(bstrBase.Chars(), bstrBase.Length());
	SmartBstr bstrWsPropOver;

	for (iprop = 0; iprop < cip; iprop++)
	{
		int tpt, nVar, nVal;
		CheckHr(pttpOverride->GetIntProp(iprop, &tpt, &nVar, &nVal));
		int nVarBase, nValBase;
		HRESULT hr;
		CheckHr(hr = pttpBase->GetIntPropValues(tpt, &nVarBase, &nValBase));
		if (hr == S_OK)		// Special case merging.
			FwStyledText::MergeIntProp(tpt, nVarBase, nValBase, nVar, nVal);
		CheckHr(qtpb->SetIntPropValues(tpt, nVar, nVal));
		stuBase = WsPropsManipulator(stuBase).RemoveIntProp(tpt);
	}
	SmartBstr bstrOver;
	for (iprop = 0; iprop < csp; iprop++)
	{
		int tpt;
		CheckHr(pttpOverride->GetStrProp(iprop, &tpt, &bstrOver));
		// Any special cases?
		switch (tpt)
		{
		case ktptWsStyle:
			// Save it to use later.
			bstrWsPropOver.Attach(bstrOver.Detach());
			break;
		case ktptFontFamily:
			stuBase = WsPropsManipulator(stuBase).RemoveFontFamily();
			// FALL THROUGH
		case ktptFontVariations:
			// We don't need to do anything special, because font variation should
			// never occur in <default properties>. It only applies to a specific font.
			// FALL THROUGH
		default:
			// Most properties just get copied to the output
			CheckHr(qtpb->SetStrPropValue(tpt, bstrOver));
			break;
		}
	}
	if (bstrWsPropOver.Length() > 0)
	{
		// Compute WsProps net effect by applying overrides to (possibly modified) base WsProps.
		FwStyledText::ComputeWsStyleInheritance(stuBase.Bstr(), bstrWsPropOver, bstrOver);
		if (BstrLen(bstrOver) != 0)
			CheckHr(qtpb->SetStrPropValue(ktptWsStyle, bstrOver));
	}
	else
	{
		// Use what we already computed.
		if (stuBase.Length())
			CheckHr(qtpb->SetStrPropValue(ktptWsStyle, stuBase.Bstr()));
	}

	CheckHr(qtpb->GetTextProps(ppttpEffect));
} //:> End of FwStyledText::ComputeInheritance.

/*----------------------------------------------------------------------------------------------
	Compute the net effect of an override on a base for the ktptWsStyle string property.

	@param bstrBase
	@param bstrOver
	@param sbstrComp
----------------------------------------------------------------------------------------------*/
void FwStyledText::ComputeWsStyleInheritance(BSTR bstrBase, BSTR bstrOver,
	SmartBstr & sbstrComp)
{
	// OPTIMIZE JohnT: is it worth making this static somewhere? Problem is to avoid getting
	// memory leaks from initializing it.
	// Or, if we knew the ktpt's were all small, we could use a static array.
	HashMap<OLECHAR, OLECHAR> hmchchOrder;	// Required order of int properties.

	if (!BstrLen(bstrBase))
	{
		sbstrComp.Assign(bstrOver, BstrLen(bstrOver));
		return;
	}
	OLECHAR rgch[30000]; // Where we will build up the style; enough for about 1000 ows's.
	OLECHAR * pch = rgch; // Outside loop so we can use it to get the result size.
	const OLECHAR * pchBase = bstrBase;
	const OLECHAR * pchOver = bstrOver;
	const OLECHAR * pchBaseLim = pchBase + BstrLen(bstrBase);
	const OLECHAR * pchOverLim = pchOver + BstrLen(bstrOver);
	unsigned int wsBase;
	unsigned int wsOver;
	// Each iteration writes info about one writing system to the destination.
	// Invariant: pchBase and pchOver each point to the start of info about one
	// ws that we have not processed.
	while (pchBase < pchBaseLim || pchOver < pchOverLim)
	{
		if (pchBase < pchBaseLim)
		{
			wsBase = *pchBase | (*(pchBase + 1) << 16);
		}
		else
			wsBase = 0xffffffff;	// Ensure that we don't try to copy any more from Base.
		if (pchOver < pchOverLim)
		{
			wsOver = *pchOver | (*(pchOver + 1) << 16);
		}
		else
			wsOver = 0xffffffff;	// Ensure that we don't try to copy any more from Over.
		if (wsBase < wsOver)
		{
			// Base < Over: base contains style info for a ws that Over does not:
			// copy base info to dest.
			CopyOneWsFontInfo(pchBase, pch);
		}
		else if (wsBase > wsOver)
		{
			// Base > Over: over contains style info for a ws that base does not:
			// copy over info to dest.
			CopyOneWsFontInfo(pchOver, pch);
		}
		else
		{
			// Base and Over contain info about the same ws: compute net effect.
			// Copy ws we know it is the same.
			MoveItems(pchBase, pch, 2);
			pchBase += 2;
			pchOver += 2;
			pch += 2;
			if (pchOver > pchOverLim || pchBase > pchBaseLim)
//-				ThrowHr(WarnHr(E_UNEXPECTED));
//- OR
				ThrowInternalError(E_INVALIDARG, "Char style defns invalid length");

			// Copy font name.
			int cch;
			if (*pchOver)
			{
				// It has a font name, so use it. Copy the chars and the length.
				cch = *pchOver + 1;
				MoveItems(pchOver, pch, cch);
			}
			else
			{
				// Use the base ff name, even if empty.
				cch = *pchBase + 1;
				MoveItems(pchBase, pch, cch);
			}
			pch += cch;
			pchBase += *pchBase + 1;
			pchOver += *pchOver + 1;
			if (pchOver > pchOverLim || pchBase > pchBaseLim)
//-				ThrowHr(WarnHr(E_UNEXPECTED));
//- OR
				ThrowInternalError(E_INVALIDARG, "Char style defns invalid length");

			// Copy any further string properties.
			int cpropBase = SignedInt(*pchBase++);
			int cpropOver = SignedInt(*pchOver++);
			OLECHAR * pcpropCombined = pch; // Remember where to put result count.
			OLECHAR cpropCombined = 0;

			if (cpropBase < 0 || cpropOver < 0)
			{
				// TODO: when we have multiple string properties, enhance this routine to
				// be aware of the proper order (as below with s_ctptWsStyleProps).
				pch++; // leave space for count
				int cpropBaseStr = (cpropBase < 0) ? cpropBase * -1 : 0;
				int cpropOverStr = (cpropOver < 0) ? cpropOver * -1 : 0;
				while (cpropBaseStr || cpropOverStr)
				{
					cpropCombined++;
					OLECHAR tptBase = cpropBaseStr ? *pchBase : (OLECHAR) 0xffff;
					OLECHAR tptOver = cpropOverStr ? *pchOver : (OLECHAR) 0xffff;
					if (tptBase == tptOver)
					{
						// TODO: Merge.
						// For now, concatenate, base followed by over. This will work fine
						// for the Graphite font features.
						switch (tptBase)
						{
						case ktptFontVariations:
							{	// BLOCK
								pchBase++;
								pchOver++;
								int cchBase = *pchBase++;
								int cchOver = *pchOver++;
								*pch++ = ktptFontVariations;
								StrUni stuBase(pchBase, cchBase);
								StrUni stuOver(pchOver, cchOver);
								StrUni stuMerged;
								stuMerged.Format(L"%s,%s", stuBase.Chars(), stuOver.Chars());
								cch = stuMerged.Length();
								*pch++ = (OLECHAR) cch;
								MoveItems(stuMerged.Chars(), pch, cch);
								pch += cch;
								pchBase += cchBase;
								pchOver += cchOver;
							}
							break;
						default:
							Assert(false);
						}
						cpropBaseStr--;
						cpropOverStr--;
					}
					else if (tptBase != 0xffff)
					{
						cch = *(pchBase + 1);
						MoveItems(pchBase, pch, cch + 2); // tpt, cch, string
						pch += cch + 2;
						pchBase += cch + 2;
						cpropBaseStr--;
					}
					else if (tptOver != 0xffff)
					{
						cch = *(pchOver + 1);
						MoveItems(pchOver, pch, cch + 2); // tpt, cch, string
						pch += cch + 2;
						pchOver += cch + 2;
						cpropOverStr--;
					}
					else
						Assert(false);
				}
				*pcpropCombined = cpropCombined * -1;
				if (cpropBase < 0)
					cpropBase = *pchBase++;
				if (cpropOver < 0)
					cpropOver = *pchOver++;
			}

			// Copy int props.
			pcpropCombined = pch; // Remember where to put result count.
			cpropCombined = 0;
			pch++;
			// One iteration transfers one int property to output.
			while (cpropBase || cpropOver)
			{
				cpropCombined++;
				// If a particular property is out, make sure it won't be chosen
				// for copying.
				OLECHAR tptBase = cpropBase ? *pchBase : (OLECHAR) 0xffff;
				OLECHAR tptOver = cpropOver ? *pchOver : (OLECHAR) 0xffff;
				if (tptBase == tptOver)
				{
					// Merge.
					int nValBase = *(pchBase + 2) | (*(pchBase + 3) << 16);
					int nVar = *(pchOver + 1);
					int nVal = *(pchOver + 2) | (*(pchOver + 3) << 16);
					MergeIntProp(tptBase, *(pchBase + 1), nValBase, nVar, nVal);
					if (nVal != -1 || nVar != -1)
					{
						// Insert the merged property into the output.
						*pch++ = tptBase;
						*pch++ = (OLECHAR) nVar;
						*pch++ = (OLECHAR) nVal;
						*pch++ = (OLECHAR) (nVal >> 16);
					}
					else
					{
						// The properties cancelled out somehow, MergeIntProp thinks the
						// merged style should say nothing about this. Just leave it out.
						cpropCombined--;
					}
					cpropBase--;
					cpropOver--;
					pchBase += 4;
					pchOver += 4;
				}
				else
				{
					// Copy whichever should come first, unchanged.
					// If we have not yet initialized the order hash map, do so now.
					// We wait until this point to do it because it is quite likely there
					// are no different properties to resolve.
					// This order must stay synchronized with AfStyleFntDlg::GetDlgValues.
					if (!hmchchOrder.Size())
					{
						for (OLECHAR itpt = 0; itpt < s_ctptWsStyleProps; itpt++)
						{
							OLECHAR tptTrunc = (OLECHAR)(s_rgtptWsStyleProps[itpt] & 0xffff);
							hmchchOrder.Insert(tptTrunc, itpt);
						}
					}
					OLECHAR nBase = 100; // Any oddballs come last.
					hmchchOrder.Retrieve(tptBase, &nBase);
					OLECHAR nOver = 100;
					hmchchOrder.Retrieve(tptOver, &nOver);
					if (nBase < nOver)
					{
						MoveItems(pchBase, pch, 4);
						pchBase += 4;
						cpropBase--;
					}
					else
					{
						MoveItems(pchOver, pch, 4);
						pchOver += 4;
						cpropOver--;
					}
					pch += 4;
				}
			}
			*pcpropCombined = cpropCombined;
		}
	}
	sbstrComp.Assign(rgch, pch - rgch);
}


/*----------------------------------------------------------------------------------------------
	Return a vector of font property constants, in the order they would be stored in the
	wsStyle string.

	@param pprgtpt
----------------------------------------------------------------------------------------------*/
int FwStyledText::WsStylesPropList(const int ** pprgtpt)
{
	AssertPtr(pprgtpt);
	*pprgtpt = s_rgtptWsStyleProps;
	return s_ctptWsStyleProps;
}


/*----------------------------------------------------------------------------------------------
	Compute the effect of merging a base and overridden integer property.
	nVar and nVal indicate the override and are changed to reflect any modified value.
	ENHANCE JohnT (version 2 or later): when we make use of property settings that are
	non-absolute, we will need more subtle algorithms here.

	@param tpt Text property type
	@param nVarBase Base Variation to be merged
	@param nValBase Base Variation to be merged
	@param nVar In/Out Overridden Variation to be merged and returned
	@param nVal In/Out Overridden Value to be merged and returned.
----------------------------------------------------------------------------------------------*/
void FwStyledText::MergeIntProp(int tpt, int nVarBase, int nValBase, int &nVar, int &nVal)
{
	switch(tpt)
	{
	default:
		// Most properties require no special handling.
		// (Until we start making use of relative values...)
		break;
	case ktptItalic:
	case ktptBold:
		Assert(nVar == ktpvEnum);
		// These cases use kttvInvert as a possible value. If this is present
		// it needs to be properly combined with the base setting.
		if (nVal == kttvInvert)
		{
			switch(nValBase)
			{
			default:
				// Was not specified: just use the invert setting.
				break;
			case kttvOff:
				// Was explicitly off: turn on.
				nVal = kttvForceOn;
				break;
			case kttvForceOn:
				// Was on: force off.
				nVal = kttvOff;
				break;
			case kttvInvert:
				// Inverting "invert" cancels it, making a ttp that does not change
				// this property at all.
				nVal = nVar = -1;
				break;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	The given font property has been set, so clear any separate setting in the list of
	properties specific for the writing-system.
----------------------------------------------------------------------------------------------*/
void FwStyledText::ZapWsStyle(StrUni & stuWsStyle, int tpt, int nVar, int nVal)
{
	const OLECHAR * pchOld = stuWsStyle.Chars();
	const OLECHAR * pchLimOld = pchOld + stuWsStyle.Length();

	OLECHAR rgchNew[30000]; // where we will build up the style; enough for about 1000 ws's
	OLECHAR * pchNew = rgchNew; // Outside loop so we can use it to get the result size
	OLECHAR * pchCpropNew;

	// Each iteration of this loop processes information about one writing system.
	while (pchOld < pchLimOld)
	{
		if (pchLimOld - pchOld < 4)
			ThrowHr(WarnHr(E_FAIL)); // minimum ws style is 4 chars

		// Copy the writing system - 2 chars;
		*pchNew++ = *pchOld++;
		*pchNew++ = *pchOld++;

		// Font family.
		const OLECHAR * pchFF = pchOld + 1;
		pchOld = pchFF + *pchOld;
		StrUni stuFF(pchFF, pchOld - pchFF);
		if (pchOld >= pchLimOld)
			ThrowHr(WarnHr(E_FAIL));
		if (tpt == ktptFontFamily)
		{
			*pchNew++ = 0;
		}
		else
		{
			*pchNew++ = (OLECHAR) stuFF.Length();
			u_strcpy(pchNew, stuFF.Chars());
			pchNew += stuFF.Length();
		}

		int cpropOld = SignedInt(*pchOld++);
		int cpropNew = 0;

		// Additional string properties.
		if (cpropOld < 0)
		{
			pchCpropNew = pchNew;
			*pchNew++ = 0;
			for ( ; cpropOld < 0; cpropOld++)
			{
				int tptNext = *pchOld++;
				int cchNext = *pchOld++;
				if (tpt == tptNext)
				{
					// Leave the property out.
				}
				else
				{
					// Copy it as is.
					cpropNew++;
					*pchNew++ = (OLECHAR)tptNext;
					*pchNew++ = (OLECHAR)cchNext;
					MoveItems(pchOld, pchNew, cchNext);
					pchNew += cchNext;
				}
				pchOld += cchNext;
			}
			*pchCpropNew = (OLECHAR)cpropNew * -1;
			cpropOld = *pchOld++;
		}

		// Integer properties.
		pchCpropNew = pchNew;
		*pchNew++ = 0;
		cpropNew = 0;
		if (pchLimOld - pchOld < 4 * cpropOld)
			ThrowHr(WarnHr(E_FAIL));
		for ( ; cpropOld > 0; cpropOld--)
		{
			int tptNext = *pchOld++;
			if (tpt == tptNext)
			{
				if (tpt == ktptFontSize && nVar == ktpvRelative)
				{
					// Copy with appropriate modification.
					*pchNew++ = (OLECHAR)tpt;
					*pchNew++ = (OLECHAR) ktpvMilliPoint;
					pchOld++; // past var
					int oldVal = *pchOld++;
					oldVal += *pchOld++ << 16;
					int newVal = oldVal * nVal / kdenTextPropRel;
					*pchNew++ = (OLECHAR) newVal;
					*pchNew++ = (OLECHAR) (newVal >> 16);
					cpropNew++;
				}
				else
				{
					// Leave the property out.
					pchOld += 3;

					// Or, we could replace it with the new value:
					//*pchNew++ = (OLECHAR)tpt;
					//*pchNew++ = (OLECHAR) nVar;
					//*pchNew++ = (OLECHAR) nVal;
					//*pchNew++ = (OLECHAR) (nVal >> 16);
					//cpropNew++;
				}
			}
			else
			{
				// Copy it as is.
				Assert(tptNext == (tptNext & 0xffff));
				*pchNew++ = (OLECHAR)tptNext;
				*pchNew++ = *pchOld++;
				*pchNew++ = *pchOld++;
				*pchNew++ = *pchOld++;
				cpropNew++;
			}
		}
		*pchCpropNew = (OLECHAR)cpropNew;
	}
	// DON'T count on null termination! May have 0's in it.
	StrUni stuNew(rgchNew, pchNew - rgchNew);
	stuWsStyle = stuNew;
}

/*----------------------------------------------------------------------------------------------
	Decode the string that lists all the font properties for a list of encodings.
	This method is static so it can be used by routines outside of this dialog.

	NOTE: This method must be kept in sync with the FwStyledText functions.
----------------------------------------------------------------------------------------------*/
void FwStyledText::DecodeFontPropsString(BSTR bstr, Vector<WsStyleInfo> & vesi,
	Vector<int> & vwsSoFar)
{
	Vector<WsStyleInfo> vesiInheritedBogus;
	Vector<ChrpInheritance> vchrpiBogus;
	Vector<int> vwsExtraBogus;

	DecodeFontPropsString(bstr, true, vesiInheritedBogus, vesi, vchrpiBogus,
		vwsSoFar, vwsExtraBogus);
}


void FwStyledText::DecodeFontPropsString(BSTR bstr, bool fExplicit,
	Vector<WsStyleInfo> & vesiInherited, Vector<WsStyleInfo> & vesiExplicit,
	Vector<ChrpInheritance> & vchrpi,
	Vector<int> & vwsSoFar, Vector<int> & vwsExtra)
{
	const OLECHAR * pch = bstr;
	const OLECHAR * pchLim = pch + BstrLen(bstr);

	// Each iteration of this loop processes information about one writing system.
	while (pch < pchLim)
	{
		int ws;
		if (pchLim - pch < 4)
			ThrowHr(WarnHr(E_FAIL)); // minimum ws style is 4 chars
		// Don't use ++ within the expression twice! Strange things happen
		ws = *pch | (*(pch + 1) << 16);
		pch += 2;

		// remove this writing system from vwsExtra, if present
		for (int iws = 0; iws < vwsExtra.Size(); iws++)
		{
			if (vwsExtra[iws] == ws)
			{
				vwsExtra.Delete(iws);
				break;
			}
		}

		int iesi = FindOrAddWsInfo(ws, vesiInherited, vesiExplicit, vchrpi, vwsSoFar);
		WsStyleInfo & esi = (fExplicit) ? vesiExplicit[iesi] : vesiInherited[iesi];
		int xEorI = (fExplicit) ? kxExplicit : kxInherited;
		ChrpInheritance & chrpi = vchrpi[iesi];

		const OLECHAR * pchFF = pch + 1;
		pch = pchFF + *pch;
		if (pch >= pchLim)
			ThrowHr(WarnHr(E_FAIL));
		StrUni stuMarkup;
		stuMarkup.Assign(pchFF, pch - pchFF);
		esi.m_stuFontFamily.Assign(FontStringMarkupToUi(false, stuMarkup));
		ConvertDefaultFontInput(esi.m_stuFontFamily);
		if (pch > pchFF)
			chrpi.xFont = xEorI;
		int cprop = SignedInt(*pch++);

		if (cprop < 0)
		{
			// The next batch of properties are additional string properties;
			// cprop = -1 * the number of them.
			for ( ; cprop < 0; cprop++)
			{
				int tpt = *pch++;
				int cch = *pch++;
				switch (tpt)
				{
				case ktptFontVariations:
					esi.m_stuFontVar.Assign(pch, cch);
					if (cch)
						chrpi.xFontVar = xEorI;
					break;
				default:
					Assert(false);
				}
				pch += cch;
			}
			cprop = *pch++;
		}
		// Integer properties
		if (pchLim - pch < 4 * cprop)
			ThrowHr(WarnHr(E_FAIL));

		for (; cprop > 0; cprop--)
		{
			int tpt = *pch++;
			int nVar = *pch++;
			int nVal = *pch | (*(pch + 1) << 16);
			pch += 2;
			// Load the property. Ignore any unexpected property or variation, for the
			// sake of forwards compatibility.
			switch (tpt)
			{
			case ktptFontSize:
				if (nVar == ktpvMilliPoint)
				{
					esi.m_mpSize = nVal;
					chrpi.xSize = xEorI;
				}
				break;
			case ktptBold:
				if (nVar == ktpvEnum)
				{
					esi.m_fBold = nVal;
					chrpi.xBold = xEorI;
				}
				break;
			case ktptItalic:
				if (nVar == ktpvEnum)
				{
					esi.m_fItalic = nVal;
					chrpi.xItalic = xEorI;
				}
				break;
			case ktptSuperscript:
				if (nVar == ktpvEnum)
				{
					esi.m_ssv = nVal;
					chrpi.xSs = xEorI;
				}
				break;
			case ktptForeColor:
				if (nVar == ktpvDefault)
				{
					esi.m_clrFore = nVal;
					chrpi.xFore = xEorI;
				}
				break;
			case ktptBackColor:
				if (nVar == ktpvDefault)
				{
					esi.m_clrBack = nVal;
					chrpi.xBack = xEorI;
				}
				break;
			case ktptUnderColor:
				if (nVar == ktpvDefault)
				{
					esi.m_clrUnder = nVal;
					chrpi.xUnder = xEorI;
				}
				// Report this only if an underline style is also given
				break;
			case ktptUnderline:
				if (nVar == ktpvEnum)
				{
					esi.m_unt = nVal;
					chrpi.xUnderT = xEorI;
				}
				break;
			case ktptOffset:
				if (nVar == ktpvMilliPoint)
				{
					esi.m_mpOffset = nVal;
					chrpi.xOffset = xEorI;
				}
				break;
			default:
				Warn("Unexpected style property found");
				break; // Ignore others (for forwards compatibility)
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Add space for an writing system in the dialog data structures, if needed.
	Keeps the writing systems ordered by ws id.
	This method is static so it can be used by routines outside of this dialog.
----------------------------------------------------------------------------------------------*/
int FwStyledText::FindOrAddWsInfo(int ws, Vector<WsStyleInfo> & vesiInherited,
	Vector<WsStyleInfo> & vesiExplicit, Vector<ChrpInheritance> & vchrpi,
	Vector<int> & vwsSoFar)
{
	int iws;
	for (iws = 0; iws < vwsSoFar.Size(); iws++)
	{
		if ((unsigned int)(vwsSoFar[iws]) == (unsigned int)ws)
			return iws;
		if ((unsigned int)(vwsSoFar[iws]) > (unsigned int)ws)
			break;
	}
	// Add it.
	// First, add a bunch of flags saying which values are explicit. (By default everything
	// is empty and therefore inherited.)
	ChrpInheritance chrpi;
	chrpi.InitToInherited();
	vchrpi.Insert(iws, chrpi);

	// Add something to hold the inherited values and the explicit values.
	WsStyleInfo esiI;
	esiI.m_ws = ws;
	vesiInherited.Insert(iws, esiI);

	WsStyleInfo esiE;
	esiE.m_ws = ws;
	vesiExplicit.Insert(iws, esiE);

	vwsSoFar.Insert(iws, (unsigned int)ws);

	return iws;
}

/*----------------------------------------------------------------------------------------------
	Generate a string that represents all the font properties of all the encodings.
	fForPara is true if writing system them for a paragraph style.  Paragraph styles store bold
	and italic as on or off rather than inverting or nothing. This is mainly because 'Normal'
	is applied initially as an overall default. Then it may get applied again when used on a
	particular paragraph. We don't want this to cancel out bold or italic.
	NOTE: This method must be kept in sync with DecodeFontPropsString.
	(However, note that this method skips an initial WsStyleInfo with m_ws 0, if any.
	DecodeFontPropsString does not automatically create this extra record.)
----------------------------------------------------------------------------------------------*/
StrUni FwStyledText::EncodeFontPropsString(Vector<WsStyleInfo> & vesi, bool fForPara)
{
	OLECHAR rgch[30000]; // where we will build up the style; enough for about 1000 ws's
	OLECHAR * pch = rgch; // Outside loop so we can use it to get the result size
	int iesi = 0;
	// Skip first record if it is WS zero (used for default properties in style dlg).
	if (vesi.Size() > 0 && vesi[0].m_ws == 0)
		iesi++;
	for (; iesi < vesi.Size(); iesi++)
	{
		WsStyleInfo & esi = vesi[iesi];
		int ws = esi.m_ws;
		*pch++ = (OLECHAR) (ws & 0xffff);
		*pch++ = (OLECHAR) (ws >> 16);
		StrUni stuFont = esi.m_stuFontFamily;
		if (stuFont == "<default>")	// ENHANCE JohnL(?): remove when we support 3 default fonts
			stuFont = static_cast<OLECHAR *>(g_pszDefaultSerif);
		*pch++ = (OLECHAR) stuFont.Length();
		u_strcpy(pch, stuFont.Chars());
		pch += stuFont.Length();

		if (esi.m_stuFontVar.Length())
		{
			*pch++ = (OLECHAR) -1; // 1 additional string property
			*pch++ = (OLECHAR) ktptFontVariations;
			*pch++ = (OLECHAR) esi.m_stuFontVar.Length();
			u_strcpy(pch, esi.m_stuFontVar.Chars());
			pch += esi.m_stuFontVar.Length();
		}
		// Enhance: add any other string properties in the same way as ktptFontVariations.

		OLECHAR * pchCount = pch; // place holder to fill in later
		pch++;
		int cprop = 0;

		// Use the hard-coded static list to get them in the right order:
		const int * prgtpt = NULL;
		int ctpt = FwStyledText::WsStylesPropList(&prgtpt);
		for (int itpt = 0; itpt < ctpt; itpt++)
		{
			switch (prgtpt[itpt])
			{
			case ktptFontSize:
				MakeProp(pch, ktptFontSize, ktpvMilliPoint, esi.m_mpSize, cprop);
				break;
			case ktptBold:
				{
					int nVal = esi.m_fBold;
					if (nVal == kttvInvert && fForPara)
						nVal = kttvForceOn;
					MakeProp(pch, ktptBold, ktpvEnum, nVal, cprop);
				}
				break;
			case ktptItalic:
				{
					int nVal = esi.m_fItalic;
					if (nVal == kttvInvert && fForPara)
						nVal = kttvForceOn;
					MakeProp(pch, ktptItalic, ktpvEnum, nVal, cprop);
				}
				break;
			case ktptSuperscript:
				MakeProp(pch, ktptSuperscript, ktpvEnum, esi.m_ssv, cprop);
				break;
			case ktptForeColor:
				MakeProp(pch, ktptForeColor, ktpvDefault, (int)esi.m_clrFore, cprop);
				break;
			case ktptBackColor:
				MakeProp(pch, ktptBackColor, ktpvDefault, (int)esi.m_clrBack, cprop);
				break;
			case ktptUnderColor:
				MakeProp(pch, ktptUnderColor, ktpvDefault, (int)esi.m_clrUnder, cprop);
				break;
			case ktptUnderline:
				MakeProp(pch, ktptUnderline, ktpvEnum, esi.m_unt, cprop);
				break;
			case ktptOffset:
				MakeProp(pch, ktptOffset, ktpvMilliPoint, esi.m_mpOffset, cprop);
				break;
			default:
				Assert(false);
			}
		}
		*pchCount = (OLECHAR) cprop;
	}
	StrUni stuRet(rgch, pch - rgch); // DON'T count on null termination! May have 0's in it.
	return stuRet;
}

/*----------------------------------------------------------------------------------------------
	Convert obsolete default font names from those stored in the string to those handled by
	FieldWorks.
----------------------------------------------------------------------------------------------*/
void FwStyledText::ConvertDefaultFontInput(StrUni & stuFont)
{
	if (stuFont == g_pszDefaultMono || stuFont == L"<default fixed>")
	{
		stuFont.Assign(L"Courier New");
	}
}


// REQUIRES Render.idh
/*----------------------------------------------------------------------------------------------
//	Decode the font props string from a style definition for a single writing system.
//	These are all the "inherited" values for a run (ie, inherited from the style).
//	NOTE: This method must be kept in sync with FwStyledText::DecodeFontPropsString.
//
//	THIS METHOD IS NOT CURRENTLY USED.
----------------------------------------------------------------------------------------------*/
//void FwStyledText::DecodeFontPropsForEnc(int wsToFind, SmartBstr sbstr,
//	LgCharRenderProps & chrp, StrUni & stuFF, StrUni & stuFontVar, ChrpInheritance & chrpi)
//{
//	const OLECHAR * pch = sbstr.Chars();
//	const OLECHAR * pchLim = pch + sbstr.Length();
//
//	// Each iteration of this loop processes information about one writing system.
//	while (pch < pchLim)
//	{
//		int ws;
//		if (pchLim - pch < 4)
//			ThrowHr(WarnHr(E_FAIL)); // minimum ws style is 4 chars
//		// Don't use ++ within the expression twice! Strange things happen
//		ws = *pch | (*(pch + 1) << 16);
//		pch += 2;
//
//		if ((unsigned int)ws > (unsigned int)wsToFind)
//			return;
//
//		const OLECHAR * pchFF = pch + 1;
//		pch = pchFF + *pch;
//		if (pch >= pchLim)
//			ThrowHr(WarnHr(E_FAIL));
//		if (ws == wsToFind)
//		{
//            if (pch > pchFF)
//				chrpi.xFont = kxSoft;
//			StrUni stuMarkup(pchFF, pch - pchFF);
//			stuFF.Assign(FwStyledText::FontStringMarkupToUi(false, stuMarkup));
//			ConvertDefaultFontInput(stuFF);
//		}
//		int cprop = SignedInt(*pch++);
//
//		if (cprop < 0)
//		{
//			// The next batch of properties are additional string properties;
//			// cprop = -1 * the number of them.
//			for ( ; cprop < 0; cprop++)
//			{
//				int tpt = *pch++;
//				int cch = *pch++;
//				if (ws == wsToFind)
//				{
//					switch (tpt)
//					{
//					case ktptFontVariations:
//						stuFontVar.Assign(pch, cch);
//						if (cch)
//							chrpi.xFontVar = kxSoft;
//						break;
//					default:
//						Assert(false);
//					}
//				}
//				pch += cch;
//			}
//			cprop = *pch++;
//		}
//		// Integer properties
//		if (pchLim - pch < 4 * cprop)
//			ThrowHr(WarnHr(E_FAIL));
//
//		for (; cprop > 0; cprop--)
//		{
//			int tpt = *pch++;
//			int nVar = *pch++;
//			int nVal = *pch | (*(pch + 1) << 16);
//			pch += 2;
//
//			if (ws != wsToFind)
//				continue;
//
//			// Load the property. Ignore any unexpected property or variation, for the
//			// sake of forwards compatibility.
//			switch (tpt)
//			{
//			case ktptFontSize:
//				if (nVar == ktpvMilliPoint)
//				{
//					chrp.dympHeight = nVal;
//					chrpi.xSize = kxSoft;
//				}
//				break;
//			case ktptBold:
//				if (nVar == ktpvEnum)
//				{
//					chrp.ttvBold = nVal;
//					chrpi.xBold = kxSoft;
//				}
//				break;
//			case ktptItalic:
//				if (nVar == ktpvEnum)
//				{
//					chrp.ttvItalic = nVal;
//					chrpi.xItalic = kxSoft;
//				}
//				break;
//			case ktptSuperscript:
//				if (nVar == ktpvEnum)
//				{
//					chrp.ssv = nVal;
//					chrpi.xSs = kxSoft;
//				}
//				break;
//			case ktptForeColor:
//				if (nVar == ktpvDefault)
//				{
//					chrp.clrFore = nVal;
//					chrpi.xFore = kxSoft;
//				}
//				break;
//			case ktptBackColor:
//				if (nVar == ktpvDefault)
//				{
//					chrp.clrBack = nVal;
//					chrpi.xBack = kxSoft;
//				}
//				break;
//			case ktptUnderColor:
//				if (nVar == ktpvDefault)
//				{
//					chrp.clrUnder = nVal;
//					chrpi.xUnder = kxSoft;
//				}
//				// Report this only if an underline style is also given
//				break;
//			case ktptUnderline:
//				if (nVar == ktpvEnum)
//				{
//					chrp.unt = nVal;
//					chrpi.xUnderT = kxSoft;
//				}
//				break;
//			case ktptOffset:
//				if (nVar == ktpvMilliPoint)
//				{
//					chrp.dympOffset = nVal;
//					chrpi.xOffset = kxSoft;
//				}
//				break;
//			default:
//				Warn("Unexpected style property found");
//				break; // Ignore others (for forwards compatibility)
//			}
//		}
//
//		// We've found what we want: return.
//		Assert(ws == wsToFind);
//		return;
//	}
//}

/*----------------------------------------------------------------------------------------------
	Convert the special font names from those stored in the string to those displayed in the UI.

	TODO: put UI strings in a resource file.
----------------------------------------------------------------------------------------------*/
StrUni FwStyledText::FontStringMarkupToUi(bool f1DefaultFont, StrUni stuMarkup)
{
	if (f1DefaultFont)
	{
		if (stuMarkup == g_pszDefaultSerif || stuMarkup == g_pszDefaultSans ||
			stuMarkup == g_pszDefaultMono || stuMarkup == g_pszDefaultBodyFont)
		{
			return DefaultFontUi();
		}
	}
	else
	{
		if (stuMarkup == g_pszDefaultSerif)
			return DefaultFontUi();
		else if (stuMarkup == g_pszDefaultSans)
			return DefaultHeadingFontUi();
		else if (stuMarkup == g_pszDefaultBodyFont)
			return DefaultBodyFontUi();
		else if (stuMarkup == g_pszDefaultMono)
			return DefaultMonoFontUi();
	}
	return stuMarkup;
}

/*----------------------------------------------------------------------------------------------
	Convert the special font names from those used in the UI to what is stored in the string.

	TODO: put UI strings in a resource file.
	Review: Do we need to handle "<default>"?
----------------------------------------------------------------------------------------------*/
StrUni FwStyledText::FontStringUiToMarkup(StrUni stuUi)
{
	if (stuUi == DefaultFontUi())
		return (OLECHAR*)g_pszDefaultSerif;
	else if (stuUi == DefaultHeadingFontUi())
		return (OLECHAR*)g_pszDefaultSans;
	else if (stuUi == DefaultBodyFontUi()) // Defining this string fixed LT-6188.
		return (OLECHAR*)g_pszDefaultBodyFont;
	else if (stuUi == DefaultMonoFontUi())
		return (OLECHAR*)g_pszDefaultMono;
	else
		return stuUi;
}

/*----------------------------------------------------------------------------------------------
	Convert the special font names from those used in the UI to what is stored in the string.

	TODO: get UI strings from a resource file.
----------------------------------------------------------------------------------------------*/
void FwStyledText::FontUiStrings(bool f1DefaultFont, Vector<StrUni> & vstu)
{
	if (f1DefaultFont)
	{
		vstu.Push(DefaultFontUi());
	}
	else
	{
		vstu.Push(DefaultFontUi());
		vstu.Push(DefaultHeadingFontUi());
		//vstu.Push(DefaultMonoFontUi());
	}
}

/*----------------------------------------------------------------------------------------------
	Return the font markup string to use when the UI string is empty.
----------------------------------------------------------------------------------------------*/
StrUni FwStyledText::FontDefaultMarkup()
{
	return (OLECHAR*)g_pszDefaultSerif;
}

/*----------------------------------------------------------------------------------------------
	Return the UI font string to use when the UI string is empty.

	TODO: put UI strings in a resource file.
----------------------------------------------------------------------------------------------*/
StrUni FwStyledText::FontDefaultUi(bool f1DefaultFont)
{
	return DefaultFontUi();
}

/*----------------------------------------------------------------------------------------------
	Return true if the given string matches one of the standard defaults.
----------------------------------------------------------------------------------------------*/
bool FwStyledText::MatchesDefaultSerifMarkup(StrUni stu)
{
	return (stu == g_pszDefaultSerif);
}

bool FwStyledText::MatchesDefaultSansMarkup(StrUni stu)
{
	return (stu == g_pszDefaultSans);
}

bool FwStyledText::MatchesDefaultBodyFontMarkup(StrUni stu)
{
	return (stu == g_pszDefaultBodyFont);
}

bool FwStyledText::MatchesDefaultMonoMarkup(StrUni stu)
{
	return (stu == g_pszDefaultMono);
}

/*----------------------------------------------------------------------------------------------
	Convert the font markup string (eg, <default heading font>) to some reasonable font name.
----------------------------------------------------------------------------------------------*/
StrUni FwStyledText::FontMarkupToFontName(StrUni stuMarkup)
{
	if (stuMarkup == g_pszDefaultSerif)
		return L"Times New Roman";
	else if (stuMarkup == g_pszDefaultSans)
		return L"Arial";
	else if (stuMarkup == g_pszDefaultBodyFont)
		return L"Charis SIL";
	else if (stuMarkup == g_pszDefaultMono)
		return L"Courier New";
	else
		return stuMarkup;
}

// Explicit instantiation.
#include "HashMap_i.cpp"
#include "Vector_i.cpp"
