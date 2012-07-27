/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: VwPattern.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Implementation for VwPattern and VwSearchKiller
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
//:>	VwPattern Methods
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwPattern::VwPattern()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	Assert(m_fStoppedAtLimit == 0);
	m_sbstrDefaultCharStyle = L"<!default chars!>";
	m_fMatchDiacritics = true;
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwPattern::~VwPattern()
{
	CleanupRegexPattern();
	ModuleEntry::ModuleRelease();
}

//:>--------------------------------------------------------------------------------------------
//:>	IUnknown Methods
//:>--------------------------------------------------------------------------------------------
STDMETHODIMP VwPattern::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwPattern)
		*ppv = static_cast<IVwPattern *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwPattern);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>--------------------------------------------------------------------------------------------
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>--------------------------------------------------------------------------------------------
static GenericFactory g_fact(
	_T("SIL.Views.VwPattern"),
	&CLSID_VwPattern,
	_T("SIL Search Pattern"),
	_T("Apartment"),
	&VwPattern::CreateCom);


void VwPattern::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwPattern> qzpat;
	qzpat.Attach(NewObj VwPattern());		// ref count initialy 1
	CheckHr(qzpat->QueryInterface(riid, ppv));
}

//:>--------------------------------------------------------------------------------------------
//:>	IVwPattern Methods
//:>--------------------------------------------------------------------------------------------

/*----------------------------------------------------------------------------------------------
	Set the pattern to be searched for. Currently this is just a sequence of chars
	to match. Later we may add options for regular expression patterns.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::putref_Pattern(ITsString * ptssPattern)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptssPattern);
	m_qtssPattern = ptssPattern;
	m_fCompiled = false;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get the pattern to be searched for.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_Pattern(ITsString ** pptssPattern)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssPattern);
	*pptssPattern = m_qtssPattern;
	AddRefObj(*pptssPattern);
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set the overlay to use in the search.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::putref_Overlay(IVwOverlay * pvo)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pvo);
	m_qvo = pvo;
	m_fCompiled = false;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get the overlay to use in the search.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_Overlay(IVwOverlay ** ppvo)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppvo);
	*ppvo = m_qvo;
	AddRefObj(*ppvo);
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set whether to Match case distinctions. Default is to ignore case.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_MatchCase(ComBool fMatch)
{
	BEGIN_COM_METHOD;
	m_fCompiled = false;
	m_fMatchCase = (bool)fMatch;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get whether to match case exactly. Default is to ignore case.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_MatchCase(ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMatch);
	*pfMatch = m_fMatchCase;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set whether to Match diacritics. Default is to ignore diacritics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_MatchDiacritics(ComBool fMatch)
{
	BEGIN_COM_METHOD;
	m_fMatchDiacritics = (bool)fMatch;
	m_fCompiled = false;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get whether to match diacritics exactly. Default is to ignore diacritics.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_MatchDiacritics(ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMatch);
	*pfMatch = m_fMatchDiacritics;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set whether to match whole words only (that is, there must be a word boundary
	before and after the pattern). Default is false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_MatchWholeWord(ComBool fMatch)
{
	BEGIN_COM_METHOD;
	m_fMatchWholeWord = (bool)fMatch;
	m_fCompiled = false;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set whether to match whole words only (that is, there must be a word boundary
	before and after the pattern). Default is false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_MatchWholeWord(ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMatch);
	*pfMatch = m_fMatchWholeWord;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set whether to match old writing system (that is, corresponding characters in
	input and output must have same writing system and ows). Default is false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_MatchOldWritingSystem(ComBool fMatch)
{
	BEGIN_COM_METHOD;
	m_fMatchWritingSystem = (bool)fMatch;
	m_fCompiled = false;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get whether to match old writing system (that is, corresponding characters in
	input and output must have same writing system and ows). Default is false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_MatchOldWritingSystem(ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMatch);
	*pfMatch = m_fMatchWritingSystem;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Set whether to treat character sequences that are canonically equivalent
	as defined by Unicode as being identical. Default is true.
	Not currently implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_MatchExactly(ComBool fMatch)
{
	BEGIN_COM_METHOD;
	m_fCompiled = false;
	m_fMatchExactly = (bool)fMatch;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get whether to treat character sequences that are canonically equivalent
	as defined by Unicode as being identical. An exact match means that
	canonically equivalent characters are not treated as the same. Default is false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_MatchExactly(ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMatch);
	*pfMatch = m_fMatchExactly;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set whether to treat character sequences that are canonically equivalent
	as defined by Unicode as being identical. Default is true.
	Not currently implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_UseRegularExpressions(ComBool fMatch)
{
	BEGIN_COM_METHOD;
	m_fCompiled = false;
	m_fUseRegularExpressions = (bool)fMatch;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get whether to treat character sequences that are canonically equivalent
	as defined by Unicode as being identical. An exact match means that
	canonically equivalent characters are not treated as the same. Default is false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_UseRegularExpressions(ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMatch);
	*pfMatch = m_fUseRegularExpressions;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Check this (should be null) to obtain any error message, especially from regular
	expression operations.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_ErrorMessage(BSTR * pbstrMsg)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrMsg);
	if (m_stuErrorMessage.Length() > 0)
		*pbstrMsg = SysAllocString(m_stuErrorMessage.Chars());
	// If the pattern needs compiling, do so, and see if that produces an error.
	if (!m_fCompiled)
		Compile();
	if (m_stuErrorMessage.Length() > 0)
		*pbstrMsg = SysAllocString(m_stuErrorMessage.Chars());
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

void Transfer(int ichMin, int ichLim, ITsIncStrBldr * ptisb, ITsString * ptss, int cch)
{
	if (ichLim == ichMin)
		return;
	if (ichMin == 0 && ichLim == cch)
	{
		CheckHr(ptisb->AppendTsString(ptss));
		return;
	}
	ITsStrBldrPtr qtsb;
	CheckHr(ptss->GetBldr(&qtsb));
	if (ichLim < cch)
		CheckHr(qtsb->ReplaceTsString(ichLim, cch, NULL));
	if (ichMin > 0)
		CheckHr(qtsb->ReplaceTsString(0, ichMin, NULL));
	ITsStringPtr qtssTransfer;
	CheckHr(qtsb->GetString(&qtssTransfer));
	CheckHr(ptisb->AppendTsString(qtssTransfer));
}

bool IsEmpty(ITsString * ptss)
{
	if (!ptss)
		return true;
	int cch;
	CheckHr(ptss->get_Length(&cch));
	return cch == 0;
}
/*----------------------------------------------------------------------------------------------
	By default this answers the same as ReplaceWith(). However, if a find has recently
	occurred involving a regular expression, this computes the replacement text,
	taking account of any uses of saved groups in the replace with string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_ReplacementText(ITsString ** pptssText)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssText);
	int ichMinRen = m_ichMinFoundLog;
	int ichLimRen = m_ichLimFoundLog;
	VwTxtSrcPtr qts;
	m_qtsWhereFound->QueryInterface(CLSID_VwStringTextSource, (void **) & qts);
	if (qts)
	{
		// It's one of ours...adjust log to render. Assume any other kind of text source doesn't need this.
		ichMinRen = qts->LogToRen(m_ichMinFoundLog);
		ichLimRen = qts->LogToRen(m_ichLimFoundLog);
	}
	ITsStringPtr qtssSrc;
	CheckHr(m_qtsWhereFound->GetSubString(ichMinRen, ichLimRen, &qtssSrc));
	if (!m_pmatcher)
	{
		// non-regular expression search. Apart from a couple of special cases, return the
		// replace-with text.
		if (m_fMatchWritingSystem && IsEmpty(m_qtssReplaceWith) && IsEmpty(m_qtssPattern))
		{
			// Special case matching writing system with empty pattern and replacement:
			// replacement is input with WS changed.
			ITsStrBldrPtr qtsb;
			CheckHr(qtssSrc->GetBldr(&qtsb));
			ITsTextPropsPtr qtpt;
			CheckHr(m_qtssReplaceWith->get_Properties(0, &qtpt));
			int ws, nvar;
			CheckHr(qtpt->GetIntPropValues(ktptWs, &nvar, &ws));
			int cch;
			CheckHr(qtsb->get_Length(&cch));
			CheckHr(qtsb->SetIntPropValues(0, cch, ktptWs, ktpvDefault, ws));
			CheckHr(qtsb->GetString(pptssText));
		}
		else
		{
			// If input isn't a smart text source or doesn't have any ORCs we need to keep, just
			// return the replacement string.
			*pptssText = m_qtssReplaceWith;
			AddRefObj(*pptssText);
		}
	}
	// regular expressions active: if we have a replace-with, build replacement text allowing for $n substitutions.
	else if (m_qtssReplaceWith)
	{
		int cch;
		const OLECHAR * pch;
		CheckHr(m_qtssReplaceWith->LockText(&pch, &cch));
		try
		{
			ITsIncStrBldrPtr qtisb;
			qtisb.CreateInstance(CLSID_TsIncStrBldr);
			int ichLast = 0; // index of last character dealt with

			for (int ich = 0; ich < cch - 1; )
			{
				OLECHAR ch = pch[ich];
				if (ch == L'\\')
				{
					// escape the next character. We do this by transferring whatever we haven't
					// already up to ich-1, then advancing ich and ichLast past the escaped character
					Transfer(ichLast, ich, qtisb, m_qtssReplaceWith, cch);
					ichLast = ich + 1; // next to transfer is the escaped character, omitting the backslash
					ich += 2; // next character we investigate is AFTER the escaped character
					continue;
				}
				if (ch == L'$')
				{
					OLECHAR chN = pch[ich + 1];
					if (chN >= L'0' && chN <= L'9')
					{
						int iGroup = chN - L'0';
						ITsStringPtr qtssGroup;
						CheckHr(get_Group(iGroup, &qtssGroup));
						Transfer(ichLast, ich, qtisb, m_qtssReplaceWith, cch);
						if (qtssGroup)
						{
							ITsTextPropsPtr qttp;
							CheckHr(m_qtssReplaceWith->get_PropertiesAt(ich, &qttp));
							if (m_fMatchWritingSystem)
							{
								// Force the matched text to the writing system of the $ character.
								int ws, nvar;
								CheckHr(qttp->GetIntPropValues(ktptWs, &nvar, &ws));
								ITsStrBldrPtr qtsb;
								CheckHr(qtssGroup->GetBldr(&qtsb));
								int len;
								CheckHr(qtssGroup->get_Length(&len));
								CheckHr(qtsb->SetIntPropValues(0, len, ktptWs, ktpvDefault, ws));
								CheckHr(qtsb->GetString(&qtssGroup));
							}
							// If the $ character has a named style, force the output to match.
							SmartBstr sbstrStyle;
							CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrStyle));
							if (BstrLen(sbstrStyle))
							{
								ITsStrBldrPtr qtsb;
								CheckHr(qtssGroup->GetBldr(&qtsb));
								int len;
								CheckHr(qtssGroup->get_Length(&len));
								CheckHr(qtsb->SetStrPropValue(0, len, ktptNamedStyle, sbstrStyle));
								CheckHr(qtsb->GetString(&qtssGroup));
							}
							CheckHr(qtisb->AppendTsString(qtssGroup));
						}
						// Otherwise just ignore
						ich += 2; // past $n
						ichLast = ich; // next possible transfer character is after $n
						continue;
					}
				}
				ich ++; // normal single increment, current char will just transfer.
			}
			Transfer(ichLast, cch, qtisb, m_qtssReplaceWith, cch); // get the tail end if any
			if (cch)
				CheckHr(qtisb->GetString(pptssText));
			else
			{
				// Empty string, builder has no props, just use current ReplaceWith
				*pptssText = m_qtssReplaceWith;
				AddRefObj(*pptssText);
			}
		}
		catch (...)
		{
			m_qtssReplaceWith->UnlockText(pch);
			throw;
		}
		CheckHr(m_qtssReplaceWith->UnlockText(pch));
	}

	// By all paths to this point, *pptssText should be the result we want to return,
	// except that possibly we need to append ORCs that were omitted from the match.
	// Deal with that now, if it is our own text source.
	ITsStrBldrPtr qtsb; // init if we need it
	if (qts)
	{
		int crun;
		CheckHr(qtssSrc->get_RunCount(&crun));
		for (int irun = 0; irun < crun; irun++)
		{
			ITsTextPropsPtr qttp;
			TsRunInfo tri;
			CheckHr(qtssSrc->FetchRunInfo(irun, &tri, &qttp));
			if (tri.ichMin == tri.ichLim - 1)
			{
				// length 1 run might be something we need to save
				OLECHAR ch;
				CheckHr(qtssSrc->FetchChars(tri.ichMin, tri.ichMin + 1, &ch));
				if (ch == 0xfffc)
				{
					// It's an ORC run, check it out.
					if (VwMappedTxtSrc::OmitOrcFromSearch(qtssSrc, tri.ichMin))
					{
						// Ones we omit from the search are exactly the ones we need to insert into
						// the result.
						if (!qtsb)
						{
							if (*pptssText)
								CheckHr((*pptssText)->GetBldr(&qtsb));
							else
								qtsb.CreateInstance(CLSID_TsStrBldr);
						}
						int cchBldr;
						CheckHr(qtsb->get_Length(&cchBldr));
						ITsStrBldrPtr qtsbOrc;
						CheckHr(qtssSrc->GetBldr(&qtsbOrc));
						int cchSrc;
						CheckHr(qtssSrc->get_Length(&cchSrc));
						CheckHr(qtsbOrc->ReplaceTsString(tri.ichMin + 1, cchSrc, NULL));
						CheckHr(qtsbOrc->ReplaceTsString(0, tri.ichMin, NULL));
						ITsStringPtr qtssOrc;
						CheckHr(qtsbOrc->GetString(&qtssOrc));
						CheckHr(qtsb->ReplaceTsString(cchBldr, cchBldr, qtssOrc));
					}
				}
			}
		}
	}

	if (qtsb)
	{
		ReleaseObj(*pptssText);
		// We found at least one ORC, get the combined string.
		CheckHr(qtsb->GetString(pptssText));
	}


	END_COM_METHOD(g_fact, IID_IVwPattern);
}
/*----------------------------------------------------------------------------------------------
	If a regular expression has just been searched successfully, this returns the
	text of the indicated group. 0 answers the whole matched string, 1 the text of the
	substring matched by the first () group, 2, the text of the second, and so forth.
	Returns an empty string (but not a bad HR) for index out of range.
	Returns E_UNEXPECTED if there has not been a previous regular expression match.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_Group(int iGroup, ITsString ** pptssGroup)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssGroup);
	if (!m_pmatcher)
		ThrowHr(WarnHr(E_UNEXPECTED));
	UErrorCode status = U_ZERO_ERROR;
	int ichMinRen = m_pmatcher->start(iGroup, status);
	int ichLimRen = m_pmatcher->end(iGroup, status);
	if (U_FAILURE(status))
	{
		status = U_ZERO_ERROR;
		// In hopes of returning an empty string in the right WS, take an empty string from the
		// start of the whole match.
		ichMinRen = ichLimRen = m_pmatcher->start(0, status);
	}
	CheckHr(m_qtsWhereFound->GetSubString(ichMinRen, ichLimRen, pptssGroup));
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Set whether to treat character sequences that are equivalent
	as defined by Unicode compatibility decompositions as being identical.
	Default is false.
	Not currently implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_MatchCompatibility(ComBool fMatch)
{
	BEGIN_COM_METHOD;
	m_fCompiled = false;
	m_fMatchCompatibility = (bool)fMatch;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get whether to treat character sequences that are equivalent
	as defined by Unicode compatibility decompositions as being identical.
	Default is false.
	Not currently implemented.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_MatchCompatibility(ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMatch);
	*pfMatch = m_fMatchCompatibility;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Find the first (or last) match of the pattern in the root box.
	Return S_FALSE if not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::Find(IVwRootBox * prootb, ComBool fForward, IVwSearchKiller * pxserkl)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prootb);
	ChkComArgPtrN(pxserkl);

	if (pxserkl)
	{
		ComBool fAbort;
		CheckHr(pxserkl->get_AbortRequest(&fAbort));
		if (fAbort == ComBool(true))
			return S_OK;
	}

	VwRootBoxPtr qrootb;
	HRESULT hr = prootb->QueryInterface(CLSID_VwRootBox, (void **)&qrootb);
	if (hr == E_NOINTERFACE)
		hr = prootb->QueryInterface(CLSID_VwInvertedRootBox, (void **)&qrootb);
	CheckHr(hr);
	m_pboxStart = qrootb;
	m_qselFound.Clear();
	m_plzbFound = false;
	return FindNext(fForward, pxserkl);

	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Find the next (or previous) match of the pattern, starting from the specified
	selection.
	Return S_FALSE if not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::FindFrom(IVwSelection * psel, ComBool fForward,
	IVwSearchKiller * pxserkl)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(psel);
	ChkComArgPtrN(pxserkl);

	if (pxserkl)
	{
		ComBool fAbort;
		CheckHr(pxserkl->get_AbortRequest(&fAbort));
		if (fAbort == ComBool(true))
			return S_OK;
	}

	// Pretend the starting point is a previous match and go on. It better be one of our
	// text selections.
	try {
		CheckHr(psel->QueryInterface(CLSID_VwTextSelection, (void **)(&m_qselFound)));
	}
	catch(Throwable& thr){
		if (thr.Result() == E_NOINTERFACE)
		{
			VwPictureSelectionPtr qpsel;
			CheckHr(psel->QueryInterface(CLSID_VwPictureSelection, (void **)(&qpsel)));
			m_pboxStart = qpsel->LeafBox();
		}
		else{
			throw thr;
		}
	}
	// Start with the box at the end of the selection if searching forward, at the start
	// if searching backwards.
	VwParagraphBox * pvpbox;
	int ichDummy;
	m_qselFound->GetLimit(fForward, &pvpbox, &ichDummy);
	m_pboxStart = pvpbox;
	m_plzbFound = false;
	return FindNext(fForward, pxserkl);

	END_COM_METHOD(g_fact, IID_IVwPattern);
}

#define kcboxDoMaxLazy 20 // How many boxes do we search before mazimizing laziness?
/*----------------------------------------------------------------------------------------------
	Find the next (or previous) match of the pattern, starting from a position
	determined by a previous search.
	Return S_FALSE if not found.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::FindNext(ComBool fForward, IVwSearchKiller * pxserkl)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pxserkl);

	m_fFound = false;
	m_fStoppedAtLimit = false;
	m_fForward = (bool)fForward;
	// If we don't have a place to start searching by now we're in trouble.
	if (!m_pboxStart)
		ThrowInternalError(E_UNEXPECTED, "FindNext lacks starting box");
	m_pboxStart->Search(this, pxserkl);

	if (pxserkl)
	{
		ComBool fAbort;
		CheckHr(pxserkl->get_AbortRequest(&fAbort));
		if (fAbort == ComBool(true))
			return S_OK;
	}

	if (m_fFound)
		return S_OK;
	// From now on no starting point, just search the whole box.
	m_qselFound = NULL;
	m_plzbFound = NULL;
	int cLazy = kcboxDoMaxLazy;
	if (fForward)
	{
		for ( ; ; )
		{
			if (pxserkl)
				CheckHr(pxserkl->FlushMessages());

			// OPTIMIZE JohnT: to better support laziness, pass false here and below,
			// and implement a smart Search on VwLazyBox.
			// It needs to create a special VwEnv, which makes no real boxes, but accumulates
			// strings until it has a paragraph, then searches it. It needs to be able
			// to expand at the necessary point if it finds successfully. Of course it
			// must not create lazy boxes as it goes, but process all targets. The big saving
			// is not having to create or lay out boxes.
			m_pboxStart = m_pboxStart->NextInRootSeq(true, pxserkl);

			if (pxserkl)
			{
				ComBool fAbort;
				CheckHr(pxserkl->get_AbortRequest(&fAbort));
				if (fAbort == ComBool(true))
					return S_OK;
			}

			if (!m_pboxStart)
				return S_FALSE;

			if (cLazy-- == 0)
			{
				cLazy = kcboxDoMaxLazy;
				m_pboxStart->Root()->MaximizeLaziness(m_pboxStart, m_pboxStart->NextOrLazy());
			}

			m_pboxStart->Search(this, pxserkl);

			if (pxserkl)
			{
				ComBool fAbort;
				CheckHr(pxserkl->get_AbortRequest(&fAbort));
				if (fAbort == ComBool(true))
					return S_OK;
			}

			if (m_fStoppedAtLimit || m_fFound)
				return S_OK;
		}
	}
	else
	{
		for ( ; ; )
		{
			if (pxserkl)
				CheckHr(pxserkl->FlushMessages());

			m_pboxStart = m_pboxStart->NextInReverseRootSeq(true, pxserkl);
			if (!m_pboxStart)
				return S_FALSE;
			if (cLazy-- == 0)
			{
				cLazy = kcboxDoMaxLazy;
				m_pboxStart->Root()->MaximizeLaziness(m_pboxStart, m_pboxStart->NextOrLazy());
			}
			m_pboxStart->Search(this, pxserkl);

			if (pxserkl)
			{
				ComBool fAbort;
				CheckHr(pxserkl->get_AbortRequest(&fAbort));
				if (fAbort == ComBool(true))
					return S_OK;
			}

			if (m_fStoppedAtLimit || m_fFound)
				return S_OK;
		}
	}
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set a selection that will function as a limit for the search. This should always
	be an insertion point. Find will stop and fail if it reaches this point.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::putref_Limit(IVwSelection * psel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(psel);
	m_qselLimit = psel;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get the selection that will function as a limit for the search. This should always
	be an insertion point. Find will stop and fail if it reaches this point.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_Limit(IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	*ppsel = m_qselLimit;
	AddRefObj(*ppsel);
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Store a selection that is, conceptually, the starting point for the current
	sequence of searches. This is in the interface as a convenience for the client,
	so that all the necessary information about the nature of the current search can
	be saved in the VwPattern. It has no influence on the behavior of the pattern.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::putref_StartingPoint(IVwSelection * psel)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(psel);
	m_qselStartingPoint = psel;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Retrieve a selection that is, conceptually, the starting point for the current
	sequence of searches. This is in the interface as a convenience for the client,
	so that all the necessary information about the nature of the current search can
	be saved in the VwPattern. The pattern never modifies it except through the
	putref_StartingPoint method.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_StartingPoint(IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	*ppsel = m_qselStartingPoint;
	AddRefObj(*ppsel);
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Store a handle to the window being searched. This has no influence on the
	behavior of the pattern, but is part of allowing the state of a sequence of
	searches to be saved. (The pattern does not care whether this is really an HWND.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_SearchWindow(DWORD hwnd)
{
	BEGIN_COM_METHOD;
	m_hwndSearch = hwnd;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get a handle to the window being searched. This just retrieves what has been
	set by put_SearchWindow (or 0, if SearchWindow has not been called).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_SearchWindow(DWORD * phwnd)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(phwnd);
	*phwnd = m_hwndSearch;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	This allows us to find out whether the search terminated at the limit or
	at the start/end of the view. It is false in all circumstances except
	when the most recent search attempt failed because it reached the ${#Limit}.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_StoppedAtLimit(ComBool * pfAtLimit)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfAtLimit);
	*pfAtLimit = m_fStoppedAtLimit;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	This allows us to set whether the search terminated at the limit or
	at the start/end of the view. It is normally only used by code internal
	to the View subsystem, but external code using FindIn directly may need
	to set the flag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_StoppedAtLimit(ComBool fAtLimit)
{
	BEGIN_COM_METHOD
	m_fStoppedAtLimit = (bool)fAtLimit;
	return S_OK;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Figure the length of the string resulting from converting cch characters in the way
	required to get a string for matching against, as donein FindIn.
	Assume that pchBuf1 and pchBuf2 are big enough to hold the converted data.
	cchMax gives the actual length of the buffers.
----------------------------------------------------------------------------------------------*/
int VwPattern::FindCorrespondingIndex(OLECHAR * pchOrig, int cch, OLECHAR *pchBuf1,
	OLECHAR * pchBuf2, ILgCharacterPropertyEngine * pcpe, int cchBuf)
{
	CopyItems(pchOrig, pchBuf1, cch);
	if (m_fMatchCompatibility)
	{
		Assert(m_fMatchCompatibility);
		CheckHr(pcpe->NormalizeKdRgch(pchBuf1, cch, pchBuf2, cchBuf, &cch));
		SwapVars(pchBuf1, pchBuf2);
	}
	if (m_fMatchCase)
	{
		CheckHr(pcpe->ToUpperRgch(pchBuf1, cch, pchBuf2, cchBuf, &cch));
		SwapVars(pchBuf1, pchBuf2);
	}
	if (!m_fMatchDiacritics)
	{
		CheckHr(pcpe->StripDiacriticsRgch(pchBuf1, cch, pchBuf2, cchBuf, &cch));
		SwapVars(pchBuf1, pchBuf2);
	}
	return cch;
}

/*----------------------------------------------------------------------------------------------
	Figure the position in pchOrig that corresponds to ich, where ich is an index into the
	buffer that results from converting pchOrig as needed for searching for this pattern.
	As well as being passed the original cch characters in pchOrig, it is passed the number
	of characters that results from converting all of them (cchConv), two buffers that are
	both big enough to convert all the characters into, and a character property engine to
	do the conversions.

	If we are not matching diactritics, it advances past any diacritics, so that they will
	correctly be included along with their corresponding base character.

	cchMax gives the actual length of the buffers.
----------------------------------------------------------------------------------------------*/
int VwPattern::FixIndex(OLECHAR * pchOrig, int cch, int cchConv, int ich, OLECHAR *pchBuf1,
	OLECHAR * pchBuf2, ILgCharacterPropertyEngine * pcpe, int cchBuf)
{
	// First guess is the exact same number, this will often be right.
	int ichGuessLow = 0; // a possible answer that generates too few converted characters.
	int ichOutLow = 0; // number of converted characters when we convert ichGuessLow.
	int ichGuessHigh = cch; // a possible answer that generates too many converted characters.
	int ichOutHigh = cchConv; // number of converted chars when we convert ichGuessHigh.
	// Loop invariant: ichOutLow < ich && ichOutHigh > ich. Ensure this is true initially.
	if (ich <= 0)
		return 0;
	if (ich >= cchConv)
		return cch;
	// Current guess, start with the desired number of output chars, which is often right.
	// Each iteration either increases ichGuessLow or decreases ichGuessHigh.
	int ichGuess = ich;
	for ( ; ; )
	{
		int ichOut = FindCorrespondingIndex(pchOrig, ichGuess, pchBuf1, pchBuf2, pcpe, cchBuf);
		if (ichOut == ich)
			break; // got it! Usually this will happen first time.
		else if (ichOut < ich)
		{
			// Current guess is too low. Increase it by the amount we are off, but
			// not to more than ichGuessHigh - 1.
			ichGuessLow = ichGuess;
			ichOutLow = ichOut;
			ichGuess = std::min(ichGuess + ich - ichOut, ichGuessHigh - 1);
		}
		else
		{
			// Current guess is too high. Decrease it by the amount we are off, but
			// not to less than ichGuessLow + 1.
			ichGuessHigh = ichGuess;
			ichOutHigh = ichOut;
			ichGuess = std::max(ichGuess + ich - ichOut, ichGuessLow + 1);
		}
		// It is pathologically possible that no guess gives the right answer. Suppose for
		// example that the position we want is after character 5; but converting 2 characters
		// gives 4, and converting 3 gives 6. In this case we will arbitrarily answer the
		// lower number.
		if (ichGuess <= ichGuessLow || ichGuess >= ichGuessHigh)
		{
			Assert(ichGuessLow == ichGuessHigh - 1);
			ichGuess = ichGuessLow;
			break;
		}
	}
	if (!m_fMatchDiacritics)
	{
		// Skip any.
		for (; ichGuess < cch; ichGuess++)
		{
			LgGeneralCharCategory cc;
			pcpe->get_GeneralCategory(pchOrig[ichGuess], &cc);
			if (cc != kccLm && cc != kccMn)
				break;
		}
	}
	return ichGuess;
}

bool GetIsLetter(ILgCharacterPropertyEngine * pcpe, OLECHAR ch)
{
	ComBool fIsLetter;
	CheckHr(pcpe->get_IsWordForming(ch, &fIsLetter));
	return fIsLetter;
}

/*----------------------------------------------------------------------------------------------
	This was originally the tail end of FindIn, but when I added checking WS, it needed to be
	done in two places: inside the forward loop and also inside the backward loop.
	It is passed the text source in pts and the range of characters being searched
	within it in ichMinSearch and ichLimSearch.
	the converted characters in pchBuf/cchBuf;
	the other conversion buffer we have been using, if any, in pchBuf2/cchBuf2;
	a character property engine to do the conversions if needed;
	a pointer to the actual match in pchMatch, and its length in ichLimMatch.
	It is passed the greatest number of characters in any conversion in cchMax,
	and the number resulting from converting everthing in cchConv.
	It returns true if we have a match (including matching old writing system), assuming that
	all other requirements have been satisfied before calling it.
	It also adjusts ichLimMatch to be relative to pchOrig (that is, measured in unconverted
	characters), and computes ichMinMatch, the offset in pchOrig of the start of the match.

	ENHANCE (SharonC): Possibly rework how to handle empty strings when we have real
	pattern-matching.
----------------------------------------------------------------------------------------------*/
//bool VwPattern::FigureIndexesAndCheckWs(IVwTextSource * pts, int ichMinSearch, int ichLimSearch,
//	OLECHAR * pchBuf, int cchBuf, OLECHAR * pchBuf2, int cchBuf2,
//	ILgCharacterPropertyEngine * pcpe, int cchMax, int cchConv,
//	OLECHAR ** ppchMatch, int & ichMinMatch, int & ichLimMatch)
//{
//	// This is the nasty bit. We got a match...but what position in the untransformed string
//	// does it correspond to? On the assumption that matches are relatively rare compared to
//	// the number of strings searched, we do a good deal of work at this point to figure the
//	// offsets we really want.
//
//	// For now these offsets are relative to the part of the string we are searching, that is,
//	// they are low by ichMinSearch.
//	ichMinMatch = *ppchMatch - pchBuf;
//	ichLimMatch += ichMinMatch;
//
//	OLECHAR * pchOrig = NULL; // Buffer of original characters searched.
//	int cchOrig = ichLimSearch - ichMinSearch;
//
//	if (cchBuf2)
//	{
//		// We had to make at least one conversion. Re-start the process.
//		// To make the measurement, we will have to redo all but the last conversion.
//		// Make sure both buffers are big enough for anything we will encounter.
//		if (cchBuf < cchMax)
//			pchBuf = (OLECHAR *)_alloca(cchMax * isizeof(OLECHAR));
//		if (cchBuf2 < cchMax)
//			pchBuf2 = (OLECHAR *)_alloca(cchMax * isizeof(OLECHAR));
//
//		pchOrig = (OLECHAR *)_alloca(cchOrig * isizeof(OLECHAR));
//		CheckHr(pts->FetchSearch(ichMinSearch, ichLimSearch, pchOrig));
//		// Figure the actual indexes by successive approximation using FindCorrespondingIndex.
//		ichMinMatch = FixIndex(pchOrig, cchOrig, cchConv, ichMinMatch, pchBuf, pchBuf2, pcpe, cchMax);
//		ichLimMatch = FixIndex(pchOrig, cchOrig, cchConv, ichLimMatch, pchBuf, pchBuf2, pcpe, cchMax);
//	}
//
//	if (m_fMatchWritingSystem || m_fMatchStyles || m_fMatchTags)
//	{
//		int crun;
//		CheckHr(m_qtssPattern->get_RunCount(&crun));
//		int cchToMatch;
//		CheckHr(m_qtssPattern->get_Length(&cchToMatch));
//		int ichMinRun = ichMinMatch;
//		int cchSoFar = 0; // characters in previous runs plus this
//		bool fMatch = true; // set to false if we find a problem.
//		// Loop over runs in pattern (often only one).
//		for (int irun = 0; fMatch && irun < crun; ++irun)
//		{
//			SmartBstr sbstrRun;
//			CheckHr(m_qtssPattern->get_RunText(irun, &sbstrRun));
//			Convert(sbstrRun); // To determine its length.
//			cchSoFar += sbstrRun.Length();
//
//			// Now find the ws/ows, char style, and tags we have to match.
//			ITsTextPropsPtr qttp;
//			CheckHr(m_qtssPattern->get_Properties(irun, &qttp));
//			int encToMatch;
//			int var;
//			CheckHr(qttp->GetIntPropValues(ktptWs, &var, &encToMatch));
//			SmartBstr sbstrStyleToMatch;
//			CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrStyleToMatch));
//			//StrUni stuStyleToMatch(sbstrStyleToMatch.Chars());
//			SmartBstr sbstrTagsToMatch;
//			CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrTagsToMatch));
//			int cguidToMatch = BstrLen(sbstrTagsToMatch) / kcchGuidRepLength;
//			Vector<StrUni> vstuGuids;
//			OLECHAR * pch = sbstrTagsToMatch;
//			for (int iguid = 0; iguid < cguidToMatch; iguid++)
//			{
//				if (!m_qvo)
//					continue;
//				ComBool fHidden;
//				COLORREF clrFore, clrBack, clrUnder;
//				int unt, cchAbbr, cchName;
//				CheckHr(m_qvo->GetDispTagInfo(pch, &fHidden, &clrFore, &clrBack, &clrUnder, &unt,
//					NULL, 0, &cchAbbr, NULL, 0, &cchName));
//				if (fHidden)
//					continue;
//				StrUni stuGuid(pch, kcchGuidRepLength);
//				vstuGuids.Push(stuGuid);
//				pch += kcchGuidRepLength;
//			}
//			cguidToMatch = vstuGuids.Size();
//
//			// Now figure the limit of the corresponding run in the search source.
//			// If it is the last run, we have already figured it.
//			int ichLimRun = ichLimMatch;
//			// If the string to match is empty, assume for now we have matched the
//			// entire remainder of the string.
//			if (cchToMatch == 0)
//				ichLimRun = ichLimSearch - ichMinSearch;
//			else if (irun < crun - 1)
//			{
//				// Not the last run--need to figure the corresponding position in
//				// pts.
//				ichLimRun = ichMinMatch + cchSoFar;
//				if (cchBuf2)
//				{
//					// We did text conversions, and must do tricks to figure position.
//					ichLimRun = FixIndex(pchOrig, cchOrig, cchConv, ichLimRun,
//						pchBuf, pchBuf2, pcpe, cchMax);
//				}
//			}
//			// Now see if all the characters from ichMinRun to ichLimRun have
//			// the right ows and ws.
//			for (int ich = ichMinRun; ich < ichLimRun; )
//			{
//				LgCharRenderProps chrp;
//				int ichMinRunProp, ichLimRunProp; // range of chars with same props
//				CheckHr(pts->GetCharProps(ich + ichMinSearch, &chrp,
//					&ichMinRunProp, &ichLimRunProp));
//				bool fWsOkay = true;
//				bool fStyleOkay = true;
//				bool fTagsOkay = true;
//				if (m_fMatchWritingSystem)
//				{
//					fWsOkay = (chrp.ws == encToMatch);
//				}
//				if (m_fMatchStyles)
//				{
//					SmartBstr sbstrName;
//					int ichMinBogus, ichLimBogus;
//					CheckHr(pts->GetCharStringProp(ich + ichMinSearch, ktptNamedStyle, &sbstrName,
//						&ichMinBogus, &ichLimBogus));
//					if (sbstrName != sbstrStyleToMatch)
//						fStyleOkay = false;
//				}
//				if (m_fMatchTags && cguidToMatch > 0)
//				{
//					SmartBstr sbstrTags;
//					int ichMinBogus, ichLimBogus;
//					CheckHr(pts->GetCharStringProp(ich + ichMinSearch, ktptTags, &sbstrTags,
//						&ichMinBogus, &ichLimBogus));
//					int cguid = BstrLen(sbstrTags) / kcchGuidRepLength;
//					for (int iguidToMatch = 0; iguidToMatch < cguidToMatch; iguidToMatch++)
//					{
//						OLECHAR * pchToMatch = (OLECHAR *)vstuGuids[iguidToMatch].Chars();
//						OLECHAR * pchPresent = sbstrTags;
//						for (int iguidPresent = 0; iguidPresent < cguid; iguidPresent++)
//						{
//							if (CompareGuids(pchToMatch, pchPresent) == 0)
//								break; // found match
//							pchPresent += kcchGuidRepLength;
//						}
//						if (iguidPresent >= cguid)
//						{
//							// A required tag was not found.
//							fTagsOkay = false;
//							break;
//						}
//					}
//				}
//
//				if (!fWsOkay || !fStyleOkay || !fTagsOkay)
//				{
//					if (cchToMatch > 0)
//						// We didn't match the ws/ows, style, or tags of the specified string.
//						return false; // also causes break in pattern run loop
//					else if (ich == ichMinRun)
//					{
//						// We're matching just ws/ows/style/tags, and there was not a match.
//						// Adjust the current matching location so we don't search this run
//						// again. -1 accounts for the fact that the caller is going to
//						// increment *ppchMatch.
//						*ppchMatch += ichLimRunProp - (ichMinMatch + ichMinSearch) - 1;
//						return false;
//					}
//					else
//					{
//						// We've hit the end of a run or several runs that match the
//						// ws/ows and style. Adjust ichLimMatch to indicate how much.
//						ichLimMatch = ich;
//						return true;
//					}
//					break;
//				}
//				ich = ichLimRunProp - ichMinSearch; // check next run in dest string if necessary
//			}
//
//			// The limit of this run is the start of the next one (in the pattern).
//			ichMinRun = ichLimRun;
//		}
//		if (cchToMatch == 0)
//		{
//			// Absorb everything to the end of the string.
//			ichLimMatch = ichLimSearch - ichMinSearch;
//		}
//	}
//	return true;
//}


/*----------------------------------------------------------------------------------------------
	This class represents the algorithm of the FindIn method (common behavior between the
	versions using regular expressions and not doing so).
----------------------------------------------------------------------------------------------*/
class FindInAlgorithmBase
{
public:
	IVwTextSource * m_pts; // Original arg, text to search.
	int m_ichStartSearch; // Range of text to search (from start to end).
	int m_ichEndSearch;
	ComBool m_fForward; // true to search forward
	int m_ichMinFoundSearch; // Result variables, the match if any (or -1)
	int m_ichLimFoundSearch;
	IVwSearchKiller * m_pxserkl; // Can be used to figure whether to abort search by user cancel
	VwPattern * m_pat; // The pattern we're trying to match.
	int m_cchSrcSearch; // count of searchable characters in m_pts.
	int m_ichMinSearch; // Range of text to search (from smallest to largest index)
	int m_ichLimSearch;
	OLECHAR * m_pchBuf; // Text contents of m_pts;
	UErrorCode m_error;

	FindInAlgorithmBase(IVwTextSource * pts, int ichStartLog, int ichEndLog, ComBool fForward,
		IVwSearchKiller * pxserkl, VwPattern * pat)
	{
		m_pts = pts;
		CheckHr(m_pts->LogToSearch(ichStartLog, &m_ichStartSearch));
		CheckHr(m_pts->LogToSearch(ichEndLog, &m_ichEndSearch));
		m_fForward = fForward;
		m_pxserkl = pxserkl;
		m_ichMinFoundSearch = -1;
		m_ichLimFoundSearch = -1;
		m_pat = pat;
		m_error = U_ZERO_ERROR;
	}

	virtual ~FindInAlgorithmBase()
	{
	}
	void CheckError()
	{
		if (U_FAILURE(m_error))
			ThrowHr(E_FAIL);
		m_error = U_ZERO_ERROR; // discard warnings so as not to confuse future calls.
	}

	// The search failed, didn't find anything. Make sure we will produce -1, -1.
	// Returning false allows the caller to do "Return Fail();"
	bool Fail()
	{
		m_ichMinFoundSearch = -1;
		m_ichLimFoundSearch = -1;
		return false;
	}

	// Check a candidate match. ICU says it is OK. If it fails on character properties,
	// return false. If it succeeds, attempt to extend it (typically to include more
	// diacritics when not matching diacritics) without exceeding m_ichLimSearch.
	bool CheckAndExtendCandidate()
	{
		if (!CheckMatchProperties())
			return false;
		// We have a match. See if we can extend it with ignorable characters.
		for ( ; ; )
		{
			// We can't if we're at the limit already.
			if (m_ichLimFoundSearch == m_ichLimSearch)
				return true;
			// Try incrementing it...
			m_ichLimFoundSearch++;
			// See if this is still a good match.
			if (!CheckMatchAndProps())
			{
				m_ichLimFoundSearch--;
				break;
			}
		}
		return true;
	}

	bool MatchProps(LgCharRenderProps &chrp, SmartBstr & sbstrNamedStyle,
		SmartBstr & sbstrTags, int ws, int ich)
	{
		if (m_pat->m_fMatchWritingSystem && ws != chrp.ws)
			return false;
		SmartBstr sbstr;
		int ichMinBogus, ichLimBogus;
		if (m_pat->m_fMatchTags)
		{
			CheckHr(m_pts->GetCharStringProp(ich, ktptTags, &sbstr,
				&ichMinBogus, &ichLimBogus));
			// This is a strict binary comparison that will handle embedded nulls
			if (sbstr != sbstrTags)
				return false;
		}
		if (m_pat->m_fMatchStyles)
		{
			CheckHr(m_pts->GetCharStringProp(ich, ktptNamedStyle, &sbstr,
				&ichMinBogus, &ichLimBogus));
			if (sbstr.Length() == 0 && sbstrNamedStyle == m_pat->m_sbstrDefaultCharStyle)
				return true;
			else if (sbstr != sbstrNamedStyle)
				return false;
		}
		return true;
	}

	// We invoke this instead of the main body of Run() if the search string is empty.
	// This means we are searching for runs in m_pts which match the properties we care
	// about. At the point this is called, m_cchSrcSearch is valid, but m_pchBuf is not
	// (because not needed for this algorithm).
	bool SearchForProperties()
	{
		// If there are no properties we want to match, searching makes no sense;
		// safest to return a false.
		if ((!m_pat->m_fMatchStyles) && !(m_pat->m_fMatchTags) &&
			!(m_pat->m_fMatchWritingSystem))
			return false;
		if (m_ichLimSearch - m_ichMinSearch <= 0)
			return false;
		SmartBstr sbstrNamedStyle;
		SmartBstr sbstrTags;
		int ws = 0;
		int var;
		TsRunInfo tri;
		ITsTextPropsPtr qttp;
		CheckHr(m_pat->m_qtssPattern->FetchRunInfo(0, &tri, &qttp));
		CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrNamedStyle));
		CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrTags));
		CheckHr(qttp->GetIntPropValues(ktptWs, &var, &ws));
		int ichMinRen;
		int ichLimRen;
		CheckHr(m_pts->SearchToRen(m_ichMinSearch, false, &ichMinRen));
		CheckHr(m_pts->SearchToRen(m_ichLimSearch, true, &ichLimRen));
		if (m_fForward)
		{
			int ichRen = ichMinRen;
			while (ichRen < ichLimRen)
			{
				LgCharRenderProps chrp;
				int ichMinRunRen, ichLimRunRen;
				CheckHr(m_pts->GetCharProps(ichRen, & chrp, & ichMinRunRen, & ichLimRunRen));
				if (MatchProps(chrp, sbstrNamedStyle, sbstrTags, ws, ichRen))
				{
					if (m_ichMinFoundSearch < 0)
					{
						// First matching run, set min
						int ichMinFoundRen = std::max(ichMinRunRen, ichMinRen);
						CheckHr(m_pts->RenToSearch(ichMinFoundRen, &m_ichMinFoundSearch));
					}

					int ichLimFoundRen = std::min(ichLimRunRen, ichLimRen);
					CheckHr(m_pts->RenToSearch(ichLimFoundRen, &m_ichLimFoundSearch));
				}
				else
				{
					if (m_ichMinFoundSearch >= 0)
						break; // found an earlier match, and this run doesn't; stop.
				}

				ichRen = ichLimRunRen; // go to next run.
			}
		}
		else
		{
			int ichRen = ichLimRen;
			// Each iteration:
			//	ichRen is the lim of the range still to test
			while (ichRen > ichMinRen)
			{
				LgCharRenderProps chrp;
				int ichMinRunRen, ichLimRunRen;
				// Subtract 1 to get the last character of the run of which ich is the Lim.
				// (There are no empty runs except in empty strings, and we already tested for
				// an empty text source.)
				// Note that even a one-character run at the start of the search range can be
				// found: on the last iteration, ich is ichMinSearch + 1, and we get the props
				// of the very first character here.
				CheckHr(m_pts->GetCharProps(ichRen - 1, &chrp, &ichMinRunRen, &ichLimRunRen));
				if (MatchProps(chrp, sbstrNamedStyle, sbstrTags, ws, ichRen - 1))
				{
					if (m_ichMinFoundSearch < 0)
					{
						// First matching run, set lim

						int ichLimFoundRen = std::min(ichLimRunRen, ichLimRen);
						CheckHr(m_pts->RenToSearch(ichLimFoundRen, &m_ichLimFoundSearch));
					}

					int ichMinFoundRen = std::max(ichMinRunRen, ichMinRen);
					CheckHr(m_pts->RenToSearch(ichMinFoundRen, &m_ichMinFoundSearch));
				}
				else
				{
					if (m_ichMinFoundSearch >= 0)
						break; // found an earlier match, and this run doesn't; stop.
				}

				ichRen = ichMinRunRen; // go to previous run.
			}
		}
		return m_ichMinFoundSearch >= 0;
	}

	// Initialize whatever variables are required to do the actual searching.
	virtual void InitSearcher() = 0;
	// Run the main search loop over a single text source.
	virtual bool Search() = 0;

	// Run the main body of the algorithm. Return true if a match is made successfully.
	bool Run()
	{
		// Get the characters (paragraph) we have to search.
		// We are getting the whole paragraph to give ICU enough context for whole word matching.
		// Optimize: (a) We could get just the current search range if not matching whole words.
		// (b) may be possible to figure some lesser limit on how many extra chars to get.
		CheckHr(m_pts->get_LengthSearch(&m_cchSrcSearch));
		// Can't match anything in an empty paragraph, and trying generates errors in ICU.
		// However, with regular expressions an empty string can match.
		if (m_cchSrcSearch == 0 && !m_pat->m_fUseRegularExpressions)
			return false;

		m_ichMinSearch = m_fForward ? m_ichStartSearch : m_ichEndSearch;
		m_ichLimSearch = m_fForward ? m_ichEndSearch : m_ichStartSearch;

		Assert(m_ichMinSearch <= m_cchSrcSearch);

		if (!m_pat->m_fCompiled)
			m_pat->Compile();

		if ((!m_pat->m_fUseRegularExpressions) && m_pat->m_stuCompiled.Length() == 0)
			return SearchForProperties();

		m_pchBuf = (OLECHAR *)(_alloca((m_cchSrcSearch + 1) * isizeof(OLECHAR)));
		CheckHr(m_pts->FetchSearch(0, m_cchSrcSearch, m_pchBuf));
		* (m_pchBuf + m_cchSrcSearch) = 0; // null termination required.
		m_ichLimSearch = std::min(m_cchSrcSearch, m_ichLimSearch); // Because of disregarded ORCs, we might get fewer charcaters in the buffer than we asked for.

		InitSearcher();

		return Search();
	}

	// Checks to see if the next char in the searched string is a Diacritic.  If it is a diacritic
	// then this is not a valid match.
	// This should only be called if the compare is using "match diacritics"
	// Return true if it is a valid match.
	bool CheckMatchDiacritic()
	{
		// We can't check the next character if we're at the limit already.
		if (m_ichLimFoundSearch == m_ichLimSearch)
			return true;

		OLECHAR rgchw[2];
		CheckHr(m_pts->FetchSearch(m_ichLimFoundSearch, m_ichLimFoundSearch + 1, &rgchw[0]));
		uint ch32;
		// if chw is the first char of a surrogate pair, and , fetch the next char as well and translate the pair into a UChar32
		// otherwise, copy chw to a UChar32.
		if (U_IS_LEAD(rgchw[0]) && m_ichLimFoundSearch + 1 < m_ichLimSearch)
		{
			CheckHr(m_pts->FetchSearch(m_ichLimFoundSearch + 1, m_ichLimFoundSearch + 2, &rgchw[1]));
			Assert(U_IS_TRAIL(rgchw[1]));
			bool fSurrogateOk = FromSurrogate(rgchw[0], rgchw[1], &ch32);
			Assert(fSurrogateOk);
			if (!fSurrogateOk)
				return false; // Treat as search failure.
		}
		else
		{
			ch32 = rgchw[0];
		}
		if (u_hasBinaryProperty(ch32, UCHAR_DIACRITIC) || u_hasBinaryProperty(ch32, UCHAR_EXTENDER))
			return false;
		return true;
	}

	void CopyStringToBldr(ITsPropsBldr * ptpb, int ich, int tpt)
	{
		SmartBstr sbstr;
		int ichMinBogus, ichLimBogus, ichRen;
		CheckHr(m_pts->SearchToRen(ich, false, &ichRen));
		CheckHr(m_pts->GetCharStringProp(ichRen, tpt, &sbstr,
			&ichMinBogus, &ichLimBogus));
		ptpb->SetStrPropValue(tpt, sbstr);
	}

	// Check a candidate match to make sure writing systems etc. are correct.
	bool CheckMatchProperties()
	{
		if (! m_pat->m_fMatchWritingSystem && ! m_pat->m_fMatchStyles &&
				! m_pat->m_fMatchTags)
		{
			return true;
		}
		return CheckMatchAndProps();
	}

	// Check a candidate match fully. This ensures all characters match and also all relevant
	// properties.
	bool CheckMatchAndProps()
	{
		// Optimize: can do something simpler if pattern and match have only one run each.

		Assert(m_ichMinFoundSearch < m_ichLimFoundSearch);

		// create a TsString that contains the relevant information from the source.
		ITsStrBldrPtr qtsb;
		qtsb.CreateInstance(CLSID_TsStrBldr);

		int cchBldr = 0;

		for (int ich = m_ichMinFoundSearch; ich < m_ichLimFoundSearch; )
		{
			// Figure the relevant properties of the run.
			LgCharRenderProps chrp;
			int ichMinRun, ichLimRun;
			int ichRen, ichRenMin, ichRenLim;
			CheckHr(m_pts->SearchToRen(ich, false, &ichRen));
			CheckHr(m_pts->GetCharProps(ichRen, & chrp, & ichRenMin, & ichRenLim));
			CheckHr(m_pts->RenToSearch(ichRenMin, &ichMinRun));
			CheckHr(m_pts->RenToSearch(ichRenLim, &ichLimRun));
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			if (m_pat->m_fMatchStyles)
			{
				CopyStringToBldr(qtpb, ich, ktptNamedStyle);
			}

			if (m_pat->m_fMatchTags)
			{
				CopyStringToBldr(qtpb, ich, ktptTags);
			}

			// We need a WS for the string we are building even if we aren't matching
			// on Writing Systems, because TSStrings require a WS for every run.
			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, chrp.ws));

			ITsTextPropsPtr qttp;
			CheckHr(qtpb->GetTextProps(&qttp));
			// Append characters from the text source to the builder: the rest
			// of this run or up to the limit, whichever is less.

			int ichLimCopy = std::min(ichLimRun, m_ichLimFoundSearch);

			int cchCopy = ichLimCopy - ich;
			CheckHr(qtsb->ReplaceRgch(cchBldr, cchBldr, m_pchBuf + ich, cchCopy, qttp));
			ich = ichLimCopy;
			cchBldr += cchCopy;
		}
		ITsStringPtr qtssMatch1;
		CheckHr(qtsb->GetString(&qtssMatch1));
		ITsStringPtr qtssMatch2;
		CheckHr(qtssMatch1->get_NormalizedForm(knmNFD, &qtssMatch2));
		ITsStringPtr qtssMatch;
		m_pat->RemoveIgnorableRuns(qtssMatch2, &qtssMatch);
		// OK, we have a normalized form of the matched text, with only the relevant
		// properties set, and ignorable runs removed. Now compare with reduced pattern.

		int crunMatch, crunPattern;
		CheckHr(qtssMatch->get_RunCount(&crunMatch));
		CheckHr(m_pat->m_qtssReducedPattern->get_RunCount(&crunPattern));
		if (crunMatch != crunPattern)
			return false;
		const OLECHAR * pchBufMatch;
		int cchMatch;
		CheckHr(qtssMatch->LockText(&pchBufMatch, &cchMatch));
		const OLECHAR * pchBufPattern;
		int cchPattern;
		CheckHr(m_pat->m_qtssReducedPattern->LockText(&pchBufPattern, &cchPattern));
		bool fMatch = true; // unless we find otherwise
		for (int irun = 0; irun < crunMatch; irun++)
		{
			TsRunInfo triMatch;
			ITsTextPropsPtr qttpMatch;
			CheckHr(qtssMatch->FetchRunInfo(irun, &triMatch, &qttpMatch));
			TsRunInfo triPattern;
			ITsTextPropsPtr qttpPattern;
			CheckHr(m_pat->m_qtssReducedPattern->FetchRunInfo(irun, &triPattern, &qttpPattern));
			if (qttpMatch != qttpPattern)
			{
				fMatch = false; // singleton for a given set of props, should be exact same objects.
				break;
			}
			if (m_pat->m_pcoll->compare(pchBufMatch + triMatch.ichMin, triMatch.ichLim - triMatch.ichMin,
				pchBufPattern + triPattern.ichMin, triPattern.ichLim - triPattern.ichMin) != 0)
			{
				fMatch = false; // singleton for a given set of props, should be exact same objects.
				break;
			}
		}
		CheckHr(qtssMatch->UnlockText(pchBufMatch));
		CheckHr(m_pat->m_qtssReducedPattern->UnlockText(pchBufPattern));

		return fMatch;
	}
};


/*----------------------------------------------------------------------------------------------
	This class represents the algorithm of the FindIn method (without regular expressions).
----------------------------------------------------------------------------------------------*/
class FindInAlgorithm : public FindInAlgorithmBase
{
	StringSearch * m_piter;
	BreakIterator * m_pbi;
public:
	FindInAlgorithm(IVwTextSource * pts, int ichStartLog, int ichEndLog, ComBool fForward,
		IVwSearchKiller * pxserkl, VwPattern * pat)
		: FindInAlgorithmBase(pts, ichStartLog, ichEndLog, fForward, pxserkl, pat)
	{
		m_piter = NULL;
		m_pbi = NULL;
	}

	virtual ~FindInAlgorithm()
	{
		if (m_pbi)
		{
			delete m_pbi;
			m_pbi = NULL;
		}
		if (m_piter)
		{
			delete m_piter;
			m_piter = NULL;
		}
	}

	virtual void InitSearcher()
	{
		if (m_pat->m_fMatchWholeWord)
		{
			// construct a suitable break iterator.
			m_pbi = BreakIterator::createWordInstance(m_pat->m_locale, m_error);
			CheckError();
		}
		// Construct ICU string search iterator in way that takes account of
		// locale/rules, match case, match diacritics, and match whole word.
		if (m_pat->m_stuRules.Length() > 0)
		{
			// Make an iterator based on the rule-based collater in the pattern.
			Assert(m_pat->m_prcoll != NULL);
			m_piter = new StringSearch(m_pat->m_stuCompiled.Chars(), m_pchBuf,
				m_pat->m_prcoll, m_pbi, m_error);
			CheckError();
		}
		else
		{
			// Make a regular iterator.
			m_piter = new StringSearch(m_pat->m_stuCompiled.Chars(), m_pchBuf, m_pat->m_locale,
				m_pbi, m_error);
			CheckError();
			// Rather surprisingly, this works even though we didn't construct it with an RBC.
			// I'd be happier with something more obviously correct, but can't find any other way
			// to specify the strength when initializing with a locale.
			m_piter->getCollator()->setStrength(m_pat->m_strength);
		}
		// Just setting the strength doesn't seem to work. This fixes things, and may help for attribute too.
		m_piter->reset();
		// Makes matches succeed even if pattern and search are differently (or not) normalized.  Note that
		// reset() resets this attribute to false (USEARCH_OFF).
		m_piter->setAttribute(USEARCH_CANONICAL_MATCH, USEARCH_ON, m_error);
		CheckError();
	}

	bool Search()
	{
		if (m_fForward)
		{
			for (m_ichMinFoundSearch = m_piter->first(m_error);
				; // termination checks are inside the loop body
				m_ichMinFoundSearch = m_piter->next(m_error) )
			{
				CheckError(); // see if first() or next() call failed.
				if (m_ichMinFoundSearch == USEARCH_DONE)
					return Fail();
				if (m_ichMinFoundSearch < m_ichMinSearch)
					continue;
				if (m_ichMinFoundSearch >= m_ichLimSearch)
					continue;
				m_ichLimFoundSearch = m_ichMinFoundSearch + m_piter->getMatchedLength();
				AdjustSearchLimitForDiacritics();
				if (m_ichLimFoundSearch > m_ichLimSearch)
					return Fail(); // The first match extends past the end of our range.
				if (m_pat->m_fMatchDiacritics && !CheckMatchDiacritic())
					continue;
				// We have a candidate match. See if it satisfies ws, style, tag requirements if any
				if (CheckAndExtendCandidate())
					return true;
			}
		}
		else
		{
			for (m_ichMinFoundSearch = m_piter->last(m_error);
				; // termination checks are inside the loop body
				m_ichMinFoundSearch = m_piter->previous(m_error) )
			{
				CheckError(); // see if last() or previous() call failed.
				if (m_ichMinFoundSearch == USEARCH_DONE)
					return Fail();
				if (m_ichMinFoundSearch < m_ichMinSearch)
					return Fail(); // The first match extends past the logical end of our range.
				if (m_ichMinFoundSearch >= m_ichLimSearch)
					continue;
				m_ichLimFoundSearch = m_ichMinFoundSearch + m_piter->getMatchedLength();
				AdjustSearchLimitForDiacritics();
				if (m_ichLimFoundSearch > m_ichLimSearch)
					continue;
				if (m_pat->m_fMatchDiacritics && !CheckMatchDiacritic())
					continue;

				// We have a candidate match. See if it satisfies ws, style, tag requirements if any
				if (CheckAndExtendCandidate())
					return true;
			}
		}
		return false; // arbitrary (should never get here).
	}

	/*------------------------------------------------------------------------------------------
		As of ICU 4.0 (or at least after ICU 3.6), the string search matches diacritics past the
		limit even when told not to match diacritics.
	------------------------------------------------------------------------------------------*/
	void AdjustSearchLimitForDiacritics()
	{
		if (m_ichLimFoundSearch <= m_ichLimSearch || m_pat->m_fMatchDiacritics)
			return;
		UnicodeString text(m_pchBuf + m_ichLimSearch, m_ichLimFoundSearch - m_ichLimSearch);
		StringCharacterIterator it(text);
		UChar32 c;
		for (c = it.last(); c != CharacterIterator::DONE; c = it.previous())
		{
			if (!u_hasBinaryProperty(c, UCHAR_DIACRITIC) &&
				!u_hasBinaryProperty(c, UCHAR_EXTENDER))
			{
				break;
			}
			else
			{
				--m_ichLimFoundSearch;
				if (c > 0xFFFF)
					--m_ichLimFoundSearch;	// has to be from surrogate pair.
			}
		}
	}
};

/*----------------------------------------------------------------------------------------------
	This class represents the algorithm of the FindIn method (with regular expressions).
----------------------------------------------------------------------------------------------*/
class RegExFindInAlgorithm : public FindInAlgorithmBase
{
	// ICU regular expression matcher, pointer copied from pattern, don't delete
	RegexMatcher * m_pmatcher;
	UnicodeString * m_pusSource;
public:
	RegExFindInAlgorithm(IVwTextSource * pts, int ichStartLog, int ichEndLog, ComBool fForward,
		IVwSearchKiller * pxserkl, VwPattern * pat)
		: FindInAlgorithmBase(pts, ichStartLog, ichEndLog, fForward, pxserkl, pat)
	{
		m_pusSource = NULL;
	}

	virtual ~RegExFindInAlgorithm()
	{
		// Do NOT delete m_pmatcher.
		if (m_pusSource)
		{
			delete(m_pusSource);
			m_pusSource = NULL;
		}
	}

protected:
	virtual void InitSearcher()
	{
		m_pmatcher = m_pat->m_pmatcher;
		if (m_pusSource)
		{
			delete(m_pusSource);
			m_pusSource = NULL;
		}
		m_pusSource = new UnicodeString(m_pchBuf, m_cchSrcSearch); // sigh, yet another copy!
		m_pmatcher->reset(*m_pusSource);
	}

	bool Search()
	{
		bool fMatch = true; // did we get a match this iteration?
		if (m_fForward)
		{
			for (fMatch = m_pmatcher->find(m_ichMinSearch, m_error); true; fMatch = m_pmatcher->find() )
			{
				if (!fMatch)
					return Fail(); // no more matches.
				m_ichMinFoundSearch = m_pmatcher->start(m_error);
				m_ichLimFoundSearch = m_pmatcher->end(m_error);
				CheckError(); // see if find() or start() or end() call failed.
				if (m_ichLimFoundSearch > m_ichLimSearch)
					return Fail(); // The current match extends past the end of our range.
				// Doesn't seem to be any reasonable way to do this
				//if (m_pat->m_fMatchDiacritics)
				//	if(!CheckMatchDiacritic())
				//		return Fail();
				// We have a candidate match. See if it satisfies ws, style, tag requirements if any
				// I don't think we can usefully do this either.
				//if (CheckAndExtendCandidate())
				//	return true;
				return true; // got a useful match.
			}
		}
		else
		{
			// Simulate searching backwards by searching forwards and keeping the last match in range.
			bool fGotPrevMatch = false; // true if we found previous good match.
			for (fMatch = m_pmatcher->find(m_ichMinSearch, m_error); true; fMatch = m_pmatcher->find() )
			{
				int ichMin = 0;
				int ichLim = 0;
				if (fMatch)
				{
					ichMin = m_pmatcher->start(m_error);
					ichLim = m_pmatcher->end(m_error);
					if (ichMin < m_ichMinSearch || ichLim > m_ichLimSearch)
						fMatch = false;
				}
				CheckError(); // see if find() or start() or end() call failed.
				if (fMatch)
				{
					// this match is good...we will use it if we don't find another before failing to match.
					// This means we will certainly succeed with some match, so we can set the actual result
					// variables. They will get overwritten by subsequent successful matches until the last.
					fGotPrevMatch = true;
					m_ichMinFoundSearch = ichMin;
					m_ichLimFoundSearch = ichLim;
				}
				else
				{
					if (fGotPrevMatch)
						return true;
					else
						return Fail(); // no matches at all.
				}
			}
		}
		return false; // arbitrary (should never get here).
	}
};

/*----------------------------------------------------------------------------------------------
	This allows patterns to be used in searching stuff other than views.
	The text to be searched must be presented as an IVwTextSource.
	The search begins at ichStart in the text source, and proceeds in the
	specified direction. *pichMinFound is set to -1 if there is no match,
	otherwise it points to the start of the match. *pichLimFound indicates the
	other end of the range.

	If searching forward, *pichMinFound must be at least ichStart; if searching
	backwards, *pichLimFound must be at most ichStart.

	If searching forward, *pichMinFound must be at most ichEnd; if searching
	backwards, *pichLimFound must be at least ichEnd.

	Note that this routine does NOT, under any circumstances, set StoppedAtLimit true.

	All character indices inputs/outputs are logical!
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::FindIn(IVwTextSource * pts, int ichStartLog, int ichEndLog, ComBool fForward,
	int * pichMinFoundLog, int * pichLimFoundLog, IVwSearchKiller * pxserkl)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pts);
	ChkComOutPtr(pichMinFoundLog);
	ChkComOutPtr(pichLimFoundLog);
	ChkComArgPtrN(pxserkl);

	// Default is we didn't find it. Set these in case of exceptions.
	int ichMinFoundSearch, ichLimFoundSearch;
	ichMinFoundSearch = *pichMinFoundLog = -1;
	ichLimFoundSearch = *pichLimFoundLog = -1;
	if (m_fUseRegularExpressions)
	{
		RegExFindInAlgorithm refia(pts, ichStartLog, ichEndLog, fForward, pxserkl, this);
		if (refia.Run())
		{
			ichMinFoundSearch = refia.m_ichMinFoundSearch;
			ichLimFoundSearch = refia.m_ichLimFoundSearch;
		}
	}
	else
	{
		FindInAlgorithm fia(pts, ichStartLog, ichEndLog, fForward, pxserkl, this);
		if (fia.Run())
		{
			ichMinFoundSearch = fia.m_ichMinFoundSearch;
			ichLimFoundSearch = fia.m_ichLimFoundSearch;
		}
	}

	if (ichMinFoundSearch >= 0)
	{
		// We got a match, make sure we ask the text source for the correct logical location
		CheckHr(pts->SearchToLog(ichMinFoundSearch, false, pichMinFoundLog));
		CheckHr(pts->SearchToLog(ichLimFoundSearch, true, pichLimFoundLog));

		if (*pichMinFoundLog == *pichLimFoundLog)
		{
			// Zero length match at beginning is okay for regular expression "^".  See LT-6707.
			if (*pichMinFoundLog > 0 || !m_fUseRegularExpressions || !m_stuCompiled.Equals(L"^"))
			{
				// The only way this can happen is a match inside the text of a hot link.
				// Select the whole link. (Other solutions are possible, but beware of the
				// previous behavior of leaving them the same: this produces an IP at the
				// start of the hot link, and then every subsequent search finds it again.)
				(*pichLimFoundLog)++;
				// not so not so....
				// a search like \n* or \s* or * also comes through here... and crashes
				// with out the following check.
				if (*pichLimFoundLog > ichEndLog)
					*pichLimFoundLog = ichEndLog;	// cant be larger than lim
			}
		}
	}

	m_ichMinFoundLog = *pichMinFoundLog;
	m_ichLimFoundLog = *pichLimFoundLog;
	m_qtsWhereFound = pts;

	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Install the current Find result as the active selection.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::Install()
{
	BEGIN_COM_METHOD;
	if (!m_fFound)
		ThrowHr(WarnHr(E_UNEXPECTED)); // Trying to install when no successful match.
	if (m_qselFound)
	{
		CheckHr(m_qselFound->Install());
	}
	else
	{
		ThrowHr(WarnHr(E_NOTIMPL));
	}
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Another way to find out whether a match was found. Returns false when the object
	is first created and until a search succeeds, then true until an unsuccessful
	search occurs.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_Found(ComBool * pfFound)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfFound);
	*pfFound = m_fFound;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Create a selection equivalent to the position found, and (optionally) install it
	@error E_UNEXPECTED if the Found() returns false.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::GetSelection(ComBool fInstall, IVwSelection ** ppsel)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppsel);
	if (!m_fFound)
		ThrowHr(WarnHr(E_UNEXPECTED)); //, "Asked for info about failed pattern match"));
	if (m_qselFound)
	{
		*ppsel = m_qselFound;
		AddRefObj(*ppsel);
		if (fInstall)
			CheckHr(Install());
	}
	else
	{
		// EHANCE JohnT: when we support searches in lazy boxes, implementing this when m_qselFound
		// is null requires expanding the lazy box.
		Assert(false);
		return E_NOTIMPL;
	}
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Indicate how many levels of nesting of (object, property) contain the target,
	including the basic property that terminates the chain.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::CLevels(int * pclev)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pclev);

	if (!m_fFound)
		ThrowHr(WarnHr(E_UNEXPECTED)); //, "Asked for info about failed pattern match"));

	if (m_qselFound)
	{
		// Found it in a real paragraph
		CheckHr(m_qselFound->CLevels(false, pclev));
	}
	else
	{
		*pclev = m_vvsli.Size();
	}
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get the same info as would be obtained by calling GetSelection, then
	sending this same message to it. This may be considerably more efficient, though.

	(Note that the last 3 arguments of the VwSelection version are omitted, as not relevant.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::AllTextSelInfo(int * pihvoRoot, int cvlsi, VwSelLevInfo * prgvsli,
	PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd, int * pws)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pihvoRoot);
	ChkComArrayArg(prgvsli, cvlsi);
	ChkComArgPtr(ptagTextProp);
	ChkComArgPtr(pcpropPrevious);
	ChkComArgPtr(pichAnchor);
	ChkComArgPtr(pichEnd);
	ChkComArgPtr(pws);

	if (!m_fFound)
		ThrowHr(WarnHr(E_UNEXPECTED)); //, "Asked for info about failed pattern match"));

	if (m_qselFound)
	{
		// Found it in a real paragraph
		// The corresponsing selection method returns some more stuff we don't want.
		ComBool fAssocPrev;
		int ihvoEnd;
		ITsTextPropsPtr qttp;

		CheckHr(m_qselFound->AllTextSelInfo(pihvoRoot, cvlsi, prgvsli, ptagTextProp, pcpropPrevious,
			pichAnchor, pichEnd, pws, &fAssocPrev, &ihvoEnd, &qttp));
	}
	else
	{
#if 0
		// Not sure what this was supposed to do, but the old m_ichMin and m_ichLim variables
		// were never set to any value, so apparently it was never properly implemented.
		if (cvlsi < m_vvsli.Size())
			ThrowHr(WarnHr(E_FAIL)); //, "Buffer too small");
		*pihvoRoot = m_ihvoRoot;
		CopyItems(m_vvsli.Begin(), prgvsli, m_vvsli.Size());
		*ptagTextProp = m_tagTextProp;
		*pcpropPrevious = m_cpropPrevious;
		*pichAnchor = m_ichMin;
		*pichEnd = m_ichLim;
		*pws = m_wsMla;
#else
		ThrowHr(WarnHr(E_NOTIMPL));
#endif
	}

	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Return true if the (whole of the) selection matches the pattern.
	The main use of this is to determine whether we can perform a replace of the current
	selection. Therefore it immediately returns false if the input is empty (insertion point
	can't be replaced).
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::MatchWhole(IVwSelection * psel, ComBool * pfMatch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(psel);
	ChkComOutPtr(pfMatch);
	*pfMatch = false;
	if (!m_fCompiled)
		Compile();
	// It better be our implementation!
	VwTextSelectionPtr qselQuery;
	CheckHr(psel->QueryInterface(CLSID_VwTextSelection, (void **)(&qselQuery)));
	// Currently we can't have a match in of a multi-paragraph selection.
	if (qselQuery->EndBox())
		return false;
	if (qselQuery->AnchorOffset() == qselQuery->EndOffset())
		return false; // IP is not a replaceable selection.
	qselQuery->AnchorBox()->MakeSourceNfd();
	int ichMinTestLog = std::min(qselQuery->AnchorOffset(), qselQuery->EndOffset());
	int ichLimTestLog = std::max(qselQuery->AnchorOffset(), qselQuery->EndOffset());
	int ichMinFoundLog, ichLimFoundLog;

	CheckHr(FindIn(qselQuery->AnchorBox()->Source(),
		ichMinTestLog,
		ichLimTestLog,
		true, // forward -> start is min
		&ichMinFoundLog,
		&ichLimFoundLog,
		NULL));
	// It's a match if we found exactly the whole input.
	*pfMatch = (ichMinFoundLog == ichMinTestLog && ichLimFoundLog == ichLimTestLog);

	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get the ICU Locale to be used in comparing strings and determining word breaks.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_IcuLocale(BSTR * pbstrLocale)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pbstrLocale);
	*pbstrLocale = SysAllocString(m_stuLocale.Chars());
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set the ICU Locale to be used in comparing strings and determining word breaks.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_IcuLocale(BSTR bstrLocale)
{
	BEGIN_COM_METHOD;
	m_stuLocale = bstrLocale;
	// The only currently available way to make an actual locale requires char arguments.
	StrAnsi staLocale = bstrLocale;
	m_locale = Locale::createFromName(staLocale.Chars());
	m_fCompiled = false;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get the collating rules (as used in creating an ICU RuleBasedCollator) to use
	in comparing strings. If empty, the default collater for the ICU Locale is used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_IcuCollatingRules(BSTR * pbstrRules)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pbstrRules);
	*pbstrRules = SysAllocString(m_stuRules.Chars());
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set the collating rules (as used in creating an ICU RuleBasedCollator) to use
	in comparing strings. If empty, the default collater for the ICU Locale is used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_IcuCollatingRules(BSTR bstrRules)
{
	BEGIN_COM_METHOD;
	ChkComBstrArg(bstrRules);
	m_stuRules = bstrRules;
	m_fCompiled = false;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Remove from the argument any runs that are 'ignorable' in the sense that the collater
	considers them equal to an empty string.
----------------------------------------------------------------------------------------------*/
void VwPattern::RemoveIgnorableRuns(ITsString * ptssIn, ITsString ** pptssOut)
{
	ITsStrBldrPtr qtsb;
	int cchBldr = 0;
	qtsb.CreateInstance(CLSID_TsStrBldr);
	int crun;
	CheckHr(ptssIn->get_RunCount(&crun));
	SmartBstr sbstrT;
	const OLECHAR * pchBuf;
	int cch;
	CheckHr(ptssIn->LockText(&pchBuf, &cch));
	for (int irun = 0; irun < crun; irun++)
	{
		TsRunInfo tri;
		ITsTextPropsPtr qttp;
		CheckHr(ptssIn->FetchRunInfo(irun, &tri, &qttp));
		int cchRun = tri.ichLim - tri.ichMin;
		OLECHAR dummy;
		// If this is an empty TSS, we also want to copy appropriate run props (at least the required WS)
		if (m_pcoll->compare(&dummy, 0, pchBuf + tri.ichMin, cchRun) != 0 || !cch)
		{
			// Not ignorable. Figure the props we care about.
			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			if (m_fMatchStyles)
			{
				CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrT));
				// Don't copy the default char style - should only occur in a pattern.
				if (sbstrT != m_sbstrDefaultCharStyle)
					CheckHr(qtpb->SetStrPropValue(ktptNamedStyle, sbstrT));
			}

			if (m_fMatchTags)
			{
				CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrT));
				CheckHr(qtpb->SetStrPropValue(ktptTags, sbstrT));
			}

			// We need a WS for the string we are building even if we aren't matching
			// on Writing Systems, because TSStrings require a WS for every run.
			int ws, var;
			CheckHr(qttp->GetIntPropValues(ktptWs, &var, &ws));
			CheckHr(qtpb->SetIntPropValues(ktptWs, var, ws));

			ITsTextPropsPtr qttpReduced;
			CheckHr(qtpb->GetTextProps(&qttpReduced));
			CheckHr(qtsb->ReplaceRgch(cchBldr, cchBldr, pchBuf + tri.ichMin,
				cchRun, qttpReduced));
			cchBldr += cchRun;
		}
	}
	CheckHr(ptssIn->UnlockText(pchBuf));
	if (!cchBldr)
	{
		// If we didn't have any non-ignorable characters, then make sure we at least have a
		// writing system in the string (FWR-2176)
		ITsTextPropsPtr qttp;
		int var;
		int ws;
		CheckHr(ptssIn->get_Properties(0, &qttp));
		CheckHr(qttp->GetIntPropValues(ktptWs, &var, &ws));
		CheckHr(qtsb->SetIntPropValues(0, 0, ktptWs, var, ws));
	}
	ITsStringPtr qtssT;
	CheckHr(qtsb->GetString(&qtssT));
	CheckHr(qtssT->get_NormalizedForm(knmNFD, pptssOut));
}

/*----------------------------------------------------------------------------------------------
	Compile the pattern into a form that supports more efficient searching.
	Currently this is just a convenient place to work out whether we care about styles and tags
----------------------------------------------------------------------------------------------*/
void VwPattern::Compile()
{
	CleanupRegexPattern();
	m_stuErrorMessage.Clear();
	SmartBstr sbstrPattern;
	UErrorCode error = U_ZERO_ERROR;
	if (m_qtssPattern)
	{
		ITsStringPtr qtssT;
		CheckHr(m_qtssPattern->get_NormalizedForm(knmNFD, &qtssT));
		m_qtssPattern = qtssT; // replace with normalized form of itself.
		CheckHr(m_qtssPattern->get_Text(&sbstrPattern));
	}
	if (m_fMatchWholeWord && !m_fMatchDiacritics)
	{
		StrUni stuPattern;
		// We need to remove any trailing diacritic characters from the pattern.  See TE-6907.
		// The ICU code hangs with trailing diacritics when not matching diacritics, but
		// matching whole words.
		stuPattern.Assign(sbstrPattern.Chars());
		int cch = stuPattern.Length();
		for (int ich = cch - 1; ich > 0; --ich)	// leave at least one char in pattern
		{
			uint ch32;
			wchar ch = stuPattern.GetAt(ich);
			if (U_IS_TRAIL(ch))
			{
				continue;
			}
			else if (U_IS_LEAD(ch) && (ich + 1) < stuPattern.Length())
			{
				wchar ch2 = stuPattern.GetAt(ich + 1);
				Assert(U_IS_TRAIL(ch2));
				bool fSurrogateOk = FromSurrogate(ch, ch2, &ch32);
				Assert(fSurrogateOk);
				if (!fSurrogateOk)
					break;
			}
			else
			{
				ch32 = (uint)ch;
			}
			if (u_hasBinaryProperty(ch32, UCHAR_DIACRITIC) ||
				u_hasBinaryProperty(ch32, UCHAR_EXTENDER))
			{
				cch = ich;
			}
			else
			{
				break;
			}
		}
		if (cch < stuPattern.Length())
			sbstrPattern.Assign(sbstrPattern.Chars(), cch);
	}
	if (m_fUseRegularExpressions)
	{
		uint32_t flags = 0;
		if (!m_fMatchCase)
			flags |= UREGEX_CASE_INSENSITIVE;
		m_pmatcher = new RegexMatcher(sbstrPattern.Chars(), flags, error);
		// Enhance JohnT: this will crash the program when the user enters a bad pattern.
		// That is not acceptable.
		if (U_FAILURE(error))
		{
			if (m_pmatcher)
			{
				delete m_pmatcher;
				m_pmatcher = NULL;
			}
			m_stuErrorMessage.Assign(u_errorName(error));
		}
		else
		{
			// We need this to check for zero-length matching pattern (ie, "^").  See LT-6707.
			m_stuCompiled.Assign(sbstrPattern.Chars(), sbstrPattern.Length());
		}
	}
	else
	{
		m_fMatchStyles = false;
		m_fMatchTags = false;

		m_strength = Collator::PRIMARY; // ignore case and diacritics
		if (m_fMatchCase)
			m_strength = Collator::TERTIARY; // case and diacritics both matched exactly.
		else if (m_fMatchDiacritics)
			m_strength = Collator::SECONDARY; // ignore case, match diacritics.

		if (m_pcoll)
		{
			delete m_pcoll;
			m_pcoll = NULL;
			m_prcoll = NULL;
		}
		if (m_stuRules.Length() > 0)
		{
			// Make a rule-based collater and an iterator based on it.
			m_pcoll = m_prcoll = new RuleBasedCollator(m_stuRules.Chars(), m_strength, error);
			if (U_FAILURE(error))
				ThrowHr(E_FAIL);
		}
		else
		{
			// Make a regular collater.
			m_pcoll = Collator::createInstance(m_locale, error);
			// Leave m_prcoll NULL.
			if (U_FAILURE(error))
				ThrowHr(E_FAIL);
			m_pcoll->setStrength(m_strength);
		}

		if (m_qtssPattern)
		{
			//--Convert(sbstrPattern);
			m_stuCompiled.Assign(sbstrPattern.Chars(), sbstrPattern.Length());
			// Indicate that we want to match styles or overlay tags if there is any
			// character style or tag in the pattern that is non-empty.
			// ENHANCE (SharonC): Possibly rework when we have real pattern-matching.
			int crun;
			CheckHr(m_qtssPattern->get_RunCount(&crun));
			for (int irun = 0; irun < crun; irun++)
			{
				TsRunInfo tri;
				ITsTextPropsPtr qttp;
				SmartBstr sbstrName;
				CheckHr(m_qtssPattern->FetchRunInfo(irun, &tri, &qttp));
				CheckHr(qttp->GetStrPropValue(ktptNamedStyle, &sbstrName));
				if (sbstrName.Length())
					m_fMatchStyles = true;

				SmartBstr sbstrTags;
				CheckHr(qttp->GetStrPropValue(ktptTags, &sbstrTags));
				if (sbstrTags.Length())
					m_fMatchTags = true;
			}
			RemoveIgnorableRuns(m_qtssPattern, &m_qtssReducedPattern); // after deducing m_fMatchStyles etc.
			// First run is used to determine writing system to use in searching (for now).
			// JohnT: this doesn't appear to accomplish anything.
			//TsRunInfo triFirst;
			//ITsTextPropsPtr qttpFirst;
			//CheckHr(m_qtssPattern->FetchRunInfo(0, &triFirst, &qttpFirst));
			//int ws, dummy;
			//CheckHr(qttpFirst->GetIntPropValues(ktptWs, &dummy, &ws));
		}
	}
	m_fCompiled = true;
}

/*----------------------------------------------------------------------------------------------
	Adapt the input string with the conversions we are currently using.
----------------------------------------------------------------------------------------------*/
//--void VwPattern::Convert(SmartBstr & sbstrPattern)
//{
//	if ((!m_fMatchCase) || (!m_fMatchDiacritics) || (!m_fMatchCompatibility))
//	{
//		ILgCharacterPropertyEnginePtr qcpe;
//		qcpe.CreateInstance(CLSID_LgIcuCharPropEngine);
//		SmartBstr sbstrConv;
//		if (!m_fMatchCompatibility)
//		{
//			CheckHr(qcpe->NormalizeKd(sbstrPattern, &sbstrConv));
//			sbstrPattern.Attach(sbstrConv.Detach());
//		}
//		if (!m_fMatchCase)
//		{
//			CheckHr(qcpe->ToUpper(sbstrPattern, &sbstrConv));
//			sbstrPattern.Attach(sbstrConv.Detach());
//		}
//		if (!m_fMatchDiacritics)
//		{
//			CheckHr(qcpe->StripDiacritics(sbstrPattern, &sbstrConv));
//			sbstrPattern.Attach(sbstrConv.Detach());
//		}
//	}
//}

/*----------------------------------------------------------------------------------------------
	Retrieve the direction of the last search. False if there has not been one.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_LastDirection(ComBool * pfForward)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfForward);
	*pfForward = m_fForward;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Set the replace with string. Currently this is just a sequence of chars
	to match. Later we may add options for regular expression patterns.
	This is currently unused by the Pattern code, it is just convenient to allow
	clients to store this here.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::putref_ReplaceWith(ITsString * ptssPattern)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(ptssPattern);
	m_qtssReplaceWith = ptssPattern;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get the replace with string.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_ReplaceWith(ITsString ** pptssPattern)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pptssPattern);
	*pptssPattern = m_qtssReplaceWith;
	AddRefObj(*pptssPattern);
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


/*----------------------------------------------------------------------------------------------
	Set a flag indicating whether the "More"
	controls in the dialog should be enabled. Not used internally.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::put_ShowMore(ComBool fMore)
{
	BEGIN_COM_METHOD;
	m_fShowMore = (bool)fMore;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Get a flag indicating whether the "More"
	controls in the dialog should be enabled. Not used internally.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwPattern::get_ShowMore(ComBool * pfMore)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pfMore);
	*pfMore = m_fShowMore;
	END_COM_METHOD(g_fact, IID_IVwPattern);
}


// Explicit instantiation
#include "Vector_i.cpp"
template class Vector<VwSelLevInfo>; // VecSelLevInfo; // Hungarian vvsli





//:>********************************************************************************************
//:>	VwSearchKiller Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
VwSearchKiller::VwSearchKiller()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_fAbort = false;
	m_hwnd = NULL;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
VwSearchKiller::~VwSearchKiller()
{
	ModuleEntry::ModuleRelease();
}

//:>--------------------------------------------------------------------------------------------
//:>	IUnknown Methods
//:>--------------------------------------------------------------------------------------------
STDMETHODIMP VwSearchKiller::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(this);
	else if (riid == IID_IVwSearchKiller)
		*ppv = static_cast<IVwSearchKiller *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo(this, IID_IVwSearchKiller);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>--------------------------------------------------------------------------------------------
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>--------------------------------------------------------------------------------------------
static GenericFactory g_factSearchKiller(
	_T("SIL.Views.VwSearchKiller"),
	&CLSID_VwSearchKiller,
	_T("SIL Search Killer"),
	_T("Apartment"),
	&VwSearchKiller::CreateCom);


void VwSearchKiller::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<VwSearchKiller> qzserkl;
	qzserkl.Attach(NewObj VwSearchKiller());		// ref count initialy 1
	CheckHr(qzserkl->QueryInterface(riid, ppv));
}

//:>--------------------------------------------------------------------------------------------
//:>	IVwSearchKiller Methods
//:>--------------------------------------------------------------------------------------------

/*----------------------------------------------------------------------------------------------
	Set the window whose messages are to be flushed.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSearchKiller::put_Window(int hwnd)
{
	BEGIN_COM_METHOD;

#if WIN32
	m_hwnd = (HWND)hwnd;
#else
	// TODO-Linux: Handle this if neccessary - problem on 64bit
	Assert(!"VwSearchKiller::put_Window shouldn't be called on Linux");
#endif

	END_COM_METHOD(g_fact, IID_IVwSearchKiller);
}


/*----------------------------------------------------------------------------------------------
	Process the messages in the queue, leaving it empty.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSearchKiller::FlushMessages()
{
	BEGIN_COM_METHOD;

#if WIN32
	if (m_hwnd)
	{
		MSG msg;
		while (::PeekMessage(&msg, m_hwnd, 0, 0, PM_REMOVE))
		{
			::TranslateMessage(&msg);
			::DispatchMessage(&msg);
		}
	}
#else
	// TODO-Linux: Handle this if neccessary now we use mono swf - using X11
	printf("Warning using unimplemented method VwSearchKiller::FlushMessages()\n");
	fflush(stdout);
#endif

	END_COM_METHOD(g_fact, IID_IVwSearchKiller);
}


/*----------------------------------------------------------------------------------------------
	Used to request the termination of a search operation.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSearchKiller::put_AbortRequest(ComBool fAbort)
{
	BEGIN_COM_METHOD;

	m_fAbort = (bool)fAbort;

	END_COM_METHOD(g_fact, IID_IVwSearchKiller);
}


/*----------------------------------------------------------------------------------------------
	Get the abort flag.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP VwSearchKiller::get_AbortRequest(ComBool * pfAbort)
{
	BEGIN_COM_METHOD;

	ChkComOutPtr(pfAbort);
	*pfAbort = ComBool(m_fAbort);

	END_COM_METHOD(g_fact, IID_IVwPattern);
}

/*----------------------------------------------------------------------------------------------
	Cleanup the associated regular expression matcher, if any.
----------------------------------------------------------------------------------------------*/
void VwPattern::CleanupRegexPattern()
{
	if (m_pmatcher != NULL)
	{
		delete m_pmatcher;
		m_pmatcher = NULL;
	}
	if (m_pcoll != NULL)
	{
		delete m_pcoll;
		m_pcoll = NULL;
		m_prcoll = NULL;
	}
	m_fCompiled = false; // we will need to recompile to get a new pattern.
}
