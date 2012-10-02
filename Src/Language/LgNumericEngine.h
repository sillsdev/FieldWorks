/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 1999, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: LgNumericEngine.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:

-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef LGNUMERICENGINE_INCLUDED
#define LGNUMERICENGINE_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: LgNumericEngine
Description: An engine that converts numbers to and from binary which can be customized by
			 setting the four variables used for decimal separator, thousands separator,
			 exponential notation, and minus.
Hungarian: lne
----------------------------------------------------------------------------------------------*/
class LgNumericEngine :
	public ILgNumericEngine,
	public ISimpleInit
{
public:

	// Static methods
	static void CreateCom(IUnknown *punkOuter, REFIID iid, void ** ppv);

	// Constructors/destructors/etc.
	LgNumericEngine();
	virtual ~LgNumericEngine();

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

	// ISimpleInit Methods
	STDMETHOD(InitNew)(const BYTE * prgb, int cb);
	STDMETHOD(get_InitializationData)(BSTR * pbstr);

	// ILgNumericEngine methods
	STDMETHOD(get_IntToString)(int n, BSTR * bstr);
	STDMETHOD(get_IntToPrettyString)(int n, BSTR * bstr);
	STDMETHOD(get_StringToInt)(BSTR bstr, int * pn);
	STDMETHOD(StringToIntRgch)(OLECHAR * prgch, int cch, int * pn, int * pichUnused);
	STDMETHOD(get_DblToString)(double dbl, int cchFracDigits, BSTR * bstr);
	STDMETHOD(get_DblToPrettyString)(double dbl, int cchFracDigits, BSTR *bstr);
	STDMETHOD(get_DblToExpString)(double dbl, int cchFracDigits, BSTR * bstr);
	STDMETHOD(get_StringToDbl)(BSTR bstr, double * pdbl);
	STDMETHOD(StringToDblRgch)(OLECHAR * prgch, int cch, double * pdbl, int * pichUnused);

	// Member variable access

	// Other public methods
protected:
	// Member variables
	long m_cref;				// standard COM ref count
	OLECHAR m_chMinus;
	OLECHAR m_chDecimal;
	OLECHAR m_chComma;
	OLECHAR m_chExp;

	// Static methods

	// Constructors/destructors/etc.

	// Other protected methods
	void IntegerToString(bool fPretty, BSTR * pbstr, int n);
	void DblToString(bool fPretty, int cchFracDigits, BSTR * pbstr, double dbl);
	bool isWhite(OLECHAR ch)
	{
		if(ch == ' ')
			return true;
		return false;
	}
	bool isDigit(OLECHAR ch)
	{
		if(ch >= '0' && ch <= '9')
			return true;
		return false;
	}
};
#endif  //LGNUMERICENGINE_INCLUDED
