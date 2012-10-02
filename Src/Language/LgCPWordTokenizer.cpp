/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgCPWordTokenizer.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:  A Tokenizer that finds word breaks by looking for sequences of word-forming
			  tokens.  A word-forming token is recognized by the IsWordforming function
			  contained in the header file.  The IsWordforming function recognizes capital
			  and lowercase letters to be word-forming by using the predefined function isalpha.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"

#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

//:>********************************************************************************************
//:>	   Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Local Constants and static variables
//:>********************************************************************************************

//:>********************************************************************************************
//:>	   Constructor/Destructor
//:>********************************************************************************************

LgCPWordTokenizer::LgCPWordTokenizer()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
}

LgCPWordTokenizer::~LgCPWordTokenizer()
{
	ModuleEntry::ModuleRelease();
}
//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	"SIL.Language.LgCPWordTokenizer",
	&CLSID_LgCPWordTokenizer,
	"SIL char property based word tokenizer",
	"Apartment",
	&LgCPWordTokenizer::CreateCom);


void LgCPWordTokenizer::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgCPWordTokenizer> qcpwt;
	qcpwt.Attach(NewObj LgCPWordTokenizer());		// ref count initialy 1
	CheckHr(pcpwt->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgCPWordTokenizer::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgTokenizer *>(this));
	else if (riid == IID_ILgTokenizer)
		*ppv = static_cast<ILgTokenizer *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<IRenderEngine *>(this),
			IID_ISimpleInit, IID_ILgTokenizer);
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
	hr = qcim->InitNew(CLSID_LgCPWordTokenizer, NULL, 0);

	Note that no init data is required by this implementation which is based on standard
	character properties.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgCPWordTokenizer::InitNew(const BYTE * prgb, int cb)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgb, cb);

	// Nothing to do at present.
	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

/*----------------------------------------------------------------------------------------------
	Return the initialization value previously set by InitNew.

	@param pbstr Pointer to a BSTR for returning the initialization data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgCPWordTokenizer::get_InitializationData(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	// Leave output null.
	END_COM_METHOD(g_fact, IID_ISimpleInit);
}


//:>********************************************************************************************
//:>	   ILgTokenizer Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Get the next token of whatever kind this tokenizer supports from the input string.
	Return E_FAIL if there are no more. (Also set *pichMin and *pichLim to -1.)
	ENHANCE: should we pass an ichFirst?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgCPWordTokenizer::GetToken(OLECHAR * prgchInput, int cch, int * pichMin,
	int * pichLim)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchInput, cch);
	ChkComArgPtr(pichMin);
	ChkComArgPtr(pichLim);

	// checks arguments

	int cchCount = 0;
	bool fFirst = false; // flags if a wordforming character has been found

	// steps through input
	while(cchCount < cch)
	{
		// checks to see if input is a wordforming character
		if(IsWordforming(*prgchInput))
		{
			// input is the first wordforming character
			if(!fFirst)
			{
				*pichMin = cchCount;
				fFirst = true;
			}
		}
		// if input is not a wordforming character and pichMin has been set
		else
		{
			if(fFirst)
			{
				*pichLim = cchCount;
				return S_OK;
			}
		}
		prgchInput++;
		cchCount++;
	}
	// no token is found
	if(!fFirst)
	{
		*pichMin = *pichLim = -1;
		return E_FAIL;
	}
	// token goes to the end of input
	else
		*pichLim = cch;
	END_COM_METHOD(g_fact, IID_ILgTokenizer);
}

/*----------------------------------------------------------------------------------------------
	For VB, get the start of the first token that begins at or after ichFirst
	Characters before ichFirst are not examined; the result is as if the string
	began at ichFirst.
	Return E_FAIL if no token found
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgCPWordTokenizer::get_TokenStart(BSTR bstrInput, int ichFirst, int * pichMin)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstrInput);
	ChkComArgPtr(pichMin);
	if (ichFirst < 0)
		ThrowInternalError(E_INVALIDARG, "Negative char offset");

	OLECHAR * pchInput;
	int cch = 0;
	pchInput = bstrInput + ichFirst;

	while (*pchInput != '\0')
	{
		// if the input is wordforming, assign to pichMin and exit loop
		if(IsWordforming(*pchInput))
		{
			*pichMin = cch;
			return S_OK;
		}
		pchInput++;
		cch++;
	}
	// a wordforming character is not found
	return E_FAIL;
	END_COM_METHOD(g_fact, IID_ILgTokenizer);
}

/*----------------------------------------------------------------------------------------------
	For VB, get the end of the first token that BEGINS at or after ichFirst.
	Note: ichFirst may be the result obtained from a previous call to TokenStart,
	rather than the value passed to TokenStart, but to obtain the limit of the same
	token it must not be larger than that. In other words, this method does NOT
	find the first end-of-token at or after ichFirst: it must find a complete token
	starting there.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgCPWordTokenizer::get_TokenEnd(BSTR bstrInput, int ichFirst, int * pichLim)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstrInput);
	ChkComArgPtr(pichLim);
	if (ichFirst < 0)
		ThrowInternalError(E_INVALIDARG, "Negative char offset");

	OLECHAR * pchInput;
	bool fFirst = false;
	int cch = 0;
	pchInput = bstrInput + ichFirst;

	while(*pchInput != '\0')
	{
		if(IsWordforming(*pchInput))
		{
			// if first wordforming character is found
			if(!fFirst)
				fFirst = true;
		}
		else
		{
			// a non-wordforming character is found after fFirst is true
			if(fFirst)
			{
				*pichLim = cch;
				return S_OK;
			}
		}
		cch++;
		pchInput++;
	}
	// if no wordforming characters are found
	if(!fFirst)
		return E_FAIL;

	// end of string is reached before finding pichLim
	else
		*pichLim = cch;
	END_COM_METHOD(g_fact, IID_ILgTokenizer);
}
