/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgUnicodeCollater.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:  	This file contains the implementation of the Unicode TR10 Collating Engine.  It
				contains methods to produce sort keys and do direct string comparisons of these
				keys.  As of now, this engine generates sort keys with full decompositions and
				takes into consideration ignorable characters as flagged by fVariant.  It also
				establishes collating elements for expansions in default table.
				ENHANCE: This engine does not implement reordering, contractions or direction.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE
#include "LIMITS.h"//included for INT_MAX
//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************
#pragma code_seg("datacode") // makes it a code segment
#pragma code_seg()
#include "LgUnicodeCollateInit.h" // initializers for standard Unicode collating sequence

// These static member variables are initialized to point at statically allocated arrays,
// which are declared in the generated file LgUnicodeCollateInit.h.
const CollatingElement * LgUnicodeCollater::g_prgcolel = g_rgcolel;
const CollatingElement * LgUnicodeCollater::g_prgcolelMultiple = g_rgcolelMultiple;
const short * LgUnicodeCollater::g_prgicolelPage = g_rgicolelPage;
const byte * LgUnicodeCollater::g_prgccolelPage = g_rgccolelPage;

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

LgUnicodeCollater::LgUnicodeCollater() {
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

LgUnicodeCollater::~LgUnicodeCollater() {
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language.LgUnicodeCollater"),
	&CLSID_LgUnicodeCollater,
	_T("SIL Unicode collater"),
	_T("Apartment"),
	&LgUnicodeCollater::CreateCom);


void LgUnicodeCollater::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));
	ComSmartPtr<LgUnicodeCollater> qunicoll;
	qunicoll.Attach(NewObj LgUnicodeCollater());		// ref count initialy 1
	CheckHr(qunicoll->QueryInterface(riid, ppv));
}



//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgUnicodeCollater::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgCollatingEngine *>(this));
	else if (riid == IID_ILgCollatingEngine)
		*ppv = static_cast<ILgCollatingEngine *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<ILgCollatingEngine *>(this),
			IID_ISimpleInit, IID_ILgCollatingEngine);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	AddRef();
	return NOERROR;
}


//:>********************************************************************************************
//:>	   ISimpleInit Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize an instance from a ClassInitMoniker

	To create a suitable moniker, do something like this:

	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	hr = qcim->InitNew(CLSID_LgUnicodeCollater, NULL, 0);

	Note that this version takes no data because it does not require initialization; it uses
	standard Unicode properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::InitNew(const BYTE * prgb, int cb)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgb, cb);

	if (cb != 0)
		ThrowHr(WarnHr(E_INVALIDARG));

	// Nothing to do at present.
	return S_OK;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

/*----------------------------------------------------------------------------------------------
	Return the initialization value previously set by InitNew.

	@param pbstr Pointer to a BSTR for returning the initialization data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::get_InitializationData(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pbstr);

	*pbstr = NULL;
	return S_OK;

	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


//:>********************************************************************************************
//:>	   ILgCollatingEngine Methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Generate a sort key.
	If output pointer is null, just gives the needed length in *pcchKey.
	prgchSource: string sort key is to be generated for.
	cchSource: number of characters in prgchSource.
	cchMaxKey: space available in prgchKey.
	prgchKey: variable that will contain sort key.
	pcchKey: Number of characters in prgchKey.
	This method generates a sort key with full decompositions, it takes into consideration
	ignorable characters (fVariant).  It also establishes collating elements for expansions.
	ENHANCE: As of now, this method does not support contractions,  reordering
		  or direction.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::SortKeyRgch(const OLECHAR * prgchSource, int cchSource,
			LgCollatingOptions colopt, int cchMaxKey, OLECHAR * prgchKey, int * pcchKey)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchSource, cchSource);
	ChkComArrayArg(prgchKey, cchMaxKey);
	if ((uint) colopt > (uint)fcoLim)
		ThrowInternalError(E_INVALIDARG, "Invalid collating options");
	ChkComArgPtr(pcchKey);

	// Create a pointer to LgCharacterPropertyEngine to access get_FullDecomp
	// ENHANCE JohnT: Should this be the default character property engine or a ws-specific one?
	if (!m_qcpe) //m_qcpe is initialized to null in constructor.
	{
		if (m_qwsf)
			CheckHr(m_qwsf->get_UnicodeCharProps(&m_qcpe));
		else
			CheckHr(LgWritingSystemFactory::GetUnicodeCharProps(&m_qcpe));
	}
	const OLECHAR * pchSource;	// points to next char to process, in prgchSource
	OLECHAR * pch;			// points to next char to process, in rgchDecomp
	OLECHAR * pchLim;
	#define MAXDECOMP 256
	OLECHAR rgchDecomp[MAXDECOMP];	// holds decompositions
	int cchDecomp;			// character count in decomposition

	OLECHAR * pchKey = prgchKey;

	const OLECHAR * pchLimSource = prgchSource + cchSource;
	CollatingElement * pcolel; // pointer to next collating element to do
	CollatingElement * pcolelLim; //will be assigned limit address in "multiple" loop
	int cchOut = 0;				// count of characters we have output
	bool fEven = true;      // true for first char of pair at levels 2 and 3

	int cchMaxOut = cchMaxKey;
	if (!cchMaxKey)
	{
		// Set a large limit so we don't report errors below, and ensure we don't output
		// anything, even if we were given a pointer to a real buffer
		cchMaxOut = INT_MAX;
		pchKey = NULL;
	}

	//Enter weight 1's into sort key
	for (pchSource = prgchSource; pchSource < pchLimSource; pchSource++)
	{
		ComBool fHasDecomp;
		// If there is no decomposition, this routine copies the one character we passed into
		// rgchDecomp.  Otherwise, it puts the decomposition there.
		CheckHr(m_qcpe->FullDecompRgch(*pchSource, MAXDECOMP, rgchDecomp, &cchDecomp,
						&fHasDecomp));
		pchLim = rgchDecomp + cchDecomp;
		for (pch = rgchDecomp; pch < pchLim; pch++)
		{
			int icolel = FindColel(*pch);
			if(icolel == -1)//an index of -1 means the weights are the standard (U, 0x20, 2)
			{
				cchOut++;
				if(cchOut > cchMaxOut)
					return E_FAIL;
				if(pchKey)
					*pchKey++ = *pch;
				continue;
			}
			pcolel = const_cast <CollatingElement *> (g_prgcolel + icolel);
			if((colopt & fcoDontIgnoreVariant) == 0)
			{
				if(pcolel->Variant())
					continue;
			}
			int ccolel = 1; // by default we have just one, the one we point at now
			if (pcolel->Multiple())
			{
				// there are several to process
				ccolel = pcolel->uWeight3; // before we change pcolel!
				// move pcolel to point at the list in the multiple array
				pcolel = const_cast<CollatingElement *>(g_prgcolelMultiple + pcolel->MultipleIndex());
			}
			pcolelLim = pcolel + ccolel;
			for(;pcolel < pcolelLim; pcolel++)
			{
				int nWeight;
				nWeight = pcolel->uWeight1;
				if (nWeight)
				{
					cchOut++;
					if (cchOut > cchMaxOut)
						return E_FAIL;
					if (pchKey)
						*pchKey++ = (OLECHAR) nWeight;
				}
			}
		}
	}

	//enter level separator into array
	if(pchKey)
	{
		*pchKey++ = 0x0001;
	}
	cchOut++;
	if(cchOut > cchMaxOut)
		return E_FAIL;

	//Packing weight 2's two per character.  If there is an odd number of weight 2's, the LSB of the
	//last character is padded with zero.
	for (pchSource = prgchSource; pchSource < pchLimSource; pchSource++)
	{
		ComBool fHasDecomp;
		// If there is no decomposition, this routine copies the one character we passed into
		// rgchDecomp.  Otherwise, it puts the decomposition there.
		CheckHr(m_qcpe->FullDecompRgch(*pchSource, MAXDECOMP, rgchDecomp, &cchDecomp,
												&fHasDecomp));
		pchLim = rgchDecomp + cchDecomp;
		for (pch = rgchDecomp; pch < pchLim; pch++)
		{
			int icolel = FindColel(*pch);
			if(icolel == -1)
			{
				if (!PackWeights(pchKey, cchOut, cchMaxOut, 0x20, fEven))
					return E_FAIL;
				continue;
			}
			pcolel = const_cast<CollatingElement *>(g_prgcolel + icolel);
			if((colopt & fcoDontIgnoreVariant) == 0)
			{
				if(pcolel->Variant())
					continue;
			}

			int ccolel = 1; // by default we have just one, the one we point at now
			if (pcolel->Multiple())
			{
				// there are several to process
				ccolel = pcolel->uWeight3; // before we change pcolel!
				// move pcolel to point at the list in the multiple array
				pcolel =const_cast<CollatingElement *>(g_prgcolelMultiple + pcolel->MultipleIndex());
			}
			pcolelLim = pcolel + ccolel;
			for(;pcolel < pcolelLim; pcolel++)
			{
				int nWeight;
				nWeight = pcolel->uWeight2;
				if (nWeight)
				{
					if (!PackWeights(pchKey, cchOut, cchMaxOut, nWeight, fEven))
						return E_FAIL;
				}
			}
		}
	}
	//uWeight3 is normally a case indicator.  If fcoIgnoreCase is set, case is ignored,
	//therefore uWeight3 is ignored in the sort key.
	if(colopt & fcoIgnoreCase)
	{
		*pcchKey = cchOut;
		return S_OK;
	}
	//enter level separator into array
	if(pchKey)
	{
		*pchKey++ = 0x0001;
	}
	cchOut++;
	if(cchOut > cchMaxOut)
		return E_FAIL;
	fEven = true;

	//Treating weight 3's like weight 2's
	for (pchSource = prgchSource; pchSource < pchLimSource; pchSource++)
	{
		ComBool fHasDecomp;
		// If there is no decomposition, this routine copies the one character we passed into
		// rgchDecomp.  Otherwise, it puts the decomposition there.
		CheckHr(m_qcpe->FullDecompRgch(*pchSource, MAXDECOMP, rgchDecomp, &cchDecomp,
												&fHasDecomp));
		pchLim = rgchDecomp + cchDecomp;
		for (pch = rgchDecomp; pch < pchLim; pch++)
		{
			int icolel = FindColel(*pch);
			if(icolel == -1) //use the standard weights
			{
				if (!PackWeights(pchKey, cchOut, cchMaxOut, 0x02, fEven))
					return E_FAIL;
				continue;
			}
			pcolel = const_cast<CollatingElement *>(g_prgcolel + icolel);
			if((colopt & fcoDontIgnoreVariant) == 0)
			{
				if(pcolel->Variant())
					continue;
			}

			int ccolel = 1; // by default we have just one, the one we point at now
			if (pcolel->Multiple())
			{
				// there are several to process
				ccolel = pcolel->uWeight3; // before we change pcolel!
				// move pcolel to point at the list in the multiple array
				pcolel = const_cast<CollatingElement *>(g_prgcolelMultiple + pcolel->MultipleIndex());
			}
			pcolelLim = pcolel + ccolel;
			for(;pcolel < pcolelLim; pcolel++)
			{
				int nWeight;
				nWeight = pcolel->uWeight3;
				if (nWeight)
				{
					if (!PackWeights(pchKey, cchOut, cchMaxOut, nWeight, fEven))
						return E_FAIL;
				}
			}
		}
	}
	if(pchKey)//level separator
	{
		*pchKey++ = 0x0001;
	}
	cchOut++;
	if(cchOut > cchMaxOut)
		return E_FAIL;

	//add the actual characters to the sort key
	for (pchSource = prgchSource; pchSource < pchLimSource; pchSource++)
	{
		cchOut++;
		if (cchOut > cchMaxOut)
			return E_FAIL;
		if (pchKey)
			*pchKey++ = *pchSource;
	}
	*pcchKey = cchOut;
	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Generate the sort key as a BSTR
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::get_SortKey(BSTR bstrValue, LgCollatingOptions colopt,
	BSTR * pbstrKey)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrValue);
	ChkComOutPtr(pbstrKey);

	HRESULT hr;
	int cchw;
	*pbstrKey = NULL;
	// Passing 0 and null just produces a length
	IgnoreHr(hr = SortKeyRgch(bstrValue, BstrLen(bstrValue), colopt, 0, NULL, &cchw));
	if (FAILED(hr))
		return hr;

	BSTR bstrOut;
	bstrOut = SysAllocStringLen(NULL, cchw);
	if (!bstrOut)
		return E_OUTOFMEMORY;
	IgnoreHr(hr = SortKeyRgch(bstrValue, BstrLen(bstrValue), colopt, cchw, bstrOut, &cchw));
	if (FAILED(hr))
	{
		SysFreeString(bstrOut);
		return hr;
	}
	*pbstrKey = bstrOut;
	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}

/*----------------------------------------------------------------------------------------------
	Do a direct string comparison.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::Compare(BSTR bstrValue1, BSTR bstrValue2,
	LgCollatingOptions colopt, int * pnVal)
{
	BEGIN_COM_METHOD
	ChkComBstrArg(bstrValue1);
	ChkComBstrArg(bstrValue2);
	ChkComOutPtr(pnVal);

	HRESULT hr;

	int cchw1;
	int cchw2;

	IgnoreHr(hr = SortKeyRgch(bstrValue1, BstrLen(bstrValue1), colopt, 0, NULL, &cchw1));
	if (FAILED(hr))
		return hr;
	IgnoreHr(hr = SortKeyRgch(bstrValue2, BstrLen(bstrValue2), colopt, 0, NULL, &cchw2));
	if (FAILED(hr))
		return hr;

	OLECHAR * pchKey1 = (OLECHAR *) _alloca(cchw1 * isizeof(OLECHAR));
	OLECHAR * pchKey2 = (OLECHAR *) _alloca(cchw2 * isizeof(OLECHAR));

	IgnoreHr(hr = SortKeyRgch(bstrValue1, BstrLen(bstrValue1), colopt, cchw1, pchKey1, &cchw1));
	if (FAILED(hr))
		return hr;
	IgnoreHr(hr = SortKeyRgch(bstrValue2, BstrLen(bstrValue2), colopt, cchw2, pchKey2, &cchw2));
	if (FAILED(hr))
		return hr;
	int nVal = wcsncmp(pchKey1, pchKey2, min(cchw1, cchw2));
	if (!nVal)
	{
		// equal as far as length of shortest key
		if (BstrLen(bstrValue1) < BstrLen(bstrValue2))
			nVal = -1;
		else if (BstrLen(bstrValue1) > BstrLen(bstrValue2))
			nVal = 1;
	}
	*pnVal = nVal;
	END_COM_METHOD(g_fact, IID_ILgCollatingEngine);
}


/*----------------------------------------------------------------------------------------------
	Get the writing system factory for this Unicode TR10 collator.

	@param ppwsf Address where to store a pointer to the writing system factory that stores/produces
					this old writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::get_WritingSystemFactory(ILgWritingSystemFactory ** ppwsf)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(ppwsf);
	AssertPtr(m_qwsf);

	*ppwsf = m_qwsf;
	if (*ppwsf)
		(*ppwsf)->AddRef();

	END_COM_METHOD(g_fact, IID_IWritingSystem);
}

/*----------------------------------------------------------------------------------------------
	Set the writing system factory for this Unicode TR10 collator.

	@param pwsf Pointer to the writing system factory that stores/produces this old writing system.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::putref_WritingSystemFactory(ILgWritingSystemFactory * pwsf)
{
	BEGIN_COM_METHOD;
	ChkComArgPtrN(pwsf);

	m_qwsf = pwsf;

	END_COM_METHOD(g_fact, IID_IWritingSystem);
}


/*----------------------------------------------------------------------------------------------
	Generate the sort key as a "SAFEARRAY".
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::get_SortKeyVariant(BSTR bstrValue, LgCollatingOptions colopt,
	VARIANT * psaKey)
{
	return E_NOTIMPL;
}

/*----------------------------------------------------------------------------------------------
	Do a direct string comparison using "SAFEARRAY"s.
	Note that, contrary to what the contract implies, this routine is not more
	efficient than the client just retrieving the keys and comparing them.
	OPTIMIZE: would we benefit significantly by implementing this using CompareString?
	Unfortunately, it is hard to avoid the need to do the WideCharToMultiByte conversion
	for the whole of both strings...
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::CompareVariant(VARIANT saValue1, VARIANT saValue2,
	LgCollatingOptions colopt, int * pnVal)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	Initialize the collating engine to the given locale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::Open(BSTR bstrLocale)
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	Close any open collating engine to the given locale.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgUnicodeCollater::Close()
{
	return E_NOTIMPL;
}


/*----------------------------------------------------------------------------------------------
	This method returns the index of the collating element found.  If the collating element
	is not found, it returns -1.
----------------------------------------------------------------------------------------------*/
int LgUnicodeCollater::FindColel(OLECHAR ch)
{
	int msb;
	int lsb;

	msb = ch >> 8;
	lsb = ch & 0xff;

	if((g_prgicolelPage[msb] == -1) || (lsb > g_prgccolelPage[msb] + 1))
	{
		return -1;
	}

	//the collating element is not predictable
	return g_rgicolelPage[msb] + lsb;
}

/*----------------------------------------------------------------------------------------------
	This method is used to assign weight 2s and weight 3s to the correct bytes of the OLECHAR.
	fEven flags whether the number of weights is even or odd.  If the weight is even, allocate
	a key character and put weight in MSB, otherwise put weight in LSB.  Pad the LSB of the
	OLECHAR with zero in the case of an odd number of weight 2's or weight 3's.  Note that it
	logically follows that the LSB will be padded with zero in the case of an odd number of
	weights, but this is not necessary.  Unless there is an equal number and comparison of
	Weight 1's, weight 2's and 3's will not be considered, therefore when they are considered,
	whether the MSB or LSB is padded with a zero is irrelevent.  This method returns true
	unless out of buffer space.
----------------------------------------------------------------------------------------------*/
bool LgUnicodeCollater::PackWeights(OLECHAR *&pchKey, int &cchOut, int cchMaxOut, int nWeight,
	bool & fEven)
{
	if (!nWeight)
		return true; // zero weights are ignored
	if (fEven)
	{
		// first character of a pair. Allocate a key character, and put weight in the MSB
		cchOut++;
		if (cchOut > cchMaxOut)
			return false;
		if (pchKey)
			*pchKey++ = (OLECHAR)(nWeight << 8);
	}
	else
	{
		// second of a pair. Put in the lsb of the previous key character
		if (pchKey)
			*(pchKey - 1) |= nWeight;
	}
	fEven = !fEven;
	return true;
}
