/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgNumericEngine.cpp
Responsibility: John Thomson
Last reviewed: Not yet.

Description:  An engine that converts numbers to and from binary which can be customized by
			  setting the four variables used for decimal separator, thousands separator,
			  exponential notation, and minus.
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "Main.h"

#pragma hdrstop
#include "limits.h"
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

LgNumericEngine::LgNumericEngine()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_chMinus = '-';
	m_chDecimal = '.';
	m_chComma = ',';
	m_chExp = 'E';
}

LgNumericEngine::~LgNumericEngine()
{
	ModuleEntry::ModuleRelease();
}

//:>********************************************************************************************
//:>	   Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	"SIL.Language1.LgNumericEngine",
	&CLSID_LgNumericEngine,
	"SIL simple numeric converter engine",
	"Apartment",
	&LgNumericEngine::CreateCom);


void LgNumericEngine::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgNumericEngine> qlne;
	qlne.Attach(NewObj LgNumericEngine());		// ref count initialy 1
	CheckHr(plne->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	   IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgNumericEngine::QueryInterface(REFIID riid, void **ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ISimpleInit *>(this));
	else if (riid == IID_ILgNumericEngine)
		*ppv = static_cast<ILgNumericEngine *>(this);
	else if (riid == IID_ISimpleInit)
		*ppv = static_cast<ISimpleInit *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<ILgNumericEngine *>(this),
			IID_ISimpleInit, IID_ILgNumericEngine);
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

		IClassInitMonikerPtr acim;
		qcim.CreateInstance(CLSID_LgNumericEngine);
		CheckHr(qcim->InitNew((BYTE*)L"-.,E", 4 * isizeof(wchar)));
		ILgNumericEnginePtr qnumeng;
		CheckHr(qcim->QueryInterface(IID_ILgNumericEngine, (void **)&qnumeng));
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::InitNew(const BYTE * prgb, int cb)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgb, cb);

	if (cb != 4 * isizeof(wchar))
		ThrowHr(WarnHr(E_INVALIDARG));

	m_chMinus = ((const wchar *)prgb)[0];
	m_chDecimal = ((const wchar *)prgb)[1];
	m_chComma = ((const wchar *)prgb)[2];
	m_chExp = ((const wchar *)prgb)[3];

	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

/*----------------------------------------------------------------------------------------------
	Return the initialization value previously set by InitNew.

	@param pbstr Pointer to a BSTR for returning the initialization data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_InitializationData(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	ThrowInternalError(E_NOTIMPL);
	END_COM_METHOD(g_fact, IID_ISimpleInit);
}


//:>********************************************************************************************
//:>	   ILgNumericEngine Methods
//:>********************************************************************************************
/*----------------------------------------------------------------------------------------------
	Default version of an integer, e.g., 123456, -654321
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_IntToString(int n, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);

	IntegerToString(false, pbstr, n); //false indicates not a pretty string

	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}

/*----------------------------------------------------------------------------------------------
	This version has commas or the equivalent, e.g., 12,345,678
 ----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_IntToPrettyString(int n, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);

	IntegerToString(true, pbstr, n); //true indicates a pretty string

	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}

/*----------------------------------------------------------------------------------------------
	Converts back anything IntToString could output
	If result cannot be represented in 32 bit signed, fails.
	If any characters are not used up in the string (except white space), fails.
	Can handle leading and trailing white space.
	ENHANCE: should it also handle output of IntToPrettyString?
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_StringToInt(BSTR bstr, int * pn)
{
	BEGIN_COM_METHOD
	ChkComBstrArgN(bstr);
	ChkComOutPtr(pn);

	OLECHAR * pch;
	int ichUnused; //holds index of first unused character after call to StringToIntRgch
	int nLength = BstrLen(bstr);
	OLECHAR * pchLim;

	IgnoreHr(hr = StringToIntRgch(bstr, nLength, pn, &ichUnused));
	if(FAILED(hr))
		 return hr;
	pchLim  = bstr + nLength;
	pch = bstr + ichUnused;
	//test after ichUnused to make sure it's whitespace
	while(pch < pchLim)
	{
		if(isWhite(*pch))
			pch++;
		else
			return E_FAIL;
	}
	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}
/*----------------------------------------------------------------------------------------------
	Basically the same functionality, but also returns the index of the first
	character not processed as part of the number, instead of failing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::StringToIntRgch(OLECHAR * prgch, int cch, int * pn,
	int * pichUnused)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgch, cch);
	ChkComOutPtr(pn);
	ChkComOutPtr(pichUnused);

	bool fNeg = false; //flags if the string to be converted is negative
	_int64 nResult = 0; //_int64 is used for testing whether the limit of an int is passed
	_int64 nLimit =(_int64)INT_MAX;

	OLECHAR * pch = prgch;
	OLECHAR * pchLim = prgch + cch;
	*pichUnused = 0;
	*pn = 0;
	//tests for leading whitespace
	while(pch != pchLim)
	{
		if(isWhite(*pch))
			pch++;
		else
			break;
	}
	//the string is completely whitespace or empty
	if(pch >= pchLim)
		return E_FAIL;

	//tests if first non-whitespace character is m_chMinus
	if(*pch == m_chMinus)
	{
		fNeg = true;
		nLimit = -(_int64)INT_MIN;
		pch++;
	}

	//the first character (possibly after minus) is not a digit or the string is empty
	if(pch >= pchLim || !(isDigit(*pch)))
		return E_FAIL;

	while(pch != pchLim)
	{
		//if the character is a numerical character, add it to result
		if(isDigit(*pch))
		{
			nResult = nResult * 10 + (*pch - '0');
			if(nResult > nLimit)
			{
				return E_FAIL;
			}
			pch++;
		}
		else //character is non-numerical
			break;
	}

	if(fNeg)
		*pn = (int)(-nResult);
	else
		*pn = (int)nResult;

	*pichUnused = pch - prgch;
	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
} //end of StringToIntRgch

// ENHANCE: should we have hex or other base output?

/*----------------------------------------------------------------------------------------------
	Default version of a double, e.g., 1234.56, -6543.21
	If the double is greater than 10^15, switch to exponential notation
	If cchFracDigits > 15, it will be treated as 15.
	Arguments:
	cchFracDigits		 number of digits after decimal
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_DblToString(double dbl, int cchFracDigits, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (!(cchFracDigits >= 0))
		ThrowInternalError(E_INVALIDARG);

	DblToString(false, cchFracDigits, pbstr, dbl); //false indicates not a pretty string

	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}

/*----------------------------------------------------------------------------------------------
	This version has commas or the equivalent, e.g., 12,345,678.9
	If cchFracDigits > 15, will be treated as 15.
	Arguments:
	cchFracDigits		 number of digits after decimal
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_DblToPrettyString(double dbl, int cchFracDigits, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if (!(cchFracDigits >= 0))
		ThrowInternalError(E_INVALIDARG);

	DblToString(true, cchFracDigits, pbstr, dbl); //true indicates a pretty string

	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}

/*----------------------------------------------------------------------------------------------
	This version outputs a double in exp notation, e.g., 1.2345E3
	Note that if cchFracDigits > 15, it will be treated as 15
	Arguments:
	cchFracDigits		 number of digits after decimal
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_DblToExpString(double dbl, int cchFracDigits, BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComOutPtr(pbstr);
	if(!(cchFracDigits >= 0))
		ThrowInternalError(E_INVALIDARG);

	OLECHAR rgchBuf [30];
	OLECHAR * pch = rgchBuf;
	char *rgchEcvt;//buffer that stores double converted to string
	char *pchStr;//pointer to move through Ecvt buffer
	int nDec; //keeps track of position of decimal for _ecvt
	int nSign;//keeps track of sign for _ecvt
	OLECHAR  rgchItow [10]; //buffer that stores integer exponent converted to string
	OLECHAR * pchItow = rgchItow;//pointer to move through Itow buffer

	if(cchFracDigits >= 15)
		cchFracDigits = 15;
	rgchEcvt = _ecvt(dbl, cchFracDigits + 1, &nDec, &nSign);//to convert double to string
	pchStr = rgchEcvt;				  //_ecvt uses a single statically allocated buffer

	//if the double is negative, put a minus on the array
	if(nSign != 0)
		*pch++ = m_chMinus;
	//enter first digit and decimal into array
	*pch++ = *pchStr++;
	*pch++ = m_chDecimal;

	//put the digits after the decimal on the array
	for(int cch = 0; cch < cchFracDigits; cch++)
	{
		if (*pchStr == '\0')
		{
			*pch++ = '0';
		}
		else
			*pch++ =(OLECHAR) *pchStr++;
	}

	//place E in string
	*pch++ = m_chExp;

	if(dbl == 0)
	{
		*pch++ =(OLECHAR)('0');
		*pch++ = '\0';
	}
	else
	{
		_itow(nDec - 1, pchItow, 10);  //convert integer exponent to string
		if(*pchItow == '-')			//using customized minus
		{
			*pch++ = m_chMinus;
			pchItow++;
		}
		while(*pchItow != '\0')  //copy exponent into result
		{
			*pch++ = *pchItow++;
		}
		*pch++ = (OLECHAR)'\0';
	}
	*pbstr = SysAllocString(rgchBuf);

	if(!pbstr)
		return E_OUTOFMEMORY;

	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}


/*----------------------------------------------------------------------------------------------
	Converts back anything DblToString or DblToExpString could output
	If any characters are not used up in the string (except white space), fails.
	Can handle leading and trailing white space.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::get_StringToDbl(BSTR bstr, double * pdbl)
{
	BEGIN_COM_METHOD
	ChkComArgPtr(pbstr);
	ChkComArgPtr(pdbl);

	HRESULT hr;
	OLECHAR *pch;
	int ichUnused;	//holds index of first unused character
	int nLength = BstrLen(bstr);
	OLECHAR *pchLim;

	IgnoreHr(hr = StringToDblRgch(bstr, nLength, pdbl, &ichUnused));
	if (FAILED(hr))
		return hr;

	pch = bstr + ichUnused;
	pchLim  = bstr + nLength;
	//test after ichUnused to make sure it's whitespace
	while(pch < pchLim)
	{
		if(isWhite(*pch))
			pch++;
		else
			return E_FAIL;
	}
	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}

/*----------------------------------------------------------------------------------------------
	Basically the same functionality, but also returns the index of the first
	character not processed as part of the number, instead of failing.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgNumericEngine::StringToDblRgch(OLECHAR * prgch, int cch, double * pdbl,
	int * pichUnused)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgch, cch);
	ChkComArgPtr(pdbl);
	ChkComArgPtr(pichUnused);

	OLECHAR * pch = prgch;
	OLECHAR * pchLim = prgch + cch;
	OLECHAR rgchExp [10];  //a buffer to hold exponent
	OLECHAR *pchExp = rgchExp;
	bool fNeg = false; //flags if string is negative
	double dbl = 0.0;
	double mult = 10;//used for calculating double
	int nExp = 0;//will contain exponent
	bool fInt = false; //flags if there is a non fraction part to the string

	//skip all leading whitespace
	while(isWhite(*pch) && (pch != pchLim))
	{
		pch++;
	}

	//the string is completely whitespace
	if(pch >= pchLim)
		return E_FAIL;

	//check if first non-whitespace character is minus
	if(*pch == m_chMinus)
	{
		fNeg = true;
		pch++;
	}

	//first character (possibly after minus) must either be a digit or a decimal
	if(pch >= pchLim || (!isDigit(*pch) && *pch != m_chDecimal))
		return E_FAIL;

	//begins calculating non-fraction part of double
	while(pch != pchLim && isDigit(*pch))
	{
		fInt = true;
		dbl = dbl * mult + (*pch - '0');
		pch++;
	}

	if(*pch == m_chDecimal)
	{
		mult = 0.1;
		pch++;

		//there are no non-fraction digits, and there is no digit after the decimal causing failure
		if(!fInt && (pch == pchLim || !isDigit(*pch)))
			return E_FAIL;
		//calculates fraction part of double
		while(pch != pchLim && isDigit(*pch))
		{
			dbl = dbl + ((*pch - '0') * mult);
			mult = mult * 0.1;
			pch++;
		}
	}

	if(*pch == m_chExp)
	{
		pch++;
		if(*pch == m_chMinus)
		{
			*pchExp++ = (OLECHAR)'-';
			pch++;
		}

		//first character in exponent (possibly after minus) must be a digit
		if(pch >= pchLim || !isDigit(*pch))
			return E_FAIL;

		//extracts exponent from string
		int cch = 0;
		while(isDigit(*pch) && pch != pchLim)  //only calculate up to four decimal digits
		{									   //an exponent > 308 will approach infinity
			if(cch < 4)
				*pchExp++ = *pch++;
			else
				pch++;
			cch++;
		}
		*pchExp = '\0';
		nExp = _wtoi(rgchExp);//converts exponent string to integer
		dbl = dbl * pow(10,nExp);//calculates double
	}

	*pdbl = dbl;
	if(fNeg)
		*pdbl = -dbl;
	*pichUnused = pch - prgch;
	END_COM_METHOD(g_fact, IID_ILgNumericEngine);
}
/*-----------------------------------------------------------------------------------------
	This method converts an integer to a plain string or pretty string (with thousands
	separators).  It fills rgchBuf from the end toward the beginning, and then uses this
	to allocate the BSTR.
-----------------------------------------------------------------------------------------*/
void LgNumericEngine::IntegerToString(bool fPretty, BSTR * pbstr, int n)
{
	OLECHAR rgchBuf[20]; //maximum possible int is about 4 billion which fits in this array
	OLECHAR * pch = rgchBuf + 19;
	*pch-- = '\0';

	int cch = 0;
	int nNumber = abs(n);

	//the integer is zero
	if(n == 0)
		*pch-- = (OLECHAR)(0 + '0');

	while (nNumber > 0)
	{
		*pch-- = (OLECHAR)((nNumber % 10) + '0');
		nNumber /= 10;
		cch++;

		//enters comma into array after every 3 digits
		if((cch % 3 == 0) && (nNumber != 0) && (fPretty))
			*pch-- = m_chComma;
	}
	//the integer is negative
	if(n < 0)
		*pch-- = m_chMinus;

	pch++; //point at the initial character of the number

	*pbstr = SysAllocString(pch);
	if(!pbstr)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
}

/*-----------------------------------------------------------------------------------------
	Method to convert double to plain string or pretty string (with thousands separators).
	This method fills a buffer from last toward first by converting the fraction part first
	then the non-fraction part.
-----------------------------------------------------------------------------------------*/
void LgNumericEngine::DblToString(bool fPretty, int cchFracDigits, BSTR * pbstr,
									 double dbl)
{
	OLECHAR rgchBuf [40];//cchFracDigits will not be > 15, when non-fraction part > 10^15,
						 //also takes into account possibility of 5 commas, a negative sign
						 //and a decimal point.
	rgchBuf[39] = '\0';


	OLECHAR * pch = rgchBuf+38;
	char * pchEcvt; //will contain fraction part of double converted to a string
	char * pchLast; //will contain last character in pchEcvt
	int nDec; //passed into ecvt to hold index of decimal position
	int nSign; //passed into ecvt to get the sign of the double
	double dblTemp;
	int cchCount = 0;

	dblTemp = dbl;

	if(cchFracDigits > 15) //a double's decimal precision is 15
		cchFracDigits = 15;


	if(dbl > pow(10,15))
	{
		return get_DblToExpString(dbl, cchFracDigits, pbstr);
	}

	//storing absolute value in dblTemp
	if (dblTemp < 0)
		dblTemp = -dblTemp;

	_int64 n = (_int64) dblTemp;//cast double to int for non fraction part of number

	double dblDec = dblTemp - n; //to get the fraction part of the double
	pchEcvt = _ecvt(dblDec, cchFracDigits, &nDec, &nSign);//to convert fraction to string
										//_ecvt use a single statically allocated buffer
	pchLast = pchEcvt + cchFracDigits - 1;

	//enter fraction into array
	for (int cch = 0; cch < cchFracDigits; cch++)
	{
		if(dblDec == 0)
			*pch-- = '0';
		else
		{
			*pch-- = (OLECHAR) *pchLast;
			pchLast--;
		}
	}

	//enter decimal character into array
	*pch-- = m_chDecimal;

	/*The following is the same as IntegerToString except for precision.  In this case, _int64 is
	used rather than int in order to handle the full precision of a double.*/
	if(n == 0) //compare zero with dbl if a zero before the decimal point is not necessary
		*pch-- = '0';

	//enter non-fraction part of double into array
	while(n > 0)
	{
		*pch-- = (OLECHAR)((n % 10) + '0');
		n /= 10;
		cchCount++;

		//entering commas in appropriate places
		if((cchCount % 3 == 0) && (n != 0) && fPretty)
			*pch-- = m_chComma;
	}
	if(dbl < 0)
		*pch-- = m_chMinus;
	pch++;//point at the initial character of the number

	*pbstr = SysAllocString(pch);
	if(!pbstr)
		ThrowHr(WarnHr(E_OUTOFMEMORY));
}
